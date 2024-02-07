using Xunit.Abstractions;

namespace Terminal.Gui.ViewTests;

/// <summary>Tests of the  <see cref="View.Text"/> property with <see cref="View.AutoSize"/> set to false.</summary>
public class AutoSizeFalseTests {
    private readonly ITestOutputHelper _output;
    public AutoSizeFalseTests (ITestOutputHelper output) { _output = output; }

    [Fact]
    public void AutoSize_False_Equal_Before_And_After_IsInitialized_With_Different_Orders () {
        var top = new View { Height = 25, Width = 80 };
        var view1 = new View { Text = "Say Hello view1 你", AutoSize = false, Width = 10, Height = 5 };
        var view2 = new View { Text = "Say Hello view2 你", AutoSize = false, Width = 10, Height = 5 };
        var view3 = new View { AutoSize = false, Width = 10, Height = 5, Text = "Say Hello view3 你" };
        var view4 = new View {
                                 Text = "Say Hello view4 你",
                                 AutoSize = false,
                                 Width = 10,
                                 Height = 5,
                                 TextDirection = TextDirection.TopBottom_LeftRight
                             };
        var view5 = new View {
                                 Text = "Say Hello view5 你",
                                 AutoSize = false,
                                 Width = 10,
                                 Height = 5,
                                 TextDirection = TextDirection.TopBottom_LeftRight
                             };
        var view6 = new View {
                                 AutoSize = false,
                                 Width = 10,
                                 Height = 5,
                                 TextDirection = TextDirection.TopBottom_LeftRight,
                                 Text = "Say Hello view6 你"
                             };
        top.Add (view1, view2, view3, view4, view5, view6);

        Assert.False (view1.IsInitialized);
        Assert.False (view2.IsInitialized);
        Assert.False (view3.IsInitialized);
        Assert.False (view4.IsInitialized);
        Assert.False (view5.IsInitialized);
        Assert.False (view1.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view1.Frame);
        Assert.Equal ("Absolute(10)", view1.Width.ToString ());
        Assert.Equal ("Absolute(5)", view1.Height.ToString ());
        Assert.False (view2.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view2.Frame);
        Assert.Equal ("Absolute(10)", view2.Width.ToString ());
        Assert.Equal ("Absolute(5)", view2.Height.ToString ());
        Assert.False (view3.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view3.Frame);
        Assert.Equal ("Absolute(10)", view3.Width.ToString ());
        Assert.Equal ("Absolute(5)", view3.Height.ToString ());
        Assert.False (view4.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view4.Frame);
        Assert.Equal ("Absolute(10)", view4.Width.ToString ());
        Assert.Equal ("Absolute(5)", view4.Height.ToString ());
        Assert.False (view5.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view5.Frame);
        Assert.Equal ("Absolute(10)", view5.Width.ToString ());
        Assert.Equal ("Absolute(5)", view5.Height.ToString ());
        Assert.False (view6.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view6.Frame);
        Assert.Equal ("Absolute(10)", view6.Width.ToString ());
        Assert.Equal ("Absolute(5)", view6.Height.ToString ());

        top.BeginInit ();
        top.EndInit ();

        Assert.True (view1.IsInitialized);
        Assert.True (view2.IsInitialized);
        Assert.True (view3.IsInitialized);
        Assert.True (view4.IsInitialized);
        Assert.True (view5.IsInitialized);
        Assert.False (view1.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view1.Frame);
        Assert.Equal ("Absolute(10)", view1.Width.ToString ());
        Assert.Equal ("Absolute(5)", view1.Height.ToString ());
        Assert.False (view2.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view2.Frame);
        Assert.Equal ("Absolute(10)", view2.Width.ToString ());
        Assert.Equal ("Absolute(5)", view2.Height.ToString ());
        Assert.False (view3.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view3.Frame);
        Assert.Equal ("Absolute(10)", view3.Width.ToString ());
        Assert.Equal ("Absolute(5)", view3.Height.ToString ());
        Assert.False (view4.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view4.Frame);
        Assert.Equal ("Absolute(10)", view4.Width.ToString ());
        Assert.Equal ("Absolute(5)", view4.Height.ToString ());
        Assert.False (view5.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view5.Frame);
        Assert.Equal ("Absolute(10)", view5.Width.ToString ());
        Assert.Equal ("Absolute(5)", view5.Height.ToString ());
        Assert.False (view6.AutoSize);
        Assert.Equal (new Rect (0, 0, 10, 5), view6.Frame);
        Assert.Equal ("Absolute(10)", view6.Width.ToString ());
        Assert.Equal ("Absolute(5)", view6.Height.ToString ());
    }

