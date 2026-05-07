// AOT smoke test that consumes Terminal.Gui via NuGet package.
// Exercises the full Application lifecycle — Init, RunAsync, layout, draw,
// event loop, and Dispose — using the published NuGet package to catch
// AOT trimming issues that only manifest with PackageReference builds.

using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace SmokeTest;

public static class Program
{
    private static async Task<int> Main ()
    {
#pragma warning disable IL2026, IL3050
        return await RunAsync ();
#pragma warning restore IL2026, IL3050
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.Application.Init")]
    [RequiresDynamicCode ("Calls Terminal.Gui.Application.Init")]
    private static async Task<int> RunAsync ()
    {
        try
        {
            ConfigurationManager.Enable (ConfigLocations.All);

            IApplication app = Application.Create ().Init ();

            // Run the full app lifecycle with a timeout — exercises layout, draw, and event loop
            using CancellationTokenSource cts = new (TimeSpan.FromSeconds (5));
            await app.RunAsync<SmokeTestWindow> (cts.Token);
            app.Dispose ();

            Console.WriteLine ("AOT NuGet smoke test passed: full app lifecycle completed successfully.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine ($"AOT NuGet smoke test FAILED: {ex}");

            return 1;
        }
    }
}

/// <summary>
///     Minimal window that creates representative views exercising AOT-sensitive paths.
/// </summary>
public sealed class SmokeTestWindow : Runnable
{
    public SmokeTestWindow ()
    {
        Title = "AOT NuGet Smoke Test";

        Add (
              new Button { Text = "OK" },
              new Label { Text = "Hello AOT", Y = 1 },
              new TextField { Text = "editable", Y = 2, Width = 20 },
              new CheckBox { Text = "Check me", Y = 3 },
              new ProgressBar { Fraction = 0.5f, Y = 4, Width = 20 },
              new DatePicker { Y = 5 },
              new NumericUpDown<int> { Y = 6 },
              new ListView { Y = 7, Width = 20, Height = 3 },
              new OptionSelector { Y = 10 },
              new FrameView { Title = "Frame", Y = 11, Width = 20, Height = 3 },
              new TextView { Text = "Multi-line", Y = 14, Width = 20, Height = 3 },
              new DropDownList { Y = 17, Width = 20 }
             );
    }
}
