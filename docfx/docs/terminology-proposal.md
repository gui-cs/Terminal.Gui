# Terminology Proposal: Renaming Application.Top and Toplevel

> **Note**: This proposal has been updated (November 2025) to reflect major architectural improvements in Terminal.Gui v2, including the removal of ALL legacy MainLoop/Driver infrastructure, RunState→SessionToken rename, and making TopLevels public. The proposal remains valid and even more relevant with the current modernized codebase.
>
> **Latest Validation**: November 12, 2025 - Verified against v2_develop after massive legacy cleanup (Issue #4374). All legacy code removed, API simplified and modernized.

## Executive Summary

This document proposes new, clearer terminology to replace the confusing `Application.Top` and `Toplevel` naming in Terminal.Gui v2. The goal is to establish intuitive names that accurately represent the concepts while maintaining backward compatibility during migration.

## Background

### Current Problems

1. **Confusing Terminology**: `Application.Top` suggests "the top of something" but actually represents the currently active view in the application's view hierarchy
2. **Overloaded Meaning**: `Toplevel` is both a class name and conceptually represents views that can be "run" (have their own event loop)
3. **Misleading Relationships**: `Application.TopLevels` (stack) vs `Application.Top` (current) creates confusion about plurality and relationships
4. **Future Direction**: The codebase has TODO comments indicating a move toward an `IRunnable` interface, suggesting the current `Toplevel` class is transitional

### Current Usage Patterns

Based on current code analysis (as of November 12, 2025):
- `Application.Top` - The currently active/running view with its own run loop
- `Application.TopLevels` - **NOW PUBLIC** stack of all active "runnable" views (was internal, now exposed as public API)
- `Toplevel` class - Base class for views that can run independently (Window, Dialog, etc.)
- `SessionToken` - Represents a running session (renamed from `RunState` in Nov 2025)
- Modal vs Non-modal - Views that can be "run" either as overlays or embedded

**Major Recent Architectural Updates (November 2025)**: 
- **ALL legacy Driver and Application code has been removed** (Issue #4374)
- `RunState` renamed to `SessionToken` for clarity
- `Application.TopLevels` **changed from internal to public** - now part of public API
- `CachedRunStateToplevel` property removed (no longer needed)
- BUGBUG comment about TopLevels removed - stack management cleaned up
- Test infrastructure completely revamped
- Driver architecture fully modernized with clean separation of concerns
- The terminology confusion addressed in this proposal is **now even more relevant** as the API has been cleaned up and simplified

### Why This Proposal Is More Important Now (November 2025)

With the recent massive cleanup of legacy code (Issue #4374), Terminal.Gui v2's architecture is now cleaner and more modern than ever. However, this makes the confusing `Top`/`TopLevels` terminology **stand out even more** as an inconsistency:

1. **TopLevels is now public**: Previously internal, `Application.TopLevels` is now part of the public API, making its confusing name a more visible problem for users.

2. **SessionToken clarity**: The rename from `RunState` to `SessionToken` shows a pattern of improving terminology clarity - this proposal continues that pattern.

3. **Clean slate**: With all legacy code removed, this is the **perfect time** to modernize terminology before v2 reaches wider adoption.

4. **No legacy baggage**: Unlike before, there are no legacy MainLoop or old Driver APIs to worry about - the codebase is clean and modern, making terminology updates easier.

5. **Consistency with modern patterns**: The cleaned-up codebase now follows modern .NET patterns more closely - `Application.Current` and `Application.SessionStack` would complete this modernization.

## Proposed Terminology

### 1. Application.Top → Application.Current

**Rationale:**
- **Clarity**: "Current" clearly indicates "the one that is active right now"
- **Familiar**: Aligns with common patterns like `Thread.CurrentThread`, `HttpContext.Current`, etc.
- **Concise**: Short, easy to type, and memorable
- **Accurate**: Directly describes what it represents - the currently running/active view

**Alternative Names Considered:**
- `Application.ActiveView` - More verbose, but very clear
- `Application.CurrentView` - More explicit but redundant with property type
- `Application.Running` - Could be confused with a boolean state
- `Application.Main` - Misleading, as it's not always the main/first view
- `Application.CurrentRunnable` - Too verbose, assumes future IRunnable

### 2. Application.TopLevels → Application.SessionStack

**Rationale:**
- **Descriptive**: Clearly indicates it's a stack of running sessions
- **Technical**: Accurately represents the ConcurrentStack<T> implementation
- **Paired**: Works well with `Application.Current` (Current from SessionStack)
- **Consistent**: Aligns with `SessionToken` terminology (renamed from `RunState` in Nov 2025)
- **Future-proof**: Works whether items are `Toplevel` or `IRunnable`
- **NOW PUBLIC** (Nov 2025): `TopLevels` was changed from internal to public, making its confusing name a **user-facing problem** that needs fixing

**Why This Is Even More Important Now:**
- With `TopLevels` now public (as of November 2025), its confusing name directly impacts users
- The rename would improve the public API without breaking existing code (via deprecation)
- Completes the modernization pattern started with `RunState` → `SessionToken`
- `SessionStack` follows the same terminology pattern as `SessionToken`

**Alternative Names Considered:**
- `Application.ViewStack` - Too generic, not all views are in this stack
- `Application.RunnableStack` - Assumes future IRunnable interface
- `Application.ModalStack` - Inaccurate, non-modal views can be in the stack
- `Application.ActiveViews` - Doesn't convey the stack nature
- `Application.Sessions` - Less clear about the stack nature; doesn't indicate the collection type
- `Application.RunStack` - Previous proposal; doesn't align with SessionToken terminology

### 3. Toplevel Class → (Keep as-is with evolution plan)

**Recommendation: Keep `Toplevel` class name for now, evolve toward `IRunnable`**

**Rationale:**
- **Too Disruptive**: Renaming would break every application
- **Planned Evolution**: The codebase already has plans to introduce `IRunnable` interface (per TODO comments)
- **Transitional**: `Toplevel` can become an implementation detail while `IRunnable` becomes the public interface
- **Gradual Migration**: Allows for deprecation strategy rather than breaking change

**Evolution Path:**
1. Introduce `IRunnable` interface (future work, issue #2491)
2. Have `Toplevel` implement `IRunnable`
3. Update APIs to accept/return `IRunnable` where appropriate
4. Deprecate `Toplevel` as a base class requirement
5. Eventually, `Toplevel` becomes just one implementation of `IRunnable`

**Alternative Considered:**
- `RunnableView` - Better name but too breaking to change now

## Proposed API Changes

### Phase 1: Add New APIs (Backward Compatible)

```csharp
// In Application.cs
namespace Terminal.Gui.App;

public static partial class Application
{
    // NEW: Current API
    /// <summary>
    /// Gets the currently active view with its own run loop.
    /// This is the view at the top of the <see cref="SessionStack"/>.
    /// </summary>
    /// <remarks>
    /// The current view receives all keyboard and mouse input and is responsible
    /// for rendering its portion of the screen. When multiple views are running
    /// (e.g., dialogs over windows), this represents the topmost, active view.
    /// </remarks>
    public static Toplevel? Current
    {
        get => Top;
        internal set => Top = value;
    }

    // DEPRECATED: Keep for backward compatibility
    [Obsolete("Use Application.Current instead. This property will be removed in a future version.", false)]
    public static Toplevel? Top
    {
        get => ApplicationImpl.Instance.Top;
        internal set => ApplicationImpl.Instance.Top = value;
    }

    // NEW: SessionStack API
    /// <summary>
    /// Gets the stack of all currently running views.
    /// Views are pushed onto this stack when <see cref="Run(Toplevel, Func{Exception, bool})"/> 
    /// is called and popped when <see cref="RequestStop(Toplevel)"/> is called.
    /// </summary>
    internal static ConcurrentStack<Toplevel> SessionStack => TopLevels;

    // DEPRECATED: Keep for backward compatibility
    [Obsolete("Use Application.SessionStack instead. This property will be removed in a future version.", false)]
    internal static ConcurrentStack<Toplevel> TopLevels => ApplicationImpl.Instance.TopLevels;
}
```

### Phase 2: Update Documentation and Examples

- Update all XML documentation to use new terminology
- Update docfx documentation articles
- Update code examples in Examples/ directory
- Add migration guide to documentation

### Phase 3: Internal Refactoring (No API Changes)

- Update internal code to use `Application.Current` and `Application.SessionStack`
- Keep old properties as simple forwards for compatibility
- Update test code to use new APIs

### Phase 4: Deprecation Warnings (Future Major Version)

- Change `Obsolete` attributes to show warnings
- Provide clear migration messages
- Allow several versions for migration

### Phase 5: Removal (Future Major Version + N)

- Remove deprecated APIs in a future major version
- Ensure all documentation reflects new terminology

## Migration Guide for Users

### Simple Find-Replace

For most code, migration is straightforward:

```csharp
// Old Code
Application.Top?.SetNeedsDraw();
var focused = Application.Top.MostFocused;

// New Code  
Application.Current?.SetNeedsDraw();
var focused = Application.Current.MostFocused;
```

### More Complex Scenarios

```csharp
// Working with the view stack
// Old Code
if (Application.TopLevels.Count > 0)
{
    foreach (Toplevel topLevel in Application.TopLevels)
    {
        // process each running view
    }
}

// New Code (when internal API is made public)
if (Application.SessionStack.Count > 0)
{
    foreach (Toplevel runnable in Application.SessionStack)
    {
        // process each running view
    }
}
```

## Benefits of This Approach

### 1. Improved Clarity
- `Application.Current` is immediately understandable
- `SessionStack` clearly describes what it is and what it contains
- Reduces cognitive load for new developers

### 2. Better Code Readability
```csharp
// Before: What is "Top"? Top of what?
Application.Top?.DoSomething();

// After: Clear that we're working with the current view
Application.Current?.DoSomething();
```

### 3. Consistency with .NET Patterns
- Aligns with `Thread.CurrentThread`, `SynchronizationContext.Current`, etc.
- Familiar to .NET developers

### 4. Future-Proof
- Works with planned `IRunnable` interface
- `Current` can return `IRunnable?` in the future
- `SessionStack` can become `ConcurrentStack<IRunnable>` in the future

### 5. Minimal Breaking Changes
- Deprecated APIs remain functional
- Gradual migration path
- No immediate breaking changes for users

## Risks and Mitigations

### Risk 1: User Migration Effort
**Mitigation**: 
- Provide clear deprecation warnings
- Offer automated migration tools (analyzers)
- Allow multiple versions for migration
- Provide comprehensive documentation

### Risk 2: Third-Party Libraries
**Mitigation**:
- Keep deprecated APIs functional for extended period
- Clearly communicate timeline in release notes
- Engage with library maintainers early

### Risk 3: Documentation Inconsistency
**Mitigation**:
- Systematic documentation update
- Search for all occurrences in docs, examples, tests
- Use automated tools to ensure completeness

## Implementation Checklist

### Core API Changes
- [ ] Add `Application.Current` property with forwarding to `Top`
- [ ] Add `[Obsolete]` attribute to `Application.Top` (warning disabled initially)
- [ ] Add `Application.SessionStack` property with forwarding to `TopLevels`
- [ ] Add `[Obsolete]` attribute to `Application.TopLevels` (warning disabled initially)
- [ ] Update XML documentation for new properties
- [ ] Update IApplication interface if needed

### Documentation Updates
- [ ] Update all docfx articles mentioning `Application.Top`
- [ ] Update API documentation
- [ ] Create migration guide document
- [ ] Update README.md if it mentions the old terminology
- [ ] Update code examples in docfx
- [ ] Update CONTRIBUTING.md and AGENTS.md if needed

### Code Updates
- [ ] Update all internal code to use `Application.Current`
- [ ] Update all internal code to use `Application.SessionStack` (where appropriate)
- [ ] Update test code to use new APIs
- [ ] Update example applications (UICatalog, Example, etc.)

### Testing
- [ ] Ensure all existing tests pass
- [ ] Add tests for new properties
- [ ] Add tests for deprecated property forwarding
- [ ] Test that obsolete attributes work correctly

### Communication
- [ ] Update issue #4329 with proposal
- [ ] Get feedback from maintainers
- [ ] Document in release notes
- [ ] Update migration guide from v1 to v2

## Alternative Proposals Considered

### Alternative 1: Application.ActiveView
**Pros**: Very clear and explicit
**Cons**: More verbose, `View` suffix is redundant with type

### Alternative 2: Application.MainView
**Pros**: Short and simple
**Cons**: Misleading - not always the "main" view

### Alternative 3: Application.CurrentTopLevel
**Pros**: Keeps "TopLevel" terminology
**Cons**: Doesn't solve the confusion about "TopLevel"

### Alternative 4: Application.ForegroundView
**Pros**: Describes visual position
**Cons**: Could be confused with focus or z-order

### Alternative 5: Keep Current Names
**Pros**: No migration needed
**Cons**: Confusion persists, misses opportunity for improvement

## Conclusion

This proposal recommends:
1. **Rename `Application.Top` → `Application.Current`**: Clear, concise, familiar
2. **Rename `Application.TopLevels` → `Application.SessionStack`**: Descriptive and accurate
3. **Keep `Toplevel` class as-is**: Allow for gradual evolution toward `IRunnable`
4. **Phased migration**: Maintain backward compatibility with clear deprecation path

The new terminology aligns with .NET conventions, improves code readability, and provides a clear migration path for users while supporting the future direction of the codebase.

## References

- Issue #4329: Rename/Clarify `Application.Toplevels`/`Top` Terminology
- Issue #2491: Toplevel refactoring (IRunnable interface)
- Current codebase analysis in Terminal.Gui v2_develop branch
