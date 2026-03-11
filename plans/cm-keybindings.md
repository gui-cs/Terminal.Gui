# Configurable Key Bindings via ConfigurationManager

> Branch: `feature/cm-keybindings` → `v2_develop` on gui-cs/Terminal.Gui
> Fixes: #3023, #3089
> Prerequisite: #4825 (Unify TextField/TextView Undo/Redo/Paste) — merge first

---

## Status

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Revert POC to clean baseline | ⬜ Pending |
| 2 | Add `Configuration` trace category + instrument CM | ⬜ Pending |
| 3 | CM infrastructure (JSON schema) | ⬜ Pending |
| 4 | `Bind` helper + `PlatformDetection` extension | ⬜ Pending |
| 5 | Application key bindings | ⬜ Pending |
| 6 | `View.ApplyKeyBindings()` instance method | ⬜ Pending |
| 7 | View base layer (`View.DefaultKeyBindings`) | ⬜ Pending |
| 8 | Migrate views (13 views, simplest→complex) | ⬜ Pending |
| 9 | Standardize popover activation keys | ⬜ Pending |
| 10 | config.json cleanup | ⬜ Pending |
| 11 | Documentation | ⬜ Pending |

---

# Part 1: Design

## Goals

1. Make all built-in key bindings configurable via `ConfigurationManager` (CM)
2. Support platform-specific key bindings (Windows / Linux / macOS)
3. Eliminate duplication — shared bindings defined once, applied to many views
4. Zero startup cost — C# code is source of truth; built-in config.json has no key binding entries
5. Backward compatible — existing `QuitKey`, `ArrangeKey`, etc. properties continue to work

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│  User config.json (overrides)                                │
│  "TextField.DefaultKeyBindings": { "Undo": { "all": [...] }}│
└──────────────┬───────────────────────────────────────────────┘
               │ CM loads & replaces static property
               ▼
┌──────────────────────────────────────────────────────────────┐
│  C# Static Properties (source of truth)                      │
│                                                              │
│  Application.DefaultKeyBindings  ← Layer 1 (app-level)      │
│  View.DefaultKeyBindings         ← Layer 2 (shared base)    │
│  TextField.DefaultKeyBindings    ← Layer 3 (view-specific)  │
└──────────────┬───────────────────────────────────────────────┘
               │ view.ApplyKeyBindings(layers...)
               ▼
┌──────────────────────────────────────────────────────────────┐
│  Platform Resolution                                         │
│  Windows: "all" + "windows"                                  │
│  Linux:   "all" + "linux"                                    │
│  macOS:   "all" + "macos"                                    │
└──────────────┬───────────────────────────────────────────────┘
               │ GetSupportedCommands() filter
               ▼
┌──────────────────────────────────────────────────────────────┐
│  view.KeyBindings.Add (key, command)                         │
│  Only for commands the view has registered handlers for      │
└──────────────────────────────────────────────────────────────┘
```

## Design Principles

1. **C# code is the source of truth** — All defaults live in static initializers. Works without CM enabled.
2. **config.json is for user overrides only** — Built-in config.json has ZERO key binding entries. This avoids startup cost and keeps things clean when CM is disabled (which is the default).
3. **Per-command platform attribution** — Each command specifies which platforms its keys apply to via `"all"`, `"windows"`, `"linux"`, `"macos"`.
4. **Shared base layer** — Common bindings (navigation, clipboard, editing) defined once on `View`, applied only to views that register handlers for those commands.
5. **Skip unhandled commands** — `ApplyKeyBindings()` checks `view.GetSupportedCommands()` and only binds commands the view actually handles. This is the key mechanism that makes the shared base layer work without views getting bindings they don't support.

---

## Platform Model

Four platform keys, **additive** within a command:

| Key | Matches |
|-----|---------|
| `"all"` | Every platform |
| `"windows"` | Windows only |
| `"linux"` | Linux only |
| `"macos"` | macOS only |

Resolution for current OS:
- **Windows**: collect keys from `"all"` + `"windows"`
- **Linux**: collect keys from `"all"` + `"linux"`
- **macOS**: collect keys from `"all"` + `"macos"`

Keys are **additive** within a command. For non-Windows bindings, specify both `linux` and `macos`:
```csharp
["DeleteCharRight"] = Bind.AllPlus ("Delete", nonWindows: ["Ctrl+D"])
// Windows gets: Delete
// Linux gets: Delete + Ctrl+D
// macOS gets: Delete + Ctrl+D

["Suspend"] = Bind.NonWindows ("Ctrl+Z")
// Windows gets: nothing
// Linux gets: Ctrl+Z
// macOS gets: Ctrl+Z
```

---

## C# Type

```csharp
// Outer key = Command name (string), Inner key = platform, Inner value = key strings
Dictionary<string, Dictionary<string, string[]>>
```

### Ergonomic Helper: `Bind` static class

New file: `Terminal.Gui/Configuration/Bind.cs`

```csharp
internal static class Bind
{
    /// <summary>All platforms get these keys.</summary>
    public static Dictionary<string, string[]> All (params string[] keys)
        => new () { { "all", keys } };

