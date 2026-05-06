# Plan: Emit OSC 8 Hyperlink Sequences in `ToAnsi()`

## Problem Statement

When `Driver.ToAnsi()` is called (e.g., for print mode in `clet --help` or `mdv --print`), the resulting ANSI string includes SGR styling (underline, color) for links but does NOT include [OSC 8 hyperlink sequences](https://gist.github.com/egmontkob/eb114294efbcd5adb1944c9f3cb5feda). This means links are visually styled but not clickable in terminals that support OSC 8.

## Existing Infrastructure

The codebase already has all the pieces:

1. **Cell-level URL storage**: `IOutputBuffer.GetCellUrl(col, row)` returns the URL associated with a cell. `OutputBufferImpl` stores these in a `_urlMap` dictionary keyed by `Point(col, row)`.

2. **URL assignment during draw**: Both `Link` view and `Markdown` view set `Driver.CurrentUrl` before drawing cells. The `OutputBufferImpl.AddStr`/`AddRune` methods call `SetCellUrl` when `CurrentUrl` is non-null.

3. **OSC 8 utilities**: `EscSeqUtils.OSC_StartHyperlink(url)` and `EscSeqUtils.OSC_EndHyperlink()` already exist.

4. **Real-time rendering already works**: `OutputBase.Write(IOutputBuffer)` already queries `buffer.GetCellUrl(col, row)` and emits OSC 8 sequences during live terminal output.

5. **`ToAnsi()` does NOT emit OSC 8**: `BuildAnsiForRegion` (called by `ToAnsi`) only handles SGR attribute changes and graphemes — it has no URL tracking.

## Design

### Approach

Modify `BuildAnsiForRegion` in `OutputBase.cs` to track the current URL state and emit OSC 8 open/close sequences when the URL changes between cells.

### Implementation

In `BuildAnsiForRegion`, add a `string? lastUrl = null` tracker. For each cell:
1. Query `buffer.GetCellUrl(col, row)` to get the cell's URL
2. If the URL differs from `lastUrl`:
   - If `lastUrl` was non-null, emit `EscSeqUtils.OSC_EndHyperlink()` to close the previous link
   - If the new URL is non-null, emit `EscSeqUtils.OSC_StartHyperlink(url)` to open the new link
   - Update `lastUrl`
3. After the loop completes (or at end of each row), if `lastUrl` is non-null, emit `EscSeqUtils.OSC_EndHyperlink()`

### Files Changed

- `Terminal.Gui/Drivers/Output/OutputBase.cs` — `BuildAnsiForRegion` method

## Unit Tests

Tests in `Tests/UnitTestsParallelizable/Drivers/Output/OutputBaseTests.cs`:

1. **`ToAnsi_CellsWithUrl_EmitsOsc8Sequences`**: Create a buffer, set `CurrentUrl`, write text, verify `ToAnsi()` output contains OSC 8 start/end sequences.

2. **`ToAnsi_CellsWithDifferentUrls_EmitsCorrectTransitions`**: Verify that transitioning between different URLs properly closes the first and opens the second.

3. **`ToAnsi_CellsWithUrl_ThenNoUrl_ClosesHyperlink`**: Verify that when URL cells are followed by non-URL cells, the hyperlink is properly closed.

4. **`ToAnsi_LegacyConsole_NoOsc8`**: Verify that legacy console mode does not emit OSC 8.

## Verification

- Existing `LinkTests.Link_Renders_With_OSC8_Hyperlink` test already verifies OSC 8 in live rendering
- New tests verify OSC 8 in `ToAnsi()` output specifically
- Run `MarkdownViewTests` to ensure no regressions
