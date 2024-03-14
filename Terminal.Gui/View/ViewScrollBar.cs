namespace Terminal.Gui;

public partial class View
{
    private bool _enableScrollBars;
    private ScrollBarView _scrollBar;
    private bool _useContentOffset;
    private Point _contentOffset;
    private Size _contentSize;

    /// <summary>If true the vertical/horizontal scroll bars won't be showed if it's not needed.</summary>
    public bool AutoHideScrollBars
    {
        get => ParentView?._scrollBar?.AutoHideScrollBars is true;
        set
        {
            if (HasVerticalScrollBar)
            {
                ParentView._scrollBar.AutoHideScrollBars = value;
            }

            if (HasHorizontalScrollBar)
            {
                ParentView._scrollBar.OtherScrollBarView.AutoHideScrollBars = value;
            }
        }
    }

    /// <summary>
    ///     Represent the content offset if <see cref="UseContentOffset"/> is true.
    /// </summary>
    public virtual Point ContentOffset
    {
        get => ParentView._contentOffset;
        set
        {
            ParentView._contentOffset = value;

            if (UseContentOffset)
            {
                SetNeedsLayout ();
                SetNeedsDisplay ();
            }

            if (ParentView._scrollBar is null)
            {
                return;
            }

            if (ParentView._scrollBar.Orientation == Orientation.Horizontal && ParentView._scrollBar.Position != -ParentView.ContentOffset.X)
            {
                ParentView._scrollBar.Position = -ParentView.ContentOffset.X;
            }
            else if (ParentView._scrollBar is { OtherScrollBarView.Orientation: Orientation.Horizontal }
                     && ParentView._scrollBar?.OtherScrollBarView.Position != -ParentView.ContentOffset.X)
            {
                ParentView._scrollBar!.OtherScrollBarView.Position = -ParentView.ContentOffset.X;
            }

            if (ParentView._scrollBar.Orientation == Orientation.Vertical && ParentView._scrollBar.Position != -ParentView.ContentOffset.Y)
            {
                ParentView._scrollBar.Position = -ParentView.ContentOffset.Y;
            }
            else if (ParentView._scrollBar is { OtherScrollBarView.Orientation: Orientation.Vertical }
                     && ParentView._scrollBar?.OtherScrollBarView.Position != -ParentView.ContentOffset.Y)
            {
                ParentView._scrollBar!.OtherScrollBarView.Position = -ParentView.ContentOffset.Y;
            }
        }
    }