    /// <summary>All platforms get the base key; specific platforms get additional keys.</summary>
    public static Dictionary<string, string[]> AllPlus (
        string key,
        string[]? nonWindows = null,
        string[]? windows = null,
        string[]? linux = null,
        string[]? macos = null)
    {
        Dictionary<string, string[]> result = new () { { "all", [key] } };
        if (nonWindows is not null)
        {
            result ["linux"] = nonWindows;
            result ["macos"] = nonWindows;
        }

        if (windows is not null) result ["windows"] = windows;
        if (linux is not null) result ["linux"] = linux;
        if (macos is not null) result ["macos"] = macos;

        return result;
    }

    /// <summary>Linux + macOS get these keys. Convenience for specifying both.</summary>
    public static Dictionary<string, string[]> NonWindows (params string[] keys)
        => new () { { "linux", keys }, { "macos", keys } };

    /// <summary>Platform-specific keys only (no "all" entry).</summary>
    public static Dictionary<string, string[]> Platform (
        string[]? windows = null,
        string[]? linux = null,
        string[]? macos = null)
    {
        Dictionary<string, string[]> result = new ();
        if (windows is not null) result ["windows"] = windows;
        if (linux is not null) result ["linux"] = linux;
        if (macos is not null) result ["macos"] = macos;

        return result;
    }
}
```

---

## Layered Architecture

Key bindings are applied in three layers. Each layer is a `Dictionary<string, Dictionary<string, string[]>>` static property decorated with `[ConfigurationProperty]`.

### Layer 1: Application Key Bindings (`ApplicationKeyboard.DefaultKeyBindings`)

Global application-level bindings. Applied by `ApplicationKeyboard.AddKeyBindings()`.

```csharp
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, Dictionary<string, string[]>>? DefaultKeyBindings { get; set; } = new ()
{
    ["Quit"]             = Bind.All ("Esc"),
    ["Suspend"]          = Bind.NonWindows ("Ctrl+Z"),
    ["Arrange"]          = Bind.All ("Ctrl+F5"),
    ["NextTabStop"]      = Bind.All ("Tab"),
    ["PreviousTabStop"]  = Bind.All ("Shift+Tab"),
    ["NextTabGroup"]     = Bind.All ("F6"),
    ["PreviousTabGroup"] = Bind.All ("Shift+F6"),
    ["Refresh"]          = Bind.All ("F5"),
};
```

Existing scalar properties (`QuitKey`, `ArrangeKey`, etc.) become convenience accessors that read from the dict for backward compatibility.

### Layer 2: View Base Key Bindings (`View.DefaultKeyBindings`)

Common bindings shared across many views. Only applied to views that have registered command handlers for those commands (via `GetSupportedCommands()` filtering).

```csharp
// View.Keyboard.cs
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, Dictionary<string, string[]>>? DefaultKeyBindings { get; set; } = new ()
{
    // Navigation
    ["Left"]       = Bind.All ("CursorLeft"),
    ["Right"]      = Bind.All ("CursorRight"),
    ["Up"]         = Bind.All ("CursorUp"),
    ["Down"]       = Bind.All ("CursorDown"),
    ["PageUp"]     = Bind.All ("PageUp"),
    ["PageDown"]   = Bind.All ("PageDown"),
    ["LeftStart"]  = Bind.All ("Home"),
    ["RightEnd"]   = Bind.All ("End"),
    ["Start"]      = Bind.All ("Ctrl+Home"),
    ["End"]        = Bind.All ("Ctrl+End"),

    // Selection-extend
    ["LeftExtend"]      = Bind.All ("Shift+CursorLeft"),
    ["RightExtend"]     = Bind.All ("Shift+CursorRight"),
    ["UpExtend"]        = Bind.All ("Shift+CursorUp"),
    ["DownExtend"]      = Bind.All ("Shift+CursorDown"),
    ["PageUpExtend"]    = Bind.All ("Shift+PageUp"),
    ["PageDownExtend"]  = Bind.All ("Shift+PageDown"),
    ["LeftStartExtend"] = Bind.All ("Shift+Home"),
    ["RightEndExtend"]  = Bind.All ("Shift+End"),
    ["StartExtend"]     = Bind.All ("Ctrl+Shift+Home"),
    ["EndExtend"]       = Bind.All ("Ctrl+Shift+End"),

    // Clipboard
    ["Copy"]  = Bind.All ("Ctrl+C"),
    ["Cut"]   = Bind.All ("Ctrl+X"),
    ["Paste"] = Bind.All ("Ctrl+V"),

    // Editing
    ["Undo"]            = Bind.Platform (windows: ["Ctrl+Z"], linux: ["Ctrl+/"], macos: ["Ctrl+/"]),
    ["Redo"]            = Bind.Platform (windows: ["Ctrl+Y"], linux: ["Ctrl+Shift+Z"], macos: ["Ctrl+Shift+Z"]),
    ["SelectAll"]       = Bind.All ("Ctrl+A"),
    ["DeleteCharLeft"]  = Bind.All ("Backspace"),
    ["DeleteCharRight"] = Bind.AllPlus ("Delete", nonWindows: ["Ctrl+D"]),
};
```

**Key design point**: ListView doesn't have `AddCommand(Command.Left, ...)` so even though `Left` is in the base dict, it won't be bound on ListView. The `GetSupportedCommands()` check filters it out automatically.

### Layer 3: View-Specific Key Bindings (`ViewType.DefaultKeyBindings`)

Each view defines ONLY its unique/additional bindings. These **extend** the base layer (never replace it).

Example — TextField (text-editing specific only):
```csharp
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, Dictionary<string, string[]>>? DefaultKeyBindings { get; set; } = new ()
{
    // Emacs shortcuts (extend base CursorLeft/Right)
    ["Left"]             = Bind.NonWindows ("Ctrl+B"),
    ["Right"]            = Bind.NonWindows ("Ctrl+F"),
    ["RightEnd"]         = Bind.All ("Ctrl+E"),
    ["WordLeft"]         = Bind.All ("Ctrl+CursorLeft", "Ctrl+CursorUp"),
    ["WordRight"]        = Bind.All ("Ctrl+CursorRight", "Ctrl+CursorDown"),
    ["WordLeftExtend"]   = Bind.All ("Ctrl+Shift+CursorLeft", "Ctrl+Shift+CursorUp"),
    ["WordRightExtend"]  = Bind.All ("Ctrl+Shift+CursorRight", "Ctrl+Shift+CursorDown"),
    ["CutToEndOfLine"]   = Bind.All ("Ctrl+K"),
    ["CutToStartOfLine"] = Bind.All ("Ctrl+Shift+K"),
    ["KillWordRight"]    = Bind.All ("Ctrl+Delete"),
    ["KillWordLeft"]     = Bind.All ("Ctrl+Backspace"),
    ["ToggleOverwrite"]  = Bind.All ("Insert"),
    ["DeleteAll"]        = Bind.All ("Ctrl+R", "Ctrl+Shift+D"),
};
```

Example — ListView (list-specific only):
```csharp
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, Dictionary<string, string[]>>? DefaultKeyBindings { get; set; } = new ()
{
    // Emacs nav shortcuts (extend base CursorUp/Down)
    ["Up"]   = Bind.NonWindows ("Ctrl+P"),
    ["Down"] = Bind.NonWindows ("Ctrl+N"),
};
// NOTE: Multi-command bindings (Shift+Space → Activate+Down) stay as direct KeyBindings.Add()
```

### Layer Application Order

In each view's setup method:

```csharp
// 1. Register command handlers (AddCommand calls)
AddCommand (Command.Left, ctx => HandleLeft (ctx));
// ...

