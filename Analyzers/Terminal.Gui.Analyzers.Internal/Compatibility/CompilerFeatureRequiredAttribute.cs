// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
///     Indicates that compiler support for a particular feature is required for the location where this attribute is
///     applied.
/// </summary>
[AttributeUsage (AttributeTargets.All, AllowMultiple = true, Inherited = false)]
internal sealed class CompilerFeatureRequiredAttribute(string featureName) : Attribute
{
    /// <summary>
    ///     The <see cref="FeatureName"/> used for the ref structs C# feature.
    /// </summary>
    public const string RefStructs = nameof (RefStructs);

    /// <summary>
    ///     The <see cref="FeatureName"/> used for the required members C# feature.
    /// </summary>
    public const string RequiredMembers = nameof (RequiredMembers);

    /// <summary>
    ///     The name of the compiler feature.
    /// </summary>
    public string FeatureName { get; } = featureName;
    /// <summary>
    ///     If true, the compiler can choose to allow access to the location where this attribute is applied if it does not
    ///     understand <see cref="FeatureName"/>.
    /// </summary>
    public bool IsOptional { get; init; }
}