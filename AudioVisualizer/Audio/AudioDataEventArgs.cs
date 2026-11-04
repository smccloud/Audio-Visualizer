namespace AudioVisualizer.Audio;

/// <summary>
/// Carries one block of interleaved floating point samples from a capture source.
/// </summary>
public sealed class AudioDataEventArgs : EventArgs
{
    public AudioDataEventArgs(
        ReadOnlyMemory<float> samples,
        AudioFormat format,
        bool isSilent,
        long devicePositionFrames)
    {
        Samples = samples;
        Format = format;
        IsSilent = isSilent;
        DevicePositionFrames = devicePositionFrames;
    }

    /// <summary>
    /// Interleaved samples in the range [-1, 1]. The length is
    /// <see cref="AudioFormat.Channels"/> times the number of frames.
    /// </summary>
    public ReadOnlyMemory<float> Samples { get; }

    /// <summary>Format of <see cref="Samples"/>.</summary>
    public AudioFormat Format { get; }

    /// <summary>True when the audio engine reported a silent packet.</summary>
    public bool IsSilent { get; }

    /// <summary>Position of the first frame in the device's stream.</summary>
    public long DevicePositionFrames { get; }
}
