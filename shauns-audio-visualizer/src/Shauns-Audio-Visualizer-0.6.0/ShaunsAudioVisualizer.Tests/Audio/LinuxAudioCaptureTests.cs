using ShaunsAudioVisualizer.Audio;

namespace ShaunsAudioVisualizer.Tests.Audio;

public sealed class LinuxAudioCaptureTests
{
    [Fact]
    public void FindOnPath_ReturnsNull_ForMissingTool()
    {
        Assert.Null(LinuxAudioCapture.FindOnPath("definitely-not-a-real-tool-xyz"));
    }

    [Fact]
    public void ResolveExecutable_ReturnsNull_WhenNoToolsOnPath()
    {
        var original = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", string.Empty);
            Assert.Null(LinuxAudioCapture.ResolveExecutable());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", original);
        }
    }

    [Fact]
    public void DetectBackend_ReturnsNone_WhenNoToolsOnPath()
    {
        var original = Environment.GetEnvironmentVariable("PATH");
        try
        {
            Environment.SetEnvironmentVariable("PATH", string.Empty);
            Assert.Equal(LinuxAudioCapture.LinuxAudioBackend.None, LinuxAudioCapture.DetectBackend());
            Assert.False(LinuxAudioCapture.IsPipeWireInstalled());
            Assert.False(LinuxAudioCapture.IsPulseAudioInstalled());
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", original);
        }
    }
}
