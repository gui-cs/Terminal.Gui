#nullable enable
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Terminal.Gui.ViewBase;

public partial class View // SuperView/SubView hierarchy management (SuperView, SubViews, Add, Remove, etc.)
{
    [SuppressMessage ("Style", "IDE1006:Naming Styles", Justification = "<Pending>")]
    private static readonly IReadOnlyCollection<View> _empty = [];

    private readonly List<View>? _subviews = [];

    // Internally, we use InternalSubViews rather than subviews, as we do not expect us
    // to make the same mistakes our users make when they poke at the SubViews.
    internal IList<View> InternalSubViews => _subviews ?? [];

    /// <summary>Gets the list of SubViews.</summary>
    /// <remarks>
    ///     Use <see cref="Add(View?)"/> and <see cref="Remove(View?)"/> to add or remove subviews.
    /// </remarks>
    public IReadOnlyCollection<View> SubViews => InternalSubViews?.AsReadOnly () ?? _empty;

    private View? _superView;

    /// <summary>
    ///     Gets this Views SuperView (the View's container), or <see langword="null"/> if this view has not been added as a
    ///     SubView.
    /// </summary>
    /// <seealso cref="OnSuperViewChanged"/>
    /// <seealso cref="SuperViewChanged"/>
    public View? SuperView
    {
        get => _superView!;
        private set => SetSuperView (value);
    }

    private void SetSuperView (View? value)
    {
        if (_superView == value)
        {
            return;
        }

        _superView = value;
        RaiseSuperViewChanged ();
    }

    private void RaiseSuperViewChanged ()
    {
        SuperViewChangedEventArgs args = new (SuperView, this);
        OnSuperViewChanged (args);

        SuperViewChanged?.Invoke (this, args);
    }

    /// <summary>
    ///     Called when the SuperView of this View has changed.
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnSuperViewChanged (SuperViewChangedEventArgs e) { }

    /// <summary>Raised when the SuperView of this View has changed.</summary>
    public event EventHandler<SuperViewChangedEventArgs>? SuperViewChanged;

    #region AddRemove

    /// <summary>Adds a SubView (child) to this view.</summary>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="SubViews"/> property. 
    ///     </para>
    ///     <para>
    ///         To check if a View has been added to this View, compare it's <see cref="SuperView"/> property to this View.
    ///     </para>
    ///     <para>
    ///         SubViews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    ///     <para>
    ///         Calls/Raises the <see cref="OnSubViewAdded"/>/<see cref="SubViewAdded"/> event.
    ///     </para>
    ///     <para>
    ///         The <see cref="OnSuperViewChanged"/>/<see cref="SuperViewChanged"/> event will be raised on the added View.
    ///     </para>
    /// </remarks>
    /// <param name="view">The view to add.</param>
    /// <returns>The view that was added.</returns>
    /// <seealso cref="Remove(View)"/>
    /// <seealso cref="RemoveAll"/>
    /// <seealso cref="OnSubViewAdded"/>
    /// <seealso cref="SubViewAdded"/>

    public virtual View? Add (View? view)
    {
        if (view is null)
        {
            return null;
        }

        //Debug.Assert (view.SuperView is null, $"{view} already has a SuperView: {view.SuperView}.");
        if (view.SuperView is {})
        {
            Logging.Warning ($"{view} already has a SuperView: {view.SuperView}.");
        }

        //Debug.Assert (!InternalSubViews.Contains (view), $"{view} has already been Added to {this}.");
        if (InternalSubViews.Contains (view))
        {
            Logging.Warning ($"{view} has already been Added to {this}.");
        }

        // TileView likes to add views that were previously added and have HasFocus = true. No bueno.
        view.HasFocus = false;

        // TODO: Make this thread safe
        InternalSubViews.Add (view);
        view.SuperView = this;

        if (view is { Enabled: true, Visible: true, CanFocus: true })
        {
            // Add will cause the newly added subview to gain focus if it's focusable
            if (HasFocus)
            {
                view.SetFocus ();
            }
        }

        if (view.Enabled && !Enabled)
        {
            view.Enabled = false;
        }

        // Raise event indicating a subview has been added
        // We do this before Init.
        RaiseSubViewAdded (view);

        if (IsInitialized && !view.IsInitialized)
        {
            view.BeginInit ();
            view.EndInit ();
        }

        SetNeedsDraw ();
        SetNeedsLayout ();

        return view;
    }

    /// <summary>Adds the specified SubView (children) to the view.</summary>
    /// <param name="views">Array of one or more views (can be optional parameter).</param>
    /// <remarks>
    ///     <para>
    ///         The Views that have been added to this view can be retrieved via the <see cref="SubViews"/> property. See also
    ///         <seealso cref="Remove(View)"/> and <seealso cref="RemoveAll"/>.
    ///     </para>
    ///     <para>
    ///         SubViews will be disposed when this View is disposed. In other-words, calling this method causes
    ///         the lifecycle of the subviews to be transferred to this View.
    ///     </para>
    /// </remarks>
    public void Add (params View []? views)
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

