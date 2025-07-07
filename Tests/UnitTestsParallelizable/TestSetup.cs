namespace UnitTests.Parallelizable;

/// <summary>
///     Ensures that tests can run in parallel without interference
///     by setting various Terminal.Gui static properties to their default values. E.g. View.EnableDebugIDisposableAsserts.
///     Annotate all test classes with [Collection("Global Test Setup")] or have it inherit from this class.
/// </summary>
public class GlobalTestSetup : IDisposable
{
    public GlobalTestSetup ()
    {
#if DEBUG_IDISPOSABLE
        // Ensure EnableDebugIDisposableAsserts is false before tests run
        View.EnableDebugIDisposableAsserts = false;
#endif
        CheckDefaultState ();
    }

    public void Dispose ()
    {
        // Optionally reset EnableDebugIDisposableAsserts after tests. Don't do this.
        // View.EnableDebugIDisposableAsserts = true;

        // Reset application state just in case a test changed something.
        // TODO: Add an Assert to ensure none of the state of Application changed.
        // TODO: Add an Assert to ensure none of the state of ConfigurationManager changed.
        CheckDefaultState ();
        Application.ResetState (true);
    }

    // IMPORTANT: Ensure this matches the code in Init_ResetState_Resets_Properties
    // here: .\Tests\UnitTests\Application\ApplicationTests.cs
    private void CheckDefaultState ()
    {
#if DEBUG_IDISPOSABLE
        Assert.False (View.EnableDebugIDisposableAsserts, "View.EnableDebugIDisposableAsserts should be false for Parallelizable tests.");
#endif

        // Check that all Application fields and properties are set to their default values

        // Public Properties
        Assert.Null (Application.Top);
        Assert.Null (Application.MouseGrabHandler.MouseGrabView);

        // Don't check Application.ForceDriver
        // Assert.Empty (Application.ForceDriver);
        // Don't check Application.Force16Colors
        //Assert.False (Application.Force16Colors);
        Assert.Null (Application.Driver);
        Assert.Null (Application.MainLoop);
        Assert.False (Application.EndAfterFirstIteration);
        Assert.Equal (Key.Tab.WithShift, Application.PrevTabKey);
        Assert.Equal (Key.Tab, Application.NextTabKey);
        Assert.Equal (Key.F6.WithShift, Application.PrevTabGroupKey);
        Assert.Equal (Key.F6, Application.NextTabGroupKey);
        Assert.Equal (Key.Esc, Application.QuitKey);

        // Internal properties
        Assert.False (Application.Initialized);
        Assert.Equal (Application.GetSupportedCultures (), Application.SupportedCultures);
        Assert.Equal (Application.GetAvailableCulturesFromEmbeddedResources (), Application.SupportedCultures);
        Assert.False (Application._forceFakeConsole);
        Assert.Equal (-1, Application.MainThreadId);
        Assert.Empty (Application.TopLevels);
        Assert.Empty (Application.CachedViewsUnderMouse);

        // Mouse
        // Do not reset _lastMousePosition
        //Assert.Null (Application._lastMousePosition);

        // Navigation
        Assert.Null (Application.Navigation);

        // Popover
        Assert.Null (Application.Popover);

        // Events - Can't check
        //Assert.Null (Application.NotifyNewRunState);
        //Assert.Null (Application.NotifyNewRunState);
        //Assert.Null (Application.Iteration);
        //Assert.Null (Application.SizeChanging);
        //Assert.Null (Application.GrabbedMouse);
        //Assert.Null (Application.UnGrabbingMouse);
        //Assert.Null (Application.GrabbedMouse);
        //Assert.Null (Application.UnGrabbedMouse);
        //Assert.Null (Application.MouseEvent);
        //Assert.Null (Application.KeyDown);
        //Assert.Null (Application.KeyUp);
    }

}

// Define a collection for the global setup
[CollectionDefinition ("Global Test Setup")]
public class GlobalTestSetupCollection : ICollectionFixture<GlobalTestSetup>
{
    // This class has no code and is never instantiated.
    // Its purpose is to apply the [CollectionDefinition] attribute
    // and associate the GlobalTestSetup with the test collection.
}
