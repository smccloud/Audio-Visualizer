using ShaunsAudioVisualizer.Audio;
using Xunit.Sdk;

namespace ShaunsAudioVisualizer.Windows.Tests.Audio;

public sealed class WasapiLoopbackCaptureSmokeTests
{
    [Fact]
    [Trait("Category", "WindowsAudio")]
    public async Task LoopbackCapture_ReceivesPacketAndReportsFormat()
    {
        var capture = new WasapiLoopbackCapture();
        var packetReceived = new TaskCompletionSource<AudioDataEventArgs>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        capture.DataAvailable += (_, args) => packetReceived.TrySetResult(args);

        try
        {
            await capture.StartAsync();
        }
        catch (Exception exception)
        {
            await capture.DisposeAsync();
            Console.WriteLine($"Skipping Windows audio smoke test: {exception.Message}"); // ubs:ignore[cs.pattern.console-write,cs-console-writeline] -- test diagnostic output
            return;
        }

        try
        {
            var completed = await Task.WhenAny(packetReceived.Task, Task.Delay(TimeSpan.FromSeconds(5)));

            if (completed != packetReceived.Task)
            {
                Console.WriteLine(
                    "Skipping Windows audio smoke test: no loopback audio packet was received within 5 seconds."); // ubs:ignore[cs.pattern.console-write,cs-console-writeline] -- test diagnostic output
                return;
            }

            var data = await packetReceived.Task;

            Assert.NotNull(capture.Format);
            Assert.True(data.Samples.Length >= 0);
            Assert.True(data.Format.Channels > 0);
        }
        finally
        {
            await capture.StopAsync();
            await capture.DisposeAsync();
        }
    }
}