# Category A Migration Summary

## Overview

This document summarizes the Category A test migration effort to move parallelizable unit tests from `UnitTests` to `UnitTests.Parallelizable`.

## Tests Migrated: 35

### Drawing/LineCanvasTests.cs: 31 tests
**Migrated pure unit tests that don't require Application.Driver:**
- ToString_Empty (1 test)
- Clear_Removes_All_Lines (1 test)
- Lines_Property_Returns_ReadOnly_Collection (1 test)
- AddLine_Adds_Line_To_Collection (1 test)
- Constructor_With_Lines_Creates_Canvas_With_Lines (1 test)
- Viewport_H_And_V_Lines_Both_Positive (7 test cases)
- Viewport_H_Line (7 test cases)
- Viewport_Specific (1 test)
- Bounds_Empty_Canvas_Returns_Empty_Rectangle (1 test)
- Bounds_Single_Point_Zero_Length (1 test)
- Bounds_Horizontal_Line (1 test)
- Bounds_Vertical_Line (1 test)
- Bounds_Multiple_Lines_Returns_Union (1 test)
- Bounds_Negative_Length_Line (1 test)
- Bounds_Complex_Box (1 test)
- ClearExclusions_Clears_Exclusion_Region (1 test)
- Exclude_Removes_Points_From_Map (1 test)
- Fill_Property_Can_Be_Set (1 test)
- Fill_Property_Defaults_To_Null (1 test)

**Tests that remain in UnitTests as integration tests:**
- All tests using GetCanvas() and View.Draw() (16 tests)
- Tests that verify rendered output (ToString with specific glyphs) - these require Application.Driver for glyph resolution

### Drawing/RulerTests.cs: 4 tests
**Migrated pure unit tests:**
- Constructor_Defaults
- Attribute_Set
- Length_Set
- Orientation_Set

**Tests that remain in UnitTests as integration tests:**
- Draw_Default (requires Application.Init with [AutoInitShutdown])
- Draw_Horizontal (uses [SetupFakeDriver] - could potentially be migrated)
- Draw_Vertical (uses [SetupFakeDriver] - could potentially be migrated)

## Key Findings

### 1. LineCanvas and Rendering Dependencies
**Issue:** LineCanvas.ToString() internally calls GetMap() which calls GetRuneForIntersects(Application.Driver). The glyph resolution depends on Application.Driver for:
- Configuration-dependent glyphs (Glyphs class)
- Line intersection character selection
- Style-specific characters (Single, Double, Heavy, etc.)

**Solution:** Tests using [SetupFakeDriver] CAN be parallelized as long as they don't use Application statics. This includes rendering tests that verify visual output with DriverAssert.

### 2. Test Categories
Tests fall into three categories:

**a) Pure Unit Tests (CAN be parallelized):**
- Tests of properties (Bounds, Lines, Length, Orientation, Attribute, Fill)
- Tests of basic operations (AddLine, Clear, Exclude, ClearExclusions)
- Tests that don't require Application static context

**b) Rendering Tests with [SetupFakeDriver] (CAN be parallelized):**
- Tests using [SetupFakeDriver] without Application statics
- Tests using View.Draw() and LayoutAndDraw() without Application statics
- Tests that verify visual output with DriverAssert (when using [SetupFakeDriver])
- Tests using GetCanvas() helper as long as Application statics are not used

**c) Integration Tests (CANNOT be parallelized):**
- Tests using [AutoInitShutdown]
- Tests using Application.Begin, Application.RaiseKeyDownEvent, or other Application static methods
- Tests that validate component behavior within full Application context
- Tests that require ConfigurationManager or Application.Navigation

### 3. View/Adornment and View/Draw Tests
**Finding:** After analyzing these tests, they all use [SetupFakeDriver] and test View.Draw() with visual verification. These are integration tests that validate how adornments render within the View system. They correctly belong in UnitTests.

**Recommendation:** Do NOT migrate these tests. They are integration tests by design and require the full Application/Driver context.

## Test Results

### UnitTests.Parallelizable
- **Before:** 9,360 tests passing
- **After:** 9,395 tests passing (+35)
- **Result:** ✅ All tests pass

### UnitTests
- **Status:** 3,488 tests passing (unchanged)
- **Result:** ✅ No regressions

## Recommendations for Future Work

### 1. Continue Focused Migration

**Tests CAN be parallelized if they:**
- ✅ Test properties, constructors, and basic operations
- ✅ Use [SetupFakeDriver] without Application statics
- ✅ Call View.Draw(), LayoutAndDraw() without Application statics
- ✅ Verify visual output with DriverAssert (when using [SetupFakeDriver])
- ✅ Create View hierarchies without Application.Top
- ✅ Test events and behavior without global state

**Tests CANNOT be parallelized if they:**
- ❌ Use [AutoInitShutdown] (requires Application.Init/Shutdown global state)
- ❌ Set Application.Driver (global singleton)
- ❌ Call Application.Init(), Application.Run/Run<T>(), or Application.Begin()
- ❌ Modify ConfigurationManager global state (Enable/Load/Apply/Disable)
- ❌ Modify static properties (Key.Separator, CultureInfo.CurrentCulture, etc.)
- ❌ Use Application.Top, Application.Driver, Application.MainLoop, or Application.Navigation
- ❌ Are true integration tests testing multiple components together

**Important Notes:**
- Many tests blindly use the above when they don't need to and CAN be rewritten
- Many tests APPEAR to be integration tests but are just poorly written and can be split
- When in doubt, analyze if the test truly needs global state or can be refactored

### 2. Documentation
Update test documentation to clarify:
- **UnitTests** = Integration tests that validate components within Application context
- **UnitTests.Parallelizable** = Pure unit tests with no global state dependencies
- Provide examples of each type

### 3. New Test Development
- Default to UnitTests.Parallelizable for new tests unless they require Application/Driver
- When testing rendering, create both:
  - Pure unit test (properties, behavior) in Parallelizable
  - Rendering test with [SetupFakeDriver] can also go in Parallelizable (as long as Application statics are not used)
  - Integration test (Application context) in UnitTests

### 4. Remaining Category A Tests
**Status:** Can be re-evaluated for migration

**Rationale:**
- View/Adornment/* tests (19 tests): Use [SetupFakeDriver] and test View.Draw() - CAN be migrated if they don't use Application statics
- View/Draw/* tests (32 tests): Use [SetupFakeDriver] and test rendering - CAN be migrated if they don't use Application statics
- Need to analyze each test individually to check for Application static dependencies

## Conclusion

This migration successfully identified and moved 52 tests (35 Category A + 17 Views) to UnitTests.Parallelizable. 

**Key Discovery:** Tests with [SetupFakeDriver] CAN run in parallel as long as they avoid Application statics. This significantly expands the scope of tests that can be parallelized beyond just property/constructor tests to include rendering tests.

The approach taken was to:
1. Identify tests that don't use Application.Begin, Application.RaiseKeyDownEvent, Application.Navigation, or other Application static members
2. Keep [SetupFakeDriver] tests that only use View.Draw() and DriverAssert
3. Move [AutoInitShutdown] tests only if they can be rewritten to not use Application.Begin

**Migration Rate:** 52 tests migrated so far. Many more tests with [SetupFakeDriver] can potentially be migrated once they're analyzed for Application static usage. Estimated ~3,400 tests remaining to analyze.
