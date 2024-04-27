// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This file is further modified from the original, for this project,
// to comply with project style.
// No changes are made which affect compatibility with the same types from
// APIs later than netstandard2.0, nor will this file be included in compilations
// targeted at later APIs.
//
// Originally rom https://github.com/dotnet/runtime/blob/ef72b95937703e485fdbbb75f3251fedfd1a0ef9/src/libraries/System.Private.CoreLib/src/System/Diagnostics/CodeAnalysis/NullableAttributes.cs

// ReSharper disable CheckNamespace

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedType.Global

namespace System.Diagnostics.CodeAnalysis;

/// <summary>Specifies that null is allowed as an input even if the corresponding type disallows it.</summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class AllowNullAttribute : Attribute;

/// <summary>Specifies that null is disallowed as an input even if the corresponding type allows it.</summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class DisallowNullAttribute : Attribute;

/// <summary>Specifies that an output may be null even if the corresponding type disallows it.</summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class MaybeNullAttribute : Attribute;

/// <summary>
///     Specifies that an output will not be null even if the corresponding type allows it. Specifies that an input
///     argument was not null when the call returns.
/// </summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class NotNullAttribute : Attribute;

/// <summary>
///     Specifies that when a method returns <see cref="ReturnValue"/>, the parameter may be null even if the corresponding
///     type disallows it.
/// </summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class MaybeNullWhenAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified return value condition.</summary>
    /// <param name="returnValue">
    ///     The return value condition. If the method returns this value, the associated parameter may be null.
    /// </param>
#pragma warning disable IDE0290 // Use primary constructor
    public MaybeNullWhenAttribute (bool returnValue) { ReturnValue = returnValue; }
#pragma warning restore IDE0290 // Use primary constructor

    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }
}

/// <summary>
///     Specifies that when a method returns <see cref="ReturnValue"/>, the parameter will not be null even if the
///     corresponding type allows it.
/// </summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class NotNullWhenAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified return value condition.</summary>
    /// <param name="returnValue">
    ///     The return value condition. If the method returns this value, the associated parameter will not be null.
    /// </param>
#pragma warning disable IDE0290 // Use primary constructor
    public NotNullWhenAttribute (bool returnValue) { ReturnValue = returnValue; }
#pragma warning restore IDE0290 // Use primary constructor

    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }
}

/// <summary>Specifies that the output will be non-null if the named parameter is non-null.</summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class NotNullIfNotNullAttribute : Attribute
{
    /// <summary>Initializes the attribute with the associated parameter name.</summary>
    /// <param name="parameterName">
    ///     The associated parameter name.  The output will be non-null if the argument to the parameter specified is non-null.
    /// </param>
#pragma warning disable IDE0290 // Use primary constructor
    public NotNullIfNotNullAttribute (string parameterName) { ParameterName = parameterName; }
#pragma warning restore IDE0290 // Use primary constructor

    /// <summary>Gets the associated parameter name.</summary>
    public string ParameterName { get; }
}

/// <summary>Applied to a method that will never return under any circumstance.</summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Method, Inherited = false)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class DoesNotReturnAttribute : Attribute;

/// <summary>Specifies that the method will not return if the associated Boolean parameter is passed the specified value.</summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class DoesNotReturnIfAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified parameter value.</summary>
    /// <param name="parameterValue">
    ///     The condition parameter value. Code after the method will be considered unreachable by diagnostics if the argument
    ///     to
    ///     the associated parameter matches this value.
    /// </param>
#pragma warning disable IDE0290 // Use primary constructor
    public DoesNotReturnIfAttribute (bool parameterValue) { ParameterValue = parameterValue; }
#pragma warning restore IDE0290 // Use primary constructor

    /// <summary>Gets the condition parameter value.</summary>
    public bool ParameterValue { get; }
}

/// <summary>
///     Specifies that the method or property will ensure that the listed field and property members have not-null
///     values.
/// </summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class MemberNotNullAttribute : Attribute
{
    /// <summary>Initializes the attribute with a field or property member.</summary>
    /// <param name="member">
    ///     The field or property member that is promised to be not-null.
    /// </param>
    public MemberNotNullAttribute (string member) { Members = [member]; }

    /// <summary>Initializes the attribute with the list of field and property members.</summary>
    /// <param name="members">
    ///     The list of field and property members that are promised to be not-null.
    /// </param>
    public MemberNotNullAttribute (params string [] members) { Members = members; }

    /// <summary>Gets field or property member names.</summary>
    public string [] Members { get; }
}

/// <summary>
///     Specifies that the method or property will ensure that the listed field and property members have not-null values
///     when returning with the specified return value condition.
/// </summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
internal sealed class MemberNotNullWhenAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified return value condition and a field or property member.</summary>
    /// <param name="returnValue">
    ///     The return value condition. If the method returns this value, the associated parameter will not be null.
    /// </param>
    /// <param name="member">
    ///     The field or property member that is promised to be not-null.
    /// </param>
    public MemberNotNullWhenAttribute (bool returnValue, string member)
    {
        ReturnValue = returnValue;
        Members = [member];
    }

    /// <summary>Initializes the attribute with the specified return value condition and list of field and property members.</summary>
    /// <param name="returnValue">
    ///     The return value condition. If the method returns this value, the associated parameter will not be null.
    /// </param>
    /// <param name="members">
    ///     The list of field and property members that are promised to be not-null.
    /// </param>
    public MemberNotNullWhenAttribute (bool returnValue, params string [] members)
    {
        ReturnValue = returnValue;
        Members = members;
    }

    /// <summary>Gets field or property member names.</summary>
    public string [] Members { get; }

    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }
}
