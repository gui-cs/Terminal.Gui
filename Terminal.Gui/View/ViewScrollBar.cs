namespace Terminal.Gui;

/// <summary>
///     The scroll bar types used by this <see cref="ScrollBarView"/>.
/// </summary>
public enum ScrollBarType
{
    /// <summary>
    ///     None scroll bar will be used and to avoid throwing an exception never use it in a constructor.
    /// </summary>
    None,

    /// <summary>
    ///     Only the vertical scroll bar will be shown.
    /// </summary>
    Vertical,

    /// <summary>
    ///     Only the horizontal scroll bar will be shown.
    /// </summary>
    Horizontal,

    /// <summary>
    ///     Both vertical and horizontal scroll bars will be shown.
    /// </summary>
    Both
}

public partial class View
{
    private ScrollBarView _scrollBar;
    private ScrollBarType _scrollBarType;
    private int _scrollColsSize;
    private int _scrollLeftOffset;
    private int _scrollRowsSize;
    private int _scrollTopOffset;

    /// <summary>If true the vertical/horizontal scroll bars won't be showed if it's not needed.</summary>
    public bool ScrollAutoHideScrollBars
    {
        get => _scrollBar.AutoHideScrollBars;
        set => _scrollBar.AutoHideScrollBars = value;
    }

