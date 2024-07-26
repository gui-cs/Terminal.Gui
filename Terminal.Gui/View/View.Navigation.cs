using System.Diagnostics;

namespace Terminal.Gui;

public partial class View // Focus and cross-view navigation management (TabStop, TabIndex, etc...)
{
    /// <summary>Returns a value indicating if this View is currently on Top (Active)</summary>
    public bool IsCurrentTop => Application.Current == this;

    // BUGBUG: The focus API is poorly defined and implemented. It deeply intertwines the view hierarchy with the tab order.

    /// <summary>Invoked when this view is gaining focus (entering).</summary>
    /// <param name="leavingView">The view that is leaving focus.</param>
    /// <returns> <see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    /// <remarks>
    ///     <para>
    ///         Overrides must call the base class method to ensure that the <see cref="Enter"/> event is raised. If the event
    ///         is handled, the method should return <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public virtual bool OnEnter (View leavingView)
    {
        var args = new FocusEventArgs (leavingView, this);
        Enter?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        return false;
    }

    /// <summary>Invoked when this view is losing focus (leaving).</summary>
    /// <param name="enteringView">The view that is entering focus.</param>
    /// <returns> <see langword="true"/>, if the event was handled, <see langword="false"/> otherwise.</returns>
    /// <remarks>
    ///     <para>
    ///         Overrides must call the base class method to ensure that the <see cref="Leave"/> event is raised. If the event
    ///         is handled, the method should return <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public virtual bool OnLeave (View enteringView)
    {
        var args = new FocusEventArgs (this, enteringView);
        Leave?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        return false;
    }

    /// <summary>Raised when the view is gaining (entering) focus. Can be cancelled.</summary>
    /// <remarks>
    ///     Raised by the <see cref="OnEnter"/> virtual method.
    /// </remarks>
    public event EventHandler<FocusEventArgs> Enter;

    /// <summary>Raised when the view is losing (leaving) focus. Can be cancelled.</summary>
    /// <remarks>
    ///     Raised by the <see cref="OnLeave"/> virtual method.
    /// </remarks>
    public event EventHandler<FocusEventArgs> Leave;

    private NavigationDirection _focusDirection;

    /// <summary>
    ///     INTERNAL API that gets or sets the focus direction for this view and all subviews.
    ///     Setting this property will set the focus direction for all views up the SuperView hierarchy.
    /// </summary>
    internal NavigationDirection FocusDirection
    {
        get => SuperView?.FocusDirection ?? _focusDirection;
        set
        {
            if (SuperView is { })
            {
                SuperView.FocusDirection = value;
            }
            else
            {
                _focusDirection = value;
            }
        }
    }

    private bool _hasFocus;

    /// <summary>
    ///     Gets or sets whether this view has focus.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Causes the <see cref="OnEnter"/> and <see cref="OnLeave"/> virtual methods (and <see cref="Enter"/> and
    ///         <see cref="Leave"/> events to be raised) when the value changes.
    ///     </para>
    ///     <para>
    ///         Setting this property to <see langword="false"/> will recursively set <see cref="HasFocus"/> to
    ///         <see langword="false"/>
    ///         for any focused subviews.
    ///     </para>
    /// </remarks>
    public bool HasFocus
    {
        // Force the specified view to have focus
        set => SetHasFocus (value, this, true);
        get => _hasFocus;
    }

