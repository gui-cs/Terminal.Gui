using System;
using JetBrains.Annotations;

namespace Terminal.Gui.Analyzers.Internal.Attributes;

/// <summary>
///     Designates an enum member for inclusion in generation of bitwise combinations with other members decorated with
///     this attribute which have the same <see cref="GroupTag"/> value.<br/>
/// </summary>
/// <remarks>
///     This attribute is only considered for members of enum types which have the
///     <see cref="GenerateEnumExtensionMethodsAttribute"/>.
/// </remarks>
[AttributeUsage (AttributeTargets.Field)]
[UsedImplicitly]
internal sealed class CombinationGroupingAttribute : Attribute
{
    /// <summary>
    ///     Name of a group this member participates in, for FastHasFlags.
    /// </summary>
    public string GroupTag { get; set; }
}
