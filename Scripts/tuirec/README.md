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
$ks = 'wait:1200,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,wait:800,Shift+Tab,wait:400,B,o,x,wait:1800,A,r,r,wait:1800,Tab,wait:400,CursorDown,CursorDown,CursorDown,wait:400,Shift+F10,wait:1500,Escape,wait:400,Escape'

tuirec record `
    --binary $binary `
    --args "run,Character Map" `
    --name CharacterMap `
    --title "Character Map" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 1500 `
    --cols 120 --rows 30 `
    --open --copy
```

Output: `artifacts/CharacterMap.gif` and `artifacts/CharacterMap.cast`.

Copy the GIF to the scenario directory:
```powershell
Copy-Item artifacts/CharacterMap.gif Examples/UICatalog/Scenarios/CharacterMap/CharacterMap.gif
```

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
5. **End with `Escape`** — the default Terminal.Gui quit key.
6. **Avoid wide glyphs** — Emoji and CJK characters cause misaligned rendering
   in terminal recordings (agg renders each cell as monospace but wide glyphs
   consume 2 cells). Prefer categories with single-width characters (Arrows,
   Box Drawing, Block Elements, Mathematical Operators, etc.).

### Template Command

```powershell
$binary = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.exe"
$ks = '<your keystroke script here>'

tuirec record `
    --binary $binary `
    --args "run,<Scenario Name>" `
    --name <ScenarioName> `
    --title "<Scenario Name>" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 2000 `
    --cols 120 --rows 30 `
    --verbosity high `
    --open --copy

# Copy GIF to scenario directory
Copy-Item artifacts/<ScenarioName>.gif Examples/UICatalog/Scenarios/<ScenarioDir>/<ScenarioName>.gif
```

### Output File Placement

GIFs live **alongside the `.cs` file they document**:

| What | Where |
|------|-------|
| Scenario in a subdirectory | `Examples/UICatalog/Scenarios/<ScenarioDir>/<ScenarioName>.gif` |
| Scenario directly in `Scenarios/` | `Examples/UICatalog/Scenarios/<ScenarioName>.gif` |
| View-derived class | Same folder as the View's `.cs` file (e.g. `Terminal.Gui/Views/Button.gif`) |

Use `--name <ScenarioName>` (PascalCase matching the class name) so the output
file is named correctly. The `--name` value determines the artifact filenames.

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
$ks = 'wait:1000,`search text`,Enter,wait:500,Escape'

# WRONG — PowerShell eats the backticks:
--keystrokes "wait:1000,`search text`,Enter"
```

### Example: Character Map Scenario

```powershell
$binary = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.exe"

# Browse categories (Box Drawing, Arrows), show context menu on a glyph
$ks = 'wait:1200,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,CursorDown,wait:800,Shift+Tab,wait:400,B,o,x,wait:1800,A,r,r,wait:1800,Tab,wait:400,CursorDown,CursorDown,CursorDown,wait:400,Shift+F10,wait:1500,Escape,wait:400,Escape'

tuirec record `
    --binary $binary `
    --args "run,Character Map" `
    --name CharacterMap `
    --title "Character Map" `
    --keystrokes $ks `
    --startup-delay 2000 `
    --drain 1500 `
    --cols 120 --rows 30 `
    --open --copy
```

**Script breakdown:**

| Step | Tokens | What happens |
|------|--------|--------------|
| 1 | `wait:1200` | Let the CharMap UI fully render |
| 2 | `CursorDown` ×8 | Scroll through the initial unicode code points |
| 3 | `wait:800,Shift+Tab` | Pause, then move focus to category table |
| 4 | `B,o,x` | Type "Box" — CollectionNavigator jumps to "Box Drawing" |
| 5 | `wait:1800` | Pause so viewer sees box-drawing characters |
| 6 | `A,r,r` | Type "Arr" — jumps to "Arrows" category |
| 7 | `wait:1800` | Pause so viewer sees arrow characters |
| 8 | `Tab` | Return focus to charmap grid |
| 9 | `CursorDown` ×3 | Navigate to a glyph |
| 10 | `Shift+F10` | Open context menu (Copy Glyph / Copy Code Point) |
| 11 | `wait:1500,Escape` | Let viewer see the menu, then dismiss |
| 12 | `Escape` | Quit |

**Key techniques demonstrated:**
- **CollectionNavigator typing** — type category name prefixes to jump directly
  (much better than scrolling through dozens of categories with arrow keys)
- **Context menu** — `Shift+F10` (the `PopoverMenu.DefaultKey`) shows the
  right-click menu on the selected glyph
- **Generous waits** — 1500–1800ms between feature demonstrations so viewers
  can absorb each state change

---

## Recording Individual View Sub-classes with EnableForDesign

(Coming soon — will use a dedicated design-mode runner that instantiates
a single View with `EnableForDesign()` and records its interactions.)

---

## Recording Standalone Example Apps

For apps in `Examples/` that are not UICatalog scenarios:

```powershell
$binary = "./Examples/<AppName>/bin/Release/net10.0/<AppName>.exe"
$ks = 'wait:1000,<keystrokes>,Escape'

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
- [ ] **Output path is correct** — scenario GIFs go with their scenario code:
  ```
  Examples/UICatalog/Scenarios/<ScenarioDir>/<ScenarioName>.gif
  ```

---

## Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| Wide glyphs misaligned in GIF | Emoji/CJK chars are 2-cell wide; agg renders per-cell | Avoid emoji/CJK categories; use single-width ranges (Arrows, Box Drawing, etc.) |
| Nav keys ignored with `--kitty-keyboard` | tuirec bug [#54](https://github.com/gui-cs/tuirec/issues/54) — sends wrong codepoints | Remove `--kitty-keyboard` |
| App doesn't quit | Wrong quit key or key not delivered | Use `Escape` (the default quit key); check `--kitty-keyboard` interaction |
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
