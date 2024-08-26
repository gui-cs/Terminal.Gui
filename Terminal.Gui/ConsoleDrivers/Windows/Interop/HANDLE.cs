namespace Terminal.Gui.ConsoleDrivers.Windows.Interop;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

/// <summary>
///     Native HANDLE type for Windows interop. Wraps a <see langword="nint"/>.
/// </summary>
/// <remarks>
///     This type should not be used outside of interop with the Win32 APIs.<br/>
///     Use <see cref="SafeHandle"/>-derived types for .net code beyond the PInvoke extern methods.
/// </remarks>
[DebuggerDisplay ($"{{{nameof (Value)}}}")]
[SuppressMessage (
                     "ReSharper",
                     "InconsistentNaming",
                     Justification = "Following recommendation to keep types named the same as the native types.")]
[SuppressMessage (
                     "ReSharper",
                     "RedundantUnsafeContext",
                     Justification =
                         """
                         Yes it is redundant on the basis of being a value type.
                         However, it is not actually redundant, because the underlying nint value can become invalid if another 
                         object releases the native resource it refers to.
                         """)]
[StructLayout (LayoutKind.Explicit)]
internal readonly unsafe struct HANDLE (nint Value)
    : IEqualityOperators<HANDLE, HANDLE, bool>, ITrueFalseOperators<HANDLE>, IEquatable<HANDLE>
{
    private const nint NULLPTR = 0;

    private static readonly HANDLE _null = NULLPTR;

    public HANDLE (HANDLE original) : this (original.Value)
    {
        // Copy constructor. Nothing to do because the primary constructor already handles it.
    }

    /// <summary>
    ///     The underlying native pointer, as a <see langword="nint"/>.
    /// </summary>
    [FieldOffset (0)]
    internal readonly nint Value = Value;

    /// <inheritdoc/>
    public static bool operator == (HANDLE left, HANDLE right) => left.Value == right.Value;

    /// <inheritdoc/>
    public static bool operator != (HANDLE left, HANDLE right) => left.Value != right.Value;

    /// <summary>Indicates whether the current <see cref="HANDLE"/> is equal to another <see cref="HANDLE"/>.</summary>
    /// <param name="other">A <see cref="HANDLE"/> to compare with this <see cref="HANDLE"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the current <see cref="HANDLE"/> is equal to the <paramref name="other"/> parameter; otherwise,
    ///     <see langword="false"/>.
    /// </returns>
    /// <remarks>There is a <see langword="ref"/> <see langword="readonly"/> version of this which should be used when possible.</remarks>
    public bool Equals (HANDLE other) => Value == other.Value;

    /// <inheritdoc/>
    public static bool operator false (HANDLE value) => value.IsNull;

    /// <inheritdoc/>
    public static bool operator true (HANDLE value) => !value.IsNull;

    /// <summary>
    ///     Deconstructor for <see cref="HANDLE"/>.
    /// </summary>
    /// <param name="Value">The underlying <see langword="nint"/> referring to the native resource.</param>
    /// <remarks>The values output by this method are copies.</remarks>
    [SuppressMessage ("ReSharper", "ParameterHidesMember", Justification = "This is a deconstructor.")]
    public void Deconstruct (out nint Value) { Value = this.Value; }

    /// <inheritdoc/>
    public override bool Equals (object? obj) => obj is HANDLE other && Equals (other);

    /// <summary>Indicates whether the current <see cref="HANDLE"/> is equal to another <see cref="HANDLE"/>.</summary>
    /// <param name="other">A readonly reference to a <see cref="HANDLE"/> to compare with this <see cref="HANDLE"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the current <see cref="HANDLE"/> is equal to the <paramref name="other"/> parameter; otherwise,
    ///     <see langword="false"/>.
    /// </returns>
    [Pure]
    public bool Equals (ref readonly HANDLE other) => Value == other.Value;

    /// <inheritdoc/>
    public override int GetHashCode () => Value.GetHashCode ();

    /// <summary>
    ///     Explicit conversion from <see cref="SafeHandle"/> to <see cref="HANDLE"/> via <see cref="SafeHandle.DangerousGetHandle"/>.
    /// </summary>
    /// <remarks>This operator is marked experimental to force explicit consent to its use.</remarks>
    [Experimental ("TGEXP0001")]
    public static explicit operator HANDLE (SafeHandle value) => new (value.DangerousGetHandle ());

    /// <summary>Explicit conversion from <see cref="HANDLE"/> to <see langword="nint"/> by returning <see cref="Value"/>.</summary>
    public static explicit operator nint (HANDLE value) => value.Value;

    /// <summary>
    ///     Explicit conversion from <see cref="HANDLE"/> to <see cref="SafeFileHandle"/> via <see cref="SafeFileHandle(nint,bool)"/>,
    ///     specifying <see langword="true"/> for ownsHandle, effectively transferring ownership of the native resource to the
    ///     SafeFileHandle.
    /// </summary>
    /// <remarks>
    ///     The returned <see cref="SafeFileHandle"/> will close the native handle during finalization.<br/>
    ///     To ensure native handles are released, you should call <see cref="SafeHandle.Dispose"/> on the <see cref="SafeFileHandle"/>
    ///     when it is no longer needed.
    ///     <para>
    ///         Use of this conversion does not duplicate the handle.<br/>
    ///         If another object releases the handle, all references to the handle, including the <see cref="SafeFileHandle"/> instance
    ///         returned by this operator, will no longer be valid, and the
    ///     </para>
    /// </remarks>
    /// <remarks>This operator is marked experimental to force explicit consent to its use.</remarks>
    [Experimental ("TGEXP0002")]
    public static explicit operator SafeFileHandle (HANDLE value) => new (value.Value, true);

    /// <summary>Implicit conversion from <see langword="nint"/> to <see cref="HANDLE"/> via <see cref="HANDLE(nint)"/>.</summary>
    public static implicit operator HANDLE (nint value) => new (value);

    /// <inheritdoc/>
    public override string ToString () => $"0x{Value:X8}";

    /// <summary>
    ///     Gets whether the underlying <see langword="nint"/> is a null pointer (value 0).
    /// </summary>
    internal bool IsNull => Value == NULLPTR;

    /// <summary>
    ///     Returns a reference to a readonly <see cref="HANDLE"/> with an underlying null pointer (value 0).
    /// </summary>
    internal static ref readonly HANDLE Null => ref _null;
}
