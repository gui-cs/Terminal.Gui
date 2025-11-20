using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

/// <summary>
///     Interface for instances that provide backing functionality to static
///     gateway class <see cref="Application"/>.
/// </summary>
public interface IApplication
{
    #region Keyboard

    /// <summary>
    ///     Handles keyboard input and key bindings at the Application level.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides access to keyboard state, key bindings, and keyboard event handling. Set during <see cref="Init"/>.
    ///     </para>
    /// </remarks>
    IKeyboard Keyboard { get; set; }

    #endregion Keyboard

    #region Mouse

    /// <summary>
    ///     Handles mouse event state and processing.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides access to mouse state, mouse grabbing, and mouse event handling. Set during <see cref="Init"/>.
    ///     </para>
    /// </remarks>
    IMouse Mouse { get; set; }

    #endregion Mouse

    #region Initialization and Shutdown

    /// <summary>Initializes a new instance of <see cref="Terminal.Gui"/> Application.</summary>
    /// <param name="driverName">
    ///     The short name (e.g. "dotnet", "windows", "unix", or "fake") of the
    ///     <see cref="IDriver"/> to use. If not specified the default driver for the platform will be used.
    /// </param>
    /// <remarks>
    ///     <para>Call this method once per instance (or after <see cref="Shutdown"/> has been called).</para>
    ///     <para>
    ///         This function loads the right <see cref="IDriver"/> for the platform, creates a main loop coordinator,
    ///         initializes keyboard and mouse handlers, and subscribes to driver events.
    ///     </para>
    ///     <para>
    ///         <see cref="Shutdown"/> must be called when the application is closing (typically after
    ///         <see cref="Run{T}"/> has returned) to ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         The <see cref="Run{T}"/> function combines <see cref="Init(string)"/> and
    ///         <see cref="Run(Toplevel, Func{Exception, bool})"/> into a single call. An application can use
    ///         <see cref="Run{T}"/> without explicitly calling <see cref="Init(string)"/>.
    ///     </para>
    /// </remarks>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public void Init (string? driverName = null);

    /// <summary>
    ///     This event is raised after the <see cref="Init"/> and <see cref="Shutdown"/> methods have been called.
    /// </summary>
    /// <remarks>
    ///     Intended to support unit tests that need to know when the application has been initialized.
    /// </remarks>
    public event EventHandler<EventArgs<bool>>? InitializedChanged;

    /// <summary>Gets or sets whether the application has been initialized.</summary>
    bool Initialized { get; set; }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    /// <remarks>
    ///     Shutdown must be called for every call to <see cref="Init"/> or
    ///     <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to ensure all resources are cleaned
    ///     up (Disposed) and terminal settings are restored.
    /// </remarks>
    public void Shutdown ();

    /// <summary>
    ///     Resets the state of this instance.
    /// </summary>
    /// <param name="ignoreDisposed">If true, ignores disposed state checks during reset.</param>
    /// <remarks>
    ///     <para>
    ///         Encapsulates all setting of initial state for Application; having this in a function like this ensures we
    ///         don't make mistakes in guaranteeing that the state of this singleton is deterministic when <see cref="Init"/>
    ///         starts running and after <see cref="Shutdown"/> returns.
    ///     </para>
    ///     <para>
    ///         IMPORTANT: Ensure all property/fields are reset here. See Init_ResetState_Resets_Properties unit test.
    ///     </para>
    /// </remarks>
    public void ResetState (bool ignoreDisposed = false);

    #endregion Initialization and Shutdown

    #region Begin->Run->Iteration->Stop->End

    /// <summary>
    ///     Building block API: Creates a <see cref="SessionToken"/> and prepares the provided <see cref="Toplevel"/> for
    ///     execution. Not usually called directly by applications. Use <see cref="Run(Toplevel, Func{Exception, bool})"/>
    ///     instead.
    /// </summary>
    /// <returns>
    ///     The <see cref="SessionToken"/> that needs to be passed to the <see cref="End(SessionToken)"/> method upon
    ///     completion.
    /// </returns>
    /// <param name="toplevel">The <see cref="Toplevel"/> to prepare execution for.</param>
    /// <remarks>
    ///     <para>
    ///         This method prepares the provided <see cref="Toplevel"/> for running. It adds this to the
    ///         list of <see cref="Toplevel"/>s, lays out the SubViews, focuses the first element, and draws the
    ///         <see cref="Toplevel"/> on the screen. This is usually followed by starting the main loop, and then the
    ///         <see cref="End(SessionToken)"/> method upon termination which will undo these changes.
    ///     </para>
    ///     <para>
    ///         Raises the <see cref="SessionBegun"/> event before returning.
    ///     </para>
    /// </remarks>
    public SessionToken Begin (Toplevel toplevel);

