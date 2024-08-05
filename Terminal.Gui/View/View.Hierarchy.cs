using System.Diagnostics;

namespace Terminal.Gui;

public partial class View // SuperView/SubView hierarchy management (SuperView, SubViews, Add, Remove, etc.)
{
    private static readonly IList<View> _empty = new List<View> (0).AsReadOnly ();
    private List<View> _subviews; // This is null, and allocated on demand.
    private View _superView;

    /// <summary>Indicates whether the view was added to <see cref="SuperView"/>.</summary>
    public bool IsAdded { get; private set; }

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

        Debug.WriteLineIf (_subviews.Contains (view), $"BUGBUG: {view} has already been added to {this}.");
        _subviews.Add (view);
        _tabIndexes.Add (view);
        view._superView = this;

        if (view.CanFocus)
        {
            // BUGBUG: This is a poor API design. Automatic behavior like this is non-obvious and should be avoided. Instead, callers to Add should be explicit about what they want.
            _addingViewSoCanFocusAlsoUpdatesSuperView = true;

            if (SuperView?.CanFocus == false)
            {
                SuperView._addingViewSoCanFocusAlsoUpdatesSuperView = true;
                SuperView.CanFocus = true;
                SuperView._addingViewSoCanFocusAlsoUpdatesSuperView = false;
            }

            // QUESTION: This automatic behavior of setting CanFocus to true on the SuperView is not documented, and is annoying.
            CanFocus = true;
            view._tabIndex = _tabIndexes.IndexOf (view);
            _addingViewSoCanFocusAlsoUpdatesSuperView = false;
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
        //view._tabIndex = -1;
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


    /// <summary>Moves <paramref name="subview"/> one position towards the start of the <see cref="Subviews"/> list</summary>
    /// <param name="subview">The subview to move forward.</param>
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

    /// <summary>Moves <paramref name="subview"/> to the start of the <see cref="Subviews"/> list.</summary>
    /// <param name="subview">The subview to send to the start.</param>
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


    /// <summary>Moves <paramref name="subview"/> one position towards the end of the <see cref="Subviews"/> list</summary>
    /// <param name="subview">The subview to move backwards.</param>
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

    /// <summary>Moves <paramref name="subview"/> to the end of the <see cref="Subviews"/> list.</summary>
    /// <param name="subview">The subview to send to the end.</param>
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

    /// <summary>
    ///     Internal API that runs <paramref name="action"/> on a subview if it is part of the <see cref="Subviews"/> list.
    /// </summary>
    /// <param name="subview"></param>
    /// <param name="action"></param>
    private void PerformActionForSubview (View subview, Action<View> action)
    {
        if (_subviews.Contains (subview))
        {
            action (subview);
        }

        // BUGBUG: this is odd. Why is this needed?
        SetNeedsDisplay ();
        subview.SetNeedsDisplay ();
    }

}
