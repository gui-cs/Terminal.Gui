// Fluent API example demonstrating IRunnable with automatic disposal and result extraction

using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Run the application with fluent API - automatically creates, runs, and disposes the runnable
Color? result = Application.Create ()
                           .Init ()
                           .Run<ColorPickerView> ()
                           .Shutdown () as Color?;

// Display the result
if (result is { })
{
    Console.WriteLine ($"Selected Color: {result}");
}
else
{
    Console.WriteLine ("No color selected");
}

/// <summary>
/// A runnable view that allows the user to select a color.
/// Demonstrates IRunnable<TResult> pattern with automatic disposal.
/// </summary>
public class ColorPickerView : Runnable<Color?>
{
    private readonly ColorPicker16 _colorPicker;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    public ColorPickerView ()
    {
        Title = "Select a Color (Esc to quit)";
        BorderStyle = LineStyle.Single;

        // Create color picker
        _colorPicker = new ColorPicker16
        {
            X = Pos.Center (),
            Y = 2,
            BoxHeight = 2,
            BoxWidth = 4
        };

        // Create OK button
        _okButton = new Button
        {
            Text = "OK",
            X = Pos.Center () - 8,
            Y = Pos.AnchorEnd (1),
            IsDefault = true
        };

        _okButton.Accepting += (s, e) =>
        {
            // Extract result before stopping
            Result = _colorPicker.SelectedColor;
            Application.RequestStop ();
            e.Handled = true;
        };

        // Create Cancel button
        _cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center () + 2,
            Y = Pos.AnchorEnd (1)
        };

        _cancelButton.Accepting += (s, e) =>
        {
            // Don't set result - leave as null
            Application.RequestStop ();
            e.Handled = true;
        };

        // Add views
        Add (_colorPicker, _okButton, _cancelButton);

        // Add instructions
        var label = new Label
        {
            Text = "Use arrow keys to select a color, Enter to accept",
            X = Pos.Center (),
            Y = 0
        };
        Add (label);
    }

    protected override bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        // Alternative place to extract result before stopping
        // This is called before the view is removed from the stack
        if (!newIsRunning && Result is null)
        {
            // User pressed Esc - could extract current selection here
            // Result = _colorPicker.SelectedColor;
        }

        return base.OnIsRunningChanging (oldIsRunning, newIsRunning);
    }
}
