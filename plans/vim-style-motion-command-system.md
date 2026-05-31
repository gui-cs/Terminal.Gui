# Vim-Style Motion Add-On Spec

## Problem Statement

Terminal.Gui has a mature command and key binding model for single key gestures: a `Key` maps to one or more `Command` values, and the focused `View` invokes supported commands through the existing routing and Cancellable Work Pattern.

Some applications need modal, Vim-style key sequences instead of single key gestures. These sequences commonly start with a leader key, continue through one or more symbolic keys, and may include numeric counts before an operator or motion.

Examples:

```text
; m 4 k  => move cursor 4 lines up
; d 2 d  => delete current line and 1 line below
```

The goal is to provide a reusable C# command-sequence add-on that lets application and view authors declare these sequences without overriding raw key handling in every view. This feature should not be added to `Terminal.Gui` core. It should ship as a separate package, following the same broad model as `Terminal.Gui.Editor`: a focused add-on that references Terminal.Gui, owns its own APIs, owns its own tests and docs, and can evolve without expanding the core toolkit surface.

## Goals

- Let developers define one or more leader keys, such as `;`, `Space`, or `Ctrl+X`.
- Let developers bind chains of keys to command handlers.
- Let chains accept printable letters, punctuation keys, and numeric input.
- Let handlers receive parsed sequence context, including leader key, matched keys, numeric count, operator key, motion key, target view, and source binding.
- Integrate with existing `View.KeyDown`, `View.KeyDownNotHandled`, `Application.Keyboard.KeyBindings`, `Command`, and `CommandContext` without adding new members to core Terminal.Gui types.
- Preserve existing single-key behavior unless a leader sequence is actively being captured.
- Support both application-scoped sequences and focused-view sequences.
- Make partial input, cancellation, timeouts, and invalid sequences predictable.
- Provide testable APIs that do not depend on static application state.

## Non-Goals

- Do not implement a full Vim editor mode system in this feature.
- Do not add Vim-specific commands to the core `Command` enum for every Vim command.
- Do not require existing views to understand Vim semantics.
- Do not replace `KeyBindings`; this feature composes with the current model.
- Do not require terminals to support key-up events or kitty-only metadata.
- Do not add `View.KeySequences`, `IKeyboard.KeySequences`, or any other new public API to the Terminal.Gui core assembly in the initial implementation.

## Package Model

Ship this as a separate add-on package.

Recommended package name:

```text
Terminal.Gui.KeySequences
```

Alternative package names:

```text
Terminal.Gui.VimMotions
Terminal.Gui.CommandSequences
```

`Terminal.Gui.KeySequences` is the preferred name because the core capability is generic sequence matching. Vim-style motions are the primary use case, but the same API can support command palettes, leader shortcuts, and application-specific multi-key chords.

Recommended namespace:

```csharp
namespace Terminal.Gui.KeySequences;
```

Repository options:

- Create a sibling repository, similar to `gui-cs/Editor`.
- Or create a separate project in this repository only if maintainers want a monorepo package. It should still build as its own NuGet package and avoid changes to the core `Terminal.Gui` assembly.

The add-on should reference `Terminal.Gui` and use public extension points only.

## Terminology

- **Leader key**: The first key that enters sequence capture, such as `;`.
- **Sequence**: The ordered keys entered after the leader key.
- **Token**: A matched element in a sequence pattern. Tokens can be literal keys, a count, a printable character, or a wildcard.
- **Count**: A decimal number parsed from one or more digit keys. Counts default to `1`.
- **Operator**: A key or token that defines the action category, such as `d` for delete.
- **Motion**: A key or token that defines a target range or movement, such as `k` for up.
- **Binding**: A declarative mapping from a sequence pattern to a handler.
- **Sequence capture**: The transient state after a leader key and before match, cancel, timeout, or invalid input.

## Proposed Add-On API

Add the primary API in the add-on package.

