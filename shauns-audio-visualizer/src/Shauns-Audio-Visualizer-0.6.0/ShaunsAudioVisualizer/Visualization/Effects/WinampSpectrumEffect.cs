using System.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class WinampSpectrumEffect : IVisualizationEffect
{
    public string Name => "Winamp spectrum";

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
            _displayed[i] = MathF.Max(frame.LeftBands[i], _displayed[i] * 0.8f);
        }
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);

        if (_displayed.Length == 0)
        {
            return;
        }

        var gap = 1;
        var barWidth = Math.Max(2, (bounds.Width - (_displayed.Length - 1) * gap) / _displayed.Length);

        for (var i = 0; i < _displayed.Length; i++)
        {
            var height = (int)(_displayed[i] * (bounds.Height - 10));
            var x = i * (barWidth + gap);
            var y = bounds.Height - height;

            using var brush = new SolidBrush(Color.FromArgb(0, 200, 0));
            graphics.FillRectangle(brush, x, y, barWidth, Math.Max(1, height));
        }
    }
}