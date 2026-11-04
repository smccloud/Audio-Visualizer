namespace AudioVisualizer.Audio.Analysis;

/// <summary>
/// In-place iterative radix-2 Cooley-Tukey fast Fourier transform.
/// </summary>
internal static class Fft
{
    /// <summary>
    /// Transforms <paramref name="real"/> and <paramref name="imaginary"/> in place.
    /// Their length must be a power of two.
    /// </summary>
    public static void Transform(Span<float> real, Span<float> imaginary)
    {
        var n = real.Length;

        if (imaginary.Length != n)
        {
            throw new ArgumentException("Real and imaginary buffers must be the same length.");
        }

        if (n == 0 || (n & (n - 1)) != 0)
        {
            throw new ArgumentException("FFT length must be a non-zero power of two.");
        }

        // Reorder samples in bit-reversed order.
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;

            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }

            j ^= bit;

            if (i < j)
            {
                (real[i], real[j]) = (real[j], real[i]);
                (imaginary[i], imaginary[j]) = (imaginary[j], imaginary[i]);
            }
        }

        for (var length = 2; length <= n; length <<= 1)
        {
            var angle = -2 * Math.PI / length;
            var stepReal = (float)Math.Cos(angle);
            var stepImaginary = (float)Math.Sin(angle);
            var half = length >> 1;

            for (var start = 0; start < n; start += length)
            {
                float twiddleReal = 1;
                float twiddleImaginary = 0;

                for (var k = 0; k < half; k++)
                {
                    var evenIndex = start + k;
                    var oddIndex = evenIndex + half;

                    var oddReal = real[oddIndex] * twiddleReal - imaginary[oddIndex] * twiddleImaginary;
                    var oddImaginary = real[oddIndex] * twiddleImaginary + imaginary[oddIndex] * twiddleReal;

                    real[oddIndex] = real[evenIndex] - oddReal;
                    imaginary[oddIndex] = imaginary[evenIndex] - oddImaginary;
                    real[evenIndex] += oddReal;
                    imaginary[evenIndex] += oddImaginary;

                    var nextTwiddleReal = twiddleReal * stepReal - twiddleImaginary * stepImaginary;
                    twiddleImaginary = twiddleReal * stepImaginary + twiddleImaginary * stepReal;
                    twiddleReal = nextTwiddleReal;
                }
            }
        }
    }
}
