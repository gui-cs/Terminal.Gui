//
// ScrollBarView.cs: ScrollBarView view.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

namespace Terminal.Gui;

/// <summary>ScrollBarViews are views that display a 1-character scrollbar, either horizontal or vertical</summary>
/// <remarks>
///     <para>
///         The scrollbar is drawn to be a representation of the Size, assuming that the scroll position is set at
///         Position.
///     </para>
///     <para>If the region to display the scrollbar is larger than three characters, arrow indicators are drawn.</para>
/// </remarks>
public class ScrollBarView : View
{
    private bool _autoHideScrollBars = true;
    private View _contentBottomRightCorner;
    private bool _hosted;
    private bool _keepContentAlwaysInViewport = true;
    private int _lastLocation = -1;
    private ScrollBarView _otherScrollBarView;
    private int _posBarOffset;
    private int _posBottomTee;
    private int _posLeftTee;
    private int _posRightTee;
    private int _posTopTee;
    private bool _showScrollIndicator;
    private int _size, _position;
    private bool _vertical;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class.
    /// </summary>
    public ScrollBarView ()
    {
        WantContinuousButtonPressed = true;

        Added += (s, e) => CreateBottomRightCorner (e.SuperView);
        Initialized += ScrollBarView_Initialized;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class.
    /// </summary>
    /// <param name="host">The view that will host this scrollbar.</param>
    /// <param name="isVertical">If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.</param>
    /// <param name="showBothScrollIndicator">
    ///     If set to <c>true (default)</c> will have the other scrollbar, otherwise will
    ///     have only one.
    /// </param>
    public ScrollBarView (View host, bool isVertical, bool showBothScrollIndicator = true)
    {
        if (host is null)
        {
            throw new ArgumentNullException ("The host parameter can't be null.");
        }

        if (host.SuperView is null)
        {
            throw new ArgumentNullException ("The host SuperView parameter can't be null.");
        }

        _hosted = true;
        IsVertical = isVertical;
        ColorScheme = host.ColorScheme;
        X = isVertical ? Pos.Right (host) - 1 : Pos.Left (host);
        Y = isVertical ? Pos.Top (host) : Pos.Bottom (host) - 1;
        Host = host;
        CanFocus = false;
        Enabled = host.Enabled;
        Visible = host.Visible;
        Initialized += ScrollBarView_Initialized;

        //Host.CanFocusChanged += Host_CanFocusChanged;
        Host.EnabledChanged += Host_EnabledChanged;
        Host.VisibleChanged += Host_VisibleChanged;
        Host.SuperView.Add (this);
        AutoHideScrollBars = true;

        if (showBothScrollIndicator)
        {
            OtherScrollBarView = new ScrollBarView
            {
                IsVertical = !isVertical,
                ColorScheme = host.ColorScheme,
                Host = host,
                CanFocus = false,
                Enabled = host.Enabled,
                Visible = host.Visible,
                OtherScrollBarView = this
            };
            OtherScrollBarView._hosted = true;
            OtherScrollBarView.X = OtherScrollBarView.IsVertical ? Pos.Right (host) - 1 : Pos.Left (host);
            OtherScrollBarView.Y = OtherScrollBarView.IsVertical ? Pos.Top (host) : Pos.Bottom (host) - 1;
            OtherScrollBarView.Host.SuperView.Add (OtherScrollBarView);
            OtherScrollBarView.ShowScrollIndicator = true;
        }

        ShowScrollIndicator = true;
        CreateBottomRightCorner (Host);
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
                SetNeedsDisplay ();
            }
        }
    }

    // BUGBUG: v2 - for consistency this should be named "Parent" not "Host"
    /// <summary>Get or sets the view that host this <see cref="ScrollBarView"/></summary>
    public View Host { get; internal set; }

    /// <summary>If set to <c>true</c> this is a vertical scrollbar, otherwise, the scrollbar is horizontal.</summary>
    public bool IsVertical
    {
        get => _vertical;
        set
        {
            _vertical = value;

            if (IsInitialized)
            {
                SetWidthHeight ();
            }
        }
    }

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBarView"/></summary>
    public bool KeepContentAlwaysInViewport
    {
        get => _keepContentAlwaysInViewport;
        set
        {
            if (_keepContentAlwaysInViewport != value)
            {
                _keepContentAlwaysInViewport = value;
                var pos = 0;

                if (value && !_vertical && _position + Host.Viewport.Width > _size)
                {
                    pos = _size - Host.Viewport.Width + (_showBothScrollIndicator ? 1 : 0);
                }

                if (value && _vertical && _position + Host.Viewport.Height > _size)
                {
                    pos = _size - Host.Viewport.Height + (_showBothScrollIndicator ? 1 : 0);
                }

                if (pos != 0)
                {
                    Position = pos;
                }

                if (OtherScrollBarView is { } && OtherScrollBarView._keepContentAlwaysInViewport != value)
                {
                    OtherScrollBarView.KeepContentAlwaysInViewport = value;
                }

                if (pos == 0)
                {
                    Refresh ();
                }
            }
        }
    }

    /// <summary>Represent a vertical or horizontal ScrollBarView other than this.</summary>
    public ScrollBarView OtherScrollBarView
    {
        get => _otherScrollBarView;
        set
        {
            if (value is { } && ((value.IsVertical && _vertical) || (!value.IsVertical && !_vertical)))
            {
                throw new ArgumentException (
                                             $"There is already a {(_vertical ? "vertical" : "horizontal")} ScrollBarView."
                                            );
            }

            _otherScrollBarView = value;
        }
    }

    /// <summary>The position, relative to <see cref="Size"/>, to set the scrollbar at.</summary>
    /// <value>The position.</value>
    public int Position
    {
        get => _position;
        set
        {
            if (_position == value)
            {
                return;
            }

            SetPosition (value);
        }
    }

    // BUGBUG: v2 - Why can't we get rid of this and just use Visible?
    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll indicator.</summary>
    /// <value><c>true</c> if show vertical or horizontal scroll indicator; otherwise, <c>false</c>.</value>
    public bool ShowScrollIndicator
    {
        get => _showScrollIndicator && Visible;
        set
        {
            //if (value == showScrollIndicator) {
            //	return;
            //}

            _showScrollIndicator = value;

            if (IsInitialized)
            {
                SetNeedsLayout ();

                if (value)
                {
                    Visible = true;
                }
                else
                {
                    Visible = false;
                    Position = 0;
                }

                SetWidthHeight ();
            }
        }
    }

    /// <summary>The size of content the scrollbar represents.</summary>
    /// <value>The size.</value>
    /// <remarks>
    ///     The <see cref="Size"/> is typically the size of the virtual content. E.g. when a Scrollbar is part of a
    ///     <see cref="View"/> the Size is set to the appropriate dimension of <see cref="Host"/>.
    /// </remarks>
    public int Size
    {
        get => _size;
        set
        {
            _size = value;

            if (IsInitialized)
            {
                SetRelativeLayout (SuperView?.Frame.Size ?? Host.Frame.Size);
                ShowHideScrollBars (false);
                SetNeedsDisplay ();
            }
        }
    }

    private bool _showBothScrollIndicator => OtherScrollBarView?.ShowScrollIndicator == true && ShowScrollIndicator;

    /// <summary>This event is raised when the position on the scrollbar has changed.</summary>
    public event EventHandler ChangedPosition;

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags != MouseFlags.Button1Pressed
            && mouseEvent.Flags != MouseFlags.Button1DoubleClicked
            && !mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)
            && mouseEvent.Flags != MouseFlags.Button1Released
            && mouseEvent.Flags != MouseFlags.WheeledDown
            && mouseEvent.Flags != MouseFlags.WheeledUp
            && mouseEvent.Flags != MouseFlags.WheeledRight
            && mouseEvent.Flags != MouseFlags.WheeledLeft
            && mouseEvent.Flags != MouseFlags.Button1TripleClicked)
        {
            return false;
        }

        if (!Host.CanFocus)
        {
            return true;
        }

        if (Host?.HasFocus == false)
        {
            Host.SetFocus ();
        }

        int location = _vertical ? mouseEvent.Position.Y : mouseEvent.Position.X;
        int barsize = _vertical ? Viewport.Height : Viewport.Width;
        int posTopLeftTee = _vertical ? _posTopTee + 1 : _posLeftTee + 1;
        int posBottomRightTee = _vertical ? _posBottomTee + 1 : _posRightTee + 1;
        barsize -= 2;
        int pos = Position;

        if (mouseEvent.Flags != MouseFlags.Button1Released && (Application.MouseGrabView is null || Application.MouseGrabView != this))
        {
            Application.GrabMouse (this);
        }
        else if (mouseEvent.Flags == MouseFlags.Button1Released && Application.MouseGrabView is { } && Application.MouseGrabView == this)
        {
            _lastLocation = -1;
            Application.UngrabMouse ();

            return true;
        }

        if (ShowScrollIndicator
            && (mouseEvent.Flags == MouseFlags.WheeledDown
                || mouseEvent.Flags == MouseFlags.WheeledUp
                || mouseEvent.Flags == MouseFlags.WheeledRight
                || mouseEvent.Flags == MouseFlags.WheeledLeft))
        {
            return Host.NewMouseEvent (mouseEvent) == true;
        }

        if (mouseEvent.Flags == MouseFlags.Button1Pressed && location == 0)
        {
            if (pos > 0)
            {
                Position = pos - 1;
            }
        }
        else if (mouseEvent.Flags == MouseFlags.Button1Pressed && location == barsize + 1)
        {
            if (CanScroll (1, out _, _vertical))
            {
                Position = pos + 1;
            }
        }
        else if (location > 0 && location < barsize + 1)
        {
            //var b1 = pos * (Size > 0 ? barsize / Size : 0);
            //var b2 = Size > 0
            //	? (KeepContentAlwaysInViewport ? Math.Min (((pos + barsize) * barsize / Size) + 1, barsize - 1) : (pos + barsize) * barsize / Size)
            //	: 0;
            //if (KeepContentAlwaysInViewport && b1 == b2) {
            //	b1 = Math.Max (b1 - 1, 0);
            //}

            if (_lastLocation > -1
                || (location >= posTopLeftTee
                    && location <= posBottomRightTee
                    && mouseEvent.Flags.HasFlag (
                                                 MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition
                                                )))
            {
                if (_lastLocation == -1)
                {
                    _lastLocation = location;

                    _posBarOffset = _keepContentAlwaysInViewport
                                        ? Math.Max (location - posTopLeftTee, 1)
                                        : 0;

                    return true;
                }

                if (location > _lastLocation)
                {
                    if (location - _posBarOffset < barsize)
                    {
                        int np = (location - _posBarOffset) * Size / barsize + Size / barsize;

                        if (CanScroll (np - pos, out int nv, _vertical))
                        {
                            Position = pos + nv;
                        }
                    }
                    else if (CanScroll (Size - pos, out int nv, _vertical))
                    {
                        Position = Math.Min (pos + nv, Size);
                    }
                }
                else if (location < _lastLocation)
                {
                    if (location - _posBarOffset > 0)
                    {
                        int np = (location - _posBarOffset) * Size / barsize - Size / barsize;

                        if (CanScroll (np - pos, out int nv, _vertical))
                        {
                            Position = pos + nv;
                        }
                    }
                    else
                    {
                        Position = 0;
                    }
                }
                else if (location - _posBarOffset >= barsize && posBottomRightTee - posTopLeftTee >= 3 && CanScroll (Size - pos, out int nv, _vertical))
                {
                    Position = Math.Min (pos + nv, Size);
                }
                else if (location - _posBarOffset >= barsize - 1 && posBottomRightTee - posTopLeftTee <= 3 && CanScroll (Size - pos, out nv, _vertical))
                {
                    Position = Math.Min (pos + nv, Size);
                }
                else if (location - _posBarOffset <= 0 && posBottomRightTee - posTopLeftTee <= 3)
                {
                    Position = 0;
                }
            }
            else if (location > posBottomRightTee)
            {
                if (CanScroll (barsize, out int nv, _vertical))
                {
                    Position = pos + nv;
                }
            }
            else if (location < posTopLeftTee)
            {
                if (CanScroll (-barsize, out int nv, _vertical))
                {
                    Position = pos + nv;
                }
            }
            else if (location == 1 && posTopLeftTee <= 3)
            {
                Position = 0;
            }
            else if (location == barsize)
            {
                if (CanScroll (Size - pos, out int nv, _vertical))
                {
                    Position = Math.Min (pos + nv, Size);
                }
            }
        }

        return true;
    }

    /// <summary>Virtual method to invoke the <see cref="ChangedPosition"/> action event.</summary>
    public virtual void OnChangedPosition () { ChangedPosition?.Invoke (this, EventArgs.Empty); }

    /// <inheritdoc/>
    public override void OnDrawContent (Rectangle viewport)
    {
        if (ColorScheme is null || ((!ShowScrollIndicator || Size == 0) && AutoHideScrollBars && Visible))
        {
            if ((!ShowScrollIndicator || Size == 0) && AutoHideScrollBars && Visible)
            {
                ShowHideScrollBars (false);
            }

            return;
        }

        if (Size == 0 || (_vertical && Viewport.Height == 0) || (!_vertical && Viewport.Width == 0))
        {
            return;
        }

        Driver.SetAttribute (Host.HasFocus ? ColorScheme.Focus : GetNormalColor ());

        if (_vertical)
        {
            if (Viewport.Right < Viewport.Width - 1)
            {
                return;
            }

            int col = Viewport.Width - 1;
            int bh = Viewport.Height;
            Rune special;

            if (bh < 4)
            {
                int by1 = _position * bh / Size;
                int by2 = (_position + bh) * bh / Size;

                Move (col, 0);

                if (Viewport.Height == 1)
                {
                    Driver.AddRune (Glyphs.Diamond);
                }
                else
                {
                    Driver.AddRune (Glyphs.UpArrow);
                }

                if (Viewport.Height == 3)
                {
                    Move (col, 1);
                    Driver.AddRune (Glyphs.Diamond);
                }

                if (Viewport.Height > 1)
                {
                    Move (col, Viewport.Height - 1);
                    Driver.AddRune (Glyphs.DownArrow);
                }
            }
            else
            {
                bh -= 2;

                int by1 = KeepContentAlwaysInViewport
                              ? _position * bh / Size
                              : _position * bh / (Size + bh);

                int by2 = KeepContentAlwaysInViewport
                              ? Math.Min ((_position + bh) * bh / Size + 1, bh - 1)
                              : (_position + bh) * bh / (Size + bh);

                if (KeepContentAlwaysInViewport && by1 == by2)
                {
                    by1 = Math.Max (by1 - 1, 0);
                }

                AddRune (col, 0, Glyphs.UpArrow);

                var hasTopTee = false;
                var hasDiamond = false;
                var hasBottomTee = false;

                for (var y = 0; y < bh; y++)
                {

                    if ((y < by1 || y > by2) && ((_position > 0 && !hasTopTee) || (hasTopTee && hasBottomTee)))
                    {
                        special = Glyphs.Stipple;
                    }
                    else
                    {
                        if (y != by2 && y > 1 && by2 - by1 == 0 && by1 < bh - 1 && hasTopTee && !hasDiamond)
                        {
                            hasDiamond = true;
                            special = Glyphs.Diamond;
                        }
                        else
                        {
                            if (y == by1 && !hasTopTee)
                            {
                                hasTopTee = true;
                                _posTopTee = y;
                                special = Glyphs.TopTee;
                            }
                            else if (((_position == 0 && y == bh - 1) || y >= by2 || by2 == 0) && !hasBottomTee)
                            {
                                hasBottomTee = true;
                                _posBottomTee = y;
                                special = Glyphs.BottomTee;
                            }
                            else
                            {
                                special = Glyphs.VLine;
                            }
                        }
                    }

                    AddRune (col, y + 1, special);
                }

                if (!hasTopTee)
                {
                    AddRune (col, Viewport.Height - 2, Glyphs.TopTee);
                }

                AddRune (col, Viewport.Height - 1, Glyphs.DownArrow);
            }
        }
        else
        {
            if (Viewport.Bottom < Viewport.Height - 1)
            {
                return;
            }

            int row = Viewport.Height - 1;
            int bw = Viewport.Width;
            Rune special;

            if (bw < 4)
            {
                int bx1 = _position * bw / Size;
                int bx2 = (_position + bw) * bw / Size;

                Move (0, row);
                Driver.AddRune (Glyphs.LeftArrow);
                Driver.AddRune (Glyphs.RightArrow);
            }
            else
            {
                bw -= 2;

                int bx1 = KeepContentAlwaysInViewport
                              ? _position * bw / Size
                              : _position * bw / (Size + bw);

                int bx2 = KeepContentAlwaysInViewport
                              ? Math.Min ((_position + bw) * bw / Size + 1, bw - 1)
                              : (_position + bw) * bw / (Size + bw);

                if (KeepContentAlwaysInViewport && bx1 == bx2)
                {
                    bx1 = Math.Max (bx1 - 1, 0);
                }

                Move (0, row);
                Driver.AddRune (Glyphs.LeftArrow);

                var hasLeftTee = false;
                var hasDiamond = false;
                var hasRightTee = false;

                for (var x = 0; x < bw; x++)
                {
                    if ((x < bx1 || x >= bx2 + 1) && ((_position > 0 && !hasLeftTee) || (hasLeftTee && hasRightTee)))
                    {
                        special = Glyphs.Stipple;
                    }
                    else
                    {
                        if (x != bx2 && x > 1 && bx2 - bx1 == 0 && bx1 < bw - 1 && hasLeftTee && !hasDiamond)
                        {
                            hasDiamond = true;
                            special = Glyphs.Diamond;
                        }
                        else
                        {
                            if (x == bx1 && !hasLeftTee)
                            {
                                hasLeftTee = true;
                                _posLeftTee = x;
                                special = Glyphs.LeftTee;
                            }
                            else if (((_position == 0 && x == bw - 1) || x >= bx2 || bx2 == 0) && !hasRightTee)
                            {
                                hasRightTee = true;
                                _posRightTee = x;
                                special = Glyphs.RightTee;
                            }
                            else
                            {
                                special = Glyphs.HLine;
                            }
                        }
                    }

                    Driver.AddRune (special);
                }

                if (!hasLeftTee)
                {
                    Move (Viewport.Width - 2, row);
                    Driver.AddRune (Glyphs.LeftTee);
                }

                Driver.AddRune (Glyphs.RightArrow);
            }
        }
    }


    /// <summary>Only used for a hosted view that will update and redraw the scrollbars.</summary>
    public virtual void Refresh () { ShowHideScrollBars (); }

    internal bool CanScroll (int n, out int max, bool isVertical = false)
    {
        if (Host?.Viewport.IsEmpty != false)
        {
            max = 0;

            return false;
        }

        int s = GetBarsize (isVertical);
        int newSize = Math.Max (Math.Min (_size - s, _position + n), 0);
        max = _size > s + newSize ? newSize == 0 ? -_position : n : _size - (s + _position) - 1;

        if (_size >= s + newSize && max != 0)
        {
            return true;
        }

        return false;
    }

    private bool CheckBothScrollBars (ScrollBarView scrollBarView, bool pending = false)
    {
        int barsize = scrollBarView._vertical ? scrollBarView.Viewport.Height : scrollBarView.Viewport.Width;

        if (barsize == 0 || barsize >= scrollBarView._size)
        {
            if (scrollBarView.ShowScrollIndicator)
            {
                scrollBarView.ShowScrollIndicator = false;
            }

            if (scrollBarView.Visible)
            {
                scrollBarView.Visible = false;
            }
        }
        else if (barsize > 0 && barsize == scrollBarView._size && scrollBarView.OtherScrollBarView is { } && pending)
        {
            if (scrollBarView.ShowScrollIndicator)
            {
                scrollBarView.ShowScrollIndicator = false;
            }

            if (scrollBarView.Visible)
            {
                scrollBarView.Visible = false;
            }

            if (scrollBarView.OtherScrollBarView is { } && scrollBarView._showBothScrollIndicator)
            {
                scrollBarView.OtherScrollBarView.ShowScrollIndicator = false;
            }

            if (scrollBarView.OtherScrollBarView.Visible)
            {
                scrollBarView.OtherScrollBarView.Visible = false;
            }
        }
        else if (barsize > 0 && barsize == _size && scrollBarView.OtherScrollBarView is { } && !pending)
        {
            pending = true;
        }
        else
        {
            if (scrollBarView.OtherScrollBarView is { } && pending)
            {
                if (!scrollBarView._showBothScrollIndicator)
                {
                    scrollBarView.OtherScrollBarView.ShowScrollIndicator = true;
                }

                if (!scrollBarView.OtherScrollBarView.Visible)
                {
                    scrollBarView.OtherScrollBarView.Visible = true;
                }
            }

            if (!scrollBarView.ShowScrollIndicator)
            {
                scrollBarView.ShowScrollIndicator = true;
            }

            if (!scrollBarView.Visible)
            {
                scrollBarView.Visible = true;
            }
        }

        return pending;
    }

    private void ContentBottomRightCorner_DrawContent (object sender, DrawEventArgs e)
    {
        Driver.SetAttribute (Host.HasFocus ? ColorScheme.Focus : GetNormalColor ());

        // I'm forced to do this here because the Clear method is
        // changing the color attribute and is different of this one
        Driver.FillRect (Driver.Clip);
        e.Cancel = true;
    }

    //private void Host_CanFocusChanged ()
    //{
    //	CanFocus = Host.CanFocus;
    //	if (otherScrollBarView is { }) {
    //		otherScrollBarView.CanFocus = CanFocus;
    //	}
    //}

    private void ContentBottomRightCorner_MouseClick (object sender, MouseEventArgs me)
    {
        if (me.Flags == MouseFlags.WheeledDown
            || me.Flags == MouseFlags.WheeledUp
            || me.Flags == MouseFlags.WheeledRight
            || me.Flags == MouseFlags.WheeledLeft)
        {
            NewMouseEvent (me);
        }
        else if (me.Flags == MouseFlags.Button1Clicked)
        {
            Host.SetFocus ();
        }

        me.Handled = true;
    }

    private void CreateBottomRightCorner (View host)
    {
        if (Host is null)
        {
            Host = host;
        }

        if (Host != null
            && ((_contentBottomRightCorner is null && OtherScrollBarView is null)
                || (_contentBottomRightCorner is null && OtherScrollBarView is { } && OtherScrollBarView._contentBottomRightCorner is null)))
        {
            _contentBottomRightCorner = new ContentBottomRightCorner { Visible = Host.Visible };

            if (_hosted)
            {
                Host.SuperView.Add (_contentBottomRightCorner);
                _contentBottomRightCorner.X = Pos.Right (Host) - 1;
                _contentBottomRightCorner.Y = Pos.Bottom (Host) - 1;
            }
            else
            {
                Host.Add (_contentBottomRightCorner);
                _contentBottomRightCorner.X = Pos.AnchorEnd (1);
                _contentBottomRightCorner.Y = Pos.AnchorEnd (1);
            }

            _contentBottomRightCorner.Width = 1;
            _contentBottomRightCorner.Height = 1;
            _contentBottomRightCorner.MouseClick += ContentBottomRightCorner_MouseClick;
            _contentBottomRightCorner.DrawContent += ContentBottomRightCorner_DrawContent;
        }
    }

    private int GetBarsize (bool isVertical)
    {
        if (Host?.Viewport.IsEmpty != false)
        {
            return 0;
        }

        return isVertical ? KeepContentAlwaysInViewport
                                ? Host.Viewport.Height + (_showBothScrollIndicator ? -2 : -1)
                                : 0 :
               KeepContentAlwaysInViewport ? Host.Viewport.Width + (_showBothScrollIndicator ? -2 : -1) : 0;
    }

    private void Host_EnabledChanged (object sender, EventArgs e)
    {
        Enabled = Host.Enabled;

        if (_otherScrollBarView is { })
        {
            _otherScrollBarView.Enabled = Enabled;
        }

        _contentBottomRightCorner.Enabled = Enabled;
    }

    private void Host_VisibleChanged (object sender, EventArgs e)
    {
        if (!Host.Visible)
        {
            Visible = Host.Visible;

            if (_otherScrollBarView is { })
            {
                _otherScrollBarView.Visible = Visible;
            }

            _contentBottomRightCorner.Visible = Visible;
        }
        else
        {
            ShowHideScrollBars ();
        }
    }

    private void ScrollBarView_Initialized (object sender, EventArgs e)
    {
        SetWidthHeight ();
        SetRelativeLayout (SuperView?.Frame.Size ?? Host?.Frame.Size ?? Frame.Size);

        if (OtherScrollBarView is null)
        {
            // Only do this once if both scrollbars are enabled
            ShowHideScrollBars ();
        }

        SetPosition (Position);
    }

    // Helper to assist Initialized event handler
    private void SetPosition (int newPosition)
    {
        if (!IsInitialized)
        {
            // We're not initialized so we can't do anything fancy. Just cache value.
            _position = newPosition;

            return;
        }

        if (newPosition < 0)
        {
            _position = 0;
            SetNeedsDisplay ();

            return;
        }
        else if (CanScroll (newPosition - _position, out int max, _vertical))
        {
            if (max == newPosition - _position)
            {
                _position = newPosition;
            }
            else
            {
                _position = Math.Max (_position + max, 0);
            }
        }
        else if (max < 0)
        {
            _position = Math.Max (_position + max, 0);
        }
        else
        {
            _position = Math.Max (newPosition, 0);
        }

        OnChangedPosition ();
        SetNeedsDisplay ();
    }

    // BUGBUG: v2 - rationalize this with View.SetMinWidthHeight
    private void SetWidthHeight ()
    {
        // BUGBUG: v2 - If Host is also the ScrollBarView's superview, this is all bogus because it's not
        // supported that a view can reference it's superview's Dims. This code also assumes the host does 
        //  not have a margin/borderframe/padding.
        if (!IsInitialized || _otherScrollBarView is { IsInitialized: false })
        {
            return;
        }

        if (_showBothScrollIndicator)
        {
            Width = _vertical ? 1 :
                    Host != SuperView ? Dim.Width (Host) - 1 : Dim.Fill () - 1;
            Height = _vertical ? Host != SuperView ? Dim.Height (Host) - 1 : Dim.Fill () - 1 : 1;

            _otherScrollBarView.Width = _otherScrollBarView._vertical ? 1 :
                                        Host != SuperView ? Dim.Width (Host) - 1 : Dim.Fill () - 1;

            _otherScrollBarView.Height = _otherScrollBarView._vertical
                                             ? Host != SuperView ? Dim.Height (Host) - 1 : Dim.Fill () - 1
                                             : 1;
        }
        else if (ShowScrollIndicator)
        {
            Width = _vertical ? 1 :
                    Host != SuperView ? Dim.Width (Host) : Dim.Fill ();
            Height = _vertical ? Host != SuperView ? Dim.Height (Host) : Dim.Fill () : 1;
        }
        else if (_otherScrollBarView?.ShowScrollIndicator == true)
        {
            _otherScrollBarView.Width = _otherScrollBarView._vertical ? 1 :
                                        Host != SuperView ? Dim.Width (Host) : Dim.Fill () - 0;

            _otherScrollBarView.Height = _otherScrollBarView._vertical
                                             ? Host != SuperView ? Dim.Height (Host) : Dim.Fill () - 0
                                             : 1;
        }
    }

    private void ShowHideScrollBars (bool redraw = true)
    {
        if (!_hosted || (_hosted && !_autoHideScrollBars))
        {
            if (_contentBottomRightCorner is { } && _contentBottomRightCorner.Visible)
            {
                _contentBottomRightCorner.Visible = false;
            }
            else if (_otherScrollBarView != null
                     && _otherScrollBarView._contentBottomRightCorner != null
                     && _otherScrollBarView._contentBottomRightCorner.Visible)
            {
                _otherScrollBarView._contentBottomRightCorner.Visible = false;
            }

            return;
        }

        bool pending = CheckBothScrollBars (this);

        if (_otherScrollBarView is { })
        {
            CheckBothScrollBars (_otherScrollBarView, pending);
        }

        SetWidthHeight ();
        SetRelativeLayout (SuperView?.Frame.Size ?? Host.Frame.Size);

        if (_otherScrollBarView is { })
        {
            OtherScrollBarView.SetRelativeLayout (SuperView?.Frame.Size ?? Host.Frame.Size);
        }

        if (_showBothScrollIndicator)
        {
            if (_contentBottomRightCorner is { })
            {
                _contentBottomRightCorner.Visible = true;
            }
            else if (_otherScrollBarView is { } && _otherScrollBarView._contentBottomRightCorner is { })
            {
                _otherScrollBarView._contentBottomRightCorner.Visible = true;
            }
        }
        else if (!ShowScrollIndicator)
        {
            if (_contentBottomRightCorner is { })
            {
                _contentBottomRightCorner.Visible = false;
            }
            else if (_otherScrollBarView is { } && _otherScrollBarView._contentBottomRightCorner is { })
            {
                _otherScrollBarView._contentBottomRightCorner.Visible = false;
            }

            if (Application.MouseGrabView is { } && Application.MouseGrabView == this)
            {
                Application.UngrabMouse ();
            }
        }
        else if (_contentBottomRightCorner is { })
        {
            _contentBottomRightCorner.Visible = false;
        }
        else if (_otherScrollBarView is { } && _otherScrollBarView._contentBottomRightCorner is { })
        {
            _otherScrollBarView._contentBottomRightCorner.Visible = false;
        }

        if (Host?.Visible == true && ShowScrollIndicator && !Visible)
        {
            Visible = true;
        }

        if (Host?.Visible == true && _otherScrollBarView?.ShowScrollIndicator == true && !_otherScrollBarView.Visible)
        {
            _otherScrollBarView.Visible = true;
        }

        if (!redraw)
        {
            return;
        }

        if (ShowScrollIndicator)
        {
            Draw ();
        }

        if (_otherScrollBarView is { } && _otherScrollBarView.ShowScrollIndicator)
        {
            _otherScrollBarView.Draw ();
        }

        if (_contentBottomRightCorner is { } && _contentBottomRightCorner.Visible)
        {
            _contentBottomRightCorner.Draw ();
        }
        else if (_otherScrollBarView is { } && _otherScrollBarView._contentBottomRightCorner is { } && _otherScrollBarView._contentBottomRightCorner.Visible)
        {
            _otherScrollBarView._contentBottomRightCorner.Draw ();
        }
    }

    internal class ContentBottomRightCorner : View
    {
        public ContentBottomRightCorner ()
        {
            ColorScheme = ColorScheme;
        }
    }
}
