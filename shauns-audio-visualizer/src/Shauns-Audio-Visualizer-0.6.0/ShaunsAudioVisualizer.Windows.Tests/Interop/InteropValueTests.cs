using System.Runtime.InteropServices;
using ShaunsAudioVisualizer.Audio.Interop;

namespace ShaunsAudioVisualizer.Windows.Tests.Interop;

public sealed class InteropValueTests
{
    [Fact]
    public void HResult_ThrowIfFailed_SucceedsForSuccessCode()
    {
        HResult.ThrowIfFailed(0, "operation");
    }

    [Fact]
    public void HResult_ThrowIfFailed_ThrowsWithOperationNameOnFailure()
    {
        var exception = Assert.Throws<COMException>(() =>
            HResult.ThrowIfFailed(unchecked((int)0x80004005), "activation"));

        Assert.Contains("activation", exception.Message, StringComparison.Ordinal);
        Assert.Equal(unchecked((int)0x80004005), exception.HResult);
    }

    [Fact]
    public void WasapiGuids_MatchDocumentedValues()
    {
        Assert.Equal(new Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"), WasapiGuids.IAudioClient);
        Assert.Equal(new Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), WasapiGuids.IAudioCaptureClient);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public void EDataFlow_ValuesMatchWindowsDefinitions(int rawValue, int expected)
    {
        Assert.Equal(expected, rawValue);
        Assert.Equal((EDataFlow)expected, (EDataFlow)rawValue);
    }

    [Fact]
    public void AudioClientStreamFlags_LoopbackValue() =>
        Assert.Equal(0x00020000u, (uint)AudioClientStreamFlags.Loopback);

    [Fact]
    public void AudioClientStreamFlags_CrossProcessValue() =>
        Assert.Equal(0x00010000u, (uint)AudioClientStreamFlags.CrossProcess);

    [Fact]
    public void AudioClientStreamFlags_EventCallbackValue() =>
        Assert.Equal(0x00040000u, (uint)AudioClientStreamFlags.EventCallback);

    [Fact]
    public void ClsCtx_AllCombinesExpectedFlags()
    {
        Assert.Equal(0x17u, (uint)ClsCtx.All);
    }
}