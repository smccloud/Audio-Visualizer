using System.Diagnostics;

namespace ShaunsAudioVisualizer.Audio;

/// <summary>
/// Captures system audio on Linux by reading raw PCM from a PulseAudio/PipeWire
/// monitor source via <c>parec</c> (falling back to <c>pw-cat</c>).
/// </summary>
/// <remarks>
/// This enables the console renderer on Linux. The capture runs on a dedicated
/// background thread and <see cref="IAudioCaptureSource.DataAvailable"/> is
/// raised from that thread, mirroring <see cref="WasapiLoopbackCapture"/>.
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
            catch
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
                    "No Linux capture tool found. Install 'parec' (PulseAudio) or 'pw-cat' (PipeWire).");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            var monitorDevice = ResolveMonitorDevice();

            if (executable.EndsWith("parec", StringComparison.Ordinal))
            {
                startInfo.ArgumentList.Add("--rate=48000");
                startInfo.ArgumentList.Add("--channels=2");
                startInfo.ArgumentList.Add("--format=s16le");
                if (!string.IsNullOrEmpty(monitorDevice))
                {
                    startInfo.ArgumentList.Add($"--device={monitorDevice}");
                }
            }
            else
            {
                startInfo.ArgumentList.Add("--record");
                startInfo.ArgumentList.Add("--format");
                startInfo.ArgumentList.Add("s16");
                startInfo.ArgumentList.Add("--rate");
                startInfo.ArgumentList.Add("48000");
                startInfo.ArgumentList.Add("--channels");
                startInfo.ArgumentList.Add("2");
                if (!string.IsNullOrEmpty(monitorDevice))
                {
                    startInfo.ArgumentList.Add("--target");
                    startInfo.ArgumentList.Add(monitorDevice);
                }
                startInfo.ArgumentList.Add("-");
            }

            var process = Process.Start(startInfo);
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

            if (!token.IsCancellationRequested)
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
            catch
            {
                // Best-effort cleanup.
            }

            cancellation.Dispose();
        }
    }

    private static string? ResolveMonitorDevice()
    {
        // Prefer the monitor source of the default sink so we capture what is
        // playing rather than a microphone input.
        try
        {
            var pactl = FindOnPath("pactl");
            if (pactl is null)
            {
                return null;
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

            using var process = Process.Start(info);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(2000);

            if (string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            return output + ".monitor";
        }
        catch
        {
            return null;
        }
    }

    private static string? ResolveExecutable()
    {
        foreach (var name in new[] { "parec", "pw-cat" })
        {
            var path = FindOnPath(name);
            if (path is not null)
            {
                return path;
            }
        }

        return null;
    }

    private static string? FindOnPath(string name)
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
}