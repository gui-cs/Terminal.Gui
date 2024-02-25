using System.Reflection;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class ScrollBarViewTests
{
    private static HostView _hostView;
    private readonly ITestOutputHelper _output;
    private bool _added;
    private ScrollBarView _scrollBar;
    public ScrollBarViewTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void AutoHideScrollBars_Check ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        _hostView.Draw ();
        Assert.True (_scrollBar.ShowScrollIndicator);
        Assert.True (_scrollBar.Visible);
        Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
        Assert.Equal (1, _scrollBar.ContentArea.Width);

        Assert.Equal (
                      "Fill(1)",
                      _scrollBar.Height.ToString ()
                     );
        Assert.Equal (24, _scrollBar.ContentArea.Height);
        Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.True (_scrollBar.OtherScrollBarView.Visible);

        Assert.Equal (
                      "Fill(1)",
                      _scrollBar.OtherScrollBarView.Width.ToString ()
                     );
        Assert.Equal (79, _scrollBar.OtherScrollBarView.ContentArea.Width);
        Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
        Assert.Equal (1, _scrollBar.OtherScrollBarView.ContentArea.Height);

        _hostView.Lines = 10;
        _hostView.Draw ();
        Assert.False (_scrollBar.ShowScrollIndicator);
        Assert.False (_scrollBar.Visible);
        Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
        Assert.Equal (1, _scrollBar.ContentArea.Width);

        Assert.Equal (
                      "Fill(1)",
                      _scrollBar.Height.ToString ()
                     );
        Assert.Equal (24, _scrollBar.ContentArea.Height);
        Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.True (_scrollBar.OtherScrollBarView.Visible);

        Assert.Equal (
                      "Fill(0)",
                      _scrollBar.OtherScrollBarView.Width.ToString ()
                     );
        Assert.Equal (80, _scrollBar.OtherScrollBarView.ContentArea.Width);
        Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
        Assert.Equal (1, _scrollBar.OtherScrollBarView.ContentArea.Height);

        _hostView.Cols = 60;
        _hostView.Draw ();
        Assert.False (_scrollBar.ShowScrollIndicator);
        Assert.False (_scrollBar.Visible);
        Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
        Assert.Equal (1, _scrollBar.ContentArea.Width);

        Assert.Equal (
                      "Fill(1)",
                      _scrollBar.Height.ToString ()
                     );
        Assert.Equal (24, _scrollBar.ContentArea.Height);
        Assert.False (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.False (_scrollBar.OtherScrollBarView.Visible);

        Assert.Equal (
                      "Fill(0)",
                      _scrollBar.OtherScrollBarView.Width.ToString ()
                     );
        Assert.Equal (80, _scrollBar.OtherScrollBarView.ContentArea.Width);
        Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
        Assert.Equal (1, _scrollBar.OtherScrollBarView.ContentArea.Height);

        _hostView.Lines = 40;
        _hostView.Draw ();
        Assert.True (_scrollBar.ShowScrollIndicator);
        Assert.True (_scrollBar.Visible);
        Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
        Assert.Equal (1, _scrollBar.ContentArea.Width);

        Assert.Equal (
                      "Fill(0)",
                      _scrollBar.Height.ToString ()
                     );
        Assert.Equal (25, _scrollBar.ContentArea.Height);
        Assert.False (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.False (_scrollBar.OtherScrollBarView.Visible);

        Assert.Equal (
                      "Fill(0)",
                      _scrollBar.OtherScrollBarView.Width.ToString ()
                     );
        Assert.Equal (80, _scrollBar.OtherScrollBarView.ContentArea.Width);
        Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
        Assert.Equal (1, _scrollBar.OtherScrollBarView.ContentArea.Height);

        _hostView.Cols = 120;
        _hostView.Draw ();
        Assert.True (_scrollBar.ShowScrollIndicator);
        Assert.True (_scrollBar.Visible);
        Assert.Equal ("Absolute(1)", _scrollBar.Width.ToString ());
        Assert.Equal (1, _scrollBar.ContentArea.Width);

        Assert.Equal (
                      "Fill(1)",
                      _scrollBar.Height.ToString ()
                     );
        Assert.Equal (24, _scrollBar.ContentArea.Height);
        Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.True (_scrollBar.OtherScrollBarView.Visible);

        Assert.Equal (
                      "Fill(1)",
                      _scrollBar.OtherScrollBarView.Width.ToString ()
                     );
        Assert.Equal (79, _scrollBar.OtherScrollBarView.ContentArea.Width);
        Assert.Equal ("Absolute(1)", _scrollBar.OtherScrollBarView.Height.ToString ());
        Assert.Equal (1, _scrollBar.OtherScrollBarView.ContentArea.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void Both_Default_Draws_Correctly ()
    {
        var width = 3;
        var height = 40;

        var super = new Window { Id = "super", Width = Dim.Fill (), Height = Dim.Fill () };
        Application.Top.Add (super);

        var horiz = new ScrollBarView
        {
            Id = "horiz",
            Size = width * 2,

            ShowScrollIndicator = true,
            IsVertical = false
        };
        super.Add (horiz);

        var vert = new ScrollBarView
        {
            Id = "vert",
            Size = height * 2,

            ShowScrollIndicator = true,
            IsVertical = true,
            OtherScrollBarView = horiz
        };
        super.Add (vert);

        Application.Begin (Application.Top);
        ((FakeDriver)Application.Driver).SetBufferSize (width, height);

        var expected = @"
┌─┐
│▲│
│┬│
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│┴│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│▼│
└─┘";
        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void ChangedPosition_Negative_Value ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        _scrollBar.Position = -20;
        Assert.Equal (0, _scrollBar.Position);
        Assert.Equal (_scrollBar.Position, _hostView.Top);

        _scrollBar.OtherScrollBarView.Position = -50;
        Assert.Equal (0, _scrollBar.OtherScrollBarView.Position);
        Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void ChangedPosition_Scrolling ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        for (var i = 0; i < _scrollBar.Size; i++)
        {
            _scrollBar.Position += 1;
            Assert.Equal (_scrollBar.Position, _hostView.Top);
        }

        for (int i = _scrollBar.Size - 1; i >= 0; i--)
        {
            _scrollBar.Position -= 1;
            Assert.Equal (_scrollBar.Position, _hostView.Top);
        }

        for (var i = 0; i < _scrollBar.OtherScrollBarView.Size; i++)
        {
            _scrollBar.OtherScrollBarView.Position += i;
            Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
        }

        for (int i = _scrollBar.OtherScrollBarView.Size - 1; i >= 0; i--)
        {
            _scrollBar.OtherScrollBarView.Position -= 1;
            Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
        }
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void ChangedPosition_Update_The_Hosted_View ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        _scrollBar.Position = 2;
        Assert.Equal (_scrollBar.Position, _hostView.Top);

        _scrollBar.OtherScrollBarView.Position = 5;
        Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
    }

    [Fact]
    [AutoInitShutdown]
    public void ClearOnVisibleFalse_Gets_Sets ()
    {
        var text =
            "This is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test";
        var label = new Label { Text = text };
        Application.Top.Add (label);

        var sbv = new ScrollBarView { IsVertical = true, Size = 100, ClearOnVisibleFalse = false };
        label.Add (sbv);
        Application.Begin (Application.Top);

        Assert.True (sbv.Visible);
        Assert.True (sbv.ShowScrollIndicator);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes░
This is a tes▼
",
                                                      _output
                                                     );

        sbv.Visible = false;

        // Visible is controlled by the AutoHideScrollBars and if
        // ShowScrollIndicator is true the Visible is also set to true
        Assert.False (sbv.Visible);
        Application.Top.Draw ();

        Assert.True (sbv.Visible);
        Assert.True (sbv.ShowScrollIndicator);

        // So it's needs to set ShowScrollIndicator and AutoHideScrollBars to false
        // There is no need to set the Visible to set the visibility, use the
        // ShowScrollIndicator because the Visible is controlled by automatism
        sbv.Visible = false;
        sbv.ShowScrollIndicator = false;
        sbv.AutoHideScrollBars = false;
        Application.Top.Draw ();
        Assert.False (sbv.Visible);
        Assert.False (sbv.ShowScrollIndicator);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a test
This is a test
This is a test
This is a test
This is a test
This is a test
",
                                                      _output
                                                     );

        sbv.Visible = true;
        Assert.True (sbv.Visible);
        Application.Top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes░
This is a tes▼
",
                                                      _output
                                                     );

        sbv.ClearOnVisibleFalse = true;
        sbv.Visible = false;
        Assert.False (sbv.Visible);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a tes
This is a tes
This is a tes
This is a tes
This is a tes
This is a tes
",
                                                      _output
                                                     );
    }

    [Fact]
    public void
        Constructor_ShowBothScrollIndicator_False_And_IsVertical_False_Refresh_Does_Not_Throws_An_Object_Null_Exception ()
    {
        Exception exception = Record.Exception (
                                                () =>
                                                {
                                                    Application.Init (new FakeDriver ());

                                                    Toplevel top = Application.Top;

                                                    var win = new Window { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };

                                                    List<string> source = new ();

                                                    for (var i = 0; i < 50; i++)
                                                    {
                                                        var text = $"item {i} - ";

                                                        for (var j = 0; j < 160; j++)
                                                        {
                                                            var col = j.ToString ();
                                                            text += col.Length == 1 ? col [0] : col [1];
                                                        }

                                                        source.Add (text);
                                                    }

                                                    var listView = new ListView
                                                    {
                                                        X = 0,
                                                        Y = 0,
                                                        Width = Dim.Fill (),
                                                        Height = Dim.Fill (),
                                                        Source = new ListWrapper (source)
                                                    };
                                                    listView.Padding.ScrollBarType = ScrollBarType.Horizontal;
                                                    win.Add (listView);

                                                    Assert.True (listView.ScrollKeepContentAlwaysInViewPort);

                                                    var newScrollBarView = listView.Padding.Subviews [0] as ScrollBarView;

                                                    newScrollBarView!.ChangedPosition += (s, e) =>
                                                                                         {
                                                                                             listView.LeftItem = newScrollBarView.Position;

                                                                                             if (listView.LeftItem != newScrollBarView.Position)
                                                                                             {
                                                                                                 newScrollBarView.Position = listView.LeftItem;
                                                                                             }

                                                                                             Assert.Equal (newScrollBarView.Position, listView.LeftItem);
                                                                                             listView.SetNeedsDisplay ();
                                                                                         };

                                                    listView.DrawContent += (s, e) =>
                                                                            {
                                                                                newScrollBarView.Size = listView.MaxLength;
                                                                                Assert.Equal (newScrollBarView.Size, listView.MaxLength);
                                                                                newScrollBarView.Position = listView.LeftItem;
                                                                                Assert.Equal (newScrollBarView.Position, listView.LeftItem);
                                                                                newScrollBarView.Refresh ();
                                                                            };

                                                    top.Ready += (s, e) =>
                                                                 {
                                                                     newScrollBarView.Position = 100;

                                                                     Assert.Equal (
                                                                                   newScrollBarView.Position,
                                                                                   newScrollBarView.Size
                                                                                   - listView.LeftItem
                                                                                   + (listView.LeftItem - listView.ContentArea.Width));
                                                                     Assert.Equal (newScrollBarView.Position, listView.LeftItem);

                                                                     Assert.Equal (92, newScrollBarView.Position);
                                                                     Assert.Equal (92, listView.LeftItem);
                                                                     Application.RequestStop ();
                                                                 };

                                                    top.Add (win);

                                                    Application.Run ();

                                                    Application.Shutdown ();
                                                });

        Assert.Null (exception);
    }

    [Fact]
    public void
        Constructor_ShowBothScrollIndicator_False_And_IsVertical_True_Refresh_Does_Not_Throws_An_Object_Null_Exception ()
    {
        Exception exception = Record.Exception (
                                                () =>
                                                {
                                                    Application.Init (new FakeDriver ());
                                                    Toplevel top = Application.Top;
                                                    var win = new Window { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill () };
                                                    List<string> source = new ();

                                                    for (var i = 0; i < 50; i++)
                                                    {
                                                        source.Add ($"item {i}");
                                                    }

                                                    var listView = new ListView
                                                    {
                                                        X = 0,
                                                        Y = 0,
                                                        Width = Dim.Fill (),
                                                        Height = Dim.Fill (),
                                                        Source = new ListWrapper (source)
                                                    };
                                                    win.Add (listView);
                                                    var newScrollBarView = new ScrollBarView { IsVertical = true, KeepContentAlwaysInViewPort = true };
                                                    listView.Add (newScrollBarView);

                                                    newScrollBarView.ChangedPosition += (s, e) =>
                                                                                        {
                                                                                            listView.TopItem = newScrollBarView.Position;

                                                                                            if (listView.TopItem != newScrollBarView.Position)
                                                                                            {
                                                                                                newScrollBarView.Position = listView.TopItem;
                                                                                            }

                                                                                            Assert.Equal (newScrollBarView.Position, listView.TopItem);
                                                                                            listView.SetNeedsDisplay ();
                                                                                        };

                                                    listView.DrawContent += (s, e) =>
                                                                            {
                                                                                newScrollBarView.Size = listView.Source.Count;
                                                                                Assert.Equal (newScrollBarView.Size, listView.Source.Count);
                                                                                newScrollBarView.Position = listView.TopItem;
                                                                                Assert.Equal (newScrollBarView.Position, listView.TopItem);
                                                                                newScrollBarView.Refresh ();
                                                                            };

                                                    top.Ready += (s, e) =>
                                                                 {
                                                                     newScrollBarView.Position = 45;

                                                                     Assert.Equal (
                                                                                   newScrollBarView.Position,
                                                                                   newScrollBarView.Size
                                                                                   - listView.TopItem
                                                                                   + (listView.TopItem - listView.ContentArea.Height)
                                                                                  );
                                                                     Assert.Equal (newScrollBarView.Position, listView.TopItem);
                                                                     Assert.Equal (27, newScrollBarView.Position);
                                                                     Assert.Equal (27, listView.TopItem);
                                                                     Application.RequestStop ();
                                                                 };
                                                    top.Add (win);
                                                    Application.Run ();
                                                    Application.Shutdown ();
                                                }
                                               );

        Assert.Null (exception);
    }

    [Fact]
    [AutoInitShutdown]
    public void ContentBottomRightCorner_Not_Redraw_If_Both_Size_Equal_To_Zero ()
    {
        var text =
            "This is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test";
        var label = new Label { Text = text };
        var sbv = new ScrollBarView { X = Pos.AnchorEnd (1), IsVertical = true, Size = 100 };
        sbv.OtherScrollBarView = new ScrollBarView { Y = Pos.AnchorEnd (1), IsVertical = false, Size = 100, OtherScrollBarView = sbv };
        label.Add (sbv, sbv.OtherScrollBarView);
        Application.Top.Add (label);
        Application.Begin (Application.Top);

        Assert.Equal (100, sbv.Size);
        Assert.Equal (100, sbv.OtherScrollBarView.Size);
        Assert.True (sbv.ShowScrollIndicator);
        Assert.True (sbv.OtherScrollBarView.ShowScrollIndicator);
        Assert.True (sbv.Visible);
        Assert.True (sbv.OtherScrollBarView.Visible);

        View contentBottomRightCorner =
            label.Subviews.First (v => v is ScrollBarView.ContentBottomRightCorner);
        Assert.True (contentBottomRightCorner is ScrollBarView.ContentBottomRightCorner);
        Assert.True (contentBottomRightCorner.Visible);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes▼
◄├─┤░░░░░░░░► 
",
                                                      _output
                                                     );

        sbv.Size = 0;
        sbv.OtherScrollBarView.Size = 0;
        Assert.Equal (0, sbv.Size);
        Assert.Equal (0, sbv.OtherScrollBarView.Size);
        Assert.False (sbv.ShowScrollIndicator);
        Assert.False (sbv.OtherScrollBarView.ShowScrollIndicator);
        Assert.False (sbv.Visible);
        Assert.False (sbv.OtherScrollBarView.Visible);
        Application.Top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a test
