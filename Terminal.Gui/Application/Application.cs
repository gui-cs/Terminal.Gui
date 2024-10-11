#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

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
    /// <summary>Gets all cultures supported by the application without the invariant language.</summary>
    public static List<CultureInfo>? SupportedCultures { get; private set; }

    /// <summary>
    ///     Gets a string representation of the Application as rendered by <see cref="Driver"/>.
    /// </summary>
    /// <returns>A string representation of the Application </returns>
    public new static string ToString ()
    {
        ConsoleDriver? driver = Driver;

        if (driver is null)
        {
            return string.Empty;
        }

        return ToString (driver);
    }

    /// <summary>
    ///     Gets a string representation of the Application rendered by the provided <see cref="ConsoleDriver"/>.
    /// </summary>
    /// <param name="driver">The driver to use to render the contents.</param>
    /// <returns>A string representation of the Application </returns>
    public static string ToString (ConsoleDriver? driver)
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

                if (rune.DecodeSurrogatePair (out char [] sp))
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

    // IMPORTANT: Ensure all property/fields are reset here. See Init_ResetState_Resets_Properties unit test.
    // Encapsulate all setting of initial state for Application; Having
    // this in a function like this ensures we don't make mistakes in
    // guaranteeing that the state of this singleton is deterministic when Init
    // starts running and after Shutdown returns.
    internal static void ResetState (bool ignoreDisposed = false)
    {
        Application.Navigation = new ApplicationNavigation ();

        // Shutdown is the bookend for Init. As such it needs to clean up all resources
        // Init created. Apps that do any threading will need to code defensively for this.
        // e.g. see Issue #537
        foreach (Toplevel? t in TopLevels)
        {
            t!.Running = false;
        }

        TopLevels.Clear ();
#if DEBUG_IDISPOSABLE

        // Don't dispose the Top. It's up to caller dispose it
        if (!ignoreDisposed && Top is { })
        {
            Debug.Assert (Top.WasDisposed);

            // If End wasn't called _cachedRunStateToplevel may be null
            if (_cachedRunStateToplevel is { })
            {
                Debug.Assert (_cachedRunStateToplevel.WasDisposed);
                Debug.Assert (_cachedRunStateToplevel == Top);
            }
        }
#endif
        Top = null;
        _cachedRunStateToplevel = null;

        // MainLoop stuff
        MainLoop?.Dispose ();
        MainLoop = null;
        MainThreadId = -1;
        Iteration = null;
        EndAfterFirstIteration = false;

        // Driver stuff
        if (Driver is { })
        {
            Driver.SizeChanged -= Driver_SizeChanged;
            Driver.KeyDown -= Driver_KeyDown;
            Driver.KeyUp -= Driver_KeyUp;
            Driver.MouseEvent -= Driver_MouseEvent;
            Driver?.End ();
            Driver = null;
        }

        // Don't reset ForceDriver; it needs to be set before Init is called.
        //ForceDriver = string.Empty;
        //Force16Colors = false;
        _forceFakeConsole = false;

        // Run State stuff
        NotifyNewRunState = null;
        NotifyStopRunState = null;
        MouseGrabView = null;
        IsInitialized = false;

        // Mouse
        _lastMousePosition = null;
        _cachedViewsUnderMouse.Clear ();
        WantContinuousButtonPressedView = null;
        MouseEvent = null;
        GrabbedMouse = null;
        UnGrabbingMouse = null;
        GrabbedMouse = null;
        UnGrabbedMouse = null;

        // Keyboard
        KeyDown = null;
        KeyUp = null;
        SizeChanging = null;

        Navigation = null;

        AddApplicationKeyBindings ();

        // Reset synchronization context to allow the user to run async/await,
        // as the main loop has been ended, the synchronization context from
        // gui.cs does no longer process any callbacks. See #1084 for more details:
        // (https://github.com/gui-cs/Terminal.Gui/issues/1084).
        SynchronizationContext.SetSynchronizationContext (null);
    }

    // Only return true if the Current has changed.
}
