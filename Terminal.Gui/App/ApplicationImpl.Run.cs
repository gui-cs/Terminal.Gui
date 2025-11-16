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

    /// <inheritdoc/>
    public event EventHandler<SessionTokenEventArgs>? SessionBegun;

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
        if (View.EnableDebugIDisposableAsserts && Current is { } && toplevel != Current && !SessionStack.Contains (Current))
        {
            // This assertion confirm if the Current was already disposed
            Debug.Assert (Current.WasDisposed);
            Debug.Assert (Current == CachedSessionTokenToplevel);
        }
#endif

        lock (SessionStack)
        {
            if (Current is { } && toplevel != Current && !SessionStack.Contains (Current))
            {
                // If Current was already disposed and isn't on the Toplevels Stack,
                // clean it up here if is the same as _CachedSessionTokenToplevel
                if (Current == CachedSessionTokenToplevel)
                {
                    Current = null;
                }
                else
                {
                    // Probably this will never hit
                    throw new ObjectDisposedException (Current.GetType ().FullName);
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

        if (Current is null)
        {
            Current = toplevel;
        }

        if ((Current?.Modal == false && toplevel.Modal)
            || (Current?.Modal == false && !toplevel.Modal)
            || (Current?.Modal == true && toplevel.Modal))
        {
            if (toplevel.Visible)
            {
                if (Current is { HasFocus: true })
                {
                    Current.HasFocus = false;
                }

                // Force leave events for any entered views in the old Current
                if (Mouse.GetLastMousePosition () is { })
                {
                    Mouse.RaiseMouseEnterLeaveEvents (Mouse.GetLastMousePosition ()!.Value, new ());
                }

                Current?.OnDeactivate (toplevel);
                Toplevel previousTop = Current!;

                Current = toplevel;
                Current.OnActivate (previousTop);
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

        Instance.LayoutAndDraw (true);

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
    public event EventHandler<IterationEventArgs>? Iteration;

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public Toplevel Run (Func<Exception, bool>? errorHandler = null, string? driver = null) { return Run<Toplevel> (errorHandler, driver); }

    /// <inheritdoc/>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public TView Run<TView> (Func<Exception, bool>? errorHandler = null, string? driver = null)
        where TView : Toplevel, new ()
    {
        if (!Initialized)
        {
            // Init() has NOT been called. Auto-initialize as per interface contract.
            Init (null, driver);
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

        Current = view;

        SessionToken rs = Application.Begin (view);

        Current.Running = true;

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
        Application.End (rs);
    }

    /// <inheritdoc/>
    public void End (SessionToken sessionToken)
    {
        ArgumentNullException.ThrowIfNull (sessionToken);

        if (Popover?.GetActivePopover () as View is { Visible: true } visiblePopover)
        {
            ApplicationPopover.HideWithQuitCommand (visiblePopover);
        }

        sessionToken.Toplevel.OnUnloaded ();

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
            Current = newTop;
            Current?.SetNeedsDraw ();
        }

        if (sessionToken.Toplevel is { HasFocus: true })
        {
            sessionToken.Toplevel.HasFocus = false;
        }

        if (Current is { HasFocus: false })
        {
            Current.SetFocus ();
        }

        CachedSessionTokenToplevel = sessionToken.Toplevel;

        sessionToken.Toplevel = null;
        sessionToken.Dispose ();

        // BUGBUG: Why layout and draw here? This causes the screen to be cleared!
        //LayoutAndDraw (true);

        SessionEnded?.Invoke (this, new (CachedSessionTokenToplevel));
    }

    /// <inheritdoc/>
    public void RequestStop () { RequestStop (null); }

    /// <inheritdoc/>
    public void RequestStop (Toplevel? top)
    {
        Logging.Trace ($"Current: '{(top is { } ? top : "null")}'");

        top ??= Current;

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

    /// <inheritdoc/>
    public void RaiseIteration () { Iteration?.Invoke (null, new ()); }

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
    public void Invoke (Action action)
    {
        // If we are already on the main UI thread
        if (Current is { Running: true } && MainThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            action ();

            return;
        }

        _timedEvents.Add (
                          TimeSpan.Zero,
                          () =>
                          {
                              action ();

                              return false;
                          }
                         );
    }

    #endregion Timeouts and Invoke
}
