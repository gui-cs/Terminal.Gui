using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class NavigationTests (ITestOutputHelper _output) : TestsAllViews
{
    [Theory]
    [MemberData (nameof (AllViewTypes))]
    [SetupFakeDriver] // SetupFakeDriver resets app state; helps to avoid test pollution
    public void AllViews_AtLeastOneNavKey_Advances (Type viewType)
    {
        View view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            _output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }


        Toplevel top = new ();
        Application.Top = top;
        Application.Navigation = new ApplicationNavigation ();

        View otherView = new ()
        {
            Id = "otherView",
            CanFocus = true,
            TabStop = view.TabStop == TabBehavior.NoStop ? TabBehavior.TabStop : view.TabStop
        };

        top.Add (view, otherView);

        // Start with the focus on our test view
        view.SetFocus ();

        Key [] navKeys = [Key.Tab, Key.Tab.WithShift, Key.CursorUp, Key.CursorDown, Key.CursorLeft, Key.CursorRight];

        if (view.TabStop == TabBehavior.TabGroup)
        {
            navKeys = new [] { Key.F6, Key.F6.WithShift };
        }

        var left = false;

        foreach (Key key in navKeys)
        {
            switch (view.TabStop)
            {
                case TabBehavior.TabStop:
                case TabBehavior.NoStop:
                case TabBehavior.TabGroup:
                    Application.RaiseKeyDownEvent (key);

                    if (view.HasFocus)
                    {
                        // Try once more (HexView)
                        Application.RaiseKeyDownEvent (key);
                    }
                    break;
                default:
                    Application.RaiseKeyDownEvent (Key.Tab);

                    break;
            }

            if (!view.HasFocus)
            {
                left = true;
                _output.WriteLine ($"{view.GetType ().Name} - {key} Left.");

                break;
            }

            _output.WriteLine ($"{view.GetType ().Name} - {key} did not Leave.");
        }

        top.Dispose ();
        Application.ResetState ();

        Assert.True (left);
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    [SetupFakeDriver] // SetupFakeDriver resets app state; helps to avoid test pollution
    public void AllViews_HasFocus_Changed_Event (Type viewType)
    {
        View view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            _output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }

        if (view is Toplevel && ((Toplevel)view).Modal)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Modal Toplevel");

            return;
        }

        Toplevel top = new ();
        Application.Top = top;
        Application.Navigation = new ApplicationNavigation ();

        View otherView = new ()
        {
            Id = "otherView",
            CanFocus = true,
            TabStop = view.TabStop == TabBehavior.NoStop ? TabBehavior.TabStop : view.TabStop
        };

        var hasFocusTrue = 0;
        var hasFocusFalse = 0;

        view.HasFocusChanged += (s, e) =>
        {
            if (e.NewValue)
            {
                hasFocusTrue++;
            }
            else
            {
                hasFocusFalse++;
            }
        };

        top.Add (view, otherView);
        Assert.False (view.HasFocus);
        Assert.False (otherView.HasFocus);

        // Ensure the view is Visible
        view.Visible = true;

        Application.Top.SetFocus ();
        Assert.True (Application.Top!.HasFocus);
        Assert.True (top.HasFocus);

        // Start with the focus on our test view
        Assert.True (view.HasFocus);

        Assert.Equal (1, hasFocusTrue);
        Assert.Equal (0, hasFocusFalse);

        // Use keyboard to navigate to next view (otherView).
        var tries = 0;

        while (view.HasFocus)
        {
            if (++tries > 10)
            {
                Assert.Fail ($"{view} is not leaving.");
            }

            switch (view.TabStop)
            {
                case null:
                case TabBehavior.NoStop:
                case TabBehavior.TabStop:
                    if (Application.RaiseKeyDownEvent (Key.Tab))
                    {
                        if (view.HasFocus)
                        {
                            // Try another nav key (e.g. for TextView that eats Tab)
                            Application.RaiseKeyDownEvent (Key.CursorDown);
                        }
                    };
                    break;

                case TabBehavior.TabGroup:
                    Application.RaiseKeyDownEvent (Key.F6);

                    break;
                default:
                    throw new ArgumentOutOfRangeException ();
            }
        }

        Assert.Equal (1, hasFocusTrue);
        Assert.Equal (1, hasFocusFalse);

        Assert.False (view.HasFocus);
        Assert.True (otherView.HasFocus);

        // Now navigate back to our test view
        switch (view.TabStop)
        {
            case TabBehavior.NoStop:
                view.SetFocus ();

                break;
            case TabBehavior.TabStop:
                Application.RaiseKeyDownEvent (Key.Tab);

                break;
            case TabBehavior.TabGroup:
                if (!Application.RaiseKeyDownEvent (Key.F6))
                {
                    view.SetFocus ();
                }

                break;
            case null:
                Application.RaiseKeyDownEvent (Key.Tab);

                break;
            default:
                throw new ArgumentOutOfRangeException ();
        }

        Assert.Equal (2, hasFocusTrue);
        Assert.Equal (1, hasFocusFalse);

        Assert.True (view.HasFocus);
        Assert.False (otherView.HasFocus);

        // Cache state because Shutdown has side effects.
        // Also ensures other tests can continue running if there's a fail
        bool otherViewHasFocus = otherView.HasFocus;
        bool viewHasFocus = view.HasFocus;

        int enterCount = hasFocusTrue;
        int leaveCount = hasFocusFalse;

        top.Dispose ();

        Assert.False (otherViewHasFocus);
        Assert.True (viewHasFocus);

        Assert.Equal (2, enterCount);
        Assert.Equal (1, leaveCount);

        Application.ResetState ();
    }

    [Theory]
    [MemberData (nameof (AllViewTypes))]
    [SetupFakeDriver] // SetupFakeDriver resets app state; helps to avoid test pollution
    public void AllViews_Visible_False_No_HasFocus_Events (Type viewType)
    {
        View view = CreateInstanceIfNotGeneric (viewType);

        if (view == null)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Generic");

            return;
        }

        if (!view.CanFocus)
        {
            _output.WriteLine ($"Ignoring {viewType} - It can't focus.");

            return;
        }

        if (view is Toplevel && ((Toplevel)view).Modal)
        {
            _output.WriteLine ($"Ignoring {viewType} - It's a Modal Toplevel");

            return;
        }

        Toplevel top = new ();

        Application.Top = top;
        Application.Navigation = new ApplicationNavigation ();

        View otherView = new ()
        {
            CanFocus = true
        };

        view.Visible = false;

        var hasFocusChangingCount = 0;
        var hasFocusChangedCount = 0;

        view.HasFocusChanging += (s, e) => hasFocusChangingCount++;
        view.HasFocusChanged += (s, e) => hasFocusChangedCount++;

        top.Add (view, otherView);

        // Start with the focus on our test view
        view.SetFocus ();

        Assert.Equal (0, hasFocusChangingCount);
        Assert.Equal (0, hasFocusChangedCount);

        Application.RaiseKeyDownEvent (Key.Tab);

        Assert.Equal (0, hasFocusChangingCount);
        Assert.Equal (0, hasFocusChangedCount);

        Application.RaiseKeyDownEvent (Key.F6);

        Assert.Equal (0, hasFocusChangingCount);
        Assert.Equal (0, hasFocusChangedCount);

        top.Dispose ();

        Application.ResetState ();

    }

    // View.Focused & View.MostFocused tests

    // View.Focused - No subviews
    [Fact]
    public void Focused_NoSubviews ()
    {
        var view = new View ();
        Assert.Null (view.Focused);

        view.CanFocus = true;
        view.SetFocus ();
    }

    [Fact]
    public void GetMostFocused_NoSubviews_Returns_Null ()
    {
        var view = new View ();
        Assert.Null (view.Focused);

        view.CanFocus = true;
        Assert.False (view.HasFocus);
        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.Null (view.MostFocused);
    }

    [Fact]
    public void GetMostFocused_Returns_Most ()
    {
        var view = new View ()
        {
            Id = "view",
            CanFocus = true
        };

        var subview = new View ()
        {
            Id = "subview",
            CanFocus = true
        };

        view.Add (subview);

        view.SetFocus ();
        Assert.True (view.HasFocus);
        Assert.True (subview.HasFocus);
        Assert.Equal (subview, view.MostFocused);

        var subview2 = new View ()
        {
            Id = "subview2",
            CanFocus = true
        };

        view.Add (subview2);
        Assert.Equal (subview2, view.MostFocused);
    }

    [Fact]
    [SetupFakeDriver]
    public void Navigation_With_Null_Focused_View ()
    {
        // Non-regression test for #882 (NullReferenceException during keyboard navigation when Focused is null)

        Application.Init (new FakeDriver ());

        var top = new Toplevel ();
        top.Ready += (s, e) => { Assert.Null (top.Focused); };

        // Keyboard navigation with tab
        FakeConsole.MockKeyPresses.Push (new ('\t', ConsoleKey.Tab, false, false, false));

        Application.Iteration += (s, a) => Application.RequestStop ();

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();
    }


    [Fact]
    [AutoInitShutdown]
    public void Application_Begin_FocusesDeepest ()
    {
        var win1 = new Window { Id = "win1", Width = 10, Height = 1 };
        var view1 = new View { Id = "view1", Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
        var win2 = new Window { Id = "win2", Y = 6, Width = 10, Height = 1 };
        var view2 = new View { Id = "view2", Width = Dim.Fill (), Height = Dim.Fill (), CanFocus = true };
        win2.Add (view2);
        win1.Add (view1, win2);

        Application.Begin (win1);

        Assert.True (win1.HasFocus);
        Assert.True (view1.HasFocus);
        Assert.False (win2.HasFocus);
        Assert.False (view2.HasFocus);
        win1.Dispose ();
    }
}
