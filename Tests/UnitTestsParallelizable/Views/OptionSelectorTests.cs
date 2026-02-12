#nullable disable
namespace ViewsTests;

public class OptionSelectorTests
{
    [Fact]
    public void Initialization_ShouldSetDefaults ()
    {
        OptionSelector optionSelector = new ();

        Assert.True (optionSelector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), optionSelector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), optionSelector.Height);
        Assert.Equal (Orientation.Vertical, optionSelector.Orientation);
        Assert.Equal (0, optionSelector.Value);
        Assert.Null (optionSelector.Labels);
    }

    [Fact]
    public void Initialization_With_Options_Value_Is_First ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        Assert.Equal (0, optionSelector.Value);

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1");
        Assert.Equal (CheckState.Checked, checkBox.Value);
    }

    [Fact]
    public void SetOptions_ShouldCreateCheckBoxes ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;

        Assert.Equal (options, optionSelector.Labels);
        Assert.Equal (options.Count, optionSelector.SubViews.OfType<CheckBox> ().Count ());
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox { Title: "Option1" });
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox { Title: "Option2" });
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox { Title: "Option3" });
    }

    [Fact]
    public void Value_Set_ShouldUpdateCheckedState ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Value = 1;

        CheckBox selectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 1);
        Assert.Equal (CheckState.Checked, selectedCheckBox.Value);

        CheckBox unselectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 0);
        Assert.Equal (CheckState.UnChecked, unselectedCheckBox.Value);
    }

    [Fact]
    public void Value_Set_OutOfRange_ShouldThrow ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;

        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.Value = -1);
        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.Value = 2);
    }

    [Fact]
    public void ValueChanged_Event_ShouldBeRaised ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        var eventRaised = false;
        optionSelector.ValueChanged += (_, _) => eventRaised = true;

        optionSelector.Value = 1;

        Assert.True (eventRaised);
    }

    [Fact]
    public void AssignHotKeys_ShouldAssignUniqueHotKeys ()
    {
        var optionSelector = new OptionSelector { AssignHotKeys = true };
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;

        List<CheckBox> checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToList ();
        Assert.Contains ('_', checkBoxes [0].Title);
        Assert.Contains ('_', checkBoxes [1].Title);
    }

    [Fact]
    public void Orientation_Set_ShouldUpdateLayout ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Orientation = Orientation.Horizontal;

        foreach (CheckBox checkBox in optionSelector.SubViews.OfType<CheckBox> ())
        {
            Assert.Equal (0, checkBox.Y);
        }
    }

    [Fact]
    public void HotKey_No_Value_Selects_First ()
    {
        var superView = new View { CanFocus = true };
        superView.Add (new View { CanFocus = true });

        var selector = new OptionSelector { HotKey = Key.G.WithAlt, Labels = ["_Left", "_Right", "Cen_tered", "_Justified"] };
        selector.Value = null;

        superView.Add (selector);

        Assert.False (selector.HasFocus);
        Assert.Null (selector.Value);

        selector.NewKeyDownEvent (Key.G.WithAlt);

        Assert.Equal (0, selector.Value);
        Assert.Equal (selector.SubViews.OfType<CheckBox> ().First (), superView.MostFocused);
    }

    [Fact]
    public void Accept_Command_Fires_Accept ()
    {
        OptionSelector optionSelector = new ();
        optionSelector.Labels = new List<string> { "Option1", "Option2" };
        var accepted = false;

        optionSelector.Accepting += OnAccept;
        optionSelector.InvokeCommand (Command.Accept);

        Assert.True (accepted);

        return;

        void OnAccept (object sender, CommandEventArgs e) => accepted = true;
    }

    [Fact]
    public void LeftButtonClicked_On_Activated_Does_Nothing ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1");
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);

        var mouse = new Mouse { Position = checkBox.Frame.Location, Flags = MouseFlags.LeftButtonClicked };

        checkBox.NewMouseEvent (mouse);

        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2").Value);
    }

    [Fact]
    public void LeftButtonPressed_On_NotActivated_Activates ()
    {
        // Arrange
        VirtualTimeProvider time = new ();
        using IApplication app = Application.Create (time);
        app.Init (DriverRegistry.Names.ANSI);
        IRunnable runnable = new Runnable ();

        var optionSelector = new OptionSelector ();
        List<string> options = ["Option1", "Option2"];
        optionSelector.Labels = options;

        ((View)runnable).Add (optionSelector);
        app.Begin (runnable);

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2");
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        app.InjectMouse (new Mouse { ScreenPosition = checkBox.Frame.Location, Flags = MouseFlags.LeftButtonPressed });
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        app.InjectMouse (new Mouse { ScreenPosition = checkBox.Frame.Location, Flags = MouseFlags.LeftButtonReleased });
        Assert.Equal (1, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
    }

    [Fact]
    public void Key_Space_On_Activated_Cycles ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1");
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);

        checkBox.NewKeyDownEvent (Key.Space);

        Assert.Equal (1, optionSelector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);
        Assert.Equal (CheckState.Checked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2").Value);
    }

    [Fact]
    public void Key_Space_On_NotActivated_Activates ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option2");
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (CheckState.Checked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
        Assert.Equal (CheckState.UnChecked, checkBox.Value);

        checkBox.NewKeyDownEvent (Key.Space);

        Assert.Equal (1, optionSelector.Value);
        Assert.Equal (CheckState.Checked, checkBox.Value);
        Assert.Equal (CheckState.UnChecked, optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1").Value);
    }

    [Fact]
    public void Values_ShouldUseOptions_WhenValuesIsNull ()
    {
        OptionSelector optionSelector = new ();
        Assert.Null (optionSelector.Values); // Initially null

        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;

        IReadOnlyList<int> values = optionSelector.Values;

        Assert.NotNull (values);
        Assert.Equal (Enumerable.Range (0, options.Count).ToList (), values);
    }

    [Fact]
    public void Values_NonSequential_ShouldWorkCorrectly ()
    {
        // Arrange
        OptionSelector optionSelector = new ();
        List<string> options = ["Option _1", "Option _2", "Option _3"];
        List<int> values = [0, 1, 5];

        optionSelector.Labels = options;
        optionSelector.Values = values;

        // Act & Assert
        Assert.Equal (values, optionSelector.Values);
        Assert.Equal (options, optionSelector.Labels);

        // Verify that the Value property updates correctly
        optionSelector.Value = 5;
        Assert.Equal (5, optionSelector.Value);

        // Verify that the CheckBox states align with the non-sequential Values
        CheckBox selectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 5);
        Assert.Equal (CheckState.Checked, selectedCheckBox.Value);

        CheckBox unselectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 0); // Index 0 corresponds to value 0
        Assert.Equal (CheckState.UnChecked, unselectedCheckBox.Value);
    }

    [Fact]
    public void Item_HotKey_Null_Value_Changes_Value_And_SetsFocus ()
    {
        var superView = new View { CanFocus = true };
        superView.Add (new View { Id = "otherView", CanFocus = true });
        var selector = new OptionSelector ();
        selector.Labels = ["_One", "_Two"];
        superView.Add (selector);
        superView.SetFocus ();

        Assert.False (selector.HasFocus);
        Assert.Equal (0, selector.Value);
        selector.Value = null;
        Assert.False (selector.HasFocus);

        selector.NewKeyDownEvent (Key.T);

        Assert.True (selector.HasFocus);
        Assert.Equal (1, selector.Value);
    }

    [Fact]
    public void FocusedItem_Get_ReturnsCorrectIndex ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        // Set focus to second checkbox
        CheckBox secondCheckBox = optionSelector.SubViews.OfType<CheckBox> ().ToArray () [1];
        secondCheckBox.SetFocus ();

        Assert.Equal (1, optionSelector.FocusedItem);

        // Set focus to third checkbox
        CheckBox thirdCheckBox = optionSelector.SubViews.OfType<CheckBox> ().ToArray () [2];
        thirdCheckBox.SetFocus ();

        Assert.Equal (2, optionSelector.FocusedItem);
    }

    [Fact]
    public void FocusedItem_Get_WhenNotFocusable_ReturnsZero ()
    {
        var optionSelector = new OptionSelector { CanFocus = false };
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        Assert.Equal (0, optionSelector.FocusedItem);
    }

    [Fact]
    public void FocusedItem_Set_ShouldMoveFocusToCorrectCheckBox ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.SetFocus (); // Set focus to optionSelector
        optionSelector.Layout ();

        // Set cursor to second checkbox
        optionSelector.FocusedItem = 1;

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.True (checkBoxes [1].HasFocus);
        Assert.Equal (1, optionSelector.FocusedItem);

        // Set cursor to third checkbox
        optionSelector.FocusedItem = 2;

        Assert.True (checkBoxes [2].HasFocus);
        Assert.Equal (2, optionSelector.FocusedItem);
    }

    [Fact]
    public void FocusedItem_Set_OutOfRange_ShouldThrow ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.FocusedItem = -1);
        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.FocusedItem = 3);
    }

    [Fact]
    public void FocusedItem_Set_WhenNotFocusable_DoesNothing ()
    {
        var optionSelector = new OptionSelector { CanFocus = false };
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Layout ();

        // Should not throw
        optionSelector.FocusedItem = 1;

        // Verify nothing changed
        Assert.Equal (0, optionSelector.FocusedItem);
        Assert.False (optionSelector is { } && optionSelector.SubViews.OfType<CheckBox> ().Any (cb => cb.HasFocus));
    }

    [Fact]
    public void FocusedItem_DoesNotChangeValue ()
    {
        OptionSelector optionSelector = new ();
        List<string> options = ["Option1", "Option2", "Option3"];

        optionSelector.Labels = options;
        optionSelector.Value = 0; // First option is selected
        optionSelector.SetFocus (); // Set focus to optionSelector
        optionSelector.Layout ();

        // Move cursor to second checkbox
        optionSelector.FocusedItem = 1;

        // Value should not change, only focus moves
        Assert.Equal (0, optionSelector.Value);
        Assert.Equal (1, optionSelector.FocusedItem);

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Equal (CheckState.Checked, checkBoxes [0].Value);
        Assert.Equal (CheckState.UnChecked, checkBoxes [1].Value);
        Assert.True (checkBoxes [1].HasFocus);
    }

    // Claude - Opus 4.6
    // Per OptionSelector spec: Space key cycles to next option
    [Fact]
    public void OptionSelector_Command_Activate_ForwardsToFocusedCheckBox ()
    {
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.BeginInit ();
        optionSelector.EndInit ();
        optionSelector.SetFocus ();

        Assert.Equal (0, optionSelector.Value);

        // Activate should BubbleDown to the focused CheckBox, triggering Cycle
        optionSelector.InvokeCommand (Command.Activate);

        Assert.Equal (1, optionSelector.Value);

        optionSelector.Dispose ();
    }

    // Claude - Opus 4.5
    // Behavior documented in docfx/docs/command.md - View Command Behaviors table
    // This test verifies current behavior which may change per issue #4473
    [Fact]
    public void OptionSelector_Command_Accept_RaisesAccepting ()
    {
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2"];
        var acceptingFired = false;

        optionSelector.Accepting += (_, e) =>
                                    {
                                        acceptingFired = true;
                                        e.Handled = true;
                                    };

        bool? result = optionSelector.InvokeCommand (Command.Accept);

        Assert.True (acceptingFired);
        Assert.True (result);

        optionSelector.Dispose ();
    }

    // Claude - Opus 4.6
    // Per OptionSelector spec: HotKey restores focus and advances Active
    [Fact]
    public void OptionSelector_Command_HotKey_ForwardsToFocusedItem ()
    {
        OptionSelector optionSelector = new ();
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.BeginInit ();
        optionSelector.EndInit ();

        Assert.Equal (0, optionSelector.Value);

        // HotKey should restore focus and advance Active (Cycle)
        optionSelector.InvokeCommand (Command.HotKey);

        Assert.True (optionSelector.HasFocus);
        Assert.Equal (1, optionSelector.Value);

        optionSelector.Dispose ();
    }

    #region Navigation Command Tests (Down, Up, Right, Left)

    // Vertical Orientation - Down Command Tests

    [Fact]
    public void Command_Down_Vertical_MovesFocusToNextCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();
        Assert.True (checkBoxes [0].HasFocus);

        optionSelector.InvokeCommand (Command.Down);

        Assert.True (checkBoxes [1].HasFocus);
        Assert.False (checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Down_Vertical_WrapsAroundToFirst ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [2].SetFocus (); // Focus last checkbox
        Assert.True (checkBoxes [2].HasFocus);

        optionSelector.InvokeCommand (Command.Down);

        Assert.True (checkBoxes [0].HasFocus); // Should wrap to first
        Assert.False (checkBoxes [2].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Down_Horizontal_ReturnsFalse ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();

        bool? result = optionSelector.InvokeCommand (Command.Down);

        Assert.False (result);
        Assert.True (checkBoxes [0].HasFocus); // Focus should not change

        optionSelector.Dispose ();
    }

    // Vertical Orientation - Up Command Tests

    [Fact]
    public void Command_Up_Vertical_MovesFocusToPreviousCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();
        Assert.True (checkBoxes [1].HasFocus);

        optionSelector.InvokeCommand (Command.Up);

        Assert.True (checkBoxes [0].HasFocus);
        Assert.False (checkBoxes [1].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Up_Vertical_WrapsAroundToLast ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus (); // Focus first checkbox
        Assert.True (checkBoxes [0].HasFocus);

        optionSelector.InvokeCommand (Command.Up);

        Assert.True (checkBoxes [2].HasFocus); // Should wrap to last
        Assert.False (checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Up_Horizontal_ReturnsFalse ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();

        bool? result = optionSelector.InvokeCommand (Command.Up);

        Assert.False (result);
        Assert.True (checkBoxes [1].HasFocus); // Focus should not change

        optionSelector.Dispose ();
    }

    // Horizontal Orientation - Right Command Tests

    [Fact]
    public void Command_Right_Horizontal_MovesFocusToNextCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();
        Assert.True (checkBoxes [0].HasFocus);

        optionSelector.InvokeCommand (Command.Right);

        Assert.True (checkBoxes [1].HasFocus);
        Assert.False (checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Right_Horizontal_WrapsAroundToFirst ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [2].SetFocus (); // Focus last checkbox
        Assert.True (checkBoxes [2].HasFocus);

        optionSelector.InvokeCommand (Command.Right);

        Assert.True (checkBoxes [0].HasFocus); // Should wrap to first
        Assert.False (checkBoxes [2].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Right_Vertical_ReturnsFalse ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();

        bool? result = optionSelector.InvokeCommand (Command.Right);

        Assert.False (result);
        Assert.True (checkBoxes [0].HasFocus); // Focus should not change

        optionSelector.Dispose ();
    }

    // Horizontal Orientation - Left Command Tests

    [Fact]
    public void Command_Left_Horizontal_MovesFocusToPreviousCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();
        Assert.True (checkBoxes [1].HasFocus);

        optionSelector.InvokeCommand (Command.Left);

        Assert.True (checkBoxes [0].HasFocus);
        Assert.False (checkBoxes [1].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Left_Horizontal_WrapsAroundToLast ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Horizontal, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus (); // Focus first checkbox
        Assert.True (checkBoxes [0].HasFocus);

        optionSelector.InvokeCommand (Command.Left);

        Assert.True (checkBoxes [2].HasFocus); // Should wrap to last
        Assert.False (checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Left_Vertical_ReturnsFalse ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus ();

        bool? result = optionSelector.InvokeCommand (Command.Left);

        Assert.False (result);
        Assert.True (checkBoxes [1].HasFocus); // Focus should not change

        optionSelector.Dispose ();
    }

    // ShowValue Style Tests

    [Fact]
    public void Command_Down_Vertical_WithShowValue_FocusesValueFieldAtEnd ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, Styles = SelectorStyles.ShowValue, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [1].SetFocus (); // Focus last checkbox

        optionSelector.InvokeCommand (Command.Down);

        // Should focus the value field instead of wrapping
        View valueField = optionSelector.SubViews.FirstOrDefault (v => v.Id == "valueField");
        Assert.NotNull (valueField);
        Assert.True (valueField.HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Up_Vertical_WithShowValue_FocusesValueFieldAtStart ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, Styles = SelectorStyles.ShowValue, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus (); // Focus first checkbox

        optionSelector.InvokeCommand (Command.Up);

        // Should focus the value field instead of wrapping
        View valueField = optionSelector.SubViews.FirstOrDefault (v => v.Id == "valueField");
        Assert.NotNull (valueField);
        Assert.True (valueField.HasFocus);

        optionSelector.Dispose ();
    }

    [Fact]
    public void Command_Up_Vertical_WithShowValue_FromValueField_FocusesLastCheckBox ()
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, Styles = SelectorStyles.ShowValue, TabBehavior = TabBehavior.NoStop };
        optionSelector.Labels = ["Option1", "Option2"];
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        View valueField = optionSelector.SubViews.FirstOrDefault (v => v.Id == "valueField");
        Assert.NotNull (valueField);
        valueField.SetFocus ();
        Assert.True (valueField.HasFocus);

        optionSelector.InvokeCommand (Command.Up);

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.True (checkBoxes [1].HasFocus); // Should focus last checkbox

        optionSelector.Dispose ();
    }

    // Navigation Tests

    [Theory]
    [CombinatorialData]
    public void Command_Down_DoesNotChangeValue (TabBehavior tabBehavior)
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = tabBehavior };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.Value = 0;
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [0].SetFocus ();

        optionSelector.InvokeCommand (Command.Down);

        Assert.Equal (0, optionSelector.Value); // Value should remain unchanged

        Assert.True (tabBehavior == TabBehavior.NoStop ? checkBoxes [1].HasFocus : checkBoxes [0].HasFocus);

        optionSelector.Dispose ();
    }

    [Theory]
    [CombinatorialData]
    public void Command_Up_DoesNotChangeValue (TabBehavior tabBehavior)
    {
        OptionSelector optionSelector = new () { Orientation = Orientation.Vertical, TabBehavior = tabBehavior };
        optionSelector.Labels = ["Option1", "Option2", "Option3"];
        optionSelector.Value = 2;
        optionSelector.SetFocus ();
        optionSelector.Layout ();

        CheckBox [] checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToArray ();
        checkBoxes [2].SetFocus ();

        optionSelector.InvokeCommand (Command.Up);

        Assert.Equal (2, optionSelector.Value); // Value should remain unchanged

        Assert.True (tabBehavior == TabBehavior.NoStop ? checkBoxes [1].HasFocus : checkBoxes [2].HasFocus);

        optionSelector.Dispose ();
    }

    #endregion
}
