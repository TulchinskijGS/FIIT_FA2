using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

public class FftMultiplier : IMultiplier
{
    private const int LimbBits = 8;
    private const int LimbBase = 1 << LimbBits;
    private const int LimbMask = LimbBase - 1;
    private const int LimbsPerUInt = 4;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        uint[] product = MultiplyMagnitudeFft(a.GetDigits(), b.GetDigits());
        return BetterBigInteger.FromMagnitude(
            product,
            a.IsNegative != b.IsNegative);
    }

    private static uint[] MultiplyMagnitudeFft(
        ReadOnlySpan<uint> a,
        ReadOnlySpan<uint> b)
    {
        if (a.Length == 0 || b.Length == 0)
            return [0];
        int aLength = checked(a.Length * LimbsPerUInt);
        int bLength = checked(b.Length * LimbsPerUInt);
        int size = 1;
        while (size < aLength + bLength)
        {
            size <<= 1;
        }
        ComplexNumber[] fa = new ComplexNumber[size];
        ComplexNumber[] fb = new ComplexNumber[size];
        for (int i = 0; i < a.Length; i++)
        {
            uint value = a[i];
            int offset = i * LimbsPerUInt;
            fa[offset] = new ComplexNumber(value & LimbMask, 0);
            fa[offset + 1] = new ComplexNumber((value >> 8) & LimbMask, 0);
            fa[offset + 2] = new ComplexNumber((value >> 16) & LimbMask, 0);
            fa[offset + 3] = new ComplexNumber((value >> 24) & LimbMask, 0);
        }
        for (int i = 0; i < b.Length; i++)
        {
            uint value = b[i];
            int offset = i * LimbsPerUInt;
            fb[offset] = new ComplexNumber(value & LimbMask, 0);
            fb[offset + 1] = new ComplexNumber((value >> 8) & LimbMask, 0);
            fb[offset + 2] = new ComplexNumber((value >> 16) & LimbMask, 0);
            fb[offset + 3] = new ComplexNumber((value >> 24) & LimbMask, 0);
        }
        Fft(fa, false);
        Fft(fb, false);
        for (int i = 0; i < size; i++)
        {
            fa[i] *= fb[i];
        }
        Fft(fa, true);
        int coefficientCount = aLength + bLength;
        long[] coefficients = new long[coefficientCount];
        for (int i = 0; i < coefficientCount; i++)
        {
            double value = fa[i].Real;
            long rounded = (long)Math.Round(value);
            if (rounded < 0)
            {
                if (Math.Abs(value) < 0.5)
                {
                    rounded = 0;
                }
                else
                {
                    throw new ArithmeticException(
                        "FFT produced an invalid negative coefficient.");
                }
            }
            coefficients[i] = rounded;
        }
        long carry = 0;
        for (int i = 0; i < coefficients.Length; i++)
        {
            long value = coefficients[i] + carry;
            if (value < 0)
            {
                throw new ArithmeticException(
                    "FFT produced an invalid coefficient.");
            }
            coefficients[i] = value & LimbMask;
            carry = value >> LimbBits;
        }
        while (carry != 0)
        {
            Array.Resize(ref coefficients, coefficients.Length + 1);
            coefficients[^1] = carry & LimbMask;
            carry >>= LimbBits;
        }
        int wordCount = (coefficients.Length + LimbsPerUInt - 1) / LimbsPerUInt;
        uint[] result = new uint[wordCount];
        for (int i = 0; i < coefficients.Length; i++)
        {
            int wordIndex = i / LimbsPerUInt;
            int shift = (i % LimbsPerUInt) * LimbBits;
            result[wordIndex] |=
                (uint)coefficients[i] << shift;
        }
        return BetterBigInteger.Normalize(result);
    }

    private static void Fft(ComplexNumber[] values, bool inverse)
    {
        int n = values.Length;
        for (int i = 1, j = 0; i < n; i++)
        {
            int bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }
            j ^= bit;
            if (i < j)
            {
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
        for (int length = 2; length <= n; length <<= 1)
        {
            double angle =
                2.0 * Math.PI / length *
                (inverse ? 1.0 : -1.0);
            ComplexNumber root =
                new(Math.Cos(angle), Math.Sin(angle));
            int half = length >> 1;
            for (int start = 0; start < n; start += length)
            {
                ComplexNumber w = new(1.0, 0.0);
                for (int j = 0; j < half; j++)
                {
                    ComplexNumber even = values[start + j];
                    ComplexNumber odd =
                        values[start + j + half] * w;
                    values[start + j] = even + odd;
                    values[start + j + half] = even - odd;
                    w *= root;
                }
            }
        }
        if (!inverse)
        {
            return;
        }
        double scale = 1.0 / n;
        for (int i = 0; i < n; i++)
        {
            values[i] *= scale;
        }
    }

    private struct ComplexNumber
    {
        public double Real;
        public double Imaginary;

        public ComplexNumber(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public static ComplexNumber operator +(
            ComplexNumber a,
            ComplexNumber b) =>
            new(
                a.Real + b.Real,
                a.Imaginary + b.Imaginary);

        public static ComplexNumber operator -(
            ComplexNumber a,
            ComplexNumber b) =>
            new(
                a.Real - b.Real,
                a.Imaginary - b.Imaginary);

        public static ComplexNumber operator *(
            ComplexNumber a,
            ComplexNumber b) =>
            new(
                a.Real * b.Real -
                a.Imaginary * b.Imaginary,
                a.Real * b.Imaginary +
                a.Imaginary * b.Real);

        public static ComplexNumber operator *(
            ComplexNumber a,
            double value) =>
            new(
                a.Real * value,
                a.Imaginary * value);
    }
}