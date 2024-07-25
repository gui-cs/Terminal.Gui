using System.Diagnostics;

namespace Terminal.Gui;

public partial class View
{
    private static readonly IList<View> _empty = new List<View> (0).AsReadOnly ();
    internal bool _addingView;
    private List<View> _subviews; // This is null, and allocated on demand.
    private View _superView;

    /// <summary>Indicates whether the view was added to <see cref="SuperView"/>.</summary>
    public bool IsAdded { get; private set; }

    /// <summary>Returns a value indicating if this View is currently on Top (Active)</summary>
    public bool IsCurrentTop => Application.Current == this;

    /// <summary>This returns a list of the subviews contained by this view.</summary>
    /// <value>The subviews.</value>
    public IList<View> Subviews => _subviews?.AsReadOnly () ?? _empty;

    /// <summary>Returns the container for this view, or null if this view has not been added to a container.</summary>
    /// <value>The super view.</value>
    public virtual View SuperView
    {
        get => _superView;
        set => throw new NotImplementedException ();
    }

    // Internally, we use InternalSubviews rather than subviews, as we do not expect us
    // to make the same mistakes our users make when they poke at the Subviews.
    internal IList<View> InternalSubviews => _subviews ?? _empty;

    /// <summary>Adds a subview (child) to this view.</summary>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. See also
    ///         <seealso cref="Remove(View)"/> <seealso cref="RemoveAll"/>
    ///     </para>
    ///     <para>
    ///         Subviews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    /// </remarks>
    /// <param name="view">The view to add.</param>
    /// <returns>The view that was added.</returns>
    public virtual View Add (View view)
    {
        if (view is null)
        {
            return view;
        }

        if (_subviews is null)
        {
            _subviews = new ();
        }

        if (_tabIndexes is null)
        {
            _tabIndexes = new ();
        }

        _subviews.Add (view);
        _tabIndexes.Add (view);
        view._superView = this;

        if (view.CanFocus)
        {
            _addingView = true;

            if (SuperView?.CanFocus == false)
            {
                SuperView._addingView = true;
                SuperView.CanFocus = true;
                SuperView._addingView = false;
            }

            // QUESTION: This automatic behavior of setting CanFocus to true on the SuperView is not documented, and is annoying.
            CanFocus = true;
            view._tabIndex = _tabIndexes.IndexOf (view);
            _addingView = false;
        }

        if (view.Enabled && !Enabled)
        {
            view._oldEnabled = true;
            view.Enabled = false;
        }

        OnAdded (new (this, view));

        if (IsInitialized && !view.IsInitialized)
        {
            view.BeginInit ();
            view.EndInit ();
        }

        CheckDimAuto ();
        SetNeedsLayout ();
        SetNeedsDisplay ();

        return view;
    }

    /// <summary>Adds the specified views (children) to the view.</summary>
    /// <param name="views">Array of one or more views (can be optional parameter).</param>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="Subviews"/> property. See also
    ///         <seealso cref="Remove(View)"/> and <seealso cref="RemoveAll"/>.
    ///     </para>
    ///     <para>
    ///         Subviews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    /// </remarks>
    public void Add (params View [] views)
    {
        if (views is null)
        {
            return;
        }

        foreach (View view in views)
        {
            Add (view);
        }
    }

    /// <summary>Event fired when this view is added to another.</summary>
    public event EventHandler<SuperViewChangedEventArgs> Added;

    /// <summary>Moves the subview backwards in the hierarchy, only one step</summary>
    /// <param name="subview">The subview to send backwards</param>
    /// <remarks>If you want to send the view all the way to the back use SendSubviewToBack.</remarks>
    public void BringSubviewForward (View subview)
    {
        PerformActionForSubview (
                                 subview,
                                 x =>
                                 {
                                     int idx = _subviews.IndexOf (x);

                                     if (idx + 1 < _subviews.Count)
                                     {
                                         _subviews.Remove (x);
                                         _subviews.Insert (idx + 1, x);
                                     }
                                 }
                                );
    }

