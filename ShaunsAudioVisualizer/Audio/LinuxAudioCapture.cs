namespace ShaunsAudioVisualizer.Audio;

/// <summary>
/// Placeholder audio capture source for non-Windows platforms.
/// </summary>
/// <remarks>
/// Real capture on Linux will use a PulseAudio/PipeWire monitor source.
/// This stub keeps the cross-platform build and console pipeline working
/// while the native backend is implemented (see issue #20).
/// </remarks>
public sealed class LinuxAudioCapture : IAudioCaptureSource
{
    public AudioFormat? Format => null;

#pragma warning disable CS0067
    public event EventHandler<AudioDataEventArgs>? DataAvailable;
    public event EventHandler<AudioCaptureErrorEventArgs>? Error;
#pragma warning restore CS0067

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException(
            "Linux audio capture is not implemented yet. See issue #20.");

    public Task StopAsync() => Task.CompletedTask;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}