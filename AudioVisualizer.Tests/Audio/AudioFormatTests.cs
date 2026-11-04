using AudioVisualizer.Audio;

namespace AudioVisualizer.Tests.Audio;

public sealed class AudioFormatTests
{
    [Fact]
    public void Properties_DescribeSampleAndFrameSize()
    {
        var format = new AudioFormat(48000, 2, 24, SampleEncoding.Pcm);

        Assert.Equal(3, format.BytesPerSample);
        Assert.Equal(6, format.BlockAlign);
    }

    [Fact]
    public void ToString_IncludesFormatDetails()
    {
        var format = new AudioFormat(44100, 2, 16, SampleEncoding.IeeeFloat);

        Assert.Equal("44100 Hz, 2 ch, 16-bit IeeeFloat", format.ToString());
    }
}
