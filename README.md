# Audio Visualizer

A .NET 10 recreation of the kind of audio visualizer that shipped with Windows in
the 1990s — the abstract, music-driven animations many of us remember from that
era.

This is a nostalgic, from-memory interpretation rather than a clone of any
specific product. The goal is to capture the look and feel of those classic
visualizations: fluid, colorful, reactive shapes that move with the music.

## How it works

- **Capture** — `WasapiLoopbackCapture` records whatever is playing on the
  default Windows playback device using WASAPI loopback. Only documented
  Windows Core Audio APIs are used; there are no third-party dependencies.
- **Analysis** — `AudioLevelAnalyzer` reports peak/RMS levels and
  `SpectrumAnalyzer` turns the stream into a log-spaced frequency spectrum.
- **Visualization** — `ConsoleVisualizer` draws the spectrum as live bars.
  This is a temporary test renderer used to verify the audio pipeline; the
  real Windows visualization comes next.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 or later

## Getting started

```bash
dotnet build AudioVisualizer.slnx
dotnet run --project AudioVisualizer/AudioVisualizer.csproj
```

Play some audio, then watch the bars react. Press **Ctrl+C** to stop.

## Status

The capture and analysis pipeline is in place, along with a console test
visualization. The Windows renderer and the retro visual effects are still to
come.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
