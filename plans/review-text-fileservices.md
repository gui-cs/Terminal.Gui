# v2.0.0 Release Candidate Code Review: Text, FileServices, Time, Testing

## P0 Critical Ship Stoppers

### [P0] Display width calculation error in vertical text formatting
**File:** Terminal.Gui/Terminal.Gui/Text/TextFormatter.cs:556
**Issue:** In `FormatAndGetSize()`, when `IsVerticalDirection(Direction)` is true, the height is calculated as `lines.Max(static line => line.Length)`, using character count instead of grapheme/column count. For text with CJK characters, emoji, combining marks, or wide glyphs, this will return incorrect height. Line 555 correctly uses `GetColumnsRequiredForVerticalText()` for width, but line 556 breaks the pattern.
**Suggested fix:** Change line 556 to `height = lines.Max(static line => line.GetColumns());` to measure actual display columns, not character count.

### [P0] Potential `null` dereference in text direction handling
**File:** Terminal.Gui/Terminal.Gui/Text/TextFormatter.cs:2264
**Issue:** In `GetColumnsRequiredForVerticalText()`, the code checks `if (strings.Length > 0)` then accesses `GraphemeHelper.GetGraphemes(strings)`. However, `strings.Length` counts UTF-16 chars, not graphemes. Empty grapheme arrays (zero-width combining sequences) could pass this check but have Length > 0, or conversely non-empty graphemes could be missed. This is a correctness bug for RTL/vertical text with combining marks.
**Suggested fix:** Check grapheme count directly: `if (GraphemeHelper.GetGraphemeCount(strings) > 0)` or use LINQ `.Any()` after calling `GetGraphemes()`.

## P1 Critical but Not Ship Stoppers

### [P1] Justification algorithm breaks with combining marks
**File:** Terminal.Gui/Terminal.Gui/Text/TextFormatter.cs:1948-1997 (Justify method)
**Issue:** The `Justify()` method uses `text.Split(' ')` to break into words, then reconstructs with padding spaces. This naively splits on space characters without considering grapheme clusters. If text contains combining marks or ZWJ sequences around spaces, the reconstruction will corrupt them. Additionally, spacing calculation doesn't account for zero-width marks in the original text.
**Suggested fix:** Use `GraphemeHelper.GetGraphemes()` to preserve grapheme integrity during split/rejoin. Account for zero-width marks when calculating space distribution.

### [P1] Text wrapping doesn't preserve zero-width grapheme attachment
**File:** Terminal.Gui/Terminal.Gui/Text/TextFormatter.cs:1614-1632
**Issue:** In `WordWrapText()`, lines are split on spaces without ensuring that zero-width combining marks stay attached to their base character. A combining accent on a space will be separated from its base when the line wraps.
**Suggested fix:** When wrapping, scan ahead to include any trailing zero-width marks with the word, or better, iterate by grapheme cluster and use grapheme-aware word boundaries.

### [P1] Path traversal risk in FileSystemTreeBuilder
**File:** Terminal.Gui/Terminal.Gui/FileServices/FileSystemTreeBuilder.cs:53
**Issue:** The `TryGetChildren()` method calls `dir.GetFileSystemInfos()` without validating the path or checking for symlink loops. On Unix/Linux, following symlinks in directory traversal can cause infinite loops. No explicit loop detection or depth limit.
**Suggested fix:** Add symlink loop detection (compare inodes or track visited paths), set a maximum recursion depth, or document that callers must handle this via IFileSystemInfo abstraction.

### [P1] String comparison case sensitivity inconsistency in DefaultSearchMatcher
**File:** Terminal.Gui/Terminal.Gui/FileServices/DefaultSearchMatcher.cs:18-22
**Issue:** Search uses `StringComparison.OrdinalIgnoreCase` but this is culture-invariant. For non-ASCII file names in locales where case folding differs (e.g., Turkish dotless-i), search may miss matches or find false positives. No culture parameter passed; behavior depends on current culture indirectly.
**Suggested fix:** Document the invariant comparison, or add a culture parameter to `Initialize()` if locale-sensitive matching is needed.

### [P1] Array Pool asymmetry in TextFormatter Draw methods
**File:** Terminal.Gui/Terminal.Gui/Text/TextFormatter.cs:128-145, 971-988
**Issue:** Both `Draw()` and `GetDrawRegion()` use `ArrayPool<string>.Shared` for grapheme storage. If either method throws before the finally block, the rented array is not returned. A catastrophic GC event could occur if many exceptions are thrown during drawing.
**Suggested fix:** Wrap pool.Rent in try-finally at acquisition, or use a using statement with a helper that implements IDisposable.

