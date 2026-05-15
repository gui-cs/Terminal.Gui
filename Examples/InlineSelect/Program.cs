// InlineSelect — demonstrates using RunnableWrapper<OptionSelector, int?> in inline mode.
//
// NOTE: See https://github.com/gui-cs/clet that turns every Terminal.Gui View into a CLI command
// NOTE: — typed inputs, a real file picker, a Markdown viewer — with consistent JSON output,
// NOTE: predictable exit codes, and full keyboard/mouse support. Works for humans and AI agents alike.
//
// Renders an OptionSelector inline in the terminal with options from the command line.
// Supports horizontal or vertical orientation via --horizontal / --vertical flags.
// Hot keys are auto-assigned from option text.
// Supports --timeout <seconds> to auto-cancel via CancellationToken (demonstrates RunAsync).
// Supports --initial <index> to pre-select an option via IValue.TrySetValueFromString.
//
// Usage:
//   dotnet run --project Examples/InlineSelect -- Apple Banana Cherry
//   dotnet run --project Examples/InlineSelect -- --horizontal Red Green Blue Yellow
//   dotnet run --project Examples/InlineSelect -- --vertical One Two Three
//   dotnet run --project Examples/InlineSelect -- --timeout 10 Apple Banana Cherry
//   dotnet run --project Examples/InlineSelect -- --initial 1 Apple Banana Cherry

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Timeout = System.Threading.Timeout;

// Parse command-line arguments
Orientation orientation = Orientation.Vertical;
List<string> options = [];
int? timeoutSeconds = null;
string? initialValue = null;

for (var i = 0; i < args.Length; i++)
{
    string arg = args [i];

    switch (arg)
    {
        case "--horizontal" or "-h": orientation = Orientation.Horizontal; break;

        case "--vertical" or "-v": orientation = Orientation.Vertical; break;

        case "--timeout" or "-t" when i + 1 < args.Length && int.TryParse (args [i + 1], out int seconds):
            timeoutSeconds = seconds;
            i++; // skip the next arg (the number)

            break;

        case "--timeout" or "-t":
            Console.Error.WriteLine ("Error: --timeout requires a number of seconds.");

            return 1;

        case "--initial" or "-i" when i + 1 < args.Length: initialValue = args [++i]; break;

        case "--initial" or "-i":
            Console.Error.WriteLine ("Error: --initial requires an index value.");

            return 1;

        default: options.Add (arg); break;
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
OptionSelector selector = new () { Labels = options, Orientation = orientation, AssignHotKeys = true };

// Wrap in RunnableWrapper — auto-extracts Value via IValue<int?>
RunnableWrapper<OptionSelector, int?> wrapper = new (selector)
{
    Title = timeoutSeconds.HasValue
                ? $"Select an option (Enter to accept, Esc to cancel, {timeoutSeconds}s timeout)"
                : "Select an option (Enter to accept, Esc to cancel)",
    Width = Dim.Fill (),
    BorderStyle = LineStyle.Rounded
};

// Apply initial value if provided — match by label (case-insensitive) or by numeric index
if (initialValue is { })
{
    // First try matching a label
    int matchIndex = options.FindIndex (o => string.Equals (o, initialValue, StringComparison.OrdinalIgnoreCase));

    if (matchIndex >= 0)
    {
        selector.Value = matchIndex;
    }
    else if (!((IValue)selector).TrySetValueFromString (initialValue))
    {
        Console.Error.WriteLine ($"Error: '{initialValue}' does not match any option and is not a valid index.");
        app.Dispose ();

        return 1;
    }
}

// Run with optional timeout via RunAsync + CancellationToken
if (timeoutSeconds.HasValue)
{
    // Use RunAsync with a CancellationToken for timeout-based cancellation
    using CancellationTokenSource cts = new (TimeSpan.FromSeconds (timeoutSeconds.Value));

    // Show terminal progress indicator counting down the timeout (OSC 9;4)
    DateTime startTime = DateTime.UtcNow;
    int totalMs = timeoutSeconds.Value * 1000;

    await using Timer progressTimer = new (_ => app.Invoke (_ =>
                                                            {
                                                                var elapsedMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                                                                int percent = Math.Min (elapsedMs * 100 / totalMs, 100);
                                                                app.Driver?.ProgressIndicator?.SetValue (percent);
                                                            }),
                                           null,
                                           0,
                                           250);

    await app.RunAsync (wrapper, cts.Token);

    // Clear the progress indicator when done
    progressTimer.Change (Timeout.Infinite, Timeout.Infinite);
    app.Driver?.ProgressIndicator?.Clear ();
}
else
{
    // Run synchronously — blocks until user accepts or cancels
    app.Run (wrapper);
}

int? result = wrapper.Result;

app.Dispose ();

if (result is { } selectedIndex and >= 0 && selectedIndex < options.Count)
{
    Console.WriteLine (options [selectedIndex]);

    return 0;
}

return 1;
