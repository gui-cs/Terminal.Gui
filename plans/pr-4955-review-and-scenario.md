# PR #4955 — Review Issues & Markdown Scenario Rewrite

## Part 1: Issues Found in PR Review

### 🔴 Bug: Infinite loop in `ParseInlines` (Critical)

**File:** `Terminal.Gui/Views/Markdown/MarkdownView.Parsing.cs`, lines 272–283

When the parser encounters a stray `!`, `[`, `` ` ``, or `*` that doesn't form valid
inline markup (e.g. `"Hello ! world"`), `FindNextSpecialToken` returns the same index
as `idx`. The code sets `idx = nextSpecial` without advancing, causing an infinite loop.

**Fix:** After the `idx = nextSpecial` assignment, if `nextSpecial == idx` (meaning the
special character couldn't be consumed by any parser), advance `idx` by 1 and emit the
single character as plain text.

### 🔴 40 New CS1591 Warnings (PR Requirement Violation)

Nearly all public API members have empty `<summary>` tags or no XML docs at all.
The PR checklist states "PRs must not introduce new compiler warnings."

**Affected files:**
- `MarkdownView.cs` — empty `<summary>` on class, constructors, properties, events, virtual methods
- `StyledSegment.cs` — `MarkdownStyleRole` enum (all 14 members), `StyledSegment` class + all members
- `ISyntaxHighlighter.cs` — interface + method
- `MarkdownLinkEventArgs.cs` — class + all members
- `MarkdownView.Drawing.cs` — `OnDrawingContent` override
- `MarkdownView.Mouse.cs` — `OnMouseEvent` override

**Fix:** Add meaningful XML docs to all public/protected members.

### 🟡 Two Public Types in One File

**File:** `StyledSegment.cs` contains both `MarkdownStyleRole` (enum) and `StyledSegment`
(class). Per the "one type per file" rule, these need separate files.

**Fix:** Move `MarkdownStyleRole` to its own `MarkdownStyleRole.cs` file.

### 🟡 Markdig Parsed but AST Unused

**File:** `MarkdownView.Parsing.cs:27`

```csharp
_ = Markdig.Markdown.Parse (_markdown, pipeline);
```

The Markdig AST is discarded. Lowering is done via custom regex on source text.
The PR description says "intentionally unused in v1 lowering" but this makes Markdig
a runtime dependency that only validates syntax — wasteful.

**Recommendation:** Either use the Markdig AST for lowering (preferred, would also fix
edge cases the regex parser misses) or defer the dependency until it's actually used.

### 🟡 `MarkdownLinkEventArgs` Doesn't Use CWP Pattern

The project uses `CancelEventArgs<T>` for cancellable events. `MarkdownLinkEventArgs`
uses a custom `Handled` property instead of inheriting from the standard base.

### 🟡 PR Title Format

Should be `"Fixes #issue. Terse description."` per checklist — no linked issue number.

---

## Part 2: Markdown Scenario Rewrite

### Goal

Rewrite `Examples/UICatalog/Scenarios/Markdown.cs` to be a documentation browser:
- **Left panel:** ListView of all `.md` files from `docfx/docs/` (fetched from GitHub)
- **Right panel:** `MarkdownView` rendering the selected document
- **Async loading:** Both the file list and individual doc content loaded via HTTP
- **Spinner:** SpinnerView shown during loading operations

### Data Source

- **List endpoint:** `https://api.github.com/repos/gui-cs/Terminal.Gui/contents/docfx/docs?ref=develop`
  - Returns JSON array with `name` and `download_url` for each file
  - Filter to `.md` files only
- **Content endpoint:** Each item's `download_url` (e.g. `https://raw.githubusercontent.com/gui-cs/Terminal.Gui/develop/docfx/docs/application.md`)

### UI Layout

```
┌─ Markdown ──────────────────────────────────────────┐
│ ┌─ Docs ──────────┐┌─ <filename> ─────────────────┐ │
│ │ ansihandling.md  ││ # Application                │ │
│ │ application.md   ││                              │ │
│ │ arrangement.md   ││ Terminal.Gui provides...     │ │
│ │ borders.md       ││ ...                          │ │
│ │ ...              ││                              │ │
│ └──────────────────┘└──────────────────────────────┘ │
│  Quit               Loading...  ◐                    │
└──────────────────────────────────────────────────────┘
```

### Implementation Steps

#### Todo: `create-scenario-layout`
Create the scenario with Window, ListView (left, ~25 cols), MarkdownView (right, Fill),
and StatusBar with quit shortcut and spinner shortcut.

#### Todo: `async-fetch-file-list`
On scenario start:
1. Show SpinnerView (AutoSpin=true) in status bar with "Loading..." text
2. Use `HttpClient.GetAsync` on a background task to fetch the GitHub API listing
3. Parse JSON to extract `name` and `download_url` for `.md` files
4. `Application.Invoke` to marshal back to UI thread
5. Populate ListView via `SetSource` with the file names
6. Hide spinner

#### Todo: `async-fetch-doc-content`
On ListView selection change (via `ValueChanged` event):
1. Show spinner with "Loading <filename>..."
2. Fetch raw markdown content from `download_url` via `HttpClient.GetStringAsync`
3. `Application.Invoke` to set `MarkdownView.Markdown` and update FrameView title
4. Hide spinner

#### Todo: `error-handling`
Handle HTTP errors gracefully — show error message in the MarkdownView rather than crashing.

### Patterns to Follow

- **HttpClient:** Follow `UcdApiClient.cs` pattern (static `HttpClient`, `.ConfigureAwait(false)`)
- **Async + UI update:** Follow `ChatView.cs` pattern (`_app.Invoke(() => { ... })`)
- **SpinnerView:** Follow `ChatView.cs` pattern (`AutoSpin = false`, toggled visible on demand)
  Or simpler: `AutoSpin = true`, toggle `Visible`
- **ListView:** Use `SetSource` with `ObservableCollection<string>` or list of display names
- **GitHub API:** Use `User-Agent` header (required by GitHub API)
