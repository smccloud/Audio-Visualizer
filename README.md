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

By default the app opens a graphical visualization window. Use the arrow keys to
switch effects, **F11** for full screen, and **Esc** to exit. Play some audio
and watch the effects react.

For the original text-based renderer (useful when troubleshooting the audio
pipeline), run with `--console`:

```bash
dotnet run --project AudioVisualizer/AudioVisualizer.csproj -- --console
```

In console mode, press **Ctrl+C** to stop.

## Screenshots

The built-in effects (switch with the arrow keys):

| Console style bars | Circular spectrum |
|---|---|
| ![Console style bars](screenshots/effect_ConsoleStyleBars.png?v=2) | ![Circular spectrum](screenshots/effect_CircularSpectrum.png?v=2) |

| Mirrored spectrum bars | Oscilloscope trail |
|---|---|
| ![Mirrored spectrum bars](screenshots/effect_MirroredSpectrumBars.png?v=2) | ![Oscilloscope trail](screenshots/effect_OscilloscopeTrail.png?v=2) |

| Pulsing shapes | Beat particles |
|---|---|
| ![Pulsing shapes](screenshots/effect_PulsingShapes.png?v=2) | ![Beat particles](screenshots/effect_BeatParticles.png?v=2) |

| Lissajous / phase scope | Particle fountain |
|---|---|
| ![Lissajous / phase scope](screenshots/effect_Lissajous.png?v=2) | ![Particle fountain](screenshots/effect_ParticleFountain.png?v=2) |

| Starfield | Milkdrop-style |
|---|---|
| ![Starfield](screenshots/effect_Starfield.png?v=2) | ![Milkdrop-style](screenshots/effect_Milkdrop.png?v=2) |

| Stereo VU meters |
|---|
| ![Stereo VU meters](screenshots/effect_VUMeter.png?v=2) |

## Installer

`installer/AudioVisualizer.Installer.wixproj` is a [WiX Toolset](https://wixtoolset.org/)
v7 project that packages the app as a Windows Installer (MSI). It publishes a
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

> **Note:** WiX can only build MSI packages on Windows. On other platforms use
> `AudioVisualizer.Linux.slnf`, which excludes the installer. The MSI must be
> built on a Windows machine.

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

### Windows-specific tests

`AudioVisualizer.Windows.Tests` is an xUnit project that covers the Windows
interop and WASAPI capture layer. It targets `net10.0-windows` and is included
in `AudioVisualizer.slnx`, so Visual Studio Test Explorer discovers it
automatically on a Windows developer machine.

The interop and lifecycle tests do not need a live audio stream:

- `WaveFormatReader` parsing of PCM, IEEE float, and `WAVE_FORMAT_EXTENSIBLE`
  headers, including channel masks and sub-formats
- rejection of malformed or unsupported formats
- `AudioFormat` values derived from native formats
- `HResult.ThrowIfFailed` success and failure behavior
- `WasapiGuids` and interop enum values
- `WasapiLoopbackCapture` disposed/twice-start/stop-before-start/idempotent
  disposal behavior

A hardware-dependent smoke test is provided under the `Category=WindowsAudio`
trait. It starts loopback capture on the default render device, waits for a
packet while audio is playing, and reports the negotiated `AudioFormat`. It
passes without doing anything when no audio device or active playback is
available, so it never breaks a normal test run.

Prerequisites:

- Windows 10 or later
- .NET 10 SDK
- An audio playback device for the smoke test

Running from Visual Studio: open `AudioVisualizer.slnx`, build the solution, and
use **Test > Test Explorer**. The Windows tests appear alongside the
cross-platform tests.

Running from the command line:

```powershell
dotnet test AudioVisualizer.slnx
```

Run everything except the hardware-dependent smoke tests:

```powershell
dotnet test AudioVisualizer.slnx --filter "Category!=WindowsAudio"
```

Run only the hardware-dependent smoke tests:

```powershell
dotnet test AudioVisualizer.slnx --filter "Category=WindowsAudio"
```

These tests cannot run on the Linux Jenkins agent because they target
`net10.0-windows` and exercise Windows Core Audio APIs. Linux builds use
`AudioVisualizer.Linux.slnf`, which excludes both the installer and the
Windows test project, so `dotnet test` on Linux stays green. The
cross-platform tests in `AudioVisualizer.Tests` and the Windows tests in
`AudioVisualizer.Windows.Tests` together cover the whole solution: pure logic
on every platform, Windows interop and capture lifecycle only where the
required APIs exist.

## Continuous integration

`Jenkinsfile` builds and publishes the solution on the `Windows-Agent` node. The
agent is pinned by label, so the built-in (controller) node is never scheduled —
if no Windows agent is online the build waits in the queue instead. The agent
needs a .NET 10 SDK on its `PATH`, and `global.json` pins the 10.0 SDK feature
band.

The pipeline builds the full `AudioVisualizer.slnx`, including the WiX installer
project, and:

- restores and builds in Release with warnings as errors
- runs the unit tests and publishes TRX results with the MSTest plugin
- publishes and archives a self-contained, single-file `win-x64` build
- produces and archives the MSI through the installer project
- runs Ultimate Bug Scanner (UBS) inside WSL; UBS is installed into the WSL
  user's `~/.local/bin` on first use

For a Linux-only build outside Jenkins, use `AudioVisualizer.Linux.slnf`, which
excludes the WiX installer project (WiX cannot produce an MSI on Linux):

```bash
dotnet build AudioVisualizer.Linux.slnf --configuration Release
dotnet test AudioVisualizer.Linux.slnf --configuration Release --no-build
```

The app targets `net10.0-windows`, so Linux machines can build, test, and
package it but cannot run it.

`Jenkinsfile.Windows` is a minimal installer-only pipeline for jobs that just
need the MSI: it builds `installer/AudioVisualizer.Installer.wixproj` (which
publishes the app payload itself) and archives the resulting MSI.

## Status

The capture and analysis pipeline is in place, along with a console test
visualization. The Windows renderer and the retro visual effects are still to
come.

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.

