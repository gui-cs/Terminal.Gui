# Parallel Agent Plan: Change View Default Activation to Released

**Status:** Ready for execution
**Created:** 2026-02-06
**Author:** Claude Opus 4.6
**Based on:** `change-default-activation-to-released.md`
**Related Issue:** #4674

---

## Current State

The **core code change is already complete**. `SetupMouse()` in `View.Mouse.cs` already
binds `LeftButtonReleased` to `Command.Activate`. The primary test suite
(`DefaultActivationTests.cs`, `MouseReleasedBindingTests.cs`, `MouseTests.cs`) has been
updated. `command.md` is updated.

**What remains** is stale documentation, view-specific test verification, example updates,
migration docs, and agent guidance updates. These are organized into **5 independent
streams** below that can run in parallel with zero file conflicts.

---

## Stream Overview

| Stream | Scope | Files Touched | Depends On |
|--------|-------|---------------|------------|
| **A** | Fix stale docs in `mouse.md` | `docfx/docs/mouse.md` | None |
| **B** | Review & fix view-specific tests | `Tests/.../CheckBoxTests.cs`, `OptionSelectorTests.cs`, `ComboBoxTests.cs`, `ColorPickerTests.cs` | None |
| **C** | Update MouseTester example | `Examples/UICatalog/Scenarios/MouseTester.cs` | None |
| **D** | Add migration guide section | `docfx/docs/migratingfromv1.md` | None |
| **E** | Update AI agent guidance | `AGENTS.md`, `CLAUDE.md` | None |
| **F** | Full test suite validation | (read-only) | A, B, C |

Streams A-E are fully independent and can run simultaneously.
Stream F is a validation gate that runs after A, B, and C complete.

---

## Stream A: Fix Stale Documentation in `mouse.md`

**File:** `docfx/docs/mouse.md`
**No other stream touches this file.**

### Task A.1: Fix "Default Mouse Bindings" code block (lines 141-148)

**Current (STALE):**
```csharp
MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.Ctrl, Command.Context);
```

**Replace with:**
```csharp
MouseBindings.Add (MouseFlags.LeftButtonReleased, Command.Activate);
MouseBindings.Add (MouseFlags.LeftButtonReleased | MouseFlags.Ctrl, Command.Context);
```

### Task A.2: Fix "Common Binding Patterns" section (lines 153-157)

**Current (STALE):**
```markdown
* **Click Events**: `MouseFlags.LeftButtonPressed` for selection/interaction
* **Context Menu**: `MouseFlags.RightButtonPressed` or `LeftButtonPressed | Ctrl`
```

**Replace with:**
```markdown
* **Activation**: `MouseFlags.LeftButtonReleased` for selection/interaction (default)
* **Context Menu**: `MouseFlags.RightButtonPressed` or `LeftButtonReleased | Ctrl`
```

### Task A.3: Fix "Default Bindings" summary in pipeline section (lines 576-578)

**Current (STALE):**
```markdown
- `LeftButtonPressed` → `Command.Activate`
- `LeftButtonPressed | Ctrl` → `Command.Context`
```

**Replace with:**
```markdown
- `LeftButtonReleased` → `Command.Activate`
- `LeftButtonReleased | Ctrl` → `Command.Context`
```

### Task A.4: Fix pipeline example heading (line 584)

**Current (STALE):**
```markdown
**Example - LeftButtonPressed → Command.Activate:**
```

**Replace with:**
```markdown
**Example - LeftButtonReleased → Command.Activate:**
```

### Task A.5: Add "Default Mouse Activation Behavior" section

After the "Best Practices" section (~line 620), add a new section explaining:

- Activation on release (default) - matches WPF, Cocoa, HTML, GTK4, Qt
- Cancellation pattern: press → drag away → release outside → no activation
- How to customize: `LeftButtonPressed` for instant activation, `LeftButtonClicked` for full click cycle
- Why Release instead of Clicked (simpler, no detection delay, matches Win32 WM_LBUTTONUP)