    /// <summary>
    ///     Internal API that sets <see cref="HasFocus"/>. This method is called by <c>HasFocus_set</c> and other methods that
    ///     need to set or remove focus from a view.
    /// </summary>
    /// <param name="newHasFocus">The new setting for <see cref="HasFocus"/>.</param>
    /// <param name="view">The view that will be gaining or losing focus.</param>
    /// <param name="force">
    ///     <see langword="true"/> to force Enter/Leave on <paramref name="view"/> regardless of whether it
    ///     already HasFocus or not.
    /// </param>
    /// <remarks>
    ///     If <paramref name="newHasFocus"/> is <see langword="false"/> and there is a focused subview (<see cref="Focused"/>
    ///     is not <see langword="null"/>),
    ///     this method will recursively remove focus from any focused subviews of <see cref="Focused"/>.
    /// </remarks>
    private void SetHasFocus (bool newHasFocus, View view, bool force = false)
    {
        if (HasFocus != newHasFocus || force)
        {
            _hasFocus = newHasFocus;

            if (newHasFocus)
            {
                OnEnter (view);
            }
            else
            {
                OnLeave (view);
            }

            SetNeedsDisplay ();
        }

        // Remove focus down the chain of subviews if focus is removed
        if (!newHasFocus && Focused is { })
        {
            View f = Focused;
            f.OnLeave (view);
            f.SetHasFocus (false, view);
            Focused = null;
        }
    }

    // BUGBUG: This is a poor API design. Automatic behavior like this is non-obvious and should be avoided. Instead, callers to Add should be explicit about what they want.
    // Set to true in Add() to indicate that the view being added to a SuperView has CanFocus=true.
    // Makes it so CanFocus will update the SuperView's CanFocus property.
    internal bool _addingViewSoCanFocusAlsoUpdatesSuperView;

    // Used to cache CanFocus on subviews when CanFocus is set to false so that it can be restored when CanFocus is changed back to true
    private bool _oldCanFocus;

