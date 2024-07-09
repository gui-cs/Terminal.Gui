﻿using System.Runtime.CompilerServices;
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
///     Tests of the <see cref="View.Text"/> and <see cref="View.TextFormatter"/> properties (independent of
///     AutoSize).
/// </summary>
public class TextTests (ITestOutputHelper output)
{
    // TextFormatter.Size should be empty unless DimAuto is set or ContentSize is set
    [Theory]
    [InlineData ("", 0, 0)]
    [InlineData (" ", 0, 0)]
    [InlineData ("01234", 0, 0)]
    public void TextFormatter_Size_Default (string text, int expectedW, int expectedH)
    {
        var view = new View ();
        view.Text = text;
        Assert.Equal (new (expectedW, expectedH), view.TextFormatter.Size);
    }

    // TextFormatter.Size should track ContentSize (without DimAuto)
    [Theory]
    [InlineData ("", 1, 1)]
    [InlineData (" ", 1, 1)]
    [InlineData ("01234", 1, 1)]
    public void TextFormatter_Size_Tracks_ContentSize (string text, int expectedW, int expectedH)
    {
        var view = new View ();
        view.SetContentSize (new (1, 1));
        view.Text = text;
        Assert.Equal (new (expectedW, expectedH), view.TextFormatter.Size);
    }

    [Fact]
    [SetupFakeDriver]
    public void Setting_With_Height_Horizontal ()
    {
        var top = new View { Width = 25, Height = 25 };

        var label = new Label { Text = "Hello", /* Width = 10, Height = 2, */ ValidatePosDim = true };
        var viewX = new View { Text = "X", X = Pos.Right (label), Width = 1, Height = 1 };
        var viewY = new View { Text = "Y", Y = Pos.Bottom (label), Width = 1, Height = 1 };

        top.Add (label, viewX, viewY);
        top.BeginInit ();
        top.EndInit ();

        Assert.Equal (new (0, 0, 5, 1), label.Frame);

        top.LayoutSubviews ();
        top.Draw ();

        var expected = @"
HelloX
Y     
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        label.Width = 10;
        label.Height = 2;

        Assert.Equal (new (0, 0, 10, 2), label.Frame);

        top.LayoutSubviews ();
        top.Draw ();

        expected = @"
Hello     X
           
Y          
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Setting_With_Height_Vertical ()
    {
        // BUGBUG: Label is Width = Dim.Auto (), Height = Dim.Auto (), so Width & Height are ignored
        var label = new Label
        { /*Width = 2, Height = 10, */
            TextDirection = TextDirection.TopBottom_LeftRight, ValidatePosDim = true
        };
        var viewX = new View { Text = "X", X = Pos.Right (label), Width = 1, Height = 1 };
        var viewY = new View { Text = "Y", Y = Pos.Bottom (label), Width = 1, Height = 1 };

        var top = new Toplevel ();
        top.Add (label, viewX, viewY);
        RunState rs = Application.Begin (top);

        label.Text = "Hello";
        Application.Refresh ();

        Assert.Equal (new (0, 0, 1, 5), label.Frame);

        var expected = @"
HX
e 
l 
l 
o 
Y 
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        label.Width = 2;
        label.Height = 10;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 2, 10), label.Frame);

        expected = @"
H X
e  
l  
l  
o  
   
   
   
   
   
Y  
"
            ;

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void TextDirection_Toggle ()
    {
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };

