#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Terminal.Gui.App;

/// <summary>
///     Interface for instances that provide backing functionality to static
///     gateway class <see cref="Application"/>.
/// </summary>
public interface IApplication
{
    /// <summary>Adds a timeout to the application.</summary>
    /// <remarks>
    ///     When time specified passes, the callback will be invoked. If the callback returns true, the timeout will be
    ///     reset, repeating the invocation. If it returns false, the timeout will stop and be removed. The returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="RemoveTimeout(object)"/>.
    /// </remarks>
    object AddTimeout (TimeSpan time, Func<bool> callback);

    /// <summary>
    ///     Gets or sets whether the screen will be cleared, and all Views redrawn, during the next Application iteration.
    /// </summary>
    bool ClearScreenNextIteration { get; set; }

    /// <summary>Gets or sets the console driver being used.</summary>
    IConsoleDriver? Driver { get; set; }

    /// <summary>
    ///     This is intended for unit tests and won't stop the <see cref="Application.RunLoop"/> if set to <see langword="true"/>
    ///     If the caller calls <see cref="Application.RunLoop"/> with the returned token from <see cref="Application.Begin"/>, setting
    ///     this property to <see langword="true"/> will cause only one iteration of the main loop to execute. The default is
    ///     <see langword="false"/>, which will cause the application to continue running until Application.RequestStop () is called.
    /// </summary>
    bool EndAfterFirstIteration { get; set; }

    /// <summary>
    ///     Gets or sets whether <see cref="Driver"/> will be forced to output only the 16 colors defined in
    ///     <see cref="ColorName16"/>. The default is <see langword="false"/>, meaning 24-bit (TrueColor) colors will be output
    ///     as long as the selected <see cref="IConsoleDriver"/> supports TrueColor.
    /// </summary>
    bool Force16Colors { get; set; }

    /// <summary>
    ///     Forces the use of the specified driver (one of "fake", "dotnet", "windows", or "unix"). If not
    ///     specified, the driver is selected based on the platform.
    /// </summary>
    string ForceDriver { get; set; }

    /// <summary>Initializes a new instance of <see cref="Terminal.Gui"/> Application.</summary>
    /// <para>Call this method once per instance (or after <see cref="Shutdown"/> has been called).</para>
    /// <para>
    ///     This function loads the right <see cref="IConsoleDriver"/> for the platform, Creates a <see cref="Toplevel"/>. and
    ///     assigns it to <see cref="Application.Top"/>
    /// </para>
    /// <para>
    ///     <see cref="Shutdown"/> must be called when the application is closing (typically after
    ///     <see cref="Run{T}"/> has returned) to ensure resources are cleaned up and
    ///     terminal settings
    ///     restored.
    /// </para>
    /// <para>
    ///     The <see cref="Run{T}"/> function combines
    ///     <see cref="Init(IConsoleDriver,string)"/> and <see cref="Run(Toplevel, Func{Exception, bool})"/>
    ///     into a single
    ///     call. An application cam use <see cref="Run{T}"/> without explicitly calling
    ///     <see cref="Init(IConsoleDriver,string)"/>.
    /// </para>
    /// <param name="driver">
    ///     The <see cref="IConsoleDriver"/> to use. If neither <paramref name="driver"/> or
    ///     <paramref name="driverName"/> are specified the default driver for the platform will be used.
    /// </param>
    /// <param name="driverName">
    ///     The driver name (e.g. "dotnet", "windows", "fake", or "unix") of the
    ///     <see cref="IConsoleDriver"/> to use. If neither <paramref name="driver"/> or <paramref name="driverName"/> are
    ///     specified the default driver for the platform will be used.
    /// </param>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Init (IConsoleDriver? driver = null, string? driverName = null);

    /// <summary>Gets or sets whether the application has been initialized.</summary>
    bool Initialized { get; set; }

    /// <summary>Runs <paramref name="action"/> on the main UI loop thread</summary>
    /// <param name="action">the action to be invoked on the main processing thread.</param>
    void Invoke (Action action);

    /// <summary>
    ///     <see langword="true"/> if implementation is 'old'. <see langword="false"/> if implementation
    ///     is cutting edge.
    /// </summary>
    bool IsLegacy { get; }

