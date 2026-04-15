# Plan: Add `VisualRole.Code` to the API

## Problem Statement

The `MarkdownCodeBlock` view currently uses `VisualRole.Editable` as a workaround for rendering code blocks, with a TODO comment noting: *"We should ideally be using the 'code' role here, but it doesn't exist yet."* Similarly, `MarkdownAttributeHelper` maps `InlineCode` and `CodeBlock` styles through `VisualRole.Editable`.

A dedicated `VisualRole.Code` would provide semantic clarity — code blocks are not editable, they are a distinct visual concept (monospace/fixed-width content with a differentiated background). This role should be usable by `MarkdownView`, `MarkdownCodeBlock`, and any future view that displays source code or preformatted text.

## Design Analysis: Two Layers, Not One

### The VisualRole vs MarkdownStyleRole distinction

`VisualRole` and `MarkdownStyleRole` operate at different abstraction layers:

| | `VisualRole` (View-level) | `MarkdownStyleRole` (Content-level) |
|---|---|---|
| **Scope** | Semantic state of a view/element | Semantic type of inline text |
| **Examples** | Normal, Focus, Disabled, Code | Heading, Emphasis, Strong, Quote |
| **Needs distinct colors?** | Yes — full `Attribute` (fg, bg, style) | Mostly no — just `TextStyle` flags on a base `Attribute` |
| **Themeable via JSON?** | Yes (`Scheme`) | Not today, but should be |
| **Resolved by** | `Scheme.GetAttributeForRole` | `MarkdownAttributeHelper.GetAttributeForSegment` (hardcoded) |

**Key insight**: `VisualRole` = "what state is this view/element in?" (needs colors/bg). `MarkdownStyleRole` = "what kind of text is this?" (needs style flags). They are complementary, not redundant.

Most `MarkdownStyleRole` members are just `Normal + TextStyle.Bold` or `Normal + TextStyle.Italic` — they don't need distinct colors/backgrounds. `Code` is the exception: it genuinely needs a distinct background color (like `Editable`/`ReadOnly`), which is why it belongs in `VisualRole`.

### Why not merge everything into VisualRole?

Expanding `VisualRole` to absorb all `MarkdownStyleRole` members would cause combinatorial explosion. Each `VisualRole` member requires: backing field, property, copy-ctor line, `TryGetExplicitlySetAttributeForRole` case, `GetAttributeForRoleCore` derivation case, `Equals`/`GetHashCode`/`ToString` updates, JSON converter case, schema entry, and docs. Adding 14 Markdown roles would bloat `Scheme` massively for what are mostly just `TextStyle` flag toggles.

### Requirement: Distinctive colors/styles must be configurable

Tools like **Glow** (Glamour), **Bat**, and **Rich** demonstrate that TUI Markdown rendering benefits from distinctive, themeable colors per element type — headings in blue, links in green, code with a contrasting background, comments in gray, etc. None of these tools truly "render" Markdown as a GUI would — they pretty-print it with ANSI styling. Terminal.Gui's `MarkdownView` actually renders Markdown as a proper interactive view, which is a stronger model, but it should support equally rich visual differentiation.

Today, `MarkdownAttributeHelper.GetAttributeForSegment` hardcodes all style mappings. This is not themeable. The future direction should make these configurable, but the right mechanism is **not** expanding `VisualRole` for every text role. Instead:

### Future direction: Themeable text roles (Issue #17 relevance)

