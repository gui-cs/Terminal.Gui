using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.InputTests;

public class KeyTests
{
    [Fact]
    public void Set_Key_Separator_With_Rune_Default_Ensure_Using_The_Default_Plus ()
    {
        Key key = new (Key.A.WithCtrl);
        Assert.Equal ((Rune)'+', Key.Separator);
        Assert.Equal ("Ctrl+A", key.ToString ());

        // NOTE: This means this test can't be parallelized
        Key.Separator = new ('-');
        Assert.Equal ((Rune)'-', Key.Separator);
        Assert.Equal ("Ctrl-A", key.ToString ());

        Key.Separator = new ();
        Assert.Equal ((Rune)'+', Key.Separator);
        Assert.Equal ("Ctrl+A", key.ToString ());
    }
}
