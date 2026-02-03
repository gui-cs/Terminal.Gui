# DimAuto Decoupling Plan

## Overview

This document outlines a plan to reduce tight coupling in `DimAuto.Calculate()` and related layout code by moving type-specific logic into the `Dim` and `Pos` subclasses themselves.

## Current State (Problem)

`DimAuto.Calculate()` in `DimAuto.cs` contains ~500 lines of layout logic with extensive knowledge of specific `Pos` and `Dim` subtypes:

### Direct Type Checking
```csharp
v.X is PosAbsolute or PosFunc
v.Width is DimAuto or DimAbsolute or DimFunc
```

### Has<T>() Checks
```csharp
v.X.Has<PosAnchorEnd>(out _)
v.Width.Has<DimFill>(out _)
v.X.Has<PosAlign>(out _)
```

### Direct Property Access After Casting
```csharp
DimFill? dimFill = dimension == Dimension.Width ? dimFillSubView.Width as DimFill : ...
dimFill?.MinimumContentDim
dimFill?.To
```

This creates tight coupling where:
- Adding a new `Pos`/`Dim` type requires modifying `DimAuto`
- `DimAuto` needs intimate knowledge of each subtype's behavior
- Testing is complicated by the interdependencies

## Completed Refactoring

### Phase 1: `GetReferencedViews()` (✅ Done)

Added `GetReferencedViews()` virtual method to `Dim` and `Pos` base classes to decouple `CollectDim`/`CollectPos` from specific subtypes.

**Files Changed:**
- `Dim.cs` - Added virtual `GetReferencedViews()`
- `DimView.cs` - Override returns `Target`
- `DimFill.cs` - Override returns `To` if set
- `DimCombine.cs` - Override aggregates from `Left` and `Right`
- `Pos.cs` - Added virtual `GetReferencedViews()`
- `PosView.cs` - Override returns `Target`
- `PosFunc.cs` - Override returns `View` if set  
- `PosCombine.cs` - Override aggregates from `Left` and `Right`
- `View.Layout.cs` - Simplified `CollectDim` and `CollectPos`

**Result:** `CollectDim` and `CollectPos` no longer need to know about `DimView`, `DimFill`, `DimCombine`, `PosView`, `PosCombine`.

## Proposed Future Refactoring

### Phase 2: `DependsOnSuperViewContentSize`

Add a virtual method to indicate whether a `Pos`/`Dim` depends on the SuperView's content size.

```csharp
// In Dim base class
internal virtual bool DependsOnSuperViewContentSize => false;

// Overrides:
// DimPercent => true
// DimFill => true (without MinimumContentDim)
// PosCenter => true  
// PosPercent => true
```

**Benefit:** `DimAuto` can categorize subviews without type checking:
```csharp
// Before
if (v.Width is DimPercent || v.Width is DimFill) { ... }

// After
if (v.Width.DependsOnSuperViewContentSize) { ... }
```

### Phase 3: `CanContributeToAutoSizing`

Add a method to indicate whether a `Dim` can contribute to auto-sizing calculations.

```csharp
internal virtual bool CanContributeToAutoSizing => true;

// DimFill without MinimumContentDim or To => false
// DimPercent => false (without other content, would be 0)
```

### Phase 4: `GetMinimumContribution()`

Move the auto-sizing contribution calculation into each `Dim` type:

```csharp
internal virtual int GetMinimumContribution(int location, int superviewContentSize, View us, Dimension dimension)
{
    return Calculate(location, superviewContentSize, us, dimension);
}

// DimFill override:
internal override int GetMinimumContribution(...)
{
    if (MinimumContentDim is { })
        return MinimumContentDim.Calculate(...);
    return 0;
}
```

### Phase 5: Categorization Methods

Add methods for the different categories used in `DimAuto`:

```csharp
// Whether this Pos/Dim is "fixed" (doesn't depend on layout)
internal virtual bool IsFixed => false;
// PosAbsolute, DimAbsolute => true

// Whether this requires the view to be laid out first
internal virtual bool RequiresTargetLayout => false;  
// PosView, DimView => true
```

## Benefits

1. **Single Responsibility**: Each `Pos`/`Dim` type knows its own behavior
2. **Open/Closed**: Adding new types doesn't require modifying `DimAuto`
3. **Testability**: Each type's behavior can be tested in isolation
4. **Maintainability**: Related code is co-located

## Implementation Order

1. ✅ `GetReferencedViews()` - Foundation for view dependencies
2. 🔲 `ReferencesOtherViews()` simplification - Use `GetReferencedViews().Any()` as default
3. 🔲 `DependsOnSuperViewContentSize` - Reduce type checking in `DimAuto`
4. 🔲 `CanContributeToAutoSizing` - Simplify auto-sizing logic
5. 🔲 `GetMinimumContribution()` - Move calculation logic into types
6. 🔲 `IsFixed` / `RequiresTargetLayout` - Final decoupling

## Testing Strategy

Each phase should include:
- Unit tests for new virtual methods in each subtype
- Regression tests ensuring layout behavior is unchanged
- Integration tests for `DimAuto` scenarios

## Related Files

- `Terminal.Gui/ViewBase/Layout/Dim.cs`
- `Terminal.Gui/ViewBase/Layout/Pos.cs`
- `Terminal.Gui/ViewBase/Layout/DimAuto.cs`
- `Terminal.Gui/ViewBase/View.Layout.cs`
- All `Dim*` and `Pos*` subtype files
