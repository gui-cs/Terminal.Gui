using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public partial class ApplicationImpl
{
    // Lock object to protect session stack operations and cached state updates
    private readonly object _sessionStackLock = new ();

    #region Session State - Stack and TopRunnable

    /// <inheritdoc/>
    public ConcurrentStack<SessionToken>? SessionStack { get; } = new ();

    /// <inheritdoc/>
    public IRunnable? TopRunnable { get; private set; }

    /// <inheritdoc/>
    public View? TopRunnableView => TopRunnable as View;

    /// <inheritdoc/>
    public event EventHandler<SessionTokenEventArgs>? SessionBegun;

    /// <inheritdoc/>
    public event EventHandler<SessionTokenEventArgs>? SessionEnded;

    #endregion Session State - Stack and TopRunnable

    #region Main Loop Iteration

    /// <inheritdoc/>
    public bool StopAfterFirstIteration { get; set; }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<IApplication?>>? Iteration;

    /// <inheritdoc/>
    public void RaiseIteration () { Iteration?.Invoke (null, new (this)); }

    #endregion Main Loop Iteration

    #region Timeouts and Invoke

    private readonly ITimedEvents _timedEvents = new TimedEvents ();

    /// <inheritdoc/>
    public ITimedEvents? TimedEvents => _timedEvents;

    /// <inheritdoc/>
    public object AddTimeout (TimeSpan time, Func<bool> callback) => _timedEvents.Add (time, callback);

    /// <inheritdoc/>
    public bool RemoveTimeout (object token) => _timedEvents.Remove (token);

    /// <inheritdoc/>
    public void Invoke (Action<IApplication>? action)
    {
        // If we are already on the main UI thread
        if (TopRunnableView is IRunnable { IsRunning: true } && MainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action?.Invoke (this);

            return;
        }

        _timedEvents.Add (
                          TimeSpan.Zero,
                          () =>
                          {
                              action?.Invoke (this);

                              return false;
                          }
                         );
    }

    /// <inheritdoc/>
    public void Invoke (Action action)
    {
        // If we are already on the main UI thread
        if (TopRunnableView is IRunnable { IsRunning: true } && MainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action?.Invoke ();

            return;
        }

        _timedEvents.Add (
                          TimeSpan.Zero,
                          () =>
                          {
                              action?.Invoke ();

                              return false;
                          }
                         );
    }

    #endregion Timeouts and Invoke

    #region Session Lifecycle - Begin

    /// <inheritdoc/>
    public SessionToken? Begin (IRunnable runnable)
    {
        ArgumentNullException.ThrowIfNull (runnable);

        if (runnable.IsRunning)
        {
            throw new ArgumentException (@"The runnable is already running.", nameof (runnable));
        }

        // Create session token
        SessionToken token = new (runnable);

        // Get old IsRunning value BEFORE any stack changes (safe - cached value)
        bool oldIsRunning = runnable.IsRunning;

        // Raise IsRunningChanging OUTSIDE lock (false -> true) - can be canceled
        if (runnable.RaiseIsRunningChanging (oldIsRunning, true))
        {
            // Starting was canceled
            return null;
        }

        // Set the application reference in the runnable
        runnable.SetApp (this);

        // Ensure the mouse is ungrabbed
        Mouse.UngrabMouse ();

        IRunnable? previousTop = null;

        // CRITICAL SECTION - Atomic stack + cached state update
        lock (_sessionStackLock)
        {
            // Get the previous top BEFORE pushing new token
            if (SessionStack?.TryPeek (out SessionToken? previousToken) == true && previousToken?.Runnable is { })
            {
                previousTop = previousToken.Runnable;
            }

            if (previousTop == runnable)
            {
                throw new ArgumentOutOfRangeException (nameof (runnable), runnable, @"Attempt to Run the runnable that's already the top runnable.");
            }

            // Push token onto SessionStack
            SessionStack?.Push (token);

            TopRunnable = runnable;

            // Update cached state atomically - IsRunning and IsModal are now consistent
            SessionBegun?.Invoke (this, new (token));
            runnable.SetIsRunning (true);
            runnable.SetIsModal (true);

            // Previous top is no longer modal
            if (previousTop != null)
            {
                previousTop.SetIsModal (false);
            }
        }

        // END CRITICAL SECTION - IsRunning/IsModal now thread-safe

        // Fire events AFTER lock released (avoid deadlocks in event handlers)
        if (previousTop != null)
        {
            previousTop.RaiseIsModalChangedEvent (false);
        }

        runnable.RaiseIsRunningChangedEvent (true);
        runnable.RaiseIsModalChangedEvent (true);

        LayoutAndDraw ();

        return token;
    }

    #endregion Session Lifecycle - Begin

    #region Session Lifecycle - Run

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public IApplication Run<TRunnable> (Func<Exception, bool>? errorHandler = null, string? driverName = null)
        where TRunnable : IRunnable, new()
    {
        if (!Initialized)
        {
            // Init() has NOT been called. Auto-initialize as per interface contract.
            Init (driverName);
        }

        if (Driver is null)
        {
            throw new InvalidOperationException (@"Driver is null after Init.");
        }

        TRunnable runnable = new ();
        object? result = Run (runnable, errorHandler);

        // We created the runnable, so dispose it if it's disposable
        if (runnable is IDisposable disposable)
        {
            disposable.Dispose ();
        }

        return this;
    }

    /// <inheritdoc/>
    public object? Run (IRunnable runnable, Func<Exception, bool>? errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull (runnable);

        if (!Initialized)
        {
            throw new NotInitializedException (@"Init must be called before Run.");
        }

        // Begin the session (adds to stack, raises IsRunningChanging/IsRunningChanged)
        SessionToken? token;

        if (runnable.IsRunning)
        {
            // Find it on the stack
            token = SessionStack?.FirstOrDefault (st => st.Runnable == runnable);
        }
        else
        {
            token = Begin (runnable);
        }

        if (token is null)
        {
            Logging.Trace (@"Run - Begin session failed or was cancelled.");

            return null;
        }

        try
        {
            // All runnables block until RequestStop() is called
            RunLoop (runnable, errorHandler);
        }
        finally
        {
            // End the session (raises IsRunningChanging/IsRunningChanged, pops from stack)
            End (token);
        }

        return token.Result;
    }

    private void RunLoop (IRunnable runnable, Func<Exception, bool>? errorHandler)
    {
        runnable.StopRequested = false;

        // Main loop - blocks until RequestStop() is called
        // Note: IsRunning is now a cached property, safe to check each iteration
        var firstIteration = true;

        while (runnable is { StopRequested: false, IsRunning: true })
        {
            if (Coordinator is null)
            {
                throw new ($"{nameof (IMainLoopCoordinator)} inexplicably became null during Run");
            }

            try
            {
                // Process one iteration of the event loop
                Coordinator.RunIteration ();
            }
            catch (Exception ex)
            {
                if (errorHandler is null || !errorHandler (ex))
                {
                    throw;
                }
            }

            if (StopAfterFirstIteration && firstIteration)
            {
                Logging.Information ("Run - Stopping after first iteration as requested");
                RequestStop (runnable);
            }

            firstIteration = false;
        }
    }

    #endregion Session Lifecycle - Run

    #region Session Lifecycle - End

    /// <inheritdoc/>
    public void End (SessionToken token)
    {
        ArgumentNullException.ThrowIfNull (token);

        if (token.Runnable is null)
        {
            return; // Already ended
        }

        // TODO: Move Poppover to utilize IRunnable arch; Get all refs to anyting
        // TODO: View-related out of ApplicationImpl.
        if (Popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            ApplicationPopover.HideWithQuitCommand (visiblePopover);
        }

        IRunnable runnable = token.Runnable;

        // Get old IsRunning value (safe - cached value)
        bool oldIsRunning = runnable.IsRunning;

        // Raise IsRunningChanging OUTSIDE lock (true -> false) - can be canceled
        // This is where Result should be extracted!
        if (runnable.RaiseIsRunningChanging (oldIsRunning, false))
        {
            // Stopping was canceled - do not proceed with End
            return;
        }

        bool wasModal = runnable.IsModal;
        IRunnable? previousRunnable = null;

        // CRITICAL SECTION - Atomic stack + cached state update
        lock (_sessionStackLock)
        {
            // Pop token from SessionStack
            if (wasModal && SessionStack?.TryPop (out SessionToken? popped) == true && popped == token)
            {
                // Restore previous top runnable
                if (SessionStack?.TryPeek (out SessionToken? previousToken) == true && previousToken?.Runnable is { })
                {
                    previousRunnable = previousToken.Runnable;

                    // Previous runnable becomes modal again
                    previousRunnable.SetIsModal (true);
                }
            }

            // Update cached state atomically - IsRunning and IsModal are now consistent
            runnable.SetIsRunning (false);
            runnable.SetIsModal (false);
        }

        // END CRITICAL SECTION - IsRunning/IsModal now thread-safe

        // Fire events AFTER lock released
        if (wasModal)
        {
            runnable.RaiseIsModalChangedEvent (false);
        }

        TopRunnable = null;

        if (previousRunnable != null)
        {
            TopRunnable = previousRunnable;
            previousRunnable.RaiseIsModalChangedEvent (true);
        }

        runnable.RaiseIsRunningChangedEvent (false);

        token.Result = runnable.Result;

        _result = token.Result;

        // Clear the Runnable from the token
        token.Runnable = null;
        SessionEnded?.Invoke (this, new (token));
    }

    #endregion Session Lifecycle - End

    #region Session Lifecycle - RequestStop

    /// <inheritdoc/>
    public void RequestStop () { RequestStop (null); }

    /// <inheritdoc/>
    public void RequestStop (IRunnable? runnable)
    {
        // Get the runnable to stop
        if (runnable is null)
        {
            // Try to get from TopRunnable
            if (TopRunnableView is IRunnable r)
            {
                runnable = r;
            }
            else
            {
                return;
            }
        }

        runnable.StopRequested = true;

        // Note: The End() method will be called from the finally block in Run()
        // and that's where IsRunningChanging/IsRunningChanged will be raised
    }

    #endregion Session Lifecycle - RequestStop
}
