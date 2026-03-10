# ConfigurationManager Key Bindings — PR #4266

> Branch: `feature/cm-keybindings` → `v2_develop` on gui-cs/Terminal.Gui
> Original PR: https://github.com/gui-cs/Terminal.Gui/pull/4266 (branch was renamed; new PR needed)
> Fixes: #3023, #3089

---

## PR Description (Original)

### Overview

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

## TODO

- [ ] Create new PR from `feature/cm-keybindings` → `v2_develop`
- [ ] Design: finalize config JSON schema
- [ ] Design: resolve static vs instance property challenge
- [ ] Design: platform-specific binding strategy
- [ ] Implementation planning
