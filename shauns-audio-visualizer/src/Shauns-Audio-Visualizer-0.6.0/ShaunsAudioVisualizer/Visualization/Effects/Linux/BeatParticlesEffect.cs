using Compat.Drawing;

namespace ShaunsAudioVisualizer.Visualization.Effects;

public sealed class BeatParticlesEffect : IVisualizationEffect
{
    public string Name => "Beat particles";

    private readonly List<Particle> _particles = new();
    private readonly Random _random = new(12345);

    private sealed class Particle
    {
        public float X;
        public float Y;
        public float Vx;
        public float Vy;
        public int Life;
    }

    public void Reset() => _particles.Clear();

    public void Update(VisualizationFrame frame)
    {
        if (frame.Beat)
        {
            for (var i = 0; i < 40; i++)
            {
                var angle = (float)(_random.NextDouble() * Math.PI * 2);
                var speed = (float)(_random.NextDouble() * 4 + 1);
                _particles.Add(new Particle
                {
                    X = 0.5f,
                    Y = 0.5f,
                    Vx = (float)Math.Cos(angle) * speed,
                    Vy = (float)Math.Sin(angle) * speed,
                    Life = 40,
                });
            }
        }

        foreach (var particle in _particles)
        {
            particle.X += particle.Vx * 0.016f;
            particle.Y += particle.Vy * 0.016f;
            particle.Life--;
        }

        _particles.RemoveAll(p => p.Life <= 0);
    }

    public void Render(Graphics graphics, Rectangle bounds)
    {
        graphics.Clear(Color.Black);

        using var brush = new SolidBrush(Color.White);

        foreach (var particle in _particles)
        {
            var x = particle.X * bounds.Width;
            var y = particle.Y * bounds.Height;
            var size = Math.Max(1, particle.Life / 8);
            graphics.FillEllipse(brush, x, y, size, size);
        }
    }
}