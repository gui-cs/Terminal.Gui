# MarkdownView PR #4955 — Session Learnings

Accumulated knowledge from building the MarkdownView, MarkdownCodeBlock, and MarkdownTable views.

---

## 1. DimAuto(Content) and SubViews — The Core Constraint

**Rule**: When `ContentSizeTracksViewport == false` AND `InternalSubViews.Count == 0`, DimAuto uses `ContentSize` directly. When `InternalSubViews.Count > 0`, DimAuto **ignores ContentSize** and calculates size from SubView extents.

**Implication**: If a view has SubViews AND calls `SetContentSize`, the ContentSize is **ignored** for auto-sizing. This is why the MarkdownCodeBlock refactor to remove the Button SubView was necessary — it lets `SetContentSize` (or explicit dimension setting) drive DimAuto.

**Reference**: `Terminal.Gui/ViewBase/Layout/DimAuto.cs`, lines ~215-219.

## 2. SetContentSize Side Effects

Calling `SetContentSize()` sets `ContentSizeTracksViewport = false`. This has a critical side effect: it restricts the Viewport to the ContentSize dimensions. When a view's Width is `Dim.Fill()` (as when embedded in MarkdownView), the Viewport width gets clamped to ContentSize width instead of following the Frame width.

**Workaround used**: Instead of `SetContentSize`, set `Width` and `Height` to explicit absolute values when content changes. Check `if (Width is DimAuto)` before overriding to avoid clobbering `Dim.Fill()` set by the parent.

```csharp
// Instead of: SetContentSize (new Size (maxWidth, _lines.Count));
if (Width is DimAuto)
{
    Width = maxWidth;
}

if (Height is DimAuto)
{
    Height = _lines.Count;
}
```

## 3. ShadowStyles.None vs null

`ShadowStyle = ShadowStyles.None` is **NOT** the same as `ShadowStyle = null`. The `SetShadow(ShadowStyles.None)` still adds margin thickness because the internal check is `if (style is { })` which is true for any non-null value including `None`.

**Always use `ShadowStyle = null`** to mean "no shadow, no thickness."

## 4. FillRect and Attribute Management

`View.FillRect(rect, Rune)` does NOT set the attribute — it relies on the caller having called `SetAttribute()` beforehand. The `View.FillRect(rect, Color?)` overload manages its own attribute internally.

When filling a code block background, the FillRect must use the **dimmed** background color (`normal.Background.GetDimmerColor()`), not the normal background. The text segments get their attribute from `MarkdownAttributeHelper.GetAttributeForSegment` which applies `GetDimmerColor()` internally via `MakeCodeAttribute`. If the fill uses `normal.Background` directly, the background fill won't match the text background, causing a visible seam.

## 5. Drawing Order in View Hierarchy

The draw order within a single View's `Draw()` method is:
1. Draw Adornments (Border, Padding)
2. Clear Viewport (`DoClearViewport`)
3. **Draw SubViews** (recursive `Draw()` on each SubView)
4. Draw Text (`DoDrawText`)
5. Draw Content (`DoDrawContent` → `OnDrawingContent`)
6. Draw Adornment SubViews
7. Render LineCanvas

**Key insight**: SubViews draw BEFORE the parent's content. The parent's ClearViewport fills the area with the normal background first, then SubViews draw on top. If a SubView's `OnDrawingContent` returns `true`, the framework skips default content drawing for that SubView.

## 6. Copy Button Refactor: Button SubView → Draw + Hit Test

Replacing a real `Button` SubView with draw-time rendering + `OnMouseEvent` hit testing:

**Before**: A `Button` SubView at `AnchorEnd(1)` position with custom text. This caused:
- DimAuto(Content) to use SubView extents instead of ContentSize
- Shadow thickness issues even with `ShadowStyles.None`
- Complex layout interactions

**After**: Override `OnMouseEvent` to check for `LeftButtonClicked` at `(Viewport.Width - 1, 0)`, draw the glyph in `OnDrawingContent` at the same position.

```csharp
protected override bool OnMouseEvent (Mouse mouse)
{
    if (!mouse.Flags.HasFlag (MouseFlags.LeftButtonClicked)) return false;
    if (mouse.Position is not { } pos) return false;
    if (pos.X != Viewport.Width - 1 || pos.Y != 0) return false;

    App?.Clipboard?.TrySetClipboardData (ExtractText ());
    return true;
}
```

