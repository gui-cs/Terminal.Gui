# mdv - A Terminal.Gui-based Markdown viewer

## Usage

```
mdv <file.md> [file2.md ...]              # Full-screen interactive mode (default)
mdv --print <file.md> [file2.md ...]      # Print mode: renders to terminal and exits
```

Wildcards are supported: `mdv *.md`, `mdv docs/*.md`.


| Mode | Command Line | How It Works         |
|---------|---------------|-----------------|
| **Full Screen (default)** |  | Opens an interactive viewer with rendered Markdown with auto scrollbars (vertical + horizontal), a **StatusBar** with Quit, Content Width control, line count, file size, status, and spinner, and a **File selector** dropdown when viewing multiple files |
| **Print**      | `--print` or `-p` | Renders the Markdown content inline in the terminal buffer and exits. The rendered content remains in scrollback history. |

### Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--print` | `-p` | Print mode: renders to terminal and exits |
| `--theme <ThemeName>` | `-t` | Syntax-highlighting theme (default: `DarkPlus`) |

### Examples

```bash
# View a single file in full-screen mode (default)
dotnet run --project Examples/mdv -- README.md

# Print rendered markdown to terminal and exit
dotnet run --project Examples/mdv -- --print README.md

# View multiple files with a file selector dropdown
dotnet run --project Examples/mdv -- *.md

# Print with a specific theme
dotnet run --project Examples/mdv -- -p -t Monokai README.md
```