    internal void RaiseSubViewAdded (View view)
    {
        OnSubViewAdded (view);
        SubViewAdded?.Invoke (this, new (this, view));
    }

    /// <summary>
    ///     Called when a SubView has been added to this View.
    /// </summary>
    /// <remarks>
    ///     If the SubView has not been initialized, this happens before BeginInit/EndInit is called.
    /// </remarks>
    /// <param name="view"></param>
    protected virtual void OnSubViewAdded (View view) { }

    /// <summary>Raised when a SubView has been added to this View.</summary>
    /// <remarks>
    ///     If the SubView has not been initialized, this happens before BeginInit/EndInit is called.
    /// </remarks>
    public event EventHandler<SuperViewChangedEventArgs>? SubViewAdded;

    /// <summary>Removes a SubView added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.</summary>
    /// <remarks>
    ///     <para>
    ///         Normally SubViews will be disposed when this View is disposed. Removing a SubView causes ownership of the
    ///         SubView's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose()"/>.
    ///     </para>
    ///     <para>
    ///         Calls/Raises the <see cref="OnSubViewRemoved"/>/<see cref="SubViewRemoved"/> event.
    ///     </para>
    ///     <para>
    ///         The <see cref="OnSuperViewChanged"/>/<see cref="SuperViewChanged"/> event will be raised on the removed View.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     The removed View. <see langword="null"/> if the View could not be removed.
    /// </returns>
    /// <seealso cref="OnSubViewRemoved"/>
    /// <seealso cref="SubViewRemoved"/>"/>
    public virtual View? Remove (View? view)
    {
        if (view is null)
        {
            return null;
        }

        if (InternalSubViews.Count == 0)
        {
           return view;
        }

        if (view.SuperView is null)
        {
            Logging.Warning ($"{view} cannot be Removed. SuperView is null.");
        }

        if (view.SuperView != this)
        {
            Logging.Warning ($"{view} cannot be Removed. SuperView is not this ({view.SuperView}.");
        }

        if (!InternalSubViews.Contains (view))
        {
            Logging.Warning ($"{view} cannot be Removed. It has not been added to {this}.");
        }

        Rectangle touched = view.Frame;

        bool hadFocus = view.HasFocus;
        bool couldFocus = view.CanFocus;

        if (hadFocus)
        {
            view.CanFocus = false; // If view had focus, this will ensure it doesn't and it stays that way
        }

        Debug.Assert (!view.HasFocus);

        InternalSubViews.Remove (view);

        // Clean up focus stuff
        _previouslyFocused = null;

        if (view.SuperView is { } && view.SuperView._previouslyFocused == this)
        {
            view.SuperView._previouslyFocused = null;
        }

        view.SuperView = null;

        SetNeedsLayout ();
        SetNeedsDraw ();

        foreach (View v in InternalSubViews)
        {
            if (v.Frame.IntersectsWith (touched))
            {
                view.SetNeedsDraw ();
            }
        }

        view.CanFocus = couldFocus; // Restore to previous value

        if (_previouslyFocused == view)
        {
            _previouslyFocused = null;
        }

        RaiseSubViewRemoved (view);

        return view;
    }

    internal void RaiseSubViewRemoved (View view)
    {
        OnSubViewRemoved (view);
        SubViewRemoved?.Invoke (this, new (this, view));
    }

    /// <summary>
    ///     Called when a SubView has been removed from this View.
    /// </summary>
    /// <param name="view"></param>
    protected virtual void OnSubViewRemoved (View view) { }

    /// <summary>Raised when a SubView has been added to this View.</summary>
    public event EventHandler<SuperViewChangedEventArgs>? SubViewRemoved;

    /// <summary>
    ///     Removes all SubViews added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Normally SubViews will be disposed when this View is disposed. Removing a SubView causes ownership of the
    ///         SubView's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose()"/> on any Views that were
    ///         added.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A list of removed Views.
    /// </returns>
    public virtual IReadOnlyCollection<View> RemoveAll ()
    {
        List<View> removedList = new List<View> ();
        while (InternalSubViews.Count > 0)
        {
            View? removed = Remove (InternalSubViews [0]);
            if (removed is { })
            {
                removedList.Add (removed);
            }
        }

        return removedList.AsReadOnly ();
    }

    /// <summary>
    ///     Removes all SubViews of a type added via <see cref="Add(View)"/> or <see cref="Add(View[])"/> from this View.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Normally SubViews will be disposed when this View is disposed. Removing a SubView causes ownership of the
    ///         SubView's
    ///         lifecycle to be transferred to the caller; the caller must call <see cref="Dispose()"/> on any Views that were
    ///         added.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     A list of removed Views.
    /// </returns>
    public virtual IReadOnlyCollection<TView> RemoveAll<TView> () where TView : View
    {
        List<TView> removedList = new List<TView> ();
        foreach (TView view in InternalSubViews.OfType<TView> ().ToList ())
        {
            Remove (view);
            removedList.Add (view);
        }
        return removedList.AsReadOnly ();
    }

#pragma warning disable CS0067 // The event is never used
    /// <summary>Raised when a SubView has been removed from this View.</summary>
    public event EventHandler<SuperViewChangedEventArgs>? Removed;
#pragma warning restore CS0067 // The event is never used   

