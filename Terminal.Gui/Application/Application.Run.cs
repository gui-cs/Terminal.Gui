#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui;

public static partial class Application // Run (Begin, Run, End, Stop)
{
    // When `End ()` is called, it is possible `RunState.Toplevel` is a different object than `Top`.
    // This variable is set in `End` in this case so that `Begin` correctly sets `Top`.
    private static Toplevel? _cachedRunStateToplevel;

    /// <summary>
    ///     Notify that a new <see cref="RunState"/> was created (<see cref="Begin(Toplevel)"/> was called). The token is
    ///     created in <see cref="Begin(Toplevel)"/> and this event will be fired before that function exits.
    /// </summary>
    /// <remarks>
    ///     If <see cref="EndAfterFirstIteration"/> is <see langword="true"/> callers to <see cref="Begin(Toplevel)"/>
    ///     must also subscribe to <see cref="NotifyStopRunState"/> and manually dispose of the <see cref="RunState"/> token
    ///     when the application is done.
    /// </remarks>
    public static event EventHandler<RunStateEventArgs>? NotifyNewRunState;

    /// <summary>Notify that an existent <see cref="RunState"/> is stopping (<see cref="End(RunState)"/> was called).</summary>
    /// <remarks>
    ///     If <see cref="EndAfterFirstIteration"/> is <see langword="true"/> callers to <see cref="Begin(Toplevel)"/>
    ///     must also subscribe to <see cref="NotifyStopRunState"/> and manually dispose of the <see cref="RunState"/> token
    ///     when the application is done.
    /// </remarks>
    public static event EventHandler<ToplevelEventArgs>? NotifyStopRunState;

