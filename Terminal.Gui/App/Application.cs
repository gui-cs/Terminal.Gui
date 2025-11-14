#nullable enable

// We use global using directives to simplify the code and avoid repetitive namespace declarations.
// Put them here so they are available throughout the application.
// Do not put them in AssemblyInfo.cs as it will break GitVersion's /updateassemblyinfo
global using Attribute = Terminal.Gui.Drawing.Attribute;
global using Color = Terminal.Gui.Drawing.Color;
global using CM = Terminal.Gui.Configuration.ConfigurationManager;
global using Terminal.Gui.App;
global using Terminal.Gui.Drivers;
global using Terminal.Gui.Input;
global using Terminal.Gui.Configuration;
global using Terminal.Gui.ViewBase;
global using Terminal.Gui.Views;
global using Terminal.Gui.Drawing;
global using Terminal.Gui.Text;
global using Terminal.Gui.Resources;
global using Terminal.Gui.FileServices;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Terminal.Gui.App;

/// <summary>A static, singleton class representing the application. This class is the entry point for the application.</summary>
/// <example>
///     <code>
///     Application.Init();
///     var win = new Window()
///     {
///         Title = $"Example App ({Application.QuitKey} to quit)"
///     };
///     Application.Run(win);
///     win.Dispose();
///     Application.Shutdown();
///     </code>
/// </example>
/// <remarks></remarks>
public static partial class Application
{
    /// <summary>
    /// Maximum number of iterations of the main loop (and hence draws)
    /// to allow to occur per second. Defaults to <see cref="DefaultMaximumIterationsPerSecond"/>> which is a 40ms sleep
    /// after iteration (factoring in how long iteration took to run).
    /// <remarks>Note that not every iteration draws (see <see cref="View.NeedsDraw"/>).
    /// Only affects v2 drivers.</remarks>
    /// </summary>
    public static ushort MaximumIterationsPerSecond = DefaultMaximumIterationsPerSecond;

    /// <summary>
    /// Default value for <see cref="MaximumIterationsPerSecond"/>
    /// </summary>
    public const ushort DefaultMaximumIterationsPerSecond = 25;

    /// <summary>
    ///     Gets a string representation of the Application as rendered by <see cref="Driver"/>.
    /// </summary>
    /// <returns>A string representation of the Application </returns>
    public new static string ToString ()
    {
        IDriver? driver = Driver;

        if (driver is null)
        {
            return string.Empty;
        }

        return ToString (driver);
    }

    /// <summary>
    ///     Gets a string representation of the Application rendered by the provided <see cref="IDriver"/>.
    /// </summary>
    /// <param name="driver">The driver to use to render the contents.</param>
    /// <returns>A string representation of the Application </returns>
    public static string ToString (IDriver? driver)
    {
        if (driver is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder ();

        Cell [,] contents = driver?.Contents!;

        for (var r = 0; r < driver!.Rows; r++)
        {
            for (var c = 0; c < driver.Cols; c++)
            {
                Rune rune = contents [r, c].Rune;

                if (rune.DecodeSurrogatePair (out char []? sp))
                {
                    sb.Append (sp);
                }
                else
                {
                    sb.Append ((char)rune.Value);
                }

                if (rune.GetColumns () > 1)
                {
                    c++;
                }

                // See Issue #2616
                //foreach (var combMark in contents [r, c].CombiningMarks) {
                //	sb.Append ((char)combMark.Value);
                //}
            }

            sb.AppendLine ();
        }

        return sb.ToString ();
    }

    /// <summary>Gets all cultures supported by the application without the invariant language.</summary>
    public static List<CultureInfo>? SupportedCultures { get; private set; } = GetSupportedCultures ();


    internal static List<CultureInfo> GetAvailableCulturesFromEmbeddedResources ()
    {
        ResourceManager rm = new (typeof (Strings));

        CultureInfo [] cultures = CultureInfo.GetCultures (CultureTypes.AllCultures);

        return cultures.Where (
                               cultureInfo =>
                                   !cultureInfo.Equals (CultureInfo.InvariantCulture)
                                   && rm.GetResourceSet (cultureInfo, true, false) is { }
                              )
                       .ToList ();
    }

    // BUGBUG: This does not return en-US even though it's supported by default
    internal static List<CultureInfo> GetSupportedCultures ()
    {
        CultureInfo [] cultures = CultureInfo.GetCultures (CultureTypes.AllCultures);

        // Get the assembly
        var assembly = Assembly.GetExecutingAssembly ();

        //Find the location of the assembly
        string assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;

        // Find the resource file name of the assembly
        var resourceFilename = $"{assembly.GetName ().Name}.resources.dll";

        if (cultures.Length > 1 && Directory.Exists (Path.Combine (assemblyLocation, "pt-PT")))
        {
            // Return all culture for which satellite folder found with culture code.
            return cultures.Where (
                                   cultureInfo =>
                                       Directory.Exists (Path.Combine (assemblyLocation, cultureInfo.Name))
                                       && File.Exists (Path.Combine (assemblyLocation, cultureInfo.Name, resourceFilename))
                                  )
                           .ToList ();
        }

        // It's called from a self-contained single-file and get available cultures from the embedded resources strings.
        return GetAvailableCulturesFromEmbeddedResources ();
    }
}
