// ReSharper disable RedundantNullableDirective
// ReSharper disable RedundantUsingDirective
// ReSharper disable ClassNeverInstantiated.Global

#nullable enable
using System;
using Attribute = System.Attribute;
using AttributeUsageAttribute = System.AttributeUsageAttribute;
using AttributeTargets = System.AttributeTargets;

namespace Terminal.Gui.Analyzers.Internal.Attributes;

/// <summary>
///     Used to enable source generation of a common set of extension methods for enum types.
/// </summary>
[AttributeUsage (AttributeTargets.Enum)]
internal sealed class GenerateEnumExtensionMethodsAttribute : Attribute
{
    /// <summary>
    ///     The name of the generated static class.
    /// </summary>
    /// <remarks>
    ///     If unspecified, null, empty, or only whitespace, defaults to the name of the enum plus "Extensions".<br/>
    ///     No other validation is performed, so illegal values will simply result in compiler errors.
    ///     <para>
    ///         Explicitly specifying a default value is unnecessary and will result in unnecessary processing.
    ///     </para>
    /// </remarks>
    public string? ClassName { get; set; }

    /// <summary>
    ///     The namespace in which to place the generated static class containing the extension methods.
    /// </summary>
    /// <remarks>
    ///     If unspecified, null, empty, or only whitespace, defaults to the namespace of the enum.<br/>
    ///     No other validation is performed, so illegal values will simply result in compiler errors.
    ///     <para>
    ///         Explicitly specifying a default value is unnecessary and will result in unnecessary processing.
    ///     </para>
    /// </remarks>
    public string? ClassNamespace { get; set; }

    /// <summary>
    ///     Whether to generate a fast, zero-allocation, non-boxing, and reflection-free alternative to the built-in
    ///     <see cref="Enum.HasFlag"/> method.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default: false
    ///     </para>
    ///     <para>
    ///         If the enum is not decorated with <see cref="FlagsAttribute"/>, this option has no effect.
    ///     </para>
    ///     <para>
    ///         If multiple members have the same value, the first member with that value will be used and subsequent members
    ///         with the same value will be skipped.
    ///     </para>
    ///     <para>
    ///         Overloads taking the enum type itself as well as the underlying type of the enum will be generated, enabling
    ///         avoidance of implicit or explicit cast overhead.
    ///     </para>
    ///     <para>
    ///         Explicitly specifying a default value is unnecessary and will result in unnecessary processing.
    ///     </para>
    /// </remarks>
    public bool FastHasFlags { get; set; }

    /// <summary>
    ///     Whether to generate a fast, zero-allocation, and reflection-free alternative to the built-in
    ///     <see cref="Enum.IsDefined"/> method,
    ///     using a switch expression as a hard-coded reverse mapping of numeric values to explicitly-named members.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default: true
    ///     </para>
    ///     <para>
    ///         If multiple members have the same value, the first member with that value will be used and subsequent members
    ///         with the same value will be skipped.
    ///     </para>
    ///     <para>
    ///         As with <see cref="Enum.IsDefined"/> the source generator only considers explicitly-named members.<br/>
    ///         Generation of values which represent valid bitwise combinations of members of enums decorated with
    ///         <see cref="FlagsAttribute"/> is not affected by this property.
    ///     </para>
    /// </remarks>
    public bool FastIsDefined { get; init; } = true;

    /// <summary>
    ///     Gets a <see langword="bool"/> value indicating if this <see cref="GenerateEnumExtensionMethodsAttribute"/> instance
    ///     contains default values only. See <see href="#remarks">remarks</see> of this method or documentation on properties of this type for details.
    /// </summary>
    /// <returns>
    ///     A <see langword="bool"/> value indicating if all property values are default for this
    ///     <see cref="GenerateEnumExtensionMethodsAttribute"/> instance.
    /// </returns>
    /// <remarks>
    ///     Default values that will result in a <see langword="true"/> return value are:<br/>
    ///     <see cref="FastIsDefined"/> &amp;&amp; !<see cref="FastHasFlags"/> &amp;&amp; <see cref="ClassName"/>
    ///     <see langword="is"/> <see langword="null"/> &amp;&amp; <see cref="ClassNamespace"/> <see langword="is"/>
    ///     <see langword="null"/>
    /// </remarks>
    public override bool IsDefaultAttribute ()
    {
        return FastIsDefined
               && !FastHasFlags
               && ClassName is null
               && ClassNamespace is null;
    }
}
