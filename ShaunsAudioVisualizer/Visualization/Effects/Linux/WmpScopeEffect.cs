using Compat.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class WmpScopeEffect : IVisualizationEffect
{
    public string Name => "WMP98 scope";

    private readonly List<float> _samples = new();
    private const int MaxSamples = 1024;

    public void Reset() => _samples.Clear();

    public void Update(VisualizationFrame frame)
    {
        if (frame.Samples.IsEmpty)
        {
            return;
        }

        foreach (var sample in frame.Samples.Span)
        {
            _samples.Add(sample);
        }

        if (_samples.Count > MaxSamples)
        {
            _samples.RemoveRange(0, _samples.Count - MaxSamples);
        }
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_samples.Count < 2)
        {
            return;
        }

        using var pen = new Pen(Color.Lime, 2f);
        var points = new PointF[_samples.Count];

        for (var i = 0; i < _samples.Count; i++)
        {
            points[i] = new PointF(
                bounds.Width * i / (float)(_samples.Count - 1),
                bounds.Height / 2f - _samples[i] * bounds.Height / 3f);
        }

        graphics.DrawLines(pen, points);
    }
}