        var view = new View ();
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);

        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (15, 15);

        Assert.Equal (new (0, 0, 15, 15), win.Frame);
        Assert.Equal (new (0, 0, 15, 15), win.Margin.Frame);
        Assert.Equal (new (0, 0, 15, 15), win.Border.Frame);
        Assert.Equal (new (1, 1, 13, 13), win.Padding.Frame);

        Assert.Equal (TextDirection.LeftRight_TopBottom, view.TextDirection);
        Assert.Equal (Rectangle.Empty, view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(0)", view.Width.ToString ());
        Assert.Equal ("Absolute(0)", view.Height.ToString ());

        var expected = @"
┌─────────────┐
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.Text = "Hello World";
        view.Width = 11;
        view.Height = 1;
        win.LayoutSubviews ();
        Application.Refresh ();

        Assert.Equal (new (0, 0, 11, 1), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(11)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());

        expected = @"
┌─────────────┐
│Hello World  │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.Width = Dim.Auto ();
        view.Height = Dim.Auto ();

        view.Text = "Hello Worlds";
        Application.Refresh ();
        int len = "Hello Worlds".Length;
        Assert.Equal (12, len);
        Assert.Equal (new (0, 0, len, 1), view.Frame);

        expected = @"
┌─────────────┐
│Hello Worlds │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.TextDirection = TextDirection.TopBottom_LeftRight;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 1, 12), view.Frame);
        Assert.Equal (new (0, 0, 1, 12), view.Frame);

        expected = @"
┌─────────────┐
│H            │
│e            │
│l            │
│l            │
│o            │
│             │
│W            │
│o            │
│r            │
│l            │
│d            │
│s            │
│             │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        // Setting to false causes Width and Height to be set to the current ContentSize
        view.Width = 1;
        view.Height = 12;

        Assert.Equal (new (0, 0, 1, 12), view.Frame);

        view.Width = 12;
        view.Height = 1;
        view.TextFormatter.Size = new (12, 1);
        win.LayoutSubviews ();
        Assert.Equal (new (12, 1), view.TextFormatter.Size);
        Assert.Equal (new (0, 0, 12, 1), view.Frame);
        top.Clear ();
        view.Draw ();
        expected = @" HelloWorlds";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        Application.Refresh ();

        // TextDirection.TopBottom_LeftRight - Height of 1 and Width of 12 means 
        // that the text will be spread "vertically" across 1 line.
        // Hence no space.
        expected = @"
┌─────────────┐
│HelloWorlds  │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.PreserveTrailingSpaces = true;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 12, 1), view.Frame);

        expected = @"
┌─────────────┐
│Hello Worlds │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
│             │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.PreserveTrailingSpaces = false;
        Rectangle f = view.Frame;
        view.Width = f.Height;
        view.Height = f.Width;
        view.TextDirection = TextDirection.TopBottom_LeftRight;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 1, 12), view.Frame);

        expected = @"
┌─────────────┐
│H            │
│e            │
│l            │
│l            │
│o            │
│             │
│W            │
│o            │
│r            │
│l            │
│d            │
│s            │
│             │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.Width = Dim.Auto ();
        view.Height = Dim.Auto ();

        Application.Refresh ();

        Assert.Equal (new (0, 0, 1, 12), view.Frame);

        expected = @"
┌─────────────┐
│H            │
│e            │
│l            │
│l            │
│o            │
│             │
│W            │
│o            │
│r            │
│l            │
│d            │
│s            │
│             │
└─────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_View_IsEmpty_False_Minimum_Width ()
    {
        var text = "Views";

        var view = new View
        {
            TextDirection = TextDirection.TopBottom_LeftRight,
            Height = Dim.Fill () - text.Length,
            Text = text
        };
        view.Width = Dim.Auto ();
        view.Height = Dim.Auto ();

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (4, 10);

        Assert.Equal (5, text.Length);

        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        Assert.Equal (new (1, 5), view.TextFormatter.Size);
        Assert.Equal (new () { "Views" }, view.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 4, 10), win.Frame);
        Assert.Equal (new (0, 0, 4, 10), Application.Top.Frame);

        var expected = @"
┌──┐
│V │
│i │
│e │
│w │
│s │
│  │
│  │
│  │
└──┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 4, 10), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //view.Height = Dim.Fill () - text.Length;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        Assert.Equal (new (1, 5), view.TextFormatter.Size);
        Exception exception = Record.Exception (() => Assert.Single (view.TextFormatter.GetLines ()));
        Assert.Null (exception);

        expected = @"
