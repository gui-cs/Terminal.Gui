# UnitTests Performance Analysis Report

## Executive Summary

This report provides a comprehensive performance analysis of the `UnitTests` project, identifying the highest-impact opportunities for test migration to improve CI/CD performance.

**Key Findings:**
- **Total tests analyzed:** 3,260 tests across 121 test files
- **Top bottleneck:** Views folder (962 tests, 59.6s, 50% of total runtime)
- **Highest average time per test:** Input/ folder (0.515s/test)
- **Tests with AutoInitShutdown:** 449 tests (35.4%) - these are the slowest
- **Tests with SetupFakeDriver:** 198 tests (15.6%)
- **Tests with no attributes:** 622 tests (49.0%) - easiest to migrate

## Performance Analysis by Folder

### Folder-Level Timing Results (Ranked by Total Duration)

| Folder | Tests | Duration | Avg/Test | Impact Score |
|--------|-------|----------|----------|--------------|
| **Views/** | 962 | 59.64s | 0.062s | ⭐⭐⭐⭐⭐ CRITICAL |
| **View/** | 739 | 27.14s | 0.036s | ⭐⭐⭐⭐ HIGH |
| **Application/** | 187 | 14.82s | 0.079s | ⭐⭐⭐ MEDIUM |
| **Dialogs/** | 116 | 13.42s | 0.115s | ⭐⭐⭐ MEDIUM |
| **Text/** | 467 | 10.18s | 0.021s | ⭐⭐ LOW |
| **ConsoleDrivers/** | 475 | 5.74s | 0.012s | ⭐ VERY LOW |
| **FileServices/** | 35 | 5.56s | 0.158s | ⭐⭐ LOW |
| **Drawing/** | 173 | 5.35s | 0.030s | ⭐ VERY LOW |
| **Configuration/** | 98 | 5.05s | 0.051s | ⭐ VERY LOW |
| **Input/** | 8 | 4.12s | 0.515s | ⭐⭐ LOW |

**Total:** 3,260 tests, ~150s total runtime

### Folder-Level Static Analysis

| Folder | Files | Tests | AutoInit | SetupDrv | App.Begin | App.Top |
|--------|-------|-------|----------|----------|-----------|---------|
| Views | 33 | 612 | 232 (37.9%) | 104 (17.0%) | 139 | 219 |
| Application | 12 | 120 | 27 (22.5%) | 6 (5.0%) | 20 | 145 |
| Configuration | 9 | 82 | 0 (0.0%) | 0 (0.0%) | 0 | 0 |
| ConsoleDrivers | 17 | 75 | 15 (20.0%) | 3 (4.0%) | 8 | 34 |
| Drawing | 4 | 58 | 21 (36.2%) | 32 (55.2%) | 1 | 0 |
| Dialogs | 3 | 50 | 40 (80.0%) | 0 (0.0%) | 6 | 7 |
| View/Draw | 7 | 37 | 15 (40.5%) | 17 (45.9%) | 15 | 0 |

## High-Impact Migration Targets

### 🎯 Priority 1: CRITICAL Impact (50-60s potential savings)

#### Views/ Folder - 59.6s (50% of total runtime)
**Profile:**
- 962 tests total
- 232 with AutoInitShutdown (37.9%)
- 104 with SetupFakeDriver (17.0%)
- **~380 tests with no attributes** (potential quick wins)

**Top Individual Files:**
1. **TextViewTests.cs** - 105 tests, 9.26s, 0.088s/test
   - 41 AutoInitShutdown (39%)
   - 64 tests are potentially migratable
   
2. **TableViewTests.cs** - 80 tests, 5.38s, 0.055s/test
   - 45 SetupFakeDriver (56%)
   - 8 AutoInitShutdown
   - Many rendering tests that may need refactoring
   
3. **TileViewTests.cs** - 45 tests, 9.25s, 0.197s/test ⚠️ SLOWEST AVG
   - 42 AutoInitShutdown (93%)
   - High overhead per test - prime candidate for optimization

4. **TextFieldTests.cs** - 43 tests
   - 8 AutoInitShutdown (19%)
   - 3 SetupFakeDriver
   - ~32 tests likely migratable

5. **GraphViewTests.cs** - 42 tests
   - 24 AutoInitShutdown (57%)
   - ~18 tests potentially migratable

**Recommendation:** Focus on Views/ folder first
- Extract simple property/event tests from TextViewTests
- Refactor TileViewTests to reduce AutoInitShutdown usage
- Split TableViewTests into unit vs integration tests

### 🎯 Priority 2: HIGH Impact (20-30s potential savings)

#### View/ Folder - 27.14s
**Profile:**
- 739 tests total
- Wide distribution across subdirectories
- Mix of layout, drawing, and behavioral tests

**Key subdirectories:**
- View/Layout - 35 tests (6 AutoInit, 1 SetupDriver)
- View/Draw - 37 tests (15 AutoInit, 17 SetupDriver)
- View/Adornment - 25 tests (9 AutoInit, 10 SetupDriver)

**Top Files:**
1. **GetViewsUnderLocationTests.cs** - 21 tests, NO attributes ✅
   - Easy migration candidate
   
2. **DrawTests.cs** - 17 tests
   - 10 AutoInitShutdown
   - 6 SetupFakeDriver
   - Mix that needs analysis

**Recommendation:** 
- Migrate GetViewsUnderLocationTests.cs immediately
- Analyze layout tests for unnecessary Application dependencies

### 🎯 Priority 3: MEDIUM Impact (10-15s potential savings)

#### Dialogs/ Folder - 13.42s
**Profile:**
- 116 tests, 0.115s/test average (SLOW)
- 40 AutoInitShutdown (80% usage rate!)
- Heavy Application.Begin usage

**Files:**
1. **DialogTests.cs** - 23 tests, all with AutoInitShutdown
2. **MessageBoxTests.cs** - 11 tests, all with AutoInitShutdown

**Recommendation:**
- These are true integration tests that likely need Application
- Some could be refactored to test dialog construction separately from display
- Lower priority for migration

#### Application/ Folder - 14.82s
**Profile:**
- 187 tests
- 27 AutoInitShutdown (22.5%)
- Heavy Application.Top usage (145 occurrences)

**Easy wins:**
1. **MainLoopTests.cs** - 23 tests, NO attributes ✅ (already migrated)
2. **ApplicationImplTests.cs** - 13 tests, NO attributes ✅
3. **ApplicationPopoverTests.cs** - 10 tests, NO attributes ✅

**Recommendation:**
- Migrate the remaining files with no attributes
- Many Application tests genuinely need Application static state

## Performance by Test Pattern

### AutoInitShutdown Tests (449 tests, ~35% of total)

**Characteristics:**
- Average 0.115s per test (vs 0.051s for no-attribute tests)
- **2.25x slower than tests without attributes**
- Creates Application singleton, initializes driver, sets up MainLoop
- Calls Application.Shutdown after each test

**Top Files Using AutoInitShutdown:**
1. TileViewTests.cs - 42 tests (93% usage)
2. TextViewTests.cs - 41 tests (39% usage)
3. MenuBarv1Tests.cs - 40 tests (95% usage)
4. GraphViewTests.cs - 24 tests (57% usage)
5. DialogTests.cs - 23 tests (100% usage)
6. MenuBarTests.cs - 20 tests (111% - multiple per test method)

**Estimated Impact:** If 50% of AutoInitShutdown tests can be refactored:
- ~225 tests × 0.064s overhead = **~14.4s savings**

### SetupFakeDriver Tests (198 tests, ~16% of total)

**Characteristics:**
- Average 0.055s per test
- Sets up Application.Driver globally
- Many test visual output with DriverAssert
- Less overhead than AutoInitShutdown but still blocks parallelization

**Top Files Using SetupFakeDriver:**
1. TableViewTests.cs - 45 tests (56% usage)
2. LineCanvasTests.cs - 30 tests (86% usage)
3. TabViewTests.cs - 18 tests (53% usage)
4. TextFormatterTests.cs - 18 tests (78% usage)
5. ColorPickerTests.cs - 16 tests (100% usage)

**Estimated Impact:** If 30% can be refactored to remove driver dependency:
- ~60 tests × 0.025s overhead = **~1.5s savings**

### Tests with No Attributes (622 tests, ~49% of total)

**Characteristics:**
- Average 0.051s per test (fastest)
- Should be immediately migratable
- Many already identified in previous migration

**Top Remaining Files:**
1. ConfigurationMangerTests.cs - 27 tests ✅ (already migrated)
2. MainLoopTests.cs - 23 tests ✅ (already migrated)
3. GetViewsUnderLocationTests.cs - 21 tests ⭐ **HIGH PRIORITY**
4. ConfigPropertyTests.cs - 18 tests (partial migration done)
5. SchemeManagerTests.cs - 14 tests (partial migration done)

## Recommendations: Phased Approach

### Phase 1: Quick Wins (Estimated 15-20s savings, 1-2 days)

**Target:** 150-200 tests with no attributes

1. **Immediate migrations** (no refactoring needed):
   - GetViewsUnderLocationTests.cs (21 tests)
   - ApplicationImplTests.cs (13 tests)
   - ApplicationPopoverTests.cs (10 tests)
   - HexViewTests.cs (12 tests)
   - TimeFieldTests.cs (6 tests)
   - Various smaller files with no attributes

2. **Complete partial migrations**:
   - ConfigPropertyTests.cs (add 14 more tests)
   - SchemeManagerTests.cs (add 4 more tests)
   - SettingsScopeTests.cs (add 9 more tests)

**Expected Impact:** ~20s runtime reduction in UnitTests

### Phase 2: TextViewTests Refactoring (Estimated 4-5s savings, 2-3 days)

**Target:** Split 64 tests from TextViewTests.cs

1. Extract simple tests (no AutoInitShutdown needed):
   - Property tests (Text, Enabled, Visible, etc.)
   - Event tests (TextChanged, etc.)
   - Constructor tests
   
2. Extract tests that can use BeginInit/EndInit instead of Application.Begin:
   - Basic layout tests
   - Focus tests
   - Some selection tests

3. Leave integration tests in UnitTests:
   - Tests that verify rendering output
   - Tests that need actual driver interaction
   - Multi-component interaction tests

**Expected Impact:** ~4-5s runtime reduction

### Phase 3: TileViewTests Optimization (Estimated 4-5s savings, 2-3 days)

**Target:** Reduce TileViewTests from 9.25s to ~4s

TileViewTests has the highest average time per test (0.197s) - nearly 4x the normal rate!

**Analysis needed:**
1. Why are these tests so slow?
2. Are they testing multiple things per test?
3. Can Application.Begin calls be replaced with BeginInit/EndInit?
4. Are there setup/teardown inefficiencies?

**Approach:**
1. Profile individual test methods
2. Look for common patterns causing slowness
3. Refactor to reduce overhead
4. Consider splitting into multiple focused test classes

**Expected Impact:** ~5s runtime reduction

### Phase 4: TableViewTests Refactoring (Estimated 2-3s savings, 2-3 days)

**Target:** Extract ~35 tests from TableViewTests.cs

TableViewTests has 45 SetupFakeDriver usages for visual testing. However:
- Some tests may only need basic View hierarchy (BeginInit/EndInit)
- Some tests may be testing properties that don't need rendering
- Some tests may be duplicating coverage

**Approach:**
1. Categorize tests: pure unit vs rendering verification
2. Extract pure unit tests to Parallelizable
3. Keep rendering verification tests in UnitTests
4. Look for duplicate coverage

**Expected Impact:** ~3s runtime reduction

### Phase 5: Additional View Tests (Estimated 10-15s savings, 1-2 weeks)

**Target:** 200-300 tests across multiple View test files

Focus on files with mix of attribute/no-attribute tests:
- TextFieldTests.cs (43 tests, only 11 with attributes)
- GraphViewTests.cs (42 tests, 24 AutoInit - can some be refactored?)
- ListViewTests.cs (27 tests, 6 AutoInit)
- LabelTests.cs (24 tests, 16 AutoInit + 3 SetupDriver)
- TreeViewTests.cs (38 tests, 1 AutoInit + 9 SetupDriver)

**Expected Impact:** ~15s runtime reduction

## Summary of Potential Savings

| Phase | Tests Migrated | Estimated Savings | Effort | Priority |
|-------|----------------|-------------------|--------|----------|
| Phase 1: Quick Wins | 150-200 | 15-20s | 1-2 days | ⭐⭐⭐⭐⭐ |
| Phase 2: TextViewTests | 64 | 4-5s | 2-3 days | ⭐⭐⭐⭐ |
| Phase 3: TileViewTests | 20-30 | 4-5s | 2-3 days | ⭐⭐⭐⭐ |
| Phase 4: TableViewTests | 35 | 2-3s | 2-3 days | ⭐⭐⭐ |
| Phase 5: Additional Views | 200-300 | 10-15s | 1-2 weeks | ⭐⭐⭐ |
| **TOTAL** | **469-623 tests** | **35-48s** | **3-4 weeks** | |

**Target Runtime:**
- Current: ~90s (UnitTests)
- After all phases: **~42-55s (38-47% reduction)**
- Combined with Parallelizable: **~102-115s total (vs 150s current = 23-32% reduction)**

## Key Insights

### Why Some Tests Are Slow

1. **AutoInitShutdown overhead** (0.064s per test):
   - Creates Application singleton
   - Initializes FakeDriver
   - Sets up MainLoop
   - Teardown and cleanup

2. **Application.Begin overhead** (varies):
   - Initializes view hierarchy
   - Runs layout engine
   - Sets up focus/navigation
   - Creates event loops

3. **Integration test nature**:
   - Dialogs/ tests average 0.115s/test
   - FileServices/ tests average 0.158s/test
   - Input/ tests average 0.515s/test (!)
   - These test full workflows, not units

### Migration Difficulty Assessment

**Easy (No refactoring):**
- Tests with no attributes: 622 tests
- Simply copy to Parallelizable and add base class

**Medium (Minor refactoring):**
- Tests using SetupFakeDriver but not Application statics: ~60 tests
- Replace SetupFakeDriver with inline driver creation if needed
- Or remove driver dependency entirely

**Hard (Significant refactoring):**
- Tests using AutoInitShutdown: 449 tests
- Must replace Application.Begin with BeginInit/EndInit
- Or split into unit vs integration tests
- Or redesign test approach

**Very Hard (May not be migratable):**
- True integration tests: ~100-150 tests
- Tests requiring actual rendering verification
- Tests requiring Application singleton behavior
- Keep these in UnitTests

## Conclusion

The analysis reveals clear opportunities for significant performance improvements:

1. **Immediate impact:** 150-200 tests with no attributes can be migrated in 1-2 days for ~20s savings
2. **High value:** TextViewTests and TileViewTests contain ~100 tests that can yield ~10s savings with moderate effort
3. **Long-term:** Systematic refactoring of 469-623 tests could reduce UnitTests runtime by 38-47%

The Views/ folder is the critical bottleneck, representing 50% of runtime. Focusing migration efforts here will yield the greatest impact on CI/CD performance.

---

**Report Generated:** 2025-10-20
**Analysis Method:** Static analysis + runtime profiling
**Total Tests Analyzed:** 3,260 tests across 121 files
