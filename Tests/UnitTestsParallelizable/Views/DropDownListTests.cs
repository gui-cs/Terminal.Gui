using System.Collections.ObjectModel;

namespace ViewsTests;

public class DropDownListTests
{
    // Claude - Opus 4.5

    [Fact]
    public void Constructor_InitializesDefaults ()
    {
        DropDownList dropdown = new ();

        Assert.NotNull (dropdown);
        Assert.True (dropdown.ReadOnly);
        Assert.Null (dropdown.Source);
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
    public void ToggleButton_ClickOpensDropdown ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"]))
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Find the toggle button in Padding
        Button? toggleButton = dropdown.Padding?.SubViews.OfType<Button> ().FirstOrDefault ();
        Assert.NotNull (toggleButton);

        // Simulate button click by invoking Accept command
        toggleButton.InvokeCommand (Command.Accept);

        // Check if popover is visible
        Assert.NotNull (app.Popovers);

        // The popover should be registered
        IEnumerable<IPopoverView> popovers = app.Popovers.Popovers;
        // TextField creates a context menu, so we have 2 popovers
        Assert.Equal (2, popovers.Count ());
        // Find the DropDownList popover (not the context menu)
        IPopoverView? dropDownListPopover = popovers.FirstOrDefault (p => (p as View)?.Id == "dropDownListPopover");
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        app.Dispose ();
    }

    [Fact]
    public void F4_OpensDropdown ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"]))
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Press F4
        dropdown.NewKeyDownEvent (Key.F4);