    /// <summary>
    ///     Runs a new Session creating a <see cref="Toplevel"/> and calling <see cref="Begin(Toplevel)"/>. When the session is
    ///     stopped, <see cref="End(SessionToken)"/> will be called.
    /// </summary>
    /// <param name="errorHandler">Handler for any unhandled exceptions (resumes when returns true, rethrows when null).</param>
    /// <param name="driverName">
    ///     The driver name. If not specified the default driver for the platform will be used. Must be
    ///     <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created <see cref="Toplevel"/>. The caller is responsible for disposing this object.</returns>
    /// <remarks>
    ///     <para>Calling <see cref="Init"/> first is not needed as this function will initialize the application.</para>
    ///     <para>
    ///         <see cref="Shutdown"/> must be called when the application is closing (typically after Run has returned) to
    ///         ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         The caller is responsible for disposing the object returned by this method.
    ///     </para>
    /// </remarks>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driverName = null);

    /// <summary>
    ///     Runs a new Session creating a <see cref="Toplevel"/>-derived object of type <typeparamref name="TView"/>
    ///     and calling <see cref="Run(Toplevel, Func{Exception, bool})"/>. When the session is stopped,
    ///     <see cref="End(SessionToken)"/> will be called.
    /// </summary>
    /// <typeparam name="TView">The type of <see cref="Toplevel"/> to create and run.</typeparam>
    /// <param name="errorHandler">Handler for any unhandled exceptions (resumes when returns true, rethrows when null).</param>
    /// <param name="driverName">
    ///     The driver name. If not specified the default driver for the platform will be used. Must be
    ///     <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created <typeparamref name="TView"/> object. The caller is responsible for disposing this object.</returns>
    /// <remarks>
    ///     <para>
    ///         This method is used to start processing events for the main application, but it is also used to run other
    ///         modal <see cref="View"/>s such as <see cref="Dialog"/> boxes.
    ///     </para>
    ///     <para>
    ///         To make <see cref="Run(Toplevel, Func{Exception, bool})"/> stop execution, call
    ///         <see cref="RequestStop()"/> or <see cref="RequestStop(Toplevel)"/>.
    ///     </para>
    ///     <para>
    ///         Calling <see cref="Run(Toplevel, Func{Exception, bool})"/> is equivalent to calling
    ///         <see cref="Begin(Toplevel)"/>, followed by starting the main loop, and then calling
    ///         <see cref="End(SessionToken)"/>.
    ///     </para>
    ///     <para>
    ///         When using <see cref="Run{T}"/> or <see cref="Run(Func{Exception, bool}, string)"/>,
    ///         <see cref="Init"/> will be called automatically.
    ///     </para>
    ///     <para>
    ///         In RELEASE builds: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the main loop will resume; otherwise this method will exit.
    ///     </para>
    ///     <para>
    ///         <see cref="Shutdown"/> must be called when the application is closing (typically after Run has returned) to
    ///         ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         In RELEASE builds: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the main loop will resume; otherwise this method will exit.
    ///     </para>
    ///     <para>
    ///         The caller is responsible for disposing the object returned by this method.
    ///     </para>
    /// </remarks>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public TView Run<TView> (Func<Exception, bool>? errorHandler = null, string? driverName = null)
        where TView : Toplevel, new ();

    /// <summary>
    ///     Runs a new Session using the provided <see cref="Toplevel"/> view and calling
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>.
    ///     When the session is stopped, <see cref="End(SessionToken)"/> will be called..
    /// </summary>
    /// <param name="view">The <see cref="Toplevel"/> to run as a modal.</param>
    /// <param name="errorHandler">Handler for any unhandled exceptions (resumes when returns true, rethrows when null).</param>
    /// <remarks>
    ///     <para>
    ///         This method is used to start processing events for the main application, but it is also used to run other
    ///         modal <see cref="View"/>s such as <see cref="Dialog"/> boxes.
    ///     </para>
    ///     <para>
    ///         To make <see cref="Run(Toplevel, Func{Exception, bool})"/> stop execution, call
    ///         <see cref="RequestStop()"/> or <see cref="RequestStop(Toplevel)"/>.
    ///     </para>
    ///     <para>
    ///         Calling <see cref="Run(Toplevel, Func{Exception, bool})"/> is equivalent to calling
    ///         <see cref="Begin(Toplevel)"/>, followed by starting the main loop, and then calling
    ///         <see cref="End(SessionToken)"/>.
    ///     </para>
    ///     <para>
    ///         When using <see cref="Run{T}"/> or <see cref="Run(Func{Exception, bool}, string)"/>,
    ///         <see cref="Init"/> will be called automatically.
    ///     </para>
    ///     <para>
    ///         <see cref="Shutdown"/> must be called when the application is closing (typically after Run has returned) to
    ///         ensure resources are cleaned up and terminal settings restored.
    ///     </para>
    ///     <para>
    ///         In RELEASE builds: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the main loop will resume; otherwise this method will exit.
    ///     </para>
    ///     <para>
    ///         The caller is responsible for disposing the object returned by this method.
    ///     </para>
    /// </remarks>
    public void Run (Toplevel view, Func<Exception, bool>? errorHandler = null);

