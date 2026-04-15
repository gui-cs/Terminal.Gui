# Plan: TextMateSharp-Powered Syntax Highlighting for MarkdownView

## Problem Statement

Terminal.Gui's `MarkdownView` has an `ISyntaxHighlighter` interface for code block colorization, but no implementation ships with the library. Code blocks render as monochrome text with a dimmed background. Meanwhile, tools like Glow, Bat, and Rich demonstrate that syntax-highlighted code is a baseline expectation for terminal Markdown rendering.

The existing `ISyntaxHighlighter` interface returns `StyledSegment`s tagged with `MarkdownStyleRole`, which is Markdown-specific and limited. A proper syntax highlighter needs per-token coloring (keywords, strings, comments, operators) with themeable colors — exactly what TextMateSharp provides.

## Background

### TextMateSharp

[TextMateSharp](https://github.com/danipen/TextMateSharp) is a .NET port of VS Code's `vscode-textmate` tokenization engine:

- **Actively maintained** — last commit March 30, 2026; NuGet v2.0.3
- **Used by AvaloniaEdit** — the primary Avalonia code editor, proving production readiness
- **netstandard2.0** — fully compatible with Terminal.Gui's net8.0 target
- **Two packages**: `TextMateSharp` (core engine) + `TextMateSharp.Grammars` (50+ bundled language grammars, 21 VS Code themes)
- **Key API**: `Registry` → `IGrammar` → `TokenizeLine()` → `IToken[]` with `Scopes` → `Theme.Match(scopes)` → foreground/background/fontStyle

### Current MarkdownView Pipeline

```
Raw Markdown → LowerFromSourceText() → List<IntermediateBlock>
  → BuildRenderedLines() → List<RenderedLine> (each has List<StyledSegment>)
  → OnDrawingSubViews() → MarkdownAttributeHelper.GetAttributeForSegment()
  → SetAttribute/AddStr per grapheme
```

Code blocks flow through `ISyntaxHighlighter.Highlight()` during lowering, if a highlighter is assigned. Currently `language` is always passed as `null`.

### winprint Precedent

The `tig/winprint` project solved this same problem by shelling out to `pygmentize.exe` (external process), getting ANSI-encoded output, then parsing ANSI escapes back into color runs via `libvt100`. This worked but was fragile (temp files, process spawning, two-pass parsing). TextMateSharp eliminates all of that — in-process, direct token objects, no intermediate format.

## Design

### Architecture: Three Layers

```
┌─────────────────────────────────────────────────────────┐
│ Layer 3: Theme (VS Code .json themes)                   │
│   Maps TextMate scopes → Terminal.Gui Attributes        │
│   "keyword.control" → Attribute(#569CD6, Bold)          │
├─────────────────────────────────────────────────────────┤
│ Layer 2: Tokenizer (TextMateSharp)                      │
│   Parses source code → tokens with TextMate scopes     │
│   "var" → ["source.cs", "keyword.other.var.cs"]         │
├─────────────────────────────────────────────────────────┤
│ Layer 1: Integration (Terminal.Gui)                     │
│   ISyntaxHighlighter impl + VisualRole.Code base attr   │
│   Connects tokenizer output to View drawing pipeline    │
└─────────────────────────────────────────────────────────┘
```

### Key Design Decisions

#### 1. StyledSegment gets an optional explicit Attribute

Today `StyledSegment` carries a `MarkdownStyleRole`, and `MarkdownAttributeHelper` resolves it to an `Attribute`. For syntax-highlighted tokens, we need per-token colors that don't map to any `MarkdownStyleRole`. Rather than expanding the enum with dozens of syntax roles, `StyledSegment` gets an optional `Attribute?`:

```csharp
public sealed class StyledSegment
{
    public string Text { get; }
    public MarkdownStyleRole StyleRole { get; }
    public Attribute? Attribute { get; }      // NEW: if set, used directly (bypasses StyleRole)
    public string? Url { get; }
    public string? ImageSource { get; }
}
```

When `Attribute` is non-null, `MarkdownAttributeHelper.GetAttributeForSegment()` returns it directly instead of resolving via the `StyleRole` switch. This is the minimal, non-breaking change that lets syntax-highlighted tokens carry their own colors.

#### 2. TextMateSyntaxHighlighter implements ISyntaxHighlighter

A new class that wraps TextMateSharp:

```csharp
public class TextMateSyntaxHighlighter : ISyntaxHighlighter
{
    private readonly Registry _registry;
    private readonly RegistryOptions _registryOptions;

    public TextMateSyntaxHighlighter (ThemeName theme = ThemeName.DarkPlus) { ... }

    // ISyntaxHighlighter.Highlight — called per line of a code block
    public IReadOnlyList<StyledSegment> Highlight (string code, string? language)
    {
        IGrammar? grammar = ResolveGrammar (language);
        if (grammar is null) return [new StyledSegment (code, MarkdownStyleRole.CodeBlock)];

        ITokenizeLineResult result = grammar.TokenizeLine (code, _ruleStack, TimeSpan.MaxValue);
        _ruleStack = result.RuleStack;

        List<StyledSegment> segments = [];
        Theme theme = _registry.GetTheme ();

        foreach (IToken token in result.Tokens)
        {
            string text = code [token.StartIndex..token.EndIndex];
            Attribute attr = ResolveAttribute (theme, token.Scopes);
            segments.Add (new StyledSegment (text, MarkdownStyleRole.CodeBlock, attribute: attr));
        }
        return segments;
    }

    // Map TextMate scopes → Terminal.Gui Attribute
    private Attribute ResolveAttribute (Theme theme, List<string> scopes) { ... }

    // Map language id/name → grammar scope name
    private IGrammar? ResolveGrammar (string? language) { ... }

    // Reset state between code blocks
    public void ResetState () { _ruleStack = null; }
}
```

#### 3. MarkdownView extracts and passes fence language

Currently `LowerFromSourceText()` detects fenced code blocks but discards the language identifier. The fix is to extract it from the fence line (e.g., `` ```csharp `` → `"csharp"`) and pass it to `ISyntaxHighlighter.Highlight(code, language)`.

#### 4. State management between lines within a code block

TextMate tokenization is **stateful** — each line's `IStateStack` must be passed to the next line. The `ISyntaxHighlighter` interface is called line-by-line for each code block. The `TextMateSyntaxHighlighter` tracks `_ruleStack` internally and exposes `ResetState()`. `MarkdownView` calls `ResetState()` at the start of each new code block.

This requires a small `ISyntaxHighlighter` interface addition:

```csharp
public interface ISyntaxHighlighter
{
    IReadOnlyList<StyledSegment> Highlight (string code, string? language);
    void ResetState ();  // NEW: called at start of each code block
}
```

#### 5. Package dependency strategy

`TextMateSharp.Grammars` bundles 50+ grammars and 21 themes as embedded resources (~5MB). Two options:

**Option A: Ship in Terminal.Gui core** — `Terminal.Gui` takes a dependency on `TextMateSharp.Grammars`. Simple but adds ~5MB to every Terminal.Gui app.

**Option B (Recommended): Separate NuGet package** — Create `Terminal.Gui.SyntaxHighlighting` that depends on `TextMateSharp.Grammars` and provides `TextMateSyntaxHighlighter`. Apps opt in:

```bash
dotnet add package Terminal.Gui.SyntaxHighlighting
```

```csharp
markdownView.SyntaxHighlighter = new TextMateSyntaxHighlighter (ThemeName.DarkPlus);
```

This keeps Terminal.Gui core lean. The `ISyntaxHighlighter` interface stays in core; the implementation is in the add-on package.

#### 6. Theme ↔ Scheme coordination

The TextMate theme provides foreground colors and font styles per token. The **background** for code regions comes from `VisualRole.Code` (from the current PR). This means:

- TextMate theme → token foreground + bold/italic/underline
- `VisualRole.Code` → code block background color
- Combined: `new Attribute(tmForeground, codeRoleBackground, tmFontStyle)`

The `TextMateSyntaxHighlighter` can optionally accept a `View` or `Scheme` reference to read the `VisualRole.Code` background, or it can be set explicitly. VS Code theme's `editor.background` can also be used as a fallback.

#### 7. How VS Code themes map to Terminal.Gui

TextMateSharp's `Theme.Match(scopes)` returns `ThemeTrieElementRule` with:
- `foreground` → color map ID → hex string like `"#569CD6"` → `Color.Parse()`
- `background` → color map ID → hex string (rarely set per-token; usually the editor bg)
- `fontStyle` → `FontStyle.Bold | Italic | Underline | Strikethrough` → maps directly to `TextStyle`

The mapping is straightforward:

```csharp
private Attribute ResolveAttribute (Theme theme, List<string> scopes)
{
    List<ThemeTrieElementRule> rules = theme.Match (scopes);
    if (rules.Count == 0) return _defaultCodeAttribute;

    ThemeTrieElementRule rule = rules [0]; // highest priority match
    string? fgHex = theme.GetColor (rule.foreground);
    Color fg = fgHex is { } ? Color.Parse (fgHex) : _defaultCodeAttribute.Foreground;

    TextStyle style = TextStyle.None;
    if (rule.fontStyle.HasFlag (FontStyle.Bold)) style |= TextStyle.Bold;
    if (rule.fontStyle.HasFlag (FontStyle.Italic)) style |= TextStyle.Italic;
    if (rule.fontStyle.HasFlag (FontStyle.Underline)) style |= TextStyle.Underline;
    if (rule.fontStyle.HasFlag (FontStyle.Strikethrough)) style |= TextStyle.Strikethrough;

    return new Attribute (fg, _codeBackground, style);
}
```

### What This Enables

With TextMateSharp integrated, a `MarkdownView` rendering a C# code block would show:

| Token | Color (DarkPlus theme) | Style |
|-------|----------------------|-------|
| `using` | #569CD6 (blue) | — |
| `System` | #4EC9B0 (teal) | — |
| `// comment` | #6A9955 (green) | Italic |
| `"string"` | #CE9178 (orange) | — |
| `class` | #569CD6 (blue) | — |
| `MyClass` | #4EC9B0 (teal) | — |
| `42` | #B5CEA8 (light green) | — |
| `var` | #569CD6 (blue) | — |

All on the `VisualRole.Code` background, with 50+ languages supported out of the box and 21 themes to choose from.

## Relationship to VisualRole.Code PR

This plan **depends on** the `VisualRole.Code` addition (see `plans/add-visualrole-code.md`). `VisualRole.Code` provides the base background attribute for code regions. TextMateSharp provides per-token foreground coloring on top of that base. They are complementary:

- `VisualRole.Code` (PR scope) → themeable code block background via `Scheme`
- `TextMateSyntaxHighlighter` (future) → per-token foreground via VS Code themes

## Implementation Phases

### Phase 1: Foundation (part of VisualRole.Code PR)
1. Add `VisualRole.Code` to `Scheme` (see `plans/add-visualrole-code.md`)
2. Extract fence language from code block markers in `LowerFromSourceText()`
3. Pass language to `ISyntaxHighlighter.Highlight()`
4. Add `ResetState()` to `ISyntaxHighlighter` interface
5. Call `ResetState()` at start of each code block in lowering

### Phase 2: StyledSegment enhancement
6. Add optional `Attribute?` property to `StyledSegment`
7. Update `MarkdownAttributeHelper.GetAttributeForSegment()` to use explicit `Attribute` when present
8. Update `InlineRun` similarly if needed for pipeline consistency

### Phase 3: TextMateSyntaxHighlighter (separate package)
9. Create `Terminal.Gui.SyntaxHighlighting` project
10. Add `TextMateSharp.Grammars` NuGet dependency
11. Implement `TextMateSyntaxHighlighter : ISyntaxHighlighter`
12. Implement scope → Attribute resolution with `VisualRole.Code` background coordination
13. Implement language → grammar resolution (using `RegistryOptions.GetScopeByLanguageId()`)
14. Add theme switching API (`SetTheme(ThemeName)`)

### Phase 4: UICatalog & Documentation
15. Add syntax highlighting to `MarkdownView` UICatalog scenario
16. Document usage in `docfx/docs/`
17. Add unit tests for tokenization → Attribute mapping

## Performance Considerations

| Concern | Mitigation |
|---------|-----------|
| Grammar loading | Lazy — only load grammar when a language is first encountered. Cache loaded grammars. |
| Tokenization speed | TextMateSharp uses Oniguruma (native regex); per-line tokenization is fast. `TimeSpan` timeout prevents hangs. |
| Memory (grammar bundle) | `TextMateSharp.Grammars` loads from embedded resources on demand, not all at once. |
| Re-tokenization on resize | Code block content doesn't change on resize — cache `StyledSegment`s. Only word-wrapped prose needs re-layout. |
| Theme matching | TextMateSharp caches scope → rule lookups in a `ConcurrentDictionary`. First match is trie-based; subsequent matches are O(1). |

## Open Questions

1. **Should `TextMateSyntaxHighlighter` auto-detect dark/light and pick an appropriate theme?** It could read the view's `Scheme.Normal` background and choose `DarkPlus` vs `LightPlus`.

2. **Should the grammar bundle be trimmed?** 50+ languages might be more than most apps need. Could offer a "common languages" subset package.

3. **Should we support user-provided `.tmLanguage.json` files?** TextMateSharp supports loading grammars from file paths. Could expose this via config.

4. **Thread safety for `_ruleStack`**: `ISyntaxHighlighter.Highlight()` is called synchronously during layout, so single-threaded. But if `MarkdownView` ever moves to async layout, the stateful tokenizer needs synchronization.

## Files Affected

### Phase 1 (VisualRole.Code PR)
- `Terminal.Gui/Views/Markdown/ISyntaxHighlighter.cs` — add `ResetState()`
- `Terminal.Gui/Views/Markdown/MarkdownView.Parsing.cs` — extract language, call `ResetState()`

### Phase 2
- `Terminal.Gui/Views/Markdown/StyledSegment.cs` — add `Attribute?` property
- `Terminal.Gui/Views/Markdown/InlineRun.cs` — add `Attribute?` property if needed
- `Terminal.Gui/Views/Markdown/MarkdownAttributeHelper.cs` — respect explicit `Attribute`

### Phase 3 (new project)
- `Terminal.Gui.SyntaxHighlighting/TextMateSyntaxHighlighter.cs`
- `Terminal.Gui.SyntaxHighlighting/Terminal.Gui.SyntaxHighlighting.csproj`

### Phase 4
- `Examples/UICatalog/Scenarios/MarkdownViewer.cs` (or similar)
- `docfx/docs/` — usage documentation
