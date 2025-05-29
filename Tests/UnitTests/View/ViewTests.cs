using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

public class ViewTests
{
    private readonly ITestOutputHelper _output;

    public ViewTests (ITestOutputHelper output)
    {
        _output = output;
    }

    // Generic lifetime (IDisposable) tests
    [Fact]
    [TestRespondersDisposed]
    public void Dispose_Works ()
    {
        var r = new View ();
#if DEBUG_IDISPOSABLE
        Assert.Equal (4, View.Instances.Count);
#endif

        r.Dispose ();
#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif
    }

    [Fact]
    public void Disposing_Event_Notify_All_Subscribers_On_The_First_Container ()
    {
#if DEBUG_IDISPOSABLE

        // Only clear before because need to test after assert
        View.Instances.Clear ();
#endif

        var container1 = new View { Id = "Container1" };
        var count = 0;

        var view = new View { Id = "View" };
        view.Disposing += ViewDisposing;
        container1.Add (view);
        Assert.Equal (container1, view.SuperView);

        Assert.Single (container1.SubViews);

        var container2 = new View { Id = "Container2" };

        // BUGBUG: It's not legit to add a View to two SuperViews
        container2.Add (view);
        Assert.Equal (container2, view.SuperView);
        Assert.Equal (container1.SubViews.Count, container2.SubViews.Count);
        container2.Dispose ();

        Assert.Empty (container1.SubViews);
        Assert.Empty (container2.SubViews);
        Assert.Equal (1, count);
        Assert.Null (view.SuperView);

        container1.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif

        return;

        void ViewDisposing (object sender, EventArgs e)
        {
            count++;
            Assert.Equal (view, sender);
            container1.Remove ((View)sender);
        }
    }

    [Fact]
    public void Disposing_Event_Notify_All_Subscribers_On_The_Second_Container ()
    {
#if DEBUG_IDISPOSABLE

        // Only clear before because need to test after assert
        View.Instances.Clear ();
#endif

        var container1 = new View { Id = "Container1" };

        var view = new View { Id = "View" };
        container1.Add (view);
        Assert.Equal (container1, view.SuperView);
        Assert.Single (container1.SubViews);

        var container2 = new View { Id = "Container2" };
        var count = 0;

        view.Disposing += View_Disposing;
        // BUGBUG: It's not legit to add a View to two SuperViews
        container2.Add (view);
        Assert.Equal (container2, view.SuperView);

        void View_Disposing (object sender, EventArgs e)
        {
            count++;
            Assert.Equal (view, sender);
            container2.Remove ((View)sender);
        }

        Assert.Equal (container1.SubViews.Count, container2.SubViews.Count);
        container1.Dispose ();

        Assert.Empty (container1.SubViews);
        Assert.Empty (container2.SubViews);
        Assert.Equal (1, count);
        Assert.Null (view.SuperView);

        container2.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif
    }

    [Fact]
    public void Not_Notifying_Dispose ()
    {
        // Only clear before because need to test after assert
#if DEBUG_IDISPOSABLE
        View.Instances.Clear ();
#endif
        var container1 = new View { Id = "Container1" };

        var view = new View { Id = "View" };
        container1.Add (view);
        Assert.Equal (container1, view.SuperView);

        Assert.Single (container1.SubViews);

        var container2 = new View { Id = "Container2" };

        // BUGBUG: It's not legit to add a View to two SuperViews
        container2.Add (view);
        Assert.Equal (container2, view.SuperView);
        Assert.Equal (container1.SubViews.Count, container2.SubViews.Count);
        container1.Dispose ();

        Assert.Empty (container1.SubViews);
        Assert.NotEmpty (container2.SubViews);
        Assert.Single (container2.SubViews);
        Assert.Null (view.SuperView);

        // Trying access disposed properties
#if DEBUG_IDISPOSABLE
        Assert.True (container2.SubViews.ElementAt (0).WasDisposed);
#endif
        Assert.False (container2.SubViews.ElementAt (0).CanFocus);
        Assert.Null (container2.SubViews.ElementAt (0).Margin);
        Assert.Null (container2.SubViews.ElementAt (0).Border);
        Assert.Null (container2.SubViews.ElementAt (0).Padding);
        Assert.Null (view.SuperView);

        container2.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif
    }

    [Fact]
    [TestRespondersDisposed]
    public void Dispose_View ()
    {
        var view = new View ();
        Assert.NotNull (view.Margin);
        Assert.NotNull (view.Border);
        Assert.NotNull (view.Padding);

#if DEBUG_IDISPOSABLE
        Assert.Equal (4, View.Instances.Count);
#endif

        view.Dispose ();
        Assert.Null (view.Margin);
        Assert.Null (view.Border);
        Assert.Null (view.Padding);
    }

    [Fact]
    public void Internal_Tests ()
    {
        var rect = new Rectangle (1, 1, 10, 1);
        var view = new View { Frame = rect };
    }

