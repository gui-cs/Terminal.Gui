# Command Bubbling Refactor - Implementation Plan

**Target Branch:** `copilot/fix-command-propagation-issue-clean`
**Base Branch:** `v2_develop`
**Parallel Work:** Yes - Tasks organized by feature area

---

## Overview

Terminal.Gui's command system needs refactoring to support hierarchical command bubbling through view hierarchies. This plan breaks the work into parallel task streams:

2. **View Framework Tasks** (Dependency: Foundation) - Command propagation
3. **View-Specific Tasks** (Dependency: View Framework) - Individual view changes
4. **Testing Tasks** (Dependency: Code completion) - Test coverage
5. **Documentation Tasks** (Dependency: All above) - Final documentation

---

## TASK STREAM 2: View Command Framework (Depends on: Stream 1)

### Task 2.1: View.Command.cs - Command Bubbling System
**Status:** Not Started

**Scope:**
- Add `CommandsToBubbleUp` property - list of commands to bubble to SuperView
- Add `DefaultAcceptView` property - first IsDefault button
- Add `TryBubbleToSuperView()` helper method
- Refactor event raising to support bubbling
- Add new non-cancellable events (Accepted, Activated)

**Files:**
- `Terminal.Gui/ViewBase/View.Command.cs`

**Key Changes:**
1. New Properties:
   - `CommandsToBubbleUp` - `List<Command>`
   - `DefaultAcceptView` - auto-discover first IsDefault button

2. Event Methods:
   - `RaiseAccepting()` - uses `TryBubbleToSuperView()`
   - `RaiseActivating()` - uses `TryBubbleToSuperView()`
   - `RaiseAccepted()` - NEW, non-cancellable
   - `RaiseActivated()` - NEW, non-cancellable

3. Handler Refactoring:
   - `DefaultActivateHandler()` → calls `RaiseActivated()`
   - `DefaultAcceptHandler()` → separated from Accepted event
   - `DefaultHotKeyHandler()` → simplified
   - `DefaultCommandNotBoundHandler()` → added

4. All `InvokeCommand` methods use `WeakReference<View>` for Source

**Acceptance Criteria:**
- [ ] `CommandsToBubbleUp` property works
- [ ] `DefaultAcceptView` auto-discovery works
- [ ] `TryBubbleToSuperView()` correctly bubbles commands
- [ ] Event order: Accepting → Accepted, Activating → Activated
- [ ] All handlers use weak references
- [ ] Bubbling respects `CommandsToBubbleUp` list
- [ ] Bubbling handles SuperView disposal gracefully

**Test Files:**
- `Tests/UnitTestsParallelizable/ViewBase/ViewCommandTests.cs` (Existing - update)
- `Tests/UnitTestsParallelizable/ViewBase/CommandBubblingTests.cs` (Existing - enable)

---

## TASK STREAM 3: View-Specific Changes (Depends on: Stream 2)

These can work in parallel once Stream 2 is complete.

### Task 3.1: Button - Accept Command Binding
**Status:** Not Started

**Scope:**
- Change Space key from `Command.HotKey` → `Command.Accept`
- Change Enter key from `Command.HotKey` → `Command.Accept`
- Change Mouse clicks to bind to `Command.Accept` (not `Command.HotKey`)
- Button raises `Accepting`/`Accepted` only (not `Activating`/`Activated`)
- Document new behavior in XML comments

**Files:**
- `Terminal.Gui/Views/Button.cs`

**Acceptance Criteria:**
- [ ] Space triggers Accept (not HotKey)
- [ ] Enter triggers Accept (not HotKey)
- [ ] Mouse clicks trigger Accept (not HotKey)
- [ ] Button does NOT raise Activating
- [ ] Button raises Accepting → Accepted
- [ ] IsDefault button behavior preserved
- [ ] HotKey still works for shortcut chars
- [ ] XML documentation updated

**Test Files:**
- `Tests/UnitTestsParallelizable/Views/ButtonTests.cs`

---

### Task 3.2: Shortcut - Command Bubbling
**Status:** Not Started

**Scope:**
- Add `CommandsToBubbleUp = [Command.Activate, Command.Accept]`
- Forward CommandView events to Shortcut (Activating, Accepting)
- Update MouseHighlightStates to `MouseState.In`
- Simplify field initialization

**Files:**
- `Terminal.Gui/Views/Shortcut.cs`

**Acceptance Criteria:**
- [ ] Shortcut bubbles Activate and Accept to SuperView
- [ ] CommandView events forwarded to Shortcut
- [ ] Activating event fires when CommandView activates
- [ ] Accepting event fires when CommandView accepts
- [ ] MouseHighlightStates shows hover state
- [ ] XML documentation updated

**Test Files:**
- `Tests/UnitTestsParallelizable/Views/ShortcutTests.cs`

---

### Task 3.3: Dialog - Button Accept Handling
**Status:** Not Started

**Scope:**
- Override `OnActivating()` for non-Default button presses
- Override `OnAccepting()` for Default button presses
- Set `Result` to button index in both handlers
- RequestStop when Default button pressed

**Files:**
- `Terminal.Gui/Views/Dialog.cs`

