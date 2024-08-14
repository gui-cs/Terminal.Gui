using System.Diagnostics;
using static Terminal.Gui.FakeDriver;

namespace Terminal.Gui;

public partial class View // Focus and cross-view navigation management (TabStop, TabIndex, etc...)
{
    #region HasFocus

    // Backs `HasFocus` and is the ultimate source of truth whether a View has focus or not.
    private bool _hasFocus;

    /// <summary>
    ///     Gets or sets whether this view has focus.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only Views that are visible, enabled, and have <see cref="CanFocus"/> set to <see langword="true"/> are focusable. If
    ///         these conditions are not met when this property is set to <see langword="true"/> <see cref="HasFocus"/> will not change.
    ///     </para>
    ///     <para>
    ///         Setting this property causes the <see cref="OnEnter"/> and <see cref="OnLeave"/> virtual methods (and <see cref="Enter"/> and
    ///         <see cref="Leave"/> events to be raised). If the event is cancelled, <see cref="HasFocus"/> will not be changed.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see langword="true"/> will recursively set <see cref="HasFocus"/> to
    ///         <see langword="true"/> for all SuperViews up the hierarchy.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see langword="true"/> will cause the subview furthest down the hierarchy that is
    ///         focusable to also gain focus (as long as <see cref="TabStop"/>
    ///     </para>
    ///     <para>
    ///         Setting this property to <see langword="false"/> will cause <see cref="ApplicationNavigation.MoveNextView"/> to set
    ///         the focus on the next view to be focused.
    ///     </para>
    /// </remarks>
    public bool HasFocus
    {
        set
        {
            if (HasFocus != value)
            {
                if (value)
                {
                    if (EnterFocus (Application.Navigation?.GetFocused ()))
                    {
                        // The change happened
                        // HasFocus is now true
                    }
                }
                else
                {
                    LeaveFocus (null);
                }
            }
        }
        get => _hasFocus;
    }

    /// <summary>
    ///     Causes this view to be focused. Calling this method has the same effect as setting <see cref="HasFocus"/> to
    ///     <see langword="true"/> but with the added benefit of returning a value indicating whether the focus was set.
    /// </summary>
    public bool SetFocus ()
    {
        return EnterFocus (Application.Navigation?.GetFocused ());
    }

    /// <summary>
    ///     Called when view is entering focus. This method is called by <see cref="SetHasFocus"/> and other methods that
    ///     set or remove focus from a view.
    /// </summary>
    /// <param name="leavingView">The previously focused view. If <see langword="null"/> there is no previously focused view.</param>
    /// <param name="traversingUp"></param>
    /// <returns><see langword="true"/> if <see cref="HasFocus"/> was changed to <see langword="true"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private bool EnterFocus ([CanBeNull] View leavingView, bool traversingUp = false)
    {
        Debug.Assert (ApplicationNavigation.IsInHierarchy (SuperView, this));

        // Pre-conditions
        if (_hasFocus)
        {
            throw new InvalidOperationException ($"EnterFocus should not be called if the view already has focus.");
        }

        if (CanFocus && SuperView?.CanFocus == false)
        {
            throw new InvalidOperationException ($"It is not possible to EnterFocus if the View's SuperView has CanFocus = false.");
        }

        if (!CanBeVisible (this) || !Enabled)
        {
            return false;
        }

        if (!CanFocus)
        {
            return false;
        }

        bool previousValue = HasFocus;

        if (!traversingUp)
        {
            // Call the virtual method
            if (OnEnter (leavingView))
            {
                // The event was cancelled
                return false;
            }

            var args = new FocusEventArgs (leavingView, this);
            Enter?.Invoke (this, args);

            if (args.Cancel)
            {
                // The event was cancelled
                return false;
            }

            // If we're here, we can be focused. But we may have subviews.

            // Restore focus to the previously most focused subview in the subview-hierarchy
            if (RestoreFocus (TabStop))
            {
                // A subview was focused. We're done because the subview has focus and it recursed up the superview hierarchy.
                return true;
            }

            // Couldn't restore focus, so use Advance to navigate to the next focusable subview
            if (AdvanceFocus (NavigationDirection.Forward, TabStop))
            {
                // A subview was focused. We're done because the subview has focus and it recursed up the superview hierarchy.
                return true;
            }
        }

        // If we're here, we're the most-focusable view in the application OR we're traversing up the superview hierarchy.

        // If we previously had a subview with focus (`Focused = subview`), we need to make sure that all subviews down the `subview`-hierarchy LeaveFocus.
        if (Focused is { })
        {
            // LeaveFocus will recurse down the subview hierarchy and will also set PreviouslyMostFocused
            Focused.LeaveFocus (this);
            Focused = null;
        }

        // We need to ensure all superviews up the superview hierarchy have focus.
        // Any of them may cancel gaining focus. In which case we need to back out.
        if (SuperView is { HasFocus: false } sv)
        {
            // Tell EnterFocus that we're traversing up the superview hierarchy
            if (!sv.EnterFocus (leavingView, traversingUp))
            {
                // The change was cancelled
                return false;
            }
        }

        // If we're here, we're the most-focusable view in the application and all superviews up the superview hierarchy have focus.

        // By setting _hasFocus to true we definitively change HasFocus for this view.
        _hasFocus = true;

        // We're the most focused view in the application, we need to set the focused view to this view.
        Application.Navigation?.SetFocused (this);

        // Post-conditions - prove correctness
        if (HasFocus == previousValue)
        {
            throw new InvalidOperationException ($"EnterFocus was not cancelled and the HasFocus value did not change.");
        }

        SetNeedsDisplay ();

        return true;
    }