```csharp
namespace Terminal.Gui.KeySequences;

public sealed class KeySequenceBindings
{
    public KeySequenceBindings ();

    public TimeSpan Timeout { get; set; }
    public Key CancelKey { get; set; }

    public void AddLeader (Key leaderKey);
    public void RemoveLeader (Key leaderKey);
    public bool IsLeader (Key key);

    public void Add (KeySequencePattern pattern, KeySequenceHandler handler);
    public void Add (string pattern, KeySequenceHandler handler);
    public bool Remove (KeySequencePattern pattern);
    public void Clear ();

    public KeySequenceResult ProcessKey (View target, Key key, CommandContext? commandContext = null);
    public void Reset ();
}

public delegate bool KeySequenceHandler (KeySequenceContext context);

public sealed record KeySequenceContext
{
    public required View Target { get; init; }
    public required Key LeaderKey { get; init; }
    public required IReadOnlyList<Key> Keys { get; init; }
    public required KeySequencePattern Pattern { get; init; }
    public int Count { get; init; } = 1;
    public Key? OperatorKey { get; init; }
    public Key? MotionKey { get; init; }
    public IReadOnlyDictionary<string, object?> Values { get; init; } = new Dictionary<string, object?> ();
    public CommandContext? CommandContext { get; init; }
}

public enum KeySequenceResult
{
    NotLeader,
    Started,
    Pending,
    Matched,
    Canceled,
    Rejected,
    TimedOut
}
```

Provide extension methods so app and view authors can attach sequence handling without changing Terminal.Gui core types.

```csharp
public static class KeySequenceViewExtensions
{
    public static KeySequenceBindings GetKeySequences (this View view);
    public static IDisposable UseKeySequences (this View view, KeySequenceBindings bindings);
    public static IDisposable UseKeySequences (this View view, Action<KeySequenceBindings> configure);
}

public static class KeySequenceApplicationExtensions
{
    public static IDisposable UseKeySequences (this IApplication application, KeySequenceBindings bindings);
    public static IDisposable UseKeySequences (this IApplication application, Action<KeySequenceBindings> configure);
}
```

The extension methods subscribe to existing public keyboard events and detach on dispose. This preserves Terminal.Gui core API stability while giving add-on users an ergonomic setup path.

Focused-view sequence handling:

```csharp
IDisposable registration = editor.UseKeySequences (bindings =>
{
    bindings.AddLeader (';');
    bindings.Add ("; m <count> k", MoveCursorUp);
});
```

Application-scoped sequence handling:

```csharp
IDisposable registration = app.UseKeySequences (bindings =>
{
    bindings.AddLeader (Key.Space);
    bindings.Add ("<Space> f f", ShowFileFinder);
});
```

## Pattern Syntax

Provide two declaration forms: a strongly typed builder and a compact string parser.

```csharp
bindings.Add ("; m <count> k", MoveUp);
bindings.Add ("; d <count> d", DeleteLines);
```

String pattern rules:

- Tokens are whitespace-separated.
- A literal printable key is written as the character itself: `m`, `k`, `;`, `.`.
- Named keys use angle brackets: `<Esc>`, `<Enter>`, `<Space>`, `<Backspace>`.
- `<count>` matches one or more digits and stores the parsed count.
- `<char>` matches any printable non-control character.
- `<key>` matches any valid key.
- Modifiers use existing `Key` names where possible: `<Ctrl+X>`, `<Alt+K>`, `<Shift+K>`.
- The leader key may be included in the pattern string for readability. Internally it is stored as a leader, not as part of `Keys`.

Builder API:

```csharp
KeySequencePattern moveUpPattern = KeySequencePattern
    .Leader (';')
    .Then (Key.M)
    .Count ("count")
    .Then (Key.K);
```

## Numeric Counts

Counts are parsed as positive base-10 integers.

Rules:

- Missing count means `1`.
- Leading zero is ignored for value parsing, so `04` means `4`.
- A count of `0` is allowed only if the binding opts in with `AllowZeroCount`.
- Count parsing stops when the next non-digit token is entered.
- A pattern may include at most one `<count>` token in the initial implementation.

The handler receives the parsed count in `KeySequenceContext.Count`.

For `; d 2 d`, the delete handler receives `Count = 2`. The view-specific handler decides whether that means "delete two lines" or "delete current line and one line below." The add-on should not encode editor-specific range semantics.

## Matching Model

`KeySequenceBindings` keeps a small state machine:

```text
Idle
  leader key -> Capturing

Capturing
  cancel key -> Canceled -> Idle
  timeout -> TimedOut -> Idle
  key extends at least one possible pattern -> Pending
  key completes exactly one pattern -> Matched -> Idle
  key completes one pattern and is prefix of longer patterns -> Pending unless binding prefers immediate match
  key matches no pattern -> Rejected -> Idle
```

Patterns should be represented as a trie for efficient prefix matching. Count tokens are trie edges with custom digit accumulation.

