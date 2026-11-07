using Compat.Drawing;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class WmpParticlesEffect : IVisualizationEffect
{
    public string Name => "WMP98 particles";

    private sealed class Particle
    {
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
        public int Life;
    }

    private readonly List<Particle> _particles = new();
    private readonly Random _random = new(98765);

    public void Reset() => _particles.Clear();

    public void Update(VisualizationFrame frame)
    {
        if (frame.Beat)
        {
            for (var i = 0; i < 25; i++)
            {
                var angle = (float)(_random.NextDouble() * Math.PI * 2);
                var speed = (float)(_random.NextDouble() * 3 + 0.5);
                _particles.Add(new Particle
                {
                    X = 0.5f,
                    Y = 0.5f,
                    Vx = (float)Math.Cos(angle) * speed,
                    Vy = (float)Math.Sin(angle) * speed,
                    Life = 50,
                });
            }
        }

        foreach (var p in _particles)
        {
            p.X += p.Vx * 0.016f;
            p.Y += p.Vy * 0.016f;
            p.Life--;
        }

        _particles.RemoveAll(p => p.Life <= 0);
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);

        using var brush = new SolidBrush(Color.White);

        foreach (var p in _particles)
        {
            var size = Math.Max(1, p.Life / 10);
            graphics.FillEllipse(brush, p.X * bounds.Width, p.Y * bounds.Height, size, size);
        }
    }
}