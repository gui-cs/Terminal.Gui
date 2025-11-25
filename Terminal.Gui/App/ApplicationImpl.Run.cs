using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public partial class ApplicationImpl
{
    #region Begin->Run->Stop->End

    // TODO: This API is not used anywhere; it can be deleted
    /// <inheritdoc/>
    public event EventHandler<SessionTokenEventArgs>? SessionBegun;

    // TODO: This API is not used anywhere; it can be deleted
    /// <inheritdoc/>
    public event EventHandler<ToplevelEventArgs>? SessionEnded;

    /// <inheritdoc/>
    public SessionToken Begin (Toplevel toplevel)
    {
        ArgumentNullException.ThrowIfNull (toplevel);

        // Ensure the mouse is ungrabbed.
        if (Mouse.MouseGrabView is { })
        {
            Mouse.UngrabMouse ();
        }

        var rs = new SessionToken (toplevel);

#if DEBUG_IDISPOSABLE
        if (View.EnableDebugIDisposableAsserts && TopRunnable is { } && toplevel != TopRunnable && !SessionStack.Contains (TopRunnable))
        {
            // This assertion confirm if the TopRunnable was already disposed
            Debug.Assert (TopRunnable.WasDisposed);
            Debug.Assert (TopRunnable == CachedSessionTokenToplevel);
        }
#endif

        lock (SessionStack)
        {
            if (TopRunnable is { } && toplevel != TopRunnable && !SessionStack.Contains (TopRunnable))
            {
                // If TopRunnable was already disposed and isn't on the Toplevels Stack,
                // clean it up here if is the same as _CachedSessionTokenToplevel
                if (TopRunnable == CachedSessionTokenToplevel)
                {
                    TopRunnable = null;
                }
                else
                {
                    // Probably this will never hit
                    throw new ObjectDisposedException (TopRunnable.GetType ().FullName);
                }
            }

            // BUGBUG: We should not depend on `Id` internally.
            // BUGBUG: It is super unclear what this code does anyway.
            if (string.IsNullOrEmpty (toplevel.Id))
            {
                var count = 1;
                var id = (SessionStack.Count + count).ToString ();

                while (SessionStack.Count > 0 && SessionStack.FirstOrDefault (x => x.Id == id) is { })
                {
                    count++;
                    id = (SessionStack.Count + count).ToString ();
                }

                toplevel.Id = (SessionStack.Count + count).ToString ();

                SessionStack.Push (toplevel);
            }
            else
            {
                Toplevel? dup = SessionStack.FirstOrDefault (x => x.Id == toplevel.Id);

                if (dup is null)
                {
                    SessionStack.Push (toplevel);
                }
            }
        }

        if (TopRunnable is null)
        {
            toplevel.App = this;
            TopRunnable = toplevel;
        }

        if ((TopRunnable?.Modal == false && toplevel.Modal)
            || (TopRunnable?.Modal == false && !toplevel.Modal)
            || (TopRunnable?.Modal == true && toplevel.Modal))
        {
            if (toplevel.Visible)
            {
                if (TopRunnable is { HasFocus: true })
                {
                    TopRunnable.HasFocus = false;
                }

                // Force leave events for any entered views in the old TopRunnable
                if (Mouse.LastMousePosition is { })
                {
                    Mouse.RaiseMouseEnterLeaveEvents (Mouse.LastMousePosition!.Value, new ());
                }

                TopRunnable?.OnDeactivate (toplevel);
                Toplevel previousTop = TopRunnable!;

                TopRunnable = toplevel;
                TopRunnable.App = this;
                TopRunnable.OnActivate (previousTop);
            }
        }

        // View implements ISupportInitializeNotification which is derived from ISupportInitialize
        if (!toplevel.IsInitialized)
        {
            toplevel.BeginInit ();
            toplevel.EndInit (); // Calls Layout
        }

        // Try to set initial focus to any TabStop
        if (!toplevel.HasFocus)
        {
            toplevel.SetFocus ();
        }

        toplevel.OnLoaded ();

        LayoutAndDraw (true);

        if (PositionCursor ())
        {
            Driver?.UpdateCursor ();
        }

        SessionBegun?.Invoke (this, new (rs));

        return rs;
    }

    /// <inheritdoc/>
    public bool StopAfterFirstIteration { get; set; }

    /// <inheritdoc/>
    public void RaiseIteration ()
    {
        Iteration?.Invoke (null, new (this));
    }

    /// <inheritdoc/>
    public event EventHandler<EventArgs<IApplication?>>? Iteration;

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driverName = null) => Run<Toplevel> (errorHandler, driverName);

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public TView Run<TView> (Func<Exception, bool>? errorHandler = null, string? driverName = null)
        where TView : Toplevel, new ()
    {
        if (!Initialized)
        {
            // Init() has NOT been called. Auto-initialize as per interface contract.
            Init (driverName);
        }

        TView top = new ();
        Run (top, errorHandler);

        return top;
    }

    /// <inheritdoc/>
    public void Run (Toplevel view, Func<Exception, bool>? errorHandler = null)
    {
        Logging.Information ($"Run '{view}'");
        ArgumentNullException.ThrowIfNull (view);

        if (!Initialized)
        {
            throw new NotInitializedException (nameof (Run));
        }

        if (Driver == null)
        {
            throw new InvalidOperationException ("Driver was inexplicably null when trying to Run view");
        }

        TopRunnable = view;

        SessionToken rs = Begin (view);

        TopRunnable.Running = true;

        var firstIteration = true;

        while (SessionStack.TryPeek (out Toplevel? found) && found == view && view.Running)
        {
            if (Coordinator is null)
            {
                throw new ($"{nameof (IMainLoopCoordinator)} inexplicably became null during Run");
            }

            Coordinator.RunIteration ();

            if (StopAfterFirstIteration && firstIteration)
            {
                Logging.Information ("Run - Stopping after first iteration as requested");
                RequestStop ((Toplevel?)view);
            }

            firstIteration = false;
        }

        Logging.Information ("Run - Calling End");
        End (rs);
    }

    /// <inheritdoc/>
    public void End (SessionToken sessionToken)
    {
        ArgumentNullException.ThrowIfNull (sessionToken);

        if (Popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            ApplicationPopover.HideWithQuitCommand (visiblePopover);
        }

        sessionToken.Toplevel?.OnUnloaded ();

        // End the Session
        // First, take it off the Toplevel Stack
        if (SessionStack.TryPop (out Toplevel? topOfStack))
        {
            if (topOfStack != sessionToken.Toplevel)
            {
                // If the top of the stack is not the SessionToken.Toplevel then
                // this call to End is not balanced with the call to Begin that started the Session
                throw new ArgumentException ("End must be balanced with calls to Begin");
            }
        }

        // Notify that it is closing
        sessionToken.Toplevel?.OnClosed (sessionToken.Toplevel);

        if (SessionStack.TryPeek (out Toplevel? newTop))
        {
            newTop.App = this;
            TopRunnable = newTop;
            TopRunnable?.SetNeedsDraw ();
        }

        if (sessionToken.Toplevel is { HasFocus: true })
        {
            sessionToken.Toplevel.HasFocus = false;
        }

        if (TopRunnable is { HasFocus: false })
        {
            TopRunnable.SetFocus ();
        }

        CachedSessionTokenToplevel = sessionToken.Toplevel;

        sessionToken.Toplevel = null;
        sessionToken.Dispose ();

        // BUGBUG: Why layout and draw here? This causes the screen to be cleared!
        //LayoutAndDraw (true);

        // TODO: This API is not used (correctly) anywhere; it can be deleted
        // TODO: Instead, callers should use the new equivalent of Toplevel.Ready 
        // TODO: which will be IsRunningChanged with newIsRunning == true
        SessionEnded?.Invoke (this, new (CachedSessionTokenToplevel));
    }

    /// <inheritdoc/>
    public void RequestStop () { RequestStop ((Toplevel?)null); }

    /// <inheritdoc/>
    public void RequestStop (Toplevel? top)
    {
        Logging.Trace ($"TopRunnable: '{(top is { } ? top : "null")}'");

        top ??= TopRunnable;

        if (top == null)
        {
            return;
        }

        ToplevelClosingEventArgs ev = new (top);
        top.OnClosing (ev);

        if (ev.Cancel)
        {
            return;
        }

        top.Running = false;
    }

    #endregion Begin->Run->Stop->End

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
        if (TopRunnable is { Running: true } && MainThreadId == Thread.CurrentThread.ManagedThreadId)
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
        if (TopRunnable is { Running: true } && MainThreadId == Thread.CurrentThread.ManagedThreadId)
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

    #region IRunnable Support

    /// <inheritdoc/>
    public RunnableSessionToken Begin (IRunnable runnable)
    {
        ArgumentNullException.ThrowIfNull (runnable);

        // Ensure the mouse is ungrabbed
        if (Mouse.MouseGrabView is { })
        {
            Mouse.UngrabMouse ();
        }

        // Create session token
        RunnableSessionToken token = new (runnable);

        // Set the App property if the runnable is a View (needed for IsRunning/IsModal checks)
        if (runnable is View runnableView)
        {
            runnableView.App = this;
        }

        // Get old IsRunning and IsModal values BEFORE any stack changes
        bool oldIsRunning = runnable.IsRunning;
        bool oldIsModalValue = runnable.IsModal;

        // Raise IsRunningChanging (false -> true) - can be canceled
        if (runnable.RaiseIsRunningChanging (oldIsRunning, true))
        {
            // Starting was canceled
            return token;
        }

        // Push token onto RunnableSessionStack (IsRunning becomes true)
        RunnableSessionStack?.Push (token);

        // Update TopRunnable to the new top of stack
        IRunnable? previousTop = null;

        // In Phase 1, Toplevel doesn't implement IRunnable yet
        // In Phase 2, it will, and this will work properly
        if (TopRunnable is IRunnable r)
        {
            previousTop = r;
        }

        // Set TopRunnable (handles both Toplevel and IRunnable)
        if (runnable is Toplevel tl)
        {
            TopRunnable = tl;
        }
        else if (runnable is View v)
        {
            // For now, we can't set a non-Toplevel View as TopRunnable
            // This is a limitation of the current architecture
            // In Phase 2, we'll make TopRunnable an IRunnable property
            Logging.Warning ($"WIP on Issue #4148 - Runnable '{runnable}' is a View but not a Toplevel; cannot set as TopRunnable");
        }

        // Raise IsRunningChanged (now true)
        runnable.RaiseIsRunningChangedEvent (true);

        // If there was a previous top, it's no longer modal
        if (previousTop != null)
        {
            // Get old IsModal value (should be true before becoming non-modal)
            bool oldIsModal = previousTop.IsModal;

            // Raise IsModalChanging (true -> false)
            previousTop.RaiseIsModalChanging (oldIsModal, false);

            // IsModal is now false (derived property)
            previousTop.RaiseIsModalChangedEvent (false);
        }

        // New runnable becomes modal
        // Raise IsModalChanging (false -> true) using the old value we captured earlier
        runnable.RaiseIsModalChanging (oldIsModalValue, true);

        // IsModal is now true (derived property)
        runnable.RaiseIsModalChangedEvent (true);

        // Initialize if needed
        if (runnable is View view && !view.IsInitialized)
        {
            view.BeginInit ();
            view.EndInit ();

            // Initialized event is raised by View.EndInit()
        }

        // Initial Layout and draw
        LayoutAndDraw (true);

        // Set focus
        if (runnable is View viewToFocus && !viewToFocus.HasFocus)
        {
            viewToFocus.SetFocus ();
        }

        if (PositionCursor ())
        {
            Driver?.UpdateCursor ();
        }

        return token;
    }

    /// <inheritdoc/>
    public void Run (IRunnable runnable, Func<Exception, bool>? errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull (runnable);

        if (!Initialized)
        {
            throw new NotInitializedException (nameof (Run));
        }

        // Begin the session (adds to stack, raises IsRunningChanging/IsRunningChanged)
        RunnableSessionToken token = Begin (runnable);

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
    }

    /// <inheritdoc/>
    public IApplication Run<TRunnable> (Func<Exception, bool>? errorHandler = null) where TRunnable : IRunnable, new ()
    {
        if (!Initialized)
        {
            throw new NotInitializedException (nameof (Run));
        }

        TRunnable runnable = new ();
        
        // Store the runnable for automatic disposal by Shutdown
        FrameworkOwnedRunnable = runnable;
        
        Run (runnable, errorHandler);

        return this;
    }

    private void RunLoop (IRunnable runnable, Func<Exception, bool>? errorHandler)
    {
        // Main loop - blocks until RequestStop() is called
        // Note: IsRunning is a derived property (stack.Contains), so we check it each iteration
        var firstIteration = true;

        while (runnable.IsRunning)
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

    /// <inheritdoc/>
    public void End (RunnableSessionToken token)
    {
        ArgumentNullException.ThrowIfNull (token);

        if (token.Runnable is null)
        {
            return; // Already ended
        }

        IRunnable runnable = token.Runnable;

        // Get old IsRunning value (should be true before stopping)
        bool oldIsRunning = runnable.IsRunning;

        // Raise IsRunningChanging (true -> false) - can be canceled
        // This is where Result should be extracted!
        if (runnable.RaiseIsRunningChanging (oldIsRunning, false))
        {
            // Stopping was canceled
            return;
        }

        // Current runnable is no longer modal
        // Get old IsModal value (should be true before becoming non-modal)
        bool oldIsModal = runnable.IsModal;

        // Raise IsModalChanging (true -> false)
        runnable.RaiseIsModalChanging (oldIsModal, false);

        // IsModal is now false (will be false after pop)
        runnable.RaiseIsModalChangedEvent (false);

        // Pop token from RunnableSessionStack (IsRunning becomes false)
        if (RunnableSessionStack?.TryPop (out RunnableSessionToken? popped) == true && popped == token)
        {
            // Restore previous top runnable
            if (RunnableSessionStack?.TryPeek (out RunnableSessionToken? previousToken) == true && previousToken?.Runnable is { })
            {
                IRunnable? previousRunnable = previousToken.Runnable;

                // Update TopRunnable if it's a Toplevel
                if (previousRunnable is Toplevel tl)
                {
                    TopRunnable = tl;
                }

                // Previous runnable becomes modal again
                // Get old IsModal value (should be false before becoming modal again)
                bool oldIsModalValue = previousRunnable.IsModal;

                // Raise IsModalChanging (false -> true)
                previousRunnable.RaiseIsModalChanging (oldIsModalValue, true);

                // IsModal is now true (derived property)
                previousRunnable.RaiseIsModalChangedEvent (true);
            }
            else
            {
                // No more runnables, clear TopRunnable
                if (TopRunnable is IRunnable)
                {
                    TopRunnable = null;
                }
            }
        }

        // Raise IsRunningChanged (now false)
        runnable.RaiseIsRunningChangedEvent (false);

        // Set focus to new TopRunnable if exists
        if (TopRunnable is View viewToFocus && !viewToFocus.HasFocus)
        {
            viewToFocus.SetFocus ();
        }

        // Clear the token
        token.Runnable = null;
    }

    /// <inheritdoc/>
    public void RequestStop (IRunnable? runnable)
    {
        // Get the runnable to stop
        if (runnable is null)
        {
            // Try to get from TopRunnable
            if (TopRunnable is IRunnable r)
            {
                runnable = r;
            }
            else
            {
                return;
            }
        }

        // For Toplevel, use the existing mechanism
        if (runnable is Toplevel toplevel)
        {
            RequestStop (toplevel);
        }

        // Note: The End() method will be called from the finally block in Run()
        // and that's where IsRunningChanging/IsRunningChanged will be raised
    }

    #endregion IRunnable Support
}
