// InlineSelect — demonstrates using RunnableWrapper<OptionSelector, int?> in inline mode.
//
// Renders an OptionSelector inline in the terminal with options from the command line.
// Supports horizontal or vertical orientation via --horizontal / --vertical flags.
// Hot keys are auto-assigned from option text.
//
// Usage:
//   dotnet run --project Examples/InlineSelect -- Apple Banana Cherry
//   dotnet run --project Examples/InlineSelect -- --horizontal Red Green Blue Yellow
//   dotnet run --project Examples/InlineSelect -- --vertical One Two Three

using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Parse command-line arguments
Orientation orientation = Orientation.Vertical;
List<string> options = [];

foreach (string arg in args)
{
    if (arg is "--horizontal" or "-h")
    {
        orientation = Orientation.Horizontal;
    }
    else if (arg is "--vertical" or "-v")
    {
        orientation = Orientation.Vertical;
    }
    else
    {
        options.Add (arg);
    }
}

if (options.Count == 0)
{
    Console.Error.WriteLine ("Usage: InlineSelect [--horizontal|--vertical] <option1> <option2> ...");

    return 1;
}

// Enable inline mode before Init
Application.AppModel = AppModel.Inline;

IApplication app = Application.Create ().Init ();

// Build the OptionSelector with command-line options
OptionSelector selector = new ()
{
    Labels = options,
    Orientation = orientation,
    AssignHotKeys = true
};

// Wrap in RunnableWrapper — auto-extracts Value via IValue<int?>
RunnableWrapper<OptionSelector, int?> wrapper = new (selector)
{
    Title = "Select an option (Enter to accept, Esc to cancel)",
    Width = Dim.Fill ()
};

// Run inline — blocks until user accepts or cancels
app.Run (wrapper);

int? result = wrapper.Result;

app.Dispose ();

if (result is { } selectedIndex && selectedIndex >= 0 && selectedIndex < options.Count)
{
    Console.WriteLine (options [selectedIndex]);

    return 0;
}

return 1;
