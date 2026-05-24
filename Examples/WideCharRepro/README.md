# Wide Character Rendering Reproduction

A minimal reproduction app demonstrating incorrect wide/fullwidth Unicode character rendering in terminal emulators.

## The Problem

Terminal emulators must advance the cursor by **2 columns** for "wide" Unicode characters (East Asian Width = Wide/Fullwidth, emoji with presentation selectors). Some terminals incorrectly advance by only 1 column, causing:

- Grid misalignment / display "tearing"
- Overlapping text
- Broken TUI (text user interface) applications

## Screenshots

### Correct rendering (Windows Terminal)

All grid separators (`│`) are vertically aligned. Each wide character occupies exactly 2 terminal columns.

![Correct rendering](correct.png)

### Broken rendering (affected terminals)

Grid separators are misaligned. Wide characters only advance the cursor by 1 column, causing all subsequent content on the line to shift left.

![Broken rendering](broken.png)

## Running the Reproduction

```bash
dotnet run
```

**Requirements:** .NET 10 SDK (or change `TargetFramework` in `.csproj` to your installed version, e.g., `net8.0` or `net9.0`).

## What to Look For

1. **Test 1–2 (Emoji / CJK grids):** The `│` separators should form perfectly vertical columns.
2. **Test 3 (Mixed content):** Lines with mixed narrow + wide characters should fit within the box.
3. **Test 4 (Programmatic grid):** Another grid alignment test with diverse wide characters.
4. **Test 5 (Column verification):** The `X` markers should all appear at column 20.

If any of these are misaligned, the terminal has a wide-character width calculation bug.

## Technical Details

Wide characters affected:
- **Emoji** (U+1F600–U+1F64F, U+1F900–U+1F9FF, etc.)
- **CJK Unified Ideographs** (U+4E00–U+9FFF)
- **CJK Compatibility Ideographs** (U+F900–U+FAFF)
- **Fullwidth Forms** (U+FF01–U+FF60)
- Any character with `East_Asian_Width` property = `W` (Wide) or `F` (Fullwidth)

The terminal must use the Unicode `East_Asian_Width` property (or an equivalent wcwidth implementation) to determine cursor advancement after printing each character.

## Related Standards

- [Unicode TR#11 – East Asian Width](https://www.unicode.org/reports/tr11/)
- [POSIX `wcwidth(3)`](https://pubs.opengroup.org/onlinepubs/9699919799/functions/wcwidth.html)
- [Unicode Emoji Presentation](https://www.unicode.org/reports/tr51/)
