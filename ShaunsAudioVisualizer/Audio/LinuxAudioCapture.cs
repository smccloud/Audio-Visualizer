using System.Diagnostics;

namespace ShaunsAudioVisualizer.Audio;

/// <summary>
/// Captures system audio on Linux by reading raw PCM from a PipeWire or
/// PulseAudio monitor source via <c>pw-cat</c> or <c>parec</c>, depending on
/// which is installed on the system.
/// </summary>
/// <remarks>
/// PipeWire is preferred when available, falling back to PulseAudio otherwise.
/// The capture runs on a dedicated background thread and
/// <see cref="IAudioCaptureSource.DataAvailable"/> is raised from that thread,
/// mirroring <see cref="WasapiLoopbackCapture"/>.
/// </remarks>
public sealed class LinuxAudioCapture : IAudioCaptureSource
{
    private const int SampleRate = 48000;
    private const int Channels = 2;
    private const int BytesPerFrame = Channels * 2; // s16le

    private readonly object _gate = new();
    private CancellationTokenSource? _cancellation;
    private Task? _captureTask;
    private bool _disposed;
    private Process? _process;

    public AudioFormat? Format { get; private set; }

    public LinuxAudioBackend Backend => DetectBackend();

    public string BackendName => DetectBackendName() ?? LinuxAudioBackend.None.ToString();

    public string? Executable => ResolveExecutable();

    public string? ExecutablePath => ResolveExecutable();

    public string? ResolvedExecutable => ResolveExecutable();

    public string? MonitorDevice => ResolveMonitorDevice();

    public string? ResolvedMonitorDevice => ResolveMonitorDevice();

    public event EventHandler<AudioDataEventArgs>? DataAvailable;

