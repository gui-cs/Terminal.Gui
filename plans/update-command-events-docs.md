# Plan: Update command.md, events.md, and View.md Documentation

## Goal

Bring all three doc files into accurate alignment with the current implementation in `View.Command.cs` and related types. Fix incorrect code examples, outdated API signatures, inaccurate diagrams, and missing coverage of new features (`IsBubblingDown`/`IsBubblingUp`, `IAcceptTarget`, `WeakReference<View>` source, etc.).

---

## Issues Found

### command.md

1. **Top-level flowchart inaccurate**: Shows "Complete - returns false" for all three paths, but `DefaultActivateHandler` returns `true`. The `DefaultHotKeyHandler` returns `false` when `RaiseHandlingHotKey` succeeds (intentionally, to allow the key char through for text input). The flowchart doesn't show `DefaultAcceptHandler`'s `BubbleDown` to `DefaultAcceptView` or its `IAcceptTarget`/`IsBubblingUp` logic.

2. **Method name mismatch**: Code calls it `TryBubbleUpToSuperView` but doc calls it `TryBubbleToSuperView` throughout.

3. **`TryBubbleToSuperView` flowchart outdated**: Missing the `IsBubblingDown` early-return check, `IsBubblingUp` context creation, `IAcceptTarget` logic (default vs non-default), and the `self is Padding` case (only `SuperView is Padding` is shown).

4. **`DefaultAcceptView` code outdated**: Shows `v is Button { IsDefault: true }` but actual code uses `v is IAcceptTarget { IsDefault: true }`.

5. **`RaiseAccepting` implementation code outdated**: Doesn't match actual code (e.g., actual code checks `Accepting is { }` before invoking).

6. **`RaiseActivating` implementation code outdated**: Same issue - minor differences from actual.

7. **`DefaultHotKeyHandler` code outdated**: Actual code passes `ctx?.Binding` to `InvokeCommand (Command.Activate, ctx?.Binding)` and returns `false` (not just calls `InvokeCommand(Command.Activate)`). The `return false` on `RaiseHandlingHotKey` success is intentional and needs explanation.

8. **`DefaultAcceptHandler` not shown fully**: The actual handler is complex - it calls `RaiseAccepting`, then checks `DefaultAcceptView`, calls `BubbleDown` to it, calls `RaiseAccepted`, and returns based on `redirected || ctx?.IsBubblingUp || this is IAcceptTarget`. The doc only describes the first two steps.

9. **Missing `IAcceptTarget` coverage**: The interface is central to Accept propagation but only `DefaultAcceptView` is described. No explanation of how `IAcceptTarget` views (Button) vs non-`IAcceptTarget` views differ in Accept flow.

10. **Code style violations in examples**: Some code blocks use `Method()` instead of `Method ()`.

### events.md

1. **`ICommandContext` interface shown is outdated**: Missing `IsBubblingDown` and `IsBubblingUp` properties. Shows `Source` as `View?` but actual type is `WeakReference<View>?`.

2. **`OnAccepting` method signature wrong**: Shows `public override bool OnAccepting(object? sender, CommandEventArgs e)` but actual is `protected virtual bool OnAccepting (CommandEventArgs args)` - no sender parameter, different visibility.

3. **Source tracking table inaccurate**: Says `ICommandContext.Source` is a View, but it's a `WeakReference<View>`.

4. **Recipe 2 uses `CancelEventArgs.Cancel`**: The pitfalls section warns against this, but Recipe 2 Option A uses it. Should use `HandledEventArgs.Handled` pattern consistently (or clarify that `CancelEventArgs` is different from CWP command events).

5. **Code style violations**: Missing spaces before parentheses throughout.

6. `View Command Behaviors` is not completely accurate. All View implementations must be studied and the table updated. Note that Shortcut, Bar, MenuItem, and the other views dependent on Shortcut should be ignored for now as Shortcut is currently broken and being redesigned.

### View.md

1. **Code examples don't follow project spacing style**: Missing spaces before `()` and `[]` in many examples (e.g., `AddCommand(Command.Accept, HandleAccept)` should be `AddCommand (Command.Accept, HandleAccept)`).

2. **Commands subsection is fine** but could benefit from mentioning `IAcceptTarget`.

---

## Changes

### 1. command.md - Major Revision