    /// <summary>Building block API: Prepares the provided <see cref="Toplevel"/> for execution.</summary>
    /// <returns>
    ///     The <see cref="RunState"/> handle that needs to be passed to the <see cref="End(RunState)"/> method upon
    ///     completion.
    /// </returns>
    /// <param name="toplevel">The <see cref="Toplevel"/> to prepare execution for.</param>
    /// <remarks>
    ///     This method prepares the provided <see cref="Toplevel"/> for running with the focus, it adds this to the list
    ///     of <see cref="Toplevel"/>s, lays out the Subviews, focuses the first element, and draws the <see cref="Toplevel"/>
    ///     in the screen. This is usually followed by executing the <see cref="RunLoop"/> method, and then the
    ///     <see cref="End(RunState)"/> method upon termination which will undo these changes.
    /// </remarks>
    public static RunState Begin (Toplevel toplevel)
    {
        ArgumentNullException.ThrowIfNull (toplevel);

#if DEBUG_IDISPOSABLE
        Debug.Assert (!toplevel.WasDisposed);

        if (_cachedRunStateToplevel is { } && _cachedRunStateToplevel != toplevel)
        {
            Debug.Assert (_cachedRunStateToplevel.WasDisposed);
        }
#endif

        if (toplevel.IsOverlappedContainer && ApplicationOverlapped.OverlappedTop != toplevel && ApplicationOverlapped.OverlappedTop is { })
        {
            throw new InvalidOperationException ("Only one Overlapped Container is allowed.");
        }

        // Ensure the mouse is ungrabbed.
        MouseGrabView = null;

        var rs = new RunState (toplevel);

        // View implements ISupportInitializeNotification which is derived from ISupportInitialize
        if (!toplevel.IsInitialized)
        {
            toplevel.BeginInit ();
            toplevel.EndInit ();
        }

#if DEBUG_IDISPOSABLE
        if (Top is { } && toplevel != Top && !TopLevels.Contains (Top))
        {
            // This assertion confirm if the Top was already disposed
            Debug.Assert (Top.WasDisposed);
            Debug.Assert (Top == _cachedRunStateToplevel);
        }
#endif

        lock (TopLevels)
        {
            if (Top is { } && toplevel != Top && !TopLevels.Contains (Top))
            {
                // If Top was already disposed and isn't on the Toplevels Stack,
                // clean it up here if is the same as _cachedRunStateToplevel
                if (Top == _cachedRunStateToplevel)
                {
                    Top = null;
                }
                else
                {
                    // Probably this will never hit
                    throw new ObjectDisposedException (Top.GetType ().FullName);
                }
            }
            else if (ApplicationOverlapped.OverlappedTop is { } && toplevel != Top && TopLevels.Contains (Top!))
            {
                // BUGBUG: Don't call OnLeave/OnEnter directly! Set HasFocus to false and let the system handle it.
                Top!.OnLeave (toplevel);
            }

            // BUGBUG: We should not depend on `Id` internally.
            // BUGBUG: It is super unclear what this code does anyway.
            if (string.IsNullOrEmpty (toplevel.Id))
            {
                var count = 1;
                var id = (TopLevels.Count + count).ToString ();

                while (TopLevels.Count > 0 && TopLevels.FirstOrDefault (x => x.Id == id) is { })
                {
                    count++;
                    id = (TopLevels.Count + count).ToString ();
                }

                toplevel.Id = (TopLevels.Count + count).ToString ();

                TopLevels.Push (toplevel);
            }
            else
            {
                Toplevel? dup = TopLevels.FirstOrDefault (x => x.Id == toplevel.Id);

                if (dup is null)
                {
                    TopLevels.Push (toplevel);
                }
            }

            if (TopLevels.FindDuplicates (new ToplevelEqualityComparer ()).Count > 0)
            {
                throw new ArgumentException ("There are duplicates Toplevel IDs");
            }
        }

        if (Top is null || toplevel.IsOverlappedContainer)
        {
            Top = toplevel;
        }

        var refreshDriver = true;

        if (ApplicationOverlapped.OverlappedTop is null
            || toplevel.IsOverlappedContainer
            || (Current?.Modal == false && toplevel.Modal)
            || (Current?.Modal == false && !toplevel.Modal)
            || (Current?.Modal == true && toplevel.Modal))
        {
            if (toplevel.Visible)
            {
                if (Current is { HasFocus: true })
                {
                    Current.HasFocus = false;
                }

                Current?.OnDeactivate (toplevel);
                Toplevel previousCurrent = Current!;

                Current = toplevel;
                Current.OnActivate (previousCurrent);

                ApplicationOverlapped.SetCurrentOverlappedAsTop ();
            }
            else
            {
                refreshDriver = false;
            }
        }
        else if ((toplevel != ApplicationOverlapped.OverlappedTop
                  && Current?.Modal == true
                  && !TopLevels.Peek ().Modal)
                 || (toplevel != ApplicationOverlapped.OverlappedTop && Current?.Running == false))
        {
            refreshDriver = false;
            ApplicationOverlapped.MoveCurrent (toplevel);
        }
        else
        {
            refreshDriver = false;
            ApplicationOverlapped.MoveCurrent (Current!);
        }

        toplevel.SetRelativeLayout (Driver!.Screen.Size);

        toplevel.LayoutSubviews ();
        toplevel.PositionToplevels ();
        toplevel.FocusFirst (null);
        ApplicationOverlapped.BringOverlappedTopToFront ();

        if (refreshDriver)
        {
            ApplicationOverlapped.OverlappedTop?.OnChildLoaded (toplevel);
            toplevel.OnLoaded ();
            toplevel.SetNeedsDisplay ();
            toplevel.Draw ();
            Driver.UpdateScreen ();

            if (PositionCursor (toplevel))
            {
                Driver.UpdateCursor ();
            }
        }

        NotifyNewRunState?.Invoke (toplevel, new (rs));

        return rs;
    }

