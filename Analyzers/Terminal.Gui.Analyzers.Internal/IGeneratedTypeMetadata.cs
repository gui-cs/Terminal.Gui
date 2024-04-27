using JetBrains.Annotations;
using Microsoft.CodeAnalysis;

namespace Terminal.Gui.Analyzers.Internal;

/// <summary>
/// Interface for all generators to use for their metadata classes.
/// </summary>
/// <typeparam name="TSelf">The type implementing this interface.</typeparam>
internal interface IGeneratedTypeMetadata<out TSelf> where TSelf : IGeneratedTypeMetadata<TSelf>
{
    [UsedImplicitly]
    string GeneratedTypeNamespace { get; }
    [UsedImplicitly]
    string? GeneratedTypeName { get; }
    [UsedImplicitly]
    string GeneratedTypeFullName { get; }
    [UsedImplicitly]
    string TargetTypeNamespace { get; }
    [UsedImplicitly]
    string TargetTypeName { get; }
    string TargetTypeFullName { get; }
    [UsedImplicitly]
    Accessibility Accessibility { get; }
    TypeKind TypeKind { get; }
    bool IsRecord { get; }
    bool IsClass { get; }
    bool IsStruct { get; }
    [UsedImplicitly]
    bool IsPartial { get; }
    bool IsByRefLike { get; }
    bool IsSealed { get; }
    bool IsAbstract { get; }
    bool IsEnum { get; }
    bool IsStatic { get; }
    [UsedImplicitly]
    bool IncludeInterface { get; }
}