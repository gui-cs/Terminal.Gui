namespace UnitTests.ViewBaseTests;

public class ViewCommandTests
{
    // See https://github.com/gui-cs/Terminal.Gui/issues/3913
    [Fact]
    [SetupFakeApplication]
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

        Application.Begin (w);
        Assert.Same (Application.TopRunnableView, w);

        // Click button 2
        Rectangle btn2Frame = btnB.FrameToScreen ();

        Application.Driver.GetInputProcessor ().EnqueueMouseEvent (
                                     null,
                                     new()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.LeftButtonPressed
                                     });

        Application.Driver.GetInputProcessor ().EnqueueMouseEvent (
                                     null,
                                     new()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.LeftButtonReleased
                                     });

        // Button A should have been accepted because B didn't cancel and A IsDefault
        // BUGBUG: This should be 1.
        // BUGBUG: We are invoking on release and clicked
        Assert.Equal (2, aAcceptedCount);
        // BUGBUG: This should be 1.
        // BUGBUG: We are invoking on release and clicked
        Assert.Equal (2, bAcceptedCount);

        bCancelAccepting = true;

        Application.Driver.GetInputProcessor ().EnqueueMouseEvent (
                                     null,
                                     new()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.LeftButtonPressed
                                     });

        Application.Driver.GetInputProcessor ().EnqueueMouseEvent (
                                     null,
                                     new()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.LeftButtonReleased
                                     });

        // Button A (IsDefault) should NOT have been accepted because B canceled
        // BUGBUG: This should be 1.
        // BUGBUG: We are invoking on release and clicked
        Assert.Equal (2, aAcceptedCount);
        // BUGBUG: This should be 2.
        // BUGBUG: We are invoking on release and clicked
        Assert.Equal (3, bAcceptedCount);

        Application.ResetState (true);
    }

    // See: https://github.com/gui-cs/Terminal.Gui/issues/3905
    [Fact]
    [SetupFakeApplication]
    public void Button_CanFocus_False_Raises_Accepted_Correctly ()
    {
        var wAcceptedCount = 0;

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
                           e.Handled = true;
                       };

        var btnAcceptedCount = 0;

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
                             e.Handled = true;
                         };

        w.Add (btn);

        Application.Begin (w);

        w.LayoutSubViews ();

        // Click button just like a driver would
        Rectangle btnFrame = btn.FrameToScreen ();

        Application.Driver.GetInputProcessor ().EnqueueMouseEvent (
                                     null,
                                     new()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.LeftButtonPressed
                                     });

        Application.Driver.GetInputProcessor ().EnqueueMouseEvent (
                                     null,
                                     new()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.LeftButtonReleased
                                     });

        Assert.Equal (0, wAcceptedCount);

        // BUGBUG: This should be 1.
        // BUGBUG: We are invoking on release and clicked
        Assert.Equal (2, btnAcceptedCount);

        // The above grabbed the mouse. Need to ungrab.
        Application.Mouse.UngrabMouse ();

        w.Dispose ();
        Application.ResetState (true);
    }
}