This is a test
This is a test
This is a test
This is a test
This is a test
",
                                                      _output
                                                     );

        sbv.Size = 50;
        sbv.OtherScrollBarView.Size = 50;
        Assert.Equal (50, sbv.Size);
        Assert.Equal (50, sbv.OtherScrollBarView.Size);
        Assert.True (sbv.ShowScrollIndicator);
        Assert.True (sbv.OtherScrollBarView.ShowScrollIndicator);
        Assert.True (sbv.Visible);
        Assert.True (sbv.OtherScrollBarView.Visible);
        Application.Top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes▼
◄├──┤░░░░░░░► 
",
                                                      _output
                                                     );
    }

    [Fact]
    [AutoInitShutdown]
    public void ContentBottomRightCorner_Not_Redraw_If_One_Size_Equal_To_Zero ()
    {
        var text =
            "This is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test";
        var label = new Label { Text = text };

        var sbv = new ScrollBarView { X = Pos.AnchorEnd (1), IsVertical = true, Size = 100 };
        label.Add (sbv);
        Application.Top.Add (label);
        Application.Begin (Application.Top);

        Assert.Equal (100, sbv.Size);
        Assert.Null (sbv.OtherScrollBarView);
        Assert.True (sbv.ShowScrollIndicator);
        Assert.True (sbv.Visible);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a tes▲
This is a tes┬
This is a tes┴
This is a tes░
This is a tes░
This is a tes▼
",
                                                      _output
                                                     );

        sbv.Size = 0;
        Assert.Equal (0, sbv.Size);
        Assert.False (sbv.ShowScrollIndicator);
        Assert.False (sbv.Visible);
        Application.Top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
