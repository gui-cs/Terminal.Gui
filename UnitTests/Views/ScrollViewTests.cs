using System.Text;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ScrollViewTests (ITestOutputHelper output)
{
    [Fact]
    public void Adding_Views ()
    {
        var sv = new ScrollView { Width = 20, Height = 10 };
        sv.SetContentSize (new (30, 20));

        sv.Add (
                new View { Width = 10, Height = 5 },
                new View { X = 12, Y = 7, Width = 10, Height = 5 }
               );

        Assert.Equal (new (30, 20), sv.GetContentSize ());
        Assert.Equal (2, sv.Subviews [0].Subviews.Count);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoHideScrollBars_False_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
    {
        var sv = new ScrollView { Width = 10, Height = 10, AutoHideScrollBars = false };

        sv.ShowHorizontalScrollIndicator = true;
        sv.ShowVerticalScrollIndicator = true;

        var top = new Toplevel ();
        top.Add (sv);
        Application.Begin (top);

        Assert.Equal (new (0, 0, 10, 10), sv.Viewport);

        Assert.False (sv.AutoHideScrollBars);
        Assert.True (sv.ShowHorizontalScrollIndicator);
        Assert.True (sv.ShowVerticalScrollIndicator);
        sv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"
         ▲
         ┬
         │
         │
         │
         │
         │
         ┴
         ▼
◄├─────┤► 
",
                                             output
                                            );

        sv.ShowHorizontalScrollIndicator = false;
        Assert.Equal (new (0, 0, 10, 10), sv.Viewport);
        sv.ShowVerticalScrollIndicator = true;
        Assert.Equal (new (0, 0, 10, 10), sv.Viewport);

        Assert.False (sv.AutoHideScrollBars);
        Assert.False (sv.ShowHorizontalScrollIndicator);
        Assert.True (sv.ShowVerticalScrollIndicator);
        sv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"
         ▲
         ┬
         │
         │
         │
         │
         │
         │
         ┴
         ▼
",
                                             output
                                            );

        sv.ShowHorizontalScrollIndicator = true;
        sv.ShowVerticalScrollIndicator = false;

        Assert.False (sv.AutoHideScrollBars);
        Assert.True (sv.ShowHorizontalScrollIndicator);
        Assert.False (sv.ShowVerticalScrollIndicator);
        sv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"
         
         
         
         
         
         
         
         
         
◄├──────┤► 
",
                                             output
                                            );

        sv.ShowHorizontalScrollIndicator = false;
        sv.ShowVerticalScrollIndicator = false;

        Assert.False (sv.AutoHideScrollBars);
        Assert.False (sv.ShowHorizontalScrollIndicator);
        Assert.False (sv.ShowVerticalScrollIndicator);
        sv.Draw ();

        TestHelpers.AssertDriverContentsAre (
                                             @"
         
         
         
         
         
         
         
         
         
         
",
                                             output
                                            );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
    {
        var sv = new ScrollView { Width = 10, Height = 10 };

        var top = new Toplevel ();
        top.Add (sv);
        Application.Begin (top);

        Assert.True (sv.AutoHideScrollBars);
        Assert.False (sv.ShowHorizontalScrollIndicator);
        Assert.False (sv.ShowVerticalScrollIndicator);
        TestHelpers.AssertDriverContentsWithFrameAre ("", output);

        sv.AutoHideScrollBars = false;
        sv.ShowHorizontalScrollIndicator = true;
        sv.ShowVerticalScrollIndicator = true;
        sv.LayoutSubviews ();
        sv.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
         ▲
         ┬
         │
         │
         │
         │
         │
         ┴
         ▼
◄├─────┤► 
",
                                                      output
                                                     );
        top.Dispose ();
    }

    // There are still issue with the lower right corner of the scroll view
    [Fact]
    [AutoInitShutdown (configLocation: ConfigurationManager.ConfigLocations.DefaultOnly)]
    public void Clear_Window_Inside_ScrollView ()
    {
        var topLabel = new Label { X = 15, Text = "At 15,0" };

        var sv = new ScrollView
        {
            X = 3,
            Y = 3,
            Width = 10,
            Height = 10,
            KeepContentAlwaysInViewport = false
        };
        sv.SetContentSize (new (23, 23));
        var bottomLabel = new Label { X = 15, Y = 15, Text = "At 15,15" };
        var top = new Toplevel ();
        top.Add (topLabel, sv, bottomLabel);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
               At 15,0 
                       
                       
            ▲          
            ┬          
            ┴          
            ░          
            ░          
            ░          
            ░          
            ░          
            ▼          
   ◄├┤░░░░░►           
                       
                       
               At 15,15",
                                                      output
                                                     );

        Attribute [] attributes =
        {
            Colors.ColorSchemes ["TopLevel"].Normal,
            Colors.ColorSchemes ["TopLevel"].Focus,
            Colors.ColorSchemes ["Base"].Normal
        };

        TestHelpers.AssertDriverAttributesAre (
                                               @"
00000000000000000000000
00000000000000000000000
00000000000000000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00011111111110000000000
00000000000000000000000
00000000000000000000000
00000000000000000000000",
                                               null,
                                               attributes
                                              );

        sv.Add (new Window { X = 3, Y = 3, Width = 20, Height = 20 });

        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
               At 15,0 
                       
                       
            ▲          
            ┬          
            ┴          
      ┌─────░          
      │     ░          
      │     ░          
      │     ░          
      │     ░          
      │     ▼          
   ◄├┤░░░░░►           
                       
                       
               At 15,15",
                                                      output
                                                     );

        TestHelpers.AssertDriverAttributesAre (
                                               @"
00000000000000000000000
00000000000000000000000
00000000000000000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000022222210000000000
00000022222210000000000
00000022222210000000000
00000022222210000000000
00000022222210000000000
00000022222210000000000
00011111111110000000000
00000000000000000000000
00000000000000000000000
00000000000000000000000",
                                               null,
                                               attributes
                                              );

        sv.ContentOffset = new (20, 20);
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
               At 15,0 
                       
                       
     │      ▲          
     │      ░          
   ──┘      ░          
            ░          
            ░          
            ┬          
            │          
            ┴          
            ▼          
   ◄░░░░├─┤►           
                       
                       
               At 15,15",
                                                      output
                                                     );

        TestHelpers.AssertDriverAttributesAre (
                                               @"
00000000000000000000000
00000000000000000000000
00000000000000000000000
00022200000010000000000
00022200000010000000000
00022200000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00000000000010000000000
00011111111110000000000
00000000000000000000000
00000000000000000000000
00000000000000000000000",
                                               null,
                                               attributes
                                              );
        top.Dispose ();
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var sv = new ScrollView ();
        Assert.True (sv.CanFocus);
        Assert.Equal (new (0, 0, 0, 0), sv.Frame);
        Assert.Equal (Rectangle.Empty, sv.Frame);
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.Equal (Size.Empty, sv.GetContentSize ());
        Assert.True (sv.AutoHideScrollBars);
        Assert.True (sv.KeepContentAlwaysInViewport);

        sv = new () { X = 1, Y = 2, Width = 20, Height = 10 };
        Assert.True (sv.CanFocus);
        Assert.Equal (new (1, 2, 20, 10), sv.Frame);
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.Equal (sv.Viewport.Size, sv.GetContentSize ());
        Assert.True (sv.AutoHideScrollBars);
        Assert.True (sv.KeepContentAlwaysInViewport);
    }

    [Fact]
    [SetupFakeDriver]
    public void ContentBottomRightCorner_Draw ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 30);

        var top = new View { Width = 30, Height = 30, ColorScheme = new () { Normal = Attribute.Default } };

        Size size = new (20, 10);

        var sv = new ScrollView
        {
            X = 1,
            Y = 1,
            Width = 10,
            Height = 5,
            ColorScheme = new () { Normal = new (Color.Red, Color.Green) }
        };
        sv.SetContentSize (size);
        string text = null;

        for (var i = 0; i < size.Height; i++)
        {
            text += "*".Repeat (size.Width);

            if (i < size.Height)
            {
                text += '\n';
            }
        }

        var view = new View
        {
            ColorScheme = new () { Normal = new (Color.Blue, Color.Yellow) },
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text),
            Text = text
        };
        sv.Add (view);

        top.Add (sv);
        top.BeginInit ();
        top.EndInit ();

        top.LayoutSubviews ();

        top.Draw ();

        View contentBottomRightCorner = sv.Subviews.First (v => v is ScrollBarView.ContentBottomRightCorner);
        Assert.True (contentBottomRightCorner is ScrollBarView.ContentBottomRightCorner);
        Assert.True (contentBottomRightCorner.Visible);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
 *********▲
 *********┬
 *********┴
 *********▼
 ◄├──┤░░░► ",
                                                      output
                                                     );

        Attribute [] attrs = { Attribute.Default, new (Color.Red, Color.Green), new (Color.Blue, Color.Yellow) };

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000000000
022222222210
022222222210
022222222210
022222222210
011111111110
000000000000",
                                               null,
                                               attrs
                                              );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void
        ContentOffset_ContentSize_AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
    {
        var sv = new ScrollView
        {
            Width = 10, Height = 10
        };
        sv.SetContentSize (new (50, 50));
        sv.ContentOffset = new (25, 25);

        var top = new Toplevel ();
        top.Add (sv);
        Application.Begin (top);

        Assert.Equal (new (-25, -25), sv.ContentOffset);
        Assert.Equal (new (50, 50), sv.GetContentSize ());
        Assert.True (sv.AutoHideScrollBars);
        Assert.True (sv.ShowHorizontalScrollIndicator);
        Assert.True (sv.ShowVerticalScrollIndicator);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
         ▲
         ░
         ░
         ░
         ┬
         │
         ┴
         ░
         ▼
◄░░░├─┤░► 
",
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void ContentSize_AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
    {
        var sv = new ScrollView { Width = 10, Height = 10 };
        sv.SetContentSize (new (50, 50));

        var top = new Toplevel ();
        top.Add (sv);
        Application.Begin (top);

        Assert.Equal (50, sv.GetContentSize ().Width);
        Assert.Equal (50, sv.GetContentSize ().Height);
        Assert.True (sv.AutoHideScrollBars);
        Assert.True (sv.ShowHorizontalScrollIndicator);
        Assert.True (sv.ShowVerticalScrollIndicator);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
         ▲
         ┬
         ┴
         ░
         ░
         ░
         ░
         ░
         ▼
◄├┤░░░░░► 
",
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void DrawTextFormatter_Respects_The_Clip_Bounds ()
    {
        var rule = "0123456789";
        Size size = new (40, 40);
        var view = new View { Frame = new (Point.Empty, size) };

        view.Add (
                  new Label
                  {
                      Width = Dim.Fill (),
                      Height = 1,
                      Text = rule.Repeat (size.Width / rule.Length)
                  }
                 );

        view.Add (
                  new Label
                  {
                      Height = Dim.Fill (),
                      Width = 1,
                      Text = rule.Repeat (size.Height / rule.Length),
                      TextDirection = TextDirection.TopBottom_LeftRight
                  }
                 );
        view.Add (new Label { X = 1, Y = 1, Text = "[ Press me! ]" });

        var scrollView = new ScrollView
        {
            X = 1,
            Y = 1,
            Width = 15,
            Height = 10,
            ShowHorizontalScrollIndicator = true,
            ShowVerticalScrollIndicator = true
        };
        scrollView.SetContentSize (size);
        scrollView.Add (view);
        var win = new Window { X = 1, Y = 1, Width = 20, Height = 14 };
        win.Add (scrollView);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);

        var expected = @"
 ┌──────────────────┐
 │                  │
 │ 01234567890123▲  │
 │ 1[ Press me! ]┬  │
 │ 2             │  │
 │ 3             ┴  │
 │ 4             ░  │
 │ 5             ░  │
 │ 6             ░  │
 │ 7             ░  │
 │ 8             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
"
            ;

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 12345678901234▲  │
 │ [ Press me! ] ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
"
            ;

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 23456789012345▲  │
 │  Press me! ]  ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄├────┤░░░░░░►   │
 │                  │
 └──────────────────┘
"
            ;

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 34567890123456▲  │
 │ Press me! ]   ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄├────┤░░░░░░►   │
 │                  │
 └──────────────────┘
