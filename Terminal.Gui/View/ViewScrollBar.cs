namespace Terminal.Gui;

/// <summary>
///     The scroll bar types used by this <see cref="ScrollBar"/>.
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
    private ScrollBar _scrollBar;
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
            if (_scrollBar is { } && _scrollBarType == value)
            {
                return;
            }

            _scrollBarType = value;
            DisposeScrollBar ();

            switch (_scrollBarType)
            {
                case ScrollBarType.Vertical:
                    _scrollBar = new ScrollBar { IsVertical = true };

                    break;
                case ScrollBarType.Horizontal:
                    _scrollBar = new ScrollBar { IsVertical = false };

                    break;
                case ScrollBarType.Both:
                    _scrollBar = new ScrollBar { IsVertical = true };
                    _scrollBar.OtherScrollBar = new ScrollBar { IsVertical = false, OtherScrollBar = _scrollBar };

                    break;
                case ScrollBarType.None:
                    return;
                default:
                    throw new ArgumentOutOfRangeException ();
            }

            Padding.Add (_scrollBar);
            AddEventHandlersForScrollBars ();
            AddKeyBindingsForScrolling (_scrollBar);

            if (_scrollBar.OtherScrollBar != null)
            {
                AddKeyBindingsForScrolling (_scrollBar.OtherScrollBar);
            }

            SetNeedsDisplay ();
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
                case true when _scrollBar.OtherScrollBar is { }:
                    if (_scrollBar.OtherScrollBar.Size == _scrollColsSize)
                    {
                        return;
                    }

                    _scrollBar.OtherScrollBar.Size = _scrollColsSize;

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

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBar"/></summary>
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
            if (!UseNegativeBoundsLocation)
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
                else if (_scrollBar is { OtherScrollBar.IsVertical: false } && _scrollBar?.OtherScrollBar.Position != _scrollLeftOffset)
                {
                    _scrollBar!.OtherScrollBar.Position = _scrollLeftOffset;
                }
            }
        }
    }

    /// <summary>Represent a vertical or horizontal ScrollBar other than this.</summary>
    public ScrollBar ScrollOtherScrollBar
    {
        get => _scrollBar.OtherScrollBar;
        set => _scrollBar.OtherScrollBar = value;
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
                case false when _scrollBar.OtherScrollBar is { }:
                    if (_scrollBar.OtherScrollBar.Size == _scrollRowsSize)
                    {
                        return;
                    }

                    _scrollBar.OtherScrollBar.Size = _scrollRowsSize;

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
            if (!UseNegativeBoundsLocation)
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
                else if (_scrollBar is { OtherScrollBar.IsVertical: true } && _scrollBar?.OtherScrollBar.Position != _scrollTopOffset)
                {
                    _scrollBar!.OtherScrollBar.Position = _scrollTopOffset;
                }
            }
        }
    }

    /// <summary>
    ///     Determines if negative bounds location is allowed for scrolling the <see cref="ContentArea"/>.
    /// </summary>
    public bool UseNegativeBoundsLocation { get; set; }

    private void AddEventHandlersForScrollBars ()
    {
        if (_scrollBar is null)
        {
            return;
        }

        _scrollBar.ChangedPosition += ScrollBar_ChangedPosition;

        if (_scrollBar.OtherScrollBar != null)
        {
            _scrollBar.OtherScrollBar.ChangedPosition += OtherScrollBar_ChangedPosition;
        }
    }

    private void AddKeyBindingsForScrolling (ScrollBar scrollBar)
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

                                      if (scrollBar.OtherScrollBar is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBar.Position++;

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

                                      if (scrollBar.OtherScrollBar is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBar.Position--;

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

                                      if (scrollBar.OtherScrollBar is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBar.Position = 0;

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

                                      if (scrollBar.OtherScrollBar is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBar.Position = ScrollRowsSize;

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
                                          scrollBar.Position += ContentArea.Height;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBar is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBar.Position += ContentArea.Height;

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
                                          scrollBar.Position -= ContentArea.Height;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBar is { IsVertical: true })
                                      {
                                          scrollBar.OtherScrollBar.Position -= ContentArea.Height;

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

                                      if (scrollBar.OtherScrollBar is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBar.Position--;

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

                                      if (scrollBar.OtherScrollBar is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBar.Position++;

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

                                      if (scrollBar.OtherScrollBar is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBar.Position = 0;

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

                                      if (scrollBar.OtherScrollBar is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBar.Position = ScrollColsSize;

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
                                          scrollBar.Position += ContentArea.Width;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBar is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBar.Position += ContentArea.Width;

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
                                          scrollBar.Position -= ContentArea.Width;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBar is { IsVertical: false })
                                      {
                                          scrollBar.OtherScrollBar.Position -= ContentArea.Width;

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

        if (_scrollBar.OtherScrollBar != null)
        {
            _scrollBar.OtherScrollBar.ChangedPosition -= OtherScrollBar_ChangedPosition;
        }

        _scrollBar.RemoveAll ();
        _scrollBar = null;
    }

    private void OtherScrollBar_ChangedPosition (object sender, EventArgs e) { SetBoundsByPosition (_scrollBar.OtherScrollBar); }

    private void ScrollBar_ChangedPosition (object sender, EventArgs e) { SetBoundsByPosition (_scrollBar); }

    private void SetBoundsByPosition (ScrollBar scrollBar)
    {
        if (scrollBar.IsVertical)
        {
            if (UseNegativeBoundsLocation)
            {
                Bounds = Bounds with { Y = -scrollBar.Position };
                Bounds = Bounds with { Height = Math.Min (Bounds.Height + scrollBar.Position, ScrollRowsSize) };

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
            if (UseNegativeBoundsLocation)
            {
                Bounds = Bounds with { X = -scrollBar.Position };
                Bounds = Bounds with { Width = Math.Min (Bounds.Width + scrollBar.Position, ScrollColsSize) };

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