This is a test
This is a test
This is a test
This is a test
This is a test
This is a test
",
                                                      _output
                                                     );
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void DrawContent_Update_The_ScrollBarView_Position ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        _hostView.Top = 3;
        _hostView.Draw ();
        Assert.Equal (_scrollBar.Position, _hostView.Top);

        _hostView.Left = 6;
        _hostView.Draw ();
        Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
    }

    [Fact]
    [AutoInitShutdown]
    public void Horizontal_Default_Draws_Correctly ()
    {
        var width = 40;
        var height = 3;

        var super = new Window { Id = "super", Width = Dim.Fill (), Height = Dim.Fill () };
        Application.Top.Add (super);

        var sbv = new ScrollBarView { Id = "sbv", Size = width * 2, ShowScrollIndicator = true };
        super.Add (sbv);
        Application.Begin (Application.Top);
        ((FakeDriver)Application.Driver).SetBufferSize (width, height);

        var expected = @"
┌──────────────────────────────────────┐
│◄├────────────────┤░░░░░░░░░░░░░░░░░░►│
└──────────────────────────────────────┘";
        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void Hosting_A_View_To_A_ScrollBarView ()
    {
        RemoveHandlers ();

        _scrollBar = new ScrollBarView { IsVertical = true, OtherScrollBarView = new ScrollBarView { IsVertical = false } };
        _hostView.Add (_scrollBar);

        Application.Begin (Application.Top);

        Assert.True (_scrollBar.IsVertical);
        Assert.False (_scrollBar.OtherScrollBarView.IsVertical);

        Assert.Equal (_scrollBar.Position, _hostView.Top);
        Assert.NotEqual (_scrollBar.Size, _hostView.Lines);
        Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
        Assert.NotEqual (_scrollBar.OtherScrollBarView.Size, _hostView.Cols);

        AddHandlers ();
        _hostView.SuperView.LayoutSubviews ();
        _hostView.Draw ();

        Assert.Equal (_scrollBar.Position, _hostView.Top);
        Assert.Equal (_scrollBar.Size, _hostView.Lines);
        Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
        Assert.Equal (_scrollBar.OtherScrollBarView.Size, _hostView.Cols);
    }

    [Fact]
    [AutoInitShutdown]
    public void Hosting_ShowBothScrollIndicator_Invisible_WordWrap_ReadOnly ()
    {
        var textView = new TextView
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text =
                "This is the help text for the Second Step.\n\nPress the button to see a message box.\n\nEnter name too."
        };
        textView.Padding.ScrollBarType = ScrollBarType.Both;
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (textView);

        Application.Top.Add (win);

        Application.Begin (Application.Top);
        ((FakeDriver)Application.Driver).SetBufferSize (45, 20);

        var scrollBar = textView.Padding.Subviews [0] as ScrollBarView;
        Assert.True (scrollBar.AutoHideScrollBars);
        Assert.False (scrollBar.ShowScrollIndicator);
        Assert.False (scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.Equal (5, textView.Lines);

        // The length is one more for the cursor on the last column of the line
        Assert.Equal (43, textView.Maxlength);
        Assert.Equal (0, textView.LeftColumn);
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (0, scrollBar.OtherScrollBarView.Position);

        var expected = @"
┌───────────────────────────────────────────┐
│This is the help text for the Second Step. │
│                                           │
│Press the button to see a message box.     │
│                                           │
│Enter name too.                            │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
│                                           │
└───────────────────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 45, 20), pos);

        textView.WordWrap = true;
        ((FakeDriver)Application.Driver).SetBufferSize (26, 20);
        Application.Refresh ();

        Assert.True (textView.WordWrap);
        Assert.True (scrollBar.AutoHideScrollBars);
        Assert.Equal (7, textView.Lines);

        // The length is one more for the cursor on the last column of a line
        Assert.Equal (23, textView.Maxlength);
        Assert.Equal (0, textView.LeftColumn);
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (0, scrollBar.OtherScrollBarView.Position);

        expected = @"
