﻿using System.Text;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console

namespace Terminal.Gui.ViewTests;

public class ViewTests
{
    public ViewTests (ITestOutputHelper output) { _output = output; }
    private readonly ITestOutputHelper _output;

    [Fact]
    [TestRespondersDisposed]
    public void Added_Removed ()
    {
        var v = new View { Frame = new Rect (0, 0, 10, 24) };
        var t = new View ();

        v.Added += (s, e) =>
                   {
                       Assert.Same (v.SuperView, e.Parent);
                       Assert.Same (t, e.Parent);
                       Assert.Same (v, e.Child);
                   };

        v.Removed += (s, e) =>
                     {
                         Assert.Same (t, e.Parent);
                         Assert.Same (v, e.Child);
                         Assert.True (v.SuperView == null);
                     };

        t.Add (v);
        Assert.True (t.Subviews.Count == 1);

        t.Remove (v);
        Assert.True (t.Subviews.Count == 0);

        t.Dispose ();
        v.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Clear_Bounds_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
    {
        var view = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

        view.DrawContent += (s, e) =>
                            {
                                Rect savedClip = Application.Driver.Clip;
                                Application.Driver.Clip = new Rect (1, 1, view.Bounds.Width, view.Bounds.Height);

                                for (var row = 0; row < view.Bounds.Height; row++)
                                {
                                    Application.Driver.Move (1, row + 1);

                                    for (var col = 0; col < view.Bounds.Width; col++)
                                    {
                                        Application.Driver.AddStr ($"{col}");
                                    }
                                }

                                Application.Driver.Clip = savedClip;
                                e.Cancel = true;
                            };
        Application.Top.Add (view);
        Application.Begin (Application.Top);
        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);

        var expected = @"
┌──────────────────┐
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
└──────────────────┘
";

        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 20, 10), pos);

        view.Clear (view.Frame);

        expected = @"
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (Rect.Empty, pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Clear_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
    {
        var view = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

        view.DrawContent += (s, e) =>
                            {
                                Rect savedClip = Application.Driver.Clip;
                                Application.Driver.Clip = new Rect (1, 1, view.Bounds.Width, view.Bounds.Height);

                                for (var row = 0; row < view.Bounds.Height; row++)
                                {
                                    Application.Driver.Move (1, row + 1);

                                    for (var col = 0; col < view.Bounds.Width; col++)
                                    {
                                        Application.Driver.AddStr ($"{col}");
                                    }
                                }

                                Application.Driver.Clip = savedClip;
                                e.Cancel = true;
                            };
        Application.Top.Add (view);
        Application.Begin (Application.Top);
        ((FakeDriver)Application.Driver).SetBufferSize (20, 10);

        var expected = @"
┌──────────────────┐
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
└──────────────────┘
";

        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 20, 10), pos);

        view.Clear (view.Frame);

        expected = @"
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (Rect.Empty, pos);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void Clear_Does_Not_Spillover_Its_Parent (bool label)
    {
        var root = new View { Width = 20, Height = 10, ColorScheme = Colors.ColorSchemes ["Base"] };

        View v = label
                     ? new Label { Text = new string ('c', 100) }
                     : new TextView { Height = 1, Text = new string ('c', 100), Width = Dim.Fill () };

        root.Add (v);

        Application.Top.Add (root);
        RunState runState = Application.Begin (Application.Top);

        if (label)
        {
            Assert.True (v.AutoSize);
            Assert.False (v.CanFocus);
            Assert.Equal (new Rect (0, 0, 100, 1), v.Frame);
        }
        else
        {
            Assert.False (v.AutoSize);
            Assert.True (v.CanFocus);
            Assert.Equal (new Rect (0, 0, 20, 1), v.Frame);
        }

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
cccccccccccccccccccc",
                                                      _output
                                                     );

        Attribute [] attributes =
        {
            Colors.ColorSchemes ["TopLevel"].Normal,
            Colors.ColorSchemes ["Base"].Normal,
            Colors.ColorSchemes ["Base"].Focus
        };

        if (label)
        {
            TestHelpers.AssertDriverAttributesAre (
                                                   @"
111111111111111111110
111111111111111111110",
                                                   Application.Driver,
                                                   attributes
                                                  );
        }
        else
        {
            TestHelpers.AssertDriverAttributesAre (
                                                   @"
222222222222222222220
111111111111111111110",
                                                   Application.Driver,
                                                   attributes
                                                  );
        }

        if (label)
        {
            root.CanFocus = true;
            v.CanFocus = true;
            Assert.False (v.HasFocus);
            v.SetFocus ();
            Assert.True (v.HasFocus);
            Application.Refresh ();

            TestHelpers.AssertDriverAttributesAre (
                                                   @"
222222222222222222220
111111111111111111110",
                                                   Application.Driver,
                                                   attributes
                                                  );
        }

        Application.End (runState);
    }

    [Fact]
    [AutoInitShutdown]
    public void Correct_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Frame ()
    {
        var label = new Label { Text = "At 0,0" };

        var view = new DerivedView
        {
            X = 2,
            Y = 2,
            Width = 30,
            Height = 2,
            Text = "A text with some long width\n and also with two lines."
        };
        Toplevel top = Application.Top;
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );

        view.Frame = new Rect (3, 3, 10, 1);
        Assert.Equal (new Rect (3, 3, 10, 1), view.Frame);
        Assert.Equal (LayoutStyle.Absolute, view.LayoutStyle);
        Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 1), view._needsDisplayRect);
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0       
             
             
   A text wit",
                                                      _output
                                                     );
        Application.End (runState);
    }

    [Fact]
    [AutoInitShutdown]
    public void Correct_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Pos_Dim ()
    {
        var label = new Label { Text = "At 0,0" };

        var view = new DerivedView
        {
            X = 2,
            Y = 2,
            Width = 30,
            Height = 2,
            Text = "A text with some long width\n and also with two lines."
        };
        Toplevel top = Application.Top;
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );

        view.X = 3;
        view.Y = 3;
        view.Width = 10;
        view.Height = 1;
        Assert.Equal (new Rect (3, 3, 10, 1), view.Frame);
        Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
        Assert.Equal (new Rect (0, 0, 30, 2), view._needsDisplayRect);
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0       
             
             
   A text wit",
                                                      _output
                                                     );
        Application.End (runState);
    }

    [Fact]
    [AutoInitShutdown]
    public void Correct_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Frame ()
    {
        var label = new Label { Text = "At 0,0" };

        var view = new DerivedView
        {
            X = 2,
            Y = 2,
            Width = 30,
            Height = 2,
            Text = "A text with some long width\n and also with two lines."
        };
        Toplevel top = Application.Top;
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );

        view.Frame = new Rect (1, 1, 10, 1);
        Assert.Equal (new Rect (1, 1, 10, 1), view.Frame);
        Assert.Equal (LayoutStyle.Absolute, view.LayoutStyle);
        Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 1), view._needsDisplayRect);
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0     
 A text wit",
                                                      _output
                                                     );
        Application.End (runState);
    }

    [Fact]
    [AutoInitShutdown]
    public void Correct_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Pos_Dim ()
    {
        var label = new Label { Text = "At 0,0" };

        var view = new DerivedView
        {
            X = 2,
            Y = 2,
            Width = 30,
            Height = 2,
            Text = "A text with some long width\n and also with two lines."
        };
        Toplevel top = Application.Top;
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );

        view.X = 1;
        view.Y = 1;
        view.Width = 10;
        view.Height = 1;
        Assert.Equal (new Rect (1, 1, 10, 1), view.Frame);
        Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
        Assert.Equal (new Rect (0, 0, 30, 2), view._needsDisplayRect);
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0     
 A text wit",
                                                      _output
                                                     );
        Application.End (runState);
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
        Assert.Equal (4, Responder.Instances.Count);