// 2. Apply layered key bindings (base + view-specific)
ApplyKeyBindings (View.DefaultKeyBindings, TextField.DefaultKeyBindings);

// 3. Post-processing (remove base View bindings like Space/Enter if needed)
KeyBindings.Remove (Key.Space);
```

---

## `View.ApplyKeyBindings()` — The Apply Mechanism

`ApplyKeyBindings` is an **instance method on `View`**. This is the natural home — it needs `this.GetSupportedCommands()` and `this.KeyBindings`, making key bindings a first-class part of `View`.

Platform detection uses the existing `PlatformDetection` class (in `Terminal.Gui.Drivers`), extended with a `GetCurrentPlatformName()` method that returns `"windows"`, `"linux"`, or `"macos"`.

```csharp
// In View.Keyboard.cs — instance method
protected void ApplyKeyBindings (params Dictionary<string, Dictionary<string, string[]>>?[] layers)
{
    HashSet<Command> supported = new (GetSupportedCommands ());

    foreach (Dictionary<string, Dictionary<string, string[]>>? layer in layers)
    {
        if (layer is null) continue;

        foreach ((string commandName, Dictionary<string, string[]> platformKeys) in layer)
        {
            if (!Enum.TryParse<Command> (commandName, out Command command)) continue;
            if (!supported.Contains (command)) continue;

            foreach (string keyString in ResolveKeysForCurrentPlatform (platformKeys))
            {
                if (!Key.TryParse (keyString, out Key? key)) continue;
                if (KeyBindings.TryGet (key, out _)) continue; // skip already-bound

                KeyBindings.Add (key, command);
            }
        }
    }
}

/// <summary>Resolves platform-specific key strings for the current OS.</summary>
private static IEnumerable<string> ResolveKeysForCurrentPlatform (Dictionary<string, string[]> platformKeys)
{
    if (platformKeys.TryGetValue ("all", out string[]? allKeys))
        foreach (string k in allKeys) yield return k;

    string platform = PlatformDetection.GetCurrentPlatformName ();

    if (platformKeys.TryGetValue (platform, out string[]? platKeys))
        foreach (string k in platKeys) yield return k;
}
```

### PlatformDetection Extension

Add to existing `PlatformDetection` class:

```csharp
/// <summary>Returns the platform name used for key binding resolution.</summary>
public static string GetCurrentPlatformName ()
{
    if (IsWindows ()) return "windows";
    if (IsMac ()) return "macos";

    return "linux";
}
```

### View Setup Calls

```csharp
// In TextField.Commands.cs:
ApplyKeyBindings (View.DefaultKeyBindings, TextField.DefaultKeyBindings);