Use code examples following the project coding style (`new ()`, no `var`, `[...]` collections).

### Verification

```bash
grep -n "LeftButtonPressed" docfx/docs/mouse.md
```

Should return zero matches when complete (all references should now be `LeftButtonReleased`
or explicitly documented as custom/alternative patterns).

---

## Stream B: Review & Fix View-Specific Tests

**Files:** Only test files under `Tests/`. No overlap with any other stream.

The default activation changed from `LeftButtonPressed` to `LeftButtonReleased`. These
view-specific tests use `LeftButtonPressed` and need review to confirm they still work
correctly. The goal is NOT to rewrite tests, but to verify correctness and fix any that
are broken by the default change.

### Task B.1: CheckBoxTests.cs

**File:** `Tests/UnitTestsParallelizable/Views/CheckBoxTests.cs`
**Lines of interest:** 357, 372, 385

CheckBox uses default View bindings. Tests that send `LeftButtonPressed` and expect
`CheckState` changes may be broken if the activation now fires on Release.

**Action:** Run these specific tests. If they fail:
- Update to send both `LeftButtonPressed` + `LeftButtonReleased` (the full sequence)
- Or update assertions to check state after the release event

### Task B.2: OptionSelectorTests.cs

**File:** `Tests/UnitTestsParallelizable/Views/OptionSelectorTests.cs`
**Line of interest:** 198 (`LeftButtonPressed_On_NotActivated_Activates`)

**Action:** Run this test. Verify the test name still matches behavior. If the test
expects activation on press but default is now release, fix the test sequence.

### Task B.3: ComboBoxTests.cs

**File:** `Tests/UnitTests/Views/ComboBoxTests.cs`
**Multiple lines** use `LeftButtonPressed` for dropdown toggle.

**Action:** ComboBox has custom `OnMouseEvent` handling. Run all ComboBox tests.
Likely fine since ComboBox handles press events directly (not via default bindings),
but verify.

### Task B.4: ColorPickerTests.cs

**File:** `Tests/UnitTestsParallelizable/Views/ColorPickerTests.cs`
**Lines of interest:** 187, 218, 247, 263, 703, 710

**Action:** ColorBar uses custom mouse handling for slider positioning. Run these tests.
Likely fine since it handles `LeftButtonPressed` in its own `OnMouseEvent`.

### Verification

```bash
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~CheckBox" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~OptionSelector" --no-build
dotnet test Tests/UnitTests --filter "FullyQualifiedName~ComboBox" --no-build
dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~ColorPicker" --no-build
```

All must pass. If any fail, fix and document the fix.

---

## Stream C: Update MouseTester Example

**File:** `Examples/UICatalog/Scenarios/MouseTester.cs`
**No other stream touches this file.**

### Task C.1: Update comments to reflect new default behavior

In the `MouseEventDemoView` class (lines 355-413), update any comments that reference
the old `LeftButtonPressed` default.

### Task C.2: Add cancellation demonstration

Add a visual section or instructions showing:
- Default behavior: "Release to activate" with visual feedback on press
- Cancellation: Press inside, drag outside, release → no activation
- Comparison: Could add a view with custom `LeftButtonPressed` binding for contrast

### Task C.3: Verify scenario runs correctly

```bash
dotnet run --project Examples/UICatalog -- --scenario "Mouse Tester"
```

Ensure the demo correctly shows activation on release for default-bound views.

---

## Stream D: Add Migration Guide Section

**File:** `docfx/docs/migratingfromv1.md`
**No other stream touches this file.**

### Task D.1: Add "Mouse Activation" subsection under "Mouse API" section

Find the "Mouse API" section in the migration doc and add:

