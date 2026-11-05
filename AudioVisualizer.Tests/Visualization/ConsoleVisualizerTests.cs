using AudioVisualizer.Audio;
using AudioVisualizer.Audio.Analysis;
using AudioVisualizer.Visualization;

using System.Linq;

namespace AudioVisualizer.Tests.Visualization;

public sealed class ConsoleVisualizerTests
{
    [Fact]
    public void Constructor_RejectsInvalidDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleVisualizer(height: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleVisualizer(bandWidth: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ConsoleVisualizer(minFrequency: 0));
        Assert.Throws<ArgumentException>(() =>
            new ConsoleVisualizer(minFrequency: 100, maxFrequency: 100));
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
            new[] { 0.1f, 0.4f, 0.7f },
            levels,
            format);

        Assert.Contains("AUDIO VISUALIZER", output);
        Assert.Contains("48kHz", output);
        Assert.Contains("2 ch", output);
        Assert.Contains("16-bit", output);
        Assert.Contains("RMS", output);
        Assert.Contains("PEAK", output);
        Assert.Contains("Press Ctrl+C to stop.", output);

        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var separatorIndex = Array.FindIndex(
            lines,
            line => line.Contains('─', StringComparison.Ordinal));
        var labelLines = lines
            .Skip(separatorIndex + 1)
            .TakeWhile(line => !line.Contains("Press", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            "87Hz",
            string.Concat(labelLines.Take(4).Select(line => line[2])));
        Assert.Equal(
            "735Hz",
            string.Concat(labelLines.Take(5).Select(line => line[4])));
        Assert.Equal(
            "6198Hz",
            string.Concat(labelLines.Take(6).Select(line => line[6])));
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
            Enumerable.Repeat(0.5f, bandCount).ToArray(),
            levels,
            format);

        var separator = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.Contains('─', StringComparison.Ordinal));

        Assert.Equal(2 + (bandCount * bandWidth), separator.Length);
    }

    [Fact]
    public void Render_StereoVisualization_DrawsChannelsSideBySide()
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
            new[] { 0.1f, 0.4f, 0.7f },
            levels,
            format);

        Assert.DoesNotContain("LEFT", output);
        Assert.DoesNotContain("RIGHT", output);

        var separator = output
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.Contains('─', StringComparison.Ordinal));
        var panels = separator.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, panels.Length);
        Assert.All(panels, panel => Assert.Equal(3 * 2, panel.Length));
    }

    [Fact]
    public void Render_ColorizesBarsByHeight()
    {
        var format = new AudioFormat(48000, 2, 16, SampleEncoding.Pcm);
        var levels = new AudioLevelSnapshot(
            new[] { 0.5f, 0.25f },
            new[] { 0.25f, 0.125f },
            peak: 0.5f,
            rms: 0.25f,
            frameCount: 480);
        var visualizer = new ConsoleVisualizer(height: 6, bandWidth: 2);

        var output = CaptureRender(
            visualizer,
            Enumerable.Repeat(1f, 4).ToArray(),
            Enumerable.Repeat(1f, 4).ToArray(),
            levels,
            format);

        Assert.Contains("\x1b[32m", output);
        Assert.Contains("\x1b[33m", output);
        Assert.Contains("\x1b[31m", output);
        Assert.Contains("\x1b[0m", output);
    }

    private static string CaptureRender(
        ConsoleVisualizer visualizer,
        float[] leftBands,
        float[] rightBands,
        AudioLevelSnapshot levels,
        AudioFormat format)
    {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            visualizer.Render(leftBands, rightBands, levels, format);
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(original);
        }
    }
}