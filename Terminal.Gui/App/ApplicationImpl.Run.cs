using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.App;

public partial class ApplicationImpl
{
    /// <summary>
    ///     INTERNAL: Gets or sets the managed thread ID of the application's main UI thread, which is set during
    ///     <see cref="Init"/> and used to determine if code is executing on the main thread.
    /// </summary>
    /// <value>
    ///     The managed thread ID of the main UI thread, or <see langword="null"/> if the application is not initialized.
    /// </value>
    internal int? MainThreadId { get; set; }

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
        Iteration?.Invoke (null, new ());
    }

    /// <inheritdoc/>
    public event EventHandler<IterationEventArgs>? Iteration;

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driverName = null) { return Run<Toplevel> (errorHandler, driverName); }

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
                view.RequestStop ();
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
    public void RequestStop () { RequestStop (null); }

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
    public object AddTimeout (TimeSpan time, Func<bool> callback) { return _timedEvents.Add (time, callback); }

    /// <inheritdoc/>
    public bool RemoveTimeout (object token) { return _timedEvents.Remove (token); }

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
}
