namespace Terminal.Gui;

public partial class View
{
    private bool _enableScrollBars;
    private ScrollBarView _scrollBar;

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

    private bool HasHorizontalScrollBar => ParentView is { _scrollBar.OtherScrollBarView: { } };

    private bool HasVerticalScrollBar => ParentView is { _scrollBar: { } };

    private View ParentView => this is Adornment adornment ? adornment.Parent : this;

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

    private void AddKeyBindingsForScrolling (ScrollBarView scrollBar)
    {
        if (scrollBar.Orientation == Orientation.Vertical)
        {
            // Things this view knows how to do
            scrollBar.AddCommand (
                                  Command.ScrollDown,
                                  () =>
                                  {
                                      scrollBar.Position++;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.ScrollUp,
                                  () =>
                                  {
                                      scrollBar.Position--;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.TopHome,
                                  () =>
                                  {
                                      scrollBar.Position = 0;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.BottomEnd,
                                  () =>
                                  {
                                      scrollBar.Position = ContentSize.Height;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.PageDown,
                                  () =>
                                  {
                                      scrollBar.Position += scrollBar.GetVisibleContentArea ().Height;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.PageUp,
                                  () =>
                                  {
                                      scrollBar.Position -= scrollBar.GetVisibleContentArea ().Height;

                                      return true;
                                  });

            // Default keybindings for this view
            scrollBar.KeyBindings.Add (Key.CursorDown, KeyBindingScope.HotKey, Command.ScrollDown);
            scrollBar.KeyBindings.Add (Key.CursorUp, KeyBindingScope.HotKey, Command.ScrollUp);
            scrollBar.KeyBindings.Add (Key.Home, KeyBindingScope.HotKey, Command.TopHome);
            scrollBar.KeyBindings.Add (Key.End, KeyBindingScope.HotKey, Command.BottomEnd);
            scrollBar.KeyBindings.Add (Key.PageDown, KeyBindingScope.HotKey, Command.PageDown);
            scrollBar.KeyBindings.Add (Key.PageUp, KeyBindingScope.HotKey, Command.PageUp);
        }
        else
        {
            // Things this view knows how to do
            scrollBar.AddCommand (
                                  Command.Left,
                                  () =>
                                  {
                                      scrollBar.Position--;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.Right,
                                  () =>
                                  {
                                      scrollBar.Position++;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.LeftHome,
                                  () =>
                                  {
                                      scrollBar.Position = 0;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.RightEnd,
                                  () =>
                                  {
                                      scrollBar.Position = ContentSize.Width;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.PageRight,
                                  () =>
                                  {
                                      scrollBar.Position += scrollBar.GetVisibleContentArea ().Width;

                                      return true;
                                  });

            scrollBar.AddCommand (
                                  Command.PageLeft,
                                  () =>
                                  {
                                      scrollBar.Position -= scrollBar.GetVisibleContentArea ().Width;

                                      return true;
                                  });

            // Default keybindings for this view
            scrollBar.KeyBindings.Add (Key.CursorLeft, KeyBindingScope.HotKey, Command.Left);
            scrollBar.KeyBindings.Add (Key.CursorRight, KeyBindingScope.HotKey, Command.Right);
            scrollBar.KeyBindings.Add (Key.Home.WithShift, KeyBindingScope.HotKey, Command.LeftHome);
            scrollBar.KeyBindings.Add (Key.End.WithShift, KeyBindingScope.HotKey, Command.RightEnd);
            scrollBar.KeyBindings.Add (Key.PageDown.WithShift, KeyBindingScope.HotKey, Command.PageRight);
            scrollBar.KeyBindings.Add (Key.PageUp.WithShift, KeyBindingScope.HotKey, Command.PageLeft);
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

    private void HorizontalScrollBar_ChangedPosition (object sender, EventArgs e) { SetBoundsByPosition (_scrollBar.OtherScrollBarView); }

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
