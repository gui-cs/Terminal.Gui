# CWP Order Analysis - Summary for Issue Discussion

## Overview

I've completed a comprehensive analysis of the Cancellable Work Pattern (CWP) order in the Terminal.Gui codebase as requested in this issue. The analysis covers:

1. Every CWP implementation in the codebase
2. Impact assessment of reversing the order
3. Code dependencies on the current order
4. Four solution options with detailed pros/cons
5. Concrete recommendations with implementation plans

## Full Analysis Documents

The complete analysis is available in the repository under `docs/analysis/`:

- **[README.md](../analysis/README.md)** - Overview and navigation guide
- **[cwp_analysis_report.md](../analysis/cwp_analysis_report.md)** - Executive summary with statistics
- **[cwp_detailed_code_analysis.md](../analysis/cwp_detailed_code_analysis.md)** - Technical deep-dive
- **[cwp_recommendations.md](../analysis/cwp_recommendations.md)** - Implementation guidance

## Key Findings

### Scale of CWP Usage

- **33+ CWP event pairs** found across the codebase
- **100+ virtual method overrides** in views that depend on current order
- **3 helper classes** implement the pattern (CWPWorkflowHelper, etc.)
- **Explicit tests** validate current order (OrientationTests)
- **Code comments** document current order as "best practice"

### CWP Categories by Impact

| Impact Level | Count | Examples |
|-------------|-------|----------|
| HIGH | 3 | OnMouseEvent, OnMouseClick, OnKeyDown |
| MEDIUM-HIGH | 4 | OnAccepting, OnSelecting, OnAdvancingFocus, OnHasFocusChanging |
| MEDIUM | 8 | OnMouseWheel, OnMouseEnter, OnKeyUp, various commands |
| LOW-MEDIUM | 10 | Property changes, view-specific events |
| LOW | 8 | Specialized/rare events |

### Dependencies on Current Order

1. **Explicit in Code**: Comments state current order is "best practice"
2. **Explicit in Tests**: Tests verify virtual method called before event
3. **Implicit in Views**: 100+ overrides assume they get first access
4. **Implicit in State Management**: Views update state first, then external code sees updated state

### The Core Problem (Issue #3714)

Views like `Slider` override `OnMouseEvent` and return `true`, preventing external code from ever seeing the event:

```csharp
// What users WANT but CANNOT do today:
slider.MouseEvent += (sender, args) => 
{
    if (shouldDisable)
        args.Handled = true; // TOO LATE - Slider.OnMouseEvent already handled it
};
```

## Four Solution Options

### Option 1: Reverse Order Globally ⚠️

**Change all 33+ CWP patterns** to call event first, virtual method second.

- **Effort**: 4-6 weeks
- **Risk**: VERY HIGH (major breaking change)
- **Verdict**: ❌ NOT RECOMMENDED unless major version change

**Why Not**: Breaks 100+ view overrides, changes fundamental framework philosophy, requires extensive testing, may break user code.

### Option 2: Add "Before" Events 🎯 RECOMMENDED

**Add new events** (e.g., `BeforeMouseEvent`) that fire before virtual method.

```csharp
// Three-phase pattern:
// 1. BeforeMouseEvent (external pre-processing) ← NEW
// 2. OnMouseEvent (view processing)            ← EXISTING
// 3. MouseEvent (external post-processing)     ← EXISTING
```

- **Effort**: 2-3 weeks
- **Risk**: LOW (non-breaking, additive only)
- **Verdict**: ✅ **RECOMMENDED**

**Why Yes**: Solves #3714, non-breaking, clear intent, selective application, provides migration path.

### Option 3: Add `MouseInputEnabled` Property 🔧

**Add property** to View to disable mouse handling entirely.

- **Effort**: 1 week
- **Risk**: VERY LOW
- **Verdict**: ⚠️ Band-aid solution, acceptable short-term

**Why Maybe**: Quick fix for immediate issue, but doesn't address root cause or help keyboard/other events.

### Option 4: Reverse Order for Mouse Only 🎯

**Reverse order** for mouse events only, keep keyboard/others unchanged.

- **Effort**: 2 weeks
- **Risk**: MEDIUM (still breaking for mouse)
- **Verdict**: ⚠️ Inconsistent pattern, Option 2 is cleaner

**Why Not**: Creates inconsistency, still breaks mouse event overrides, doesn't help future similar issues.

## Recommended Solution: Option 2

### Implementation Overview

Add new `BeforeXxx` events that fire BEFORE virtual methods:

