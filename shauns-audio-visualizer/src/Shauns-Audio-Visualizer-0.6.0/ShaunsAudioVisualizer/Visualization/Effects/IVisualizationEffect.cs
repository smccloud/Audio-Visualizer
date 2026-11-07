using System.Drawing;

namespace ShaunsAudioVisualizer.Visualization.Effects;

/// <summary>
/// A music-reactive visualization effect.
/// </summary>
public interface IVisualizationEffect
{
    /// <summary>Display name shown in the window title.</summary>
    string Name { get; }

    /// <summary>Resets any internal state.</summary>
    void Reset();

    /// <summary>Advances the effect using the latest audio frame.</summary>
    void Update(VisualizationFrame frame);

    /// <summary>Renders the effect into the supplied graphics surface.</summary>
    void Render(Graphics graphics, Rectangle bounds);
}