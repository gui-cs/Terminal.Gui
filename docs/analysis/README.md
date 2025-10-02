# Cancellable Work Pattern (CWP) Order Analysis

This directory contains a comprehensive analysis of the Cancellable Work Pattern (CWP) in Terminal.Gui, specifically examining the question of whether to reverse the calling order from "virtual method first, event second" to "event first, virtual method second."

## Documents in This Analysis

### 1. [CWP Analysis Report](cwp_analysis_report.md)
**Executive-level overview** of the analysis with:
- Summary statistics (33+ CWP implementations found)
- Impact assessment by category (mouse, keyboard, commands, etc.)
- Analysis of each CWP implementation
- Risk assessment by impact level
- Four solution options with pros/cons
- Recommendations

**Read this first** for high-level understanding.

### 2. [CWP Detailed Code Analysis](cwp_detailed_code_analysis.md)
**Technical deep-dive** with:
- Code examples of current implementation
- Detailed explanation of Slider mouse event issue (#3714)
- Complete catalog of all CWP implementations
- Analysis of helper classes (CWPWorkflowHelper, etc.)
- Documentation review
- Dependencies on current order
- Breaking change assessment

**Read this second** for technical details.

### 3. [CWP Recommendations](cwp_recommendations.md)
**Implementation guidance** with:
- Detailed comparison of 4 solution options
- Recommended approach (Option 2: Add Before Events)
- Complete implementation specification
- Code examples showing solution
- Migration path
- Testing strategy
- Implementation checklist

**Read this third** for actionable recommendations.

## Quick Summary

### The Issue
External code cannot prevent views (like Slider) from handling events because the virtual method (e.g., `OnMouseEvent`) is called before the event is raised, allowing the view to mark the event as handled before external subscribers see it.

### Analysis Results
- **33+ CWP implementations** found across codebase
- **100+ virtual method overrides** depend on current order
- **HIGH IMPACT** change if order reversed globally
- **Tests explicitly validate** current order
- **Code comments document** current order as "best practice"

### Recommended Solution: **Add "Before" Events** ✅

Instead of changing the existing pattern (breaking change), ADD new events:
- `BeforeMouseEvent`, `BeforeMouseClick`, etc.
- Called BEFORE virtual method
- Allows external code to cancel before view processes
- Non-breaking, additive change
- Solves issue #3714 completely

**Effort**: 2-3 weeks  
**Risk**: LOW  
**Breaking Changes**: None

### Implementation Example

```csharp
// What users want and NOW CAN DO:
var slider = new Slider();

slider.BeforeMouseEvent += (sender, args) =>
{
    if (shouldDisableSlider)
    {
        args.Handled = true; // Prevents Slider.OnMouseEvent from being called
    }
};
```

### Three-Phase Pattern

```
1. BeforeXxx event    → External pre-processing (NEW)
2. OnXxx method       → View processing (EXISTING)
3. Xxx event          → External post-processing (EXISTING)
```

## For Issue Maintainers

This analysis was requested in issue #3714 to:
> "Use the AIs to do an analysis of the entire code base, creating a report of EVERY instance where CWP-style eventing is used and what the potential impact of changing the CWP guidance would be."

The analysis is complete and recommends **Option 2 (Add Before Events)** as the best balance of:
- Solving the issue
- Minimizing risk
- Preserving existing behavior
- Providing migration path

## Next Steps

1. Review analysis and recommendation
2. Decide on approach
3. If proceeding with Option 2:
   - Implement BeforeMouseEvent, BeforeMouseClick, BeforeMouseWheel
   - Update documentation
   - Add tests
   - Verify Slider scenario works
4. If proceeding with alternative:
   - Document rationale
   - Implement chosen solution

## Files Changed During Analysis

- ❌ No production code changed (analysis only)
- ✅ Analysis documents created (this directory)

## Related Issues

- #3714: Cancellable Work Pattern order issue (mouse events)
- #3417: Related mouse event handling issue

## Date of Analysis

Generated: 2024 (exact date in git history)
