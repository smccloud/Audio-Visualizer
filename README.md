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

## Installer

`installer/AudioVisualizer.Installer.wixproj` is a [WiX Toolset](https://wixtoolset.org/)
v6 project that packages the app as a Windows Installer (MSI). It publishes a
self-contained, single-file `win-x64` build first, so the installed app does not
need a separate .NET runtime.

Build the MSI on Windows:

```powershell
dotnet build AudioVisualizer.slnx --configuration Release
# MSI: installer\bin\x64\Release\AudioVisualizer.msi
```

To build only the installer:

```powershell
dotnet build installer/AudioVisualizer.Installer.wixproj --configuration Release
# MSI: installer\bin\Release\AudioVisualizer.msi
```

The MSI installs to `C:\Program Files\Audio Visualizer`, adds a Start Menu
shortcut, and registers an entry in **Apps & features**. It uses the standard
WiX install-directory UI with a license page.

> **Note:** WiX can only build MSI packages on Windows. The Linux CI pipeline
> therefore builds `AudioVisualizer.Linux.slnf`, which excludes the installer. The
> MSI installer must be built locally on a Windows machine when needed.

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

On Linux, use the solution filter that excludes the Windows-only installer:

```bash
dotnet test AudioVisualizer.Linux.slnf
```

To match CI, build in Release and run the already-built tests:

```bash
dotnet build AudioVisualizer.Linux.slnf --configuration Release
dotnet test AudioVisualizer.Linux.slnf --configuration Release --no-build
```

In Visual Studio, open `AudioVisualizer.slnx` and use **Test > Test Explorer**
to run or debug individual tests. The Jenkins pipeline finds the test project
automatically and publishes the results through the Jenkins MSTest plugin.

## Continuous integration

`Jenkinsfile` builds and publishes the solution on a Windows Jenkins agent
labelled `Windows-Agent`. The agent needs a .NET 10 SDK on its `PATH`;
`global.json` pins the 10.0 SDK feature band.

The pipeline builds the full `AudioVisualizer.slnx`, including the WiX installer
project, and:

- restores and builds in Release with warnings as errors
- runs the unit tests and publishes TRX results with the MSTest plugin
- publishes a self-contained, single-file `win-x64` build and archives it
- produces the MSI through the installer project

Because the app targets `net10.0-windows`, the pipeline must run on Windows. If
you need a Linux-only build, use `AudioVisualizer.Linux.slnf`, which excludes the
WiX installer project (WiX cannot produce an MSI on Linux):

```bash
dotnet build AudioVisualizer.Linux.slnf --configuration Release
dotnet test AudioVisualizer.Linux.slnf --configuration Release --no-build
```

`Jenkinsfile.Windows` provides the same build for jobs that publish the MSI as a
first-class artifact.

## Status

The capture and analysis pipeline is in place, along with a console test
visualization. The Windows renderer and the retro visual effects are still to
come.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
