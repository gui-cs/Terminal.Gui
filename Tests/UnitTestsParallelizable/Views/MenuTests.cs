using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace Terminal.Gui.ViewsTests;

public class MenuTests ()
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var menu = new Menuv2 { };
        Assert.Empty (menu.Title);
        Assert.Empty (menu.Text);
    }

}
