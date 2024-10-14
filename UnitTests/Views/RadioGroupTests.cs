using System.ComponentModel;
using Xunit.Abstractions;

// ReSharper disable AccessToModifiedClosure

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
        Application.Top = new ();
        Application.Top.Add (rg);
        rg.SetFocus ();

        Assert.Equal (-1, rg.SelectedItem);
        Application.RaiseKeyDownEvent (Key.Space);
        Assert.Equal (0, rg.SelectedItem);

        Application.Top.Dispose ();
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
    public void Commands_HasFocus ()
    {
        Application.Navigation = new ();
        var rg = new RadioGroup { RadioLabels = new [] { "Test", "New Test" } };
        Application.Top = new ();
        Application.Top.Add (rg);
        rg.SetFocus ();
        Assert.Equal (Orientation.Vertical, rg.Orientation);

        var selectedItemChangedCount = 0;
        rg.SelectedItemChanged += (s, e) => selectedItemChangedCount++;

        var selectingCount = 0;
        rg.Selecting += (s, e) => selectingCount++;

        var acceptedCount = 0;
        rg.Accepting += (s, e) => acceptedCount++;

        // By default the first item is selected
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);
        Assert.Equal (Key.Empty, rg.HotKey);

        // With HasFocus
        // Test up/down without Select
        Assert.False (Application.RaiseKeyDownEvent (Key.CursorUp)); // Should not change (should focus prev view if there was one, which there isn't)
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (0, rg.Cursor);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);

        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (0, rg.SelectedItem); // Cursor changed, but selection didnt
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);

        Assert.False (Application.RaiseKeyDownEvent (Key.CursorDown)); // Should not change selection (should focus next view if there was one, which there isn't)
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);

        // Test Select (Space) when Cursor != SelectedItem - Should select cursor
        Assert.True (Application.RaiseKeyDownEvent (Key.Space));
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (1, selectedItemChangedCount);
        Assert.Equal (1, selectingCount);
        Assert.Equal (0, acceptedCount);

        // Test Select (Space) when Cursor == SelectedItem - Should cycle
        Assert.True (Application.RaiseKeyDownEvent (Key.Space));
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (0, rg.Cursor);
        Assert.Equal (2, selectedItemChangedCount);
        Assert.Equal (2, selectingCount);
        Assert.Equal (0, acceptedCount);

        Assert.True (Application.RaiseKeyDownEvent (Key.Space));
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.True (Application.RaiseKeyDownEvent (Key.Space));
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (0, rg.Cursor);
        Assert.True (Application.RaiseKeyDownEvent (Key.Space));
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);

        Assert.True (Application.RaiseKeyDownEvent (Key.Home));
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (0, rg.Cursor);
        Assert.True (Application.RaiseKeyDownEvent (Key.Space));
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (0, rg.Cursor);

        Assert.True (Application.RaiseKeyDownEvent (Key.End));
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.True (Application.RaiseKeyDownEvent (Key.Space));
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (7, selectedItemChangedCount);
        Assert.Equal (7, selectingCount);
        Assert.Equal (0, acceptedCount);

        // Test HotKey
        //    Selected == Cursor (1) - Advance state and raise Select event - DO NOT raise Accept

        rg.HotKey = Key.L;
        Assert.Equal (Key.L, rg.HotKey);
        Assert.True (Application.RaiseKeyDownEvent (rg.HotKey));
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (0, rg.Cursor);
        Assert.Equal (8, selectedItemChangedCount);
        Assert.Equal (8, selectingCount);
        Assert.Equal (0, acceptedCount);

        //     Make Selected != Cursor
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);

        //    Selected != Cursor - Raise HotKey event - Since we're focused, this should just advance
        Assert.True (Application.RaiseKeyDownEvent (rg.HotKey));
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (9, selectedItemChangedCount);
        Assert.Equal (9, selectingCount);
        Assert.Equal (0, acceptedCount);

        Application.ResetState (true);
    }

    [Fact]
    public void HotKey_HasFocus_False ()
    {
        Application.Navigation = new ();
        var rg = new RadioGroup { RadioLabels = new [] { "Test", "New Test" } };
        Application.Top = new ();

        // With !HasFocus
        View otherView = new () { Id = "otherView", CanFocus = true };

        Label label = new ()
        {
            Id = "label",
            Title = "_R"
        };

        Application.Top.Add (label, rg, otherView);
        otherView.SetFocus ();

        var selectedItemChangedCount = 0;
        rg.SelectedItemChanged += (s, e) => selectedItemChangedCount++;

        var selectCount = 0;
        rg.Selecting += (s, e) => selectCount++;

        var acceptCount = 0;
        rg.Accepting += (s, e) => acceptCount++;

        // By default the first item is selected
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (Orientation.Vertical, rg.Orientation);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);
        Assert.Equal (Key.Empty, rg.HotKey);

        Assert.False (rg.HasFocus);

        // Test HotKey
        //    Selected (0) == Cursor (0) - SetFocus
        rg.HotKey = Key.L;
        Assert.Equal (Key.L, rg.HotKey);
        Assert.True (Application.RaiseKeyDownEvent (rg.HotKey));
        Assert.True (rg.HasFocus);
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (0, rg.Cursor);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);

        //     Make Selected != Cursor
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);

        otherView.SetFocus ();

        //    Selected != Cursor - SetFocus
        Assert.True (Application.RaiseKeyDownEvent (rg.HotKey));
        Assert.True (rg.HasFocus);
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);

        Assert.True (Application.RaiseKeyDownEvent (rg.HotKey));
        Assert.True (rg.HasFocus);
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (1, selectedItemChangedCount);
        Assert.Equal (1, selectCount);
        Assert.Equal (0, acceptCount);

        Application.ResetState (true);
    }

    [Fact]
    public void HotKeys_HasFocus_False_Does_Not_SetFocus_Selects ()
    {
        Application.Navigation = new ();
        var rg = new RadioGroup { RadioLabels = new [] { "Item _A", "Item _B" } };
        Application.Top = new ();

        // With !HasFocus
        View otherView = new () { Id = "otherView", CanFocus = true };

        Label label = new ()
        {
            Id = "label",
            Title = "_R"
        };

        Application.Top.Add (label, rg, otherView);
        otherView.SetFocus ();

        var selectedItemChangedCount = 0;
        rg.SelectedItemChanged += (s, e) => selectedItemChangedCount++;

        var selectCount = 0;
        rg.Selecting += (s, e) => selectCount++;

        var acceptCount = 0;
        rg.Accepting += (s, e) => acceptCount++;

        // By default the first item is selected
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (Orientation.Vertical, rg.Orientation);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);
        Assert.Equal (Key.Empty, rg.HotKey);

        Assert.False (rg.HasFocus);

        // Test RadioTitem.HotKey - Should never SetFocus
        //    Selected (0) == Cursor (0) 
        Assert.True (Application.RaiseKeyDownEvent (Key.A));
        Assert.False (rg.HasFocus);
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (0, rg.Cursor);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);

        rg.SetFocus ();

        //     Make Selected != Cursor
        Assert.True (Application.RaiseKeyDownEvent (Key.CursorDown));
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);

        otherView.SetFocus ();

        //    Selected != Cursor
        Assert.True (Application.RaiseKeyDownEvent (Key.A));
        Assert.False (rg.HasFocus);
        Assert.Equal (0, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (0, selectedItemChangedCount);
        Assert.Equal (0, selectCount);
        Assert.Equal (0, acceptCount);

        //    Selected != Cursor - Should not set focus
        Assert.True (Application.RaiseKeyDownEvent (Key.B));
        Assert.False (rg.HasFocus);
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (1, selectedItemChangedCount);
        Assert.Equal (1, selectCount);
        Assert.Equal (0, acceptCount);

        Assert.True (Application.RaiseKeyDownEvent (Key.B));
        Assert.False (rg.HasFocus);
        Assert.Equal (1, rg.SelectedItem);
        Assert.Equal (1, rg.Cursor);
        Assert.Equal (1, selectedItemChangedCount);
        Assert.Equal (1, selectCount);
        Assert.Equal (0, acceptCount);

        Application.ResetState (true);
    }

    [Fact]
    public void HotKeys_HasFocus_True_Selects ()
    {
        var rg = new RadioGroup { RadioLabels = new [] { "_Left", "_Right", "Cen_tered", "_Justified" } };
        Application.Top = new ();
        Application.Top.Add (rg);
        rg.SetFocus ();

        Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L));
        Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L | KeyCode.ShiftMask));
        Assert.NotEmpty (rg.KeyBindings.GetCommands (KeyCode.L | KeyCode.AltMask));

        Assert.True (Application.RaiseKeyDownEvent (Key.T));
        Assert.Equal (2, rg.SelectedItem);
        Assert.True (Application.RaiseKeyDownEvent (Key.L));
        Assert.Equal (0, rg.SelectedItem);
        Assert.True (Application.RaiseKeyDownEvent (Key.J));
        Assert.Equal (3, rg.SelectedItem);
        Assert.True (Application.RaiseKeyDownEvent (Key.R));
        Assert.Equal (1, rg.SelectedItem);

        Assert.True (Application.RaiseKeyDownEvent (Key.T.WithAlt));
        Assert.Equal (2, rg.SelectedItem);
        Assert.True (Application.RaiseKeyDownEvent (Key.L.WithAlt));
        Assert.Equal (0, rg.SelectedItem);
        Assert.True (Application.RaiseKeyDownEvent (Key.J.WithAlt));
        Assert.Equal (3, rg.SelectedItem);
        Assert.True (Application.RaiseKeyDownEvent (Key.R.WithAlt));
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

        Application.Top.Dispose ();
    }

    [Fact]
    public void HotKey_SetsFocus ()
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
    public void HotKey_No_SelectedItem_Selects_First ()
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
        group.SelectedItem = -1;

        superView.Add (group);

        Assert.False (group.HasFocus);
        Assert.Equal (-1, group.SelectedItem);

        group.NewKeyDownEvent (Key.G.WithAlt);

        Assert.Equal (0, group.SelectedItem);
        Assert.False (group.HasFocus);
    }

    [Fact]
    public void HotKeys_Does_Not_SetFocus ()
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
        Assert.False (group.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var group = new RadioGroup { RadioLabels = new [] { "_Left", "_Right", "Cen_tered", "_Justified" } };
        var accepted = false;

        group.Accepting += OnAccept;
        group.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Command_Fires_Accept ()
    {
        var group = new RadioGroup { RadioLabels = new [] { "_Left", "_Right", "Cen_tered", "_Justified" } };
        var accepted = false;

        group.Accepting += OnAccept;
        group.InvokeCommand (Command.Accept);

        Assert.True (accepted);

        return;

        void OnAccept (object sender, CommandEventArgs e) { accepted = true; }
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

    #region Mouse Tests

    [Fact]
    [SetupFakeDriver]
    public void Mouse_Click ()
    {
        var radioGroup = new RadioGroup
        {
            RadioLabels = ["_1", "_2"]
        };
        Assert.True (radioGroup.CanFocus);

        var selectedItemChanged = 0;
        radioGroup.SelectedItemChanged += (s, e) => selectedItemChanged++;

        var selectingCount = 0;
        radioGroup.Selecting += (s, e) => selectingCount++;

        var acceptedCount = 0;
        radioGroup.Accepting += (s, e) => acceptedCount++;

        Assert.Equal (Orientation.Vertical, radioGroup.Orientation);

        radioGroup.HasFocus = true;
        Assert.True (radioGroup.HasFocus);
        Assert.Equal (0, radioGroup.SelectedItem);
        Assert.Equal (0, selectedItemChanged);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);

        // Click on the first item, which is already selected
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (0, radioGroup.SelectedItem);
        Assert.Equal (0, selectedItemChanged);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);

        // Click on the second item
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 1), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (1, radioGroup.SelectedItem);
        Assert.Equal (1, selectedItemChanged);
        Assert.Equal (1, selectingCount);
        Assert.Equal (0, acceptedCount);

        // Click on the first item
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (0, radioGroup.SelectedItem);
        Assert.Equal (2, selectedItemChanged);
        Assert.Equal (2, selectingCount);
        Assert.Equal (0, acceptedCount);
    }

    [Fact]
    [SetupFakeDriver]
    public void Mouse_DoubleClick ()
    {
        var radioGroup = new RadioGroup
        {
            RadioLabels = ["_1", "__2"]
        };
        Assert.True (radioGroup.CanFocus);

        var selectedItemChanged = 0;
        radioGroup.SelectedItemChanged += (s, e) => selectedItemChanged++;

        var selectingCount = 0;
        radioGroup.Selecting += (s, e) => selectingCount++;

        var acceptedCount = 0;
        var handleAccepted = false;

        radioGroup.Accepting += (s, e) =>
                             {
                                 acceptedCount++;
                                 e.Cancel = handleAccepted;
                             };

        Assert.True (radioGroup.DoubleClickAccepts);
        Assert.Equal (Orientation.Vertical, radioGroup.Orientation);

        radioGroup.HasFocus = true;
        Assert.True (radioGroup.HasFocus);
        Assert.Equal (0, radioGroup.SelectedItem);
        Assert.Equal (0, selectedItemChanged);
        Assert.Equal (0, selectingCount);
        Assert.Equal (0, acceptedCount);

        // NOTE: Drivers ALWAYS generate a Button1Clicked event before Button1DoubleClicked
        // NOTE: We need to do the same

        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1DoubleClicked }));
        Assert.Equal (0, radioGroup.SelectedItem);
        Assert.Equal (0, selectedItemChanged);
        Assert.Equal (0, selectingCount);
        Assert.Equal (1, acceptedCount);

        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 1), Flags = MouseFlags.Button1Clicked }));
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 1), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (1, radioGroup.SelectedItem);
        Assert.Equal (1, selectedItemChanged);
        Assert.Equal (1, selectingCount);
        Assert.Equal (1, acceptedCount);

        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 1), Flags = MouseFlags.Button1Clicked }));
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 1), Flags = MouseFlags.Button1DoubleClicked }));
        Assert.Equal (1, radioGroup.SelectedItem);
        Assert.Equal (1, selectedItemChanged);
        Assert.Equal (1, selectingCount);
        Assert.Equal (2, acceptedCount);

        View superView = new () { Id = "superView", CanFocus = true };
        superView.Add (radioGroup);
        superView.SetFocus ();

        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.Equal (0, radioGroup.SelectedItem);
        Assert.Equal (2, selectedItemChanged);
        Assert.Equal (2, selectingCount);
        Assert.Equal (2, acceptedCount);

        var superViewAcceptCount = 0;

        superView.Accepting += (s, a) =>
                            {
                                superViewAcceptCount++;
                                a.Cancel = true;
                            };

        Assert.Equal (0, superViewAcceptCount);

        // By handling the event, we're cancelling it. So the radio group should not change.
        handleAccepted = true;
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1DoubleClicked }));
        Assert.Equal (0, radioGroup.SelectedItem);
        Assert.Equal (2, selectedItemChanged);
        Assert.Equal (2, selectingCount);
        Assert.Equal (3, acceptedCount);
        Assert.Equal (0, superViewAcceptCount);

        handleAccepted = false;
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1Clicked }));
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 0), Flags = MouseFlags.Button1DoubleClicked }));
        Assert.Equal (0, radioGroup.SelectedItem);
        Assert.Equal (2, selectedItemChanged);
        Assert.Equal (2, selectingCount);
        Assert.Equal (4, acceptedCount);
        Assert.Equal (1, superViewAcceptCount); // Accept bubbles up to superview

        radioGroup.DoubleClickAccepts = false;
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 1), Flags = MouseFlags.Button1Clicked }));
        Assert.True (radioGroup.NewMouseEvent (new () { Position = new (0, 1), Flags = MouseFlags.Button1DoubleClicked }));
    }

    #endregion Mouse Tests
}
