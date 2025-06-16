namespace Terminal.Gui.ViewsTests;

public class OptionSelectorTests
{
    [Fact]
    public void Initialization_ShouldSetDefaults ()
    {
        var optionSelector = new OptionSelector ();

        Assert.True (optionSelector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), optionSelector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), optionSelector.Height);
        Assert.Equal (Orientation.Vertical, optionSelector.Orientation);
        Assert.Null (optionSelector.Value);
        Assert.Null (optionSelector.Labels);
    }

    [Fact]
    public void SetOptions_ShouldCreateCheckBoxes ()
    {
        var optionSelector = new OptionSelector ();
        List<string> options = new () { "Option1", "Option2", "Option3" };

        optionSelector.Labels = options;

        Assert.Equal (options, optionSelector.Labels);
        Assert.Equal (options.Count, optionSelector.SubViews.OfType<CheckBox> ().Count ());
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox cb && cb.Title == "Option1");
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox cb && cb.Title == "Option2");
        Assert.Contains (optionSelector.SubViews, sv => sv is CheckBox cb && cb.Title == "Option3");
    }

    [Fact]
    public void Value_Set_ShouldUpdateCheckedState ()
    {
        var optionSelector = new OptionSelector ();
        List<string> options = new () { "Option1", "Option2" };

        optionSelector.Labels = options;
        optionSelector.Value = 1;

        CheckBox selectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data == 1);
        Assert.Equal (CheckState.Checked, selectedCheckBox.CheckedState);

        CheckBox unselectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data == 0);
        Assert.Equal (CheckState.UnChecked, unselectedCheckBox.CheckedState);
    }

    [Fact]
    public void Value_Set_OutOfRange_ShouldThrow ()
    {
        var optionSelector = new OptionSelector ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;

        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.Value = -1);
        Assert.Throws<ArgumentOutOfRangeException> (() => optionSelector.Value = 2);
    }

    [Fact]
    public void ValueChanged_Event_ShouldBeRaised ()
    {
        var optionSelector = new OptionSelector ();
        List<string> options = new () { "Option1", "Option2" };

        optionSelector.Labels = options;
        var eventRaised = false;
        optionSelector.ValueChanged += (sender, args) => eventRaised = true;

        optionSelector.Value = 1;

        Assert.True (eventRaised);
    }

    [Fact]
    public void AssignHotKeys_ShouldAssignUniqueHotKeys ()
    {
        var optionSelector = new OptionSelector
        {
            AssignHotKeys = true
        };
        List<string> options = new () { "Option1", "Option2" };

        optionSelector.Labels = options;

        List<CheckBox> checkBoxes = optionSelector.SubViews.OfType<CheckBox> ().ToList ();
        Assert.Contains ('_', checkBoxes [0].Title);
        Assert.Contains ('_', checkBoxes [1].Title);
    }

    [Fact]
    public void Orientation_Set_ShouldUpdateLayout ()
    {
        var optionSelector = new OptionSelector ();
        List<string> options = new () { "Option1", "Option2" };

        optionSelector.Labels = options;
        optionSelector.Orientation = Orientation.Horizontal;

        foreach (CheckBox checkBox in optionSelector.SubViews.OfType<CheckBox> ())
        {
            Assert.Equal (0, checkBox.Y);
        }
    }

    [Fact]
    public void HotKey_SetsFocus ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });

        var optionSelector = new OptionSelector
        {
            Title = "_OptionSelector"
        };
        optionSelector.Labels = new List<string> { "Option_1", "Option_2" };

        superView.Add (optionSelector);

        Assert.False (optionSelector.HasFocus);
        Assert.Equal (0, optionSelector.Value);

        optionSelector.NewKeyDownEvent (Key.O.WithAlt);

        Assert.Equal (0, optionSelector.Value);
        Assert.True (optionSelector.HasFocus);
    }

    [Fact]
    public void Accept_Command_Fires_Accept ()
    {
        var optionSelector = new OptionSelector ();
        optionSelector.Labels = new List<string> { "Option1", "Option2" };
        var accepted = false;

        optionSelector.Accepting += OnAccept;
        optionSelector.InvokeCommand (Command.Accept);

        Assert.True (accepted);

        return;

        void OnAccept (object sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Mouse_Click_Activates ()
    {
        OptionSelector optionSelector = new OptionSelector ();
        List<string> options = ["Option1", "Option2"];

        optionSelector.Labels = options;

        CheckBox checkBox = optionSelector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Option1");

        var mouseEvent = new MouseEventArgs
        {
            Position = checkBox.Frame.Location,
            Flags = MouseFlags.Button1Clicked
        };

        optionSelector.NewMouseEvent (mouseEvent);

        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
    }

    [Fact]
    public void Values_ShouldUseOptions_WhenValuesIsNull ()
    {
        var optionSelector = new OptionSelector ();
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
        List<string> options = new () { "Option _1", "Option _2", "Option _3" };
        List<int> values = new () { 0, 1, 5 };

        optionSelector.Labels = options;
        optionSelector.Values = values;

        // Act & Assert
        Assert.Equal (values, optionSelector.Values);
        Assert.Equal (options, optionSelector.Labels);

        // Verify that the Value property updates correctly
        optionSelector.Value = 5;
        Assert.Equal (5, optionSelector.Value);

        // Verify that the CheckBox states align with the non-sequential Values
        CheckBox selectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ()
            .First (cb => (int)cb.Data == 5);
        Assert.Equal (CheckState.Checked, selectedCheckBox.CheckedState);

        CheckBox unselectedCheckBox = optionSelector.SubViews.OfType<CheckBox> ()
            .First (cb => (int)cb.Data == 0); // Index 0 corresponds to value 0
        Assert.Equal (CheckState.UnChecked, unselectedCheckBox.CheckedState);
    }


    [Fact]
    public void Item_HotKey_Null_Value_Changes_Value_And_Does_Not_SetFocus ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });
        var selector = new OptionSelector ();
        selector.Labels = ["_One", "_Two"];
        superView.Add (selector);

        Assert.False (selector.HasFocus);
        Assert.Equal (0, selector.Value);

        selector.NewKeyDownEvent (Key.T);

        Assert.False (selector.HasFocus);
        Assert.Equal (1, selector.Value);
    }
}
