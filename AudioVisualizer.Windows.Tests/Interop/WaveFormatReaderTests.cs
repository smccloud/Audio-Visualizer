using System.Runtime.InteropServices;
using AudioVisualizer.Audio;
using AudioVisualizer.Audio.Interop;

namespace AudioVisualizer.Windows.Tests.Interop;

public sealed class WaveFormatReaderTests
{
    private static readonly Guid SubtypePcm =
        new("00000001-0000-0010-8000-00AA00389B71");

    private static readonly Guid SubtypeIeeeFloat =
        new("00000003-0000-0010-8000-00AA00389B71");

    [Fact]
    public void Read_PcmWaveFormat_ReturnsPcmAudioFormat()
    {
        var format = new WaveFormatEx
        {
            FormatTag = WaveFormatTags.Pcm,
            Channels = 2,
            SamplesPerSecond = 48000,
            AverageBytesPerSecond = 48000 * 2 * 2,
            BlockAlign = 4,
            BitsPerSample = 16,
            ExtraSize = 0,
        };

        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatEx>());

        try
        {
            Marshal.StructureToPtr(format, pointer, false);

            var audioFormat = WaveFormatReader.Read(pointer);

            Assert.Equal(48000, audioFormat.SampleRate);
            Assert.Equal(2, audioFormat.Channels);
            Assert.Equal(16, audioFormat.BitsPerSample);
            Assert.Equal(SampleEncoding.Pcm, audioFormat.Encoding);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Fact]
    public void Read_IeeeFloatWaveFormat_ReturnsFloatAudioFormat()
    {
        var format = new WaveFormatEx
        {
            FormatTag = WaveFormatTags.IeeeFloat,
            Channels = 1,
            SamplesPerSecond = 44100,
            AverageBytesPerSecond = 44100 * 4,
            BlockAlign = 4,
            BitsPerSample = 32,
            ExtraSize = 0,
        };

        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatEx>());

        try
        {
            Marshal.StructureToPtr(format, pointer, false);

            var audioFormat = WaveFormatReader.Read(pointer);

            Assert.Equal(44100, audioFormat.SampleRate);
            Assert.Equal(1, audioFormat.Channels);
            Assert.Equal(32, audioFormat.BitsPerSample);
            Assert.Equal(SampleEncoding.IeeeFloat, audioFormat.Encoding);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Fact]
    public void Read_ExtensiblePcmWaveFormat_ReturnsPcmAudioFormat()
    {
        var format = new WaveFormatExtensible
        {
            Format = new WaveFormatEx
            {
                FormatTag = WaveFormatTags.Extensible,
                Channels = 6,
                SamplesPerSecond = 48000,
                AverageBytesPerSecond = 48000 * 6 * 2,
                BlockAlign = 12,
                BitsPerSample = 16,
                ExtraSize = 22,
            },
            ValidBitsPerSample = 16,
            ChannelMask = 0x3F,
            SubFormat = SubtypePcm,
        };

        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatExtensible>());

        try
        {
            Marshal.StructureToPtr(format, pointer, false);

            var audioFormat = WaveFormatReader.Read(pointer);

            Assert.Equal(48000, audioFormat.SampleRate);
            Assert.Equal(6, audioFormat.Channels);
            Assert.Equal(16, audioFormat.BitsPerSample);
            Assert.Equal(SampleEncoding.Pcm, audioFormat.Encoding);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Fact]
    public void Read_ExtensibleFloatWaveFormat_ReturnsFloatAudioFormat()
    {
        var format = new WaveFormatExtensible
        {
            Format = new WaveFormatEx
            {
                FormatTag = WaveFormatTags.Extensible,
                Channels = 2,
                SamplesPerSecond = 96000,
                AverageBytesPerSecond = 96000 * 2 * 4,
                BlockAlign = 8,
                BitsPerSample = 32,
                ExtraSize = 22,
            },
            ValidBitsPerSample = 32,
            ChannelMask = 0x3,
            SubFormat = SubtypeIeeeFloat,
        };

        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatExtensible>());

        try
        {
            Marshal.StructureToPtr(format, pointer, false);

            var audioFormat = WaveFormatReader.Read(pointer);

            Assert.Equal(96000, audioFormat.SampleRate);
            Assert.Equal(2, audioFormat.Channels);
            Assert.Equal(32, audioFormat.BitsPerSample);
            Assert.Equal(SampleEncoding.IeeeFloat, audioFormat.Encoding);
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Fact]
    public void Read_NullPointer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => WaveFormatReader.Read(IntPtr.Zero));
    }

    [Theory]
    [InlineData(0)]
    public void Read_ZeroChannels_Throws(int _)
    {
        var format = new WaveFormatEx
        {
            FormatTag = WaveFormatTags.Pcm,
            Channels = 0,
            SamplesPerSecond = 48000,
            BitsPerSample = 16,
            ExtraSize = 0,
        };

        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatEx>());

        try
        {
            Marshal.StructureToPtr(format, pointer, false);

            Assert.Throws<ArgumentException>(() => WaveFormatReader.Read(pointer));
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Fact]
    public void Read_UnsupportedFormatTag_Throws()
    {
        var format = new WaveFormatEx
        {
            FormatTag = 0x0002, // ADPCM
            Channels = 1,
            SamplesPerSecond = 48000,
            BitsPerSample = 16,
            ExtraSize = 0,
        };

        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatEx>());

        try
        {
            Marshal.StructureToPtr(format, pointer, false);

            Assert.Throws<NotSupportedException>(() => WaveFormatReader.Read(pointer));
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Fact]
    public void Read_ExtensibleWithMissingFields_Throws()
    {
        var format = new WaveFormatEx
        {
            FormatTag = WaveFormatTags.Extensible,
            Channels = 2,
            SamplesPerSecond = 48000,
            BitsPerSample = 16,
            ExtraSize = 0,
        };

        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatEx>());

        try
        {
            Marshal.StructureToPtr(format, pointer, false);

            Assert.Throws<ArgumentException>(() => WaveFormatReader.Read(pointer));
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }

    [Fact]
    public void Read_ExtensibleWithUnsupportedSubFormat_Throws()
    {
        var format = new WaveFormatExtensible
        {
            Format = new WaveFormatEx
            {
                FormatTag = WaveFormatTags.Extensible,
                Channels = 2,
                SamplesPerSecond = 48000,
                BitsPerSample = 16,
                ExtraSize = 22,
            },
            ValidBitsPerSample = 16,
            ChannelMask = 0x3,
            SubFormat = new Guid("12345678-1234-1234-1234-123456789ABC"),
        };

        var pointer = Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatExtensible>());

        try
        {
            Marshal.StructureToPtr(format, pointer, false);

            Assert.Throws<NotSupportedException>(() => WaveFormatReader.Read(pointer));
        }
        finally
        {
            Marshal.FreeHGlobal(pointer);
        }
    }
}