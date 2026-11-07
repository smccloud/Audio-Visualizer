using Compat.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class PulsingShapesEffect : IVisualizationEffect
{
    public string Name => "Pulsing shapes";

    private float _energy;
    private float _angle;

    public void Reset()
    {
        _energy = 0;
        _angle = 0;
    }

    public void Update(VisualizationFrame frame)
    {
        var level = frame.Levels.Rms;
        _energy = _energy * 0.85f + level * 0.15f;
        _angle += 0.02f + _energy * 0.1f;
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var cx = bounds.Width / 2f;
        var cy = bounds.Height / 2f;
        var radius = Math.Min(bounds.Width, bounds.Height) * (0.15f + _energy * 0.35f);

        using var matrix = new Matrix();
        matrix.RotateAt(_angle * 180f / (float)Math.PI, new PointF(cx, cy));
        graphics.Transform = matrix;

        using var brush = new SolidBrush(Color.FromArgb(180, 100, 200, 255));
        graphics.FillEllipse(brush, cx - radius, cy - radius, radius * 2, radius * 2);

        graphics.ResetTransform();
    }
}