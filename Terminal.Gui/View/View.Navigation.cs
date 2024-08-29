#nullable enable
using System.Diagnostics;

namespace Terminal.Gui;

public partial class View // Focus and cross-view navigation management (TabStop, TabIndex, etc...)
{
    private bool _canFocus;

    /// <summary>
    ///     Advances the focus to the next or previous view in the focus chain, based on
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

        View? focused = Focused;

        if (focused is { } && focused.AdvanceFocus (direction, behavior))
        {
            return true;
        }

        // AdvanceFocus did not advance
        View [] index = GetSubviewFocusChain (direction, behavior);

        if (index.Length == 0)
        {
            return false;
        }

        if (behavior == TabBehavior.TabGroup)
        {
            if (direction == NavigationDirection.Forward && focused == index [^1] && SuperView is null)
            {
                // We're at the top of the focus chain. Go back down the focus chain and focus the first TabGroup
                View [] views = GetSubviewFocusChain (NavigationDirection.Forward, TabBehavior.TabGroup);

                if (views.Length > 0)
                {
                    View [] subViews = views [0].GetSubviewFocusChain (NavigationDirection.Forward, TabBehavior.TabStop);

                    if (subViews.Length > 0)
                    {
                        if (subViews [0].SetFocus ())
                        {
                            return true;
                        }
                    }
                }
            }

            if (direction == NavigationDirection.Backward && focused == index [0])
            {
                // We're at the bottom of the focus chain
                View [] views = GetSubviewFocusChain (NavigationDirection.Forward, TabBehavior.TabGroup);

                if (views.Length > 0)
                {
                    View [] subViews = views [^1].GetSubviewFocusChain (NavigationDirection.Forward, TabBehavior.TabStop);

                    if (subViews.Length > 0)
                    {
                        if (subViews [0].SetFocus ())
                        {
                            return true;
                        }
                    }
                }
            }

        }


        int focusedIndex = index.IndexOf (Focused); // Will return -1 if Focused can't be found or is null
        var next = 0;

        if (focusedIndex < index.Length - 1)
        {
            // We're moving w/in the subviews
            next = focusedIndex + 1;
        }
        else
        {
            // We're moving beyond the last subview

            // Determine if focus should remain in this focus chain, or move to the superview's focus chain

            // If we are TabStop and our SuperView has at least one other TabStop subview, move to the SuperView's chain
            if (TabStop == TabBehavior.TabStop && SuperView is { } && SuperView.GetSubviewFocusChain (direction, behavior).Length > 1)
            {
                return false;
            }

            // TabGroup is special-cased. 
            if (focused?.TabStop == TabBehavior.TabGroup)
            {
                if (SuperView?.GetSubviewFocusChain (direction, TabBehavior.TabGroup)?.Length > 0)
                {
                    // Our superview has a TabGroup subview; signal we couldn't move so we nav out to it
                    return false;
                }
            }
        }

        View view = index [next];

        if (view.HasFocus)
        {
            // We could not advance
            return view == this;
        }

        // The subview does not have focus, but at least one other that can. Can this one be focused?
        (bool focusSet, bool _) = view.SetHasFocusTrue (Focused);

