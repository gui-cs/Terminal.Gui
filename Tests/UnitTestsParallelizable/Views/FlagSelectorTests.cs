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
    public void SetFlags_WithDictionary_ShouldSetFlags ()
    {
        var flagSelector = new FlagSelector ();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags (flags);

        Assert.Equal (flags, flagSelector.Flags);
    }

    [Fact]
    public void SetFlags_WithDictionary_ShouldSetValue ()
    {
        var flagSelector = new FlagSelector ();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags (flags);

        Assert.Equal ((uint)1, flagSelector.Value);
    }

    [Fact]
    public void SetFlags_WithEnum_ShouldSetFlags ()
    {
        var flagSelector = new FlagSelector ();

        flagSelector.SetFlags<FlagSelectorStyles> ();

        var expectedFlags = Enum.GetValues<FlagSelectorStyles> ()
                                .ToDictionary (f => Convert.ToUInt32 (f), f => f.ToString ());

        Assert.Equal (expectedFlags, flagSelector.Flags);
    }

    [Fact]
    public void SetFlags_WithEnumAndCustomNames_ShouldSetFlags ()
    {
        var flagSelector = new FlagSelector ();

        flagSelector.SetFlags<FlagSelectorStyles> (f => f switch
        {
            FlagSelectorStyles.ShowNone => "Show None Value",
            FlagSelectorStyles.ShowValueEdit => "Show Value Editor",
            FlagSelectorStyles.All => "Everything",
            _ => f.ToString ()
        });

        var expectedFlags = Enum.GetValues<FlagSelectorStyles> ()
                                .ToDictionary (f => Convert.ToUInt32 (f), f => f switch
                                {
                                    FlagSelectorStyles.ShowNone => "Show None Value",
                                    FlagSelectorStyles.ShowValueEdit => "Show Value Editor",
                                    FlagSelectorStyles.All => "Everything",
                                    _ => f.ToString ()
                                });

        Assert.Equal (expectedFlags, flagSelector.Flags);
    }

    [Fact]
    public void Value_Set_ShouldUpdateCheckedState ()
    {
        var flagSelector = new FlagSelector ();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags (flags);
        flagSelector.Value = 1;

        var checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (uint)cb.Data == 1);
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (uint)cb.Data == 2);
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
    }

    [Fact]
    public void Styles_Set_ShouldCreateSubViews ()
    {
        var flagSelector = new FlagSelector ();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags (flags);
        flagSelector.Styles = FlagSelectorStyles.ShowNone;

        Assert.Contains (flagSelector.SubViews, sv => sv is CheckBox cb && cb.Title == "None");
    }

    [Fact]
    public void ValueChanged_Event_ShouldBeRaised ()
    {
        var flagSelector = new FlagSelector ();
        var flags = new Dictionary<uint, string>
        {
            { 1, "Flag1" },
            { 2, "Flag2" }
        };

        flagSelector.SetFlags (flags);
        bool eventRaised = false;
        flagSelector.ValueChanged += (sender, args) => eventRaised = true;

        flagSelector.Value = 2;

        Assert.True (eventRaised);
    }

    // Tests for FlagSelector<TEnum>
    [Fact]
    public void GenericInitialization_ShouldSetDefaults ()
    {
        var flagSelector = new FlagSelector<FlagSelectorStyles> ();

        Assert.True (flagSelector.CanFocus);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Width);
        Assert.Equal (Dim.Auto (DimAutoStyle.Content), flagSelector.Height);
        Assert.Equal (Orientation.Vertical, flagSelector.Orientation);
    }

    [Fact]
    public void Generic_SetFlags_Methods_Throw ()
    {
        var flagSelector = new FlagSelector<FlagSelectorStyles> ();

        Assert.Throws<InvalidOperationException> (() => flagSelector.SetFlags (new Dictionary<uint, string> ()));
        Assert.Throws<InvalidOperationException> (() => flagSelector.SetFlags<FlagSelectorStyles> ());
        Assert.Throws<InvalidOperationException> (() => flagSelector.SetFlags<FlagSelectorStyles> (styles => null));
    }

    [Fact]
    public void GenericSetFlagNames_ShouldSetFlagNames ()
    {
        var flagSelector = new FlagSelector<FlagSelectorStyles> ();

        flagSelector.SetFlagNames (f => f switch
        {
            FlagSelectorStyles.ShowNone => "Show None Value",
            FlagSelectorStyles.ShowValueEdit => "Show Value Editor",
            FlagSelectorStyles.All => "Everything",
            _ => f.ToString ()
        });

        var expectedFlags = Enum.GetValues<FlagSelectorStyles> ()
                                .ToDictionary (f => Convert.ToUInt32 (f), f => f switch
                                {
                                    FlagSelectorStyles.ShowNone => "Show None Value",
                                    FlagSelectorStyles.ShowValueEdit => "Show Value Editor",
                                    FlagSelectorStyles.All => "Everything",
                                    _ => f.ToString ()
                                });

        Assert.Equal (expectedFlags, flagSelector.Flags);
    }

    [Fact]
    public void GenericValue_Set_ShouldUpdateCheckedState ()
    {
        var flagSelector = new FlagSelector<FlagSelectorStyles> ();

        flagSelector.SetFlagNames (f => f.ToString ());
        flagSelector.Value = FlagSelectorStyles.ShowNone;

        var checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (uint)cb.Data == Convert.ToUInt32 (FlagSelectorStyles.ShowNone));
        Assert.Equal (CheckState.Checked, checkBox.CheckedState);

        checkBox = flagSelector.SubViews.OfType<CheckBox> ().First (cb => (uint)cb.Data == Convert.ToUInt32 (FlagSelectorStyles.ShowValueEdit));
        Assert.Equal (CheckState.UnChecked, checkBox.CheckedState);
    }

    [Fact]
    public void GenericValueChanged_Event_ShouldBeRaised ()
    {
        var flagSelector = new FlagSelector<FlagSelectorStyles> ();

        flagSelector.SetFlagNames (f => f.ToString ());
        bool eventRaised = false;
        flagSelector.ValueChanged += (sender, args) => eventRaised = true;

        flagSelector.Value = FlagSelectorStyles.ShowNone;

        Assert.True (eventRaised);
    }

    [Fact]
    public void Constructors_Defaults ()
    {
        var flagSelector = new FlagSelector ();
        Assert.True (flagSelector.CanFocus);
        Assert.Null (flagSelector.Flags);
        Assert.Equal (Rectangle.Empty, flagSelector.Frame);
        Assert.Null (flagSelector.Value);

        flagSelector = new ();
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 1, "Flag1" },
        });
        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Flags!);
        Assert.Equal ((uint)1, flagSelector.Value);

        flagSelector = new ()
        {
            X = 1,
            Y = 2,
            Width = 20,
            Height = 5,
        };
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 1, "Flag1" },
        });

        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Flags!);
        Assert.Equal (new (1, 2, 20, 5), flagSelector.Frame);
        Assert.Equal ((uint)1, flagSelector.Value);

        flagSelector = new () { X = 1, Y = 2 };
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 1, "Flag1" },
        });

        var view = new View { Width = 30, Height = 40 };
        view.Add (flagSelector);
        view.BeginInit ();
        view.EndInit ();
        view.LayoutSubViews ();

        Assert.True (flagSelector.CanFocus);
        Assert.Single (flagSelector.Flags!);
        Assert.Equal (new (1, 2, 7, 1), flagSelector.Frame);
        Assert.Equal ((uint)1, flagSelector.Value);
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
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 0, "_Left" },
            { 1, "_Right" },
        });

        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Equal ((uint)0, flagSelector.Value);

        flagSelector.NewKeyDownEvent (Key.F.WithAlt);

        Assert.Equal ((uint)0, flagSelector.Value);
        Assert.True (flagSelector.HasFocus);
    }

    [Fact]
    public void HotKey_No_SelectedItem_Selects_First ()
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
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 0, "_Left" },
            { 1, "_Right" },
        });
        flagSelector.Value = null;

        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Null (flagSelector.Value);

        flagSelector.InvokeCommand (Command.HotKey);

        Assert.Equal ((uint)9, flagSelector.Value);
        Assert.False (flagSelector.HasFocus);
    }

    [Fact]
    public void HotKeys_Change_Value_And_Does_Not_SetFocus ()
    {
        var superView = new View
        {
            CanFocus = true
        };
        superView.Add (new View { CanFocus = true });
        var flagSelector = new FlagSelector ();
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 0, "_Left" },
            { 1, "_Right" },
        });
        superView.Add (flagSelector);

        Assert.False (flagSelector.HasFocus);
        Assert.Equal ((uint)0, flagSelector.Value);

        flagSelector.NewKeyDownEvent (Key.R);

        Assert.Equal ((uint)1, flagSelector.Value);
        Assert.False (flagSelector.HasFocus);
    }

    [Fact]
    public void HotKey_Command_Does_Not_Accept ()
    {
        var flagSelector = new FlagSelector ();
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 0, "_Left" },
            { 1, "_Right" },
        });
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
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 0, "_Left" },
            { 1, "_Right" },
        });
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
        uint? newValue = null;
        var flagSelector = new FlagSelector ();
        flagSelector.SetFlags (new Dictionary<uint, string>
        {
            { 0, "_Left" },
            { 1, "_Right" },
        });

        flagSelector.ValueChanged += (s, e) =>
        {
            newValue = e.Value;
        };

        flagSelector.Value = 1;
        Assert.Equal (newValue, flagSelector.Value);
    }

    #region Mouse Tests

    [Fact]
    public void Mouse_Click_Activates ()
    {

    }

    [Fact]
    public void Mouse_DoubleClick_Accepts ()
    {

    }

    #endregion Mouse Tests
}


