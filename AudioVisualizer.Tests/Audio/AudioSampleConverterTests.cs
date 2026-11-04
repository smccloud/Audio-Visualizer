using System.Buffers.Binary;
using AudioVisualizer.Audio;

namespace AudioVisualizer.Tests.Audio;

public sealed class AudioSampleConverterTests
{
    [Fact]
    public void Convert_16BitPcm_ScalesAndSignExtends()
    {
        short[] values = [0, 16384, -16384, short.MaxValue, short.MinValue];
        var source = new byte[values.Length * sizeof(short)];

        for (var i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(
                source.AsSpan(i * sizeof(short), sizeof(short)),
                values[i]);
        }

        var destination = new float[values.Length];
        AudioSampleConverter.Convert(source, destination, PcmFormat(16));

        AssertClose(0f, destination[0]);
        AssertClose(0.5f, destination[1]);
        AssertClose(-0.5f, destination[2]);
        AssertClose(32767f / 32768f, destination[3]);
        AssertClose(-1f, destination[4]);
    }

    [Fact]
    public void Convert_24BitPcm_ScalesAndSignExtends()
    {
        int[] values = [0x010000, 0x7FFFFF, 0x800000, 0xFF0000];
        var expected = new[]
        {
            65536f / 8388608f,
            8388607f / 8388608f,
            -1f,
            -65536f / 8388608f,
        };
        var source = new byte[values.Length * 3];

        for (var i = 0; i < values.Length; i++)
        {
            WritePcm24(source.AsSpan(i * 3, 3), values[i]);
        }

        var destination = new float[values.Length];
        AudioSampleConverter.Convert(source, destination, PcmFormat(24));

        for (var i = 0; i < expected.Length; i++)
        {
            AssertClose(expected[i], destination[i]);
        }
    }

    [Fact]
    public void Convert_32BitPcm_ScalesAndSignExtends()
    {
        int[] values = [0, 1073741824, -1073741824, int.MaxValue, int.MinValue];
        var source = new byte[values.Length * sizeof(int)];

        for (var i = 0; i < values.Length; i++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                source.AsSpan(i * sizeof(int), sizeof(int)),
                values[i]);
        }

        var destination = new float[values.Length];
        AudioSampleConverter.Convert(source, destination, PcmFormat(32));

        AssertClose(0f, destination[0]);
        AssertClose(0.5f, destination[1]);
        AssertClose(-0.5f, destination[2]);
        AssertClose(1f, destination[3]);
        AssertClose(-1f, destination[4]);
    }

    [Fact]
    public void Convert_32BitFloat_PassesSamplesThrough()
    {
        float[] values = [0.1f, -0.5f, 1f, -1f];
        var source = new byte[values.Length * sizeof(float)];
        Buffer.BlockCopy(values, 0, source, 0, source.Length);

        var destination = new float[values.Length];
        AudioSampleConverter.Convert(
            source,
            destination,
            new AudioFormat(48000, 1, 32, SampleEncoding.IeeeFloat));

        Assert.Equal(values, destination);
    }

    [Fact]
    public void Convert_UnsupportedEncoding_Throws()
    {
        var format = new AudioFormat(48000, 1, 16, (SampleEncoding)99);

        Assert.Throws<NotSupportedException>(() =>
            AudioSampleConverter.Convert(new byte[2], new float[1], format));
    }

    [Fact]
    public void Convert_UnsupportedPcmSampleSize_Throws()
    {
        var format = PcmFormat(8);

        Assert.Throws<NotSupportedException>(() =>
            AudioSampleConverter.Convert(new byte[1], new float[1], format));
    }

    private static AudioFormat PcmFormat(int bitsPerSample) =>
        new(48000, 1, bitsPerSample, SampleEncoding.Pcm);

    private static void WritePcm24(Span<byte> destination, int value)
    {
        destination[0] = (byte)value;
        destination[1] = (byte)(value >> 8);
        destination[2] = (byte)(value >> 16);
    }

    private static void AssertClose(float expected, float actual) =>
        Assert.InRange(actual, expected - 1e-6f, expected + 1e-6f);
}
