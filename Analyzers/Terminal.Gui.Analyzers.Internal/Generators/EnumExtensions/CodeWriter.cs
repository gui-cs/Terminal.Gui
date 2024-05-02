using System;
using System.CodeDom.Compiler;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Terminal.Gui.Analyzers.Internal.Constants;

namespace Terminal.Gui.Analyzers.Internal.Generators.EnumExtensions;

/// <summary>
///     The class responsible for turning an <see cref="EnumExtensionMethodsGenerationInfo"/>
///     into actual C# code.
/// </summary>
/// <remarks>Try to use this type as infrequently as possible.</remarks>
/// <param name="metadata">
///     A reference to an <see cref="IGeneratedTypeMetadata{TSelf}"/> which will be used
///     to generate the extension class code. The object will not be validated,
///     so it is critical that it be correct and remain unchanged while in use
///     by an instance of this class. Behavior if those rules are not followed
///     is undefined.
/// </param>
[SuppressMessage ("CodeQuality", "IDE0079", Justification = "Suppressions here are intentional and the warnings they disable are just noise.")]
internal sealed class CodeWriter (in EnumExtensionMethodsGenerationInfo metadata) : IStandardCSharpCodeGenerator<EnumExtensionMethodsGenerationInfo>
{
    // Using the null suppression operator here because this will always be
    // initialized to non-null before a reference to it is returned.
    private SourceText _sourceText = null!;

    /// <inheritdoc/>
    public EnumExtensionMethodsGenerationInfo Metadata
    {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        [return: NotNull]
        get;
        [param: DisallowNull]
        set;
    } = metadata;

    /// <inheritdoc/>
    public ref readonly SourceText GenerateSourceText (Encoding? encoding = null)
    {
        encoding ??= Encoding.UTF8;
        _sourceText = SourceText.From (GetFullSourceText (), encoding);

        return ref _sourceText;
    }

    /// <summary>
    ///     Gets the using directive for the namespace containing the enum,
    ///     if different from the extension class namespace, or an empty string, if they are the same.
    /// </summary>
    private string EnumNamespaceUsingDirective => Metadata.TargetTypeNamespace != Metadata.GeneratedTypeNamespace

                                                      // ReSharper disable once HeapView.ObjectAllocation
                                                      ? $"using {Metadata.TargetTypeNamespace};"
                                                      : string.Empty;

    private string EnumTypeKeyword => Metadata.EnumBackingTypeCode switch
                                      {
                                          TypeCode.Int32 => "int",
                                          TypeCode.UInt32 => "uint",
                                          _ => string.Empty
                                      };

    /// <summary>Gets the class declaration line.</summary>
    private string ExtensionClassDeclarationLine => $"public static partial class {Metadata.GeneratedTypeName}";

    // ReSharper disable once HeapView.ObjectAllocation
    /// <summary>Gets the XmlDoc for the extension class declaration.</summary>
    private string ExtensionClassDeclarationXmlDoc =>
        $"/// <summary>Extension methods for the <see cref=\"{Metadata.TargetTypeFullName}\"/> <see langword=\"enum\" /> type.</summary>";

    // ReSharper disable once HeapView.ObjectAllocation
    /// <summary>Gets the extension class file-scoped namespace directive.</summary>
    private string ExtensionClassNamespaceDirective => $"namespace {Metadata.GeneratedTypeNamespace};";

    /// <summary>
    ///     An attribute to decorate the extension class with for easy mapping back to the target enum type, for reflection and
    ///     analysis.
    /// </summary>
    private string ExtensionsForTypeAttributeLine => $"[ExtensionsForEnumType<{Metadata.TargetTypeFullName}>]";

    /// <summary>
    ///     Creates the code for the FastHasFlags method.
    /// </summary>
    /// <remarks>
    ///     Since the generator already only writes code for enums backed by <see langword="int"/> and <see langword="uint"/>,
    ///     this method is safe, as we'll always be using a DWORD.
    /// </remarks>
    /// <param name="w">An instance of an <see cref="IndentedTextWriter"/> to write to.</param>
    private void GetFastHasFlagsMethods (IndentedTextWriter w)
    {
        // The version taking the same enum type as the check value.
        w.WriteLine (
                     $"/// <summary>Determines if the specified flags are set in the current value of this <see cref=\"{Metadata.TargetTypeFullName}\" />.</summary>");
        w.WriteLine ("/// <remarks>NO VALIDATION IS PERFORMED!</remarks>");

        w.WriteLine (
                     $"/// <returns>True, if all flags present in <paramref name=\"checkFlags\" /> are also present in the current value of the <see cref=\"{Metadata.TargetTypeFullName}\" />.<br />Otherwise false.</returns>");
        w.WriteLine (Strings.DotnetNames.Attributes.Applications.AggressiveInlining);

        w.Push (
                $"{Metadata.Accessibility.ToCSharpString ()} static bool FastHasFlags (this {Metadata.TargetTypeFullName} e, {Metadata.TargetTypeFullName} checkFlags)");
        w.WriteLine ($"ref uint enumCurrentValueRef = ref Unsafe.As<{Metadata.TargetTypeFullName},uint> (ref e);");
        w.WriteLine ($"ref uint checkFlagsValueRef = ref Unsafe.As<{Metadata.TargetTypeFullName},uint> (ref checkFlags);");
        w.WriteLine ("return (enumCurrentValueRef & checkFlagsValueRef) == checkFlagsValueRef;");
        w.Pop ();

        // The version taking the underlying type of the enum as the check value.
        w.WriteLine (
                     $"/// <summary>Determines if the specified mask bits are set in the current value of this <see cref=\"{Metadata.TargetTypeFullName}\" />.</summary>");

        w.WriteLine (
                     $"/// <param name=\"e\">The <see cref=\"{Metadata.TargetTypeFullName}\" /> value to check against the <paramref name=\"mask\" /> value.</param>");
        w.WriteLine ("/// <param name=\"mask\">A mask to apply to the current value.</param>");

        w.WriteLine (
                     $"/// <returns>True, if all bits set to 1 in the mask are also set to 1 in the current value of the <see cref=\"{Metadata.TargetTypeFullName}\" />.<br />Otherwise false.</returns>");
        w.WriteLine ("/// <remarks>NO VALIDATION IS PERFORMED!</remarks>");
        w.WriteLine (Strings.DotnetNames.Attributes.Applications.AggressiveInlining);

        w.Push (
                $"{Metadata.Accessibility.ToCSharpString ()} static bool FastHasFlags (this {Metadata.TargetTypeFullName} e, {EnumTypeKeyword} mask)");
        w.WriteLine ($"ref {EnumTypeKeyword} enumCurrentValueRef = ref Unsafe.As<{Metadata.TargetTypeFullName},{EnumTypeKeyword}> (ref e);");
        w.WriteLine ("return (enumCurrentValueRef & mask) == mask;");
        w.Pop ();
    }

