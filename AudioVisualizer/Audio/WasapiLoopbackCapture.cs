using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AudioVisualizer.Audio.Interop;

namespace AudioVisualizer.Audio;

/// <summary>
/// Captures the audio that is currently playing on the default Windows playback
/// device using WASAPI loopback. Only documented Core Audio APIs are used.
/// </summary>
/// <remarks>
/// Loopback capture must run on Windows. Capture happens on a dedicated
/// background thread and <see cref="DataAvailable"/> is raised from that thread.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WasapiLoopbackCapture : IAudioCaptureSource
{
    private readonly object _gate = new();
    private CancellationTokenSource? _cancellation;
    private Task? _captureTask;
    private bool _disposed;

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

        lock (_gate)
        {
            cancellation = _cancellation;
            captureTask = _captureTask;
            _cancellation = null;
            _captureTask = null;
        }

        if (cancellation is null || captureTask is null)
        {
            return;
        }

        try
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
            await captureTask.ConfigureAwait(false);
        }
        finally
        {
            cancellation.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }

        await StopAsync().ConfigureAwait(false);
    }

    private void CaptureLoop(
        CancellationTokenSource cancellation,
        TaskCompletionSource<AudioFormat> ready)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        IAudioClient? audioClient = null;
        IAudioCaptureClient? captureClient = null;
        var mixFormatPointer = IntPtr.Zero;
        var comInitialized = false;

        try
        {
            var result = NativeMethods.CoInitializeEx(
                IntPtr.Zero,
                NativeMethods.CoInitMultithreaded);

            if (result < 0 && result != NativeMethods.RpcEChangedMode)
            {
                Marshal.ThrowExceptionForHR(result);
            }

            comInitialized = result >= 0;

            enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();

            HResult.ThrowIfFailed(
                enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Console, out device),
                "IMMDeviceEnumerator.GetDefaultAudioEndpoint");

            var audioClientId = WasapiGuids.IAudioClient;
            HResult.ThrowIfFailed(
                device.Activate(ref audioClientId, ClsCtx.All, IntPtr.Zero, out var audioClientObject),
                "IMMDevice.Activate");

            audioClient = (IAudioClient)audioClientObject;

            HResult.ThrowIfFailed(
                audioClient.GetMixFormat(out mixFormatPointer),
                "IAudioClient.GetMixFormat");

            var format = WaveFormatReader.Read(mixFormatPointer);

            HResult.ThrowIfFailed(
                audioClient.Initialize(
                    AudioClientShareMode.Shared,
                    AudioClientStreamFlags.Loopback,
                    0,
                    0,
                    mixFormatPointer,
                    IntPtr.Zero),
                "IAudioClient.Initialize");

            var captureClientId = WasapiGuids.IAudioCaptureClient;
            HResult.ThrowIfFailed(
                audioClient.GetService(ref captureClientId, out var captureClientPointer),
                "IAudioClient.GetService");

            try
            {
                captureClient = (IAudioCaptureClient)Marshal.GetObjectForIUnknown(captureClientPointer);
            }
            finally
            {
                Marshal.Release(captureClientPointer);
            }

            HResult.ThrowIfFailed(audioClient.Start(), "IAudioClient.Start");

            Format = format;
            ready.TrySetResult(format);

            CapturePackets(cancellation.Token, format, captureClient);

            audioClient.Stop();
        }
        catch (Exception exception)
        {
            if (!ready.TrySetException(exception) && !cancellation.IsCancellationRequested)
            {
                RaiseError(exception);
            }
        }
        finally
        {
            // If capture never started (for example it was cancelled first),
            // make sure StartAsync does not wait forever.
            ready.TrySetCanceled();

            ReleaseComObject(captureClient);
            ReleaseComObject(audioClient);
            ReleaseComObject(device);
            ReleaseComObject(enumerator);

            if (mixFormatPointer != IntPtr.Zero)
            {
                NativeMethods.CoTaskMemFree(mixFormatPointer);
            }

            if (comInitialized)
            {
                NativeMethods.CoUninitialize();
            }
        }
    }

    private void CapturePackets(
        CancellationToken cancellationToken,
        AudioFormat format,
        IAudioCaptureClient captureClient)
    {
        var byteBuffer = Array.Empty<byte>();
        var sampleBuffer = Array.Empty<float>();

        while (!cancellationToken.IsCancellationRequested)
        {
            HResult.ThrowIfFailed(
                captureClient.GetNextPacketSize(out var packetFrames),
                "IAudioCaptureClient.GetNextPacketSize");

            if (packetFrames == 0)
            {
                // WASAPI loopback has no event handle; poll for new packets but
                // wake immediately when capture is cancelled.
                cancellationToken.WaitHandle.WaitOne(5);
                continue;
            }

            while (packetFrames > 0 && !cancellationToken.IsCancellationRequested)
            {
                HResult.ThrowIfFailed(
                    captureClient.GetBuffer(
                        out var data,
                        out var frames,
                        out var flags,
                        out var devicePosition,
                        out _),
                    "IAudioCaptureClient.GetBuffer");

                try
                {
                    var sampleCount = checked((int)frames * format.Channels); // ubs:ignore[cs.pattern.int-cast] -- checked context guards the narrowing cast
                    var byteCount = checked((int)frames * format.BlockAlign); // ubs:ignore[cs.pattern.int-cast] -- checked context guards the narrowing cast
                    var isSilent = (flags & AudioClientBufferFlags.Silent) != 0;

                    if (sampleBuffer.Length < sampleCount)
                    {
                        sampleBuffer = new float[sampleCount];
                    }

                    var samples = sampleBuffer.AsSpan(0, sampleCount);

                    if (isSilent || data == IntPtr.Zero)
                    {
                        samples.Clear();
                    }
                    else
                    {
                        if (byteBuffer.Length < byteCount)
                        {
                            byteBuffer = new byte[byteCount];
                        }

                        Marshal.Copy(data, byteBuffer, 0, byteCount);
                        AudioSampleConverter.Convert(
                            byteBuffer.AsSpan(0, byteCount),
                            samples,
                            format);
                    }

                    RaiseData(new AudioDataEventArgs(
                        samples.ToArray(),
                        format,
                        isSilent,
                        devicePosition));
                }
                finally
                {
                    HResult.ThrowIfFailed(
                        captureClient.ReleaseBuffer(frames),
                        "IAudioCaptureClient.ReleaseBuffer");
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                HResult.ThrowIfFailed(
                    captureClient.GetNextPacketSize(out packetFrames),
                    "IAudioCaptureClient.GetNextPacketSize");
            }
        }
    }

    private void RaiseData(AudioDataEventArgs args)
    {
        var handler = DataAvailable;

        if (handler is null)
        {
            return;
        }

        try
        {
            handler(this, args);
        }
        catch (Exception exception)
        {
            RaiseError(exception);
        }
    }

    private void RaiseError(Exception exception)
    {
        var handler = Error;

        if (handler is null)
        {
            return;
        }

        try
        {
            handler(this, new AudioCaptureErrorEventArgs(exception));
        }
        catch
        {
            // A failing error handler must not take down the capture thread.
        }
    }

    private static void ReleaseComObject(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance))
        {
            Marshal.ReleaseComObject(instance);
        }
    }
}