    /// <summary>
    ///     Handles keyboard input and key bindings at the Application level.
    /// </summary>
    IKeyboard Keyboard { get; set; }

    /// <summary>
    ///     Causes any Toplevels that need layout to be laid out. Then draws any Toplevels that need display. Only Views that
    ///     need to be laid out (see <see cref="View.NeedsLayout"/>) will be laid out.
    ///     Only Views that need to be drawn (see <see cref="View.NeedsDraw"/>) will be drawn.
    /// </summary>
    /// <param name="forceRedraw">
    ///     If <see langword="true"/> the entire View hierarchy will be redrawn. The default is <see langword="false"/> and
    ///     should only be overriden for testing.
    /// </param>
    public void LayoutAndDraw (bool forceRedraw = false);

    /// <summary>
    ///     Called when the application's size changes. Sets the size of all <see cref="Toplevel"/>s and fires the
    ///     SizeChanging event.
    /// </summary>
    /// <param name="args">The new size.</param>
    /// <returns><see langword="true"/> if the size was changed.</returns>
    bool OnSizeChanging (SizeChangedEventArgs args);

    /// <summary>
    ///     Maximum number of iterations of the main loop (and hence draws)
    ///     to allow to occur per second. Defaults to <see cref="Application.DEFAULT_MAXIMUM_ITERATIONS_PER_SECOND"/> which is
    ///     a 40ms sleep
    ///     after iteration (factoring in how long iteration took to run).
    ///     <remarks>
    ///         Note that not every iteration draws (see <see cref="View.NeedsDraw"/>).
    ///         Only affects v2 drivers.
    ///     </remarks>
    /// </summary>
    ushort MaximumIterationsPerSecond { get; set; }

    /// <summary>
    ///     Handles mouse event state and processing.
    /// </summary>
    IMouse Mouse { get; set; }

    /// <summary>Gets or sets the navigation manager.</summary>
    ApplicationNavigation? Navigation { get; set; }

    /// <summary>Gets or sets the popover manager.</summary>
    ApplicationPopover? Popover { get; set; }

    /// <summary>Removes a previously scheduled timeout</summary>
    /// <remarks>The token parameter is the value returned by <see cref="AddTimeout"/>.</remarks>
    /// <returns>
    ///     <see langword="true"/>
    ///     if the timeout is successfully removed; otherwise,
    ///     <see langword="false"/>
    ///     .
    ///     This method also returns
    ///     <see langword="false"/>
    ///     if the timeout is not found.
    /// </returns>
    bool RemoveTimeout (object token);

    /// <summary>Requests that the application stop running.</summary>
    void RequestStop ();

    /// <summary>Stops the provided <see cref="Toplevel"/>, causing or the <paramref name="top"/> if provided.</summary>
    /// <param name="top">The <see cref="Toplevel"/> to stop.</param>
    /// <remarks>
    ///     <para>This will cause <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to return.</para>
    ///     <para>
    ///         Calling <see cref="RequestStop(Toplevel)"/> is equivalent to setting the <see cref="Toplevel.Running"/>
    ///         property on the currently running <see cref="Toplevel"/> to false.
    ///     </para>
    /// </remarks>
    void RequestStop (Toplevel? top);

    /// <summary>
    ///     Resets the application state to defaults. This is called by <see cref="Shutdown"/>.
    /// </summary>
    /// <param name="ignoreDisposed">If true, will not assert that views are disposed.</param>
    void ResetState (bool ignoreDisposed = false);

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/> object and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>.
    /// </summary>
    /// <remarks>
    ///     <para>Calling <see cref="Init"/> first is not needed as this function will initialize the application.</para>
    ///     <para>
    ///         <see cref="Shutdown"/> must be called when the application is closing (typically after Run> has returned) to
    ///         ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         The caller is responsible for disposing the object returned by this method.
    ///     </para>
    /// </remarks>
    /// <returns>The created <see cref="Toplevel"/> object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, IConsoleDriver? driver = null);