A lighter-weight system (e.g., a `TextRole` → `TextStyle` + optional color override mapping, or making `MarkdownStyleRole` → `Attribute` configurable on `MarkdownView` itself) would serve both Markdown and a future `AttributedLabel` (Issue #17) without bloating `Scheme`. This is out of scope for this PR but is the natural next step.

## Approach

For this PR: add `VisualRole.Code` following the pattern established by `Editable` and `ReadOnly`. This addresses the immediate need (code blocks get a proper semantic role with distinct colors) and is the correct granularity for `VisualRole`.

### Derivation Algorithm for `Code`

When not explicitly set, `Code` derives from `Editable`:
- **Foreground**: Same as `Editable`'s foreground.
- **Background**: `Editable`'s background dimmed by 20% (via `GetDimmerColor(0.2, isDark)`).
- **Style**: `Editable`'s style with `TextStyle.Bold` added.

This matches the industry pattern (Glamour uses bold + dimmed background for code blocks). Theme authors can override `Code` explicitly in JSON config for full control, including distinct colors per theme (e.g., green-on-black for phosphor themes).

The `ISyntaxHighlighter` interface already exists in the Markdown namespace for per-token syntax highlighting within code blocks. `VisualRole.Code` provides the *base* attribute for the code region; syntax highlighting layers on top — the correct separation of concerns.

## Todos

### 1. Add `VisualRole.Code` enum member
**File**: `Terminal.Gui/Drawing/VisualRole.cs`  
Add `Code` after `ReadOnly` with XML docs describing it as the visual role for preformatted/code content (e.g., `MarkdownCodeBlock`, inline code).

### 2. Add `Code` backing field, property, and wiring in `Scheme`
**File**: `Terminal.Gui/Drawing/Scheme.cs`  
- Add `private readonly Attribute? _code;` backing field.
- Add `public Attribute Code { get; init; }` property (same pattern as `Editable`/`ReadOnly`).
- Add `_code` to the copy constructor (`Scheme(Scheme? scheme)`).
- Add `VisualRole.Code => _code` to `TryGetExplicitlySetAttributeForRole`.
- Add `case VisualRole.Code:` to `GetAttributeForRoleCore` with the derivation algorithm.
- Add `Code` to `Equals`, `GetHashCode`, and `ToString`.

### 3. Update `Scheme` class-level XML docs
**File**: `Terminal.Gui/Drawing/Scheme.cs`  
Add the `Code` derivation description to the algorithm list in the `<remarks>` block (between `ReadOnly` and `Disabled`).

### 4. Update `SchemeJsonConverter`
**File**: `Terminal.Gui/Configuration/SchemeJsonConverter.cs`  
Add `"code" => scheme with { Code = attribute }` case to the `Read` method's switch. The `Write` method already iterates `Enum.GetValues<VisualRole>()` so it will pick up the new role automatically.

### 5. Update JSON config schema
**File**: `docfx/schemas/tui-config-schema.json`  
Add `"Code"` property with `$ref` to `#/definitions/attribute` and description.

### 6. Update `MarkdownCodeBlock` to use `VisualRole.Code`
**File**: `Terminal.Gui/Views/Markdown/MarkdownCodeBlock.cs`  
Replace `GetAttributeForRole (VisualRole.Editable)` with `GetAttributeForRole (VisualRole.Code)`. Remove the TODO comments.

### 7. Update `MarkdownAttributeHelper` to use `VisualRole.Code`
**File**: `Terminal.Gui/Views/Markdown/MarkdownAttributeHelper.cs`  
Replace `view.GetAttributeForRole (VisualRole.Editable)` in the `InlineCode`/`CodeBlock` case with `view.GetAttributeForRole (VisualRole.Code)`.

### 8. Add unit tests
**File**: `Tests/UnitTestsParallelizable/Drawing/SchemeTests.cs` (and related test files)  
- Test that `Code` is not explicitly set by default.
- Test that `Code` derives correctly from `Editable`.
- Test that explicitly setting `Code` is returned as-is.
- Test copy constructor preserves `Code`.
- Test JSON round-trip serialization/deserialization of `Code`.
- Test `Equals`/`GetHashCode` includes `Code`.
- Test `ToString` includes `Code`.

### 9. Update documentation
**Files**: `docfx/docs/scheme.md`, `docfx/includes/scheme-overview.md`  
Mention `Code` in the derivation-aware roles list and any role tables.

### 10. Update built-in themes in `config.json`
**File**: `Terminal.Gui/Resources/config.json`  
Add explicit `Code` attributes to built-in themes to ensure distinctive styling. At minimum, phosphor/retro themes should have visually distinct code styling. Other themes should also specify `Code` to demonstrate the capability and provide good defaults. This follows the pattern of tools like Bat and Glow where code blocks are immediately visually distinct.

## Verification

1. `dotnet build --no-restore` — no new warnings.
2. `dotnet test --project Tests/UnitTestsParallelizable --no-build` — all tests pass including new ones.
3. `dotnet test --project Tests/UnitTests --no-build` — existing tests still pass.

## Notes

- The JSON `Write` path in `SchemeJsonConverter` uses `Enum.GetValues<VisualRole>()` so serialization is automatic.
- All existing code using `VisualRole.Editable` for non-code purposes (TextField, TextView, HexView, etc.) is unaffected.
- The `default:` case in `GetAttributeForRoleCore` already falls back to `Normal`, so forgetting to add the `Code` case would degrade gracefully (but we won't forget).
- `ISyntaxHighlighter` already exists for per-token syntax coloring within code blocks. `VisualRole.Code` is the base attribute; syntax highlighting layers on top.

## Future Work (Out of Scope)

- **Themeable `MarkdownStyleRole` mappings**: Make heading color, link color, emphasis style, etc. configurable — either on `MarkdownView` itself or via a lightweight `TextRole` → `Attribute` system. This would bring Terminal.Gui's Markdown rendering to parity with Glow/Bat/Rich theming capabilities without bloating `VisualRole`/`Scheme`.
- **Heading rendering — follow Glow's approach**: Glow/Glamour takes a hybrid approach to Markdown rendering. It renders structural elements (tables → box-drawn tables, `*` → bullet glyphs, code → distinct background blocks) but deliberately preserves `#`/`##`/`###` markers on headings, styling them blue+bold. This communicates heading level without needing font-size variation (impossible in terminals). Terminal.Gui's `MarkdownView` currently strips markers entirely. The goal is to adopt Glow's approach: **keep heading markers visible and use color/style to differentiate** — more information-preserving for technical users who think in Markdown. This should be addressed when `MarkdownStyleRole` theming is implemented.
- **Issue #17 (Attributed Label)**: A `TextRole` system would also serve an attributed label control, enabling inline styled text with semantic roles mapped to configurable attributes.
- **Syntax highlighting themes**: Extend `ISyntaxHighlighter` to support themeable token-level coloring (keywords, strings, comments in distinct colors), similar to Bat's `.tmTheme` integration.
