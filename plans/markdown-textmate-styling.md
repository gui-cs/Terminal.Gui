# Plan: TextMate-Based Markdown Styling

## Problem Statement

Currently, all non-code-block markdown elements (headers, bold, italic, links, quotes, etc.) are styled using hardcoded `TextStyle` flags (Bold, Italic, Faint, etc.) applied to the `Normal` attribute. This produces monochrome rendering — no color differentiation between element types. Additionally:

1. **Heading `#` prefix is always stripped** — H1-H6 look identical (all just Bold).
2. **MarkdownCodeBlock standalone** has no `SyntaxHighlighter` or `Language` property — can't highlight code on its own.
3. **MarkdownTable standalone** has no syntax highlighter wiring.
4. **Embedded `md` code blocks** (in `EnableForDesign`) don't render with markdown styling.
5. **`MarkdownStyleRole` → `TextStyle` mapping** doesn't leverage theme colors at all.

## Proposed Approach

Use the **TextMate markdown grammar** (already bundled in TextMateSharp.Grammars) to style the entire markdown document. The TextMate theme becomes the single source of truth for all markdown element colors.

### Architecture

The key insight: **don't tokenize raw markdown with TextMate**. Instead, use TextMate's **theme color resolution** (`theme.Match(scopes)`) to map markdown element scopes to `Attribute` values, then apply those through the existing pipeline.

```
Parse → MarkdownStyleRole → TextMate scope lookup → Attribute → Draw
```

Each `MarkdownStyleRole` maps to a TextMate scope:
| MarkdownStyleRole | TextMate Scope |
|---|---|
| Heading | `markup.heading.markdown` |
| Emphasis | `markup.italic.markdown` |
| Strong | `markup.bold.markdown` |
| InlineCode | `markup.inline.raw.string.markdown` |
| Link | `markup.underline.link.markdown` |
| Quote | `markup.quote.markdown` |
| ListMarker | `punctuation.definition.list.begin.markdown` |
| ImageAlt | `markup.italic.markdown` (reuse) |
| TaskDone | `markup.strikethrough.markdown` |
| ThematicBreak | `meta.separator.markdown` |

This means `MarkdownAttributeHelper.GetAttributeForSegment()` checks for a `SyntaxHighlighter` first, queries it for the scope-based attribute, and falls back to current `TextStyle` logic when no highlighter is set.

### Heading Prefix Visibility

Add `ShowHeadingPrefix` property (default: `true`). When true, include the `#` / `##` / `###` characters in the rendered heading — they get styled by the theme's `punctuation.definition.heading.markdown` scope. When false, strip them (current behavior).

### Standalone Code Block / Table Support

- Add `SyntaxHighlighter` and `Language` properties to `MarkdownCodeBlock` so it can highlight code standalone.
- Add `SyntaxHighlighter` property to `MarkdownTable` for inline formatting with theme colors.
- When `MarkdownView` creates these SubViews, propagate its `SyntaxHighlighter` to them.

### ISyntaxHighlighter Extension

Add a new method to `ISyntaxHighlighter`:
```csharp
Attribute? GetAttributeForScope (MarkdownStyleRole role);
```

This allows the highlighter to resolve a markdown style role to a theme-derived `Attribute`. The `TextMateSyntaxHighlighter` implementation uses the scope mapping table above to call `theme.Match(scopes)` and return the appropriate attribute.

## Phases

### Phase 1: ShowHeadingPrefix + heading level in IntermediateBlock
- Add `ShowHeadingPrefix` property to `MarkdownView` (default: `true`)
- Store heading level in `IntermediateBlock` (currently discarded)
- When `ShowHeadingPrefix = true`, prepend `# ` / `## ` etc. to heading InlineRuns with `MarkdownStyleRole.HeadingMarker` (new enum value)
- Tests: heading renders with `# ` prefix by default, without when `ShowHeadingPrefix = false`, level is preserved

### Phase 2: ISyntaxHighlighter.GetAttributeForScope + MarkdownAttributeHelper integration
- Add `GetAttributeForScope(MarkdownStyleRole)` to `ISyntaxHighlighter`
- Implement in `TextMateSyntaxHighlighter` with scope mapping table
- Update `MarkdownAttributeHelper.GetAttributeForSegment` to accept optional `ISyntaxHighlighter?`, query it first, fall back to `TextStyle` logic
- Propagate the highlighter through the draw pipeline (Layout/Drawing pass the highlighter)
- Tests: with highlighter, segments get theme-derived colors; without, they get current TextStyle treatment

### Phase 3: Standalone MarkdownCodeBlock + MarkdownTable
- Add `SyntaxHighlighter` and `Language` properties to `MarkdownCodeBlock`
- When `CodeLines` is set with a `SyntaxHighlighter` + `Language`, re-highlight through the highlighter
- `MarkdownView.SyncCodeBlockViews()` propagates `SyntaxHighlighter` and language to code blocks
- Add `SyntaxHighlighter` property to `MarkdownTable`
- `MarkdownView.FlushTableLines()` / `BuildRenderedLines()` propagates highlighter to tables
- Tests: standalone `MarkdownCodeBlock` with highlighter + language produces colored segments