    /// <summary>Virtual method invoked when this view is gaining focus (entering).</summary>
    /// <param name="leavingView">The view that is leaving focus.</param>
    /// <returns> <see langword="true"/>, if the event is to be cancelled, <see langword="false"/> otherwise.</returns>
    protected virtual bool OnEnter ([CanBeNull] View leavingView)
    {
        return false;
    }

    /// <summary>Raised when the view is gaining (entering) focus. Can be cancelled.</summary>
    /// <remarks>
    ///     Raised by <see cref="EnterFocus"/>.
    /// </remarks>
    public event EventHandler<FocusEventArgs> Enter;

    /// <summary>
    ///     Called when view is losing focus.
    /// </summary>
    /// <param name="enteringView">The previously focused view. If <see langword="null"/> there is no previously focused view.</param>
    /// <returns><see langword="true"/> if <see cref="HasFocus"/> was changed.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private void LeaveFocus ([CanBeNull] View enteringView)
    {
        // Pre-conditions
        if (_hasFocus)
        {
            throw new InvalidOperationException ($"LeaveFocus should not be called if the view does not have focus.");
        }

        // If enteringView is null, we need to find the view that should get focus, and SetFocus on it.
        if (enteringView is null)
        {
            if (SuperView?.PreviouslyMostFocused != this)
            {
                SuperView?.PreviouslyMostFocused?.SetFocus ();

                // The above will cause LeaveFocus, so we can return
                return;
            }
            else
            {
                // Temporarily ensure this view can't get focus
                bool prevCanFocus = _canFocus;
                _canFocus = false;
                ApplicationNavigation.MoveNextView ();
                _canFocus = prevCanFocus;

                // The above will cause LeaveFocus, so we can return
                return;
            }
        }

        // Before we can leave focus, we need to make sure that all views down the subview-hierarchy have left focus.
        if (Application.Navigation?.GetFocused () != this)
        {
            // Save the most focused view in the subview-hierarchy
            View originalBottom = Application.Navigation?.GetFocused ();
            // Start at the bottom and work our way up to us
            View bottom = originalBottom;

            while (bottom is { } && bottom != this)
            {
                if (bottom.HasFocus)
                {
                    bottom.LeaveFocus (enteringView);
                    return ;
                }
                bottom = bottom.SuperView;
            }

            PreviouslyMostFocused = originalBottom;
        }

        bool previousValue = HasFocus;

        // Call the virtual method - NOTE: Leave cannot be cancelled
        OnLeave (enteringView);

        var args = new FocusEventArgs (enteringView, this);
        Leave?.Invoke (this, args);

        Focused = null;
        _hasFocus = false;

        if (Application.Navigation?.GetFocused () != this)
        {
            PreviouslyMostFocused = null;

            if (SuperView is { })
            {
                SuperView.Focused = null;
                SuperView.PreviouslyMostFocused = this;
            }
        }

        // Post-conditions - prove correctness
        if (HasFocus == previousValue)
        {
            throw new InvalidOperationException ($"LeaveFocus and the HasFocus value did not change.");
        }

        SetNeedsDisplay ();
    }

