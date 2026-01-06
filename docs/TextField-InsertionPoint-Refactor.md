# TextField Refactor: InsertionPoint Nomenclature

## Overview

This document outlines the refactoring plan to rename cursor-related fields and methods in `TextField`, `DateField`, and `TimeField` to use "InsertionPoint" terminology.

### Rationale

The current naming uses "CursorPosition" for what is actually a **logical index into the text** where insertion/deletion occurs. This is confusing because:

1. **"Cursor"** implies the visible terminal cursor (screen position)
2. **"Position"** suggests coordinates, but it's really an index
3. The actual cursor positioning is done by `PositionCursor()` which converts the index to screen coordinates

The term **"InsertionPoint"** clearly indicates:
- Where new text will be inserted
- It's a logical position in the text, not a screen position
- For `TextView` (2D), this naturally extends to a `Point` (column, row)

---

## Renaming Tables

### TextField

| Current | Proposed | Notes |
|---------|----------|-------|
| `_cursorPosition` | `_insertionPoint` | Backing field (int for 1D) |
| `CursorPosition` | `InsertionPoint` | Public virtual property |
| `_preTextChangedCursorPos` | `_preChangeInsertionPoint` | Cache before edits |
| `_selectedStart` | `_selectionAnchor` | Where selection began |
| `_start` | `_selectionStart` | Normalized start for drawing |
| `CursorIsAtEnd()` | `InsertionPointIsAtEnd()` | Internal helper |
| `CursorIsAtStart()` | `InsertionPointIsAtStart()` | Internal helper |
| `PositionCursor(Mouse)` | `SetInsertionPointFromMouse()` | Private - sets insertion point from mouse |
| `PositionCursor(int, bool)` | `SetInsertionPointFromScreen()` | Private - sets insertion point from screen coords |
| `PositionCursor()` | **Keep as-is** | Actually positions terminal cursor on screen |

### DateField

| Current | Proposed | Notes |
|---------|----------|-------|
| `CursorPosition` (override) | `InsertionPoint` | Override of base property |
| `AdjCursorPosition()` | `AdjustInsertionPoint()` | Skip separators |
| `IncCursorPosition()` | `IncrementInsertionPoint()` | Move right |
| `DecCursorPosition()` | `DecrementInsertionPoint()` | Move left |

### TimeField

| Current | Proposed | Notes |
|---------|----------|-------|
| `CursorPosition` (override) | `InsertionPoint` | Override of base property |
| `AdjCursorPosition()` | `AdjustInsertionPoint()` | Skip separators |
| `IncCursorPosition()` | `IncrementInsertionPoint()` | Move right |
| `DecCursorPosition()` | `DecrementInsertionPoint()` | Move left |

### TextFieldAutocomplete

| Current | Proposed | Notes |
|---------|----------|-------|
| `SetCursorPosition()` | `SetInsertionPoint()` | Override from base class |

---

## Future: TextView (2D)

| Concept | TextField (1D) | TextView (2D) |
|---------|----------------|---------------|
| Insertion Point | `int` | `Point` (column, row) |
| Selection Anchor | `int` | `Point` |
| Selection Start | `int` | `Point` |

---

## Refactor Plan

### Phase 1: TextField Core

1. **Rename private fields:**
   - `_cursorPosition` → `_insertionPoint`
   - `_preTextChangedCursorPos` → `_preChangeInsertionPoint`
   - `_selectedStart` → `_selectionAnchor`
   - `_start` → `_selectionStart`

2. **Rename public property:**
   - `CursorPosition` → `InsertionPoint`

3. **Rename internal methods:**
   - `CursorIsAtEnd()` → `InsertionPointIsAtEnd()`
   - `CursorIsAtStart()` → `InsertionPointIsAtStart()`

4. **Rename private methods:**
   - `PositionCursor(Mouse mouse)` → `SetInsertionPointFromMouse(Mouse mouse)`
   - `PositionCursor(int x, bool getX)` → `SetInsertionPointFromScreen(int x, bool getX)`

5. **Update XML documentation** to reflect new terminology

### Phase 2: DateField

1. **Rename override property:**
   - `CursorPosition` → `InsertionPoint`

2. **Rename private methods:**
   - `AdjCursorPosition()` → `AdjustInsertionPoint()`
   - `IncCursorPosition()` → `IncrementInsertionPoint()`
   - `DecCursorPosition()` → `DecrementInsertionPoint()`

3. **Update all internal references** to use new names

4. **Update XML documentation**

### Phase 3: TimeField

1. **Rename override property:**
   - `CursorPosition` → `InsertionPoint`

2. **Rename private methods:**
   - `AdjCursorPosition()` → `AdjustInsertionPoint()`
   - `IncCursorPosition()` → `IncrementInsertionPoint()`
   - `DecCursorPosition()` → `DecrementInsertionPoint()`

3. **Update all internal references** to use new names

4. **Update XML documentation**

### Phase 4: TextFieldAutocomplete

1. **Rename override method:**
   - `SetCursorPosition()` → `SetInsertionPoint()`

### Phase 5: Update Tests

1. **Search for all test references** to renamed members
2. **Update test code** to use new names
3. **Verify all tests pass**

### Phase 6: Update Documentation

1. **Update class-level XML docs** in all three files
2. **Update any markdown documentation** referencing these APIs
3. **Update CLAUDE.md** if needed

---

## Files to Modify

- `Terminal.Gui/Views/TextInput/TextField.cs`
- `Terminal.Gui/Views/TextInput/DateField.cs`
- `Terminal.Gui/Views/TextInput/TimeField.cs`
- `Tests/UnitTests/Views/TextFieldTests.cs`
- `Tests/UnitTestsParallelizable/Views/TextFieldTests.cs`
- `Tests/UnitTestsParallelizable/Views/DateFieldTests.cs`
- `Tests/UnitTestsParallelizable/Views/TimeFieldTests.cs`
- Any other files referencing `CursorPosition` on TextField/DateField/TimeField

---

## Search Patterns for Refactoring

```bash
# Find all references to CursorPosition in TextField context
grep -r "CursorPosition" --include="*.cs" Terminal.Gui/Views/TextInput/
grep -r "CursorPosition" --include="*.cs" Tests/

# Find all references to the private fields
grep -r "_cursorPosition" --include="*.cs" .
grep -r "_selectedStart" --include="*.cs" .
grep -r "_start" --include="*.cs" Terminal.Gui/Views/TextInput/

# Find cursor-related method names
grep -r "CursorIsAt" --include="*.cs" .
grep -r "AdjCursorPosition" --include="*.cs" .
grep -r "IncCursorPosition" --include="*.cs" .
grep -r "DecCursorPosition" --include="*.cs" .
```

---

## Notes

- `PositionCursor()` (the public override) is **NOT renamed** because it actually positions the terminal cursor on screen
- No backward compatibility shims are needed per user request
- The refactor should be done in a single commit to maintain consistency
