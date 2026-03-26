# Terminal.Gui Key Binding Config Examples

This folder contains example `config.json` files that override Terminal.Gui's default
key bindings to match platform conventions.

## How to Use

Copy the desired file to `~/.tui/config.json` (the global Terminal.Gui config location).

| OS | Want macOS feel? | Want Windows feel? |
|----|------------------|--------------------|
| **Windows** | Copy `macos.json` → `~/.tui/config.json` | (already default) |
| **macOS**   | (already default) | Copy `windows.json` → `~/.tui/config.json` |

On Windows `~` expands to `C:\Users\<username>`.  
On macOS/Linux `~` expands to `/home/<username>` (or `/Users/<username>` on macOS).

## What Each File Changes

### `macos.json` — macOS-style bindings (for Windows users)

Overrides Terminal.Gui's default key bindings to match macOS conventions:

| What changes | Default (Windows) | With `macos.json` |
|---|---|---|
| Quit app | `Esc` | `Esc` or `Ctrl+Q` |
| Suspend app to background | *(not available)* | `Ctrl+Z` |
| Undo | `Ctrl+Z` | `Ctrl+Z` or `Ctrl+/` |
| Redo | `Ctrl+Y` | `Ctrl+Y` or `Ctrl+Shift+Z` |
| Delete char right | `Delete` | `Delete` or `Ctrl+D` |

Note: Emacs navigation shortcuts (`Ctrl+B`/`Ctrl+F` for left/right in text fields,
`Ctrl+N`/`Ctrl+P` for up/down in text views and lists) are already available on all
platforms — no override needed.

### `windows.json` — Windows-style bindings (for macOS users)

Overrides Terminal.Gui's default key bindings to match Windows conventions:

| What changes | Default (macOS) | With `windows.json` |
|---|---|---|
| Quit app | `Esc` or `Ctrl+Q` | `Esc` only |
| Suspend app to background | `Ctrl+Z` | *(disabled)* |
| Undo | `Ctrl+Z` or `Ctrl+/` | `Ctrl+Z` only |
| Redo | `Ctrl+Y` or `Ctrl+Shift+Z` | `Ctrl+Y` only |
| Delete char right | `Delete` or `Ctrl+D` | `Delete` only |

**Limitation:** Emacs navigation shortcuts built into text views (`Ctrl+B`, `Ctrl+F`,
`Ctrl+N`, `Ctrl+P`, `Ctrl+K`, etc.) are set in C# code and cannot be removed via
`config.json`. They remain available alongside the standard keys.

## How It Works

Terminal.Gui's `ConfigurationManager` loads `~/.tui/config.json` and uses it to
replace the values of three key binding properties:

- **`Application.DefaultKeyBindings`** — app-level commands (Quit, Suspend, Tab navigation)
- **`View.DefaultKeyBindings`** — shared commands across all views (navigation, clipboard, editing)
- **`View.ViewKeyBindings`** — per-view overrides (keyed by view type name, e.g. `"TextField"`)

The JSON format maps command names to `PlatformKeyBinding` objects:

```json
{
    "Application.DefaultKeyBindings": {
        "Quit": { "All": ["Esc", "Ctrl+Q"] }
    },
    "View.DefaultKeyBindings": {
        "Undo": { "All": ["Ctrl+Z"], "Linux": ["Ctrl+/"], "Macos": ["Ctrl+/"] }
    },
    "View.ViewKeyBindings": {
        "TextField": {
            "WordLeft": { "All": ["Ctrl+CursorLeft"] }
        }
    }
}
```

Each `PlatformKeyBinding` has four optional fields:

| Field | Applies to |
|-------|-----------|
| `All` | Every platform |
| `Windows` | Windows only (added to `All`) |
| `Linux` | Linux only (added to `All`) |
| `Macos` | macOS only (added to `All`) |

**Important:** When you override a property (e.g. `View.DefaultKeyBindings`), your
JSON replaces the entire default dictionary. Any command you omit reverts to
having no binding from that layer. Always include all commands you want active.
