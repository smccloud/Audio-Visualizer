using AudioVisualizer.Audio;

namespace AudioVisualizer.Tests.Audio;

public sealed class AudioFormatTests
{
    [Theory]
    [InlineData(48000, 1, 16, SampleEncoding.Pcm, 2, 2)]
    [InlineData(44100, 2, 16, SampleEncoding.Pcm, 2, 4)]
    [InlineData(48000, 2, 24, SampleEncoding.Pcm, 3, 6)]
    [InlineData(96000, 6, 24, SampleEncoding.Pcm, 3, 18)]
    [InlineData(48000, 2, 32, SampleEncoding.IeeeFloat, 4, 8)]
    public void Properties_DescribeSampleAndFrameSize(
        int sampleRate,
        int channels,
        int bitsPerSample,
        SampleEncoding encoding,
        int expectedBytesPerSample,
        int expectedBlockAlign)
    {
        var format = new AudioFormat(sampleRate, channels, bitsPerSample, encoding);

        Assert.Equal(expectedBytesPerSample, format.BytesPerSample);
        Assert.Equal(expectedBlockAlign, format.BlockAlign);
    }

    [Fact]
    public void ToString_IncludesFormatDetails()
    {
        var format = new AudioFormat(44100, 2, 16, SampleEncoding.IeeeFloat);

        Assert.Equal("44100 Hz, 2 ch, 16-bit IeeeFloat", format.ToString());
    }
}
