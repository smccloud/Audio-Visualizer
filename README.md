# Audio Visualizer

[![Build Status](https://jenkins.smccloud.com/buildStatus/icon?job=Audio-Visualizer&style=plastic)](https://jenkins.smccloud.com/job/Audio-Visualizer/)

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

## Testing

`AudioVisualizer.Tests` is an xUnit project covering the platform-independent
audio analysis and conversion code: the FFT, spectrum bands, level metering,
PCM/float sample conversion, audio format helpers, and the console renderer.
These are pure logic tests and never open an audio device, so they run on both
Windows and Linux even though the app targets `net10.0-windows`.

Run the suite from the repository root:

```bash
dotnet test AudioVisualizer.slnx
```

To match CI, build in Release and run the already-built tests:

```bash
dotnet build AudioVisualizer.slnx --configuration Release
dotnet test AudioVisualizer.slnx --configuration Release --no-build
```

In Visual Studio, open `AudioVisualizer.slnx` and use **Test > Test Explorer**
to run or debug individual tests. The Jenkins pipeline finds the test project
automatically and publishes the results through the Jenkins MSTest plugin.

## Continuous integration

`Jenkinsfile` builds and publishes the solution on a Linux Jenkins agent. The
agent needs a .NET 10 SDK on its `PATH`; `global.json` pins the 10.0 SDK feature
band.

The pipeline:

- restores and builds in Release with warnings as errors
- runs the unit tests and publishes TRX results with the MSTest plugin
- publishes a framework-dependent `win-x64` build and archives it

Because the app targets `net10.0-windows`, Linux can build and package it but
cannot run it — the audio capture smoke test still happens on Windows.

## Status

The capture and analysis pipeline is in place, along with a console test
visualization. The Windows renderer and the retro visual effects are still to
come.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