#endif

        view.Dispose ();
        Assert.Null (view.Margin);
        Assert.Null (view.Border);
        Assert.Null (view.Padding);
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawContentComplete_Event_Is_Always_Called ()
    {
        var viewCalled = false;
        var tvCalled = false;

        var view = new View { Width = 10, Height = 10, Text = "View" };
        view.DrawContentComplete += (s, e) => viewCalled = true;
        var tv = new TextView { Y = 11, Width = 10, Height = 10 };
        tv.DrawContentComplete += (s, e) => tvCalled = true;

        Application.Top.Add (view, tv);
        Application.Begin (Application.Top);

        Assert.True (viewCalled);
        Assert.True (tvCalled);
    }

    [Fact]
    [AutoInitShutdown]
    public void Frame_Set_After_Initialize_Update_NeededDisplay ()
    {
        var frame = new FrameView ();

        var label = new Label
        {
            ColorScheme = Colors.ColorSchemes ["Menu"], X = 0, Y = 0, Text = "This should be the first line."
        };

        var button = new Button
        {
            X = 0, // don't overcomplicate unit tests
            Y = 1,
            Text = "Press me!"
        };

        frame.Add (label, button);

        frame.X = Pos.Center ();
        frame.Y = Pos.Center ();
        frame.Width = 40;
        frame.Height = 8;

        Toplevel top = Application.Top;

        top.Add (frame);

        RunState runState = Application.Begin (top);

        top.LayoutComplete += (s, e) => { Assert.Equal (new Rect (0, 0, 80, 25), top._needsDisplayRect); };

        frame.LayoutComplete += (s, e) => { Assert.Equal (new Rect (0, 0, 40, 8), frame._needsDisplayRect); };

        label.LayoutComplete += (s, e) => { Assert.Equal (new Rect (0, 0, 38, 1), label._needsDisplayRect); };

        button.LayoutComplete += (s, e) => { Assert.Equal (new Rect (0, 0, 13, 1), button._needsDisplayRect); };

        Assert.True (label.AutoSize);
        Assert.Equal (new Rect (0, 0, 80, 25), top.Frame);
        Assert.Equal (new Rect (20, 8, 40, 8), frame.Frame);

        Assert.Equal (
                      new Rect (20, 8, 60, 16),
                      new Rect (
                                frame.Frame.Left,
                                frame.Frame.Top,
                                frame.Frame.Right,
                                frame.Frame.Bottom
                               )
                     );
        Assert.Equal (new Rect (0, 0, 30, 1), label.Frame);
        Assert.Equal (new Rect (0, 1, 13, 1), button.Frame); // this proves frame was set
        Application.End (runState);
    }

    [Fact]
    [AutoInitShutdown]
    public void GetHotNormalColor_ColorScheme ()
    {
        var view = new View { ColorScheme = Colors.ColorSchemes ["Base"] };

        Assert.Equal (view.ColorScheme.HotNormal, view.GetHotNormalColor ());

        view.Enabled = false;
        Assert.Equal (view.ColorScheme.Disabled, view.GetHotNormalColor ());
        view.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void GetNormalColor_ColorScheme ()
    {
        var view = new View { ColorScheme = Colors.ColorSchemes ["Base"] };

        Assert.Equal (view.ColorScheme.Normal, view.GetNormalColor ());

        view.Enabled = false;
        Assert.Equal (view.ColorScheme.Disabled, view.GetNormalColor ());
        view.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void GetTopSuperView_Test ()
    {
        var v1 = new View ();
        var fv1 = new FrameView ();
        fv1.Add (v1);
        var tf1 = new TextField ();
        var w1 = new Window ();
        w1.Add (fv1, tf1);
        var top1 = new Toplevel ();
        top1.Add (w1);

        var v2 = new View ();
        var fv2 = new FrameView ();
        fv2.Add (v2);
        var tf2 = new TextField ();
        var w2 = new Window ();
        w2.Add (fv2, tf2);
        var top2 = new Toplevel ();
        top2.Add (w2);

        Assert.Equal (top1, v1.GetTopSuperView ());
        Assert.Equal (top2, v2.GetTopSuperView ());

        v1.Dispose ();
        fv1.Dispose ();
        tf1.Dispose ();
        w1.Dispose ();
        top1.Dispose ();
        v2.Dispose ();
        fv2.Dispose ();
        tf2.Dispose ();
        w2.Dispose ();
        top2.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Incorrect_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Frame ()
    {
        var label = new Label { Text = "At 0,0" };

        var view = new DerivedView
        {
            X = 2,
            Y = 2,
            Width = 30,
            Height = 2,
            Text = "A text with some long width\n and also with two lines."
        };
        Toplevel top = Application.Top;
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );

        view.Frame = new Rect (3, 3, 10, 1);
        Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 1), view._needsDisplayRect);
        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   A text witith two lines.  ",
                                                      _output
                                                     );
        Application.End (runState);
    }

    [Fact]
    [AutoInitShutdown]
    public void Incorrect_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Pos_Dim ()
    {
        var label = new Label { Text = "At 0,0" };

        var view = new DerivedView
        {
            X = 2,
            Y = 2,
            Width = 30,
            Height = 2,
            Text = "A text with some long width\n and also with two lines."
        };
        Toplevel top = Application.Top;
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );

        view.X = 3;
        view.Y = 3;
        view.Width = 10;
        view.Height = 1;
        Assert.Equal (new Rect (3, 3, 10, 1), view.Frame);
        Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
        Assert.Equal (new Rect (0, 0, 30, 2), view._needsDisplayRect);
        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   A text witith two lines.  ",
                                                      _output
                                                     );
        Application.End (runState);
    }

    [Fact]
    [AutoInitShutdown]
    public void Incorrect_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Frame ()
    {
        var label = new Label { Text = "At 0,0" };

        var view = new DerivedView
        {
            X = 2,
            Y = 2,
            Width = 30,
            Height = 2,
            Text = "A text with some long width\n and also with two lines."
        };
        Toplevel top = Application.Top;
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );

        view.Frame = new Rect (1, 1, 10, 1);
        Assert.Equal (new Rect (1, 1, 10, 1), view.Frame);
        Assert.Equal (LayoutStyle.Absolute, view.LayoutStyle);
        Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
        Assert.Equal (new Rect (0, 0, 10, 1), view._needsDisplayRect);
        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
 A text wit                  
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );
        Application.End (runState);
    }

    [Fact]
    [AutoInitShutdown]
    public void Incorrect_Redraw_Bounds_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Pos_Dim ()
    {
        var label = new Label { Text = "At 0,0" };

        var view = new DerivedView
        {
            X = 2,
            Y = 2,
            Width = 30,
            Height = 2,
            Text = "A text with some long width\n and also with two lines."
        };
        Toplevel top = Application.Top;
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );

        view.X = 1;
        view.Y = 1;
        view.Width = 10;
        view.Height = 1;
        Assert.Equal (new Rect (1, 1, 10, 1), view.Frame);
        Assert.Equal (new Rect (0, 0, 10, 1), view.Bounds);
        Assert.Equal (new Rect (0, 0, 30, 2), view._needsDisplayRect);
        view.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
 A text wit                  
  A text with some long width
   and also with two lines.  ",
                                                      _output
                                                     );
        Application.End (runState);
    }

    [Fact]
    [TestRespondersDisposed]
    public void Initialized_Event_Comparing_With_Added_Event ()
    {
        Application.Init (new FakeDriver ());

        var top = new Toplevel { Id = "0" }; // Frame: 0, 0, 80, 25; Bounds: 0, 0, 80, 25

        var winAddedToTop = new Window
        {
            Id = "t", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 0, 0, 80, 25; Bounds: 0, 0, 78, 23

        var v1AddedToWin = new View
        {
            Id = "v1", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (because Windows has a border)

        var v2AddedToWin = new View
        {
            Id = "v2", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (because Windows has a border)

        var svAddedTov1 = new View
        {
            Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill ()
        }; // Frame: 1, 1, 78, 23 (same as it's superview v1AddedToWin)

        int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

        winAddedToTop.Added += (s, e) =>
                               {
                                   Assert.Equal (e.Parent.Frame.Width, winAddedToTop.Frame.Width);
                                   Assert.Equal (e.Parent.Frame.Height, winAddedToTop.Frame.Height);
                               };

        v1AddedToWin.Added += (s, e) =>
                              {
                                  Assert.Equal (e.Parent.Frame.Width, v1AddedToWin.Frame.Width);
                                  Assert.Equal (e.Parent.Frame.Height, v1AddedToWin.Frame.Height);
                              };

        v2AddedToWin.Added += (s, e) =>
                              {
                                  Assert.Equal (e.Parent.Frame.Width, v2AddedToWin.Frame.Width);
                                  Assert.Equal (e.Parent.Frame.Height, v2AddedToWin.Frame.Height);
                              };

        svAddedTov1.Added += (s, e) =>
                             {
                                 Assert.Equal (e.Parent.Frame.Width, svAddedTov1.Frame.Width);
                                 Assert.Equal (e.Parent.Frame.Height, svAddedTov1.Frame.Height);
                             };

        top.Initialized += (s, e) =>
                           {
                               tc++;
                               Assert.Equal (1, tc);
                               Assert.Equal (1, wc);
                               Assert.Equal (1, v1c);
                               Assert.Equal (1, v2c);
                               Assert.Equal (1, sv1c);

                               Assert.True (top.CanFocus);
                               Assert.True (winAddedToTop.CanFocus);
                               Assert.False (v1AddedToWin.CanFocus);
                               Assert.False (v2AddedToWin.CanFocus);
                               Assert.False (svAddedTov1.CanFocus);

                               Application.Refresh ();
                           };

        winAddedToTop.Initialized += (s, e) =>
                                     {
                                         wc++;
                                         Assert.Equal (top.Bounds.Width, winAddedToTop.Frame.Width);
                                         Assert.Equal (top.Bounds.Height, winAddedToTop.Frame.Height);
                                     };

        v1AddedToWin.Initialized += (s, e) =>
                                    {
                                        v1c++;

                                        // Top.Frame: 0, 0, 80, 25; Top.Bounds: 0, 0, 80, 25
                                        // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                        // in no way should the v1AddedToWin.Frame be the same as the Top.Frame/Bounds
                                        // as it is a subview of winAddedToTop, which has a border!
                                        //Assert.Equal (top.Bounds.Width,  v1AddedToWin.Frame.Width);
                                        //Assert.Equal (top.Bounds.Height, v1AddedToWin.Frame.Height);
                                    };

        v2AddedToWin.Initialized += (s, e) =>
                                    {
                                        v2c++;

                                        // Top.Frame: 0, 0, 80, 25; Top.Bounds: 0, 0, 80, 25
                                        // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                        // in no way should the v2AddedToWin.Frame be the same as the Top.Frame/Bounds
                                        // as it is a subview of winAddedToTop, which has a border!
                                        //Assert.Equal (top.Bounds.Width,  v2AddedToWin.Frame.Width);
                                        //Assert.Equal (top.Bounds.Height, v2AddedToWin.Frame.Height);
                                    };

        svAddedTov1.Initialized += (s, e) =>
                                   {
                                       sv1c++;

                                       // Top.Frame: 0, 0, 80, 25; Top.Bounds: 0, 0, 80, 25
                                       // BUGBUG: This is wrong, it should be 78, 23. This test has always been broken.
                                       // in no way should the svAddedTov1.Frame be the same as the Top.Frame/Bounds
                                       // because sv1AddedTov1 is a subview of v1AddedToWin, which is a subview of
                                       // winAddedToTop, which has a border!
                                       //Assert.Equal (top.Bounds.Width,  svAddedTov1.Frame.Width);
                                       //Assert.Equal (top.Bounds.Height, svAddedTov1.Frame.Height);
                                       Assert.False (svAddedTov1.CanFocus);
                                       Assert.Throws<InvalidOperationException> (() => svAddedTov1.CanFocus = true);
                                       Assert.False (svAddedTov1.CanFocus);
                                   };

        v1AddedToWin.Add (svAddedTov1);
        winAddedToTop.Add (v1AddedToWin, v2AddedToWin);
        top.Add (winAddedToTop);

        Application.Iteration += (s, a) =>
                                 {
                                     Application.Refresh ();
                                     top.Running = false;
                                 };

        Application.Run (top);
        Application.Shutdown ();

        Assert.Equal (1, tc);
        Assert.Equal (1, wc);
        Assert.Equal (1, v1c);
        Assert.Equal (1, v2c);
        Assert.Equal (1, sv1c);

        Assert.True (top.CanFocus);
        Assert.True (winAddedToTop.CanFocus);
        Assert.False (v1AddedToWin.CanFocus);
        Assert.False (v2AddedToWin.CanFocus);
        Assert.False (svAddedTov1.CanFocus);

        v1AddedToWin.CanFocus = true;
        Assert.False (svAddedTov1.CanFocus); // False because sv1 was disposed and it isn't a subview of v1.
    }

    [Fact]
    [TestRespondersDisposed]
    public void Initialized_Event_Will_Be_Invoked_When_Added_Dynamically ()
    {
        Application.Init (new FakeDriver ());

        var t = new Toplevel { Id = "0" };

        var w = new Window { Id = "t", Width = Dim.Fill (), Height = Dim.Fill () };
        var v1 = new View { Id = "v1", Width = Dim.Fill (), Height = Dim.Fill () };
        var v2 = new View { Id = "v2", Width = Dim.Fill (), Height = Dim.Fill () };

        int tc = 0, wc = 0, v1c = 0, v2c = 0, sv1c = 0;

        t.Initialized += (s, e) =>
                         {
                             tc++;
                             Assert.Equal (1, tc);
                             Assert.Equal (1, wc);
                             Assert.Equal (1, v1c);
                             Assert.Equal (1, v2c);
                             Assert.Equal (0, sv1c); // Added after t in the Application.Iteration.

                             Assert.True (t.CanFocus);
                             Assert.True (w.CanFocus);
                             Assert.False (v1.CanFocus);
                             Assert.False (v2.CanFocus);

                             Application.Refresh ();
                         };

        w.Initialized += (s, e) =>
                         {
                             wc++;
                             Assert.Equal (t.Bounds.Width, w.Frame.Width);
                             Assert.Equal (t.Bounds.Height, w.Frame.Height);
                         };

        v1.Initialized += (s, e) =>
                          {
                              v1c++;

                              //Assert.Equal (t.Bounds.Width, v1.Frame.Width);
                              //Assert.Equal (t.Bounds.Height, v1.Frame.Height);
                          };

        v2.Initialized += (s, e) =>
                          {
                              v2c++;

                              //Assert.Equal (t.Bounds.Width,  v2.Frame.Width);
                              //Assert.Equal (t.Bounds.Height, v2.Frame.Height);
                          };
        w.Add (v1, v2);
        t.Add (w);

        Application.Iteration += (s, a) =>
                                 {
                                     var sv1 = new View { Id = "sv1", Width = Dim.Fill (), Height = Dim.Fill () };

                                     sv1.Initialized += (s, e) =>
                                                        {
                                                            sv1c++;
                                                            Assert.NotEqual (t.Frame.Width, sv1.Frame.Width);
                                                            Assert.NotEqual (t.Frame.Height, sv1.Frame.Height);
                                                            Assert.False (sv1.CanFocus);
                                                            Assert.Throws<InvalidOperationException> (() => sv1.CanFocus = true);
                                                            Assert.False (sv1.CanFocus);
                                                        };

                                     v1.Add (sv1);

                                     Application.Refresh ();
                                     t.Running = false;
                                 };

        Application.Run (t);
        Application.Shutdown ();

        Assert.Equal (1, tc);
        Assert.Equal (1, wc);
        Assert.Equal (1, v1c);
        Assert.Equal (1, v2c);
        Assert.Equal (1, sv1c);

        Assert.True (t.CanFocus);
        Assert.True (w.CanFocus);
        Assert.False (v1.CanFocus);
        Assert.False (v2.CanFocus);
    }

    [Fact]
    public void Internal_Tests ()
    {
        var rect = new Rect (1, 1, 10, 1);
        var view = new View { Frame = rect };
    }

    [Fact]
    [TestRespondersDisposed]
    public void IsAdded_Added_Removed ()
    {
        var top = new Toplevel ();
        var view = new View ();
        Assert.False (view.IsAdded);
        top.Add (view);
        Assert.True (view.IsAdded);
        top.Remove (view);
        Assert.False (view.IsAdded);

        top.Dispose ();
        view.Dispose ();
    }

    [Theory]
    [TestRespondersDisposed]
    [InlineData (1)]
    [InlineData (2)]
    [InlineData (3)]
    public void LabelChangeText_RendersCorrectly_Constructors (int choice)
    {
        var driver = new FakeDriver ();
        Application.Init (driver);

        try
        {
            // Create a label with a short text 
            Label lbl;
            var text = "test";

            if (choice == 1)
            {
                // An object initializer should call the default constructor.
                lbl = new Label { Text = text };
            }
            else if (choice == 2)
            {
                // Calling the default constructor followed by the object initializer.
                lbl = new Label { Text = text };
            }
            else
            {
                // Calling the Text constructor.
                lbl = new Label { Text = text };
            }

            Application.Top.Add (lbl);
            Application.Begin (Application.Top);

            // should have the initial text
            Assert.Equal ((Rune)'t', driver.Contents [0, 0].Rune);
            Assert.Equal ((Rune)'e', driver.Contents [0, 1].Rune);
            Assert.Equal ((Rune)'s', driver.Contents [0, 2].Rune);
            Assert.Equal ((Rune)'t', driver.Contents [0, 3].Rune);
            Assert.Equal ((Rune)' ', driver.Contents [0, 4].Rune);
        }
        finally
        {
            Application.Shutdown ();
        }
    }

    [Fact]
    [TestRespondersDisposed]
    public void New_Initializes ()
    {
        // Parameterless
        var r = new View ();
        Assert.NotNull (r);
        Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
        Assert.Equal ("View()(0,0,0,0)", r.ToString ());
        Assert.False (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
        Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
        Assert.Null (r.Focused);
        Assert.Null (r.ColorScheme);
        Assert.Equal (0, r.Width);
        Assert.Equal (0, r.Height);
        Assert.Equal (0, r.X);
        Assert.Equal (0, r.Y);
        Assert.False (r.IsCurrentTop);
        Assert.Empty (r.Id);
        Assert.Empty (r.Subviews);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
        r.Dispose ();

        // Empty Rect
        r = new View { Frame = Rect.Empty };
        Assert.NotNull (r);
        Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
        Assert.Equal ("View()(0,0,0,0)", r.ToString ());
        Assert.False (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new Rect (0, 0, 0, 0), r.Bounds);
        Assert.Equal (new Rect (0, 0, 0, 0), r.Frame);
        Assert.Null (r.Focused);
        Assert.Null (r.ColorScheme);
        Assert.Equal (0, r.Width);
        Assert.Equal (0, r.Height);
        Assert.Equal (0, r.X);
        Assert.Equal (0, r.Y);
        Assert.False (r.IsCurrentTop);
        Assert.Empty (r.Id);
        Assert.Empty (r.Subviews);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
        r.Dispose ();

        // Rect with values
        r = new View { Frame = new Rect (1, 2, 3, 4) };
        Assert.NotNull (r);
        Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);
        Assert.Equal ("View()(1,2,3,4)", r.ToString ());
        Assert.False (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new Rect (0, 0, 3, 4), r.Bounds);
        Assert.Equal (new Rect (1, 2, 3, 4), r.Frame);
        Assert.Null (r.Focused);
        Assert.Null (r.ColorScheme);
        Assert.Equal (3, r.Width);
        Assert.Equal (4, r.Height);
        Assert.Equal (1, r.X);
        Assert.Equal (2, r.Y);
        Assert.False (r.IsCurrentTop);
        Assert.Empty (r.Id);
        Assert.Empty (r.Subviews);
        Assert.False (r.WantContinuousButtonPressed);
        Assert.False (r.WantMousePositionReports);
        Assert.Null (r.SuperView);
        Assert.Null (r.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, r.TextDirection);
        r.Dispose ();

        // Initializes a view with a vertical direction
        r = new View
        {
            Text = "Vertical View", TextDirection = TextDirection.TopBottom_LeftRight, AutoSize = true
        }; // BUGBUG: AutoSize or Height need be set
        Assert.NotNull (r);
        Assert.Equal (LayoutStyle.Absolute, r.LayoutStyle);

        // BUGBUG: IsInitialized must be true to process calculation
        r.BeginInit ();
        r.EndInit ();
        Assert.Equal ("View(Vertical View)(0,0,1,13)", r.ToString ());
        Assert.False (r.CanFocus);
        Assert.False (r.HasFocus);
        Assert.Equal (new Rect (0, 0, 1, 13), r.Bounds);
        Assert.Equal (new Rect (0, 0, 1, 13), r.Frame);
        Assert.Null (r.Focused);
        Assert.Null (r.ColorScheme);
        Assert.False (r.IsCurrentTop);
        Assert.Equal ("Vertical View", r.Id);
        Assert.Empty (r.Subviews);
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

        Assert.False (r.OnKeyDown (new Key { KeyCode = KeyCode.Null }));

        //Assert.False (r.OnKeyDown (new KeyEventArgs () { Key = Key.Unknown }));
        Assert.False (r.OnKeyUp (new Key { KeyCode = KeyCode.Null }));
        Assert.False (r.MouseEvent (new MouseEvent { Flags = MouseFlags.AllEvents }));
        Assert.False (r.OnMouseEnter (new MouseEvent { Flags = MouseFlags.AllEvents }));
        Assert.False (r.OnMouseLeave (new MouseEvent { Flags = MouseFlags.AllEvents }));

        var v1 = new View ();
        Assert.False (r.OnEnter (v1));
        v1.Dispose ();

        var v2 = new View ();
        Assert.False (r.OnLeave (v2));
        v2.Dispose ();

        r.Dispose ();

        // TODO: Add more
    }

    [Fact]
    [AutoInitShutdown]
    public void Test_Nested_Views_With_Height_Equal_To_One ()
    {
        var v = new View { Width = 11, Height = 3, ColorScheme = new ColorScheme () };

        var top = new View { Width = Dim.Fill (), Height = 1 };
        var bottom = new View { Width = Dim.Fill (), Height = 1, Y = 2 };

        top.Add (new Label { Text = "111" });
        v.Add (top);
        v.Add (new LineView (Orientation.Horizontal) { Y = 1 });
        bottom.Add (new Label { Text = "222" });
        v.Add (bottom);

        v.BeginInit ();
        v.EndInit ();
        v.LayoutSubviews ();
        v.Draw ();

        var looksLike =
            @"    
111
───────────
222";
        TestHelpers.AssertDriverContentsAre (looksLike, _output);
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
        var super = new View { Frame = new Rect (0, 0, 10, 10) };
        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubviews ();

        Assert.Equal (1, view.X);
        Assert.Equal (2, view.Y);
        Assert.Equal (3, view.Width);
        Assert.Equal (4, view.Height);
        Assert.False (view.Frame.IsEmpty);
        Assert.Equal (new Rect (1, 2, 3, 4), view.Frame);
        Assert.False (view.Bounds.IsEmpty);
        Assert.Equal (new Rect (0, 0, 3, 4), view.Bounds);

        view.LayoutSubviews ();

        Assert.Equal (1, view.X);
        Assert.Equal (2, view.Y);
        Assert.Equal (3, view.Width);
        Assert.Equal (4, view.Height);
        Assert.False (view.Frame.IsEmpty);
        Assert.False (view.Bounds.IsEmpty);
        super.Dispose ();

#if DEBUG_IDISPOSABLE
        Assert.Empty (Responder.Instances);
#endif

        // Default Constructor
        view = new View ();
        Assert.Equal (0, view.X);
        Assert.Equal (0, view.Y);
        Assert.Equal (0, view.Width);
        Assert.Equal (0, view.Height);
        Assert.True (view.Frame.IsEmpty);
        Assert.True (view.Bounds.IsEmpty);
        view.Dispose ();

        // Object Initializer
        view = new View { X = 1, Y = 2, Text = "" };
        Assert.Equal (1, view.X);
        Assert.Equal (2, view.Y);
        Assert.Equal (0, view.Width);
        Assert.Equal (0, view.Height);
        Assert.False (view.Frame.IsEmpty);
        Assert.True (view.Bounds.IsEmpty);
        view.Dispose ();

        // Default Constructor and post assignment equivalent to Object Initializer
        view = new View ();
        view.X = 1;
        view.Y = 2;
        view.Width = 3;
        view.Height = 4;
        super = new View { Frame = new Rect (0, 0, 10, 10) };
        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubviews ();
        Assert.Equal (1, view.X);
        Assert.Equal (2, view.Y);
        Assert.Equal (3, view.Width);
        Assert.Equal (4, view.Height);
        Assert.False (view.Frame.IsEmpty);
        Assert.Equal (new Rect (1, 2, 3, 4), view.Frame);
        Assert.False (view.Bounds.IsEmpty);
        Assert.Equal (new Rect (0, 0, 3, 4), view.Bounds);
        super.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Visible_Clear_The_View_Output ()
    {
        var view = new View { Text = "Testing visibility." }; // use View, not Label to avoid AutoSize == true

        // BUGBUG: AutoSize is false and size wasn't provided so it's 0,0
        Assert.Equal (0, view.Frame.Width);
        Assert.Equal (0, view.Height);
        var win = new Window ();
        win.Add (view);
        Toplevel top = Application.Top;
        top.Add (win);
        RunState rs = Application.Begin (top);

        view.AutoSize = true;
        Assert.Equal ("Testing visibility.".Length, view.Frame.Width);
        Assert.True (view.Visible);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        TestHelpers.AssertDriverContentsWithFrameAre (
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
        Application.RunIteration (ref rs, ref firstIteration);

        TestHelpers.AssertDriverContentsWithFrameAre (
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
    }

    [Fact]
    [AutoInitShutdown]
    public void Visible_Sets_Also_Sets_Subviews ()
    {
        var button = new Button { Text = "Click Me" };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (button);
        Toplevel top = Application.Top;
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
                                     Assert.True (RunesCount () > 0);

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
                                     top.Draw ();
                                     Assert.True (RunesCount () == 0);

                                     win.Visible = true;
                                     win.FocusFirst ();
                                     Assert.True (button.HasFocus);
                                     Assert.True (win.HasFocus);
                                     top.Draw ();
                                     Assert.True (RunesCount () > 0);

                                     Application.RequestStop ();
                                 };

        Application.Run ();

        Assert.Equal (1, iterations);

        int RunesCount ()
        {
            Cell [,] contents = ((FakeDriver)Application.Driver).Contents;
            var runesCount = 0;

            for (var i = 0; i < Application.Driver.Rows; i++)
            {
                for (var j = 0; j < Application.Driver.Cols; j++)
                {
                    if (contents [i, j].Rune != (Rune)' ')
                    {
                        runesCount++;
                    }
                }
            }

            return runesCount;
        }
    }

    public class DerivedView : View
    {
        public DerivedView () { CanFocus = true; }
        public bool IsKeyDown { get; set; }
        public bool IsKeyPress { get; set; }
        public bool IsKeyUp { get; set; }
        public override string Text { get; set; }

        public override void OnDrawContent (Rect contentArea)
        {
            var idx = 0;

            // BUGBUG: v2 - this should use Bounds, not Frame
            for (var r = 0; r < Frame.Height; r++)
            {
                for (var c = 0; c < Frame.Width; c++)
                {
                    if (idx < Text.Length)
                    {
                        char rune = Text [idx];

                        if (rune != '\n')
                        {
                            AddRune (c, r, (Rune)Text [idx]);
                        }

                        idx++;

                        if (rune == '\n')
                        {
                            break;
                        }
                    }
                }
            }

            ClearLayoutNeeded ();
            ClearNeedsDisplay ();
        }

        public override bool OnKeyDown (Key keyEvent)
        {
            IsKeyDown = true;

            return true;
        }

        public override bool OnKeyUp (Key keyEvent)
        {
            IsKeyUp = true;

            return true;
        }

        public override bool OnProcessKeyDown (Key keyEvent)
        {
            IsKeyPress = true;

            return true;
        }
    }
}
