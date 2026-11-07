using ShaunsAudioVisualizer.Audio.Analysis;

namespace ShaunsAudioVisualizer.Visualization.Effects;

/// <summary>
/// Immutable snapshot of the analyzed audio for a single visualization frame.
/// </summary>
public sealed class VisualizationFrame
{
    public VisualizationFrame(
        float[] leftBands,
        float[] rightBands,
        AudioLevelSnapshot levels,
        ReadOnlyMemory<float> samples,
        int sampleRate,
        TimeSpan elapsed,
        bool beat)
    {
        LeftBands = leftBands;
        RightBands = rightBands;
        Levels = levels;
        Samples = samples;
        SampleRate = sampleRate;
        Elapsed = elapsed;
        Beat = beat;
    }

    public float[] LeftBands { get; }

    public float[] RightBands { get; }

    public AudioLevelSnapshot Levels { get; }

    public ReadOnlyMemory<float> Samples { get; }

    public int SampleRate { get; }

    public TimeSpan Elapsed { get; }

    public bool Beat { get; }
}