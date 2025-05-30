#nullable enable

using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Terminal.Gui.ViewBase;

/// <summary>Extension methods for the <see cref="BorderSettings"/> <see langword="enum"/> type.</summary>
[GeneratedCode ("Terminal.Gui.Analyzers.Internal", "1.0")]
[CompilerGenerated]
[DebuggerNonUserCode]
[ExcludeFromCodeCoverage (Justification = "Generated code is already tested.")]
[PublicAPI]
public static class BorderSettingsExtensions
{
    /// <summary>
    ///     Directly converts this <see cref="BorderSettings"/> value to an <see langword="int"/> value with the
    ///     same binary representation.
    /// </summary>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static int AsInt32 (this BorderSettings e) => Unsafe.As<BorderSettings, int> (ref e);

    /// <summary>
    ///     Directly converts this <see cref="BorderSettings"/> value to a <see langword="uint"/> value with the
    ///     same binary representation.
    /// </summary>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static uint AsUInt32 (this BorderSettings e) => Unsafe.As<BorderSettings, uint> (ref e);

    /// <summary>
    ///     Determines if the specified flags are set in the current value of this
    ///     <see cref="BorderSettings"/>.
    /// </summary>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    /// <returns>
    ///     True, if all flags present in <paramref name="checkFlags"/> are also present in the current value of the
    ///     <see cref="BorderSettings"/>.<br/>Otherwise false.
    /// </returns>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool FastHasFlags (this BorderSettings e, BorderSettings checkFlags)
    {
        ref uint enumCurrentValueRef = ref Unsafe.As<BorderSettings, uint> (ref e);
        ref uint checkFlagsValueRef = ref Unsafe.As<BorderSettings, uint> (ref checkFlags);

        return (enumCurrentValueRef & checkFlagsValueRef) == checkFlagsValueRef;
    }

    /// <summary>
    ///     Determines if the specified mask bits are set in the current value of this
    ///     <see cref="BorderSettings"/>.
    /// </summary>
    /// <param name="e">The <see cref="BorderSettings"/> value to check against the <paramref name="mask"/> value.</param>
    /// <param name="mask">A mask to apply to the current value.</param>
    /// <returns>
    ///     True, if all bits set to 1 in the mask are also set to 1 in the current value of the
    ///     <see cref="BorderSettings"/>.<br/>Otherwise false.
    /// </returns>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static bool FastHasFlags (this BorderSettings e, int mask)
    {
        ref int enumCurrentValueRef = ref Unsafe.As<BorderSettings, int> (ref e);

        return (enumCurrentValueRef & mask) == mask;
    }

    /// <summary>
    ///     Determines if the specified <see langword="int"/> value is explicitly defined as a named value of the
    ///     <see cref="BorderSettings"/> <see langword="enum"/> type.
    /// </summary>
    /// <remarks>
    ///     Only explicitly named values return true, as with IsDefined. Combined valid flag values of flags enums which are
    ///     not explicitly named will return false.
    /// </remarks>
    public static bool FastIsDefined (this BorderSettings _, int value)
    {
        return value switch
               {
                   0 => true,
                   1 => true,
                   2 => true,
                   _ => false
               };
    }
}
