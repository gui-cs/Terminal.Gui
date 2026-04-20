# Terminal.Gui v2 — AI Agent Primer

> **This file is the canonical v1→v2 correction reference for all AI coding agents.**
> It is referenced by `.cursorrules`, `.windsurfrules`, `.aider.md`, `AGENTS.md`,
> `CLAUDE.md`, `.github/copilot-instructions.md`, and `llms.txt`.
>
> **If you are an AI agent generating Terminal.Gui code, read this file first.**

---

## CRITICAL: Discard v1 Training Data

Terminal.Gui v2 is a **complete rewrite**. If your training data is from before 2025,
most of what you "know" about Terminal.Gui is **wrong**. The API has fundamentally changed.

> **Do NOT use**: `Application.Init()`, `Application.Run()`, `Application.Shutdown()`,
> `Application.Top`, `Toplevel`, `Clicked`, `Bounds`, `LayoutStyle`, `NStack`,
> `RadioGroup`, `ColorScheme`, old mouse/keyboard APIs, `using Terminal.Gui;` (bare namespace).
>
> **Always read this file first.** When in doubt, check `docfx/apispec/` for current API.

---

## v1 → v2 Quick Corrections

| v1 (WRONG — do not use) | v2 (CORRECT) |
|---|---|
| `Application.Init ();` | `IApplication app = Application.Create ().Init ();` |
| `Application.Run ();` | `app.Run<MyWindow> ();` |
| `Application.Shutdown ();` | `app.Dispose ();` (use `using` pattern) |
| `Application.Top` | No global top — pass root view to `app.Run ()` |
| `new Toplevel ()` | Use `Runnable` subclass or `Window` |
| `using Terminal.Gui;` | `using Terminal.Gui.App;` / `Terminal.Gui.Views;` / etc. |
| `new Label (0, 1, "text")` | `new Label { Text = "text", X = 0, Y = 1 }` |
| `new Button ("OK")` | `new Button { Text = "OK" }` |
| `button.Clicked += ...` | `button.Accepting += (_, e) => { e.Handled = true; };` |
| `view.Bounds` | `view.Viewport` |
| `LayoutStyle.Computed` | Removed — all layout is declarative via `Pos`/`Dim` |
| `new RadioGroup (...)` | `new OptionSelector { ... }` |
| `Colors.ColorSchemes ["name"]` | `Schemes.Resolve ("name")` or use `Scheme` directly |
| `Application.RequestStop ()` | `App!.RequestStop ()` (from inside a `Runnable`) |
| `Pos.At (n)` / `Pos.Left (v)` | Assign integers directly: `X = 5;` (implicit conversion) |

---

## Correct Minimal App (v2)

```csharp
using Terminal.Gui.App;
using Terminal.Gui.Views;

IApplication app = Application.Create ().Init ();
app.Run<MainWindow> ();
app.Dispose ();

public sealed class MainWindow : Runnable
{
    public MainWindow ()
    {
        Title = "My App (Esc to quit)";

        Button button = new ()
        {
            Text = "Click Me",
            X = Pos.Center (),
            Y = Pos.Center ()
        };

        button.Accepting += (_, e) =>
        {
            MessageBox.Query (App!, "Hello", "Button was clicked!", "OK");
            e.Handled = true;
        };

        Add (button);
    }
}
```

---

## Key Namespaces (v2)

| Namespace | Contents |
|-----------|----------|
| `Terminal.Gui.App` | `Application`, `IApplication`, `Clipboard`, session management |
| `Terminal.Gui.Views` | All controls: `Button`, `Label`, `TextField`, `ListView`, `Dialog`, etc. |
| `Terminal.Gui.ViewBase` | `View`, `Pos`, `Dim`, adornments (`Border`, `Margin`, `Padding`) |
| `Terminal.Gui.Drawing` | `Color`, `Attribute`, `Scheme`, `LineCanvas`, `Glyphs` |
| `Terminal.Gui.Input` | `Key`, `KeyCode`, `Command`, `KeyBindings`, `MouseBindings` |
| `Terminal.Gui.Text` | `TextFormatter`, `TextDirection` |
| `Terminal.Gui.Configuration` | `ConfigurationManager`, themes |

---

## Common v2 Patterns

### Dialog with Result

```csharp
public sealed class ConfirmDialog : Runnable<bool>
{
    public ConfirmDialog (string message)
    {
        Title = "Confirm";
        Width = 40;
        Height = 8;

        Label label = new () { Text = message, X = Pos.Center (), Y = 1 };

        Button yesButton = new () { Text = "Yes", Y = 4, X = Pos.Center () - 6 };
        yesButton.Accepting += (_, e) =>
        {
            Result = true;
            App!.RequestStop ();
            e.Handled = true;
        };

        Button noButton = new () { Text = "No", Y = 4, X = Pos.Center () + 2 };
        noButton.Accepting += (_, e) =>
        {
            Result = false;
            App!.RequestStop ();
            e.Handled = true;
        };

        Add (label, yesButton, noButton);
    }
}
```

### Layout with Pos/Dim

```csharp
// Absolute position
view.X = 5;
view.Y = 2;

// Centered
view.X = Pos.Center ();
view.Y = Pos.Center ();

// Relative to another view
view.X = Pos.Right (otherView) + 1;
view.Y = Pos.Bottom (otherView);

// Percentage-based
view.Width = Dim.Percent (50);
view.Height = Dim.Fill ();  // fill remaining space

// Content-based sizing
view.Width = Dim.Auto ();
```

### Event Handling (Cancellable Workflow Pattern)

```csharp
// Button click
button.Accepting += (_, e) =>
{
    // Handle button press
    e.Handled = true;
};

// Text changed
textField.HasFocusChanged += (_, e) =>
{
    // React to focus change
};

// Key binding
view.KeyBindings.Add (Key.F5, Command.Refresh);
```

---

## Gotchas for AI Agents

1. **Space before `()` and `[]`** — This codebase uses `Method ()` not `Method()`,
   and `array [i]` not `array[i]`. This is the #1 formatting mistake agents make.
2. **No `var`** — Use explicit types except for built-in types (`int`, `string`, `bool`, etc.).
3. **Use `new ()`** — Target-typed new: `Button btn = new ()` not `Button btn = new Button ()`.
4. **Collection expressions** — Use `[...]` not `new List<T> { ... }`.
5. **SubView/SuperView** — Never say "child", "parent", or "container". Use SubView/SuperView.
6. **`Accepting` not `Clicked`** — The `Clicked` event does not exist in v2.
7. **`Runnable` not `Toplevel`** — `Toplevel` does not exist in v2. Use `Runnable` or `Window`.
8. **Instance-based app** — Use `Application.Create ().Init ()` to get an `IApplication` instance.
   Do not use the static `Application.Init ()` / `Application.Run ()` / `Application.Shutdown ()` pattern.
9. **Always set `e.Handled = true`** in `Accepting` event handlers.
10. **Use `App!.RequestStop ()`** to close a window from inside a `Runnable`, not `Application.RequestStop ()`.

---

## Where to Find More

| Resource | Path |
|----------|------|
| Compressed API docs | `docfx/apispec/namespace-*.md` |
| Deep-dive docs | `docfx/docs/` |
| Common UI patterns | `.claude/cookbook/common-patterns.md` |
| App building guide | `.claude/tasks/build-app.md` |
| Working examples | `Examples/Example/` (minimal), `Examples/UICatalog/` (comprehensive) |
| Full agent instructions | `AGENTS.md` |
