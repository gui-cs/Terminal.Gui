using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace UnitTests_Parallelizable.ViewsTests;

public class MenuTests ()
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var menu = new Menu { };
        Assert.Empty (menu.Title);
        Assert.Empty (menu.Text);
    }

}
