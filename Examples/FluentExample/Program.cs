#nullable enable
// Fluent API example demonstrating IRunnable with automatic disposal and result extraction

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Examples;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

[assembly: ExampleMetadata ("Fluent API Example", "Demonstrates the fluent IApplication API with IRunnable pattern")]
[assembly: ExampleCategory ("API Patterns")]
[assembly: ExampleCategory ("Controls")]
[assembly: ExampleDemoKeyStrokes (KeyStrokes = ["CursorDown", "CursorDown", "CursorRight", "Enter"], Order = 1)]
[assembly: ExampleDemoKeyStrokes (KeyStrokes = ["Esc"], DelayMs = 100, Order = 2)]


// Check for test context to determine driver
string? contextJson = Environment.GetEnvironmentVariable (ExampleContext.ENVIRONMENT_VARIABLE_NAME);
string? driverName = null;
var isExample = false;

if (!string.IsNullOrEmpty (contextJson))
{
    ExampleContext? context = ExampleContext.FromJson (contextJson);
    driverName = context?.DriverName;
    isExample = true;
}

IApplication? app = Application.Create (example: isExample)
                               .Init (driverName)
                               .Run<ColorPickerView> ();

// Run the application with fluent API - automatically creates, runs, and disposes the runnable
Color? result = app.GetResult () as Color?;

// Shut down the app with Dispose before we can use Console.WriteLine
app.Dispose ();

if (result is { })
{
    Console.WriteLine (@$"Selected Color: {result}");
}
else
{
    Console.WriteLine (@"No color selected");
}

/// <summary>
///     A runnable view that allows the user to select a color.
///     Demonstrates the Runnable with type pattern with automatic disposal.
/// </summary>
public class ColorPickerView : Runnable<Color?>
{
    public ColorPickerView ()
    {
        Title = "Select a Color (Esc to quit)";
        BorderStyle = LineStyle.Single;
        Height = Dim.Auto ();
        Width = Dim.Auto ();

        // Add instructions
        var instructions = new Label
        {
            Text = "Use arrow keys to select a color, Enter to accept",
            X = Pos.Center (),
            Y = 0
        };

        // Create color picker
        ColorPicker colorPicker = new ()
        {
            X = Pos.Center (),
            Y = Pos.Bottom (instructions),
            Style = new ColorPickerStyle ()
            {
                ShowColorName = true,
                ShowTextFields = true
            }
        };
        colorPicker.ApplyStyleChanges ();

        // Create OK button
        Button okButton = new ()
        {
            Title = "_OK",
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            IsDefault = true
        };

        okButton.Accepting += (s, e) =>
                              {
                                  // Extract result before stopping
                                  Result = colorPicker.SelectedColor;
                                  RequestStop ();
                                  e.Handled = true;
                              };

        // Create Cancel button
        Button cancelButton = new ()
        {
            Title = "_Cancel",
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd ()
        };

        cancelButton.Accepting += (s, e) =>
                                  {
                                      // Don't set result - leave as null
                                      RequestStop ();
                                      e.Handled = true;
                                  };

        // Add views
        Add (instructions, colorPicker, okButton, cancelButton);
    }

    protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        // Alternative place to extract result before stopping
        // This is called before the view is removed from the stack
        if (!newIsRunning && Result is null)
        {
            // User pressed Esc - could extract current selection here
            //Result = SelectedColor;
        }

        return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    }
}
