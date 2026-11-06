using System.Drawing;
using System.Drawing.Drawing2D;

namespace AudioVisualizer.Visualization.Effects;

public sealed class WmpBarsEffect : IVisualizationEffect
{
    public string Name => "WMP98 bars";

    private float[] _displayed = Array.Empty<float>();

    public void Reset() => _displayed = Array.Empty<float>();

    public void Update(VisualizationFrame frame)
    {
        if (_displayed.Length != frame.LeftBands.Length)
        {
            _displayed = new float[frame.LeftBands.Length];
        }

        for (var i = 0; i < frame.LeftBands.Length; i++)
        {
            _displayed[i] = MathF.Max(frame.LeftBands[i], _displayed[i] * 0.75f);
        }
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_displayed.Length == 0)
        {
            return;
        }

        var gap = 2;
        var barWidth = Math.Max(2, (bounds.Width - (_displayed.Length - 1) * gap) / _displayed.Length);
        var baseY = bounds.Height - 20;

        for (var i = 0; i < _displayed.Length; i++)
        {
            var height = (int)(_displayed[i] * (bounds.Height - 40));
            var x = i * (barWidth + gap);
            var y = baseY - height;

            using var brush = new LinearGradientBrush(
                new Rectangle(x, y, barWidth, Math.Max(1, height)),
                Color.FromArgb(0, 120, 215),
                Color.FromArgb(0, 200, 255),
                LinearGradientMode.Vertical);

            graphics.FillRectangle(brush, x, y, barWidth, Math.Max(1, height));
        }
    }
}