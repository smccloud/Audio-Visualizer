using System.Text;

using ShaunsAudioVisualizer.Audio;
using ShaunsAudioVisualizer.Audio.Analysis;

namespace ShaunsAudioVisualizer.Visualization;

/// <summary>
/// A deliberately simple console visualization used to verify that captured
/// audio reaches the visualization layer. Bars react to a log-spaced spectrum;
/// a header shows overall level. The real renderer will replace this later.
/// </summary>
public sealed class ConsoleVisualizer
{
    private const char FullBlock = '\u2588';
    private const char HalfBlock = '\u2584';
    private const int ChannelGap = 4;
    private const string ColorReset = "\x1b[0m";
    private const string ColorLow = "\x1b[32m";
    private const string ColorMid = "\x1b[33m";
    private const string ColorHigh = "\x1b[31m";

    private readonly int _height;
    private readonly int _bandWidth;
    private readonly double _minFrequency;
    private readonly double _maxFrequency;
    private float[] _leftDisplayed = Array.Empty<float>();
    private float[] _rightDisplayed = Array.Empty<float>();
    private bool _started;

    public ConsoleVisualizer(
        int height = 16,
        int bandWidth = 2,
        double minFrequency = 30,
        double maxFrequency = 18000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(bandWidth, 1);

        if (minFrequency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minFrequency));
        }

        if (maxFrequency <= minFrequency)
        {
            throw new ArgumentException(
                "Maximum frequency must exceed the minimum.",
                nameof(maxFrequency));
        }

        _height = height;
        _bandWidth = bandWidth;
        _minFrequency = minFrequency;
        _maxFrequency = maxFrequency;
    }

    public void Render(
        ReadOnlySpan<float> leftBands,
        ReadOnlySpan<float> rightBands,
        AudioLevelSnapshot levels,
        AudioFormat format)
    {
        if (!_started)
        {
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }

            _started = true;
        }

        if (_leftDisplayed.Length != leftBands.Length || _rightDisplayed.Length != rightBands.Length)
        {
            _leftDisplayed = new float[leftBands.Length];
            _rightDisplayed = new float[rightBands.Length];
        }

        // Ease the bars downward so they do not flicker between packets.
        for (var band = 0; band < leftBands.Length; band++)
        {
            _leftDisplayed[band] = MathF.Max(leftBands[band], _leftDisplayed[band] * 0.82f);
        }

        for (var band = 0; band < rightBands.Length; band++)
        {
            _rightDisplayed[band] = MathF.Max(rightBands[band], _rightDisplayed[band] * 0.82f);
        }

        var output = new StringBuilder();
        output.Append("\x1b[H");
        output.AppendLine();
        output.Append("  AUDIO VISUALIZER");
        output.Append($"   {FormatFrequency(format.SampleRate)}  {format.Channels} ch  {format.BitsPerSample}-bit");
        output.AppendLine();
        output.Append($"  RMS {levels.RmsDb,6:0.0} dBFS   PEAK {levels.PeakDb,6:0.0} dBFS");
        output.AppendLine();
        output.AppendLine();

        var showRight = format.Channels > 1;

        // Render both channels side by side so the full stereo image fits on screen.
        for (var row = _height; row >= 1; row--)
        {
            output.Append("  ");
            AppendBars(output, _leftDisplayed, row);

            if (showRight)
            {
                output.Append(' ', ChannelGap);
                AppendBars(output, _rightDisplayed, row);
            }

            output.AppendLine();
        }

        output.Append("  ");
        output.Append('─', _leftDisplayed.Length * _bandWidth);

        if (showRight)
        {
            output.Append(' ', ChannelGap);
            output.Append('─', _rightDisplayed.Length * _bandWidth);
        }

        output.AppendLine();

        var maxFrequency = Math.Min(_maxFrequency, format.SampleRate / 2.0);
        var minFrequency = Math.Min(_minFrequency, maxFrequency);

        AppendFrequencyLabels(
            output,
            _leftDisplayed.Length,
            _rightDisplayed.Length,
            showRight,
            minFrequency,
            maxFrequency);

        output.AppendLine();
        output.AppendLine("  Press Ctrl+C to stop.");

        Console.Write(output.ToString()); // ubs:ignore[cs.pattern.console-write] -- intended console UI output
    }

    private void AppendFrequencyLabels(
        StringBuilder output,
        int leftBandCount,
        int rightBandCount,
        bool showRight,
        double minFrequency,
        double maxFrequency)
    {
        var leftLabels = GetBandFrequencies(leftBandCount, minFrequency, maxFrequency);
        var rightLabels = showRight
            ? GetBandFrequencies(rightBandCount, minFrequency, maxFrequency)
            : Array.Empty<string>();

        var rowCount = 0;

        foreach (var label in leftLabels)
        {
            rowCount = Math.Max(rowCount, label.Length);
        }

        foreach (var label in rightLabels)
        {
            rowCount = Math.Max(rowCount, label.Length);
        }

        const int leftPanelStart = 2;
        var leftPanelWidth = Math.Max(1, leftBandCount * _bandWidth);
        var rightPanelStart = leftPanelStart + leftPanelWidth + ChannelGap;
        var totalWidth = showRight
            ? rightPanelStart + Math.Max(1, rightBandCount * _bandWidth)
            : leftPanelStart + leftPanelWidth;

        for (var row = 0; row < rowCount; row++)
        {
            var line = new char[totalWidth];
            Array.Fill(line, ' ');

            for (var band = 0; band < leftLabels.Length; band++)
            {
                PlaceVertical(line, leftPanelStart + (band * _bandWidth), leftLabels[band], row);
            }

            if (showRight)
            {
                for (var band = 0; band < rightLabels.Length; band++)
                {
                    PlaceVertical(line, rightPanelStart + (band * _bandWidth), rightLabels[band], row);
                }
            }

            output.AppendLine(new string(line).TrimEnd());
        }
    }

    private static string[] GetBandFrequencies(
        int bandCount,
        double minFrequency,
        double maxFrequency)
    {
        var labels = new string[bandCount];

        if (bandCount == 0)
        {
            return labels;
        }

        var logMin = Math.Log(minFrequency);
        var logMax = Math.Log(maxFrequency);

        for (var band = 0; band < bandCount; band++)
        {
            var fraction = (band + 0.5) / bandCount;
            var frequency = Math.Exp(logMin + ((logMax - logMin) * fraction));
            labels[band] = FormatFrequency(frequency);
        }

        return labels;
    }

    private static void PlaceVertical(char[] line, int column, string text, int row)
    {
        if (row < text.Length && column >= 0 && column < line.Length)
        {
            line[column] = text[row];
        }
    }

    private static string FormatFrequency(double frequency)
    {
        var rounded = Math.Round(frequency, MidpointRounding.AwayFromZero);

        return rounded < 10000
            ? $"{rounded:0}Hz"
            : $"{rounded / 1000:0}kHz";
    }

    private void AppendBars(StringBuilder output, float[] displayed, int row)
    {
        var color = ColorForRow(row);

        for (var band = 0; band < displayed.Length; band++)
        {
            var level = displayed[band] * _height;
            var glyph = level >= row
                ? FullBlock
                : level > row - 1
                    ? HalfBlock
                    : ' ';

            if (glyph == ' ')
            {
                output.Append(' ');
            }
            else
            {
                output.Append(color);
                output.Append(glyph);
                output.Append(ColorReset);
            }

            if (_bandWidth > 1)
            {
                output.Append(' ', _bandWidth - 1);
            }
        }
    }

    private string ColorForRow(int row)
    {
        var fraction = _height == 1 ? 1f : (float)(row - 1) / (_height - 1);

        return fraction >= 0.8f
            ? ColorHigh
            : fraction >= 0.5f
                ? ColorMid
                : ColorLow;
    }
}