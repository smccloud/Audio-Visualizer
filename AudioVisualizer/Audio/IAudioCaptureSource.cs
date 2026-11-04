namespace AudioVisualizer.Audio;

/// <summary>
/// A source of audio samples for the visualizer.
/// </summary>
public interface IAudioCaptureSource : IDisposable
{
    /// <summary>
    /// Format of the data produced by the source, or <c>null</c> before it starts.
    /// </summary>
    AudioFormat? Format { get; }

    /// <summary>
    /// Raised on the capture thread for every packet of audio received.
    /// Handlers should copy or analyze the data quickly; it is reused.
    /// </summary>
    event EventHandler<AudioDataEventArgs>? DataAvailable;

    /// <summary>
    /// Raised on the capture thread when capture fails after it has started.
    /// </summary>
    event EventHandler<AudioCaptureErrorEventArgs>? Error;

    /// <summary>Starts capturing audio.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops capturing audio and waits for the capture thread to exit.</summary>
    Task StopAsync();
}

/// <summary>Wraps an error raised by the capture thread.</summary>
public sealed class AudioCaptureErrorEventArgs : EventArgs
{
    public AudioCaptureErrorEventArgs(Exception exception) => Exception = exception;

    public Exception Exception { get; }
}
