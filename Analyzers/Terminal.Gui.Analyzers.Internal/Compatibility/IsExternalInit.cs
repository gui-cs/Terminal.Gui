using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
///     Reserved to be used by the compiler for tracking metadata.
///     This class should not be used by developers in source code.
/// </summary>
/// <remarks>
///     Copied from .net source code, for support of init property accessors in netstandard2.0.
/// </remarks>
[ExcludeFromCodeCoverage]
[DebuggerNonUserCode]
[EditorBrowsable (EditorBrowsableState.Never)]
public static class IsExternalInit;
