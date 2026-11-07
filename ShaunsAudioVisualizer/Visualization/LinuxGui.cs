#if !WINDOWS
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Threading;
using ShaunsAudioVisualizer.Audio;
using ShaunsAudioVisualizer.Audio.Analysis;
using System;

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
    private readonly Canvas _canvas = new();
    private readonly int _barCount = 64;
    private readonly Rectangle[] _leftBars;
    private readonly Rectangle[] _rightBars;

    public LinuxVisualizerWindow()
    {
        Title = "Shauns Audio Visualizer - Linux";
        Width = 960;
        Height = 540;
        Background = Brushes.Black;

        _leftBars = new Rectangle[_barCount];
        _rightBars = new Rectangle[_barCount];

        for (var i = 0; i < _barCount; i++)
        {
            var left = new Rectangle
            {
                Fill = Brushes.CornflowerBlue,
                Width = 6,
            };
            var right = new Rectangle
            {
                Fill = Brushes.OrangeRed,
                Width = 6,
            };

            _leftBars[i] = left;
            _rightBars[i] = right;
            _canvas.Children.Add(left);
            _canvas.Children.Add(right);
        }

        Content = _canvas;

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
    }

    private void OnDataAvailable(object? sender, AudioDataEventArgs data)
    {
        var levels = _levelAnalyzer.Analyze(data.Samples.Span, data.Format.Channels);
        _spectrumAnalyzer.Push(data.Samples.Span, data.Format.Channels);
        var (left, right) = _spectrumAnalyzer.Analyze(data.Format.SampleRate);

        Dispatcher.UIThread.Post(() =>
        {
            var width = _canvas.Bounds.Width;
            var height = _canvas.Bounds.Height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var barWidth = width / _barCount;
            var mid = height / 2;

            for (var i = 0; i < _barCount; i++)
            {
                var leftIndex = Math.Min(i, left.Length - 1);
                var rightIndex = Math.Min(i, right.Length - 1);
                var leftHeight = left[leftIndex] * mid;
                var rightHeight = right[rightIndex] * (height - mid);

                Canvas.SetLeft(_leftBars[i], i * barWidth);
                Canvas.SetTop(_leftBars[i], mid - leftHeight);
                _leftBars[i].Height = Math.Max(1, leftHeight);

                Canvas.SetLeft(_rightBars[i], i * barWidth);
                Canvas.SetTop(_rightBars[i], mid);
                _rightBars[i].Height = Math.Max(1, rightHeight);
            }
        });
    }
}
#endif
