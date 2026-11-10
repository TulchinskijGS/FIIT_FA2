using Arithmetic.BigInt.Interfaces;
using Arithmetic.BigInt.MultiplyStrategy;

namespace Arithmetic.BigInt;

public sealed class BetterBigInteger : IBigInteger
{
    private int _signBit;
    
    private uint _smallValue; // Если число маленькое, храним его прямо в этом поле, а _data == null.
    private uint[]? _data;
    
    public bool IsNegative => _signBit == 1;
    
    /// От массива цифр (little endian)
    public BetterBigInteger(uint[] digits, bool isNegative = false)
    {
        if (digits is null) throw new ArgumentNullException(nameof(digits));
        Initialize(digits, isNegative);
    }
    
    public BetterBigInteger(IEnumerable<uint> digits, bool isNegative = false)
    {
        if (digits is null) throw new ArgumentNullException(nameof(digits));
        Initialize(digits.ToArray(), isNegative);
    }
    
    public BetterBigInteger(string value, int radix)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        ValidateRadix(radix);
        if (value.Length == 0) throw new FormatException("Empty integer.");

        int index = 0;
        bool negative = false;
        if (value[0] is '+' or '-')
        {
            negative = value[0] == '-';
            index = 1;
        }
        if (index == value.Length) throw new FormatException("Invalid integer.");

