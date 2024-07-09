using System.Text;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis.Text;

namespace Terminal.Gui.Analyzers.Internal;

internal interface IStandardCSharpCodeGenerator<T> where T : IGeneratedTypeMetadata<T>
{
    /// <summary>
    ///     Generates and returns the full source text corresponding to <see cref="Metadata"/>,
    ///     in the requested <paramref name="encoding"/> or <see cref="Encoding.UTF8"/> if not provided.
    /// </summary>
    /// <param name="encoding">
    ///     The <see cref="Encoding"/> of the generated source text or <see cref="Encoding.UTF8"/> if not
    ///     provided.
    /// </param>
    /// <returns></returns>
    [UsedImplicitly]
    [SkipLocalsInit]
    ref readonly SourceText GenerateSourceText (Encoding? encoding = null);

    /// <summary>
    ///     A type implementing <see cref="IGeneratedTypeMetadata{T}"/> which
    ///     will be used for source generation.
    /// </summary>
    [UsedImplicitly]
    T Metadata { get; set; }
}