        return focusSet;
    }

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
    ///         When set to <see langword="false"/>, the value of <see cref="CanFocus"/> for all
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
            if (_canFocus == value)
            {
                return;
            }

            _canFocus = value;

            if (TabStop is null && _canFocus)
            {
                TabStop = TabBehavior.TabStop;
            }

            if (!_canFocus && HasFocus)
            {
                // If CanFocus is set to false and this view has focus, make it leave focus
                HasFocus = false;
            }

            if (_canFocus && !HasFocus && Visible && SuperView is { } && SuperView.Focused is null)
            {
                // If CanFocus is set to true and this view does not have focus, make it enter focus
                SetFocus ();
            }

            OnCanFocusChanged ();
        }
    }

    /// <summary>Raised when <see cref="CanFocus"/> has been changed.</summary>
    /// <remarks>
    ///     Raised by the <see cref="OnCanFocusChanged"/> virtual method.
    /// </remarks>
    public event EventHandler? CanFocusChanged;

    /// <summary>
    ///     Focuses the deepest focusable Subview if one exists. If there are no focusable Subviews then the focus is set to the view itself.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="behavior"></param>
    /// <returns><see langword="true"/> if a subview other than this was focused.</returns>
    public bool FocusDeepest (NavigationDirection direction, TabBehavior? behavior)
    {
        View? deepest = FindDeepestFocusableView (direction, behavior);

        if (deepest is { })
        {
            return deepest.SetFocus ();
        }

        return SetFocus ();
    }

    /// <summary>Gets the currently focused Subview of this view, or <see langword="null"/> if nothing is focused.</summary>
    public View? Focused
    {
        get { return Subviews.FirstOrDefault (v => v.HasFocus); }
    }

    /// <summary>Returns a value indicating if this View is currently on Top (Active)</summary>
    public bool IsCurrentTop => Application.Current == this;

    /// <summary>
    ///     Returns the most focused Subview down the subview-hierarchy.
    /// </summary>
    /// <value>The most focused Subview, or <see langword="null"/> if no Subview is focused.</value>
    public View? MostFocused
    {
        get
        {
            // TODO: Remove this API. It's duplicative of Application.Navigation.GetFocused.
            if (Focused is null)
            {
                return null;
            }

            View? most = Focused.MostFocused;

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

    /// <summary>
    ///     INTERNAL API to restore focus to the subview that had focus before this view lost focus.
    /// </summary>
    /// <returns>
    ///     Returns true if focus was restored to a subview, false otherwise.
    /// </returns>
    internal bool RestoreFocus (TabBehavior? behavior)
    {
        if (Focused is null && _subviews?.Count > 0)
        {
            if (_previouslyMostFocused is { } /* && (behavior is null || _previouslyMostFocused.TabStop == behavior)*/)
            {
                return _previouslyMostFocused.SetFocus ();
            }

            return false;
        }

        return false;
    }

    private View? FindDeepestFocusableView (NavigationDirection direction, TabBehavior? behavior)
    {
        View [] indicies = GetSubviewFocusChain (direction, behavior);

        foreach (View v in indicies)
        {
            return v.FindDeepestFocusableView (direction, behavior);
        }

        return null;
    }

    #region HasFocus

    // Backs `HasFocus` and is the ultimate source of truth whether a View has focus or not.
    private bool _hasFocus;

    /// <summary>
    ///     Gets or sets whether this view has focus.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Only Views that are visible, enabled, and have <see cref="CanFocus"/> set to <see langword="true"/> are
    ///         focusable. If
    ///         these conditions are not met when this property is set to <see langword="true"/> <see cref="HasFocus"/> will
    ///         not change.
    ///     </para>
    ///     <para>
    ///         Setting this property causes the <see cref="OnHasFocusChanging"/> and <see cref="OnHasFocusChanged"/> virtual
    ///         methods (and <see cref="HasFocusChanging"/> and
    ///         <see cref="HasFocusChanged"/> events to be raised). If the event is cancelled, <see cref="HasFocus"/> will not
    ///         be changed.
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
    ///         Setting this property to <see langword="false"/> will cause <see cref="AdvanceFocus"/> to set
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
                    // NOTE: If Application.Navigation is null, we pass null to FocusChanging. For unit tests.
                    (bool focusSet, bool _) = SetHasFocusTrue (Application.Navigation?.GetFocused ());

                    if (focusSet)
                    {
                        // The change happened
                        // HasFocus is now true
                    }
                }
                else
                {
                    SetHasFocusFalse (null);
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
        (bool focusSet, bool _) = SetHasFocusTrue (Application.Navigation?.GetFocused ());

        return focusSet;
    }

    /// <summary>
    ///     INTERNAL: Called when focus is going to change to this view. This method is called by <see cref="SetFocus"/> and
    ///     other methods that
    ///     set or remove focus from a view.
    /// </summary>
    /// <param name="previousFocusedView">
    ///     The previously focused view. If <see langword="null"/> there is no previously focused
    ///     view.
    /// </param>
    /// <param name="traversingUp"></param>
    /// <returns><see langword="true"/> if <see cref="HasFocus"/> was changed to <see langword="true"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    private (bool focusSet, bool cancelled) SetHasFocusTrue (View? previousFocusedView, bool traversingUp = false)
    {
        Debug.Assert (ApplicationNavigation.IsInHierarchy (SuperView, this));

        // Pre-conditions
        if (_hasFocus)
        {
            return (false, false);
        }

        if (CanFocus && SuperView is { CanFocus: false })
        {
            Debug.WriteLine ($@"WARNING: Attempt to FocusChanging where SuperView.CanFocus == false. {this}");

            return (false, false);
        }

        if (!CanBeVisible (this) || !Enabled)
        {
            return (false, false);
        }

        if (!CanFocus)
        {
            return (false, false);
        }

        bool previousValue = HasFocus;

        bool cancelled = NotifyFocusChanging (false, true, previousFocusedView, this);

        if (cancelled)
        {
            return (false, true);
        }

        // Make sure superviews up the superview hierarchy have focus.
        // Any of them may cancel gaining focus. In which case we need to back out.
        if (SuperView is { HasFocus: false } sv)
        {
            (bool focusSet, bool svCancelled) = sv.SetHasFocusTrue (previousFocusedView, true);

            if (!focusSet)
            {
                return (false, svCancelled);
            }
        }

        if (_hasFocus)
        {
            // Something else beat us to the change (likely a FocusChanged handler).
            return (true, false);
        }

        // By setting _hasFocus to true we definitively change HasFocus for this view.

        // Get whatever peer has focus, if any
        View? focusedPeer = SuperView?.Focused;

        _hasFocus = true;

        // Ensure that the peer loses focus
        focusedPeer?.SetHasFocusFalse (this, true);

        if (!traversingUp)
        {
            // Restore focus to the previously most focused subview in the subview-hierarchy
            if (!RestoreFocus (TabStop))
            {
                // Couldn't restore focus, so use Advance to navigate to the next focusable subview
                if (!AdvanceFocus (NavigationDirection.Forward, null))
                {
                    // Couldn't advance, so we're the most focused view in the application
                    _previouslyMostFocused = null;
                    Application.Navigation?.SetFocused (this);
                }
            }
        }

        if (previousFocusedView is { HasFocus: true } && Subviews.Contains (previousFocusedView))
        {
            previousFocusedView.SetHasFocusFalse (this);
        }

        NotifyFocusChanged (HasFocus, previousFocusedView, this);
        SetNeedsDisplay ();

        // Post-conditions - prove correctness
        if (HasFocus == previousValue)
        {
            throw new InvalidOperationException ("NotifyFocusChanging was not cancelled and the HasFocus value did not change.");
        }

        return (true, false);
    }

    private bool NotifyFocusChanging (bool currentHasFocus, bool newHasFocus, View? currentFocused, View? newFocused)
    {
        // Call the virtual method
        if (OnHasFocusChanging (currentHasFocus, newHasFocus, currentFocused, newFocused))
        {
            // The event was cancelled
            return true;
        }

        var args = new HasFocusEventArgs (currentHasFocus, newHasFocus, currentFocused, newFocused);
        HasFocusChanging?.Invoke (this, args);

        if (args.Cancel)
        {
            // The event was cancelled
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Invoked when <see cref="View.HasFocus"/> is about to change. This method is called before the
    ///     <see cref="HasFocusChanging"/> event is raised.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Use <see cref="OnHasFocusChanged"/> to be notified after the focus has changed.
    ///     </para>
    /// </remarks>
    /// <param name="currentHasFocus">The current value of <see cref="View.HasFocus"/>.</param>
    /// <param name="newHasFocus">The value <see cref="View.HasFocus"/> will have if the focus change happens.</param>
    /// <param name="currentFocused">The view that is currently Focused. May be <see langword="null"/>.</param>
    /// <param name="newFocused">The view that will be focused. May be <see langword="null"/>.</param>
    /// <returns>
    ///     <see langword="true"/>, if the change to <see cref="View.HasFocus"/> is to be cancelled, <see langword="false"/>
    ///     otherwise.
    /// </returns>
    protected virtual bool OnHasFocusChanging (bool currentHasFocus, bool newHasFocus, View? currentFocused, View? newFocused)
    {
        return false;
    }

    /// <summary>
    ///     Raised when <see cref="View.HasFocus"/> is about to change.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Cancel the event to prevent the focus from changing.
    ///     </para>
    ///     <para>
    ///         Use <see cref="HasFocusChanged"/> to be notified after the focus has changed.
    ///     </para>
    /// </remarks>
    public event EventHandler<HasFocusEventArgs>? HasFocusChanging;

    /// <summary>
    ///     Called when this view should stop being focused.
    /// </summary>
    /// <param name="newFocusedView">The new focused view. If <see langword="null"/> it is not known which view will be focused.</param>
    /// <param name="traversingDown">Set to true to indicate method is being called recurively, traversing down the focus chain.</param>
    /// <exception cref="InvalidOperationException"></exception>
    private void SetHasFocusFalse (View? newFocusedView, bool traversingDown = false)
    {
        // Pre-conditions
        if (!_hasFocus)
        {
            throw new InvalidOperationException ("SetHasFocusFalse should not be called if the view does not have focus.");
        }

        // If newFocusedVew is null, we need to find the view that should get focus, and SetFocus on it.
        if (!traversingDown && newFocusedView is null)
        {
            if (SuperView?._previouslyMostFocused is { } && SuperView?._previouslyMostFocused != this)
            {
                SuperView?._previouslyMostFocused?.SetFocus ();

                // The above will cause SetHasFocusFalse, so we can return
                return;
            }

            if (SuperView is { } && SuperView.AdvanceFocus (NavigationDirection.Forward, TabStop))
            {
                // The above will cause SetHasFocusFalse, so we can return
                return;
            }

            if (Application.Navigation is { } && Application.Current is { })
            {
                // Temporarily ensure this view can't get focus
                bool prevCanFocus = _canFocus;
                _canFocus = false;
                bool restoredFocus = Application.Current!.RestoreFocus (null);
                _canFocus = prevCanFocus;

                if (restoredFocus)
                {
                    // The above caused SetHasFocusFalse, so we can return
                    return;
                }
            }

            // No other focusable view to be found. Just "leave" us...
        }

        // Before we can leave focus, we need to make sure that all views down the subview-hierarchy have left focus.
        View? mostFocused = MostFocused;

        if (mostFocused is { } && (newFocusedView is null || mostFocused != newFocusedView))
        {
            // Start at the bottom and work our way up to us
            View? bottom = mostFocused;

            while (bottom is { } && bottom != this)
            {
                if (bottom.HasFocus)
                {
                    bottom.SetHasFocusFalse (newFocusedView, true);
                }

                bottom = bottom.SuperView;
            }

            _previouslyMostFocused = mostFocused;
        }

        bool previousValue = HasFocus;

        // Note, can't be cancelled.
        NotifyFocusChanging (HasFocus, !HasFocus, newFocusedView, this);

        // Get whatever peer has focus, if any
        View? focusedPeer = SuperView?.Focused;
        _hasFocus = false;

        if (Application.Navigation is { })
        {
            View? appFocused = Application.Navigation.GetFocused ();

            if (appFocused is { } || appFocused == this)
            {
                Application.Navigation.SetFocused (newFocusedView ?? SuperView);
            }
        }

        NotifyFocusChanged (HasFocus, this, newFocusedView);

        if (_hasFocus)
        {
            // Notify caused HasFocus to change to true.
            return;
        }

        if (SuperView is { })
        {
            SuperView._previouslyMostFocused = focusedPeer;
        }

        // Post-conditions - prove correctness
        if (HasFocus == previousValue)
        {
            throw new InvalidOperationException ("SetHasFocusFalse and the HasFocus value did not change.");
        }

        SetNeedsDisplay ();
    }

    /// <summary>
    ///     Caches the most focused subview when this view is losing focus. This is used by <see cref="RestoreFocus"/>.
    /// </summary>
    private View? _previouslyMostFocused;

    private void NotifyFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedVew)
    {
        // Call the virtual method
        OnHasFocusChanged (newHasFocus, previousFocusedView, focusedVew);

        // Raise the event
        var args = new HasFocusEventArgs (newHasFocus, newHasFocus, previousFocusedView, focusedVew);
        HasFocusChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Invoked after <see cref="HasFocus"/> has changed. This method is called before the <see cref="HasFocusChanged"/>
    ///     event is raised.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This event cannot be cancelled.
    ///     </para>
    /// </remarks>
    /// <param name="newHasFocus">The new value of <see cref="View.HasFocus"/>.</param>
    /// <param name="previousFocusedView"></param>
    /// <param name="focusedVew">The view that is now focused. May be <see langword="null"/></param>
    protected virtual void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? focusedVew) { }

    /// <summary>Raised after <see cref="HasFocus"/> has changed.</summary>
    /// <remarks>
    ///     <para>
    ///         This event cannot be cancelled.
    ///     </para>
    /// </remarks>
    public event EventHandler<HasFocusEventArgs>? HasFocusChanged;

    #endregion HasFocus

    #region Tab/Focus Handling

    /// <summary>
    ///     Gets TabIndexes that are scoped to the specified behavior and direction. If behavior is null, all TabIndexes are
    ///     returned.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="behavior"></param>
    /// <returns></returns>
    /// GetScopedTabIndexes
    internal View [] GetSubviewFocusChain (NavigationDirection direction, TabBehavior? behavior)
    {
        IEnumerable<View>? fitleredSubviews;

        if (behavior.HasValue)
        {
            fitleredSubviews = _subviews?.Where (v => v.TabStop == behavior && v is { CanFocus: true, Visible: true, Enabled: true });
        }
        else
        {
            fitleredSubviews = _subviews?.Where (v => v is { CanFocus: true, Visible: true, Enabled: true });
        }

        if (direction == NavigationDirection.Backward)
        {
            fitleredSubviews = fitleredSubviews?.Reverse ();
        }

        return fitleredSubviews?.ToArray () ?? Array.Empty<View> ();
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
    ///         The default <see cref="TabBehavior.TabStop"/> keys are <see cref="Application.NextTabKey"/> (<c>Key.Tab</c>)
    ///         and <see cref="Application.PrevTabKey"/> (<c>Key>Tab.WithShift</c>).
    ///     </para>
    ///     <para>
    ///         The default <see cref="TabBehavior.TabGroup"/> keys are <see cref="Application.NextTabGroupKey"/> (
    ///         <c>Key.F6</c>) and <see cref="Application.PrevTabGroupKey"/> (<c>Key>Key.F6.WithShift</c>).
    ///     </para>
    /// </remarks>
    public TabBehavior? TabStop
    {
        get => _tabStop;
        set
        {
            if (_tabStop is { } && _tabStop == value)
            {
                return;
            }

            _tabStop = value;
        }
    }

    #endregion Tab/Focus Handling
}