        uint[] magnitude = [0];
        for (; index < value.Length; index++)
        {
            int digit = DigitValue(value[index]);
            if (digit < 0 || digit >= radix)
                throw new FormatException($"Invalid digit '{value[index]}'.");
            magnitude = AddSmall(MultiplySmall(magnitude, (uint)radix), (uint)digit);
        }
        Initialize(magnitude, negative);
    }
    
    
    public ReadOnlySpan<uint> GetDigits()
    {
        return _data ?? [_smallValue];
    }
    
    public int CompareTo(IBigInteger? other)
    {
        if (other is null) return 1;
        bool otherNegative = other.IsNegative && !IsZero(other.GetDigits());
        if (IsNegative != otherNegative) return IsNegative ? -1 : 1;
        int cmp = CompareMagnitude(GetDigits(), other.GetDigits());
        return IsNegative ? -cmp : cmp;
    }

    public bool Equals(IBigInteger? other) => other is not null && CompareTo(other) == 0;
    public override bool Equals(object? obj) => obj is IBigInteger other && Equals(other);
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (IsNegative ? 1 : 0);
            foreach (uint d in GetDigits()) hash = hash * 31 + d.GetHashCode();
            return hash;
        }
    }
    
    public static BetterBigInteger operator +(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        var am = a.GetDigits();
        var bm = b.GetDigits();
        if (a.IsNegative == b.IsNegative)
            return FromMagnitude(AddMagnitude(am, bm), a.IsNegative);
        int cmp = CompareMagnitude(am, bm);
        if (cmp == 0) return Zero;
        return cmp > 0 ? FromMagnitude(SubMagnitude(am, bm), a.IsNegative)
                    : FromMagnitude(SubMagnitude(bm, am), b.IsNegative);
    }

    public static BetterBigInteger operator -(BetterBigInteger a, BetterBigInteger b) => a + -b;

    public static BetterBigInteger operator -(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return FromMagnitude(a.GetDigits().ToArray(), !a.IsNegative);
    }
    public static BetterBigInteger operator /(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if (IsZero(b.GetDigits())) throw new DivideByZeroException();
        var (q, _) = DivideMagnitude(a.GetDigits(), b.GetDigits());
        return FromMagnitude(q, a.IsNegative != b.IsNegative);
    }

    public static BetterBigInteger operator %(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        if (IsZero(b.GetDigits())) throw new DivideByZeroException();
        var (_, r) = DivideMagnitude(a.GetDigits(), b.GetDigits());
        return FromMagnitude(r, a.IsNegative);
    }

    public static BetterBigInteger operator *(BetterBigInteger a, BetterBigInteger b)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        int n = Math.Max(a.GetDigits().Length, b.GetDigits().Length);
        IMultiplier multiplier = n >= 32 ? new FftMultiplier() : n >= 8 ? new KaratsubaMultiplier() : new SimpleMultiplier();
        return multiplier.Multiply(a, b);
    }

    public static BetterBigInteger operator ~(BetterBigInteger a)
    {
        ArgumentNullException.ThrowIfNull(a);
        return -a - One;
    }

    public static BetterBigInteger operator &(BetterBigInteger a, BetterBigInteger b) => Bitwise(a, b, (x, y) => x & y);
    public static BetterBigInteger operator |(BetterBigInteger a, BetterBigInteger b) => Bitwise(a, b, (x, y) => x | y);
    public static BetterBigInteger operator ^(BetterBigInteger a, BetterBigInteger b) => Bitwise(a, b, (x, y) => x ^ y);

    public static BetterBigInteger operator <<(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift < 0) return a >> -shift;
        if (shift == 0 || IsZero(a.GetDigits())) return a;
        return FromMagnitude(ShiftLeftMagnitude(a.GetDigits(), shift), a.IsNegative);
    }

    public static BetterBigInteger operator >>(BetterBigInteger a, int shift)
    {
        ArgumentNullException.ThrowIfNull(a);
        if (shift < 0) return a << -shift;
        if (shift == 0 || IsZero(a.GetDigits())) return a;
        if (!a.IsNegative)
            return FromMagnitude(ShiftRightMagnitude(a.GetDigits(), shift), false);
        uint[] add = ShiftLeftMagnitude([1], shift);
        uint[] adjusted = AddMagnitude(a.GetDigits(), SubMagnitude(add, [1]));
        return FromMagnitude(ShiftRightMagnitude(adjusted, shift), true);
    }
    
    public static bool operator ==(BetterBigInteger a, BetterBigInteger b) => Equals(a, b);
    public static bool operator !=(BetterBigInteger a, BetterBigInteger b) => !Equals(a, b);
    public static bool operator <(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) < 0;
    public static bool operator >(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) > 0;
    public static bool operator <=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) <= 0;
    public static bool operator >=(BetterBigInteger a, BetterBigInteger b) => a.CompareTo(b) >= 0;
    
    
    public override string ToString() => ToString(10);

    public string ToString(int radix)
    {
        ValidateRadix(radix);
        if (IsZero(GetDigits())) return "0";
        uint[] current = GetDigits().ToArray();
        char[] chars = new char[Math.Max(1, current.Length * 32)];
        int pos = chars.Length;
        while (!IsZero(current))
        {
            var (q, rem) = DivideBySmall(current, (uint)radix);
            chars[--pos] = DigitChar((int)rem);
            current = q;
        }
        if (IsNegative) chars[--pos] = '-';
        return new string(chars, pos, chars.Length - pos);
    }

    internal static BetterBigInteger FromMagnitude(uint[] magnitude, bool negative)
    {
        return new BetterBigInteger(magnitude, negative);
    }

    internal static uint[] MultiplyMagnitude(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (IsZero(a) || IsZero(b)) return [0];
        uint[] result = new uint[a.Length + b.Length];
        for (int i = 0; i < a.Length; i++)
        {
            ulong carry = 0;
            for (int j = 0; j < b.Length; j++)
            {
                ulong cur = (ulong)a[i] * b[j] + result[i + j] + carry;
                result[i + j] = (uint)cur;
                carry = cur >> 32;
            }
            int k = i + b.Length;
            while (carry != 0)
            {
                ulong cur = (ulong)result[k] + carry;
                result[k++] = (uint)cur;
                carry = cur >> 32;
            }
        }
        return Normalize(result);
    }

    internal static uint[] AddMagnitude(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int n = Math.Max(a.Length, b.Length);
        uint[] result = new uint[n + 1];
        ulong carry = 0;
        for (int i = 0; i < n; i++)
        {
            ulong s = carry + (i < a.Length ? a[i] : 0UL) + (i < b.Length ? b[i] : 0UL);
            result[i] = (uint)s;
            carry = s >> 32;
        }
        result[n] = (uint)carry;
        return Normalize(result);
    }

    internal static uint[] SubMagnitude(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        if (CompareMagnitude(a, b) < 0) throw new ArgumentException("First magnitude must not be smaller.");
        uint[] result = new uint[a.Length];
        long borrow = 0;
        for (int i = 0; i < a.Length; i++)
        {
            long diff = (long)a[i] - (i < b.Length ? b[i] : 0L) - borrow;
            if (diff < 0) { diff += 1L << 32; borrow = 1; } else borrow = 0;
            result[i] = (uint)diff;
        }
        return Normalize(result);
    }

    internal static int CompareMagnitude(ReadOnlySpan<uint> a, ReadOnlySpan<uint> b)
    {
        int al = EffectiveLength(a), bl = EffectiveLength(b);
        if (al != bl) return al.CompareTo(bl);
        for (int i = al - 1; i >= 0; i--)
            if (a[i] != b[i]) return a[i].CompareTo(b[i]);
        return 0;
    }

    internal static uint[] Normalize(uint[] digits)
    {
        int n = digits.Length;
        while (n > 1 && digits[n - 1] == 0) n--;
        if (n != digits.Length) Array.Resize(ref digits, n);
        if (digits.Length == 0) return [0];
        return digits;
    }

    private void Initialize(uint[] digits, bool negative)
    {
        uint[] normalized = Normalize((uint[])digits.Clone());
        if (normalized.Length == 1)
        {
            _smallValue = normalized[0]; _data = null;
        }
        else
        {
            _smallValue = 0; _data = normalized;
        }
        _signBit = normalized.Length == 1 && normalized[0] == 0 ? 0 : negative ? 1 : 0;
    }

    private static readonly BetterBigInteger Zero = new([0]);
    private static readonly BetterBigInteger One = new([1]);

    private static BetterBigInteger Bitwise(BetterBigInteger a, BetterBigInteger b, Func<uint, uint, uint> op)
    {
        ArgumentNullException.ThrowIfNull(a); ArgumentNullException.ThrowIfNull(b);
        int width = Math.Max(a.GetDigits().Length, b.GetDigits().Length) + 1;
        uint[] x = ToTwosComplement(a, width), y = ToTwosComplement(b, width);
        for (int i = 0; i < width; i++) x[i] = op(x[i], y[i]);
        return FromTwosComplement(x);
    }

    private static uint[] ToTwosComplement(BetterBigInteger value, int width)
    {
        uint[] result = new uint[width];
        value.GetDigits().CopyTo(result);
        if (!value.IsNegative) return result;
        for (int i = 0; i < width; i++) result[i] = ~result[i];
        ulong carry = 1;
        for (int i = 0; i < width && carry != 0; i++)
        {
            ulong s = (ulong)result[i] + carry;
            result[i] = (uint)s; carry = s >> 32;
        }
        return result;
    }

    private static BetterBigInteger FromTwosComplement(uint[] value)
    {
        bool negative = (value[^1] & 0x80000000u) != 0;
        if (!negative) return FromMagnitude(value, false);
        for (int i = 0; i < value.Length; i++) value[i] = ~value[i];
        ulong carry = 1;
        for (int i = 0; i < value.Length && carry != 0; i++)
        {
            ulong s = (ulong)value[i] + carry;
            value[i] = (uint)s; carry = s >> 32;
        }
        return FromMagnitude(value, true);
    }

    private static uint[] ShiftLeftMagnitude(ReadOnlySpan<uint> a, int shift)
    {
        if (shift == 0) return a.ToArray();
        int wordShift = shift / 32, bitShift = shift % 32;
        uint[] result = new uint[a.Length + wordShift + (bitShift == 0 ? 0 : 1)];
        ulong carry = 0;
        for (int i = 0; i < a.Length; i++)
        {
            ulong cur = ((ulong)a[i] << bitShift) | carry;
            result[i + wordShift] = (uint)cur;
            carry = cur >> 32;
        }
        if (bitShift != 0) result[a.Length + wordShift] = (uint)carry;
        return Normalize(result);
    }

    private static uint[] ShiftRightMagnitude(ReadOnlySpan<uint> a, int shift)
    {
        int wordShift = shift / 32, bitShift = shift % 32;
        if (wordShift >= a.Length) return [0];
        int n = a.Length - wordShift;
        uint[] result = new uint[n];
        if (bitShift == 0) { for (int i = 0; i < n; i++) result[i] = a[i + wordShift]; return Normalize(result); }
        uint carry = 0;
        for (int i = n - 1; i >= 0; i--)
        {
            uint cur = a[i + wordShift];
            result[i] = (cur >> bitShift) | (carry << (32 - bitShift));
            carry = cur;
        }
        return Normalize(result);
    }

    internal static (uint[] Quotient, uint[] Remainder) DivideMagnitude(ReadOnlySpan<uint> dividend, ReadOnlySpan<uint> divisor)
    {
        if (IsZero(divisor)) throw new DivideByZeroException();
        if (CompareMagnitude(dividend, divisor) < 0) return ([0], Normalize(dividend.ToArray()));
        int bits = EffectiveLength(dividend) * 32;
        uint[] quotient = new uint[EffectiveLength(dividend)];
        uint[] remainder = [0];
        for (int bit = bits - 1; bit >= 0; bit--)
        {
            remainder = ShiftLeftMagnitude(remainder, 1);
            int wi = bit / 32, bi = bit % 32;
            if (((dividend[wi] >> bi) & 1u) != 0) remainder = AddSmall(remainder, 1);
            if (CompareMagnitude(remainder, divisor) >= 0)
            {
                remainder = SubMagnitude(remainder, divisor);
                quotient[wi] |= 1u << bi;
            }
        }
        return (Normalize(quotient), Normalize(remainder));
    }

    private static uint[] MultiplySmall(ReadOnlySpan<uint> a, uint m)
    {
        if (m == 0 || IsZero(a)) return [0];
        uint[] result = new uint[a.Length + 1]; ulong carry = 0;
        for (int i = 0; i < a.Length; i++) { ulong cur = (ulong)a[i] * m + carry; result[i] = (uint)cur; carry = cur >> 32; }
        result[a.Length] = (uint)carry; return Normalize(result);
    }

    private static uint[] AddSmall(ReadOnlySpan<uint> a, uint value)
    {
        uint[] result = a.ToArray(); ulong carry = value;
        for (int i = 0; i < result.Length && carry != 0; i++) { ulong s = (ulong)result[i] + carry; result[i] = (uint)s; carry = s >> 32; }
        if (carry != 0) { Array.Resize(ref result, result.Length + 1); result[^1] = (uint)carry; }
        return Normalize(result);
    }

    private static (uint[] Quotient, uint Remainder) DivideBySmall(ReadOnlySpan<uint> a, uint divisor)
    {
        if (divisor == 0) throw new DivideByZeroException();
        uint[] q = new uint[a.Length]; ulong rem = 0;
        for (int i = a.Length - 1; i >= 0; i--) { ulong cur = (rem << 32) | a[i]; q[i] = (uint)(cur / divisor); rem = cur % divisor; }
        return (Normalize(q), (uint)rem);
    }

    private static int EffectiveLength(ReadOnlySpan<uint> a) { int n = a.Length; while (n > 1 && a[n - 1] == 0) n--; return n; }
    private static bool IsZero(ReadOnlySpan<uint> a) => EffectiveLength(a) == 1 && a[0] == 0;
    private static void ValidateRadix(int radix) { if (radix is < 2 or > 36) throw new ArgumentOutOfRangeException(nameof(radix)); }
    private static int DigitValue(char c) => c is >= '0' and <= '9' ? c - '0' : c is >= 'A' and <= 'Z' ? c - 'A' + 10 : c is >= 'a' and <= 'z' ? c - 'a' + 10 : -1;
    private static char DigitChar(int d) => (char)(d < 10 ? '0' + d : 'A' + d - 10);
}