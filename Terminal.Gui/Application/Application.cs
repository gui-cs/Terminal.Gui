#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Terminal.Gui;

/// <summary>A static, singleton class representing the application. This class is the entry point for the application.</summary>
/// <example>
///     <code>
///     Application.Init();
///     var win = new Window ($"Example App ({Application.QuitKey} to quit)");
///     Application.Run(win);
///     win.Dispose();
///     Application.Shutdown();
///     </code>
/// </example>
/// <remarks>TODO: Flush this out.</remarks>
public static partial class Application
{
    /// <summary>Gets all cultures supported by the application without the invariant language.</summary>
    public static List<CultureInfo> SupportedCultures { get; private set; }

    internal static List<CultureInfo> GetSupportedCultures ()
    {
        CultureInfo [] culture = CultureInfo.GetCultures (CultureTypes.AllCultures);

        // Get the assembly
        var assembly = Assembly.GetExecutingAssembly ();

        //Find the location of the assembly
        string assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;

        // Find the resource file name of the assembly
        var resourceFilename = $"{Path.GetFileNameWithoutExtension (AppContext.BaseDirectory)}.resources.dll";

        // Return all culture for which satellite folder found with culture code.
        return culture.Where (
                              cultureInfo =>
                                  Directory.Exists (Path.Combine (assemblyLocation, cultureInfo.Name))
                                  && File.Exists (Path.Combine (assemblyLocation, cultureInfo.Name, resourceFilename))
                             )
                      .ToList ();
    }

    // IMPORTANT: Ensure all property/fields are reset here. See Init_ResetState_Resets_Properties unit test.
    // Encapsulate all setting of initial state for Application; Having
    // this in a function like this ensures we don't make mistakes in
    // guaranteeing that the state of this singleton is deterministic when Init
    // starts running and after Shutdown returns.
    internal static void ResetState (bool ignoreDisposed = false)
    {
        // Shutdown is the bookend for Init. As such it needs to clean up all resources
        // Init created. Apps that do any threading will need to code defensively for this.
        // e.g. see Issue #537
        foreach (Toplevel? t in _topLevels)
        {
            t!.Running = false;
        }

        _topLevels.Clear ();
        Current = null;
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
        _mainThreadId = -1;
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
        _initialized = false;

        // Mouse
        _mouseEnteredView = null;
        WantContinuousButtonPressedView = null;
        MouseEvent = null;
        GrabbedMouse = null;
        UnGrabbingMouse = null;
        GrabbedMouse = null;
        UnGrabbedMouse = null;

        // Keyboard
        AlternateBackwardKey = Key.Empty;
        AlternateForwardKey = Key.Empty;
        QuitKey = Key.Empty;
        KeyDown = null;
        KeyUp = null;
        SizeChanging = null;
        ClearKeyBindings ();

        Colors.Reset ();

        // Reset synchronization context to allow the user to run async/await,
        // as the main loop has been ended, the synchronization context from
        // gui.cs does no longer process any callbacks. See #1084 for more details:
        // (https://github.com/gui-cs/Terminal.Gui/issues/1084).
        SynchronizationContext.SetSynchronizationContext (null);
    }

    // When `End ()` is called, it is possible `RunState.Toplevel` is a different object than `Top`.
    // This field is set in `End` in this case so that `Begin` correctly sets `Top`.

    // TODO: Determine if this is really needed. The only code that calls WakeUp I can find
    // is ProgressBarStyles, and it's not clear it needs to.

    #region Toplevel handling

    /// <summary>Holds the stack of TopLevel views.</summary>

    // BUGBUG: Technically, this is not the full lst of TopLevels. There be dragons here, e.g. see how Toplevel.Id is used. What
    // about TopLevels that are just a SubView of another View?
    internal static readonly Stack<Toplevel> _topLevels = new ();

    /// <summary>The <see cref="Toplevel"/> object used for the application on startup (<seealso cref="Application.Top"/>)</summary>
    /// <value>The top.</value>
    public static Toplevel? Top { get; private set; }

    /// <summary>
    ///     The current <see cref="Toplevel"/> object. This is updated in <see cref="Application.Begin"/> enters and leaves to
    ///     point to the current
    ///     <see cref="Toplevel"/> .
    /// </summary>
    /// <remarks>
    ///     Only relevant in scenarios where <see cref="Toplevel.IsOverlappedContainer"/> is <see langword="true"/>.
    /// </remarks>
    /// <value>The current.</value>
    public static Toplevel? Current { get; private set; }