    /// <summary>
    ///     Raises the <see cref="Iteration"/> event.
    /// </summary>
    /// <remarks>
    ///     This is called once per main loop iteration, before processing input, timeouts, or rendering.
    /// </remarks>
    public void RaiseIteration ();

    /// <summary>This event is raised on each iteration of the main loop.</summary>
    /// <remarks>
    ///     <para>
    ///         This event is raised before input processing, timeout callbacks, and rendering occur each iteration.
    ///     </para>
    ///     <para>See also <see cref="AddTimeout"/> and <see cref="TimedEvents"/>.</para>
    /// </remarks>
    public event EventHandler<IterationEventArgs>? Iteration;

    /// <summary>Runs <paramref name="action"/> on the main UI loop thread.</summary>
    /// <param name="action">The action to be invoked on the main processing thread.</param>
    /// <remarks>
    ///     <para>
    ///         If called from the main thread, the action is executed immediately. Otherwise, it is queued via
    ///         <see cref="AddTimeout"/> with <see cref="TimeSpan.Zero"/> and will be executed on the next main loop
    ///         iteration.
    ///     </para>
    /// </remarks>
    void Invoke (Action<IApplication>? action);

    /// <summary>Runs <paramref name="action"/> on the main UI loop thread.</summary>
    /// <param name="action">The action to be invoked on the main processing thread.</param>
    /// <remarks>
    ///     <para>
    ///         If called from the main thread, the action is executed immediately. Otherwise, it is queued via
    ///         <see cref="AddTimeout"/> with <see cref="TimeSpan.Zero"/> and will be executed on the next main loop
    ///         iteration.
    ///     </para>
    /// </remarks>
    void Invoke (Action action);

    /// <summary>
    ///     Building block API: Ends a Session and completes the execution of a <see cref="Toplevel"/> that was started with
    ///     <see cref="Begin(Toplevel)"/>. Not usually called directly by applications.
    ///     <see cref="Run(Toplevel, Func{Exception, bool})"/>
    ///     will automatically call this method when the session is stopped.
    /// </summary>
    /// <param name="sessionToken">The <see cref="SessionToken"/> returned by the <see cref="Begin(Toplevel)"/> method.</param>
    /// <remarks>
    ///     <para>
    ///         This method removes the <see cref="Toplevel"/> from the stack, raises the <see cref="SessionEnded"/>
    ///         event, and disposes the <paramref name="sessionToken"/>.
    ///     </para>
    /// </remarks>
    public void End (SessionToken sessionToken);

    /// <summary>Requests that the currently running Session stop. The Session will stop after the current iteration completes.</summary>
    /// <remarks>
    ///     <para>This will cause <see cref="Run(Toplevel, Func{Exception, bool})"/> to return.</para>
    ///     <para>
    ///         This is equivalent to calling <see cref="RequestStop(Toplevel)"/> with <see cref="Current"/> as the parameter.
    ///     </para>
    /// </remarks>
    void RequestStop ();

    /// <summary>Requests that the currently running Session stop. The Session will stop after the current iteration completes.</summary>
    /// <param name="top">
    ///     The <see cref="Toplevel"/> to stop. If <see langword="null"/>, stops the currently running <see cref="Current"/>.
    /// </param>
    /// <remarks>
    ///     <para>This will cause <see cref="Run(Toplevel, Func{Exception, bool})"/> to return.</para>
    ///     <para>
    ///         Calling <see cref="RequestStop(Toplevel)"/> is equivalent to setting the <see cref="Toplevel.IsRunning"/>
    ///         property on the specified <see cref="Toplevel"/> to <see langword="false"/>.
    ///     </para>
    /// </remarks>
    void RequestStop (Toplevel? top);

