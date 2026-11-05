namespace AudioVisualizer.Audio.Analysis;

/// <summary>
/// Turns a stream of interleaved audio packets into a small, log-spaced
/// spectrum suitable for driving a visualization.
/// </summary>
/// <remarks>
/// Samples are accumulated into a ring buffer so that each analysis window
/// spans multiple capture packets. Results are normalized to the range [0, 1].
/// Instances are not thread-safe; use them from the capture thread only.
/// </remarks>
public sealed class SpectrumAnalyzer
{
    private readonly int _fftSize;
    private readonly int _bandCount;
    private readonly float[] _window;
    private readonly float[] _ring;
    private readonly float[] _real;
    private readonly float[] _imaginary;
    private readonly int[] _bandStart;
    private readonly int[] _bandEnd;
    private readonly double _minFrequency;
    private readonly double _maxFrequency;
    private int _writeIndex;
    private int _sampleCount;
    private int _bandSampleRate;

    public SpectrumAnalyzer(
        int fftSize = 2048,
        int bandCount = 24,
        double minFrequency = 30,
        double maxFrequency = 18000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(fftSize, 2);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bandCount);

        if ((fftSize & (fftSize - 1)) != 0)
        {
            throw new ArgumentException("FFT size must be a power of two.", nameof(fftSize));
        }

        if (maxFrequency <= minFrequency)
        {
            throw new ArgumentException("Maximum frequency must exceed the minimum.");
        }

        _fftSize = fftSize;
        _bandCount = bandCount;
        _minFrequency = minFrequency;
        _maxFrequency = maxFrequency;
        _window = new float[fftSize];
        _ring = new float[fftSize];
        _real = new float[fftSize];
        _imaginary = new float[fftSize];
        _bandStart = new int[bandCount];
        _bandEnd = new int[bandCount];

        for (var i = 0; i < fftSize; i++)
        {
            _window[i] = (float)(0.5 * (1 - Math.Cos(2 * Math.PI * i / (fftSize - 1))));
        }
    }

    /// <summary>Number of spectrum bands produced by <see cref="Analyze"/>.</summary>
    public int BandCount => _bandCount;

    /// <summary>Adds one packet of interleaved samples to the analysis window.</summary>
    public void Push(ReadOnlySpan<float> interleavedSamples, int channels)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(channels);

        var frames = interleavedSamples.Length / channels;

        for (var frame = 0; frame < frames; frame++)
        {
            var offset = frame * channels;
            var mono = 0f;
            var rightChannelMono = 0f;

            for (var channel = 0; channel < channels; channel++)
            {
                if (channel == 0) { mono += interleavedSamples[offset + channel]; } else { rightChannelMono += interleavedSamples[offset + channel]; }
            }

            _ring[_writeIndex] = mono / 2;
            _ring[_writeIndex + _fftSize / 2] = rightChannelMono / 2;
            _writeIndex = (_writeIndex + 1) % _fftSize;

            if (_sampleCount < _fftSize)
            {
                _sampleCount++;
            }
        }
    }

    /// <summary>
    /// Computes the current spectrum as <see cref="BandCount"/> values in [0, 1].
    /// </summary>
    public (float[] leftBands, float[] rightBands) Analyze(int sampleRate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sampleRate);
        UpdateBands(sampleRate);

        var start = (_writeIndex - _sampleCount + _fftSize) % _fftSize;

        for (var i = 0; i < _fftSize; i++)
        {
            _real[i] = i < _sampleCount
                ? _ring[(start + i) % _fftSize] * _window[i]
                : 0f;
            _imaginary[i] = 0f;
        }

        Fft.Transform(_real, _imaginary);
        Fft.Transform(_real.Skip(_fftSize / 2).ToArray(), _imaginary.Skip(_fftSize / 2).ToArray());

        var leftBands = new float[_bandCount];
        var rightBands = new float[_bandCount];
        var normalization = 2f / _fftSize;

        for (var band = 0; band < _bandCount; band++)
        {
            var sum = 0.0;
            var bins = 0;

            for (var bin = _bandStart[band]; bin < _bandEnd[band]; bin++)
            {
                var real = _real[bin];
                var imaginary = _imaginary[bin];
                var magnitude = Math.Sqrt(real * real + imaginary * imaginary) * normalization;
                sum += magnitude * magnitude;
                bins++;
            }

            var rms = bins > 0 ? Math.Sqrt(sum / bins) : 0;
            var decibels = 20 * Math.Log10(Math.Max(rms, 1e-7));

            // Map -70 dBFS..0 dBFS onto 0..1.
            leftBands[band] = (float)Math.Clamp((decibels + 70) / 70, 0, 1);

            sum = 0;
            bins = 0;

            for (var bin = _bandStart[band] + _fftSize / 2; bin < _bandEnd[band] + _fftSize / 2; bin++)
            {
                var real = _real[bin % _fftSize];
                var imaginary = _imaginary[bin % _fftSize];
                var magnitude = Math.Sqrt(real * real + imaginary * imaginary) * normalization;
                sum += magnitude * magnitude;
                bins++;
            }

            rms = bins > 0 ? Math.Sqrt(sum / bins) : 0;
            var decibelsRight = 20 * Math.Log10(Math.Max(rms, 1e-7));

            // Map -70 dBFS..0 dBFS onto 0..1.
            rightBands[band] = (float)Math.Clamp((decibelsRight + 70) / 70, 0, 1);
        }

        return (leftBands, rightBands);
    }

    private void UpdateBands(int sampleRate)
    {
        if (_bandSampleRate == sampleRate)
        {
            return;
        }

        _bandSampleRate = sampleRate;

        var nyquist = sampleRate / 2.0;
        var maxFrequency = Math.Min(_maxFrequency, nyquist);
        var minFrequency = Math.Min(_minFrequency, maxFrequency);
        var logMin = Math.Log(minFrequency);
        var logMax = Math.Log(maxFrequency);
        var maxBin = _fftSize / 2 - 1;

        for (var band = 0; band < _bandCount; band++)
        {
            var lowFrequency = Math.Exp(logMin + (logMax - logMin) * band / _bandCount);
            var highFrequency = Math.Exp(logMin + (logMax - logMin) * (band + 1) / _bandCount);

            var start = (int)Math.Round(lowFrequency * _fftSize / sampleRate);
            var end = (int)Math.Round(highFrequency * _fftSize / sampleRate);

            _bandStart[band] = Math.Clamp(start, 1, maxBin);
            _bandEnd[band] = Math.Clamp(Math.Max(end, _bandStart[band] + 1), 2, maxBin + 1);
        }
    }
}