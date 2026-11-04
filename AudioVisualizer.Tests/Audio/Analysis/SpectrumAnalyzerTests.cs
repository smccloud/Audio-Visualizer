using AudioVisualizer.Audio.Analysis;

namespace AudioVisualizer.Tests.Audio.Analysis;

public sealed class SpectrumAnalyzerTests
{
    private const int FftSize = 2048;
    private const int BandCount = 24;
    private const double MinFrequency = 30;
    private const double MaxFrequency = 18000;

    [Fact]
    public void Constructor_RejectsInvalidArguments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectrumAnalyzer(fftSize: 1));
        Assert.Throws<ArgumentException>(() => new SpectrumAnalyzer(fftSize: 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectrumAnalyzer(bandCount: 0));
        Assert.Throws<ArgumentException>(() =>
            new SpectrumAnalyzer(minFrequency: 100, maxFrequency: 100));
    }

    [Fact]
    public void BandCount_ReturnsConfiguredBandCount()
    {
        var analyzer = new SpectrumAnalyzer(FftSize, BandCount);

        Assert.Equal(BandCount, analyzer.BandCount);
    }

    [Fact]
    public void Analyze_ReturnsOneValuePerBandWithinRange()
    {
        var analyzer = new SpectrumAnalyzer(FftSize, BandCount);
        analyzer.Push(CreateTone(48000, 1000, FftSize), channels: 1);

        var bands = analyzer.Analyze(48000);

        Assert.Equal(BandCount, bands.Length);
        Assert.All(bands, band => Assert.InRange(band, 0f, 1f));
    }

    [Fact]
    public void Analyze_Silence_ReturnsZeroBands()
    {
        var analyzer = new SpectrumAnalyzer(FftSize, BandCount);
        analyzer.Push(new float[FftSize], channels: 1);

        var bands = analyzer.Analyze(48000);

        Assert.All(bands, band => Assert.Equal(0f, band, precision: 6));
    }

    [Fact]
    public void Analyze_Tone_RaisesBandContainingFrequency()
    {
        const int sampleRate = 48000;
        const double frequency = 1000;
        var analyzer = new SpectrumAnalyzer(FftSize, BandCount, MinFrequency, MaxFrequency);
        analyzer.Push(CreateTone(sampleRate, frequency, FftSize), channels: 1);

        var bands = analyzer.Analyze(sampleRate);
        var expectedBand = ExpectedBand(frequency, sampleRate);
        var strongestBand = Array.IndexOf(bands, bands.Max());

        Assert.Equal(expectedBand, strongestBand);
        Assert.True(bands[expectedBand] > 0.5f);
    }

    [Fact]
    public void Analyze_WindowCanSpanMultiplePushes()
    {
        const int sampleRate = 48000;
        const double frequency = 1000;
        var tone = CreateTone(sampleRate, frequency, FftSize);

        var singlePush = new SpectrumAnalyzer(FftSize, BandCount);
        singlePush.Push(tone, channels: 1);
        var expected = singlePush.Analyze(sampleRate);

        var multiplePushes = new SpectrumAnalyzer(FftSize, BandCount);
        multiplePushes.Push(tone.AsSpan(0, 700), channels: 1);
        multiplePushes.Push(tone.AsSpan(700, 900), channels: 1);
        multiplePushes.Push(tone.AsSpan(1600), channels: 1);
        var actual = multiplePushes.Analyze(sampleRate);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Analyze_UpdatesBandMappingWhenSampleRateChanges()
    {
        const double frequency = 1000;
        var analyzer = new SpectrumAnalyzer(FftSize, BandCount, MinFrequency, MaxFrequency);

        analyzer.Push(CreateTone(48000, frequency, FftSize), channels: 1);
        var bandsAt48k = analyzer.Analyze(48000);
        var expectedAt48k = ExpectedBand(frequency, 48000);

        analyzer.Push(CreateTone(8000, frequency, FftSize), channels: 1);
        var bandsAt8k = analyzer.Analyze(8000);
        var expectedAt8k = ExpectedBand(frequency, 8000);

        Assert.Equal(expectedAt48k, Array.IndexOf(bandsAt48k, bandsAt48k.Max()));
        Assert.Equal(expectedAt8k, Array.IndexOf(bandsAt8k, bandsAt8k.Max()));
        Assert.NotEqual(expectedAt48k, expectedAt8k);
    }

    [Fact]
    public void Push_RejectsInvalidChannelCount()
    {
        var analyzer = new SpectrumAnalyzer();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            analyzer.Push(new float[8], channels: 0));
    }

    [Fact]
    public void Analyze_RejectsInvalidSampleRate()
    {
        var analyzer = new SpectrumAnalyzer();

        Assert.Throws<ArgumentOutOfRangeException>(() => analyzer.Analyze(0));
    }

    private static float[] CreateTone(
        int sampleRate,
        double frequency,
        int frameCount,
        int channels = 1)
    {
        var samples = new float[frameCount * channels];

        for (var frame = 0; frame < frameCount; frame++)
        {
            var value = (float)Math.Sin(2 * Math.PI * frequency * frame / sampleRate);

            for (var channel = 0; channel < channels; channel++)
            {
                samples[(frame * channels) + channel] = value;
            }
        }

        return samples;
    }

    private static int ExpectedBand(double frequency, int sampleRate)
    {
        var nyquist = sampleRate / 2.0;
        var maxFrequency = Math.Min(MaxFrequency, nyquist);
        var minFrequency = Math.Min(MinFrequency, maxFrequency);
        var logMin = Math.Log(minFrequency);
        var logMax = Math.Log(maxFrequency);

        for (var band = 0; band < BandCount; band++)
        {
            var lowFrequency = Math.Exp(logMin + ((logMax - logMin) * band / BandCount));
            var highFrequency = Math.Exp(logMin + ((logMax - logMin) * (band + 1) / BandCount));

            if (frequency >= lowFrequency && frequency < highFrequency)
            {
                return band;
            }
        }

        throw new ArgumentOutOfRangeException(
            nameof(frequency),
            frequency,
            "Frequency is outside the analyzer's band range.");
    }
}