    /// <summary>
    ///     Gets or sets the <see cref="ScrollBarType"/> used by this view.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is out of range.</exception>
    public virtual ScrollBarType ScrollBarType
    {
        get => _scrollBarType;
        set
        {
            View view = this is Adornment adornment ? adornment.Parent : this;

            if (view._scrollBar is { } && view._scrollBarType == value)
            {
                return;
            }

            view._scrollBarType = value;
            view.DisposeScrollBar ();

            switch (view._scrollBarType)
            {
                case ScrollBarType.Vertical:
                    view._scrollBar = new ScrollBarView { IsVertical = true };

                    break;
                case ScrollBarType.Horizontal:
                    view._scrollBar = new ScrollBarView { IsVertical = false };

                    break;
                case ScrollBarType.Both:
                    view._scrollBar = new ScrollBarView { IsVertical = true };
                    view._scrollBar.OtherScrollBarView = new ScrollBarView { IsVertical = false, OtherScrollBarView = view._scrollBar };

                    break;
                case ScrollBarType.None:
                    return;
                default:
                    throw new ArgumentOutOfRangeException ();
            }

            Add (view._scrollBar);
            view.AddEventHandlersForScrollBars (view._scrollBar);
            view.AddKeyBindingsForScrolling (view._scrollBar);

            if (view._scrollBar.OtherScrollBarView != null)
            {
                view.AddKeyBindingsForScrolling (view._scrollBar.OtherScrollBarView);
            }

            view.SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Determines the number of columns to scrolling.
    /// </summary>
    public int ScrollColsSize
    {
        get => _scrollColsSize;
        set
        {
            _scrollColsSize = value;

            if (ScrollBarType == ScrollBarType.None)
            {
                return;
            }

            switch (_scrollBar.IsVertical)
            {
                case true when _scrollBar.OtherScrollBarView is { }:
                    if (_scrollBar.OtherScrollBarView.Size == _scrollColsSize)
                    {
                        return;
                    }

                    _scrollBar.OtherScrollBarView.Size = _scrollColsSize;

                    break;
                case false:
                    if (_scrollBar.Size == _scrollColsSize)
                    {
                        return;
                    }

                    _scrollBar.Size = _scrollColsSize;

                    break;
            }

            SetNeedsDisplay ();
        }
    }

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBarView"/></summary>
    public bool ScrollKeepContentAlwaysInViewPort
    {
        get => _scrollBar.KeepContentAlwaysInViewPort;
        set => _scrollBar.KeepContentAlwaysInViewPort = value;
    }

    /// <summary>
    ///     Determines the left offset on scrolling.
    /// </summary>
    public virtual int ScrollLeftOffset
    {
        get => _scrollLeftOffset;
        set
        {
            if (!UseContentOffset)
            {
                _scrollLeftOffset = value;

                if (_scrollBar is null)
                {
                    return;
                }

                if (!_scrollBar.IsVertical && _scrollBar.Position != _scrollLeftOffset)
                {
                    _scrollBar.Position = _scrollLeftOffset;
                }
                else if (_scrollBar is { OtherScrollBarView.IsVertical: false } && _scrollBar?.OtherScrollBarView.Position != _scrollLeftOffset)
                {
                    _scrollBar!.OtherScrollBarView.Position = _scrollLeftOffset;
                }
            }
        }
    }

    /// <summary>Represent a vertical or horizontal ScrollBarView other than this.</summary>
    public ScrollBarView ScrollOtherScrollBarView
    {
        get => _scrollBar.OtherScrollBarView;
        set => _scrollBar.OtherScrollBarView = value;
    }

    /// <summary>The position, relative to <see cref="Size"/>, to set the scrollbar at.</summary>
    /// <value>The position.</value>
    public int ScrollPosition
    {
        get => _scrollBar.Position;
        set => _scrollBar.Position = value;
    }

    /// <summary>
    ///     Determines the number of rows to scrolling.
    /// </summary>
    public int ScrollRowsSize
    {
        get => _scrollRowsSize;
        set
        {
            _scrollRowsSize = value;

            if (ScrollBarType == ScrollBarType.None)
            {
                return;
            }

            switch (_scrollBar.IsVertical)
            {
                case true:
                    if (_scrollBar.Size == _scrollRowsSize)
                    {
                        return;
                    }

                    _scrollBar.Size = _scrollRowsSize;

                    break;
                case false when _scrollBar.OtherScrollBarView is { }:
                    if (_scrollBar.OtherScrollBarView.Size == _scrollRowsSize)
                    {
                        return;
                    }

                    _scrollBar.OtherScrollBarView.Size = _scrollRowsSize;

                    break;
            }

            SetNeedsDisplay ();
        }
    }

    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll indicator.</summary>
    /// <value><c>true</c> if show vertical or horizontal scroll indicator; otherwise, <c>false</c>.</value>
    public bool ScrollShowScrollIndicator
    {
        get => _scrollBar.ShowScrollIndicator;
        set => _scrollBar.ShowScrollIndicator = value;
    }

    /// <summary>
    ///     Determines the top offset on scrolling.
    /// </summary>
    public virtual int ScrollTopOffset
    {
        get => _scrollTopOffset;
        set
        {
            if (!UseContentOffset)
            {
                _scrollTopOffset = value;

                if (_scrollBar is null)
                {
                    return;
                }

                if (_scrollBar.IsVertical && _scrollBar.Position != _scrollTopOffset)
                {
                    _scrollBar.Position = _scrollTopOffset;
                }
                else if (_scrollBar is { OtherScrollBarView.IsVertical: true } && _scrollBar?.OtherScrollBarView.Position != _scrollTopOffset)
                {
                    _scrollBar!.OtherScrollBarView.Position = _scrollTopOffset;
                }
            }
        }
    }

    /// <summary>
    ///     Determines if negative bounds location is allowed for scrolling the <see cref="GetVisibleContentArea"/>.
    /// </summary>
    public bool UseContentOffset { get; set; }

    private void AddEventHandlersForScrollBars (ScrollBarView scrollBar)
    {
        if (scrollBar is null)
        {
            return;
        }

        scrollBar.ChangedPosition += ScrollBar_ChangedPosition;

        if (_scrollBar.OtherScrollBarView != null)
        {
            _scrollBar.OtherScrollBarView.ChangedPosition += OtherScrollBarView_ChangedPosition;
        }
    }

    private void AddKeyBindingsForScrolling (ScrollBarView scrollBar)
    {
        if (scrollBar.IsVertical)
        {
            // Things this view knows how to do
            scrollBar.AddCommand (
                                  Command.ScrollDown,
                                  () =>
                                  {
                                      if (scrollBar.IsVertical)
                                      {
                                          scrollBar.Position++;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBarView.Position++;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.ScrollUp,
                                  () =>
                                  {
                                      if (scrollBar.IsVertical)
                                      {
                                          scrollBar.Position--;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBarView.Position--;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.TopHome,
                                  () =>
                                  {
                                      if (scrollBar.IsVertical)
                                      {
                                          scrollBar.Position = 0;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBarView.Position = 0;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.BottomEnd,
                                  () =>
                                  {
                                      if (scrollBar.IsVertical)
                                      {
                                          scrollBar.Position = ScrollRowsSize;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBarView.Position = ScrollRowsSize;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.PageDown,
                                  () =>
                                  {
                                      if (scrollBar.IsVertical)
                                      {
                                          scrollBar.Position += GetVisibleContentArea().Height;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBarView.Position += GetVisibleContentArea().Height;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.PageUp,
                                  () =>
                                  {
                                      if (scrollBar.IsVertical)
                                      {
                                          scrollBar.Position -= GetVisibleContentArea().Height;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBarView.Position -= GetVisibleContentArea().Height;

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
                                      if (!scrollBar.IsVertical)
                                      {
                                          scrollBar.Position--;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBarView.Position--;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.Right,
                                  () =>
                                  {
                                      if (!scrollBar.IsVertical)
                                      {
                                          scrollBar.Position++;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBarView.Position++;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.LeftHome,
                                  () =>
                                  {
                                      if (!scrollBar.IsVertical)
                                      {
                                          scrollBar.Position = 0;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBarView.Position = 0;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.RightEnd,
                                  () =>
                                  {
                                      if (!scrollBar.IsVertical)
                                      {
                                          scrollBar.Position = ScrollColsSize;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBarView.Position = ScrollColsSize;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.PageRight,
                                  () =>
                                  {
                                      if (!scrollBar.IsVertical)
                                      {
                                          scrollBar.Position += GetVisibleContentArea().Width;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBarView.Position += GetVisibleContentArea().Width;

                                          return true;
                                      }

                                      return false;
                                  });

            scrollBar.AddCommand (
                                  Command.PageLeft,
                                  () =>
                                  {
                                      if (!scrollBar.IsVertical)
                                      {
                                          scrollBar.Position -= GetVisibleContentArea().Width;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBarView.Position -= GetVisibleContentArea ().Width;

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
        if (_scrollBar is null)
        {
            return;
        }

        _scrollBar.ChangedPosition -= ScrollBar_ChangedPosition;

        if (_scrollBar.OtherScrollBarView != null)
        {
            _scrollBar.OtherScrollBarView.ChangedPosition -= OtherScrollBarView_ChangedPosition;
        }

        _scrollBar.RemoveAll ();
        _scrollBar = null;
    }

    private void OtherScrollBarView_ChangedPosition (object sender, EventArgs e) { SetBoundsByPosition (_scrollBar.OtherScrollBarView); }

    private void ScrollBar_ChangedPosition (object sender, EventArgs e) { SetBoundsByPosition (_scrollBar); }

    private void SetBoundsByPosition (ScrollBarView scrollBar)
    {
        if (scrollBar.IsVertical)
        {
            if (UseContentOffset)
            {
                ContentOffset = new Point (Bounds.X, -scrollBar.Position);

                if (Bounds.Y != -scrollBar.Position)
                {
                    scrollBar.Position = Bounds.Y;
                }
            }
            else
            {
                if (ScrollTopOffset != scrollBar.Position)
                {
                    ScrollTopOffset = scrollBar.Position;
                }
            }
        }
        else
        {
            if (UseContentOffset)
            {
                ContentOffset = new Point (-scrollBar.Position, Bounds.Y);

                if (Bounds.X != -scrollBar.Position)
                {
                    scrollBar.Position = Bounds.X;
                }
            }
            else
            {
                if (ScrollLeftOffset != scrollBar.Position)
                {
                    ScrollLeftOffset = scrollBar.Position;
                }
            }
        }

        SetTextFormatterSize ();
        SetNeedsDisplay ();
    }
}
