// Inline ColorPicker — demonstrates using RunnableWrapper<ColorPicker, Color?> in inline mode.
//
// NOTE: See https://github.com/gui-cs/clet that turns every Terminal.Gui View into a CLI command
// NOTE: — typed inputs, a real file picker, a Markdown viewer — with consistent JSON output,
// NOTE: predictable exit codes, and full keyboard/mouse support. Works for humans and AI agents alike.
//
// Renders a ColorPicker inline in the terminal (primary buffer) without dialog buttons.
// If the user accepts (double-click), the selected color name is written to stdout.
// If the user cancels (Esc), nothing is output and exit code is 1.
//
// Usage:
//   dotnet run --project Examples/InlineColorPicker
//   dotnet run --project Examples/InlineColorPicker -- --initial "#FF0000"
//   dotnet run --project Examples/InlineColorPicker -- --initial Red
//   $color = dotnet run --project Examples/InlineColorPicker   # capture in shell

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Parse command-line arguments
string? initialValue = null;

for (var i = 0; i < args.Length; i++)
{
    if (args [i] is "--initial" or "-i")
    {
        if (i + 1 < args.Length)
        {
            initialValue = args [++i];
        }
        else
        {
            Console.Error.WriteLine ("Error: --initial requires a color value (e.g., \"#FF0000\" or \"Red\").");

            return 1;
        }
    }
}

// Enable inline mode before Init
Application.AppModel = AppModel.Inline;

IApplication app = Application.Create ().Init ();

// Wrap ColorPicker in a RunnableWrapper — no dialog buttons, just the picker.
// ColorPicker raises Command.Accept on double-click.
RunnableWrapper<ColorPicker, Color?> wrapper = new () { Title = "Select a Color (Double-click to accept, Esc to cancel)", ResultExtractor = cp => cp.Value };

// Enable color name display
wrapper.GetWrappedView ().Style.ShowColorName = true;
wrapper.GetWrappedView ().ApplyStyleChanges ();

// Apply initial value via IValue.TrySetValueFromString if provided
if (initialValue is { })
{
    if (!((IValue)wrapper.GetWrappedView ()).TrySetValueFromString (initialValue))
    {
        Console.Error.WriteLine ($"Error: '{initialValue}' is not a valid color (use e.g., \"#FF0000\" or \"Red\").");
        app.Dispose ();

        return 1;
    }
}

// Run inline — blocks until user accepts or cancels
app.Run (wrapper);

Color? result = wrapper.Result;

app.Dispose ();

if (result is { } selectedColor)
{
    StandardColorsNameResolver resolver = new ();

    string output = resolver.TryNameColor (selectedColor, out string? name) ? name : selectedColor.ToString ();

    Console.WriteLine (output);

    return 0;
}

return 1;