    /// <summary>
    ///     Caches the most focused subview when this view is losing focus. This is used by <see cref="RestoreFocus"/>.
    /// </summary>
    [CanBeNull]
    internal View PreviouslyMostFocused { get; set; }

    /// <summary>Virtual method invoked when this view is losing focus (leaving).</summary>
    /// <param name="enteringView">The view that is gaining focus.</param>
    protected virtual void OnLeave ([CanBeNull] View enteringView)
    {
        return;
    }

    /// <summary>Raised when the view is gaining (entering) focus. Can NOT be cancelled.</summary>
    /// <remarks>
    ///     Raised by <see cref="LeaveFocus"/>.
    /// </remarks>
    public event EventHandler<FocusEventArgs> Leave;

    #endregion HasFocus

    /// <summary>
    ///     Advances the focus to the next or previous view in <see cref="View.TabIndexes"/>, based on
    ///     <paramref name="direction"/>.
    ///     itself.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If there is no next/previous view, the focus is set to the view itself.
    ///     </para>
    /// </remarks>
    /// <param name="direction"></param>
    /// <param name="behavior"></param>
    /// <returns>
    ///     <see langword="true"/> if focus was changed to another subview (or stayed on this one), <see langword="false"/>
    ///     otherwise.
    /// </returns>
    public bool AdvanceFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        if (!CanBeVisible (this)) // TODO: is this check needed?
        {
            return false;
        }

        if (TabIndexes is null || TabIndexes.Count == 0)
        {
            return false;
        }

        if (Focused is null)
        {
            FocusDeepest (behavior, direction);

            return Focused is { };
        }

        if (Focused is { })
        {
            if (Focused.AdvanceFocus (direction, behavior))
            {
                // TODO: Temporary hack to make Application.Navigation.FocusChanged work
                if (Focused.Focused is null)
                {
                    Application.Navigation?.SetFocused (Focused);
                }
                return true;
            }
        }

        var index = GetScopedTabIndexes (behavior, direction);

        if (index.Length == 0)
        {
            return false;
        }

        var focusedIndex = index.IndexOf (Focused);
        int next = 0;

        if (focusedIndex < index.Length - 1)
        {
            next = focusedIndex + 1;
        }
        else
        {
            if (behavior == TabBehavior.TabGroup && behavior == TabStop && SuperView?.TabStop == TabBehavior.TabGroup)
            {
                // Go down the subview-hierarchy and leave
                // BUGBUG: This doesn't seem right
                Focused.HasFocus = false;

                // TODO: Should we check the return value of SetHasFocus?

                return false;
            }
        }

        View view = index [next];

        if (view.HasFocus)
        {
            return true;
        }

        // The subview does not have focus, but at least one other that can. Can this one be focused?
        if (view.CanFocus && view.Visible && view.Enabled)
        {
            // Make Focused Leave
            // BUGBUG: This doesn't seem right
            Focused.HasFocus = false;

            view.FocusDeepest (TabBehavior.TabStop, direction);

            // TODO: Temporary hack to make Application.Navigation.FocusChanged work
            if (view.Focused is null)
            {
                Application.Navigation?.SetFocused (view);
            }

            return true;
        }

        if (Focused is { })
        {
            // Leave
            // BUGBUG: This doesn't seem right
            Focused.HasFocus = false;

            // Signal that nothing is focused, and callers should try a peer-subview
            Focused = null;
        }