#### 1a. Fix the top-level flowchart
- Show accurate return values for each path
- Add `DefaultAcceptView`/`BubbleDown` step to Accept path
- Show that `DefaultHotKeyHandler` returns `false` when `RaiseHandlingHotKey` is `true` (allowing key-as-input)
- Show that `DefaultActivateHandler` returns `true` at the end

#### 1b. Fix method name: `TryBubbleToSuperView` → `TryBubbleUpToSuperView`
- Replace all occurrences throughout the document

#### 1c. Update `TryBubbleUpToSuperView` flowchart
- Add `IsBubblingDown` check at the top (returns false immediately)
- Add `IAcceptTarget` logic for Accept commands: default vs non-default behavior
- Add `self is Padding` case alongside `SuperView is Padding`
- Show `IsBubblingUp` flag being set on context when bubbling

#### 1d. Fix `DefaultAcceptView` code
- Change `Button { IsDefault: true }` to `IAcceptTarget { IsDefault: true }`

#### 1e. Remove or significantly trim `RaiseAccepting`/`RaiseActivating` implementation code
- These are implementation details that drift. Replace with concise behavioral descriptions.
- Keep the conceptual flow (OnX → Event → TryBubble) without showing full method bodies.

#### 1f. Update `DefaultHotKeyHandler` section
- Show the actual return behavior: returns `false` when `RaiseHandlingHotKey` is `true`
- Explain WHY: so the key character can still be processed as text input (e.g., TextField whose HotKey matches a typed character)
- Show `InvokeCommand (Command.Activate, ctx?.Binding)` - binding is preserved

#### 1g. Add `DefaultAcceptHandler` detailed description
- Document the full flow: RaiseAccepting → DefaultAcceptView BubbleDown → RaiseAccepted → return logic
- Explain `IAcceptTarget` role: non-default `IAcceptTarget` bubbles up to SuperView; default `IAcceptTarget` flows normally
- Explain the return value: `redirected || ctx?.IsBubblingUp || this is IAcceptTarget`

#### 1h. Add `IAcceptTarget` section
- Explain the interface and its purpose
- Explain how Button implements it
- Explain the difference in Accept flow for `IAcceptTarget` vs plain views

#### 1i. Fix code style in all examples
- Add spaces before `()` and `[]` per project conventions

#### 1j. Verify all mermaid diagrams render on GitHub
- Use `flowchart TD` or `flowchart LR` (supported)
- Avoid unsupported features (subgraphs with complex styling)
- Test that node labels don't contain special characters that break rendering

### 2. events.md - Targeted Fixes

#### 2a. Fix `ICommandContext` interface display
- Update to show `WeakReference<View>?` for `Source`
- Add `IsBubblingDown` and `IsBubblingUp` properties

#### 2b. Fix `OnAccepting` method signature
- Change to `protected virtual bool OnAccepting (CommandEventArgs args)` (no sender param)
- Fix both occurrences (in Source Tracking section and Binding Types section)

#### 2c. Fix Source Tracking table
- Change `ICommandContext.Source` description to note it's a `WeakReference<View>` with `TryGetTarget` access pattern

#### 2d. Fix Recipe 2 Option A
- Either change to use `HandledEventArgs.Handled` pattern, or add a note explaining that `CancelEventArgs` is a different pattern from command events and is appropriate for non-command workflows

#### 2e. Fix code style in all examples
- Add spaces before `()` and `[]`

### 3. View.md - Code Style Fixes

#### 3a. Fix code style in all code examples
- Add spaces before `()` and `[]` throughout
- Fix target-typed new usage where missing
- This affects: Creating a Custom View, Adding SubViews, Using Adornments, Implementing Scrolling, Modal View examples

#### 3b. Add `IAcceptTarget` mention in Commands subsection
- Brief mention under the Commands subsection linking to command.md

---

## Files Modified

1. `docfx/docs/command.md` - Major revision
2. `docfx/docs/events.md` - Targeted fixes
3. `docfx/docs/View.md` - Code style fixes + minor additions

## Verification

- All mermaid diagrams use GitHub-compatible syntax
- All code examples follow project code style (space before `()` and `[]`, Allman braces, etc.)
- All API references match actual code in `View.Command.cs`, `ICommandContext.cs`, `CommandContext.cs`, `CommandEventArgs.cs`, `IAcceptTarget.cs`
- No implementation code shown unless essential for understanding the concept
- Cross-references between docs are valid
