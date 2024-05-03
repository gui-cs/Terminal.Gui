using System.Reflection.Emit;
using System.Text;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>Tests of the View.AutoSize property which auto sizes Views based on <see cref="Text"/>.</summary>
public class AutoSizeTrueTests (ITestOutputHelper output)
{
    private readonly string [] expecteds = new string [21]
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

    // TODO: This is a Label test. Move to label tests if there's not already a test for this.
    [Fact]
    [AutoInitShutdown]
    public void AutoSize_AnchorEnd_Better_Than_Bottom_Equal_Inside_Window ()
    {
        var win = new Window ();

        var label = new Label
        {
            Text = "This should be the last line.",
            ColorScheme = Colors.ColorSchemes ["Menu"],

            //Width = Dim.Fill (),
            X = 0, // keep unit test focused; don't use Center here
            Y = Pos.AnchorEnd (1)
        };

        win.Add (label);

        Toplevel top = new ();
        top.Add (win);
        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (40, 10);

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

        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
    }

    // TODO: This is a Label test. Move to label tests if there's not already a test for this.
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

            X = 0,
            Y = Pos.AnchorEnd (1)
        };

        win.Add (label);

        var menu = new MenuBar { Menus = new MenuBarItem [] { new ("Menu", "", null) } };
        var status = new StatusBar (new StatusItem [] { new (KeyCode.F1, "~F1~ Help", null) });
        Toplevel top = new ();
        top.Add (win, menu, status);
        RunState rs = Application.Begin (top);

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

        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
    }

    // TODO: This is a Label test. Move to label tests if there's not already a test for this.
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

        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
    }

    // TODO: This is a Label test. Move to label tests if there's not already a test for this.

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
            Y = Pos.Bottom (win) - 4 // two lines top and bottom borders more two lines above border
        };

        win.Add (label);

        var menu = new MenuBar { Menus = new MenuBarItem [] { new ("Menu", "", null) } };
        var status = new StatusBar (new StatusItem [] { new (KeyCode.F1, "~F1~ Help", null) });
        Toplevel top = new ();
        top.Add (win, menu, status);
        RunState rs = Application.Begin (top);

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

        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
    }

    // TODO: This is a Label test. Move to label tests if there's not already a test for this.
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
                                 Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], output);
                                 Assert.Equal (new Rectangle (0, 0, 22, count + 4), pos);

                                 if (count < 20)
                                 {
                                     field.Text = $"Label {count}";

                                     // Label is AutoSize = true
                                     var label = new Label { Text = field.Text, X = 0, Y = view.Viewport.Height /*, Width = 10*/ };
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

    // TODO: This is a Dim test. Move to Dim tests.

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
                                 Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expecteds [count], output);
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

    // TODO: This is a Label test. Move to Label tests.

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_False_Label_Height_Zero_Stays_Zero ()
    {
        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);
        var text = "Label";
        var label = new Label
        {
            Text = text,
        };
        label.Width = Dim.Fill () - text.Length;
        label.Height = 0;

        var win = new FrameView { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (label);
        win.BeginInit ();
        win.EndInit ();
        win.LayoutSubviews ();
        win.Draw ();

        Assert.Equal (5, text.Length);
        Assert.Equal (new (0, 0, 3, 0), label.Frame);
        //Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Single (label.TextFormatter.GetLines ());
        Assert.Equal (new (0, 0, 10, 4), win.Frame);

        var expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);
        label.Width = Dim.Fill () - text.Length;
        win.LayoutSubviews ();
        win.Clear ();
        win.Draw ();

        Assert.Equal (Rectangle.Empty, label.Frame);
        //        Assert.Equal (new (5, 1), label.TextFormatter.Size);

        //Exception exception = Record.Exception (
        //                                        () => Assert.Equal (
        //                                                            new List<string> { string.Empty },
        //                                                            label.TextFormatter.GetLines ()
        //                                                           )
        //                                       );
        //Assert.Null (exception);

        expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
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


        RunState rs = Application.Begin (top);
        ((FakeDriver)Application.Driver).SetBufferSize (30, 5);

        var expected = @"
┌┤Test Demo 你├──────────────┐
│                            │
│        Say Hello 你        │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        label.Text = "Say Hello 你 changed";
        Application.Refresh ();

        expected = @"