    /// <summary>
    ///     Calls <see cref="View.PositionCursor"/> on the most focused view in the view starting with <paramref name="view"/>.
    /// </summary>
    /// <remarks>
    ///     Does nothing if <paramref name="view"/> is <see langword="null"/> or if the most focused view is not visible or
    ///     enabled.
    ///     <para>
    ///         If the most focused view is not visible within it's superview, the cursor will be hidden.
    ///     </para>
    /// </remarks>
    /// <returns><see langword="true"/> if a view positioned the cursor and the position is visible.</returns>
    internal static bool PositionCursor (View view)
    {
        // Find the most focused view and position the cursor there.
        View? mostFocused = view?.MostFocused;

        if (mostFocused is null)
        {
            if (view is { HasFocus: true })
            {
                mostFocused = view;
            }
            else
            {
                return false;
            }
        }

        // If the view is not visible or enabled, don't position the cursor
        if (!mostFocused.Visible || !mostFocused.Enabled)
        {
            Driver!.GetCursorVisibility (out CursorVisibility current);

            if (current != CursorVisibility.Invisible)
            {
                Driver.SetCursorVisibility (CursorVisibility.Invisible);
            }

            return false;
        }

        // If the view is not visible within it's superview, don't position the cursor
        Rectangle mostFocusedViewport = mostFocused.ViewportToScreen (mostFocused.Viewport with { Location = Point.Empty });
        Rectangle superViewViewport = mostFocused.SuperView?.ViewportToScreen (mostFocused.SuperView.Viewport with { Location = Point.Empty }) ?? Driver!.Screen;

        if (!superViewViewport.IntersectsWith (mostFocusedViewport))
        {
            return false;
        }

        Point? cursor = mostFocused.PositionCursor ();

        Driver!.GetCursorVisibility (out CursorVisibility currentCursorVisibility);

        if (cursor is { })
        {
            // Convert cursor to screen coords
            cursor = mostFocused.ViewportToScreen (mostFocused.Viewport with { Location = cursor.Value }).Location;

            // If the cursor is not in a visible location in the SuperView, hide it
            if (!superViewViewport.Contains (cursor.Value))
            {
                if (currentCursorVisibility != CursorVisibility.Invisible)
                {
                    Driver.SetCursorVisibility (CursorVisibility.Invisible);
                }

                return false;
            }

            // Show it
            if (currentCursorVisibility == CursorVisibility.Invisible)
            {
                Driver.SetCursorVisibility (mostFocused.CursorVisibility);
            }

            return true;
        }

        if (currentCursorVisibility != CursorVisibility.Invisible)
        {
            Driver.SetCursorVisibility (CursorVisibility.Invisible);
        }

        return false;
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
    public static Toplevel Run (Func<Exception, bool>? errorHandler = null, ConsoleDriver? driver = null) { return Run<Toplevel> (errorHandler, driver); }

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
    ///     The <see cref="ConsoleDriver"/> to use. If not specified the default driver for the platform will
    ///     be used ( <see cref="WindowsDriver"/>, <see cref="CursesDriver"/>, or <see cref="NetDriver"/>). Must be
    ///     <see langword="null"/> if <see cref="Init"/> has already been called.
    /// </param>
    /// <returns>The created T object. The caller is responsible for disposing this object.</returns>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public static T Run<T> (Func<Exception, bool>? errorHandler = null, ConsoleDriver? driver = null)
        where T : Toplevel, new()
    {
        if (!IsInitialized)
        {
            // Init() has NOT been called.
            InternalInit (driver, null, true);
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
    ///         To make a <see cref="Run(Terminal.Gui.Toplevel,System.Func{System.Exception,bool})"/> stop execution, call
    ///         <see cref="Application.RequestStop"/>.
    ///     </para>
    ///     <para>
    ///         Calling <see cref="Run(Terminal.Gui.Toplevel,System.Func{System.Exception,bool})"/> is equivalent to calling
    ///         <see cref="Begin(Toplevel)"/>, followed by <see cref="RunLoop(RunState)"/>, and then calling
    ///         <see cref="End(RunState)"/>.
    ///     </para>
    ///     <para>
    ///         Alternatively, to have a program control the main loop and process events manually, call
    ///         <see cref="Begin(Toplevel)"/> to set things up manually and then repeatedly call
    ///         <see cref="RunLoop(RunState)"/> with the wait parameter set to false. By doing this the
    ///         <see cref="RunLoop(RunState)"/> method will only process any pending events, timers, idle handlers and then
    ///         return control immediately.
    ///     </para>
    ///     <para>When using <see cref="Run{T}"/> or
    ///         <see cref="Run(System.Func{System.Exception,bool},Terminal.Gui.ConsoleDriver)"/>
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
    public static void Run (Toplevel view, Func<Exception, bool>? errorHandler = null)
    {
        ArgumentNullException.ThrowIfNull (view);

        if (IsInitialized)
        {
            if (Driver is null)
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
            RunState runState = Begin (view);

            // If EndAfterFirstIteration is true then the user must dispose of the runToken
            // by using NotifyStopRunState event.
            RunLoop (runState);

            if (runState.Toplevel is null)
            {
#if DEBUG_IDISPOSABLE
                Debug.Assert (TopLevels.Count == 0);
#endif
                runState.Dispose ();

                return;
            }

            if (!EndAfterFirstIteration)
            {
                End (runState);
            }
#if !DEBUG
            }
            catch (Exception error)
            {
                if (errorHandler is null)
                {
                    throw;
                }

                resume = errorHandler (error);
            }
#endif
        }
    }

    /// <summary>Adds a timeout to the application.</summary>
    /// <remarks>
    ///     When time specified passes, the callback will be invoked. If the callback returns true, the timeout will be
    ///     reset, repeating the invocation. If it returns false, the timeout will stop and be removed. The returned value is a
    ///     token that can be used to stop the timeout by calling <see cref="RemoveTimeout(object)"/>.
    /// </remarks>
    public static object AddTimeout (TimeSpan time, Func<bool> callback) { return MainLoop!.AddTimeout (time, callback); }

    /// <summary>Removes a previously scheduled timeout</summary>
    /// <remarks>The token parameter is the value returned by <see cref="AddTimeout"/>.</remarks>
    /// Returns
    /// <c>true</c>
    /// if the timeout is successfully removed; otherwise,
    /// <c>false</c>
    /// .
    /// This method also returns
    /// <c>false</c>
    /// if the timeout is not found.
    public static bool RemoveTimeout (object token) { return MainLoop?.RemoveTimeout (token) ?? false; }

    /// <summary>Runs <paramref name="action"/> on the thread that is processing events</summary>
    /// <param name="action">the action to be invoked on the main processing thread.</param>
    public static void Invoke (Action action)
    {
        MainLoop?.AddIdle (
                           () =>
                           {
                               action ();

                               return false;
                           }
                          );
    }

    // TODO: Determine if this is really needed. The only code that calls WakeUp I can find
    // is ProgressBarStyles, and it's not clear it needs to.

    /// <summary>Wakes up the running application that might be waiting on input.</summary>
    public static void Wakeup () { MainLoop?.Wakeup (); }

    /// <summary>Triggers a refresh of the entire display.</summary>
    public static void Refresh ()
    {
        // TODO: Figure out how to remove this call to ClearContents. Refresh should just repaint damaged areas, not clear
        Driver!.ClearContents ();

        foreach (Toplevel v in TopLevels.Reverse ())
        {
            if (v.Visible)
            {
                v.SetNeedsDisplay ();
                v.SetSubViewNeedsDisplay ();
                v.Draw ();
            }
        }

        Driver.Refresh ();
    }

    /// <summary>This event is raised on each iteration of the main loop.</summary>
    /// <remarks>See also <see cref="Timeout"/></remarks>
    public static event EventHandler<IterationEventArgs>? Iteration;

    /// <summary>The <see cref="MainLoop"/> driver for the application</summary>
    /// <value>The main loop.</value>
    internal static MainLoop? MainLoop { get; private set; }

    /// <summary>
    ///     Set to true to cause <see cref="End"/> to be called after the first iteration. Set to false (the default) to
    ///     cause the application to continue running until Application.RequestStop () is called.
    /// </summary>
    public static bool EndAfterFirstIteration { get; set; }

    /// <summary>Building block API: Runs the main loop for the created <see cref="Toplevel"/>.</summary>
    /// <param name="state">The state returned by the <see cref="Begin(Toplevel)"/> method.</param>
    public static void RunLoop (RunState state)
    {
        ArgumentNullException.ThrowIfNull (state);
        ObjectDisposedException.ThrowIf (state.Toplevel is null, "state");

        var firstIteration = true;

        for (state.Toplevel.Running = true; state.Toplevel?.Running == true;)
        {
            MainLoop!.Running = true;

            if (EndAfterFirstIteration && !firstIteration)
            {
                return;
            }

            RunIteration (ref state, ref firstIteration);
        }

        MainLoop!.Running = false;

        // Run one last iteration to consume any outstanding input events from Driver
        // This is important for remaining OnKeyUp events.
        RunIteration (ref state, ref firstIteration);
    }

    /// <summary>Run one application iteration.</summary>
    /// <param name="state">The state returned by <see cref="Begin(Toplevel)"/>.</param>
    /// <param name="firstIteration">
    ///     Set to <see langword="true"/> if this is the first run loop iteration. Upon return, it
    ///     will be set to <see langword="false"/> if at least one iteration happened.
    /// </param>
    public static void RunIteration (ref RunState state, ref bool firstIteration)
    {
        if (MainLoop!.Running && MainLoop.EventsPending ())
        {
            // Notify Toplevel it's ready
            if (firstIteration)
            {
                state.Toplevel.OnReady ();
            }

            MainLoop.RunIteration ();
            Iteration?.Invoke (null, new ());
            EnsureModalOrVisibleAlwaysOnTop (state.Toplevel);

            // TODO: Overlapped - Move elsewhere
            if (state.Toplevel != Current)
            {
                ApplicationOverlapped.OverlappedTop?.OnDeactivate (state.Toplevel);
                state.Toplevel = Current;
                ApplicationOverlapped.OverlappedTop?.OnActivate (state.Toplevel!);
                Top!.SetSubViewNeedsDisplay ();
                Refresh ();
            }
        }

        firstIteration = false;

        if (Current == null)
        {
            return;
        }

        if (state.Toplevel != Top && (Top!.NeedsDisplay || Top.SubViewNeedsDisplay || Top.LayoutNeeded))
        {
            state.Toplevel!.SetNeedsDisplay (state.Toplevel.Frame);
            Top.Draw ();

            foreach (Toplevel top in TopLevels.Reverse ())
            {
                if (top != Top && top != state.Toplevel)
                {
                    top.SetNeedsDisplay ();
                    top.SetSubViewNeedsDisplay ();
                    top.Draw ();
                }
            }
        }

        if (TopLevels.Count == 1
            && state.Toplevel == Top
            && (Driver!.Cols != state.Toplevel!.Frame.Width
                || Driver!.Rows != state.Toplevel.Frame.Height)
            && (state.Toplevel.NeedsDisplay
                || state.Toplevel.SubViewNeedsDisplay
                || state.Toplevel.LayoutNeeded))
        {
            Driver.ClearContents ();
        }

        if (state.Toplevel!.NeedsDisplay || state.Toplevel.SubViewNeedsDisplay || state.Toplevel.LayoutNeeded || ApplicationOverlapped.OverlappedChildNeedsDisplay ())
        {
            state.Toplevel.SetNeedsDisplay ();
            state.Toplevel.Draw ();
            Driver!.UpdateScreen ();

            //Driver.UpdateCursor ();
        }

        if (PositionCursor (state.Toplevel))
        {
            Driver!.UpdateCursor ();
        }

        //        else
        {
            //if (PositionCursor (state.Toplevel))
            //{
            //    Driver.Refresh ();
            //}
            //Driver.UpdateCursor ();
        }

        if (state.Toplevel != Top && !state.Toplevel.Modal && (Top!.NeedsDisplay || Top.SubViewNeedsDisplay || Top.LayoutNeeded))
        {
            Top.Draw ();
        }
    }

    /// <summary>Stops the provided <see cref="Toplevel"/>, causing or the <paramref name="top"/> if provided.</summary>
    /// <param name="top">The <see cref="Toplevel"/> to stop.</param>
    /// <remarks>
    ///     <para>This will cause <see cref="Application.Run(Toplevel, Func{Exception, bool})"/> to return.</para>
    ///     <para>
    ///         Calling <see cref="RequestStop(Terminal.Gui.Toplevel)"/> is equivalent to setting the <see cref="Toplevel.Running"/>
    ///         property on the currently running <see cref="Toplevel"/> to false.
    ///     </para>
    /// </remarks>
    public static void RequestStop (Toplevel? top = null)
    {
        if (ApplicationOverlapped.OverlappedTop is null || top is null)
        {
            top = Current;
        }

        if (ApplicationOverlapped.OverlappedTop != null
            && top!.IsOverlappedContainer
            && top?.Running == true
            && (Current?.Modal == false || Current is { Modal: true, Running: false }))
        {
            ApplicationOverlapped.OverlappedTop.RequestStop ();
        }
        else if (ApplicationOverlapped.OverlappedTop != null
                 && top != Current
                 && Current is { Running: true, Modal: true }
                 && top!.Modal
                 && top.Running)
        {
            var ev = new ToplevelClosingEventArgs (Current);
            Current.OnClosing (ev);

            if (ev.Cancel)
            {
                return;
            }

            ev = new (top);
            top.OnClosing (ev);

            if (ev.Cancel)
            {
                return;
            }

            Current.Running = false;
            OnNotifyStopRunState (Current);
            top.Running = false;
            OnNotifyStopRunState (top);
        }
        else if ((ApplicationOverlapped.OverlappedTop != null
                  && top != ApplicationOverlapped.OverlappedTop
                  && top != Current
                  && Current is { Modal: false, Running: true }
                  && !top!.Running)
                 || (ApplicationOverlapped.OverlappedTop != null
                     && top != ApplicationOverlapped.OverlappedTop
                     && top != Current
                     && Current is { Modal: false, Running: false }
                     && !top!.Running
                     && TopLevels.ToArray () [1].Running))
        {
            ApplicationOverlapped.MoveCurrent (top);
        }
        else if (ApplicationOverlapped.OverlappedTop != null
                 && Current != top
                 && Current?.Running == true
                 && !top!.Running
                 && Current?.Modal == true
                 && top.Modal)
        {
            // The Current and the top are both modal so needed to set the Current.Running to false too.
            Current.Running = false;
            OnNotifyStopRunState (Current);
        }
        else if (ApplicationOverlapped.OverlappedTop != null
                 && Current == top
                 && ApplicationOverlapped.OverlappedTop?.Running == true
                 && Current?.Running == true
                 && top!.Running
                 && Current?.Modal == true
                 && top!.Modal)
        {
            // The OverlappedTop was requested to stop inside a modal Toplevel which is the Current and top,
            // both are the same, so needed to set the Current.Running to false too.
            Current.Running = false;
            OnNotifyStopRunState (Current);
        }
        else
        {
            Toplevel currentTop;

            if (top == Current || (Current?.Modal == true && !top!.Modal))
            {
                currentTop = Current!;
            }
            else
            {
                currentTop = top!;
            }

            if (!currentTop.Running)
            {
                return;
            }

            var ev = new ToplevelClosingEventArgs (currentTop);
            currentTop.OnClosing (ev);

            if (ev.Cancel)
            {
                return;
            }

            currentTop.Running = false;
            OnNotifyStopRunState (currentTop);
        }
    }

    private static void OnNotifyStopRunState (Toplevel top)
    {
        if (EndAfterFirstIteration)
        {
            NotifyStopRunState?.Invoke (top, new (top));
        }
    }

    /// <summary>
    ///     Building block API: completes the execution of a <see cref="Toplevel"/> that was started with
    ///     <see cref="Begin(Toplevel)"/> .
    /// </summary>
    /// <param name="runState">The <see cref="RunState"/> returned by the <see cref="Begin(Toplevel)"/> method.</param>
    public static void End (RunState runState)
    {
        ArgumentNullException.ThrowIfNull (runState);

        if (ApplicationOverlapped.OverlappedTop is { })
        {
            ApplicationOverlapped.OverlappedTop.OnChildUnloaded (runState.Toplevel);
        }
        else
        {
            runState.Toplevel.OnUnloaded ();
        }

        // End the RunState.Toplevel
        // First, take it off the Toplevel Stack
        if (TopLevels.Count > 0)
        {
            if (TopLevels.Peek () != runState.Toplevel)
            {
                // If the top of the stack is not the RunState.Toplevel then
                // this call to End is not balanced with the call to Begin that started the RunState
                throw new ArgumentException ("End must be balanced with calls to Begin");
            }

            TopLevels.Pop ();
        }

        // Notify that it is closing
        runState.Toplevel?.OnClosed (runState.Toplevel);

        // If there is a OverlappedTop that is not the RunState.Toplevel then RunState.Toplevel
        // is a child of MidTop, and we should notify the OverlappedTop that it is closing
        if (ApplicationOverlapped.OverlappedTop is { } && !runState.Toplevel!.Modal && runState.Toplevel != ApplicationOverlapped.OverlappedTop)
        {
            ApplicationOverlapped.OverlappedTop.OnChildClosed (runState.Toplevel);
        }

        // Set Current and Top to the next TopLevel on the stack
        if (TopLevels.Count == 0)
        {
            if (Current is { HasFocus: true })
            {
                Current.HasFocus = false;
            }
            Current = null;
        }
        else
        {
            if (TopLevels.Count > 1 && TopLevels.Peek () == ApplicationOverlapped.OverlappedTop && ApplicationOverlapped.OverlappedChildren?.Any (t => t.Visible) != null)
            {
                ApplicationOverlapped.OverlappedMoveNext ();
            }

            Current = TopLevels.Peek ();

            if (TopLevels.Count == 1 && Current == ApplicationOverlapped.OverlappedTop)
            {
                ApplicationOverlapped.OverlappedTop.OnAllChildClosed ();
            }
            else
            {
                ApplicationOverlapped.SetCurrentOverlappedAsTop ();
                // BUGBUG: We should not call OnEnter/OnLeave directly; they should only be called by SetHasFocus
                if (runState.Toplevel is { HasFocus: true })
                {
                    runState.Toplevel.HasFocus = false;
                }

                if (Current is { HasFocus: false })
                {
                    Current.SetFocus ();
                    Current.RestoreFocus ();
                }
            }

            Refresh ();
        }

        // Don't dispose runState.Toplevel. It's up to caller dispose it
        // If it's not the same as the current in the RunIteration,
        // it will be fixed later in the next RunIteration.
        if (ApplicationOverlapped.OverlappedTop is { } && !TopLevels.Contains (ApplicationOverlapped.OverlappedTop))
        {
            _cachedRunStateToplevel = ApplicationOverlapped.OverlappedTop;
        }
        else
        {
            _cachedRunStateToplevel = runState.Toplevel;
        }

        runState.Toplevel = null;
        runState.Dispose ();
    }
}
