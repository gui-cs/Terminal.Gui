#nullable enable
using System.Text;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

[Trait ("Category", "Output")]
public class DrawTests (ITestOutputHelper output)
{

    [Fact]
    [AutoInitShutdown]
    [Trait ("Category", "Unicode")]
    public void CJK_Compatibility_Ideographs_ConsoleWidth_ColumnWidth_Equal_Two ()
    {
        const string us = "\U0000f900";
        var r = (Rune)0xf900;

        Assert.Equal ("豈", us);
        Assert.Equal ("豈", r.ToString ());
        Assert.Equal (us, r.ToString ());

        Assert.Equal (2, us.GetColumns ());
        Assert.Equal (2, r.GetColumns ());

        var win = new Window { Title = us };
        var view = new View { Text = r.ToString (), Height = Dim.Fill (), Width = Dim.Fill () };
        var tf = new TextField { Text = us, Y = 1, Width = 3 };
        win.Add (view, tf);
        Toplevel top = new ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 4);

        const string expectedOutput = """

                                      ┌┤豈├────┐
                                      │豈      │
                                      │豈      │
                                      └────────┘
                                      """;
        DriverAssert.AssertDriverContentsWithFrameAre (expectedOutput, output);

        DriverAssert.AssertDriverContentsAre (expectedOutput, output);

        // This test has nothing to do with color - removing as it is not relevant and fragile
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    [Trait ("Category", "Output")]
    public void Colors_On_TextAlignment_Right_And_Bottom ()
    {
        var viewRight = new View
        {
            Text = "Test",
            Width = 6,
            Height = 1,
            TextAlignment = Alignment.End,
        };

        var viewBottom = new View
        {
            Text = "Test",
            TextDirection = TextDirection.TopBottom_LeftRight,
            Y = 1,
            Width = 1,
            Height = 6,
            VerticalTextAlignment = Alignment.End,
        };
        Toplevel top = new ();
        top.Add (viewRight, viewBottom);

        var rs = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (7, 7);
        Application.RunIteration (ref rs);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                        Test
                                                            
                                                            
                                                      T     
                                                      e     
                                                      s     
                                                      t     
                                                      """,
                                                      output
                                                     );

        DriverAssert.AssertDriverAttributesAre (
                                               """

                                               000000
                                               0
                                               0
                                               0
                                               0
                                               0
                                               0
                                               """,
                                               output,
                                               Application.Driver,
                                               SchemeManager.GetSchemes () ["Base"]!.Normal
                                              );
        top.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport ()
    {
        var view = new View { Width = 2, Height = 2, BorderStyle = LineStyle.Single };
        Assert.True (view.NeedsLayout);
        Assert.True (view.NeedsDraw);
        view.Layout ();

        Assert.Equal (new (0, 0, 2, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        Assert.True (view.NeedsDraw);
        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """

                                                      ┌┐
                                                      └┘
                                                      """,
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Bottom ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border!.Thickness = new (1, 1, 1, 0);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 2, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre ("──", output);
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Left ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border!.Thickness = new (0, 1, 1, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 1, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """

                                                      │
                                                      │
                                                      """,
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Right ()
    {
        var view = new View { Width = 1, Height = 2, BorderStyle = LineStyle.Single };
        view.Border!.Thickness = new (1, 1, 0, 1);
        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 1, 2), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """

                                                      │
                                                      │
                                                      """,
                                                      output
                                                     );
    }

    [Fact]
    [SetupFakeDriver]
    public void Draw_Minimum_Full_Border_With_Empty_Viewport_Without_Top ()
    {
        var view = new View { Width = 2, Height = 1, BorderStyle = LineStyle.Single };
        view.Border!.Thickness = new (1, 0, 1, 1);

        view.BeginInit ();
        view.EndInit ();
        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (0, 0, 2, 1), view.Frame);
        Assert.Equal (Rectangle.Empty, view.Viewport);

        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      "││",
                                                      output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Horizontal_With_New_Lines ()
    {
        var subView = new View
        {
            Id = "subView",
            X = 1,
            Width = 1,
            Height = 7,
            Text = """
                   s
                   u
                   b
                   V
                   i
                   e
                   w
                   """
        };

        var view = new View
        {
            Id = "view", Width = 2, Height = 20, Text = """
                                                        0
                                                        1
                                                        2
                                                        3
                                                        4
                                                        5
                                                        6
                                                        7
                                                        8
                                                        9
                                                        0
                                                        1
                                                        2
                                                        3
                                                        4
                                                        5
                                                        6
                                                        7
                                                        8
                                                        9
                                                        """
        };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);
        var rs = Application.Begin (top);

        top.Draw ();
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       0s
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                      """,
                                                      output
                                                     );

