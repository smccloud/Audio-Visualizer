using System.Runtime.InteropServices;

namespace AudioVisualizer.Audio.Interop;

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

    public static AudioFormat Read(IntPtr formatPointer)
    {
        var waveFormat = Marshal.PtrToStructure<WaveFormatEx>(formatPointer);
        var encoding = waveFormat.FormatTag == WaveFormatTags.IeeeFloat
            ? SampleEncoding.IeeeFloat
            : SampleEncoding.Pcm;

        if (waveFormat.FormatTag == WaveFormatTags.Extensible &&
            waveFormat.ExtraSize >= 22)
        {
            var extensible = Marshal.PtrToStructure<WaveFormatExtensible>(formatPointer);
            encoding = extensible.SubFormat == SubtypeIeeeFloat
                ? SampleEncoding.IeeeFloat
                : SampleEncoding.Pcm;
        }

        return new AudioFormat(
            (int)waveFormat.SamplesPerSecond,
            waveFormat.Channels,
            waveFormat.BitsPerSample,
            encoding);
    }
}
