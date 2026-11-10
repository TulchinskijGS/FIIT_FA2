using Arithmetic.BigInt.Interfaces;

namespace Arithmetic.BigInt.MultiplyStrategy;

internal class SimpleMultiplier : IMultiplier
{
    public BetterBigInteger Multiply(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        uint[] product = BetterBigInteger.MultiplyMagnitude(a.GetDigits(), b.GetDigits());
        return BetterBigInteger.FromMagnitude(product, a.IsNegative != b.IsNegative);
    }
}