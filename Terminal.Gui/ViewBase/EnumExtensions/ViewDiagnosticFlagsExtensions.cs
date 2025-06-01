#nullable enable

using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Terminal.Gui.ViewBase;

/// <summary>Extension methods for the <see cref="ViewDiagnosticFlags"/> <see langword="enum"/> type.</summary>
[GeneratedCode ("Terminal.Gui.Analyzers.Internal", "1.0")]
[CompilerGenerated]
[DebuggerNonUserCode]
[ExcludeFromCodeCoverage (Justification = "Generated code is already tested.")]
[PublicAPI]
public static class ViewDiagnosticFlagsExtensions
{
    /// <summary>
    ///     Directly converts this <see cref="ViewDiagnosticFlags"/> value to an <see langword="int"/> value with
    ///     the same binary representation.
    /// </summary>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static int AsInt32 (this ViewDiagnosticFlags e) => Unsafe.As<ViewDiagnosticFlags, int> (ref e);

    /// <summary>
    ///     Directly converts this <see cref="ViewDiagnosticFlags"/> value to a <see langword="uint"/> value with
    ///     the same binary representation.
    /// </summary>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static uint AsUInt32 (this ViewDiagnosticFlags e) => Unsafe.As<ViewDiagnosticFlags, uint> (ref e);

    /// <summary>
    ///     Determines if the specified flags are set in the current value of this
    ///     <see cref="ViewDiagnosticFlags"/>.
    /// </summary>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    /// <returns>
    ///     True, if all flags present in <paramref name="checkFlags"/> are also present in the current value of the
    ///     <see cref="ViewDiagnosticFlags"/>.<br/>Otherwise false.
    /// </returns>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool FastHasFlags (this ViewDiagnosticFlags e, ViewDiagnosticFlags checkFlags)
    {
        ref uint enumCurrentValueRef = ref Unsafe.As<ViewDiagnosticFlags, uint> (ref e);
        ref uint checkFlagsValueRef = ref Unsafe.As<ViewDiagnosticFlags, uint> (ref checkFlags);

        return (enumCurrentValueRef & checkFlagsValueRef) == checkFlagsValueRef;
    }

    /// <summary>
    ///     Determines if the specified mask bits are set in the current value of this
    ///     <see cref="ViewDiagnosticFlags"/>.
    /// </summary>
    /// <param name="e">
    ///     The <see cref="ViewDiagnosticFlags"/> value to check against the <paramref name="mask"/>
    ///     value.
    /// </param>
    /// <param name="mask">A mask to apply to the current value.</param>
    /// <returns>
    ///     True, if all bits set to 1 in the mask are also set to 1 in the current value of the
    ///     <see cref="ViewDiagnosticFlags"/>.<br/>Otherwise false.
    /// </returns>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool FastHasFlags (this ViewDiagnosticFlags e, uint mask)
    {
        ref uint enumCurrentValueRef = ref Unsafe.As<ViewDiagnosticFlags, uint> (ref e);

        return (enumCurrentValueRef & mask) == mask;
    }

    /// <summary>
    ///     Determines if the specified <see langword="uint"/> value is explicitly defined as a named value of the
    ///     <see cref="ViewDiagnosticFlags"/> <see langword="enum"/> type.
    /// </summary>
    /// <remarks>
    ///     Only explicitly named values return true, as with IsDefined. Combined valid flag values of flags enums which are
    ///     not explicitly named will return false.
    /// </remarks>
    public static bool FastIsDefined (this ViewDiagnosticFlags _, uint value)
    {
        return value switch
               {
                   0 => true,
                   1 => true,
                   2 => true,
                   4 => true,
                   _ => false
               };
    }
}
