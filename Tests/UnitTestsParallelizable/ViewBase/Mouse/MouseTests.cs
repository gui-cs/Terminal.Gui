using UnitTests;
using Xunit.Abstractions;

namespace ViewBaseTests.MouseTests;

[Trait ("Category", "Input")]
public class MouseTests (ITestOutputHelper output) : TestsAllViews
{
    [Fact]
    public void Default_MouseBindings ()
    {
        var testView = new View ();

        Assert.Contains (MouseFlags.Button1Clicked, testView.MouseBindings.GetAllFromCommands (Command.Accept));
        //        Assert.Contains (MouseFlags.Button1DoubleClicked, testView.MouseBindings.GetAllFromCommands (Command.Accept));

        Assert.Equal (6, testView.MouseBindings.GetBindings ().Count ());
    }

    [Theory(Skip = "Broken in #4474")]
    [InlineData (false, false, false)]
    [InlineData (true, false, true)]
    [InlineData (true, true, true)]
    public void MouseClick_SetsFocus_If_CanFocus (bool canFocus, bool setFocus, bool expectedHasFocus)
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View { CanFocus = true, Width = 1, Height = 1 };
        var testView = new View { CanFocus = canFocus, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, testView);

        focusedView.SetFocus ();

        Assert.True (superView.HasFocus);
        Assert.True (focusedView.HasFocus);
        Assert.False (testView.HasFocus);

        if (setFocus)
        {
            testView.SetFocus ();
        }

        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonPressed });
        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonReleased });
        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.LeftButtonClicked });
        Assert.True (superView.HasFocus);
        Assert.Equal (expectedHasFocus, testView.HasFocus);
    }

    [Theory]
    [InlineData (false, false, 1)]
    [InlineData (true, false, 1)]
    [InlineData (true, true, 1)]
    public void MouseClick_Raises_Accepting (bool canFocus, bool setFocus, int expectedAcceptingCount)
    {
        var superView = new View { CanFocus = true, Height = 1, Width = 15 };
        var focusedView = new View { CanFocus = true, Width = 1, Height = 1 };
        var testView = new View { CanFocus = canFocus, X = 4, Width = 4, Height = 1 };
        superView.Add (focusedView, testView);

        focusedView.SetFocus ();

        Assert.True (superView.HasFocus);
        Assert.True (focusedView.HasFocus);
        Assert.False (testView.HasFocus);

        if (setFocus)
        {
            testView.SetFocus ();
        }

        var acceptingCount = 0;
        testView.Accepting += (sender, args) => acceptingCount++;

        testView.NewMouseEvent (new () { Timestamp = DateTime.Now, Position = new Point (0, 0), Flags = MouseFlags.Button1Clicked });
        Assert.True (superView.HasFocus);
        Assert.Equal (expectedAcceptingCount, acceptingCount);
    }

    [Theory]
    [InlineData (MouseFlags.WheeledUp | MouseFlags.ButtonCtrl, MouseFlags.WheeledLeft)]
    [InlineData (MouseFlags.WheeledDown | MouseFlags.ButtonCtrl, MouseFlags.WheeledRight)]
    public void WheeledLeft_WheeledRight (MouseFlags mouseFlags, MouseFlags expectedMouseFlagsFromEvent)
    {
        var mouseFlagsFromEvent = MouseFlags.None;
        var view = new View ();
        view.MouseEvent += (s, e) => mouseFlagsFromEvent = e.Flags;

        view.NewMouseEvent (new () { Timestamp = DateTime.Now, Flags = mouseFlags });
        Assert.Equal (mouseFlagsFromEvent, expectedMouseFlagsFromEvent);
    }

    [Fact]
    public void NewMouseEvent_Invokes_MouseEvent_Properly ()
    {
        View view = new ()
        {
            Width = 1,
            Height = 1
        };
        var mouseEventInvoked = false;

        view.MouseEvent += (s, e) =>
                           {
                               mouseEventInvoked = true;
                               e.Handled = true;
                           };

        Mouse me = new () { Timestamp = DateTime.Now };
        view.NewMouseEvent (me);
        Assert.True (mouseEventInvoked);
        Assert.True (me.Handled);

        view.Dispose ();
    }

    [Fact]
    public void NewMouseEvent_DoubleClick_Pattern_MouseEvent_Raised_Correctly ()
    {
        View view = new ()
        {
            Visible = true,
            Enabled = true,
            Width = 1,
            Height = 1
        };
        int mouseEventCount = 0;

        view.MouseEvent += (s, e) =>
                           {
                               mouseEventCount++;
                              // e.Handled = true;
                           };

        Mouse mouseEventPressed1 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Pressed };
        view.NewMouseEvent (mouseEventPressed1);
        Mouse mouseEventReleased1 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Released };
        view.NewMouseEvent (mouseEventReleased1);
        Mouse mouseEventClicked = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Clicked };
        view.NewMouseEvent (mouseEventClicked);

        Mouse mouseEventPressed2 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Pressed };
        view.NewMouseEvent (mouseEventPressed2);
        Mouse mouseEventReleased2 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Released };
        view.NewMouseEvent (mouseEventReleased2);
        Mouse mouseEventDoubleClicked = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1DoubleClicked };
        view.NewMouseEvent (mouseEventDoubleClicked);

        Assert.Equal (6, mouseEventCount);

        view.Dispose ();
    }

    [Fact]
    public void NewMouseEvent_DoubleClick_Pattern_Raises_Accept_Once ()
    {
        View view = new ()
        {
            Visible = true,
            Enabled = true,
            Width = 1,
            Height = 1
        };
        int acceptingCount = 0;

        view.Accepting += (s, e) =>
                           {
                               acceptingCount++;
                               e.Handled = true;
                           };

        Mouse mouseEventPressed1 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Pressed };
        view.NewMouseEvent (mouseEventPressed1);
        Mouse mouseEventReleased1 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Released };
        view.NewMouseEvent (mouseEventReleased1);
        Mouse mouseEventClicked = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Clicked };
        view.NewMouseEvent (mouseEventClicked);

        Mouse mouseEventPressed2 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Pressed };
        view.NewMouseEvent (mouseEventPressed2);
        Mouse mouseEventReleased2 = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1Released };
        view.NewMouseEvent (mouseEventReleased2);
        Mouse mouseEventDoubleClicked = new () { Timestamp = DateTime.Now, Flags = MouseFlags.Button1DoubleClicked };
        view.NewMouseEvent (mouseEventDoubleClicked);

        Assert.Equal (1, acceptingCount);

        view.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_NewMouseEvent_Enabled_False_Does_Not_Set_Handled (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        view.Enabled = false;
        var me = new Terminal.Gui.Input.Mouse () { Timestamp = DateTime.Now };
        view.NewMouseEvent (me);
        Assert.False (me.Handled);
        view.Dispose ();
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    public void AllViews_NewMouseEvent_Clicked_Enabled_False_Does_Not_Set_Handled (Type viewType)
    {
        View? view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        view.Enabled = false;

        var me = new Terminal.Gui.Input.Mouse
        {
            Timestamp = DateTime.Now,
            Flags = MouseFlags.Button1Clicked
        };
        view.NewMouseEvent (me);
        Assert.False (me.Handled);
        view.Dispose ();
    }
}