        return false;
    }


    /// <summary>
    /// INTERNAL API to restore focus to the subview that had focus before this view lost focus.
    /// </summary>
    /// <returns>
    ///     Returns true if focus was restored to a subview, false otherwise.
    /// </returns>
    internal bool RestoreFocus (TabBehavior? behavior)
    {
        if (Focused is null && _subviews?.Count > 0)
        {
            // TODO: Find the previous focused view and set focus to it
            if (PreviouslyMostFocused is { } && PreviouslyMostFocused.TabStop == behavior)
            {
                return PreviouslyMostFocused.SetFocus ();
            }
            return true;
        }

        return false;
    }

    ///// <summary>
    /////     Internal API that causes <paramref name="viewToEnterFocus"/> to enter focus.
    /////     <paramref name="viewToEnterFocus"/> must be a subview.
    /////     Recursively sets focus up the superview hierarchy.
    ///// </summary>
    ///// <param name="viewToEnterFocus"></param>
    ///// <returns><see langword="true"/> if <paramref name="viewToEnterFocus"/> got focus.</returns>
    //private bool SetFocus (View viewToEnterFocus)
    //{
    //    if (viewToEnterFocus is null)
    //    {
    //        return false;
    //    }

    //    if (!viewToEnterFocus.CanFocus || !viewToEnterFocus.Visible || !viewToEnterFocus.Enabled)
    //    {
    //        return false;
    //    }

    //    // If viewToEnterFocus is already the focused view, don't do anything
    //    if (Focused?._hasFocus == true && Focused == viewToEnterFocus)
    //    {
    //        return false;
    //    }

    //    // If a subview has focus and viewToEnterFocus is the focused view's superview OR viewToEnterFocus is this view,
    //    // then make viewToEnterFocus.HasFocus = true and return
    //    if ((Focused?._hasFocus == true && Focused?.SuperView == viewToEnterFocus) || viewToEnterFocus == this)
    //    {
    //        if (!viewToEnterFocus._hasFocus)
    //        {
    //            viewToEnterFocus._hasFocus = true;
    //        }

    //        // viewToEnterFocus is already focused
    //        return true;
    //    }

    //    // Make sure that viewToEnterFocus is a subview of this view
    //    View c;

    //    for (c = viewToEnterFocus._superView; c != null; c = c._superView)
    //    {
    //        if (c == this)
    //        {
    //            break;
    //        }
    //    }

    //    if (c is null)
    //    {
    //        throw new ArgumentException (@$"The specified view {viewToEnterFocus} is not part of the hierarchy of {this}.");
    //    }

    //    // If a subview has focus, make it leave focus. This will leave focus up the hierarchy.
    //    Focused?.SetHasFocus (false, viewToEnterFocus);

    //    // make viewToEnterFocus Focused and enter focus
    //    View f = Focused;
    //    Focused = viewToEnterFocus;
    //    Focused?.SetHasFocus (true, f, true);
    //    Focused?.FocusDeepest (null, NavigationDirection.Forward);

    //    // Recursively set focus up the superview hierarchy
    //    if (SuperView is { })
    //    {
    //        // BUGBUG: If focus is cancelled at any point, we should stop and restore focus to the previous focused view
    //        SuperView.SetFocus (this);
    //    }
    //    else
    //    {
    //        // BUGBUG: this makes no sense in the new design
    //        // If there is no SuperView, then this is a top-level view
    //        SetFocus (this);

    //    }

    //    // TODO: Temporary hack to make Application.Navigation.FocusChanged work
    //    if (HasFocus && Focused.Focused is null)
    //    {
    //        Application.Navigation?.SetFocused (Focused);
    //    }

    //    // TODO: This is a temporary hack to make overlapped non-Toplevels have a zorder. See also: View.OnDrawContent.
    //    if (viewToEnterFocus is { } && (viewToEnterFocus.TabStop == TabBehavior.TabGroup && viewToEnterFocus.Arrangement.HasFlag (ViewArrangement.Overlapped)))
    //    {
    //        viewToEnterFocus.TabIndex = 0;
    //    }

    //    return true;
    //}


#if AUTO_CANFOCUS
    // BUGBUG: This is a poor API design. Automatic behavior like this is non-obvious and should be avoided. Instead, callers to Add should be explicit about what they want.
    // Set to true in Add() to indicate that the view being added to a SuperView has CanFocus=true.
    // Makes it so CanFocus will update the SuperView's CanFocus property.
    internal bool _addingViewSoCanFocusAlsoUpdatesSuperView;

    // Used to cache CanFocus on subviews when CanFocus is set to false so that it can be restored when CanFocus is changed back to true
    private bool _oldCanFocus;
