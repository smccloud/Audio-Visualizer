namespace AudioVisualizer.Audio.Analysis;

/// <summary>Level measurements for one block of audio.</summary>
public sealed class AudioLevelSnapshot
{
    public AudioLevelSnapshot(
        float[] channelPeaks,
        float[] channelRms,
        float peak,
        float rms,
        int frameCount)
    {
        ChannelPeaks = channelPeaks;
        ChannelRms = channelRms;
        Peak = peak;
        Rms = rms;
        FrameCount = frameCount;
    }

    /// <summary>Peak absolute sample per channel.</summary>
    public float[] ChannelPeaks { get; }

    /// <summary>Root-mean-square level per channel.</summary>
    public float[] ChannelRms { get; }

    /// <summary>Peak absolute sample across all channels.</summary>
    public float Peak { get; }

    /// <summary>Root-mean-square level across all channels.</summary>
    public float Rms { get; }

    public int FrameCount { get; }

    /// <summary>Peak level in dBFS, clamped to a sensible floor.</summary>
    public double PeakDb => ToDecibels(Peak);

    /// <summary>RMS level in dBFS, clamped to a sensible floor.</summary>
    public double RmsDb => ToDecibels(Rms);

    private static double ToDecibels(float linear) =>
        20 * Math.Log10(Math.Max(linear, 1e-7f));
}

/// <summary>
/// Computes simple time-domain level information from interleaved samples.
/// This is intentionally lightweight and allocation-friendly for use on the
/// capture thread; spectral analysis can be layered on top later.
/// </summary>
public sealed class AudioLevelAnalyzer
{
    public AudioLevelSnapshot Analyze(ReadOnlySpan<float> interleavedSamples, int channels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);

        var frameCount = interleavedSamples.Length / channels;
        var peaks = new float[channels];
        var sums = new double[channels];
        var peak = 0f;
        var totalSum = 0.0;

        for (var frame = 0; frame < frameCount; frame++)
        {
            var offset = frame * channels;

            for (var channel = 0; channel < channels; channel++)
            {
                var sample = interleavedSamples[offset + channel];
                var absolute = MathF.Abs(sample);

                if (absolute > peaks[channel])
                {
                    peaks[channel] = absolute;
                }

                if (absolute > peak)
                {
                    peak = absolute;
                }

                var square = (double)sample * sample;
                sums[channel] += square;
                totalSum += square;
            }
        }

        var rms = new float[channels];

        for (var channel = 0; channel < channels; channel++)
        {
            rms[channel] = frameCount > 0
                ? (float)Math.Sqrt(sums[channel] / frameCount)
                : 0f;
        }

        var totalSamples = frameCount * channels;
        var overallRms = totalSamples > 0
            ? (float)Math.Sqrt(totalSum / totalSamples)
            : 0f;

        return new AudioLevelSnapshot(peaks, rms, peak, overallRms, frameCount);
    }
}
