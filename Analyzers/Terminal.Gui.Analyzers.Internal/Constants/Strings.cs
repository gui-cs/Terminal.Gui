// ReSharper disable MemberCanBePrivate.Global

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.Analyzers.Internal.Attributes;

namespace Terminal.Gui.Analyzers.Internal.Constants;

/// <summary>String constants for frequently-used boilerplate.</summary>
/// <remarks>These are for performance, instead of using Roslyn to build it all during execution of analyzers.</remarks>
internal static class Strings
{
    internal const string AnalyzersAttributesNamespace = $"{InternalAnalyzersNamespace}.Attributes";

    internal const string AssemblyExtendedEnumTypeAttributeFullName = $"{AnalyzersAttributesNamespace}.{nameof (AssemblyExtendedEnumTypeAttribute)}";

    internal const string DefaultTypeNameSuffix = "Extensions";

    internal const string FallbackClassNamespace = $"{TerminalGuiRootNamespace}";

    internal const string InternalAnalyzersNamespace = $"{AnalyzersRootNamespace}.Internal";

    internal const string TerminalGuiRootNamespace = "Terminal.Gui";

    private const string AnalyzersRootNamespace = $"{TerminalGuiRootNamespace}.Analyzers";
    private const string NetStandard20CompatibilityNamespace = $"{InternalAnalyzersNamespace}.Compatibility";

    /// <summary>
    ///     Names of dotnet namespaces and types. Included as compile-time constants to avoid unnecessary work for the Roslyn
    ///     source generators.
    /// </summary>
    /// <remarks>Implemented as nested static types because XmlDoc doesn't work on namespaces.</remarks>
    internal static class DotnetNames
    {
        /// <summary>Fully-qualified attribute type names. Specific applications (uses) are in <see cref="Applications"/>.</summary>
        internal static class Attributes
        {
            /// <inheritdoc cref="CompilerGeneratedAttribute"/>
            internal const string CompilerGenerated = $"{Namespaces.System_Runtime_CompilerServices}.{nameof (CompilerGeneratedAttribute)}";

            /// <inheritdoc cref="DebuggerNonUserCodeAttribute"/>
            internal const string DebuggerNonUserCode = $"{Namespaces.System_Diagnostics}.{nameof (DebuggerNonUserCodeAttribute)}";

            /// <inheritdoc cref="ExcludeFromCodeCoverageAttribute"/>
            internal const string ExcludeFromCodeCoverage = $"{Namespaces.System_Diagnostics_CodeAnalysis}.{nameof (ExcludeFromCodeCoverageAttribute)}";

            internal const string Flags = $"{Namespaces.SystemNs}.{nameof (FlagsAttribute)}";

            internal const string GeneratedCode = $"{Namespaces.System_CodeDom_Compiler}.{nameof (GeneratedCodeAttribute)}";

            /// <inheritdoc cref="MethodImplOptions.AggressiveInlining"/>
            /// <remarks>Use of this attribute should be carefully evaluated.</remarks>
            internal const string MethodImpl = $"{Namespaces.System_Runtime_CompilerServices}.{nameof (MethodImplAttribute)}";

            /// <summary>Attributes formatted for use in code, including square brackets.</summary>
            internal static class Applications
            {
                // ReSharper disable MemberHidesStaticFromOuterClass
                internal const string Flags = $"[{Attributes.Flags}]";

                /// <inheritdoc cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/>
                internal const string GeneratedCode = $"""[{Attributes.GeneratedCode}("{InternalAnalyzersNamespace}","1.0")]""";

                /// <inheritdoc cref="MethodImplOptions.AggressiveInlining"/>
                /// <remarks>Use of this attribute should be carefully evaluated.</remarks>
                internal const string AggressiveInlining = $"[{MethodImpl}({Types.MethodImplOptions}.{nameof (MethodImplOptions.AggressiveInlining)})]";

                /// <inheritdoc cref="DebuggerNonUserCodeAttribute"/>
                internal const string DebuggerNonUserCode = $"[{Attributes.DebuggerNonUserCode}]";

                /// <inheritdoc cref="CompilerGeneratedAttribute"/>
                internal const string CompilerGenerated = $"[{Attributes.CompilerGenerated}]";

                /// <inheritdoc cref="ExcludeFromCodeCoverageAttribute"/>
                internal const string ExcludeFromCodeCoverage = $"[{Attributes.ExcludeFromCodeCoverage}]";

                // ReSharper restore MemberHidesStaticFromOuterClass
            }
        }

        /// <summary>Names of dotnet namespaces.</summary>
        internal static class Namespaces
        {
            internal const string SystemNs = nameof (System);
            // ReSharper disable InconsistentNaming
            internal const string System_CodeDom = $"{SystemNs}.{nameof (System.CodeDom)}";
            internal const string System_CodeDom_Compiler = $"{System_CodeDom}.{nameof (System.CodeDom.Compiler)}";
            internal const string System_ComponentModel = $"{SystemNs}.{nameof (System.ComponentModel)}";
            internal const string System_Diagnostics = $"{SystemNs}.{nameof (System.Diagnostics)}";
            internal const string System_Diagnostics_CodeAnalysis = $"{System_Diagnostics}.{nameof (System.Diagnostics.CodeAnalysis)}";
            internal const string System_Numerics = $"{SystemNs}.{nameof (System.Numerics)}";
            internal const string System_Runtime = $"{SystemNs}.{nameof (System.Runtime)}";
            internal const string System_Runtime_CompilerServices = $"{System_Runtime}.{nameof (System.Runtime.CompilerServices)}";
            // ReSharper restore InconsistentNaming
        }