    /// <summary>
    ///     Creates the code for the FastIsDefined method.
    /// </summary>
    [SuppressMessage ("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault", Justification = "Only need to handle int and uint.")]
    [SuppressMessage ("ReSharper", "SwitchStatementMissingSomeEnumCasesNoDefault", Justification = "Only need to handle int and uint.")]
    private void GetFastIsDefinedMethod (IndentedTextWriter w)
    {
        w.WriteLine (
                     $"/// <summary>Determines if the specified <see langword=\"{EnumTypeKeyword}\" /> value is explicitly defined as a named value of the <see cref=\"{Metadata.TargetTypeFullName}\" /> <see langword=\"enum\" /> type.</summary>");

        w.WriteLine (
                     "/// <remarks>Only explicitly named values return true, as with IsDefined. Combined valid flag values of flags enums which are not explicitly named will return false.</remarks>");

        w.Push (
                $"{Metadata.Accessibility.ToCSharpString ()} static bool FastIsDefined (this {Metadata.TargetTypeFullName} e, {EnumTypeKeyword} value)");
        w.Push ("return value switch");

        switch (Metadata.EnumBackingTypeCode)
        {
            case TypeCode.Int32:
                foreach (int definedValue in Metadata.IntMembers)
                {
                    w.WriteLine ($"{definedValue:D} => true,");
                }

                break;
            case TypeCode.UInt32:
                foreach (uint definedValue in Metadata.UIntMembers)
                {
                    w.WriteLine ($"{definedValue:D} => true,");
                }

                break;
        }

        w.WriteLine ("_ => false");

        w.Pop ("};");
        w.Pop ();
    }

    private string GetFullSourceText ()
    {
        StringBuilder sb = new (
                                $"""
                                 {Strings.Templates.StandardHeader}

                                 [assembly: {Strings.AssemblyExtendedEnumTypeAttributeFullName} (typeof({Metadata.TargetTypeFullName}), typeof({Metadata.GeneratedTypeFullName}))]

                                 {EnumNamespaceUsingDirective}
                                 {ExtensionClassNamespaceDirective}
                                 {ExtensionClassDeclarationXmlDoc}
                                 {Strings.Templates.AttributesForGeneratedTypes}
                                 {ExtensionsForTypeAttributeLine}
                                 {ExtensionClassDeclarationLine}
                                 
                                 """,
                                4096);

        using IndentedTextWriter w = new (new StringWriter (sb));
        w.Push ();

        GetNamedValuesToInt32Method (w);
        GetNamedValuesToUInt32Method (w);

        if (Metadata.GenerateFastIsDefined)
        {
            GetFastIsDefinedMethod (w);
        }

        if (Metadata.GenerateFastHasFlags)
        {
            GetFastHasFlagsMethods (w);
        }

        w.Pop ();

        w.Flush ();

        return sb.ToString ();
    }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    private void GetNamedValuesToInt32Method (IndentedTextWriter w)
    {
        w.WriteLine (
                     $"/// <summary>Directly converts this <see cref=\"{Metadata.TargetTypeFullName}\" /> value to an <see langword=\"int\" /> value with the same binary representation.</summary>");
        w.WriteLine ("/// <remarks>NO VALIDATION IS PERFORMED!</remarks>");
        w.WriteLine (Strings.DotnetNames.Attributes.Applications.AggressiveInlining);
        w.Push ($"{Metadata.Accessibility.ToCSharpString ()} static int AsInt32 (this {Metadata.TargetTypeFullName} e)");
        w.WriteLine ($"return Unsafe.As<{Metadata.TargetTypeFullName},int> (ref e);");
        w.Pop ();
    }

    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    private void GetNamedValuesToUInt32Method (IndentedTextWriter w)
    {
        w.WriteLine (
                     $"/// <summary>Directly converts this <see cref=\"{Metadata.TargetTypeFullName}\" /> value to a <see langword=\"uint\" /> value with the same binary representation.</summary>");
        w.WriteLine ("/// <remarks>NO VALIDATION IS PERFORMED!</remarks>");
        w.WriteLine (Strings.DotnetNames.Attributes.Applications.AggressiveInlining);
        w.Push ($"{Metadata.Accessibility.ToCSharpString ()} static uint AsUInt32 (this {Metadata.TargetTypeFullName} e)");
        w.WriteLine ($"return Unsafe.As<{Metadata.TargetTypeFullName},uint> (ref e);");
        w.Pop ();
    }
}