Ambiguity handling:

- Prefer the longest match by default.
- If a pattern is both complete and a prefix of another pattern, stay `Pending` until the timeout or another key resolves the ambiguity.
- Allow a binding to set `MatchMode = KeySequenceMatchMode.Immediate` for commands that should execute as soon as complete.

## Integration With Key Routing

The add-on must integrate through public Terminal.Gui APIs. It must not edit `View.NewKeyDownEvent`, `IKeyboard`, or other core routing internals.

Focused-view routing:

1. `UseKeySequences (this View view, ...)` subscribes to `view.KeyDownNotHandled` by default.
2. The add-on processes a key only after the view and its normal key bindings do not handle it.
3. During active sequence capture, the add-on marks keys as handled when the public event args allow it.
4. An opt-in early interception mode can subscribe to `view.KeyDown` for scenarios where a leader must preempt normal key bindings.

This default preserves normal Terminal.Gui behavior and avoids making leader keys shadow existing bindings unless the app explicitly asks for that behavior.

Application-scoped routing:

1. `UseKeySequences (this IApplication application, ...)` subscribes to the public application keyboard event.
2. The add-on should run after focused view routing whenever the public event model allows that ordering.
3. If current public events cannot provide reliable ordering, the add-on should document the limitation and propose a small future core hook rather than adding sequence APIs to core.

## Handler Semantics

Handlers return `true` when they consumed the sequence.

```csharp
bindings.Add ("; m <count> k", context =>
{
    for (int i = 0; i < context.Count; i++)
    {
        context.Target.InvokeCommand (Command.Up);
    }

    return true;
});
```

Handlers may call existing `View.InvokeCommand` APIs or custom view methods. This keeps the sequence layer generic and lets views decide how to interpret motions.

For operator-plus-motion commands:

```csharp
bindings.Add ("; d <count> d", context =>
{
    MyEditorView editor = (MyEditorView)context.Target;
    editor.DeleteLines (context.Count);

    return true;
});
```

If a handler returns `false`, the sequence is considered matched but not consumed. The original keys are not replayed in the first implementation. This avoids surprising reentrancy. A later enhancement can add replay.

## Configuration Examples

Focused view:

```csharp
TextView editor = new ();

IDisposable sequenceRegistration = editor.UseKeySequences (bindings =>
{
    bindings.AddLeader (';');
    bindings.Add ("; m <count> k", MoveCursorUp);
    bindings.Add ("; d <count> d", DeleteLines);
});
```

Application-scoped:

```csharp
IDisposable sequenceRegistration = app.UseKeySequences (bindings =>
{
    bindings.AddLeader (Key.Space);
    bindings.Add ("<Space> f f", context =>
    {
        ShowFileFinder ();
        return true;
    });
});
```

Multiple leaders:

```csharp
IDisposable sequenceRegistration = view.UseKeySequences (bindings =>
{
    bindings.AddLeader (';');
    bindings.AddLeader (Key.G);
});
```

## User Feedback

The initial implementation should expose state but not prescribe UI.

```csharp
public event EventHandler<KeySequenceStateChangedEventArgs>? StateChanged;
```

The event should include:

- Current state.
- Leader key.
- Entered keys.
- Current count text.
- Candidate pattern count.

Applications can use this to show command palettes, status bar hints, or transient prompts.

## Cancellation And Timeout

Defaults:

- `CancelKey = Key.Esc`
- `Timeout = TimeSpan.FromSeconds (1)`

Timeout requires an injected time provider or application timeout integration for testability. Use `ITimeProvider` or existing timed-event infrastructure instead of direct `DateTime.Now`.

Pressing `Esc` during capture cancels and consumes the key. Pressing an invalid key rejects the sequence and consumes the invalid key by default. Add a future option for replay if user testing shows that invalid input should fall through.

## Error Handling

- Adding a pattern without a leader throws `ArgumentException`.
- Adding a duplicate pattern throws `InvalidOperationException`.
- Adding a pattern with multiple `<count>` tokens throws `ArgumentException`.
- Adding an invalid key token throws `FormatException` from the string parser.
- Processing `Key.Empty`, invalid keys, or modifier-only keys returns `Rejected` during capture and `NotLeader` while idle.

## Add-On File Changes

Likely implementation files in the add-on package:

