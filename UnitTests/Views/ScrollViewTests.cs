using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ScrollViewTests
{
    private readonly ITestOutputHelper _output;
    public ScrollViewTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void Adding_Views ()
    {
        var sv = new ScrollView { Width = 20, Height = 10, ContentSize = new (30, 20) };

        sv.Add (
                new View { Width = 10, Height = 5 },
                new View { X = 12, Y = 7, Width = 10, Height = 5 }
               );

        Assert.Equal (new (30, 20), sv.ContentSize);
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

        Assert.Equal (new Rectangle (0, 0, 10, 10), sv.Bounds);

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
                                             _output
                                            );

        sv.ShowHorizontalScrollIndicator = false;
        Assert.Equal (new Rectangle (0, 0, 10, 10), sv.Bounds);
        sv.ShowVerticalScrollIndicator = true;
        Assert.Equal (new Rectangle (0, 0, 10, 10), sv.Bounds);

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
                                             _output
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
                                             _output
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
                                             _output
                                            );
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
        TestHelpers.AssertDriverContentsWithFrameAre ("", _output);

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
                                                      _output
                                                     );
    }

    // There are still issue with the lower right corner of the scroll view
    [Fact]
    [AutoInitShutdown]
    public void Clear_Window_Inside_ScrollView ()
    {
        var topLabel = new Label { X = 15, Text = "At 15,0" };

        var sv = new ScrollView
        {
            X = 3,
            Y = 3,
            Width = 10,
            Height = 10,
            ContentSize = new (23, 23),
            KeepContentAlwaysInViewport = false
        };
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
                                                      _output
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
                                                      _output
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

        sv.ContentOffset = new Point (20, 20);
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
                                                      _output
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
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var sv = new ScrollView ();
        Assert.Equal (LayoutStyle.Absolute, sv.LayoutStyle);
        Assert.True (sv.CanFocus);
        Assert.Equal (new Rectangle (0, 0, 0, 0), sv.Frame);
        Assert.Equal (Rectangle.Empty, sv.Frame);
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.Equal (Size.Empty, sv.ContentSize);
        Assert.True (sv.AutoHideScrollBars);
        Assert.True (sv.KeepContentAlwaysInViewport);

        sv = new ScrollView { X = 1, Y = 2, Width = 20, Height = 10 };
        Assert.Equal (LayoutStyle.Absolute, sv.LayoutStyle);
        Assert.True (sv.CanFocus);
        Assert.Equal (new Rectangle (1, 2, 20, 10), sv.Frame);
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.Equal (Size.Empty, sv.ContentSize);
        Assert.True (sv.AutoHideScrollBars);
        Assert.True (sv.KeepContentAlwaysInViewport);
    }

    [Fact]
    [SetupFakeDriver]
    public void ContentBottomRightCorner_Draw ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (30, 30);

        var top = new View { Width = 30, Height = 30, ColorScheme = new ColorScheme { Normal = Attribute.Default } };

        Size size = new (20, 10);

        var sv = new ScrollView
        {
            X = 1,
            Y = 1,
            Width = 10,
            Height = 5,
            ContentSize = size,
            ColorScheme = new ColorScheme { Normal = new Attribute (Color.Red, Color.Green) }
        };
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
            Width = size.Width,
            Height = size.Height,
            ColorScheme = new ColorScheme { Normal = new Attribute (Color.Blue, Color.Yellow) },
            AutoSize = true,
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
                                                      _output
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
    }

    [Fact]
    [AutoInitShutdown]
    public void
        ContentOffset_ContentSize_AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
    {
        var sv = new ScrollView
        {
            Width = 10, Height = 10, ContentSize = new (50, 50), ContentOffset = new (25, 25)
        };

        var top = new Toplevel ();
        top.Add (sv);
        Application.Begin (top);

        Assert.Equal(new(-25,-25),sv.ContentOffset);
        Assert.Equal(new(50,50),sv.ContentSize);
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
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ContentSize_AutoHideScrollBars_ShowHorizontalScrollIndicator_ShowVerticalScrollIndicator ()
    {
        var sv = new ScrollView { Width = 10, Height = 10, ContentSize = new (50, 50) };

        var top = new Toplevel ();
        top.Add (sv);
        Application.Begin (top);

        Assert.Equal (50, sv.ContentSize.Width);
        Assert.Equal (50, sv.ContentSize.Height);
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
                                                      _output
                                                     );
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
                      AutoSize = false, Width = Dim.Fill (), Height = 1, Text = rule.Repeat (size.Width / rule.Length)
                  }
                 );

        view.Add (
                  new Label
                  {
                      AutoSize = false,
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
            ContentSize = size,
            ShowHorizontalScrollIndicator = true,
            ShowVerticalScrollIndicator = true
        };
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

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorRight));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorRight));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorRight));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorRight));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorRight));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorRight));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorRight));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.End.WithCtrl));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.Home.WithCtrl));
        Assert.True (scrollView.OnKeyDown (Key.CursorDown));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorDown));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.CursorDown));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);

        Assert.True (scrollView.OnKeyDown (Key.End));
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (1, 1, 21, 14), pos);
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
            ContentSize = new (50, 50)
        };

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
                                                      _output
                                                     );

        sv.ContentOffset = new Point (5, 5);
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
                                                      _output
                                                     );
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        var sv = new ScrollView { Width = 20, Height = 10, ContentSize = new (40, 20) };

        sv.Add (
                new View { Width = 20, Height = 5 },
                new View { X = 22, Y = 7, Width = 10, Height = 5 }
               );

        sv.BeginInit ();
        sv.EndInit ();

        Assert.True (sv.KeepContentAlwaysInViewport);
        Assert.True (sv.AutoHideScrollBars);
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.CursorUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.CursorDown));
        Assert.Equal (new (0, -1), sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.CursorUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.PageUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.PageDown));
        Point point0xMinus10 = new (0, -10);
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.PageDown));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.CursorDown));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.V.WithAlt));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.V.WithCtrl));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.CursorLeft));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.CursorRight));
        Assert.Equal (new (-1, -10), sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.CursorLeft));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.PageUp.WithCtrl));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.PageDown.WithCtrl));
        Point pointMinus20xMinus10 = new (-20, -10);
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.CursorRight));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.Home));
        Point pointMinus20x0 = new (-20, 0);
        Assert.Equal (pointMinus20x0, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.Home));
        Assert.Equal (pointMinus20x0, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.End));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.End));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.Home.WithCtrl));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.Home.WithCtrl));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.End.WithCtrl));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.End.WithCtrl));
        Assert.Equal (pointMinus20xMinus10, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.Home));
        Assert.Equal (pointMinus20x0, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.Home.WithCtrl));
        Assert.Equal (Point.Empty, sv.ContentOffset);

        sv.KeepContentAlwaysInViewport = false;
        Assert.False (sv.KeepContentAlwaysInViewport);
        Assert.True (sv.AutoHideScrollBars);
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.CursorUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.CursorDown));
        Assert.Equal (new (0, -1), sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.CursorUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.PageUp));
        Assert.Equal (Point.Empty, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.PageDown));
        Assert.Equal (point0xMinus10, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.PageDown));
        Point point0xMinus19 = new (0, -19);
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.PageDown));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.CursorDown));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.V.WithAlt));
        Assert.Equal (new (0, -9), sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.V.WithCtrl));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.CursorLeft));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.CursorRight));
        Assert.Equal (new (-1, -19), sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.CursorLeft));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.PageUp.WithCtrl));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.PageDown.WithCtrl));
        Assert.Equal (new (-20, -19), sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.PageDown.WithCtrl));
        Point pointMinus39xMinus19 = new (-39, -19);
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.PageDown.WithCtrl));
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.CursorRight));
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.PageUp.WithCtrl));
        Point pointMinus19xMinus19 = new Point (-19, -19);
        Assert.Equal (pointMinus19xMinus19, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.Home));
        Assert.Equal (new (-19, 0), sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.Home));
        Assert.Equal (new (-19, 0), sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.End));
        Assert.Equal (pointMinus19xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.End));
        Assert.Equal (pointMinus19xMinus19, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.Home.WithCtrl));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.Home.WithCtrl));
        Assert.Equal (point0xMinus19, sv.ContentOffset);
        Assert.True (sv.OnKeyDown (Key.End.WithCtrl));
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
        Assert.False (sv.OnKeyDown (Key.End.WithCtrl));
        Assert.Equal (pointMinus39xMinus19, sv.ContentOffset);
    }

    [Fact]
    [AutoInitShutdown]
    public void Remove_Added_View_Is_Allowed ()
    {
        var sv = new ScrollView { Width = 20, Height = 20, ContentSize = new (100, 100) };

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
    }

    private class CustomButton : FrameView
    {
        private readonly Label labelFill;
        private readonly Label labelText;

        public CustomButton (string fill, string text, int width, int height)
        {
            Width = width;
            Height = height;

            //labelFill = new Label { AutoSize = false, X = Pos.Center (), Y = Pos.Center (), Width = Dim.Fill (), Height = Dim.Fill (), Visible = false };
            labelFill = new Label { AutoSize = false, Width = Dim.Fill (), Height = Dim.Fill (), Visible = false };

            labelFill.LayoutComplete += (s, e) =>
                                        {
                                            var fillText = new StringBuilder ();

                                            for (var i = 0; i < labelFill.Bounds.Height; i++)
                                            {
                                                if (i > 0)
                                                {
                                                    fillText.AppendLine ("");
                                                }

                                                for (var j = 0; j < labelFill.Bounds.Width; j++)
                                                {
                                                    fillText.Append (fill);
                                                }
                                            }

                                            labelFill.Text = fillText.ToString ();
                                        };

            labelText = new Label { X = Pos.Center (), Y = Pos.Center (), Text = text };
            Add (labelFill, labelText);
            CanFocus = true;
        }

        public override bool OnEnter (View view)
        {
            Border.LineStyle = LineStyle.None;
            Border.Thickness = new Thickness (0);
            labelFill.Visible = true;
            view = this;

            return base.OnEnter (view);
        }

        public override bool OnLeave (View view)
        {
            Border.LineStyle = LineStyle.Single;
            Border.Thickness = new Thickness (1);
            labelFill.Visible = false;

            if (view == null)
            {
                view = this;
            }

            return base.OnLeave (view);
        }
    }
}
