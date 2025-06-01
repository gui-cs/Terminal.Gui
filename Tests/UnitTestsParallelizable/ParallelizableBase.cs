
namespace UnitTests.Parallelizable;

/// <summary>
///     Base class for parallelizable tests. Ensures that tests can run in parallel without interference
///     by setting various Terminal.Gui static properties to their default values. E.g. View.EnableDebugIDisposableAsserts.
/// </summary>
[Collection ("Global Test Setup")]
public abstract class ParallelizableBase
{
    // Common setup or utilities for all tests can go here
}
