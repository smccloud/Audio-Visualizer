using System.Text;

using AudioVisualizer.Audio;
using AudioVisualizer.Audio.Analysis;

namespace AudioVisualizer.Visualization;

/// <summary>
/// A deliberately simple console visualization used to verify that captured
/// audio reaches the visualization layer. Bars react to a log-spaced spectrum;
/// a header shows overall level. The real renderer will replace this later.
/// </summary>
public sealed class ConsoleVisualizer
{
    private const char FullBlock = '\u2588';
    private const char HalfBlock = '\u2584';

    private readonly int _height;
    private readonly int _bandWidth;
    private float[] _leftDisplayed = Array.Empty<float>();
    private float[] _rightDisplayed = Array.Empty<float>();
    private bool _started;

    public ConsoleVisualizer(int height = 16, int bandWidth = 2)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentOutOfRangeException.ThrowIfLessThan(bandWidth, 1);

        _height = height;
        _bandWidth = bandWidth;
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
            _rightDisplayed[band] = MathF.Max(rightBands[band], _rightDisplayed[band] * 0.82f);
        }

        var output = new StringBuilder();
        output.Append("\x1b[H");
        output.AppendLine();
        output.Append("  AUDIO VISUALIZER");
        output.Append($"   {format.SampleRate} Hz  {format.Channels} ch  {format.BitsPerSample}-bit");
        output.AppendLine();
        output.Append($"  RMS {levels.RmsDb,6:0.0} dBFS   PEAK {levels.PeakDb,6:0.0} dBFS");
        output.AppendLine();
        output.AppendLine();

        // Render left channel
        for (var row = _height; row >= 1; row--)
        {
            output.Append("  LEFT ");

            for (var band = 0; band < _leftDisplayed.Length; band++)
            {
                var level = _leftDisplayed[band] * _height;
                var glyph = level >= row
                    ? FullBlock
                    : level > row - 1
                        ? HalfBlock
                        : ' ';

                output.Append(glyph);

                if (_bandWidth > 1)
                {
                    output.Append(' ', _bandWidth - 1);
                }
            }

            output.AppendLine();
        }

        output.Append("  ");
        output.Append('─', _leftDisplayed.Length * _bandWidth);
        output.AppendLine();
        output.Append("  LOW");
        output.Append(' ', Math.Max(1, _leftDisplayed.Length * _bandWidth - 7));
        output.AppendLine("HIGH");
        output.AppendLine();

        // Render right channel
        for (var row = _height; row >= 1; row--)
        {
            output.Append("  RIGHT ");

            for (var band = 0; band < _rightDisplayed.Length; band++)
            {
                var level = _rightDisplayed[band] * _height;
                var glyph = level >= row
                    ? FullBlock
                    : level > row - 1
                        ? HalfBlock
                        : ' ';

                output.Append(glyph);

                if (_bandWidth > 1)
                {
                    output.Append(' ', _bandWidth - 1);
                }
            }

            output.AppendLine();
        }

        output.Append("  ");
        output.Append('─', _rightDisplayed.Length * _bandWidth);
        output.AppendLine();
        output.Append("  LOW");
        output.Append(' ', Math.Max(1, _rightDisplayed.Length * _bandWidth - 7));
        output.AppendLine("HIGH");
        output.AppendLine();
        output.AppendLine("  Press Ctrl+C to stop.");

        Console.Write(output.ToString());
    }
}