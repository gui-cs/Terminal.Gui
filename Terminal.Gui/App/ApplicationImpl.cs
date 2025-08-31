#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

/// <summary>
/// Original Terminal.Gui implementation of core <see cref="Application"/> methods.
/// </summary>
public class ApplicationImpl : IApplication
{
    // Private static readonly Lazy instance of Application
    private static Lazy<IApplication> _lazyInstance = new (() => new ApplicationImpl ());

    /// <summary>
    /// Gets the currently configured backend implementation of <see cref="Application"/> gateway methods.
    /// Change to your own implementation by using <see cref="ChangeInstance"/> (before init).
    /// </summary>
    public static IApplication Instance => _lazyInstance.Value;


    /// <inheritdoc/>
    public virtual ITimedEvents? TimedEvents => Application.MainLoop?.TimedEvents;

    /// <summary>
    /// Handles which <see cref="View"/> (if any) has captured the mouse
    /// </summary>
    public IMouseGrabHandler MouseGrabHandler { get; set; } = new MouseGrabHandler ();

    /// <summary>
    /// Change the singleton implementation, should not be called except before application
    /// startup. This method lets you provide alternative implementations of core static gateway
    /// methods of <see cref="Application"/>.
    /// </summary>
    /// <param name="newApplication"></param>
    public static void ChangeInstance (IApplication newApplication)
    {
        _lazyInstance = new Lazy<IApplication> (newApplication);
    }

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public virtual void Init (IConsoleDriver? driver = null, string? driverName = null)
    {
        Application.InternalInit (driver, string.IsNullOrWhiteSpace (driverName) ? Application.ForceDriver : driverName);
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
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, IConsoleDriver? driver = null) { return Run<Toplevel> (errorHandler, driver); }

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
    ///     be used ( <see cref="WindowsDriver"/>, <see cref="CursesDriver"/>, or <see cref="NetDriver"/>). Must be
    ///     <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created T object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public virtual T Run<T> (Func<Exception, bool>? errorHandler = null, IConsoleDriver? driver = null)
        where T : Toplevel, new()
    {
        if (!Application.Initialized)
        {
            // Init() has NOT been called.
            Application.InternalInit (driver, Application.ForceDriver, true);
        }

        if (Instance is ApplicationV2)
        {
            return Instance.Run<T> (errorHandler, driver);
        }

        var top = new T ();

        Run (top, errorHandler);

        return top;
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
    ///         <see cref="Application.Begin(Toplevel)"/>, followed by <see cref="Application.RunLoop(RunState)"/>, and then calling
    ///         <see cref="Application.End(RunState)"/>.
    ///     </para>
    ///     <para>
    ///         Alternatively, to have a program control the main loop and process events manually, call
    ///         <see cref="Application.Begin(Toplevel)"/> to set things up manually and then repeatedly call
    ///         <see cref="Application.RunLoop(RunState)"/> with the wait parameter set to false. By doing this the
    ///         <see cref="Application.RunLoop(RunState)"/> method will only process any pending events, timers handlers and then
    ///         return control immediately.
    ///     </para>
    ///     <para>When using <see cref="Run{T}"/> or
    ///         <see cref="Run(System.Func{System.Exception,bool},IConsoleDriver)"/>
    ///         <see cref="Init"/> will be called automatically.
    ///     </para>
    ///     <para>
    ///         RELEASE builds only: When <paramref name="errorHandler"/> is <see langword="null"/> any exceptions will be
    ///         rethrown. Otherwise, if <paramref name="errorHandler"/> will be called. If <paramref name="errorHandler"/>
    ///         returns <see langword="true"/> the <see cref="Application.RunLoop(RunState)"/> will resume; otherwise this method will
    ///         exit.
    ///     </para>
    /// </remarks>
    /// <param name="view">The <see cref="Toplevel"/> to run as a modal.</param>
    /// <param name="errorHandler">
    ///     RELEASE builds only: Handler for any unhandled exceptions (resumes when returns true,
    ///     rethrows when null).
    /// </param>
    public virtual void Run (Toplevel view, Func<Exception, bool>? errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull (view);

        if (Application.Initialized)
        {
            if (Application.Driver is null)
            {
                // Disposing before throwing
                view.Dispose ();

                // This code path should be impossible because Init(null, null) will select the platform default driver
                throw new InvalidOperationException (
                                                     "Init() completed without a driver being set (this should be impossible); Run<T>() cannot be called."
                                                    );
            }
        }
        else
        {
            // Init() has NOT been called.
            throw new InvalidOperationException (
                                                 "Init() has not been called. Only Run() or Run<T>() can be used without calling Init()."
                                                );
        }

        var resume = true;

        while (resume)
        {
#if !DEBUG
            try
            {
#endif
                resume = false;
                RunState runState = Application.Begin (view);

                // If EndAfterFirstIteration is true then the user must dispose of the runToken
                // by using NotifyStopRunState event.
                Application.RunLoop (runState);

                if (runState.Toplevel is null)
                {
#if DEBUG_IDISPOSABLE
                if (View.EnableDebugIDisposableAsserts)
                {
                    Debug.Assert (Application.TopLevels.Count == 0);
                }
#endif
                    runState.Dispose ();

                    return;
                }

                if (!Application.EndAfterFirstIteration)
                {
                    Application.End (runState);
                }
#if !DEBUG
            }
            catch (Exception error)
            {
                Logging.Warning ($"Release Build Exception: {error}");
                if (errorHandler is null)
                {
                    throw;
                }

                resume = errorHandler (error);
            }
#endif
        }
    }

    /// <summary>Shutdown an application initialized with <see cref="Init"/>.</summary>
    /// <remarks>
    ///     Shutdown must be called for every call to <see cref="Init"/> or
    ///     <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to ensure all resources are cleaned
    ///     up (Disposed)
    ///     and terminal settings are restored.
    /// </remarks>
    public virtual void Shutdown ()
    {
        // TODO: Throw an exception if Init hasn't been called.

        bool wasInitialized = Application.Initialized;
        Application.ResetState ();
        ConfigurationManager.PrintJsonErrors ();

        if (wasInitialized)
        {
            bool init = Application.Initialized;

            Application.OnInitializedChanged (this, new (in init));
        }

        _lazyInstance = new (() => new ApplicationImpl ());
    }

    /// <inheritdoc />
    public virtual void RequestStop (Toplevel? top)
    {
        top ??= Application.Top;

        if (!top!.Running)
        {
            return;
        }

        var ev = new ToplevelClosingEventArgs (top);
        top.OnClosing (ev);

        if (ev.Cancel)
        {
            return;
        }

        top.Running = false;
        Application.OnNotifyStopRunState (top);
    }

    /// <inheritdoc />
    public virtual void Invoke (Action action)
    {

        // If we are already on the main UI thread
        if (Application.MainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action ();
            WakeupMainLoop ();

            return;
        }

        if (Application.MainLoop == null)
        {
            Logging.Warning ("Ignored Invoke because MainLoop is not initialized yet");
            return;
        }


        Application.AddTimeout (TimeSpan.Zero,
                           () =>
                           {
                               action ();

                               return false;
                           }
                          );

        WakeupMainLoop ();

        void WakeupMainLoop ()
        {
            // Ensure the action is executed in the main loop
            // Wakeup mainloop if it's waiting for events
            Application.MainLoop?.Wakeup ();
        }
    }

    /// <inheritdoc />
    public bool IsLegacy { get; protected set; } = true;

    /// <inheritdoc />
    public virtual object AddTimeout (TimeSpan time, Func<bool> callback)
    {
        if (Application.MainLoop is null)
        {
            throw new NotInitializedException ("Cannot add timeout before main loop is initialized", null);
        }

        return Application.MainLoop.TimedEvents.Add (time, callback);
    }

    /// <inheritdoc />
    public virtual bool RemoveTimeout (object token)
    {
        return Application.MainLoop?.TimedEvents.Remove (token) ?? false;
    }

    /// <inheritdoc />
    public virtual void LayoutAndDraw (bool forceDraw)
    {
        Application.LayoutAndDrawImpl (forceDraw);
    }
}
