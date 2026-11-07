using Compat.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class MirroredSpectrumBarsEffect : IVisualizationEffect
{
    public string Name => "Mirrored spectrum bars";

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
        var gap = 2;
        var barWidth = Math.Max(1, (bounds.Width - (count - 1) * gap) / count);
        var midY = bounds.Height / 2;

        using var leftBrush = new SolidBrush(Color.FromArgb(80, 120, 255));
        using var rightBrush = new SolidBrush(Color.FromArgb(255, 90, 120));

        for (var i = 0; i < count; i++)
        {
            var x = i * (barWidth + gap);
            var leftHeight = (int)(_left[i] * midY);
            var rightHeight = (int)(_right.Length > i ? _right[i] * midY : 0);

            graphics.FillRectangle(leftBrush, x, midY - leftHeight, barWidth, leftHeight);
            graphics.FillRectangle(rightBrush, x, midY, barWidth, rightHeight);
        }
    }
}