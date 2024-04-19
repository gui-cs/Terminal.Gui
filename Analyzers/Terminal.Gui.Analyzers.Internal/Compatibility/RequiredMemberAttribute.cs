// ReSharper disable CheckNamespace
// ReSharper disable ConditionalAnnotation

using JetBrains.Annotations;

namespace System.Runtime.CompilerServices;

/// <summary>Polyfill to enable netstandard2.0 assembly to use the required keyword.</summary>
/// <remarks>Excluded from output assembly via file specified in ApiCompatExcludeAttributesFile element in the project file.</remarks>
[AttributeUsage (AttributeTargets.Property)]
[UsedImplicitly]
public sealed class RequiredMemberAttribute : Attribute;