        internal static class Types
        {
            internal const string Attribute = $"{Namespaces.SystemNs}.{nameof (System.Attribute)}";
            internal const string AttributeTargets = $"{Namespaces.SystemNs}.{nameof (System.AttributeTargets)}";
            internal const string AttributeUsageAttribute = $"{Namespaces.SystemNs}.{nameof (System.AttributeUsageAttribute)}";

            internal const string MethodImplOptions =
                $"{Namespaces.System_Runtime_CompilerServices}.{nameof (System.Runtime.CompilerServices.MethodImplOptions)}";
        }
    }

    internal static class Templates
    {
        internal const string AutoGeneratedCommentBlock = $"""
                                                           //------------------------------------------------------------------------------
                                                           // <auto-generated>
                                                           //   This file and the code it contains was generated by a source generator in
                                                           //   the {InternalAnalyzersNamespace} library.
                                                           //
                                                           //   Modifications to this file are not supported and will be lost when
                                                           //   source generation is triggered, either implicitly or explicitly.
                                                           // </auto-generated>
                                                           //------------------------------------------------------------------------------
                                                           """;

        /// <summary>
        ///     A set of explicit type aliases to work around Terminal.Gui having name collisions with types like
        ///     <see cref="System.Attribute"/>.
        /// </summary>
        internal const string DotnetExplicitTypeAliasUsingDirectives = $"""
                                                                        using Attribute = {DotnetNames.Types.Attribute};
                                                                        using AttributeUsageAttribute = {DotnetNames.Types.AttributeUsageAttribute};
                                                                        using GeneratedCode = {DotnetNames.Attributes.GeneratedCode};
                                                                        """;

        /// <summary>Using directives for common namespaces in generated code.</summary>
        internal const string DotnetNamespaceUsingDirectives = $"""
                                                                using {DotnetNames.Namespaces.SystemNs};
                                                                using {DotnetNames.Namespaces.System_CodeDom};
                                                                using {DotnetNames.Namespaces.System_CodeDom_Compiler};
                                                                using {DotnetNames.Namespaces.System_ComponentModel};
                                                                using {DotnetNames.Namespaces.System_Numerics};
                                                                using {DotnetNames.Namespaces.System_Runtime};
                                                                using {DotnetNames.Namespaces.System_Runtime_CompilerServices};
                                                                """;

        /// <summary>
        ///     A set of empty namespaces that MAY be referenced in generated code, especially in using statements,
        ///     which are always included to avoid additional complexity due to conditional compilation.
        /// </summary>
        internal const string DummyNamespaceDeclarations = $$"""
                                                             // These are dummy declarations to avoid complexity with conditional compilation.
                                                             #pragma warning disable IDE0079 // Remove unnecessary suppression
                                                             #pragma warning disable RCS1259 // Remove empty syntax
                                                             namespace {{TerminalGuiRootNamespace}} { }
                                                             namespace {{AnalyzersRootNamespace}} { }
                                                             namespace {{InternalAnalyzersNamespace}} { }
                                                             namespace {{NetStandard20CompatibilityNamespace}} { }
                                                             namespace {{AnalyzersAttributesNamespace}} { }
                                                             #pragma warning restore RCS1259 // Remove empty syntax
                                                             #pragma warning restore IDE0079 // Remove unnecessary suppression
                                                             """;

        internal const string StandardHeader = $"""
                                                {AutoGeneratedCommentBlock}
                                                // ReSharper disable RedundantUsingDirective
                                                // ReSharper disable once RedundantNullableDirective
                                                {NullableContextDirective}

                                                {StandardUsingDirectivesText}
                                                """;

        /// <summary>
        ///     Standard set of using directives for generated extension method class files.
        ///     Not all are always needed, but all are included so we don't have to worry about it.
        /// </summary>
        internal const string StandardUsingDirectivesText = $"""
                                                             {DotnetNamespaceUsingDirectives}
                                                             {DotnetExplicitTypeAliasUsingDirectives}
                                                             using {TerminalGuiRootNamespace};
                                                             using {AnalyzersRootNamespace};
                                                             using {InternalAnalyzersNamespace};
                                                             using {AnalyzersAttributesNamespace};
                                                             using {NetStandard20CompatibilityNamespace};
                                                             """;

        internal const string AttributesForGeneratedInterfaces = $"""
                                                                  {DotnetNames.Attributes.Applications.GeneratedCode}
                                                                  {DotnetNames.Attributes.Applications.CompilerGenerated}
                                                                  """;

        internal const string AttributesForGeneratedTypes = $"""
                                                             {DotnetNames.Attributes.Applications.GeneratedCode}
                                                             {DotnetNames.Attributes.Applications.CompilerGenerated}
                                                             {DotnetNames.Attributes.Applications.DebuggerNonUserCode}
                                                             {DotnetNames.Attributes.Applications.ExcludeFromCodeCoverage}
                                                             """;

        /// <summary>
        ///     Preprocessor directive to enable nullability context for generated code.<br/>
        ///     This should always be emitted, as it applies only to generated code.<br/>
        ///     As such, generated code MUST be properly annotated.
        /// </summary>
        internal const string NullableContextDirective = "#nullable enable";
    }
}
