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
    private bool _pendingBeat;
    private readonly float[] _peakHistory = new float[10];
    private int _peakHistoryIndex;
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
        try
        {
            var stream = typeof(VisualizationWindow).Assembly.GetManifestResourceStream("AudioVisualizer.app-icon.ico");
            if (stream is not null)
            {
                Icon = new Icon(stream);
            }
        }
        catch
        {
            // Icon is cosmetic; ignore failures.
        }
        DoubleBuffered = true;
        KeyPreview = true;

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

        _effectIndex = LoadLastEffectIndex();
        Text = $"Audio Visualizer - {_effects[_effectIndex].Name}";

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
        if (beat)
        {
            _pendingBeat = true;
        }

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
        // Detect beats as onsets: a peak that rises well above the recent
        // minimum peak level.
        var minRecent = _peakHistory.Min();
        var isBeat = levels.Peak - minRecent > 0.15f
            && levels.Peak > 0.1f
            && (now - _lastBeat).TotalMilliseconds > 80;

        _peakHistory[_peakHistoryIndex] = levels.Peak;
        _peakHistoryIndex = (_peakHistoryIndex + 1) % _peakHistory.Length;

        if (isBeat)
        {
            _lastBeat = now;
        }

        return isBeat;
    }

    private void Tick()
    {
        var beat = _pendingBeat;
        _pendingBeat = false;

        VisualizationFrame? frame;
        lock (_frameLock)
        {
            frame = _latestFrame;
        }

        if (frame is not null)
        {
            // A beat may have occurred on a packet that was already superseded
            // before this tick ran, so latch it and replay it on the frame handed
            // to the effect.
            if (beat && !frame.Beat)
            {
                frame = new VisualizationFrame(
                    frame.LeftBands,
                    frame.RightBands,
                    frame.Levels,
                    frame.Samples,
                    frame.SampleRate,
                    frame.Elapsed,
                    true);
            }

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
        if (e.Control && e.Shift && e.KeyCode == Keys.W)
        {
            Mp3Player.PlayEmbedded("AudioVisualizer.LockChime.mp3");
            e.Handled = true;
            return;
        }

        switch (e.KeyCode)
        {
            case Keys.Right:
            case Keys.Down:
                _effectIndex = (_effectIndex + 1) % _effects.Count;
                _effects[_effectIndex].Reset();
                Text = $"Audio Visualizer - {_effects[_effectIndex].Name}";
                SaveLastEffectIndex();
                break;
            case Keys.Left:
            case Keys.Up:
                _effectIndex = (_effectIndex - 1 + _effects.Count) % _effects.Count;
                _effects[_effectIndex].Reset();
                Text = $"Audio Visualizer - {_effects[_effectIndex].Name}";
                SaveLastEffectIndex();
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
        SaveLastEffectIndex();
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

    private static string SettingsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioVisualizer",
            "last-effect.txt");

    private int LoadLastEffectIndex()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var text = File.ReadAllText(SettingsPath).Trim();
                if (int.TryParse(text, out var index) && index >= 0 && index < _effects.Count)
                {
                    return index;
                }
            }
        }
        catch
        {
            // Ignore settings errors; fall back to default.
        }

        return 0;
    }

    private void SaveLastEffectIndex()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SettingsPath, _effectIndex.ToString());
        }
        catch
        {
            // Best-effort persistence.
        }
    }
}