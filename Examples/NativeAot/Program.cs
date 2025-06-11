// This is a test application for a native Aot file.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Terminal.Gui.Configuration;
using Terminal.Gui.App;

namespace NativeAot;

public static class Program
{
    [RequiresUnreferencedCode ("Calls Terminal.Gui.Application.Init(IConsoleDriver, String)")]
    [RequiresDynamicCode ("Calls Terminal.Gui.Application.Init(IConsoleDriver, String)")]
    private static void Main (string [] args)
    {
        ConfigurationManager.Enable(ConfigLocations.All);
        Application.Init (null, args.Length > 0 ? args [0] : null);

        #region The code in this region is not intended for use in a native Aot self-contained. It's just here to make sure there is no functionality break with localization in Terminal.Gui using self-contained

        if (Equals(Thread.CurrentThread.CurrentUICulture, CultureInfo.InvariantCulture) && Application.SupportedCultures!.Count == 0)
        {
            // Only happens if the project has <InvariantGlobalization>true</InvariantGlobalization>
            Debug.Assert (Application.SupportedCultures.Count == 0);
        }
        else
        {
            Debug.Assert (Application.SupportedCultures!.Count > 0);
            Debug.Assert (Equals (CultureInfo.CurrentCulture, Thread.CurrentThread.CurrentUICulture));
        }

        #endregion

        ExampleWindow app = new ();
        Application.Run (app);

        // Dispose the app object before shutdown
        app.Dispose ();

        // Before the application exits, reset Terminal.Gui for clean shutdown
        Application.Shutdown ();

        // To see this output on the screen it must be done after shutdown,
        // which restores the previous screen.
        Console.WriteLine ($@"Username: {ExampleWindow.UserName}");
    }
}
