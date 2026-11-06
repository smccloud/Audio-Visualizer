using System.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class WmpAmbientEffect : IVisualizationEffect
{
    public string Name => "WMP98 ambient";

    private float _energy;

    public void Reset() => _energy = 0;

    public void Update(VisualizationFrame frame)
    {
        _energy = _energy * 0.9f + frame.Levels.Rms * 0.1f;
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);

        var cx = bounds.Width / 2f;
        var cy = bounds.Height / 2f;
        var radius = Math.Max(10, (int)(Math.Min(bounds.Width, bounds.Height) * (0.2f + _energy * 0.5f)));

        using var brush = new PathGradientBrush(new[]
        {
            new PointF(cx - radius, cy - radius),
            new PointF(cx + radius, cy - radius),
            new PointF(cx + radius, cy + radius),
            new PointF(cx - radius, cy + radius),
        });

        brush.CenterColor = Color.FromArgb(120, 0, 180, 220);
        brush.SurroundColors = new[] { Color.Black };

        graphics.FillRectangle(brush, bounds);
    }
}