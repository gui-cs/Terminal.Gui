using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>Tests of the  <see cref="View.AutoSize"/> property which auto sizes Views based on <see cref="Text"/>.</summary>
public class AutoSizeTrueTests
{
    private readonly ITestOutputHelper _output;

    private readonly string [] expecteds = new string[21]
    {
        @"
┌────────────────────┐
│View with long text │
│                    │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 0             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 1             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 2             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 3             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 4             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 5             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 6             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 7             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 8             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 9             │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 10            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 11            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 12            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 13            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 14            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 15            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 16            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 17            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 18            │
│Label 18            │
└────────────────────┘",
        @"
┌────────────────────┐
│View with long text │
│Label 0             │
│Label 1             │
│Label 2             │
│Label 3             │
│Label 4             │
│Label 5             │
│Label 6             │
│Label 7             │
│Label 8             │
│Label 9             │
│Label 10            │
│Label 11            │
│Label 12            │
│Label 13            │
│Label 14            │
│Label 15            │
│Label 16            │
│Label 17            │
│Label 18            │
│Label 19            │
│Label 19            │
└────────────────────┘"
    };

    private static readonly Size _size1x1 = new (1, 1);

    public AutoSizeTrueTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_AnchorEnd_Better_Than_Bottom_Equal_Inside_Window ()
    {
        var win = new Window ();

        // Label is AutoSize == true
        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            //Width = Dim.Fill (),
            X = 0, // keep unit test focused; don't use Center here
            Y = Pos.AnchorEnd (1)
        };

        win.Add (label);

        Toplevel top =new ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (40, 10);

        Assert.True (label.AutoSize);
        Assert.Equal (29, label.Text.Length);
        Assert.Equal (new Rectangle (0, 0, 40, 10), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 40, 10), win.Frame);
        Assert.Equal (new Rectangle (0, 7, 29, 1), label.Frame);

        var expected = @"
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│This should be the last line.         │
└──────────────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_AnchorEnd_Better_Than_Bottom_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
    {
        var win = new Window ();

        // Label is AutoSize == true
        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            //Width = Dim.Fill (),
            X = 0,
            Y = Pos.AnchorEnd (1)
        };

        win.Add (label);

        var menu = new MenuBar { Menus = new MenuBarItem [] { new ("Menu", "", null) } };
        var status = new StatusBar (new StatusItem [] { new (KeyCode.F1, "~F1~ Help", null) });
        Toplevel top = new ();
        top.Add (win, menu, status);
        RunState rs = Application.Begin (top);

        Assert.True (label.AutoSize);
        Assert.Equal (new Rectangle (0, 0, 80, 25), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 80, 1), menu.Frame);
        Assert.Equal (new Rectangle (0, 24, 80, 1), status.Frame);
        Assert.Equal (new Rectangle (0, 1, 80, 23), win.Frame);
        Assert.Equal (new Rectangle (0, 20, 29, 1), label.Frame);

        var expected = @"
 Menu                                                                           
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│This should be the last line.                                                 │
└──────────────────────────────────────────────────────────────────────────────┘
 F1 Help                                                                        
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Bottom_Equal_Inside_Window ()
    {
        var win = new Window ();

        // Label is AutoSize == true
        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            //Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (win)
                - 3 // two lines top and bottom borders more one line above the bottom border
        };

        win.Add (label);

        Toplevel top = new ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (40, 10);

        Assert.True (label.AutoSize);
        Assert.Equal (new Rectangle (0, 0, 40, 10), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 40, 10), win.Frame);
        Assert.Equal (new Rectangle (0, 7, 29, 1), label.Frame);

        var expected = @"
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│This should be the last line.         │
└──────────────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Bottom_Equal_Inside_Window_With_MenuBar_And_StatusBar_On_Toplevel ()
    {
        var win = new Window ();

        // Label is AutoSize == true
        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            //Width = Dim.Fill (),
            X = 0,
            Y = Pos.Bottom (win)
                - 4 // two lines top and bottom borders more two lines above border
        };

        win.Add (label);

        var menu = new MenuBar { Menus = new MenuBarItem [] { new ("Menu", "", null) } };
        var status = new StatusBar (new StatusItem [] { new (KeyCode.F1, "~F1~ Help", null) });
        Toplevel top = new ();
        top.Add (win, menu, status);
        RunState rs = Application.Begin (top);

        Assert.True (label.AutoSize);
        Assert.Equal (new Rectangle (0, 0, 80, 25), top.Frame);
        Assert.Equal (new Rectangle (0, 0, 80, 1), menu.Frame);
        Assert.Equal (new Rectangle (0, 24, 80, 1), status.Frame);
        Assert.Equal (new Rectangle (0, 1, 80, 23), win.Frame);
        Assert.Equal (new Rectangle (0, 20, 29, 1), label.Frame);

        var expected = @"
 Menu                                                                           
