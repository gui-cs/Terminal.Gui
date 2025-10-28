# Terminal.Gui Terminology Proposal - Documentation Index

> **Updated October 2025**: Proposal validated and updated to reflect recent architectural modernizations, including removal of legacy MainLoop infrastructure, driver architecture refactoring, and test infrastructure improvements.
>
> **Latest Validation**: October 28, 2025 - Verified against current codebase including FakeDriver consolidation and recent API refinements.

This directory contains a comprehensive proposal for renaming `Application.Top` and related terminology in Terminal.Gui v2.

## 📚 Documents

### 1. [terminology-proposal-summary.md](terminology-proposal-summary.md) ⭐ **Start Here**
Quick overview of the proposal with key recommendations in a table format. Best for getting a high-level understanding.

**Contents:**
- Recommended changes table
- Key benefits summary
- Migration strategy overview
- Quick code examples

### 2. [terminology-diagrams.md](terminology-diagrams.md) 📊 **Visual Diagrams**
Mermaid diagrams visualizing the proposal, relationships, and migration path.

**Contents:**
- Current vs Proposed terminology comparison
- Stack relationship diagrams
- Before/After naming patterns
- .NET pattern consistency
- View hierarchy and run stack
- Usage flow examples
- Evolution path timeline
- Migration phases Gantt chart

### 3. [terminology-before-after.md](terminology-before-after.md) 📝 **Code Examples**
Side-by-side comparisons showing how the new terminology improves code clarity.

**Contents:**
- API naming comparisons
- Real-world code examples
- Documentation clarity improvements
- Consistency with .NET patterns
- Summary comparison table

### 4. [terminology-proposal.md](terminology-proposal.md) 📖 **Full Details**
Complete, comprehensive proposal with all analysis, rationale, and implementation details.

**Contents:**
- Executive summary
- Background and current problems
- Detailed proposal and rationale
- Migration strategy (5 phases)
- Proposed API changes with code
- Benefits, risks, and mitigations
- Implementation checklist
- Alternative proposals considered

## 🎯 Quick Summary

### Recommended Changes

| Current | Proposed | Why |
|---------|----------|-----|
| `Application.Top` | `Application.Current` | Clear, follows .NET patterns, self-documenting |
| `Application.TopLevels` | `Application.RunStack` | Describes structure and content accurately |
| `Toplevel` class | Keep (for now) | Allow evolution to `IRunnable` interface |

### Key Benefits

1. **Clarity**: Names immediately convey their purpose
2. **Consistency**: Aligns with .NET ecosystem patterns
3. **Readability**: Self-documenting code
4. **Future-proof**: Works with planned `IRunnable` interface
5. **Compatibility**: Backward-compatible migration path

## 📖 Reading Guide

**If you want to...**

- 📋 **Get the gist quickly**: Read [terminology-proposal-summary.md](terminology-proposal-summary.md)
- 🎨 **See visual diagrams**: Read [terminology-diagrams.md](terminology-diagrams.md)
- 👀 **See concrete examples**: Read [terminology-before-after.md](terminology-before-after.md)
- 🔍 **Understand all details**: Read [terminology-proposal.md](terminology-proposal.md)
- 💡 **Implement the changes**: See implementation checklist in [terminology-proposal.md](terminology-proposal.md)

## 🔗 Related Issues

- **Issue #4329**: Rename/Clarify `Application.Toplevels`/`Top` Terminology (this proposal)
- **Issue #2491**: Toplevel refactoring and `IRunnable` interface work

## 💭 Feedback

This proposal is open for discussion and feedback from the Terminal.Gui maintainers and community. Please comment on Issue #4329 with:
- Questions about the proposal
- Alternative naming suggestions
- Migration concerns
- Implementation details

## 📝 Note on Implementation

This proposal focuses on **naming and terminology only**. The actual implementation (adding new properties, deprecating old ones, updating documentation) would be a separate effort pending approval of this proposal.

---

**Created**: October 2025  
**Issue**: #4329  
**Related**: #2491  
**Status**: Proposal - Awaiting Review
