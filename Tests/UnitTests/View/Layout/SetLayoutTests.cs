using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.LayoutTests;

public class SetLayoutTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;


    [Fact]
    [AutoInitShutdown]
    public void Screen_Size_Change_Causes_Layout ()
    {
        Application.Current = new ();

        var view = new View
        {
            X = 3,
            Y = 2,
            Width = 10,
            Height = 1,
            Text = "0123456789"
        };
        Application.Current.Add (view);

        var rs = Application.Begin (Application.Current);
        Application.Driver!.SetScreenSize (80, 25);

        Assert.Equal (new (0, 0, 80, 25), new Rectangle (0, 0, Application.Screen.Width, Application.Screen.Height));
        Assert.Equal (new (0, 0, Application.Screen.Width, Application.Screen.Height), Application.Current.Frame);
        Assert.Equal (new (0, 0, 80, 25), Application.Current.Frame);

        Application.Driver!.SetScreenSize (20, 10);
        Assert.Equal (new (0, 0, Application.Screen.Width, Application.Screen.Height), Application.Current.Frame);

        Assert.Equal (new (0, 0, 20, 10), Application.Current.Frame);

        Application.End (rs);
        Application.Current.Dispose ();
    }
}
