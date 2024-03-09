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
                    view._scrollBar = new ScrollBarView { Orientation = Orientation.Vertical };

                    break;
                case ScrollBarType.Horizontal:
                    view._scrollBar = new ScrollBarView { Orientation = Orientation.Horizontal };

                    break;
                case ScrollBarType.Both:
                    view._scrollBar = new ScrollBarView { Orientation = Orientation.Vertical };
                    view._scrollBar.OtherScrollBarView = new ScrollBarView { Orientation = Orientation.Horizontal, OtherScrollBarView = view._scrollBar };

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

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBarView"/></summary>
    public bool ScrollKeepContentAlwaysInViewPort
    {
        get => _scrollBar.KeepContentAlwaysInViewPort;
        set => _scrollBar.KeepContentAlwaysInViewPort = value;
    }

    /// <summary>Represent a vertical or horizontal ScrollBarView other than this.</summary>
    public ScrollBarView ScrollOtherScrollBarView
    {
        get => _scrollBar.OtherScrollBarView;
        set => _scrollBar.OtherScrollBarView = value;
    }

    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll indicator.</summary>
    /// <value><c>true</c> if show vertical or horizontal scroll indicator; otherwise, <c>false</c>.</value>
    public bool ScrollShowScrollIndicator
    {
        get => _scrollBar.ShowScrollIndicator;
        set => _scrollBar.ShowScrollIndicator = value;
    }

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

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Vertical })
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
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position--;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Vertical })
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
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position = 0;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Vertical })
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
                                      if (scrollBar.Orientation == Orientation.Vertical)
                                      {
                                          scrollBar.Position = ContentSize.Height;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Vertical })
                                      {
                                          scrollBar.OtherScrollBarView.Position = ContentSize.Height;

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

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Vertical })
                                      {
                                          scrollBar.OtherScrollBarView.Position += GetVisibleContentArea ().Height;

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

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Vertical })
                                      {
                                          scrollBar.OtherScrollBarView.Position -= GetVisibleContentArea ().Height;

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

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Horizontal })
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
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position++;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Horizontal })
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
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position = 0;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Horizontal })
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
                                      if (scrollBar.Orientation == Orientation.Horizontal)
                                      {
                                          scrollBar.Position = ContentSize.Width;

                                          return true;
                                      }

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Horizontal })
                                      {
                                          scrollBar.OtherScrollBarView.Position = ContentSize.Width;

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

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Horizontal })
                                      {
                                          scrollBar.OtherScrollBarView.Position += GetVisibleContentArea ().Width;

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

                                      if (scrollBar.OtherScrollBarView is { Orientation: Orientation.Horizontal })
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
}