┌──┐
│V │
│i │
│e │
│w │
│s │
│  │
│  │
│  │
└──┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 4, 10), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_View_IsEmpty_False_Minimum_Width_Wide_Rune ()
    {
        var text = "界View";

        var view = new View
        {
            TextDirection = TextDirection.TopBottom_LeftRight,
            Text = text,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (4, 10);

        Assert.Equal (5, text.Length);
        Assert.Equal (new (0, 0, 2, 5), view.Frame);
        Assert.Equal (new (2, 5), view.TextFormatter.Size);
        Assert.Equal (new () { "界View" }, view.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 4, 10), win.Frame);
        Assert.Equal (new (0, 0, 4, 10), Application.Top.Frame);

        var expected = @"
┌──┐
│界│
│V │
│i │
│e │
│w │
│  │
│  │
│  │
└──┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 4, 10), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //view.Height = Dim.Fill () - text.Length;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 2, 5), view.Frame);
        Assert.Equal (new (2, 5), view.TextFormatter.Size);

        Exception exception = Record.Exception (
                                                () => Assert.Equal (
                                                                    new () { "界View" },
                                                                    view.TextFormatter.GetLines ()
                                                                   )
                                               );
        Assert.Null (exception);

        expected = @"
┌──┐
│界│
│V │
│i │
│e │
│w │
│  │
│  │
│  │
└──┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 4, 10), pos);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_Width_Height_SetMinWidthHeight_Narrow_Wide_Runes ()
    {
        var text = $"0123456789{Environment.NewLine}01234567891";

        var horizontalView = new View
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = text
        };

        var verticalView = new View
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Y = 3,

            //Height = 11,
            //Width = 2,
            Text = text,
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        var win = new Window
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "Window"
        };
        win.Add (horizontalView, verticalView);
        var top = new Toplevel ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (20, 20);

        Assert.Equal (new (0, 0, 11, 2), horizontalView.Frame);
        Assert.Equal (new (0, 3, 2, 11), verticalView.Frame);

        var expected = @"
┌──────────────────┐
│0123456789        │
│01234567891       │
│                  │
│00                │
│11                │
│22                │
│33                │
│44                │
│55                │
│66                │
│77                │
│88                │
│99                │
│ 1                │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        verticalView.Text = $"最初の行{Environment.NewLine}二行目";
        Application.Top.Draw ();
        Assert.Equal (new (0, 3, 4, 4), verticalView.Frame);

        expected = @"
