#nullable enable

namespace Terminal.Gui.ConsoleDrivers.Windows.Interop;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

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
internal readonly struct BOOL : IEqualityOperators<BOOL, BOOL, bool>, IEqualityOperators<BOOL, bool, bool>,
                                       ITrueFalseOperators<BOOL>, IEquatable<bool>
{
    /// <summary>
    ///     Creates a new <see cref="BOOL"/> directly from the provided <see cref="int"/>.
    /// </summary>
    /// <param name="value">The value to initialize the new <see cref="BOOL"/> to.</param>
    /// <remarks>
    ///     Any non-zero value is interpreted as true, but setting all bits to 1 (ie -1 or <see cref="IBinaryNumber{TSelf}.AllBitsSet"/>)
    ///     is recommended.
    /// </remarks>
    /// <remarks><see cref="INumberBase{TSelf}.Zero"/> is the only value interpreted as <see langword="false"/>.</remarks>
    internal BOOL (int value) { Value = value; }

    /// <summary>
    ///     Constructor which sets the new <see cref="BOOL"/> to <see cref="IBinaryNumber{T}.AllBitsSet"/> for <see langword="true"/> or
    ///     <see cref="IBinaryNumber{T}.Zero"/> for <see langword="false"/>.
    /// </summary>
    /// <param name="value">The equivalent <see langword="bool"/> value to initialize the new <see cref="BOOL"/> to.</param>
    internal BOOL (bool value) { Value = value ? -1 : 0; }

    internal readonly int Value;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator == (BOOL left, bool right) => left.Equals (right);

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator != (BOOL left, bool right) => !left.Equals (right);
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator == (BOOL left, BOOL right) => left.Equals (right);

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator != (BOOL left, BOOL right) => !left.Equals (right);

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public bool Equals (BOOL other) => IsTrue == other.IsTrue;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public bool Equals (bool other) => other == IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator false (BOOL value) => value.IsFalse;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator true (BOOL value) => value.IsTrue;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode () => IsTrue ? -1 : 0;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (bool left, BOOL right) => left && right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (BOOL left, bool right) => left.IsTrue && right;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (BOOL left, BOOL right) => left.IsTrue && right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (int left, BOOL right) => left != 0 && right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator & (BOOL left, int right) => left.IsTrue && right != 0;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (bool left, BOOL right) => left || right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (BOOL left, bool right) => left.IsTrue || right;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (int left, BOOL right) => left != 0 || right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (BOOL left, int right) => left.IsTrue || right != 0;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator | (BOOL left, BOOL right) => left.IsTrue || right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (bool left, BOOL right) => left ^ right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (BOOL left, bool right) => left.IsTrue ^ right;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (int left, BOOL right) => (left != 0) ^ right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (BOOL left, int right) => left.IsTrue ^ (right == 0);

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ^ (BOOL left, BOOL right) => left.IsTrue ^ right.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static explicit operator bool (BOOL value) => value.IsTrue;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static explicit operator BOOL (bool value) => new (value);

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static implicit operator BOOL (int value) => new (value);

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static implicit operator int (BOOL value) => value.Value;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ! (BOOL value) => value != true;

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool operator ~ (BOOL value) => value.Value == 0;

    /// <inheritdoc/>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public override string ToString () => $"{this}";

    private bool IsFalse
    {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        get => Value == 0;
    }

    private bool IsTrue
    {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        get => Value != 0;
    }
}
