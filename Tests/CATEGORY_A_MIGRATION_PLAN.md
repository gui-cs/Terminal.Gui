# Category A Migration Plan - Detailed Scope

## Overview

Per @tig's request, migrating ALL Category A tests (Drawing/LineCanvas, Drawing/Ruler, View/Adornment/*, View/Draw/*) to parallelizable. These should ALL be unit tests with no Application dependency.

## Detailed Analysis

### 1. LineCanvasTests.cs (1426 lines, 33 SetupFakeDriver uses)

**Test Categories:**
- **13 tests use ToString() directly** - Can migrate immediately, no driver needed
  - Length_0_Is_1_Long, Length_n_Is_n_Long, Length_Negative, Length_Zero_*, ToString_*, Add_2_Lines
- **16 tests use GetCanvas() + View.Draw()** - Require refactoring
  - TestLineCanvas_Window_*, TestLineCanvas_LeaveMargin_*, Viewport_*, Canvas_Updates_On_Changes

**Migration Strategy:**
1. Port 13 ToString() tests directly to Parallelizable (straightforward)
2. For GetCanvas() tests: LineCanvas has GetMap() method - can test directly without View
3. Delete old LineCanvasTests.cs after migration

**Estimated Effort:** 3-4 hours

### 2. RulerTests.cs

**Status:** Need to analyze
**Estimated Effort:** 1-2 hours

### 3. View/Adornment/*.cs (5 files)

Files:
- AdornmentTests.cs
- BorderTests.cs  
- MarginTests.cs
- PaddingTests.cs
- ShadowStyleTests.cs

**Status:** Need to analyze each
**Estimated Effort:** 3-4 hours total

### 4. View/Draw/*.cs (5 files)

Files:
- AllViewsDrawTests.cs
- ClearViewportTests.cs
- ClipTests.cs
- DrawTests.cs
- TransparentTests.cs

**Status:** Need to analyze each
**Estimated Effort:** 4-5 hours total

## Total Estimated Effort: 11-15 hours

## Immediate Action Plan

### Phase 1: LineCanvasTests (Starting Now)
1. Create Tests/UnitTestsParallelizable/Drawing/LineCanvasTests.cs
2. Migrate all 13 ToString() tests (30 minutes)
3. Refactor 16 GetCanvas() tests to use GetMap() directly (2 hours)
4. Add any missing test coverage (1 hour)
5. Delete Tests/UnitTests/Drawing/LineCanvasTests.cs
6. Test and verify (30 minutes)

### Phase 2: RulerTests
Similar approach

### Phase 3: Adornment Tests
Systematic file-by-file migration

### Phase 4: View/Draw Tests  
Systematic file-by-file migration

## Current Status
- Starting Phase 1: LineCanvasTests migration
- Created Drawing directory in UnitTestsParallelizable
- Ready to begin test creation