    #endregion AddRemove

    // TODO: This drives a weird coupling of Application.Top and View. It's not clear why this is needed.
    /// <summary>Get the top superview of a given <see cref="View"/>.</summary>
    /// <returns>The superview view.</returns>
    internal View? GetTopSuperView (View? view = null, View? superview = null)
    {
        View? top = superview ?? Application.Top;

        for (View? v = view?.SuperView ?? this?.SuperView; v != null; v = v.SuperView)
        {
            top = v;

            if (top == superview)
            {
                break;
            }
        }

        return top;
    }

    /// <summary>
    ///     Gets whether <paramref name="view"/> is in the View hierarchy of <paramref name="start"/>.
    /// </summary>
    /// <param name="start">The View at the start of the hierarchy.</param>
    /// <param name="view">The View to test.</param>
    /// <param name="includeAdornments">Will include all <see cref="Adornment"/>s in addition to Subviews if true.</param>
    /// <returns></returns>
    public static bool IsInHierarchy (View? start, View? view, bool includeAdornments = false)
    {
        if (view is null || start is null)
        {
            return false;
        }

        if (view == start)
        {
            return true;
        }

        foreach (View subView in start.InternalSubViews)
        {
            if (view == subView)
            {
                return true;
            }

            bool found = IsInHierarchy (subView, view, includeAdornments);

            if (found)
            {
                return found;
            }
        }

        if (includeAdornments)
        {
            bool found = IsInHierarchy (start.Padding, view, includeAdornments);

            if (found)
            {
                return found;
            }

            found = IsInHierarchy (start.Border, view, includeAdornments);

            if (found)
            {
                return found;
            }

            found = IsInHierarchy (start.Margin, view, includeAdornments);

            if (found)
            {
                return found;
            }
        }

        return false;
    }

    #region SubViewOrdering

    /// <summary>
    ///     Moves <paramref name="subview"/> one position towards the end of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewTowardsEnd (View subview)
    {
        PerformActionForSubView (
                                 subview,
                                 x =>
                                 {
                                     int idx = InternalSubViews!.IndexOf (x);

                                     if (idx + 1 < InternalSubViews.Count)
                                     {
                                         InternalSubViews.Remove (x);
                                         InternalSubViews.Insert (idx + 1, x);
                                     }
                                 }
                                );
    }

    /// <summary>
    ///     Moves <paramref name="subview"/> to the end of the <see cref="SubViews"/> list.
    ///     If the <see cref="Arrangement"/> is <see cref="ViewArrangement.Overlapped"/>, keeps the original sorting.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewToEnd (View subview)
    {
        if (Arrangement.HasFlag (ViewArrangement.Overlapped))
        {
            PerformActionForSubView (
                                     subview,
                                     x =>
                                     {
                                         while (InternalSubViews!.IndexOf (x) != InternalSubViews.Count - 1)
                                         {
                                             View v = InternalSubViews [0];
                                             InternalSubViews!.Remove (v);
                                             InternalSubViews.Add (v);
                                         }
                                     }
                                    );

            return;
        }

        PerformActionForSubView (
                                 subview,
                                 x =>
                                 {
                                     InternalSubViews!.Remove (x);
                                     InternalSubViews.Add (x);
                                 }
                                );
    }

    /// <summary>
    ///     Moves <paramref name="subview"/> one position towards the start of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewTowardsStart (View subview)
    {
        PerformActionForSubView (
                                 subview,
                                 x =>
                                 {
                                     int idx = InternalSubViews!.IndexOf (x);

                                     if (idx > 0)
                                     {
                                         InternalSubViews.Remove (x);
                                         InternalSubViews.Insert (idx - 1, x);
                                     }
                                 }
                                );
    }

    /// <summary>
    ///     Moves <paramref name="subview"/> to the start of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview">The subview to move.</param>
    public void MoveSubViewToStart (View subview)
    {
        PerformActionForSubView (
                                 subview,
                                 x =>
                                 {
                                     InternalSubViews!.Remove (x);
                                     InternalSubViews.Insert (0, subview);
                                 }
                                );
    }

    /// <summary>
    ///     Internal API that runs <paramref name="action"/> on a subview if it is part of the <see cref="SubViews"/> list.
    /// </summary>
    /// <param name="subview"></param>
    /// <param name="action"></param>
    private void PerformActionForSubView (View subview, Action<View> action)
    {
        if (InternalSubViews.Contains (subview))
        {
            action (subview);
        }

        // BUGBUG: this is odd. Why is this needed?
        SetNeedsDraw ();
        subview.SetNeedsDraw ();
    }

    #endregion SubViewOrdering
}
