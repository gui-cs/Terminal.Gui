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

## Implementation Status

### ✅ Phase 1: Foundation — COMPLETE
- VisualRole.Code added to Scheme with derivation (dimmed bg + bold from Editable)
- ISyntaxHighlighter.ResetState() added and called per code block
- Fence language extraction from ` ```csharp ` lines, passed to Highlight()
- 12 VisualRole.Code tests + 12 pipeline tests = 24 tests passing

### ✅ Phase 2: StyledSegment Enhancement — COMPLETE
- StyledSegment.Attribute optional property added
- MarkdownAttributeHelper guard clause for explicit Attribute
- MarkdownCodeBlock and InlineCode use VisualRole.Code (not Editable)

### ✅ Phase 3: TextMateSyntaxHighlighter — COMPLETE
- Terminal.Gui.SyntaxHighlighting project created
- TextMateSharp.Grammars 1.0.52 dependency via centralized package management
- TextMateSyntaxHighlighter implements ISyntaxHighlighter with:
  - Grammar caching, language alias resolution
  - Scope → Attribute mapping via Theme.Match()
  - Theme switching via SetTheme(ThemeName)
  - Stateful multi-line tokenization with ResetState()
  - **Graceful degradation**: catches `DllNotFoundException`/`TypeInitializationException` on first `TokenizeLine` and falls back to unstyled code blocks (prevents crash on ARM64 where `onigwrap` native lib is missing)
- 23 TextMateSharp tests passing (requires -r win-x64 on ARM64 dev machines)

### ✅ Phase 4: Integration & Polish — COMPLETE
- mdv example wired up with TextMateSyntaxHighlighter(ThemeName.DarkPlus)
- UICatalog Markdown scenario wired up (both Deepdives and Markdown Tester)
- UICatalog.csproj references Terminal.Gui.SyntaxHighlighting

### Test Summary
| Suite | Count | Status |
|-------|-------|--------|
| VisualRole.Code (SchemeTests.CodeRoleTests) | 12 | ✅ Pass |
| Pipeline (SyntaxHighlighterPipelineTests) | 12 | ✅ Pass |
| TextMateSharp (TextMateSyntaxHighlighterTests) | 23 | ✅ Pass (x64) |
| MarkdownView (existing) | 49 | ✅ Pass |
| Scheme (existing) | 42 | ✅ Pass |
| **Total** | **138** | **✅ All passing** |

## Lessons Learned

1. **ARM64 native library gap — RESOLVED**: TextMateSharp 1.0.52 bundled `onigwrap` internally with no win-arm64/linux-arm64 binaries. Upgrading to TextMateSharp 2.0.3 (which depends on the separate `Onigwrap 1.0.11` package) provides native binaries for all 13 platform RIDs including win-arm64, linux-arm64, osx-arm64, and linux-musl-arm64. The `-r win-x64` workaround is no longer needed.

2. **FontStyle is a static class, not an enum**: TextMateSharp's `FontStyle` uses `int` constants (Bold=2, Italic=1, Underline=4, Strikethrough=8). Use bitwise AND (`& FontStyle.Bold`) not `HasFlag()`.

3. **Attribute ambiguity**: Both `System.Attribute` and `Terminal.Gui.Drawing.Attribute` exist in scope. Solved with `using TgAttribute = Terminal.Gui.Drawing.Attribute;` alias.

4. **Centralized package management**: All NuGet versions go in `Directory.Packages.props`, not individual csproj files. Using `<PackageVersion>` there + `<PackageReference>` without version in csproj.

5. **Target framework is net10.0** (not net8.0 as some docs state). C# 14 with LangVersion 14.

6. **VisualRole.Code derivation touches many files**: Adding a new VisualRole requires changes to: enum, Scheme (6 locations), SchemeJsonConverter, and all consumers (MarkdownCodeBlock, MarkdownAttributeHelper).

7. **Existing test needed updating**: Adding Bold to VisualRole.Code derivation changed the expected ANSI output for the InlineCode rendering test — had to update the assertion to include Bold escape code.

8. **Native library resilience is mandatory**: The `onigwrap` DLL crash on ARM64 only manifested at runtime (tests ran under x64 emulation). Production code using native interop must always catch `DllNotFoundException` and degrade gracefully — never let a missing native dependency crash the host application.

9. **Intermediate representations must propagate all data**: The `StyledSegment → InlineRun → StyledSegment` round-trip silently dropped the `Attribute` property because `InlineRun` didn't have one. Every intermediate type in a pipeline must carry all fields, or data will be silently lost. This is a classic "lossy conversion" bug — caught by tracing the pipeline end-to-end.

## Files Changed (Complete List)

### New Files
- `Terminal.Gui.SyntaxHighlighting/Terminal.Gui.SyntaxHighlighting.csproj`
- `Terminal.Gui.SyntaxHighlighting/TextMateSyntaxHighlighter.cs`
- `Tests/UnitTestsParallelizable/Drawing/SchemeTests.CodeRoleTests.cs`
- `Tests/UnitTestsParallelizable/Views/Markdown/SyntaxHighlighterPipelineTests.cs`
- `Tests/UnitTestsParallelizable/Views/Markdown/TextMateSyntaxHighlighterTests.cs`

### Modified Files
- `Directory.Packages.props` — TextMateSharp.Grammars 1.0.52
- `Terminal.sln` — Added SyntaxHighlighting project
- `Terminal.Gui/Drawing/VisualRole.cs` — Code enum member
- `Terminal.Gui/Drawing/Scheme.cs` — Code property, derivation, equality
- `Terminal.Gui/Configuration/SchemeJsonConverter.cs` — "code" case
- `Terminal.Gui/Views/Markdown/ISyntaxHighlighter.cs` — ResetState()
- `Terminal.Gui/Views/Markdown/StyledSegment.cs` — Attribute property
- `Terminal.Gui/Views/Markdown/MarkdownAttributeHelper.cs` — explicit Attribute guard
- `Terminal.Gui/Views/Markdown/MarkdownView.Parsing.cs` — fence language extraction
- `Terminal.Gui/Views/Markdown/MarkdownCodeBlock.cs` — VisualRole.Code
- `Tests/UnitTestsParallelizable/UnitTests.Parallelizable.csproj` — SyntaxHighlighting ref
- `Tests/UnitTestsParallelizable/Drawing/SchemeTests.cs` — Code assertion
- `Tests/UnitTestsParallelizable/Views/Markdown/MarkdownViewTests.cs` — Bold assertion fix
- `Examples/mdv/mdv.csproj` — SyntaxHighlighting reference
- `Examples/mdv/Program.cs` — TextMateSyntaxHighlighter wiring
- `Examples/UICatalog/UICatalog.csproj` — SyntaxHighlighting reference
- `Examples/UICatalog/Scenarios/Markdown.cs` — TextMateSyntaxHighlighter
- `Examples/UICatalog/Scenarios/MarkdownTester.cs` — TextMateSyntaxHighlighter