### Phase 4: EnableForDesign + recursive md code blocks
- Verify that `EnableForDesign` (which sets `SyntaxHighlighter = new TextMateSyntaxHighlighter(DarkPlus)`) works with embedded ```md blocks
- The md code block should get markdown syntax highlighting via the "markdown" language grammar
- Tests: code block with language "md" produces theme-colored markdown tokens

## Files To Modify

### New/Modified Production Code
| File | Changes |
|---|---|
| `Terminal.Gui/Drawing/Markdown/MarkdownStyleRole.cs` | Add `HeadingMarker` enum value |
| `Terminal.Gui/Drawing/Markdown/ISyntaxHighlighter.cs` | Add `GetAttributeForScope(MarkdownStyleRole)` method |
| `Terminal.Gui/Drawing/Markdown/TextMateSyntaxHighlighter.cs` | Implement `GetAttributeForScope` with scope mapping |
| `Terminal.Gui/Drawing/Markdown/MarkdownAttributeHelper.cs` | Accept optional `ISyntaxHighlighter?`, query for scope-derived attributes |
| `Terminal.Gui/Views/Markdown/MarkdownView.cs` | Add `ShowHeadingPrefix` property |
| `Terminal.Gui/Views/Markdown/MarkdownView.Parsing.cs` | Include heading prefix when enabled; store heading level |
| `Terminal.Gui/Views/Markdown/IntermediateBlock.cs` | Add `HeadingLevel` property |
| `Terminal.Gui/Views/Markdown/MarkdownView.Drawing.cs` | Pass `SyntaxHighlighter` to `GetAttributeForSegment` |
| `Terminal.Gui/Views/Markdown/MarkdownCodeBlock.cs` | Add `SyntaxHighlighter`, `Language` properties; re-highlight on set |
| `Terminal.Gui/Views/Markdown/MarkdownTable.cs` | Add `SyntaxHighlighter` property; use in cell rendering |
| `Terminal.Gui/Views/Markdown/MarkdownView.Layout.cs` | Propagate highlighter to code blocks and tables |

### New/Modified Test Code
| File | Tests |
|---|---|
| `Tests/.../Views/Markdown/MarkdownViewTests.cs` | ShowHeadingPrefix tests, theme-styled heading tests |
| `Tests/.../Views/Markdown/SyntaxHighlighterPipelineTests.cs` | GetAttributeForScope tests, mock updates |
| `Tests/.../Views/Markdown/TextMateSyntaxHighlighterTests.cs` | GetAttributeForScope integration tests |
| `Tests/.../Views/Markdown/MarkdownCodeBlockTests.cs` | Standalone highlighting tests (new file if needed) |

## Design Decisions

1. **Query theme, don't re-tokenize**: Rather than running the markdown through TextMate tokenization (which would require mapping tokens back to the layout-processed text), we use `theme.Match(scopes)` to resolve colors. This keeps the existing parse/layout/draw pipeline intact.

2. **Fallback to TextStyle**: When no `ISyntaxHighlighter` is set, the current `TextStyle`-based rendering continues to work exactly as before. Zero breaking changes.

3. **Heading prefix is text, not decoration**: The `#` characters are real text content (InlineRuns), not drawn decorations. This means they participate in word-wrapping, selection, and copy correctly.

4. **Single heading attribute**: All heading levels (H1-H6) get the same color from the TextMate theme (`markup.heading.markdown`). The `#` count distinguishes them visually.

5. **HeadingMarker vs Heading roles**: The `#` prefix gets `MarkdownStyleRole.HeadingMarker` (mapped to `punctuation.definition.heading.markdown`) while the text gets `MarkdownStyleRole.Heading` (mapped to `markup.heading.markdown`). Most themes color these differently.

## Risk Assessment

1. **Scope mapping accuracy**: TextMate themes vary in which scopes they define colors for. Some themes may not color `markup.heading.markdown` differently from body text. Mitigation: fall back to `TextStyle` flags when `theme.Match()` returns no rules.

2. **Breaking test assertions**: Existing tests assert specific SGR codes (e.g., `\x1b[1m` for Bold). With a syntax highlighter, colors change. Mitigation: tests that validate theme-based styling are separate from tests that validate fallback styling. Existing tests pass `SyntaxHighlighter = null`.

3. **Performance**: `theme.Match()` is called per segment per draw. Mitigation: cache scope→Attribute mappings in the highlighter (they only change on `SetTheme()`).

## Lessons Learned (Prior Work)

- `FontStyle.NotSet = -1` (all bits set) — must guard with `>= 0` before bitwise checks
- Moving types between `Terminal.Gui.Views` and `Terminal.Gui.Drawing` namespaces requires no consumer changes due to global usings
- `Attribute?` on `InlineRun`/`StyledSegment` is the established pattern for explicit styling that bypasses role-based resolution
- TextMateSharp 2.0.3 + Onigwrap 1.0.11 provides all platform RIDs including ARM64

