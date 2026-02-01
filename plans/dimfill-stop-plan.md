# DimFill "to:" Parameter Implementation Plan

## Overview
Extend `Dim.Fill` to support an optional `to:` parameter that allows a view to fill up to (but not including) another view's position.

## Current Issue
Currently, developers must use `Dim.Func` to calculate fill dimensions that stop at another view:
```cs
Width = Dim.Fill (Dim.Func (_ => otherView.Frame.Width))
```

## Proposed Solution
Add a `to:` parameter to `DimFill`:
```cs
Width = Dim.Fill (to: otherView)
// or with margin:
Width = Dim.Fill (margin: 0, to: otherView)
```

## Implementation Changes

### 1. Update DimFill Record
**File:** `Terminal.Gui/ViewBase/Layout/DimFill.cs`

- ✅ Add `View? To` parameter to record definition
- ✅ Update `Calculate` method to use `to.Frame.X` or `to.Frame.Y` as endpoint
- ✅ Update `ToString()` method to include `to` information
- ✅ Update XML documentation
- ✅ Add `ReferencesOtherViews()` override
- ✅ Use StringBuilder for performance (code review feedback)

### 2. Update Dim.Fill Factory Methods
**File:** `Terminal.Gui/ViewBase/Layout/Dim.cs`

Added overloads:
- ✅ `Fill (View to)`
- ✅ `Fill (Dim margin, View to)`
- ✅ `Fill (Dim margin, Dim? minimumContentDim, View to)`
- ✅ Use Dim.Absolute(0) for consistency (code review feedback)

### 3. Add Unit Tests
**File:** `Tests/UnitTestsParallelizable/ViewBase/Layout/Dim.FillTests.cs`

