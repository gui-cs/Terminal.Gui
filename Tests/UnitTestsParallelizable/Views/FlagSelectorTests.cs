using System.Collections.ObjectModel;
using Terminal.Gui.Views;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

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

        var checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data == 1);
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data == 2);
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

        var checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data == Convert.ToInt32 (SelectorStyles.ShowNoneFlag));
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (int)cb.Data == Convert.ToInt32 (SelectorStyles.ShowValue));
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

        void OnAccept (object sender, CommandEventArgs e) { accepted = true; }
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

        void OnAccept (object sender, CommandEventArgs e) { accepted = true; }
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
}


