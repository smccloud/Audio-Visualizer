using AudioVisualizer.Audio.Analysis;

namespace AudioVisualizer.Tests.Audio.Analysis;

public sealed class AudioLevelAnalyzerTests
{
    private readonly AudioLevelAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_Silence_ReturnsZeroLevels()
    {
        var snapshot = _analyzer.Analyze(new float[8], channels: 2);

        Assert.Equal(4, snapshot.FrameCount);
        Assert.Equal(0f, snapshot.Peak);
        Assert.Equal(0f, snapshot.Rms);
        Assert.All(snapshot.ChannelPeaks, peak => Assert.Equal(0f, peak));
        Assert.All(snapshot.ChannelRms, rms => Assert.Equal(0f, rms));
        Assert.Equal(-140, snapshot.PeakDb, precision: 4);
        Assert.Equal(-140, snapshot.RmsDb, precision: 4);
    }

    [Fact]
    public void Analyze_FullScaleSamples_ReturnsUnityLevels()
    {
        var snapshot = _analyzer.Analyze(new[] { 1f, -1f, 1f, -1f }, channels: 1);

        Assert.Equal(4, snapshot.FrameCount);
        Assert.Equal(1f, snapshot.Peak);
        Assert.Equal(1f, snapshot.Rms);
        Assert.Equal(0, snapshot.PeakDb, precision: 4);
        Assert.Equal(0, snapshot.RmsDb, precision: 4);
    }

    [Fact]
    public void Analyze_KnownAmplitude_ReturnsExpectedDecibels()
    {
        var snapshot = _analyzer.Analyze(new[] { 0.5f, -0.5f, 0.5f, -0.5f }, channels: 1);

        Assert.Equal(0.5f, snapshot.Peak);
        Assert.Equal(0.5f, snapshot.Rms);
        Assert.Equal(-6.0206, snapshot.PeakDb, precision: 4);
        Assert.Equal(-6.0206, snapshot.RmsDb, precision: 4);
    }

    [Fact]
    public void Analyze_FullScaleSine_ReturnsExpectedRms()
    {
        var snapshot = _analyzer.Analyze(new[] { 0f, 1f, 0f, -1f }, channels: 1);

        Assert.Equal(1f, snapshot.Peak);
        Assert.Equal(MathF.Sqrt(0.5f), snapshot.Rms, precision: 4);
        Assert.Equal(-3.0103, snapshot.RmsDb, precision: 4);
    }

    [Fact]
    public void Analyze_EmptyInput_ReturnsZeroFramesAndLevels()
    {
        var snapshot = _analyzer.Analyze(Array.Empty<float>(), channels: 2);

        Assert.Equal(0, snapshot.FrameCount);
        Assert.Equal(0f, snapshot.Peak);
        Assert.Equal(0f, snapshot.Rms);
        Assert.All(snapshot.ChannelPeaks, peak => Assert.Equal(0f, peak));
        Assert.All(snapshot.ChannelRms, rms => Assert.Equal(0f, rms));
        Assert.Equal(-140, snapshot.PeakDb, precision: 4);
        Assert.Equal(-140, snapshot.RmsDb, precision: 4);
    }

    [Fact]
    public void Analyze_Stereo_CalculatesChannelsIndependently()
    {
        var snapshot = _analyzer.Analyze(
            new[] { 0.5f, 0.1f, -0.25f, 0.1f },
            channels: 2);

        Assert.Equal(2, snapshot.FrameCount);
        Assert.Equal(0.5f, snapshot.Peak);

        Assert.Equal(0.5f, snapshot.ChannelPeaks[0]);
        Assert.Equal(0.1f, snapshot.ChannelPeaks[1]);

        Assert.Equal(0.3953f, snapshot.ChannelRms[0], precision: 4);
        Assert.Equal(0.1f, snapshot.ChannelRms[1], precision: 4);

        Assert.Equal(0.2883f, snapshot.Rms, precision: 4);
    }

    [Fact]
    public void Analyze_IgnoresIncompleteFinalFrame()
    {
        // Five samples with two channels hold two complete frames. The trailing
        // sample is not part of a frame and must not affect peak or RMS.
        var snapshot = _analyzer.Analyze(
            new[] { 0.5f, 0.1f, -0.25f, 0.1f, 1f },
            channels: 2);

        Assert.Equal(2, snapshot.FrameCount);
        Assert.Equal(0.5f, snapshot.Peak);
        Assert.Equal(0.5f, snapshot.ChannelPeaks[0]);
        Assert.Equal(0.1f, snapshot.ChannelPeaks[1]);
    }

    [Fact]
    public void Analyze_TinySamples_ClampsDecibelsToFloor()
    {
        var snapshot = _analyzer.Analyze(new[] { 1e-8f, -1e-8f }, channels: 1);

        Assert.Equal(-140, snapshot.PeakDb, precision: 4);
        Assert.Equal(-140, snapshot.RmsDb, precision: 4);
    }

    [Fact]
    public void Analyze_RejectsInvalidChannelCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _analyzer.Analyze(new float[4], channels: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _analyzer.Analyze(new float[4], channels: -1));
    }
}
