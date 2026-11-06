using AudioVisualizer.Audio.Analysis;

namespace AudioVisualizer.Tests.Audio.Analysis;

public sealed class FftTests
{
    [Fact]
    public void Transform_DcInput_PutsEnergyInFirstBin()
    {
        const int size = 16;
        var real = new float[size];
        var imaginary = new float[size];
        Array.Fill(real, 1f);

        Fft.Transform(real, imaginary);

        Assert.Equal(size, real[0], precision: 4);

        for (var bin = 1; bin < size; bin++)
        {
            Assert.Equal(0f, real[bin], precision: 4);
            Assert.Equal(0f, imaginary[bin], precision: 4);
        }
    }

    [Fact]
    public void Transform_SingleBinSine_PeaksAtExpectedBin()
    {
        const int size = 64;
        const int expectedBin = 7;
        var real = new float[size];
        var imaginary = new float[size];

        for (var i = 0; i < size; i++)
        {
            real[i] = MathF.Cos(2 * MathF.PI * expectedBin * i / size);
        }

        Fft.Transform(real, imaginary);

        var magnitude = MathF.Sqrt(
            (real[expectedBin] * real[expectedBin]) +
            (imaginary[expectedBin] * imaginary[expectedBin])) / size;

        Assert.InRange(magnitude, 0.49f, 0.51f);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void Transform_MatchesNaiveDft(int size)
    {
        var real = new float[size];
        var imaginary = new float[size];
        var random = new Random(size); // ubs:ignore[cs-weak-random] -- fixed seed keeps this numeric test deterministic; no security use

        for (var i = 0; i < size; i++)
        {
            real[i] = (float)((random.NextDouble() * 2) - 1);
        }

        var expectedReal = new double[size];
        var expectedImaginary = new double[size];

        for (var bin = 0; bin < size; bin++)
        {
            for (var sample = 0; sample < size; sample++)
            {
                var angle = -2 * Math.PI * bin * sample / size;
                expectedReal[bin] += real[sample] * Math.Cos(angle);
                expectedImaginary[bin] += real[sample] * Math.Sin(angle);
            }
        }

        Fft.Transform(real, imaginary);

        for (var bin = 0; bin < size; bin++)
        {
            Assert.Equal(expectedReal[bin], real[bin], precision: 4);
            Assert.Equal(expectedImaginary[bin], imaginary[bin], precision: 4);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void Transform_WithComplexInput_MatchesNaiveDft(int size)
    {
        var real = new float[size];
        var imaginary = new float[size];
        var random = new Random((size * 31) + 7); // ubs:ignore[cs-weak-random] -- fixed seed keeps this numeric test deterministic; no security use

        for (var i = 0; i < size; i++)
        {
            real[i] = (float)((random.NextDouble() * 2) - 1);
            imaginary[i] = (float)((random.NextDouble() * 2) - 1);
        }

        var inputReal = (float[])real.Clone();
        var inputImaginary = (float[])imaginary.Clone();
        var expectedReal = new double[size];
        var expectedImaginary = new double[size];

        for (var bin = 0; bin < size; bin++)
        {
            for (var sample = 0; sample < size; sample++)
            {
                var angle = -2 * Math.PI * bin * sample / size;
                var cosine = Math.Cos(angle);
                var sine = Math.Sin(angle);

                expectedReal[bin] +=
                    (inputReal[sample] * cosine) - (inputImaginary[sample] * sine);
                expectedImaginary[bin] +=
                    (inputReal[sample] * sine) + (inputImaginary[sample] * cosine);
            }
        }

        Fft.Transform(real, imaginary);

        for (var bin = 0; bin < size; bin++)
        {
            Assert.Equal(expectedReal[bin], real[bin], precision: 4);
            Assert.Equal(expectedImaginary[bin], imaginary[bin], precision: 4);
        }
    }

    [Fact]
    public void Transform_RejectsMismatchedBufferLengths()
    {
        Assert.Throws<ArgumentException>(() =>
            Fft.Transform(new float[4], new float[8]));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(6)]
    public void Transform_RejectsLengthsThatAreNotPowerOfTwo(int size)
    {
        Assert.Throws<ArgumentException>(() =>
            Fft.Transform(new float[size], new float[size]));
    }
}
