# Plan: Change View Default Activation to Released

**Status:** Draft
**Created:** 2026-02-03
**Author:** Claude Opus 4.5
**Related Issue:** #4674

---

## Executive Summary

Change View's default mouse activation behavior from **LeftButtonPressed** to **LeftButtonReleased** to align with industry standards across all major UI frameworks (Windows WPF/WinForms, macOS Cocoa, Web HTML, GTK4, Qt).

**Key Benefits:**
- Aligns with universal GUI conventions (40+ years of established UX patterns)
- Enables cancellation of accidental clicks (press, drag away, release)
- Matches user expectations across all platforms
- Provides better visual feedback before commitment

---

## Research Summary

All major UI frameworks activate on **release**:

| Framework | Activation Event | Cancellation Support |
|-----------|------------------|----------------------|
| Web (HTML) | click (mousedown + mouseup) | ✅ Yes |
| Windows (WPF/WinForms) | MouseUp | ✅ Yes |
| macOS (Cocoa) | Mouse release | ✅ Yes |
| GTK4 | clicked (press + release) | ✅ Yes |
| Qt | clicked() signal | ✅ Yes |

**Industry Pattern:** "Activate on release" allows users to:
1. Press button → see visual feedback
2. Realize mistake → drag away
3. Release outside → cancel action without triggering

