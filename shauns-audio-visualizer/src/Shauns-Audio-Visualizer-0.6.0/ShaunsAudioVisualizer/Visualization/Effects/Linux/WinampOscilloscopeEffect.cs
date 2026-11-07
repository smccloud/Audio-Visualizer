using Compat.Drawing;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class WinampOscilloscopeEffect : IVisualizationEffect
{
    public string Name => "Winamp oscilloscope";

    private readonly List<float> _samples = new();
    private const int MaxSamples = 512;

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

        if (_samples.Count < 2)
        {
            return;
        }

        using var pen = new Pen(Color.FromArgb(0, 255, 0), 1f);
        var points = new PointF[_samples.Count];

        for (var i = 0; i < _samples.Count; i++)
        {
            points[i] = new PointF(
                bounds.Width * i / (float)(_samples.Count - 1),
                bounds.Height / 2f - _samples[i] * bounds.Height / 2.5f);
        }

        graphics.DrawLines(pen, points);
    }
}