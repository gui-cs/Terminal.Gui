using Terminal.Gui.Drawing;
using Terminal.Gui.Views;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;
using Attribute = Terminal.Gui.Drawing.Attribute;

namespace Terminal.Gui.SyntaxHighlighting;

/// <summary>
///     An <see cref="ISyntaxHighlighter"/> implementation powered by TextMateSharp.
///     Provides syntax highlighting for 50+ languages using VS Code's TextMate grammar engine.
/// </summary>
/// <remarks>
///     <para>
///         Assign an instance to <see cref="MarkdownView.SyntaxHighlighter"/> to enable
///         colorized code blocks in Markdown rendering:
///     </para>
///     <code>
///         markdownView.SyntaxHighlighter = new TextMateSyntaxHighlighter (ThemeName.DarkPlus);
///     </code>
///     <para>
///         The highlighter maintains per-line tokenizer state internally. <see cref="MarkdownView"/>
///         calls <see cref="ResetState"/> at the start of each code block so that multi-line
///         constructs (strings, comments) don't leak across blocks.
///     </para>
/// </remarks>
public class TextMateSyntaxHighlighter : ISyntaxHighlighter
{
    private RegistryOptions _registryOptions;
    private Registry _registry;
    private IStateStack? _ruleStack;
    private bool _nativeLibUnavailable;

    private readonly Dictionary<string, IGrammar?> _grammarCache = new (StringComparer.OrdinalIgnoreCase);

    /// <summary>Initializes a new <see cref="TextMateSyntaxHighlighter"/> with the specified theme.</summary>
    /// <param name="theme">
    ///     The VS Code theme to use for colorization. Defaults to <see cref="ThemeName.DarkPlus"/>.
    /// </param>
    public TextMateSyntaxHighlighter (ThemeName theme = ThemeName.DarkPlus)
    {
        _registryOptions = new RegistryOptions (theme);
        _registry = new Registry (_registryOptions);
    }

    /// <inheritdoc/>
    public IReadOnlyList<StyledSegment> Highlight (string code, string? language)
    {
        if (_nativeLibUnavailable)
        {
            return [new StyledSegment (code, MarkdownStyleRole.CodeBlock)];
        }

        IGrammar? grammar = ResolveGrammar (language);

        if (grammar is null)
        {
            return [new StyledSegment (code, MarkdownStyleRole.CodeBlock)];
        }

        ITokenizeLineResult result;

        try
        {
            result = grammar.TokenizeLine (code, _ruleStack, TimeSpan.MaxValue);
        }
        catch (Exception ex) when (ex is DllNotFoundException or TypeInitializationException)
        {
            // Native onigwrap library not available on this platform (e.g., win-arm64).
            // Degrade gracefully to unstyled code blocks for the rest of this session.
            _nativeLibUnavailable = true;

            return [new StyledSegment (code, MarkdownStyleRole.CodeBlock)];
        }

        _ruleStack = result.RuleStack;

        Theme theme = _registry.GetTheme ();
        List<StyledSegment> segments = [];

        foreach (IToken token in result.Tokens)
        {
            int startIndex = token.StartIndex;
            int endIndex = Math.Min (token.EndIndex, code.Length);

            if (startIndex >= endIndex)
            {
                continue;
            }

            string text = code [startIndex..endIndex];
            Attribute attr = ResolveAttribute (theme, token.Scopes);
            segments.Add (new StyledSegment (text, MarkdownStyleRole.CodeBlock, attribute: attr));
        }

        if (segments.Count == 0)
        {
            return [new StyledSegment (code, MarkdownStyleRole.CodeBlock)];
        }

        return segments;
    }

    /// <inheritdoc/>
    public void ResetState () => _ruleStack = null;

    /// <summary>
    ///     Switches the active theme used for colorization. Clears the grammar cache
    ///     since theme changes may affect tokenization colors.
    /// </summary>
    /// <param name="theme">The new VS Code theme to use.</param>
    public void SetTheme (ThemeName theme)
    {
        _registryOptions = new RegistryOptions (theme);
        _registry = new Registry (_registryOptions);
        _grammarCache.Clear ();
        _ruleStack = null;
    }

    private Attribute ResolveAttribute (Theme theme, List<string> scopes)
    {
        List<ThemeTrieElementRule> rules = theme.Match (scopes);

        if (rules.Count == 0)
        {
            return new Attribute (Color.White, Color.Black);
        }

        ThemeTrieElementRule rule = rules [0];
        string? fgHex = theme.GetColor (rule.foreground);
        Color fg = !string.IsNullOrEmpty (fgHex) ? Color.Parse (fgHex) : Color.White;

        TextStyle style = TextStyle.None;

        if ((rule.fontStyle & FontStyle.Bold) != 0)
        {
            style |= TextStyle.Bold;
        }

        if ((rule.fontStyle & FontStyle.Italic) != 0)
        {
            style |= TextStyle.Italic;
        }

        if ((rule.fontStyle & FontStyle.Underline) != 0)
        {
            style |= TextStyle.Underline;
        }

        if ((rule.fontStyle & FontStyle.Strikethrough) != 0)
        {
            style |= TextStyle.Strikethrough;
        }

        return new Attribute (fg, Color.Black, style);
    }

    private IGrammar? ResolveGrammar (string? language)
    {
        if (string.IsNullOrEmpty (language))
        {
            return null;
        }

        if (_grammarCache.TryGetValue (language, out IGrammar? cached))
        {
            return cached;
        }

        IGrammar? grammar = TryLoadGrammar (language);
        _grammarCache [language] = grammar;

        return grammar;
    }

    private IGrammar? TryLoadGrammar (string language)
    {
        // Try language ID first (e.g., "csharp", "python")
        try
        {
            string? scopeName = _registryOptions.GetScopeByLanguageId (language);

            if (!string.IsNullOrEmpty (scopeName))
            {
                return _registry.LoadGrammar (scopeName);
            }
        }
        catch
        {
            // GetScopeByLanguageId can throw for unknown language IDs
        }

        // Try as file extension (e.g., ".cs", ".py")
        string extension = language.StartsWith ('.') ? language : $".{language}";

        try
        {
            string? scopeName = _registryOptions.GetScopeByExtension (extension);

            if (!string.IsNullOrEmpty (scopeName))
            {
                return _registry.LoadGrammar (scopeName);
            }
        }
        catch
        {
            // GetScopeByExtension can throw for unknown extensions
        }

        // Try aliases — search available languages for matching alias
        try
        {
            foreach (Language lang in _registryOptions.GetAvailableLanguages ())
            {
                if (string.Equals (lang.Id, language, StringComparison.OrdinalIgnoreCase))
                {
                    string? scopeName = _registryOptions.GetScopeByLanguageId (lang.Id);

                    if (!string.IsNullOrEmpty (scopeName))
                    {
                        return _registry.LoadGrammar (scopeName);
                    }
                }

                if (lang.Aliases is null)
                {
                    continue;
                }

                foreach (string alias in lang.Aliases)
                {
                    if (!string.Equals (alias, language, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string? scopeName = _registryOptions.GetScopeByLanguageId (lang.Id);

                    if (!string.IsNullOrEmpty (scopeName))
                    {
                        return _registry.LoadGrammar (scopeName);
                    }
                }
            }
        }
        catch
        {
            // Defensive — grammar loading should never crash the app
        }

        return null;
    }
}
