# Test Parallelization Analysis

## Summary

This document provides an analysis of the test parallelization effort for Terminal.Gui. The goal is to identify tests in `UnitTests` that can be moved to `UnitTests.Parallelizable` and remove duplicates.

## Key Findings

### Test Counts
- **UnitTests**: ~1446 tests (1213 Fact, 233 Theory) across 140 files
- **UnitTestsParallelizable**: ~1437 tests (1078 Fact, 359 Theory) across 142 files
- **Tests using [AutoInitShutdown]**: 452
- **Tests using [SetupFakeDriver]**: 206

### What Prevents Parallelization

Tests cannot be parallelized if they:
1. Use `[AutoInitShutdown]` - requires `Application.Init/Shutdown` which creates global state
2. Use `[SetupFakeDriver]` - sets `Application.Driver` which is global
3. Call `Application.Init()` directly
4. Modify `ConfigurationManager` global state (Enable/Load/Apply/Disable)
5. Modify static properties like `Key.Separator`, `CultureInfo.CurrentCulture`, etc.
6. Use `Application.Top`, `Application.Driver`, `Application.MainLoop` or other singleton state
7. Are integration tests that test multiple components working together

### Test Files Analysis

#### Files Without AutoInitShutdown or SetupFakeDriver (49 files)

Many of these still cannot be parallelized because they use global state:

**Configuration Tests (8 files)**:
- `SchemeManagerTests.cs` - Uses ConfigurationManager.Enable/Disable - NOT parallelizable
- `ConfigPropertyTests.cs` - Different from Parallelizable version, complementary
- `AppScopeTests.cs` - Needs analysis
- `ThemeManagerTests.cs` - Uses ConfigurationManager - NOT parallelizable
- `KeyJsonConverterTests.cs` - Different from Parallelizable version
- `ThemeScopeTests.cs` - Different from Parallelizable version
- `GlyphTests.cs` - Uses ConfigurationManager - NOT parallelizable
- `ConfigurationMangerTests.cs` - Different from Parallelizable version
- `SettingsScopeTests.cs` - Different from Parallelizable version

**Input Tests (1 file)**:
- `KeyTests.cs` - Modifies `Key.Separator` static property with comment noting it can't be parallelized - CANNOT BE MOVED

**Application Tests (7 files)**:
- `MainLoopTests.cs` - Needs analysis
- `MainLoopCoordinatorTests.cs` - Needs analysis
- `StackExtensionsTests.cs` - **MOVED TO PARALLELIZABLE** ✅
- `ApplicationPopoverTests.cs` - Needs analysis
- `ApplicationMouseEnterLeaveTests.cs` - Needs analysis
- `MainLoopTTests.cs` - Needs analysis
- `ApplicationImplTests.cs` - Needs analysis

**View/Component Tests (8 files)**:
- `SliderTests.cs` - Needs analysis
- `Menuv1Tests.cs` - Likely uses Application state
- `TabTests.cs` - Needs analysis
- `TimeFieldTests.cs` - Needs analysis
- `TextValidateFieldTests.cs` - Needs analysis
- `HexViewTests.cs` - Needs analysis
- `ViewCommandTests.cs` - Different from Parallelizable, complementary
- `ViewportSettings.TransparentMouseTests.cs` - Needs analysis

**View Internal Tests (6 files)**:
- `AdornmentSubViewTests.cs` - Needs analysis
- `DiagnosticsTests.cs` - Needs analysis
- `Dim.FillTests.cs` - Needs analysis
- `Pos.ViewTests.cs` - Needs analysis
- `Pos.Tests.cs` - Needs analysis
- `GetViewsUnderLocationTests.cs` - Needs analysis

