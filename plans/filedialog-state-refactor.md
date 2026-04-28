# FileDialog State Refactor & Simplification Plan

## Problem Statement

PR #5090 enabled `#nullable enable` on `FileDialogState` and removed the virtual-call-in-constructor
pattern, but introduced regressions. Additionally, the FileDialog subsystem has significant
pre-v2 over-engineering that should be cleaned up.

---

## Part 1: Fix State/SearchState Architecture (Bug Fixes)

### 1.1 Eager enumeration in `FileDialogState` constructor (Performance)
**Before (develop):** Base constructor called `RefreshChildren()` (virtual). `SearchState` overrode
it as a no-op, avoiding directory enumeration.

**After (PR):** Constructor inlines `Children = GetChildren(Directory).ToArray()` directly.
`SearchState` can't prevent this — it always pays for a full directory enumeration it immediately
discards.

### 1.2 Triple enumeration in `SearchState` constructor (Performance)
`SearchState` constructor currently does:
1. `base(dir, parent)` → enumerates directory (waste — immediately discarded)
2. `Children = []` → discards result from step 1
3. `BeginSearch()` → starts async recursive search
4. `RefreshChildren()` → re-enumerates directory **again** (waste)

### 1.3 Nullable issue in `AutocompleteFilepathContext.cs`
`validSuggestions` is `string?[]` but passed to `new Suggestion(string, string, string)`.

### Fix:
- Add `protected` skip-enumeration constructor: `FileDialogState(dir, parent, skipEnumeration: true)`
- `SearchState` calls that ctor, owns its own Children lifecycle
- Restore `RefreshChildren` as `internal virtual`; `SearchState` overrides as no-op
- Remove spurious `RefreshChildren()` in SearchState ctor
- Fix nullable with `.OfType<string>()`

---

## Part 2: Dead Code Removal

### 2.1 `FileSystemInfoStats.IsExecutable()` — REMOVE
- Never called anywhere in codebase
- Along with `_executableExtensions` static list
- File: `Terminal.Gui/FileServices/FileSystemInfoStats.cs:23,65-73`

### 2.2 `FileSystemInfoStats.IsImage()` — REMOVE
- Never called anywhere in codebase
- Along with `_imageExtensions` static list
- File: `Terminal.Gui/FileServices/FileSystemInfoStats.cs:25-33,75-82`

### 2.3 `FileSystemTreeBuilder.Sorter` property — REMOVE
- Always set to `this` in constructor, never customized
- Inline `this` at the one usage site (`GetChildren` → `OrderBy(k => k, this)`)
- File: `Terminal.Gui/FileServices/FileSystemTreeBuilder.cs:9,15,48`


## Part 5: Unit Tests

### New tests needed (in `Tests/UnitTestsParallelizable`):

| Test | What it verifies |
|------|-----------------|
| `FileDialogState_Constructor_EnumeratesChildren` | Standard ctor populates Children from directory |
| `FileDialogState_SkipEnumeration_ChildrenEmpty` | Skip-ctor results in empty Children |
| `FileDialogState_RefreshChildren_ReEnumerates` | Base `RefreshChildren()` updates Children |
| `SearchState_Constructor_DoesNotEnumerateDirectory` | SearchState uses skip-ctor, starts with `[]` |
| `SearchState_RefreshChildren_IsNoOp` | Override does nothing |
| `SearchState_Cancel_StopsBackgroundSearch` | Cancellation prevents further child updates |
| `FilepathSuggestionGenerator_ReturnsNonNullSuggestions` | No null values in suggestion results |

---

## Verification Steps

1. `dotnet build --no-restore` — no new warnings
2. `dotnet test --project Tests/UnitTestsParallelizable --no-build` — all pass
3. `dotnet test --project Tests/IntegrationTests --no-build --filter-class "*FileDialog*"` — all pass
4. Verify SearchState ctor only touches filesystem via `BeginSearch`'s `RecursiveFind`, not base ctor

---

## Execution Order

1. **Part 1** — Fix State bugs (critical correctness/performance)
2. **Part 2** — Remove dead code (safe, no API impact)
3. **Part 3** — Simplify interfaces (API changes, need care)
4. **Part 4** — Reduce visibility (API changes, low risk)
5. **Part 5** — Add tests (validates all above)