// In ListView.Commands.cs:
ApplyKeyBindings (View.DefaultKeyBindings, ListView.DefaultKeyBindings);
```

---

## config.json Strategy

### Built-in config.json: ZERO key binding entries

All defaults are in C# static initializers. This means:
- Zero startup cost for key binding parsing
- Works identically with CM disabled (the default)
- C# code is self-documenting

### User config.json: Override format

```jsonc
{
    // Override TextField undo to use Ctrl+Z everywhere
    "TextField.DefaultKeyBindings": {
        "Undo": { "all": ["Ctrl+Z"] }
    },

    // Add a custom Application quit key on Linux and macOS
    "Application.DefaultKeyBindings": {
        "Quit": { "all": ["Esc"], "linux": ["Ctrl+Q"], "macos": ["Ctrl+Q"] }
    }
}
```

**Override semantics**: CM replaces the entire static property value. A user override for `TextField.DefaultKeyBindings` replaces ALL of TextField's view-specific bindings. `View.DefaultKeyBindings` is unaffected (separate property).

---

## SourceGenerationContext Registration

```csharp
[JsonSerializable (typeof (Dictionary<string, Dictionary<string, string[]>>))]
[JsonSerializable (typeof (Dictionary<string, string[]>))]
[JsonSerializable (typeof (string[]))]
```

---

## Known Constraint: Open Generic Types

`[ConfigurationProperty]` CANNOT be placed on open generic types (`TreeView<T>`, `NumericUpDown<T>`, `LinearRange<T>`). CM's `ConfigProperty.Initialize()` calls `PropertyInfo.GetValue(null)` which throws on open generics. Solution: place the `[ConfigurationProperty]` static properties on the non-generic base class (e.g., `TreeView`, `NumericUpDown`, `LinearRange`) and reference them from the generic class's setup code.

---

## Prerequisite: Undo/Redo/Paste Unification (#4825)

TextField and TextView currently have incompatible key bindings for Undo/Redo/Paste/DeleteAll. This is tracked as a separate issue (#4825) and must be merged before this PR's implementation begins. After #4825, both views will use:

| Command | All Platforms | Non-Windows (additional) |
|---------|--------------|--------------------------|
| Paste | Ctrl+V | — |
| Undo | Ctrl+Z | Ctrl+/ |
| Redo | Ctrl+Y | Ctrl+Shift+Z |
| DeleteAll | Ctrl+Shift+D | — |

---

## Implementation Phases (Test-First, CI-Gated)

Each phase follows this workflow:
1. **Write tests** for the phase
2. **Implement** until all tests pass locally (both test projects)
3. **Commit and push** to the PR branch
4. **Wait for CI** — all GitHub Actions runners must pass (~10 min). Use `gh run list` / `gh run watch` to monitor. **Do NOT proceed to the next phase until CI is green.**
5. **Update deepdive docs** (`keyboard.md`, `config.md`, etc.) if the phase affects documented behavior
6. **Update this plan's Status table** to mark the phase ✅ Done

**Debugging guidance:** When tests fail or behavior is unexpected, **use `Trace.Configuration(...)` calls and log output** to diagnose the problem — do NOT try to reason over the code or rely on memory. Add temporary trace calls, run the failing test in Debug, read the trace output, then fix. Remove temporary traces after diagnosis. Note: `Trace` methods are `[Conditional("DEBUG")]` so they are unavailable in Release builds — never assert on or depend on trace output in unit tests.

### Phase 1: Revert POC

1. Revert all POC changes to v2_develop baseline
2. Confirm all existing tests pass
3. Commit: "Revert POC key bindings implementation"

### Phase 2: Add `Configuration` Trace Category + Instrument CM (→ separate PR, Fixes #4826)

**Modified:** `Terminal.Gui/App/Tracing/TraceCategory.cs`, `Terminal.Gui/App/Tracing/Trace.cs`
**Modified:** Key files in `Terminal.Gui/Configuration/` (ConfigurationManager, SourcesManager, etc.)

Add a `Configuration` trace category and `Trace.Configuration(...)` method, then instrument the ConfigurationManager so that all subsequent phases can use trace output to diagnose issues.

**TraceCategory.cs** — add:
```csharp
Configuration = 32,
```
Update `All` to include `Configuration`.

**Trace.cs** — add:
```csharp
[Conditional ("DEBUG")]
public static void Configuration (string? id, string phase, string? message = null, [CallerMemberName] string method = "")
{
    if (!EnabledCategories.HasFlag (TraceCategory.Configuration))
    {
        return;
    }

    Backend.Log (new TraceEntry (TraceCategory.Configuration, id, phase, method, message, DateTime.UtcNow));
}
```

**Instrument CM** — add `Trace.Configuration(...)` calls to key paths:
- `ConfigurationManager.Apply()` — log start/end, property count
- `SourcesManager.LoadSources()` — log each source loaded
- Property discovery — log each `[ConfigurationProperty]` found
- Property assignment — log when a property value is set from config
- Error paths — log when JSON deserialization fails, when a property is skipped

Tests (`Tests/UnitTestsParallelizable/App/Tracing/TraceConfigurationTests.cs`):

Note: All trace methods are `[Conditional("DEBUG")]` — they compile to no-ops in Release. Tests must NOT assert on trace output. Instead, test that the category and method exist and that enabling/disabling the category works.

| # | Test | Validates |
|---|------|-----------|
| 1 | `TraceCategory_Configuration_HasExpectedValue` | `TraceCategory.Configuration == 32` |
| 2 | `TraceCategory_All_IncludesConfiguration` | `TraceCategory.All.HasFlag(TraceCategory.Configuration)` |
| 3 | `Configuration_Category_CanBeEnabled` | `Trace.EnabledCategories = TraceCategory.Configuration` doesn't throw (DEBUG-only scope test via `PushScope`) |

Commit: "Add Configuration trace category and instrument ConfigurationManager"

### Phase 3: CM Infrastructure (JSON Schema Support)

**Modified:** `Terminal.Gui/Configuration/SourceGenerationContext.cs`

Register the new dict-of-dict type for JSON serialization:
```csharp
[JsonSerializable (typeof (Dictionary<string, Dictionary<string, string[]>>))]
[JsonSerializable (typeof (Dictionary<string, string[]>))]
[JsonSerializable (typeof (string[]))]
```

Tests first (`Tests/UnitTestsParallelizable/Configuration/KeyBindingSchemaTests.cs`):

| # | Test | Validates |
|---|------|-----------|
| 1 | `KeyBindingDict_RoundTrips_ThroughJson` | Serialize → deserialize a `Dictionary<string, Dictionary<string, string[]>>` preserves all data |
| 2 | `KeyBindingDict_Deserializes_FromUserConfigFormat` | Parse `{ "Left": { "all": ["CursorLeft"], "linux": ["Ctrl+B"] } }` correctly |
| 3 | `KeyBindingDict_EmptyDict_RoundTrips` | Empty dict serializes/deserializes without error |
| 4 | `KeyBindingDict_CM_CanDiscover_DictProperty` | A `[ConfigurationProperty]` of this type is found by CM's property discovery |

Commit: "Add JSON schema support for key binding dictionaries"

### Phase 3: `Bind` Helper + `PlatformDetection` Extension

**New file:** `Terminal.Gui/Configuration/Bind.cs`
**Modified:** `Terminal.Gui/Drivers/PlatformDetection.cs` — add `GetCurrentPlatformName()`

Tests first (`Tests/UnitTestsParallelizable/Configuration/BindTests.cs`):

| # | Test | Validates |
|---|------|-----------|
| 1 | `Bind_All_SingleKey_ReturnsAllEntry` | `Bind.All("CursorLeft")` → `{ "all": ["CursorLeft"] }` |
| 2 | `Bind_All_MultipleKeys_ReturnsAllEntry` | `Bind.All("Home", "Ctrl+Home")` → `{ "all": ["Home", "Ctrl+Home"] }` |
| 3 | `Bind_AllPlus_NonWindowsKeys_ReturnsBothLinuxAndMacos` | `Bind.AllPlus("Delete", nonWindows: ["Ctrl+D"])` → `{ "all": ["Delete"], "linux": ["Ctrl+D"], "macos": ["Ctrl+D"] }` |
| 4 | `Bind_AllPlus_WindowsKeys_ReturnsAllAndWindows` | `Bind.AllPlus("X", windows: ["Ctrl+X"])` → `{ "all": ["X"], "windows": ["Ctrl+X"] }` |
| 5 | `Bind_AllPlus_NullPlatforms_OmitsNullEntries` | Null platforms don't create entries |
| 6 | `Bind_NonWindows_ReturnsBothLinuxAndMacos` | `Bind.NonWindows("Ctrl+Z")` → `{ "linux": ["Ctrl+Z"], "macos": ["Ctrl+Z"] }` — no "all" |
| 7 | `Bind_Platform_LinuxOnly` | `Bind.Platform(linux: ["Ctrl+Z"])` → `{ "linux": ["Ctrl+Z"] }` — no "all", no "macos" |
| 8 | `Bind_Platform_WindowsAndMacos` | Both present, no "all" |
| 9 | `Bind_Platform_AllNulls_ReturnsEmpty` | Empty dict when all null |

Tests for `PlatformDetection.GetCurrentPlatformName()`:

| # | Test | Validates |
|---|------|-----------|
| 10 | `GetCurrentPlatformName_ReturnsValidName` | Returns one of `"windows"`, `"linux"`, `"macos"` |

Commit: "Add Bind helper and PlatformDetection.GetCurrentPlatformName()"

### Phase 4: Application Key Bindings

**Modified:** `Terminal.Gui/App/Keyboard/ApplicationKeyboard.cs`

Tests (`Tests/UnitTestsParallelizable/App/ApplicationDefaultKeyBindingsTests.cs`):

| # | Test | Validates |
|---|------|-----------|
| 1 | `Application_DefaultKeyBindings_IsNotNull` | Static property initialized |
| 2 | `Application_DefaultKeyBindings_ContainsQuit` | `"Quit"` present with `"Esc"` on all |
| 3 | `Application_DefaultKeyBindings_SuspendIsNonWindows` | `"Suspend"` has `linux: ["Ctrl+Z"]` and `macos: ["Ctrl+Z"]`, no `"all"` |
| 4 | `Application_DefaultKeyBindings_AllKeyStringsParseable` | Every key string passes `Key.TryParse` |
| 5 | `Application_DefaultKeyBindings_HasConfigurationPropertyAttribute` | Decorated with `[ConfigurationProperty]` |
| 6 | `QuitKey_Getter_ReadsFromDict` | `QuitKey` property returns key from dict (backward compat wrapper) |

Commit: "Add Application.DefaultKeyBindings with platform support"

### Phase 5: `View.ApplyKeyBindings()` Instance Method

**Modified:** `Terminal.Gui/ViewBase/View.Keyboard.cs`

Tests first (`Tests/UnitTestsParallelizable/ViewBase/ApplyKeyBindingsTests.cs`):

**ResolveKeysForCurrentPlatform tests** (private method — test indirectly via ApplyKeyBindings):

| # | Test | Validates |
|---|------|-----------|
| 1 | `ApplyKeyBindings_AllPlatform_BindsKey` | `{ "Left": { "all": ["CursorLeft"] } }` binds CursorLeft→Left |
| 2 | `ApplyKeyBindings_CurrentPlatformOnly_BindsOnThisPlatform` | Platform-specific entry binds on matching OS |
| 3 | `ApplyKeyBindings_OtherPlatformOnly_DoesNotBind` | Entry for different platform doesn't bind |
| 4 | `ApplyKeyBindings_AllPlusPlatform_Additive` | Both `"all"` and platform-specific keys bind |
| 5 | `ApplyKeyBindings_BindsSupportedCommand` | View with command handler gets binding |
| 6 | `ApplyKeyBindings_SkipsUnsupportedCommand` | View without handler does NOT get binding |
| 7 | `ApplyKeyBindings_MultipleLayers_Additive` | Base layer + view layer both contribute bindings |
| 8 | `ApplyKeyBindings_NullLayer_Skipped` | `ApplyKeyBindings(null, dict)` works without NullReferenceException |
| 9 | `ApplyKeyBindings_InvalidCommandName_Skipped` | `{ "NotACommand": Bind.All("X") }` doesn't throw, just skips |
| 10 | `ApplyKeyBindings_InvalidKeyString_Skipped` | `{ "Left": Bind.All("???invalid???") }` doesn't throw, just skips |
| 11 | `ApplyKeyBindings_AlreadyBoundKey_NotOverwritten` | If CursorLeft already bound, ApplyKeyBindings doesn't overwrite it |
| 12 | `ApplyKeyBindings_MultipleKeysPerCommand` | `{ "Left": Bind.All("CursorLeft", "Ctrl+B") }` binds both keys |
| 13 | `ApplyKeyBindings_EmptyDict_NoOp` | Empty dictionary doesn't throw or change bindings |
| 14 | `ApplyKeyBindings_ViewSpecificExtendsBase_SameCommand` | Base has `Left→CursorLeft`, view has `Left→Ctrl+B` — both keys get bound |

Note: Tests use `View` + `CommandNotBound` event — no dependency on `./Views`.

Commit: "Add View.ApplyKeyBindings() with platform resolution and command filtering"

### Phase 6: View Base Layer (`View.DefaultKeyBindings`)

**Modified:** `Terminal.Gui/ViewBase/View.Keyboard.cs`

Tests (`Tests/UnitTestsParallelizable/ViewBase/ViewDefaultKeyBindingsTests.cs`):

| # | Test | Validates |
|---|------|-----------|
| 1 | `View_DefaultKeyBindings_IsNotNull` | Static property is initialized |
| 2 | `View_DefaultKeyBindings_ContainsNavigationCommands` | Left, Right, Up, Down, PageUp, PageDown, Home, End, Start, End all present |
| 3 | `View_DefaultKeyBindings_ContainsClipboardCommands` | Copy, Cut, Paste present |
| 4 | `View_DefaultKeyBindings_ContainsEditingCommands` | Undo, Redo, SelectAll, DeleteCharLeft, DeleteCharRight present |
| 5 | `View_DefaultKeyBindings_AllKeyStringsParseable` | Every key string in every entry passes `Key.TryParse` |
| 6 | `View_DefaultKeyBindings_AllCommandNamesParseable` | Every command name in the dict parses to a `Command` enum |
| 7 | `View_DefaultKeyBindings_HasConfigurationPropertyAttribute` | `[ConfigurationProperty]` is applied |
| 8 | `View_DefaultKeyBindings_NoBindingsApplied_WhenNoCommandHandlers` | Plain `View` with no AddCommand beyond base — ApplyKeyBindings doesn't crash |

At this point, `View.DefaultKeyBindings` is defined but NOT yet wired into any view's setup. Wiring happens in Phase 7.

Commit: "Add View.DefaultKeyBindings shared base layer"

### Phase 7: Migrate Views (One at a Time)

For each view, follow this sub-pattern:
1. Write tests that verify the view's CURRENT key bindings (snapshot the expected state)
2. Switch the view from direct `KeyBindings.Add` to `ApplyKeyBindings(View.DefaultKeyBindings, ViewType.DefaultKeyBindings)`
3. Verify the snapshot tests still pass (no behavior change)
4. Add tests for the static `DefaultKeyBindings` property (not null, parseable, CM discoverable)

**View migration order** (simplest → most complex):

#### 7a: TabView
- Simple: 8 navigation commands, no platform differences
- View-specific dict: empty or near-empty (all covered by base)
- Tests: verify CursorLeft→Left, CursorRight→Right, etc. still work

#### 7b: HexView
- 15 bindings, unique commands: `StartOfPage`, `EndOfPage`, `Insert`
- Removes Space and Enter after Apply
- Tests: verify all 15 keys still bound correctly

#### 7c: DropDownList
- Only 2 unique bindings: `F4→Toggle`, `Alt+CursorDown→Toggle`
- Inherits from TextField — uses `new` keyword for `DefaultKeyBindings`
- Tests: verify Toggle bindings, verify no TextField bindings leak through

#### 7d: NumericUpDown
- 2 bindings: CursorUp→Up, CursorDown→Down (already in base)
- Generic type constraint: `[ConfigurationProperty]` on non-generic `NumericUpDown`
- Tests: verify generic class references non-generic property

#### 7e: LinearRange
- 2 unique + orientation-dependent bindings (stay as direct `KeyBindings.Add`)
- Generic type constraint: same pattern as NumericUpDown
- Tests: verify Home→LeftStart, End→RightEnd + orientation bindings

#### 7f: TreeView
- 17 bindings, unique commands: `Expand`, `ExpandAll`, `Collapse`, `CollapseAll`, `LineUpToFirstBranch`, `LineDownToLastBranch`
- Generic type constraint: same pattern
- Instance-dependent `ObjectActivationKey` stays as direct KeyBindings.Add
- Tests: verify unique tree commands + shared nav commands from base

#### 7g: ListView
- 12 bindings + multi-command `Shift+Space→Activate+Down` (stays direct)
- Emacs nav shortcuts: `Ctrl+P→Up`, `Ctrl+N→Down` (linux platform-specific)
- Tests: verify nav from base, emacs from view-specific, multi-command direct

#### 7h: TableView
- 21 bindings — most overlap with base
- Instance-dependent `CellActivationKey` stays direct
- Tests: verify all nav/extend from base, SelectAll from base

#### 7i: TextField
- 25 bindings — many from base, many unique
- Removes Space after Apply
- Context menu binding stays direct
- Tests: verify base nav/clipboard/editing, verify unique word-nav/cut-line/etc.

#### 7j: TextView
- 42 bindings — most complex view
- Dynamic Enter binding (Multiline flag), Tab/Shift+Tab bindings
- Emacs shortcuts, unique commands: `ToggleExtend`, `Open`, `NewLine`
- Tests: verify base bindings, verify unique bindings, verify dynamic Enter behavior

#### 7k: TextValidateField (+ DateEditor, TimeEditor)
- 6 bindings: Home→LeftStart, End→RightEnd, Delete→DeleteCharRight, Backspace→DeleteCharLeft, CursorLeft→Left, CursorRight→Right
- All 6 overlap with base layer — view-specific dict may be empty
- DateEditor and TimeEditor inherit from TextValidateField (add zero bindings)
- Tests: verify deletion keys consistent with TextField/TextView after Phase 6 unification

#### 7l: MenuBar
- Activation key F10 (standardized in Phase 8), plus CursorLeft→Left, CursorRight→Right
- HotKey binding stays direct (dynamic `Key` property)
- Tests: verify F10 activates, nav keys work

#### 7m: PopoverMenu
- Context menu key Shift+F10, plus CursorLeft→Left, CursorRight→Right
- Tests: verify Shift+F10 activates context menu

Each sub-phase: commit after tests pass.

### Phase 8: Standardize Popover Activation Keys

Currently inconsistent:
- **MenuBar**: `F9` (hardcoded, non-standard)
- **PopoverMenu (context menu)**: `Shift+F10` (correct for Windows/Linux)
- **DropDownList**: `F4`, `Alt+CursorDown` (F4 is Windows standard; Alt+Down is universal)

**Platform standards** (from research):

| Action | Windows | Linux (GTK/Qt) | macOS |
|--------|---------|-----------------|-------|
| Activate menu bar | `F10` | `F10` | `Ctrl+F2` |
| Context menu | `Shift+F10` or Menu key | `Shift+F10` or Menu key | (no universal; some apps use `Ctrl+Return`) |
| Open dropdown | `Alt+Down`, `F4` | `Alt+Down` | `Space` or `Down` |

**Changes:**
- **MenuBar.DefaultKey**: `F9` → `F10` on all platforms (standard for Windows/Linux; macOS can override to `Ctrl+F2`)
- **PopoverMenu.DefaultKey**: Keep `Shift+F10` on all (already correct)
- **DropDownList**: Keep `F4` + `Alt+CursorDown` (already correct)

These keys should move into the `DefaultKeyBindings` dict pattern so they're configurable:

```csharp
// MenuBar
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, Dictionary<string, string[]>>? DefaultKeyBindings { get; set; } = new ()
{
    ["HotKey"] = Bind.All ("F10"),
};