    private static void EnsureModalOrVisibleAlwaysOnTop (Toplevel topLevel)
    {
        if (!topLevel.Running
            || (topLevel == Current && topLevel.Visible)
            || OverlappedTop == null
            || _topLevels.Peek ().Modal)
        {
            return;
        }

        foreach (Toplevel? top in _topLevels.Reverse ())
        {
            if (top.Modal && top != Current)
            {
                MoveCurrent (top);

                return;
            }
        }

        if (!topLevel.Visible && topLevel == Current)
        {
            OverlappedMoveNext ();
        }
    }

#nullable enable
    private static Toplevel? FindDeepestTop (Toplevel start, in Point location)
    {
        if (!start.Frame.Contains (location))
        {
            return null;
        }

        if (_topLevels is { Count: > 0 })
        {
            int rx = location.X - start.Frame.X;
            int ry = location.Y - start.Frame.Y;

            foreach (Toplevel? t in _topLevels)
            {
                if (t != Current)
                {
                    if (t != start && t.Visible && t.Frame.Contains (rx, ry))
                    {
                        start = t;

                        break;
                    }
                }
            }
        }

        return start;
    }
#nullable restore

    private static View FindTopFromView (View view)
    {
        View top = view?.SuperView is { } && view?.SuperView != Top
                       ? view.SuperView
                       : view;

        while (top?.SuperView is { } && top?.SuperView != Top)
        {
            top = top.SuperView;
        }

        return top;
    }

#nullable enable

    // Only return true if the Current has changed.
    private static bool MoveCurrent (Toplevel top)
    {
        // The Current is modal and the top is not modal Toplevel then
        // the Current must be moved above the first not modal Toplevel.
        if (OverlappedTop is { }
            && top != OverlappedTop
            && top != Current
            && Current?.Modal == true
            && !_topLevels.Peek ().Modal)
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
            }

            var index = 0;
            Toplevel? [] savedToplevels = _topLevels.ToArray ();

            foreach (Toplevel? t in savedToplevels)
            {
                if (!t.Modal && t != Current && t != top && t != savedToplevels [index])
                {
                    lock (_topLevels)
                    {
                        _topLevels.MoveTo (top, index, new ToplevelEqualityComparer ());
                    }
                }

                index++;
            }

            return false;
        }

        // The Current and the top are both not running Toplevel then
        // the top must be moved above the first not running Toplevel.
        if (OverlappedTop is { }
            && top != OverlappedTop
            && top != Current
            && Current?.Running == false
            && top?.Running == false)
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (Current, 0, new ToplevelEqualityComparer ());
            }

            var index = 0;

            foreach (Toplevel? t in _topLevels.ToArray ())
            {
                if (!t.Running && t != Current && index > 0)
                {
                    lock (_topLevels)
                    {
                        _topLevels.MoveTo (top, index - 1, new ToplevelEqualityComparer ());
                    }
                }

                index++;
            }

            return false;
        }

        if ((OverlappedTop is { } && top?.Modal == true && _topLevels.Peek () != top)
            || (OverlappedTop is { } && Current != OverlappedTop && Current?.Modal == false && top == OverlappedTop)
            || (OverlappedTop is { } && Current?.Modal == false && top != Current)
            || (OverlappedTop is { } && Current?.Modal == true && top == OverlappedTop))
        {
            lock (_topLevels)
            {
                _topLevels.MoveTo (top, 0, new ToplevelEqualityComparer ());
                Current = top;
            }
        }

        return true;
    }
#nullable restore

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    /// <remarks>
    ///     Event handlers can set <see cref="SizeChangedEventArgs.Cancel"/> to <see langword="true"/> to prevent
    ///     <see cref="Application"/> from changing it's size to match the new terminal size.
    /// </remarks>
    public static event EventHandler<SizeChangedEventArgs> SizeChanging;

    /// <summary>
    ///     Called when the application's size changes. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="args">The new size.</param>
    /// <returns><see lanword="true"/>if the size was changed.</returns>
    public static bool OnSizeChanging (SizeChangedEventArgs args)
    {
        SizeChanging?.Invoke (null, args);

        if (args.Cancel || args.Size is null)
        {
            return false;
        }

        foreach (Toplevel t in _topLevels)
        {
            t.SetRelativeLayout (args.Size.Value);
            t.LayoutSubviews ();
            t.PositionToplevels ();
            t.OnSizeChanging (new (args.Size));

            if (PositionCursor (t))
            {
                Driver.UpdateCursor ();
            }
        }

        Refresh ();

        return true;
    }

    #endregion Toplevel handling

    /// <summary>
    ///     Gets a string representation of the Application as rendered by <see cref="Driver"/>.
    /// </summary>
    /// <returns>A string representation of the Application </returns>
    public new static string ToString ()
    {
        ConsoleDriver driver = Driver;

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
    public static string ToString (ConsoleDriver driver)
    {
        var sb = new StringBuilder ();

        Cell [,] contents = driver.Contents;

        for (var r = 0; r < driver.Rows; r++)
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
}
