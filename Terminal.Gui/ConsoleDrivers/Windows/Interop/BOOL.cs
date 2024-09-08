#nullable enable

namespace Terminal.Gui.ConsoleDrivers.Windows.Interop;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Resources;

/// <summary>
///     Native boolean type for Windows interop, based on a 32-bit signed integer (DWORD), which is considered false if 0 and true
///     for all other
///     values, as defined in WinDef.h.
/// </summary>
/// <remarks>
///     This type is what should be used for any actual DllImport/LibraryImport PInvoke methods that return or take BOOL, instead of
///     <see langword="bool"/>, which is not blittable..
/// </remarks>
/// <remarks>This type is not intended for use outside of interop with the Win32 APIs.</remarks>
/// <remarks>
///     The .net <see langword="bool"/> type is not blittable, mostly for reasons around inconsistent implementations between
///     different APIs.<br/>
///     This struct is blittable and is a signed 32-bit integer (DWORD), as the BOOL type in WinDef.h is defined.
/// </remarks>
[DebuggerDisplay ($"{{{nameof (Value)}}}")]
[SuppressMessage (
                     "ReSharper",
                     "InconsistentNaming",
                     Justification = "Following recommendation to keep types named the same as the native types.")]
[PublicAPI]
[StructLayout (LayoutKind.Explicit)]
public readonly struct BOOL :
    IComparisonOperators<BOOL, BOOL, bool>,
    IEqualityOperators<BOOL, bool, bool>,
    IEqualityOperators<BOOL, int, bool>,
    ITrueFalseOperators<BOOL>,
    IEquatable<BOOL>,
    IEquatable<bool>,
    IEquatable<int>,
    IComparable<bool>,
    IComparable<BOOL>,
    IComparable
{
    /// <summary>
    ///     Creates a new <see cref="BOOL"/> based on the equivalent truth of the provided <see cref="int"/> value.
    /// </summary>
    /// <param name="value">The zero or non-zero value to initialize the new <see cref="BOOL"/> to.</param>
    /// <remarks>
    ///     Any non-zero value is interpreted as <see langword="true"/>, and the underlying value will be set to <see langword="int"/>.
    ///     <see cref="IBinaryNumber{TSelf}.AllBitsSet"/> if <paramref name="value"/> is <see langword="true"/>.
    /// </remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    internal BOOL (int value) : this (value != 0) { }

    /// <summary>
    ///     Constructor which sets the new <see cref="BOOL"/> to <see cref="IBinaryNumber{T}.AllBitsSet"/> for <see langword="true"/> or
    ///     <see cref="IBinaryNumber{T}.Zero"/> for <see langword="false"/>.
    /// </summary>
    /// <param name="value">The equivalent <see langword="bool"/> value to initialize the new <see cref="BOOL"/> to.</param>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    internal BOOL (bool value) { Value = value ? -1 : 0; }

    /// <summary>
    ///     The underlying <see langword="int"/> value of the <see cref="BOOL"/>.
    /// </summary>
    /// <remarks>
    ///     This is the value that will be marshaled in and out of native method calls.<br/>
    ///     Any non-zero value is interpreted by Win32 APIs as <see langword="true"/>, and zero is interpreted as <see langword="false"/>
    ///     .<br/>
    ///     The specific bitwise value of this field should not be used in any way, nor should it be expected to be preserved across
    ///     PInvoke calls.<br/>
    ///     Only the zero or non-zero status of the field is significant and .
    /// </remarks>
    [FieldOffset (0)]
    [MarshalAs (UnmanagedType.I4)]
    private readonly int Value;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public int CompareTo (object? obj)
    {
        return obj switch
               {
                   null     => 1,
                   BOOL b   => CompareTo (b.IsTrue),
                   bool b   => CompareTo (b),
                   int n    => CompareTo (n),
                   uint n   => CompareTo (n),
                   short n  => CompareTo (n),
                   ushort n => CompareTo (n),
                   long n   => CompareTo (n),
                   ulong n  => CompareTo (n),
                   byte n   => CompareTo (n),
                   sbyte n  => CompareTo (n),
                   not BOOL => throw new ArgumentException (
                                                            Strings.BOOL_CompareTo_UnsupportedType,
                                                            new NotSupportedException (Strings.BOOL_CompareTo_UnsupportedType))
               };
    }

    /// <inheritdoc/>
    /// <remarks>Directly compares <see cref="IsTrue"/> to <paramref name="value"/> to avoid type conversion.</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public int CompareTo (bool value)
    {
        // Truth table.
        // Orders the same way as System.Boolean.
        return (IsTrue, value) switch
               {
                   (false, false) => 0,
                   (false, true)  => -1,
                   (true, false)  => 1,
                   (true, true)   => 0
               };
    }

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public int CompareTo (BOOL value) => this == value ? 0 : IsTrue ? 1 : -1;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator > (BOOL left, BOOL right) => left.CompareTo (right) > 0;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator >= (BOOL left, BOOL right) => left.CompareTo (right) >= 0;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator < (BOOL left, BOOL right) => left.CompareTo (right) < 0;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator <= (BOOL left, BOOL right) => left.CompareTo (right) <= 0;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator == (BOOL left, BOOL right) => left.Equals (right);

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator != (BOOL left, BOOL right) => !left.Equals (right);

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator == (BOOL left, bool right) => left.Equals (right);

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator != (BOOL left, bool right) => !left.Equals (right);

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator == (BOOL left, int right) => left.Equals (right);

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator != (BOOL left, int right) => left.IsTrue != (right != 0);

    /// <inheritdoc cref="bool.Equals(bool)"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public bool Equals (bool other) => other == IsTrue;

    /// <summary>Indicates whether the current <see cref="BOOL"/> is equal to another <see cref="BOOL"/>.</summary>
    /// <param name="other">A <see cref="BOOL"/> to compare with this <see cref="BOOL"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the current value is equal to the <paramref name="other"/> value; otherwise,
    ///     <see langword="false"/>.
    /// </returns>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public bool Equals (BOOL other) => IsTrue == other.IsTrue;

    /// <inheritdoc cref="int.Equals(int)"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public bool Equals (int other) => other != 0 == IsTrue;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator false (BOOL value) => value.IsFalse;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator true (BOOL value) => value.IsTrue;

    /// <summary>
    ///     Directly compares this <see cref="BOOL"/> value against the provided <see cref="INumberBase{TSelf}"/>? value, avoiding type
    ///     conversions.
    /// </summary>
    /// <remarks>
    ///     If <paramref name="number"/> is <see langword="null"/>, returns 1. Otherwise, compares 0 as <see langword="false"/> and
    ///     non-zero as <see langword="true"/> by delegating to <see cref="CompareTo(bool)"/>.
    /// </remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public int CompareTo<T> (T? number) where T : INumberBase<T>
    {
        return number switch
               {
                   null                     => 1,
                   _ when T.IsZero (number) => CompareTo (false),
                   _                        => CompareTo (true)
               };
    }

    /// <inheritdoc cref="ValueType.Equals(object?)"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public override bool Equals (object? obj)
    {
        return obj switch
               {
                   BOOL b when Equals (b) => true,
                   bool b when Equals (b) => true,
                   _                      => false
               };
    }

    /// <summary>Indicates whether the current <see cref="BOOL"/> value is equal to the equivalent truth of a numeric value.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public bool Equals<T> (T number) where T : INumberBase<T> => IsTrue == T.IsZero (number);

    /// <summary>Override of <see cref="ValueType.GetHashCode"/> for the <see cref="BOOL"/> type.</summary>
    /// <remarks>Values returned are either all bits set or no bits set, depending on the value of the <see cref="BOOL"/>.</remarks>
    /// <returns>
    ///     If <see langword="true"/>, <see langword="int"/>.<see cref="IBinaryNumber{TSelf}.AllBitsSet"/> (-1).<br/>
    ///     If <see langword="false"/>, <see langword="int"/>.<see cref="INumberBase{TSelf}.Zero"/> (0).
    /// </returns>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode () => IsTrue ? -1 : 0;

    /// <summary>Computes the logical AND result of a <see langword="bool"/> and a <see cref="BOOL"/> directly, avoiding conversion.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (bool left, BOOL right) => left && right.IsTrue;

    /// <summary>Computes the logical AND result of a <see cref="BOOL"/> and a <see langword="bool"/> directly, avoiding conversion.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (BOOL left, bool right) => left.IsTrue && right;

    /// <summary>Computes the logical AND result of two <see cref="BOOL"/> values.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (BOOL left, BOOL right) => left.IsTrue && right.IsTrue;

    /// <summary>Computes the logical AND result of an <see langword="int"/> and a <see cref="BOOL"/> directly, avoiding conversion.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (int left, BOOL right) => left != 0 && right.IsTrue;

    /// <summary>Computes the logical OR result of a <see cref="BOOL"/> and an <see langword="int"/> directly, avoiding conversion.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (BOOL left, int right) => left.IsTrue && right != 0;

    /// <summary>Computes the logical OR result of a <see langword="bool"/> and a <see cref="BOOL"/> directly, avoiding conversion.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (bool left, BOOL right) => left || right.IsTrue;

    /// <summary>Computes the logical OR result of a <see cref="BOOL"/> and a <see langword="bool"/> directly, avoiding conversion.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (BOOL left, bool right) => left.IsTrue || right;

    /// <summary>Computes the logical OR result of an <see langword="int"/> and a <see cref="BOOL"/> directly, avoiding conversion.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (int left, BOOL right) => left != 0 || right.IsTrue;

    /// <summary>Computes the logical OR result of a <see cref="BOOL"/> and an <see langword="int"/> directly, avoiding conversion.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (BOOL left, int right) => left.IsTrue || right != 0;

    /// <summary>Computes the logical OR result of two <see cref="BOOL"/> values.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (BOOL left, BOOL right) => left.IsTrue || right.IsTrue;

    /// <inheritdoc cref="IEqualityOperators{TSelf,TOther,TResult}"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator == (int left, BOOL right) => right.Equals (left);

    /// <inheritdoc cref="IEqualityOperators{TSelf,TOther,TResult}"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator == (bool left, BOOL right) => right.Equals (left);

    /// <summary>
    ///     Computes the logical Exclusive OR result of a <see langword="bool"/> and a <see cref="BOOL"/> directly, avoiding conversion.
    /// </summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (bool left, BOOL right) => left ^ right.IsTrue;

    /// <summary>
    ///     Computes the logical Exclusive OR result of a <see cref="BOOL"/> and a <see langword="bool"/> directly, avoiding conversion.
    /// </summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (BOOL left, bool right) => left.IsTrue ^ right;

    /// <summary>
    ///     Computes the logical Exclusive OR result of an <see langword="int"/> and a <see cref="BOOL"/> directly, avoiding conversion.
    /// </summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (int left, BOOL right) => (left != 0) ^ right.IsTrue;

    /// <summary>
    ///     Computes the logical Exclusive OR result of a <see cref="BOOL"/> and an <see langword="int"/> directly, avoiding conversion.
    /// </summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (BOOL left, int right) => left.IsTrue ^ (right == 0);

    /// <summary>Computes the logical Exclusive OR result of two <see cref="BOOL"/> values.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (BOOL left, BOOL right) => left.IsTrue ^ right.IsTrue;

    /// <summary>Explicit conversion of a <see cref="BOOL"/> value to an equivalent <see langword="bool"/> value.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static explicit operator bool (BOOL value) => value.IsTrue;

    /// <summary>Explicit conversion of a <see langword="bool"/> value to an equivalent <see cref="BOOL"/> value.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static explicit operator BOOL (bool value) => new (value);

    /// <summary>Implicit conversion of an <see langword="int"/> value to an equivalent <see cref="BOOL"/> value.</summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static implicit operator BOOL (int value) => new (value);

    /// <summary>Implicit conversion of a <see cref="BOOL"/> value to an equivalent <see langword="int"/> value.</summary>
    /// <remarks>Values returned are either all bits set or no bits set, depending on the value of the <see cref="BOOL"/>.</remarks>
    /// <returns>
    ///     If <see langword="true"/>, <see langword="int"/>.<see cref="IBinaryNumber{TSelf}.AllBitsSet"/> (-1).<br/>
    ///     If <see langword="false"/>, <see langword="int"/>.<see cref="INumberBase{TSelf}.Zero"/> (0).
    /// </returns>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static implicit operator int (BOOL value) => value.Value;

    /// <inheritdoc cref="IEqualityOperators{TSelf,TOther,TResult}"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator != (int left, BOOL right) => !right.Equals (left);

    /// <inheritdoc cref="IEqualityOperators{TSelf,TOther,TResult}"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator != (bool left, BOOL right) => !right.Equals (left);

    /// <summary>
    ///     Computes the logical NOT (negation) of the current <see cref="BOOL"/> value, as a <see langword="bool"/>, avoiding
    ///     conversion.
    /// </summary>
    /// <seealso cref="op_OnesComplement"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ! (BOOL value) => value.IsFalse;

    /// <summary>
    ///     Computes the logical NOT (negation) of the current <see cref="BOOL"/> value, as a <see langword="bool"/>, avoiding
    ///     conversion.
    /// </summary>
    /// <seealso cref="op_LogicalNot"/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ~ (BOOL value) => value.IsFalse;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public override string ToString ()
    {
        return Value switch
               {
                   0 => bool.FalseString,
                   _ => bool.TrueString
               };
    }

    /// <summary>
    ///     Gets whether the current <see cref="BOOL"/> value is equivalent to <see langword="false"/>.
    /// </summary>
    private bool IsFalse
    {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        get => Value == 0;
    }

    /// <summary>
    ///     Gets whether the current <see cref="BOOL"/> value is equivalent to <see langword="true"/>.
    /// </summary>
    private bool IsTrue
    {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        get => Value != 0;
    }
}