- `Terminal.Gui.KeySequences/Terminal.Gui.KeySequences.csproj`
- `Terminal.Gui.KeySequences/KeySequenceBindings.cs`
- `Terminal.Gui.KeySequences/KeySequenceBinding.cs`
- `Terminal.Gui.KeySequences/KeySequencePattern.cs`
- `Terminal.Gui.KeySequences/KeySequenceContext.cs`
- `Terminal.Gui.KeySequences/KeySequenceParser.cs`
- `Terminal.Gui.KeySequences/KeySequenceResult.cs`
- `Terminal.Gui.KeySequences/KeySequenceStateChangedEventArgs.cs`
- `Terminal.Gui.KeySequences/KeySequenceViewExtensions.cs`
- `Terminal.Gui.KeySequences/KeySequenceApplicationExtensions.cs`
- `Terminal.Gui.KeySequences/KeySequenceMatchMode.cs`
- `Terminal.Gui.KeySequences/README.md`
- `docfx/apispec/namespace-keysequences.md` if this repository hosts the package docs

Test files:

- `Terminal.Gui.KeySequences.Tests/KeySequenceBindingsTests.cs`
- `Terminal.Gui.KeySequences.Tests/KeySequenceParserTests.cs`
- `Terminal.Gui.KeySequences.Tests/ViewKeySequenceExtensionTests.cs`
- `Terminal.Gui.KeySequences.Tests/ApplicationKeySequenceExtensionTests.cs`

Avoid changes to:

- `Terminal.Gui/Input/Keyboard/KeyBindings.cs`
- `Terminal.Gui/ViewBase/View.Keyboard.cs`
- `Terminal.Gui/App/Keyboard/IKeyboard.cs`
- `Terminal.Gui/App/Keyboard/ApplicationKeyboard.cs`
- `Terminal.Gui/Input/Command.cs`

## Verification Plan

- Unit test leader detection and reset behavior.
- Unit test literal sequence matching.
- Unit test count parsing for `4`, `04`, missing count, and rejected zero count.
- Unit test operator-plus-count-plus-motion matching.
- Unit test ambiguous prefix behavior and timeout behavior.
- Unit test cancellation with `Esc`.
- Unit test invalid sequence rejection.
- Unit test focused SubView receives sequences before its SuperView when both have add-on registrations.
- Unit test normal `KeyBindings` still work when no leader is active.
- Unit test leader key consumes normal bindings only when it starts a configured sequence.
- Unit test application-scoped sequences run only when the add-on receives unhandled application input.
- Run:

```bash
dotnet test --project Terminal.Gui.KeySequences.Tests --filter-class "*KeySequenceBindingsTests"
dotnet test --project Terminal.Gui.KeySequences.Tests --filter-class "*ViewKeySequenceExtensionTests"
```

## Documentation Plan

Add package documentation explaining:

- How to install the add-on package.
- How leader capture fits into existing public key events.
- How counts are parsed.
- How to define focused-view and application-scoped sequences.
- How sequence handlers should invoke existing commands.
- Why this is not part of Terminal.Gui core.

Use the documentation instruction style:

```text
To define a leader sequence, add a leader key and bind a sequence pattern to a handler.
```

## Open Questions

- Should the add-on subscribe to `KeyDown` or `KeyDownNotHandled` by default? This spec recommends `KeyDownNotHandled` for focused views when possible, with an opt-in early interception mode for leader keys that must preempt normal bindings.
    - Use `KeyDownNotHandled` for focused views when possible, with an opt-in early interception mode for leader keys that must preempt normal bindings.
- Should invalid keys be replayed to normal key handling? This spec proposes no replay for the first implementation.
    - No replay for the first implementation.
- Should sequence bindings be configurable through Terminal.Gui `ConfigurationManager`, or should the add-on own its own configuration model?
    - Use Terminal.Gui `ConfigurationManager`.
- Should repeated operators such as `d d` be represented as operator-plus-motion metadata, or should handlers infer semantics from the matched pattern?
- Should a later core hook be proposed if public events cannot provide the needed routing precision?
    - Yes.

## Recommended First Milestone

Build a focused-view-only add-on implementation first:

1. Add pattern parsing, trie matching, count parsing, and handler invocation.
2. Add `UseKeySequences` extension methods for `View`.
3. Integrate through public key events only.
4. Add focused-view tests for `; m 4 k` and `; d 2 d`.
5. Document the API.

After that is stable, add `IApplication.UseKeySequences` for application-scoped sequences and add-on-owned configuration support.
