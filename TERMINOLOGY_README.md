# Application.Run Terminology Proposal - README

This directory contains a comprehensive proposal for improving the terminology around `Application.Run` and related APIs in Terminal.Gui.

## 📋 Documents

### 1. [TERMINOLOGY_PROPOSAL.md](TERMINOLOGY_PROPOSAL.md)
**Complete proposal with detailed analysis**

Contents:
- Executive Summary
- Problem Statement (why current terminology is confusing)
- Current Terminology Analysis
- Three proposed options with pros/cons
- Recommendation: Option 1 (Session-Based)
- Migration strategy
- Documentation changes required
- FAQ

**Start here** for the full context and rationale.

### 2. [TERMINOLOGY_QUICK_REFERENCE.md](TERMINOLOGY_QUICK_REFERENCE.md)
**Quick comparison tables and code examples**

Contents:
- Current vs Proposed (all 3 options)
- Side-by-side comparison table
- Usage examples for each option
- Manual event loop control examples
- High-level vs low-level API comparison

**Use this** for quick lookup and comparison.

### 3. [TERMINOLOGY_INDUSTRY_COMPARISON.md](TERMINOLOGY_INDUSTRY_COMPARISON.md)
**How Terminal.Gui compares to other frameworks**

Contents:
- Comparison with WPF, WinForms, Avalonia, GTK, Qt
- Web framework patterns (ASP.NET, Entity Framework)
- Game engine patterns (Unity, Unreal)
- Industry standard terminology analysis
- Why "Session" is the right choice

**Read this** to understand industry context.

### 4. [TERMINOLOGY_VISUAL_GUIDE.md](TERMINOLOGY_VISUAL_GUIDE.md)
**Visual diagrams and flowcharts**

Contents:
- Visual comparison of current vs proposed
- Lifecycle diagrams
- Event flow diagrams
- Nested sessions (modal dialogs)
- Complete example flows
- Benefits visualization

**Use this** for visual learners and presentations.

## 🎯 The Problem

The current `Application.Run` terminology is confusing:

```csharp
// What's the difference between these "Run" methods?
Application.Run(window);           // Complete lifecycle
Application.RunLoop(runState);     // Event loop
Application.RunIteration();        // One iteration

// What is RunState? State or a handle?
RunState runState = Application.Begin(window);  // Begin what?

// What's ending?
Application.End(runState);  // End what?
```

**Result:** Confused users, steeper learning curve, unclear documentation.

## ✅ The Solution

### Option 1: Session-Based Terminology (Recommended)

```csharp
// High-level API (unchanged)
Application.Run(window);  // Simple and familiar

// Low-level API (clearer names)
ToplevelSession session = Application.BeginSession(window);    // ✅ Clear
Application.ProcessEvents(session);                            // ✅ Clear
Application.EndSession(session);                               // ✅ Clear
```

**Why this wins:**
- ✅ "Session" accurately describes bounded execution
- ✅ "ProcessEvents" is explicit about what happens
- ✅ "BeginSession/EndSession" are unambiguous
- ✅ Aligns with industry patterns (HttpContext, DbContext)
- ✅ Minimal disruption to existing API

### Complete Mapping

| Current | Proposed | Why |
|---------|----------|-----|
| `Run()` | `Run()` | Keep - familiar |
| `RunState` | `ToplevelSession` | Clear it's a session token |
| `Begin()` | `BeginSession()` | Clear what's beginning |
| `RunLoop()` | `ProcessEvents()` | Describes the action |
| `RunIteration()` | `ProcessEventsIteration()` | Consistent |
| `End()` | `EndSession()` | Clear what's ending |
| `RequestStop()` | `StopProcessingEvents()` | Explicit |

## 📊 Comparison Matrix

| Criterion | Current | Proposed (Option 1) |
|-----------|---------|---------------------|
| **Clarity** | ⚠️ "Run" overloaded | ✅ Each term is distinct |
| **Accuracy** | ⚠️ "State" is misleading | ✅ "Session" is accurate |
| **Learnability** | ⚠️ Steep curve | ✅ Self-documenting |
| **Industry Alignment** | ⚠️ Unique terminology | ✅ Standard patterns |
| **Breaking Changes** | N/A | ✅ None (old APIs kept) |