        // Check if popover is visible
        Assert.NotNull (app.Popovers);
        IEnumerable<IPopoverView> popovers = app.Popovers.Popovers;
        // TextField creates a context menu, so we have 2 popovers
        Assert.Equal (2, popovers.Count ());
        // Find the DropDownList popover (not the context menu)
        IPopoverView? dropDownListPopover = popovers.FirstOrDefault (p => (p as View)?.Id == "dropDownListPopover");
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        app.Dispose ();
    }

    [Fact]
    public void AltDown_OpensDropdown ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"]))
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Press Alt+Down
        dropdown.NewKeyDownEvent (Key.CursorDown.WithAlt);

        // Check if popover is visible
        Assert.NotNull (app.Popovers);
        IEnumerable<IPopoverView> popovers = app.Popovers.Popovers;
        // TextField creates a context menu, so we have 2 popovers
        Assert.Equal (2, popovers.Count ());
        // Find the DropDownList popover (not the context menu)
        IPopoverView? dropDownListPopover = popovers.FirstOrDefault (p => (p as View)?.Id == "dropDownListPopover");
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        app.Dispose ();
    }

    [Fact]
    public void ReadOnlyMode_PreventsTextEditing ()
    {
        DropDownList dropdown = new ()
        {
            ReadOnly = true,
            Text = "Initial"
        };
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Try to type text
        dropdown.NewKeyDownEvent (Key.A);

        // Text should not change in ReadOnly mode
        Assert.Equal ("Initial", dropdown.Text);
    }

    [Fact]
    public void EditableMode_AllowsTextEditing ()
    {
        DropDownList dropdown = new ()
        {
            ReadOnly = false,
            Text = "Initial"
        };
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Try to type text
        dropdown.NewKeyDownEvent (Key.A);

        // Text should change in editable mode
        Assert.NotEqual ("Initial", dropdown.Text);
    }

    [Fact]
    public void ValueChanged_RaisedOnSelection ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])),
            ReadOnly = true
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        string? oldValue = null;
        string? newValue = null;
        var valueChangedCalled = false;

        dropdown.ValueChanged += (s, e) =>
        {
            valueChangedCalled = true;
            oldValue = e.OldValue;
            newValue = e.NewValue;
        };

        // Set text to trigger ValueChanged
        dropdown.Text = "Item2";

        Assert.True (valueChangedCalled);
        Assert.Empty (oldValue ?? "");
        Assert.Equal ("Item2", newValue);

        app.Dispose ();
    }

    [Fact]
    public void ValueChanging_RaisedBeforeChange ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])),
            ReadOnly = true,
            Text = "Item1"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        string? currentValue = null;
        string? newValue = null;
        var valueChangingCalled = false;

        dropdown.ValueChanging += (s, e) =>
        {
            valueChangingCalled = true;
            currentValue = e.CurrentValue;
            newValue = e.NewValue;
        };

        // Set text to trigger ValueChanging
        dropdown.Text = "Item2";

        Assert.True (valueChangingCalled);
        Assert.Equal ("Item1", currentValue);
        Assert.Equal ("Item2", newValue);

        app.Dispose ();
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2", "Item3"])),
            ReadOnly = true,
            Text = "Item1"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        dropdown.ValueChanging += (s, e) =>
        {
            // Cancel the change
            e.Handled = true;
        };

        // Try to set text
        dropdown.Text = "Item2";

        // Text should remain unchanged
        Assert.Equal ("Item1", dropdown.Text);

        app.Dispose ();
    }

    [Fact]
    public void PreSelection_SelectsMatchingItem ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        ObservableCollection<string> items = ["Item1", "Item2", "Item3"];
        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (items),
            ReadOnly = true,
            Text = "Item2"
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open dropdown with F4
        dropdown.NewKeyDownEvent (Key.F4);

        // Get the popover's ListView (filter out the context menu)
        IPopoverView? popover = app.Popovers?.Popovers.FirstOrDefault (p => (p as View)?.Id == "dropDownListPopover");
        Assert.NotNull (popover);

        // Access the ListView (popover is Popover<ListView, string?>)
        var listView = (popover as dynamic)?.ContentView as ListView;
        Assert.NotNull (listView);

        // Check that Item2 (index 1) is selected
        Assert.Equal (1, listView.SelectedItem);

        app.Dispose ();
    }

    [Fact]
    public void ToggleCommand_OpensAndClosesDropdown ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"]))
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open with F4
        dropdown.NewKeyDownEvent (Key.F4);

        IEnumerable<IPopoverView> popovers = app.Popovers!.Popovers;
        // TextField creates a context menu, so we have 2 popovers
        Assert.Equal (2, popovers.Count ());
        // Find the DropDownList popover (not the context menu)
        IPopoverView? dropDownListPopover = popovers.FirstOrDefault (p => (p as View)?.Id == "dropDownListPopover");
        Assert.NotNull (dropDownListPopover);
        Assert.True (dropDownListPopover.Visible);

        // Close with F4 again
        dropdown.NewKeyDownEvent (Key.F4);

        Assert.False (dropDownListPopover.Visible);

        app.Dispose ();
    }

    [Fact]
    public void FocusGain_AutoRegistersPopover ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"]))
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();

        // Initially, no popovers registered (not focused yet)
        Assert.Empty (app.Popovers!.Popovers);

        // Set focus - should auto-register both context menu and DropDownList popover
        dropdown.SetFocus ();

        // Now both context menu and DropDownList popover are registered
        Assert.Equal (2, app.Popovers.Popovers.Count ());

        app.Dispose ();
    }

    [Fact]
    public void Dispose_CleansUpResources ()
    {
        ApplicationImpl app = new ();
        ApplicationPopover popoverManager = new () { App = app };
        app.Popovers = popoverManager;

        DropDownList dropdown = new ()
        {
            Source = new ListWrapper<string> (new ObservableCollection<string> (["Item1", "Item2"]))
        };
        dropdown.App = app;
        dropdown.BeginInit ();
        dropdown.EndInit ();
        dropdown.SetFocus ();

        // Open dropdown
        dropdown.NewKeyDownEvent (Key.F4);

        // Both context menu and DropDownList popover are registered
        Assert.Equal (2, app.Popovers!.Popovers.Count ());

        // Dispose dropdown - should deregister DropDownList popover and context menu
        dropdown.Dispose ();

        // Both popovers should be deregistered when dropdown is disposed
        Assert.Empty (app.Popovers.Popovers);

        app.Dispose ();
    }

    [Fact]
    public void IValue_InterfaceWorks ()
    {
        DropDownList dropdown = new ()
        {
            Text = "Initial"
        };

        // Test IValue<string> interface
        IValue<string> value = dropdown;

        Assert.Equal ("Initial", value.Value);

        value.Value = "Updated";

        Assert.Equal ("Updated", dropdown.Text);
        Assert.Equal ("Updated", value.Value);
    }
}
