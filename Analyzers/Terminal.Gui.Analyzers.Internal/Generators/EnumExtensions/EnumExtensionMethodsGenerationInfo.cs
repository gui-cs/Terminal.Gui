using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Terminal.Gui.Analyzers.Internal.Attributes;
using Terminal.Gui.Analyzers.Internal.Constants;

namespace Terminal.Gui.Analyzers.Internal.Generators.EnumExtensions;

/// <summary>
///     Type containing the information necessary to generate code according to the declared attribute values,
///     as well as the actual code to create the corresponding source code text, to be used in the
///     source generator pipeline.
/// </summary>
/// <remarks>
///     Minimal validation is performed by this type.<br/>
///     Errors in analyzed source code will result in generation failure or broken output.<br/>
///     This type is not intended for use outside of Terminal.Gui library development.
/// </remarks>
internal sealed record EnumExtensionMethodsGenerationInfo : IGeneratedTypeMetadata<EnumExtensionMethodsGenerationInfo>,
                                                            IEqualityOperators<EnumExtensionMethodsGenerationInfo, EnumExtensionMethodsGenerationInfo, bool>
{
    private const int ExplicitFastHasFlagsMask  = 0b_0100;
    private const int ExplicitFastIsDefinedMask = 0b_1000;
    private const int ExplicitNameMask          = 0b_0010;
    private const int ExplicitNamespaceMask     = 0b_0001;
    private const string GeneratorAttributeFullyQualifiedName = $"{GeneratorAttributeNamespace}.{GeneratorAttributeName}";
    private const string GeneratorAttributeName = nameof (GenerateEnumExtensionMethodsAttribute);
    private const string GeneratorAttributeNamespace = Strings.AnalyzersAttributesNamespace;

    /// <summary>
    ///     Type containing the information necessary to generate code according to the declared attribute values,
    ///     as well as the actual code to create the corresponding source code text, to be used in the
    ///     source generator pipeline.
    /// </summary>
    /// <param name="enumNamespace">The fully-qualified namespace of the enum type, without assembly name.</param>
    /// <param name="enumTypeName">
    ///     The name of the enum type, as would be given by <see langword="nameof"/> on the enum's type
    ///     declaration.
    /// </param>
    /// <param name="typeNamespace">
    ///     The fully-qualified namespace in which to place the generated code, without assembly name. If omitted or explicitly
    ///     null, uses the value provided in <paramref name="enumNamespace"/>.
    /// </param>
    /// <param name="typeName">
    ///     The name of the generated class. If omitted or explicitly null, appends "Extensions" to the value of
    ///     <paramref name="enumTypeName"/>.
    /// </param>
    /// <param name="enumBackingTypeCode">The backing type of the enum. Defaults to <see cref="int"/>.</param>
    /// <param name="generateFastHasFlags">
    ///     Whether to generate a fast HasFlag alternative. (Default: true) Ignored if the enum does not also have
    ///     <see cref="FlagsAttribute"/>.
    /// </param>
    /// <param name="generateFastIsDefined">Whether to generate a fast IsDefined alternative. (Default: true)</param>
    /// <remarks>
    ///     Minimal validation is performed by this type.<br/>
    ///     Errors in analyzed source code will result in generation failure or broken output.<br/>
    ///     This type is not intended for use outside of Terminal.Gui library development.
    /// </remarks>
    public EnumExtensionMethodsGenerationInfo (
        string enumNamespace,
        string enumTypeName,
        string? typeNamespace = null,
        string? typeName = null,
        TypeCode enumBackingTypeCode = TypeCode.Int32,
        bool generateFastHasFlags = true,
        bool generateFastIsDefined = true
    ) : this (enumNamespace, enumTypeName, enumBackingTypeCode)
    {
        GeneratedTypeNamespace = typeNamespace ?? enumNamespace;
        GeneratedTypeName = typeName ?? string.Concat (enumTypeName, Strings.DefaultTypeNameSuffix);
        GenerateFastHasFlags = generateFastHasFlags;
        GenerateFastIsDefined = generateFastIsDefined;
    }

    public EnumExtensionMethodsGenerationInfo (string enumNamespace, string enumTypeName, TypeCode enumBackingType)
    {
        // Interning these since they're rather unlikely to change.
        string enumInternedNamespace = string.Intern (enumNamespace);
        string enumInternedName = string.Intern (enumTypeName);
        TargetTypeNamespace = enumInternedNamespace;
        TargetTypeName = enumInternedName;
        EnumBackingTypeCode = enumBackingType;
    }

    [AccessedThroughProperty (nameof (EnumBackingTypeCode))]
    private readonly TypeCode _enumBackingTypeCode;

    [AccessedThroughProperty (nameof (GeneratedTypeName))]
    private string? _generatedTypeName;

    [AccessedThroughProperty (nameof (GeneratedTypeNamespace))]
    private string? _generatedTypeNamespace;

    private BitVector32 _discoveredProperties = new (0);

    /// <summary>The name of the extension class.</summary>
    public string? GeneratedTypeName
    {
        get => _generatedTypeName ?? string.Concat (TargetTypeName, Strings.DefaultTypeNameSuffix);
        set => _generatedTypeName = value ?? string.Concat (TargetTypeName, Strings.DefaultTypeNameSuffix);
    }

    /// <summary>The namespace for the extension class.</summary>
    /// <remarks>
    ///     Value is not validated by the set accessor.<br/>
    ///     Get accessor will never return null and is thus marked [NotNull] for static analysis, even though the property is
    ///     declared as a nullable <see langword="string?"/>.<br/>If the backing field for this property is null, the get
    ///     accessor will return <see cref="TargetTypeNamespace"/> instead.
    /// </remarks>
    public string? GeneratedTypeNamespace
    {
        get => _generatedTypeNamespace ?? TargetTypeNamespace;
        set => _generatedTypeNamespace = value ?? TargetTypeNamespace;
    }

    /// <inheritdoc/>
    public string TargetTypeFullName => string.Concat (TargetTypeNamespace, ".", TargetTypeName);

    /// <inheritdoc/>
    public Accessibility Accessibility
    {
        get;
        [UsedImplicitly]
        internal set;
    } = Accessibility.Public;

    /// <inheritdoc/>
    public TypeKind TypeKind => TypeKind.Class;

    /// <inheritdoc/>
    public bool IsRecord => false;

    /// <inheritdoc/>
    public bool IsClass => true;

    /// <inheritdoc/>
    public bool IsStruct => false;

    /// <inheritdoc/>
    public bool IsByRefLike => false;

    /// <inheritdoc/>
    public bool IsSealed => false;

    /// <inheritdoc/>
    public bool IsAbstract => false;

    /// <inheritdoc/>
    public bool IsEnum => false;

    /// <inheritdoc/>
    public bool IsStatic => true;

    /// <inheritdoc/>
    public bool IncludeInterface => false;

    public string GeneratedTypeFullName => $"{GeneratedTypeNamespace}.{GeneratedTypeName}";

    /// <summary>Whether to generate the extension class as partial (Default: true)</summary>
    public bool IsPartial => true;

    /// <summary>The fully-qualified namespace of the source enum type.</summary>
    public string TargetTypeNamespace
    {
        get;
        [UsedImplicitly]
        set;
    }

    /// <summary>The UNQUALIFIED name of the source enum type.</summary>
    public string TargetTypeName
    {
        get;
        [UsedImplicitly]
        set;
    }

    /// <summary>
    ///     The backing type for the enum.
    /// </summary>
    /// <remarks>For simplicity and formality, only System.Int32 and System.UInt32 are supported at this time.</remarks>
    public TypeCode EnumBackingTypeCode
    {
        get => _enumBackingTypeCode;
        init
        {
            if (value is not TypeCode.Int32 and not TypeCode.UInt32)
            {
                throw new NotSupportedException ("Only System.Int32 and System.UInt32 are supported at this time.");
            }

            _enumBackingTypeCode = value;
        }
    }

    /// <summary>
    ///     Whether a fast alternative to the built-in Enum.HasFlag method will be generated (Default: false)
    /// </summary>
    public bool GenerateFastHasFlags { [UsedImplicitly] get; set; }

    /// <summary>Whether a switch-based IsDefined replacement will be generated (Default: true)</summary>
    public bool GenerateFastIsDefined { [UsedImplicitly]get; set; } = true;

    internal ImmutableHashSet<int>? _intMembers;
    internal ImmutableHashSet<uint>? _uIntMembers;

    /// <summary>
    ///     Fully-qualified name of the extension class
    /// </summary>
    internal string FullyQualifiedClassName => $"{GeneratedTypeNamespace}.{GeneratedTypeName}";

    /// <summary>
    ///     Whether a Flags was found on the enum type.
    /// </summary>
    internal bool HasFlagsAttribute {[UsedImplicitly] get; set; }

    private static readonly SymbolDisplayFormat FullyQualifiedSymbolDisplayFormatWithoutGlobal =
        SymbolDisplayFormat.FullyQualifiedFormat
                           .WithGlobalNamespaceStyle (
                                                      SymbolDisplayGlobalNamespaceStyle.Omitted);


    internal bool TryConfigure (INamedTypeSymbol enumSymbol, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
        cts.Token.ThrowIfCancellationRequested ();

        ImmutableArray<AttributeData> attributes = enumSymbol.GetAttributes ();

        // This is theoretically impossible, but guarding just in case and canceling if it does happen.
        if (attributes.Length == 0)
        {
            cts.Cancel (true);

            return false;
        }

        // Check all attributes provided for anything interesting.
        // Attributes can be in any order, so just check them all and adjust at the end if necessary.
        // Note that we do not perform as strict validation on actual usage of the attribute, at this stage,
        // because the analyzer should have already thrown errors for invalid uses like global namespace
        // or unsupported enum underlying types.
        foreach (AttributeData attr in attributes)
        {
            cts.Token.ThrowIfCancellationRequested ();
            string? attributeFullyQualifiedName = attr.AttributeClass?.ToDisplayString (FullyQualifiedSymbolDisplayFormatWithoutGlobal);

            // Skip if null or not possibly an attribute we care about
            if (attributeFullyQualifiedName is null or not { Length: >= 5 })
            {
                continue;
            }

            switch (attributeFullyQualifiedName)
            {
                // For Flags enums
                case Strings.DotnetNames.Attributes.Flags:
                {
                    HasFlagsAttribute = true;
                }

                    continue;

                // For the attribute that started this whole thing
                case GeneratorAttributeFullyQualifiedName:

                {
                    // If we can't successfully complete this method,
                    // something is wrong enough that we may as well just stop now.
                    if (!TryConfigure (attr, cts.Token))
                    {
                        if (cts.Token.CanBeCanceled)
                        {
                            cts.Cancel ();
                        }

                        return false;
                    }
                }

                    continue;
            }
        }

        // Now get the members, if we know we'll need them.
        if (GenerateFastIsDefined || GenerateFastHasFlags)
        {
            if (EnumBackingTypeCode == TypeCode.Int32)
            {
                PopulateIntMembersHashSet (enumSymbol);
            }
            else if (EnumBackingTypeCode == TypeCode.UInt32)
            {
                PopulateUIntMembersHashSet (enumSymbol);
            }
        }

        return true;
    }

    private void PopulateIntMembersHashSet (INamedTypeSymbol enumSymbol)
    {
        ImmutableArray<ISymbol> enumMembers = enumSymbol.GetMembers ();
        IEnumerable<IFieldSymbol> fieldSymbols = enumMembers.OfType<IFieldSymbol> ();
        _intMembers = fieldSymbols.Select (static m => m.HasConstantValue ? (int)m.ConstantValue : 0).ToImmutableHashSet ();
    }
    private void PopulateUIntMembersHashSet (INamedTypeSymbol enumSymbol)
    {
        _uIntMembers = enumSymbol.GetMembers ().OfType<IFieldSymbol> ().Select (static m => (uint)m.ConstantValue).ToImmutableHashSet ();
    }

    private bool HasExplicitFastHasFlags
    {
        [UsedImplicitly]get => _discoveredProperties [ExplicitFastHasFlagsMask];
        set => _discoveredProperties [ExplicitFastHasFlagsMask] = value;
    }

    private bool HasExplicitFastIsDefined
    {
        [UsedImplicitly]get => _discoveredProperties [ExplicitFastIsDefinedMask];
        set => _discoveredProperties [ExplicitFastIsDefinedMask] = value;
    }

    private bool HasExplicitTypeName
    {
        get => _discoveredProperties [ExplicitNameMask];
        set => _discoveredProperties [ExplicitNameMask] = value;
    }

    private bool HasExplicitTypeNamespace
    {
        get => _discoveredProperties [ExplicitNamespaceMask];
        set => _discoveredProperties [ExplicitNamespaceMask] = value;
    }

    [MemberNotNullWhen (true, nameof (_generatedTypeName), nameof (_generatedTypeNamespace))]
    private bool TryConfigure (AttributeData attr, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
        cts.Token.ThrowIfCancellationRequested ();

        if (attr is not { NamedArguments.Length: > 0 })
        {
            // Just a naked attribute, so configure with appropriate defaults.
            GeneratedTypeNamespace = TargetTypeNamespace;
            GeneratedTypeName = string.Concat (TargetTypeName, Strings.DefaultTypeNameSuffix);

            return true;
        }

        cts.Token.ThrowIfCancellationRequested ();

        foreach (KeyValuePair<string, TypedConstant> kvp in attr.NamedArguments)
        {
            string? propName = kvp.Key;
            TypedConstant propValue = kvp.Value;

            cts.Token.ThrowIfCancellationRequested ();

            // For every property name and value pair, set associated metadata
            // property, if understood.
            switch (propName, propValue)
            {
                // Null or empty string doesn't make sense, so skip if it happens.
                case (null, _):
                case ("", _):
                    continue;

                // ClassName is specified, not explicitly null, and at least 1 character long.
                case (AttributeProperties.TypeNamePropertyName, { IsNull: false, Value: string { Length: > 1 } classNameProvidedValue }):
                    if (string.IsNullOrWhiteSpace (classNameProvidedValue))
                    {
                        return false;
                    }

                    GeneratedTypeName = classNameProvidedValue;
                    HasExplicitTypeName = true;

                    continue;

                // Class namespace is specified, not explicitly null, and at least 1 character long.
                case (AttributeProperties.TypeNamespacePropertyName, { IsNull: false, Value: string { Length: > 1 } classNamespaceProvidedValue }):

                    if (string.IsNullOrWhiteSpace (classNamespaceProvidedValue))
                    {
                        return false;
                    }

                    GeneratedTypeNamespace = classNamespaceProvidedValue;
                    HasExplicitTypeNamespace = true;

                    continue;

                // FastHasFlags is specified
                case (AttributeProperties.FastHasFlagsPropertyName, { IsNull: false } fastHasFlagsConstant):
                    GenerateFastHasFlags = fastHasFlagsConstant.Value is true;
                    HasExplicitFastHasFlags = true;

                    continue;

                // FastIsDefined is specified
                case (AttributeProperties.FastIsDefinedPropertyName, { IsNull: false } fastIsDefinedConstant):
                    GenerateFastIsDefined = fastIsDefinedConstant.Value is true;
                    HasExplicitFastIsDefined = true;

                    continue;
            }
        }

        // The rest is simple enough it's not really worth worrying about cancellation, so don't bother from here on...

        // Configure anything that wasn't specified that doesn't have an implicitly safe default
        if (!HasExplicitTypeName || _generatedTypeName is null)
        {
            _generatedTypeName = string.Concat (TargetTypeName, Strings.DefaultTypeNameSuffix);
        }

        if (!HasExplicitTypeNamespace || _generatedTypeNamespace is null)
        {
            _generatedTypeNamespace = TargetTypeNamespace;
        }

        if (!HasFlagsAttribute)
        {
            GenerateFastHasFlags = false;
        }

        return true;
    }

    private static class AttributeProperties
    {
        internal const string FastHasFlagsPropertyName = nameof (GenerateEnumExtensionMethodsAttribute.FastHasFlags);
        internal const string FastIsDefinedPropertyName = nameof (GenerateEnumExtensionMethodsAttribute.FastIsDefined);
        internal const string TypeNamePropertyName = nameof (GenerateEnumExtensionMethodsAttribute.ClassName);
        internal const string TypeNamespacePropertyName = nameof (GenerateEnumExtensionMethodsAttribute.ClassNamespace);
    }
}