## 🚀 Migration Path

### Phase 1: Add New APIs (Release 1)
```csharp
// Add new APIs
public static ToplevelSession BeginSession(Toplevel toplevel) { ... }

// Mark old APIs obsolete
[Obsolete("Use BeginSession instead. See TERMINOLOGY_PROPOSAL.md")]
public static RunState Begin(Toplevel toplevel) { ... }
```

### Phase 2: Update Documentation (Release 1-2)
- Update all docs to use new terminology
- Add migration guide
- Update examples

### Phase 3: Community Adoption (Release 2-4)
- Examples use new APIs
- Community feedback period
- Adjust based on feedback

### Phase 4: Consider Removal (Release 5+)
- After 2-3 releases, consider removing `[Obsolete]` APIs
- Or keep them indefinitely with internal delegation

## 💡 Key Insights

### 1. High-Level API Unchanged
Most users won't be affected:
```csharp
Application.Init();
Application.Run(window);  // Still works exactly the same
Application.Shutdown();
```

### 2. Low-Level API Clarified
Advanced users get clearer APIs:
```csharp
// Before (confusing)
var rs = Application.Begin(window);
Application.RunLoop(rs);
Application.End(rs);

// After (clear)
var session = Application.BeginSession(window);
Application.ProcessEvents(session);
Application.EndSession(session);
```

### 3. Complete Backward Compatibility
```csharp
// Old code continues to work
RunState rs = Application.Begin(window);  // Works, but obsolete warning
Application.RunLoop(rs);                   // Works, but obsolete warning
Application.End(rs);                       // Works, but obsolete warning
```

## 📈 Benefits

### For Users
- ✅ **Faster learning** - Self-documenting APIs
- ✅ **Less confusion** - Clear, distinct names
- ✅ **Better understanding** - Matches mental model

### For Maintainers
- ✅ **Easier to explain** - Clear terminology in docs
- ✅ **Fewer questions** - Users understand the pattern
- ✅ **Better code** - Internal code can use clearer names

### For the Project
- ✅ **Professional** - Aligns with industry standards
- ✅ **Accessible** - Lower barrier to entry
- ✅ **Maintainable** - Clearer code is easier to maintain

## 🤔 Alternatives Considered

### Option 2: Modal/Show Terminology
```csharp
Application.ShowModal(window);
var handle = Application.Activate(window);
Application.EventLoop(handle);
Application.Deactivate(handle);
```
**Rejected:** Doesn't fit Terminal.Gui's model well.

### Option 3: Lifecycle Terminology
```csharp
var context = Application.Start(window);
Application.Execute(context);
Application.Stop(context);
```
**Rejected:** Breaks Begin/End pattern, "Execute" less explicit.

See [TERMINOLOGY_PROPOSAL.md](TERMINOLOGY_PROPOSAL.md) for full analysis.

## 📚 Additional Resources

### Terminal.Gui Documentation
- [Application Class](https://gui-cs.github.io/Terminal.Gui/api/Terminal.Gui.App.Application.html)
- [Multitasking Guide](docfx/docs/multitasking.md)

### Related Issues
- Issue #4329 - Original discussion about Run terminology

## 🗳️ Community Feedback

We welcome feedback on this proposal:

1. **Preferred option?** Session-Based, Modal/Show, or Lifecycle?
2. **Better names?** Suggest alternatives
3. **Migration concerns?** Share your use cases
4. **Timeline?** How long do you need to migrate?

## 📞 Contact

For questions or feedback:
- Open an issue in the Terminal.Gui repository
- Comment on the PR associated with this proposal
- Join the discussion in the community forums

## 📄 License

This proposal is part of the Terminal.Gui project and follows the same license (MIT).

---

**Status:** 📝 Proposal (awaiting community feedback)

**Author:** GitHub Copilot (generated based on issue #4329)

**Date:** 2025-10-25

**Version:** 1.0