┌────────────────────────┐
│This is the help text   │
│for the Second Step.    │
│                        │
│Press the button to     │
│see a message box.      │
│                        │
│Enter name too.         │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
│                        │
└────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 26, 20), pos);

        ((FakeDriver)Application.Driver).SetBufferSize (10, 11);
        Application.Refresh ();

        Assert.True (textView.WordWrap);
        Assert.True (scrollBar.AutoHideScrollBars);
        Assert.Equal (21, textView.Lines);

        // The length is one more for the cursor on the last column of a line
        Assert.Equal (7, textView.Maxlength);
        Assert.Equal (0, textView.LeftColumn);
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (0, scrollBar.OtherScrollBarView.Position);
        Assert.True (scrollBar.ShowScrollIndicator);

        expected = @"
┌────────┐
│This   ▲│
│is     ┬│
│the    ││
│help   ││
│text   ┴│
│for    ░│
│the    ░│
│Second ░│
│ Step. ▼│
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 10, 11), pos);

        Assert.False (textView.ReadOnly);
        textView.ReadOnly = true;
        Application.Refresh ();
        Assert.True (textView.WordWrap);
        Assert.True (scrollBar.AutoHideScrollBars);
        Assert.Equal (20, textView.Lines);

        // The length is one more for the cursor on the last column of a line
        Assert.Equal (7, textView.Maxlength);
        Assert.Equal (0, textView.LeftColumn);
        Assert.Equal (0, scrollBar.Position);
        Assert.Equal (0, scrollBar.OtherScrollBarView.Position);
        Assert.True (scrollBar.ShowScrollIndicator);

        expected = @"
