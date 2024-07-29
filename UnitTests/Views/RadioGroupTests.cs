using System.ComponentModel;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class RadioGroupTests (ITestOutputHelper output)
{
    [Fact]
    public void Constructors_Defaults ()
    {
        var rg = new RadioGroup ();
        Assert.True (rg.CanFocus);
        Assert.Empty (rg.RadioLabels);
        Assert.Equal (Rectangle.Empty, rg.Frame);
        Assert.Equal (0, rg.SelectedItem);

        rg = new () { RadioLabels = new [] { "Test" } };
        Assert.True (rg.CanFocus);
        Assert.Single (rg.RadioLabels);
        Assert.Equal (0, rg.SelectedItem);

        rg = new ()
        {
            X = 1,
            Y = 2,
            Width = 20,
            Height = 5,
            RadioLabels = new [] { "Test" }
        };
        Assert.True (rg.CanFocus);
        Assert.Single (rg.RadioLabels);
        Assert.Equal (new (1, 2, 20, 5), rg.Frame);
        Assert.Equal (0, rg.SelectedItem);

        rg = new () { X = 1, Y = 2, RadioLabels = new [] { "Test" } };

        var view = new View { Width = 30, Height = 40 };
        view.Add (rg);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubviews ();

        Assert.True (rg.CanFocus);
        Assert.Single (rg.RadioLabels);
        Assert.Equal (new (1, 2, 6, 1), rg.Frame);
        Assert.Equal (0, rg.SelectedItem);
    }

    [Fact]
    public void Initialize_SelectedItem_With_Minus_One ()
    {
        var rg = new RadioGroup { RadioLabels = new [] { "Test" }, SelectedItem = -1 };
        Application.Current = new Toplevel ();
        Application.Current.Add (rg);
        rg.SetFocus ();

        Assert.Equal (-1, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.Space));
        Assert.Equal (0, rg.SelectedItem);

        Application.Current.Dispose ();
    }

    [Fact]
    public void KeyBindings_Are_Added_Correctly ()
    {
        var rg = new RadioGroup { RadioLabels = new [] { "_Left", "_Right" } };
        Assert.NotEmpty (rg.KeyBindings.GetCommands (Key.L));
        Assert.NotEmpty (rg.KeyBindings.GetCommands (Key.R));

        Assert.NotEmpty (rg.KeyBindings.GetCommands (Key.L.WithShift));
        Assert.NotEmpty (rg.KeyBindings.GetCommands (Key.L.WithAlt));

        Assert.NotEmpty (rg.KeyBindings.GetCommands (Key.R.WithShift));
        Assert.NotEmpty (rg.KeyBindings.GetCommands (Key.R.WithAlt));
    }

    [Fact]
    public void KeyBindings_Command ()
    {
        var rg = new RadioGroup { RadioLabels = new [] { "Test", "New Test" } };
        Application.Current = new Toplevel ();
        Application.Current.Add (rg);
        rg.SetFocus();
        Assert.Equal(Orientation.Vertical, rg.Orientation);
        Assert.Equal(0, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.CursorUp)); // Should not change (should focus prev if there was one)
        Assert.Equal (0, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.CursorDown));
        Assert.True (Application.OnKeyDown (Key.Space));
        Assert.Equal (1, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.CursorDown)); // Should not change (should focus prev if there was one)
        Assert.True (Application.OnKeyDown (Key.Space));
        Assert.Equal (1, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.Home));
        Assert.True (Application.OnKeyDown (Key.Space));
        Assert.Equal (0, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.End));
        Assert.True (Application.OnKeyDown (Key.Space));
        Assert.Equal (1, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.Space));
        Assert.Equal (1, rg.SelectedItem);
        Application.Current.Dispose ();
    }

    [Fact]
    public void HotKeys_Select_RadioLabels ()
    {
        var rg = new RadioGroup { RadioLabels = new [] { "_Left", "_Right", "Cen_tered", "_Justified" } };
        Application.Current = new Toplevel ();
        Application.Current.Add (rg);
        rg.SetFocus ();

        Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L));
        Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L | KeyCode.ShiftMask));
        Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L | KeyCode.AltMask));

        // BUGBUG: These tests only test that RG works on it's own, not if it's a subview
        Assert.True (Application.OnKeyDown (Key.T));
        Assert.Equal (2, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.L));
        Assert.Equal (0, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.J));
        Assert.Equal (3, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.R));
        Assert.Equal (1, rg.SelectedItem);

        Assert.True (Application.OnKeyDown (Key.T.WithAlt));
        Assert.Equal (2, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.L.WithAlt));
        Assert.Equal (0, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.J.WithAlt));
        Assert.Equal (3, rg.SelectedItem);
        Assert.True (Application.OnKeyDown (Key.R.WithAlt));
        Assert.Equal (1, rg.SelectedItem);

        var superView = new View ();
        superView.Add (rg);
        Assert.True (superView.NewKeyDownEvent (Key.T));
        Assert.Equal (2, rg.SelectedItem);
        Assert.True (superView.NewKeyDownEvent (Key.L));
        Assert.Equal (0, rg.SelectedItem);
        Assert.True (superView.NewKeyDownEvent (Key.J));
        Assert.Equal (3, rg.SelectedItem);
        Assert.True (superView.NewKeyDownEvent (Key.R));
        Assert.Equal (1, rg.SelectedItem);

        Assert.True (superView.NewKeyDownEvent (Key.T.WithAlt));
        Assert.Equal (2, rg.SelectedItem);
        Assert.True (superView.NewKeyDownEvent (Key.L.WithAlt));
        Assert.Equal (0, rg.SelectedItem);
        Assert.True (superView.NewKeyDownEvent (Key.J.WithAlt));
        Assert.Equal (3, rg.SelectedItem);
        Assert.True (superView.NewKeyDownEvent (Key.R.WithAlt));
        Assert.Equal (1, rg.SelectedItem);

        Application.Current.Dispose ();
    }

    [Fact]
    public void HotKey_For_View_SetsFocus ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });

        var group = new RadioGroup
        {
            Title = "Radio_Group",
            RadioLabels = new [] { "_Left", "_Right", "Cen_tered", "_Justified" }
        };
        superView.Add (group);

        Assert.False (group.HasFocus);
        Assert.Equal (0, group.SelectedItem);

        group.NewKeyDownEvent (Key.G.WithAlt);

        Assert.Equal (0, group.SelectedItem);
        Assert.True (group.HasFocus);
    }

    [Fact]
    public void HotKey_For_Item_SetsFocus ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });
        var group = new RadioGroup { RadioLabels = new [] { "_Left", "_Right", "Cen_tered", "_Justified" } };
        superView.Add (group);

        Assert.False (group.HasFocus);
        Assert.Equal (0, group.SelectedItem);

        group.NewKeyDownEvent (Key.R);

        Assert.Equal (1, group.SelectedItem);
        Assert.True (group.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var group = new RadioGroup { RadioLabels = new [] { "_Left", "_Right", "Cen_tered", "_Justified" } };
        var accepted = false;

        group.Accept += OnAccept;
        group.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object sender, HandledEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Command_Fires_Accept ()
    {
        var group = new RadioGroup { RadioLabels = new [] { "_Left", "_Right", "Cen_tered", "_Justified" } };
        var accepted = false;

        group.Accept += OnAccept;
        group.InvokeCommand (Command.Accept);

        Assert.True (accepted);

        return;

        void OnAccept (object sender, HandledEventArgs e) { accepted = true; }
    }

    [Fact]
    [AutoInitShutdown]
    public void Orientation_Width_Height_Vertical_Horizontal_Space ()
    {
        var rg = new RadioGroup { RadioLabels = new [] { "Test", "New Test 你" } };
        var win = new Window { Width = Dim.Fill (), Height = Dim.Fill () };
        win.Add (rg);
        var top = new Toplevel ();
        top.Add (win);

        Application.Begin (top);
        ((FakeDriver)Application.Driver!).SetBufferSize (30, 5);

        Assert.Equal (Orientation.Vertical, rg.Orientation);
        Assert.Equal (2, rg.RadioLabels.Length);
        Assert.Equal (0, rg.X);
        Assert.Equal (0, rg.Y);
        Assert.Equal (13, rg.Frame.Width);
        Assert.Equal (2, rg.Frame.Height);

        var expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test                      │
│{CM.Glyphs.UnSelected} New Test 你               │
│                            │
└────────────────────────────┘
";

        Rectangle pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        rg.Orientation = Orientation.Horizontal;
        Application.Refresh ();

        Assert.Equal (Orientation.Horizontal, rg.Orientation);
        Assert.Equal (2, rg.HorizontalSpace);
        Assert.Equal (0, rg.X);
        Assert.Equal (0, rg.Y);
        Assert.Equal (21, rg.Frame.Width);
        Assert.Equal (1, rg.Frame.Height);

        expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test  {CM.Glyphs.UnSelected} New Test 你       │
│                            │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);

        rg.HorizontalSpace = 4;
        Application.Refresh ();

        Assert.Equal (Orientation.Horizontal, rg.Orientation);
        Assert.Equal (4, rg.HorizontalSpace);
        Assert.Equal (0, rg.X);
        Assert.Equal (0, rg.Y);
        Assert.Equal (23, rg.Frame.Width);
        Assert.Equal (1, rg.Frame.Height);

        expected = @$"
┌────────────────────────────┐
│{CM.Glyphs.Selected} Test    {CM.Glyphs.UnSelected} New Test 你     │
│                            │
│                            │
└────────────────────────────┘
";

        pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 30, 5), pos);
        top.Dispose ();
    }

    [Fact]
    public void SelectedItemChanged_Event ()
    {
        int previousSelectedItem = -1;
        int selectedItem = -1;
        var rg = new RadioGroup { RadioLabels = new [] { "Test", "New Test" } };

        rg.SelectedItemChanged += (s, e) =>
                                  {
                                      previousSelectedItem = e.PreviousSelectedItem;
                                      selectedItem = e.SelectedItem;
                                  };

        rg.SelectedItem = 1;
        Assert.Equal (0, previousSelectedItem);
        Assert.Equal (selectedItem, rg.SelectedItem);
    }
}
