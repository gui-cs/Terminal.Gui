using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class MouseTests (ITestOutputHelper output)
{

    [Theory]
    [InlineData(false, false, false)]
    [InlineData (true, false, true)]
    [InlineData (true, true, true)]
    public void MouseClick_SetsFocus_If_CanFocus (bool canFocus, bool setFocus, bool expectedHasFocus)
    {
        var superView = new View () { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View () { CanFocus = true, Width = 1, Height = 1 };
        var testView = new View () { CanFocus = canFocus, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, testView);
        superView.BeginInit ();
        superView.EndInit (); 
        
        focusedView.SetFocus();

        Assert.True (superView.HasFocus);
        Assert.True (focusedView.HasFocus);
        Assert.False (testView.HasFocus);

        if (setFocus)
        {
            testView.SetFocus ();
        }

        testView.OnMouseEvent (new MouseEvent () { X = 0, Y = 0, Flags = MouseFlags.Button1Clicked });
        Assert.True (superView.HasFocus);
        Assert.Equal(expectedHasFocus, testView.HasFocus);
    }
}