```markdown
### Default Mouse Activation Changed from Pressed to Released

**Breaking Change:** The default mouse activation binding changed from
`LeftButtonPressed` to `LeftButtonReleased`.

| Before | After |
|--------|-------|
| `LeftButtonPressed` → `Command.Activate` | `LeftButtonReleased` → `Command.Activate` |
| Activates immediately on press | Activates on release (cancellable) |

This aligns with industry-standard GUI conventions (Windows WPF/WinForms, macOS Cocoa,
Web HTML click events, GTK4, Qt) where activation on release allows cancellation of
accidental clicks.

**To restore old behavior:**
```csharp
view.MouseBindings.ReplaceCommands (MouseFlags.LeftButtonPressed, Command.Activate);
view.MouseBindings.Remove (MouseFlags.LeftButtonReleased);
```
```

### Verification

Review the section in context of the surrounding migration content. Ensure formatting
is consistent with the rest of the document.

---

## Stream E: Update AI Agent Guidance

**Files:** `AGENTS.md`, `CLAUDE.md`
**No other stream touches these files.**

### Task E.1: Add plans directory guidance to AGENTS.md

In the "For Library Contributors" section, add:

```markdown
### Implementation Plans

When creating implementation plans for significant changes, place them in the `./plans/`
directory at the repository root. Plans should include:
- Executive summary and rationale
- File-by-file change descriptions
- Testing strategy
- Migration considerations (if breaking change)
```

### Task E.2: Add plans directory guidance to CLAUDE.md

In the "Contributor Guide" section, add a note:

```markdown
## Implementation Plans

Place implementation plans in `./plans/` directory. See existing plans for format examples.
```

---

## Stream F: Full Test Suite Validation (Gate)

**Depends on:** Streams A, B, C completing.
**This stream is read-only / test-only.**

### Task F.1: Build

```bash
dotnet restore && dotnet build --no-restore
```

Must succeed with zero new warnings.

### Task F.2: Run parallelizable tests

```bash
dotnet test Tests/UnitTestsParallelizable --no-build
```

### Task F.3: Run non-parallel tests

```bash
dotnet test Tests/UnitTests --no-build
```

### Task F.4: Verify no stale references

```bash
# Should return no matches in the source library (only in tests where explicit custom bindings are tested)
grep -rn "LeftButtonPressed.*Command\.Activate" Terminal.Gui/
```

Must return zero results.

### Success Criteria

- All tests pass
- No new warnings introduced
- No stale `LeftButtonPressed` default references remain in library code or docs
- Documentation is internally consistent

---

## Agent Dispatch Matrix

For orchestrating parallel execution, dispatch agents as follows:

```
Time ─────────────────────────────────────────────────►

Agent 1: ██ Stream A (mouse.md fixes) ██
Agent 2: ██ Stream B (view test review) ████████████████
Agent 3: ██ Stream C (MouseTester example) ██
Agent 4: ██ Stream D (migration guide) ██
Agent 5: ██ Stream E (agent guidance) ██
                                          ▼
Agent 6:                          ██ Stream F (validation) ██
```

Each agent should:
1. Read this plan document first
2. Read `.claude/REFRESH.md` before editing files
3. Follow all coding rules in `.claude/rules/`
4. Only touch files listed in their stream
5. Commit with message: `chore: [stream-X] <description>`

---

## File Ownership Map (Conflict Prevention)

| File | Owner Stream | Other Streams: Hands Off |
|------|--------------|--------------------------|
| `docfx/docs/mouse.md` | A | B, C, D, E |
| `Tests/**/CheckBoxTests.cs` | B | A, C, D, E |
| `Tests/**/OptionSelectorTests.cs` | B | A, C, D, E |
| `Tests/**/ComboBoxTests.cs` | B | A, C, D, E |
| `Tests/**/ColorPickerTests.cs` | B | A, C, D, E |
| `Examples/UICatalog/Scenarios/MouseTester.cs` | C | A, B, D, E |
| `docfx/docs/migratingfromv1.md` | D | A, B, C, E |
| `AGENTS.md` | E | A, B, C, D |
| `CLAUDE.md` | E | A, B, C, D |

---

## Sign-off

**Plan Author:** Claude Opus 4.6
**Date:** 2026-02-06
**Status:** Ready for parallel execution
