// InlineSelect — demonstrates using RunnableWrapper<OptionSelector, int?> in inline mode.
//
// Renders an OptionSelector inline in the terminal with options from the command line.
// Supports horizontal or vertical orientation via --horizontal / --vertical flags.
// Hot keys are auto-assigned from option text.
// Supports --timeout <seconds> to auto-cancel via CancellationToken (demonstrates RunAsync).
//
// Usage:
//   dotnet run --project Examples/InlineSelect -- Apple Banana Cherry
//   dotnet run --project Examples/InlineSelect -- --horizontal Red Green Blue Yellow
//   dotnet run --project Examples/InlineSelect -- --vertical One Two Three
//   dotnet run --project Examples/InlineSelect -- --timeout 10 Apple Banana Cherry

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

// Parse command-line arguments
Orientation orientation = Orientation.Vertical;
List<string> options = [];
int? timeoutSeconds = null;

for (int i = 0; i < args.Length; i++)
{
    string arg = args [i];

    if (arg is "--horizontal" or "-h")
    {
        orientation = Orientation.Horizontal;
    }
    else if (arg is "--vertical" or "-v")
    {
        orientation = Orientation.Vertical;
    }
    else if (arg is "--timeout" or "-t")
    {
        if (i + 1 < args.Length && int.TryParse (args [i + 1], out int seconds))
        {
            timeoutSeconds = seconds;
            i++; // skip the next arg (the number)
        }
        else
        {
            Console.Error.WriteLine ("Error: --timeout requires a number of seconds.");

            return 1;
        }
    }
    else
    {
        options.Add (arg);
    }
}

if (options.Count == 0)
{
    Console.Error.WriteLine ("Usage: InlineSelect [--horizontal|--vertical] [--timeout <seconds>] <option1> <option2> ...");

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
    Title = timeoutSeconds.HasValue
                ? $"Select an option (Enter to accept, Esc to cancel, {timeoutSeconds}s timeout)"
                : "Select an option (Enter to accept, Esc to cancel)",
    Width = Dim.Fill (),
    BorderStyle = LineStyle.Rounded
};

// Run with optional timeout via RunAsync + CancellationToken
if (timeoutSeconds.HasValue)
{
    // Use RunAsync with a CancellationToken for timeout-based cancellation
    using CancellationTokenSource cts = new (TimeSpan.FromSeconds (timeoutSeconds.Value));
    await app.RunAsync (wrapper, cts.Token);
}
else
{
    // Run synchronously — blocks until user accepts or cancels
    app.Run (wrapper);
}

int? result = wrapper.Result;

app.Dispose ();

if (result is { } selectedIndex && selectedIndex >= 0 && selectedIndex < options.Count)
{
    Console.WriteLine (options [selectedIndex]);

    return 0;
}

return 1;
