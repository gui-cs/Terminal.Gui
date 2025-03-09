namespace Terminal.Gui.ViewTests;

public class ViewCommandTests
{
  
    // See https://github.com/gui-cs/Terminal.Gui/issues/3913
    [Fact]
    public void Button_IsDefault_Raises_Accepted_Correctly ()
    {
        int aAcceptedCount = 0;
        bool aCancelAccepting = false;

        int bAcceptedCount = 0;
        bool bCancelAccepting = false;

        var w = new Window ()
        {
            BorderStyle = LineStyle.None,
            Width = 10,
            Height = 10
        };

        var btnA = new Button ()
        {
            Width = 3,
            IsDefault = true
        };
        btnA.Accepting += (s, e) =>
                          {
                              aAcceptedCount++;
                              e.Cancel = aCancelAccepting;
                          };

        var btnB = new Button ()
        {
            Width = 3,
            X = Pos.Right (btnA)
        };

        btnB.Accepting += (s, e) =>
                          {
                              bAcceptedCount++;
                              e.Cancel = bCancelAccepting;
                          };
        w.Add (btnA, btnB);

        w.LayoutSubViews ();

        Application.Top = w;
        Application.TopLevels.Push(w);
        Assert.Same (Application.Top, w);

        // Click button 2
        var btn2Frame = btnB.FrameToScreen ();

        Application.RaiseMouseEvent (
                         new MouseEventArgs ()
                         {
                             ScreenPosition = btn2Frame.Location,
                             Flags = MouseFlags.Button1Clicked
                         });

        // Button A should have been accepted because B didn't cancel and A IsDefault
        Assert.Equal (1, aAcceptedCount);
        Assert.Equal (1, bAcceptedCount);

        bCancelAccepting = true;
        Application.RaiseMouseEvent (
                                     new MouseEventArgs ()
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
        int wAcceptedCount = 0;
        bool wCancelAccepting = false;
        var w = new Window ()
        {
            Title = "Window",
            BorderStyle = LineStyle.None,
            Width = 10,
            Height = 10
        };

        w.Accepting += (s, e) =>
                       {
                           wAcceptedCount++;
                           e.Cancel = wCancelAccepting;
                       };

        int btnAcceptedCount = 0;
        bool btnCancelAccepting = false;
        var btn = new Button ()
        {
            Title = "Button",
            Width = 3,
            IsDefault = true,
        };
        btn.CanFocus = true;

        btn.Accepting += (s, e) =>
                         {
                             btnAcceptedCount++;
                             e.Cancel = btnCancelAccepting;
                         };

        w.Add (btn);


        Application.Top = w;
        Application.TopLevels.Push (w);
        Assert.Same (Application.Top, w);

        w.LayoutSubViews ();

        // Click button just like a driver would
        var btnFrame = btn.FrameToScreen ();
        Application.RaiseMouseEvent (
                                     new MouseEventArgs ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Pressed
                                     });

        Application.RaiseMouseEvent (
                                     new MouseEventArgs ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Released
                                     });

        Application.RaiseMouseEvent (
                                     new MouseEventArgs ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        Assert.Equal (1, btnAcceptedCount);
        Assert.Equal (2, wAcceptedCount);

        Application.ResetState (true);
    }
}