    [Fact]
    [TestRespondersDisposed]
    public void New_Initializes ()
    {
        // Parameterless
        var r = new View ();
        Assert.NotNull (r);
        Assert.True (r.Enabled);
        Assert.True (r.Visible);

        Assert.Equal ($"View(){r.Viewport}", r.ToString ());
        Assert.False (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new (0, 0, 0, 0), r.Viewport);
        Assert.Equal (new (0, 0, 0, 0), r.Frame);
        Assert.Null (r.Focused);
        Assert.False (r.HasScheme);
        Assert.NotNull (r.GetScheme ());
        Assert.Equal (r.GetScheme (), SchemeManager.GetSchemesForCurrentTheme () ["Base"]);
        Assert.Equal (0, r.Width);
        Assert.Equal (0, r.Height);
        Assert.Equal (0, r.X);
        Assert.Equal (0, r.Y);
        Assert.False (r.IsCurrentTop);
        Assert.Empty (r.Id);
        Assert.Empty (r.SubViews);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
        r.Dispose ();

        // Empty Rect
        r = new () { Frame = Rectangle.Empty };
        Assert.NotNull (r);
        Assert.Equal ($"View(){r.Viewport}", r.ToString ());
        Assert.False (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new (0, 0, 0, 0), r.Viewport);
        Assert.Equal (new (0, 0, 0, 0), r.Frame);
        Assert.Null (r.Focused);
        Assert.False (r.HasScheme);
        Assert.NotNull (r.GetScheme ());
        Assert.Equal (r.GetScheme (), SchemeManager.GetSchemesForCurrentTheme () ["Base"]);
        Assert.Equal (0, r.Width);
        Assert.Equal (0, r.Height);
        Assert.Equal (0, r.X);
        Assert.Equal (0, r.Y);
        Assert.False (r.IsCurrentTop);
        Assert.Empty (r.Id);
        Assert.Empty (r.SubViews);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
        r.Dispose ();

        // Rect with values
        r = new () { Frame = new (1, 2, 3, 4) };
        Assert.NotNull (r);
        Assert.Equal ($"View(){r.Frame}", r.ToString ());
        Assert.False (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new (0, 0, 3, 4), r.Viewport);
        Assert.Equal (new (1, 2, 3, 4), r.Frame);
        Assert.Null (r.Focused);
        Assert.False (r.HasScheme);
        Assert.NotNull (r.GetScheme ());
        Assert.Equal (r.GetScheme (), SchemeManager.GetSchemesForCurrentTheme () ["Base"]);
        Assert.Equal (3, r.Width);
        Assert.Equal (4, r.Height);
        Assert.Equal (1, r.X);
        Assert.Equal (2, r.Y);
        Assert.False (r.IsCurrentTop);
        Assert.Empty (r.Id);
        Assert.Empty (r.SubViews);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
        r.Dispose ();

        // Initializes a view with a vertical direction
        r = new ()
        {
            Text = "Vertical View",
            TextDirection = TextDirection.TopBottom_LeftRight,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        r.TextFormatter.WordWrap = false;
        Assert.NotNull (r);

        r.BeginInit ();
        r.EndInit ();
        Assert.False (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new (0, 0, 1, 13), r.Viewport);
        Assert.Equal (new (0, 0, 1, 13), r.Frame);
        Assert.Null (r.Focused);
        Assert.False (r.HasScheme);
        Assert.NotNull (r.GetScheme ());
        Assert.Equal (r.GetScheme (), SchemeManager.GetSchemesForCurrentTheme () ["Base"]);
        Assert.False (r.IsCurrentTop);
        Assert.Equal (string.Empty, r.Id);
        Assert.Empty (r.SubViews);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.TopBottom_LeftRight, r.TextDirection);
        r.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void New_Methods_Return_False ()
    {
        var r = new View ();

        Assert.False (r.NewKeyDownEvent (Key.Empty));

        //Assert.False (r.OnKeyDown (new KeyEventArgs () { Key = Key.Unknown }));
        Assert.False (r.NewKeyUpEvent (Key.Empty));
        Assert.False (r.NewMouseEvent (new () { Flags = MouseFlags.AllEvents }));

        r.Dispose ();

        // TODO: Add more
    }

    [Fact]
    [AutoInitShutdown]
    public void Test_Nested_Views_With_Height_Equal_To_One ()
    {
        var v = new View { Width = 11, Height = 3 };

        var top = new View { Width = Dim.Fill (), Height = 1 };
        var bottom = new View { Width = Dim.Fill (), Height = 1, Y = 2 };

        top.Add (new Label { Text = "111" });
        v.Add (top);
        v.Add (new LineView (Orientation.Horizontal) { Y = 1 });
        bottom.Add (new Label { Text = "222" });
        v.Add (bottom);

        v.BeginInit ();
        v.EndInit ();
        v.LayoutSubViews ();
        v.Draw ();

        var looksLike =
            @"    
111
───────────
222";
        DriverAssert.AssertDriverContentsAre (looksLike, _output);
        v.Dispose ();
        top.Dispose ();
        bottom.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void View_With_No_Difference_Between_An_Object_Initializer_Compute_And_A_Absolute ()
    {
        // Object Initializer Computed
        var view = new View { X = 1, Y = 2, Width = 3, Height = 4 };

        // Object Initializer Absolute
        var super = new View { Frame = new (0, 0, 10, 10) };
        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubViews ();

        Assert.Equal (1, view.X);
        Assert.Equal (2, view.Y);
        Assert.Equal (3, view.Width);
        Assert.Equal (4, view.Height);
        Assert.False (view.Frame.IsEmpty);
        Assert.Equal (new (1, 2, 3, 4), view.Frame);
        Assert.False (view.Viewport.IsEmpty);
        Assert.Equal (new (0, 0, 3, 4), view.Viewport);

        view.LayoutSubViews ();

        Assert.Equal (1, view.X);
        Assert.Equal (2, view.Y);
        Assert.Equal (3, view.Width);
        Assert.Equal (4, view.Height);
        Assert.False (view.Frame.IsEmpty);
        Assert.False (view.Viewport.IsEmpty);
        super.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.Empty (View.Instances);
#endif

        // Default Constructor
        view = new ();
        Assert.Equal (0, view.X);
        Assert.Equal (0, view.Y);
        Assert.Equal (0, view.Width);
        Assert.Equal (0, view.Height);
        Assert.True (view.Frame.IsEmpty);
        Assert.True (view.Viewport.IsEmpty);
        view.Dispose ();

        // Object Initializer
        view = new () { X = 1, Y = 2, Text = "" };
        Assert.Equal (1, view.X);
        Assert.Equal (2, view.Y);
        Assert.Equal (0, view.Width);
        Assert.Equal (0, view.Height);
        Assert.False (view.Frame.IsEmpty);
        Assert.True (view.Viewport.IsEmpty);
        view.Dispose ();

        // Default Constructor and post assignment equivalent to Object Initializer
        view = new ();
        view.X = 1;
        view.Y = 2;
        view.Width = 3;
        view.Height = 4;
        super = new () { Frame = new (0, 0, 10, 10) };
        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubViews ();
        Assert.Equal (1, view.X);
        Assert.Equal (2, view.Y);
        Assert.Equal (3, view.Width);
        Assert.Equal (4, view.Height);
        Assert.False (view.Frame.IsEmpty);
        Assert.Equal (new (1, 2, 3, 4), view.Frame);
        Assert.False (view.Viewport.IsEmpty);
        Assert.Equal (new (0, 0, 3, 4), view.Viewport);
        super.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Visible_Clear_The_View_Output ()
    {
        var view = new View { Text = "Testing visibility." }; // use View, not Label to avoid AutoSize == true

        Assert.Equal (0, view.Frame.Width);
        Assert.Equal (0, view.Height);
        var win = new Window ();
        win.Add (view);
        Toplevel top = new ();
        top.Add (win);
        RunState rs = Application.Begin (top);

        view.Width = Dim.Auto ();
        view.Height = Dim.Auto ();
        Application.RunIteration (ref rs);
        Assert.Equal ("Testing visibility.".Length, view.Frame.Width);
        Assert.True (view.Visible);
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 5);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌────────────────────────────┐
│Testing visibility.         │
│                            │
│                            │
└────────────────────────────┘
",
                                                       _output
                                                      );

        view.Visible = false;

        var firstIteration = false;
        Application.RunIteration (ref rs, firstIteration);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
┌────────────────────────────┐
│                            │
│                            │
│                            │
└────────────────────────────┘
",
                                                       _output
                                                      );
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Visible_Sets_Also_Sets_SubViews ()
    {
        var button = new Button { Text = "Click Me" };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (button);
        Toplevel top = new ();
        top.Add (win);

        var iterations = 0;

        Application.Iteration += (s, a) =>
                                 {
                                     iterations++;

                                     Assert.True (button.Visible);
                                     Assert.True (button.CanFocus);
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.Visible);
                                     Assert.True (win.CanFocus);
                                     Assert.True (win.HasFocus);

                                     win.Visible = false;
                                     Assert.True (button.Visible);
                                     Assert.True (button.CanFocus);
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.Visible);
                                     Assert.True (win.CanFocus);
                                     Assert.False (win.HasFocus);

                                     button.SetFocus ();
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.HasFocus);

                                     win.SetFocus ();
                                     Assert.False (button.HasFocus);
                                     Assert.False (win.HasFocus);

                                     win.Visible = true;
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.HasFocus);

                                     Application.RequestStop ();
                                 };

        Application.Run (top);
        top.Dispose ();
        Assert.Equal (1, iterations);
    }
}
