using System.Drawing;
using System.Drawing.Drawing2D;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class ParticleFountainEffect : IVisualizationEffect
{
    public string Name => "Particle fountain";

    private sealed class Particle
    {
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
        public float Life;
        public float MaxLife;
        public Color Color;
    }

    private readonly List<Particle> _particles = new();
    private readonly Random _random = new(98765);

    public void Reset() => _particles.Clear();

    public void Update(VisualizationFrame frame)
    {
        var left = frame.LeftBands;
        var right = frame.RightBands;

        // Bass level from the lowest few bands.
        var bass = 0f;
        var count = 0;
        for (var i = 0; i < Math.Min(4, left.Length); i++)
        {
            bass += left[i];
            count++;
        }
        for (var i = 0; i < Math.Min(4, right.Length); i++)
        {
            bass += right[i];
            count++;
        }
        if (count > 0)
        {
            bass /= count;
        }

        // Emit particles proportional to bass.
        var emit = (int)(bass * 20);
        for (var i = 0; i < emit; i++)
        {
            var angle = (float)(_random.NextDouble() * Math.PI / 3 - Math.PI / 6);
            var speed = (float)(_random.NextDouble() * 3 + 2 + bass * 4);
            _particles.Add(new Particle
            {
                X = 0.5f,
                Y = 0.95f,
                Vx = (float)Math.Sin(angle) * speed * 0.02f,
                Vy = -(float)Math.Cos(angle) * speed * 0.02f,
                Life = 60,
                MaxLife = 60,
                Color = ColorFromHsv((float)(_random.NextDouble() * 60 + 10), 1f, 1f),
            });
        }

        foreach (var p in _particles)
        {
            p.X += p.Vx;
            p.Y += p.Vy;
            p.Vy += 0.0008f; // gravity
            p.Life--;
        }

        _particles.RemoveAll(p => p.Life <= 0 || p.Y > 1.1f);
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        foreach (var p in _particles)
        {
            var x = p.X * bounds.Width;
            var y = p.Y * bounds.Height;
            var alpha = (int)(255 * (p.Life / p.MaxLife));
            if (alpha < 0) alpha = 0;
            using var brush = new SolidBrush(Color.FromArgb(alpha, p.Color));
            var size = 3f;
            graphics.FillEllipse(brush, x, y, size, size);
        }
    }

    private static Color ColorFromHsv(float h, float s, float v)
    {
        var hi = (int)(h / 60f) % 6;
        var f = h / 60f - (int)(h / 60f);
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f) * s);

        float r, g, b;
        switch (hi)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }

        return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }
}