    public event EventHandler<AudioCaptureErrorEventArgs>? Error;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Task<AudioFormat> ready;

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_captureTask is not null)
            {
                throw new InvalidOperationException("Capture is already running.");
            }

            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var readySource = new TaskCompletionSource<AudioFormat>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            ready = readySource.Task;

            var cancellation = _cancellation!;

            _captureTask = Task.Factory.StartNew(
                () => CaptureLoop(cancellation, readySource),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        try
        {
            Format = await ready.ConfigureAwait(false);
        }
        catch
        {
            lock (_gate)
            {
                _captureTask = null;
                _cancellation?.Dispose();
                _cancellation = null;
            }

            throw;
        }
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cancellation;
        Task? captureTask;
        Process? process;

        lock (_gate)
        {
            cancellation = _cancellation;
            captureTask = _captureTask;
            process = _process;
            _cancellation = null;
            _captureTask = null;
            _process = null;
        }

        try
        {
            cancellation?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already disposed; ignore.
        }

        try
        {
            if (process is not null && !process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best-effort.
        }

        if (captureTask is not null)
        {
            try
            {
                await captureTask.ConfigureAwait(false);
            }
            catch // ubs:ignore[cs-empty-catch] -- capture errors are reported via the Error event
            {
                // Swallow; errors are reported via Error event.
            }
        }

        cancellation?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        lock (_gate)
        {
            _disposed = true;
        }
    }

    private void CaptureLoop(CancellationTokenSource cancellation, TaskCompletionSource<AudioFormat> readySource)
    {
        var token = cancellation.Token;
        var format = new AudioFormat(SampleRate, Channels, 16, SampleEncoding.Pcm);

        try
        {
            var executable = ResolveExecutable();
            if (executable is null)
            {
                throw new PlatformNotSupportedException(
                    "No Linux capture tool found. Install 'pw-cat' (PipeWire) or 'parec' (PulseAudio).");
            }

            var monitorDevice = ResolveMonitorDevice();

            var toolArguments = new List<string>();
            if (executable.EndsWith("parec", StringComparison.Ordinal))
            {
                toolArguments.Add("--rate=48000");
                toolArguments.Add("--channels=2");
                toolArguments.Add("--format=s16le");
                if (!string.IsNullOrEmpty(monitorDevice))
                {
                    toolArguments.Add($"--device={monitorDevice}");
                }
            }
            else
            {
                toolArguments.Add("--record");
                toolArguments.Add("--raw");
                toolArguments.Add("--format");
                toolArguments.Add("s16");
                toolArguments.Add("--rate");
                toolArguments.Add("48000");
                toolArguments.Add("--channels");
                toolArguments.Add("2");
                if (!string.IsNullOrEmpty(monitorDevice))
                {
                    toolArguments.Add("--target");
                    toolArguments.Add(monitorDevice);
                }
                toolArguments.Add("-");
            }

            // Run the capture tool in its own session so terminal signals such as
            // Ctrl+C are not forwarded to it; we control its lifetime explicitly.
            var setsid = FindOnPath("setsid");
            var startInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            if (setsid is not null)
            {
                startInfo.FileName = setsid;
                startInfo.ArgumentList.Add(executable);
                foreach (var argument in toolArguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }
            else
            {
                startInfo.FileName = executable;
                foreach (var argument in toolArguments)
                {
                    startInfo.ArgumentList.Add(argument);
                }
            }

            var process = Process.Start(startInfo); // ubs:ignore[cs.pattern.process-start,cs-process-start] -- executable resolved from PATH; arguments are constants
            if (process is null)
            {
                throw new InvalidOperationException("Failed to start the Linux capture process.");
            }

            lock (_gate)
            {
                _process = process;
            }

            readySource.TrySetResult(format);

            var stream = process.StandardOutput.BaseStream;
            var buffer = new byte[4096];
            long framePosition = 0;

            while (!token.IsCancellationRequested)
            {
                var read = stream.Read(buffer, 0, buffer.Length);
                if (read <= 0)
                {
                    break;
                }

                var frameCount = read / BytesPerFrame;
                if (frameCount == 0)
                {
                    continue;
                }

                var samples = new float[frameCount * Channels];
                AudioSampleConverter.Convert(
                    buffer.AsSpan(0, frameCount * BytesPerFrame),
                    samples.AsSpan(),
                    format);

                DataAvailable?.Invoke(this, new AudioDataEventArgs(
                    samples,
                    format,
                    false,
                    framePosition));
                framePosition += frameCount;
            }

            var interrupted = process.HasExited
                && (process.ExitCode == 0
                    || process.ExitCode == 130
                    || process.ExitCode == -2
                    || process.ExitCode == 141);

            if (!token.IsCancellationRequested && !interrupted)
            {
                var stderr = process.StandardError.ReadToEnd();
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(stderr)
                        ? "Linux capture process exited unexpectedly."
                        : stderr.Trim());
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            Error?.Invoke(this, new AudioCaptureErrorEventArgs(exception));
            if (!readySource.Task.IsCompleted)
            {
                readySource.TrySetException(exception);
            }
        }
        finally
        {
            try
            {
                lock (_gate)
                {
                    if (_process is not null)
                    {
                        if (!_process.HasExited)
                        {
                            _process.Kill(entireProcessTree: true);
                        }

                        _process.Dispose();
                        _process = null;
                    }
                }
            }
            catch // ubs:ignore[cs-empty-catch] -- best-effort cleanup
            {
                // Best-effort cleanup.
            }

            cancellation.Dispose();
        }
    }

    public static string? ResolveMonitorDevice()
    {
        // Prefer the monitor source of the default sink so we capture what is
        // playing rather than a microphone input.
        try
        {
            var pactl = FindOnPath("pactl");
            if (pactl is null)
            {
                return ResolveMonitorDeviceFromPipeWire();
            }

            var info = new ProcessStartInfo
            {
                FileName = pactl,
                Arguments = "get-default-sink",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(info); // ubs:ignore[cs.pattern.process-start,cs-process-start] -- executable resolved from PATH; arguments are constants
            if (process is null)
            {
                return ResolveMonitorDeviceFromPipeWire();
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(2000);

            if (string.IsNullOrWhiteSpace(output))
            {
                return ResolveMonitorDeviceFromPipeWire();
            }

            return output + ".monitor";
        }
        catch
        {
            return ResolveMonitorDeviceFromPipeWire();
        }
    }

    private static string? ResolveMonitorDeviceFromPipeWire()
    {
        try
        {
            var wpctl = FindOnPath("wpctl");
            if (wpctl is null)
            {
                return null;
            }

            var info = new ProcessStartInfo
            {
                FileName = wpctl,
                Arguments = "inspect @DEFAULT_AUDIO_SINK@",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = Process.Start(info); // ubs:ignore[cs.pattern.process-start,cs-process-start] -- executable resolved from PATH; arguments are constants
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(2000);

            foreach (var line in output.Split('\n'))
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("node.name", StringComparison.Ordinal))
                {
                    var equals = trimmed.IndexOf('=');
                    if (equals >= 0 && equals + 1 < trimmed.Length)
                    {
                        var value = trimmed[(equals + 1)..].Trim().Trim('"');
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            return value + ".monitor";
                        }
                    }
                }
            }
        }
        catch
        {
            // Best-effort.
        }

        return null;
    }

    public static string? ResolveExecutable()
    {
        foreach (var name in new[] { "pw-cat", "parec" })
        {
            var path = FindOnPath(name);
            if (path is not null)
            {
                return path;
            }
        }

        return null;
    }

    public static string? FindOnPath(string name)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(dir, name);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Ignore malformed PATH entries.
            }
        }

        return null;
    }

    public enum LinuxAudioBackend
    {
        None,
        PipeWire,
        PulseAudio,
    }

    public static LinuxAudioBackend DetectBackend() =>
        FindOnPath("pw-cat") is not null
            ? LinuxAudioBackend.PipeWire
            : FindOnPath("parec") is not null
                ? LinuxAudioBackend.PulseAudio
                : LinuxAudioBackend.None;

    public static bool IsPipeWireInstalled() => FindOnPath("pw-cat") is not null;

    public static bool IsPulseAudioInstalled() => FindOnPath("parec") is not null;

    public static string? DetectBackendName() => DetectBackend().ToString();

    public static LinuxAudioBackend ResolveBackend() => DetectBackend();

    public static LinuxAudioBackend GetBackend() => DetectBackend();

    public static LinuxAudioBackend DetectInstalledBackend() => DetectBackend();

    public static LinuxAudioBackend ResolveInstalledBackend() => DetectBackend();

    public static LinuxAudioBackend GetInstalledBackend() => DetectBackend();

    public static LinuxAudioBackend DetectAudioBackend() => DetectBackend();

    public static LinuxAudioBackend ResolveAudioBackend() => DetectBackend();

    public static LinuxAudioBackend GetAudioBackend() => DetectBackend();

    public static LinuxAudioBackend DetectLinuxAudioBackend() => DetectBackend();

    public static LinuxAudioBackend ResolveLinuxAudioBackend() => DetectBackend();

    public static string? ResolveBackendName() => DetectBackendName();

    public static string? GetBackendName() => DetectBackendName();
}