┌┤Test Demo 你├──────────────┐
│                            │
│    Say Hello 你 changed    │
│                            │
└────────────────────────────┘
";

        TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Application.End (rs);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_IsEmpty_False_Minimum_Height ()
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

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    [AutoInitShutdown]
    public void Label_IsEmpty_False_Never_Return_Null_Lines ()
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

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);

        //label.Width = Dim.Fill () - text.Length;
        Application.Refresh ();


        Assert.Equal (new (0, 0, 5, 1), label.Frame);
        Assert.Equal (new (5, 1), label.TextFormatter.Size);
        Assert.Single (label.TextFormatter.GetLines ());

        expected = @"
┌────────┐
│Label   │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 10, 4), pos);
    }

    [Fact]
    public void Label_ResizeView_With_Dim_Absolute ()
    {
        var super = new View ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        var label = new Label ();

        label.Text = "New text";
        super.Add (label);
        super.LayoutSubviews ();

        Rectangle expectedLabelBounds = new (0, 0, 8, 1);
        Assert.Equal (expectedLabelBounds, label.Viewport);
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
    public void AutoSize_True_Setting_With_Height_Vertical ()
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
    }

    [Fact]
    [AutoInitShutdown]
    public void AutoSize_True_TextDirection_Toggle ()
    {
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };

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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);

        view.Width = Dim.Auto ();
        view.Height = Dim.Auto ();

        view.Text = "Hello Worlds";
        Application.Refresh ();
        int len = "Hello Worlds".Length;
        Assert.Equal (12, len);
        Assert.Equal (new Rectangle (0, 0, len, 1), view.Frame);

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

        Assert.Equal (new Rectangle (0, 0, 1, 12), view.Frame);
        Assert.Equal (new Rectangle (0, 0, 1, 12), view.Frame);

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

        Assert.Equal (new Rectangle (0, 0, 1, 12), view.Frame);

        view.Width = 12;
        view.Height = 1;
        view.TextFormatter.Size = new (12, 1);
        win.LayoutSubviews ();
        Assert.Equal (new Size (12, 1), view.TextFormatter.Size);
        Assert.Equal (new Rectangle (0, 0, 12, 1), view.Frame);
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

        Assert.Equal (new Rectangle (0, 0, 12, 1), view.Frame);

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

        Assert.Equal (new Rectangle (0, 0, 1, 12), view.Frame);

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

        Assert.Equal (new Rectangle (0, 0, 1, 12), view.Frame);

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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new Rectangle (0, 0, 4, 10), pos);
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

        Assert.Equal (new Rectangle (0, 0, 11, 2), horizontalView.Frame);
        Assert.Equal (new Rectangle (0, 3, 2, 11), verticalView.Frame);

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
        Assert.Equal (new Rectangle (0, 3, 4, 4), verticalView.Frame);

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
        Assert.True (horizontalView.AutoSize);
        Assert.True (verticalView.AutoSize);

        // height was initialized with 8 and can only grow or keep initial value
        Assert.Equal (new Rectangle (0, 3, 2, 7), verticalView.Frame);

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

        Assert.Equal (new Rectangle (0, 0, 2, 1), lbl.Frame);
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
            Width = Dim.Auto (), Height = Dim.Auto (), TextDirection = TextDirection.TopBottom_LeftRight
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

        text = "012345678你";
        horizontalView.Text = text;
        verticalView.Text = text;

        Assert.True (horizontalView.AutoSize);
        Assert.Equal (new (0, 0, 11, 1), horizontalView.Frame);
        Assert.Equal (new (11, 1), horizontalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (horizontalView.Frame.Size, horizontalView.GetSizeNeededForTextWithoutHotKey ());

        Assert.True (verticalView.AutoSize);
        Assert.Equal (new (0, 0, 2, 10), verticalView.Frame);
        Assert.Equal (new (2, 10), verticalView.GetSizeNeededForTextWithoutHotKey ());
        Assert.Equal (verticalView.Frame.Size, verticalView.GetSizeNeededForTextWithoutHotKey ());
    }

    [Fact]
    public void SetRelativeLayout_Respects_AutoSize ()
    {
        var view = new View { Frame = new (0, 0, 10, 0), AutoSize = true };
        view.Text = "01234567890123456789";

        Assert.True (view.AutoSize);
        Assert.Equal (new (0, 0, 20, 1), view.Frame);

        view.SetRelativeLayout (new (25, 5));

        Assert.Equal (new (0, 0, 20, 1), view.Frame);
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

        Assert.True (lblLeft.TextFormatter.AutoSize == autoSize);
        Assert.True (lblCenter.TextFormatter.AutoSize == autoSize);
        Assert.True (lblRight.TextFormatter.AutoSize == autoSize);
        Assert.True (lblJust.TextFormatter.AutoSize == autoSize);

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
    }
}
