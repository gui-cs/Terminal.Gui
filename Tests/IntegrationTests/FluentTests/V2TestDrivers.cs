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
