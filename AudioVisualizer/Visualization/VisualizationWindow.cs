using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using AudioVisualizer.Audio;
using AudioVisualizer.Audio.Analysis;
using AudioVisualizer.Visualization.Effects;

namespace AudioVisualizer.Visualization;

public sealed class VisualizationWindow : Form
{
    private readonly WasapiLoopbackCapture _capture;
    private readonly AudioLevelAnalyzer _levelAnalyzer = new();
    private readonly SpectrumAnalyzer _spectrumAnalyzer = new();
    private readonly List<IVisualizationEffect> _effects;
    private int _effectIndex;
    private readonly object _frameLock = new();
    private VisualizationFrame? _latestFrame;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private DateTime _lastBeat = DateTime.MinValue;
    private readonly System.Windows.Forms.Timer _timer;

    public VisualizationWindow()
    {
        Text = "Audio Visualizer";
        ClientSize = new Size(960, 540);
        BackColor = Color.Black;
        DoubleBuffered = true;
        KeyPreview = true;

        _effects = new List<IVisualizationEffect>
        {
            new ConsoleStyleBarsEffect(),
            new MirroredSpectrumBarsEffect(),
            new OscilloscopeTrailEffect(),
            new PulsingShapesEffect(),
            new BeatParticlesEffect(),
        };

        _capture = new WasapiLoopbackCapture();

        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();

        Load += OnLoad;
        FormClosing += OnFormClosing;
        KeyDown += OnKeyDown;
        Paint += OnPaint;
    }

    private async void OnLoad(object? sender, EventArgs e) // ubs:ignore[cs.pattern.async-void] -- WinForms event handler requires async void
    {
        try
        {
            _capture.DataAvailable += OnDataAvailable;
            await _capture.StartAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Capture error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
        }
    }

    private void OnDataAvailable(object? sender, AudioDataEventArgs data)
    {
        var levels = _levelAnalyzer.Analyze(data.Samples.Span, data.Format.Channels);
        _spectrumAnalyzer.Push(data.Samples.Span, data.Format.Channels);

        var (left, right) = _spectrumAnalyzer.Analyze(data.Format.SampleRate);
        var beat = IsBeat(levels);

        var frame = new VisualizationFrame(
            left,
            right,
            levels,
            data.Samples,
            data.Format.SampleRate,
            _stopwatch.Elapsed,
            beat);

        lock (_frameLock)
        {
            _latestFrame = frame;
        }
    }

    private bool IsBeat(AudioLevelSnapshot levels)
    {
        var now = DateTime.UtcNow;
        if (levels.Peak > 0.85f && (now - _lastBeat).TotalMilliseconds > 120)
        {
            _lastBeat = now;
            return true;
        }

        return false;
    }

    private void Tick()
    {
        VisualizationFrame? frame;
        lock (_frameLock)
        {
            frame = _latestFrame;
        }

        if (frame is not null)
        {
            _effects[_effectIndex].Update(frame);
        }

        Invalidate();
    }

    private void OnPaint(object? sender, PaintEventArgs e)
    {
        _effects[_effectIndex].Render(e.Graphics, ClientRectangle);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.KeyCode)
        {
            case Keys.Right:
            case Keys.Down:
                _effectIndex = (_effectIndex + 1) % _effects.Count;
                _effects[_effectIndex].Reset();
                Text = $"Audio Visualizer - {_effects[_effectIndex].Name}";
                break;
            case Keys.Left:
            case Keys.Up:
                _effectIndex = (_effectIndex - 1 + _effects.Count) % _effects.Count;
                _effects[_effectIndex].Reset();
                Text = $"Audio Visualizer - {_effects[_effectIndex].Name}";
                break;
            case Keys.F11:
                ToggleFullScreen();
                break;
            case Keys.Escape:
                Close();
                break;
        }
    }

    private bool _fullScreen;

    private void ToggleFullScreen()
    {
        if (_fullScreen)
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            WindowState = FormWindowState.Normal;
            TopMost = false;
            _fullScreen = false;
        }
        else
        {
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;
            TopMost = true;
            _fullScreen = true;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        _timer.Stop();
        _capture.DataAvailable -= OnDataAvailable;
        try
        {
            _capture.StopAsync().GetAwaiter().GetResult(); // ubs:ignore[cs.pattern.getawaiter-getresult,cs-task-getawaiter-getresult] -- synchronous shutdown on UI thread
        }
        catch
        {
            // Best-effort shutdown.
        }

        _capture.DisposeAsync().GetAwaiter().GetResult(); // ubs:ignore[cs.pattern.getawaiter-getresult,cs-task-getawaiter-getresult] -- synchronous shutdown on UI thread
    }
}