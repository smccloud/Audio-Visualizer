using System.Drawing;
using System.Drawing.Text;

namespace AudioVisualizer.Visualization.Effects;

public sealed class ConsoleStyleBarsEffect : IVisualizationEffect
{
    public string Name => "Console-style bars";

    private float[] _leftDisplayed = Array.Empty<float>();
    private float[] _rightDisplayed = Array.Empty<float>();
    private VisualizationFrame? _lastFrame;

    public void Reset()
    {
        _leftDisplayed = Array.Empty<float>();
        _rightDisplayed = Array.Empty<float>();
    }

    public void Update(VisualizationFrame frame)
    {
        _lastFrame = frame;

        if (_leftDisplayed.Length != frame.LeftBands.Length)
        {
            _leftDisplayed = new float[frame.LeftBands.Length];
        }

        if (_rightDisplayed.Length != frame.RightBands.Length)
        {
            _rightDisplayed = new float[frame.RightBands.Length];
        }

        for (var i = 0; i < frame.LeftBands.Length; i++)
        {
            _leftDisplayed[i] = MathF.Max(frame.LeftBands[i], _leftDisplayed[i] * 0.82f);
        }

        for (var i = 0; i < frame.RightBands.Length; i++)
        {
            _rightDisplayed[i] = MathF.Max(frame.RightBands[i], _rightDisplayed[i] * 0.82f);
        }
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var font = new Font("Consolas", 11f);
        using var headerBrush = new SolidBrush(Color.White);
        using var greenBrush = new SolidBrush(Color.LimeGreen);
        using var yellowBrush = new SolidBrush(Color.Yellow);
        using var redBrush = new SolidBrush(Color.Red);

        var sampleRateText = _lastFrame is null ? "0kHz" : $"{_lastFrame.SampleRate / 1000}kHz";
        var channelCount = _lastFrame?.Levels.ChannelPeaks.Length ?? 0;

        graphics.DrawString("AUDIO VISUALIZER", font, headerBrush, 10, 10);
        graphics.DrawString($"{sampleRateText}  {channelCount} ch  {_leftDisplayed.Length} bands", font, headerBrush, 10, 30);

        if (_lastFrame is not null)
        {
            graphics.DrawString(
                $"RMS {_lastFrame.Levels.RmsDb,6:0.0} dBFS   PEAK {_lastFrame.Levels.PeakDb,6:0.0} dBFS",
                font,
                headerBrush,
                10,
                48);
        }

        var originX = 10;
        var originY = 60;
        var maxBarHeight = Math.Max(40, bounds.Height - originY - 10);

        var channels = _rightDisplayed.Length > 0 ? 2 : 1;
        var interChannelGap = 20;
        var totalInterChannelGap = channels > 1 ? interChannelGap : 0;
        var usableWidth = Math.Max(40, bounds.Width - (originX * 2) - totalInterChannelGap);
        var perChannelWidth = usableWidth / channels;
        var bandCount = Math.Max(1, _leftDisplayed.Length);
        var slotWidth = Math.Max(4, perChannelWidth / bandCount);
        var barWidth = Math.Max(2, (int)(slotWidth * 0.7));
        var gap = Math.Max(1, slotWidth - barWidth);

        DrawBars(graphics, _leftDisplayed, originX, originY, barWidth, gap, maxBarHeight, greenBrush, yellowBrush, redBrush);

        if (_rightDisplayed.Length > 0)
        {
            var rightOrigin = originX + (_leftDisplayed.Length * (barWidth + gap)) + interChannelGap;
            DrawBars(graphics, _rightDisplayed, rightOrigin, originY, barWidth, gap, maxBarHeight, greenBrush, yellowBrush, redBrush);
        }


    }

    private static void DrawBars(
        Graphics graphics,
        float[] displayed,
        int originX,
        int originY,
        int barWidth,
        int gap,
        int maxBarHeight,
        SolidBrush greenBrush,
        SolidBrush yellowBrush,
        SolidBrush redBrush)
    {
        var greenThreshold = (int)(0.5f * maxBarHeight);
        var yellowThreshold = (int)(0.8f * maxBarHeight);

        for (var band = 0; band < displayed.Length; band++)
        {
            var level = displayed[band];
            var height = (int)(level * maxBarHeight);
            if (height <= 0)
            {
                continue;
            }

            var x = originX + band * (barWidth + gap);
            var baseY = originY + maxBarHeight;

            // Color is determined by vertical position (like the console
            // renderer), not by the overall level: only the portion of the bar
            // that rises above each ANSI color threshold takes the new color.
            var greenHeight = Math.Min(height, greenThreshold);
            var yellowHeight = Math.Min(Math.Max(height - greenThreshold, 0), yellowThreshold - greenThreshold);
            var redHeight = Math.Max(height - yellowThreshold, 0);

            if (greenHeight > 0)
            {
                graphics.FillRectangle(greenBrush, x, baseY - greenHeight, barWidth, greenHeight);
            }

            if (yellowHeight > 0)
            {
                graphics.FillRectangle(yellowBrush, x, baseY - greenHeight - yellowHeight, barWidth, yellowHeight);
            }

            if (redHeight > 0)
            {
                graphics.FillRectangle(redBrush, x, baseY - greenHeight - yellowHeight - redHeight, barWidth, redHeight);
            }
        }
    }
}