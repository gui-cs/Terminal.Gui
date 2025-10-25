# Application.Run Terminology Proposal - Executive Summary

## 🎯 Purpose

Propose clearer, more intuitive terminology for the `Application.Run` lifecycle APIs in Terminal.Gui.

## 📊 The Problem in 30 Seconds

```csharp
// Current: "Run" means too many things
Application.Run(window);           // ← Complete lifecycle
Application.RunLoop(runState);     // ← Event loop?
Application.RunIteration();        // ← One iteration?

RunState runState = Application.Begin(window);  // ← Begin what? What's RunState?
Application.End(runState);                      // ← End what?
```

**Result:** Confused users, unclear docs, steep learning curve.

## ✅ The Solution in 30 Seconds

```csharp
// Proposed: Clear, self-documenting names
Application.Run(window);                            // ← Unchanged (high-level)

ToplevelSession session = Application.BeginSession(window);  // ✅ Clear
Application.ProcessEvents(session);                          // ✅ Clear
Application.EndSession(session);                             // ✅ Clear
```

**Result:** Self-documenting APIs, faster learning, industry alignment.

## 📈 Impact

### Who This Affects
- ✅ **New users:** Easier to understand and learn
- ✅ **Existing users:** Optional upgrade via [Obsolete] warnings
- ✅ **Documentation:** Clearer explanations possible
- ✅ **Maintainers:** Fewer confused user questions

### Breaking Changes
- ❌ **NONE** - All existing APIs continue to work
- ✅ Old APIs marked `[Obsolete]` with helpful migration messages
- ✅ Gradual migration at each user's own pace

## 🔄 Complete Mapping

| Current API | Proposed API | Benefit |
|-------------|--------------|---------|
| `RunState` | `ToplevelSession` | Clear it's a session token |
| `Begin()` | `BeginSession()` | Unambiguous what's beginning |
| `RunLoop()` | `ProcessEvents()` | Describes the action |
| `RunIteration()` | `ProcessEventsIteration()` | Consistent naming |
| `End()` | `EndSession()` | Unambiguous what's ending |
| `RequestStop()` | `StopProcessingEvents()` | Explicit about what stops |

## 💡 Why "Session"?

1. **Industry Standard**
   - `HttpContext` - one HTTP request session
   - `DbContext` - one database session
   - `CancellationToken` - one cancellation scope
   
2. **Accurate**
   - A Toplevel execution IS a bounded session
   - Multiple sessions can exist (nested modals)
   - Sessions have clear begin/end lifecycle

3. **Clear**
   - "Session" implies temporary, bounded execution
   - "BeginSession/EndSession" are unambiguous pairs
   - "ToplevelSession" clearly indicates purpose

## 📚 Documentation Structure

```
TERMINOLOGY_README.md (Start Here)
    ├─ Overview and navigation
    ├─ Problem statement
    └─ Links to all documents

TERMINOLOGY_PROPOSAL.md
    ├─ Complete analysis
    ├─ 3 options with rationale
    ├─ Migration strategy
    └─ FAQ

TERMINOLOGY_QUICK_REFERENCE.md
    ├─ Side-by-side comparisons
    ├─ Usage examples
    └─ Quick lookup tables

TERMINOLOGY_INDUSTRY_COMPARISON.md
    ├─ Framework comparisons
    ├─ Industry patterns
    └─ Why this solution

TERMINOLOGY_VISUAL_GUIDE.md
    ├─ ASCII diagrams
    ├─ Flow charts
    └─ Visual comparisons
```

## 🚀 Next Steps

1. **Review** - Community reviews this proposal
2. **Feedback** - Gather comments and suggestions
3. **Refine** - Adjust based on feedback
4. **Approve** - Get maintainer approval
5. **Implement** - Add new APIs with [Obsolete] on old ones
6. **Document** - Update all documentation
7. **Migrate** - Examples and guides use new terminology

## ⏱️ Timeline (Proposed)

- **Phase 1 (Release N):** Add new APIs, mark old ones obsolete
- **Phase 2 (Release N+1):** Update all documentation
- **Phase 3 (Release N+2):** Update all examples
- **Phase 4 (Release N+3+):** Consider removing obsolete APIs (or keep forever)

## 🗳️ Alternative Options

This proposal includes 3 options:

1. **Session-Based** ⭐ (Recommended)
   - BeginSession/ProcessEvents/EndSession
   - Most accurate and industry-aligned

2. **Modal/Show**
   - Activate/EventLoop/Deactivate
   - Aligns with WPF patterns

3. **Lifecycle**
   - Start/Execute/Stop
   - Simple verbs

See [TERMINOLOGY_PROPOSAL.md](TERMINOLOGY_PROPOSAL.md) for detailed comparison.

## 💬 Feedback Welcome

- What do you think of the proposed names?
- Do you prefer a different option?
- Any concerns about migration?
- Timeline reasonable for your projects?

## 📖 Full Documentation

Read the complete proposal: [TERMINOLOGY_README.md](TERMINOLOGY_README.md)

---

**Status:** 📝 Awaiting Community Feedback

**Issue:** #4329

**Created:** 2025-10-25

**Author:** GitHub Copilot
