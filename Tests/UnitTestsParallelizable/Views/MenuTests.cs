using Xunit.Abstractions;

//using static Terminal.Gui.ViewBaseTests.MenuTests;

namespace ViewsTests;

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
