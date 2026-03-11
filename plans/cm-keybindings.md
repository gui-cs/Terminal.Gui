# ConfigurationManager Key Bindings — PR #4266

> Branch: `feature/cm-keybindings` → `v2_develop` on gui-cs/Terminal.Gui
> Original PR: https://github.com/gui-cs/Terminal.Gui/pull/4266 (branch was renamed; new PR needed)
> Fixes: #3023, #3089

---

## Implementation Status

### ✅ Completed

| Phase | Description | Status |
|-------|-------------|--------|
| Infrastructure | `KeyBindingConfigHelper.cs` — `Apply(view, baseBindings, platformBindings)` | ✅ Done |
| Infrastructure | Registered `Dictionary<string, string[]>` and `string[]` in `SourceGenerationContext.cs` | ✅ Done |
| TextField | Migrated to `DefaultKeyBindings` + `DefaultKeyBindingsUnix` static properties | ✅ Done |
| TextView | Migrated; preserves dynamic Enter binding (Multiline) and Space removal after Apply | ✅ Done |
| ListView | Migrated; multi-command bindings (Shift+Space) stay as direct `KeyBindings.Add` | ✅ Done |
| TableView | Migrated; dynamic `CellActivationKey` stays as direct `KeyBindings.Add` | ✅ Done |
| TabView | Migrated | ✅ Done |
| HexView | Migrated | ✅ Done |
| DropDownList | Migrated; uses `new` keyword to hide inherited TextField members | ✅ Done |
| NumericUpDown | `[ConfigurationProperty]` on non-generic `NumericUpDown` class; generic `NumericUpDown<T>` references it | ✅ Done |
| TreeView | `[ConfigurationProperty]` on non-generic `TreeView` class; generic `TreeView<T>` references it | ✅ Done |
| LinearRange | `[ConfigurationProperty]` on non-generic `LinearRange` class; generic `LinearRange<T>` references it | ✅ Done |
| config.json | All 10 views have `DefaultKeyBindings` entries in config.json | ✅ Done |
| Tests — Apply | 14 low-level tests in `Configuration/KeyBindingConfigHelperTests.cs` (View + CommandNotBound only, no Views dependency) | ✅ Done |
| Tests — Views | 26 tests in `Views/DefaultKeyBindingsTests.cs` (static props, key string validation, CM discovery, binding consistency, behavior) | ✅ Done |
| Generic type fix | Moved `[ConfigurationProperty]` from generic base classes to non-generic derived classes (TreeView, NumericUpDown, LinearRange) | ✅ Done |
| InsertChar fix | HexView: `"InsertChar"` → `"Insert"` (Key.TryParse resolves via KeyCode enum) | ✅ Done |

### 🔲 Remaining

| Task | Description | Priority |
|------|-------------|----------|
| Documentation | Update `docfx/docs/keyboard.md` and `docfx/docs/config.md` with configurable key bindings sections | High |
| New PR | Create PR from `feature/cm-keybindings` → `v2_develop` | High |
| Undo/Redo inconsistency | TextField uses `Ctrl+Y` for Redo; TextView uses `Ctrl+R` for Redo and `Ctrl+Y` for Paste. Consider unifying or documenting. | Low / Out of scope |

### All tests pass: 15,836 total (14,819 parallelizable + 1,017 non-parallelizable)

---

## Architecture

### Pattern: Two Static Properties Per View

Each view that has configurable key bindings exposes:

```csharp
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, string[]>? DefaultKeyBindings { get; set; } = new ()
{
    { "CommandName", ["KeyString1", "KeyString2"] },
    // ...
};

[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, string[]>? DefaultKeyBindingsUnix { get; set; }
```

