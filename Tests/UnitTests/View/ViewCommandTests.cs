namespace Terminal.Gui.ViewTests;

public class ViewCommandTests
{
    // See https://github.com/gui-cs/Terminal.Gui/issues/3913
    [Fact]
    public void Button_IsDefault_Raises_Accepted_Correctly ()
    {
        var aAcceptedCount = 0;
        var aCancelAccepting = false;

        var bAcceptedCount = 0;
        var bCancelAccepting = false;

        var w = new Window
        {
            BorderStyle = LineStyle.None,
            Width = 10,
            Height = 10
        };

        var btnA = new Button
        {
            Width = 3,
            IsDefault = true
        };

        btnA.Accepting += (s, e) =>
                          {
                              aAcceptedCount++;
                              e.Handled = aCancelAccepting;
                          };

        var btnB = new Button
        {
            Width = 3,
            X = Pos.Right (btnA)
        };

        btnB.Accepting += (s, e) =>
                          {
                              bAcceptedCount++;
                              e.Handled = bCancelAccepting;
                          };
        w.Add (btnA, btnB);

        w.LayoutSubViews ();

        Application.Top = w;
        Application.TopLevels.Push (w);
        Assert.Same (Application.Top, w);

        // Click button 2
        Rectangle btn2Frame = btnB.FrameToScreen ();

        Application.RaiseMouseEvent (
                                     new()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        // Button A should have been accepted because B didn't cancel and A IsDefault
        Assert.Equal (1, aAcceptedCount);
        Assert.Equal (1, bAcceptedCount);

        bCancelAccepting = true;

        Application.RaiseMouseEvent (
                                     new()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        // Button A (IsDefault) should NOT have been accepted because B canceled
        Assert.Equal (1, aAcceptedCount);
        Assert.Equal (2, bAcceptedCount);

        Application.ResetState (true);
    }

    // See: https://github.com/gui-cs/Terminal.Gui/issues/3905
    [Fact]
    public void Button_CanFocus_False_Raises_Accepted_Correctly ()
    {
        Application.Init (new FakeDriver ());
        var wAcceptedCount = 0;
        var wCancelAccepting = false;

        var w = new Window
        {
            Title = "Window",
            BorderStyle = LineStyle.None,
            Width = 10,
            Height = 10
        };

        w.Accepting += (s, e) =>
                       {
                           wAcceptedCount++;
                           e.Handled = wCancelAccepting;
                       };

        var btnAcceptedCount = 0;
        var btnCancelAccepting = true;

        var btn = new Button
        {
            Title = "Button",
            Width = 3,
            IsDefault = true
        };
        btn.CanFocus = true;

        btn.Accepting += (s, e) =>
                         {
                             btnAcceptedCount++;
                             e.Handled = btnCancelAccepting;
                         };

        w.Add (btn);

        Application.Top = w;
        Application.TopLevels.Push (w);
        Assert.Same (Application.Top, w);

        w.LayoutSubViews ();

        // Click button just like a driver would
        Rectangle btnFrame = btn.FrameToScreen ();

        Application.RaiseMouseEvent (
                                     new()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Pressed
                                     });

        Application.RaiseMouseEvent (
                                     new()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Released
                                     });

        Application.RaiseMouseEvent (
                                     new()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        Assert.Equal (1, btnAcceptedCount);
        Assert.Equal (0, wAcceptedCount);

        Application.ResetState (true);
    }
}
