# Testing Patterns

**Source:** [CONTRIBUTING.md - Testing Requirements](../../CONTRIBUTING.md#testing-requirements)

## Code Coverage

- **Never decrease code coverage** - PRs must maintain or increase coverage
- Target: **70%+ coverage** for new code
- Coverage collection:
  - Centralized in `TestResults/` directory at repository root
  - Collected only on Linux (ubuntu-latest) runners in CI for performance
  - Windows and macOS runners skip coverage collection to reduce execution time
  - Coverage reports uploaded to Codecov automatically from Linux runner
  - CI monitors coverage on each PR

## Test Patterns

**⚠️ AI-created tests MUST follow these patterns exactly:**

1. **Add comment indicating the test was AI generated**
   - Example: `// CoPilot - ChatGPT v4`

2. **Make tests granular**
   - Each test should cover smallest area possible

3. **Follow existing test patterns**
   - Study tests in respective test projects before writing new ones

4. **Avoid adding new tests to `UnitTests` Project**
   - Make them parallelizable and add them to `UnitTestsParallelizable`

5. **Avoid static dependencies**
   - DO NOT use the legacy/static `Application` API or `ConfigurationManager` in tests unless the tests explicitly test related functionality

6. **Don't use `[AutoInitShutdown]` or `[SetupFakeApplication]`**
   - Legacy pattern, being phased out

## Test Projects

### 1. Non-Parallel Tests (`Tests/UnitTests/`)

**When to use:**
- Testing functionality that depends on static state
- Testing `Application.Init` and `Application.Shutdown`

**Characteristics:**
- ~10 min timeout
- Uses `Application.Init` and static state
- Cannot run in parallel
- Includes `--diagnostic` flag for logging

**Command:**
```bash
dotnet test --project Tests/UnitTests --no-build --verbosity normal
```

### 2. Parallel Tests (`Tests/UnitTestsParallelizable/`) - **PREFERRED**

**When to use:**
- All new tests should go here unless they explicitly need static state

**Characteristics:**
- ~10 min timeout
- No dependencies on static state
- Can run concurrently
- Faster execution

**Command:**
```bash
dotnet test --project Tests/UnitTestsParallelizable --no-build --verbosity normal
```

### 3. Integration Tests (`Tests/IntegrationTests/`)

**When to use:**
- Testing cross-component interactions
- End-to-end scenarios

**Command:**
```bash
dotnet test --project Tests/IntegrationTests --no-build --verbosity normal
```

## Test Configuration Files

- `xunit.runner.json` - xUnit configuration
- `coverlet.runsettings` - Coverage settings (OpenCover format)

## Example Test Pattern

```csharp
// CoPilot - ChatGPT v4
[Fact]
public void MyFeature_ShouldBehaveProperly_WhenConditionMet ()
{
    // Arrange
    View view = new () { Width = 10, Height = 5 };
    
    // Act
    view.Draw ();
    
    // Assert
    Assert.Equal (10, view.Width);
}
```