**Acceptance Criteria:**
- [ ] Non-Default button: Click → Activate → OnActivating → Result set
- [ ] Default button: Enter → Accept → OnAccepting → Result set → RequestStop
- [ ] Result correctly maps to button index
- [ ] Weak reference handling in command context
- [ ] Edge case: button not in Dialog.Buttons handled
- [ ] Edge case: source is not a Button handled

**Test Files:**
- `Tests/UnitTestsParallelizable/Views/DialogTests.cs`

---

### Task 3.4: Menu System - Command Propagation
**Status:** Not Started

**Scope:**
- Update Menu, MenuBar, MenuItem, PopoverMenu for weak references
- Menu hierarchy bubbles Accept through SuperMenuItem
- MenuBar checks QuitKey in Accepting handler
- Command context Source tracking through menu levels

**Files:**
- `Terminal.Gui/Views/Menu/Menu.cs`
- `Terminal.Gui/Views/Menu/MenuBar.cs`
- `Terminal.Gui/Views/Menu/MenuItem.cs`
- `Terminal.Gui/Views/Menu/PopoverMenu.cs`

**Acceptance Criteria:**
- [ ] Accept bubbles through menu hierarchy
- [ ] Weak references used in command contexts
- [ ] QuitKey handled in MenuBar.OnAccepting
- [ ] Command context Source preserved through levels
- [ ] MenuItem with CommandView forwards events correctly

**Test Files:**
- `Tests/UnitTestsParallelizable/Views/MenuBarItemTests.cs`
- `Tests/UnitTestsParallelizable/Views/MenuItemTests.cs`

---

### Task 3.5: TextView - Accept vs Enter Separation
**Status:** Not Started

**Scope:**
- Change Enter key from triggering `Command.Accept` → insert newline
- Accept command returns `null` when not handled (enable bubbling)
- Separate "submit" action from "new line" action

**Files:**
- `Terminal.Gui/Views/TextInput/TextView/TextView.Commands.cs`

**Acceptance Criteria:**
- [ ] Enter key inserts newline (not Accept)
- [ ] Accept returns null when unhandled
- [ ] Accept can bubble to SuperView
- [ ] Integration: TextView in Dialog - Enter inserts, not closes
- [ ] Test coverage for new behavior

**Test Files:**
- `Tests/UnitTestsParallelizable/Views/TextViewTests.cs`
- `Tests/UnitTestsParallelizable/Views/TextView.InputTests.cs`

---

### Task 3.6: TextField - Accepting Event Update
**Status:** Not Started

**Scope:**
- Update to use `Accepting` event instead of deprecated patterns
- Better command context handling
- Verify weak reference Source works correctly

**Files:**
- `Terminal.Gui/Views/TextInput/TextField/TextField.cs`

**Acceptance Criteria:**
- [ ] Uses Accepting event pattern
- [ ] Command context Source verification works
- [ ] Accept bubbling from TextField works
- [ ] TextField in Dialog - Enter triggers Accept
- [ ] Tests updated

**Test Files:**
- `Tests/UnitTestsParallelizable/Views/TextFieldTests.cs`

---

## TASK STREAM 4: Testing & Coverage (Depends on: Stream 3)

### Task 4.1: Weak Reference Command Tests
**Status:** Not Started

**Scope:**
- Test Source view disposal during propagation
- Test dead weak reference handling
- Test memory leak prevention
- Test weak reference cleanup

**Files:**
- `Tests/UnitTestsParallelizable/ViewBase/WeakReferenceCommandTests.cs` (NEW)

**Test Cases:**
- View disposed during bubbling
- Dead weak reference handling
- Memory leak prevention verification
- TryGetTarget success/failure scenarios

---

### Task 4.2: Command Bubbling Tests
**Status:** Not Started

**Scope:**
- Enable existing skipped tests in CommandBubblingTests.cs
- Add deep hierarchy tests (3+ levels)
- Add DefaultAcceptView discovery tests
- Add Padding view bubbling tests

**Files:**
- `Tests/UnitTestsParallelizable/ViewBase/CommandBubblingTests.cs` (Existing - expand)

**Test Cases:**
- Deep hierarchies (3+ levels)
- Multiple commands in CommandsToBubbleUp
- Bubbling through Padding views
- Circular hierarchy detection
- DefaultAcceptView discovery
- Custom DefaultAcceptView

---

### Task 4.3: Accepted/Activated Events Tests
**Status:** Not Started

**Scope:**
- Test Accepted event (non-cancellable)
- Test Activated event (non-cancellable)
- Test event ordering
- Verify events raised after cancellable events succeed

**Files:**
- `Tests/UnitTestsParallelizable/ViewBase/AcceptedActivatedEventsTests.cs` (NEW)

**Test Cases:**
- Accepted event fires after Accepting succeeds
- Activated event fires after Activating succeeds
- Event order verification
- Non-cancellable behavior verified

---

### Task 4.4: Extension Methods Tests
**Status:** Not Started

**Scope:**
- Test ViewExtensions.ToIdentifyingString() all scenarios
- Test WeakReferenceExtensions.ToIdentifyingString() all scenarios

