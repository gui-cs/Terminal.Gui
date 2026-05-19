using AppTestHelpers;
using Terminal.Gui.Drivers;

namespace IntegrationTests;

/// <summary>
///     Demonstrates <see cref="AppTestHelper.AssertAnsiSnapshot" />: render a screen, capture it
///     as pure ANSI into a golden, compare byte-for-byte thereafter. The recorded
///     <c>__snapshots__/*.ans</c> can be <c>cat</c>'d in a truecolor terminal to see the exact
///     look without an interactive run.
/// </summary>
public class AnsiSnapshotTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Fact]
    public void AssertAnsiSnapshot_Records_Then_Compares ()
    {
        using AppTestHelper c = With.A<Window> (20, 4, DriverRegistry.Names.ANSI, _out)
                                    .Add (
                                          new Label
                                          {
                                              X = 1,
                                              Y = 1,
                                              Text = "Hello, snapshot!"
                                          })
                                    .WaitIteration ()
                                    .AssertAnsiSnapshot (nameof (AssertAnsiSnapshot_Records_Then_Compares))
                                    .Stop ();
    }
}
