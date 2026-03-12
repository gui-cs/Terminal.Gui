# Configurable Key Bindings via ConfigurationManager

> Branch: `feature/cm-keybindings` → `v2_develop` on gui-cs/Terminal.Gui
> Fixes: #3023, #3089
> Prerequisite: #4825 (Unify TextField/TextView Undo/Redo/Paste) — ✅ merged as PR #4828

---

## Status

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | Prerequisite: Unify TextField/TextView keybindings (#4828) | ✅ Merged |
| 1 | Revert POC to clean baseline | ✅ Done |
| 1b | Change DeleteAll from Ctrl+Shift+D → Ctrl+Shift+Delete | ✅ Done |
| 2 | Add `Configuration` trace category + instrument CM | ✅ Done (PR #4827) |
| 3 | CM infrastructure (JSON schema) | ✅ Done |
| 4 | `Bind` helper + `PlatformDetection` extension | ✅ Done |
| 5 | Application key bindings | ✅ Done |
| 6 | `View.ApplyKeyBindings()` instance method | ✅ Done |
| 7 | View base layer (`View.DefaultKeyBindings`) | ✅ Done |
| 8 | Migrate views (13 views, simplest→complex) | ✅ Done |
| 9 | Standardize popover activation keys (MenuBar F9→F10) | ✅ Done |
| 10 | config.json cleanup | ✅ Done (already clean) |
| 11 | Documentation | ✅ Done |
| 11b | Move `DefaultKeyBindings` from `ApplicationKeyboard` → `Application`; create `Examples/Config/` | ✅ Done |
| 12a | **Add `TuiPlatform` enum; type-safe platform resolution** (Breaking) | ✅ Done |
| 12b | **Register `Dictionary<Command,PlatformKeyBinding>` with STJ source gen** | ✅ Done |
| 12c | **Change all `DefaultKeyBindings` to `Dictionary<Command, PlatformKeyBinding>`** (Breaking) | ✅ Done |
| 12d | **Remove single-key properties; wire `AddKeyBindings()` to `DefaultKeyBindings`** (Breaking) | 🔲 TODO |
| 12e | **Update tests** | 🔲 TODO |
| 13a | **Move `Bind` + `PlatformKeyBinding` from `Configuration/` to `Input/Keyboard/`** | 🔲 TODO |
| 13b | **Move related tests from `Configuration/` to `Input/Keyboard/` in test project** | 🔲 TODO |
| 13c | **Make `Bind` type-safe: `string[]` → `Key[]` in `PlatformKeyBinding` and `Bind`** | 🔲 TODO |
| 13d | **Add dedicated `PlatformKeyBinding` tests** | 🔲 TODO |

---

# Part 1: Design

## Goals

1. Make all built-in key bindings configurable via `ConfigurationManager` (CM)
2. Support platform-specific key bindings (Windows / Linux / macOS)
3. Eliminate duplication — shared bindings defined once, applied to many views
4. Zero startup cost — C# code is source of truth; built-in config.json has no key binding entries
5. ~~Backward compatible~~ **Breaking changes are acceptable and will be documented in the PR**
6. **MEC-ready** — Minimize coupling to CM internals so the future migration to `Microsoft.Extensions.Configuration` is tractable. Specifically: use strongly-typed POCOs instead of raw nested dictionaries, and limit `[ConfigurationProperty]` decorations to only 3 properties (Application, View base, View per-type overrides)

## Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│  User config.json (overrides)                                │
│  "View.ViewKeyBindings": {                                   │
│    "TextField": { "Undo": { "All": ["Ctrl+Z"] } }           │
│  }                                                           │
└──────────────┬───────────────────────────────────────────────┘
               │ CM loads & replaces static property
               ▼
┌──────────────────────────────────────────────────────────────┐
│  C# Static Properties (source of truth)                      │
│  [ConfigurationProperty] — only 3 properties:                │
│                                                              │
│  Application.DefaultKeyBindings  ← Layer 1 (app-level)      │
│  View.DefaultKeyBindings         ← Layer 2 (shared base)    │
│  View.ViewKeyBindings            ← User overrides (merged)  │
│                                                              │
│  Plain statics (no [ConfigurationProperty]):                 │
│  TextField.DefaultKeyBindings    ← Layer 3 (view-specific)  │
│  ListView.DefaultKeyBindings     ← Layer 3 (view-specific)  │
│  ...                                                         │
└──────────────┬───────────────────────────────────────────────┘
               │ view.ApplyKeyBindings(layers...)
               ▼
┌──────────────────────────────────────────────────────────────┐
│  Platform Resolution (PlatformKeyBinding POCO)               │
│  Windows: All + Windows                                      │
│  Linux:   All + Linux                                        │
│  macOS:   All + Macos                                        │
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
3. **Per-command platform attribution** — Each command specifies which platforms its keys apply to via `All`, `Windows`, `Linux`, `Macos` properties on a strongly-typed `PlatformKeyBinding` record.
4. **Shared base layer** — Common bindings (navigation, clipboard, editing) defined once on `View`, applied only to views that register handlers for those commands.
5. **Skip unhandled commands** — `ApplyKeyBindings()` checks `view.GetSupportedCommands()` and only binds commands the view actually handles. This is the key mechanism that makes the shared base layer work without views getting bindings they don't support.
6. **MEC-ready: Strongly-typed POCOs** — Platform key bindings use a `PlatformKeyBinding` record instead of raw `Dictionary<string, string[]>`. This maps cleanly to MEC's `IOptions<T>` binding model.
7. **MEC-ready: Minimal `[ConfigurationProperty]` surface** — Only 3 properties are decorated with `[ConfigurationProperty]`: `Application.DefaultKeyBindings`, `View.DefaultKeyBindings`, and `View.ViewKeyBindings` (a merged dictionary for all view-specific overrides). Individual view `DefaultKeyBindings` properties are plain statics — not CM-discoverable. This keeps the migration surface small (3 properties vs ~15+) while still allowing user customization of any view's bindings via the merged `View.ViewKeyBindings` property.

---

## Platform Model

Four platform properties on `PlatformKeyBinding`, **additive** within a command:

| Property | Matches |
|----------|---------|
| `All` | Every platform |
| `Windows` | Windows only |
| `Linux` | Linux only |
| `Macos` | macOS only |

Resolution for current OS:
- **Windows**: collect keys from `All` + `Windows`
- **Linux**: collect keys from `All` + `Linux`
- **macOS**: collect keys from `All` + `Macos`

Keys are **additive** within a command. For non-Windows bindings, specify both `Linux` and `Macos` (or use `Bind.NonWindows`):
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

## C# Types

### `PlatformKeyBinding` — Strongly-Typed Platform Key Mapping

New file: `Terminal.Gui/Configuration/PlatformKeyBinding.cs`

**MEC-readiness:** This is a POCO record, not a raw dictionary. MEC's `IOptions<T>` / `ConfigurationBinder` can bind to it directly. It also serializes cleanly with `System.Text.Json`.

```csharp
/// <summary>
/// Defines the key strings for a single command, optionally varying by platform.
/// </summary>
public record PlatformKeyBinding
{
    /// <summary>Keys that apply on all platforms.</summary>
    public string[]? All { get; init; }

    /// <summary>Additional keys for Windows only.</summary>
    public string[]? Windows { get; init; }

    /// <summary>Additional keys for Linux only.</summary>
    public string[]? Linux { get; init; }

    /// <summary>Additional keys for macOS only.</summary>
    public string[]? Macos { get; init; }
}
```

The outer container type for a set of key bindings is:

```csharp
// Outer key = Command name (string), value = platform-attributed keys
Dictionary<string, PlatformKeyBinding>
```

### Ergonomic Helper: `Bind` static class

New file: `Terminal.Gui/Configuration/Bind.cs`

The `Bind` helper returns `PlatformKeyBinding` instances (not raw dictionaries):

```csharp
internal static class Bind
{
    /// <summary>All platforms get these keys.</summary>
    public static PlatformKeyBinding All (params string[] keys)
        => new () { All = keys };

    /// <summary>All platforms get the base key; specific platforms get additional keys.</summary>
    public static PlatformKeyBinding AllPlus (
        string key,
        string[]? nonWindows = null,
        string[]? windows = null,
        string[]? linux = null,
        string[]? macos = null)
    {
        return new ()
        {
            All = [key],
            Windows = windows,
            Linux = nonWindows is not null && linux is null ? nonWindows : linux,
            Macos = nonWindows is not null && macos is null ? nonWindows : macos,
        };
    }

    /// <summary>Linux + macOS get these keys. Convenience for specifying both.</summary>
    public static PlatformKeyBinding NonWindows (params string[] keys)
        => new () { Linux = keys, Macos = keys };

    /// <summary>Platform-specific keys only (no "all" entry).</summary>
    public static PlatformKeyBinding Platform (
        string[]? windows = null,
        string[]? linux = null,
        string[]? macos = null)
        => new () { Windows = windows, Linux = linux, Macos = macos };
}
```

---

## Layered Architecture

Key bindings are applied in three layers. Each layer is a `Dictionary<string, PlatformKeyBinding>` static property. **Only 3 properties** are decorated with `[ConfigurationProperty]` — this is an intentional design choice to minimize the migration surface when moving to MEC post-v2.

| Layer | Property | `[ConfigurationProperty]`? | Purpose |
|-------|----------|---------------------------|---------|
| 1 | `Application.DefaultKeyBindings` | ✅ Yes | Global app-level bindings |
| 2 | `View.DefaultKeyBindings` | ✅ Yes | Shared base layer for all views |
| 3 | `View.ViewKeyBindings` | ✅ Yes | Merged dict of per-view overrides (keyed by type name) |
| — | `TextField.DefaultKeyBindings` etc. | ❌ No — plain static | View-specific defaults (code only) |

**Why only 3 `[ConfigurationProperty]` decorations?** Today's CM discovers properties via assembly-scanning reflection and sets them via `PropertyInfo.SetValue(null, ...)`. MEC uses `IOptions<T>` on instances. Every `[ConfigurationProperty]` is a migration point. By keeping the count at 3 instead of ~15+, we reduce the MEC migration surface by ~80%.

**How users customize per-view bindings:** Via the merged `View.ViewKeyBindings` property, which is a `Dictionary<string, Dictionary<string, PlatformKeyBinding>>` — outer key is the type name (e.g., `"TextField"`), inner dict is command→keys. CM can discover and override this single property; at apply time, each view checks whether `ViewKeyBindings` has an entry for its type name and merges those bindings.

### Layer 1: Application Key Bindings (`Application.DefaultKeyBindings`)

Global application-level bindings. Applied by `ApplicationKeyboard.AddKeyBindings()`.

> **Phase 12 breaking change:** The separate `Application.QuitKey`, `ArrangeKey`, `NextTabKey`,
> `PrevTabKey`, `NextTabGroupKey`, `PrevTabGroupKey` properties (and matching `IKeyboard` members)
> are **removed**. Use `Application.GetDefaultKey("Quit")` / `Application.GetDefaultKeys("Quit")`
> instead. See Phase 12 below.

```csharp
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
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

### Layer 2: View Base Key Bindings (`View.DefaultKeyBindings`)

Common bindings shared across many views. Only applied to views that have registered command handlers for those commands (via `GetSupportedCommands()` filtering).

```csharp
// View.Keyboard.cs
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
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

**No `[ConfigurationProperty]` on view-specific properties.** These are plain statics — code-only defaults. User customization goes through the merged `View.ViewKeyBindings` property (see below).

Example — TextField (text-editing specific only):
```csharp
// NO [ConfigurationProperty] — plain static, not CM-discoverable
public static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
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
    ["DeleteAll"]        = Bind.All ("Ctrl+Shift+Delete"),
};
```

Example — ListView (list-specific only):
```csharp
// NO [ConfigurationProperty] — plain static
public static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
{
    // Emacs nav shortcuts (extend base CursorUp/Down)
    ["Up"]   = Bind.NonWindows ("Ctrl+P"),
    ["Down"] = Bind.NonWindows ("Ctrl+N"),
};
// NOTE: Multi-command bindings (Shift+Space → Activate+Down) stay as direct KeyBindings.Add()
```

### Layer 3b: Merged View Overrides (`View.ViewKeyBindings`) — CM/MEC bridge

This is the **single `[ConfigurationProperty]`-decorated entry point** for per-view key binding customization. It's a `Dictionary<string, Dictionary<string, PlatformKeyBinding>>` where the outer key is the view type name (e.g., `"TextField"`, `"ListView"`).

```csharp
// View.Keyboard.cs
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, Dictionary<string, PlatformKeyBinding>>? ViewKeyBindings { get; set; }
```

**Default value is `null`** (no overrides). When a user provides overrides in config.json, CM populates this. At apply time, `ApplyKeyBindings()` checks `View.ViewKeyBindings?[GetType().Name]` and merges any entries found.

**Why this design?**
- Only 1 `[ConfigurationProperty]` for ALL view-specific overrides (instead of ~13 separate ones)
- Users can still customize any view's bindings via config.json
- MEC migration: 1 `IOptions<ViewKeyBindingsOptions>` replaces 1 `[ConfigurationProperty]`
- Code defaults remain in each view's plain static `DefaultKeyBindings` (no CM overhead)

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
protected void ApplyKeyBindings (params Dictionary<string, PlatformKeyBinding>?[] layers)
{
    HashSet<Command> supported = new (GetSupportedCommands ());

    // Apply code-defined layers (base + view-specific)
    foreach (Dictionary<string, PlatformKeyBinding>? layer in layers)
    {
        if (layer is null) continue;

        ApplyLayer (layer, supported);
    }

    // Apply user overrides from View.ViewKeyBindings (CM/MEC bridge)
    string typeName = GetType ().Name;

    if (ViewKeyBindings?.TryGetValue (typeName, out Dictionary<string, PlatformKeyBinding>? overrides) == true
        && overrides is not null)
    {
        ApplyLayer (overrides, supported);
    }
}

private void ApplyLayer (Dictionary<string, PlatformKeyBinding> layer, HashSet<Command> supported)
{
    foreach ((string commandName, PlatformKeyBinding platformKeys) in layer)
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

/// <summary>Resolves platform-specific key strings for the current OS.</summary>
private static IEnumerable<string> ResolveKeysForCurrentPlatform (PlatformKeyBinding platformKeys)
{
    if (platformKeys.All is not null)
    {
        foreach (string k in platformKeys.All) yield return k;
    }

    string platform = PlatformDetection.GetCurrentPlatformName ();
    string[]? platKeys = platform switch
    {
        "windows" => platformKeys.Windows,
        "linux" => platformKeys.Linux,
        "macos" => platformKeys.Macos,
        _ => null
    };

    if (platKeys is not null)
    {
        foreach (string k in platKeys) yield return k;
    }
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
    // Override Application quit key — adds Ctrl+Q on Linux/macOS
    "Application.DefaultKeyBindings": {
        "Quit": { "All": ["Esc"], "Linux": ["Ctrl+Q"], "Macos": ["Ctrl+Q"] }
    },

    // Override TextField-specific bindings via the merged ViewKeyBindings property
    "View.ViewKeyBindings": {
        "TextField": {
            "Undo": { "All": ["Ctrl+Z"] }
        },
        "ListView": {
            "Up": { "Linux": ["Ctrl+P"], "Macos": ["Ctrl+P"] }
        }
    }
}
```

**Override semantics**: CM replaces the entire static property value. A user override for `View.ViewKeyBindings` replaces ALL per-view overrides. `View.DefaultKeyBindings` and `Application.DefaultKeyBindings` are separate properties and unaffected.

---

## SourceGenerationContext Registration

```csharp
[JsonSerializable (typeof (PlatformKeyBinding))]
[JsonSerializable (typeof (Dictionary<string, PlatformKeyBinding>))]
[JsonSerializable (typeof (Dictionary<string, Dictionary<string, PlatformKeyBinding>>))]
```

---

## Known Constraint: Open Generic Types

`[ConfigurationProperty]` CANNOT be placed on open generic types (`TreeView<T>`, `NumericUpDown<T>`, `LinearRange<T>`). CM's `ConfigProperty.Initialize()` calls `PropertyInfo.GetValue(null)` which throws on open generics. **With the new design, this is largely moot** — view-specific `DefaultKeyBindings` are plain statics without `[ConfigurationProperty]`, so they can live on non-generic base classes without the CM constraint being relevant. User customization of generic views goes through the merged `View.ViewKeyBindings` property (keyed by type name, e.g., `"TreeView"`).

---

## Prerequisite: Undo/Redo/Paste Unification (#4825) — ✅ MERGED

TextField and TextView had incompatible key bindings for Undo/Redo/Paste/DeleteAll. This was resolved by PR #4828 (merged). After that merge, both views use:

| Command | All Platforms | Non-Windows (additional) |
|---------|--------------|--------------------------|
| Paste | Ctrl+V | — |
| Undo | Ctrl+Z | Ctrl+/ |
| Redo | Ctrl+Y | Ctrl+Shift+Z |
| DeleteAll | Ctrl+Shift+Delete | — |

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
**New file:** `Terminal.Gui/Configuration/PlatformKeyBinding.cs`

Register the new POCO type and its containers for JSON serialization:
```csharp
[JsonSerializable (typeof (PlatformKeyBinding))]
[JsonSerializable (typeof (Dictionary<string, PlatformKeyBinding>))]
[JsonSerializable (typeof (Dictionary<string, Dictionary<string, PlatformKeyBinding>>))]
```

Tests first (`Tests/UnitTestsParallelizable/Configuration/KeyBindingSchemaTests.cs`):

| # | Test | Validates |
|---|------|-----------|
| 1 | `PlatformKeyBinding_RoundTrips_ThroughJson` | Serialize → deserialize a `PlatformKeyBinding` preserves All/Windows/Linux/Macos |
| 2 | `KeyBindingDict_RoundTrips_ThroughJson` | Serialize → deserialize a `Dictionary<string, PlatformKeyBinding>` preserves all data |
| 3 | `KeyBindingDict_Deserializes_FromUserConfigFormat` | Parse `{ "Left": { "All": ["CursorLeft"], "Linux": ["Ctrl+B"] } }` correctly |
| 4 | `KeyBindingDict_EmptyDict_RoundTrips` | Empty dict serializes/deserializes without error |
| 5 | `KeyBindingDict_CM_CanDiscover_DictProperty` | A `[ConfigurationProperty]` of this type is found by CM's property discovery |
| 6 | `ViewKeyBindings_RoundTrips_ThroughJson` | `Dictionary<string, Dictionary<string, PlatformKeyBinding>>` round-trips (the merged override dict) |

Commit: "Add PlatformKeyBinding POCO and JSON schema support"

### Phase 3b: `Bind` Helper + `PlatformDetection` Extension

**New file:** `Terminal.Gui/Configuration/Bind.cs`
**Modified:** `Terminal.Gui/Drivers/PlatformDetection.cs` — add `GetCurrentPlatformName()`

Tests first (`Tests/UnitTestsParallelizable/Configuration/BindTests.cs`):

| # | Test | Validates |
|---|------|-----------|
| 1 | `Bind_All_SingleKey_ReturnsPlatformKeyBinding` | `Bind.All("CursorLeft")` → `PlatformKeyBinding { All = ["CursorLeft"] }` |
| 2 | `Bind_All_MultipleKeys` | `Bind.All("Home", "Ctrl+Home")` → `{ All = ["Home", "Ctrl+Home"] }` |
| 3 | `Bind_AllPlus_NonWindowsKeys` | `Bind.AllPlus("Delete", nonWindows: ["Ctrl+D"])` → `{ All = ["Delete"], Linux = ["Ctrl+D"], Macos = ["Ctrl+D"] }` |
| 4 | `Bind_AllPlus_WindowsKeys` | `Bind.AllPlus("X", windows: ["Ctrl+X"])` → `{ All = ["X"], Windows = ["Ctrl+X"] }` |
| 5 | `Bind_AllPlus_NullPlatforms_LeavesNull` | Null platforms stay null on the record |
| 6 | `Bind_NonWindows_SetsLinuxAndMacos` | `Bind.NonWindows("Ctrl+Z")` → `{ Linux = ["Ctrl+Z"], Macos = ["Ctrl+Z"] }` — All is null |
| 7 | `Bind_Platform_LinuxOnly` | `Bind.Platform(linux: ["Ctrl+Z"])` → only Linux set |
| 8 | `Bind_Platform_WindowsAndMacos` | Both present, All is null |
| 9 | `Bind_Platform_AllNulls_ReturnsEmpty` | All properties null |

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
| 1 | `ApplyKeyBindings_AllPlatform_BindsKey` | `PlatformKeyBinding { All = ["CursorLeft"] }` binds CursorLeft→Left |
| 2 | `ApplyKeyBindings_CurrentPlatformOnly_BindsOnThisPlatform` | Platform-specific property binds on matching OS |
| 3 | `ApplyKeyBindings_OtherPlatformOnly_DoesNotBind` | Entry for different platform doesn't bind |
| 4 | `ApplyKeyBindings_AllPlusPlatform_Additive` | Both `All` and platform-specific keys bind |
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
| 15 | `ApplyKeyBindings_ViewKeyBindings_MergesOverrides` | `View.ViewKeyBindings["TestView"]` entries merge into bindings |
| 16 | `ApplyKeyBindings_ViewKeyBindings_Null_NoOp` | `View.ViewKeyBindings = null` doesn't throw |
| 17 | `ApplyKeyBindings_ViewKeyBindings_NoEntryForType_NoOp` | ViewKeyBindings exists but no entry for this view's type name — no change |

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
| 9 | `View_ViewKeyBindings_IsNull_ByDefault` | `View.ViewKeyBindings` is null when no overrides are configured |
| 10 | `View_ViewKeyBindings_HasConfigurationPropertyAttribute` | `[ConfigurationProperty]` is applied to `View.ViewKeyBindings` |

At this point, `View.DefaultKeyBindings` is defined but NOT yet wired into any view's setup. Wiring happens in Phase 7.

Commit: "Add View.DefaultKeyBindings shared base layer"

### Phase 7: Migrate Views (One at a Time)

For each view, follow this sub-pattern:
1. Write tests that verify the view's CURRENT key bindings (snapshot the expected state)
2. Add a `public static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings` property (**no `[ConfigurationProperty]`** — plain static)
3. Switch the view from direct `KeyBindings.Add` to `ApplyKeyBindings(View.DefaultKeyBindings, ViewType.DefaultKeyBindings)`
4. Verify the snapshot tests still pass (no behavior change)
5. Add tests for the static `DefaultKeyBindings` property (not null, parseable keys, parseable command names)
6. **Do NOT add `[ConfigurationProperty]`** to any view-specific property — user customization goes through `View.ViewKeyBindings`

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
- Generic type: plain static on non-generic base class (no CM constraint since no `[ConfigurationProperty]`)
- Tests: verify generic class references non-generic property

#### 7e: LinearRange
- 2 unique + orientation-dependent bindings (stay as direct `KeyBindings.Add`)
- Generic type: same pattern as NumericUpDown
- Tests: verify Home→LeftStart, End→RightEnd + orientation bindings

#### 7f: TreeView
- 17 bindings, unique commands: `Expand`, `ExpandAll`, `Collapse`, `CollapseAll`, `LineUpToFirstBranch`, `LineDownToLastBranch`
- Generic type: same pattern — plain static on non-generic base
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

These keys should move into the `DefaultKeyBindings` dict pattern so they're configurable (via `View.ViewKeyBindings`):

```csharp
// MenuBar — plain static (no [ConfigurationProperty])
public static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
{
    ["HotKey"] = Bind.All ("F10"),
};

// PopoverMenu — plain static (no [ConfigurationProperty])
public static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
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
| 4 | `MenuBar_Configurable_ViaViewKeyBindings` | Override via `View.ViewKeyBindings["MenuBar"]` works |
| 5 | `PopoverMenu_Configurable_ViaViewKeyBindings` | Override via `View.ViewKeyBindings["PopoverMenu"]` works |

Commit: "Standardize popover activation keys (MenuBar F9→F10)"

### Phase 9: config.json Cleanup

- Remove ALL `DefaultKeyBindings` / `DefaultKeyBindingsUnix` entries from built-in `config.json`
- Verify CM discovers the 3 `[ConfigurationProperty]`-decorated properties: `Application.DefaultKeyBindings`, `View.DefaultKeyBindings`, `View.ViewKeyBindings`
- Verify user override format works by writing a test that sets `View.ViewKeyBindings` and confirms ApplyKeyBindings uses it

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

---

## Phase 11b: Move DefaultKeyBindings + Create Config Examples (✅ Done)

- Moved `DefaultKeyBindings` `[ConfigurationProperty]` from internal `ApplicationKeyboard` to
  the public `static partial class Application` in `Application.Keyboard.cs` so the CM JSON key
  is `Application.DefaultKeyBindings` (not `ApplicationKeyboard.DefaultKeyBindings`).
- Created `Examples/Config/` folder with:
  - `macos.json` — overrides `Quit` to `["Esc","Ctrl+Q"]` etc. for Windows users who want macOS bindings
  - `windows.json` — overrides `Quit` to `["Ctrl+Q"]` etc. for macOS users who want Windows bindings
  - `README.md` — usage guide and comparison tables

---

## Phase 12: `Dictionary<Command, PlatformKeyBinding>` Throughout + Remove Single-Key Properties (🔲 TODO)

> **BREAKING CHANGE — will be documented in PR.**
>
> JSON format for key binding dicts also changes (see Step 12a).

### Overview

Two tightly-coupled goals addressed together:

1. **Type-safe key dict:** Change all `DefaultKeyBindings` dictionaries from `Dictionary<string, PlatformKeyBinding>` to `Dictionary<Command, PlatformKeyBinding>`. This eliminates string parsing in the hot path and catches command-name typos at compile time.

2. **Remove single-key properties:** Remove `Application.QuitKey`, `ArrangeKey`, `NextTabKey`, `PrevTabKey`, `NextTabGroupKey`, `PrevTabGroupKey` (and matching `IKeyboard` members) and wire `ApplicationKeyboard.AddKeyBindings()` to read `Application.DefaultKeyBindings` instead.

### New C# API

```csharp
// All DefaultKeyBindings dictionaries change type:
//   Before: Dictionary<string, PlatformKeyBinding>
//   After:  Dictionary<Command, PlatformKeyBinding>

// Application-level helpers
public static Key GetDefaultKey (Command command);               // first platform-resolved key
public static IEnumerable<Key> GetDefaultKeys (Command command); // all platform-resolved keys

// Fires when Application.DefaultKeyBindings is replaced (e.g. by CM)
public static event EventHandler? DefaultKeyBindingsChanged;
```

### JSON Format

The JSON format uses standard flat objects throughout — this is unchanged from the current files.
Command enum names (`"Quit"`, `"Arrange"`, etc.) are the JSON property keys; STJ source generation
maps them to `Command.Quit`, `Command.Arrange`, etc. automatically. The full set of properties:

```json
{
  "Application.DefaultKeyBindings": {
    "Quit":   { "All": ["Esc", "Ctrl+Q"] },
    "Arrange": { "All": ["Ctrl+F5"] }
  },
  "View.DefaultKeyBindings": {
    "Copy": { "All": ["Ctrl+C"] },
    "Undo": { "All": ["Ctrl+Z"], "Macos": ["Ctrl+/"] }
  },
  "View.ViewKeyBindings": {
    "TextField": {
      "CutToEndOfLine": { "All": ["Ctrl+K"] },
      "WordLeft":       { "All": ["Ctrl+CursorLeft"] }
    }
  }
}
```

`View.ViewKeyBindings` outer key is the view type name (string); inner keys are command names.
See `Examples/Config/macos.json` and `windows.json` for complete examples.

---

### Step 12a: Add `TuiPlatform` Enum + Type-Safe Platform Resolution

**Problem:** `PlatformDetection.GetCurrentPlatformName()` returns a raw string (`"windows"`, `"linux"`, `"macos"`). The `switch` in `ResolveKeysForCurrentPlatform` matches on those strings — silently broken if the string ever changes. Making it type-safe also enables a clean `PlatformKeyBinding.GetCurrentPlatformKeys()` instance method.

**New type** (new file `Terminal.Gui/Drivers/TuiPlatform.cs`):

```csharp
/// <summary>Identifies the operating system for platform-specific key binding resolution.</summary>
public enum TuiPlatform
{
    /// <summary>Microsoft Windows.</summary>
    Windows,

    /// <summary>Linux.</summary>
    Linux,

    /// <summary>macOS (Darwin).</summary>
    Macos,
}
```

**`PlatformDetection.cs`** — replace `GetCurrentPlatformName()` with `GetCurrentPlatform()`:

```csharp
// BEFORE
public static string GetCurrentPlatformName ()
{
    if (IsWindows ()) return "windows";
    if (IsMac ()) return "macos";
    return "linux";
}

// AFTER
public static TuiPlatform GetCurrentPlatform ()
{
    if (IsWindows ()) return TuiPlatform.Windows;
    if (IsMac ()) return TuiPlatform.Macos;
    return TuiPlatform.Linux;
}
```

**`PlatformKeyBinding.cs`** — add `GetCurrentPlatformKeys()` (moves logic out of `View.Keyboard.cs`):

```csharp
// BEFORE: private static in View.Keyboard.cs, switched on string
string []? platKeys = platform switch
{
    "windows" => platformKeys.Windows,
    "linux"   => platformKeys.Linux,
    "macos"   => platformKeys.Macos,
    _         => null
};

// AFTER: public instance method on PlatformKeyBinding, switches on TuiPlatform
public IEnumerable<string> GetCurrentPlatformKeys ()
{
    if (All is { })
    {
        foreach (string k in All) yield return k;
    }

    string []? platKeys = PlatformDetection.GetCurrentPlatform () switch
    {
        TuiPlatform.Windows => Windows,
        TuiPlatform.Linux   => Linux,
        TuiPlatform.Macos   => Macos,
        _                   => null
    };

    if (platKeys is null) yield break;

    foreach (string k in platKeys) yield return k;
}
```

**`View.Keyboard.cs`** — replace `ResolveKeysForCurrentPlatform(entry.Value)` calls with `entry.Value.GetCurrentPlatformKeys()`.

**`BindTests.cs`** — update tests that assert on `GetCurrentPlatformName()` string results.

Files: `TuiPlatform.cs` (new), `PlatformDetection.cs`, `PlatformKeyBinding.cs`, `View.Keyboard.cs`, `BindTests.cs`

Commit: "Add TuiPlatform enum; replace string platform IDs with typed enum"

---

### Step 12b: Register `Dictionary<Command, PlatformKeyBinding>` with STJ Source Gen

Standard STJ source generation supports enum-keyed dicts — enum names become JSON string keys automatically. No custom converter needed.

**`SourceGenerationContext.cs`** — update registrations:

```csharp
// Remove:
[JsonSerializable (typeof (Dictionary<string, PlatformKeyBinding>))]
[JsonSerializable (typeof (Dictionary<string, Dictionary<string, PlatformKeyBinding>>))]

// Add:
[JsonSerializable (typeof (Dictionary<Command, PlatformKeyBinding>))]
[JsonSerializable (typeof (Dictionary<string, Dictionary<Command, PlatformKeyBinding>>))]
```

`DictionaryJsonConverter<T>` is not used for key binding dicts (source-gen context takes precedence).

Commit: "Register Command-keyed key binding dict types with STJ source gen"

---
### Step 12b: Change All `DefaultKeyBindings` to `Dictionary<Command, PlatformKeyBinding>`

#### `Application.cs` — before/after

```csharp
// BEFORE
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
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

// AFTER
[ConfigurationProperty (Scope = typeof (SettingsScope))]
public static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
{
    [Command.Quit]             = Bind.All ("Esc"),
    [Command.Suspend]          = Bind.NonWindows ("Ctrl+Z"),
    [Command.Arrange]          = Bind.All ("Ctrl+F5"),
    [Command.NextTabStop]      = Bind.All ("Tab"),
    [Command.PreviousTabStop]  = Bind.All ("Shift+Tab"),
    [Command.NextTabGroup]     = Bind.All ("F6"),
    [Command.PreviousTabGroup] = Bind.All ("Shift+F6"),
    [Command.Refresh]          = Bind.All ("F5"),
};
```

#### `TextField.Commands.cs` — before/after (representative of all 13 per-view dicts)

```csharp
// BEFORE
public new static Dictionary<string, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
{
    ["Left"]            = Bind.All ("Ctrl+B"),
    ["Right"]           = Bind.All ("Ctrl+F"),
    ["WordLeft"]        = Bind.All ("Ctrl+CursorLeft", "Ctrl+CursorUp"),
    ["WordRight"]       = Bind.All ("Ctrl+CursorRight", "Ctrl+CursorDown"),
    ["WordLeftExtend"]  = Bind.All ("Ctrl+Shift+CursorLeft", "Ctrl+Shift+CursorUp"),
    ["WordRightExtend"] = Bind.All ("Ctrl+Shift+CursorRight", "Ctrl+Shift+CursorDown"),
    ["CutToEndOfLine"]  = Bind.All ("Ctrl+K"),
    // ... (13 entries total)
};

// AFTER
public new static Dictionary<Command, PlatformKeyBinding>? DefaultKeyBindings { get; set; } = new ()
{
    [Command.Left]            = Bind.All ("Ctrl+B"),
    [Command.Right]           = Bind.All ("Ctrl+F"),
    [Command.WordLeft]        = Bind.All ("Ctrl+CursorLeft", "Ctrl+CursorUp"),
    [Command.WordRight]       = Bind.All ("Ctrl+CursorRight", "Ctrl+CursorDown"),
    [Command.WordLeftExtend]  = Bind.All ("Ctrl+Shift+CursorLeft", "Ctrl+Shift+CursorUp"),
    [Command.WordRightExtend] = Bind.All ("Ctrl+Shift+CursorRight", "Ctrl+Shift+CursorDown"),
    [Command.CutToEndOfLine]  = Bind.All ("Ctrl+K"),
    // ... (13 entries total)
};
```

#### `View.Keyboard.cs` — `ApplyKeyBindings` loop — before/after

```csharp
// BEFORE — requires string→Command parsing
foreach (KeyValuePair<string, PlatformKeyBinding> entry in defaults)
{
    if (!Enum.TryParse (entry.Key, out Command command))
        continue; // silently ignore unknown command names

    foreach (string keyStr in ResolveKeysForCurrentPlatform (entry.Value))
    {
        if (!Key.TryParse (keyStr, out Key key))
            continue;

        if (KeyBindings.TryGet (key, out _))
            continue;

        KeyBindings.Add (key, command);
    }
}

// AFTER — Command key is already typed; no parsing needed
foreach (KeyValuePair<Command, PlatformKeyBinding> entry in defaults)
{
    foreach (string keyStr in entry.Value.GetCurrentPlatformKeys ())
    {
        if (!Key.TryParse (keyStr, out Key key))
            continue;

        if (KeyBindings.TryGet (key, out _))
            continue;

        KeyBindings.Add (key, entry.Key);
    }
}
```

#### JSON — unchanged (enum names serialize as strings automatically)

```json
{
    "Application.DefaultKeyBindings": {
        "Quit":   { "All": ["Esc", "Ctrl+Q"] },
        "Arrange": { "All": ["Ctrl+F5"] }
    },
    "View.DefaultKeyBindings": {
        "Copy": { "All": ["Ctrl+C"] },
        "Undo": { "All": ["Ctrl+Z"], "Macos": ["Ctrl+/"] }
    },
    "View.ViewKeyBindings": {
        "TextField": {
            "CutToEndOfLine": { "All": ["Ctrl+K"] },
            "WordLeft":       { "All": ["Ctrl+CursorLeft"] }
        }
    }
}
```

#### Files changed

| Location | Change |
|----------|--------|
| `App/Application.cs` — `DefaultKeyBindings` | `Dictionary<string, …>` → `Dictionary<Command, …>` |
| `ViewBase/View.Keyboard.cs` — `DefaultKeyBindings` (base layer) | same |
| `ViewBase/View.Keyboard.cs` — `ViewKeyBindings` | inner dict: `Dictionary<string, …>` → `Dictionary<Command, …>` |
| `ViewBase/View.Keyboard.cs` — `ApplyKeyBindings()` | iterate `Command` keys directly — drop `Enum.TryParse` |
| All 13 per-view `DefaultKeyBindings` (TextField, TextView, ListView, etc.) | same type change |
| `Tests/…ApplicationDefaultKeyBindingsTests.cs` | Update dict literals to use `Command.Quit` etc. |
| `Tests/…ViewDefaultKeyBindingsTests.cs` and per-view tests | same |
| `Examples/UICatalog/Scenarios/KeyBindings.cs` | `FormatDefaultKeyBindings()` — iterate `Command` keys, call `.ToString()` for display |

Commit: "Change all DefaultKeyBindings from string-keyed to Command-keyed"

---

### Step 12c: Remove Single-Key Properties; Wire `AddKeyBindings()` to `DefaultKeyBindings`

**Files in `Terminal.Gui/`:**

| File | Change |
|------|--------|
| `App/Application.cs` | Remove `QuitKey`, `ArrangeKey`, `NextTabKey`, `PrevTabKey`, `NextTabGroupKey`, `PrevTabGroupKey` props + `*Changed` events. Add `DefaultKeyBindingsChanged` event (fired from `DefaultKeyBindings` setter). Add `GetDefaultKey(Command)` + `GetDefaultKeys(Command)` helpers. |
| `App/Keyboard/IKeyboard.cs` | Remove the 6 single-key members from the interface. |
| `App/Keyboard/ApplicationKeyboard.cs` | Remove 6 backing fields + properties + event handlers. Subscribe to `Application.DefaultKeyBindingsChanged`. Rewrite `AddKeyBindings()` (see below). |
| `App/ApplicationImpl.Lifecycle.cs` | Delete key-preservation block (lines 67–88, marked BUGBUG). |
| `Configuration/PlatformKeyBinding.cs` | Add `GetCurrentPlatformKeys()` instance method (refactored out of `View.ResolveKeysForCurrentPlatform` private static). |
| `App/Popovers/PopoverImpl.cs` | Loop `Application.GetDefaultKeys(Command.Quit)` to bind all quit keys. |
| `Views/Menu/MenuBar.cs` | Remove redundant `KeyBindings.ReplaceCommands(Application.QuitKey, …)`. |
| `Views/Menu/MenuBarItem.cs` | `e == Application.QuitKey` → `Application.GetDefaultKeys(Command.Quit).Any(k => k == e)`. |
| `Views/Menu/PopoverMenu.cs` | Same pattern for two `Application.QuitKey` comparisons. |
| `Views/TextInput/TextValidateField.cs` | Same pattern for `key == Application.QuitKey`. |
| `ViewBase/Adornment/Arranger.cs` | Loop `Application.GetDefaultKeys(Command.Arrange)` to bind all arrange keys. |

**Files in `Examples/`:**

| File | Change |
|------|--------|
| `UICatalog/Scenario.cs` | `GetQuitKeyAndName()` uses `Application.GetDefaultKey(Command.Quit)`. |
| `UICatalog/UICatalogRunnable.cs` | Replace 3 `Application.QuitKey` refs. |
| ~40 scenario files | Most use `GetQuitKeyAndName()` (auto-fixed). Fix remaining direct refs. |

**`AddKeyBindings()` after rewrite:**

```csharp
internal void AddKeyBindings ()
{
    _commandImplementations.Clear ();

    AddCommand (Command.Quit, () => { App?.RequestStop (); return true; });
    AddCommand (Command.Suspend, () => { /* ... */ return true; });
    AddCommand (Command.NextTabStop, () => App?.Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop));
    AddCommand (Command.PreviousTabStop, () => App?.Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop));
    AddCommand (Command.NextTabGroup, () => App?.Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup));
    AddCommand (Command.PreviousTabGroup, () => App?.Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup));
    AddCommand (Command.Refresh, () => { App?.LayoutAndDraw (true); return true; });
    AddCommand (Command.Arrange, () => { /* ... */ return false; });

    KeyBindings.Clear ();

    if (Application.DefaultKeyBindings is { } defaults)
    {
        foreach (KeyValuePair<Command, PlatformKeyBinding> entry in defaults)
        {
            foreach (string keyStr in entry.Value.GetCurrentPlatformKeys ())
            {
                if (Key.TryParse (keyStr, out Key key))
                {
                    KeyBindings.Add (key, entry.Key);
                }
            }
        }
    }

    // Non-configurable navigation aliases
    KeyBindings.ReplaceCommands (Key.CursorRight, Command.NextTabStop);
    KeyBindings.ReplaceCommands (Key.CursorDown, Command.NextTabStop);
    KeyBindings.ReplaceCommands (Key.CursorLeft, Command.PreviousTabStop);
    KeyBindings.ReplaceCommands (Key.CursorUp, Command.PreviousTabStop);
}
```

Commit: "Remove single-key properties; wire ApplicationKeyboard to DefaultKeyBindings"

---

### Step 12d: Tests

| File | Change |
|------|--------|
| `Application/Keyboard/KeyboardTests.cs` | Remove tests for 6 single-key props. Add: adding `Ctrl+Q` to `DefaultKeyBindings[Command.Quit]` causes it to be live-bound. |
| `Application/Keyboard/ApplicationKeyboardThreadSafetyTests.cs` | Remove single-key prop refs. |
| `Application/Keyboard/ApplicationDefaultKeyBindingsTests.cs` | Add: `DefaultKeyBindingsChanged` fires on set. Add: multi-key Quit resolves on current platform. |

Commit: "Update tests for Command-keyed DefaultKeyBindings and removed single-key properties"

---

### Breaking Change Documentation (for PR)

```
BREAKING CHANGES in this PR:

Key binding dictionary type change:
  Before: Dictionary<string, PlatformKeyBinding>
  After:  Dictionary<Command, PlatformKeyBinding>
  Affects: Application.DefaultKeyBindings, View.DefaultKeyBindings,
           View.ViewKeyBindings (inner dict), all per-view DefaultKeyBindings.

Removed single-key properties from Application and IKeyboard:
  Application.QuitKey / QuitKeyChanged
  Application.ArrangeKey / ArrangeKeyChanged
  Application.NextTabKey / NextTabKeyChanged
  Application.PrevTabKey / PrevTabKeyChanged
  Application.NextTabGroupKey / NextTabGroupKeyChanged
  Application.PrevTabGroupKey / PrevTabGroupKeyChanged

Key binding JSON format change:
  New:  standard flat object              {"Quit": {...}, ...}
  Update any custom ~/.tui/config.json key binding sections to use object format.


Migration:
  Application.QuitKey                        → Application.GetDefaultKey (Command.Quit)
  key == Application.QuitKey                 → Application.GetDefaultKeys (Command.Quit).Any (k => k == key)
  Application.DefaultKeyBindings["Quit"]     → Application.DefaultKeyBindings[Command.Quit]
  keyboard.QuitKey = Key.Q.WithCtrl          → Application.DefaultKeyBindings[Command.Quit] = Bind.All ("Ctrl+Q")
```

---

## Phase 13: Move Files, Make `Bind` Type-Safe, Add `PlatformKeyBinding` Tests

### Step 13a: Move `Bind` + `PlatformKeyBinding` to `Input/Keyboard/`

**Rationale:** These types define key bindings — they belong with the other keyboard/input types, not in `Configuration/`. The `Configuration/` folder is for CM infrastructure (JSON converters, property discovery, themes). `Bind` and `PlatformKeyBinding` are used by `View.Keyboard.cs`, `ApplicationKeyboard.cs`, and all view command setup files — all input-layer consumers.

**Files:**
- Move `Terminal.Gui/Configuration/Bind.cs` → `Terminal.Gui/Input/Keyboard/Bind.cs`
- Move `Terminal.Gui/Configuration/PlatformKeyBinding.cs` → `Terminal.Gui/Input/Keyboard/PlatformKeyBinding.cs`

**No namespace change** — both are already `namespace Terminal.Gui;` (file-scoped).

**No code changes needed** — just file moves. All `using` statements and references remain valid.

Commit: "Move Bind and PlatformKeyBinding from Configuration/ to Input/Keyboard/"

---

### Step 13b: Move Related Tests to `Input/Keyboard/`

**Files:**
- Move `Tests/UnitTestsParallelizable/Configuration/BindTests.cs` → `Tests/UnitTestsParallelizable/Input/Keyboard/BindTests.cs`
- Move `Tests/UnitTestsParallelizable/Configuration/KeyBindingSchemaTests.cs` → `Tests/UnitTestsParallelizable/Input/Keyboard/KeyBindingSchemaTests.cs`

**Create `Tests/UnitTestsParallelizable/Input/Keyboard/` directory** if it doesn't exist.

Commit: "Move Bind and KeyBindingSchema tests to Input/Keyboard/"

---

### Step 13c: Make `Bind` Type-Safe — `string[]` → `Key[]`

**Problem:** `Bind.All("CursorLeft")` and `PlatformKeyBinding.All` use raw `string[]`. Typos like `"CusorLeft"` compile fine but fail silently at runtime when `Key.TryParse` returns `false`. Since `Key` has implicit conversion from `string`, making the types `Key[]` catches mistakes earlier and provides IDE auto-complete.

**Changes to `PlatformKeyBinding`:**

```csharp
// BEFORE
public record PlatformKeyBinding
{
    public string[]? All { get; init; }
    public string[]? Windows { get; init; }
    public string[]? Linux { get; init; }
    public string[]? Macos { get; init; }
}

// AFTER
public record PlatformKeyBinding
{
    public Key[]? All { get; init; }
    public Key[]? Windows { get; init; }
    public Key[]? Linux { get; init; }
    public Key[]? Macos { get; init; }
}
```

**Changes to `Bind`:**

```csharp
// BEFORE
public static PlatformKeyBinding All (params string[] keys)
    => new () { All = keys };

// AFTER
public static PlatformKeyBinding All (params Key[] keys)
    => new () { All = keys };
```

Same pattern for `AllPlus`, `NonWindows`, `Platform`.

**Changes to `GetCurrentPlatformKeys()`:**

```csharp
// BEFORE
public IEnumerable<string> GetCurrentPlatformKeys () { ... yield return k; }

// AFTER
public IEnumerable<Key> GetCurrentPlatformKeys () { ... yield return k; }
```

**Changes to `View.Keyboard.cs` `ApplyLayer()`:**

```csharp
// BEFORE
foreach (string keyStr in entry.Value.GetCurrentPlatformKeys ())
{
    if (!Key.TryParse (keyStr, out Key? key)) continue;
    ...
}

// AFTER
foreach (Key key in entry.Value.GetCurrentPlatformKeys ())
{
    // No TryParse needed — already a Key
    if (KeyBindings.TryGet (key, out _)) continue;
    KeyBindings.Add (key, entry.Key);
}
```

**Call sites** — `Bind.All ("CursorLeft")` still works because `Key` has `implicit operator Key (string)`. No changes needed at 90%+ of call sites.

**JSON serialization** — `Key` already has `KeyJsonConverter` registered in `SourceGenerationContext`. Need to register `Key[]` if not already, and verify `PlatformKeyBinding` round-trips through JSON with `Key[]` properties. The JSON format for keys remains strings (`"CursorLeft"`, `"Ctrl+A"`) — the converter handles the `Key` ↔ `string` mapping.

**`ToString()` on PlatformKeyBinding** — update to call `Key.ToString()` instead of using raw strings (likely no change needed since `Key` has `ToString()`).

Files changed:
- `Terminal.Gui/Input/Keyboard/PlatformKeyBinding.cs` (after 13a move)
- `Terminal.Gui/Input/Keyboard/Bind.cs` (after 13a move)
- `Terminal.Gui/ViewBase/View.Keyboard.cs` — `ApplyLayer()`, `ApplyKeyBindings()`
- `Terminal.Gui/App/Keyboard/ApplicationKeyboard.cs` — `AddKeyBindings()` if it uses string keys
- `Terminal.Gui/Configuration/SourceGenerationContext.cs` — register `Key[]` if needed
- All test files referencing `PlatformKeyBinding` string properties

Commit: "Make Bind and PlatformKeyBinding type-safe with Key[] instead of string[]"

---

### Step 13d: Add Dedicated `PlatformKeyBinding` Tests

**New file:** `Tests/UnitTestsParallelizable/Input/Keyboard/PlatformKeyBindingTests.cs`

| # | Test | Validates |
|---|------|-----------|
| 1 | `PlatformKeyBinding_Default_AllPropertiesNull` | `new PlatformKeyBinding ()` has all null properties |
| 2 | `PlatformKeyBinding_All_SetsCorrectly` | `new PlatformKeyBinding { All = [Key.CursorLeft] }` |
| 3 | `PlatformKeyBinding_Windows_SetsCorrectly` | Windows-only property set |
| 4 | `PlatformKeyBinding_Linux_SetsCorrectly` | Linux-only property set |
| 5 | `PlatformKeyBinding_Macos_SetsCorrectly` | macOS-only property set |
| 6 | `GetCurrentPlatformKeys_AllOnly_ReturnsAllKeys` | Only `All` set → returns those keys on any platform |
| 7 | `GetCurrentPlatformKeys_PlatformOnly_ReturnsCurrentPlatformKeys` | Only current platform set → returns those keys |
| 8 | `GetCurrentPlatformKeys_AllPlusPlatform_Additive` | Both `All` and current platform set → both returned |
| 9 | `GetCurrentPlatformKeys_OtherPlatformOnly_ReturnsEmpty` | Only non-current platform set → returns nothing |
| 10 | `GetCurrentPlatformKeys_AllNull_ReturnsEmpty` | All properties null → empty enumerable |
| 11 | `ToString_ShowsAllPlatforms` | Human-readable output includes all non-null platforms |
| 12 | `ToString_NullProperties_Omitted` | Null platforms not shown in ToString |
| 13 | `PlatformKeyBinding_RoundTrips_ThroughJson` | Serialize → deserialize preserves all Key[] properties |
| 14 | `PlatformKeyBinding_Equality_RecordSemantics` | Two records with same keys are equal |

Commit: "Add dedicated PlatformKeyBinding tests"