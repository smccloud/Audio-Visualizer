using System.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class MilkdropStyleEffect : IVisualizationEffect
{
    public string Name => "Milkdrop-style";

    private float[] _bands = Array.Empty<float>();
    private float _rotation;
    private float _zoom = 1f;

    public void Reset()
    {
        _bands = Array.Empty<float>();
        _rotation = 0;
        _zoom = 1f;
    }

    public void Update(VisualizationFrame frame)
    {
        var left = frame.LeftBands;
        var right = frame.RightBands;
        var count = Math.Max(left.Length, right.Length);
        if (_bands.Length != count)
        {
            _bands = new float[count];
        }

        for (var i = 0; i < count; i++)
        {
            var l = i < left.Length ? left[i] : 0f;
            var r = i < right.Length ? right[i] : 0f;
            _bands[i] = (l + r) / 2f;
        }

        _rotation += 0.01f + frame.Levels.Rms * 0.05f;
        _zoom = 1f + frame.Levels.Rms * 0.3f;
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_bands.Length == 0)
        {
            return;
        }

        var cx = bounds.Width / 2f;
        var cy = bounds.Height / 2f;
        var maxR = Math.Min(bounds.Width, bounds.Height) / 2f - 10;

        // Rotating radial spectrum
        var count = _bands.Length;
        for (var i = 0; i < count; i++)
        {
            var level = _bands[i];
            var angle = (float)(i * 2 * Math.PI / count + _rotation);
            var inner = maxR * 0.2f;
            var outer = inner + level * maxR * 0.8f * _zoom;

            var x1 = cx + (float)Math.Cos(angle) * inner;
            var y1 = cy + (float)Math.Sin(angle) * inner;
            var x2 = cx + (float)Math.Cos(angle) * outer;
            var y2 = cy + (float)Math.Sin(angle) * outer;

            var hue = (i * 360f / count + _rotation * 50) % 360f;
            using var pen = new Pen(ColorFromHsv(hue, 1f, 1f), 2f);
            graphics.DrawLine(pen, x1, y1, x2, y2);
        }
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