using System.Collections.ObjectModel;
using Terminal.Gui.Views;
using TextMateSharp.Grammars;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace Terminal.Gui.Drawing;

/// <summary>
///     An <see cref="ISyntaxHighlighter"/> implementation powered by TextMateSharp.
///     Provides syntax highlighting for 50+ languages using VS Code's TextMate grammar engine.
/// </summary>
/// <remarks>
///     <para>
///         Assign an instance to <see cref="Markdown.SyntaxHighlighter"/> to enable
///         colorized code blocks in Markdown rendering:
///     </para>
///     <code>
///         markdownView.SyntaxHighlighter = new TextMateSyntaxHighlighter (ThemeName.DarkPlus);
///     </code>
///     <para>
///         The highlighter maintains per-line tokenizer state internally. <see cref="Markdown"/>
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
    private Color _defaultForeground;
    private Color _defaultBackground;

    private readonly Dictionary<string, IGrammar?> _grammarCache = new (StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<MarkdownStyleRole, Attribute?> _scopeAttributeCache = [];

    /// <summary>
    ///     Maps <see cref="MarkdownStyleRole"/> values to candidate TextMate scope arrays.
    ///     Each role has one or more candidate scope lists, tried in order until one returns
    ///     rules from the active theme.
    /// </summary>
    private static readonly Dictionary<MarkdownStyleRole, string [] []> _scopeMap = new ()
    {
        [MarkdownStyleRole.Heading] = [["entity.name.section"], ["markup.heading"]],
        [MarkdownStyleRole.HeadingMarker] = [["entity.name.section"], ["markup.heading"]],
        [MarkdownStyleRole.Emphasis] = [["markup.italic"]],
        [MarkdownStyleRole.Strong] = [["markup.bold"]],
        [MarkdownStyleRole.InlineCode] = [["markup.inline.raw"], ["markup.raw"]],
        [MarkdownStyleRole.CodeBlock] = [["markup.fenced_code.block.markdown"], ["markup.raw"]],
        [MarkdownStyleRole.Link] = [["markup.underline.link"], ["string.other.link"], ["markup.underline"]],
        [MarkdownStyleRole.Quote] = [["markup.quote"], ["markup.changed"]],
        [MarkdownStyleRole.ListMarker] = [["punctuation.definition.list.begin.markdown"], ["keyword.control"]],
        [MarkdownStyleRole.ImageAlt] = [["markup.italic"]],
        [MarkdownStyleRole.TaskDone] = [["markup.strikethrough"], ["markup.deleted"]],
        [MarkdownStyleRole.ThematicBreak] = [["meta.separator.markdown"], ["comment"]]
    };

    /// <summary>Initializes a new <see cref="TextMateSyntaxHighlighter"/> with the specified theme.</summary>
    /// <param name="theme">
    ///     The VS Code theme to use for colorization. Defaults to <see cref="ThemeName.DarkPlus"/>.
    /// </param>
    public TextMateSyntaxHighlighter (ThemeName theme = ThemeName.DarkPlus)
    {
        CurrentThemeName = theme;
        _registryOptions = new RegistryOptions (theme);
        _registry = new Registry (_registryOptions);
        CacheThemeDefaults ();
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
    ///     Returns a <see cref="ThemeName"/> appropriate for the given terminal background color.
    ///     Returns <see cref="ThemeName.DarkPlus"/> for dark backgrounds and
    ///     <see cref="ThemeName.LightPlus"/> for light backgrounds.
    /// </summary>
    /// <param name="background">The terminal background color to evaluate.</param>
    /// <returns>A theme appropriate for the background luminance.</returns>
    public static ThemeName GetThemeForBackground (Color background) =>
        background.IsDarkColor () ? ThemeName.DarkPlus : ThemeName.LightPlus;

    /// <summary>Gets the <see cref="ThemeName"/> that is currently active.</summary>
    public ThemeName CurrentThemeName { get; private set; }

    /// <inheritdoc/>
    public Color? DefaultBackground => _defaultBackground;

    /// <inheritdoc/>
    public Attribute? GetAttributeForScope (MarkdownStyleRole role)
    {
        if (_scopeAttributeCache.TryGetValue (role, out Attribute? cached))
        {
            return cached;
        }

        if (!_scopeMap.TryGetValue (role, out string [] []? candidateScopes))
        {
            _scopeAttributeCache [role] = null;

            return null;
        }

        Theme theme = _registry.GetTheme ();

        foreach (string [] scopes in candidateScopes)
        {
            List<ThemeTrieElementRule> rules = theme.Match (scopes.ToList ());

            if (rules.Count == 0)
            {
                continue;
            }

            Attribute attr = ResolveAttribute (theme, scopes.ToList ());
            _scopeAttributeCache [role] = attr;

            return attr;
        }

        _scopeAttributeCache [role] = null;

        return null;
    }

    /// <summary>
    ///     Switches the active theme used for colorization. Clears the grammar cache
    ///     since theme changes may affect tokenization colors.
    /// </summary>
    /// <param name="theme">The new VS Code theme to use.</param>
    public void SetTheme (ThemeName theme)
    {
        CurrentThemeName = theme;
        _registryOptions = new RegistryOptions (theme);
        _registry = new Registry (_registryOptions);
        _grammarCache.Clear ();
        _scopeAttributeCache.Clear ();
        _ruleStack = null;
        CacheThemeDefaults ();
    }

    private void CacheThemeDefaults ()
    {
        Theme t = _registry.GetTheme ();

        // Prefer the VS Code GUI color dictionary for the true editor background/foreground.
        // This gives us "editor.background" and "editor.foreground" which are the actual
        // theme colors, unlike scope-based matching which may not differentiate themes.
        ReadOnlyDictionary<string, string> guiColors = t.GetGuiColorDictionary ();

        if (guiColors.TryGetValue ("editor.background", out string? bgHex) && !string.IsNullOrEmpty (bgHex))
        {
            _defaultBackground = Color.Parse (bgHex);
        }
        else
        {
            // Fallback: match the "source" scope for themes that don't specify GUI colors
            List<ThemeTrieElementRule> defaultRules = t.Match (["source"]);

            if (defaultRules.Count > 0)
            {
                string? scopeBgHex = t.GetColor (defaultRules [0].background);
                _defaultBackground = !string.IsNullOrEmpty (scopeBgHex) ? Color.Parse (scopeBgHex) : Color.Black;
            }
            else
            {
                _defaultBackground = Color.Black;
            }
        }

        if (guiColors.TryGetValue ("editor.foreground", out string? fgHex) && !string.IsNullOrEmpty (fgHex))
        {
            _defaultForeground = Color.Parse (fgHex);
        }
        else
        {
            List<ThemeTrieElementRule> defaultRules = t.Match (["source"]);

            if (defaultRules.Count > 0)
            {
                string? scopeFgHex = t.GetColor (defaultRules [0].foreground);
                _defaultForeground = !string.IsNullOrEmpty (scopeFgHex) ? Color.Parse (scopeFgHex) : Color.White;
            }
            else
            {
                _defaultForeground = Color.White;
            }
        }
    }

    private Attribute ResolveAttribute (Theme theme, List<string> scopes)
    {
        List<ThemeTrieElementRule> rules = theme.Match (scopes);

        if (rules.Count == 0)
        {
            return new Attribute (_defaultForeground, _defaultBackground);
        }

        ThemeTrieElementRule rule = rules [0];
        string? fgHex = theme.GetColor (rule.foreground);
        Color fg = !string.IsNullOrEmpty (fgHex) ? Color.Parse (fgHex) : _defaultForeground;

        TextStyle style = TextStyle.None;

        // FontStyle.NotSet is -1 (all bits set) — guard against it
        if (rule.fontStyle < 0)
        {
            return new Attribute (fg, _defaultBackground, style);
        }

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

        return new Attribute (fg, _defaultBackground, style);
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