### [P1] Null checks missing in FindHotKey rune iteration
**File:** Terminal.Gui/Terminal.Gui/Text/TextFormatter.cs:2472, 2496
**Issue:** In `FindHotKey()`, the code checks `(char)c.Value != 0xFFFD` but this is indirect and fragile. A malformed rune could cause issues. No validation of `text.EnumerateRunes()` output.
**Suggested fix:** Use `Rune.IsValid()` or explicit checks instead of character value comparisons.

## P2 Nice to Fix

### [P2] Unused variable in Justify method
**File:** Terminal.Gui/Terminal.Gui/Text/TextFormatter.cs:1980-1981
**Issue:** Inside the loop at line 1980-1981, the loop `for (var i = 0; i < 1; i++)` is useless; it always executes once. This is a code smell suggesting incomplete refactoring.
**Suggested fix:** Replace with direct `s.Append(spaceChar)` call; remove the loop.

### [P2] Inconsistent string builder usage in ReplaceCRLFWithSpace
**File:** Terminal.Gui/Terminal.Gui/Text/TextFormatter.cs:1317-1373
**Issue:** Both `StripCRLF()` and `ReplaceCRLFWithSpace()` are nearly identical; code is duplicated. The pattern could be unified with a callback or predicate.
**Suggested fix:** Extract common logic into a helper method to reduce duplication and maintenance burden.

### [P2] ModuleInitializer executes ConfigurationManager.Initialize unconditionally
**File:** Terminal.Gui/Terminal.Gui/ModuleInitializers.cs:22-25
**Issue:** The module initializer calls `ConfigurationManager.Initialize()` on every assembly load. While documented as safe (no file I/O), this is a side effect at load time. In some hosting contexts (e.g., AOT, trimming, plugin scenarios), this may cause issues.
**Suggested fix:** Document that this initializer runs unconditionally and will always be executed; consider making it optional or lazy if configuration can be initialized on first access instead.

### [P2] VirtualTimeProvider fires timers eagerly during Advance
**File:** Terminal.Gui/Terminal.Gui/Time/VirtualTimeProvider.cs:24-27
**Issue:** When `Advance()` is called, all timers that should have fired are triggered immediately via `.ToList()` snapshot. If a timer's callback schedules another timer, the new timer won't fire until the next `Advance()` call, even if the time has passed. This may be intentional but is a subtle timing trap.
**Suggested fix:** Document this behavior clearly, or consider a recursive iteration approach if re-checking is desired.

### [P2] GlobalResources uses null! on non-null reference parameters
**File:** Terminal.Gui/Terminal.Gui/Resources/GlobalResources.cs:29, 68
**Issue:** Methods `GetObject()` and `GetString()` declare `culture = null!` as default, which suppresses warnings but may indicate API design confusion. The nullable annotations are inconsistent.
**Suggested fix:** Use `culture = null` (true nullable) and handle null explicitly, or remove the null! to enforce non-null culture parameter.

### [P2] InputInjector lacks null safety in VirtualTimeProvider cast
**File:** Terminal.Gui/Terminal.Gui/Testing/InputInjector.cs:213-216
**Issue:** In `InjectSequence()`, the code checks `if (evt.Delay.HasValue && _timeProvider is VirtualTimeProvider vtp)` but provides no fallback if the provider is not virtual. A SystemTimeProvider will silently ignore delays, which may confuse tests.
**Suggested fix:** Either throw an exception if delays are used with non-virtual providers, or document this limitation clearly.

---

## Summary

**Critical Issues (P0-P1):** 6 issues affecting grapheme handling in text measurement, text wrapping, justification, file system traversal, and searching.

**Nice-to-fix (P2):** 5 issues related to code clarity, duplication, and side effects.

**Unicode/Grapheme Compliance:** The codebase largely follows the grapheme-aware patterns per `.claude/rules/unicode-graphemes.md`, but two critical violations exist in `FormatAndGetSize()` (line 556) and potential issues in vertical text width calculation when combining marks are present.

**Testing Isolation:** Testing subsystem properly avoids static Application state in core classes; InputInjector uses dependency injection for ITimeProvider and correctly allows both SystemTimeProvider and VirtualTimeProvider.

**API Stability:** Public APIs are stable for v2.0.0. ModuleInitializer side effects are documented and safe but worth auditing in AOT/trimming scenarios.