    /// <summary>
    ///     Runs the application by creating a <see cref="Toplevel"/>-derived object of type <c>T</c> and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>.
    /// </summary>
    /// <remarks>
    ///     <para>Calling <see cref="Init"/> first is not needed as this function will initialize the application.</para>
    ///     <para>
    ///         <see cref="Shutdown"/> must be called when the application is closing (typically after Run> has returned) to
    ///         ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         The caller is responsible for disposing the object returned by this method.
    ///     </para>
    /// </remarks>
    /// <param name="errorHandler"></param>
    /// <param name="driver">
    ///     The <see cref="IConsoleDriver"/> to use. If not specified the default driver for the platform will
    ///     be used. Must be
    ///     <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created T object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public T Run<T> (Func<Exception, bool>? errorHandler = null, IConsoleDriver? driver = null)
        where T : Toplevel, new ();

    /// <summary>Runs the Application using the provided <see cref="Toplevel"/> view.</summary>
    /// <remarks>
    ///     <para>
    ///         This method is used to start processing events for the main application, but it is also used to run other
    ///         modal <see cref="View"/>s such as <see cref="Dialog"/> boxes.
    ///     </para>
    ///     <para>
    ///         To make a <see cref="Run(Toplevel,System.Func{System.Exception,bool})"/> stop execution, call
    ///         <see cref="Application.RequestStop"/>.
    ///     </para>
    ///     <para>
    ///         Calling <see cref="Run(Toplevel,System.Func{System.Exception,bool})"/> is equivalent to calling
    ///         <see cref="Application.Begin(Toplevel)"/>, followed by <see cref="Application.RunLoop(RunState)"/>, and then
    ///         calling
    ///         <see cref="Application.End(RunState)"/>.
    ///     </para>
    ///     <para>
    ///         Alternatively, to have a program control the main loop and process events manually, call
    ///         <see cref="Application.Begin(Toplevel)"/> to set things up manually and then repeatedly call
    ///         <see cref="Application.RunLoop(RunState)"/> with the wait parameter set to false. By doing this the
    ///         <see cref="Application.RunLoop(RunState)"/> method will only process any pending events, timers handlers and
    ///         then
    ///         return control immediately.
    ///     </para>
    ///     <para>
    ///         When using <see cref="Run{T}"/> or
    ///         <see cref="Run(System.Func{System.Exception,bool},IConsoleDriver)"/>
    ///         <see cref="Init"/> will be called automatically.
    ///     </para>
    ///     <para>
    ///         RELEASE builds only: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, if <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the <see cref="Application.RunLoop(RunState)"/> will resume; otherwise this
    ///         method will
    ///         exit.
    ///     </para>
    /// </remarks>
    /// <param name="view">The <see cref="Toplevel"/> to run as a modal.</param>
    /// <param name="errorHandler">
    ///     RELEASE builds only: Handler for any unhandled exceptions (resumes when returns true,
    ///     rethrows when null).
    /// </param>
    public void Run (Toplevel view, Func<Exception, bool>? errorHandler = null);

    /// <summary>
    ///     Gets or sets the size of the screen. By default, this is the size of the screen as reported by the
    ///     <see cref="IConsoleDriver"/>.
    ///     Setting the position is not supported and may throw <see cref="NotImplementedException"/>. The size may be set
    ///     but will not persist if the terminal is resized and will not impact the actual terminal size.
    /// </summary>
    Rectangle Screen { get; set; }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    /// <remarks>
    ///     Shutdown must be called for every call to <see cref="Init"/> or
    ///     <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to ensure all resources are cleaned
    ///     up (Disposed)
    ///     and terminal settings are restored.
    /// </remarks>
    public void Shutdown ();

    /// <summary>
    ///     Collection of sixel images to write out to screen when updating.
    ///     Only add to this collection if you are sure terminal supports sixel format.
    /// </summary>
    List<SixelToRender> Sixel { get; }

    /// <summary>Gets all cultures supported by the application without the invariant language.</summary>
    List<CultureInfo>? SupportedCultures { get; }

    /// <summary>
    ///     Handles recurring events. These are invoked on the main UI thread - allowing for
    ///     safe updates to <see cref="View"/> instances.
    /// </summary>
    ITimedEvents? TimedEvents { get; }

    /// <summary>Gets the currently active Toplevel.</summary>
    Toplevel? Top { get; set; }

    /// <summary>Gets the stack of all Toplevels.</summary>
    ConcurrentStack<Toplevel> TopLevels { get; }
}
