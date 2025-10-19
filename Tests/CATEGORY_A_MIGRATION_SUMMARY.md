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
- Draw_Default (requires Application.Init)
- Draw_Horizontal (requires SetupFakeDriver)
- Draw_Vertical (requires SetupFakeDriver)

## Key Findings

### 1. LineCanvas and Rendering Dependencies
**Issue:** LineCanvas.ToString() internally calls GetMap() which calls GetRuneForIntersects(Application.Driver). The glyph resolution depends on Application.Driver for:
- Configuration-dependent glyphs (Glyphs class)
- Line intersection character selection
- Style-specific characters (Single, Double, Heavy, etc.)

**Solution:** Only migrate tests that check properties and behavior without verifying rendered output. Tests that verify visual output require Application.Driver and must remain as integration tests.

### 2. Test Categories
Tests fall into three categories:

**a) Pure Unit Tests (CAN be parallelized):**
- Tests of properties (Bounds, Lines, Length, Orientation, Attribute, Fill)
- Tests of basic operations (AddLine, Clear, Exclude, ClearExclusions)
- Tests that don't require rendering or Application context

**b) Rendering Tests (CANNOT be parallelized):**
- Tests using View.Draw()
- Tests that verify ToString() output with specific glyphs
- Tests using GetCanvas() helper that creates View and uses DrawComplete event

**c) Integration Tests (SHOULD NOT be parallelized):**
- Tests using [AutoInitShutdown] or [SetupFakeDriver]
- Tests that validate component behavior within Application context
- Tests that require ConfigurationManager, keyboard/mouse input, or driver-specific behavior

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
When migrating tests, use this checklist:
- ✅ Tests properties without rendering
- ✅ Tests basic operations (add, remove, clear)
- ✅ Tests constructors and defaults
- ❌ Uses [AutoInitShutdown] or [SetupFakeDriver]
- ❌ Calls View.Draw(), View.LayoutAndDraw(), or rendering methods
- ❌ Verifies visual output (ToString(), DriverAssert)
- ❌ Requires Application context, ConfigurationManager, or driver

### 2. Documentation
Update test documentation to clarify:
- **UnitTests** = Integration tests that validate components within Application context
- **UnitTests.Parallelizable** = Pure unit tests with no global state dependencies
- Provide examples of each type

### 3. New Test Development
- Default to UnitTests.Parallelizable for new tests unless they require Application/Driver
- When testing rendering, create both:
  - Pure unit test (properties, behavior) in Parallelizable
  - Integration test (rendering) in UnitTests

### 4. Remaining Category A Tests
**Status:** Analyzed and determined NOT to migrate

**Rationale:**
- View/Adornment/* tests (19 tests): All test View.Draw() with visual verification - integration tests
- View/Draw/* tests (32 tests): All test View rendering and visual output - integration tests
- These tests correctly belong in UnitTests as they validate component integration

## Conclusion

This migration successfully identified and moved 35 pure unit tests from Category A to UnitTests.Parallelizable. The remaining tests in Category A are integration tests that correctly belong in UnitTests as they validate rendering and component behavior within the Application context.

The approach taken was surgical and focused on tests that clearly don't require global state. This ensures the migrated tests can run in parallel without interference while maintaining the integrity of integration tests that validate the full component behavior.

**Migration Rate:** 35 out of ~86 Category A tests (41%) were identified as pure unit tests and migrated. The remaining 59% are integration tests that should remain in UnitTests.
