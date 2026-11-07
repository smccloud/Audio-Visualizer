using Compat.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class VUMeterEffect : IVisualizationEffect
{
    public string Name => "Stereo VU meters";

    private float _leftLevel;
    private float _rightLevel;
    private float _leftPeak;
    private float _rightPeak;
    private int _leftPeakHold;
    private int _rightPeakHold;

    private Image? _background;

    public void Reset()
    {
        _leftLevel = 0;
        _rightLevel = 0;
        _leftPeak = 0;
        _rightPeak = 0;
        _leftPeakHold = 0;
        _rightPeakHold = 0;
    }

    public void Update(VisualizationFrame frame)
    {
        var levels = frame.Levels;
        var targetLeft = levels.ChannelRms.Length > 0 ? levels.ChannelRms[0] : levels.Rms;
        var targetRight = levels.ChannelRms.Length > 1 ? levels.ChannelRms[1] : targetLeft;

        // Fast attack, slow decay (analogue ballistics).
        _leftLevel = targetLeft > _leftLevel
            ? targetLeft
            : _leftLevel * 0.92f;
        _rightLevel = targetRight > _rightLevel
            ? targetRight
            : _rightLevel * 0.92f;

        // Peak hold.
        if (targetLeft > _leftPeak)
        {
            _leftPeak = targetLeft;
            _leftPeakHold = 30;
        }
        else if (_leftPeakHold > 0)
        {
            _leftPeakHold--;
        }
        else
        {
            _leftPeak *= 0.95f;
        }

        if (targetRight > _rightPeak)
        {
            _rightPeak = targetRight;
            _rightPeakHold = 30;
        }
        else if (_rightPeakHold > 0)
        {
            _rightPeakHold--;
        }
        else
        {
            _rightPeak *= 0.95f;
        }
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        EnsureBackground();

        var halfWidth = bounds.Width / 2 - 20;
        var availableHeight = bounds.Height - 40;
        var meterSize = Math.Max(40, Math.Min(halfWidth, availableHeight));

        var leftRect = new Rectangle(
            10 + (halfWidth - meterSize) / 2,
            (bounds.Height - meterSize) / 2,
            meterSize,
            meterSize);
        var rightRect = new Rectangle(
            bounds.Width / 2 + 10 + (halfWidth - meterSize) / 2,
            (bounds.Height - meterSize) / 2,
            meterSize,
            meterSize);

        if (_background is not null)
        {
            graphics.DrawImage(_background, leftRect);
            graphics.DrawImage(_background, rightRect);
        }

        DrawNeedle(graphics, leftRect, _leftLevel, _leftPeak, "L");
        DrawNeedle(graphics, rightRect, _rightLevel, _rightPeak, "R");
    }

    private void EnsureBackground()
    {
        if (_background is not null)
        {
            return;
        }

        try
        {
            var stream = typeof(VUMeterEffect).Assembly.GetManifestResourceStream("ShaunsAudioVisualizer.vumeter.png");
            if (stream is not null)
            {
                _background = Image.FromStream(stream);
            }
        }
        catch
        {
            _background = null;
        }
    }

    private static void DrawNeedle(Graphics graphics, Rectangle rect, float level, float peak, string label)
    {
        var cx = rect.Left + rect.Width * 0.5f;
        var cy = rect.Top + rect.Height * (470f / 550f);
        var radius = rect.Width * (220f / 550f);

        // Needle
        var angleDeg = 200f + level * 140f;
        var angle = (float)(angleDeg * Math.PI / 180.0);
        var nx = cx + (float)Math.Cos(angle) * radius * 1.05f;
        var ny = cy + (float)Math.Sin(angle) * radius * 1.05f;
        using (var needlePen = new Pen(Color.White, 2f))
        {
            graphics.DrawLine(needlePen, cx, cy, nx, ny);
        }

        // Peak hold marker
        if (peak > 0)
        {
            var peakAngle = (float)((200f + peak * 140f) * Math.PI / 180.0);
            var px = cx + (float)Math.Cos(peakAngle) * radius * 1.08f;
            var py = cy + (float)Math.Sin(peakAngle) * radius * 1.08f;
            using var peakBrush = new SolidBrush(Color.Red);
            graphics.FillEllipse(peakBrush, px - 3, py - 3, 6, 6);
        }

        // Label
        using var font = new Font("Consolas", 12f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        graphics.DrawString(label, font, brush, cx - 6, rect.Bottom + 5);
    }
}