**Files:**
- `Tests/UnitTestsParallelizable/ViewBase/ViewExtensionsTests.cs` (NEW)
- `Tests/UnitTestsParallelizable/ViewBase/WeakReferenceExtensionsTests.cs` (NEW)

**Test Cases:**
- View with Id, Title, Text, or nothing set
- Null views
- Dead weak references
- Live weak references
- Priority ordering (Id → Title → Text → Type)

---

### Task 4.5: Integration Tests
**Status:** Not Started

**Scope:**
- Full Accept flow: KeyPress → Binding → Command → Bubbling → DefaultButton → RequestStop
- Menu hierarchy Accept bubbling
- Shortcut in MenuItem command forwarding
- Dialog with various button configurations

**Files:**
- `Tests/IntegrationTests/CommandBubblingIntegrationTests.cs` (NEW)

**Test Cases:**
- Full keyboard flow through hierarchies
- Menu navigation and Accept
- Shortcut forwarding in menus
- Dialog Accept handling with various buttons

---

## TASK STREAM 5: Documentation & Cleanup (Depends on: Stream 4)

### Task 5.1: API Documentation Updates
**Status:** Not Started

**Scope:**
- Update XML docs for all modified public APIs
- Add `<summary>`, `<remarks>`, `<example>` tags as needed
- Document weak reference usage patterns
- Document CommandsToBubbleUp usage
- Link to conceptual docs where appropriate

**Files:**
- All modified public APIs (identified in Streams 1-3)

**Acceptance Criteria:**
- [ ] All public APIs have complete XML docs
- [ ] WeakReference usage documented
- [ ] CommandsToBubbleUp documented with examples
- [ ] Event flow documented
- [ ] See cref links used for cross-references

---

### Task 5.2: Conceptual Documentation
**Status:** Not Started

**Scope:**
- Update/create conceptual documentation
- Command bubbling architecture guide
- Event flow diagrams and explanations
- Migration guide for breaking changes

**Files:**
- `docfx/docs/command.md` (Update)
- `docfx/docs/events.md` (NEW or Update)
- `docfx/docs/command-bubbling-guide.md` (NEW)
- `docfx/docs/breaking-changes.md` (NEW if needed)

**Content:**
- How command bubbling works
- View hierarchy and command propagation
- Event ordering and non-cancellable events
- DefaultAcceptView behavior
- Pattern matching examples for ICommandContext.Binding
- Migration examples for code using old patterns

---

### Task 5.3: Example Apps & Demos
**Status:** Not Started

**Scope:**
- Create/update example showing Shortcut usage
- Update UICatalog EventLog for command event logging
- Create dialog example showing command bubbling

**Files:**
- `Examples/ShortcutTest/ShortcutTest.cs` (NEW or Update)
- `Examples/UICatalog/Scenarios/EditorsAndHelpers/EventLog.cs` (Update)

---

### Task 5.4: Final Validation & Cleanup
**Status:** Not Started

**Scope:**
- Verify all tests pass (parallelizable and standard)
- Run POST-GENERATION-VALIDATION.md checks
- Verify coverage not decreased
- Remove temporary debug code
- Final documentation review

**Checklist:**
- [ ] All unit tests pass
- [ ] Code formatting correct (spacing, braces, blank lines)
- [ ] No new warnings introduced
- [ ] Coverage maintained or improved
- [ ] All breaking changes documented
- [ ] API documentation complete
- [ ] Examples working correctly

---

## Dependency Graph

```
Stream 1 (Foundation)
├── Task 1.1: CommandContext Refactoring
├── Task 1.2: WeakReferenceExtensions
└── Task 1.3: ViewExtensions
    ↓
Stream 2 (View Framework)
└── Task 2.1: View.Command.cs
    ↓
Stream 3 (View-Specific) - Can work in parallel
├── Task 3.1: Button
├── Task 3.2: Shortcut
├── Task 3.3: Dialog
├── Task 3.4: Menu System
├── Task 3.5: TextView
└── Task 3.6: TextField
    ↓
Stream 4 (Testing) - Can work in parallel
├── Task 4.1: Weak Reference Tests
├── Task 4.2: Bubbling Tests
├── Task 4.3: Event Tests
├── Task 4.4: Extension Methods Tests
└── Task 4.5: Integration Tests
    ↓
Stream 5 (Documentation)
├── Task 5.1: API Documentation
├── Task 5.2: Conceptual Docs
├── Task 5.3: Examples
└── Task 5.4: Final Validation
```

---

## Acceptance Criteria Summary

**All Streams:**
- No decrease in test coverage
- All breaking changes documented
- XML documentation complete for all public APIs
- Code formatting per CLAUDE.md rules
- No new compiler warnings

**Functionality:**
- Command bubbling works through view hierarchies
- WeakReference prevents memory leaks
- Non-cancellable events fire at correct times
- All view-specific changes work as documented
- Menu and Dialog integration tests pass

**Quality:**
- 70%+ code coverage for new code
- All tests parallelizable where possible
- Performance acceptable for deep hierarchies
- Memory leak prevention verified
