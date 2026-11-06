using System.Drawing;
using System.Drawing.Drawing2D;

namespace AudioVisualizer.Visualization.Effects;

public sealed class LissajousEffect : IVisualizationEffect
{
    public string Name => "Lissajous / phase scope";

    private PointF[] _points = Array.Empty<PointF>();

    public void Reset() => _points = Array.Empty<PointF>();

    public void Update(VisualizationFrame frame)
    {
        var samples = frame.Samples.Span;
        var channels = frame.Levels.ChannelPeaks.Length;
        if (channels < 1)
        {
            channels = 1;
        }

        var frames = samples.Length / channels;
        if (frames == 0)
        {
            _points = Array.Empty<PointF>();
            return;
        }

        // Keep a modest number of points for a smooth, stable trace.
        var step = Math.Max(1, frames / 512);
        var count = frames / step;
        var points = new PointF[count];

        for (var i = 0; i < count; i++)
        {
            var frameIndex = i * step;
            var offset = frameIndex * channels;
            var left = samples[offset];
            var right = channels > 1 ? samples[offset + 1] : left;
            points[i] = new PointF(left, right);
        }

        _points = points;
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_points.Length < 2)
        {
            return;
        }

        var cx = bounds.Width / 2f;
        var cy = bounds.Height / 2f;
        var scale = Math.Min(bounds.Width, bounds.Height) / 2f - 10;

        // Axes
        using (var axisPen = new Pen(Color.FromArgb(60, 60, 60), 1f))
        {
            graphics.DrawLine(axisPen, cx - scale, cy, cx + scale, cy);
            graphics.DrawLine(axisPen, cx, cy - scale, cx, cy + scale);
        }

        // Trace
        var pts = new PointF[_points.Length];
        for (var i = 0; i < _points.Length; i++)
        {
            pts[i] = new PointF(cx + _points[i].X * scale, cy - _points[i].Y * scale);
        }

        using var pen = new Pen(Color.LimeGreen, 1.5f);
        graphics.DrawLines(pen, pts);
    }
}