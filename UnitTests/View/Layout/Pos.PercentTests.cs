using Microsoft.VisualStudio.TestPlatform.Utilities;
using Xunit.Abstractions;
using static Terminal.Gui.Dim;
using static Terminal.Gui.Pos;

namespace Terminal.Gui.LayoutTests;

public class PosPercentTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void PosPercent_PlusOne (bool testHorizontal)
    {
        var container = new View { Width = 100, Height = 100 };

        var view = new View
        {
            X = testHorizontal ? Pos.Percent (50) + Pos.Percent (10) + 1 : 1,
            Y = testHorizontal ? 1 : Pos.Percent (50) + Pos.Percent (10) + 1,
            Width = 10,
            Height = 10
        };

        container.Add (view);
        var top = new Toplevel ();
        top.Add (container);
        top.LayoutSubviews ();

        Assert.Equal (100, container.Frame.Width);
        Assert.Equal (100, container.Frame.Height);

        if (testHorizontal)
        {
            Assert.Equal (61, view.Frame.X);
            Assert.Equal (1, view.Frame.Y);
        }
        else
        {
            Assert.Equal (1, view.Frame.X);
            Assert.Equal (61, view.Frame.Y);
        }
    }

    [Fact]
    public void PosPercent_SetsValue ()
    {
        int f = 0;
        Pos pos = Pos.Percent (f);
        Assert.Equal ($"Percent({f})", pos.ToString ());
        f = 50;
        pos = Pos.Percent (f);
        Assert.Equal ($"Percent({f})", pos.ToString ());
        f = 100;
        pos = Pos.Percent (f);
        Assert.Equal ($"Percent({f})", pos.ToString ());
    }

    [Fact]
    public void PosPercent_ThrowsOnIvalid ()
    {
        Pos pos = Pos.Percent (0);
        Assert.Throws<ArgumentException> (() => pos = Pos.Percent (-1));
        //Assert.Throws<ArgumentException> (() => pos = Pos.Percent (101));
        //Assert.Throws<ArgumentException> (() => pos = Pos.Percent (1000001));
    }

}
