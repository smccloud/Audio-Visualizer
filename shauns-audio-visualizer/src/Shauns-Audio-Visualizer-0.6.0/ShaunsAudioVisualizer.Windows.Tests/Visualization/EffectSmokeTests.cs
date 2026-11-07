using ShaunsAudioVisualizer.Audio;
using ShaunsAudioVisualizer.Audio.Analysis;
using ShaunsAudioVisualizer.Visualization.Effects;

using System.Drawing;
using System.Threading.Tasks;

using Xunit;

namespace ShaunsAudioVisualizer.Windows.Tests.Visualization;

public sealed class EffectSmokeTests
{
    private static VisualizationFrame CreateFrame()
    {
        var levels = new AudioLevelSnapshot(
            new[] { 0.5f, 0.25f },
            new[] { 0.25f, 0.125f },
            peak: 0.5f,
            rms: 0.25f,
            frameCount: 480);
        var samples = new float[8];
        return new VisualizationFrame(
            new[] { 0.2f, 0.5f, 0.8f },
            new[] { 0.1f, 0.4f, 0.7f },
            levels,
            samples.AsMemory(),
            48000,
            TimeSpan.Zero,
            beat: true);
    }

    [Fact]
    public void Effects_UpdateAndRender_DoNotThrow()
    {
        var frame = CreateFrame();
        IVisualizationEffect[] effects =
        {
            new ConsoleStyleBarsEffect(),
            new CircularSpectrumEffect(),
            new MirroredSpectrumBarsEffect(),
            new OscilloscopeTrailEffect(),
            new PulsingShapesEffect(),
            new BeatParticlesEffect(),
            new LissajousEffect(),
            new ParticleFountainEffect(),
            new StarfieldEffect(),
            new MilkdropStyleEffect(),
            new VUMeterEffect(),
        };

        foreach (var effect in effects)
        {
            effect.Reset();
            effect.Update(frame);

            using var bitmap = new Bitmap(320, 240);
            using var graphics = Graphics.FromImage(bitmap);
            var exception = Record.Exception(() => effect.Render(graphics, new Rectangle(0, 0, 320, 240)));
            Assert.Null(exception);
        }
    }
}