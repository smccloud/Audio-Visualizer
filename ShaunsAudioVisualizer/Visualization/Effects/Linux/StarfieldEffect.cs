using Compat.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class StarfieldEffect : IVisualizationEffect
{
    public string Name => "Starfield / tunnel";

    private sealed class Star
    {
        public float X;
        public float Y;
        public float Z;
    }

    private readonly Star[] _stars;
    private readonly Random _random = new(24680);

    public StarfieldEffect(int starCount = 200)
    {
        _stars = new Star[starCount];
        for (var i = 0; i < _stars.Length; i++)
        {
            _stars[i] = NewStar(true);
        }
    }

    public void Reset()
    {
        for (var i = 0; i < _stars.Length; i++)
        {
            _stars[i] = NewStar(true);
        }
    }

    private Star NewStar(bool randomZ)
    {
        return new Star
        {
            X = (float)(_random.NextDouble() * 2 - 1),
            Y = (float)(_random.NextDouble() * 2 - 1),
            Z = randomZ ? (float)(_random.NextDouble()) : 1f,
        };
    }

    public void Update(VisualizationFrame frame)
    {
        var speed = 0.02f + frame.Levels.Rms * 0.05f;

        for (var i = 0; i < _stars.Length; i++)
        {
            var s = _stars[i];
            s.Z -= speed;
            if (s.Z <= 0.01f)
            {
                _stars[i] = NewStar(false);
            }
        }
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var cx = bounds.Width / 2f;
        var cy = bounds.Height / 2f;

        foreach (var s in _stars)
        {
            var scale = 1f / s.Z;
            var x = cx + s.X * scale * (bounds.Width / 4f);
            var y = cy + s.Y * scale * (bounds.Height / 4f);

            if (x < 0 || x >= bounds.Width || y < 0 || y >= bounds.Height)
            {
                continue;
            }

            var brightness = (int)(255 * Math.Clamp(1f - s.Z, 0f, 1f));
            var size = Math.Max(1f, (1f - s.Z) * 4f);

            using var brush = new SolidBrush(Color.FromArgb(brightness, 255, 255, 255));
            graphics.FillEllipse(brush, x, y, size, size);
        }
    }
}