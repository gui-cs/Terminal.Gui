using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.LayoutTests;

public class SetLayoutTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    [Fact]
    [AutoInitShutdown]
    public void Screen_Size_Change_Causes_Layout ()
    {
        Application.Top = new ();

        var view = new View
        {
            X = 3,
            Y = 2,
            Width = 10,
            Height = 1,
            Text = "0123456789"
        };
        Application.Top.Add (view);

        var rs = Application.Begin (Application.Top);

        Assert.Equal (new (0, 0, 80, 25), new Rectangle (0, 0, Application.Screen.Width, Application.Screen.Height));
        Assert.Equal (new (0, 0, Application.Screen.Width, Application.Screen.Height), Application.Top.Frame);
        Assert.Equal (new (0, 0, 80, 25), Application.Top.Frame);

        ((FakeDriver)Application.Driver!).SetBufferSize (20, 10);
        Assert.Equal (new (0, 0, Application.Screen.Width, Application.Screen.Height), Application.Top.Frame);

        Assert.Equal (new (0, 0, 20, 10), Application.Top.Frame);

        Application.End (rs);

    }
}