    [Fact]
    public void AutoSize_False_If_Text_Empty () {
        var view1 = new View ();
        var view2 = new View ("");
        var view3 = new View { Text = "" };

        Assert.False (view1.AutoSize);
        Assert.False (view2.AutoSize);
        Assert.False (view3.AutoSize);
        view1.Dispose ();
        view2.Dispose ();
        view3.Dispose ();
    }

    [Fact]
    public void AutoSize_False_If_Text_Is_Not_Empty () {
        var view1 = new View ();
        view1.Text = "Hello World";
        var view2 = new View ("Hello World");
        var view3 = new View { Text = "Hello World" };

        Assert.False (view1.AutoSize);
        Assert.False (view2.AutoSize);
        Assert.False (view3.AutoSize);
        view1.Dispose ();
        view2.Dispose ();
        view3.Dispose ();
    }

    [Fact]
    public void AutoSize_False_ResizeView_Is_Always_False () {
        var super = new View ();
        var view = new View ();
        super.Add (view);

        view.Text = "New text";
        super.LayoutSubviews ();

        Assert.False (view.AutoSize);
        Assert.Equal ("(0,0,0,0)", view.Bounds.ToString ());
        super.Dispose ();
    }

    [Fact]
    public void AutoSize_False_ResizeView_With_Dim_Fill_After_IsInitialized () {
        var super = new View (new Rect (0, 0, 30, 80));
        var view = new View { Width = Dim.Fill (), Height = Dim.Fill () };
        super.Add (view);
        Assert.False (view.AutoSize);

        view.Text = "New text\nNew line";
        super.LayoutSubviews ();

        Assert.False (view.AutoSize);
        Assert.Equal ("(0,0,30,80)", view.Bounds.ToString ());
        Assert.False (view.IsInitialized);

        super.BeginInit ();
        super.EndInit ();

        Assert.True (view.IsInitialized);
        Assert.False (view.AutoSize);
        Assert.Equal ("(0,0,30,80)", view.Bounds.ToString ());
    }

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_False_Setting_AutoSize_False_Keeps_Dims () {
        var super = new View {
                                 Width = 10,
                                 Height = 10
                             };
        var view = new View ();
        view.Width = 2;
        view.Height = 1;
        Assert.Equal ("Absolute(2)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());

        super.Add (view);
        super.BeginInit ();
        super.EndInit ();
        super.LayoutSubviews ();
        Assert.Equal ("Absolute(2)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());

        view.AutoSize = true;

        // There's no Text, so the view should be sized (0, 0)
        Assert.Equal ("Absolute(0)", view.Width.ToString ());
        Assert.Equal ("Absolute(0)", view.Height.ToString ());

        view.AutoSize = false;
        Assert.Equal ("Absolute(0)", view.Width.ToString ());
        Assert.Equal ("Absolute(0)", view.Height.ToString ());

        view.Width = 2;
        view.Height = 1;
        Assert.Equal ("Absolute(2)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());

        view.AutoSize = false;
        Assert.Equal ("Absolute(2)", view.Width.ToString ());
        Assert.Equal ("Absolute(1)", view.Height.ToString ());
    }

    [Fact]
    public void AutoSize_False_Text_Does_Not_Change_Size () {
        var view = new View {
                                Width = Dim.Fill (),
                                Height = Dim.Fill ()
                            };

        view.SetRelativeLayout (new Rect (0, 0, 10, 4));
        Assert.Equal (new Rect (0, 0, 10, 4), view.Frame);
        Assert.Equal (new Size (0, 0), view.TextFormatter.Size);
        Assert.False (view.AutoSize);
        Assert.True (view.TextFormatter.NeedsFormat);
        Assert.Equal (string.Empty, view.TextFormatter.Format ()); // There's no size, so it returns an empty string
        Assert.False (view.TextFormatter.NeedsFormat);
        Assert.Single (view.TextFormatter.GetLines ());
        Assert.True (string.IsNullOrEmpty (view.TextFormatter.GetLines ()[0]));

        view.Text = "Views";
        Assert.True (view.TextFormatter.NeedsFormat);
        Assert.Equal (new Size (0, 0), view.TextFormatter.Size);
        Assert.Equal (string.Empty, view.TextFormatter.Format ()); // There's no size, so it returns an empty string
        Assert.False (view.TextFormatter.NeedsFormat);
        Assert.Single (view.TextFormatter.GetLines ());
        Assert.True (string.IsNullOrEmpty (view.TextFormatter.GetLines ()[0]));
    }

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_False_View_IsEmpty_False_Return_Null_Lines () {
        var text = "Views";
        var view = new View {
                                Width = Dim.Fill () - text.Length,
                                Height = 1,
                                Text = text
                            };
        var frame = new FrameView {
                                      Width = Dim.Fill (),
                                      Height = Dim.Fill ()
                                  };
        frame.Add (view);

        ((FakeDriver)Application.Driver).SetBufferSize (10, 4);
        frame.BeginInit ();
        frame.EndInit ();
        frame.LayoutSubviews ();

        Assert.Equal (5, text.Length);
        Assert.False (view.AutoSize);
        Assert.Equal (new Rect (0, 0, 3, 1), view.Frame);
        Assert.Equal (new Size (3, 1), view.TextFormatter.Size);
        Assert.Equal (new List<string> { "Vie" }, view.TextFormatter.GetLines ());
        Assert.Equal (new Rect (0, 0, 10, 4), frame.Frame);

        frame.LayoutSubviews ();
        frame.Clear ();
        frame.Draw ();
        var expected = @"
┌────────┐
│Vie     │
│        │
└────────┘
";

        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 10, 4), pos);

        text = "0123456789";
        Assert.Equal (10, text.Length);
        view.Width = Dim.Fill () - text.Length;

        frame.LayoutSubviews ();
        frame.Clear ();
        frame.Draw ();

        Assert.Equal (new Rect (0, 0, 0, 1), view.Frame);
        Assert.Equal (new Size (0, 1), view.TextFormatter.Size);
        Assert.Equal (new List<string> { string.Empty }, view.TextFormatter.GetLines ());
        expected = @"
┌────────┐
│        │
│        │
└────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
        Assert.Equal (new Rect (0, 0, 10, 4), pos);
    }

    [Fact]
    [SetupFakeDriver]
    public void AutoSize_False_Width_Height_SetMinWidthHeight_Narrow_Wide_Runes () {
        ((FakeDriver)Application.Driver).SetBufferSize (32, 32);
        var top = new View { Width = 32, Height = 32 };

        var text = $"First line{Environment.NewLine}Second line";
        var horizontalView = new View {
                                          Width = 20,
                                          Height = 1,
                                          Text = text
                                      };

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        horizontalView.TextFormatter.Size = new Size (20, 1);

        var verticalView = new View {
                                        Y = 3,
                                        Height = 20,
                                        Width = 1,
                                        Text = text,
                                        TextDirection = TextDirection.TopBottom_LeftRight
                                    };

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        verticalView.TextFormatter.Size = new Size (1, 20);

        var frame = new FrameView {
                                      Width = Dim.Fill (),
                                      Height = Dim.Fill (),
                                      Text = "Window"
                                  };
        frame.Add (horizontalView, verticalView);
        top.Add (frame);
        top.BeginInit ();
        top.EndInit ();

        Assert.False (horizontalView.AutoSize);
        Assert.False (verticalView.AutoSize);
        Assert.Equal (new Rect (0, 0, 20, 1), horizontalView.Frame);
        Assert.Equal (new Rect (0, 3, 1, 20), verticalView.Frame);

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

        Rect pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

        verticalView.Text = $"最初の行{Environment.NewLine}二行目";
        Assert.True (verticalView.TextFormatter.NeedsFormat);

        // Autosize is off, so we have to explicitly set TextFormatter.Size
        // We know these glpyhs are 2 cols wide, so we need to widen the view
        verticalView.Width = 2;
        verticalView.TextFormatter.Size = new Size (2, 20);
        Assert.True (verticalView.TextFormatter.NeedsFormat);

        top.Draw ();
        Assert.Equal (new Rect (0, 3, 2, 20), verticalView.Frame);
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

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
    }
}
