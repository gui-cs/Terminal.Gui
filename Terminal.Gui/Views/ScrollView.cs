﻿//
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

namespace Terminal.Gui;

/// <summary>
///     Scrollviews are views that present a window into a virtual space where subviews are added.  Similar to the iOS
///     UIScrollView.
/// </summary>
/// <remarks>
///     <para>
///         The subviews that are added to this <see cref="Gui.ScrollView"/> are offset by the
///         <see cref="ContentOffset"/> property.  The view itself is a window into the space represented by the
///         <see cref="ContentSize"/>.
///     </para>
///     <para>Use the</para>
/// </remarks>
public class ScrollView : View
{
    private readonly ContentView _contentView;
    private readonly ScrollBar _horizontal;
    private readonly ScrollBar _vertical;
    private View _contentBottomRightCorner;
    private Point _contentOffset;
    private Size _contentSize;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Gui.ScrollView"/> class using <see cref="LayoutStyle.Computed"/>
    ///     positioning.
    /// </summary>
    public ScrollView ()
    {
        _contentView = new ContentView ();
        base.Add (_contentView);

        _vertical = new ScrollBar
        {
            X = Pos.AnchorEnd (1),
            Y = 0,
            Width = 1,
            Size = 1,
            IsVertical = true
        };

        _horizontal = new ScrollBar
        {
            X = 0,
            Y = Pos.AnchorEnd (1),
            Height = 1,
            Size = 1,
            IsVertical = false
        };
        _vertical.OtherScrollBar = _horizontal;
        _horizontal.OtherScrollBar = _vertical;

        // The _horizontal will be automatically added
        // through the OtherScrollBar property
        base.Add (_vertical);

        CanFocus = true;

        MouseEnter += View_MouseEnter;
        MouseLeave += View_MouseLeave;
        _contentView.MouseEnter += View_MouseEnter;
        _contentView.MouseLeave += View_MouseLeave;

        // Things this view knows how to do
        AddCommand (Command.ScrollUp, () => ScrollUp (1));
        AddCommand (Command.ScrollDown, () => ScrollDown (1));
        AddCommand (Command.ScrollLeft, () => ScrollLeft (1));
        AddCommand (Command.ScrollRight, () => ScrollRight (1));
        AddCommand (Command.PageUp, () => ScrollUp (Bounds.Height));
        AddCommand (Command.PageDown, () => ScrollDown (Bounds.Height));
        AddCommand (Command.PageLeft, () => ScrollLeft (Bounds.Width));
        AddCommand (Command.PageRight, () => ScrollRight (Bounds.Width));
        AddCommand (Command.TopHome, () => ScrollUp (_contentSize.Height));
        AddCommand (Command.BottomEnd, () => ScrollDown (_contentSize.Height));
        AddCommand (Command.LeftHome, () => ScrollLeft (_contentSize.Width));
        AddCommand (Command.RightEnd, () => ScrollRight (_contentSize.Width));

        // Default keybindings for this view
        KeyBindings.Add (KeyCode.CursorUp, Command.ScrollUp);
        KeyBindings.Add (KeyCode.CursorDown, Command.ScrollDown);
        KeyBindings.Add (KeyCode.CursorLeft, Command.ScrollLeft);
        KeyBindings.Add (KeyCode.CursorRight, Command.ScrollRight);

        KeyBindings.Add (KeyCode.PageUp, Command.PageUp);
        KeyBindings.Add ((KeyCode)'v' | KeyCode.AltMask, Command.PageUp);

        KeyBindings.Add (KeyCode.PageDown, Command.PageDown);
        KeyBindings.Add (KeyCode.V | KeyCode.CtrlMask, Command.PageDown);

        KeyBindings.Add (KeyCode.PageUp | KeyCode.CtrlMask, Command.PageLeft);
        KeyBindings.Add (KeyCode.PageDown | KeyCode.CtrlMask, Command.PageRight);
        KeyBindings.Add (KeyCode.Home, Command.TopHome);
        KeyBindings.Add (KeyCode.End, Command.BottomEnd);
        KeyBindings.Add (KeyCode.Home | KeyCode.CtrlMask, Command.LeftHome);
        KeyBindings.Add (KeyCode.End | KeyCode.CtrlMask, Command.RightEnd);

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
                           _contentView.Frame = new Rect (ContentOffset, ContentSize);
                           _vertical.ChangedPosition += delegate { ContentOffset = new Point (ContentOffset.X, _vertical.Position); };
                           _horizontal.ChangedPosition += delegate { ContentOffset = new Point (_horizontal.Position, ContentOffset.Y); };
                       };
    }

    /// <summary>If true the vertical/horizontal scroll bars won't be showed if it's not needed.</summary>
    public bool AutoHideScrollBars
    {
        get => _horizontal?.AutoHideScrollBars ?? _vertical.AutoHideScrollBars;
        set
        {
            if (_horizontal.AutoHideScrollBars || _vertical.AutoHideScrollBars != value)
            {
                _vertical.AutoHideScrollBars = value;
                _horizontal.AutoHideScrollBars = value;

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
                ;

                return;
            }

            SetContentOffset (value);
        }
    }

    /// <summary>Represents the contents of the data shown inside the scrollview</summary>
    /// <value>The size of the content.</value>
    public Size ContentSize
    {
        get => _contentSize;
        set
        {
            if (_contentSize != value)
            {
                _contentSize = value;
                _contentView.Frame = new Rect (_contentOffset, value);
                _vertical.Size = _contentSize.Height;
                _horizontal.Size = _contentSize.Width;
                SetNeedsDisplay ();
            }
        }
    }

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollView"/></summary>
    public bool KeepContentAlwaysInViewPort
    {
        get => _horizontal?.KeepContentAlwaysInViewPort ?? _vertical.KeepContentAlwaysInViewPort;
        set
        {
            if (_horizontal.KeepContentAlwaysInViewPort || _vertical.KeepContentAlwaysInViewPort != value)
            {
                _vertical.KeepContentAlwaysInViewPort = value;
                _horizontal.KeepContentAlwaysInViewPort = value;
            }
        }
    }

    /// <summary>Gets or sets the visibility for the horizontal scroll indicator.</summary>
    /// <value><c>true</c> if show horizontal scroll indicator; otherwise, <c>false</c>.</value>
    public bool ShowHorizontalScrollIndicator
    {
        get => _horizontal.ShowScrollIndicator;
        set
        {
            if (value != _horizontal.ShowScrollIndicator)
            {
                _horizontal.ShowScrollIndicator = value;
                SetNeedsLayout ();

                if (value)
                {
                    _horizontal.OtherScrollBar = _vertical;

                    if (!Subviews.Contains (_horizontal))
                    {
                        base.Add (_horizontal);
                    }

                    _horizontal.ShowScrollIndicator = true;
                    _horizontal.AutoHideScrollBars = AutoHideScrollBars;
                    _horizontal.OtherScrollBar.ShowScrollIndicator = true;
                    _horizontal.MouseEnter += View_MouseEnter;
                    _horizontal.MouseLeave += View_MouseLeave;
                }
                else
                {
                    base.Remove (_horizontal);
                    _horizontal.ShowScrollIndicator = false;
                    _horizontal.MouseEnter -= View_MouseEnter;
                    _horizontal.MouseLeave -= View_MouseLeave;
                }
            }
        }
    }

    /// <summary>Gets or sets the visibility for the vertical scroll indicator.</summary>
    /// <value><c>true</c> if show vertical scroll indicator; otherwise, <c>false</c>.</value>
    public bool ShowVerticalScrollIndicator
    {
        get => _vertical.ShowScrollIndicator;
        set
        {
            if (value != _vertical.ShowScrollIndicator)
            {
                _vertical.ShowScrollIndicator = value;
                SetNeedsLayout ();

                if (value)
                {
                    _vertical.OtherScrollBar = _horizontal;

                    if (!Subviews.Contains (_vertical))
                    {
                        base.Add (_vertical);
                    }

                    _vertical.ShowScrollIndicator = true;
                    _vertical.AutoHideScrollBars = AutoHideScrollBars;
                    _vertical.OtherScrollBar.ShowScrollIndicator = true;
                    _vertical.MouseEnter += View_MouseEnter;
                    _vertical.MouseLeave += View_MouseLeave;
                }
                else
                {
                    Remove (_vertical);
                    _vertical.ShowScrollIndicator = false;
                    _vertical.MouseEnter -= View_MouseEnter;
                    _vertical.MouseLeave -= View_MouseLeave;
                }
            }
        }
    }

    /// <summary>Adds the view to the scrollview.</summary>
    /// <param name="view">The view to add to the scrollview.</param>
    public override void Add (View view)
    {
        if (view is ScrollBar.ContentBottomRightCorner)
        {
            _contentBottomRightCorner = view;
            base.Add (view);
        }
        else if (view is ScrollBar)
        {
            base.Add (view);
        }
        else
        {
            if (!IsOverridden (view, "MouseEvent"))
            {
                view.MouseEnter += View_MouseEnter;
                view.MouseLeave += View_MouseLeave;
            }

            _contentView.Add (view);
        }

        SetNeedsLayout ();
    }

    /// <inheritdoc/>
    public override bool MouseEvent (MouseEvent me)
    {
        if (me.Flags != MouseFlags.WheeledDown
            && me.Flags != MouseFlags.WheeledUp
            && me.Flags != MouseFlags.WheeledRight
            && me.Flags != MouseFlags.WheeledLeft
            &&

            //				me.Flags != MouseFlags.Button1Pressed && me.Flags != MouseFlags.Button1Clicked &&
            !me.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            return false;
        }

        if (me.Flags == MouseFlags.WheeledDown && ShowVerticalScrollIndicator)
        {
            ScrollDown (1);
        }
        else if (me.Flags == MouseFlags.WheeledUp && ShowVerticalScrollIndicator)
        {
            ScrollUp (1);
        }
        else if (me.Flags == MouseFlags.WheeledRight && _horizontal.ShowScrollIndicator)
        {
            ScrollRight (1);
        }
        else if (me.Flags == MouseFlags.WheeledLeft && ShowVerticalScrollIndicator)
        {
            ScrollLeft (1);
        }
        else if (me.X == _vertical.Frame.X && ShowVerticalScrollIndicator)
        {
            _vertical.MouseEvent (me);
        }
        else if (me.Y == _horizontal.Frame.Y && ShowHorizontalScrollIndicator)
        {
            _horizontal.MouseEvent (me);
        }
        else if (IsOverridden (me.View, "MouseEvent"))
        {
            Application.UngrabMouse ();
        }

        return true;
    }

    /// <inheritdoc/>
    public override void OnDrawContent (Rect contentArea)
    {
        SetViewsNeedsDisplay ();

        Rect savedClip = ClipToBounds ();

        // TODO: It's bad practice for views to always clear a view. It negates clipping.
        Clear ();

        if (!string.IsNullOrEmpty (_contentView.Text) || _contentView.Subviews.Count > 0)
        {
            _contentView.Draw ();
        }

        DrawScrollBars ();

        Driver.Clip = savedClip;
    }

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        if (Subviews.Count == 0 || !Subviews.Any (subview => subview.CanFocus))
        {
            Application.Driver?.SetCursorVisibility (CursorVisibility.Invisible);
        }

        return base.OnEnter (view);
    }

    /// <inheritdoc/>
    public override bool OnKeyDown (Key a)
    {
        if (base.OnKeyDown (a))
        {
            return true;
        }

        bool? result = InvokeKeyBindings (a);

        if (result is { })
        {
            return (bool)result;
        }

        return false;
    }

    /// <inheritdoc/>
    public override void PositionCursor ()
    {
        if (InternalSubviews.Count == 0)
        {
            Move (0, 0);
        }
        else
        {
            base.PositionCursor ();
        }
    }

    /// <summary>Removes the view from the scrollview.</summary>
    /// <param name="view">The view to remove from the scrollview.</param>
    public override void Remove (View view)
    {
        if (view is null)
        {
            return;
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
    }

    /// <summary>Removes all widgets from this container.</summary>
    public override void RemoveAll () { _contentView.RemoveAll (); }

    /// <summary>Scrolls the view down.</summary>
    /// <returns><c>true</c>, if left was scrolled, <c>false</c> otherwise.</returns>
    /// <param name="lines">Number of lines to scroll.</param>
    public bool ScrollDown (int lines)
    {
        if (_vertical.CanScroll (_vertical.Position + lines, out _, true))
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
        if (_horizontal.CanScroll (_horizontal.Position + cols, out _))
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
        if (!_vertical.ShowScrollIndicator)
        {
            // It was not added to SuperView, so it won't get disposed automatically
            _vertical?.Dispose ();
        }

        if (!_horizontal.ShowScrollIndicator)
        {
            // It was not added to SuperView, so it won't get disposed automatically
            _horizontal?.Dispose ();
        }

        base.Dispose (disposing);
    }

    private void DrawScrollBars ()
    {
        if (AutoHideScrollBars)
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
        if (_contentBottomRightCorner is null)
        {
            return;
        }

        if (_horizontal.ShowScrollIndicator && _vertical.ShowScrollIndicator)
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
        _contentOffset = new Point (-Math.Abs (offset.X), -Math.Abs (offset.Y));
        _contentView.Frame = new Rect (_contentOffset, _contentSize);
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

        if (Bounds.Height == 0 || Bounds.Height > _contentSize.Height)
        {
            if (ShowVerticalScrollIndicator)
            {
                ShowVerticalScrollIndicator = false;
            }

            v = false;
        }
        else if (Bounds.Height > 0 && Bounds.Height == _contentSize.Height)
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

        if (Bounds.Width == 0 || Bounds.Width > _contentSize.Width)
        {
            if (ShowHorizontalScrollIndicator)
            {
                ShowHorizontalScrollIndicator = false;
            }

            h = false;
        }
        else if (Bounds.Width > 0 && Bounds.Width == _contentSize.Width && p)
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
            _vertical.SetRelativeLayout (Bounds);
            _vertical.Draw ();
        }

        if (h)
        {
            _horizontal.SetRelativeLayout (Bounds);
            _horizontal.Draw ();
        }

        SetContentBottomRightCornerVisibility ();

        if (v && h)
        {
            _contentBottomRightCorner.SetRelativeLayout (Bounds);
            _contentBottomRightCorner.Draw ();
        }
    }

    private void View_MouseEnter (object sender, MouseEventEventArgs e) { Application.GrabMouse (this); }

    private void View_MouseLeave (object sender, MouseEventEventArgs e)
    {
        if (Application.MouseGrabView is { } && Application.MouseGrabView != _vertical && Application.MouseGrabView != _horizontal)
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