#endif

    private bool _canFocus;

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> can be focused.</summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="SuperView"/> must also have <see cref="CanFocus"/> set to <see langword="true"/>.
    ///     </para>
    ///     <para>
    ///         When set to <see langword="false"/>, if an attempt is made to make this view focused, the focus will be set to
    ///         the next focusable view.
    ///     </para>
    ///     <para>
    ///         When set to <see langword="false"/>, the <see cref="TabIndex"/> will be set to -1.
    ///     </para>
    ///     <para>
    ///         When set to <see langword="false"/>, the values of <see cref="CanFocus"/> and <see cref="TabIndex"/> for all
    ///         subviews will be cached so that when <see cref="CanFocus"/> is set back to <see langword="true"/>, the subviews
    ///         will be restored to their previous values.
    ///     </para>
    ///     <para>
    ///         Changing this property to <see langword="true"/> will cause <see cref="TabStop"/> to be set to
    ///         <see cref="TabBehavior.TabStop"/>" as a convenience. Changing this property to
    ///         <see langword="false"/> will have no effect on <see cref="TabStop"/>.
    ///     </para>
    /// </remarks>
    public bool CanFocus
    {
        get => _canFocus;
        set
        {
#if AUTO_CANFOCUS
            if (!_addingViewSoCanFocusAlsoUpdatesSuperView && IsInitialized && SuperView?.CanFocus == false && value)
            {
                throw new InvalidOperationException ("Cannot set CanFocus to true if the SuperView CanFocus is false!");
            }
#endif

            if (_canFocus == value)
            {
                return;
            }

            _canFocus = value;

#if AUTO_CANFOCUS
            switch (_canFocus)
            {
                case false when _tabIndex > -1:
                    // BUGBUG: This is a poor API design. Automatic behavior like this is non-obvious and should be avoided. Callers should adjust TabIndex explicitly.
                    //TabIndex = -1;

                    break;

                case true when SuperView?.CanFocus == false && _addingViewSoCanFocusAlsoUpdatesSuperView:
                    SuperView.CanFocus = true;

                    break;
            }
#endif

            if (TabStop is null && _canFocus)
            {
                TabStop = TabBehavior.TabStop;
            }

            if (!_canFocus && SuperView?.Focused == this)
            {
                SuperView.Focused = null;
            }

            if (!_canFocus && HasFocus)
            {
                HasFocus = false;
                SuperView?.RestoreFocus (null);

                // If EnsureFocus () didn't set focus to a view, focus the next focusable view in the application
                if (SuperView is { Focused: null })
                {
                    SuperView.AdvanceFocus (NavigationDirection.Forward, null);

                    if (SuperView.Focused is null && Application.Current is { })
                    {
                        Application.Current.AdvanceFocus (NavigationDirection.Forward, null);
                    }

                    ApplicationOverlapped.BringOverlappedTopToFront ();
                }
            }

            if (_subviews is { } && IsInitialized)
            {
#if AUTO_CANFOCUS
                // Change the CanFocus of all subviews to the same value as this view
                // if the CanFocus of the subview is different from the value being set
                foreach (View view in _subviews)
                {
                    if (view.CanFocus != value)
                    {
                        if (!value)
                        {
                            // Cache the old CanFocus and TabIndex so that they can be restored when CanFocus is changed back to true
                            view._oldCanFocus = view.CanFocus;
                            view._oldTabIndex = view._tabIndex;
                            view.CanFocus = false;

                            //view._tabIndex = -1;
                        }
                        else
                        {
                            if (_addingViewSoCanFocusAlsoUpdatesSuperView)
                            {
                                view._addingViewSoCanFocusAlsoUpdatesSuperView = true;
                            }

                            // Restore the old CanFocus and TabIndex to the values they held before CanFocus was set to false
                            view.CanFocus = view._oldCanFocus;
                            view._tabIndex = view._oldTabIndex;
                            view._addingViewSoCanFocusAlsoUpdatesSuperView = false;
                        }
                    }
                }
#endif
                if (this is Toplevel && Application.Current!.Focused != this)
                {
                    ApplicationOverlapped.BringOverlappedTopToFront ();
                }
            }

            OnCanFocusChanged ();
            SetNeedsDisplay ();
        }
    }

    /// <summary>Raised when <see cref="CanFocus"/> has been changed.</summary>
    /// <remarks>
    ///     Raised by the <see cref="OnCanFocusChanged"/> virtual method.
    /// </remarks>
    public event EventHandler CanFocusChanged;

    /// <summary>Returns the currently focused Subview inside this view, or <see langword="null"/> if nothing is focused.</summary>
    /// <value>The currently focused Subview.</value>
    [CanBeNull]
    public View Focused { get; private set; }

    /// <summary>
    ///     Focuses the deepest focusable view in <see cref="View.TabIndexes"/> if one exists. If there are no views in
    ///     <see cref="View.TabIndexes"/> then the focus is set to the view itself.
    /// </summary>
    /// <param name="behavior"></param>
    /// <param name="direction"></param>
    public void FocusDeepest (TabBehavior? behavior, NavigationDirection direction)
    {
        if (!CanBeVisible (this))
        {
            return;
        }

        View deepest = FindDeepestFocusableView (behavior, direction);

        if (deepest is { })
        {
            deepest.SetFocus ();
        }
    }

    [CanBeNull]
    private View FindDeepestFocusableView (TabBehavior? behavior, NavigationDirection direction)
    {
        var indicies = GetScopedTabIndexes (behavior, direction);

        foreach (View v in indicies)
        {
            if (v.TabIndexes.Count == 0)
            {
                return v;
            }

            return v.FindDeepestFocusableView (behavior, direction);
        }

        return null;
    }

    /// <summary>Returns a value indicating if this View is currently on Top (Active)</summary>
    public bool IsCurrentTop => Application.Current == this;

    /// <summary>
    ///     Returns the most focused Subview in the chain of subviews (the leaf view that has the focus), or
    ///     <see langword="null"/> if nothing is focused.
    /// </summary>
    /// <value>The most focused Subview.</value>
    public View MostFocused
    {
        get
        {
            if (Focused is null)
            {
                return null;
            }

            View most = Focused.MostFocused;

            if (most is { })
            {
                return most;
            }

            return Focused;
        }
    }

    /// <summary>Invoked when the <see cref="CanFocus"/> property from a view is changed.</summary>
    /// <remarks>
    ///     Raises the <see cref="CanFocusChanged"/> event.
    /// </remarks>
    public virtual void OnCanFocusChanged () { CanFocusChanged?.Invoke (this, EventArgs.Empty); }

    #region Tab/Focus Handling

