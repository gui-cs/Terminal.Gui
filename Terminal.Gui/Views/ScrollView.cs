//
// ScrollView.cs: ScrollView view.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
//
// TODO:
// - focus in scrollview
// - focus handling in scrollview to auto scroll to focused view
// - Raise events
// - Perhaps allow an option to not display the scrollbar arrow indicators?

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Scrollviews are views that present a window into a virtual space where subviews are added.  Similar to the iOS
///     UIScrollView.
/// </summary>
/// <remarks>
///     <para>
///         The subviews that are added to this <see cref="Gui.ScrollView"/> are offset by the
///         <see cref="ContentOffset"/> property.  The view itself is a window into the space represented by the
///         <see cref="View.GetContentSize ()"/>.
///     </para>
///     <para>Use the</para>
/// </remarks>
public class ScrollView : View
{
    private readonly ContentView _contentView;
    private readonly ScrollBarView _horizontal;
    private readonly ScrollBarView _vertical;
    private bool _autoHideScrollBars = true;
    private View _contentBottomRightCorner;
    private Point _contentOffset;
    private bool _keepContentAlwaysInViewport = true;
    private bool _showHorizontalScrollIndicator;
    private bool _showVerticalScrollIndicator;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Gui.ScrollView"/> class.
    /// </summary>
    public ScrollView ()
    {
        _contentView = new ContentView ();

        _vertical = new ScrollBarView
        {
            X = Pos.AnchorEnd (1),
            Y = 0,
            Width = 1,
            Height = Dim.Fill (_showHorizontalScrollIndicator ? 1 : 0),
            Size = 1,
            IsVertical = true,
            Host = this
        };

        _horizontal = new ScrollBarView
        {
            X = 0,
            Y = Pos.AnchorEnd (1),
            Width = Dim.Fill (_showVerticalScrollIndicator ? 1 : 0),
            Height = 1,
            Size = 1,
            IsVertical = false,
            Host = this
        };

        _vertical.OtherScrollBarView = _horizontal;
        _horizontal.OtherScrollBarView = _vertical;
        base.Add (_contentView);
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;

        MouseEnter += View_MouseEnter;
        MouseLeave += View_MouseLeave;
        _contentView.MouseEnter += View_MouseEnter;
        _contentView.MouseLeave += View_MouseLeave;

        Application.UnGrabbedMouse += Application_UnGrabbedMouse;

        // Things this view knows how to do
        AddCommand (Command.ScrollUp, () => ScrollUp (1));
        AddCommand (Command.ScrollDown, () => ScrollDown (1));
        AddCommand (Command.ScrollLeft, () => ScrollLeft (1));
        AddCommand (Command.ScrollRight, () => ScrollRight (1));
        AddCommand (Command.PageUp, () => ScrollUp (Viewport.Height));
        AddCommand (Command.PageDown, () => ScrollDown (Viewport.Height));
        AddCommand (Command.PageLeft, () => ScrollLeft (Viewport.Width));
        AddCommand (Command.PageRight, () => ScrollRight (Viewport.Width));
        AddCommand (Command.Start, () => ScrollUp (GetContentSize ().Height));
        AddCommand (Command.End, () => ScrollDown (GetContentSize ().Height));
        AddCommand (Command.LeftStart, () => ScrollLeft (GetContentSize ().Width));
        AddCommand (Command.RightEnd, () => ScrollRight (GetContentSize ().Width));

        // Default keybindings for this view
        KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
        KeyBindings.Add (Key.CursorDown, Command.ScrollDown);
        KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
        KeyBindings.Add (Key.CursorRight, Command.ScrollRight);

        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.V.WithAlt, Command.PageUp);

        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.V.WithCtrl, Command.PageDown);

        KeyBindings.Add (Key.PageUp.WithCtrl, Command.PageLeft);
        KeyBindings.Add (Key.PageDown.WithCtrl, Command.PageRight);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (Key.Home.WithCtrl, Command.LeftStart);
        KeyBindings.Add (Key.End.WithCtrl, Command.RightEnd);

        Initialized += (s, e) =>
                       {
                           if (!_vertical.IsInitialized)
                           {
                               _vertical.BeginInit ();
                               _vertical.EndInit ();
                           }

                           if (!_horizontal.IsInitialized)
                           {
                               _horizontal.BeginInit ();
                               _horizontal.EndInit ();
                           }

                           SetContentOffset (_contentOffset);
                           _contentView.Frame = new Rectangle (ContentOffset, GetContentSize ());

                           // PERF: How about calls to Point.Offset instead?
                           _vertical.ChangedPosition += delegate { ContentOffset = new Point (ContentOffset.X, _vertical.Position); };
                           _horizontal.ChangedPosition += delegate { ContentOffset = new Point (_horizontal.Position, ContentOffset.Y); };
                       };
        ContentSizeChanged += ScrollViewContentSizeChanged;
    }

    private void ScrollViewContentSizeChanged (object sender, SizeChangedEventArgs e)
    {
        if (e.Size is null)
        {
            return;
        }
        _contentView.Frame = new Rectangle (ContentOffset, e.Size.Value with { Width = e.Size.Value.Width - 1, Height = e.Size.Value.Height - 1 });
        _vertical.Size = e.Size.Value.Height;
        _horizontal.Size = e.Size.Value.Width;
    }

    private void Application_UnGrabbedMouse (object sender, ViewEventArgs e)
    {
        var parent = e.View is Adornment adornment ? adornment.Parent : e.View;

        if (parent is { })
        {
            var supView = parent.SuperView;

            while (supView is { })
            {
                if (supView == _contentView)
                {
                    Application.GrabMouse (this);

                    break;
                }

                supView = supView.SuperView;
            }
        }
    }

    /// <summary>If true the vertical/horizontal scroll bars won't be showed if it's not needed.</summary>
    public bool AutoHideScrollBars
    {
        get => _autoHideScrollBars;
        set
        {
            if (_autoHideScrollBars != value)
            {
                _autoHideScrollBars = value;

                if (Subviews.Contains (_vertical))
                {
                    _vertical.AutoHideScrollBars = value;
                }

                if (Subviews.Contains (_horizontal))
                {
                    _horizontal.AutoHideScrollBars = value;
                }

                SetNeedsDisplay ();
            }
        }
    }

    /// <summary>Represents the top left corner coordinate that is displayed by the scrollview</summary>
    /// <value>The content offset.</value>
    public Point ContentOffset
    {
        get => _contentOffset;
        set
        {
            if (!IsInitialized)
            {
                // We're not initialized so we can't do anything fancy. Just cache value.
                _contentOffset = new Point (-Math.Abs (value.X), -Math.Abs (value.Y));

                return;
            }

            SetContentOffset (value);
        }
    }

    ///// <summary>Represents the contents of the data shown inside the scrollview</summary>
    ///// <value>The size of the content.</value>
    //public new Size ContentSize
    //{
    //    get => ContentSize;
    //    set
    //    {
    //        if (GetContentSize () != value)
    //        {
    //            ContentSize = value;
    //            _contentView.Frame = new Rectangle (_contentOffset, value);
    //            _vertical.Size = GetContentSize ().Height;
    //            _horizontal.Size = GetContentSize ().Width;
    //            SetNeedsDisplay ();
    //        }
    //    }
    //}

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollView"/></summary>
    public bool KeepContentAlwaysInViewport
    {
        get => _keepContentAlwaysInViewport;
        set
        {
            if (_keepContentAlwaysInViewport != value)
            {
                _keepContentAlwaysInViewport = value;
                _vertical.OtherScrollBarView.KeepContentAlwaysInViewport = value;
                _horizontal.OtherScrollBarView.KeepContentAlwaysInViewport = value;
                Point p = default;

                if (value && -_contentOffset.X + Viewport.Width > GetContentSize ().Width)
                {
                    p = new Point (
                                   GetContentSize ().Width - Viewport.Width + (_showVerticalScrollIndicator ? 1 : 0),
                                   -_contentOffset.Y
                                  );
                }

                if (value && -_contentOffset.Y + Viewport.Height > GetContentSize ().Height)
                {
                    if (p == default (Point))
                    {
                        p = new Point (
                                       -_contentOffset.X,
                                       GetContentSize ().Height - Viewport.Height + (_showHorizontalScrollIndicator ? 1 : 0)
                                      );
                    }
                    else
                    {
                        p.Y = GetContentSize ().Height - Viewport.Height + (_showHorizontalScrollIndicator ? 1 : 0);
                    }
                }

                if (p != default (Point))
                {
                    ContentOffset = p;
                }
            }
        }
    }

    /// <summary>Gets or sets the visibility for the horizontal scroll indicator.</summary>
    /// <value><c>true</c> if show horizontal scroll indicator; otherwise, <c>false</c>.</value>
    public bool ShowHorizontalScrollIndicator
    {
        get => _showHorizontalScrollIndicator;
        set
        {
            if (value != _showHorizontalScrollIndicator)
            {
                _showHorizontalScrollIndicator = value;
                SetNeedsLayout ();

                if (value)
                {
                    _horizontal.OtherScrollBarView = _vertical;
                    base.Add (_horizontal);
                    _horizontal.ShowScrollIndicator = value;
                    _horizontal.AutoHideScrollBars = _autoHideScrollBars;
                    _horizontal.OtherScrollBarView.ShowScrollIndicator = value;
                    _horizontal.MouseEnter += View_MouseEnter;
                    _horizontal.MouseLeave += View_MouseLeave;
                }
                else
                {
                    base.Remove (_horizontal);
                    _horizontal.OtherScrollBarView = null;
                    _horizontal.MouseEnter -= View_MouseEnter;
                    _horizontal.MouseLeave -= View_MouseLeave;
                }
            }

            _vertical.Height = Dim.Fill (_showHorizontalScrollIndicator ? 1 : 0);
        }
    }

    /// <summary>Gets or sets the visibility for the vertical scroll indicator.</summary>
    /// <value><c>true</c> if show vertical scroll indicator; otherwise, <c>false</c>.</value>
    public bool ShowVerticalScrollIndicator
    {
        get => _showVerticalScrollIndicator;
        set
        {
            if (value != _showVerticalScrollIndicator)
            {
                _showVerticalScrollIndicator = value;
                SetNeedsLayout ();

                if (value)
                {
                    _vertical.OtherScrollBarView = _horizontal;
                    base.Add (_vertical);
                    _vertical.ShowScrollIndicator = value;
                    _vertical.AutoHideScrollBars = _autoHideScrollBars;
                    _vertical.OtherScrollBarView.ShowScrollIndicator = value;
                    _vertical.MouseEnter += View_MouseEnter;
                    _vertical.MouseLeave += View_MouseLeave;
                }
                else
                {
                    Remove (_vertical);
                    _vertical.OtherScrollBarView = null;
                    _vertical.MouseEnter -= View_MouseEnter;
                    _vertical.MouseLeave -= View_MouseLeave;
                }
            }

            _horizontal.Width = Dim.Fill (_showVerticalScrollIndicator ? 1 : 0);
        }
    }

    /// <summary>Adds the view to the scrollview.</summary>
    /// <param name="view">The view to add to the scrollview.</param>
    public override View Add (View view)
    {
        if (view is ScrollBarView.ContentBottomRightCorner)
        {
            _contentBottomRightCorner = view;
            base.Add (view);
        }
        else
        {
            if (!IsOverridden (view, "OnMouseEvent"))
            {
                view.MouseEnter += View_MouseEnter;
                view.MouseLeave += View_MouseLeave;
            }

            _contentView.Add (view);
        }

        SetNeedsLayout ();
        return view;
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        SetViewsNeedsDisplay ();

        // TODO: It's bad practice for views to always clear a view. It negates clipping.
        Clear ();

        if (!string.IsNullOrEmpty (_contentView.Text) || _contentView.Subviews.Count > 0)
        {
            _contentView.Draw ();
        }

        DrawScrollBars ();
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key a)
    {
        if (base.OnKeyDown (a))
        {
            return true;
        }

        bool? result = InvokeCommands (a, KeyBindingScope.HotKey | KeyBindingScope.Focused);

        if (result is { })
        {
            return (bool)result;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs me)
    {
        if (!Enabled)
        {
            // A disabled view should not eat mouse events
            return false;
        }

        if (me.Flags == MouseFlags.WheeledDown && ShowVerticalScrollIndicator)
        {
            return ScrollDown (1);
        }
        else if (me.Flags == MouseFlags.WheeledUp && ShowVerticalScrollIndicator)
        {
            return ScrollUp (1);
        }
        else if (me.Flags == MouseFlags.WheeledRight && _showHorizontalScrollIndicator)
        {
            return ScrollRight (1);
        }
        else if (me.Flags == MouseFlags.WheeledLeft && ShowVerticalScrollIndicator)
        {
            return ScrollLeft (1);
        }
        else if (me.Position.X == _vertical.Frame.X && ShowVerticalScrollIndicator)
        {
            _vertical.NewMouseEvent (me);
        }
        else if (me.Position.Y == _horizontal.Frame.Y && ShowHorizontalScrollIndicator)
        {
            _horizontal.NewMouseEvent (me);
        }
        else if (IsOverridden (me.View, "OnMouseEvent"))
        {
            Application.UngrabMouse ();
        }

        return me.Handled;
    }

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        if (InternalSubviews.Count == 0)
        {
            Move (0, 0);

            return null; // Don't show the cursor
        }
        return base.PositionCursor ();
    }

    /// <summary>Removes the view from the scrollview.</summary>
    /// <param name="view">The view to remove from the scrollview.</param>
    public override View Remove (View view)
    {
        if (view is null)
        {
            return view;
        }

        SetNeedsDisplay ();
        View container = view?.SuperView;

        if (container == this)
        {
            base.Remove (view);
        }
        else
        {
            container?.Remove (view);
        }

        if (_contentView.InternalSubviews.Count < 1)
        {
            CanFocus = false;
        }

        return view;
    }

    /// <summary>Removes all widgets from this container.</summary>
    public override void RemoveAll () { _contentView.RemoveAll (); }

    /// <summary>Scrolls the view down.</summary>
    /// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
    /// <param name="lines">Number of lines to scroll.</param>
    public bool ScrollDown (int lines)
    {
        if (_vertical.CanScroll (lines, out _, true))
        {
            ContentOffset = new Point (_contentOffset.X, _contentOffset.Y - lines);

            return true;
        }

        return false;
    }

    /// <summary>Scrolls the view to the left</summary>
    /// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
    /// <param name="cols">Number of columns to scroll by.</param>
    public bool ScrollLeft (int cols)
    {
        if (_contentOffset.X < 0)
        {
            ContentOffset = new Point (Math.Min (_contentOffset.X + cols, 0), _contentOffset.Y);

            return true;
        }

        return false;
    }

    /// <summary>Scrolls the view to the right.</summary>
    /// <returns><c>true</c>, if right was scrolled, <c>false</c> otherwise.</returns>
    /// <param name="cols">Number of columns to scroll by.</param>
    public bool ScrollRight (int cols)
    {
        if (_horizontal.CanScroll (cols, out _))
        {
            ContentOffset = new Point (_contentOffset.X - cols, _contentOffset.Y);

            return true;
        }

        return false;
    }

    /// <summary>Scrolls the view up.</summary>
    /// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
    /// <param name="lines">Number of lines to scroll.</param>
    public bool ScrollUp (int lines)
    {
        if (_contentOffset.Y < 0)
        {
            ContentOffset = new Point (_contentOffset.X, Math.Min (_contentOffset.Y + lines, 0));

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (!_showVerticalScrollIndicator)
        {
            // It was not added to SuperView, so it won't get disposed automatically
            _vertical?.Dispose ();
        }

        if (!_showHorizontalScrollIndicator)
        {
            // It was not added to SuperView, so it won't get disposed automatically
            _horizontal?.Dispose ();
        }

        Application.UnGrabbedMouse -= Application_UnGrabbedMouse;

        base.Dispose (disposing);
    }

    private void DrawScrollBars ()
    {
        if (_autoHideScrollBars)
        {
            ShowHideScrollBars ();
        }
        else
        {
            if (ShowVerticalScrollIndicator)
            {
                _vertical.Draw ();
            }

            if (ShowHorizontalScrollIndicator)
            {
                _horizontal.Draw ();
            }

            if (ShowVerticalScrollIndicator && ShowHorizontalScrollIndicator)
            {
                SetContentBottomRightCornerVisibility ();
                _contentBottomRightCorner.Draw ();
            }
        }
    }

    private void SetContentBottomRightCornerVisibility ()
    {
        if (_showHorizontalScrollIndicator && _showVerticalScrollIndicator)
        {
            _contentBottomRightCorner.Visible = true;
        }
        else if (_horizontal.IsAdded || _vertical.IsAdded)
        {
            _contentBottomRightCorner.Visible = false;
        }
    }

    private void SetContentOffset (Point offset)
    {
        // INTENT: Unclear intent. How about a call to Offset?
        _contentOffset = new Point (-Math.Abs (offset.X), -Math.Abs (offset.Y));
        _contentView.Frame = new Rectangle (_contentOffset, GetContentSize ());
        int p = Math.Max (0, -_contentOffset.Y);

        if (_vertical.Position != p)
        {
            _vertical.Position = Math.Max (0, -_contentOffset.Y);
        }

        p = Math.Max (0, -_contentOffset.X);

        if (_horizontal.Position != p)
        {
            _horizontal.Position = Math.Max (0, -_contentOffset.X);
        }
        SetNeedsDisplay ();
    }

    private void SetViewsNeedsDisplay ()
    {
        foreach (View view in _contentView.Subviews)
        {
            view.SetNeedsDisplay ();
        }
    }

    private void ShowHideScrollBars ()
    {
        bool v = false, h = false;
        var p = false;

        if (GetContentSize () is { } && (Viewport.Height == 0 || Viewport.Height > GetContentSize ().Height))
        {
            if (ShowVerticalScrollIndicator)
            {
                ShowVerticalScrollIndicator = false;
            }

            v = false;
        }
        else if (GetContentSize () is { } && Viewport.Height > 0 && Viewport.Height == GetContentSize ().Height)
        {
            p = true;
        }
        else
        {
            if (!ShowVerticalScrollIndicator)
            {
                ShowVerticalScrollIndicator = true;
            }

            v = true;
        }

        if (GetContentSize () is { } && (Viewport.Width == 0 || Viewport.Width > GetContentSize ().Width))
        {
            if (ShowHorizontalScrollIndicator)
            {
                ShowHorizontalScrollIndicator = false;
            }

            h = false;
        }
        else if (GetContentSize () is { } && Viewport.Width > 0 && Viewport.Width == GetContentSize ().Width && p)
        {
            if (ShowHorizontalScrollIndicator)
            {
                ShowHorizontalScrollIndicator = false;
            }

            h = false;

            if (ShowVerticalScrollIndicator)
            {
                ShowVerticalScrollIndicator = false;
            }

            v = false;
        }
        else
        {
            if (p)
            {
                if (!ShowVerticalScrollIndicator)
                {
                    ShowVerticalScrollIndicator = true;
                }

                v = true;
            }

            if (!ShowHorizontalScrollIndicator)
            {
                ShowHorizontalScrollIndicator = true;
            }

            h = true;
        }

        Dim dim = Dim.Fill (h ? 1 : 0);

        if (!_vertical.Height.Equals (dim))
        {
            _vertical.Height = dim;
        }

        dim = Dim.Fill (v ? 1 : 0);

        if (!_horizontal.Width.Equals (dim))
        {
            _horizontal.Width = dim;
        }

        if (v)
        {
            _vertical.SetRelativeLayout (Viewport.Size);
            _vertical.Draw ();
        }

        if (h)
        {
            _horizontal.SetRelativeLayout (Viewport.Size);
            _horizontal.Draw ();
        }

        SetContentBottomRightCornerVisibility ();

        if (v && h)
        {
            _contentBottomRightCorner.SetRelativeLayout (Viewport.Size);
            _contentBottomRightCorner.Draw ();
        }
    }

    private void View_MouseEnter (object sender, CancelEventArgs e) { Application.GrabMouse (this); }

    private void View_MouseLeave (object sender, EventArgs e)
    {
        if (Application.MouseGrabView is { } && Application.MouseGrabView != this && Application.MouseGrabView != _vertical && Application.MouseGrabView != _horizontal)
        {
            Application.UngrabMouse ();
        }
    }

    // The ContentView is the view that contains the subviews  and content that are being scrolled
    // The ContentView is the size of the ContentSize and is offset by the ContentOffset
    private class ContentView : View
    {
        public ContentView () { CanFocus = true; }
    }
}
