// ReSharper disable RedundantUsingDirective

using System;
using JetBrains.Annotations;
using Terminal.Gui.Analyzers.Internal.Compatibility;

namespace Terminal.Gui.Analyzers.Internal.Attributes;

/// <summary>
///     Designates an enum member for inclusion in generation of bitwise combinations with other members decorated with
///     this attribute which have the same <see cref="GroupTag"/> value.<br/>
/// </summary>
/// <remarks>
///     <para>
///         This attribute is only considered for enum types with the <see cref="GenerateEnumExtensionMethodsAttribute"/>.
///     </para>
/// </remarks>
[AttributeUsage (AttributeTargets.Enum)]
[UsedImplicitly]
public sealed class GenerateEnumMemberCombinationsAttribute : System.Attribute
{
    private const byte MaximumPopCountLimit = 14;
    private uint _mask;
    private uint _maskPopCount;
    private byte _popCountLimit = 8;
    /// <inheritdoc cref="CombinationGroupingAttribute.GroupTag" />
    public string GroupTag { get; set; }

    /// <summary>
    /// The mask for the group defined in <see cref="GroupTag"/>
    /// </summary>
    public uint Mask
    {
        get => _mask;
        set
        {
#if NET8_0_OR_GREATER
            _maskPopCount = uint.PopCount (value);
#else
            _maskPopCount = value.GetPopCount ();
#endif
            PopCountLimitExceeded = _maskPopCount > PopCountLimit;
            MaximumPopCountLimitExceeded = _maskPopCount > MaximumPopCountLimit;

            if (PopCountLimitExceeded || MaximumPopCountLimitExceeded)
            {
                return;
            }

            _mask = value;
        }
    }

    /// <summary>
    ///     The maximum number of bits allowed to be set to 1 in <see cref="Mask"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Default: 8 (256 possible combinations)
    ///     </para>
    ///     <para>
    ///         Increasing this value is not recommended!<br/>
    ///         Decreasing this value is pointless unless you want to limit maximum possible generated combinations even
    ///         further.
    ///     </para>
    ///     <para>
    ///         If the result of <see cref="NumericExtensions.GetPopCount(uint)"/>(<see cref="Mask"/>) exceeds 2 ^
    ///         <see cref="PopCountLimit"/>, no
    ///         combinations will be generated for the members which otherwise would have been included by <see cref="Mask"/>.
    ///         Values exceeding the actual population count of <see cref="Mask"/> have no effect.
    ///     </para>
    ///     <para>
    ///         This option is set to a sane default of 8, but also has a hard-coded limit of 14 (16384 combinations), as a
    ///         protection against generation of extremely large files.
    ///     </para>
    ///     <para>
    ///         CAUTION: The maximum number of possible combinations possible is equal to 1 &lt;&lt;
    ///         <see cref="NumericExtensions.GetPopCount(uint)"/>(<see cref="Mask"/>).
    ///         See <see cref="MaximumPopCountLimit"/> for hard-coded limit,
    ///     </para>
    /// </remarks>
    public byte PopCountLimit
    {
        get => _popCountLimit;
        set
        {
#if NET8_0_OR_GREATER
            _maskPopCount = uint.PopCount (_mask);
#else
            _maskPopCount = _mask.GetPopCount ();
#endif

            PopCountLimitExceeded = _maskPopCount > value;
            MaximumPopCountLimitExceeded = _maskPopCount > MaximumPopCountLimit;

            if (PopCountLimitExceeded || MaximumPopCountLimitExceeded)
            {
                return;
            }

            _mask = value;
            _popCountLimit = value;
        }
    }

    [UsedImplicitly]
    internal bool MaximumPopCountLimitExceeded { get; private set; }
    [UsedImplicitly]
    internal bool PopCountLimitExceeded { get; private set; }
}
