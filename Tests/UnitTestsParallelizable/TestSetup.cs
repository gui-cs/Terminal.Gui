namespace UnitTests.Parallelizable;

/// <summary>
///     Ensures that tests can run in parallel without interference
///     by setting various Terminal.Gui static properties to their default values. E.g. View.DebugIDisposable.
/// </summary>
public class GlobalTestSetup : IDisposable
{
    public GlobalTestSetup ()
    {
#if DEBUG_IDISPOSABLE
        // Ensure DebugIDisposable is false before tests run
        View.DebugIDisposable = false;
#endif
    }

    public void Dispose ()
    {
        // Optionally reset DebugIDisposable after tests. Don't do this.
        // View.DebugIDisposable = true;
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