## Progress

### Phase 1: ShowHeadingPrefix + heading level ✅
- Added `ShowHeadingPrefix` property (default: true) to MarkdownView
- Added `MarkdownStyleRole.HeadingMarker` enum value
- Heading `#` prefix rendered as InlineRun with HeadingMarker role
- 8 new tests, all passing

### Phase 2: ISyntaxHighlighter.GetAttributeForScope ✅
- Added `GetAttributeForScope(MarkdownStyleRole)` to ISyntaxHighlighter
- Implemented multi-candidate scope mapping with lazy caching in TextMateSyntaxHighlighter
- Updated MarkdownAttributeHelper for 3-tier priority: explicit Attribute > scope-derived > TextStyle fallback
- 11 new tests, all passing

### Theme Auto-Detection ✅ (unplanned addition)
- Added `GetThemeForBackground(Color)` static method
- Dark terminals → DarkPlus, light terminals → LightPlus

### Phase 3: Standalone MarkdownCodeBlock ✅
- Made `ThemeBackground` public
- Added `SyntaxHighlighter` and `Language` public properties
- `CodeLines` setter now highlights through the highlighter when both are set
- Wired `SyntaxHighlighter` into code block draw path
- 5 new tests, all passing

### Phase 4: Standalone MarkdownTable + EnableForDesign ✅
- Added `SyntaxHighlighter` property to MarkdownTable
- Wired highlighter into table cell rendering
- MarkdownView.Layout.cs propagates highlighter to tables
- Verified EnableForDesign with embedded `md` code block works
- 4 new tests (2 table, 2 EnableForDesign/recursive), all passing

**All phases complete.** Full test suite: all tests pass (0 failures).

### Phase 5: Text Property Unification ✅
Unified content-setting API across all three markdown views to use `View.Text`:

- **MarkdownView**: Overrode `Text` (virtual on View) to route through `SetMarkdown` logic. Removed the `Markdown` property entirely. Updated all callers (6 example files, 3 test files).
- **MarkdownCodeBlock**: Overrode `Text` to accept fenced code block format (` ```lang\ncode\n``` `). The setter extracts `Language` from the opening fence, strips fences, and feeds code lines through `CodeLines` (which uses highlighter if set). Plain text (no fences) also works. The getter round-trips in fenced format.
- **MarkdownTable**: Overrode `Text` to accept pipe-delimited markdown table text. Parses via `TableData.TryParse()`, sets `Data` internally. Invalid/empty text clears the table. Getter reconstructs pipe format. Added constructor guard for `View()` base calling `Text = ""` before fields initialize.
- **EnableForDesign**: Both MarkdownCodeBlock and MarkdownTable now use `Text` + `SyntaxHighlighter` in EnableForDesign. MarkdownTable sample includes inline formatting (`**bold**`, `*italic*`, `` `code` ``).
- 10 new tests + all existing tests updated from `.Markdown` → `.Text`.

### Phase 6: UseThemeBackground + Theme Selectors ✅

Added `UseThemeBackground` property to `Markdown` and `MarkdownTable`:
- Default: `false` — no change to existing rendering
- When `true`, the view's background fills with the TextMate theme's `editor.background` color
- Rewrote `TextMateSyntaxHighlighter.CacheThemeDefaults()` to use `Theme.GetGuiColorDictionary()` for accurate per-theme backgrounds (previously `["source"]` scope match returned identical values for many themes)
- `MarkdownAttributeHelper.GetAttributeForSegment()` now accepts `Color? themeBackground` parameter — uses it for background instead of view's normal attribute when non-null
- `InlineCode`/`CodeBlock` segments only override background when `themeBackground` is non-null; without it, `VisualRole.Code` background is preserved
- `MarkdownView.Layout.cs` propagates `UseThemeBackground` to table views and code blocks

Fixed MarkdownTable constructor NRE:
- Fields (`_data`, `_columnWidths`, `_headerSegments`, `_rowSegments`, `_bodyRowHeights`) moved from constructor body to field initializers — prevents null reference during `View()` base constructor → `SetupText()` → `Text = ""` → `Recalculate()` call chain

UI wiring:
- **MarkdownTester**: Added `UseThemeBackground` toggle (CheckBox in StatusBar) + existing theme picker
- **Deepdives**: Added theme picker (`DropDownList<ThemeName>`) + `UseThemeBackground` toggle in StatusBar
- **mdv**: `UseThemeBackground = true` by default; added theme picker + toggle in StatusBar

4 new `SyntaxHighlighterPipelineTests` (theme background propagation), 2 new `TextMateSyntaxHighlighterTests` (per-theme defaults), all existing tests updated for new `GetAttributeForSegment` signature. Full suite: 0 failures.
