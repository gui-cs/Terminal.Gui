using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Terminal.Gui.Analyzers.Internal.Attributes;
using Terminal.Gui.Analyzers.Internal.Constants;

namespace Terminal.Gui.Analyzers.Internal.Generators.EnumExtensions;

/// <summary>
/// Implementation of <see cref="IIncrementalGenerator"/> for types decorated with <see cref="GenerateEnumMemberCombinationsAttribute"/>.
/// </summary>
[Generator]
internal sealed class EnumMemberCombinationsGenerator : IIncrementalGenerator
{
    private const string AttributeCodeText = $$"""
                                               {{Strings.Templates.StandardHeader}}
                                               
                                               namespace {{Strings.AnalyzersAttributesNamespace}};
                                               
                                               /// <summary>
                                               ///     Designates an enum member for inclusion in generation of bitwise combinations with other members decorated with
                                               ///     this attribute which have the same <see cref="{{nameof (GenerateEnumMemberCombinationsAttribute.GroupTag)}}"/> value.<br/>
                                               /// </summary>
                                               /// <remarks>
                                               ///     <para>
                                               ///         This attribute is only considered for enum types with the <see cref="{{nameof (GenerateEnumMemberCombinationsAttribute)}}"/>.
                                               ///     </para>
                                               ///     <para>
                                               ///         Masks with more than 8 bits set will
                                               ///     </para>
                                               /// </remarks>
                                               [AttributeUsageAttribute(AttributeTargets.Enum)]
                                               internal sealed class {{nameof (GenerateEnumMemberCombinationsAttribute)}} : System.Attribute
                                               {
                                                   public const byte MaximumPopCountLimit = 14;
                                                   private uint _mask;
                                                   private uint _maskPopCount;
                                                   private byte _popCountLimit = 8;
                                                   public required string GroupTag { get; set; }
                                               
                                                   public required uint Mask
                                                   {
                                                       get => _mask;
                                                       set
                                                       {
                                                           _maskPopCount = uint.PopCount(value);
                                               
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
                                                   ///     The maximum number of bits allowed to be set to 1 in <see cref="{{nameof (GenerateEnumMemberCombinationsAttribute.Mask)}}"/>.
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
                                                   ///         If the result of <see cref="uint.PopCount"/>(<see cref="{{nameof (GenerateEnumMemberCombinationsAttribute.Mask)}}"/>) exceeds 2 ^ <see cref="PopCountLimit"/>, no
                                                   ///         combinations will be generated for the members which otherwise would have been included by <see cref="{{nameof (GenerateEnumMemberCombinationsAttribute.Mask)}}"/>.
                                                   ///         Values exceeding the actual population count of <see cref="{{nameof (GenerateEnumMemberCombinationsAttribute.Mask)}}"/> have no effect.
                                                   ///     </para>
                                                   ///     <para>
                                                   ///         This option is set to a sane default of 8, but also has a hard-coded limit of 14 (16384 combinations), as a
                                                   ///         protection against generation of extremely large files.
                                                   ///     </para>
                                                   ///     <para>
                                                   ///         CAUTION: The maximum number of possible combinations possible is equal to 1 &lt;&lt;
                                                   ///         <see cref="uint.PopCount"/>(<see cref="{{nameof (GenerateEnumMemberCombinationsAttribute.Mask)}}"/>).
                                                   ///         See <see cref="MaximumPopCountLimit"/> for hard-coded limit,
                                                   ///     </para>
                                                   /// </remarks>
                                                   public byte PopCountLimit
                                                   {
                                                       get => _popCountLimit;
                                                       set
                                                       {
                                                           _maskPopCount = uint.PopCount(_mask);
                                               
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
                                               
                                                   internal bool MaximumPopCountLimitExceeded { get; private set; }
                                                   internal bool PopCountLimitExceeded { get; private set; }
                                               }

                                               """;

    private const string AttributeFullyQualifiedName = $"{Strings.AnalyzersAttributesNamespace}.{AttributeName}";
    private const string AttributeName = "GenerateEnumMemberCombinationsAttribute";

    /// <inheritdoc/>
    public void Initialize (IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput (GenerateAttributeCode);

        return;

        static void GenerateAttributeCode (IncrementalGeneratorPostInitializationContext initContext)
        {
#pragma warning disable IDE0061 // Use expression body for local function
            initContext.AddSource ($"{AttributeFullyQualifiedName}.g.cs", SourceText.From (AttributeCodeText, Encoding.UTF8));
#pragma warning restore IDE0061 // Use expression body for local function
        }
    }
}
