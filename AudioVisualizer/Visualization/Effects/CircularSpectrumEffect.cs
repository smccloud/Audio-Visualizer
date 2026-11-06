using System.Drawing;
using System.Drawing.Drawing2D;

namespace AudioVisualizer.Visualization.Effects;

public sealed class CircularSpectrumEffect : IVisualizationEffect
{
    public string Name => "Circular spectrum";

    private float[] _left = Array.Empty<float>();
    private float[] _right = Array.Empty<float>();

    public void Reset()
    {
        _left = Array.Empty<float>();
        _right = Array.Empty<float>();
    }

    public void Update(VisualizationFrame frame)
    {
        _left = frame.LeftBands.ToArray();
        _right = frame.RightBands.ToArray();
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_left.Length == 0)
        {
            return;
        }

        var count = _left.Length;
        var cx = bounds.Width / 2f;
        var cy = bounds.Height / 2f;
        var maxRadius = Math.Min(bounds.Width, bounds.Height) / 2f - 10;
        var innerRadius = maxRadius * 0.25f;
        var barWidth = Math.Max(2f, (float)(2 * Math.PI * innerRadius / count * 0.6));

        using var pen = new Pen(Color.White, barWidth) { StartCap = LineCap.Round, EndCap = LineCap.Round };

        for (var i = 0; i < count; i++)
        {
            var level = _left[i];
            if (_right.Length > i)
            {
                level = (level + _right[i]) / 2f;
            }

            var angle = (float)(i * 2 * Math.PI / count - Math.PI / 2);
            var outer = innerRadius + level * (maxRadius - innerRadius);
            var x1 = cx + (float)Math.Cos(angle) * innerRadius;
            var y1 = cy + (float)Math.Sin(angle) * innerRadius;
            var x2 = cx + (float)Math.Cos(angle) * outer;
            var y2 = cy + (float)Math.Sin(angle) * outer;

            var hue = (float)i / count;
            pen.Color = ColorFromHsv(hue * 360f, 1f, 1f);
            graphics.DrawLine(pen, x1, y1, x2, y2);
        }

        // Center circle
        using var centerBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
        var r = innerRadius * 0.9f;
        graphics.FillEllipse(centerBrush, cx - r, cy - r, r * 2, r * 2);
    }

    private static Color ColorFromHsv(float h, float s, float v)
    {
        var hi = (int)(h / 60f) % 6;
        var f = h / 60f - (int)(h / 60f);
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f) * s);

        float r, g, b;
        switch (hi)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }

        return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }
}