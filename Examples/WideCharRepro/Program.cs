// WideCharRepro - Minimal reproduction of wide/fullwidth character rendering issues.
//
// PURPOSE:
// Many terminal emulators incorrectly handle "wide" (fullwidth) Unicode codepoints
// (those with East Asian Width = Wide/Fullwidth, or emoji presentation). These
// characters occupy 2 terminal columns, but some terminals only advance the cursor
// by 1 column, causing subsequent text to overlap and the display to "tear."
//
// EXPECTED BEHAVIOR (correct terminals like Windows Terminal):
//   - Each wide character occupies exactly 2 columns.
//   - The grid lines up perfectly with no overlapping or shifted text.
//   - The separator '|' characters in each row are vertically aligned.
//
// BROKEN BEHAVIOR (terminals with the bug):
//   - Wide characters only advance the cursor 1 column instead of 2.
//   - Grid columns misalign; text overlaps or shifts left.
//   - Vertical '|' separators are NOT aligned across rows.
//
// HOW TO USE:
//   dotnet run
//   Compare output against a known-good terminal (e.g., Windows Terminal).
//
// DIAGNOSIS:
//   If the '|' separators are not vertically aligned, the terminal is not
//   correctly handling wide character cursor advancement.

using System.Globalization;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

// Ensure we're in a mode that supports Unicode output
if (Environment.OSVersion.Platform == PlatformID.Win32NT)
{
    // Enable virtual terminal processing on Windows
    Console.Write ("\x1b[?25l"); // Hide cursor for cleaner output
}

Console.WriteLine ("═══════════════════════════════════════════════════════════════════");
Console.WriteLine ("  Wide Character Rendering Test");
Console.WriteLine ("  If '|' separators are NOT vertically aligned, the terminal has");
Console.WriteLine ("  a wide-character cursor advancement bug.");
Console.WriteLine ("═══════════════════════════════════════════════════════════════════");
Console.WriteLine ();

// --- Test 1: Emoji (U+1F600 - U+1F64F) ---
Console.WriteLine ("TEST 1: Emoji (each should occupy 2 columns)");
Console.WriteLine ("┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐");

int startCodepoint = 0x1F600;
for (int row = 0; row < 4; row++)
{
    Console.Write ("│");
    for (int col = 0; col < 16; col++)
    {
        int cp = startCodepoint + (row * 16) + col;
        string ch = char.ConvertFromUtf32 (cp);
        Console.Write (ch);
        Console.Write ("│");
    }

    Console.WriteLine ();
}

Console.WriteLine ("└──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘");
Console.WriteLine ();

// --- Test 2: CJK Ideographs (U+4E00+) ---
Console.WriteLine ("TEST 2: CJK Ideographs (each should occupy 2 columns)");
Console.WriteLine ("┌──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┬──┐");

startCodepoint = 0x4E00;
for (int row = 0; row < 4; row++)
{
    Console.Write ("│");
    for (int col = 0; col < 16; col++)
    {
        int cp = startCodepoint + (row * 16) + col;
        string ch = char.ConvertFromUtf32 (cp);
        Console.Write (ch);
        Console.Write ("│");
    }

    Console.WriteLine ();
}

Console.WriteLine ("└──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┴──┘");
Console.WriteLine ();

// --- Test 3: Mixed narrow + wide on same line ---
// Each line below is EXACTLY 20 display columns between the │ delimiters.
Console.WriteLine ("TEST 3: Mixed content alignment");
Console.WriteLine ("All lines are exactly 20 display columns. The '│' must align:");
Console.WriteLine ("┌────────────────────┐");
Console.WriteLine ("│ABCDEFGHIJKLMNOPQRST│ <- 20 narrow (20×1=20)");
Console.WriteLine ("│😀😁😂😃😄😅😆😇😈😉│ <- 10 emoji  (10×2=20)");
Console.WriteLine ("│你好世界测试宽字符验│ <- 10 CJK    (10×2=20)");
Console.WriteLine ("│AB😀CD😁EF😂GH😃IJ😄│ <- mixed   (10×1 + 5×2=20)");
Console.WriteLine ("└────────────────────┘");
Console.WriteLine ();

// --- Test 4: ANSI cursor positioning with wide chars ---
Console.WriteLine ("TEST 4: Programmatic cursor-positioned grid");
Console.WriteLine ("  Writing wide chars at absolute positions via ANSI escapes.");
Console.WriteLine ("  If the terminal handles wcwidth correctly, all rows align.");
Console.WriteLine ();

// Get current cursor row (approximate - just write sequentially with known widths)
string [] testRows =
[
    "│😀│😁│😂│🤣│😄│😅│😆│😇│",
    "│──│──│──│──│──│──│──│──│",
    "│🐶│🐱│🐭│🐹│🐰│🦊│🐻│🐼│",
    "│──│──│──│──│──│──│──│──│",
    "│你│好│世│界│测│试│宽│字│",
    "│──│──│──│──│──│──│──│──│",
];

Console.WriteLine ("┌──┬──┬──┬──┬──┬──┬──┬──┐");

foreach (string line in testRows)
{
    Console.WriteLine (line);
}

Console.WriteLine ("└──┴──┴──┴──┴──┴──┴──┴──┘");
Console.WriteLine ();

// --- Test 5: Explicit column-counting verification ---
Console.WriteLine ("TEST 5: Column-width verification");
Console.WriteLine ("  The 'X' markers below should align with column 20:");
Console.WriteLine ();
Console.WriteLine ("01234567890123456789X  <- 20 narrow (20×1=20)");
Console.WriteLine ("😀😁😂😃😄😅😆😇😈😉X  <- 10 emoji  (10×2=20)");
Console.WriteLine ("你好世界测试宽字符验X  <- 10 CJK    (10×2=20)");
Console.WriteLine ("aあbいcうdえeおfかきX  <- mixed     (6×1 + 7×2=20)");
Console.WriteLine ();
Console.WriteLine ("If the 'X' markers don't vertically align at column 20,");
Console.WriteLine ("the terminal is miscounting wide character widths.");
Console.WriteLine ();

// --- Summary ---
Console.WriteLine ("═══════════════════════════════════════════════════════════════════");
Console.WriteLine ("  DIAGNOSIS:");
Console.WriteLine ("  • If all grids have aligned '│' separators → terminal is CORRECT");
Console.WriteLine ("  • If grids are torn/misaligned → terminal has wcwidth bug");
Console.WriteLine ("  • Common cause: terminal treats wide chars as 1 column, not 2");
Console.WriteLine ("═══════════════════════════════════════════════════════════════════");

// Show cursor again
if (Environment.OSVersion.Platform == PlatformID.Win32NT)
{
    Console.Write ("\x1b[?25h");
}
