using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ArrangementTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // Test that TopResizable and Movable are mutually exclusive and Movable wins
    [Fact]
    public void TopResizableAndMovableMutuallyExclusive ()
    {
      // TODO: Write test.
    }

}
