namespace Terminal.Gui;

public partial class View
{
    private bool _enableScrollBars;
    private View _parent;
    private ScrollBarView _scrollBar;

    /// <summary>If true the vertical/horizontal scroll bars won't be showed if it's not needed.</summary>
    public bool AutoHideScrollBars
    {
        get => _parent?._scrollBar?.AutoHideScrollBars is true;
        set
        {
            if (HasVerticalScrollBar)
            {
                _parent._scrollBar.AutoHideScrollBars = value;
            }

            if (HasHorizontalScrollBar)
            {
                _parent._scrollBar.OtherScrollBarView.AutoHideScrollBars = value;
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
            if (this is Adornment adornment)
            {
                _parent = adornment.Parent;
                adornment.Parent._parent = _parent;
            }
            else
            {
                _parent = this;
            }

            if (_parent._scrollBar is { } && _parent._enableScrollBars == value)
            {
                return;
            }

            _parent._enableScrollBars = value;
            DisposeScrollBar ();

            if (!value)
            {
                return;
            }

            var vertical = new ScrollBarView { Orientation = Orientation.Vertical };
            var horizontal = new ScrollBarView { Orientation = Orientation.Horizontal };
            _parent._scrollBar = vertical;
            _parent._scrollBar.OtherScrollBarView = horizontal;

            Add (_parent._scrollBar);
            _parent.AddEventHandlersForScrollBars (_parent._scrollBar);
            _parent.AddKeyBindingsForScrolling (_parent._scrollBar);

            if (_parent._scrollBar.OtherScrollBarView != null)
            {
                _parent.AddKeyBindingsForScrolling (_parent._scrollBar.OtherScrollBarView);
            }

            _parent.SetNeedsDisplay ();
        }
    }

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBarView"/></summary>
    public bool KeepContentAlwaysInContentArea
    {
        get => _parent?._scrollBar?.KeepContentAlwaysInViewPort is true || _parent?._scrollBar?.OtherScrollBarView.KeepContentAlwaysInViewPort is true;
        set
        {
            if (HasVerticalScrollBar)
            {
                _parent._scrollBar.KeepContentAlwaysInViewPort = value;
            }

            if (HasHorizontalScrollBar)
            {
                _parent._scrollBar.OtherScrollBarView.KeepContentAlwaysInViewPort = value;
            }
        }
    }

    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll bar.</summary>
    /// <value><c>true</c> if show horizontal scroll bar; otherwise, <c>false</c>.</value>
    public virtual bool ShowHorizontalScrollBar
    {
        get => _parent?._scrollBar?.OtherScrollBarView.ShowScrollIndicator is true;
        set
        {
            if (_parent._scrollBar?.OtherScrollBarView is { })
            {
                _parent._scrollBar.OtherScrollBarView.ShowScrollIndicator = value;
            }
        }
    }

    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll bar.</summary>
    /// <value><c>true</c> if show vertical scroll bar; otherwise, <c>false</c>.</value>
    public virtual bool ShowVerticalScrollBar
    {
        get => _parent?._scrollBar?.ShowScrollIndicator is true;
        set
        {
            if (_parent._scrollBar is { })
            {
                _parent._scrollBar.ShowScrollIndicator = value;
            }
        }
    }

    private bool HasHorizontalScrollBar => _parent is { _scrollBar.OtherScrollBarView: { } };

    private bool HasVerticalScrollBar => _parent is { _scrollBar: { } };

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
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position++;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.ScrollUp,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position--;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.TopHome,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position = 0;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.BottomEnd,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position = ContentSize.Height;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.PageDown,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position += GetVisibleContentArea ().Height;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.PageUp,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position -= GetVisibleContentArea ().Height;

                                          return true;
                                      }

                                      return false;
                                  });

            // Default keybindings for this view
            scrollBar.KeyBindings.Add (KeyCode.CursorDown, KeyBindingScope.HotKey, Command.ScrollDown);
            scrollBar.KeyBindings.Add (KeyCode.CursorUp, KeyBindingScope.HotKey, Command.ScrollUp);
            scrollBar.KeyBindings.Add (KeyCode.Home, KeyBindingScope.HotKey, Command.TopHome);
            scrollBar.KeyBindings.Add (KeyCode.End, KeyBindingScope.HotKey, Command.BottomEnd);
            scrollBar.KeyBindings.Add (KeyCode.PageDown, KeyBindingScope.HotKey, Command.PageDown);
            scrollBar.KeyBindings.Add (KeyCode.PageUp, KeyBindingScope.HotKey, Command.PageUp);
        }
        else
        {
            // Things this view knows how to do
            scrollBar.AddCommand (
                                  Command.Left,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position--;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.Right,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position++;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.LeftHome,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position = 0;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.RightEnd,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position = ContentSize.Width;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.PageRight,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position += GetVisibleContentArea ().Width;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.PageLeft,
                                  () =>
                                  {
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position -= GetVisibleContentArea ().Width;

                                          return true;
                                      }

                                      return false;
                                  });

            // Default keybindings for this view
            scrollBar.KeyBindings.Add (KeyCode.CursorLeft, KeyBindingScope.HotKey, Command.Left);
            scrollBar.KeyBindings.Add (KeyCode.CursorRight, KeyBindingScope.HotKey, Command.Right);
            scrollBar.KeyBindings.Add (KeyCode.Home | KeyCode.ShiftMask, KeyBindingScope.HotKey, Command.LeftHome);
            scrollBar.KeyBindings.Add (KeyCode.End | KeyCode.ShiftMask, KeyBindingScope.HotKey, Command.RightEnd);
            scrollBar.KeyBindings.Add (KeyCode.PageDown | KeyCode.ShiftMask, KeyBindingScope.HotKey, Command.PageRight);
            scrollBar.KeyBindings.Add (KeyCode.PageUp | KeyCode.ShiftMask, KeyBindingScope.HotKey, Command.PageLeft);
        }
    }

    private void DisposeScrollBar ()
    {
        if (_parent._scrollBar is null)
        {
            return;
        }

        _parent._scrollBar.ChangedPosition -= VerticalScrollBar_ChangedPosition;
        _parent.Remove (_parent._scrollBar);

        if (_parent._scrollBar.OtherScrollBarView != null)
        {
            _parent._scrollBar.OtherScrollBarView.ChangedPosition -= HorizontalScrollBar_ChangedPosition;
            _parent.Remove (_parent._scrollBar.OtherScrollBarView);
        }

        if (_parent.Subviews.First (v => v is ScrollBarView.ContentBottomRightCorner) is { } contentBottomRightCorner)
        {
            _parent.Remove (contentBottomRightCorner);
        }

        _parent._scrollBar = null;
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
