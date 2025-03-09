namespace Terminal.Gui.InputTests;

public class MouseEventArgsTests
{
    [Fact]
    public void Constructor_Default_ShouldSetFlagsToNone ()
    {
        var eventArgs = new MouseEventArgs ();
        Assert.Equal (MouseFlags.None, eventArgs.Flags);
    }

    [Fact]
    public void HandledProperty_ShouldBeFalseByDefault ()
    {
        var eventArgs = new MouseEventArgs ();
        Assert.False (eventArgs.Handled);
    }
}