"
            ;

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 45678901234567▲  │
 │ ress me! ]    ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░├───┤░░░░░░►   │
 │                  │
 └──────────────────┘
"
            ;

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 56789012345678▲  │
 │ ess me! ]     ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░├────┤░░░░░►   │
 │                  │
 └──────────────────┘
"
            ;

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 67890123456789▲  │
 │ ss me! ]      ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░├────┤░░░░░►   │
 │                  │
 └──────────────────┘
"
            ;

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorRight));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 78901234567890▲  │
 │ s me! ]       ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░░├───┤░░░░░►   │
 │                  │
 └──────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.End.WithCtrl));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 67890123456789▲  │
 │               ┬  │
 │               │  │
 │               ┴  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ░  │
 │               ▼  │
 │ ◄░░░░░░░├───┤►   │
 │                  │
 └──────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.Home.WithCtrl));
        Assert.True (scrollView.NewKeyDownEvent (Key.CursorDown));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 1[ Press me! ]▲  │
 │ 2             ┬  │
 │ 3             │  │
 │ 4             ┴  │
 │ 5             ░  │
 │ 6             ░  │
 │ 7             ░  │
 │ 8             ░  │
 │ 9             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorDown));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 2             ▲  │
 │ 3             ┬  │
 │ 4             │  │
 │ 5             ┴  │
 │ 6             ░  │
 │ 7             ░  │
 │ 8             ░  │
 │ 9             ░  │
 │ 0             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.CursorDown));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 3             ▲  │
 │ 4             ┬  │
 │ 5             │  │
 │ 6             ┴  │
 │ 7             ░  │
 │ 8             ░  │
 │ 9             ░  │
 │ 0             ░  │
 │ 1             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (1, 1, 21, 14), pos);

        Assert.True (scrollView.NewKeyDownEvent (Key.End));
        top.Draw ();

        expected = @"
 ┌──────────────────┐
 │                  │
 │ 1             ▲  │
 │ 2             ░  │
 │ 3             ░  │
 │ 4             ░  │
 │ 5             ░  │
 │ 6             ░  │
 │ 7             ┬  │
 │ 8             ┴  │
 │ 9             ▼  │
 │ ◄├───┤░░░░░░░►   │
 │                  │
 └──────────────────┘
