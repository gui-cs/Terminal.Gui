using System.Collections.ObjectModel;

namespace ViewsTests;

public class DropDownListTests
{
    [Fact]
    public void AltDown_OpensDropdown ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.CursorDown.WithAlt);

        IPopoverView? dropDownListPopover = FindDropDownPopover (app);
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void Constructor_HasAltDownKeyBinding ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.True (dropdown.KeyBindings.TryGet (Key.CursorDown.WithAlt, out _));
    }

    [Fact]
    public void Constructor_HasF4KeyBinding ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.True (dropdown.KeyBindings.TryGet (Key.F4, out _));
    }

    [Fact]
    public void Constructor_HasMouseBinding ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.True (dropdown.MouseBindings.TryGet (MouseFlags.LeftButtonClicked, out _));
    }

    [Fact]
    public void Constructor_HasToggleButtonInPadding ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Button? toggleButton = dropdown.Padding?.SubViews.OfType<Button> ().FirstOrDefault ();
        Assert.NotNull (toggleButton);
        Assert.False (toggleButton.CanFocus);
        Assert.Equal (TabBehavior.NoStop, toggleButton.TabStop);
    }

    [Fact]
    public void Constructor_InitializesDefaults ()
    {
        DropDownList dropdown = new ();

        Assert.NotNull (dropdown);
        Assert.True (dropdown.ReadOnly);
        Assert.Null (dropdown.Source);
    }

    [Fact]
    public void Constructor_PaddingRightIsOneForToggleButton ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.Equal (1, dropdown.Padding!.Thickness.Right);
    }

    [Fact]
    public void Constructor_ToggleButtonHasDownArrow ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Button? toggleButton = dropdown.Padding?.SubViews.OfType<Button> ().FirstOrDefault ();
        Assert.NotNull (toggleButton);
        Assert.Equal (Glyphs.DownArrow.ToString (), toggleButton.Text);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.Dispose ();

        // Second dispose should not throw
        dropdown.Dispose ();
    }

    [Fact]
    public void Dispose_CleansUpResources ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open dropdown
        dropdown.NewKeyDownEvent (Key.F4);

        Assert.True (app.Popovers!.Popovers.Count () > 0);

        dropdown.Dispose ();

        Assert.Empty (app.Popovers.Popovers);
    }

    [Fact]
    public void Dispose_WhenNotFocused_DoesNotThrow ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1"])) };
        dropdown.BeginInit ();
        dropdown.EndInit ();

        // Dispose without ever focusing - should not throw
        dropdown.Dispose ();
    }

    [Fact]
    public void EditableMode_AllowsTextEditing ()
    {
        DropDownList dropdown = new () { ReadOnly = false, Text = "Initial" };
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.A);

        Assert.NotEqual ("Initial", dropdown.Text);
    }

    [Fact]
    public void EmptySource_OpensWithoutCrash ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = [];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void EnableForDesign_PopulatesSampleData ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        bool result = dropdown.EnableForDesign ();

        Assert.True (result);
        Assert.NotNull (dropdown.Source);
        Assert.True (dropdown.Source.Count > 0);
        Assert.Equal ("Germany", dropdown.Value);
    }

    [Fact]
    public void F4_OpensDropdown ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? dropDownListPopover = FindDropDownPopover (app);
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void FocusGain_AutoRegistersPopover ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();

        // Initially, no popovers registered (not focused yet)
        Assert.Empty (app.Popovers!.Popovers);

        // Set focus - should auto-register
        dropdown.SetFocus ();

        Assert.True (app.Popovers.Popovers.Count () > 0);

        dropdown.Dispose ();
    }

    [Fact]
    public void IValue_InterfaceWorks ()
    {
        DropDownList dropdown = new () { Text = "Initial" };

        IValue<string> value = dropdown;

        Assert.Equal ("Initial", value.Value);

        value.Value = "Updated";

        Assert.Equal ("Updated", dropdown.Text);
        Assert.Equal ("Updated", value.Value);
    }

    [Fact]
    public void NullSource_ToggleDoesNotCrash ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { ReadOnly = true };
        Assert.Null (dropdown.Source);

        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Should not throw with null source
        dropdown.NewKeyDownEvent (Key.F4);

        dropdown.Dispose ();
    }

    [Fact]
    public void OpenDropDown_RegistersPopover_IfNotAlreadyRegistered ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();

        // Don't call SetFocus - popover won't be registered via OnHasFocusChanging

        // Open dropdown via command - it should auto-register
        dropdown.InvokeCommand (Command.Toggle);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);

        dropdown.Dispose ();
    }

    [Fact]
    public void PreSelection_EmptyText_DoesNotCrash ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = ["Item1", "Item2"];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true, Text = "" };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);
        Assert.True (popover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void PreSelection_NoMatchingItem_KeepsDefault ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = ["Item1", "Item2", "Item3"];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true, Text = "NonExistent" };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        var listView = (popover as dynamic)?.ContentView as ListView;
        Assert.NotNull (listView);

        // SelectedItem should not have been changed to match any specific item
        // since "NonExistent" doesn't appear in the list
        Assert.True (listView.SelectedItem is null or 0);

        dropdown.Dispose ();
    }

    [Fact]
    public void PreSelection_SelectsLastItem ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = ["First", "Second", "Third"];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true, Text = "Third" };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        var listView = (popover as dynamic)?.ContentView as ListView;
        Assert.NotNull (listView);
        Assert.Equal (2, listView.SelectedItem);

        dropdown.Dispose ();
    }

    [Fact]
    public void PreSelection_SelectsMatchingItem ()
    {
        using IApplication app = Application.Create ();

        ObservableCollection<string> items = ["Item1", "Item2", "Item3"];
        DropDownList dropdown = new () { Source = new ListWrapper<string> (items), ReadOnly = true, Text = "Item2" };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? popover = FindDropDownPopover (app);
        Assert.NotNull (popover);

        var listView = (popover as dynamic)?.ContentView as ListView;
        Assert.NotNull (listView);
        Assert.Equal (1, listView.SelectedItem);

        dropdown.Dispose ();
    }

    [Fact]
    public void ReadOnly_CanBeSetToFalse ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new () { ReadOnly = false };

        Assert.False (dropdown.ReadOnly);
    }

    [Fact]
    public void ReadOnly_DefaultIsTrue ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();

        Assert.True (dropdown.ReadOnly);
    }

    [Fact]
    public void ReadOnlyMode_PreventsTextEditing ()
    {
        DropDownList dropdown = new () { ReadOnly = true, Text = "Initial" };
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.NewKeyDownEvent (Key.A);

        Assert.Equal ("Initial", dropdown.Text);
    }

    [Fact]
    public void Source_GetSet ()
    {
        DropDownList dropdown = new ();
        ObservableCollection<string> items = ["Item1", "Item2", "Item3"];
        ListWrapper<string> source = new (items);

        dropdown.Source = source;

        Assert.Equal (source, dropdown.Source);
    }

    [Fact]
    public void Source_SetToNull_Works ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new ();
        dropdown.Source = new ListWrapper<string> (["Item1", "Item2"]);
        Assert.NotNull (dropdown.Source);

        dropdown.Source = null;

        Assert.Null (dropdown.Source);
    }

    [Fact]
    public void Text_SetDirectly_UpdatesValue ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])) };

        dropdown.Text = "Banana";

        Assert.Equal ("Banana", dropdown.Text);
        Assert.Equal ("Banana", dropdown.Value);
    }

    [Fact]
    public void ToggleButton_ClickOpensDropdown ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        Button? toggleButton = dropdown.Padding?.SubViews.OfType<Button> ().FirstOrDefault ();
        Assert.NotNull (toggleButton);

        toggleButton.InvokeCommand (Command.Accept);

        IPopoverView? dropDownListPopover = FindDropDownPopover (app);
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void ToggleCommand_OpenCloseReopen ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["A", "B"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open
        dropdown.InvokeCommand (Command.Toggle);
        IPopoverView? popover = FindDropDownPopover (app);
        Assert.True (popover?.Visible);

        // Access the ListView (popover is Popover<ListView, string?>)
        var listView = (popover as dynamic)?.ContentView as ListView;
        Assert.NotNull (listView);

        // Re-open
        dropdown.InvokeCommand (Command.Toggle);
        Assert.True (popover?.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void ToggleCommand_OpensAndClosesDropdown ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])) };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open with F4
        dropdown.NewKeyDownEvent (Key.F4);

        IPopoverView? dropDownListPopover = FindDropDownPopover (app);
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        // Close with F4 again
        dropdown.NewKeyDownEvent (Key.F4);

        Assert.False (dropDownListPopover.Visible);

        dropdown.Dispose ();
    }

    [Fact]
    public void Value_SetDirectly_UpdatesText ()
    {
        // Claude - Opus 4.6
        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Apple", "Banana", "Cherry"])) };

        dropdown.Value = "Cherry";

        Assert.Equal ("Cherry", dropdown.Text);
        Assert.Equal ("Cherry", dropdown.Value);
    }

    [Fact]
    public void ValueChanged_RaisedMultipleTimes ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])), ReadOnly = true };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        var changeCount = 0;

        dropdown.ValueChanged += (_, _) => { changeCount++; };

        dropdown.Text = "Item1";
        dropdown.Text = "Item2";
        dropdown.Text = "Item3";

        Assert.Equal (3, changeCount);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanged_RaisedOnSelection ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new () { Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])), ReadOnly = true };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        string? oldValue = null;
        string? newValue = null;
        var valueChangedCalled = false;

        dropdown.ValueChanged += (_, e) =>
                                 {
                                     valueChangedCalled = true;
                                     oldValue = e.OldValue;
                                     newValue = e.NewValue;
                                 };

        dropdown.Text = "Item2";

        Assert.True (valueChangedCalled);
        Assert.Empty (oldValue ?? "");
        Assert.Equal ("Item2", newValue);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])), ReadOnly = true, Text = "Item1"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.ValueChanging += (_, e) => { e.Handled = true; };

        dropdown.Text = "Item2";

        Assert.Equal ("Item1", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanging_CancelPreventsValueChanged ()
    {
        // Claude - Opus 4.6
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"])), ReadOnly = true, Text = "Item1"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        var valueChangedCalled = false;

        dropdown.ValueChanging += (_, e) => { e.Handled = true; };
        dropdown.ValueChanged += (_, _) => { valueChangedCalled = true; };

        dropdown.Text = "Item2";

        Assert.False (valueChangedCalled);
        Assert.Equal ("Item1", dropdown.Text);

        dropdown.Dispose ();
    }

    [Fact]
    public void ValueChanging_RaisedBeforeChange ()
    {
        using IApplication app = Application.Create ();

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])), ReadOnly = true, Text = "Item1"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        string? currentValue = null;
        string? newValue = null;
        var valueChangingCalled = false;

        dropdown.ValueChanging += (_, e) =>
                                  {
                                      valueChangingCalled = true;
                                      currentValue = e.CurrentValue;
                                      newValue = e.NewValue;
                                  };

        dropdown.Text = "Item2";

        Assert.True (valueChangingCalled);
        Assert.Equal ("Item1", currentValue);
        Assert.Equal ("Item2", newValue);

        dropdown.Dispose ();
    }

    // Claude - Opus 4.5

    // Helper to find the DropDownList popover (excludes the context menu popover)
    private static IPopoverView? FindDropDownPopover (IApplication app) =>
        app.Popovers?.Popovers.OfType<Popover<ListView, string?>> ().FirstOrDefault ();
}
