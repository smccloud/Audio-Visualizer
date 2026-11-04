using AudioVisualizer.Audio;
using AudioVisualizer.Audio.Analysis;
using AudioVisualizer.Visualization;

namespace AudioVisualizer.Tests.Visualization;

public sealed class ConsoleVisualizerTests
{
    [Fact]
    public void Constructor_RejectsInvalidDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleVisualizer(height: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleVisualizer(bandWidth: 0));
    }

    [Fact]
    public void Render_WritesHeaderLevelsAndBandLabels()
    {
        var format = new AudioFormat(48000, 2, 16, SampleEncoding.Pcm);
        var levels = new AudioLevelSnapshot(
            new[] { 0.5f, 0.25f },
            new[] { 0.25f, 0.125f },
            peak: 0.5f,
            rms: 0.25f,
            frameCount: 480);
        var visualizer = new ConsoleVisualizer(height: 4, bandWidth: 2);

        var output = CaptureRender(
            visualizer,
            new[] { 0.2f, 0.5f, 0.8f },
            levels,
            format);

        Assert.Contains("AUDIO VISUALIZER", output);
        Assert.Contains("48000 Hz", output);
        Assert.Contains("2 ch", output);
        Assert.Contains("16-bit", output);
        Assert.Contains("RMS", output);
        Assert.Contains("PEAK", output);
        Assert.Contains("LOW", output);
        Assert.Contains("HIGH", output);
        Assert.Contains("Press Ctrl+C to stop.", output);
    }

    [Fact]
    public void Render_DrawsOneColumnPerBandAndPixel()
    {
        const int bandCount = 5;
        const int bandWidth = 3;
        var format = new AudioFormat(48000, 1, 16, SampleEncoding.Pcm);
        var levels = new AudioLevelSnapshot(
            new[] { 0.5f },
            new[] { 0.25f },
            peak: 0.5f,
            rms: 0.25f,
            frameCount: 480);
        var visualizer = new ConsoleVisualizer(height: 2, bandWidth: bandWidth);

        var output = CaptureRender(
            visualizer,
            Enumerable.Repeat(0.5f, bandCount).ToArray(),
            levels,
            format);

        var separator = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.Contains('─', StringComparison.Ordinal));

        Assert.Equal(2 + (bandCount * bandWidth), separator.Length);
    }

    private static string CaptureRender(
        ConsoleVisualizer visualizer,
        float[] bands,
        AudioLevelSnapshot levels,
        AudioFormat format)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            visualizer.Render(bands, levels, format);
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}
