# Building a Terminal.Gui Application

> **For AI agents helping users build apps with Terminal.Gui.**
> This guide focuses on app development, not library contribution.

## Quick Assessment

When a user says "I want to build a terminal.gui app that does XYZ":

1. **Is this a new project?** Use the templates (see Setup below)
2. **Adding to existing project?** Add NuGet package
3. **What UI patterns are needed?** See Common Patterns below

## Project Setup

### New Project (Recommended)
```bash
dotnet new install Terminal.Gui.Templates@2.0.0-alpha.*
dotnet new tui-simple -n ProjectName
cd ProjectName
dotnet run
```

### Existing Project
```bash
dotnet add package Terminal.Gui
```

### Required Namespaces
```csharp
using Terminal.Gui.App;          // Application, IApplication
using Terminal.Gui.Views;        // All controls (Button, Label, etc.)
using Terminal.Gui.ViewBase;     // View, Pos, Dim
using Terminal.Gui.Drawing;      // Colors, Attribute, LineStyle
using Terminal.Gui.Input;        // Key, KeyCode, MouseFlags
using Terminal.Gui.Configuration; // ConfigurationManager (optional)
```

## Application Structure

### Modern Pattern (Recommended)
```csharp
using Terminal.Gui.App;
using Terminal.Gui.Views;

// Create and initialize application
IApplication app = Application.Create ().Init ();

// Run the main window
app.Run<MainWindow> ();

// Clean up
app.Dispose ();

// Main window class
public sealed class MainWindow : Runnable
{
    public MainWindow ()
    {
        Title = "My App (Esc to quit)";

        // Add controls here
        Button button = new () { Text = "Click Me", X = Pos.Center (), Y = Pos.Center () };
        button.Accepting += (_, e) =>
        {
            MessageBox.Query (App!, "Hello", "Button clicked!", "OK");
            e.Handled = true;
        };

        Add (button);
    }
}
```

### With Return Value
```csharp
public sealed class LoginWindow : Runnable<string?>
{
    public LoginWindow ()
    {
        // ... setup UI ...

        loginButton.Accepting += (_, e) =>
        {
            Result = usernameField.Text;  // Set return value
            App!.RequestStop ();          // Close window
            e.Handled = true;
        };
    }
}

// Usage:
string? username = app.Run<LoginWindow> ().GetResult<string> ();
```

## Layout System

### Position (Pos)
```csharp
X = 5;                           // Absolute: 5 from left
X = Pos.Center ();               // Centered horizontally
X = Pos.Right (otherView) + 1;   // 1 right of another view
X = Pos.Left (otherView);        // Aligned with left of another view
X = Pos.Percent (25);            // 25% from left
X = Pos.AnchorEnd (10);          // 10 from right edge
```

### Size (Dim)
```csharp
Width = 20;                      // Absolute: 20 characters
Width = Dim.Fill ();             // Fill remaining width
Width = Dim.Fill (1);            // Fill minus 1 character margin
Width = Dim.Auto ();             // Size to content
Width = Dim.Percent (50);        // 50% of container
Width = Dim.Width (otherView);   // Same width as another view
```

## API Reference

Consult these compressed API files for available types:

| File | Contents |
|------|----------|
| `docfx/apispec/namespace-app.md` | Application, IApplication, Clipboard |
| `docfx/apispec/namespace-views.md` | All UI controls (Button, Label, etc.) |
| `docfx/apispec/namespace-viewbase.md` | View, Pos, Dim, Adornments |
| `docfx/apispec/namespace-drawing.md` | Colors, LineStyle, Attribute |
| `docfx/apispec/namespace-input.md` | Key, KeyCode, Mouse handling |
| `docfx/apispec/namespace-text.md` | Text manipulation, autocomplete |

## Common Patterns

See `.claude/cookbook/common-patterns.md` for recipes including:
- Form with validation
- List with selection
- Menu bar and dialogs
- Split views and tabs
- File dialogs
- Progress indicators

## Examples to Study

| Example | Location | Description |
|---------|----------|-------------|
| Hello World | `Examples/Example/` | Minimal login form |
| All Controls | `Examples/UICatalog/` | Comprehensive demo app |
| MVVM | `Examples/CommunityToolkitExample/` | CommunityToolkit MVVM |
| Reactive | `Examples/ReactiveExample/` | ReactiveUI integration |

## Event Handling Patterns

### Button Click
```csharp
button.Accepting += (sender, e) =>
{
    // Handle the click
    e.Handled = true;  // Prevent further processing
};
```

### Text Changed
```csharp
textField.TextChanged += (_, _) =>
{
    // React to text changes
};
```

### Selection Changed
```csharp
listView.SelectedItemChanged += (_, e) =>
{
    SelectedItem item = e.Value;
};
```

### Keyboard Shortcuts
```csharp
// Add a key binding
view.AddCommand (Command.Accept, myHandler);
view.KeyBindings.Add (Key.F5, Command.Accept);
```

## Dialogs

### Message Box
```csharp
int result = MessageBox.Query (App!, "Title", "Message", "Yes", "No");
// result: 0 = Yes, 1 = No
```

### Error Dialog
```csharp
MessageBox.ErrorQuery (App!, "Error", "Something went wrong", "OK");
```

### Custom Dialog
```csharp
Dialog dialog = new ()
{
    Title = "Custom Dialog",
    Width = 40,
    Height = 10,
    Buttons = [new Button ("OK"), new Button ("Cancel")]
};
// Add controls to dialog...
app.Run (dialog);
```

### File Dialogs
```csharp
OpenDialog openDialog = new () { Title = "Open File" };
app.Run (openDialog);
if (!openDialog.Canceled)
{
    string path = openDialog.FilePaths.First ();
}
```

## Styling

### Border Styles
```csharp
view.BorderStyle = LineStyle.Rounded;    // Rounded corners
view.BorderStyle = LineStyle.Single;     // Single line
view.BorderStyle = LineStyle.Double;     // Double line
view.BorderStyle = LineStyle.None;       // No border
```

### Colors (via Themes)
```csharp
// Set theme at startup
ConfigurationManager.RuntimeConfig = """{ "Theme": "Dark" }""";
ConfigurationManager.Enable (ConfigLocations.All);
```

Available themes: `Default`, `Dark`, `Light`, `Amber Phosphor`, `Green Phosphor`, `Blue Phosphor`

## Checklist for Building Apps

- [ ] Project setup with correct packages
- [ ] Main window class inheriting from `Runnable` or `Runnable<T>`
- [ ] Application lifecycle: Create -> Init -> Run -> Dispose
- [ ] Layout using Pos/Dim (not hardcoded positions)
- [ ] Event handlers with `e.Handled = true` when appropriate
- [ ] Proper cleanup with `Dispose` pattern

## What NOT to Do

- Don't use `Application.Init()` / `Application.Shutdown()` (legacy static API)
- Don't hardcode sizes - use `Dim.Fill()`, `Dim.Auto()`, `Dim.Percent()`
- Don't forget `e.Handled = true` in Accepting handlers
- Don't block the main thread - use `Application.AddTimeout` for async work
