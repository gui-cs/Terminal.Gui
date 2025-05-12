using System.Collections;
using TerminalGuiFluentTesting;

namespace IntegrationTests.FluentTests;

public class V2TestDrivers : IEnumerable<object []>
{
    public IEnumerator<object []> GetEnumerator ()
    {
        yield return new object [] { V2TestDriver.V2Win };
        yield return new object [] { V2TestDriver.V2Net };
    }

    IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
}

/// <summary>
/// Test cases for functions with signature <code>V2TestDriver d, bool someFlag</code>
/// that enumerates all variations
/// </summary>
public class V2TestDrivers_WithTrueFalseParameter : IEnumerable<object []>
{
    public IEnumerator<object []> GetEnumerator ()
    {
        yield return new object [] { V2TestDriver.V2Win,false };
        yield return new object [] { V2TestDriver.V2Net,false };
        yield return new object [] { V2TestDriver.V2Win,true };
        yield return new object [] { V2TestDriver.V2Net,true };
    }

    IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
}