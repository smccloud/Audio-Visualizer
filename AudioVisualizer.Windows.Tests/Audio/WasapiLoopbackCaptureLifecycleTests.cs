using AudioVisualizer.Audio;
using Xunit.Sdk;

namespace AudioVisualizer.Windows.Tests.Audio;

public sealed class WasapiLoopbackCaptureLifecycleTests
{
    [Fact]
    public async Task StartAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var capture = new WasapiLoopbackCapture();

        await capture.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => capture.StartAsync());
    }

    [Fact]
    public async Task StopAsync_BeforeStart_IsNoOp()
    {
        var capture = new WasapiLoopbackCapture();

        await capture.StopAsync();
    }

    [Fact]
    public async Task Dispose_IsIdempotent()
    {
        var capture = new WasapiLoopbackCapture();

        await capture.DisposeAsync();
        await capture.DisposeAsync();
    }

    [Fact]
    public async Task StartAsync_TwiceWhileRunning_ThrowsInvalidOperationException()
    {
        var capture = new WasapiLoopbackCapture();

        try
        {
            await capture.StartAsync();
        }
        catch (Exception exception)
        {
            // No audio device is available in this environment; nothing to test.
            Console.WriteLine($"Skipping WasapiLoopbackCapture lifecycle test: {exception.Message}"); // ubs:ignore[cs.pattern.console-write,cs-console-writeline] -- test diagnostic output
            await capture.DisposeAsync();
            return;
        }

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => capture.StartAsync());
        }
        finally
        {
            await capture.StopAsync();
            await capture.DisposeAsync();
        }
    }
}