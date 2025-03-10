namespace Terminal.Gui.ApplicationTests;

public class ApplicationPopoverHostTests
{
    [Fact]
    public void PopoverHost_Defaults ()
    {
        var host = new PopoverHost ();
        Assert.True (host.CanFocus);
        Assert.False (host.Visible);
        Assert.Equal (ViewportSettings.Transparent | ViewportSettings.TransparentMouse, host.ViewportSettings);
        Assert.True (host.Width!.Has<DimFill> (out _));
        Assert.True (host.Height!.Has<DimFill> (out _));
    }
}
