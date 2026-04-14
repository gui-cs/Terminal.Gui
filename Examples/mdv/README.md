# mdv

A Terminal.Gui-based Markdown viewer for the terminal.

## Usage

```
mdv <file.md> [file2.md ...]               # Inline mode (default)
mdv --full-screen <file.md> [file2.md ...]  # Full-screen interactive mode
```

Wildcards are supported: `mdv *.md`, `mdv docs/*.md`.

### Inline Mode (default)

Renders the Markdown content inline in the terminal buffer using `AppModel.Inline` and exits.
The rendered content remains in scrollback history.

### Full-Screen Mode (`--full-screen` or `-f`)

Opens an interactive viewer with:

- Rendered Markdown with auto scrollbars (vertical + horizontal)
- **StatusBar** with Quit, Content Width control, line count, file size, status, and spinner
- **File selector** dropdown when viewing multiple files

### Examples

```bash
# View a single file inline
dotnet run --project Examples/mdv -- README.md

# View in full-screen mode
dotnet run --project Examples/mdv -- --full-screen README.md

# View multiple files with a file selector dropdown
dotnet run --project Examples/mdv -- -f *.md
```
