namespace Terminal.Gui.ConsoleDrivers.Windows.Interop;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

/// <summary>
///     Native boolean type for Windows interop, based on a 32-bit integer which is considered false if 0 and true for all other
///     values.
/// </summary>
/// <remarks>
///     This type should not be used outside of interop with the Win32 APIs.
/// </remarks>
[DebuggerDisplay ($"{{{nameof (Value)}}}")]
[SuppressMessage (
                     "ReSharper",
                     "InconsistentNaming",
                     Justification = "Following recommendation to keep types named the same as the native types.")]
internal readonly record struct BOOL : IEqualityOperators<BOOL, BOOL, bool>, ITrueFalseOperators<BOOL>
{
    internal BOOL (int value) { Value = value; }

    internal BOOL (bool value) { Value = value ? 1 : 0; }

    internal readonly int Value;

    /// <inheritdoc/>
    public bool Equals (BOOL other) => Value == other.Value;

    /// <inheritdoc/>
    public override int GetHashCode () => Value.GetHashCode ();

    public static explicit operator BOOL (int value) => new (value);
    public static bool operator false (BOOL value) => value.Value == 0;

    public static implicit operator int (BOOL value) => value.Value;

    public static implicit operator bool (BOOL value) => value.Value != 0;

    public static implicit operator BOOL (bool value) => new (value);

    public static bool operator true (BOOL value) => value.Value != 0;

    /// <inheritdoc/>
    public override string ToString () => $"{(bool)this}";
}