    /// <summary>Brings the specified subview to the front so it is drawn on top of any other views.</summary>
    /// <param name="subview">The subview to send to the front</param>
    /// <remarks><seealso cref="SendSubviewToBack"/>.</remarks>
    public void BringSubviewToFront (View subview)
    {
        PerformActionForSubview (
                                 subview,
                                 x =>
                                 {
                                     _subviews.Remove (x);
                                     _subviews.Add (x);
                                 }
                                );
    }

    /// <summary>Get the top superview of a given <see cref="View"/>.</summary>
    /// <returns>The superview view.</returns>
    public View GetTopSuperView (View view = null, View superview = null)
    {
        View top = superview ?? Application.Top;

        for (View v = view?.SuperView ?? this?.SuperView; v != null; v = v.SuperView)
        {
            top = v;

            if (top == superview)
            {
                break;
            }
        }

        return top;
    }

    /// <summary>Method invoked when a subview is being added to this view.</summary>
    /// <param name="e">Event where <see cref="ViewEventArgs.View"/> is the subview being added.</param>
    public virtual void OnAdded (SuperViewChangedEventArgs e)
    {
        View view = e.Child;
        view.IsAdded = true;
        view.OnResizeNeeded ();
        view.Added?.Invoke (this, e);
    }

    /// <summary>Method invoked when a subview is being removed from this view.</summary>
    /// <param name="e">Event args describing the subview being removed.</param>
    public virtual void OnRemoved (SuperViewChangedEventArgs e)
    {
        View view = e.Child;
        view.IsAdded = false;
        view.Removed?.Invoke (this, e);
    }

    /// <summary>Removes a subview added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.</summary>
    /// <remarks>
    ///     <para>
    ///         Normally Subviews will be disposed when this View is disposed. Removing a Subview causes ownership of the
    ///         Subview's
    ///         lifecycle to be transferred to the caller; the caller muse call <see cref="Dispose"/>.
    ///     </para>
    /// </remarks>
    public virtual View Remove (View view)
    {
        if (view is null || _subviews is null)
        {
            return view;
        }

        Rectangle touched = view.Frame;
        _subviews.Remove (view);
        _tabIndexes.Remove (view);
        view._superView = null;
        view._tabIndex = -1;
        SetNeedsLayout ();
        SetNeedsDisplay ();

        foreach (View v in _subviews)
        {
            if (v.Frame.IntersectsWith (touched))
            {
                view.SetNeedsDisplay ();
            }
        }

        OnRemoved (new (this, view));

        if (Focused == view)
        {
            Focused = null;
        }

        return view;
    }

    /// <summary>
    ///     Removes all subviews (children) added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Normally Subviews will be disposed when this View is disposed. Removing a Subview causes ownership of the
    ///         Subview's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose"/> on any Views that were
    ///         added.
    ///     </para>
    /// </remarks>
    public virtual void RemoveAll ()
    {
        if (_subviews is null)
        {
            return;
        }

        while (_subviews.Count > 0)
        {
            Remove (_subviews [0]);
        }
    }

    /// <summary>Event fired when this view is removed from another.</summary>
    public event EventHandler<SuperViewChangedEventArgs> Removed;

    /// <summary>Moves the subview backwards in the hierarchy, only one step</summary>
    /// <param name="subview">The subview to send backwards</param>
    /// <remarks>If you want to send the view all the way to the back use SendSubviewToBack.</remarks>
    public void SendSubviewBackwards (View subview)
    {
        PerformActionForSubview (
                                 subview,
                                 x =>
                                 {
                                     int idx = _subviews.IndexOf (x);

                                     if (idx > 0)
                                     {
                                         _subviews.Remove (x);
                                         _subviews.Insert (idx - 1, x);
                                     }
                                 }
                                );
    }

    /// <summary>Sends the specified subview to the front so it is the first view drawn</summary>
    /// <param name="subview">The subview to send to the front</param>
    /// <remarks><seealso cref="BringSubviewToFront(View)"/>.</remarks>
    public void SendSubviewToBack (View subview)
    {
        PerformActionForSubview (
                                 subview,
                                 x =>
                                 {
                                     _subviews.Remove (x);
                                     _subviews.Insert (0, subview);
                                 }
                                );
    }

    private void PerformActionForSubview (View subview, Action<View> action)
    {
        if (_subviews.Contains (subview))
        {
            action (subview);
        }

        SetNeedsDisplay ();
        subview.SetNeedsDisplay ();
    }