┌──────────────────┐
│0123456789        │
│01234567891       │
│                  │
│最二              │
│初行              │
│の目              │
│行                │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_Width_Height_Stay_True_If_TextFormatter_Size_Fit ()
    {
        var text = "Finish 終";

        var horizontalView = new View
        {
            Id = "horizontalView",
            Width = Dim.Auto (), Height = Dim.Auto (), Text = text
        };

        var verticalView = new View
        {
            Id = "verticalView",
            Y = 3,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            Text = text,
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        var win = new Window { Id = "win", Width = Dim.Fill (), Height = Dim.Fill (), Text = "Window" };
        win.Add (horizontalView, verticalView);
        var top = new Toplevel ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (22, 22);

        Assert.Equal (new (text.GetColumns (), 1), horizontalView.TextFormatter.Size);
        Assert.Equal (new (2, 8), verticalView.TextFormatter.Size);

        //Assert.Equal (new (0, 0, 10, 1), horizontalView.Frame);
        //Assert.Equal (new (0, 3, 10, 9), verticalView.Frame);

        var expected = @"
┌────────────────────┐
│Finish 終           │
│                    │
│                    │
│F                   │
│i                   │
│n                   │
│i                   │
│s                   │
│h                   │
│                    │
│終                  │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        verticalView.Text = "最初の行二行目";
        Application.Top.Draw ();

        // height was initialized with 8 and can only grow or keep initial value
        Assert.Equal (new (0, 3, 2, 7), verticalView.Frame);

        expected = @"
┌────────────────────┐
│Finish 終           │
│                    │
│                    │
│最                  │
│初                  │
│の                  │
│行                  │
│二                  │
│行                  │
│目                  │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
│                    │
└────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Excess_Text_Is_Erased_When_The_Width_Is_Reduced ()
    {
        var lbl = new Label { Text = "123" };
        var top = new Toplevel ();
        top.Add (lbl);
        RunState rs = Application.Begin (top);

        Assert.Equal ("123 ", GetContents ());

        lbl.Text = "12";

        Assert.Equal (new (0, 0, 2, 1), lbl.Frame);
        Assert.Equal (new (0, 0, 3, 1), lbl._needsDisplayRect);
        Assert.Equal (new (0, 0, 0, 0), lbl.SuperView._needsDisplayRect);
        Assert.True (lbl.SuperView.LayoutNeeded);
        lbl.SuperView.Draw ();
        Assert.Equal ("12  ", GetContents ());

        string GetContents ()
        {
            var text = "";

            for (var i = 0; i < 4; i++)
            {
                text += Application.Driver.Contents [0, i].Rune;
            }

            return text;
        }

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void GetTextFormatterBoundsSize_GetSizeNeededForText_HotKeySpecifier ()
    {
        var text = "Say Hello 你";

        // Frame: 0, 0, 12, 1
        var horizontalView = new View
        {
            Width = Dim.Auto (), Height = Dim.Auto ()
        };
        horizontalView.TextFormatter.HotKeySpecifier = (Rune)'_';
        horizontalView.Text = text;

        // Frame: 0, 0, 1, 12
        var verticalView = new View
        {
            Width = Dim.Auto (), Height = Dim.Auto (), TextDirection = TextDirection.TopBottom_LeftRight
        };
        verticalView.Text = text;
        verticalView.TextFormatter.HotKeySpecifier = (Rune)'_';

        var top = new Toplevel ();
        top.Add (horizontalView, verticalView);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (50, 50);

        Assert.Equal (new (0, 0, 12, 1), horizontalView.Frame);
        Assert.Equal (new (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

        Assert.Equal (new (0, 0, 2, 11), verticalView.Frame);
        Assert.Equal (new (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());

        text = "012345678你";
        horizontalView.Text = text;
        verticalView.Text = text;

        Assert.Equal (new (0, 0, 11, 1), horizontalView.Frame);
        Assert.Equal (new (11, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

        Assert.Equal (new (0, 0, 2, 10), verticalView.Frame);
        Assert.Equal (new (2, 10), verticalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());
        top.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void View_Draw_Horizontal_Simple_TextAlignments (bool autoSize)
    {
        var text = "Hello World";
        var width = 20;

        var lblLeft = new View
        {
            Text = text,
            Width = width,
            Height = 1
        };

        if (autoSize)
        {
            lblLeft.Width = Dim.Auto ();
            lblLeft.Height = Dim.Auto ();
        }

        var lblCenter = new View
        {
            Text = text,
            Y = 1,
            Width = width,
            Height = 1,
            TextAlignment = Alignment.Center
        };

        if (autoSize)
        {
            lblCenter.Width = Dim.Auto ();
            lblCenter.Height = Dim.Auto ();
        }

        var lblRight = new View
        {
            Text = text,
            Y = 2,
            Width = width,
            Height = 1,
            TextAlignment = Alignment.End
        };

        if (autoSize)
        {
            lblRight.Width = Dim.Auto ();
            lblRight.Height = Dim.Auto ();
        }

        var lblJust = new View
        {
            Text = text,
            Y = 3,
            Width = width,
            Height = 1,
            TextAlignment = Alignment.Fill
        };

        if (autoSize)
        {
            lblJust.Width = Dim.Auto ();
            lblJust.Height = Dim.Auto ();
        }

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
        frame.Add (lblLeft, lblCenter, lblRight, lblJust);
        var top = new Toplevel ();
        top.Add (frame);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (width + 2, 6);

        if (autoSize)
        {
            Size expectedSize = new (11, 1);
            Assert.Equal (expectedSize, lblLeft.TextFormatter.Size);
            Assert.Equal (expectedSize, lblCenter.TextFormatter.Size);
            Assert.Equal (expectedSize, lblRight.TextFormatter.Size);
            Assert.Equal (expectedSize, lblJust.TextFormatter.Size);
        }
        else
        {
            Size expectedSize = new (width, 1);
            Assert.Equal (expectedSize, lblLeft.TextFormatter.Size);
            Assert.Equal (expectedSize, lblCenter.TextFormatter.Size);
            Assert.Equal (expectedSize, lblRight.TextFormatter.Size);
            Assert.Equal (expectedSize, lblJust.TextFormatter.Size);
        }

        Assert.Equal (new (0, 0, width + 2, 6), frame.Frame);

        string expected;

        if (autoSize)
        {
            expected = @"
┌────────────────────┐
│Hello World         │
│Hello World         │
│Hello World         │
│Hello World         │
└────────────────────┘
";
        }
        else
        {
            expected = @"
┌────────────────────┐
│Hello World         │
│    Hello World     │
│         Hello World│
│Hello          World│
└────────────────────┘
";
        }

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, width + 2, 6), pos);
        top.Dispose ();
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void View_Draw_Vertical_Simple_TextAlignments (bool autoSize)
    {
        var text = "Hello World";
        var height = 20;

        var lblLeft = new View
        {
            Text = text,
            Width = 1,
            Height = height,
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        if (autoSize)
        {
            lblLeft.Width = Dim.Auto ();
            lblLeft.Height = Dim.Auto ();
        }

        var lblCenter = new View
        {
            Text = text,
            X = 2,
            Width = 1,
            Height = height,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Center
        };

        if (autoSize)
        {
            lblCenter.Width = Dim.Auto ();
            lblCenter.Height = Dim.Auto ();
        }

        var lblRight = new View
        {
            Text = text,
            X = 4,
            Width = 1,
            Height = height,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.End
        };

        if (autoSize)
        {
            lblRight.Width = Dim.Auto ();
            lblRight.Height = Dim.Auto ();
        }

        var lblJust = new View
        {
            Text = text,
            X = 6,
            Width = 1,
            Height = height,
            TextDirection = TextDirection.TopBottom_LeftRight,
            VerticalTextAlignment = Alignment.Fill
        };

        if (autoSize)
        {
            lblJust.Width = Dim.Auto ();
            lblJust.Height = Dim.Auto ();
        }

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

        frame.Add (lblLeft, lblCenter, lblRight, lblJust);
        var top = new Toplevel ();
        top.Add (frame);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (9, height + 2);

        if (autoSize)
        {
            Assert.Equal (new (1, 11), lblLeft.TextFormatter.Size);
            Assert.Equal (new (1, 11), lblCenter.TextFormatter.Size);
            Assert.Equal (new (1, 11), lblRight.TextFormatter.Size);
            Assert.Equal (new (1, 11), lblJust.TextFormatter.Size);
            Assert.Equal (new (0, 0, 9, height + 2), frame.Frame);
        }
        else
        {
            Assert.Equal (new (1, height), lblLeft.TextFormatter.Size);
            Assert.Equal (new (1, height), lblCenter.TextFormatter.Size);
            Assert.Equal (new (1, height), lblRight.TextFormatter.Size);
            Assert.Equal (new (1, height), lblJust.TextFormatter.Size);
            Assert.Equal (new (0, 0, 9, height + 2), frame.Frame);
        }

        string expected;

        if (autoSize)
        {
            expected = @"
┌───────┐
│H H H H│
│e e e e│
│l l l l│
│l l l l│
│o o o o│
│       │
│W W W W│
│o o o o│
│r r r r│
│l l l l│
│d d d d│
│       │
│       │
│       │
│       │
│       │
│       │
│       │
│       │
│       │
└───────┘
";
        }
        else
        {
            expected = @"
┌───────┐
│H     H│
│e     e│
│l     l│
│l     l│
│o H   o│
│  e    │
│W l    │
│o l    │
│r o    │
│l   H  │
│d W e  │
│  o l  │
│  r l  │
│  l o  │
│  d    │
│    W W│
│    o o│
│    r r│
│    l l│
│    d d│
└───────┘
";
        }

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 9, height + 2), pos);
        top.Dispose ();
    }

    // Test that View.PreserveTrailingSpaces removes trailing spaces
    [Fact]
    public void PreserveTrailingSpaces_Removes_Trailing_Spaces ()
    {
        var view = new View { Text = "Hello World " };
        Assert.Equal ("Hello World ", view.TextFormatter.Text);

        view.TextFormatter.WordWrap = true;
        view.TextFormatter.Size = new (5, 3);

        view.PreserveTrailingSpaces = false;
        Assert.Equal ($"Hello{Environment.NewLine}World", view.TextFormatter.Format ());

        view.PreserveTrailingSpaces = true;
        Assert.Equal ($"Hello{Environment.NewLine} {Environment.NewLine}World", view.TextFormatter.Format ());
    }

    // View.PreserveTrailingSpaces Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
    // or not when <see cref="TextFormatter.WordWrap"/> is enabled.
    // If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when
    // <see cref = "Text" / > is formatted for display.The default is <see langword = "false" / >.
    [Fact]
    public void PreserveTrailingSpaces_Set_Get ()
    {
        var view = new View { Text = "Hello World" };

        Assert.False (view.PreserveTrailingSpaces);

        view.PreserveTrailingSpaces = true;
        Assert.True (view.PreserveTrailingSpaces);
    }

    // Setting TextFormatter DOES NOT update Text
    [Fact]
    public void SettingTextFormatterDoesNotUpdateText ()
    {
        var view = new View ();
        view.TextFormatter.Text = "Hello World";

        Assert.True (string.IsNullOrEmpty (view.Text));
    }

    // Setting Text updates TextFormatter
    [Fact]
    public void SettingTextUpdatesTextFormatter ()
    {
        var view = new View { Text = "Hello World" };

        Assert.Equal ("Hello World", view.Text);
        Assert.Equal ("Hello World", view.TextFormatter.Text);
    }

    // Setting Text does NOT set the HotKey
    [Fact]
    public void Text_Does_Not_Set_HotKey ()
    {
        var view = new View { HotKeySpecifier = (Rune)'_', Text = "_Hello World" };

        Assert.NotEqual (Key.H, view.HotKey);
    }

    // Test that TextFormatter is init only
    [Fact]
    public void TextFormatterIsInitOnly ()
    {
        var view = new View ();

        // Use reflection to ensure the TextFormatter property is `init` only
        Assert.Contains (
                         typeof (IsExternalInit),
                         typeof (View).GetMethod ("set_TextFormatter")
                                      .ReturnParameter.GetRequiredCustomModifiers ());
    }

    // Test that the Text property is set correctly.
    [Fact]
    public void TextProperty ()
    {
        var view = new View { Text = "Hello World" };

        Assert.Equal ("Hello World", view.Text);
    }

    // Test view.UpdateTextFormatterText overridden in a subclass updates TextFormatter.Text
    [Fact]
    public void UpdateTextFormatterText_Overridden ()
    {
        var view = new TestView { Text = "Hello World" };

        Assert.Equal ("Hello World", view.Text);
        Assert.Equal (">Hello World<", view.TextFormatter.Text);
    }

    private class TestView : View
    {
        protected override void UpdateTextFormatterText () { TextFormatter.Text = $">{Text}<"; }
    }

    [Fact]
    public void TextDirection_Horizontal_Dims_Correct ()
    {
        // Initializes a view with a vertical direction
        var view = new View
        {
            Text = "01234",
            TextDirection = TextDirection.LeftRight_TopBottom,
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text)
        };
        Assert.Equal (new (0, 0, 5, 1), view.Frame);
        Assert.Equal (new (0, 0, 5, 1), view.Viewport);

        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (new (0, 0, 5, 1), view.Frame);
        Assert.Equal (new (0, 0, 5, 1), view.Viewport);
    }

    // BUGBUG: this is a temporary test that helped identify #3469 - It needs to be expanded upon (and renamed)
    [Fact]
    public void TextDirection_Horizontal_Dims_Correct_WidthAbsolute ()
    {
        var view = new View
        {
            Text = "01234",
            TextDirection = TextDirection.LeftRight_TopBottom,
            TextAlignment = Alignment.Center,
            Width = 10,
            Height = Dim.Auto (DimAutoStyle.Text)
        };
        view.BeginInit ();
        view.EndInit ();
        Assert.Equal (new (0, 0, 10, 1), view.Frame);
        Assert.Equal (new (0, 0, 10, 1), view.Viewport);

        Assert.Equal (new (10, 1), view.TextFormatter.Size);
    }

    [Fact]
    public void TextDirection_Vertical_Dims_Correct ()
    {
        // Initializes a view with a vertical direction
        var view = new View
        {
            TextDirection = TextDirection.TopBottom_LeftRight,
            Text = "01234",
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text)
        };
        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        Assert.Equal (new (0, 0, 1, 5), view.Viewport);

        view.BeginInit ();
        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        view.EndInit ();
        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        Assert.Equal (new (0, 0, 1, 5), view.Viewport);
    }

    [Fact]
    [SetupFakeDriver]
    public void Narrow_Wide_Runes ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (32, 32);
        var top = new View { Width = 32, Height = 32 };

        var text = $"First line{Environment.NewLine}Second line";
        var horizontalView = new View { Width = 20, Height = 1, Text = text };

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        horizontalView.TextFormatter.Size = new (20, 1);

        var verticalView = new View
        {
            Y = 3,
            Height = 20,
            Width = 1,
            Text = text,
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        verticalView.TextFormatter.Size = new (1, 20);

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), Text = "Window" };
        frame.Add (horizontalView, verticalView);
        top.Add (frame);
        top.BeginInit ();
        top.EndInit ();

        Assert.Equal (new (0, 0, 20, 1), horizontalView.Frame);
        Assert.Equal (new (0, 3, 1, 20), verticalView.Frame);

        top.Draw ();

        var expected = @"
┌──────────────────────────────┐
│First line Second li          │
│                              │
│                              │
│F                             │
│i                             │
│r                             │
│s                             │
│t                             │
│                              │
│l                             │
│i                             │
│n                             │
│e                             │
│                              │
│S                             │
│e                             │
│c                             │
│o                             │
│n                             │
│d                             │
│                              │
│l                             │
│i                             │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
└──────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        verticalView.Text = $"最初の行{Environment.NewLine}二行目";
        Assert.True (verticalView.TextFormatter.NeedsFormat);

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        // We know these glpyhs are 2 cols wide, so we need to widen the view
        verticalView.Width = 2;
        verticalView.TextFormatter.Size = new (2, 20);
        Assert.True (verticalView.TextFormatter.NeedsFormat);

        top.Draw ();
        Assert.Equal (new (0, 3, 2, 20), verticalView.Frame);

        expected = @"
┌──────────────────────────────┐
│First line Second li          │
│                              │
│                              │
│最                            │
│初                            │
│の                            │
│行                            │
│                              │
│二                            │
│行                            │
│目                            │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
│                              │
└──────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
    }

    // Test behavior of AutoSize property. 
    // - Default is false
    // - Setting to true invalidates Height/Width
    // - Setting to false invalidates Height/Width
}
