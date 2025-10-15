using System.Collections;
using TerminalGuiFluentTesting;

namespace IntegrationTests.FluentTests;

public class TestDrivers : IEnumerable<object []>
{
    public IEnumerator<object []> GetEnumerator ()
    {
        yield return new object [] { TestDriver.Windows };
        yield return new object [] { TestDriver.DotNet };
    }

    IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
}

/// <summary>
/// Test cases for functions with signature <code>TestDriver d, bool someFlag</code>
/// that enumerates all variations
/// </summary>
public class TestDrivers_WithTrueFalseParameter : IEnumerable<object []>
{
    public IEnumerator<object []> GetEnumerator ()
    {
        yield return new object [] { TestDriver.Windows,false };
        yield return new object [] { TestDriver.DotNet,false };
        yield return new object [] { TestDriver.Windows,true };
        yield return new object [] { TestDriver.DotNet,true };
    }

    IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
}