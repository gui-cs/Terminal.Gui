using UnitTests;

namespace Terminal.Gui.ViewTests;

public class EnabledTests
{
  
    [Fact]
    [AutoInitShutdown]
    public void _Enabled_Sets_Also_Sets_SubViews ()
    {
        var wasClicked = false;
        var button = new Button { Text = "Click Me" };
        button.IsDefault = true;
        button.Accepting += (s, e) => wasClicked = !wasClicked;
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (button);
        var top = new Toplevel ();
        top.Add (win);

        var iterations = 0;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     win.NewKeyDownEvent (Key.Enter);
                                     Assert.True (wasClicked);
                                     button.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked });
                                     Assert.False (wasClicked);
                                     Assert.True (button.Enabled);
                                     Assert.True (button.CanFocus);
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.Enabled);
                                     Assert.True (win.CanFocus);
                                     Assert.True (win.HasFocus);

                                     Assert.True (button.HasFocus);
                                     win.Enabled = false;
                                     Assert.False (button.HasFocus);
                                     button.NewKeyDownEvent (Key.Enter);
                                     Assert.False (wasClicked);
                                     button.NewMouseEvent (new () { Flags = MouseFlags.Button1Clicked });
                                     Assert.False (wasClicked);
                                     Assert.False (button.Enabled);
                                     Assert.True (button.CanFocus);
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.Enabled);
                                     Assert.True (win.CanFocus);
                                     Assert.False (win.HasFocus);
                                     button.SetFocus ();
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.HasFocus);
                                     win.SetFocus ();
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.HasFocus);

                                     win.Enabled = true;
                                     win.FocusDeepest (NavigationDirection.Forward, null);
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.HasFocus);

                                     Application.RequestStop ();
                                 };

        Application.Run (top);

        Assert.Equal (1, iterations);
        top.Dispose ();
    }
}
