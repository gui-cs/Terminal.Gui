# Bug Report: Wide/Fullwidth Character Rendering Causes Display Tearing

## Summary

The terminal incorrectly renders wide (fullwidth) Unicode characters, advancing the cursor by only 1 column instead of the required 2. This causes all subsequent content on the line to shift left, breaking grid alignment and causing display "tearing" in TUI applications.

## Reproduction

```bash
git clone https://github.com/gui-cs/Terminal.Gui.git
cd Terminal.Gui/Examples/WideCharRepro
dotnet run
```

Or paste this minimal C# program (requires .NET 8+):

<details>
<summary>Minimal repro (click to expand)</summary>

```csharp
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine ("TEST: Emoji grid (each emoji = 2 columns)");
Console.WriteLine ("┌──┬──┬──┬──┬──┬──┬──┬──┐");
Console.WriteLine ("│😀│😁│😂│🤣│😄│😅│😆│😇│");
Console.WriteLine ("│──│──│──│──│──│──│──│──│");
Console.WriteLine ("│🐶│🐱│🐭│🐹│🐰│🦊│🐻│🐼│");
Console.WriteLine ("└──┴──┴──┴──┴──┴──┴──┴──┘");
Console.WriteLine ();
Console.WriteLine ("TEST: Column alignment (X should be at column 20)");
Console.WriteLine ("01234567890123456789X  <- 20 narrow (20×1=20)");
Console.WriteLine ("😀😁😂😃😄😅😆😇😈😉X  <- 10 emoji  (10×2=20)");
Console.WriteLine ("你好世界测试宽字符验X  <- 10 CJK    (10×2=20)");
```

</details>

## Expected Behavior

Each wide character (emoji, CJK ideograph, fullwidth form) should advance the cursor by **2 columns**. Grid separators (`│`) should be vertically aligned:

```
┌──┬──┬──┬──┬──┬──┬──┬──┐
│😀│😁│😂│🤣│😄│😅│😆│😇│
│──│──│──│──│──│──│──│──│
│🐶│🐱│🐭│🐹│🐰│🦊│🐻│🐼│
└──┴──┴──┴──┴──┴──┴──┴──┘
```

The `X` markers should vertically align at column 20:
```
01234567890123456789X
😀😁😂😃😄😅😆😇😈😉X
你好世界测试宽字符验X
```

## Actual Behavior

Wide characters advance the cursor by only 1 column. Grids misalign and text overlaps:

```
┌──┬──┬──┬──┬──┬──┬──┬──┐
│😀│😁│😂│🤣│😄│😅│😆│😇│   <- shifted left
│──│──│──│──│──│──│──│──│
│🐶│🐱│🐭│🐹│🐰│🦊│🐻│🐼│   <- shifted left
└──┴──┴──┴──┴──┴──┴──┴──┘
```

## Terminal Compatibility Matrix

| Terminal | Version | OS | Test 1 (Emoji) | Test 2 (CJK) | Test 3 (Mixed) | Test 4 (Grid) | Test 5 (Align) | Status |
|---------|---------|-----|:-:|:-:|:-:|:-:|:-:|--------|
| Alacritty | 0.13+ | cross | | | | | | _untested_ |
| Ghostty | — | macOS | ✅ | ✅ | ✅ | ✅ | ✅ | **PASS** |
| GitHub Copilot (terminal) | — | macOS | ❌ | ❌ | ❌ | ❌ | ❌ | **FAIL** |
| GitHub Copilot (terminal) | — | Windows | ❌ | ❌ | ❌ | ❌ | ❌ | **FAIL** |
| GNOME Terminal | 3.x | Linux | | | | | | _untested_ |
| iTerm2 | 3.5+ | macOS | ✅ | ✅ | ✅ | ✅ | ✅ | **PASS** |
| Kitty | 0.35+ | macOS | ✅ | ✅ | ✅ | ✅ | ✅ | **PASS** |
| Terminal.app | — | macOS | ✅ | ✅ | ✅ | ✅ | ✅ | **PASS** |
| Visual Studio 2026 (terminal) | — | Windows | ✅ | ✅ | ✅ | ✅ | ✅ | **PASS** |
| VS Code Insiders (terminal) | 1.x | Windows | ✅ | ✅ | ✅ | ✅ | ✅ | **PASS** |
| WezTerm | — | cross | | | | | | _untested_ |
| Windows Terminal | 1.22+ | Windows | ✅ | ✅ | ✅ | ✅ | ✅ | **PASS** |

> **To contributors**: please fill in your terminal's results and submit a PR or comment.

## Affected Unicode Ranges

- **Emoji** (U+1F600–U+1F64F, U+1F900–U+1F9FF, etc.)
- **CJK Unified Ideographs** (U+4E00–U+9FFF)
- **CJK Compatibility Ideographs** (U+F900–U+FAFF)
- **Fullwidth Forms** (U+FF01–U+FF60)
- Any codepoint with Unicode `East_Asian_Width` = `W` (Wide) or `F` (Fullwidth)

## Impact

This bug breaks any TUI application that renders wide characters in a grid or alongside narrow characters. Affected applications include:
- [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) (cross-platform .NET TUI toolkit)
- Any ncurses/curses-based app using CJK or emoji
- tmux, vim, etc. when displaying wide characters

## Relevant Standards

- [Unicode TR#11 – East Asian Width](https://www.unicode.org/reports/tr11/)
- [POSIX `wcwidth(3)`](https://pubs.opengroup.org/onlinepubs/9699919799/functions/wcwidth.html)
- [Unicode TR#51 – Emoji Presentation](https://www.unicode.org/reports/tr51/)

## Environment

- OS: [e.g., Windows 11 24H2]
- Terminal: [e.g., VS Code Insiders 1.x integrated terminal]
- Shell: [e.g., PowerShell 7.5]
- Font: [e.g., Cascadia Code NF]
- Locale: [e.g., en-US, UTF-8]

## Screenshots

<!-- Attach screenshots of correct (Windows Terminal) vs. broken (your terminal) output -->