**Console Driver Tests (13 files)**:
- `MainLoopDriverTests.cs` - Needs analysis
- `AnsiKeyboardParserTests.cs` - Needs analysis
- `ConsoleDriverTests.cs` - Creates driver instances, likely NOT parallelizable
- `DriverColorTests.cs` - Needs analysis
- `ConsoleInputTests.cs` - Needs analysis
- `ContentsTests.cs` - Needs analysis
- `ClipRegionTests.cs` - Needs analysis
- `NetInputProcessorTests.cs` - Needs analysis
- `KeyCodeTests.cs` - Uses `Application.QuitKey` - NOT parallelizable
- `MouseInterpreterTests.cs` - Needs analysis
- `WindowSizeMonitorTests.cs` - Needs analysis
- `AddRuneTests.cs` - Calls `driver.Init()` - likely NOT parallelizable
- `AnsiResponseParserTests.cs` - Needs analysis
- `WindowsInputProcessorTests.cs` - Needs analysis
- `AnsiMouseParserTests.cs` - Needs analysis
- `AnsiRequestSchedulerTests.cs` - Needs analysis
- `ConsoleScrolllingTests.cs` - Needs analysis

**Resource Tests (1 file)**:
- `ResourceManagerTests.cs` - Modifies `CultureInfo.CurrentCulture` - NOT parallelizable

**Other Tests (5 files)**:
- `EscSeqRequestsTests.cs` - Needs analysis
- Drawing tests mostly use AutoInitShutdown

### Pattern Analysis

**Complementary vs Duplicate Tests**:

Most test files with the same name in both projects are **COMPLEMENTARY, NOT DUPLICATES**:
- **UnitTests** typically contains integration tests that test components working with `Application`, drivers, and ConfigurationManager
- **UnitTestsParallelizable** typically contains unit tests that test components in isolation without global state

Examples:
- `ThicknessTests.cs`:
  - UnitTests: Tests `Draw()` method with Application.Driver (255 lines)
  - Parallelizable: Tests constructors, properties, operators (619 lines)
  
- `ViewCommandTests.cs`:
  - UnitTests: Tests Button clicks with Application mouse events
  - Parallelizable: Tests Command pattern in isolation
  
- `ConfigPropertyTests.cs`:
  - UnitTests: Tests Apply() with static properties
  - Parallelizable: Tests concurrent access patterns

## Completed Work

✅ **StackExtensionsTests.cs** (10 tests, 195 lines)
- Pure unit test of Stack extension methods
- No dependencies on Application or ConfigurationManager
- Successfully moved from UnitTests to UnitTestsParallelizable
- All tests pass in parallelizable project

## Recommendations

### Immediate Actions
1. Most tests in UnitTests should REMAIN there as they are integration tests
2. Focus on identifying truly duplicated tests rather than moving tests
3. Tests that modify global state cannot be parallelized

### Long-term Strategy
1. **Documentation**: Create clear guidelines on when tests belong in each project
2. **Naming Convention**: Consider renaming to make the distinction clear (e.g., `IntegrationTests` vs `UnitTests`)
3. **New Test Guidelines**: All new tests should be written for UnitTests.Parallelizable unless they require global state

### Tests That MUST Stay in UnitTests
- Any test using `[AutoInitShutdown]` or `[SetupFakeDriver]`
- Any test that calls `Application.Init()` or `Application.Shutdown()`
- Any test that uses `Application.Driver`, `Application.Top`, `Application.MainLoop`
- Any test that modifies `ConfigurationManager` state
- Any test that modifies static properties
- Integration tests that test multiple components together

### Candidates for Further Analysis
The following files need deeper analysis to determine if they can be moved or have duplicates:
- MainLoop related tests
- Some View component tests that might not use global state
- Some Console driver tests that might be pure unit tests

## Scope Assessment

Given the analysis:
- ~1446 tests in UnitTests
- 452 use [AutoInitShutdown]
- 206 use [SetupFakeDriver]
- Most remaining tests use Application or ConfigurationManager state

**Estimate**: Only 5-10% of tests (50-150 tests) could potentially be moved to Parallelizable, and many of those already have complementary versions there. This is a massive undertaking that would require:
- Detailed analysis of each of ~140 test files
- Understanding the intent of each test
- Determining if tests are duplicates or complementary
- Rewriting tests to remove dependencies on global state where possible
- Extensive testing to ensure nothing breaks

This would easily be 40-80 hours of careful, methodical work.
