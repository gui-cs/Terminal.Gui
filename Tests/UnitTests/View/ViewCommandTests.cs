namespace UnitTests.ViewTests;

public class ViewCommandTests
{
    [Fact]
    public void Command_Accept_Handled_Stops_Propagation_To_IsDefaultAcceptView_Button_Peer ()
    {
        var acceptOk = 0;
        var acceptCancel = 0;
        Button btnOk = new () { Id = "btnOk", Text = "Ok", IsDefaultAcceptView = true };
        btnOk.Accepting += (s, e) => acceptOk++;
        Button btnCancel = new () { Id = "btnCancel", Text = "Cancel", IsDefaultAcceptView = false };

        btnCancel.Accepting += (s, e) =>
                               {
                                   acceptCancel++;
                                   e.Handled = true;
                               };

        View superView = new View () { Id = "superView" };
        superView.Add (btnOk, btnCancel);

        btnCancel.InvokeCommand (Command.Accept);
        Assert.Equal (0, acceptOk);
        Assert.Equal (1, acceptCancel);
    }

    [Fact]
    public void Command_Accept_Not_Handled_Propagates_To_IsDefaultAcceptView_Button_Peer ()
    {
        var acceptOk = 0;
        var acceptCancel = 0;
        Button btnOk = new () { Id = "btnOk", Text = "Ok", IsDefaultAcceptView = true };
        btnOk.Accepting += (s, e) => acceptOk++;
        Button btnCancel = new () { Id = "btnCancel", Text = "Cancel", IsDefaultAcceptView = false };

        btnCancel.Accepting += (s, e) =>
                               {
                                   acceptCancel++;
                               };
        View superView = new View () { Id = "superView" };
        superView.Add (btnOk, btnCancel);

        btnCancel.InvokeCommand (Command.Accept);
        Assert.Equal (1, acceptOk);
        Assert.Equal (1, acceptCancel);
    }

    [Fact]
    public void Command_Accept_Handled_Stops_Propagation_To_Not_IsDefaultAcceptView_Button_Peer ()
    {
        var acceptOk = 0;
        var acceptCancel = 0;
        Button btnOk = new () { Id = "btnOk", Text = "Ok", IsDefaultAcceptView = false };
        btnOk.Accepting += (s, e) => acceptOk++;
        Button btnCancel = new () { Id = "btnCancel", Text = "Cancel", IsDefaultAcceptView = false };

        btnCancel.Accepting += (s, e) =>
                               {
                                   acceptCancel++;
                                   e.Handled = true;
                               };

        View superView = new View () { Id = "superView" };
        superView.Add (btnOk, btnCancel);

        btnCancel.InvokeCommand (Command.Accept);
        Assert.Equal (0, acceptOk);
        Assert.Equal (1, acceptCancel);
    }

    [Fact]
    public void Command_Accept_Not_Handled_Propagates_To_Not_IsDefaultAcceptView_Button_Peer ()
    {
        var acceptOk = 0;
        var acceptCancel = 0;
        Button btnOk = new () { Id = "btnOk", Text = "Ok", IsDefaultAcceptView = false };
        btnOk.Accepting += (s, e) => acceptOk++;
        Button btnCancel = new () { Id = "btnCancel", Text = "Cancel", IsDefaultAcceptView = false };

        btnCancel.Accepting += (s, e) =>
                               {
                                   acceptCancel++;
                               };
        View superView = new View () { Id = "superView" };
        superView.Add (btnOk, btnCancel);

        btnCancel.InvokeCommand (Command.Accept);
        Assert.Equal (0, acceptOk);
        Assert.Equal (1, acceptCancel);
    }

    [Fact]
    [AutoInitShutdown]
    public void HotKey_From_Non_IsDefaultAcceptView_Button_Raises_Accept_In_The_Default_Button ()
    {
        var acceptOk = 0;
        var acceptCancel = 0;
        Button btnOk = new () { Id = "Ok", Text = "_Ok", IsDefaultAcceptView = true };
        btnOk.Accepting += (s, e) => acceptOk++;
        Button btnCancel = new () { Id = "Cancel", Y = 1, Text = "_Cancel" };
        btnCancel.Accepting += (s, e) => acceptCancel++;
        Application.Top = new ();
        Application.Top.Add (btnOk, btnCancel);
        var rs = Application.Begin (Application.Top);

        Application.RaiseKeyDownEvent(Key.C);
        Assert.Equal (1, acceptOk);
        Assert.Equal (1, acceptCancel);

        Application.End (rs);
        Application.Top.Dispose ();
        Application.ResetState ();
    }

    // See https://github.com/gui-cs/Terminal.Gui/issues/3913
    [Fact]
    public void Button_IsDefaultAcceptView_Raises_Accepted_Correctly ()
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
            IsDefaultAcceptView = true
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
                                     new ()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        // Button A should have been accepted because B didn't cancel and A IsDefaultAcceptView
        Assert.Equal (1, aAcceptedCount);
        Assert.Equal (1, bAcceptedCount);

        bCancelAccepting = true;

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = btn2Frame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        // Button A (IsDefaultAcceptView) should NOT have been accepted because B canceled
        Assert.Equal (1, aAcceptedCount);
        Assert.Equal (2, bAcceptedCount);

        Application.ResetState (true);
    }

    // See: https://github.com/gui-cs/Terminal.Gui/issues/3905
    [Fact]// (Skip = "Failing as part of ##4270. Disabling temporarily.")]
    [SetupFakeApplication]
    public void Button_CanFocus_False_Raises_Accepted_Correctly ()
    {
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
            IsDefaultAcceptView = true
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
                                     new ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Pressed
                                     });

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Released
                                     });

        Application.RaiseMouseEvent (
                                     new ()
                                     {
                                         ScreenPosition = btnFrame.Location,
                                         Flags = MouseFlags.Button1Clicked
                                     });

        Assert.Equal (1, btnAcceptedCount);
        Assert.Equal (0, wAcceptedCount);

        Application.ResetState (true);
    }
}
