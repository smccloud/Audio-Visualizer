using ShaunsAudioVisualizer.Audio;

using System.Threading.Tasks;

using Xunit;

namespace ShaunsAudioVisualizer.Windows.Tests.Audio;

public sealed class LinuxAudioCaptureTests
{
    [Fact]
    public async Task StartAsync_ThrowsPlatformNotSupportedException_OnWindows()
    {
        await using var capture = new LinuxAudioCapture();
        await Assert.ThrowsAsync<PlatformNotSupportedException>(() => capture.StartAsync());
    }
}