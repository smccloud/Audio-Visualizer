#if !WINDOWS
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Diagnostics;
using Avalonia.Threading;
using Compat.Drawing;
using ShaunsAudioVisualizer.Audio;
using ShaunsAudioVisualizer.Audio.Analysis;
using ShaunsAudioVisualizer.Visualization.Effects;
using SkiaSharp;
using System;
using System.IO;

namespace ShaunsAudioVisualizer.Visualization;

public static class LinuxGui
{
    public static int Run(string[] args)
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}

public sealed class App : Application
{
    public override void Initialize()
    {
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new LinuxVisualizerWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

public sealed class LinuxVisualizerWindow : Window
{
    private readonly LinuxAudioCapture _capture = new();
    private readonly AudioLevelAnalyzer _levelAnalyzer = new();
    private readonly SpectrumAnalyzer _spectrumAnalyzer = new();
    private readonly Avalonia.Controls.Image _image = new();
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;
    private readonly List<IVisualizationEffect> _effects;
    private int _effectIndex;
    private readonly Stopwatch _stopwatch = System.Diagnostics.Stopwatch.StartNew();
    private DateTime _lastBeat = DateTime.MinValue;
    private readonly float[] _peakHistory = new float[10];
    private int _peakHistoryIndex;
    private bool _pendingBeat;
    private readonly object _frameLock = new();
    private VisualizationFrame? _latestFrame;

    public LinuxVisualizerWindow()
    {
        Title = "Shauns Audio Visualizer - Linux";
        Width = 960;
        Height = 540;
        Background = Brushes.Black;

        _bitmap = new SKBitmap(960, 540);
        _canvas = new SKCanvas(_bitmap);

        _effects = new List<IVisualizationEffect>
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
            new WmpAmbientEffect(),
            new WmpBarsEffect(),
            new WmpScopeEffect(),
            new WmpParticlesEffect(),
            new WinampSpectrumEffect(),
            new WinampOscilloscopeEffect(),
        };

        _effectIndex = 0;
        Title = $"Audio Visualizer - {_effects[_effectIndex].Name}";

        Content = _image;

        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Right || e.Key == Key.Down)
            {
                _effectIndex = (_effectIndex + 1) % _effects.Count;
                _effects[_effectIndex].Reset();
                Title = $"Audio Visualizer - {_effects[_effectIndex].Name}";
            }
            else if (e.Key == Key.Left || e.Key == Key.Up)
            {
                _effectIndex = (_effectIndex + _effects.Count - 1) % _effects.Count;
                _effects[_effectIndex].Reset();
                Title = $"Audio Visualizer - {_effects[_effectIndex].Name}";
            }
        };

        Opened += async (_, _) =>
        {
            try
            {
                _capture.DataAvailable += OnDataAvailable;
                await _capture.StartAsync();
            }
            catch (Exception exception)
            {
                Title = $"Capture error: {exception.Message}";
            }
        };

        Closed += (_, _) =>
        {
            _ = _capture.StopAsync();
        };

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) => Tick();
        timer.Start();
    }

    private void OnDataAvailable(object? sender, AudioDataEventArgs data)
    {
        var levels = _levelAnalyzer.Analyze(data.Samples.Span, data.Format.Channels);
        _spectrumAnalyzer.Push(data.Samples.Span, data.Format.Channels);

        var (left, right) = _spectrumAnalyzer.Analyze(data.Format.SampleRate);
        var beat = IsBeat(levels);
        if (beat) _pendingBeat = true;

        var frame = new VisualizationFrame(
            left, right, levels, data.Samples, data.Format.SampleRate,
            _stopwatch.Elapsed, beat);

        lock (_frameLock) { _latestFrame = frame; }
    }

    private bool IsBeat(AudioLevelSnapshot levels)
    {
        var now = DateTime.UtcNow;
        var minRecent = _peakHistory.Min();
        var isBeat = levels.Peak - minRecent > 0.15f
            && levels.Peak > 0.1f
            && (now - _lastBeat).TotalMilliseconds > 80;

        _peakHistory[_peakHistoryIndex] = levels.Peak;
        _peakHistoryIndex = (_peakHistoryIndex + 1) % _peakHistory.Length;

        if (isBeat) _lastBeat = now;
        return isBeat;
    }

    private void Tick()
    {
        var beat = _pendingBeat;
        _pendingBeat = false;

        VisualizationFrame? frame;
        lock (_frameLock) { frame = _latestFrame; }

        if (frame is not null)
        {
            if (beat && !frame.Beat)
            {
                frame = new VisualizationFrame(
                    frame.LeftBands, frame.RightBands, frame.Levels, frame.Samples,
                    frame.SampleRate, frame.Elapsed, true);
            }

            _effects[_effectIndex].Update(frame);

            var bounds = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);
            _canvas.Clear(SKColors.Black);
            var g = new Compat.Drawing.Graphics(_canvas);
            _effects[_effectIndex].Render(g, bounds);

            using var ms = new MemoryStream();
            _bitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
            ms.Position = 0;
            var bmp = new Avalonia.Media.Imaging.Bitmap(ms);
            _image.Source = bmp;
        }
    }
}
#endif
