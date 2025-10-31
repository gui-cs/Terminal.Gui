using System.Collections;
using TerminalGuiFluentTesting;

namespace IntegrationTests.FluentTests;

public class TestDrivers : IEnumerable<object []>
{
    public IEnumerator<object []> GetEnumerator ()
    {
        yield return [TestDriver.Windows];
        yield return [TestDriver.DotNet];
       // yield return [TestDriver.Unix];
        //yield return [TestDriver.Fake];
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
        yield return [TestDriver.Windows,false];
        yield return [TestDriver.DotNet,false];
        yield return [TestDriver.Windows,true];
        yield return [TestDriver.DotNet,true];
    }

    IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
}