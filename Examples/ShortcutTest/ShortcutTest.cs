// Test app for Command Propagation through Shortcut hierarchy
// Tests: CheckBox (CommandView) -> Shortcut -> Window

using System.Collections.ObjectModel;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

ConfigurationManager.Enable (ConfigLocations.All);

using IApplication app = Application.Create ().Init ();
app.Run<ShortcutTestWindow> ();

public sealed class ShortcutTestWindow : Window
{
    private readonly ObservableCollection<string> _eventLog = [];
    private readonly ListView _eventLogView;

    public ShortcutTestWindow ()
    {
        Title = $"Shortcut Command Propagation Test ({Application.QuitKey} to quit)";

        AssignHotKeys = true;

        // Event log on the right
        _eventLogView = new ListView
        {
            X = Pos.AnchorEnd (),
            Y = 0,
            Width = Dim.Percent (50),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (_eventLog),
            BorderStyle = LineStyle.Double,
            Title = "Event Log"
        };
        Add (_eventLogView);

        // Test Shortcut 1: CheckBox CommandView
        CheckBox cb1 = new () { Text = "Option 1", CanFocus = false };
        var shortcut1 = new Shortcut
        {
            MouseHighlightStates = MouseState.In,
            HelpText = "Option1",
            X = 0,
            Y = 0,
            Width = Dim.Fill () - Dim.Width (_eventLogView),
            CommandView = cb1,
            Key = Key.F5
        };
        shortcut1.Activating += (_, args) => LogEvent ("Shortcut1.Activating", args);
        shortcut1.Accepting += (_, args) => LogEvent ("Shortcut1.Accepting", args);
        cb1.Activating += (_, args) => LogEvent ("CB1.Activating", args);
        cb1.ValueChanged += (_, args) => LogEvent ($"CB1.ValueChanged: {args.OldValue} -> {args.NewValue}", null);
        Add (shortcut1);

        // Test Shortcut 2: CheckBox CommandView (CanFocus = true)
        var shortcut2 = new Shortcut
        {
            MouseHighlightStates = MouseState.In,
            HelpText = "Option2",
            X = 0,
            Y = Pos.Bottom (shortcut1) + 1,
            Width = Dim.Fill () - Dim.Width (_eventLogView),
            CommandView = new CheckBox { Text = "Option 2 (CanFocus)", CanFocus = true },
            Key = Key.F6
        };
        shortcut2.Activating += (_, args) => LogEvent ("Shortcut2.Activating", args);
        shortcut2.Accepting += (_, args) => LogEvent ("Shortcut2.Accepting", args);
        Add (shortcut2);

        // Test Shortcut 3: Button CommandView
        var shortcut3 = new Shortcut
        {
            HelpText = "Button",
            MouseHighlightStates = MouseState.In,
            X = 0,
            Y = Pos.Bottom (shortcut2) + 1,
            Width = Dim.Fill () - Dim.Width (_eventLogView),
            CommandView = new Button { Text = "_Action Button" },
            Key = Key.F7
        };
        shortcut3.Activating += (_, args) => LogEvent ("Shortcut3.Activating", args);
        shortcut3.Accepting += (_, args) => LogEvent ("Shortcut3.Accepting", args);
        Add (shortcut3);

        // Instructions
        var instructions = new Label
        {
            X = 0,
            Y = Pos.Bottom (shortcut3) + 2,
            Width = Dim.Fill () - Dim.Width (_eventLogView),
            Text = "Press F5, F6, or F7 to trigger shortcuts.\nClick checkboxes with mouse.\nWatch event log to see command propagation."
        };
        Add (instructions);

        // Window level handlers
        Activating += (_, args) => LogEvent ("Window.Activating", args);
        Accepting += (_, args) =>
                     {
                         LogEvent ("Window.Accepting", args);
                         args.Handled = true;
                     };
    }

    private void LogEvent (string source, CommandEventArgs? args)
    {
        string entry;

        if (args is null)
        {
            entry = source;
        }
        else
        {
            View? sourceView = null;
            args.Context?.Source?.TryGetTarget (out sourceView);
            string bindingType = args.Context?.Binding?.GetType ().Name ?? "null";
            entry = $"{source}: Cmd={args.Context?.Command}, Binding={bindingType}, Src={sourceView?.Id ?? "null"}, Handled={args.Handled}";
        }

        _eventLog.Add (entry);
        _eventLogView.MoveDown ();
    }
}
