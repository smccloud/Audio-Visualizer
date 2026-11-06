using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace ShaunsAudioVisualizer.Audio;

/// <summary>
/// Converts interleaved PCM from a native WASAPI buffer into normalized
/// 32-bit floating point samples in the range [-1, 1].
/// </summary>
internal static class AudioSampleConverter
{
    public static void Convert(
        ReadOnlySpan<byte> source,
        Span<float> destination,
        AudioFormat format)
    {
        if (format.Encoding == SampleEncoding.IeeeFloat && format.BitsPerSample == 32)
        {
            var floats = MemoryMarshal.Cast<byte, float>(source);
            floats.CopyTo(destination);
            return;
        }

        if (format.Encoding != SampleEncoding.Pcm)
        {
            throw new NotSupportedException(
                $"Unsupported sample encoding {format.Encoding} with {format.BitsPerSample} bits.");
        }

        var bytesPerSample = format.BytesPerSample;
        var sampleCount = destination.Length;

        for (var i = 0; i < sampleCount; i++)
        {
            var sample = source.Slice(i * bytesPerSample, bytesPerSample);
            destination[i] = bytesPerSample switch
            {
                2 => BinaryPrimitives.ReadInt16LittleEndian(sample) / 32768f,
                3 => ReadPcm24(sample) / 8388608f,
                4 => BinaryPrimitives.ReadInt32LittleEndian(sample) / 2147483648f,
                _ => throw new NotSupportedException(
                    $"Unsupported PCM sample size: {format.BitsPerSample} bits."),
            };
        }
    }

    private static int ReadPcm24(ReadOnlySpan<byte> sample)
    {
        var value = sample[0] | (sample[1] << 8) | (sample[2] << 16);

        // Sign-extend the 24-bit value to 32 bits.
        if ((value & 0x0080_0000) != 0)
        {
            value |= unchecked((int)0xFF00_0000);
        }

        return value;
    }
}
