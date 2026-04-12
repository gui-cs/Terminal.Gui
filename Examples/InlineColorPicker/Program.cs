// Inline ColorPicker — demonstrates using RunnableWrapper<ColorPicker, Color?> in inline mode.
//
// Renders a ColorPicker inline in the terminal (primary buffer) without dialog buttons.
// If the user accepts (double-click), the selected color name is written to stdout.
// If the user cancels (Esc), nothing is output and exit code is 1.
//
// Usage:
//   dotnet run --project Examples/InlineColorPicker
//   $color = dotnet run --project Examples/InlineColorPicker   # capture in shell

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Views;

// Enable inline mode before Init
Application.AppModel = AppModel.Inline;

IApplication app = Application.Create ().Init ();

// Wrap ColorPicker in a RunnableWrapper — no dialog buttons, just the picker.
// ColorPicker raises Command.Accept on double-click.
RunnableWrapper<ColorPicker, Color?> wrapper = new ()
{
    Title = "Select a Color (Double-click to accept, Esc to cancel)",
    ResultExtractor = cp => cp.Value
};

// Enable color name display
wrapper.GetWrappedView ().Style.ShowColorName = true;

// Run inline — blocks until user accepts or cancels
app.Run (wrapper);

Color? result = wrapper.Result;

app.Dispose ();

if (result is { } selectedColor)
{
    StandardColorsNameResolver resolver = new ();

    string output = resolver.TryNameColor (selectedColor, out string? name)
                        ? name
                        : selectedColor.ToString ();

    Console.WriteLine (output);

    return 0;
}

return 1;
