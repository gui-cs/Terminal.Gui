# InlineCLI Example

Demonstrates Terminal.Gui's **Inline** rendering mode, which renders UI inline within
the primary (scrollback) terminal buffer instead of switching to the alternate screen buffer.

## How It Works

Setting `Application.AppModel = AppModel.Inline` before calling `Init()` tells
Terminal.Gui to:

1. **Skip the alternate screen buffer** — no `CSI ?1049h` is emitted.
2. **Render in the primary buffer** — output appears below the shell prompt.
3. **Exit cleanly** — no `CSI ?1049l` is emitted; the shell prompt appears
   naturally below the rendered content, which stays in scrollback history.

## Running

```bash
dotnet run --project Examples/InlineCLI
```

## API

```csharp
// Set Inline mode BEFORE Init
Application.AppModel = AppModel.Inline;

IApplication app = Application.Create ().Init ();
app.Run<MyView> ();
app.Dispose ();
```

## Inspiration

This mode is inspired by tools like:
- [Claude Code CLI](https://docs.anthropic.com/en/docs/claude-code)
- [GitHub Copilot CLI](https://docs.github.com/en/copilot/github-copilot-in-the-cli)
- [Ink](https://github.com/vadimdemedes/ink) (React for terminals)
