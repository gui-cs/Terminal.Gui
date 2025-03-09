namespace Terminal.Gui.ViewsTests;

public class TabTests
{
    [Fact]
    public void Constructor_Defaults ()
    {
        var tab = new Tab ();
        Assert.Equal ("Unnamed", tab.DisplayText);
        Assert.Null (tab.View);
        Assert.Equal (LineStyle.Rounded, tab.BorderStyle);
        Assert.True (tab.CanFocus);
    }
}
