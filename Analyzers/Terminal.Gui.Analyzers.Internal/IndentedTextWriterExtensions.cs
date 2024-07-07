using System.CodeDom.Compiler;

namespace Terminal.Gui.Analyzers.Internal;

/// <summary>
///     Just a simple set of extension methods to increment and decrement the indentation
///     level of an <see cref="IndentedTextWriter"/> via push and pop terms, and to avoid having
///     explicit values all over the place.
/// </summary>
public static class IndentedTextWriterExtensions
{
    /// <summary>
    ///     Decrements <see cref="IndentedTextWriter.Indent"/> by 1, but only if it is greater than 0.
    /// </summary>
    /// <returns>
    ///     The resulting indentation level of the <see cref="IndentedTextWriter"/>.
    /// </returns>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static int Pop (this IndentedTextWriter w, string endScopeDelimiter = "}")
    {
        if (w.Indent > 0)
        {
            w.Indent--;
            w.WriteLine (endScopeDelimiter);
        }
        return w.Indent;
    }

    /// <summary>
    ///     Decrements <see cref="IndentedTextWriter.Indent"/> by 1 and then writes a closing curly brace.
    /// </summary>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static void PopCurly (this IndentedTextWriter w, bool withSemicolon = false)
    {
        w.Indent--;

        if (withSemicolon)
        {
            w.WriteLine ("};");
        }
        else
        {
            w.WriteLine ('}');
        }
    }

    /// <summary>
    ///     Increments <see cref="IndentedTextWriter.Indent"/> by 1, with optional parameters to customize the scope push.
    /// </summary>
    /// <param name="w">An instance of an <see cref="IndentedTextWriter"/>.</param>
    /// <param name="declaration">
    ///     The first line to be written before indenting and before the optional <paramref name="scopeDelimiter"/> line or
    ///     null if not needed.
    /// </param>
    /// <param name="scopeDelimiter">
    ///     An opening delimiter to write. Written before the indentation and after <paramref name="declaration"/> (if provided). Default is an opening curly brace.
    /// </param>
    /// <remarks>Calling with no parameters will write an opening curly brace and a line break at the current indentation and then increment.</remarks>
    [MethodImpl (MethodImplOptions.AggressiveInlining)]
    public static void Push (this IndentedTextWriter w, string? declaration = null, char scopeDelimiter = '{')
    {
        if (declaration is { Length: > 0 })
        {
            w.WriteLine (declaration);
        }

        w.WriteLine (scopeDelimiter);

        w.Indent++;
    }
}