";

        TestHelpers.AssertDriverContentsAre (expected, output);

        top.Dispose ();
    }

    // There still have an issue with lower right corner of the scroll view
    [Fact]
    [AutoInitShutdown]
    public void Frame_And_Labels_Does_Not_Overspill_ScrollView ()
    {
        var sv = new ScrollView
        {
            X = 3,
            Y = 3,
            Width = 10,
            Height = 10,
            TabStop = TabBehavior.TabStop
        };
        sv.SetContentSize (new (50, 50));

        for (var i = 0; i < 8; i++)
        {
            sv.Add (new CustomButton ("█", $"Button {i}", 20, 3) { Y = i * 3 });
        }

        var top = new Toplevel ();
        top.Add (sv);
        Application.Begin (top);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
   █████████▲
   ██████But┬
   █████████┴
   ┌────────░
   │     But░
   └────────░
   ┌────────░
   │     But░
   └────────▼
   ◄├┤░░░░░► ",
                                                      output
                                                     );

        sv.ContentOffset = new (5, 5);
        sv.LayoutSubviews ();
        Application.Refresh ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
   ─────────▲
   ─────────┬
    Button 2│
   ─────────┴
   ─────────░
    Button 3░
   ─────────░
   ─────────░
    Button 4▼
   ◄├─┤░░░░► ",
                                                      output
                                                     );
        top.Dispose ();
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        var sv = new ScrollView { Width = 20, Height = 10 };
        sv.SetContentSize (new (40, 20));

        sv.Add (
                new View { Width = 20, Height = 5 },
                new View { X = 22, Y = 7, Width = 10, Height = 5 }
               );

        sv.BeginInit ();
        sv.EndInit ();

        Assert.True (sv.KeepContentAlwaysInViewport);
        Assert.True (sv.AutoHideScrollBars);
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (new (0, -1), sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.PageUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.PageDown));
        Point point0xMinus10 = new (0, -10);
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.PageDown));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.V.WithAlt));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.V.WithCtrl));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (new (-1, -10), sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.PageUp.WithCtrl));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.PageDown.WithCtrl));
        Point pointMinus20xMinus10 = new (-20, -10);
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.Home));
        Point pointMinus20x0 = new (-20, 0);
        Assert.Equal (pointMinus20x0, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.Home));
        Assert.Equal (pointMinus20x0, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.End));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.End));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.Home.WithCtrl));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.Home.WithCtrl));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.Home));
        Assert.Equal (pointMinus20x0, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.Home.WithCtrl));
        Assert.Equal (Point.Empty, sv.ContentOffset);

        sv.KeepContentAlwaysInViewport = false;
        Assert.False (sv.KeepContentAlwaysInViewport);
        Assert.True (sv.AutoHideScrollBars);
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (new (0, -1), sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.CursorUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.PageUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.PageDown));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.PageDown));
        Point point0xMinus19 = new (0, -19);
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.PageDown));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.CursorDown));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.V.WithAlt));
        Assert.Equal (new (0, -9), sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.V.WithCtrl));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (new (-1, -19), sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.CursorLeft));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.PageUp.WithCtrl));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.PageDown.WithCtrl));
        Assert.Equal (new (-20, -19), sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.PageDown.WithCtrl));
        Point pointMinus39xMinus19 = new (-39, -19);
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.PageDown.WithCtrl));
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.CursorRight));
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.PageUp.WithCtrl));
        var pointMinus19xMinus19 = new Point (-19, -19);
        Assert.Equal (pointMinus19xMinus19, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.Home));
        Assert.Equal (new (-19, 0), sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.Home));
        Assert.Equal (new (-19, 0), sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.End));
        Assert.Equal (pointMinus19xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.End));
        Assert.Equal (pointMinus19xMinus19, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.Home.WithCtrl));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.Home.WithCtrl));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.True (sv.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
        Assert.False (sv.NewKeyDownEvent (Key.End.WithCtrl));
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
    }

    [Fact]
    [AutoInitShutdown]
    public void Remove_Added_View_Is_Allowed ()
    {
        var sv = new ScrollView { Width = 20, Height = 20 };
        sv.SetContentSize (new (100, 100));

        sv.Add (
                new View { Width = Dim.Fill (), Height = Dim.Fill (50), Id = "View1" },
                new View { Y = 51, Width = Dim.Fill (), Height = Dim.Fill (), Id = "View2" }
               );

        var top = new Toplevel ();
        top.Add (sv);
        Application.Begin (top);

        Assert.Equal (4, sv.Subviews.Count);
        Assert.Equal (2, sv.Subviews [0].Subviews.Count);

        sv.Remove (sv.Subviews [0].Subviews [1]);
        Assert.Equal (4, sv.Subviews.Count);
        Assert.Single (sv.Subviews [0].Subviews);
        Assert.Equal ("View1", sv.Subviews [0].Subviews [0].Id);
        top.Dispose ();
    }

    private class CustomButton : FrameView
    {
        private readonly Label labelFill;
        private readonly Label labelText;

        public CustomButton (string fill, string text, int width, int height)
        {
            Width = width;
            Height = height;

            labelFill = new () { Width = Dim.Fill (), Height = Dim.Fill (), Visible = false };

            labelFill.LayoutComplete += (s, e) =>
                                        {
                                            var fillText = new StringBuilder ();

                                            for (var i = 0; i < labelFill.Viewport.Height; i++)
                                            {
                                                if (i > 0)
                                                {
                                                    fillText.AppendLine ("");
                                                }

                                                for (var j = 0; j < labelFill.Viewport.Width; j++)
                                                {
                                                    fillText.Append (fill);
                                                }
                                            }

                                            labelFill.Text = fillText.ToString ();
                                        };

            labelText = new () { X = Pos.Center (), Y = Pos.Center (), Text = text };
            Add (labelFill, labelText);
            CanFocus = true;
        }

        protected override void OnHasFocusChanged (bool newHasFocus, [CanBeNull] View previousFocusedView, [CanBeNull] View focusedVew)
        {
            if (newHasFocus)
            {
                Border.LineStyle = LineStyle.None;
                Border.Thickness = new (0);
                labelFill.Visible = true;
            }
            else
            {
                Border.LineStyle = LineStyle.Single;
                Border.Thickness = new (1);
                labelFill.Visible = false;
            }
        }
    }
}
