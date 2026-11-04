namespace AudioVisualizer.Audio;

/// <summary>
/// Describes the PCM sample format delivered by an audio capture source.
/// </summary>
/// <param name="SampleRate">Samples per second, per channel (for example 48000).</param>
/// <param name="Channels">Number of interleaved channels (for example 2 for stereo).</param>
/// <param name="BitsPerSample">Bits used by one sample on one channel.</param>
/// <param name="Encoding">How the samples are encoded.</param>
public sealed record AudioFormat(
    int SampleRate,
    int Channels,
    int BitsPerSample,
    SampleEncoding Encoding)
{
    /// <summary>Bytes occupied by one sample on one channel.</summary>
    public int BytesPerSample => BitsPerSample / 8;

    /// <summary>Bytes occupied by one frame (one sample for every channel).</summary>
    public int BlockAlign => Channels * BytesPerSample;

    public override string ToString() =>
        $"{SampleRate} Hz, {Channels} ch, {BitsPerSample}-bit {Encoding}";
}

/// <summary>Encoding of individual PCM samples.</summary>
public enum SampleEncoding
{
    /// <summary>Signed integer PCM.</summary>
    Pcm,

    /// <summary>32-bit IEEE floating point PCM in the range [-1, 1].</summary>
    IeeeFloat,
}