    /// <summary>
    ///     Set to <see langword="true"/> to cause the session to stop running after first iteration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Used primarily for unit testing. When <see langword="true"/>, <see cref="End"/> will be called
    ///         automatically after the first main loop iteration.
    ///     </para>
    /// </remarks>
    bool StopAfterFirstIteration { get; set; }

    /// <summary>
    ///     Raised when <see cref="Begin(Toplevel)"/> has been called and has created a new <see cref="SessionToken"/>.
    /// </summary>
    /// <remarks>
    ///     If <see cref="StopAfterFirstIteration"/> is <see langword="true"/>, callers to <see cref="Begin(Toplevel)"/>
    ///     must also subscribe to <see cref="SessionEnded"/> and manually dispose of the <see cref="SessionToken"/> token
    ///     when the application is done.
    /// </remarks>
    public event EventHandler<SessionTokenEventArgs>? SessionBegun;

    /// <summary>
    ///     Raised when <see cref="End(SessionToken)"/> was called and the session is stopping. The event args contain a
    ///     reference to the <see cref="Toplevel"/>
    ///     that was active during the session. This can be used to ensure the Toplevel is disposed of properly.
    /// </summary>
    /// <remarks>
    ///     If <see cref="StopAfterFirstIteration"/> is <see langword="true"/>, callers to <see cref="Begin(Toplevel)"/>
    ///     must also subscribe to <see cref="SessionEnded"/> and manually dispose of the <see cref="SessionToken"/> token
    ///     when the application is done.
    /// </remarks>
    public event EventHandler<ToplevelEventArgs>? SessionEnded;

    #endregion Begin->Run->Iteration->Stop->End

    #region Toplevel Management

    /// <summary>Gets or sets the currently active Toplevel.</summary>
    /// <remarks>
    ///     <para>
    ///         This is set by <see cref="Begin(Toplevel)"/> and cleared by <see cref="End(SessionToken)"/>.
    ///     </para>
    /// </remarks>
    Toplevel? Current { get; set; }

    /// <summary>Gets the stack of all active Toplevel sessions.</summary>
    /// <remarks>
    ///     <para>
    ///         Toplevels are added to this stack by <see cref="Begin(Toplevel)"/> and removed by
    ///         <see cref="End(SessionToken)"/>.
    ///     </para>
    /// </remarks>
    ConcurrentStack<Toplevel> SessionStack { get; }

    /// <summary>
    ///     Caches the Toplevel associated with the current Session.
    /// </summary>
    /// <remarks>
    ///     Used internally to optimize Toplevel state transitions.
    /// </remarks>
    Toplevel? CachedSessionTokenToplevel { get; set; }

    #endregion Toplevel Management

    #region Screen and Driver

    /// <summary>Gets or sets the console driver being used.</summary>
    /// <remarks>
    ///     <para>
    ///         Set by <see cref="Init"/> based on the driver parameter or platform default.
    ///     </para>
    /// </remarks>
    IDriver? Driver { get; set; }

    /// <summary>
    ///     Gets or sets whether <see cref="Driver"/> will be forced to output only the 16 colors defined in
    ///     <see cref="ColorName16"/>. The default is <see langword="false"/>, meaning 24-bit (TrueColor) colors will be
    ///     output as long as the selected <see cref="IDriver"/> supports TrueColor.
    /// </summary>
    bool Force16Colors { get; set; }

    /// <summary>
    ///     Forces the use of the specified driver (one of "fake", "dotnet", "windows", or "unix"). If not
    ///     specified, the driver is selected based on the platform.
    /// </summary>
    string ForceDriver { get; set; }

    /// <summary>
    ///     Gets or sets the size of the screen. By default, this is the size of the screen as reported by the
    ///     <see cref="IDriver"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the <see cref="IDriver"/> has not been initialized, this will return a default size of 2048x2048; useful
    ///         for unit tests.
    ///     </para>
    /// </remarks>
    Rectangle Screen { get; set; }

    /// <summary>Raised when the terminal's size changed. The new size of the terminal is provided.</summary>
    /// <remarks>
    ///     <para>
    ///         This event is raised when the driver detects a screen size change. The event provides the new screen
    ///         rectangle.
    ///     </para>
    /// </remarks>
    public event EventHandler<EventArgs<Rectangle>>? ScreenChanged;

