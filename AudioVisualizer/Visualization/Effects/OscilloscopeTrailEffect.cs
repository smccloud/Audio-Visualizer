using System.Drawing;
using System.Drawing.Drawing2D;

namespace AudioVisualizer.Visualization.Effects;

public sealed class OscilloscopeTrailEffect : IVisualizationEffect
{
    public string Name => "Oscilloscope trail";

    private readonly List<float> _history = new();
    private const int MaxSamples = 2048;

    public void Reset() => _history.Clear();

    public void Update(VisualizationFrame frame)
    {
        if (frame.Samples.IsEmpty)
        {
            return;
        }

        foreach (var sample in frame.Samples.Span)
        {
            _history.Add(sample);
        }

        if (_history.Count > MaxSamples)
        {
            _history.RemoveRange(0, _history.Count - MaxSamples);
        }
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_history.Count < 2)
        {
            return;
        }

        using var pen = new Pen(Color.LimeGreen, 2f);
        var points = new PointF[_history.Count];

        for (var i = 0; i < _history.Count; i++)
        {
            var x = bounds.Width * i / (float)(_history.Count - 1);
            var y = bounds.Height / 2f - _history[i] * bounds.Height / 2f;
            points[i] = new PointF(x, y);
        }

        graphics.DrawLines(pen, points);
    }
}