Added 14 new tests covering:
- ✅ Fill to another view's X position (width)
- ✅ Fill to another view's Y position (height)
- ✅ Fill with margin and to parameter
- ✅ Fill with minimumContentDim and to parameter
- ✅ The example from the issue (#4656)
- ✅ Edge cases (negative results return 0)
- ✅ ToString() output
- ✅ ReferencesOtherViews() behavior

All tests passing: 40/40 DimFillTests, 395/395 Dim tests, 2,387/2,387 ViewBase tests, 13,402/13,402 parallelizable tests

## Usages of Dim.Func That Could Be Simplified

Below is a comprehensive list of locations where `Dim.Func` is used with `Frame.Width`, `Frame.Height`, `Frame.X`, or `Frame.Y` that could potentially be simplified with the new `to:` parameter:

### Height with Dim.Fill(Dim.Func(...Frame.Height))

1. **Examples/UICatalog/UICatalogRunnable.cs:397**
   ```cs
   Height = Dim.Fill (Dim.Func (v => v!.Frame.Height, _statusBar))
   ```
   Could become: `Height = Dim.Fill (to: _statusBar)`

2. **Examples/UICatalog/UICatalogRunnable.cs:481**
   ```cs
   Height = Dim.Fill (Dim.Func (v => v!.Frame.Height, _statusBar))
   ```
   Could become: `Height = Dim.Fill (to: _statusBar)`

3. **Examples/UICatalog/Scenarios/ConfigurationEditor.cs:43**
   ```cs
   Height = Dim.Fill (Dim.Func (_ => statusBar.Frame.Height))
   ```
   Could become: `Height = Dim.Fill (to: statusBar)`

4. **Examples/UICatalog/Scenarios/ViewportSettings.cs:35**
   ```cs
   Height = Dim.Fill (Dim.Func (_ => mainWindow.IsInitialized ? viewportSettingsEditor.Frame.Height : 1))
   ```
   Could become: `Height = Dim.Fill (to: viewportSettingsEditor)` (with conditional logic handled separately)

5. **Terminal.Gui/ViewBase/View.ScrollBars.cs:90**
   ```cs
   Height = Dim.Fill (Dim.Func (_ => Padding!.Thickness.Bottom))
   ```
   This is using Padding thickness, not a view's Frame, so NOT a candidate for simplification.

### Width with Dim.Fill(Dim.Func(...Frame.Width))

6. **Examples/UICatalog/Scenarios/Shortcuts.cs:151**
   ```cs
   Width = Dim.Fill (Dim.Func (_ => eventLog.Frame.Width))
   ```
   Could become: `Width = Dim.Fill (to: eventLog)`

7. **Examples/UICatalog/Scenarios/ViewportSettings.cs:34**
   ```cs
   Width = Dim.Fill (Dim.Func (_ => mainWindow.IsInitialized ? adornmentsEditor.Frame.Width + 1 : 1))
   ```
   Could become: `Width = Dim.Fill (margin: 1, to: adornmentsEditor)` (with conditional logic handled separately)

8. **Examples/UICatalog/Scenarios/Adornments.cs:42**
   ```cs
   Width = Dim.Fill (Dim.Func (_ => editor.Frame.Width))
   ```
   Could become: `Width = Dim.Fill (to: editor)`

9. **Examples/UICatalog/Scenarios/CharacterMap/CharacterMap.cs:182**
   ```cs
   Width = Dim.Fill (Dim.Func (v => v!.Frame.Width, _categoryList))
   ```
   Could become: `Width = Dim.Fill (to: _categoryList)`

10. **Terminal.Gui/Views/FileDialogs/FileDialog.cs:1393**
    ```cs
    Width = Dim.Fill (Dim.Func (_ => IsInitialized ? _tableViewContainer!.Frame.Width - 30 : 30))
    ```
    This has complex logic with margin calculation, but core could be: `Width = Dim.Fill (margin: 30, to: _tableViewContainer)` (with conditional logic for IsInitialized)

11. **Terminal.Gui/ViewBase/View.ScrollBars.cs:106**
    ```cs
    Width = Dim.Fill (Dim.Func (_ => Padding!.Thickness.Right))
    ```
    This is using Padding thickness, not a view's Frame, so NOT a candidate for simplification.

### Other Dim.Func Usages (NOT candidates)

The following use `Dim.Func` but are NOT good candidates for the `to:` parameter because they:
- Don't reference Frame.Width/Height/X/Y of another view
- Perform complex calculations
- Use dynamic calculations based on content

- Examples/UICatalog/Scenarios/AllViewsTester.cs:164 (complex calculation)
- Examples/UICatalog/Scenarios/ScrollBarDemo.cs (multiple, calculating label widths)
- Examples/UICatalog/Scenarios/Shortcuts.cs:49 (complex Min calculation)
- Examples/UICatalog/Scenarios/EditorsAndHelpers/* (calculating offsets and widths)
- Terminal.Gui/Views/* (various complex calculations)

## Summary

**Total candidates for simplification: 10 locations**

These will serve as validation that the new feature provides value. We won't necessarily update all of them in this PR (to keep changes minimal), but documenting them shows the feature's utility.

## Testing Strategy

1. ✅ Create comprehensive unit tests covering all scenarios
2. ✅ Ensure backward compatibility (existing code continues to work)
3. ✅ Validate the example from issue #4656 works correctly
4. ✅ Run full test suite to ensure no regressions

## Implementation Steps

1. ✅ Create this plan document
2. ✅ Modify `DimFill` record to add `to:` parameter
3. ✅ Update `Dim.Fill()` factory methods
4. ✅ Add comprehensive unit tests
5. ✅ Build and test the changes
6. ✅ Run code review
7. ✅ Run security scan (codeql)
8. ✅ Final validation

## Results

- **All tests passing:** 13,402 parallelizable tests, including 40 DimFillTests
- **Code review:** Completed, all feedback addressed
- **Security scan:** Passed (no issues detected)
- **Backward compatibility:** Maintained (all existing tests pass)
- **Documentation:** XML documentation added to all new methods

## Files Changed

1. `Terminal.Gui/ViewBase/Layout/DimFill.cs` - Added To parameter, updated Calculate and ToString
2. `Terminal.Gui/ViewBase/Layout/Dim.cs` - Added 3 new factory method overloads
3. `Tests/UnitTestsParallelizable/ViewBase/Layout/Dim.FillTests.cs` - Added 14 new tests
4. `plans/dimfill-stop-plan.md` - This document
