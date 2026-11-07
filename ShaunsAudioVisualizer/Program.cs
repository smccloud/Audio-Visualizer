using System.Diagnostics;
using System.Text;
#if NET10_0_WINDOWS
using System.Windows.Forms;
#endif
using ShaunsAudioVisualizer.Audio;
using ShaunsAudioVisualizer.Audio.Analysis;
using ShaunsAudioVisualizer.Visualization;
using System;
using System.Threading.Tasks;

#if NET10_0_WINDOWS
if (args.Contains("--console") == false && OperatingSystem.IsWindows())
{
    ApplicationConfiguration.Initialize();
    Application.Run(new VisualizationWindow());
    return 0;
}
#endif

try
{
    Console.OutputEncoding = Encoding.UTF8;
}
catch (IOException exception)
{
    // Output may be redirected or unavailable; the visualization still works
    // without UTF-8, so fall back to the default encoding and continue.
    Debug.WriteLine($"Unable to set UTF-8 output encoding: {exception.Message}");
}

IAudioCaptureSource capture = OperatingSystem.IsWindows()
    ? new WasapiLoopbackCapture()
    : new LinuxAudioCapture();
await using var captureLifetime = (IAsyncDisposable)capture;
var levelAnalyzer = new AudioLevelAnalyzer();
var spectrumAnalyzer = new SpectrumAnalyzer();
var visualizer = new ConsoleVisualizer(
    minFrequency: spectrumAnalyzer.MinFrequency,
    maxFrequency: spectrumAnalyzer.MaxFrequency);
var stop = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
var lastRender = Stopwatch.StartNew();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    stop.TrySetResult(true);
};

capture.DataAvailable += (_, data) =>
{
    var levels = levelAnalyzer.Analyze(data.Samples.Span, data.Format.Channels);
    spectrumAnalyzer.Push(data.Samples.Span, data.Format.Channels);

    // Redraw at roughly 20 frames per second regardless of packet rate.
    if (lastRender.ElapsedMilliseconds < 50)
    {
        return;
    }

    lastRender.Restart();
    var (leftBands, rightBands) = spectrumAnalyzer.Analyze(data.Format.SampleRate);
    visualizer.Render(leftBands, rightBands, levels, data.Format);
};

capture.Error += (_, error) =>
{
    Console.Error.WriteLine($"\nCapture error: {error.Exception.Message}");
    stop.TrySetResult(false);
};

try
{
    await capture.StartAsync();
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Unable to start loopback capture: {exception.Message}");
    return 1;
}

Console.WriteLine($"Capturing system audio ({capture.Format})"); // ubs:ignore[cs.pattern.console-write,cs-console-writeline] -- intended console UI output
Console.WriteLine("Waiting for audio... press Ctrl+C to stop."); // ubs:ignore[cs.pattern.console-write,cs-console-writeline] -- intended console UI output

await stop.Task;
await capture.StopAsync();

Console.WriteLine(); // ubs:ignore[cs.pattern.console-write,cs-console-writeline] -- intended console UI output
Console.WriteLine("Stopped."); // ubs:ignore[cs.pattern.console-write,cs-console-writeline] -- intended console UI output
return 0;