┌──────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│                                                                              │
│This should be the last line.                                                 │
└──────────────────────────────────────────────────────────────────────────────┘
 F1 Help                                                                        
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Dim_Add_Operator_With_Text ()
    {
        Toplevel top = new ();

        var view = new View
        {
            Text = "View with long text",
            X = 0,
            Y = 0,
            Width = 20,
            Height = 1
        };
        var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
        var count = 0;

        // Label is AutoSize == true
        List<Label> listLabels = new ();

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 ((FakeDriver)Application.Driver).SetBufferSize (22, count + 4);
                                 Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], _output);
                                 Assert.Equal (new Rectangle (0, 0, 22, count + 4), pos);

                                 if (count < 20)
                                 {
                                     field.Text = $"Label {count}";

                                     // Label is AutoSize = true
                                     var label = new Label { Text = field.Text, X = 0, Y = view.Bounds.Height /*, Width = 10*/ };
                                     view.Add (label);
                                     Assert.Equal ($"Label {count}", label.Text);
                                     Assert.Equal ($"Absolute({count + 1})", label.Y.ToString ());
                                     listLabels.Add (label);

                                     //if (count == 0) {
                                     //	Assert.Equal ($"Absolute({count})", view.Height.ToString ());
                                     //	view.Height += 2;
                                     //} else {
                                     Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
                                     view.Height += 1;

                                     //}
                                     count++;
                                 }

                                 Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
                             }
                         };

        Application.Iteration += (s, a) =>
                                 {
                                     while (count < 21)
                                     {
                                         field.NewKeyDownEvent (Key.Enter);

                                         if (count == 20)
                                         {
                                             field.NewKeyDownEvent (Key.Enter);

                                             break;
                                         }
                                     }

                                     Application.RequestStop ();
                                 };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);

        Assert.Equal (20, count);
        Assert.Equal (count, listLabels.Count);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Dim_Subtract_Operator_With_Text ()
    {
        Toplevel top = new ();

        var view = new View
        {
            Text = "View with long text",
            X = 0,
            Y = 0,
            Width = 20,
            Height = 1
        };
        var field = new TextField { X = 0, Y = Pos.Bottom (view), Width = 20 };
        var count = 20;

        // Label is AutoSize == true
        List<Label> listLabels = new ();

        for (var i = 0; i < count; i++)
        {
            field.Text = $"Label {i}";
            var label = new Label { Text = field.Text, X = 0, Y = i + 1 /*, Width = 10*/ };
            view.Add (label);
            Assert.Equal ($"Label {i}", label.Text);
            Assert.Equal ($"Absolute({i + 1})", label.Y.ToString ());
            listLabels.Add (label);

            if (i == 0)
            {
                Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
                view.Height += 1;
                Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
            }
            else
            {
                Assert.Equal ($"Absolute({i + 1})", view.Height.ToString ());
                view.Height += 1;
                Assert.Equal ($"Absolute({i + 2})", view.Height.ToString ());
            }
        }

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 ((FakeDriver)Application.Driver).SetBufferSize (22, count + 4);
                                 Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], _output);
                                 Assert.Equal (new Rectangle (0, 0, 22, count + 4), pos);

                                 if (count > 0)
                                 {
                                     Assert.Equal ($"Label {count - 1}", listLabels [count - 1].Text);
                                     view.Remove (listLabels [count - 1]);
                                     listLabels [count - 1].Dispose ();
                                     listLabels.RemoveAt (count - 1);
                                     Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
                                     view.Height -= 1;
                                     count--;

                                     if (listLabels.Count > 0)
                                     {
                                         field.Text = listLabels [count - 1].Text;
                                     }
                                     else
                                     {
                                         field.Text = string.Empty;
                                     }
                                 }

                                 Assert.Equal ($"Absolute({count + 1})", view.Height.ToString ());
                             }
                         };

        Application.Iteration += (s, a) =>
                                 {
                                     while (count > -1)
                                     {
                                         field.NewKeyDownEvent (Key.Enter);

                                         if (count == 0)
                                         {
                                             field.NewKeyDownEvent (Key.Enter);

                                             break;
                                         }
                                     }

                                     Application.RequestStop ();
                                 };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);

        Assert.Equal (0, count);
        Assert.Equal (count, listLabels.Count);
    }

    //[Fact]
    //[AutoInitShutdown]
    //public void AutoSize_False_Label_IsEmpty_True_Return_Null_Lines ()
    //{
    //    var text = "Label";
    //    var label = new Label
    //    {
    //        AutoSize = false,
    //        Height = 1,
    //        Text = text,
    //    };
    //    var win = new Window
    //    {
    //        Width = Dim.Fill (),
    //        Height = Dim.Fill ()
    //    };
    //    win.Add (label);
    //    var top = new Toplevel ();
    //    top.Add (win);
    //    Application.Begin (top);
    //    ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

    //    Assert.Equal (5, text.Length);
    //    Assert.False (label.AutoSize);
    //    Assert.Equal (new (0, 0, 0, 1), label.Frame);
    //    Assert.Equal (new (3, 1), label.TextFormatter.Size);
    //    Assert.Equal (new List<string> { "Lab" }, label.TextFormatter.GetLines());
    //    Assert.Equal (new (0, 0, 10, 4), win.Frame);
    //    Assert.Equal (new (0, 0, 10, 4), Application.Top.Frame);
    //    var expected = @"
    //┌────────┐
    //│Lab     │
    //│        │
    //└────────┘
    //";

    //    var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    //    Assert.Equal (new (0, 0, 10, 4), pos);

    //    text = "0123456789";
    //    Assert.Equal (10, text.Length);
    //    //label.Width = Dim.Fill () - text.Length;
    //    Application.Refresh ();

    //    Assert.False (label.AutoSize);
    //    Assert.Equal (new (0, 0, 0, 1), label.Frame);
    //    Assert.Equal (new (0, 1), label.TextFormatter.Size);
    //    Assert.Equal (new List<string> { string.Empty }, label.TextFormatter.GetLines());
    //    expected = @"
    //┌────────┐
    //│        │
    //│        │
    //└────────┘
    //";

    //    pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    //    Assert.Equal (new (0, 0, 10, 4), pos);
    //}

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_False_Label_Height_Zero_Stays_Zero ()
    {
        var text = "Label";
        var label = new Label { Text = text, AutoSize = false };
        label.Width = Dim.Fill () - text.Length;
        label.Height = 0;

        var win = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);
        win.BeginInit ();
        win.EndInit ();
        win.LayoutSubviews ();
        win.Draw ();

        Assert.Equal (5, text.Length);
        Assert.False (label.AutoSize);
        Assert.Equal (new (0, 0, 3, 0), label.Frame);
        Assert.Equal (new (3, 0), label.TextFormatter.Size);
        Assert.Single (label.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), win.Frame);

        var expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);
        label.Width = Dim.Fill () - text.Length;
        win.LayoutSubviews ();
        win.Clear ();
        win.Draw ();

        Assert.Equal (Rectangle.Empty, label.Frame);
        Assert.Equal (Size.Empty, label.TextFormatter.Size);

        Exception exception = Record.Exception (
                                                () => Assert.Equal (
                                                                    new List<string> { string.Empty },
                                                                    label.TextFormatter.GetLines ()
                                                                   )
                                               );
        Assert.Null (exception);

        expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    public void AutoSize_False_SetWidthHeight_With_Dim_Fill_And_Dim_Absolute_With_Initialization ()
    {
        var win = new Window { Frame = new (0, 0, 30, 80) };
        var label = new Label ();
        win.Add (label);
        win.BeginInit ();
        win.EndInit ();

        Assert.True (label.AutoSize);
        Rectangle expectedLabelBounds = Rectangle.Empty;
        Assert.Equal (expectedLabelBounds, label.Bounds);
        Assert.True (label.AutoSize);

        label.Text = "First line\nSecond line";
        win.LayoutSubviews ();

        expectedLabelBounds = new (0, 0, 11, 2);
        Assert.True (label.AutoSize);
        Assert.Equal (expectedLabelBounds, label.Bounds);

        label.AutoSize = false;
        label.Width = Dim.Fill ();
        label.Height = 2;
        win.LayoutSubviews ();

        // Here the SetMinWidthHeight ensuring the minimum height
        // #3127: After: (0,0,28,2) because turning off AutoSize leaves
        // Height set to 2.
        expectedLabelBounds = new (0, 0, 28, 2);
        Assert.False (label.AutoSize);
        Assert.Equal (expectedLabelBounds, label.Bounds);

        label.Text = "First changed line\nSecond changed line\nNew line";
        win.LayoutSubviews ();

        // Here the AutoSize is false and the width 28 (Dim.Fill) and
        // #3127: Before: height 1 because it wasn't set and SetMinWidthHeight ensuring the minimum height
        // #3127: After: (0,0,28,2) because setting Text leaves Height set to 2.
        expectedLabelBounds = new (0, 0, 28, 2);
        Assert.False (label.AutoSize);
        Assert.Equal (expectedLabelBounds, label.Bounds);

        label.AutoSize = true;

        win.LayoutSubviews ();

        // Here the AutoSize ensuring the right size with width 19 (width of longest line)
        // and height 3 because the text has 3 lines
        expectedLabelBounds = new (0, 0, 19, 3);
        Assert.True (label.AutoSize);
        Assert.Equal (expectedLabelBounds, label.Bounds);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_GetAutoSize_Centered ()
    {
        var text = "This is some text.";
        var view = new View { Text = text, TextAlignment = TextAlignment.Centered, AutoSize = true };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Size size = view.GetAutoSize ();
        Assert.Equal (new (text.Length, 1), size);

        view.Text = $"{text}\n{text}";
        size = view.GetAutoSize ();
        Assert.Equal (new (text.Length, 2), size);

        view.Text = $"{text}\n{text}\n{text}+";
        size = view.GetAutoSize ();
        Assert.Equal (new (text.Length + 1, 3), size);

        text = string.Empty;
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (Size.Empty, size);

        text = "1";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (_size1x1, size);

        text = "界";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (new (2, 1), size);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_GetAutoSize_Horizontal ()
    {
        var text = "text";
        var view = new View { Text = text, AutoSize = true };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Size size = view.GetAutoSize ();
        Assert.Equal (new (text.Length, 1), size);

        view.Text = $"{text}\n{text}";
        size = view.GetAutoSize ();
        Assert.Equal (new (text.Length, 2), size);

        view.Text = $"{text}\n{text}\n{text}+";
        size = view.GetAutoSize ();
        Assert.Equal (new (text.Length + 1, 3), size);

        text = string.Empty;
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (Size.Empty, size);

        text = "1";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (_size1x1, size);

        text = "界";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (new (2, 1), size);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_GetAutoSize_Left ()
    {
        var text = "This is some text.";
        var view = new View { Text = text, TextAlignment = TextAlignment.Left, AutoSize = true };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Size size = view.GetAutoSize ();
        Assert.Equal (new (text.Length, 1), size);

        view.Text = $"{text}\n{text}";
        size = view.GetAutoSize ();
        Assert.Equal (new (text.Length, 2), size);

        view.Text = $"{text}\n{text}\n{text}+";
        size = view.GetAutoSize ();
        Assert.Equal (new (text.Length + 1, 3), size);

        text = string.Empty;
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (Size.Empty, size);

        text = "1";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (_size1x1, size);

        text = "界";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (new (2, 1), size);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_GetAutoSize_Right ()
    {
        var text = "This is some text.";
        var view = new View { Text = text, TextAlignment = TextAlignment.Right, AutoSize = true };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Size size = view.GetAutoSize ();
        Assert.Equal (new (text.Length, 1), size);

        view.Text = $"{text}\n{text}";
        size = view.GetAutoSize ();
        Assert.Equal (new (text.Length, 2), size);

        view.Text = $"{text}\n{text}\n{text}+";
        size = view.GetAutoSize ();
        Assert.Equal (new (text.Length + 1, 3), size);

        text = string.Empty;
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (Size.Empty, size);

        text = "1";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (_size1x1, size);

        text = "界";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (new (2, 1), size);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_GetAutoSize_Vertical ()
    {
        var text = "text";
        var view = new View { Text = text, TextDirection = TextDirection.TopBottom_LeftRight, AutoSize = true };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Size size = view.GetAutoSize ();
        Assert.Equal (new (1, text.Length), size);

        view.Text = $"{text}\n{text}";
        size = view.GetAutoSize ();
        Assert.Equal (new (2, text.Length), size);

        view.Text = $"{text}\n{text}\n{text}+";
        size = view.GetAutoSize ();
        Assert.Equal (new (3, text.Length + 1), size);

        text = string.Empty;
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (Size.Empty, size);

        text = "1";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (_size1x1, size);

        text = "界";
        view.Text = text;
        size = view.GetAutoSize ();
        Assert.Equal (new (2, 1), size);
    }

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_Label_Set_AutoSize_To_False_Height_Positive_Does_Not_Change ()
    {
        var text = "Label";
        var label = new Label { Text = text };
        Assert.Equal ("Absolute(1)", label.Height.ToString ());
        label.AutoSize = false;
        label.Width = Dim.Fill () - text.Length;
        label.Height = 1;
        Assert.Equal ("Absolute(1)", label.Height.ToString ());

        var win = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);
        win.BeginInit ();
        win.EndInit ();
        win.LayoutSubviews ();
        win.Draw ();

        Assert.Equal (5, text.Length);
        Assert.False (label.AutoSize);
        Assert.Equal (new (0, 0, 3, 1), label.Frame);
        Assert.Equal (new (3, 1), label.TextFormatter.Size);
        Assert.Single (label.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), win.Frame);

        var expected = @"
┌────────┐
│Lab     │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);
        label.Width = Dim.Fill () - text.Length;
        win.LayoutSubviews ();
        win.Clear ();
        win.Draw ();

        Assert.Equal (new (0, 0, 0, 1), label.Frame);
        Assert.Equal (new (0, 1), label.TextFormatter.Size);

        Exception exception = Record.Exception (
                                                () => Assert.Equal (
                                                                    new List<string> { string.Empty },
                                                                    label.TextFormatter.GetLines ()
                                                                   )
                                               );
        Assert.Null (exception);

        expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_Stays_True_Center_HotKeySpecifier ()
    {
        var label = new Label { X = Pos.Center (), Y = Pos.Center (), Text = "Say Hello 你" };

        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill (), Title = "Test Demo 你" };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);

        Assert.True (label.AutoSize);

        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        var expected = @"
┌┤Test Demo 你├──────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        Assert.True (label.AutoSize);
        label.Text = "Say Hello 你 changed";
        Assert.True (label.AutoSize);
        Application.Refresh ();

        expected = @"
┌┤Test Demo 你├──────────────┐
│                            │
│    Say Hello 你 changed    │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    public void AutoSize_True_Equal_Before_And_After_IsInitialized_With_Different_Orders ()
    {
        var top = new Toplevel ();

        var view1 = new View
        {
            Text = "Say Hello view1 你", AutoSize = true /*, Width = 10, Height = 5*/, ValidatePosDim = true
        };

        var view2 = new View
        {
            Text = "Say Hello view2 你",
            Width = 10,
            Height = 5,
            AutoSize = true,
            ValidatePosDim = true
        };

        var view3 = new View
        {
            AutoSize = true /*, Width = 10, Height = 5*/, Text = "Say Hello view3 你", ValidatePosDim = true
        };

        var view4 = new View
        {
            Text = "Say Hello view4 你",
            AutoSize = true,

            //Width = 10,
            //Height = 5,
            TextDirection = TextDirection.TopBottom_LeftRight,
            ValidatePosDim = true
        };

        var view5 = new View
        {
            Text = "Say Hello view5 你",
            AutoSize = true,

            //Width = 10,
            //Height = 5,
            TextDirection = TextDirection.TopBottom_LeftRight,
            ValidatePosDim = true
        };

        var view6 = new View
        {
            AutoSize = true,

            //Width = 10,
            //Height = 5,
            TextDirection = TextDirection.TopBottom_LeftRight,
            Text = "Say Hello view6 你",
            ValidatePosDim = true
        };
        top.Add (view1, view2, view3, view4, view5, view6);

        Assert.False (view1.IsInitialized);
        Assert.False (view2.IsInitialized);
        Assert.False (view3.IsInitialized);
        Assert.False (view4.IsInitialized);
        Assert.False (view5.IsInitialized);
        Assert.True (view1.AutoSize);
        Assert.Equal (new (0, 0, 18, 1), view1.Frame);
        Assert.Equal ("Absolute(18)", view1.Width.ToString ());
        Assert.Equal ("Absolute(1)", view1.Height.ToString ());
        Assert.True (view2.AutoSize);
        Assert.Equal ("Say Hello view2 你".GetColumns (), view2.Width);
        Assert.Equal (18, view2.Width);
        Assert.Equal (new (0, 0, 18, 5), view2.Frame);
        Assert.Equal ("Absolute(18)", view2.Width.ToString ());
        Assert.Equal ("Absolute(5)", view2.Height.ToString ());
        Assert.True (view3.AutoSize);
        Assert.Equal (new (0, 0, 18, 1), view3.Frame); // BUGBUG: AutoSize = true, so the height should be 1.
        Assert.Equal ("Absolute(18)", view2.Width.ToString ());
        Assert.Equal ("Absolute(1)", view3.Height.ToString ());
        Assert.True (view4.AutoSize);

        Assert.Equal ("Say Hello view4 你".GetColumns (), view2.Width);
        Assert.Equal (18, view2.Width);

        Assert.Equal (new (0, 0, 18, 17), view4.Frame);
        Assert.Equal ("Absolute(18)", view4.Width.ToString ());
        Assert.Equal ("Absolute(17)", view4.Height.ToString ());
        Assert.True (view5.AutoSize);
        Assert.Equal (new (0, 0, 18, 17), view5.Frame);
        Assert.True (view6.AutoSize);
        Assert.Equal (new (0, 0, 2, 17), view6.Frame); // BUGBUG: AutoSize = true, so the Width should be 2.

        top.BeginInit ();
        top.EndInit ();

        Assert.True (view1.IsInitialized);
        Assert.True (view2.IsInitialized);
        Assert.True (view3.IsInitialized);
        Assert.True (view4.IsInitialized);
        Assert.True (view5.IsInitialized);
        Assert.True (view1.AutoSize);
        Assert.Equal (new (0, 0, 18, 1), view1.Frame);
        Assert.Equal ("Absolute(18)", view1.Width.ToString ());
        Assert.Equal ("Absolute(1)", view1.Height.ToString ());
        Assert.True (view2.AutoSize);

        Assert.Equal (new (0, 0, 18, 5), view2.Frame);
        Assert.Equal ("Absolute(18)", view2.Width.ToString ());
        Assert.Equal ("Absolute(5)", view2.Height.ToString ());
        Assert.True (view3.AutoSize);
        Assert.Equal (new (0, 0, 18, 1), view3.Frame); // BUGBUG: AutoSize = true, so the height should be 1.
        Assert.Equal ("Absolute(18)", view5.Width.ToString ());
        Assert.Equal ("Absolute(1)", view3.Height.ToString ());
        Assert.True (view4.AutoSize);
        Assert.Equal (new (0, 0, 18, 17), view4.Frame);
        Assert.Equal ("Absolute(18)", view5.Width.ToString ());
        Assert.Equal ("Absolute(17)", view4.Height.ToString ());
        Assert.True (view5.AutoSize);
        Assert.Equal (new (0, 0, 18, 17), view5.Frame);
        Assert.Equal ("Absolute(18)", view5.Width.ToString ());
        Assert.Equal ("Absolute(17)", view5.Height.ToString ());
        Assert.True (view6.AutoSize);
        Assert.Equal (new (0, 0, 2, 17), view6.Frame); // BUGBUG: AutoSize = true, so the Width should be 2.
        Assert.Equal ("Absolute(2)", view6.Width.ToString ());
        Assert.Equal ("Absolute(17)", view6.Height.ToString ());
    }

    [Fact]
    public void AutoSize_True_Label_If_Text_Empty ()
    {
        var label1 = new Label ();
        var label3 = new Label { Text = "" };

        Assert.True (label1.AutoSize);
        Assert.True (label3.AutoSize);
        label1.Dispose ();
        label3.Dispose ();
    }

    [Fact]
    public void AutoSize_True_Label_If_Text_Is_Not_Empty ()
    {
        var label1 = new Label ();
        label1.Text = "Hello World";
        var label3 = new Label { Text = "Hello World" };

        Assert.True (label1.AutoSize);
        Assert.True (label3.AutoSize);
        label1.Dispose ();
        label3.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_Label_IsEmpty_False_Minimum_Height ()
    {
        var text = "Label";

        var label = new Label
        {
            //Width = Dim.Fill () - text.Length,
            Text = text
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Assert.Equal (5, text.Length);
        Assert.True (label.AutoSize);
        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Equal (["Label"], label.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), win.Frame);
        Assert.Equal (new (0, 0, 10, 4), Application.Top.Frame);

        var expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //label.Width = Dim.Fill () - text.Length;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Exception exception = Record.Exception (() => Assert.Single (label.TextFormatter.GetLines ()));
        Assert.Null (exception);

        expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_Label_IsEmpty_False_Never_Return_Null_Lines ()
    {
        var text = "Label";

        var label = new Label
        {
            //Width = Dim.Fill () - text.Length,
            //Height = 1,
            Text = text
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);

        Assert.Equal (5, text.Length);
        Assert.True (label.AutoSize);
        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Equal (["Label"], label.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), win.Frame);
        Assert.Equal (new (0, 0, 10, 4), Application.Top.Frame);

        var expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //label.Width = Dim.Fill () - text.Length;
        Application.Refresh ();

        Assert.True (label.AutoSize);
        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Single (label.TextFormatter.GetLines ());

        expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    public void AutoSize_True_ResizeView_With_Dim_Absolute ()
    {
        var super = new View ();
        var label = new Label ();

        label.Text = "New text";
        super.Add (label);
        super.LayoutSubviews ();

        Assert.True (label.AutoSize);
        Rectangle expectedLabelBounds = new (0, 0, 8, 1);
        Assert.Equal (expectedLabelBounds, label.Bounds);
        super.Dispose ();
    }

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_True_Setting_With_Height_Horizontal ()
    {
        var top = new View { Width = 25, Height = 25 };

        var label = new Label { Text = "Hello", /* Width = 10, Height = 2, */ ValidatePosDim = true };
        var viewX = new View { Text = "X", X = Pos.Right (label), Width = 1, Height = 1 };
        var viewY = new View { Text = "Y", Y = Pos.Bottom (label), Width = 1, Height = 1 };

        top.Add (label, viewX, viewY);
        top.BeginInit ();
        top.EndInit ();

        Assert.True (label.AutoSize);
        Assert.Equal (new (0, 0, 5, 1), label.Frame);

        top.LayoutSubviews ();
        top.Draw ();

        var expected = @"
HelloX
Y     
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        label.AutoSize = false;
        label.Width = 10;
        label.Height = 2;
        Assert.False (label.AutoSize);
        Assert.Equal (new (0, 0, 10, 2), label.Frame);

        top.LayoutSubviews ();
        top.Draw ();

        expected = @"
Hello     X
           
Y          
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_Setting_With_Height_Vertical ()
    {
        // BUGBUG: Label is AutoSize = true, so Width & Height are ignored
        var label = new Label
        { /*Width = 2, Height = 10, */
            TextDirection = TextDirection.TopBottom_LeftRight, ValidatePosDim = true
        };
        var viewX = new View { Text = "X", X = Pos.Right (label), Width = 1, Height = 1 };
        var viewY = new View { Text = "Y", Y = Pos.Bottom (label), Width = 1, Height = 1 };

        var top = new Toplevel ();
        top.Add (label, viewX, viewY);
        RunState rs = Application.Begin (top);

        Assert.True (label.AutoSize);
        label.Text = "Hello";
        Application.Refresh ();

        Assert.Equal (new (0, 0, 1, 5), label.Frame); // BUGBUG: AutoSize = true, so the Width should be 1.

        var expected = @"
HX
e 
l 
l 
o 
Y 
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        label.AutoSize = false;
        label.Width = 2;
        label.Height = 10;
        Application.Refresh ();

        Assert.False (label.AutoSize);
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_TextDirection_Toggle ()
    {
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };

        // View is AutoSize == true
        var view = new View ();
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);

        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (15, 15);

        Assert.Equal (new Rectangle (0, 0, 15, 15), win.Frame);
        Assert.Equal (new Rectangle (0, 0, 15, 15), win.Margin.Frame);
        Assert.Equal (new Rectangle (0, 0, 15, 15), win.Border.Frame);
        Assert.Equal (new Rectangle (1, 1, 13, 13), win.Padding.Frame);
        Assert.False (view.AutoSize);
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

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        view.Text = "Hello World";
        view.Width = 11;
        view.Height = 1;
        win.LayoutSubviews ();
        Application.Refresh ();

        Assert.Equal (new Rectangle (0, 0, 11, 1), view.Frame);
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        view.AutoSize = true;
        view.Text = "Hello Worlds";
        Application.Refresh ();
        int len = "Hello Worlds".Length;
        Assert.Equal (12, len);
        Assert.Equal (new Rectangle (0, 0, len, 1), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(12)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());

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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        view.TextDirection = TextDirection.TopBottom_LeftRight;
        Application.Refresh ();

        Assert.Equal (new Rectangle (0, 0, 12, 12), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(12)", view.Width.ToString ());
        Assert.Equal ("Absolute(12)", view.Height.ToString ());

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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        view.AutoSize = false;
        view.Height = 1;
        Application.Refresh ();

        Assert.Equal (new Rectangle (0, 0, 12, 1), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(12)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());

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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        view.PreserveTrailingSpaces = true;
        Application.Refresh ();

        Assert.Equal (new Rectangle (0, 0, 12, 1), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(12)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());

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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        view.PreserveTrailingSpaces = false;
        Rectangle f = view.Frame;
        view.Width = f.Height;
        view.Height = f.Width;
        view.TextDirection = TextDirection.TopBottom_LeftRight;
        Application.Refresh ();

        Assert.Equal (new Rectangle (0, 0, 1, 12), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(1)", view.Width.ToString ());
        Assert.Equal ("Absolute(12)", view.Height.ToString ());

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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        view.AutoSize = true;
        Application.Refresh ();

        Assert.Equal (new Rectangle (0, 0, 1, 12), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(1)", view.Width.ToString ());
        Assert.Equal ("Absolute(12)", view.Height.ToString ());

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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
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
            Text = text,
            AutoSize = true
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (4, 10);

        Assert.Equal (5, text.Length);
        Assert.True (view.AutoSize);
        Assert.Equal (new (0, 0, 1, 5), view.Frame);
        Assert.Equal (new (1, 5), view.TextFormatter.Size);
        Assert.Equal (new List<string> { "Views" }, view.TextFormatter.GetLines ());
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

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 4, 10), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_View_IsEmpty_False_Minimum_Width_Wide_Rune ()
    {
        var text = "界View";

        var view = new View
        {
            TextDirection = TextDirection.TopBottom_LeftRight,
            Height = Dim.Fill () - text.Length,
            Text = text,
            AutoSize = true
        };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (view);
        var top = new Toplevel ();
        top.Add (win);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (4, 10);

        Assert.Equal (5, text.Length);
        Assert.True (view.AutoSize);
        Assert.Equal (new (0, 0, 2, 5), view.Frame);
        Assert.Equal (new (2, 5), view.TextFormatter.Size);
        Assert.Equal (new List<string> { "界View" }, view.TextFormatter.GetLines ());
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

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 4, 10), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //view.Height = Dim.Fill () - text.Length;
        Application.Refresh ();

        Assert.Equal (new (0, 0, 2, 5), view.Frame);
        Assert.Equal (new (2, 5), view.TextFormatter.Size);

        Exception exception = Record.Exception (
                                                () => Assert.Equal (
                                                                    new List<string> { "界View" },
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rectangle (0, 0, 4, 10), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_Width_Height_SetMinWidthHeight_Narrow_Wide_Runes ()
    {
        var text = $"First line{Environment.NewLine}Second line";

        var horizontalView = new View
        {
            AutoSize = true,

            //Width = 20,  // BUGBUG: These are ignored
            //Height = 1,  // BUGBUG: These are ignored
            Text = text
        };

        var verticalView = new View
        {
            AutoSize = true,
            Y = 3,

            //Height = 20, // BUGBUG: These are ignored
            //Width = 1,   // BUGBUG: These are ignored
            Text = text,
            TextDirection = TextDirection.TopBottom_LeftRight
        };

        var win = new Window
        {
            AutoSize = true,

            //Width = Dim.Fill (), // BUGBUG: These are ignored
            //Height = Dim.Fill (),// BUGBUG: These are ignored
            Text = "Window"
        };
        win.Add (horizontalView, verticalView);
        var top = new Toplevel ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (20, 20);

        Assert.True (horizontalView.AutoSize);
        Assert.True (verticalView.AutoSize);
        Assert.Equal (new Rectangle (0, 0, 11, 2), horizontalView.Frame);
        Assert.Equal (new Rectangle (0, 3, 11, 11), verticalView.Frame);

        var expected = @"
┌──────────────────┐
│First line        │
│Second line       │
│                  │
│FS                │
│ie                │
│rc                │
│so                │
│tn                │
│ d                │
│l                 │
│il                │
│ni                │
│en                │
│ e                │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        verticalView.Text = $"最初の行{Environment.NewLine}二行目";
        Application.Top.Draw ();
        Assert.Equal (new Rectangle (0, 3, 11, 11), verticalView.Frame);

        expected = @"
┌──────────────────┐
│First line        │
│Second line       │
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_Width_Height_Stay_True_If_TextFormatter_Size_Fit ()
    {
        var text = "Fi_nish 終";

        var horizontalView = new View
        {
            Id = "horizontalView", AutoSize = true, Text = text
        };

        var verticalView = new View
        {
            Id = "verticalView",
            Y = 3,
            AutoSize = true,
            Text = text,
            TextDirection = TextDirection.TopBottom_LeftRight
        };
        var win = new Window { Id = "win", Width = Dim.Fill (), Height = Dim.Fill (), Text = "Window" };
        win.Add (horizontalView, verticalView);
        var top = new Toplevel ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (22, 22);

        Assert.True (horizontalView.AutoSize);
        Assert.True (verticalView.AutoSize);
        Assert.Equal (new (text.GetColumns (), 1), horizontalView.TextFormatter.Size);
        Assert.Equal (new (2, 9), verticalView.TextFormatter.Size);
        Assert.Equal (new (0, 0, 10, 1), horizontalView.Frame);
        Assert.Equal (new (0, 3, 10, 9), verticalView.Frame);

        var expected = @"
┌────────────────────┐
│Fi_nish 終          │
│                    │
│                    │
│F                   │
│i                   │
│_                   │
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
└────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        verticalView.Text = "最初_の行二行目";
        Application.Top.Draw ();
        Assert.True (horizontalView.AutoSize);
        Assert.True (verticalView.AutoSize);

        // height was initialized with 8 and can only grow or keep initial value
        Assert.Equal (new Rectangle (0, 3, 10, 9), verticalView.Frame);

        expected = @"
┌────────────────────┐
│Fi_nish 終          │
│                    │
│                    │
│最                  │
│初                  │
│_                   │
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
└────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Application.End (rs);
    }

    // Moved from DimTests.cs - This is really a bogus test
    [Theory]
    [AutoInitShutdown]
    [InlineData (0, true)]
    [InlineData (0, false)]
    [InlineData (50, true)]
    [InlineData (50, false)]
    public void DimPercentPlusOne (int startingDistance, bool testHorizontal)
    {
        var container = new View { Width = 100, Height = 100 };

        var label = new Label
        {
            X = testHorizontal ? startingDistance : 0, Y = testHorizontal ? 0 : startingDistance

            //Width = testHorizontal ? Dim.Percent (50) + 1 : 1,
            //Height = testHorizontal ? 1 : Dim.Percent (50) + 1
        };

        container.Add (label);
        var top = new Toplevel ();
        top.Add (container);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubviews ();

        Assert.Equal (100, container.Frame.Width);
        Assert.Equal (100, container.Frame.Height);

        if (testHorizontal)
        {
            Assert.Equal (0, label.Frame.Width); // BUGBUG: Shoudl be 0 not since there's no text
            Assert.Equal (0, label.Frame.Height);
        }
        else
        {
            Assert.Equal (0, label.Frame.Width); // BUGBUG: Shoudl be 0 not since there's no text
            Assert.Equal (0, label.Frame.Height);
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void Excess_Text_Is_Erased_When_The_Width_Is_Reduced ()
    {
        var lbl = new Label { Text = "123" };
        var top = new Toplevel ();
        top.Add (lbl);
        RunState rs = Application.Begin (top);

        Assert.True (lbl.AutoSize);
        Assert.Equal ("123 ", GetContents ());

        lbl.Text = "12";

        // Here the AutoSize ensuring the right size with width 3 (Dim.Absolute)
        // that was set on the OnAdded method with the text length of 3
        // and height 1 because wasn't set and the text has 1 line
        Assert.Equal (new Rectangle (0, 0, 3, 1), lbl.Frame);
        Assert.Equal (new Rectangle (0, 0, 3, 1), lbl._needsDisplayRect);
        Assert.Equal (new Rectangle (0, 0, 0, 0), lbl.SuperView._needsDisplayRect);
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
    }

    [Fact]
    public void GetCurrentHeight_TrySetHeight ()
    {
        var top = new View { X = 0, Y = 0, Height = 20 };

        var v = new View { Height = Dim.Fill (), ValidatePosDim = true };
        top.Add (v);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubviews ();

        Assert.False (v.AutoSize);
        Assert.False (v.TrySetHeight (0, out _));
        Assert.Equal (20, v.Frame.Height);

        v.Height = Dim.Fill (1);
        top.LayoutSubviews ();

        Assert.False (v.TrySetHeight (0, out _));
        Assert.True (v.Height is Dim.DimFill);
        Assert.Equal (19, v.Frame.Height);

        v.AutoSize = true;
        top.LayoutSubviews ();

        Assert.True (v.TrySetHeight (0, out _));
        Assert.True (v.Height is Dim.DimAbsolute);
        Assert.Equal (0, v.Frame.Height); // No text, so height is 0
        top.Dispose ();
    }

    [Fact]
    [TestRespondersDisposed]
    public void GetCurrentWidth_TrySetWidth ()
    {
        var top = new View { X = 0, Y = 0, Width = 80 };

        var v = new View { Width = Dim.Fill (), ValidatePosDim = true };
        top.Add (v);
        top.BeginInit ();
        top.EndInit ();
        top.LayoutSubviews ();

        Assert.False (v.AutoSize);
        Assert.False (v.TrySetWidth (0, out _));
        Assert.True (v.Width is Dim.DimFill);
        Assert.Equal (80, v.Frame.Width);

        v.Width = Dim.Fill (1);
        top.LayoutSubviews ();

        Assert.False (v.TrySetWidth (0, out _));
        Assert.True (v.Width is Dim.DimFill);
        Assert.Equal (79, v.Frame.Width);

        v.AutoSize = true;
        top.LayoutSubviews ();

        Assert.True (v.TrySetWidth (0, out _));
        Assert.True (v.Width is Dim.DimAbsolute);
        Assert.Equal (0, v.Frame.Width); // No text, so width is 0
        top.Dispose ();
    }

//    [Fact]
//    [AutoInitShutdown]
//    public void AutoSize_False_TextDirection_Toggle ()
//    {
//        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
//        // View is AutoSize == true
//        var view = new View ();
//        win.Add (view);
//        var top = new Toplevel ();
//        top.Add (win);

//        var rs = Application.Begin (top);
//        ((FakeDriver)Application.Driver).SetBufferSize (22, 22);

//        Assert.Equal (new (0, 0, 22, 22), win.Frame);
//        Assert.Equal (new (0, 0, 22, 22), win.Margin.Frame);
//        Assert.Equal (new (0, 0, 22, 22), win.Border.Frame);
//        Assert.Equal (new (1, 1, 20, 20), win.Padding.Frame);
//        Assert.False (view.AutoSize);
//        Assert.Equal (TextDirection.LeftRight_TopBottom, view.TextDirection);
//        Assert.Equal (Rectangle.Empty, view.Frame);
//        Assert.Equal ("Absolute(0)", view.X.ToString ());
//        Assert.Equal ("Absolute(0)", view.Y.ToString ());
//        Assert.Equal ("Absolute(0)", view.Width.ToString ());
//        Assert.Equal ("Absolute(0)", view.Height.ToString ());
//        var expected = @"
//┌────────────────────┐
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//└────────────────────┘";

//        var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//        Assert.Equal (new (0, 0, 22, 22), pos);

//        view.Text = "Hello World";
//        view.Width = 11;
//        view.Height = 1;
//        win.LayoutSubviews ();
//        Application.Refresh ();

//        Assert.Equal (new (0, 0, 11, 1), view.Frame);
//        Assert.Equal ("Absolute(0)", view.X.ToString ());
//        Assert.Equal ("Absolute(0)", view.Y.ToString ());
//        Assert.Equal ("Absolute(11)", view.Width.ToString ());
//        Assert.Equal ("Absolute(1)", view.Height.ToString ());
//        expected = @"
//┌────────────────────┐
//│Hello World         │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//└────────────────────┘";

//        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//        Assert.Equal (new (0, 0, 22, 22), pos);

//        view.AutoSize = true;
//        view.Text = "Hello Worlds";
//        Application.Refresh ();

//        Assert.Equal (new (0, 0, 12, 1), view.Frame);
//        Assert.Equal ("Absolute(0)", view.X.ToString ());
//        Assert.Equal ("Absolute(0)", view.Y.ToString ());
//        Assert.Equal ("Absolute(11)", view.Width.ToString ());
//        Assert.Equal ("Absolute(1)", view.Height.ToString ());
//        expected = @"
//┌────────────────────┐
//│Hello Worlds        │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//└────────────────────┘";

//        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//        Assert.Equal (new (0, 0, 22, 22), pos);

//        view.TextDirection = TextDirection.TopBottom_LeftRight;
//        Application.Refresh ();

//        Assert.Equal (new (0, 0, 11, 12), view.Frame);
//        Assert.Equal ("Absolute(0)", view.X.ToString ());
//        Assert.Equal ("Absolute(0)", view.Y.ToString ());
//        Assert.Equal ("Absolute(11)", view.Width.ToString ());
//        Assert.Equal ("Absolute(1)", view.Height.ToString ());
//        expected = @"
//┌────────────────────┐
//│H                   │
//│e                   │
//│l                   │
//│l                   │
//│o                   │
//│                    │
//│W                   │
//│o                   │
//│r                   │
//│l                   │
//│d                   │
//│s                   │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//└────────────────────┘";

//        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//        Assert.Equal (new (0, 0, 22, 22), pos);

//        view.AutoSize = false;
//        view.Height = 1;
//        Application.Refresh ();

//        Assert.Equal (new (0, 0, 11, 1), view.Frame);
//        Assert.Equal ("Absolute(0)", view.X.ToString ());
//        Assert.Equal ("Absolute(0)", view.Y.ToString ());
//        Assert.Equal ("Absolute(11)", view.Width.ToString ());
//        Assert.Equal ("Absolute(1)", view.Height.ToString ());
//        expected = @"
//┌────────────────────┐
//│HelloWorlds         │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//└────────────────────┘";

//        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//        Assert.Equal (new (0, 0, 22, 22), pos);

//        view.PreserveTrailingSpaces = true;
//        Application.Refresh ();

//        Assert.Equal (new (0, 0, 11, 1), view.Frame);
//        Assert.Equal ("Absolute(0)", view.X.ToString ());
//        Assert.Equal ("Absolute(0)", view.Y.ToString ());
//        Assert.Equal ("Absolute(11)", view.Width.ToString ());
//        Assert.Equal ("Absolute(1)", view.Height.ToString ());
//        expected = @"
//┌────────────────────┐
//│Hello World         │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//└────────────────────┘";

//        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//        Assert.Equal (new (0, 0, 22, 22), pos);

//        view.PreserveTrailingSpaces = false;
//        var f = view.Frame;
//        view.Width = f.Height;
//        view.Height = f.Width;
//        view.TextDirection = TextDirection.TopBottom_LeftRight;
//        Application.Refresh ();

//        Assert.Equal (new (0, 0, 1, 11), view.Frame);
//        Assert.Equal ("Absolute(0)", view.X.ToString ());
//        Assert.Equal ("Absolute(0)", view.Y.ToString ());
//        Assert.Equal ("Absolute(1)", view.Width.ToString ());
//        Assert.Equal ("Absolute(11)", view.Height.ToString ());
//        expected = @"
//┌────────────────────┐
//│H                   │
//│e                   │
//│l                   │
//│l                   │
//│o                   │
//│                    │
//│W                   │
//│o                   │
//│r                   │
//│l                   │
//│d                   │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//└────────────────────┘";

//        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//        Assert.Equal (new (0, 0, 22, 22), pos);

//        view.AutoSize = true;
//        Application.Refresh ();

//        Assert.Equal (new (0, 0, 1, 12), view.Frame);
//        Assert.Equal ("Absolute(0)", view.X.ToString ());
//        Assert.Equal ("Absolute(0)", view.Y.ToString ());
//        Assert.Equal ("Absolute(1)", view.Width.ToString ());
//        Assert.Equal ("Absolute(12)", view.Height.ToString ());
//        expected = @"
//┌────────────────────┐
//│H                   │
//│e                   │
//│l                   │
//│l                   │
//│o                   │
//│                    │
//│W                   │
//│o                   │
//│r                   │
//│l                   │
//│d                   │
//│s                   │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//│                    │
//└────────────────────┘";

//        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
//        Assert.Equal (new (0, 0, 22, 22), pos);
//        Application.End (rs);
//    }

    [Fact]
    [AutoInitShutdown]
    public void GetTextFormatterBoundsSize_GetSizeNeededForText_HotKeySpecifier ()
    {
        var text = "Say Hello 你";

        // Frame: 0, 0, 12, 1
        var horizontalView = new View { AutoSize = true };
        horizontalView.TextFormatter.HotKeySpecifier = (Rune)'_';
        horizontalView.Text = text;

        // Frame: 0, 0, 1, 12
        var verticalView = new View
        {
            AutoSize = true, TextDirection = TextDirection.TopBottom_LeftRight
        };
        verticalView.Text = text;
        verticalView.TextFormatter.HotKeySpecifier = (Rune)'_';

        var top = new Toplevel ();
        top.Add (horizontalView, verticalView);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (50, 50);

        Assert.True (horizontalView.AutoSize);
        Assert.Equal (new (0, 0, 12, 1), horizontalView.Frame);
        Assert.Equal (new (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

        Assert.True (verticalView.AutoSize);
        Assert.Equal (new (0, 0, 2, 11), verticalView.Frame);
        Assert.Equal (new (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());

        text = "Say He_llo 你";
        horizontalView.Text = text;
        verticalView.Text = text;

        Assert.True (horizontalView.AutoSize);
        Assert.Equal (new (0, 0, 12, 1), horizontalView.Frame);
        Assert.Equal (new (12, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

        Assert.True (verticalView.AutoSize);
        Assert.Equal (new (0, 0, 2, 11), verticalView.Frame);
        Assert.Equal (new (2, 11), verticalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());
    }

    [Fact]
    public void SetRelativeLayout_Respects_AutoSize ()
    {
        var view = new View { Frame = new (0, 0, 10, 0), AutoSize = true };
        view.Text = "01234567890123456789";

        Assert.True (view.AutoSize);
        Assert.Equal (LayoutStyle.Absolute, view.LayoutStyle);
        Assert.Equal (new (0, 0, 20, 1), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(20)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());

        view.SetRelativeLayout (new (0, 0, 25, 5));

        Assert.True (view.AutoSize);
        Assert.Equal (LayoutStyle.Absolute, view.LayoutStyle);
        Assert.Equal (new (0, 0, 20, 1), view.Frame);
        Assert.Equal ("Absolute(0)", view.X.ToString ());
        Assert.Equal ("Absolute(0)", view.Y.ToString ());
        Assert.Equal ("Absolute(20)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());
    }

    [Fact]
    [AutoInitShutdown]
    public void Setting_Frame_Dont_Respect_AutoSize_True_On_Layout_Absolute ()
    {
        var view1 = new View { Frame = new (0, 0, 10, 0), Text = "Say Hello view1 你", AutoSize = true };

        var viewTopBottom_LeftRight = new View
        {
            Frame = new (0, 0, 0, 10),
            Text = "Say Hello view2 你",
            AutoSize = true,
            TextDirection =
                TextDirection.TopBottom_LeftRight
        };
        var top = new Toplevel ();
       top.Add (view1, viewTopBottom_LeftRight);

        RunState rs = Application.Begin (top);

        Assert.True (view1.AutoSize);
        Assert.Equal (LayoutStyle.Absolute, view1.LayoutStyle);
        Assert.Equal (new (0, 0, 18, 1), view1.Frame);
        Assert.Equal ("Absolute(0)", view1.X.ToString ());
        Assert.Equal ("Absolute(0)", view1.Y.ToString ());
        Assert.Equal ("Absolute(18)", view1.Width.ToString ());
        Assert.Equal ("Absolute(1)", view1.Height.ToString ());

        Assert.True (viewTopBottom_LeftRight.AutoSize);
        Assert.Equal (LayoutStyle.Absolute, viewTopBottom_LeftRight.LayoutStyle);
        Assert.Equal (new (0, 0, 18, 17), viewTopBottom_LeftRight.Frame);
        Assert.Equal ("Absolute(0)", viewTopBottom_LeftRight.X.ToString ());
        Assert.Equal ("Absolute(0)", viewTopBottom_LeftRight.Y.ToString ());
        Assert.Equal ("Absolute(18)", viewTopBottom_LeftRight.Width.ToString ());
        Assert.Equal ("Absolute(17)", viewTopBottom_LeftRight.Height.ToString ());

        view1.Frame = new (0, 0, 25, 4);
        var firstIteration = false;
        Application.RunIteration (ref rs, ref firstIteration);

        Assert.True (view1.AutoSize);
        Assert.Equal (LayoutStyle.Absolute, view1.LayoutStyle);
        Assert.Equal (new (0, 0, 25, 4), view1.Frame);
        Assert.Equal ("Absolute(0)", view1.X.ToString ());
        Assert.Equal ("Absolute(0)", view1.Y.ToString ());
        Assert.Equal ("Absolute(25)", view1.Width.ToString ());
        Assert.Equal ("Absolute(4)", view1.Height.ToString ());

        viewTopBottom_LeftRight.Frame = new (0, 0, 1, 25);
        Application.RunIteration (ref rs, ref firstIteration);

        Assert.True (viewTopBottom_LeftRight.AutoSize);
        Assert.Equal (LayoutStyle.Absolute, viewTopBottom_LeftRight.LayoutStyle);
        Assert.Equal (new (0, 0, 2, 25), viewTopBottom_LeftRight.Frame);
        Assert.Equal ("Absolute(0)", viewTopBottom_LeftRight.X.ToString ());
        Assert.Equal ("Absolute(0)", viewTopBottom_LeftRight.Y.ToString ());
        Assert.Equal ("Absolute(2)", viewTopBottom_LeftRight.Width.ToString ());
        Assert.Equal ("Absolute(25)", viewTopBottom_LeftRight.Height.ToString ());
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void TrySetHeight_ForceValidatePosDim ()
    {
        var top = new View { X = 0, Y = 0, Height = 20 };

        var v = new View { Height = Dim.Fill (), ValidatePosDim = true };
        top.Add (v);

        Assert.False (v.TrySetHeight (10, out int rHeight));
        Assert.Equal (10, rHeight);

        v.Height = Dim.Fill (1);
        Assert.False (v.TrySetHeight (10, out rHeight));
        Assert.Equal (9, rHeight);

        v.Height = 0;
        Assert.True (v.TrySetHeight (10, out rHeight));
        Assert.Equal (10, rHeight);
        Assert.False (v.IsInitialized);

        var toplevel = new Toplevel ();
        toplevel.Add (top);
        Application.Begin (toplevel);

        Assert.True (v.IsInitialized);

        v.Height = 15;
        Assert.True (v.TrySetHeight (5, out rHeight));
        Assert.Equal (5, rHeight);
    }

    [Fact]
    [AutoInitShutdown]
    public void TrySetWidth_ForceValidatePosDim ()
    {
        var top = new View { X = 0, Y = 0, Width = 80 };

        var v = new View { Width = Dim.Fill (), ValidatePosDim = true };
        top.Add (v);

        Assert.False (v.TrySetWidth (70, out int rWidth));
        Assert.Equal (70, rWidth);

        v.Width = Dim.Fill (1);
        Assert.False (v.TrySetWidth (70, out rWidth));
        Assert.Equal (69, rWidth);

        v.Width = 0;
        Assert.True (v.TrySetWidth (70, out rWidth));
        Assert.Equal (70, rWidth);
        Assert.False (v.IsInitialized);

        var toplevel = new Toplevel ();
        toplevel.Add (top);
        Application.Begin (toplevel);

        Assert.True (v.IsInitialized);
        v.Width = 75;
        Assert.True (v.TrySetWidth (60, out rWidth));
        Assert.Equal (60, rWidth);
    }

    [Theory]
    [AutoInitShutdown]
    [InlineData (true)]
    [InlineData (false)]
    public void View_Draw_Horizontal_Simple_TextAlignments (bool autoSize)
    {
        var text = "Hello World";
        var width = 20;
        var lblLeft = new View { Text = text, Width = width, Height = 1, AutoSize = autoSize };

        var lblCenter = new View
        {
            Text = text,
            Y = 1,
            Width = width,
            Height = 1,
            TextAlignment = TextAlignment.Centered,
            AutoSize = autoSize
        };

        var lblRight = new View
        {
            Text = text,
            Y = 2,
            Width = width,
            Height = 1,
            TextAlignment = TextAlignment.Right,
            AutoSize = autoSize
        };

        var lblJust = new View
        {
            Text = text,
            Y = 3,
            Width = width,
            Height = 1,
            TextAlignment = TextAlignment.Justified,
            AutoSize = autoSize
        };
        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
        frame.Add (lblLeft, lblCenter, lblRight, lblJust);
        var top = new Toplevel ();
        top.Add (frame);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (width + 2, 6);

        Assert.True (lblLeft.AutoSize == autoSize);
        Assert.True (lblCenter.AutoSize == autoSize);
        Assert.True (lblRight.AutoSize == autoSize);
        Assert.True (lblJust.AutoSize == autoSize);
        Assert.True (lblLeft.TextFormatter.AutoSize == autoSize);
        Assert.True (lblCenter.TextFormatter.AutoSize == autoSize);
        Assert.True (lblRight.TextFormatter.AutoSize == autoSize);
        Assert.True (lblJust.TextFormatter.AutoSize == autoSize);

        if (autoSize)
        {
            Size expectedSize = new (11, 1);
            Assert.Equal (expectedSize, lblLeft.TextFormatter.Size);
            Assert.Equal (expectedSize, lblCenter.TextFormatter.Size);
            Assert.Equal (expectedSize, lblRight.TextFormatter.Size);
            expectedSize = new (width, 1);
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

        var expected = @"
┌────────────────────┐
│Hello World         │
│    Hello World     │
│         Hello World│
│Hello          World│
└────────────────────┘
"
            ;

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, width + 2, 6), pos);
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
            TextDirection = TextDirection.TopBottom_LeftRight,
            AutoSize = autoSize
        };

        var lblCenter = new View
        {
            Text = text,
            X = 2,
            Width = 1,
            Height = height,
            TextDirection = TextDirection.TopBottom_LeftRight,
            AutoSize = autoSize,
            VerticalTextAlignment = VerticalTextAlignment.Middle
        };

        var lblRight = new View
        {
            Text = text,
            X = 4,
            Width = 1,
            Height = height,
            TextDirection = TextDirection.TopBottom_LeftRight,
            AutoSize = autoSize,
            VerticalTextAlignment = VerticalTextAlignment.Bottom
        };

        var lblJust = new View
        {
            Text = text,
            X = 6,
            Width = 1,
            Height = height,
            TextDirection = TextDirection.TopBottom_LeftRight,
            AutoSize = autoSize,
            VerticalTextAlignment = VerticalTextAlignment.Justified
        };
        var frame = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };

        frame.Add (lblLeft, lblCenter, lblRight, lblJust);
        var top = new Toplevel ();
        top.Add (frame);
        Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (9, height + 2);

        Assert.True (lblLeft.AutoSize == autoSize);
        Assert.True (lblCenter.AutoSize == autoSize);
        Assert.True (lblRight.AutoSize == autoSize);
        Assert.True (lblJust.AutoSize == autoSize);
        Assert.True (lblLeft.TextFormatter.AutoSize == autoSize);
        Assert.True (lblCenter.TextFormatter.AutoSize == autoSize);
        Assert.True (lblRight.TextFormatter.AutoSize == autoSize);
        Assert.True (lblJust.TextFormatter.AutoSize == autoSize);

        if (autoSize)
        {
            Assert.Equal (new (1, 11), lblLeft.TextFormatter.Size);
            Assert.Equal (new (1, 11), lblCenter.TextFormatter.Size);
            Assert.Equal (new (1, 11), lblRight.TextFormatter.Size);
            Assert.Equal (new (1, height), lblJust.TextFormatter.Size);
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

        var expected = @"
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
"
            ;

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new (0, 0, 9, height + 2), pos);
    }
}
