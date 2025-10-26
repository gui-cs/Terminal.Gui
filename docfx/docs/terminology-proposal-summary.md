# Terminology Proposal Summary

> **Updated October 2025**: Proposal validated against current modernized codebase (post-MainLoop removal).

This is a brief summary of the [full terminology proposal](terminology-proposal.md).

## Recommended Changes

| Current Name | Proposed Name | Rationale |
|--------------|---------------|-----------|
| `Application.Top` | `Application.Current` | Clear, follows .NET patterns (e.g., `Thread.CurrentThread`), indicates "currently active" |
| `Application.TopLevels` | `Application.RunStack` | Descriptive of the stack structure, pairs well with `Current` |
| `Toplevel` class | Keep as-is (for now) | Too disruptive to rename; allow gradual evolution toward `IRunnable` |

## Why These Names?

### Application.Current
- ✅ Immediately understandable
- ✅ Consistent with .NET conventions
- ✅ Short and memorable
- ✅ Accurately describes the "currently active/running view"

### Application.RunStack
- ✅ Describes what it contains (running views)
- ✅ Describes its structure (stack)
- ✅ Works with future `IRunnable` interface
- ✅ Clear relationship with `Current` (top of the stack)

## Migration Strategy

1. **Phase 1**: Add new properties, mark old ones `[Obsolete]` (no warnings initially)
2. **Phase 2**: Update documentation and examples
3. **Phase 3**: Refactor internal code to use new names
4. **Phase 4**: Enable deprecation warnings
5. **Phase 5**: Remove deprecated APIs (future major version)

## Example Code

```csharp
// Before
Application.Top?.SetNeedsDraw();
var focused = Application.Top.MostFocused;

// After
Application.Current?.SetNeedsDraw();
var focused = Application.Current.MostFocused;
```

## Key Benefits

1. **Improved Clarity**: Names that immediately convey their purpose
2. **Better Readability**: Code is self-documenting
3. **Consistency**: Aligns with .NET ecosystem patterns
4. **Future-Proof**: Works with planned `IRunnable` interface
5. **Minimal Disruption**: Backward-compatible migration path

## See Also

- [Visual Diagrams](terminology-diagrams.md) - Mermaid diagrams visualizing the proposal
- [Full Proposal Document](terminology-proposal.md) - Complete analysis and implementation details
- [Before/After Examples](terminology-before-after.md) - Side-by-side code comparisons
- [Documentation Index](terminology-index.md) - Navigation guide
- Issue #4329 - Original issue requesting terminology improvements
- Issue #2491 - Toplevel refactoring and `IRunnable` interface work