## 7. Command System API Quick Reference

- `CommandImplementation` is `delegate bool? (ICommandContext? ctx)` — returns `null` (no event), `false` (not handled), `true` (handled)
- `AddCommand(Command, CommandImplementation)` — context-aware handler
- `AddCommand(Command, Func<bool?>)` — simple handler (no context)
- `MouseBindings.Add(MouseFlags, Command)` — binds mouse events to commands
- `MouseBinding.MouseEvent` — gets the `Mouse` data (not `.Mouse`)
- `ICommandContext.Binding` — pattern match with `is MouseBinding mb` to get mouse data
- `Mouse.Position` is `Point?` (nullable)
- `Mouse.Flags` uses `MouseFlags` enum (e.g., `MouseFlags.LeftButtonClicked`)

## 8. MarkdownView Drawing Architecture

### Code Block Lines
MarkdownView's `OnDrawingContent` **skips** lines marked `IsCodeBlock` — those are drawn by `MarkdownCodeBlock` SubViews positioned at the correct content Y coordinate.

### Table Lines
Same pattern: lines marked `IsTable` are skipped; `MarkdownTable` SubViews handle them.

### SubView Positioning
Code blocks and tables are created in `SyncCodeBlockViews()` / `SyncTableViews()` with `X = 0, Y = startLine, Width = Dim.Fill()`. They overlay the corresponding rendered lines.

## 9. Constructor Pattern for Compound Views

All three Markdown views use parameterless constructors + property initializers:

```csharp
// ✅ Correct
MarkdownCodeBlock codeBlock = new ()
{
    StyledLines = codeLines,
    X = 0,
    Y = start,
    Width = Dim.Fill ()
};

// ❌ Old pattern (removed)
MarkdownCodeBlock codeBlock = new (codeLines);
```

**Initializer order matters**: The constructor sets defaults (e.g., `Width = Dim.Auto()`), then property setters fire (e.g., `StyledLines` setter calls `UpdateContentSize()`), then remaining initializer properties set (e.g., `Width = Dim.Fill()` overrides). This sequencing is critical for understanding when `UpdateContentSize` runs relative to dimension overrides.

## 10. Focus and Active Link Interaction

When MarkdownView receives focus, `OnAdvancingFocus` sets `_activeLinkIndex = 0`. The active-link highlight in drawing reverses colors. To prevent this from affecting style-only tests, set `CanFocus = false` on the MarkdownView.

`OnHasFocusChanged` resets `_activeLinkIndex = -1` on focus loss. The active-link branch in drawing now also emits OSC8 escape sequences (for terminal hyperlinks).

## 11. MarkdownTable — No Drawing in Layout

`MarkdownTable.Recalculate()` was originally called from the draw codepath. This is a no-no — setting `Height` during draw triggers layout, which can cause infinite loops. `Recalculate` should only be called from:
- The `Data` property setter
- `OnSubViewLayout` (which runs during layout, not draw)

## 12. Test Conventions

- Style/rendering tests: set `CanFocus = false` to avoid focus-related attribute changes
- Use `app.Driver.GetOutput().GetLastOutput()` to capture ANSI output for verification
- ANSI color codes: `103m` = bright yellow bg (dimmed white), `107m` = bright white bg
- `CountOccurrences` helper for counting ANSI escape sequences in output
- xUnit v3 filtering: `--filter-class "*ClassName"`, `--filter-method "*MethodName"`

## 13. Remaining Work (Dim.Auto Rollout)

| View | Status | Notes |
|------|--------|-------|
| MarkdownCodeBlock | ✅ Done | No SubViews, explicit Width/Height from content |
| MarkdownTable | Pending | Needs same pattern: explicit dims from content |
| MarkdownView | Pending | Depends on table completion |

The pattern for each: set `Width/Height = Dim.Auto(Content)` as defaults in constructor, then in the content-update method, set explicit absolute dimensions when the current Dim is still DimAuto. When embedded (parent sets `Width = Dim.Fill()`), the Fill overrides Auto gracefully.
