// ReSharper disable RedundantNameQualifier
// ReSharper disable RedundantNullableDirective
// ReSharper disable UnusedType.Global
#pragma warning disable IDE0001, IDE0240
#nullable enable

namespace Terminal.Gui.Analyzers.Internal.Attributes;

/// <summary>
///     Attribute written by the source generator for <see langword="enum" /> extension classes, for easier analysis and reflection.
/// </summary>
/// <remarks>
///     Properties are just convenient shortcuts to properties of <typeparamref name="TEnum"/>.
/// </remarks>
[System.AttributeUsage (System.AttributeTargets.Class | System.AttributeTargets.Interface)]
internal sealed class ExtensionsForEnumTypeAttribute<TEnum>: System.Attribute, IExtensionsForEnumTypeAttributes where TEnum : struct, System.Enum
{
    /// <summary>
    ///     The namespace-qualified name of <typeparamref name="TEnum"/>.
    /// </summary>
    public string EnumFullName => EnumType.FullName!;

    /// <summary>
    ///     The unqualified name of <typeparamref name="TEnum"/>.
    /// </summary>
    public string EnumName => EnumType.Name;

    /// <summary>
    ///     The namespace containing <typeparamref name="TEnum"/>.
    /// </summary>
    public string EnumNamespace => EnumType.Namespace!;

    /// <summary>
    ///     The <see cref="System.Type"/> given by <see langword="typeof"/>(<typeparamref name="TEnum"/>).
    /// </summary>
    public System.Type EnumType => typeof (TEnum);
}