#nullable enable

    private List<View>? _tabIndexes;

    // TODO: This should be a get-only property?
    // BUGBUG: This returns an AsReadOnly list, but isn't declared as such.
    /// <summary>Gets a list of the subviews that are a <see cref="TabStop"/>.</summary>
    /// <value>The tabIndexes.</value>
    public IList<View> TabIndexes => _tabIndexes?.AsReadOnly () ?? _empty;

    /// <summary>
    /// Gets TabIndexes that are scoped to the specified behavior and direction. If behavior is null, all TabIndexes are returned.
    /// </summary>
    /// <param name="behavior"></param>
    /// <param name="direction"></param>
    /// <returns></returns>GetScopedTabIndexes
    private View [] GetScopedTabIndexes (TabBehavior? behavior, NavigationDirection direction)
    {
        IEnumerable<View>? indicies;

        if (behavior.HasValue)
        {
            indicies = _tabIndexes?.Where (v => v.TabStop == behavior && v is { CanFocus: true, Visible: true, Enabled: true });
        }
        else
        {
            indicies = _tabIndexes?.Where (v => v is { CanFocus: true, Visible: true, Enabled: true });
        }

        if (direction == NavigationDirection.Backward)
        {
            indicies = indicies?.Reverse ();
        }

        return indicies?.ToArray () ?? Array.Empty<View> ();

    }

    private int? _tabIndex; // null indicates the view has not yet been added to TabIndexes
    private int? _oldTabIndex;

    /// <summary>
    ///     Indicates the order of the current <see cref="View"/> in <see cref="TabIndexes"/> list.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see langword="null"/>, the view is not part of the tab order.
    ///     </para>
    ///     <para>
    ///         On set, if <see cref="SuperView"/> is <see langword="null"/> or has not TabStops, <see cref="TabIndex"/> will
    ///         be set to 0.
    ///     </para>
    ///     <para>
    ///         On set, if <see cref="SuperView"/> has only one TabStop, <see cref="TabIndex"/> will be set to 0.
    ///     </para>
    ///     <para>
    ///         See also <seealso cref="TabStop"/>.
    ///     </para>
    /// </remarks>
    public int? TabIndex
    {
        get => _tabIndex;

        // TOOD: This should be a get-only property. Introduce SetTabIndex (int value) (or similar).
        set
        {
            // Once a view is in the tab order, it should not be removed from the tab order; set TabStop to NoStop instead.
            Debug.Assert (value >= 0);
            Debug.Assert (value is { });

            if (SuperView?._tabIndexes is null || SuperView?._tabIndexes.Count == 1)
            {
                // BUGBUG: Property setters should set the property to the value passed in and not have side effects.
                _tabIndex = 0;

                return;
            }

            if (_tabIndex == value && TabIndexes.IndexOf (this) == value)
            {
                return;
            }

            _tabIndex = value > SuperView!.TabIndexes.Count - 1 ? SuperView._tabIndexes.Count - 1 :
                        value < 0 ? 0 : value;
            _tabIndex = GetGreatestTabIndexInSuperView ((int)_tabIndex);

            if (SuperView._tabIndexes.IndexOf (this) != _tabIndex)
            {
                // BUGBUG: we have to use _tabIndexes and not TabIndexes because TabIndexes returns is a read-only version of _tabIndexes
                SuperView._tabIndexes.Remove (this);
                SuperView._tabIndexes.Insert ((int)_tabIndex, this);
                UpdatePeerTabIndexes ();
            }
            return;

            // Updates the <see cref="TabIndex"/>s of the views in the <see cref="SuperView"/>'s to match their order in <see cref="TabIndexes"/>.
            void UpdatePeerTabIndexes ()
            {
                if (SuperView is null)
                {
                    return;
                }

                var i = 0;

                foreach (View superViewTabStop in SuperView._tabIndexes)
                {
                    if (superViewTabStop._tabIndex is null)
                    {
                        continue;
                    }

                    superViewTabStop._tabIndex = i;
                    i++;
                }
            }
        }
    }


    /// <summary>
    ///     Gets the greatest <see cref="TabIndex"/> of the <see cref="SuperView"/>'s <see cref="TabIndexes"/> that is less
    ///     than or equal to <paramref name="idx"/>.
    /// </summary>
    /// <param name="idx"></param>
    /// <returns>The minimum of <paramref name="idx"/> and the <see cref="SuperView"/>'s <see cref="TabIndexes"/>.</returns>
    private int GetGreatestTabIndexInSuperView (int idx)
    {
        if (SuperView is null)
        {
            return 0;
        }

        var i = 0;

        foreach (View superViewTabStop in SuperView._tabIndexes)
        {
            if (superViewTabStop._tabIndex is null || superViewTabStop == this)
            {
                continue;
            }

            i++;
        }

        return Math.Min (i, idx);
    }



    private TabBehavior? _tabStop;

    /// <summary>
    ///     Gets or sets the behavior of <see cref="AdvanceFocus"/> for keyboard navigation.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see langword="null"/> the tab stop has not been set and setting <see cref="CanFocus"/> to true will set it
    ///         to
    ///         <see cref="TabBehavior.TabStop"/>.
    ///     </para>
    ///     <para>
    ///         TabStop is independent of <see cref="CanFocus"/>. If <see cref="CanFocus"/> is <see langword="false"/>, the
    ///         view will not gain
    ///         focus even if this property is set and vice-versa.
    ///     </para>
    ///     <para>
    ///         The default <see cref="TabBehavior.TabStop"/> keys are <see cref="Application.NextTabKey"/> (<c>Key.Tab</c>) and <see cref="Application.PrevTabKey"/> (<c>Key>Tab.WithShift</c>).
    ///     </para>
    ///     <para>
    ///         The default <see cref="TabBehavior.TabGroup"/> keys are <see cref="Application.NextTabGroupKey"/> (<c>Key.F6</c>) and <see cref="Application.PrevTabGroupKey"/> (<c>Key>Key.F6.WithShift</c>).
    ///     </para>
    /// </remarks>
    public TabBehavior? TabStop
    {
        get => _tabStop;
        set
        {
            if (_tabStop == value)
            {
                return;
            }

            Debug.Assert (value is { });

            if (_tabStop is null && TabIndex is null)
            {
                // This view has not yet been added to TabIndexes (TabStop has not been set previously).
                TabIndex = GetGreatestTabIndexInSuperView (SuperView is { } ? SuperView._tabIndexes.Count : 0);
            }

            _tabStop = value;
        }
    }

    #endregion Tab/Focus Handling
}