    private bool _canFocus;

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> can be focused.</summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="SuperView"/> must also have <see cref="CanFocus"/> set to <see langword="true"/>.
    ///     </para>
    ///     <para>
    ///         When set to <see langword="false"/>, if this view is focused, the focus will be set to the next focusable view.
    ///     </para>
    ///     <para>
    ///         When set to <see langword="false"/>, the <see cref="TabIndex"/> will be set to -1.
    ///     </para>
    ///     <para>
    ///         When set to <see langword="false"/>, the values of <see cref="CanFocus"/> and <see cref="TabIndex"/> for all
    ///         subviews will be cached so that when <see cref="CanFocus"/> is set back to <see langword="true"/>, the subviews
    ///         will be restored to their previous values.
    ///     </para>
    /// </remarks>
    public bool CanFocus
    {
        get => _canFocus;
        set
        {
            if (!_addingViewSoCanFocusAlsoUpdatesSuperView && IsInitialized && SuperView?.CanFocus == false && value)
            {
                throw new InvalidOperationException ("Cannot set CanFocus to true if the SuperView CanFocus is false!");
            }

            if (_canFocus == value)
            {
                return;
            }

            _canFocus = value;

            switch (_canFocus)
            {
                case false when _tabIndex > -1:
                    TabIndex = -1;

                    break;

                case true when SuperView?.CanFocus == false && _addingViewSoCanFocusAlsoUpdatesSuperView:
                    SuperView.CanFocus = true;

                    break;
            }

            if (_canFocus && _tabIndex == -1)
            {
                TabIndex = SuperView is { } ? SuperView._tabIndexes.IndexOf (this) : -1;
            }

            TabStop = _canFocus;

            if (!_canFocus && SuperView?.Focused == this)
            {
                SuperView.Focused = null;
            }

            if (!_canFocus && HasFocus)
            {
                SetHasFocus (false, this);
                SuperView?.FocusFirstOrLast ();

                // If EnsureFocus () didn't set focus to a view, focus the next focusable view in the application
                if (SuperView is { Focused: null })
                {
                    SuperView.AdvanceFocus (NavigationDirection.Forward);

                    if (SuperView.Focused is null && Application.Current is { })
                    {
                        Application.Current.AdvanceFocus (NavigationDirection.Forward);
                    }

                    ApplicationOverlapped.BringOverlappedTopToFront ();
                }
            }

            if (_subviews is { } && IsInitialized)
            {
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
                            view._tabIndex = -1;
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

    /// <summary>Invoked when the <see cref="CanFocus"/> property from a view is changed.</summary>
    /// <remarks>
    ///     Raises the <see cref="CanFocusChanged"/> event.
    /// </remarks>
    public virtual void OnCanFocusChanged () { CanFocusChanged?.Invoke (this, EventArgs.Empty); }

    /// <summary>Returns the currently focused Subview inside this view, or <see langword="null"/> if nothing is focused.</summary>
    /// <value>The currently focused Subview.</value>
    public View Focused { get; private set; }

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

    /// <summary>
    ///     Internal API that causes <paramref name="viewToEnterFocus"/> to enter focus.
    ///     <paramref name="viewToEnterFocus"/> does not need to be a subview.
    ///     Recursively sets focus upwards in the view hierarchy.
    /// </summary>
    /// <param name="viewToEnterFocus"></param>
    private void SetFocus (View viewToEnterFocus)
    {
        if (viewToEnterFocus is null)
        {
            return;
        }

        if (!viewToEnterFocus.CanFocus || !viewToEnterFocus.Visible || !viewToEnterFocus.Enabled)
        {
            return;
        }

        // If viewToEnterFocus is already the focused view, don't do anything
        if (Focused?._hasFocus == true && Focused == viewToEnterFocus)
        {
            return;
        }

        // If a subview has focus and viewToEnterFocus is the focused view's superview OR viewToEnterFocus is this view,
        // then make viewToEnterFocus.HasFocus = true and return
        if ((Focused?._hasFocus == true && Focused?.SuperView == viewToEnterFocus) || viewToEnterFocus == this)
        {
            if (!viewToEnterFocus._hasFocus)
            {
                viewToEnterFocus._hasFocus = true;
            }

            return;
        }

        // Make sure that viewToEnterFocus is a subview of this view
        View c;

        for (c = viewToEnterFocus._superView; c != null; c = c._superView)
        {
            if (c == this)
            {
                break;
            }
        }

        if (c is null)
        {
            throw new ArgumentException (@$"The specified view {viewToEnterFocus} is not part of the hierarchy of {this}.");
        }

        // If a subview has focus, make it leave focus
        Focused?.SetHasFocus (false, viewToEnterFocus);

        // make viewToEnterFocus Focused and enter focus
        View f = Focused;
        Focused = viewToEnterFocus;
        Focused.SetHasFocus (true, f);

        // Ensure on either the first or last focusable subview of Focused
        Focused.FocusFirstOrLast ();

        // Recursively set focus upwards in the view hierarchy
        if (SuperView is { })
        {
            SuperView.SetFocus (this);
        }
        else
        {
            // If there is no SuperView, then this is a top-level view
            SetFocus (this);
        }
    }

    /// <summary>
    ///     Causes this view to be focused. All focusable views up the Superview hierarchy will also be focused.
    /// </summary>
    public void SetFocus ()
    {
        if (!CanBeVisible (this) || !Enabled)
        {
            if (HasFocus)
            {
                // If this view is focused, make it leave focus
                SetHasFocus (false, this);
            }

            return;
        }

        // Recursively set focus upwards in the view hierarchy
        if (SuperView is { })
        {
            SuperView.SetFocus (this);
        }
        else
        {
            SetFocus (this);
        }
    }

    /// <summary>
    ///     INTERNAL helper for calling <see cref="FocusFirst"/> or <see cref="FocusLast"/> based on
    ///     <see cref="FocusDirection"/>.
    ///     FocusDirection is not public. This API is thus non-deterministic from a public API perspective.
    /// </summary>
    internal void FocusFirstOrLast ()
    {
        if (Focused is null && _subviews?.Count > 0)
        {
            if (FocusDirection == NavigationDirection.Forward)
            {
                FocusFirst ();
            }
            else
            {
                FocusLast ();
            }
        }
    }

    /// <summary>
    ///     Focuses the first focusable view in <see cref="View.TabIndexes"/> if one exists. If there are no views in
    ///     <see cref="View.TabIndexes"/> then the focus is set to the view itself.
    /// </summary>
    /// <param name="overlappedOnly">
    ///     If <see langword="true"/>, only subviews where <see cref="Arrangement"/> has
    ///     <see cref="ViewArrangement.Overlapped"/> set
    ///     will be considered.
    /// </param>
    public void FocusFirst (bool overlappedOnly = false)
    {
        if (!CanBeVisible (this))
        {
            return;
        }

        if (_tabIndexes is null)
        {
            SuperView?.SetFocus (this);

            return;
        }

        foreach (View view in _tabIndexes.Where (v => !overlappedOnly || v.Arrangement.HasFlag (ViewArrangement.Overlapped)))
        {
            if (view.CanFocus && view._tabStop && view.Visible && view.Enabled)
            {
                SetFocus (view);

                return;
            }
        }
    }

    /// <summary>
    ///     Focuses the last focusable view in <see cref="View.TabIndexes"/> if one exists. If there are no views in
    ///     <see cref="View.TabIndexes"/> then the focus is set to the view itself.
    /// </summary>
    /// <param name="overlappedOnly">
    ///     If <see langword="true"/>, only subviews where <see cref="Arrangement"/> has
    ///     <see cref="ViewArrangement.Overlapped"/> set
    ///     will be considered.
    /// </param>
    public void FocusLast (bool overlappedOnly = false)
    {
        if (!CanBeVisible (this))
        {
            return;
        }

        if (_tabIndexes is null)
        {
            SuperView?.SetFocus (this);

            return;
        }

        foreach (View view in _tabIndexes.Where (v => !overlappedOnly || v.Arrangement.HasFlag (ViewArrangement.Overlapped)).Reverse ())
        {
            if (view.CanFocus && view._tabStop && view.Visible && view.Enabled)
            {
                SetFocus (view);

                return;
            }
        }
    }

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
    /// <returns>
    ///     <see langword="true"/> if focus was changed to another subview (or stayed on this one), <see langword="false"/>
    ///     otherwise.
    /// </returns>
    public bool AdvanceFocus (NavigationDirection direction)
    {
        if (!CanBeVisible (this))
        {
            return false;
        }

        FocusDirection = direction;

        if (TabIndexes is null || TabIndexes.Count == 0)
        {
            return false;
        }

        if (Focused is null)
        {
            switch (direction)
            {
                case NavigationDirection.Forward:
                    FocusFirst ();

                    break;
                case NavigationDirection.Backward:
                    FocusLast ();

                    break;
                default:
                    throw new ArgumentOutOfRangeException (nameof (direction), direction, null);
            }

            return Focused is { };
        }

        var focusedFound = false;

        foreach (View w in direction == NavigationDirection.Forward
                               ? TabIndexes.ToArray ()
                               : TabIndexes.ToArray ().Reverse ())
        {
            if (w.HasFocus)
            {
                // A subview has focus, tell *it* to FocusNext
                if (w.AdvanceFocus (direction))
                {
                    // The subview changed which of it's subviews had focus
                    return true;
                }

                Debug.Assert (w.HasFocus);

                // The subview has no subviews that can be next. Cache that we found a focused subview.
                focusedFound = true;

                continue;
            }

            // The subview does not have focus, but at least one other that can. Can this one be focused?
            if (focusedFound && w.CanFocus && w._tabStop && w.Visible && w.Enabled)
            {
                // Make Focused Leave
                Focused.SetHasFocus (false, w);

                //// If the focused view is overlapped don't focus on the next if it's not overlapped.
                //if (Focused.Arrangement.HasFlag (ViewArrangement.Overlapped)/* && !w.Arrangement.HasFlag (ViewArrangement.Overlapped)*/)
                //{
                //    return false;
                //}

                //// If the focused view is not overlapped and the next is, skip it
                //if (!Focused.Arrangement.HasFlag (ViewArrangement.Overlapped) && w.Arrangement.HasFlag (ViewArrangement.Overlapped))
                //{
                //    continue;
                //}

                switch (direction)
                {
                    case NavigationDirection.Forward:
                        w.FocusFirst ();

                        break;
                    case NavigationDirection.Backward:
                        w.FocusLast ();

                        break;
                }

                SetFocus (w);

                return true;
            }
        }

        if (Focused is { })
        {
            // Leave
            Focused.SetHasFocus (false, this);

            // Signal that nothing is focused, and callers should try a peer-subview
            Focused = null;
        }

        return false;
    }

    #region Tab/Focus Handling

    private List<View> _tabIndexes;

    // TODO: This should be a get-only property?
    // BUGBUG: This returns an AsReadOnly list, but isn't declared as such.
    /// <summary>Gets a list of the subviews that are a <see cref="TabStop"/>.</summary>
    /// <value>The tabIndexes.</value>
    public IList<View> TabIndexes => _tabIndexes?.AsReadOnly () ?? _empty;

    // TODO: Change this to int? and use null to indicate the view is not in the tab order.
    private int _tabIndex = -1;
    private int _oldTabIndex;

    /// <summary>
    ///     Indicates the index of the current <see cref="View"/> from the <see cref="TabIndexes"/> list. See also:
    ///     <seealso cref="TabStop"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the value is -1, the view is not part of the tab order.
    ///     </para>
    ///     <para>
    ///         On set, if <see cref="CanFocus"/> is <see langword="false"/>, <see cref="TabIndex"/> will be set to -1.
    ///     </para>
    ///     <para>
    ///         On set, if <see cref="SuperView"/> is <see langword="null"/> or has not TabStops, <see cref="TabIndex"/> will
    ///         be set to 0.
    ///     </para>
    ///     <para>
    ///         On set, if <see cref="SuperView"/> has only one TabStop, <see cref="TabIndex"/> will be set to 0.
    ///     </para>
    /// </remarks>
    public int TabIndex
    {
        get => _tabIndex;
        set
        {
            if (!CanFocus)
            {
                // BUGBUG: Property setters should set the property to the value passed in and not have side effects.
                _tabIndex = -1;

                return;
            }

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
            _tabIndex = GetGreatestTabIndexInSuperView (_tabIndex);

            if (SuperView._tabIndexes.IndexOf (this) != _tabIndex)
            {
                // BUGBUG: we have to use _tabIndexes and not TabIndexes because TabIndexes returns is a read-only version of _tabIndexes
                SuperView._tabIndexes.Remove (this);
                SuperView._tabIndexes.Insert (_tabIndex, this);
                ReorderSuperViewTabIndexes ();
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
        var i = 0;

        foreach (View superViewTabStop in SuperView._tabIndexes)
        {
            if (superViewTabStop._tabIndex == -1 || superViewTabStop == this)
            {
                continue;
            }

            i++;
        }

        return Math.Min (i, idx);
    }

    /// <summary>
    ///     Re-orders the <see cref="TabIndex"/>s of the views in the <see cref="SuperView"/>'s <see cref="TabIndexes"/>.
    /// </summary>
    private void ReorderSuperViewTabIndexes ()
    {
        var i = 0;

        foreach (View superViewTabStop in SuperView._tabIndexes)
        {
            if (superViewTabStop._tabIndex == -1)
            {
                continue;
            }

            superViewTabStop._tabIndex = i;
            i++;
        }
    }

    private bool _tabStop = true;

    /// <summary>
    ///     Gets or sets whether the view is a stop-point for keyboard navigation of focus. Will be <see langword="true"/>
    ///     only if <see cref="CanFocus"/> is <see langword="true"/>. Set to <see langword="false"/> to prevent the
    ///     view from being a stop-point for keyboard navigation.
    /// </summary>
    /// <remarks>
    ///     The default keyboard navigation keys are <c>Key.Tab</c> and <c>Key>Tab.WithShift</c>. These can be changed by
    ///     modifying the key bindings (see <see cref="KeyBindings.Add(Key, Command[])"/>) of the SuperView.
    /// </remarks>
    public bool TabStop
    {
        get => _tabStop;
        set
        {
            if (_tabStop == value)
            {
                return;
            }

            _tabStop = CanFocus && value;
        }
    }

    #endregion Tab/Focus Handling
}
