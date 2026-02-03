# Terminal.Gui Common Patterns Cookbook

> **Recipes for common UI patterns in Terminal.Gui applications.**

## Table of Contents

1. [Form with Validation](#form-with-validation)
2. [List with Selection](#list-with-selection)
3. [Menu Bar Application](#menu-bar-application)
4. [Split View Layout](#split-view-layout)
5. [Tab View](#tab-view)
6. [Progress Indicator](#progress-indicator)
7. [File Browser](#file-browser)
8. [Data Table](#data-table)
9. [Tree View](#tree-view)
10. [Status Bar with Shortcuts](#status-bar-with-shortcuts)

---

## Form with Validation

A typical form with labels, text fields, and validation.

```csharp
public sealed class FormWindow : Runnable<FormData?>
{
    public FormWindow ()
    {
        Title = "User Registration";
        Width = 50;
        Height = 15;

        Label nameLabel = new () { Text = "Name:", Y = 1 };
        TextField nameField = new () { X = 12, Y = 1, Width = Dim.Fill (1) };

        Label emailLabel = new () { Text = "Email:", Y = 3 };
        TextField emailField = new () { X = 12, Y = 3, Width = Dim.Fill (1) };

        Label ageLabel = new () { Text = "Age:", Y = 5 };
        NumericUpDown<int> ageField = new () { X = 12, Y = 5, Value = 18 };

        Label errorLabel = new ()
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill (1),
            ColorScheme = Colors.ColorSchemes ["Error"]
        };

        Button submitButton = new ()
        {
            Text = "Submit",
            X = Pos.Center (),
            Y = 9,
            IsDefault = true
        };

        submitButton.Accepting += (_, e) =>
        {
            // Validate
            if (string.IsNullOrWhiteSpace (nameField.Text))
            {
                errorLabel.Text = "Name is required";
                nameField.SetFocus ();
                e.Handled = true;
                return;
            }

            if (!emailField.Text.Contains ('@'))
            {
                errorLabel.Text = "Invalid email address";
                emailField.SetFocus ();
                e.Handled = true;
                return;
            }

            // Success - return data
            Result = new FormData (nameField.Text, emailField.Text, ageField.Value);
            App!.RequestStop ();
            e.Handled = true;
        };

        Add (nameLabel, nameField, emailLabel, emailField, ageLabel, ageField, errorLabel, submitButton);
    }
}

public record FormData (string Name, string Email, int Age);
```

---

## List with Selection

A scrollable list with item selection and actions.

```csharp
public sealed class ListWindow : Runnable
{
    private readonly ListView _listView;
    private readonly Label _detailLabel;
    private readonly List<string> _items = ["Apple", "Banana", "Cherry", "Date", "Elderberry"];

    public ListWindow ()
    {
        Title = "Fruit Picker";

        _listView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent (50),
            Height = Dim.Fill (2),
            Source = new ListWrapper<string> (_items)
        };

        _detailLabel = new ()
        {
            X = Pos.Right (_listView) + 1,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (2),
            Text = "Select an item"
        };

        Button selectButton = new ()
        {
            Text = "Select",
            X = Pos.Center (),
            Y = Pos.AnchorEnd (1)
        };

        _listView.SelectedItemChanged += (_, e) =>
        {
            if (e.Value >= 0 && e.Value < _items.Count)
            {
                _detailLabel.Text = $"Selected: {_items [e.Value]}";
            }
        };

        selectButton.Accepting += (_, e) =>
        {
            if (_listView.SelectedItem >= 0)
            {
                MessageBox.Query (App!, "Selection", $"You selected: {_items [_listView.SelectedItem]}", "OK");
            }
            e.Handled = true;
        };

        Add (_listView, _detailLabel, selectButton);
    }
}
```

---

## Menu Bar Application

Application with a menu bar and standard menu items.

```csharp
public sealed class MenuApp : Runnable
{
    public MenuApp ()
    {
        Title = "Menu Demo";

        MenuBar menuBar = new ()
        {
            Menus =
            [
                new MenuBarItem (
                    "File",
                    [
                        new MenuItem ("New", "", () => NewFile (), null, null, KeyCode.N | KeyCode.CtrlMask),
                        new MenuItem ("Open...", "", () => OpenFile (), null, null, KeyCode.O | KeyCode.CtrlMask),
                        new MenuItem ("Save", "", () => SaveFile (), null, null, KeyCode.S | KeyCode.CtrlMask),
                        null, // Separator
                        new MenuItem ("Exit", "", () => App!.RequestStop (), null, null, KeyCode.Q | KeyCode.CtrlMask)
                    ]),
                new MenuBarItem (
                    "Edit",
                    [
                        new MenuItem ("Cut", "", null, null, null, KeyCode.X | KeyCode.CtrlMask),
                        new MenuItem ("Copy", "", null, null, null, KeyCode.C | KeyCode.CtrlMask),
                        new MenuItem ("Paste", "", null, null, null, KeyCode.V | KeyCode.CtrlMask)
                    ]),
                new MenuBarItem (
                    "Help",
                    [
                        new MenuItem ("About...", "", () => ShowAbout ())
                    ])
            ]
        };

        TextView editor = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        Add (menuBar, editor);
    }

    private void NewFile () => MessageBox.Query (App!, "New", "New file created", "OK");
    private void OpenFile () { /* Show OpenDialog */ }
    private void SaveFile () { /* Show SaveDialog */ }
    private void ShowAbout () => MessageBox.Query (App!, "About", "Menu Demo v1.0", "OK");
}
```

---

## Split View Layout

Horizontal or vertical split layout with resizable panes.

```csharp
public sealed class SplitWindow : Runnable
{
    public SplitWindow ()
    {
        Title = "Split View Demo";

        TileView tileView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Orientation = Orientation.Vertical  // or Horizontal
        };

        // Left pane
        FrameView leftPane = new () { Title = "Left Pane" };
        leftPane.Add (new Label { Text = "Left content", X = 1, Y = 1 });

        // Right pane
        FrameView rightPane = new () { Title = "Right Pane" };
        rightPane.Add (new Label { Text = "Right content", X = 1, Y = 1 });

        tileView.Tiles.ElementAt (0).ContentView.Add (leftPane);
        tileView.Tiles.ElementAt (1).ContentView.Add (rightPane);

        Add (tileView);
    }
}
```

---

## Tab View

Tabbed interface with multiple content pages.

```csharp
public sealed class TabbedWindow : Runnable
{
    public TabbedWindow ()
    {
        Title = "Tabbed Interface";

        TabView tabView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        // Tab 1: Settings
        View settingsTab = new ();
        settingsTab.Add (
            new Label { Text = "Enable Feature:", X = 1, Y = 1 },
            new CheckBox { X = 20, Y = 1, Text = "Enabled" }
        );

        // Tab 2: About
        View aboutTab = new ();
        aboutTab.Add (new Label { Text = "Version 1.0.0", X = 1, Y = 1 });

        tabView.AddTab (new Tab { DisplayText = "Settings", View = settingsTab }, false);
        tabView.AddTab (new Tab { DisplayText = "About", View = aboutTab }, false);

        Add (tabView);
    }
}
```

---

## Progress Indicator

Long-running operation with progress feedback.

```csharp
public sealed class ProgressWindow : Runnable
{
    private readonly ProgressBar _progressBar;
    private readonly Label _statusLabel;
    private int _progress;

    public ProgressWindow ()
    {
        Title = "Processing...";
        Width = 50;
        Height = 8;

        _statusLabel = new ()
        {
            Text = "Starting...",
            X = Pos.Center (),
            Y = 1
        };

        _progressBar = new ()
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill (1),
            ProgressBarStyle = ProgressBarStyle.Continuous
        };

        Button cancelButton = new ()
        {
            Text = "Cancel",
            X = Pos.Center (),
            Y = 5
        };

        cancelButton.Accepting += (_, e) =>
        {
            App!.RequestStop ();
            e.Handled = true;
        };

        Add (_statusLabel, _progressBar, cancelButton);
    }

    public override void OnLoaded ()
    {
        base.OnLoaded ();

        // Start simulated work
        App!.AddTimeout (TimeSpan.FromMilliseconds (100), () =>
        {
            _progress += 5;
            _progressBar.Fraction = _progress / 100f;
            _statusLabel.Text = $"Processing... {_progress}%";

            if (_progress >= 100)
            {
                _statusLabel.Text = "Complete!";
                return false;  // Stop timer
            }
            return true;  // Continue timer
        });
    }
}
```

---

## File Browser

Open and save file dialogs.

```csharp
// Open single file
private void OpenFile ()
{
    OpenDialog dialog = new ()
    {
        Title = "Open File",
        AllowsMultipleSelection = false,
        DirectoryPath = Environment.CurrentDirectory
    };

    // Optional: filter by extension
    dialog.AllowedTypes = [new AllowedType ("Text Files", ".txt", ".md")];

    App!.Run (dialog);

    if (!dialog.Canceled && dialog.FilePaths.Any ())
    {
        string path = dialog.FilePaths.First ();
        // Process the file...
    }
}

// Save file
private void SaveFile ()
{
    SaveDialog dialog = new ()
    {
        Title = "Save File",
        DirectoryPath = Environment.CurrentDirectory,
        FileName = "document.txt"
    };

    App!.Run (dialog);

    if (!dialog.Canceled && !string.IsNullOrEmpty (dialog.FileName))
    {
        string path = dialog.FileName;
        // Save to path...
    }
}

// Open multiple files
private void OpenMultipleFiles ()
{
    OpenDialog dialog = new ()
    {
        Title = "Select Files",
        AllowsMultipleSelection = true
    };

    App!.Run (dialog);

    if (!dialog.Canceled)
    {
        foreach (string path in dialog.FilePaths)
        {
            // Process each file...
        }
    }
}
```

---

## Data Table

Display tabular data with TableView.

```csharp
public sealed class DataTableWindow : Runnable
{
    public DataTableWindow ()
    {
        Title = "Data Table";

        // Create DataTable
        DataTable table = new ();
        table.Columns.Add ("ID", typeof (int));
        table.Columns.Add ("Name", typeof (string));
        table.Columns.Add ("Price", typeof (decimal));
        table.Columns.Add ("InStock", typeof (bool));

        // Add rows
        table.Rows.Add (1, "Widget", 9.99m, true);
        table.Rows.Add (2, "Gadget", 19.99m, false);
        table.Rows.Add (3, "Doohickey", 4.99m, true);
        table.Rows.Add (4, "Thingamajig", 29.99m, true);

        TableView tableView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            Table = new DataTableSource (table),
            FullRowSelect = true
        };

        Label statusLabel = new ()
        {
            X = 0,
            Y = Pos.AnchorEnd (1),
            Text = "Select a row"
        };

        tableView.SelectedCellChanged += (_, e) =>
        {
            if (e.NewRow >= 0 && e.NewRow < table.Rows.Count)
            {
                DataRow row = table.Rows [e.NewRow];
                statusLabel.Text = $"Selected: {row ["Name"]} - ${row ["Price"]}";
            }
        };

        Add (tableView, statusLabel);
    }
}
```

---

## Tree View

Hierarchical data display.

```csharp
public sealed class TreeWindow : Runnable
{
    public TreeWindow ()
    {
        Title = "Tree View";

        TreeView<FileSystemInfo> treeView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        // Set up tree builder for file system
        treeView.TreeBuilder = new DelegateTreeBuilder<FileSystemInfo> (
            GetChildren,
            CanExpand
        );

        // Add root nodes (drives or directories)
        DirectoryInfo root = new (Environment.CurrentDirectory);
        treeView.AddObject (root);

        treeView.SelectionChanged += (_, e) =>
        {
            if (e.NewValue is FileInfo file)
            {
                Title = $"Selected: {file.Name}";
            }
        };

        Add (treeView);
    }

    private IEnumerable<FileSystemInfo> GetChildren (FileSystemInfo parent)
    {
        if (parent is DirectoryInfo dir)
        {
            try
            {
                return dir.GetFileSystemInfos ();
            }
            catch
            {
                return [];
            }
        }
        return [];
    }

    private bool CanExpand (FileSystemInfo item) => item is DirectoryInfo;
}
```

---

## Status Bar with Shortcuts

Application status bar with keyboard shortcuts.

```csharp
public sealed class StatusBarApp : Runnable
{
    public StatusBarApp ()
    {
        Title = "Status Bar Demo";

        TextView editor = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (1)  // Leave room for status bar
        };

        StatusBar statusBar = new ()
        {
            Y = Pos.AnchorEnd (1),
            Visible = true
        };

        statusBar.Add (
            new Shortcut
            {
                Title = "New",
                Key = Key.N.WithCtrl,
                Action = () => MessageBox.Query (App!, "New", "Creating new file...", "OK")
            },
            new Shortcut
            {
                Title = "Save",
                Key = Key.S.WithCtrl,
                Action = () => MessageBox.Query (App!, "Save", "Saving file...", "OK")
            },
            new Shortcut
            {
                Title = "Quit",
                Key = Key.Q.WithCtrl,
                Action = () => App!.RequestStop ()
            }
        );

        Add (editor, statusBar);
    }
}
```

---

## Tips for All Patterns

1. **Always use `e.Handled = true`** in `Accepting` event handlers
2. **Use `Dim.Fill()` and `Pos.Center()`** instead of hardcoded values
3. **Call `App!.RequestStop()`** to close the current window
4. **Use `MessageBox.Query`** for simple dialogs
5. **Dispose resources properly** when the window closes