```csharp
public partial class View // Mouse APIs
{
    public event EventHandler<MouseEventArgs>? BeforeMouseEvent;
    public event EventHandler<MouseEventArgs>? BeforeMouseClick;
    
    public bool RaiseMouseEvent(MouseEventArgs mouseEvent)
    {
        // Phase 1: Before event (NEW) - external pre-processing
        BeforeMouseEvent?.Invoke(this, mouseEvent);
        if (mouseEvent.Handled)
            return true;
        
        // Phase 2: Virtual method (EXISTING) - view processing
        if (OnMouseEvent(mouseEvent) || mouseEvent.Handled)
            return true;

        // Phase 3: After event (EXISTING) - external post-processing
        MouseEvent?.Invoke(this, mouseEvent);
        return mouseEvent.Handled;
    }
}
```

### Usage Example (Solves #3714)

```csharp
var slider = new Slider();

// NOW THIS WORKS!
slider.BeforeMouseEvent += (sender, args) =>
{
    if (shouldDisableSlider)
    {
        args.Handled = true; // Prevents Slider.OnMouseEvent from being called
    }
};
```

### Benefits

1. ✅ **Solves #3714**: External code can now prevent views from handling events
2. ✅ **Non-Breaking**: All existing code continues to work
3. ✅ **Clear Intent**: "Before" explicitly means "external pre-processing"
4. ✅ **Selective**: Add to problematic events (mouse, keyboard), not all 33
5. ✅ **Future-Proof**: Provides migration path for v3.0
6. ✅ **Minimal Risk**: Additive only, no changes to existing behavior

### Implementation Checklist

- [ ] Week 1: Add BeforeMouseEvent, BeforeMouseClick, BeforeMouseWheel
- [ ] Week 2: Update documentation (events.md, cancellable-work-pattern.md)
- [ ] Week 3: Add BeforeKeyDown if needed
- [ ] Week 4: Testing and refinement

## Comparison Table

| Aspect | Option 1: Reverse Order | Option 2: Before Events | Option 3: Property | Option 4: Mouse Only |
|--------|------------------------|------------------------|-------------------|---------------------|
| Solves #3714 | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| Breaking Change | ❌ Major | ✅ None | ✅ None | ⚠️ Medium |
| Consistency | ✅ Perfect | ⚠️ Two patterns | ⚠️ Band-aid | ❌ Inconsistent |
| Effort | ❌ 4-6 weeks | ✅ 2-3 weeks | ✅ 1 week | ⚠️ 2 weeks |
| Risk | ❌ Very High | ✅ Low | ✅ Very Low | ⚠️ Medium |
| Future-Proof | ✅ Yes | ✅ Yes | ❌ No | ⚠️ Partial |
| **Verdict** | ❌ Not Recommended | ✅ **RECOMMENDED** | ⚠️ Short-term only | ⚠️ Option 2 better |

## What About Just Fixing Slider?

**No** - this is a systemic issue affecting 30+ views:
- ListView, TextView, TextField
- TableView, TreeView
- ScrollBar, ScrollSlider
- MenuBar, TabRow
- ColorBar, HexView
- And 20+ more...

Any view that overrides `OnMouseEvent` has the same problem. We need a general solution.

## Design Philosophy Consideration

The current order reflects "inheritance over composition":
- Virtual methods (inheritance) get priority
- Events (composition) get second chance
- Appropriate for tightly-coupled UI framework

The proposed "Before" events enable "composition over inheritance" when needed:
- External code (composition) can prevent via BeforeXxx
- Virtual methods (inheritance) process if not prevented
- Events (composition) observe after processing
- Best of both worlds

## Next Steps

1. **Review** this analysis and recommendation
2. **Decide** on approach (recommend Option 2)
3. **Implement** if proceeding:
   - Add BeforeMouseEvent, BeforeMouseClick, BeforeMouseWheel
   - Update documentation with three-phase pattern
   - Add tests validating new events
   - Verify Slider scenario works as expected
4. **Document** decision and rationale

## Questions for Discussion

1. Do we agree Option 2 (Before Events) is the best approach?
2. Should we implement keyboard events at the same time, or mouse-only first?
3. Should we apply this pattern to commands/navigation events, or wait for user requests?
4. What should the naming convention be? (`BeforeXxx`, `PreXxx`, `XxxPre`?)
5. Should we mark old pattern as "legacy" in v3.0, or keep both indefinitely?

## Timeline

If we proceed with **Option 2**:

- **Week 1-2**: Implementation (mouse events)
- **Week 3**: Documentation
- **Week 4**: Testing and refinement
- **Total**: ~1 month to complete solution

## Conclusion

The analysis shows that **reversing CWP order globally would be a major breaking change** affecting 100+ locations. However, **adding "Before" events is a low-risk solution** that achieves the same goal without breaking existing code.

**Recommendation**: Proceed with Option 2 (Add Before Events)

---

For complete technical details, code examples, and implementation specifications, see the full analysis documents in `docs/analysis/`.
