# mdv - A Terminal.Gui-based Markdown viewer

## Usage

```
mdv <file.md> [file2.md ...]               # Inline mode (default)
mdv --full-screen <file.md> [file2.md ...]  # Full-screen interactive mode
```

Wildcards are supported: `mdv *.md`, `mdv docs/*.md`.


| Mode | Command Line | How It Works         |
|---------|---------------|-----------------|
| **Inline (default)** |  | Renders the Markdown content inline in the terminal buffer using `AppModel.Inline` and exits. The rendered content remains in scrollback history. |
| **Full Screen**      | `--full-screen` or `-f` |Opens an interactive viewer with rendered Markdown with auto scrollbars (vertical + horizontal), a **StatusBar** with Quit, Content Width control, line count, file size, status, and spinner, and a **File selector** dropdown when viewing multiple files |

### Examples

```bash
# View a single file inline
dotnet run --project Examples/mdv -- README.md

# View in full-screen mode
dotnet run --project Examples/mdv -- --full-screen README.md

# View multiple files with a file selector dropdown
dotnet run --project Examples/mdv -- -f *.md
```
