# Audio Visualizer

A .NET 10 recreation of the kind of audio visualizer that shipped with Windows in
the 1990s — the abstract, music-driven animations many of us remember from that
era.

This is a nostalgic, from-memory interpretation rather than a clone of any
specific product. The goal is to capture the look and feel of those classic
visualizations: fluid, colorful, reactive shapes that move with the music.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows (the visualizer targets the classic Windows desktop experience)

## Getting started

```bash
dotnet build AudioVisualizer.slnx
dotnet run --project AudioVisualizer/AudioVisualizer.csproj
```

## Status

Early days — the project is currently a console scaffold. The visualization
engine and Windows UI are still to come.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
