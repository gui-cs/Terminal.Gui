// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.InputTests;

public class ResponderTests
{
    [Fact]
    public void KeyPressed_Handled_True_Cancels_KeyPress ()
    {
        var r = new View ();
        var args = new Key { KeyCode = KeyCode.Null };

        Assert.False (r.NewKeyDownEvent (args));
        Assert.False (args.Handled);

        r.KeyDown += (s, a) => a.Handled = true;
        Assert.True (r.NewKeyDownEvent (args));
        Assert.True (args.Handled);

        r.Dispose ();
    }

    public class DerivedView : View
    {
        protected override bool OnKeyDown (Key keyEvent) { return true; }
    }
}
