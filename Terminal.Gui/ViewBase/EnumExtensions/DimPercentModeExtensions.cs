#nullable enable

using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Terminal.Gui.ViewBase;

/// <summary>Extension methods for the <see cref="DimPercentMode"/> <see langword="enum"/> type.</summary>
[GeneratedCode ("Terminal.Gui.Analyzers.Internal", "1.0")]
[CompilerGenerated]
[DebuggerNonUserCode]
[ExcludeFromCodeCoverage (Justification = "Generated code is already tested.")]
[PublicAPI]
public static class DimPercentModeExtensions
{
    /// <summary>
    ///     Directly converts this <see cref="DimPercentMode"/> value to an <see langword="int"/> value with the
    ///     same binary representation.
    /// </summary>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static int AsInt32 (this DimPercentMode e) => Unsafe.As<DimPercentMode, int> (ref e);

    /// <summary>
    ///     Directly converts this <see cref="DimPercentMode"/> value to a <see langword="uint"/> value with the
    ///     same binary representation.
    /// </summary>
    /// <remarks>NO VALIDATION IS PERFORMED!</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static uint AsUInt32 (this DimPercentMode e) => Unsafe.As<DimPercentMode, uint> (ref e);

    /// <summary>
    ///     Determines if the specified <see langword="int"/> value is explicitly defined as a named value of the
    ///     <see cref="DimPercentMode"/> <see langword="enum"/> type.
    /// </summary>
    /// <remarks>
    ///     Only explicitly named values return true, as with IsDefined. Combined valid flag values of flags enums which are
    ///     not explicitly named will return false.
    /// </remarks>
    public static bool FastIsDefined (this DimPercentMode _, int value)
    {
        return value switch
               {
                   0 => true,
                   1 => true,
                   _ => false
               };
    }
}