    /// <summary>
    ///     Represents the contents size of the data shown inside the <see cref="ContentArea"/>.
    /// </summary>
    public Size ContentSize
    {
        get => ParentView._contentSize;
        set
        {
            if (ParentView._contentSize != value)
            {
                ParentView._contentSize = value;
                SetNeedsDisplay ();
            }

            if (_scrollBar is null)
            {
                return;
            }

            if (_scrollBar.Orientation == Orientation.Vertical)
            {
                if (_scrollBar.Size != ContentSize.Height)
                {
                    _scrollBar.Size = ContentSize.Height;
                }

                if (_scrollBar.OtherScrollBarView is { })
                {
                    if (_scrollBar.OtherScrollBarView.Size != ContentSize.Width)
                    {
                        _scrollBar.OtherScrollBarView.Size = ContentSize.Width;
                    }
                }
            }
            else
            {
                if (_scrollBar.Size != ContentSize.Width)
                {
                    _scrollBar.Size = ContentSize.Width;
                }

                if (_scrollBar.OtherScrollBarView is { })
                {
                    if (_scrollBar.OtherScrollBarView.Size != ContentSize.Height)
                    {
                        _scrollBar.OtherScrollBarView.Size = ContentSize.Height;
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="EnableScrollBars"/> used by this view.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is out of range.</exception>
    public virtual bool EnableScrollBars
    {
        get => _enableScrollBars;
        set
        {
            if (ParentView._scrollBar is { } && ParentView._enableScrollBars == value)
            {
                return;
            }

            ParentView._enableScrollBars = value;
            DisposeScrollBar ();

            if (!value)
            {
                return;
            }

            var vertical = new ScrollBarView { Orientation = Orientation.Vertical, Size = ParentView.ContentSize.Height };
            var horizontal = new ScrollBarView { Orientation = Orientation.Horizontal, Size = ParentView.ContentSize.Width };
            ParentView._scrollBar = vertical;
            ParentView._scrollBar.OtherScrollBarView = horizontal;

            Add (ParentView._scrollBar);
            ParentView.AddEventHandlersForScrollBars (ParentView._scrollBar);
            ParentView.AddKeyBindingsForScrolling (ParentView._scrollBar);

            if (ParentView._scrollBar.OtherScrollBarView != null)
            {
                ParentView.AddKeyBindingsForScrolling (ParentView._scrollBar.OtherScrollBarView);
            }

            ParentView.SetNeedsDisplay ();
        }
    }

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBarView"/></summary>
    public bool KeepContentAlwaysInContentArea
    {
        get => ParentView?._scrollBar?.KeepContentAlwaysInViewPort is true || ParentView?._scrollBar?.OtherScrollBarView.KeepContentAlwaysInViewPort is true;
        set
        {
            if (HasVerticalScrollBar)
            {
                ParentView._scrollBar.KeepContentAlwaysInViewPort = value;
            }

            if (HasHorizontalScrollBar)
            {
                ParentView._scrollBar.OtherScrollBarView.KeepContentAlwaysInViewPort = value;
            }
        }
    }

    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll bar.</summary>
    /// <value><c>true</c> if show horizontal scroll bar; otherwise, <c>false</c>.</value>
    public virtual bool ShowHorizontalScrollBar
    {
        get => ParentView?._scrollBar?.OtherScrollBarView.ShowScrollIndicator is true;
        set
        {
            if (ParentView._scrollBar?.OtherScrollBarView is { })
            {
                ParentView._scrollBar.OtherScrollBarView.ShowScrollIndicator = value;
            }
        }
    }

    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll bar.</summary>
    /// <value><c>true</c> if show vertical scroll bar; otherwise, <c>false</c>.</value>
    public virtual bool ShowVerticalScrollBar
    {
        get => ParentView?._scrollBar?.ShowScrollIndicator is true;
        set
        {
            if (ParentView._scrollBar is { })
            {
                ParentView._scrollBar.ShowScrollIndicator = value;
            }
        }
    }

    /// <summary>
    ///     Determines if negative bounds location is allowed for scrolling the <see cref="GetVisibleContentArea"/>.
    /// </summary>
    public bool UseContentOffset
    {
        get => _useContentOffset;
        set
        {
            _useContentOffset = value;

            if (IsInitialized && _useContentOffset)
            {
                ParentView.AddKeyBindingsForScrolling ();
            }
        }
    }

    private void AddEventHandlersForScrollBars (ScrollBarView scrollBar)
    {
        if (scrollBar is null)
        {
            return;
        }

        scrollBar.ChangedPosition += VerticalScrollBar_ChangedPosition;

        if (_scrollBar.OtherScrollBarView != null)
        {
            _scrollBar.OtherScrollBarView.ChangedPosition += HorizontalScrollBar_ChangedPosition;
        }
    }

    private void AddKeyBindingsForScrolling (ScrollBarView scrollBar = null)
    {
        View view = scrollBar ?? ParentView;

        if (view is ScrollBarView { Orientation: Orientation.Vertical } || (view == ParentView && view.UseContentOffset))
        {
            // Things this view knows how to do vertical scrolling
            if (scrollBar is { Orientation: Orientation.Vertical })
            {
                view.AddCommand (
                                 Command.ScrollDown,
                                 () =>
                                 {
                                     scrollBar.Position++;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.ScrollUp,
                                 () =>
                                 {
                                     scrollBar.Position--;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.TopHome,
                                 () =>
                                 {
                                     scrollBar.Position = 0;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.BottomEnd,
                                 () =>
                                 {
                                     scrollBar.Position = ContentSize.Height;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.PageDown,
                                 () =>
                                 {
                                     scrollBar.Position += scrollBar.GetVisibleContentArea ().Height;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.PageUp,
                                 () =>
                                 {
                                     scrollBar.Position -= scrollBar.GetVisibleContentArea ().Height;

                                     return true;
                                 });
            }
            else
            {
                view.AddCommand (
                                 Command.ScrollDown,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with
                                     {
                                         Y = Math.Max (
                                                       ParentView.ContentOffset.Y - 1,
                                                       -(ParentView.ContentSize.Height - ParentView.GetVisibleContentArea ().Height))
                                     };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.ScrollUp,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with { Y = Math.Min (ParentView.ContentOffset.Y + 1, 0) };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.TopHome,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with { Y = 0 };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.BottomEnd,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with
                                     {
                                         Y = Math.Max (
                                                       -ParentView.ContentSize.Height,
                                                       -(ParentView.ContentSize.Height - ParentView.GetVisibleContentArea ().Height))
                                     };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.PageDown,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with
                                     {
                                         Y = Math.Max (
                                                       -(ParentView.GetVisibleContentArea ().Height - ParentView.ContentOffset.Y),
                                                       ParentView.GetVisibleContentArea ().Height - ParentView.ContentSize.Height)
                                     };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.PageUp,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with
                                     {
                                         Y = Math.Max (-(ParentView.ContentOffset.Y + ParentView.GetVisibleContentArea ().Height), 0)
                                     };

                                     return true;
                                 });
            }

            // Default keybindings for vertical scrolling
            view.KeyBindings.Add (Key.CursorDown, KeyBindingScope.HotKey, Command.ScrollDown);
            view.KeyBindings.Add (Key.CursorUp, KeyBindingScope.HotKey, Command.ScrollUp);
            view.KeyBindings.Add (Key.Home, KeyBindingScope.HotKey, Command.TopHome);
            view.KeyBindings.Add (Key.End, KeyBindingScope.HotKey, Command.BottomEnd);
            view.KeyBindings.Add (Key.PageDown, KeyBindingScope.HotKey, Command.PageDown);
            view.KeyBindings.Add (Key.PageUp, KeyBindingScope.HotKey, Command.PageUp);
        }

        if (view is ScrollBarView { Orientation: Orientation.Horizontal } || (view == ParentView && view.UseContentOffset))
        {
            if (scrollBar is { Orientation: Orientation.Horizontal })
            {
                // Things this view knows how to do horizontal scrolling
                view.AddCommand (
                                 Command.Left,
                                 () =>
                                 {
                                     scrollBar.Position--;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.Right,
                                 () =>
                                 {
                                     scrollBar.Position++;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.LeftHome,
                                 () =>
                                 {
                                     scrollBar.Position = 0;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.RightEnd,
                                 () =>
                                 {
                                     scrollBar.Position = ContentSize.Width;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.PageRight,
                                 () =>
                                 {
                                     scrollBar.Position += scrollBar.GetVisibleContentArea ().Width;

                                     return true;
                                 });

                view.AddCommand (
                                 Command.PageLeft,
                                 () =>
                                 {
                                     scrollBar.Position -= scrollBar.GetVisibleContentArea ().Width;

                                     return true;
                                 });
            }
            else
            {
                view.AddCommand (
                                 Command.Left,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with { X = Math.Min (ParentView.ContentOffset.X + 1, 0) };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.Right,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with
                                     {
                                         X = Math.Max (
                                                       ParentView.ContentOffset.X - 1,
                                                       -(ParentView.ContentSize.Width - ParentView.GetVisibleContentArea ().Width))
                                     };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.LeftHome,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with { X = 0 };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.RightEnd,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with
                                     {
                                         X = Math.Max (
                                                       -ParentView.ContentSize.Width,
                                                       -(ParentView.ContentSize.Width - ParentView.GetVisibleContentArea ().Width))
                                     };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.PageRight,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with
                                     {
                                         X = Math.Max (
                                                       -(ParentView.GetVisibleContentArea ().Width - ParentView.ContentOffset.X),
                                                       ParentView.GetVisibleContentArea ().Width - ParentView.ContentSize.Width)
                                     };

                                     return true;
                                 });

                view.AddCommand (
                                 Command.PageLeft,
                                 () =>
                                 {
                                     ParentView.ContentOffset = ParentView.ContentOffset with
                                     {
                                         X = Math.Max (-(ParentView.ContentOffset.X + ParentView.GetVisibleContentArea ().Width), 0)
                                     };

                                     return true;
                                 });
            }

            // Default keybindings for horizontal scrolling
            view.KeyBindings.Add (Key.CursorLeft, KeyBindingScope.HotKey, Command.Left);
            view.KeyBindings.Add (Key.CursorRight, KeyBindingScope.HotKey, Command.Right);
            view.KeyBindings.Add (Key.Home.WithShift, KeyBindingScope.HotKey, Command.LeftHome);
            view.KeyBindings.Add (Key.End.WithShift, KeyBindingScope.HotKey, Command.RightEnd);
            view.KeyBindings.Add (Key.PageDown.WithShift, KeyBindingScope.HotKey, Command.PageRight);
            view.KeyBindings.Add (Key.PageUp.WithShift, KeyBindingScope.HotKey, Command.PageLeft);
        }
    }

    private void DisposeScrollBar ()
    {
        if (ParentView._scrollBar is null)
        {
            return;
        }

        ParentView._scrollBar.ChangedPosition -= VerticalScrollBar_ChangedPosition;
        ParentView.Remove (ParentView._scrollBar);

        if (ParentView._scrollBar.OtherScrollBarView != null)
        {
            ParentView._scrollBar.OtherScrollBarView.ChangedPosition -= HorizontalScrollBar_ChangedPosition;
            ParentView.Remove (ParentView._scrollBar.OtherScrollBarView);
        }

        if (ParentView.Subviews.First (v => v is ScrollBarView.ContentBottomRightCorner) is { } contentBottomRightCorner)
        {
            ParentView.Remove (contentBottomRightCorner);
        }

        ParentView._scrollBar = null;
    }

    private bool HasHorizontalScrollBar => ParentView is { _scrollBar.OtherScrollBarView: { } };

    private bool HasVerticalScrollBar => ParentView is { _scrollBar: { } };

    private void HorizontalScrollBar_ChangedPosition (object sender, EventArgs e) { SetBoundsByPosition (_scrollBar.OtherScrollBarView); }

    private View ParentView => this is Adornment adornment ? adornment.Parent : this;

    private void SetBoundsByPosition (ScrollBarView scrollBar)
    {
        if (scrollBar.Orientation == Orientation.Vertical)
        {
            ContentOffset = ContentOffset with { Y = -scrollBar.Position };
        }
        else
        {
            ContentOffset = ContentOffset with { X = -scrollBar.Position };
        }

        SetTextFormatterSize ();
        SetNeedsDisplay ();
    }

    private void VerticalScrollBar_ChangedPosition (object sender, EventArgs e) { SetBoundsByPosition (_scrollBar); }
}
