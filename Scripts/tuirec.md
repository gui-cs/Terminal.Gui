# Recording Terminal.Gui Apps with `tuirec`

Use this guide when an issue or PR asks for a GIF/video capture of a Terminal.Gui
app or scenario. The recording tool is [`gui-cs/tuirec`](https://github.com/gui-cs/tuirec) —
a Go CLI that spawns the target app in a PTY, injects keystrokes, records terminal
output as an asciinema v2 cast, and renders an animated GIF via `agg`.

## Install

```powershell
# Requires Go 1.22+
go install github.com/gui-cs/tuirec/cmd/tuirec@latest
tuirec --version

# agg is auto-downloaded on first use — no separate install needed.
```

Verify: `tuirec --version`. If not on PATH, add `$(go env GOPATH)\bin` to PATH.

## Quick Start — Recording a UICatalog Scenario

```powershell
# 1. Build ScenarioRunner (do this ONCE before recording)
dotnet build Examples/ScenarioRunner/ScenarioRunner.csproj -c Release

# 2. Record
$binary = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.exe"
$ks = 'wait:1000,CursorDown,CursorDown,CursorDown,wait:1000,Ctrl+Q'

tuirec record `
    --binary $binary `
    --args "run,Character Map" `
    --name charmap `
    --title "Character Map — Unicode Viewer" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 2000 `
    --cols 120 --rows 30 `
    --open --copy
```

Output: `artifacts/charmap.gif` and `artifacts/charmap.cast`.

---

## Recording UICatalog Scenarios

### Prerequisites

1. **Build ScenarioRunner** — always build before recording to avoid startup noise:
   ```powershell
   dotnet build Examples/ScenarioRunner/ScenarioRunner.csproj -c Release
   ```
2. **Know the scenario name** — list available scenarios:
   ```powershell
   dotnet run --project Examples/ScenarioRunner -c Release --no-build -- list
   ```

### Finding the Right Keystrokes

Each scenario has a `GetDemoKeyStrokes()` method that defines a canonical
interaction sequence for benchmarking. **Use this as your starting point:**

```powershell
# Find the demo keystrokes for a scenario:
grep -n "GetDemoKeyStrokes" Examples/UICatalog/Scenarios/<ScenarioFile>.cs
```

The demo keystrokes show what keys the scenario expects and what UI flow is
interesting. Translate them to tuirec syntax:

| Terminal.Gui Key | tuirec Token |
|---|---|
| `Key.CursorDown` | `CursorDown` |
| `Key.CursorLeft` | `CursorLeft` |
| `Key.Tab` | `Tab` |
| `Key.Tab.WithShift` | `Shift+Tab` |
| `Key.Enter` | `Enter` |
| `Key.Esc` | `Esc` |
| `Key.B` | `B` (or `` `B` `` for literal) |

### Composing the Keystroke Script

**Principles for a great recording:**

1. **Start with `wait:1000`** — let the UI render fully after startup-delay.
2. **Add `wait:` between logical steps** — `wait:500` to `wait:1500` between
   groups of actions so viewers can follow what's happening.
3. **Keep it short** — 10–20 seconds of real-time interaction. Fewer keystrokes
   with generous waits beats many rapid keystrokes.
4. **Show variety** — demonstrate 2–3 features of the scenario, not just
   scrolling. Navigate between controls, trigger category changes, etc.
5. **End with `Ctrl+Q`** — the standard Terminal.Gui quit key.

### Template Command

```powershell
$binary = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.exe"
$ks = '<your keystroke script here>'

tuirec record `
    --binary $binary `
    --args "run,<Scenario Name>" `
    --name <scenario-id> `
    --title "<Scenario Name> — <subtitle>" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 2000 `
    --cols 120 --rows 30 `
    --verbosity high `
    --open --copy
```

### Critical: `--kitty-keyboard` Decision

**Known bug ([gui-cs/tuirec#54](https://github.com/gui-cs/tuirec/issues/54)):**
tuirec currently encodes navigation keys (`CursorUp`, `CursorDown`, `CursorLeft`,
`CursorRight`, `PageUp`, `PageDown`, `Home`, `End`) incorrectly under
`--kitty-keyboard` — it sends fabricated CSI u codepoints that the Kitty spec
doesn't define. Terminal.Gui ignores or misinterprets these sequences.

**Workaround until fixed:**
- **Omit `--kitty-keyboard`** for demos that use navigation keys.
- **Add `--kitty-keyboard`** only when you need modifier disambiguation for
  non-navigation keys (`Ctrl+M` vs Enter, `Ctrl+I` vs Tab, `Ctrl+Q`, etc.)
  and the demo doesn't rely on arrow/page/home/end keys.

Once the bug is fixed, `--kitty-keyboard` should be the default for all
Terminal.Gui recordings (it provides cleaner modifier handling).

### `--args` for ScenarioRunner

The `--args` flag uses **comma-separated** values (not space-separated):

```powershell
--args "run,Character Map"      # Correct: two args ["run", "Character Map"]
--args "run Character Map"      # WRONG: one arg "run Character Map"
```

### PowerShell Quoting

Always assign keystrokes to a **single-quoted** `$ks` variable to preserve
backtick literals:

```powershell
# Correct — single quotes prevent PowerShell backtick interpolation:
$ks = 'wait:1000,`search text`,Enter,wait:500,Ctrl+Q'

# WRONG — PowerShell eats the backticks:
--keystrokes "wait:1000,`search text`,Enter"
```

### Example: Character Map Scenario

```powershell
$binary = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.exe"

# Scroll the char grid, switch to category list, browse categories, switch back
$ks = 'wait:1000,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,wait:1000,Shift+Tab,wait:500,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,wait:1000,Tab,wait:500,CursorLeft,CursorLeft,CursorLeft,CursorLeft,CursorLeft,CursorLeft,CursorLeft,CursorLeft,CursorLeft,CursorLeft,wait:1500,Ctrl+Q'

tuirec record `
    --binary $binary `
    --args "run,Character Map" `
    --name charmap `
    --title "Character Map — Unicode Viewer" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 2000 `
    --cols 120 --rows 30 `
    --open --copy
```

**Script breakdown:**

| Step | Tokens | What happens |
|------|--------|--------------|
| 1 | `wait:1000` | Let the CharMap UI render |
| 2 | `CursorDown` ×20 | Scroll through unicode characters in the grid |
| 3 | `wait:1000,Shift+Tab` | Pause, then move focus to category table |
| 4 | `CursorDown` ×10 | Browse down through unicode categories |
| 5 | `wait:1000,Tab` | Pause, then return focus to char grid |
| 6 | `CursorLeft` ×10 | Scroll horizontally to show different columns |
| 7 | `wait:1500,Ctrl+Q` | Final pause for viewer, then quit |

---

## Recording Individual View Sub-classes with EnableForDesign

(Coming soon — will use a dedicated design-mode runner that instantiates
a single View with `EnableForDesign()` and records its interactions.)

---

## Recording Standalone Example Apps

For apps in `Examples/` that are not UICatalog scenarios:

```powershell
$binary = "./Examples/<AppName>/bin/Release/net10.0/<AppName>.exe"
$ks = 'wait:1000,<keystrokes>,Ctrl+Q'

tuirec record `
    --binary $binary `
    --name <app-id> `
    --title "<App Name> Demo" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 2000 `
    --cols 120 --rows 30 `
    --open --copy
```

---

## Validation Checklist

After every recording, verify:

- [ ] `tuirec record` exited with code 0 and wrote both `.gif` and `.cast`.
- [ ] **Error check** — no errors in the cast:
  ```powershell
  Select-String -Path artifacts/<name>.cast -Pattern "error|unknown|not found|usage:" -CaseSensitive:$false
  ```
- [ ] **GIF is not blank** — file size > 100KB for a typical scenario recording.
      (A blank/static GIF is typically < 50KB.)
- [ ] **Visual check** — open the GIF (`--open` flag) and confirm:
  - The app content is visible (menu bar, controls, content).
  - The interaction sequence is visible (scrolling, focus changes, etc.).
  - The recording ends cleanly (no frozen frame or abrupt cutoff).
- [ ] **Output path is correct** — for docs assets, copy to `docfx/images/`.

---

## Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| Nav keys ignored with `--kitty-keyboard` | tuirec bug [#54](https://github.com/gui-cs/tuirec/issues/54) — sends wrong codepoints | Remove `--kitty-keyboard` |
| App doesn't quit | `Ctrl+Q` needs Kitty for disambiguation | Add `--kitty-keyboard` (Ctrl keys work correctly) |
| Blank frames at start | Startup too slow | Increase `--startup-delay` |
| Recording times out | App stuck / wrong keystrokes | Check with `--verbosity high`, fix script |
| `--binary` permission error | Relative path on Windows | Use `./` prefix or absolute path with forward slashes |
| Backtick text missing | PowerShell interpolation | Use single-quoted `$ks` variable |

---

## Agent Workflow Summary

When asked to record a scenario GIF:

1. **Build** — `dotnet build Examples/ScenarioRunner -c Release`
2. **Find scenario name** — `dotnet run --project Examples/ScenarioRunner -c Release --no-build -- list`
3. **Read `GetDemoKeyStrokes()`** — find it in the scenario source file
4. **Compose keystrokes** — translate to tuirec syntax, add waits, keep short
5. **Record** — `tuirec record --binary ... --args "run,<Name>" --keystrokes $ks ...`
6. **Validate** — error-grep the cast, check GIF file size, visual confirm
7. **If nav keys fail** — remove `--kitty-keyboard` and retry
8. **Report** — share the output paths and exact command used

---

## Reference

- **tuirec repo:** https://github.com/gui-cs/tuirec
- **Full keystroke syntax:** `tuirec agent-guide` (embeds the complete reference)
- **CLI flags:** `tuirec record --help`
- **ScenarioRunner:** `Examples/ScenarioRunner/` — CLI that runs individual UICatalog scenarios