        content.X = -1;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       s
                                                       u
                                                       b
                                                       V
                                                       i
                                                      """,
                                                      output
                                                     );

        content.X = -2;
        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre (@"", output);

        content.X = 0;
        content.Y = -1;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                       5e
                                                      """,
                                                      output
                                                     );

        content.Y = -6;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       6w
                                                       7 
                                                       8 
                                                       9 
                                                       0 
                                                      """,
                                                      output
                                                     );

        content.Y = -19;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       9
                                                      """,
                                                      output
                                                     );

        content.Y = -20;
        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre ("", output);

        content.X = -2;
        content.Y = 0;
        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre ("", output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Horizontal_Without_New_Lines ()
    {
        // BUGBUG: This previously assumed the default height of a View was 1.
        var subView = new View
        {
            Id = "subView",
            Y = 1,
            Width = 7,
            Height = 1,
            Text = "subView"
        };
        var view = new View { Id = "view", Width = 20, Height = 2, Text = "01234567890123456789" };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);

        // BUGBUG: v2 - it's bogus to reference .Frame before BeginInit. And why is the clip being set anyway???

        top.SubViewsLaidOut += Top_LayoutComplete;
        Application.Begin (top);

        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       01234
                                                       subVi
                                                      """,
                                                      output
                                                     );

        content.X = -1;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       12345
                                                       ubVie
                                                      """,
                                                      output
                                                     );

        content.Y = -1;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       ubVie
                                                      """,
                                                      output
                                                     );

        content.Y = -2;
        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre ("", output);

        content.X = -20;
        content.Y = 0;
        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre ("", output);
        top.Dispose ();

        return;

        void Top_LayoutComplete (object? sender, LayoutEventArgs e) { Application.Driver!.Clip = new (container.Frame); }
    }

    [Fact]
    [AutoInitShutdown]
    public void Draw_Negative_Viewport_Vertical ()
    {
        var subView = new View
        {
            Id = "subView",
            X = 1,
            Width = 1,
            Height = 7,
            Text = "subView",
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        var view = new View
        {
            Id = "view",
            Width = 2,
            Height = 20,
            Text = "01234567890123456789",
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        view.Add (subView);
        var content = new View { Id = "content", Width = 20, Height = 20 };
        content.Add (view);

        var container = new View
        {
            Id = "container",
            X = 1,
            Y = 1,
            Width = 5,
            Height = 5
        };
        container.Add (content);
        Toplevel top = new ();
        top.Add (container);
        Application.Begin (top);

        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       0s
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                      """,
                                                      output
                                                     );

        content.X = -1;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       s
                                                       u
                                                       b
                                                       V
                                                       i
                                                      """,
                                                      output
                                                     );

        content.X = -2;
        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre (@"", output);

        content.X = 0;
        content.Y = -1;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       1u
                                                       2b
                                                       3V
                                                       4i
                                                       5e
                                                      """,
                                                      output
                                                     );

        content.Y = -6;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       6w
                                                       7 
                                                       8 
                                                       9 
                                                       0 
                                                      """,
                                                      output
                                                     );

        content.Y = -19;
        Application.LayoutAndDraw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      """
                                                      
                                                       9
                                                      """,
                                                      output
                                                     );

        content.Y = -20;
        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre ("", output);

        content.X = -2;
        content.Y = 0;
        Application.LayoutAndDraw ();
        DriverAssert.AssertDriverContentsWithFrameAre ("", output);
        top.Dispose ();
    }

    [Theory]
    [SetupFakeDriver]
    [InlineData ("𝔽𝕆𝕆𝔹𝔸R")]
    [InlineData ("a𐐀b")]
    public void DrawHotString_NonBmp (string expected)
    {
        var view = new View { Width = 10, Height = 1 };
        view.DrawHotString (expected, Attribute.Default, Attribute.Default);

        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
    }

    // TODO: The tests below that use Label should use View instead.
    [Fact]
    [AutoInitShutdown]
    public void Non_Bmp_ConsoleWidth_ColumnWidth_Equal_Two ()
    {
        var us = "\U0001d539";
        var r = (Rune)0x1d539;

        Assert.Equal ("𝔹", us);
        Assert.Equal ("𝔹", r.ToString ());
        Assert.Equal (us, r.ToString ());

        Assert.Equal (1, us.GetColumns ());
        Assert.Equal (1, r.GetColumns ());

        var win = new Window { Title = us };
        var view = new Label { Text = r.ToString () };
        var tf = new TextField { Text = us, Y = 1, Width = 3 };
        win.Add (view, tf);
        Toplevel top = new ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (10, 4);

        var expected = """

                       ┌┤𝔹├─────┐
                       │𝔹       │
                       │𝔹       │
                       └────────┘
                       """;
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        DriverAssert.AssertDriverContentsAre (expected, output);
        top.Dispose ();

        // This test has nothing to do with color - removing as it is not relevant and fragile
    }

    [Fact]
    [TestRespondersDisposed]
    public void Draw_Throws_IndexOutOfRangeException_With_Negative_Bounds ()
    {
        Application.Init (new FakeDriver ());

        Toplevel top = new ();

        var view = new View { X = -2, Text = "view" };
        top.Add (view);

        Application.Iteration += (s, a) =>
                                 {
                                     Assert.Equal (-2, view.X);

                                     Application.RequestStop ();
                                 };

        try
        {
            Application.Run (top);
        }
        catch (IndexOutOfRangeException ex)
        {
            // After the fix this exception will not be caught.
            Assert.IsType<IndexOutOfRangeException> (ex);
        }

        top.Dispose ();

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }


    [Fact]
    [AutoInitShutdown]
    public void Correct_Redraw_Viewport_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Frame ()
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
        Toplevel top = new ();
        top.Add (label, view);
        RunState runState = Application.Begin (top);
        Application.RunIteration (ref runState);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  "
        ,
                                                      output
                                                     );

        view.Frame = new (3, 3, 10, 1);
        Assert.Equal (new (3, 3, 10, 1), view.Frame);
        Assert.Equal (new (0, 0, 10, 1), view.Viewport);
        Assert.Equal (new (0, 0, 10, 1), view.NeedsDrawRect);
        //Application.Refresh();
        top.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
        @"
At 0,0       
             
             
   A text wit",
                                                      output
                                                     );
        Application.End (runState);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Correct_Redraw_Viewport_NeedDisplay_On_Shrink_And_Move_Down_Right_Using_Pos_Dim ()
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
        Toplevel top = new ();
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        top.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  "
        ,
                                                      output
                                                     );

        view.X = 3;
        view.Y = 3;
        view.Width = 10;
        view.Height = 1;
        Assert.Equal (new (3, 3, 10, 1), view.Frame);
        Assert.Equal (new (0, 0, 10, 1), view.Viewport);
        Assert.Equal (new (0, 0, 10, 1), view.NeedsDrawRect);
        View.SetClipToScreen ();
        top.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0       
             
             
   A text wit"
        ,
                                                      output
                                                     );
        Application.End (runState);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Correct_Redraw_Viewport_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Frame ()
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
        Toplevel top = new ();
        top.Add (label, view);
        RunState runState = Application.Begin (top);
        Application.RunIteration (ref runState);

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  "
        ,
                                                      output
                                                     );

        view.Frame = new (1, 1, 10, 1);
        Assert.Equal (new (1, 1, 10, 1), view.Frame);
        Assert.Equal (new (0, 0, 10, 1), view.Viewport);
        Assert.Equal (new (0, 0, 10, 1), view.NeedsDrawRect);
        top.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
        @"
At 0,0     
 A text wit"
        ,
                                                      output
                                                     );
        Application.End (runState);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Correct_Redraw_Viewport_NeedDisplay_On_Shrink_And_Move_Up_Left_Using_Pos_Dim ()
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
        Toplevel top = new ();
        top.Add (label, view);
        RunState runState = Application.Begin (top);

        top.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0                       
                             
  A text with some long width
   and also with two lines.  "
        ,
                                                      output
                                                     );

        view.X = 1;
        view.Y = 1;
        view.Width = 10;
        view.Height = 1;
        Assert.Equal (new (1, 1, 10, 1), view.Frame);
        Assert.Equal (new (0, 0, 10, 1), view.Viewport);
        Assert.Equal (new (0, 0, 10, 1), view.NeedsDrawRect);
        View.SetClipToScreen ();

        top.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                      @"
At 0,0     
 A text wit"
        ,
                                                      output
                                                     );
        Application.End (runState);
        top.Dispose ();
    }
    public class DerivedView : View
    {
        public DerivedView () { CanFocus = true; }
        public bool IsKeyDown { get; set; }
        public bool IsKeyPress { get; set; }
        public bool IsKeyUp { get; set; }
        public override string Text { get; set; } = null!;

        protected override bool OnDrawingContent ()
        {
            var idx = 0;

            // BUGBUG: v2 - this should use Viewport, not Frame
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

            ClearNeedsDraw ();

            return true;
        }

        protected override bool OnKeyDown (Key keyEvent)
        {
            IsKeyDown = true;

            return true;
        }

        public override bool OnKeyUp (Key keyEvent)
        {
            IsKeyUp = true;

            return true;
        }

        protected override bool OnKeyDownNotHandled (Key keyEvent)
        {
            IsKeyPress = true;

            return true;
        }
    }
}
