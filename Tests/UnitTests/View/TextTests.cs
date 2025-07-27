using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>
///     Tests of the <see cref="View.Text"/> and <see cref="View.TextFormatter"/> properties.
/// </summary>
public class TextTests (ITestOutputHelper output)
{
    [Fact]
    [SetupFakeDriver]
    public void Setting_With_Height_Horizontal ()
    {
        var top = new View { Width = 25, Height = 25 };

        var label = new Label { Text = "Hello", /* Width = 10, Height = 2, */ ValidatePosDim = true };
        var viewX = new View { Text = "X", X = Pos.Right (label), Width = 1, Height = 1 };
        var viewY = new View { Text = "Y", Y = Pos.Bottom (label), Width = 1, Height = 1 };

        top.Add (label, viewX, viewY);
        top.Layout ();

        Assert.Equal (new (0, 0, 5, 1), label.Frame);

        top.LayoutSubViews ();
        top.Draw ();

        var expected = @"
HelloX
Y     
";

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        label.Width = 10;
        label.Height = 2;

        Assert.Equal (new (0, 0, 10, 2), label.Frame);

        top.LayoutSubViews ();
        View.SetClipToScreen ();
        top.Draw ();

        expected = @"
Hello     X
           
Y          
";

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        Application.RunIteration (ref rs);

        Assert.Equal (new (0, 0, 1, 5), label.Frame);

        var expected = @"
HX
e 
l 
l 
o 
Y 
";

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        label.Width = 2;
        label.Height = 10;
        Application.RunIteration (ref rs);

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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        ((FakeDriver)Application.Driver!).SetBufferSize (15, 15);

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

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.Text = "Hello World";
        view.Width = 11;
        view.Height = 1;
        Application.RunIteration (ref rs);
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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.Width = Dim.Auto ();
        view.Height = Dim.Auto ();
        view.Text = "Hello Worlds";
        Application.RunIteration (ref rs);
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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.TextDirection = TextDirection.TopBottom_LeftRight;
        Application.RunIteration (ref rs);
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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        // Setting to false causes Width and Height to be set to the current ContentSize
        view.Width = 1;
        view.Height = 12;
        Application.RunIteration (ref rs);
        Assert.Equal (new (0, 0, 1, 12), view.Frame);

        view.Width = 12;
        view.Height = 1;
        view.TextFormatter.ConstrainToSize = new (12, 1);
        Application.RunIteration (ref rs);
        Assert.Equal (new (12, 1), view.TextFormatter.ConstrainToSize);
        Assert.Equal (new (0, 0, 12, 1), view.Frame);

        top.ClearViewport ();
        view.SetNeedsDraw ();
        view.Draw ();
        expected = @" HelloWorlds";
        DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        Application.RunIteration (ref rs);

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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.PreserveTrailingSpaces = true;
        Application.RunIteration (ref rs);

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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.PreserveTrailingSpaces = false;
        Rectangle f = view.Frame;
        view.Width = f.Height;
        view.Height = f.Width;
        view.TextDirection = TextDirection.TopBottom_LeftRight;
        Application.RunIteration (ref rs);

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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        view.Width = Dim.Auto ();
        view.Height = Dim.Auto ();

        Application.RunIteration (ref rs);

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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void View_IsEmpty_False_Minimum_Width ()
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
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (4, 10);

        Assert.Equal (5, text.Length);

        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        Assert.Equal (new (1, 5), view.TextFormatter.ConstrainToSize);
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

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 4, 10), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //view.Height = Dim.Fill () - text.Length;
        Application.RunIteration (ref rs);

        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        Assert.Equal (new (1, 5), view.TextFormatter.ConstrainToSize);
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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 4, 10), pos);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void DimAuto_Vertical_TextDirection_Wide_Rune ()
    {
        var text = "界View";

        var view = new View
        {
            TextDirection = TextDirection.TopBottom_LeftRight,
            Text = text,
            Width = Dim.Auto (),
            Height = Dim.Auto ()
        };

        view.SetRelativeLayout (new (4, 10));

        Assert.Equal (5, text.Length);

        // Vertical text - 2 wide, 5 down
        Assert.Equal (new (0, 0, 2, 5), view.Frame);
        Assert.Equal (new (2, 5), view.TextFormatter.ConstrainToSize);
        Assert.Equal (new () { "界View" }, view.TextFormatter.GetLines ());

        view.Draw ();

        var expected = @"
界
V 
i 
e 
w ";

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Width_Height_SetMinWidthHeight_Narrow_Wide_Runes ()
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
        ((FakeDriver)Application.Driver!).SetBufferSize (20, 20);

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

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        verticalView.Text = $"最初の行{Environment.NewLine}二行目";
        Application.RunIteration (ref rs);
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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Width_Height_Stay_True_If_TextFormatter_Size_Fit ()
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
        ((FakeDriver)Application.Driver!).SetBufferSize (22, 22);

        Assert.Equal (new (text.GetColumns (), 1), horizontalView.TextFormatter.ConstrainToSize);
        Assert.Equal (new (2, 8), verticalView.TextFormatter.ConstrainToSize);

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

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        verticalView.Text = "最初の行二行目";
        Application.RunIteration (ref rs);

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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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
        Application.RunIteration (ref rs);

        Assert.Equal (new (0, 0, 3, 1), lbl.Frame);

        Assert.Equal ("123 ", GetContents ());

        lbl.Text = "12";
        lbl.Layout ();

        Assert.Equal (new (0, 0, 2, 1), lbl.Frame);
        Assert.Equal (new (0, 0, 2, 1), lbl.NeedsDrawRect);
        Assert.Equal (new (0, 0, 80, 25), lbl.SuperView.NeedsDrawRect);
        Assert.True (lbl.SuperView.NeedsLayout);
        Application.RunIteration (ref rs);

        Assert.Equal ("12  ", GetContents ());

        string GetContents ()
        {
            var text = "";

            for (var i = 0; i < 4; i++)
            {
                text += Application.Driver?.Contents [0, i].Rune;
            }

            return text;
        }

        Application.End (rs);
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

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };
        frame.Add (lblLeft, lblCenter, lblRight, lblJust);
        var top = new Toplevel ();
        top.Add (frame);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (width + 2, 6);

        // frame.Width is width + border wide (20 + 2) and 6 high

        if (autoSize)
        {
            Size expectedSize = new (11, 1);
            Assert.Equal (expectedSize, lblLeft.TextFormatter.ConstrainToSize);
            Assert.Equal (expectedSize, lblCenter.TextFormatter.ConstrainToSize);
            Assert.Equal (expectedSize, lblRight.TextFormatter.ConstrainToSize);
            Assert.Equal (expectedSize, lblJust.TextFormatter.ConstrainToSize);
        }
        else
        {
            Size expectedSize = new (width, 1);
            Assert.Equal (expectedSize, lblLeft.TextFormatter.ConstrainToSize);
            Assert.Equal (expectedSize, lblCenter.TextFormatter.ConstrainToSize);
            Assert.Equal (expectedSize, lblRight.TextFormatter.ConstrainToSize);
            Assert.Equal (expectedSize, lblJust.TextFormatter.ConstrainToSize);
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

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
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

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };

        frame.Add (lblLeft, lblCenter, lblRight, lblJust);
        var top = new Toplevel ();
        top.Add (frame);
        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (9, height + 2);

        if (autoSize)
        {
            Assert.Equal (new (1, 11), lblLeft.TextFormatter.ConstrainToSize);
            Assert.Equal (new (1, 11), lblCenter.TextFormatter.ConstrainToSize);
            Assert.Equal (new (1, 11), lblRight.TextFormatter.ConstrainToSize);
            Assert.Equal (new (1, 11), lblJust.TextFormatter.ConstrainToSize);
            Assert.Equal (new (0, 0, 9, height + 2), frame.Frame);
        }
        else
        {
            Assert.Equal (new (1, height), lblLeft.TextFormatter.ConstrainToSize);
            Assert.Equal (new (1, height), lblCenter.TextFormatter.ConstrainToSize);
            Assert.Equal (new (1, height), lblRight.TextFormatter.ConstrainToSize);
            Assert.Equal (new (1, height), lblJust.TextFormatter.ConstrainToSize);
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

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 9, height + 2), pos);
        top.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void Narrow_Wide_Runes ()
    {
        ((FakeDriver)Application.Driver!).SetBufferSize (32, 32);
        var top = new View { Width = 32, Height = 32 };

        var text = $"First line{Environment.NewLine}Second line";
        var horizontalView = new View { Width = 20, Height = 1, Text = text };

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        horizontalView.TextFormatter.ConstrainToSize = new (20, 1);

        var verticalView = new View
        {
            Y = 3,
            Height = 20,
            Width = 1,
            Text = text,
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        verticalView.TextFormatter.ConstrainToSize = new (1, 20);

        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), Text = "Window", BorderStyle = LineStyle.Single };
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

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        verticalView.Text = $"最初の行{Environment.NewLine}二行目";
        Assert.True (verticalView.TextFormatter.NeedsFormat);

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        // We know these glpyhs are 2 cols wide, so we need to widen the view
        verticalView.Width = 2;
        verticalView.TextFormatter.ConstrainToSize = new (2, 20);
        Assert.True (verticalView.TextFormatter.NeedsFormat);
        View.SetClipToScreen ();
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

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
    }

    [Fact]
    [SetupFakeDriver]
    public void SetText_RendersCorrectly ()
    {
        View view;
        var text = "test";

        view = new Label { Text = text };
        view.BeginInit ();
        view.EndInit ();
        view.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (text, output);
    }
}