// PopoverMenu
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, Dictionary<string, string[]>>? DefaultKeyBindings { get; set; } = new ()
{
    ["Context"] = Bind.All ("Shift+F10"),
};
```

Tests:

| # | Test | Validates |
|---|------|-----------|
| 1 | `MenuBar_DefaultKey_IsF10` | MenuBar activation key is F10 (changed from F9) |
| 2 | `PopoverMenu_DefaultKey_IsShiftF10` | Context menu key is Shift+F10 |
| 3 | `DropDownList_Toggle_F4_And_AltDown` | Dropdown opens with F4 and Alt+Down |
| 4 | `MenuBar_DefaultKeyBindings_HasConfigurationProperty` | CM can discover/override |
| 5 | `PopoverMenu_DefaultKeyBindings_HasConfigurationProperty` | CM can discover/override |

Commit: "Standardize popover activation keys (MenuBar F9→F10)"

### Phase 9: config.json Cleanup

- Remove ALL `DefaultKeyBindings` / `DefaultKeyBindingsUnix` entries from built-in `config.json`
- Verify CM still discovers the properties (they're `[ConfigurationProperty]` decorated)
- Verify user override format works by writing a test that sets a dict value and confirms ApplyKeyBindings uses it

Commit: "Remove key binding entries from built-in config.json"

### Phase 10: Documentation

- Update `docfx/docs/keyboard.md` — document platform-aware key bindings, layered architecture, how to customize
- Update `docfx/docs/config.md` — document override format for key bindings
- Verify accuracy of all code examples

Commit: "Update keyboard and config documentation"

---

## Views NOT Being Migrated (Use Direct KeyBindings.Add)

These views have simple or special-case bindings that don't benefit from the config pattern:

| View | Reason |
|------|--------|
| CharMap | Simple 8-key nav, unlikely to customize |
| GraphView | 4 scroll commands, unique to GraphView |
| ColorPicker16 | Simple 4-key nav |
| ColorBar | 6-key nav + extend |
| Shortcut | Dynamic key from property |
| Arranger | Temporary mode bindings |

These can be migrated in follow-up PRs if needed.

---

## Current Binding Inventory (Reference)

### Shared Navigation (base layer candidates)

| Command | Key(s) | Views Using |
|---------|--------|-------------|
| Left | CursorLeft | TextField, TextView, TableView, HexView, TabView |
| Right | CursorRight | TextField, TextView, TableView, HexView, TabView |
| Up | CursorUp | TextView, ListView, TableView, HexView, TabView, TreeView |
| Down | CursorDown | TextView, ListView, TableView, HexView, TabView, TreeView |
| PageUp | PageUp | TextView, ListView, TableView, HexView, TabView, TreeView |
| PageDown | PageDown | TextView, ListView, TableView, HexView, TabView, TreeView |
| LeftStart | Home | TextField, TextView, TableView, TabView |
| RightEnd | End | TextField, TextView, TableView, TabView |
| Start | Ctrl+Home | TextView, TableView |
| End | Ctrl+End | TextView, TableView |

### Shared Selection-Extend

| Command | Key(s) | Views Using |
|---------|--------|-------------|
| LeftExtend | Shift+CursorLeft | TextField, TextView, TableView |
| RightExtend | Shift+CursorRight | TextField, TextView, TableView |
| UpExtend | Shift+CursorUp | TextView, ListView, TableView, TreeView |
| DownExtend | Shift+CursorDown | TextView, ListView, TableView, TreeView |
| PageUpExtend | Shift+PageUp | TextView, ListView, TableView, TreeView |
| PageDownExtend | Shift+PageDown | TextView, ListView, TableView, TreeView |
| LeftStartExtend | Shift+Home | TextField, TextView, TableView |
| RightEndExtend | Shift+End | TextField, TextView, TableView |
| StartExtend | Ctrl+Shift+Home | TextField, TextView, TableView |
| EndExtend | Ctrl+Shift+End | TextField, TextView, TableView |

### Shared Clipboard/Editing

| Command | Key(s) | Views Using |
|---------|--------|-------------|
| Copy | Ctrl+C | TextField, TextView |
| Cut | Ctrl+X | TextField, TextView |
| Paste | Ctrl+V | TextField (TextView uses Ctrl+Y!) |
| Undo | Ctrl+Z | TextField, TextView |
| Redo | Ctrl+Y (TF) / Ctrl+R (TV) | TextField, TextView (INCONSISTENT) |
| SelectAll | Ctrl+A | TextField, TextView, ListView, TableView, TreeView |
| DeleteCharLeft | Backspace | TextField, TextView, HexView |
| DeleteCharRight | Delete | TextField, TextView, HexView |

### View-Specific Unique Commands

**TextField only:** WordLeft, WordRight, WordLeftExtend, WordRightExtend, CutToEndOfLine, CutToStartOfLine, KillWordRight, KillWordLeft, ToggleOverwrite, DeleteAll

**TextView only:** ToggleExtend, Open, NewLine, NextTabStop (Tab), PreviousTabStop (Shift+Tab), also Cut uses Ctrl+W additionally

**ListView only:** Multi-command `Shift+Space→Activate+Down`

**TreeView only:** Expand, ExpandAll, Collapse, CollapseAll, LineUpToFirstBranch, LineDownToLastBranch

**HexView only:** StartOfPage, EndOfPage, Insert

**DropDownList only:** Toggle (F4, Alt+CursorDown)