    #region Focus

    /// <summary>Exposed as `internal` for unit tests. Indicates focus navigation direction.</summary>
    internal enum NavigationDirection
    {
        /// <summary>Navigate forward.</summary>
        Forward,

        /// <summary>Navigate backwards.</summary>
        Backward
    }

    /// <summary>Event fired when the view gets focus.</summary>
    public event EventHandler<FocusEventArgs> Enter;

    /// <summary>Event fired when the view looses focus.</summary>
    public event EventHandler<FocusEventArgs> Leave;

    private NavigationDirection _focusDirection;

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

    /// <inheritdoc/>
    public bool HasFocus
    {
        set => SetHasFocus (value, this, true);
        get => _hasFocus;
    }

    private void SetHasFocus (bool value, View view, bool force = false)
    {
        if (HasFocus != value || force)
        {
            _hasFocus = value;

            if (value)
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
        if (!value && Focused is { })
        {
            View f = Focused;
            f.OnLeave (view);
            f.SetHasFocus (false, view);
            Focused = null;
        }
    }

    /// <summary>Event fired when the <see cref="CanFocus"/> value is being changed.</summary>
    public event EventHandler CanFocusChanged;

    /// <summary>Method invoked when the <see cref="CanFocus"/> property from a view is changed.</summary>
    public virtual void OnCanFocusChanged () { CanFocusChanged?.Invoke (this, EventArgs.Empty); }

    private bool _oldCanFocus;
    private bool _canFocus;

    /// <summary>Gets or sets a value indicating whether this <see cref="View"/> can focus.</summary>
    public bool CanFocus
    {
        get => _canFocus;
        set
        {
            if (!_addingView && IsInitialized && SuperView?.CanFocus == false && value)
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
                case true when SuperView?.CanFocus == false && _addingView:
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
                SuperView?.EnsureFocus ();

                if (SuperView is { Focused: null })
                {
                    SuperView.FocusNext ();

                    if (SuperView.Focused is null && Application.Current is { })
                    {
                        Application.Current.FocusNext ();
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
                            view._oldCanFocus = view.CanFocus;
                            view._oldTabIndex = view._tabIndex;
                            view.CanFocus = false;
                            view._tabIndex = -1;
                        }
                        else
                        {
                            if (_addingView)
                            {
                                view._addingView = true;
                            }

                            view.CanFocus = view._oldCanFocus;
                            view._tabIndex = view._oldTabIndex;
                            view._addingView = false;
                        }
                    }
                }

                if (this is Toplevel && Application.Current.Focused != this)
                {
                    ApplicationOverlapped.BringOverlappedTopToFront ();
                }
            }

            OnCanFocusChanged ();
            SetNeedsDisplay ();
        }
    }

    /// <summary>
    /// Called when a view gets focus.
    /// </summary>
    /// <param name="view">The view that is losing focus.</param>
    /// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
    public virtual bool OnEnter (View view)
    {
        var args = new FocusEventArgs (view, this);
        Enter?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        return false;
    }

    /// <summary>Method invoked when a view loses focus.</summary>
    /// <param name="view">The view that is getting focus.</param>
    /// <returns><c>true</c>, if the event was handled, <c>false</c> otherwise.</returns>
    public virtual bool OnLeave (View view)
    {
        var args = new FocusEventArgs (this, view);
        Leave?.Invoke (this, args);

        if (args.Handled)
        {
            return true;
        }

        return false;
    }

    // BUGBUG: This API is poorly defined and implemented. It does not specify what it means if THIS view is focused and has no subviews.
    /// <summary>Returns the currently focused Subview inside this view, or null if nothing is focused.</summary>
    /// <value>The focused.</value>
    public View Focused { get; private set; }

    // BUGBUG: This API is poorly defined and implemented. It does not specify what it means if THIS view is focused and has no subviews.
    /// <summary>Returns the most focused Subview in the chain of subviews (the leaf view that has the focus).</summary>
    /// <value>The most focused View.</value>
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

    /// <summary>Causes the specified subview to have focus.</summary>
    /// <param name="view">View.</param>
    private void SetFocus (View view)
    {
        if (view is null)
        {
            return;
        }

        //Console.WriteLine ($"Request to focus {view}");
        if (!view.CanFocus || !view.Visible || !view.Enabled)
        {
            return;
        }

        if (Focused?._hasFocus == true && Focused == view)
        {
            return;
        }

        if ((Focused?._hasFocus == true && Focused?.SuperView == view) || view == this)
        {
            if (!view._hasFocus)
            {
                view._hasFocus = true;
            }

            return;
        }

        // Make sure that this view is a subview
        View c;

        for (c = view._superView; c != null; c = c._superView)
        {
            if (c == this)
            {
                break;
            }
        }

        if (c is null)
        {
            throw new ArgumentException ("the specified view is not part of the hierarchy of this view");
        }

        if (Focused is { })
        {
            Focused.SetHasFocus (false, view);
        }

        View f = Focused;
        Focused = view;
        Focused.SetHasFocus (true, f);
        Focused.EnsureFocus ();

        // Send focus upwards
        if (SuperView is { })
        {
            SuperView.SetFocus (this);
        }
        else
        {
            SetFocus (this);
        }
    }

    /// <summary>Causes this view to be focused and entire Superview hierarchy to have the focused order updated.</summary>
    public void SetFocus ()
    {
        if (!CanBeVisible (this) || !Enabled)
        {
            if (HasFocus)
            {
                SetHasFocus (false, this);
            }

            return;
        }

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
    ///     If there is no focused subview, calls <see cref="FocusFirst"/> or <see cref="FocusLast"/> based on <see cref="FocusDirection"/>. 
    ///     does nothing.
    /// </summary>
    public void EnsureFocus ()
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
    ///     Focuses the last focusable view in <see cref="View.TabIndexes"/> if one exists. If there are no views in <see cref="View.TabIndexes"/> then the focus is set to the view itself.
    /// </summary>
    public void FocusFirst (bool overlapped = false)
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

        foreach (View view in _tabIndexes.Where (v => !overlapped || v.Arrangement.HasFlag (ViewArrangement.Overlapped)))
        {
            if (view.CanFocus && view._tabStop && view.Visible && view.Enabled)
            {
                SetFocus (view);

                return;
            }
        }
    }

    /// <summary>
    ///     Focuses the last focusable view in <see cref="View.TabIndexes"/> if one exists. If there are no views in <see cref="View.TabIndexes"/> then the focus is set to the view itself.
    /// </summary>
    public void FocusLast (bool overlapped = false)
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

        foreach (View view in _tabIndexes.Where (v => !overlapped || v.Arrangement.HasFlag (ViewArrangement.Overlapped)).Reverse ())
        {
            if (view.CanFocus && view._tabStop && view.Visible && view.Enabled)
            {
                SetFocus (view);

                return;
            }
        }
    }

    /// <summary>
    ///     Focuses the previous view in <see cref="View.TabIndexes"/>. If there is no previous view, the focus is set to the view itself.
    /// </summary>
    /// <returns><see langword="true"/> if previous was focused, <see langword="false"/> otherwise.</returns>
    public bool FocusPrev ()
    {
        if (!CanBeVisible (this))
        {
            return false;
        }

        FocusDirection = NavigationDirection.Backward;

        if (TabIndexes is null || TabIndexes.Count == 0)
        {
            return false;
        }

        if (Focused is null)
        {
            FocusLast ();

            return Focused != null;
        }

        int focusedIdx = -1;

        for (int i = TabIndexes.Count; i > 0;)
        {
            i--;
            View w = TabIndexes [i];

            if (w.HasFocus)
            {
                if (w.FocusPrev ())
                {
                    return true;
                }

                focusedIdx = i;

                continue;
            }

            if (w.CanFocus && focusedIdx != -1 && w._tabStop && w.Visible && w.Enabled)
            {
                Focused.SetHasFocus (false, w);

                // If the focused view is overlapped don't focus on the next if it's not overlapped.
                if (Focused.Arrangement.HasFlag (ViewArrangement.Overlapped) && !w.Arrangement.HasFlag (ViewArrangement.Overlapped))
                {
                    return false;
                }

                // If the focused view is not overlapped and the next is, skip it
                if (!Focused.Arrangement.HasFlag (ViewArrangement.Overlapped) && w.Arrangement.HasFlag (ViewArrangement.Overlapped))
                {
                   continue;
                }

                if (w.CanFocus && w._tabStop && w.Visible && w.Enabled)
                {
                    w.FocusLast ();
                }

                SetFocus (w);

                return true;
            }
        }

        // There's no prev view in tab indexes.
        if (Focused is { })
        {
            // Leave Focused
            Focused.SetHasFocus (false, this);

            if (Focused.Arrangement.HasFlag (ViewArrangement.Overlapped))
            {
                FocusLast (true);
                return true;
            }

            // Signal to caller no next view was found
            Focused = null;
        }

        return false;
    }

    /// <summary>
    ///     Focuses the next view in <see cref="View.TabIndexes"/>. If there is no next view, the focus is set to the view itself.
    /// </summary>
    /// <returns><see langword="true"/> if next was focused, <see langword="false"/> otherwise.</returns>
    public bool FocusNext ()
    {
        if (!CanBeVisible (this))
        {
            return false;
        }

        FocusDirection = NavigationDirection.Forward;

        if (TabIndexes is null || TabIndexes.Count == 0)
        {
            return false;
        }

        if (Focused is null)
        {
            FocusFirst ();

            return Focused != null;
        }

        int focusedIdx = -1;

        for (var i = 0; i < TabIndexes.Count; i++)
        {
            View w = TabIndexes [i];

            if (w.HasFocus)
            {
                if (w.FocusNext ())
                {
                    return true;
                }

                focusedIdx = i;

                continue;
            }

            if (w.CanFocus && focusedIdx != -1 && w._tabStop && w.Visible && w.Enabled)
            {
                Focused.SetHasFocus (false, w);

                // If the focused view is overlapped don't focus on the next if it's not overlapped.
                if (Focused.Arrangement.HasFlag (ViewArrangement.Overlapped) && !w.Arrangement.HasFlag (ViewArrangement.Overlapped))
                {
                    return false;
                }

                // If the focused view is not overlapped and the next is, skip it
                if (!Focused.Arrangement.HasFlag (ViewArrangement.Overlapped) && w.Arrangement.HasFlag (ViewArrangement.Overlapped))
                {
                    continue;
                }

                if (w.CanFocus && w._tabStop && w.Visible && w.Enabled)
                {
                    w.FocusFirst ();
                }

                SetFocus (w);

                return true;
            }
        }

        // There's no next view in tab indexes.
        if (Focused is { })
        {
            // Leave Focused
            Focused.SetHasFocus (false, this);

            if (Focused.Arrangement.HasFlag (ViewArrangement.Overlapped))
            {
                FocusFirst (true);
                return true;
            }

            // Signal to caller no next view was found
            Focused = null;
        }

        return false;
    }

    private View GetMostFocused (View view)
    {
        if (view is null)
        {
            return null;
        }

        return view.Focused is { } ? GetMostFocused (view.Focused) : view;
    }

    /// <summary>
    /// Gets or sets the cursor style to be used when the view is focused. The default is <see cref="CursorVisibility.Invisible"/>.
    /// </summary>
    public CursorVisibility CursorVisibility { get; set; } = CursorVisibility.Invisible;

    /// <summary>
    ///     Positions the cursor in the right position based on the currently focused view in the chain.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Views that are focusable should override <see cref="PositionCursor"/> to make sure that the cursor is
    ///         placed in a location that makes sense. Some terminals do not have a way of hiding the cursor, so it can be
    ///         distracting to have the cursor left at the last focused view. So views should make sure that they place the
    ///         cursor in a visually sensible place. The default implementation of <see cref="PositionCursor"/> will place the
    ///         cursor at either the hotkey (if defined) or <c>0,0</c>.
    ///     </para>
    /// </remarks>
    /// <returns>Viewport-relative cursor position. Return <see langword="null"/> to ensure the cursor is not visible.</returns>
    public virtual Point? PositionCursor ()
    {
        if (IsInitialized && CanFocus && HasFocus)
        {
            // By default, position the cursor at the hotkey (if any) or 0, 0.
            Move (TextFormatter.HotKeyPos == -1 ? 0 : TextFormatter.CursorPosition, 0);
        }

        // Returning null will hide the cursor.
        return null;
    }

    #endregion Focus
}
