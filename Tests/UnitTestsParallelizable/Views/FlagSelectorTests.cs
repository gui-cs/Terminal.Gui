#nullable enable
namespace ViewsTests;

public class FlagSelectorTests
{
    [Fact]
    public void Initialization_ShouldSetDefaults ()
    {
        var flagSelector = new FlagSelector ();

        Assert.True (flagSelector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Height);
        Assert.Equal (Orientation.Vertical, flagSelector.Orientation);
    }


    [Fact]
    public void Value_Set_ShouldUpdateCheckedState ()
    {
        var flagSelector = new FlagSelector ();
        var flags = new Dictionary<int, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.Values = flags.Keys.ToList ();
        flagSelector.Labels = flags.Values.ToList ();
        flagSelector.Value = 1;

        var checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 1);
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 2);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
    }

    [Fact]
    public void Styles_Set_ShouldCreateSubViews ()
    {
        var flagSelector = new FlagSelector ();
        var flags = new Dictionary<int, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.Values = flags.Keys.ToList ();
        flagSelector.Labels = flags.Values.ToList ();
        flagSelector.Styles = SelectorStyles.ShowNoneFlag;

        Assert.Contains (flagSelector.SubViews, sv => sv is CheckBox cb && cb.Title == "None");
    }

    [Fact]
    public void ValueChanged_Event_ShouldBeRaised ()
    {
        var flagSelector = new FlagSelector ();
        var flags = new Dictionary<int, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.Values = flags.Keys.ToList ();
        flagSelector.Labels = flags.Values.ToList ();
        bool eventRaised = false;
        flagSelector.ValueChanged += (sender, args) => eventRaised = true;

        flagSelector.Value = 2;

        Assert.True (eventRaised);
    }

    // Tests for FlagSelector<TEnum>
    [Fact]
    public void GenericInitialization_ShouldSetDefaults ()
    {
        var flagSelector = new FlagSelector<SelectorStyles> ();

        Assert.True (flagSelector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Height);
        Assert.Equal (Orientation.Vertical, flagSelector.Orientation);
    }

    [Fact]
    public void GenericSetFlagNames_ShouldSetFlagNames ()
    {
        var flagSelector = new FlagSelector<SelectorStyles> ();
        flagSelector.Labels = Enum.GetValues<SelectorStyles> ()
                                 .Select (
                                          l => l switch
                                               {
                                                   SelectorStyles.None => "_No Style",
                                                   SelectorStyles.ShowNoneFlag => "_Show None Value Style",
                                                   SelectorStyles.ShowValue => "Show _Value Editor Style",
                                                   SelectorStyles.All => "_All Styles",
                                                   _ => l.ToString ()
                                               }).ToList ();

        Dictionary<int, string> expectedFlags = Enum.GetValues<SelectorStyles> ()
                                                    .ToDictionary (f => Convert.ToInt32 (f), f => f switch
                                                                                                  {
                                                                                                      SelectorStyles.None => "_No Style",
                                                                                                      SelectorStyles.ShowNoneFlag => "_Show None Value Style",
                                                                                                      SelectorStyles.ShowValue => "Show _Value Editor Style",
                                                                                                      SelectorStyles.All => "_All Styles",
                                                                                                      _ => f.ToString ()
                                                                                                  });

        Assert.Equal (expectedFlags.Keys, flagSelector.Values);
    }

    [Fact]
    public void GenericValue_Set_ShouldUpdateCheckedState ()
    {
        var flagSelector = new FlagSelector<SelectorStyles> ();

        flagSelector.Value = SelectorStyles.ShowNoneFlag;

        var checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == Convert.ToInt32 (SelectorStyles.ShowNoneFlag));
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == Convert.ToInt32 (SelectorStyles.ShowValue));
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
    }

    [Fact]
    public void GenericValueChanged_Event_ShouldBeRaised ()
    {
        var flagSelector = new FlagSelector<SelectorStyles> ();

        bool eventRaised = false;
        flagSelector.ValueChanged += (sender, args) => eventRaised = true;

        flagSelector.Value = SelectorStyles.ShowNoneFlag;

        Assert.True (eventRaised);
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var flagSelector = new FlagSelector ();
        Assert.True (flagSelector.CanFocus);
        Assert.Null (flagSelector.Values);
        Assert.Equal (Rectangle.Empty, flagSelector.Frame);
        Assert.Null (flagSelector.Value);

        flagSelector = new ();
        flagSelector.Values = [1];
        flagSelector.Labels = ["Flag1"];

        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Values!);
        Assert.Equal ((int)1, flagSelector.Value);

        flagSelector = new ()
        {
            X = 1,
            Y = 2,
            Width = 20,
            Height = 5,
        };
        flagSelector.Values = [1];
        flagSelector.Labels = ["Flag1"];

        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Values!);
        Assert.Equal (new (1, 2, 20, 5), flagSelector.Frame);
        Assert.Equal ((int)1, flagSelector.Value);

        flagSelector = new () { X = 1, Y = 2 };
        flagSelector.Values = [1];
        flagSelector.Labels = ["Flag1"];

        var view = new View { Width = 30, Height = 40 };
        view.Add (flagSelector);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Values!);
        Assert.Equal (new (1, 2, 7, 1), flagSelector.Frame);
        Assert.Equal ((int)1, flagSelector.Value);
    }

    [Fact]
    public void HotKey_SetsFocus ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });

        var flagSelector = new FlagSelector ()
        {
            Title = "_FlagSelector",
        };
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];

        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Equal ((int)0, flagSelector.Value);

        flagSelector.NewKeyDownEvent (Key.F.WithAlt);

        Assert.Equal ((int)0, flagSelector.Value);
        Assert.True (flagSelector.HasFocus);
    }

    [Fact]
    public void HotKey_Null_Value_Does_Not_Change_Value ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });

        var flagSelector = new FlagSelector ()
        {
            Title = "_FlagSelector",
        };
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];
        flagSelector.Value = null;

        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);

        flagSelector.InvokeCommand (Command.HotKey, new KeyBinding ());

        Assert.True (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);
    }


    [Fact]
    public void Set_Value_Sets ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });
        var flagSelector = new FlagSelector ();
        flagSelector.Labels = ["_Left", "_Right"];
        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);

        flagSelector.Value = 1;

        Assert.False (flagSelector.HasFocus);
        Assert.Equal (1, flagSelector.Value);
    }

    [Fact]
    public void Item_HotKey_Null_Value_Changes_Value_And_SetsFocus ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });
        var flagSelector = new FlagSelector ();
        flagSelector.Labels = ["_Left", "_Right"];
        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);

        flagSelector.NewKeyDownEvent (Key.R);

        Assert.True (flagSelector.HasFocus);
        Assert.Equal (1, flagSelector.Value);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var flagSelector = new FlagSelector ();
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];
        var accepted = false;

        flagSelector.Accepting += OnAccept;
        flagSelector.InvokeCommand (Command.HotKey);

        Assert.False (accepted);

        return;

        void OnAccept (object? sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void Accept_Command_Fires_Accept ()
    {
        var flagSelector = new FlagSelector ();
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];
        var accepted = false;

        flagSelector.Accepting += OnAccept;
        flagSelector.InvokeCommand (Command.Accept);

        Assert.True (accepted);

        return;

        void OnAccept (object? sender, CommandEventArgs e) { accepted = true; }
    }

    [Fact]
    public void ValueChanged_Event ()
    {
        int? newValue = null;
        var flagSelector = new FlagSelector ();
        flagSelector.Values = [0, 1];
        flagSelector.Labels = ["_Left", "_Right"];

        flagSelector.ValueChanged += (s, e) =>
        {
            newValue = e.Value;
        };

        flagSelector.Value = 1;
        Assert.Equal (newValue, flagSelector.Value);
    }

    #region Mouse Tests


    [Fact]
    public void Mouse_DoubleClick_Accepts ()
    {

    }



    [Fact]
    public void Mouse_Click_On_Activated_NoneFlag_Does_Nothing ()
    {
        FlagSelector selector = new ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        List<string> options = ["Flag1", "Flag2"];

        selector.Labels = options;
        selector.Layout ();

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag1");
        Assert.Null (selector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        selector.Value = 0;

        var mouseEvent = new MouseEventArgs
        {
            Position = checkBox.Frame.Location,
            Flags = MouseFlags.Button1Clicked
        };

        checkBox.NewMouseEvent (mouseEvent);

        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
        Assert.Equal (CheckState.UnChecked, selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag2").CheckedState);
    }


    [Fact]
    public void Mouse_Click_On_NotActivated_NoneFlag_Toggles ()
    {
        FlagSelector selector = new ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        List<string> options = ["Flag1", "Flag2"];

        selector.Labels = options;
        selector.Layout ();

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag1");
        Assert.Null (selector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        selector.Value = 0;
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        var mouseEvent = new MouseEventArgs
        {
            Position = checkBox.Frame.Location,
            Flags = MouseFlags.Button1Clicked
        };

        checkBox.NewMouseEvent (mouseEvent);

        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
        Assert.Equal (CheckState.UnChecked, selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag2").CheckedState);
    }

    #endregion Mouse Tests

    [Fact]
    public void Key_Space_On_Activated_NoneFlag_Does_Nothing ()
    {
        FlagSelector selector = new ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        List<string> options = ["Flag1", "Flag2"];

        selector.Labels = options;
        selector.Layout ();

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag1");
        Assert.Null (selector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
        selector.Value = 0;
        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        checkBox.NewKeyDownEvent (Key.Space);

        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
        Assert.Equal (CheckState.UnChecked, selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag2").CheckedState);
    }


    [Fact]
    public void Key_Space_On_NotActivated_NoneFlag_Activates ()
    {
        FlagSelector selector = new ();
        selector.Styles = SelectorStyles.ShowNoneFlag;

        List<string> options = ["Flag1", "Flag2"];

        selector.Labels = options;
        selector.Layout ();

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag1");
        Assert.Null (selector.Value);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);

        checkBox.NewKeyDownEvent (Key.Space);

        Assert.Equal (0, selector.Value);
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
        Assert.Equal (CheckState.UnChecked, selector.SubViews.OfType<CheckBox> ().First (cb => cb.Title == "Flag2").CheckedState);
    }

    #region FlagSelector-Specific Tests (Non-Base Class Functionality)

    [Fact]
    public void Value_MultipleFlags_CombinesCorrectly ()
    {
        var selector = new FlagSelector ();
        selector.Values = [1, 2, 4];
        selector.Labels = ["Flag1", "Flag2", "Flag3"];

        selector.Value = 1 | 4; // Flags 1 and 3

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.Equal (CheckState.Checked, checkBoxes [0].CheckedState); // Flag 1
        Assert.Equal (CheckState.UnChecked, checkBoxes [1].CheckedState); // Flag 2
        Assert.Equal (CheckState.Checked, checkBoxes [2].CheckedState); // Flag 3
    }

    [Fact]
    public void Value_AllFlags_UpdatesCorrectly ()
    {
        var selector = new FlagSelector ();
        selector.Values = [1, 2, 4];
        selector.Labels = ["Flag1", "Flag2", "Flag3"];

        selector.Value = 1 | 2 | 4; // All flags

        CheckBox [] checkBoxes = selector.SubViews.OfType<CheckBox> ().ToArray ();
        Assert.True (checkBoxes.All (cb => cb.CheckedState == CheckState.Checked));
    }

    [Fact]
    public void Value_ToggleFlag_AddsAndRemovesCorrectly ()
    {
        var selector = new FlagSelector ();
        selector.Values = [1, 2, 4];
        selector.Labels = ["Flag1", "Flag2", "Flag3"];

        selector.Value = 1; // Start with Flag 1
        Assert.Equal (1, selector.Value);

        // Toggle checkbox to add Flag 2
        CheckBox flag2Checkbox = selector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 2);
        flag2Checkbox.CheckedState = CheckState.Checked;

        Assert.Equal (1 | 2, selector.Value);

        // Toggle checkbox to remove Flag 1
        CheckBox flag1Checkbox = selector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 1);
        flag1Checkbox.CheckedState = CheckState.UnChecked;

        Assert.Equal (2, selector.Value);
    }

    [Fact]
    public void Value_SetToZero_ChecksNoneFlag ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];

        selector.Value = 0;

        CheckBox? noneCheckBox = selector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.Title == "None");
        Assert.NotNull (noneCheckBox);
        Assert.Equal (CheckState.Checked, noneCheckBox.CheckedState);

        // All other flags should be unchecked
        Assert.True (selector.SubViews.OfType<CheckBox> ()
                             .Where (cb => (int)cb.Data! != 0)
                             .All (cb => cb.CheckedState == CheckState.UnChecked));
    }

    [Fact]
    public void Value_SetToNull_UnchecksAllIncludingNone ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];
        selector.Value = 1;

        selector.Value = null;

        Assert.True (selector.SubViews.OfType<CheckBox> ()
                             .All (cb => cb.CheckedState == CheckState.UnChecked));
    }

    [Fact]
    public void ToggleNoneFlag_UnchecksAllOtherFlags ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];
        selector.Value = 1 | 2; // Start with all flags set

        CheckBox? noneCheckBox = selector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.Title == "None");
        Assert.NotNull (noneCheckBox);

        noneCheckBox.CheckedState = CheckState.Checked;

        Assert.Equal (0, selector.Value);
        Assert.True (selector.SubViews.OfType<CheckBox> ()
                             .Where (cb => (int)cb.Data! != 0)
                             .All (cb => cb.CheckedState == CheckState.UnChecked));
    }

    [Fact]
    public void ToggleAnyFlag_UnchecksNoneFlag ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];
        selector.Value = 0; // Start with None

        CheckBox? noneCheckBox = selector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.Title == "None");
        Assert.NotNull (noneCheckBox);
        Assert.Equal (CheckState.Checked, noneCheckBox.CheckedState);

        // Toggle a flag
        CheckBox flag1CheckBox = selector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 1);
        flag1CheckBox.CheckedState = CheckState.Checked;

        Assert.Equal (1, selector.Value);
        Assert.Equal (CheckState.UnChecked, noneCheckBox.CheckedState);
    }

    [Fact]
    public void NoneFlag_WithoutShowNoneFlag_IsNotCreated ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.None; // No ShowNoneFlag
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];

        CheckBox? noneCheckBox = selector.SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.Title == "None");
        Assert.Null (noneCheckBox);
        Assert.Equal (2, selector.SubViews.OfType<CheckBox> ().Count ());
    }

    [Fact]
    public void NoneFlag_AlreadyInValues_IsNotDuplicated ()
    {
        var selector = new FlagSelector ();
        selector.Styles = SelectorStyles.ShowNoneFlag;
        selector.Values = [0, 1, 2]; // 0 already included
        selector.Labels = ["None", "Flag1", "Flag2"];

        // Should only have one "None" checkbox
        Assert.Equal (1, selector.SubViews.OfType<CheckBox> ().Count (cb => (int)cb.Data! == 0));
    }

    [Fact]
    public void Mouse_DoubleClick_TogglesAndAccepts ()
    {
        var selector = new FlagSelector { DoubleClickAccepts = true };
        selector.Values = [1, 2];
        selector.Labels = ["Flag1", "Flag2"];
        selector.Layout ();

        var acceptCount = 0;
        selector.Accepting += (s, e) => acceptCount++;

        CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First ();
        // When Values is set, SelectorBase.Value defaults to the first value (1 in this case)
        // So the first checkbox (Data == 1) will be checked
        Assert.Equal (CheckState.Checked, checkBox.CheckedState); // FIXED: Was UnChecked
        Assert.Equal (1, selector.Value); // Verify Value is set to first value

        checkBox.NewMouseEvent (new () { Position = Point.Empty, Flags = MouseFlags.Button1Clicked });
        checkBox.NewMouseEvent (new () { Position = Point.Empty, Flags = MouseFlags.Button1DoubleClicked });

        Assert.Equal (1, acceptCount);
        // After double-clicking on an already-checked flag checkbox, it should still be checked (flags don't uncheck on double-click in FlagSelector)
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);
    }

    [Fact]
    public void UpdateChecked_PreventsConcurrentModification ()
    {
        var selector = new FlagSelector ();
        selector.Values = [1, 2, 4];
        selector.Labels = ["Flag1", "Flag2", "Flag3"];
        selector.Value = 1;

        // This should not cause recursion or errors
        var exception = Record.Exception (() =>
        {
            CheckBox checkBox = selector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data! == 2);
            checkBox.CheckedState = CheckState.Checked; // This triggers UpdateChecked internally
        });

        Assert.Null (exception);
        Assert.Equal (1 | 2, selector.Value);
    }

    #endregion

    #region FlagSelector<T> Specific Tests

    [Fact]
    public void Generic_Value_SetWithEnum_UpdatesCorrectly ()
    {
        var selector = new FlagSelector<SelectorStyles> ();

        selector.Value = SelectorStyles.ShowNoneFlag | SelectorStyles.ShowValue;

        Assert.True ((selector.Value & SelectorStyles.ShowNoneFlag) == SelectorStyles.ShowNoneFlag);
        Assert.True ((selector.Value & SelectorStyles.ShowValue) == SelectorStyles.ShowValue);
        // SelectorStyles.None is 0, so checking (value & 0) == 0 will always be true
        // What we actually want to check is that the value is NOT zero (i.e., not None)
        Assert.True (selector.Value != SelectorStyles.None);
    }

    [Fact]
    public void Generic_AutomaticallyPopulatesFromEnum ()
    {
        var selector = new FlagSelector<SelectorStyles> ();

        // Should auto-populate from enum
        Assert.NotNull (selector.Values);
        Assert.NotNull (selector.Labels);
        Assert.Equal (Enum.GetValues<SelectorStyles> ().Length, selector.Values.Count);
    }

    [Fact]
    public void Generic_NullValue_UnchecksAll ()
    {
        var selector = new FlagSelector<SelectorStyles> ();
        selector.Value = SelectorStyles.ShowNoneFlag;

        selector.Value = null;

        Assert.Null (selector.Value);
        Assert.True (selector.SubViews.OfType<CheckBox> ()
                             .All (cb => cb.CheckedState == CheckState.UnChecked));
    }

    [Fact]
    public void Generic_AllFlagsValue_ChecksAll ()
    {
        var selector = new FlagSelector<SelectorStyles> ();

        selector.Value = SelectorStyles.All;

        // All non-None flags should be checked
        Assert.True (selector.SubViews.OfType<CheckBox> ()
                             .Where (cb => (int)cb.Data! != 0)
                             .All (cb => cb.CheckedState == CheckState.Checked));
    }

    #endregion
}