┌────────┐
│This   ▲│
│is the ┬│
│help   ││
│text   ││
│for    ┴│
│the    ░│
│Second ░│
│Step.  ░│
│       ▼│
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 10, 11), pos);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void Hosting_Two_Horizontal_ScrollBarView_Throws_ArgumentException ()
    {
        var top = new Toplevel ();
        var host = new View ();
        top.Add (host);
        var v = new ScrollBarView { IsVertical = false };
        var h = new ScrollBarView { IsVertical = false };

        Assert.Throws<ArgumentException> (() => v.OtherScrollBarView = h);
        Assert.Throws<ArgumentException> (() => h.OtherScrollBarView = v);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void Hosting_Two_Vertical_ScrollBarView_Throws_ArgumentException ()
    {
        var top = new Toplevel ();
        var host = new View ();
        top.Add (host);
        var v = new ScrollBarView { IsVertical = true };
        var h = new ScrollBarView { IsVertical = true };

        Assert.Throws<ArgumentException> (() => v.OtherScrollBarView = h);
        Assert.Throws<ArgumentException> (() => h.OtherScrollBarView = v);
    }

    [Fact]
    [AutoInitShutdown]
    public void Internal_Tests ()
    {
        Toplevel top = Application.Top;
        Assert.Equal (new Rectangle (0, 0, 80, 25), top.ContentArea);
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
        var sbv = new ScrollBarView { IsVertical = true, OtherScrollBarView = new ScrollBarView { IsVertical = false } };
        view.Add (sbv);
        top.Add (view);
        Assert.Equal (view, sbv.SuperView);
        sbv.Size = 40;
        sbv.Position = 0;
        sbv.OtherScrollBarView.Size = 100;
        sbv.OtherScrollBarView.Position = 0;

        // Host bounds is not empty.
        Assert.True (sbv.CanScroll (10, out int max, sbv.IsVertical));
        Assert.Equal (10, max);
        Assert.True (sbv.OtherScrollBarView.CanScroll (10, out max, sbv.OtherScrollBarView.IsVertical));
        Assert.Equal (10, max);

        Application.Begin (top);

        // They are visible so they are drawn.
        Assert.True (sbv.Visible);
        Assert.True (sbv.OtherScrollBarView.Visible);
        top.LayoutSubviews ();

        // Now the host bounds is not empty.
        Assert.True (sbv.CanScroll (10, out max, sbv.IsVertical));
        Assert.Equal (10, max);
        Assert.True (sbv.OtherScrollBarView.CanScroll (10, out max, sbv.OtherScrollBarView.IsVertical));
        Assert.Equal (10, max);
        Assert.True (sbv.CanScroll (50, out max, sbv.IsVertical));
        Assert.Equal (40, sbv.Size);
        Assert.Equal (16, max); // 16+25=41
        Assert.True (sbv.OtherScrollBarView.CanScroll (150, out max, sbv.OtherScrollBarView.IsVertical));
        Assert.Equal (100, sbv.OtherScrollBarView.Size);
        Assert.Equal (21, max); // 21+80=101
        Assert.True (sbv.Visible);
        Assert.True (sbv.OtherScrollBarView.Visible);
        sbv.KeepContentAlwaysInViewPort = false;
        sbv.OtherScrollBarView.KeepContentAlwaysInViewPort = false;
        Assert.True (sbv.CanScroll (50, out max, sbv.IsVertical));
        Assert.Equal (39, max); // Keep 1 row visible
        Assert.True (sbv.OtherScrollBarView.CanScroll (150, out max, sbv.OtherScrollBarView.IsVertical));
        Assert.Equal (99, max); // Keep 1 column visible
        Assert.True (sbv.Visible);
        Assert.True (sbv.OtherScrollBarView.Visible);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void KeepContentAlwaysInViewport_False ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        _scrollBar.KeepContentAlwaysInViewPort = false;
        _scrollBar.Position = 50;
        Assert.Equal (_scrollBar.Position, _scrollBar.Size - 1);
        Assert.Equal (_scrollBar.Position, _hostView.Top);
        Assert.Equal (29, _scrollBar.Position);
        Assert.Equal (29, _hostView.Top);

        _scrollBar.OtherScrollBarView.Position = 150;
        Assert.Equal (_scrollBar.OtherScrollBarView.Position, _scrollBar.OtherScrollBarView.Size - 1);
        Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
        Assert.Equal (99, _scrollBar.OtherScrollBarView.Position);
        Assert.Equal (99, _hostView.Left);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void KeepContentAlwaysInViewport_True ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        Assert.Equal (80, _hostView.ContentArea.Width);
        Assert.Equal (25, _hostView.ContentArea.Height);
        Assert.Equal (79, _scrollBar.OtherScrollBarView.ContentArea.Width);
        Assert.Equal (24, _scrollBar.ContentArea.Height);
        Assert.Equal (30, _scrollBar.Size);
        Assert.Equal (100, _scrollBar.OtherScrollBarView.Size);
        Assert.True (_scrollBar.ShowScrollIndicator);
        Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.True (_scrollBar.Visible);
        Assert.True (_scrollBar.OtherScrollBarView.Visible);

        _scrollBar.Position = 50;
        Assert.Equal (_scrollBar.Position, _scrollBar.Size - _scrollBar.ContentArea.Height);
        Assert.Equal (_scrollBar.Position, _hostView.Top);
        Assert.Equal (6, _scrollBar.Position);
        Assert.Equal (6, _hostView.Top);
        Assert.True (_scrollBar.ShowScrollIndicator);
        Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.True (_scrollBar.Visible);
        Assert.True (_scrollBar.OtherScrollBarView.Visible);

        _scrollBar.OtherScrollBarView.Position = 150;

        Assert.Equal (
                      _scrollBar.OtherScrollBarView.Position,
                      _scrollBar.OtherScrollBarView.Size - _scrollBar.OtherScrollBarView.ContentArea.Width
                     );
        Assert.Equal (_scrollBar.OtherScrollBarView.Position, _hostView.Left);
        Assert.Equal (21, _scrollBar.OtherScrollBarView.Position);
        Assert.Equal (21, _hostView.Left);
        Assert.True (_scrollBar.ShowScrollIndicator);
        Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
        Assert.True (_scrollBar.Visible);
        Assert.True (_scrollBar.OtherScrollBarView.Visible);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void OtherScrollBarView_Not_Null ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        Assert.NotNull (_scrollBar.OtherScrollBarView);
        Assert.NotEqual (_scrollBar, _scrollBar.OtherScrollBarView);
        Assert.Equal (_scrollBar.OtherScrollBarView.OtherScrollBarView, _scrollBar);
    }

    [Fact]
    [SetupFakeDriver]
    public void ScrollBar_Ungrab_Mouse_If_The_Mouse_Is_On_Another_View ()
    {
        var top = new Toplevel { Id = "top", Width = 10, Height = 10 };
        var viewLeft = new View { Id = "left", Width = 5, Height = 5, ScrollBarType = ScrollBarType.Vertical, ContentSize = new Size (0, 20), CanFocus = true };

        var viewRight = new View
        {
            Id = "right", X = Pos.Right (viewLeft), Width = 5, Height = 6, ScrollBarType = ScrollBarType.Vertical, ContentSize = new Size (0, 20),
            CanFocus = true
        };
        top.Add (viewLeft, viewRight);
        Application.Begin (top);

        Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent { X = 1, Y = 0, Flags = MouseFlags.WheeledDown }));

        View firstGrabbed = Application.MouseGrabView;
        Assert.IsType<ScrollBarView> (firstGrabbed);
        Assert.Equal (new Rectangle (4, 0, 1, 5), firstGrabbed.Frame);

        Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent { X = 7, Y = 0, Flags = MouseFlags.WheeledDown }));

        View secondGrabbed = Application.MouseGrabView;
        Assert.IsType<ScrollBarView> (firstGrabbed);
        Assert.Equal (new Rectangle (4, 0, 1, 6), secondGrabbed.Frame);
        Assert.NotEqual (firstGrabbed, secondGrabbed);
    }

    [Fact]
    public void ScrollBarType_IsBuiltIn_In_Padding ()
    {
        var view = new View { ScrollBarType = ScrollBarType.None };
        Assert.Empty (view.Padding.Subviews);

        view = new View ();
        view.Padding.ScrollBarType = ScrollBarType.Both;
        Assert.Equal (3, view.Padding.Subviews.Count);

        foreach (View sbv in view.Padding.Subviews)
        {
            if (sbv is not ScrollBarView)
            {
                Assert.True (sbv is ScrollBarView.ContentBottomRightCorner);
            }
            else
            {
                Assert.True (sbv is ScrollBarView);
            }
        }

        view = new View ();
        view.Padding.ScrollBarType = ScrollBarType.Vertical;
        Assert.Single (view.Padding.Subviews);
        Assert.True (view.Padding.Subviews [0] is ScrollBarView);

        view = new View ();
        view.Padding.ScrollBarType = ScrollBarType.Horizontal;
        Assert.Single (view.Padding.Subviews);
        Assert.True (view.Padding.Subviews [0] is ScrollBarView);
    }

    [Fact]
    public void ScrollBarType_IsBuiltIn_In_Padding_Does_Not_Throws_If_ScrollBarType_None ()
    {
        Exception exception = Record.Exception (() => () => new View { ScrollBarType = ScrollBarType.None });
        Assert.Null (exception);
    }

    [Fact]
    [SetupFakeDriver]
    public void ScrollBarType_IsBuiltIn_UseNegativeBoundsLocation_In_Padding_Thickness ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (15, 11);

        var top = new View { Width = 15, Height = 11, ColorScheme = new ColorScheme (Attribute.Default) };

        var view = new View
        {
            X = Pos.Center (), Y = Pos.Center (), Width = 9, Height = 6,
            Text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line", CanFocus = true,
            UseContentOffset = true
        };
        view.Padding.ScrollBarType = ScrollBarType.Both;
        view.TextFormatter.WordWrap = false;
        view.TextFormatter.MultiLine = true;
        string [] strings = view.Text.Split ("\n").ToArray ();
        view.ContentSize = new Size (strings.OrderByDescending (s => s.Length).First ().GetColumns (), strings.Length);

        view.ColorScheme = new ColorScheme
        {
            Normal = new Attribute (Color.Green, Color.Red),
            Focus = new Attribute (Color.Red, Color.Green)
        };

        view.Padding.ColorScheme = new ColorScheme
        {
            Normal = new Attribute (Color.Black, Color.Gray),
            Focus = new Attribute (Color.White, Color.DarkGray)
        };
        var view2 = new View { X = Pos.Center (), Y = Pos.Bottom (view) + 1, Text = "Test", CanFocus = true, AutoSize = true };
        view2.ColorScheme = view.ColorScheme;
        top.Add (view, view2);
        top.BeginInit ();
        top.EndInit ();
        top.FocusFirst ();
        top.LayoutSubviews ();
        top.Draw ();
        Assert.True (view.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (12, view.ContentSize.Width);
        Assert.Equal (7, view.ContentSize.Height);
        Assert.Equal ("{X=0,Y=0,Width=8,Height=5}", view.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=5}", view.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=3,Y=2,Width=9,Height=6}", view.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=5}", view.Padding.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=5}", view.Padding.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Padding.Frame.ToString ());
        Assert.Equal ("(Left=0,Top=0,Right=1,Bottom=1)", view.Padding.Thickness.ToString ());
        Assert.Equal ("{Width=8, Height=5}", view.TextFormatter.Size.ToString ());

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
   First Li▲
   Second L┬
   Third Li│
   Fourth L┴
   Fifth Li▼
   ◄├───┤░► 
            
     Test   ",
                                                      _output);

        Attribute [] attrs =
        [
            Attribute.Default,
            new Attribute (Color.Red, Color.Green),
            new Attribute (Color.Green, Color.Red),
            new Attribute (Color.White, Color.DarkGray),
            new Attribute (Color.Black, Color.Gray)
        ];

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000000000000
000000000000000
000111111113000
000111111113000
000111111113000
000111111113000
000111111113000
000333333333000
000000000000000
000002222000000
000000000000000",
                                               null,
                                               attrs);

        Assert.True (view.Padding.OnInvokingKeyBindings (new Key (KeyCode.End)));
        Assert.True (view.Padding.OnInvokingKeyBindings (new Key (KeyCode.End | KeyCode.ShiftMask)));
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
   d Line  ▲
   th Line ┬
   h Line  │
   h Line  ┴
   nth Line▼
   ◄░░├──┤► 
            
     Test   ",
                                                      _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void ScrollBarType_IsBuiltIn_UseNegativeBoundsLocation_In_Padding_Thickness_Inside_Another_Container ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (15, 11);

        var superTop = new View { Width = 15, Height = 11, ColorScheme = new ColorScheme (Attribute.Default) };

        var view = new View
        {
            X = Pos.Center (), Y = Pos.Center (), Width = 9, Height = 6,
            Text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line", CanFocus = true,
            UseContentOffset = true
        };
        view.Padding.ScrollBarType = ScrollBarType.Both;
        view.TextFormatter.WordWrap = false;
        view.TextFormatter.MultiLine = true;
        string [] strings = view.Text.Split ("\n").ToArray ();
        view.ContentSize = new Size (strings.OrderByDescending (s => s.Length).First ().GetColumns (), strings.Length);

        view.ColorScheme = new ColorScheme
        {
            Normal = new Attribute (Color.Green, Color.Red),
            Focus = new Attribute (Color.Red, Color.Green)
        };

        view.Padding.ColorScheme = new ColorScheme
        {
            Normal = new Attribute (Color.Black, Color.Gray),
            Focus = new Attribute (Color.White, Color.DarkGray)
        };
        var view2 = new View { X = Pos.Center (), Y = Pos.Bottom (view) + 1, Text = "Test", CanFocus = true, AutoSize = true };
        view2.ColorScheme = view.ColorScheme;

        var top = new View
            { Width = Dim.Fill (), Height = Dim.Fill (), ColorScheme = new ColorScheme { Normal = Attribute.Default }, BorderStyle = LineStyle.Single };
        top.Add (view, view2);
        superTop.Add (top);
        superTop.BeginInit ();
        superTop.EndInit ();
        superTop.FocusFirst ();
        superTop.LayoutSubviews ();
        superTop.Draw ();
        Assert.True (view.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (12, view.ContentSize.Width);
        Assert.Equal (7, view.ContentSize.Height);
        Assert.Equal ("{X=0,Y=0,Width=8,Height=5}", view.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=5}", view.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=2,Y=1,Width=9,Height=6}", view.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=5}", view.Padding.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=8,Height=5}", view.Padding.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Padding.Frame.ToString ());
        Assert.Equal ("(Left=0,Top=0,Right=1,Bottom=1)", view.Padding.Thickness.ToString ());
        Assert.Equal ("{Width=8, Height=5}", view.TextFormatter.Size.ToString ());

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌─────────────┐
│             │
│  First Li▲  │
│  Second L┬  │
│  Third Li│  │
│  Fourth L┴  │
│  Fifth Li▼  │
│  ◄├───┤░►   │
│             │
│    Test     │
└─────────────┘",
                                                      _output);

        Attribute [] attrs =
        [
            Attribute.Default,
            new Attribute (Color.Red, Color.Green),
            new Attribute (Color.Green, Color.Red),
            new Attribute (Color.White, Color.DarkGray),
            new Attribute (Color.Black, Color.Gray)
        ];

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000000000000
000000000000000
000111111113000
000111111113000
000111111113000
000111111113000
000111111113000
000333333333000
000000000000000
000002222000000
000000000000000",
                                               null,
                                               attrs);

        Assert.True (view.Padding.OnInvokingKeyBindings (new Key (KeyCode.End)));
        Assert.True (view.Padding.OnInvokingKeyBindings (new Key (KeyCode.End | KeyCode.ShiftMask)));
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌─────────────┐
│             │
│  d Line  ▲  │
│  th Line ┬  │
│  h Line  │  │
│  h Line  ┴  │
│  nth Line▼  │
│  ◄░░├──┤►   │
│             │
│    Test     │
└─────────────┘",
                                                      _output);
    }

    [Fact]
    [SetupFakeDriver]
    public void ScrollBarType_IsBuiltIn_UseNegativeBoundsLocation_In_Parent_Inside_Another_Container ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (15, 11);

        var superTop = new View { Width = 15, Height = 11, ColorScheme = new ColorScheme (Attribute.Default) };

        var view = new View
        {
            X = Pos.Center (), Y = Pos.Center (), Width = 9, Height = 6,
            Text = "First Line\nSecond Line\nThird Line\nFourth Line\nFifth Line\nSixth Line\nSeventh Line", CanFocus = true,
            ScrollBarType = ScrollBarType.Both,
            UseContentOffset = true
        };
        view.TextFormatter.WordWrap = false;
        view.TextFormatter.MultiLine = true;
        string [] strings = view.Text.Split ("\n").ToArray ();
        view.ContentSize = new Size (strings.OrderByDescending (s => s.Length).First ().GetColumns (), strings.Length);

        view.ColorScheme = new ColorScheme
        {
            Normal = new Attribute (Color.Green, Color.Red),
            Focus = new Attribute (Color.Red, Color.Green)
        };

        var view2 = new View { X = Pos.Center (), Y = Pos.Bottom (view) + 1, Text = "Test", CanFocus = true, AutoSize = true };
        view2.ColorScheme = view.ColorScheme;

        var top = new View
            { Width = Dim.Fill (), Height = Dim.Fill (), ColorScheme = new ColorScheme { Normal = Attribute.Default }, BorderStyle = LineStyle.Single };
        top.Add (view, view2);
        superTop.Add (top);
        superTop.BeginInit ();
        superTop.EndInit ();
        superTop.FocusFirst ();
        superTop.LayoutSubviews ();
        superTop.Draw ();
        Assert.True (view.HasFocus);
        Assert.False (view2.HasFocus);
        Assert.Equal (12, view.ContentSize.Width);
        Assert.Equal (7, view.ContentSize.Height);
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=2,Y=1,Width=9,Height=6}", view.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Margin.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Border.Frame.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Padding.ContentArea.ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Padding.GetVisibleContentArea ().ToString ());
        Assert.Equal ("{X=0,Y=0,Width=9,Height=6}", view.Padding.Frame.ToString ());
        Assert.Equal ("(Left=0,Top=0,Right=0,Bottom=0)", view.Padding.Thickness.ToString ());
        Assert.Equal ("{Width=9, Height=6}", view.TextFormatter.Size.ToString ());

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌─────────────┐
│             │
│  First Li▲  │
│  Second L┬  │
│  Third Li│  │
│  Fourth L┴  │
│  Fifth Li▼  │
│  ◄├───┤░►   │
│             │
│    Test     │
└─────────────┘",
                                                      _output);

        Attribute [] attrs =
        [
            Attribute.Default,
            new Attribute (Color.Red, Color.Green),
            new Attribute (Color.Green, Color.Red)
        ];

        TestHelpers.AssertDriverAttributesAre (
                                               @"
000000000000000
000000000000000
000111111111000
000111111111000
000111111111000
000111111111000
000111111111000
000111111111000
000000000000000
000002222000000
000000000000000",
                                               null,
                                               attrs);

        Assert.True (view.OnInvokingKeyBindings (Key.End));
        Assert.True (view.OnInvokingKeyBindings (Key.End.WithShift));
        top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @"
