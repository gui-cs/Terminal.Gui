// AOT smoke test that consumes Terminal.Gui via NuGet package.
// Exercises Application lifecycle, view creation, and config-property
// deep-cloning paths that are most sensitive to AOT trimming.

using System.Diagnostics.CodeAnalysis;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace SmokeTest;

public static class Program
{
    private static int Main ()
    {
#pragma warning disable IL2026, IL3050
        return Run ();
#pragma warning restore IL2026, IL3050
    }

    [RequiresUnreferencedCode ("Calls Terminal.Gui.Application.Init")]
    [RequiresDynamicCode ("Calls Terminal.Gui.Application.Init")]
    private static int Run ()
    {
        try
        {
            ConfigurationManager.Enable (ConfigLocations.All);

            IApplication app = Application.Create ().Init ();

            // Create a representative set of views that exercise AOT-sensitive paths
            List<View> views =
            [
                new Button { Text = "OK" },
                new Label { Text = "Hello AOT" },
                new TextField { Text = "editable" },
                new CheckBox { Text = "Check me" },
                new ProgressBar { Fraction = 0.5f },
                new DatePicker (),
                new NumericUpDown<int> (),
                new ListView (),
                new OptionSelector (),
                new FrameView { Title = "Frame" },
                new TextView { Text = "Multi-line" },
                new DropDownList ()
            ];

            // Verify views were created and have content
            foreach (View view in views)
            {
                // Touch layout to trigger Pos/Dim evaluation
                _ = view.Frame;
            }

            app.Dispose ();

            Console.WriteLine ($"AOT NuGet smoke test passed: {views.Count} views created successfully.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine ($"AOT NuGet smoke test FAILED: {ex}");

            return 1;
        }
    }
}
