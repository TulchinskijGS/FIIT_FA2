using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

public class KaratsubaMultiplier : IMultiplier
{
    private const int BaseCase = 4;

    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        uint[] product = MultiplyRecursive(a.GetDigits(), b.GetDigits());
        return BetterBigInteger.FromMagnitude(product, a.IsNegative != b.IsNegative);
    }

    private static uint[] MultiplyRecursive(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (a.Length == 0 || b.Length == 0) return [0];
        if (Math.Min(a.Length, b.Length) <= BaseCase)
            return BetterBigInteger.MultiplyMagnitude(a, b);
        int n = Math.Max(a.Length, b.Length) / 2;
        ReadOnlySpan<uint> a0 = a[..Math.Min(n, a.Length)];
        ReadOnlySpan<uint> a1 = n < a.Length ? a[n..] : [];
        ReadOnlySpan<uint> b0 = b[..Math.Min(n, b.Length)];
        ReadOnlySpan<uint> b1 = n < b.Length ? b[n..] : [];
        uint[] z0 = MultiplyRecursive(a0, b0);
        uint[] z2 = MultiplyRecursive(a1, b1);
        uint[] aSum = BetterBigInteger.AddMagnitude(a0, a1);
        uint[] bSum = BetterBigInteger.AddMagnitude(b0, b1);
        uint[] z1 = MultiplyRecursive(aSum, bSum);
        z1 = BetterBigInteger.SubMagnitude(z1, z0);
        z1 = BetterBigInteger.SubMagnitude(z1, z2);
        uint[] result = new uint[a.Length + b.Length + 1];
        AddAt(result, z0, 0);
        AddAt(result, z1, n);
        AddAt(result, z2, n * 2);
        return BetterBigInteger.Normalize(result);
    }

    private static void AddAt(uint[] target, ReadOnlySpan<uint> value, int offset)
    {
        ulong carry = 0;
        for (int i = 0; i < value.Length; i++)
        {
            int index = i + offset;
            ulong sum = (ulong)target[index] + value[i] + carry;
            target[index] = (uint)sum;
            carry = sum >> 32;
        }
        int pos = offset + value.Length;
        while (carry != 0)
        {
            ulong sum = (ulong)target[pos] + carry;
            target[pos++] = (uint)sum;
            carry = sum >> 32;
        }
    }
}