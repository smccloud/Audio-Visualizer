using System.Runtime.InteropServices;

namespace ShaunsAudioVisualizer.Audio.Interop;

/// <summary>WAVE format tags used by WASAPI.</summary>
internal static class WaveFormatTags
{
    public const ushort Pcm = 0x0001;
    public const ushort IeeeFloat = 0x0003;
    public const ushort Extensible = 0xFFFE;
}

/// <summary>Native WAVEFORMATEX (mmreg.h).</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct WaveFormatEx
{
    public ushort FormatTag;
    public ushort Channels;
    public uint SamplesPerSecond;
    public uint AverageBytesPerSecond;
    public ushort BlockAlign;
    public ushort BitsPerSample;
    public ushort ExtraSize;
}

/// <summary>Native WAVEFORMATEXTENSIBLE (mmreg.h).</summary>
[StructLayout(LayoutKind.Sequential, Pack = 2)]
internal struct WaveFormatExtensible
{
    public WaveFormatEx Format;
    public ushort ValidBitsPerSample;
    public uint ChannelMask;
    public Guid SubFormat;
}

/// <summary>Reads a native WAVE format block into an <see cref="AudioFormat"/>.</summary>
internal static class WaveFormatReader
{
    // KSDATAFORMAT_SUBTYPE_IEEE_FLOAT
    private static readonly Guid SubtypeIeeeFloat =
        new("00000003-0000-0010-8000-00AA00389B71");

    // KSDATAFORMAT_SUBTYPE_PCM
    private static readonly Guid SubtypePcm =
        new("00000001-0000-0010-8000-00AA00389B71");

    public static AudioFormat Read(IntPtr formatPointer)
    {
        if (formatPointer == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(formatPointer));
        }

        var waveFormat = Marshal.PtrToStructure<WaveFormatEx>(formatPointer);

        if (waveFormat.Channels == 0)
        {
            throw new ArgumentException("Channel count must be greater than zero.", nameof(formatPointer));
        }

        if (waveFormat.SamplesPerSecond == 0)
        {
            throw new ArgumentException("Sample rate must be greater than zero.", nameof(formatPointer));
        }

        if (waveFormat.BitsPerSample == 0)
        {
            throw new ArgumentException("Bits per sample must be greater than zero.", nameof(formatPointer));
        }

        SampleEncoding encoding;

        if (waveFormat.FormatTag == WaveFormatTags.Pcm)
        {
            if (waveFormat.BitsPerSample is not (8 or 16 or 24 or 32))
            {
                throw new NotSupportedException(
                    $"Unsupported PCM bit depth: {waveFormat.BitsPerSample}.");
            }

            encoding = SampleEncoding.Pcm;
        }
        else if (waveFormat.FormatTag == WaveFormatTags.IeeeFloat)
        {
            if (waveFormat.BitsPerSample != 32)
            {
                throw new NotSupportedException(
                    $"Unsupported IEEE float bit depth: {waveFormat.BitsPerSample}.");
            }

            encoding = SampleEncoding.IeeeFloat;
        }
        else if (waveFormat.FormatTag == WaveFormatTags.Extensible)
        {
            if (waveFormat.ExtraSize < 22)
            {
                throw new ArgumentException(
                    "Extensible WAVE format is missing its extended fields.",
                    nameof(formatPointer));
            }

            var extensible = Marshal.PtrToStructure<WaveFormatExtensible>(formatPointer);

            if (extensible.SubFormat == SubtypeIeeeFloat)
            {
                if (waveFormat.BitsPerSample != 32)
                {
                    throw new NotSupportedException(
                        $"Unsupported IEEE float bit depth: {waveFormat.BitsPerSample}.");
                }

                encoding = SampleEncoding.IeeeFloat;
            }
            else if (extensible.SubFormat == SubtypePcm)
            {
                if (waveFormat.BitsPerSample is not (8 or 16 or 24 or 32))
                {
                    throw new NotSupportedException(
                        $"Unsupported PCM bit depth: {waveFormat.BitsPerSample}.");
                }

                encoding = SampleEncoding.Pcm;
            }
            else
            {
                throw new NotSupportedException(
                    $"Unsupported WAVE sub-format: {extensible.SubFormat}.");
            }
        }
        else
        {
            throw new NotSupportedException(
                $"Unsupported WAVE format tag: {waveFormat.FormatTag}.");
        }

        return new AudioFormat(
            checked((int)waveFormat.SamplesPerSecond), // ubs:ignore[cs.pattern.int-cast] -- checked cast rejects out-of-range sample rates
            waveFormat.Channels,
            waveFormat.BitsPerSample,
            encoding);
    }
}
