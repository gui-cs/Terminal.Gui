#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public static partial class Application // Run (Begin -> Run -> Layout/Draw -> End -> Stop)
{
    /// <summary>Gets or sets the key to quit the application.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key QuitKey
    {
        get => ApplicationImpl.Instance.Keyboard.QuitKey;
        set => ApplicationImpl.Instance.Keyboard.QuitKey = value;
    }

    /// <summary>Gets or sets the key to activate arranging views using the keyboard.</summary>
    [ConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key ArrangeKey
    {
        get => ApplicationImpl.Instance.Keyboard.ArrangeKey;
        set => ApplicationImpl.Instance.Keyboard.ArrangeKey = value;
    }
    
    /// <summary>Building block API: Prepares the provided <see cref="Toplevel"/> for execution.</summary>
    /// <returns>
    ///     The <see cref="RunState"/> handle that needs to be passed to the <see cref="End(RunState)"/> method upon
    ///     completion.
    /// </returns>
    /// <param name="toplevel">The <see cref="Toplevel"/> to prepare execution for.</param>
    /// <remarks>
    ///     This method prepares the provided <see cref="Toplevel"/> for running with the focus, it adds this to the list
    ///     of <see cref="Toplevel"/>s, lays out the SubViews, focuses the first element, and draws the <see cref="Toplevel"/>
    ///     in the screen. This is usually followed by executing the <see cref="RunLoop"/> method, and then the
    ///     <see cref="End(RunState)"/> method upon termination which will undo these changes.
    /// </remarks>
    public static RunState Begin (Toplevel toplevel) => ApplicationImpl.Instance.Begin (toplevel);


    /// <summary>
    ///     Calls <see cref="View.PositionCursor"/> on the most focused view.
    /// </summary>
    /// <remarks>
    ///     Does nothing if there is no most focused view.
    ///     <para>
    ///         If the most focused view is not visible within it's superview, the cursor will be hidden.
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/> if a view positioned the cursor and the position is visible.</returns>
    public static bool PositionCursor ()
    {
        return ApplicationImpl.Instance.PositionCursor ();
    }

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
    public static Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driver = null)
    {
        return ApplicationImpl.Instance.Run (errorHandler, driver);
    }

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
    ///     The <see cref="IDriver"/> to use. If not specified the default driver for the platform will
    ///     be used. Must be <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created TView object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static TView Run<TView> (Func<Exception, bool>? errorHandler = null, string? driver = null)
        where TView : Toplevel, new()
    {
        return ApplicationImpl.Instance.Run<TView> (errorHandler, driver);
    }

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
    ///         <see cref="Begin(Toplevel)"/>, followed by <see cref="RunLoop(RunState)"/>, and then calling
    ///         <see cref="End(RunState)"/>.
    ///     </para>
    ///     <para>
    ///         Alternatively, to have a program control the main loop and process events manually, call
    ///         <see cref="Begin(Toplevel)"/> to set things up manually and then repeatedly call
    ///         <see cref="RunLoop(RunState)"/> with the wait parameter set to false. By doing this the
    ///         <see cref="RunLoop(RunState)"/> method will only process any pending events, timers handlers and then
    ///         return control immediately.
    ///     </para>
    ///     <para>
    ///         When using <see cref="Run{T}"/> or
    ///         <see cref="Init"/> will be called automatically.
    ///     </para>
    ///     <para>
    ///         RELEASE builds only: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, if <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the <see cref="RunLoop(RunState)"/> will resume; otherwise this method will
    ///         exit.
    ///     </para>
    /// </remarks>
    /// <param name="view">The <see cref="Toplevel"/> to run as a modal.</param>
    /// <param name="errorHandler">
    ///     RELEASE builds only: Handler for any unhandled exceptions (resumes when returns true,
    ///     rethrows when null).
    /// </param>
    public static void Run (Toplevel view, Func<Exception, bool>? errorHandler = null) { ApplicationImpl.Instance.Run (view, errorHandler); }

    /// <summary>Adds a timeout to the application.</summary>
    /// <remarks>
    ///     When time specified passes, the callback will be invoked. If the callback returns true, the timeout will be
    ///     reset, repeating the invocation. If it returns false, the timeout will stop and be removed. The returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="RemoveTimeout(object)"/>.
    /// </remarks>
    public static object? AddTimeout (TimeSpan time, Func<bool> callback) { return ApplicationImpl.Instance.AddTimeout (time, callback); }

    /// <summary>Removes a previously scheduled timeout</summary>
    /// <remarks>The token parameter is the value returned by <see cref="AddTimeout"/>.</remarks>
    /// Returns
    /// <see langword="true"/>
    /// if the timeout is successfully removed; otherwise,
    /// <see langword="false"/>
    /// .
    /// This method also returns
    /// <see langword="false"/>
    /// if the timeout is not found.
    public static bool RemoveTimeout (object token) { return ApplicationImpl.Instance.RemoveTimeout (token); }

    /// <summary>Runs <paramref name="action"/> on the thread that is processing events</summary>
    /// <param name="action">the action to be invoked on the main processing thread.</param>
    public static void Invoke (Action action) { ApplicationImpl.Instance.Invoke (action); }

    /// <summary>
    ///     Causes any Toplevels that need layout to be laid out. Then draws any Toplevels that need display. Only Views that
    ///     need to be laid out (see <see cref="View.NeedsLayout"/>) will be laid out.
    ///     Only Views that need to be drawn (see <see cref="View.NeedsDraw"/>) will be drawn.
    /// </summary>
    /// <param name="forceRedraw">
    ///     If <see langword="true"/> the entire View hierarchy will be redrawn. The default is <see langword="false"/> and
    ///     should only be overriden for testing.
    /// </param>
    public static void LayoutAndDraw (bool forceRedraw = false)
    {
        ApplicationImpl.Instance.LayoutAndDraw (forceRedraw);
    }


    /// <summary>
    ///     Set to true to cause <see cref="End"/> to be called after the first iteration. Set to false (the default) to
    ///     cause the application to continue running until Application.RequestStop () is called.
    /// </summary>
    public static bool StopAfterFirstIteration
    {
        get => ApplicationImpl.Instance.StopAfterFirstIteration;
        set => ApplicationImpl.Instance.StopAfterFirstIteration = value;
    }

    /// <summary>Stops the provided <see cref="Toplevel"/>, causing or the <paramref name="top"/> if provided.</summary>
    /// <param name="top">The <see cref="Toplevel"/> to stop.</param>
    /// <remarks>
    ///     <para>This will cause <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to return.</para>
    ///     <para>
    ///         Calling <see cref="RequestStop(Toplevel)"/> is equivalent to setting the
    ///         <see cref="Toplevel.Running"/>
    ///         property on the currently running <see cref="Toplevel"/> to false.
    ///     </para>
    /// </remarks>
    public static void RequestStop (Toplevel? top = null) { ApplicationImpl.Instance.RequestStop (top); }

    /// <summary>
    ///     Building block API: completes the execution of a <see cref="Toplevel"/> that was started with
    ///     <see cref="Begin(Toplevel)"/> .
    /// </summary>
    /// <param name="runState">The <see cref="RunState"/> returned by the <see cref="Begin(Toplevel)"/> method.</param>
    public static void End (RunState runState)
    {
        ApplicationImpl.Instance.End (runState);
    }

    internal static void RaiseIteration () => ApplicationImpl.Instance.RaiseIteration ();
}