- **`DefaultKeyBindings`** — applied on all platforms (initialized with C# defaults)
- **`DefaultKeyBindingsUnix`** — overlaid on non-Windows at runtime (null by default = no overrides)

### KeyBindingConfigHelper.Apply()

```csharp
internal static void Apply (
    View view,
    Dictionary<string, string[]>? baseBindings,
    Dictionary<string, string[]>? platformBindings = null)
```

1. Iterates `baseBindings` entries
2. Parses command name via `Enum.TryParse<Command>()`
3. Parses each key string via `Key.TryParse()`
4. Skips already-bound keys (`view.KeyBindings.TryGet(key, out _)`)
5. If non-Windows, also applies `platformBindings`
6. Silently skips invalid command names and unparseable key strings

### config.json Format

```jsonc
// In "View Specific Settings" section:
"TextField.DefaultKeyBindings": {
    "Left": [ "CursorLeft", "Ctrl+B" ],
    "Right": [ "CursorRight", "Ctrl+F" ],
    "Undo": [ "Ctrl+Z" ],
    // ...
},
"TextField.DefaultKeyBindingsUnix": null
```

### Generic Type Constraint

**CRITICAL**: `[ConfigurationProperty]` CANNOT be placed on open generic types (`TreeView<T>`, `NumericUpDown<T>`, `LinearRange<T>`). CM reflection calls `PropertyInfo.GetValue(null)` which throws `InvalidOperationException` on open generics.

**Solution**: Place the static properties on a non-generic class (e.g., `TreeView`) and have the generic constructor reference them explicitly:

```csharp
// Non-generic class holds the config properties
public class TreeView : TreeView<TreeNode> { /* ... */ }

// In TreeView<T> constructor:
KeyBindingConfigHelper.Apply (this, TreeView.DefaultKeyBindings, TreeView.DefaultKeyBindingsUnix);
```

### Bindings That Stay as Direct KeyBindings.Add()

Some bindings cannot be expressed in the `Dictionary<string, string[]>` format:

- **Multi-command**: `KeyBindings.Add(Key.Space.WithShift, Command.Activate, Command.Down)` (ListView)
- **Data-bearing**: `KeyBindings.Add(Key.A.WithCtrl, new KeyBinding([Command.SelectAll], true))` (ListView mark-all)
- **Dynamic/instance**: `KeyBindings.Add(CellActivationKey, Command.Accept)` (TableView), `KeyBindings.Add(ObjectActivationKey, Command.Activate)` (TreeView)
- **Conditional**: Enter binding depends on `Multiline` property (TextView)
- **Orientation-dependent**: `SetKeyBindings()` (LinearRange)

### Test Structure

- **`Tests/UnitTestsParallelizable/Configuration/KeyBindingConfigHelperTests.cs`** (14 tests)
  - Low-level `Apply` tests using only `View` + `CommandNotBound` event
  - Zero dependencies on `Terminal.Gui.Views`
  
- **`Tests/UnitTestsParallelizable/Views/DefaultKeyBindingsTests.cs`** (26 tests)
  - Static property validation for all 10 views
  - Key string parseability (catches typos)
  - CM discovery verification (catches generic type issues)
  - Binding consistency (every declared key exists on fresh instance)
  - Behavioral tests (keys actually trigger expected actions)

---

This PR provides a comprehensive design document for addressing issue #3089, which requests the ability to configure default key bindings through ConfigurationManager. Currently, all default key bindings in Terminal.Gui are hard-coded in View constructors, making them non-configurable by users.

### Problem Statement

Terminal.Gui views like `TextField` have key bindings hard-coded in their constructors:

```csharp
// Current approach in TextField constructor
KeyBindings.Add(Key.Delete, Command.DeleteCharRight);
KeyBindings.Add(Key.D.WithCtrl, Command.DeleteCharRight);
KeyBindings.Add(Key.Backspace, Command.DeleteCharLeft);
```

This creates several issues:
- Users cannot customize default key bindings without modifying source code
- Platform-specific conventions (e.g., Delete on Windows vs Ctrl+D on Linux) cannot be configured
- No way to override bindings at system, user, or application level

### Proposed Design

#### 1. Configuration Structure

Introduce a new `DefaultKeyBindings` section in `config.json`:

```json
{
  "DefaultKeyBindings": {
    "TextField": [
      {
        "Command": "DeleteCharRight",
        "Keys": ["Delete"],
        "Platforms": ["Windows", "Linux", "macOS"]
      },
      {
        "Command": "DeleteCharRight",
        "Keys": ["Ctrl+D"],
        "Platforms": ["Linux", "macOS"]
      }
    ]
  }
}
```

#### 2. Implementation Approach

**New Classes:**
- `KeyBindingConfig`: Represents a configurable key binding with command, keys, and platform filters
- `DefaultKeyBindingsScope`: Static scope containing all default bindings configuration
- `KeyBindingConfigManager`: Helper class that applies platform-filtered bindings to Views

**Integration:**
Views would call a helper method during initialization that automatically applies the appropriate platform-specific bindings from configuration:

```csharp
// In TextField constructor
KeyBindingConfigManager.ApplyDefaultBindings(this, "TextField");
```

Platform filtering happens automatically based on `RuntimeInformation.IsOSPlatform()`.

#### 3. Key Design Decisions

**Challenge: Static vs Instance Properties**
- ConfigurationManager requires `static` properties (enforced by reflection)
- KeyBindings are instance properties on each View
- **Solution**: Use a static configuration dictionary accessed by a helper manager class

**Challenge: Platform-Specific Bindings**
- Different platforms need different key conventions
- **Solution**: Include explicit platform filters in configuration; helper class filters at runtime

**Challenge: Backward Compatibility**
- Existing code manually calls `KeyBindings.Add()`
- **Solution**: Config-based bindings applied first; manual additions still work and can override

### Migration Path

1. **Phase 1**: Create infrastructure (classes, JSON support, manager)
2. **Phase 2**: Migrate TextField as proof-of-concept
3. **Phase 3**: Systematically migrate remaining views
4. **Phase 4**: Update documentation

### Status

**This PR contains the design document only** — no implementation code has been written yet. This design review is intended to gather feedback before proceeding with implementation.

### Open Questions

1. Should we support platform wildcards like "Unix" (Linux + macOS)?
2. How should view inheritance work? Should subclasses inherit parent bindings automatically?
3. Should we validate Commands at config load time or silently skip invalid ones?
4. Best format for "all platforms" — explicit list or special "All" value?

---

## Original Issue Text (#3089 / #3023)

### All built-in view subclasses should use ConfigurationManager to specify the default keybindings.

For example, `TextField` currently has code like this in its constructor:

```cs
KeyBindings.Add (KeyCode.DeleteChar, Command.DeleteCharRight);
KeyBindings.Add (KeyCode.D | KeyCode.CtrlMask, Command.DeleteCharRight);

KeyBindings.Add (KeyCode.Delete, Command.DeleteCharLeft);
KeyBindings.Add (KeyCode.Backspace, Command.DeleteCharLeft);
```

This should be replaced with configuration in `.\Terminal.Gui\Resources\config.json` like this:

```json
"TextField.DefaultKeyBindings": {
  "DeleteCharRight" : {
    "Key" : "DeleteChar"
  },
  "DeleteCharRight" : {
    "Key" : "Ctrl+D"
  },
  "DeleteCharLeft" : {
     "Key" : "Delete"
  },
  "DeleteCharLeft" : {
     "Key" : "Backspace"
  }
}
```

For this to work, `View` and any subclass that defines default keybindings should have a member like this:

```cs
public partial class View : Responder, ISupportInitializeNotification {

    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Dictionary DefaultKeyBindings { get; set; }
```

(This requires more thought — because CM requires config properties to be `static` it's not possible to inherit default keybindings!)

### Default KeyBinding mappings should be platform specific

The above config.json example includes both the Windows (DeleteChar) and Linux (Ctrl-D) idioms. When a user is on Linux, only the Linux binding should work and vice versa.

We need to figure out a way of enabling this. Current best ideas:

- Have each View specify all possibilities in `config.json`, but have a flag that indicates platform.
- Have some way for `ConsoleDrivers` to have mappings within them. This may not be a good idea given some drivers (esp Netdriver) run on all platforms.

### The codebase should be scoured for cases where code is looking at `Key`s and not using KeyBindings.

---

## PR Comment: @tig — Thoughts on Ctrl-Z / Suspend

> Date: 2026-03-10

Here's how it works today:

- It's only supported on *nix platforms. On Windows ctrl-z does nothing if not handled by a View.
- On *nix, if no View handles ctrl-z, the app suspends and `fg` resumes.

It's been a long time since I regularly used *nix. Back when I did, ctrlz/fg was muscle memory. I suspect most *nix TUI users expect it to ALWAYS work. Or, at least, they expect to be able to configure things such that ctrl-z will always suspend.

If, as part of this PR, `CtrlZ` was not mapped to Undo by default when on *nix or Mac, then unless someone tried really hard, ctrl-z would always work. That's cool.

**There are legitimate (though relatively rare) cases** where a *nix/macOS TUI application author might deliberately want to prevent or strongly discourage **Ctrl+Z** (SIGTSTP) from suspending the process.

Here are the main realistic scenarios:

1. **The program is performing critical, non-idempotent or dangerous work**
   - Actively writing to a database / journal / blockchain / filesystem
   - Holding exclusive locks on hardware
   - Running inside a restricted sandbox where resuming after suspension is unreliable

2. **The TUI is part of a long-running daemon-style tool that should not be backgrounded**
   - AI coding agents, local LLM front-ends, build servers with live progress, debuggers/profilers
   - Suspension is either useless or actively harmful

3. **Security-oriented or tightly controlled environments**
   - Kiosk-like setups, shared student lab machines, CI runners

4. **The application re-uses Ctrl+Z for its own shortcut**
   - Very uncommon nowadays, but historically seen

### How applications prevent / weaken Ctrl+Z today

| Technique | Effect | Considered good practice? |
|-|-|-|
| `signal(SIGTSTP, SIG_IGN)` | ^Z does nothing | Usually frowned upon |
| `signal(SIGTSTP, custom_handler)` | Prints status/warning then exits or re-sends SIGSTOP | Better than plain ignore |
| Raw mode + no special-char processing | ^Z becomes a normal key — app can bind it | Normal & expected |
| Leave SIGTSTP alone | Classic behavior | Usually best choice |

### Bottom line – most common answer in 2025/2026

For **well-behaved everyday TUIs** almost nobody disables ^Z anymore — users expect it to work.

### Suggested key for undo on *nix

Popular choices that avoid most conflicts:

- **Ctrl+/** — quite discoverable
- **Ctrl+Shift+Z** — familiar from GUI
- **Alt+Z** / **⌥Z** (Mac-friendly)
- **Ctrl+Y** — sometimes used for "yank"/paste in emacs-style, but can conflict

Many modern TUIs use **Ctrl+Z** for undo on Windows but **Ctrl+/** or similar on Unix-like to preserve suspend.

---

## Issue Comment Thread (from #3089)

@tig on KeyJsonConverter:
- Wanted same format as `ToString`/`TryParse` — `"Key+modifiers"` is simple and easy to remember
- The old format was clumsy and brittle
- Making it internal was intentional; doesn't like making things public until there's a clear need
- Not eager to rewrite CM to use `Microsoft.Extensions.Configuration` — CM does a lot of things that would need to be supported in a replacement

---

## Deep Dive: ConfigurationManager Constraints

From analysis of the existing CM infrastructure:

### Hard Requirements
- **Property MUST be `public static`** — CM reflection throws `InvalidOperationException` otherwise
- **JSON key format**: `ClassName.PropertyName` (or just `PropertyName` if `OmitClassName = true`)
- **Scope**: `[ConfigurationProperty(Scope = typeof(SettingsScope))]` for top-level config.json entries
- **Unknown JSON keys throw** `JsonException` — no forward compatibility; every new property must be registered

### Type Support
- `Key` already serializes as a **plain string** (`"Ctrl+Z"`, `"Shift+F10"`) via the globally-registered `KeyJsonConverter`
- New collection types (e.g. `Dictionary<string, string[]>`) must be added to `SourceGenerationContext`
- `KeyBinding`/`KeyBindings` are **NOT serializable as-is** — they contain `WeakReference<View>` and `View` fields

### Practical Serializable Type for a Bindings Map
The simplest type that works:
```csharp
// Command name (string) → one or more key strings
public static Dictionary<string, string[]> DefaultKeyBindings { get; set; }
```
- `string[]` needs `[JsonSerializable(typeof(string[]))]` (likely already registered)
- `Dictionary<string, string[]>` needs `[JsonSerializable(typeof(Dictionary<string, string[]>))]`
- No new converters required — `Key` round-trips via `Key.TryParse()`/`Key.ToString()`

---

## Existing Application-Level Key Properties (Already in config.json)

These already work and will NOT change format:

```json
"Application.QuitKey": "Esc",
"Application.ArrangeKey": "Ctrl+F5",
"Application.NextTabKey": "Tab",
"Application.PrevTabKey": "Shift+Tab",
"Application.NextTabGroupKey": "F6",
"Application.PrevTabGroupKey": "Shift+F6",
"PopoverMenu.DefaultKey": "Shift+F10"
```

---

## Platform Binding Strategy

### The Problem
Several key conventions differ fundamentally by platform:
- **`Ctrl+Z`** = Undo on Windows/macOS GUI; = SIGTSTP (suspend) on Unix TUI
- **`Ctrl+Y`** = Redo on Windows; = Paste/Yank in emacs/Unix TUI
- **`Ctrl+R`** = DeleteAll in TextField; = Redo in TextView (inconsistency!)
- **`Ctrl+C`** = Copy GUI; = SIGINT on some Unix terminals (handled by raw mode)
- **`Ctrl+W`** = not bound in TextField; = Cut in TextView (emacs kill-region)

### Guiding Principle
> **Prefer popular TUI conventions (vim, emacs, less, tig, ranger) over GUI conventions (Word, Notepad) on Unix.**
> On Windows, GUI conventions are fine since `Ctrl+Z` suspend doesn't apply.

### Design: Two Static Properties Per Class
Use **two** `public static` properties per view — a base set and a Unix-specific override. CM merges them at runtime:

```json
"TextField.DefaultKeyBindings": {
  "Undo":  ["Ctrl+Z"],
  "Redo":  ["Ctrl+Y", "Ctrl+Shift+Z"]
},
"TextField.DefaultKeyBindingsUnix": {
  "Undo":  ["Ctrl+Slash"],
  "Redo":  ["Ctrl+Shift+Z"]
}
```

The view's `CreateCommandsAndBindings()` applies `DefaultKeyBindings` first, then overlays `DefaultKeyBindingsUnix` (or `DefaultKeyBindingsWindows` if needed) at runtime using `RuntimeInformation.IsOSPlatform()`.

---

## Complete Existing Key Bindings (Ground Truth)

### `View` (base class) — `View.Keyboard.cs`

| Key | Command | Notes |
|-----|---------|-------|
| `Space` | `Activate` | All views |
| `Enter` | `Accept` | All views |

### `Application` — `ApplicationKeyboard.cs`

These are already single-Key properties in config.json; no change to format needed.

| Property | Default | Command |
|----------|---------|---------|
| `Application.QuitKey` | `Esc` | `Quit` |
| `Application.NextTabKey` | `Tab` | `NextTabStop` |
| `Application.PrevTabKey` | `Shift+Tab` | `PreviousTabStop` |
| `Application.NextTabGroupKey` | `F6` | `NextTabGroup` |
| `Application.PrevTabGroupKey` | `Shift+F6` | `PreviousTabGroup` |
| `Application.ArrangeKey` | `Ctrl+F5` | `Arrange` |

Additionally hardcoded (not configurable today):
- `CursorRight` / `CursorDown` → `NextTabStop` (dialog navigation)

### `TextField` — `TextField.Commands.cs`

| Key(s) | Command | TUI Notes |
|--------|---------|-----------|
| `Delete` | `DeleteCharRight` | Universal |
| `Ctrl+D` | `DeleteCharRight` | Emacs |
| `Backspace` | `DeleteCharLeft` | Universal |
| `Home`, `Ctrl+Home` | `LeftStart` | Universal |
| `End`, `Ctrl+End`, `Ctrl+E` | `RightEnd` | Universal / Emacs |
| `CursorLeft`, `Ctrl+B` | `Left` | Universal / Emacs |
| `CursorRight`, `Ctrl+F` | `Right` | Universal / Emacs |
| `Ctrl+CursorLeft`, `Ctrl+CursorUp` | `WordLeft` | Universal |
| `Ctrl+CursorRight`, `Ctrl+CursorDown` | `WordRight` | Universal |
| `Shift+CursorLeft`, `Shift+CursorUp` | `LeftExtend` | Universal |
| `Shift+CursorRight`, `Shift+CursorDown` | `RightExtend` | Universal |
| `Ctrl+Shift+CursorLeft`, `Ctrl+Shift+CursorUp` | `WordLeftExtend` | |
| `Ctrl+Shift+CursorRight`, `Ctrl+Shift+CursorDown` | `WordRightExtend` | |
| `Shift+Home`, `Ctrl+Shift+Home`, `Ctrl+Shift+A` | `LeftStartExtend` | |
| `Shift+End`, `Ctrl+Shift+End`, `Ctrl+Shift+E` | `RightEndExtend` | |
| `Ctrl+K` | `CutToEndOfLine` | Emacs kill-line |
| `Ctrl+Shift+K` | `CutToStartOfLine` | |
| **`Ctrl+Z`** | `Undo` | ⚠️ Unix: conflicts with SIGTSTP |
| **`Ctrl+Y`** | `Redo` | ⚠️ Emacs: Ctrl+Y = paste (yank) |
| `Ctrl+Delete` | `KillWordRight` | kill-word-forward |
| `Ctrl+Backspace` | `KillWordLeft` | kill-word-backward |
| `Insert` | `ToggleOverwrite` | Universal |
| `Ctrl+C` | `Copy` | Universal (raw mode safe) |
| `Ctrl+X` | `Cut` | Universal |
| `Ctrl+V` | `Paste` | Universal |
| `Ctrl+A` | `SelectAll` | ⚠️ Emacs: Ctrl+A = line start |
| `Ctrl+R`, `Ctrl+Shift+D` | `DeleteAll` | ⚠️ Ctrl+R = Redo in TextView! |

### `TextView` — `TextView.Commands.cs`

Inherits TextField commands. Key **differences and additions**:

| Key(s) | Command | Notes |
|--------|---------|-------|
| `Enter` | `NewLine` (if multiline) / `Accept` | |
| `PageDown`, `Ctrl+V` | `PageDown` | Ctrl+V = pgdn in emacs |
| `Shift+PageDown` | `PageDownExtend` | |
| `PageUp` | `PageUp` | |
| `Shift+PageUp` | `PageUpExtend` | |
| `CursorDown`, `Ctrl+N` | `Down` | Ctrl+N = next in emacs |
| `Shift+CursorDown` | `DownExtend` | |
| `CursorUp`, `Ctrl+P` | `Up` | Ctrl+P = prev in emacs |
| `Shift+CursorUp` | `UpExtend` | |
| `Ctrl+End` | `End` (doc end) | |
| `Ctrl+Shift+End` | `EndExtend` | |
| `Ctrl+Home` | `Start` (doc start) | |
| `Ctrl+Shift+Home` | `StartExtend` | |
| `Ctrl+Space` | `ToggleExtend` | Emacs mark |
| **`Ctrl+Y`** | `Paste` | ⚠️ Emacs yank (≠ TextField's Redo!) |
| `Ctrl+W`, `Ctrl+X` | `Cut` | Emacs kill-region + GUI |
| `Ctrl+Shift+Delete` | `CutToEndOfLine` | |
| `Ctrl+Shift+Backspace` | `CutToStartOfLine` | |
| `Ctrl+Shift+Right` | `WordRightExtend` | |
| `Ctrl+Shift+Left` | `WordLeftExtend` | |
| `Tab` | `NextTabStop` | |
| `Shift+Tab` | `PreviousTabStop` | |
| `Ctrl+Z` | `Undo` | ⚠️ Same Unix issue |
| **`Ctrl+R`** | `Redo` | ⚠️ Conflicts with TextField's DeleteAll! |
| `Ctrl+G` , `Ctrl+Shift+D` | `DeleteAll` | |
| `Ctrl+L` | `Open` (color picker) | |

### `ListView` — `ListView.Commands.cs`

| Key(s) | Command |
|--------|---------|
| `CursorUp`, `Ctrl+P` | `Up` |
| `CursorDown`, `Ctrl+N` | `Down` |
| `PageUp` | `PageUp` |
| `PageDown`, `Ctrl+V` | `PageDown` |
| `Home` | `Start` |
| `End` | `End` |
| `Shift+CursorUp`, `Ctrl+Shift+P` | `UpExtend` |
| `Shift+CursorDown`, `Ctrl+Shift+N` | `DownExtend` |
| `Shift+PageUp` | `PageUpExtend` |
| `Shift+PageDown` | `PageDownExtend` |
| `Shift+Home` | `StartExtend` |
| `Shift+End` | `EndExtend` |
| `Shift+Space` | `Activate` + `Down` |
| `Ctrl+A` | `SelectAll` (mark all) |
| `Ctrl+U` | `SelectAll` (unmark all) |

### `TableView` — `TableView.cs`

| Key(s) | Command |
|--------|---------|
| `CursorLeft` | `Left` |
| `CursorRight` | `Right` |
| `CursorUp` | `Up` |
| `CursorDown` | `Down` |
| `PageUp` | `PageUp` |
| `PageDown` | `PageDown` |
| `Home` | `LeftStart` (row start) |
| `End` | `RightEnd` (row end) |
| `Ctrl+Home` | `Start` (table start) |
| `Ctrl+End` | `End` (table end) |
| `Shift+CursorLeft` | `LeftExtend` |
| `Shift+CursorRight` | `RightExtend` |
| `Shift+CursorUp` | `UpExtend` |
| `Shift+CursorDown` | `DownExtend` |
| `Shift+PageUp` | `PageUpExtend` |
| `Shift+PageDown` | `PageDownExtend` |
| `Shift+Home` | `LeftStartExtend` |
| `Shift+End` | `RightEndExtend` |
| `Ctrl+Shift+Home` | `StartExtend` |
| `Ctrl+Shift+End` | `EndExtend` |
| `Ctrl+A` | `SelectAll` |
| `Enter` | `Accept` (`CellActivationKey`) |

### `TreeView` — `TreeView.cs`

| Key(s) | Command |
|--------|---------|
| `CursorUp` | `Up` |
| `Shift+CursorUp` | `UpExtend` |
| `Ctrl+CursorUp` | `LineUpToFirstBranch` |
| `CursorDown` | `Down` |
| `Shift+CursorDown` | `DownExtend` |
| `Ctrl+CursorDown` | `LineDownToLastBranch` |
| `CursorRight` | `Expand` |
| `Ctrl+CursorRight` | `ExpandAll` |
| `CursorLeft` | `Collapse` |
| `Ctrl+CursorLeft` | `CollapseAll` |
| `PageUp` | `PageUp` |
| `PageDown` | `PageDown` |
| `Shift+PageUp` | `PageUpExtend` |
| `Shift+PageDown` | `PageDownExtend` |
| `Home` | `Start` |
| `End` | `End` |
| `Ctrl+A` | `SelectAll` |
| `Enter` | `Activate` (`ObjectActivationKey`) |

### `TabView` — `TabView.cs`

| Key(s) | Command |
|--------|---------|
| `CursorLeft` | `Left` (prev tab) |
| `CursorRight` | `Right` (next tab) |
| `CursorUp` | `Up` |
| `CursorDown` | `Down` |
| `Home` | `LeftStart` (first tab) |
| `End` | `RightEnd` (last tab) |
| `PageUp` | `PageUp` |
| `PageDown` | `PageDown` |

### Other Views (minor bindings)

**`DropDownList`**: `F4` → `Toggle`, `CursorDown` → open

**`HexView`**: Arrow keys, PageUp/Down, Home, End, Backspace→`DeleteCharLeft`, Delete→`DeleteCharRight`, Insert→`ToggleOverwrite`

**`NumericUpDown`**: `CursorUp`→`Up`, `CursorDown`→`Down`

**`ColorBar`**: `CursorLeft`/`Right` + Shift extend variants + `Home`/`End`

**`ColorPicker.16`**: Arrow keys for 16-color grid

**`CharMap`**: Arrow keys, PageUp/Down, Home, End, `Shift+F10`→`Context`

**`LinearRange`**: Arrow keys, PageUp/Down, Home, End, extend variants

**`PopoverImpl`**: `Application.QuitKey` → `Quit`

---

## Inconsistencies Found (Fix as Part of This PR)

| Issue | TextField | TextView | Recommendation |
|-------|-----------|----------|----------------|
| **Redo key** | `Ctrl+Y` | `Ctrl+R` | Standardize: use `Ctrl+Shift+Z` everywhere; `Ctrl+R` on Unix |
| **Paste key** | `Ctrl+V` | `Ctrl+V` + `Ctrl+Y` | TextField should also have `Ctrl+Y`→Paste on Unix |
| **DeleteAll** | `Ctrl+R`, `Ctrl+Shift+D` | `Ctrl+G`, `Ctrl+Shift+D` | Remove `Ctrl+R` from TextField (conflicts with Redo); keep `Ctrl+Shift+D` for all |
| **CutToStart** | `Ctrl+Shift+K` | `Ctrl+Shift+Backspace` | Different keys — pick one or bind both everywhere |
| **`Ctrl+W`** (Cut) | Not bound | Bound | Add to TextField on Unix |
| **`Ctrl+V`** (PageDown in emacs) | Not a pager | `Ctrl+V`→PageDown | Fine; no conflict |

---

## Proposed `config.json` Additions

### Design Rules Applied
1. All keys that are **universal** (same on all platforms) go in the base `DefaultKeyBindings`
2. Keys that **conflict with Unix signals or conventions** go in `DefaultKeyBindingsUnix` (override)
3. On Unix, omitting a key from the override means the base binding is **cleared** for that command if it conflicts — the override **replaces**, not appends, for that command
4. Emacs shortcuts are included in both base and Unix sections as they're safe on both

```json
{
  "$schema": "https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json",

  // ─── Existing application-level key properties (unchanged) ────────────────
  "Application.QuitKey": "Esc",
  "Application.ArrangeKey": "Ctrl+F5",
  "Application.NextTabKey": "Tab",
  "Application.PrevTabKey": "Shift+Tab",
  "Application.NextTabGroupKey": "F6",
  "Application.PrevTabGroupKey": "Shift+F6",
  "PopoverMenu.DefaultKey": "Shift+F10",

  // ─── View (base class) ────────────────────────────────────────────────────
  "View.DefaultKeyBindings": {
    "Activate": [ "Space" ],
    "Accept":   [ "Enter" ]
  },

  // ─── TextField ────────────────────────────────────────────────────────────
  "TextField.DefaultKeyBindings": {
    "DeleteCharRight":   [ "Delete", "Ctrl+D" ],
    "DeleteCharLeft":    [ "Backspace" ],
    "LeftStart":         [ "Home", "Ctrl+Home" ],
    "RightEnd":          [ "End", "Ctrl+End", "Ctrl+E" ],
    "Left":              [ "CursorLeft", "Ctrl+B" ],
    "Right":             [ "CursorRight", "Ctrl+F" ],
    "WordLeft":          [ "Ctrl+CursorLeft" ],
    "WordRight":         [ "Ctrl+CursorRight" ],
    "LeftExtend":        [ "Shift+CursorLeft" ],
    "RightExtend":       [ "Shift+CursorRight" ],
    "WordLeftExtend":    [ "Ctrl+Shift+CursorLeft" ],
    "WordRightExtend":   [ "Ctrl+Shift+CursorRight" ],
    "LeftStartExtend":   [ "Shift+Home", "Ctrl+Shift+Home" ],
    "RightEndExtend":    [ "Shift+End", "Ctrl+Shift+End" ],
    "CutToEndOfLine":    [ "Ctrl+K" ],
    "CutToStartOfLine":  [ "Ctrl+Shift+K" ],
    "Undo":              [ "Ctrl+Z" ],
    "Redo":              [ "Ctrl+Shift+Z" ],
    "KillWordRight":     [ "Ctrl+Delete" ],
    "KillWordLeft":      [ "Ctrl+Backspace" ],
    "ToggleOverwrite":   [ "InsertChar" ],
    "Copy":              [ "Ctrl+C" ],
    "Cut":               [ "Ctrl+X" ],
    "Paste":             [ "Ctrl+V" ],
    "SelectAll":         [ "Ctrl+A" ],
    "DeleteAll":         [ "Ctrl+Shift+D" ],
    "Context":           [ "Shift+F10" ]
  },
  "TextField.DefaultKeyBindingsUnix": {
    // On Unix: Ctrl+Z = SIGTSTP. Use Ctrl+/ for Undo (readline default).
    // Ctrl+Shift+Z works in most modern terminals (xterm, kitty, wezterm) for Redo.
    "Undo":     [ "Ctrl+Slash" ],
    "Redo":     [ "Ctrl+Shift+Z", "Ctrl+R" ],
    "Paste":    [ "Ctrl+V", "Ctrl+Y" ],
    "Cut":      [ "Ctrl+X", "Ctrl+W" ],
    "SelectAll": [ "Ctrl+A" ]
    // Note: Ctrl+A conflicts with emacs 'line start' but SelectAll is more useful in a single-line field
  },

  // ─── TextView ─────────────────────────────────────────────────────────────
  "TextView.DefaultKeyBindings": {
    // Movement (inherits single-line; adds multi-line)
    "Up":              [ "CursorUp", "Ctrl+P" ],
    "Down":            [ "CursorDown", "Ctrl+N" ],
    "PageUp":          [ "PageUp" ],
    "PageDown":        [ "PageDown" ],
    "PageUpExtend":    [ "Shift+PageUp" ],
    "PageDownExtend":  [ "Shift+PageDown" ],
    "Start":           [ "Ctrl+Home" ],
    "End":             [ "Ctrl+End" ],
    "StartExtend":     [ "Ctrl+Shift+Home" ],
    "EndExtend":       [ "Ctrl+Shift+End" ],
    "UpExtend":        [ "Shift+CursorUp" ],
    "DownExtend":      [ "Shift+CursorDown" ],
    "LeftStart":       [ "Home" ],
    "RightEnd":        [ "End", "Ctrl+E" ],
    "LeftStartExtend": [ "Shift+Home" ],
    "RightEndExtend":  [ "Shift+End" ],
    "Left":            [ "CursorLeft", "Ctrl+B" ],
    "Right":           [ "CursorRight", "Ctrl+F" ],
    "LeftExtend":      [ "Shift+CursorLeft" ],
    "RightExtend":     [ "Shift+CursorRight" ],
    "WordLeft":        [ "Ctrl+CursorLeft" ],
    "WordRight":       [ "Ctrl+CursorRight" ],
    "WordLeftExtend":  [ "Ctrl+Shift+CursorLeft" ],
    "WordRightExtend": [ "Ctrl+Shift+CursorRight" ],
    "ToggleExtend":    [ "Ctrl+Space" ],
    // Editing
    "DeleteCharLeft":   [ "Backspace" ],
    "DeleteCharRight":  [ "Delete", "Ctrl+D" ],
    "KillWordRight":    [ "Ctrl+Delete" ],
    "KillWordLeft":     [ "Ctrl+Backspace" ],
    "CutToEndOfLine":   [ "Ctrl+K", "Ctrl+Shift+Delete" ],
    "CutToStartOfLine": [ "Ctrl+Shift+Backspace" ],
    "Undo":             [ "Ctrl+Z" ],
    "Redo":             [ "Ctrl+Shift+Z" ],
    "Copy":             [ "Ctrl+C" ],
    "Cut":              [ "Ctrl+X" ],
    "Paste":            [ "Ctrl+V" ],
    "SelectAll":        [ "Ctrl+A" ],
    "DeleteAll":        [ "Ctrl+Shift+D" ],
    "ToggleOverwrite":  [ "InsertChar" ],
    "NextTabStop":      [ "Tab" ],
    "PreviousTabStop":  [ "Shift+Tab" ],
    "NewLine":          [ "Enter" ],
    "Open":             [ "Ctrl+L" ]
  },
  "TextView.DefaultKeyBindingsUnix": {
    "Undo":   [ "Ctrl+Slash" ],
    "Redo":   [ "Ctrl+Shift+Z", "Ctrl+R" ],
    "Paste":  [ "Ctrl+V", "Ctrl+Y" ],
    "Cut":    [ "Ctrl+X", "Ctrl+W" ],
    // On Unix, Ctrl+V = PageDown in emacs. Override PageDown:
    "PageDown": [ "PageDown", "Ctrl+V" ]
  },

  // ─── ListView ─────────────────────────────────────────────────────────────
  "ListView.DefaultKeyBindings": {
    "Up":             [ "CursorUp", "Ctrl+P" ],
    "Down":           [ "CursorDown", "Ctrl+N" ],
    "PageUp":         [ "PageUp" ],
    "PageDown":       [ "PageDown", "Ctrl+V" ],
    "Start":          [ "Home" ],
    "End":            [ "End" ],
    "UpExtend":       [ "Shift+CursorUp" ],
    "DownExtend":     [ "Shift+CursorDown" ],
    "PageUpExtend":   [ "Shift+PageUp" ],
    "PageDownExtend": [ "Shift+PageDown" ],
    "StartExtend":    [ "Shift+Home" ],
    "EndExtend":      [ "Shift+End" ],
    "SelectAll":      [ "Ctrl+A" ]
  },
  "ListView.DefaultKeyBindingsUnix": {
    // On Unix, Ctrl+V commonly = paste. Keep for pgdn since TUIs (less, htop) use it.
    // No overrides needed — Ctrl+V PageDown is a TUI convention, not a conflict.
  },

  // ─── TableView ────────────────────────────────────────────────────────────
  "TableView.DefaultKeyBindings": {
    "Left":           [ "CursorLeft" ],
    "Right":          [ "CursorRight" ],
    "Up":             [ "CursorUp" ],
    "Down":           [ "CursorDown" ],
    "PageUp":         [ "PageUp" ],
    "PageDown":       [ "PageDown" ],
    "LeftStart":      [ "Home" ],
    "RightEnd":       [ "End" ],
    "Start":          [ "Ctrl+Home" ],
    "End":            [ "Ctrl+End" ],
    "LeftExtend":     [ "Shift+CursorLeft" ],
    "RightExtend":    [ "Shift+CursorRight" ],
    "UpExtend":       [ "Shift+CursorUp" ],
    "DownExtend":     [ "Shift+CursorDown" ],
    "PageUpExtend":   [ "Shift+PageUp" ],
    "PageDownExtend": [ "Shift+PageDown" ],
    "LeftStartExtend": [ "Shift+Home" ],
    "RightEndExtend":  [ "Shift+End" ],
    "StartExtend":    [ "Ctrl+Shift+Home" ],
    "EndExtend":      [ "Ctrl+Shift+End" ],
    "SelectAll":      [ "Ctrl+A" ],
    "Accept":         [ "Enter" ]
  },

  // ─── TreeView ─────────────────────────────────────────────────────────────
  "TreeView.DefaultKeyBindings": {
    "Up":                  [ "CursorUp" ],
    "UpExtend":            [ "Shift+CursorUp" ],
    "LineUpToFirstBranch": [ "Ctrl+CursorUp" ],
    "Down":                [ "CursorDown" ],
    "DownExtend":          [ "Shift+CursorDown" ],
    "LineDownToLastBranch":[ "Ctrl+CursorDown" ],
    "Expand":              [ "CursorRight" ],
    "ExpandAll":           [ "Ctrl+CursorRight" ],
    "Collapse":            [ "CursorLeft" ],
    "CollapseAll":         [ "Ctrl+CursorLeft" ],
    "PageUp":              [ "PageUp" ],
    "PageDown":            [ "PageDown" ],
    "PageUpExtend":        [ "Shift+PageUp" ],
    "PageDownExtend":      [ "Shift+PageDown" ],
    "Start":               [ "Home" ],
    "End":                 [ "End" ],
    "SelectAll":           [ "Ctrl+A" ],
    "Activate":            [ "Enter" ]
  },

  // ─── TabView ──────────────────────────────────────────────────────────────
  "TabView.DefaultKeyBindings": {
    "Left":      [ "CursorLeft" ],
    "Right":     [ "CursorRight" ],
    "Up":        [ "CursorUp" ],
    "Down":      [ "CursorDown" ],
    "LeftStart": [ "Home" ],
    "RightEnd":  [ "End" ],
    "PageUp":    [ "PageUp" ],
    "PageDown":  [ "PageDown" ]
  },

  // ─── HexView ──────────────────────────────────────────────────────────────
  "HexView.DefaultKeyBindings": {
    "Left":            [ "CursorLeft" ],
    "Right":           [ "CursorRight" ],
    "Up":              [ "CursorUp" ],
    "Down":            [ "CursorDown" ],
    "PageUp":          [ "PageUp" ],
    "PageDown":        [ "PageDown" ],
    "Start":           [ "Home" ],
    "End":             [ "End" ],
    "LeftExtend":      [ "Shift+CursorLeft" ],
    "RightExtend":     [ "Shift+CursorRight" ],
    "UpExtend":        [ "Shift+CursorUp" ],
    "DownExtend":      [ "Shift+CursorDown" ],
    "DeleteCharLeft":  [ "Backspace" ],
    "DeleteCharRight": [ "Delete" ],
    "ToggleOverwrite": [ "InsertChar" ]
  },

  // ─── NumericUpDown ────────────────────────────────────────────────────────
  "NumericUpDown.DefaultKeyBindings": {
    "Up":   [ "CursorUp" ],
    "Down": [ "CursorDown" ]
  },

  // ─── DropDownList ─────────────────────────────────────────────────────────
  "DropDownList.DefaultKeyBindings": {
    "Toggle": [ "F4" ],
    "Down":   [ "CursorDown" ]
  },

  // ─── ColorBar ─────────────────────────────────────────────────────────────
  "ColorBar.DefaultKeyBindings": {
    "Left":       [ "CursorLeft" ],
    "Right":      [ "CursorRight" ],
    "LeftExtend": [ "Shift+CursorLeft" ],
    "RightExtend":[ "Shift+CursorRight" ],
    "LeftStart":  [ "Home" ],
    "RightEnd":   [ "End" ]
  },

  // ─── LinearRange ──────────────────────────────────────────────────────────
  // Orientation-aware: horizontal uses Left/Right, vertical uses Up/Down
  "LinearRange.DefaultKeyBindings": {
    "Left":       [ "CursorLeft" ],
    "Right":      [ "CursorRight" ],
    "Up":         [ "CursorUp" ],
    "Down":       [ "CursorDown" ],
    "LeftExtend": [ "Ctrl+CursorLeft", "Ctrl+CursorUp" ],
    "RightExtend":[ "Ctrl+CursorRight", "Ctrl+CursorDown" ],
    "LeftStart":  [ "Home" ],
    "RightEnd":   [ "End" ],
    "Accept":     [ "Enter" ],
    "Activate":   [ "Space" ]
  },

  // ─── Views with no configurable bindings (rely on View base or are internal) ─
  // Button    — overrides Space/Enter → Accept via ReplaceCommands (inherits from View)
  // CheckBox  — MouseBindings only; inherits Space/Enter from View
  // ScrollBar — no keyboard bindings (mouse/programmatic only)
  // StatusBar — no keyboard bindings
  // MenuBar   — uses HotKeyBindings (single Key property: Application.QuitKey)
  // Shortcut  — dynamic: bound via App.Keyboard.KeyBindings.AddApp at runtime
  // FileDialog— binds internally on its _tableView (inherits TableView bindings)
}
```

---

## Implementation Plan

### Phase 1: Infrastructure

**New type:** `DefaultKeyBindingCollection` (or just use `Dictionary<string, string[]>`)

1. **Add to `SourceGenerationContext`**:
   ```csharp
   [JsonSerializable(typeof(Dictionary<string, string[]>))]
   ```

2. **Add a static helper** `KeyBindingConfigHelper` with one method:
   ```csharp
   internal static class KeyBindingConfigHelper
   {
       // Applies a DefaultKeyBindings dict to a view's KeyBindings,
       // replacing existing bindings for each command.
       // Then overlays the platform-specific overrides.
       internal static void Apply (
           View view,
           Dictionary<string, string[]>? baseBindings,
           Dictionary<string, string[]>? unixOverrides = null)
   ```
   - Iterates `baseBindings`, parses each key string with `Key.TryParse`
   - Calls `view.KeyBindings.Add(key, command)` (or `ReplaceCommands`)
   - If `RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || IsOSPlatform(OSPlatform.OSX)`, overlays `unixOverrides`
   - Commands validated against `Enum.TryParse<Command>()`; invalid names logged and skipped

3. **Add to `SettingsScope`** (to accept the new JSON keys without throwing):
   Each new `ClassName.DefaultKeyBindings` property needs to be registered as a `[ConfigurationProperty]` on a new static property somewhere, OR we add a bulk registration mechanism.

   **Recommended approach**: a single static class `ViewDefaultKeyBindings` with one property per view:
   ```csharp
   public static class ViewDefaultKeyBindings
   {
       [ConfigurationProperty(Scope = typeof(SettingsScope))]
       public static Dictionary<string, string[]>? TextField { get; set; }

       [ConfigurationProperty(Scope = typeof(SettingsScope))]
       public static Dictionary<string, string[]>? TextFieldUnix { get; set; }

       [ConfigurationProperty(Scope = typeof(SettingsScope))]
       public static Dictionary<string, string[]>? TextView { get; set; }
       // ... etc.
   }
   ```
   JSON key would then be `ViewDefaultKeyBindings.TextField` — or use `[JsonPropertyName("TextField.DefaultKeyBindings")]` override.

   **Alternative**: Add static properties directly on each View class (one per view), which gives JSON keys of `TextField.DefaultKeyBindings` naturally. This is cleaner but requires each view to have a static dependency on CM.

4. **Register and apply** in each view's `CreateCommandsAndBindings()`:
   ```csharp
   KeyBindingConfigHelper.Apply (
       this,
       ViewDefaultKeyBindings.TextField,
       ViewDefaultKeyBindings.TextFieldUnix);
   ```

### Phase 2: Migrate TextField (Proof of Concept)

- Move all `KeyBindings.Add` calls in `TextField.Commands.cs` to `config.json`
- Implement the Unix override for Undo/Redo/Paste/Cut
- Tests: verify bindings load from config, verify platform-specific overrides apply

### Phase 3: Migrate Remaining Views

Order: `TextView` → `ListView` → `TableView` → `TreeView` → `TabView` → others

### Phase 4: Fix Inconsistencies

- Standardize Undo (`Ctrl+Z` / `Ctrl+/` on Unix)
- Standardize Redo (`Ctrl+Shift+Z` everywhere; `Ctrl+R` on Unix as secondary)
- Remove `Ctrl+R` from TextField's `DeleteAll` (use only `Ctrl+Shift+D`)
- Remove `Ctrl+Y` as Redo from TextField (it's Paste/Yank in emacs; use `Ctrl+Shift+Z`)
- Add `Ctrl+W` as Cut alternative in TextField (emacs kill-region)

### Phase 5: Documentation

- Update `docfx/docs/keyboard.md`
- Add config.json JSON schema entries for new properties
- Update UICatalog keyboard scenario to show configurable bindings

---

## Open Questions

1. **Static property location**: One class (`ViewDefaultKeyBindings`) with all views, or one static property per view class? The former keeps CM deps isolated; the latter is more discoverable.

2. **Override semantics**: Does `DefaultKeyBindingsUnix` **replace** the command's binding(s) entirely, or **append** to them? Replace is simpler; append allows stacking (base keeps `Ctrl+Z`, Unix adds `Ctrl+/`). **Recommendation: append** so users can always still use the base key.

3. **Config schema `$schema`**: New properties need to be added to the JSON schema file at `https://gui-cs.github.io/Terminal.Gui/schemas/tui-config-schema.json`.

4. **`Ctrl+Slash` key name**: Verify `Key.TryParse("Ctrl+Slash")` works (or is it `Ctrl+/`?). Need to check `Key.ToString()` output for this key.

5. **`Ctrl+A` in TextView**: Current code binds `Ctrl+A` to `SelectAll`. Emacs users expect `Ctrl+A` = line start. For now: keep `SelectAll`; document the conflict.

6. **User override merging**: If a user's `~/.tui-config.json` has `"TextField.DefaultKeyBindings": { "Undo": ["F9"] }`, does it replace or merge with the built-in defaults? CM's current merge strategy: later scopes win. This means user config completely replaces the built-in dict for that command — which is the right behavior.

7. **New PR**: Original PR #4266 branch was renamed. Need to open new PR from `feature/cm-keybindings` → `v2_develop`.