┌─────────────┐
│             │
│  d Line  ▲  │
│  th Line ┬  │
│  h Line  │  │
│  h Line  ┴  │
│  nth Line▼  │
│  ◄░░├──┤►   │
│             │
│    Test     │
└─────────────┘",
                                                      _output);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void Scrolling_With_Default_Constructor_Do_Not_Scroll ()
    {
        var sbv = new ScrollBarView { Position = 1 };
        Assert.Equal (1, sbv.Position);
        Assert.NotEqual (0, sbv.Position);
    }

    [Fact]
    [ScrollBarAutoInitShutdown]
    public void ShowScrollIndicator_Check ()
    {
        Hosting_A_View_To_A_ScrollBarView ();

        AddHandlers ();

        Assert.True (_scrollBar.ShowScrollIndicator);
        Assert.True (_scrollBar.OtherScrollBarView.ShowScrollIndicator);
    }

    [Fact]
    [AutoInitShutdown]
    public void ShowScrollIndicator_False_Must_Also_Set_Visible_To_False_To_Not_Respond_To_Events ()
    {
        var clicked = false;
        var text = "This is a test\nThis is a test\nThis is a test\nThis is a test\nThis is a test";
        var label = new Label { AutoSize = false, Width = 14, Height = 5, Text = text };
        var btn = new Button { X = 14, Text = "Click Me!" };
        btn.Accept += (s, e) => clicked = true;

        var sbv = new ScrollBarView { IsVertical = true, Size = 5 };
        label.Add (sbv);
        Application.Top.Add (label, btn);
        Application.Begin (Application.Top);

        Assert.Equal (5, sbv.Size);
        Assert.Null (sbv.OtherScrollBarView);
        Assert.False (sbv.ShowScrollIndicator);
        Assert.False (sbv.Visible);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @$"
This is a test{
    CM.Glyphs.LeftBracket
} Click Me! {
    CM.Glyphs.RightBracket
}
This is a test             
This is a test             
This is a test             
This is a test             ",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 15, Y = 0, Flags = MouseFlags.Button1Clicked }
                                                          )
                                 );

        Assert.Null (Application.MouseGrabView);
        Assert.True (clicked);

        clicked = false;

        sbv.Visible = true;
        Assert.Equal (5, sbv.Size);
        Assert.False (sbv.ShowScrollIndicator);
        Assert.True (sbv.Visible);
        Application.Top.Draw ();

        // Set the visibility to true doesn't ensure the scroll bar from showing
        Assert.False (sbv.Visible);

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @$"
This is a test{
    CM.Glyphs.LeftBracket
} Click Me! {
    CM.Glyphs.RightBracket
}
This is a test             
This is a test             
This is a test             
This is a test             ",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 15, Y = 0, Flags = MouseFlags.Button1Clicked }
                                                          )
                                 );

        Assert.Null (Application.MouseGrabView);
        Assert.True (clicked);
        Assert.Equal (5, sbv.Size);
        Assert.False (sbv.ShowScrollIndicator);
        Assert.False (sbv.Visible);

        // It's needed to set ShowScrollIndicator to true and AutoHideScrollBars to false forcing
        // showing the scroll bar, otherwise AutoHideScrollBars will automatically control it
        Assert.True (sbv.AutoHideScrollBars);
        sbv.ShowScrollIndicator = true;
        sbv.AutoHideScrollBars = false;
        Application.Top.Draw ();

        TestHelpers.AssertDriverContentsWithFrameAre (
                                                      @$"
This is a tes▲{
    CM.Glyphs.LeftBracket
} Click Me! {
    CM.Glyphs.RightBracket
}
This is a tes┬             
This is a tes│             
This is a tes┴             
This is a tes▼             ",
                                                      _output
                                                     );

        Application.OnMouseEvent (
                                  new MouseEventEventArgs (
                                                           new MouseEvent { X = 15, Y = 0, Flags = MouseFlags.Button1Clicked }
                                                          )
                                 );

        Assert.Null (Application.MouseGrabView);
        Assert.True (clicked);
        Assert.Equal (5, sbv.Size);
        Assert.True (sbv.ShowScrollIndicator);
        Assert.True (sbv.Visible);
    }

    [Fact]
    [AutoInitShutdown]
    public void Vertical_Default_Draws_Correctly ()
    {
        var width = 3;
        var height = 40;

        var super = new Window { Id = "super", Width = Dim.Fill (), Height = Dim.Fill () };
        Application.Top.Add (super);

        var sbv = new ScrollBarView
        {
            Id = "sbv",
            Size = height * 2,

            ShowScrollIndicator = true,
            IsVertical = true
        };

        super.Add (sbv);
        Application.Begin (Application.Top);
        ((FakeDriver)Application.Driver).SetBufferSize (width, height);

        var expected = @"
┌─┐
│▲│
│┬│
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│││
│┴│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│░│
│▼│
└─┘";
        _ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    private void _hostView_DrawContent (object sender, DrawEventArgs e)
    {
        _scrollBar.Size = _hostView.Lines;
        _scrollBar.Position = _hostView.Top;
        _scrollBar.OtherScrollBarView.Size = _hostView.Cols;
        _scrollBar.OtherScrollBarView.Position = _hostView.Left;
        _scrollBar.Refresh ();
    }

    private void _scrollBar_ChangedPosition (object sender, EventArgs e)
    {
        _hostView.Top = _scrollBar.Position;

        if (_hostView.Top != _scrollBar.Position)
        {
            _scrollBar.Position = _hostView.Top;
        }

        _hostView.SetNeedsDisplay ();
    }

    private void _scrollBar_OtherScrollBarView_ChangedPosition (object sender, EventArgs e)
    {
        _hostView.Left = _scrollBar.OtherScrollBarView.Position;

        if (_hostView.Left != _scrollBar.OtherScrollBarView.Position)
        {
            _scrollBar.OtherScrollBarView.Position = _hostView.Left;
        }

        _hostView.SetNeedsDisplay ();
    }

    private void AddHandlers ()
    {
        if (!_added)
        {
            _hostView.DrawContent += _hostView_DrawContent;
            _scrollBar.ChangedPosition += _scrollBar_ChangedPosition;
            _scrollBar.OtherScrollBarView.ChangedPosition += _scrollBar_OtherScrollBarView_ChangedPosition;
        }

        _added = true;
    }

    private void RemoveHandlers ()
    {
        if (_added)
        {
            _hostView.DrawContent -= _hostView_DrawContent;
            _scrollBar.ChangedPosition -= _scrollBar_ChangedPosition;
            _scrollBar.OtherScrollBarView.ChangedPosition -= _scrollBar_OtherScrollBarView_ChangedPosition;
        }

        _added = false;
    }

    public class HostView : View
    {
        public int Cols { get; set; }
        public int Left { get; set; }
        public int Lines { get; set; }
        public int Top { get; set; }
    }

    // This class enables test functions annotated with the [InitShutdown] attribute
    // to have a function called before the test function is called and after.
    // 
    // This is necessary because a) Application is a singleton and Init/Shutdown must be called
    // as a pair, and b) all unit test functions should be atomic.
    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Method)]
    public class ScrollBarAutoInitShutdownAttribute : AutoInitShutdownAttribute
    {
        public override void After (MethodInfo methodUnderTest)
        {
            _hostView = null;
            base.After (methodUnderTest);
        }

        public override void Before (MethodInfo methodUnderTest)
        {
            base.Before (methodUnderTest);

            _hostView = new HostView
            {
                Width = Dim.Fill (),
                Height = Dim.Fill (),
                Top = 0,
                Lines = 30,
                Left = 0,
                Cols = 100
            };

            Application.Top.Add (_hostView);
        }
    }
}