**Sources:**
- [Element: mouseup event - MDN](https://developer.mozilla.org/en-US/docs/Web/API/Element/mouseup_event)
- [GTK4 Button Class](https://docs.gtk.org/gtk4/class.Button.html)
- [QAbstractButton - Qt](https://doc.qt.io/qt-6/qabstractbutton.html)

---

## Current State Analysis

### Location
`Terminal.Gui/ViewBase/Mouse/View.Mouse.cs` lines 13-23

### Current Default Bindings
```csharp
internal void SetupMouse ()
{
    MouseBindings.Clear ();

    // Current: Activate on PRESSED
    MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
    MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.Ctrl, Command.Context);

    // Released bindings added/removed dynamically based on MouseHoldRepeat
}
```

### Why This Matters
- **No cancellation:** Users cannot abort accidental presses
- **Inconsistent UX:** Differs from every other GUI framework users know
- **Unexpected behavior:** Trained muscle memory from other apps doesn't work

---

## Implementation Plan

### Phase 1: Change Default Binding

**File:** `Terminal.Gui/ViewBase/Mouse/View.Mouse.cs`

**Change:** Lines 13-23 in `SetupMouse()`

```csharp
// BEFORE (current)
MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);
MouseBindings.Add (MouseFlags.LeftButtonPressed | MouseFlags.Ctrl, Command.Context);

// AFTER (proposed)
MouseBindings.Add (MouseFlags.LeftButtonReleased, Command.Activate);
MouseBindings.Add (MouseFlags.LeftButtonReleased | MouseFlags.Ctrl, Command.Context);
```

**Rationale:**
- Minimal change (2 lines)
- Leverages existing Released event infrastructure (already fixed in #4674)
- Auto-grab behavior already handles press/release lifecycle correctly

---

### Phase 2: Update Tests

#### 2.1 Update Existing Tests

**Files to audit:**
- `Tests/UnitTests/ViewBase/Mouse/*.cs`
- `Tests/UnitTestsParallelizable/ViewBase/Mouse/*.cs`

**Actions:**
1. Identify tests that depend on `LeftButtonPressed → Command.Activate`
2. Update to expect `LeftButtonReleased → Command.Activate`
3. Ensure tests follow press → release sequence (not just single event)

#### 2.2 Add New Tests

**File:** `Tests/UnitTestsParallelizable/ViewBase/Mouse/DefaultActivationTests.cs` (new)

**Test coverage:**
- ✅ Default activation fires on Released, not Pressed
- ✅ Cancellation: Press inside, drag outside, release → no activation
- ✅ Normal flow: Press inside, release inside → activation fires
- ✅ Multiple views: Press on view1, release on view2 → only view1 processes
- ✅ Modifier keys: Ctrl+Released invokes Command.Context
- ✅ AutoGrab lifecycle: Grab on press, ungrab on release
- ✅ Backward compatibility: Custom Pressed bindings still work

**Example test:**
```csharp
// Claude - Opus 4.5
[Fact]
public void DefaultActivation_FiresOnRelease_NotOnPress ()
{
    // Arrange
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI);
    IRunnable runnable = new Runnable ();

    View view = new () { Width = 10, Height = 10 };
    (runnable as View)?.Add (view);
    app.Begin (runnable);

    var activatedOnPress = false;
    var activatedOnRelease = false;

    view.Activating += (_, _) =>
    {
        // Check which event triggered this
        if (app.Mouse.LastMouseEvent?.Flags.HasFlag (MouseFlags.LeftButtonPressed) ?? false)
            activatedOnPress = true;
        if (app.Mouse.LastMouseEvent?.Flags.HasFlag (MouseFlags.LeftButtonReleased) ?? false)
            activatedOnRelease = true;
    };

    // Act
    app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (0, 0) });
    app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (0, 0) });

    // Assert
    Assert.False (activatedOnPress, "Should NOT activate on press");
    Assert.True (activatedOnRelease, "Should activate on release");

    (runnable as View)?.Dispose ();
}

[Fact]
public void DefaultActivation_Cancellation_DragAwayBeforeRelease ()
{
    // Arrange
    VirtualTimeProvider time = new ();
    using IApplication app = Application.Create (time);
    app.Init (DriverRegistry.Names.ANSI);
    IRunnable runnable = new Runnable ();

    View view = new () { X = 0, Y = 0, Width = 10, Height = 10, MouseHighlightStates = MouseState.Pressed };
    (runnable as View)?.Add (view);
    app.Begin (runnable);

    var activated = false;
    view.Activating += (_, _) => activated = true;

    // Act - Press inside, move outside, release outside
    app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonPressed, ScreenPosition = new (5, 5) });
    Assert.True (app.Mouse.IsGrabbed (view), "View should grab mouse on press");

    app.InjectMouse (new Mouse { Flags = MouseFlags.LeftButtonReleased, ScreenPosition = new (50, 50) }); // Outside

    // Assert
    Assert.False (activated, "Should NOT activate when released outside");
    Assert.False (app.Mouse.IsGrabbed (view), "Mouse should be ungrabbed after release");

    (runnable as View)?.Dispose ();
}
```

---

### Phase 3: Update Examples

**File:** `Examples/UICatalog/Scenarios/MouseTester.cs`

**Actions:**
1. Update comments to reflect new default behavior
2. Add visual demonstration of cancellation behavior
3. Show difference between Pressed, Released, and Clicked bindings

**Optional enhancement:**
Add a demo section showing:
- Default behavior: "Click (release) to activate"
- Custom Pressed binding: "Press to activate (instant feedback)"
- Comparison side-by-side

---

### Phase 4: Documentation Updates

#### 4.1 API Documentation

**File:** `Terminal.Gui/ViewBase/Mouse/View.Mouse.cs`

Update XML comments in `SetupMouse()`:

```csharp
/// <summary>
/// Initializes the default mouse bindings for this View.
/// </summary>
/// <remarks>
/// Default bindings:
/// <list type="bullet">
///   <item><see cref="MouseFlags.LeftButtonReleased"/> → <see cref="Command.Activate"/> - Standard activation (aligns with industry conventions)</item>
///   <item><see cref="MouseFlags.LeftButtonReleased"/> + Ctrl → <see cref="Command.Context"/> - Context menu</item>
/// </list>
/// <para>
/// Views activate on button <em>release</em> (not press) to allow cancellation: press the button,
/// move cursor away, then release to abort the action without triggering it.
/// This matches the behavior of all major GUI frameworks (Windows, macOS, Web, GTK, Qt).
/// </para>
/// <para>
/// To customize activation behavior, use <see cref="MouseBindings"/> to add bindings for
/// <see cref="MouseFlags.LeftButtonPressed"/> (immediate activation) or
/// <see cref="MouseFlags.LeftButtonClicked"/> (full click cycle required).
/// </para>
/// </remarks>
```

#### 4.2 Update command.md

**File:** `docfx/docs/command.md`

**Change 1: Update Line 47 (Command System Summary table)**

```markdown
<!-- BEFORE -->
| **Mouse → Command Pipeline** | See [Mouse Pipeline](mouse.md#complete-mouse-event-pipeline)<br>**Current:** `LeftButtonClicked` → `Activate`<br>**Recommended:** `LeftButtonClicked` → `Activate` (first click)<br>`LeftButtonDoubleClicked` → `Accept` (framework-provided) |

<!-- AFTER -->
| **Mouse → Command Pipeline** | See [Mouse Pipeline](mouse.md#complete-mouse-event-pipeline)<br>**Default:** `LeftButtonReleased` → `Activate` (aligns with industry standards - allows cancellation)<br>**Alternative:** `LeftButtonPressed` → `Activate` (immediate feedback, no cancellation)<br>`LeftButtonDoubleClicked` → `Accept` (framework-provided) |
```

**Change 2: Update View Command Behaviors Table (Lines 56-86)**

Update the **View** (base) row in the table:

```markdown
<!-- BEFORE -->
| **View** (base) | `Command.Activate` (default) | `Command.Accept` (default) | `Command.HotKey` (default) | Base OnMouseEvent (updates MouseState) | Base OnMouseEvent (updates MouseState) | Not bound by default | Not bound by default |

<!-- AFTER -->
| **View** (base) | `Command.Activate` (default) | `Command.Accept` (default) | `Command.HotKey` (default) | Base OnMouseEvent (updates MouseState) | `Command.Activate` (default) | Not bound by default | Not bound by default |
```

Explanation: The "Released" column (5th column) should show `Command.Activate` (default) instead of "Base OnMouseEvent (updates MouseState)"

**Change 3: Add Note About Cancellation Behavior**

Add to the "Notes on Command Behaviors" section (after line 130):

```markdown
11. **Default Activation on Release**: The base `View` class binds `LeftButtonReleased` to `Command.Activate`, following industry-standard GUI conventions. This allows users to:
    - Press the button → See visual feedback (MouseState.Pressed)
    - Drag away → Realize mistake
    - Release outside → Cancel action without triggering

    This matches behavior in Windows (WPF/WinForms), macOS (Cocoa), Web (HTML click), GTK4, and Qt. To activate on press instead (immediate feedback, no cancellation), replace the binding:
    ```csharp
    view.MouseBindings.ReplaceCommands (MouseFlags.LeftButtonPressed, Command.Activate);
    view.MouseBindings.Remove (MouseFlags.LeftButtonReleased);
    ```
```

#### 4.3 Conceptual Documentation (Optional)

**File:** `docfx/docs/mouse.md` (create if doesn't exist)

Add section:

```markdown
## Default Mouse Activation Behavior

Terminal.Gui follows industry-standard GUI conventions for mouse activation:

### Activation on Release (Default)

By default, views activate when the mouse button is **released** (not pressed). This allows users to:

1. **Press** the button → View provides visual feedback (highlight, pressed state)
2. **Drag away** (optional) → User realizes this wasn't the intended action
3. **Release outside** → Action is cancelled, nothing happens

This "release to commit" pattern matches all major GUI frameworks:
- Windows (WPF, WinForms)
- macOS (Cocoa/AppKit)
- Web browsers (HTML click events)
- GTK4 and Qt

### Customizing Activation

To change when a view activates, modify its `MouseBindings`:

```csharp
// Activate immediately on press (instant feedback, no cancellation)
view.MouseBindings.Clear ();
view.MouseBindings.Add (MouseFlags.LeftButtonPressed, Command.Activate);

// Activate on full click cycle (press AND release on same view)
view.MouseBindings.Clear ();
view.MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Activate);

// Activate on release (default - explicit example)
view.MouseBindings.Clear ();
view.MouseBindings.Add (MouseFlags.LeftButtonReleased, Command.Activate);
```

### Why Release Instead of Clicked?

Terminal.Gui uses `LeftButtonReleased` (not `LeftButtonClicked`) as the default because:

- **Matches Windows conventions:** Win32 WM_LBUTTONUP, not WM_LBUTTONDBLCLK
- **Simpler mental model:** One event (release) instead of lifecycle (press → release → clicked)
- **Flexible:** Released events fire regardless of click count (single/double/triple)
- **Performance:** No click detection delay

The `Clicked` event remains available for use cases requiring full click cycle validation.
```

#### 4.3 Migration Guide

**File:** `docfx/docs/migration-v2.md` (or create `docfx/docs/breaking-changes-v2-alpha.md`)

Add section:

```markdown
## Mouse Activation Changed from Pressed to Released

**Breaking Change:** Default mouse activation changed from `LeftButtonPressed` to `LeftButtonReleased`.

### What Changed

| Version | Default Binding | Behavior |
|---------|----------------|----------|
| v2 Alpha (before) | `LeftButtonPressed → Command.Activate` | Activates immediately on press |
| v2 Alpha (after) | `LeftButtonReleased → Command.Activate` | Activates on release (cancellable) |

### Migration

If your application depends on immediate activation (press, not release):

```csharp
// Restore old behavior (activate on press)
view.MouseBindings.ReplaceCommands (MouseFlags.LeftButtonPressed, Command.Activate);
view.MouseBindings.Remove (MouseFlags.LeftButtonReleased);
```

### Why This Change?

To align with industry-standard GUI conventions across all major frameworks (Windows, macOS, Web, GTK, Qt),
which activate on release to allow cancellation of accidental clicks.
```

---

## Testing Strategy

### Automated Tests

1. **Unit tests** (parallelizable):
   - Default binding is Released, not Pressed
   - Cancellation behavior (press inside, release outside)
   - AutoGrab lifecycle (grab on press, ungrab on release)
   - Custom Pressed bindings still work
   - Modifier keys with Released (Ctrl+Released)

2. **Integration tests**:
   - Button click behavior
   - Dialog button activation
   - Menu item selection
   - All core widgets maintain expected behavior

3. **Regression tests**:
   - Run full test suite (UnitTests + UnitTestsParallelizable)
   - Ensure no existing tests break (or fix them appropriately)

### Manual Testing

**Test Plan:**

1. **UICatalog MouseTester scenario:**
   - Verify default activation on release
   - Test cancellation (press, drag out, release)
   - Test different MouseHighlightStates

2. **Core widgets:**
   - Button click behavior
   - CheckBox toggle
   - RadioGroup selection
   - ListView item selection
   - Dialog button activation

3. **Edge cases:**
   - Multiple views overlapping
   - Modal dialogs
   - Disabled views
   - Views with custom bindings

---

## Migration Considerations

### Backward Compatibility

**Breaking Change:** This IS a breaking change in default behavior.

**Mitigation:**
- Document clearly in release notes
- Provide migration code snippet (restore old behavior)
- Version: v2 is still Alpha, breaking changes expected

### User Impact Assessment

**Low Risk:**
- v2 is still Alpha (not stable release)
- New behavior matches user expectations from other apps
- Change aligns with industry standards
- Easy to revert for specific views if needed

**Potential Issues:**
1. **Automated tests in user code:** May expect Pressed behavior
   - **Solution:** Update tests or restore old binding
2. **Muscle memory during development:** Developers used to Pressed
   - **Solution:** Quick adaptation, new behavior is more intuitive
3. **Custom controls relying on default:** Rare, but possible
   - **Solution:** Explicit binding in custom control constructor

---

## Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Breaks existing v2 Alpha apps | Medium | Medium | Document migration path, provide code snippet |
| Test suite failures | High | Low | Update tests to match new behavior |
| User confusion during transition | Low | Low | Clear documentation, matches industry standards |
| Performance regression | Very Low | Low | No new logic, just changed binding flag |
| Introduces new bugs | Low | Medium | Comprehensive testing, leverage existing Released infrastructure |

---

## Implementation Checklist

### Code Changes
- [ ] Update `SetupMouse()` in `View.Mouse.cs` (2 lines changed)
- [ ] Add `DefaultActivationTests.cs` with comprehensive coverage
- [ ] Update existing tests that depend on Pressed activation
- [ ] Run full test suite (UnitTests + UnitTestsParallelizable)
- [ ] Update `MouseTester.cs` example with new behavior demo

### Documentation
- [ ] Update XML comments in `View.Mouse.cs`
- [ ] Update `command.md` with new default behavior and table changes
- [ ] Create/update `docfx/docs/mouse.md` with activation section (optional)
- [ ] Add migration guide to `docfx/docs/migration-v2.md`
- [ ] Update release notes with breaking change notice

### AI Agent Guidance
- [ ] Update `AGENTS.md` and/or `CLAUDE.md` to document that plans should be created in `./plans` directory
  - Add to "For Library Contributors" section in AGENTS.md
  - Add to "Contributor Guide" section in CLAUDE.md
  - Guidance: "When creating implementation plans, place them in `./plans/` directory (not `~/.claude/plans/`)"

### Testing
- [ ] Unit tests pass (all)
- [ ] Integration tests pass (all)
- [ ] Manual testing of core widgets (Button, CheckBox, etc.)
- [ ] Manual testing of UICatalog MouseTester scenario
- [ ] Verify cancellation behavior works as expected

### Review
- [ ] Code review by maintainers
- [ ] Documentation review for clarity
- [ ] Test coverage review (should maintain or increase coverage)
- [ ] Migration path validated with sample code

---

## Timeline Estimate

**No time estimates provided per project policy.**

**Scope:**
- **Minimal:** 2-line code change + focused test updates
- **Full:** Code + comprehensive tests + documentation + examples

**Dependencies:**
- None (Released event infrastructure already complete via #4674)

---

## Open Questions

1. **Should we add a global setting** to restore v1/old behavior?
   - **Recommendation:** No, adds complexity. Per-view binding is sufficient.

2. **Should Button/CheckBox/etc. override with custom behavior?**
   - **Recommendation:** No, all should use View default for consistency.

3. **Should we emit a warning when old Pressed binding is used?**
   - **Recommendation:** No, Pressed bindings are valid use cases (e.g., drag handles).

4. **Should MouseHoldRepeat default change as well?**
   - **Recommendation:** No, MouseHoldRepeat is opt-in, leave as-is.

---

## References

- **Issue:** #4674 - MouseBindings for Released events not invoking commands
- **Commit:** d1b7a8885 - Fix Released binding invocation
- **Related Files:**
  - `Terminal.Gui/ViewBase/Mouse/View.Mouse.cs`
  - `Terminal.Gui/Input/Mouse/MouseBindings.cs`
  - `Tests/UnitTestsParallelizable/ViewBase/Mouse/MouseReleasedBindingTests.cs`
  - `Examples/UICatalog/Scenarios/MouseTester.cs`

- **Industry Research:**
  - [MDN: Element mouseup event](https://developer.mozilla.org/en-US/docs/Web/API/Element/mouseup_event)
  - [GTK4 Button Documentation](https://docs.gtk.org/gtk4/class.Button.html)
  - [Qt QAbstractButton](https://doc.qt.io/qt-6/qabstractbutton.html)
  - [QuirksMode: Click Events](https://www.quirksmode.org/dom/events/click.html)

---

## Sign-off

**Plan Author:** Claude Opus 4.5
**Date:** 2026-02-03
**Status:** Ready for review and implementation