    /// <summary>
    ///     Gets or sets whether the screen will be cleared, and all Views redrawn, during the next Application iteration.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is typically set to <see langword="true"/> when a View's <see cref="View.Frame"/> changes and that view
    ///         has no SuperView (e.g. when <see cref="Current"/> is moved or resized).
    ///     </para>
    ///     <para>
    ///         Automatically reset to <see langword="false"/> after <see cref="LayoutAndDraw"/> processes it.
    ///     </para>
    /// </remarks>
    bool ClearScreenNextIteration { get; set; }

    /// <summary>
    ///     Collection of sixel images to write out to screen when updating.
    ///     Only add to this collection if you are sure terminal supports sixel format.
    /// </summary>
    List<SixelToRender> Sixel { get; }

    #endregion Screen and Driver

    #region Layout and Drawing

    /// <summary>
    ///     Causes any Toplevels that need layout to be laid out, then draws any Toplevels that need display. Only Views
    ///     that need to be laid out (see <see cref="View.NeedsLayout"/>) will be laid out. Only Views that need to be drawn
    ///     (see <see cref="View.NeedsDraw"/>) will be drawn.
    /// </summary>
    /// <param name="forceRedraw">
    ///     If <see langword="true"/> the entire View hierarchy will be redrawn. The default is <see langword="false"/> and
    ///     should only be overridden for testing.
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method is called automatically each main loop iteration when any views need layout or drawing.
    ///     </para>
    ///     <para>
    ///         If <see cref="ClearScreenNextIteration"/> is <see langword="true"/>, the screen will be cleared before
    ///         drawing and the flag will be reset to <see langword="false"/>.
    ///     </para>
    /// </remarks>
    public void LayoutAndDraw (bool forceRedraw = false);

    /// <summary>
    ///     Calls <see cref="View.PositionCursor"/> on the most focused view.
    /// </summary>
    /// <remarks>
    ///     <para>Does nothing if there is no most focused view.</para>
    ///     <para>
    ///         If the most focused view is not visible within its superview, the cursor will be hidden.
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/> if a view positioned the cursor and the position is visible.</returns>
    public bool PositionCursor ();

    #endregion Layout and Drawing

    #region Navigation and Popover

    /// <summary>Gets or sets the popover manager.</summary>
    /// <remarks>
    ///     <para>
    ///         Manages application-level popover views. Initialized during <see cref="Init"/>.
    ///     </para>
    /// </remarks>
    ApplicationPopover? Popover { get; set; }

    /// <summary>Gets or sets the navigation manager.</summary>
    /// <remarks>
    ///     <para>
    ///         Manages focus navigation and tracking of the most focused view. Initialized during <see cref="Init"/>.
    ///     </para>
    /// </remarks>
    ApplicationNavigation? Navigation { get; set; }

    #endregion Navigation and Popover

    #region Timeouts

    /// <summary>Adds a timeout to the application.</summary>
    /// <param name="time">The time span to wait before invoking the callback.</param>
    /// <param name="callback">
    ///     The callback to invoke. If it returns <see langword="true"/>, the timeout will be reset and repeat. If it
    ///     returns <see langword="false"/>, the timeout will stop and be removed.
    /// </param>
    /// <returns>
    ///     Call <see cref="RemoveTimeout(object)"/> with the returned value to stop the timeout.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         When the time specified passes, the callback will be invoked on the main UI thread.
    ///     </para>
    ///     <para>
    ///         <see cref="IApplication.Shutdown"/> calls StopAll on <see cref="TimedEvents"/> to remove all timeouts.
    ///     </para>
    /// </remarks>
    object AddTimeout (TimeSpan time, Func<bool> callback);

    /// <summary>Removes a previously scheduled timeout.</summary>
    /// <param name="token">The token returned by <see cref="AddTimeout"/>.</param>
    /// <returns>
    ///     <see langword="true"/> if the timeout is successfully removed; otherwise, <see langword="false"/>.
    ///     This method also returns <see langword="false"/> if the timeout is not found.
    /// </returns>
    bool RemoveTimeout (object token);

    /// <summary>
    ///     Handles recurring events. These are invoked on the main UI thread - allowing for
    ///     safe updates to <see cref="View"/> instances.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Provides low-level access to the timeout management system. Most applications should use
    ///         <see cref="AddTimeout"/> and <see cref="RemoveTimeout"/> instead.
    ///     </para>
    /// </remarks>
    ITimedEvents? TimedEvents { get; }

    #endregion Timeouts

    /// <summary>
    ///     Gets a string representation of the Application as rendered by <see cref="Driver"/>.
    /// </summary>
    /// <returns>A string representation of the Application </returns>
    public string ToString ();
}
