// ReSharper disable once CheckNamespace
namespace Terminal.Gui.Analyzers.Internal.Compatibility;

/// <summary>
///     Extension methods for <see langword="int"/> and <see langword="uint"/> types.
/// </summary>
/// <remarks>
///     This is mostly just for backward compatibility with netstandard2.0.
/// </remarks>
public static class NumericExtensions
{
    /// <summary>
    ///     Gets the population count (number of bits set to 1) of this 32-bit value.
    /// </summary>
    /// <param name="value">The value to get the population count of.</param>
    /// <remarks>
    ///     The algorithm is the well-known SWAR (SIMD Within A Register) method for population count.<br/>
    ///     Included for hardware- and runtime- agnostic support for the equivalent of the x86 popcnt instruction, since
    ///     System.Numerics.Intrinsics isn't available in netstandard2.0.<br/>
    ///     It performs the operation simultaneously on 4 bytes at a time, rather than the naive method of testing all 32 bits
    ///     individually.<br/>
    ///     Most compilers can recognize this and turn it into a single platform-specific instruction, when available.
    /// </remarks>
    /// <returns>
    ///     An unsigned 32-bit integer value containing the population count of <paramref name="value"/>.
    /// </returns>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static uint GetPopCount (this uint value)
    {
        unchecked
        {
            value -= (value >> 1) & 0x55555555;
            value = (value & 0x33333333) + ((value >> 2) & 0x33333333);
            value = (value + (value >> 4)) & 0x0F0F0F0F;

            return (value * 0x01010101) >> 24;
        }
    }

    /// <inheritdoc cref="GetPopCount(uint)"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static uint GetPopCount (this int value) { return GetPopCount (Unsafe.As<int, uint> (ref value)); }
}
