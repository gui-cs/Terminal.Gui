# UnitTests.Parallelizable

This project contains unit tests that can run in parallel without interference. Tests here must not depend on global state or static Application infrastructure.

## Migration Rules

### Tests CAN be parallelized if they:
- ✅ Test properties, constructors, and basic operations
- ✅ Use `[SetupFakeDriver]` without Application statics
- ✅ Call `View.Draw()`, `LayoutAndDraw()` without Application statics
- ✅ Verify visual output with `DriverAssert` (when using `[SetupFakeDriver]`)
- ✅ Create View hierarchies without `Application.Top`
- ✅ Test events and behavior without global state
- ✅ Use `View.BeginInit()` / `View.EndInit()` for initialization

### Tests CANNOT be parallelized if they:
- ❌ Use `[AutoInitShutdown]` - requires `Application.Init/Shutdown` which creates global state
- ❌ Set `Application.Driver` (global singleton)
- ❌ Call `Application.Init()`, `Application.Run/Run<T>()`, or `Application.Begin()`
- ❌ Modify `ConfigurationManager` global state (Enable/Load/Apply/Disable)
- ❌ Modify static properties like `Key.Separator`, `CultureInfo.CurrentCulture`, etc.
- ❌ Use `Application.Top`, `Application.Driver`, `Application.MainLoop`, or `Application.Navigation`
- ❌ Are true integration tests that test multiple components working together

### Important Notes
- Many tests in `UnitTests` blindly use the above patterns when they don't actually need them
- These tests CAN be rewritten to remove unnecessary dependencies and migrated here
- Many tests APPEAR to be integration tests but are just poorly written and cover multiple surface areas - these can be split into focused unit tests
- When in doubt, analyze if the test truly needs global state or can be refactored

## How to Migrate Tests

1. **Identify** tests in `UnitTests` that don't actually need Application statics
2. **Rewrite** tests to remove `[AutoInitShutdown]`, `Application.Begin()`, etc. if not needed
3. **Move** the test to the equivalent file in `UnitTests.Parallelizable`
4. **Delete** the old test from `UnitTests` to avoid duplicates
5. **Verify** no duplicate test names exist (CI will check this)
6. **Test** to ensure the migrated test passes

## Example Migrations

### Simple Property Test (no changes needed)
```csharp
// Before (in UnitTests)
[Fact]
public void Constructor_Sets_Defaults ()
{
    var view = new Button ();
    Assert.Empty (view.Text);
}

// After (in UnitTests.Parallelizable) - just move it!
[Fact]
public void Constructor_Sets_Defaults ()
{
    var view = new Button ();
    Assert.Empty (view.Text);
}
```

### Remove Unnecessary [SetupFakeDriver]
```csharp
// Before (in UnitTests)
[Fact]
[SetupFakeDriver]
public void Event_Fires_When_Property_Changes ()
{
    var view = new Button ();
    var fired = false;
    view.TextChanged += (s, e) => fired = true;
    view.Text = "Hello";
    Assert.True (fired);
}

// After (in UnitTests.Parallelizable) - remove attribute!
[Fact]
public void Event_Fires_When_Property_Changes ()
{
    var view = new Button ();
    var fired = false;
    view.TextChanged += (s, e) => fired = true;
    view.Text = "Hello";
    Assert.True (fired);
}
```

### Replace Application.Begin with View Initialization
```csharp
// Before (in UnitTests)
[Fact]
[AutoInitShutdown]
public void Focus_Test ()
{
    var view = new Button ();
    var top = new Toplevel ();
    top.Add (view);
    Application.Begin (top);
    view.SetFocus ();
    Assert.True (view.HasFocus);
    top.Dispose ();
}

// After (in UnitTests.Parallelizable) - use BeginInit/EndInit!
[Fact]
public void Focus_Test ()
{
    var superView = new View ();
    var view = new Button ();
    superView.Add (view);
    superView.BeginInit ();
    superView.EndInit ();
    view.SetFocus ();
    Assert.True (view.HasFocus);
}
```

## Running Tests

Tests in this project run in parallel automatically. To run them:

```bash
dotnet test Tests/UnitTestsParallelizable/UnitTests.Parallelizable.csproj
```

## See Also
- [Category A Migration Summary](../CATEGORY_A_MIGRATION_SUMMARY.md) - Detailed analysis and migration guidelines
- [.NET Unit Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)
