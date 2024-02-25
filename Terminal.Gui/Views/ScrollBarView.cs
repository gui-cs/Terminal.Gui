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
    private bool _keepContentAlwaysInViewport = true;
    private int _lastLocation = -1;
    private Orientation _orientation;
    private ScrollBarView _otherScrollBarView;
    private int _posBarOffset;
    private int _posBottomTee;
    private int _posLeftTee;
    private int _posRightTee;
    private int _posTopTee;
    private bool _showScrollIndicator;
    private int _size, _position;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Gui.ScrollBarView"/> class using
    ///     <see cref="LayoutStyle.Computed"/> layout.
    /// </summary>
    public ScrollBarView ()
    {
        ShowScrollIndicator = true;
        WantContinuousButtonPressed = true;
        ClearOnVisibleFalse = false;
        CanFocus = false;

        Added += ScrollBarView_Added;
        Initialized += ScrollBarView_Initialized;
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

    /// <summary>Get or sets if the view-port is kept always visible in the area of this <see cref="ScrollBarView"/></summary>
    public bool KeepContentAlwaysInViewPort
    {
        get => _keepContentAlwaysInViewport;
        set
        {
            if (_keepContentAlwaysInViewport != value)
            {
                _keepContentAlwaysInViewport = value;

                AdjustContentInViewport ();
            }
        }
    }

    /// <summary>Defines if a scrollbar is vertical or horizontal.</summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;

            if (IsInitialized)
            {
                SetWidthHeight ();
            }
        }
    }

    /// <summary>Represent a vertical or horizontal ScrollBarView other than this.</summary>
    public ScrollBarView OtherScrollBarView
    {
        get => _otherScrollBarView;
        set
        {
            if (value is { }
                && ((value.Orientation == Orientation.Vertical && _orientation == Orientation.Vertical)
                    || (value.Orientation == Orientation.Horizontal && _orientation == Orientation.Horizontal)))
            {
                throw new ArgumentException (
                                             $"There is already a {(_orientation == Orientation.Vertical ? "vertical" : "horizontal")} ScrollBarView."
                                            );
            }

            _otherScrollBarView = value;
            _otherScrollBarView._otherScrollBarView = this;

            if (SuperView != null && _otherScrollBarView?.SuperView is null && !SuperView.Subviews.Contains (_otherScrollBarView))
            {
                SuperView.Add (_otherScrollBarView);
            }
        }
    }

    /// <summary>The position, relative to <see cref="Size"/>, to set the scrollbar at.</summary>
    /// <value>The position.</value>
    public int Position
    {
        get => _position;
        set
        {
            if (IsInitialized)
            {
                // We're not initialized so we can't do anything fancy. Just cache value.
                SetPosition (value);
            }
            else
            {
                _position = value;
            }
        }
    }

    // BUGBUG: v2 - Why can't we get rid of this and just use Visible?
    // We need this property to distinguish from Visible which will also affect the parent
    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll indicator.</summary>
    /// <value><c>true</c> if show vertical or horizontal scroll indicator; otherwise, <c>false</c>.</value>
    public bool ShowScrollIndicator
    {
        get => _showScrollIndicator;
        set
        {
            _showScrollIndicator = value;

            if (value)
            {
                Visible = true;
            }
            else
            {
                Visible = false;
            }

            SetNeedsDisplay ();
        }
    }

    /// <summary>The size of content the scrollbar represents.</summary>
    /// <value>The size.</value>
    /// <remarks>
    ///     The <see cref="Size"/> is typically the size of the virtual content. E.g. when a Scrollbar is part of a
    ///     <see cref="View"/> the Size is set to the appropriate dimension of virtual space.
    /// </remarks>
    public int Size
    {
        get => _size;
        set
        {
            _size = value;

            if (IsInitialized)
            {
                ShowHideScrollBars (false);
            }
        }
    }

    private bool _showBothScrollIndicator => OtherScrollBarView?._showScrollIndicator == true && _showScrollIndicator;

    private bool IsBuiltIn => SuperView is Adornment;

    /// <summary>This event is raised when the position on the scrollbar has changed.</summary>
    public event EventHandler ChangedPosition;

    /// <inheritdoc/>
    public override bool MouseEvent (MouseEvent mouseEvent)
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

        View host = SuperView is Adornment adornment ? adornment.Parent : SuperView;

        if (!host.CanFocus)
        {
            return true;
        }

        if (host?.HasFocus == false)
        {
            host.SetFocus ();
        }

        int location = _orientation == Orientation.Vertical ? mouseEvent.Y : mouseEvent.X;
        int barsize = _orientation == Orientation.Vertical ? ContentArea.Height : ContentArea.Width;
        int posTopLeftTee = _orientation == Orientation.Vertical ? _posTopTee + 1 : _posLeftTee + 1;
        int posBottomRightTee = _orientation == Orientation.Vertical ? _posBottomTee + 1 : _posRightTee + 1;
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

        if (_showScrollIndicator
            && (mouseEvent.Flags == MouseFlags.WheeledDown
                || mouseEvent.Flags == MouseFlags.WheeledUp
                || mouseEvent.Flags == MouseFlags.WheeledRight
                || mouseEvent.Flags == MouseFlags.WheeledLeft))
        {
            return host.MouseEvent (mouseEvent);
        }

        if (_lastLocation == -1 && mouseEvent.Flags == MouseFlags.Button1Pressed && location == 0)
        {
            if (pos > 0)
            {
                Position = pos - 1;
            }
        }
        else if (_lastLocation == -1 && mouseEvent.Flags == MouseFlags.Button1Pressed && location == barsize + 1)
        {
            if (CanScroll (pos + 1, out _, _orientation))
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
                    if (location == barsize)
                    {
                        Position = Size;
                    }
                    else if (location - _posBarOffset < barsize)
                    {
                        int np = (location - _posBarOffset) * Size / barsize + Size / barsize;

                        if (CanScroll (np, out int nv, _orientation))
                        {
                            Position = pos + nv;
                        }
                    }
                    else if (CanScroll (Size, out int nv, _orientation))
                    {
                        Position = Math.Min (pos + nv, Size);
                    }
                }
                else if (location < _lastLocation)
                {
                    if (location - _posBarOffset > 0)
                    {
                        int np = (location - _posBarOffset) * Size / barsize - Size / barsize;

                        if (CanScroll (np, out int nv, _orientation))
                        {
                            Position = pos + nv;
                        }
                    }
                    else
                    {
                        Position = 0;
                    }
                }
                else if (location == _lastLocation)
                {
                    Position = Size;
                }
                else if (location - _posBarOffset >= barsize && posBottomRightTee - posTopLeftTee >= 3 && CanScroll (Size - pos, out int nv, _orientation))
                {
                    Position = Math.Min (pos + nv, Size);
                }
                else if (location - _posBarOffset >= barsize - 1 && posBottomRightTee - posTopLeftTee <= 3 && CanScroll (Size - pos, out nv, _orientation))
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
                if (CanScroll (pos + barsize, out int nv, _orientation))
                {
                    Position = pos + nv;
                }
            }
            else if (location < posTopLeftTee)
            {
                if (CanScroll (pos - barsize, out int nv, _orientation))
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
                if (CanScroll (Size, out int nv, _orientation))
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
    public override void OnDrawContent (Rectangle contentArea)
    {
        if (ColorScheme is null || ((!_showScrollIndicator || Size == 0) && AutoHideScrollBars && Visible))
        {
            if ((!_showScrollIndicator || Size == 0) && AutoHideScrollBars && Visible)
            {
                ShowHideScrollBars (false);
            }

            return;
        }

        if (Size == 0
            || (_orientation == Orientation.Vertical && ContentArea.Height == 0)
            || (_orientation == Orientation.Horizontal && ContentArea.Width == 0))
        {
            return;
        }

        if (IsBuiltIn)
        {
            Driver.SetAttribute (((Adornment)SuperView).Parent.HasFocus ? ColorScheme.Focus : GetNormalColor ());
        }
        else
        {
            Driver.SetAttribute (SuperView.HasFocus ? ColorScheme.Focus : GetNormalColor ());
        }

        if (_orientation == Orientation.Vertical)
        {
            if (ContentArea.Right < ContentArea.Width - 1)
            {
                return;
            }

            int col = ContentArea.Width - 1;
            int bh = ContentArea.Height;
            Rune special;

            if (bh < 4)
            {
                int by1 = _position * bh / Size;
                int by2 = (_position + bh) * bh / Size;

                Move (col, 0);

                if (ContentArea.Height == 1)
                {
                    Driver.AddRune (Glyphs.Diamond);
                }
                else
                {
                    Driver.AddRune (Glyphs.UpArrow);
                }

                if (ContentArea.Height == 3)
                {
                    Move (col, 1);
                    Driver.AddRune (Glyphs.Diamond);
                }

                if (ContentArea.Height > 1)
                {
                    Move (col, ContentArea.Height - 1);
                    Driver.AddRune (Glyphs.DownArrow);
                }
            }
            else
            {
                bh -= 2;

                int by1 = KeepContentAlwaysInViewPort
                              ? _position * bh / Size
                              : _position * bh / (Size + bh);

                int by2 = KeepContentAlwaysInViewPort
                              ? Math.Min ((_position + bh) * bh / Size + 1, bh - 1)
                              : (_position + bh) * bh / (Size + bh);

                if (KeepContentAlwaysInViewPort && by1 == by2)
                {
                    by1 = Math.Max (by1 - 1, 0);
                }

                Move (col, 0);
                Driver.AddRune (Glyphs.UpArrow);

                var hasTopTee = false;
                var hasDiamond = false;
                var hasBottomTee = false;

                for (var y = 0; y < bh; y++)
                {
                    Move (col, y + 1);

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

                    Driver.AddRune (special);
                }

                if (!hasTopTee)
                {
                    Move (col, ContentArea.Height - 2);
                    Driver.AddRune (Glyphs.TopTee);
                }

                Move (col, ContentArea.Height - 1);
                Driver.AddRune (Glyphs.DownArrow);
            }
        }
        else
        {
            if (ContentArea.Bottom < ContentArea.Height - 1)
            {
                return;
            }

            int row = ContentArea.Height - 1;
            int bw = ContentArea.Width;
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

                int bx1 = KeepContentAlwaysInViewPort
                              ? _position * bw / Size
                              : _position * bw / (Size + bw);

                int bx2 = KeepContentAlwaysInViewPort
                              ? Math.Min ((_position + bw) * bw / Size + 1, bw - 1)
                              : (_position + bw) * bw / (Size + bw);

                if (KeepContentAlwaysInViewPort && bx1 == bx2)
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
                    Move (ContentArea.Width - 2, row);
                    Driver.AddRune (Glyphs.LeftTee);
                }

                Driver.AddRune (Glyphs.RightArrow);
            }
        }
    }

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnEnter (view);
    }

    /// <inheritdoc/>
    public override bool OnMouseEnter (MouseEvent mouseEvent)
    {
        Application.GrabMouse (this);

        return base.OnMouseEnter (mouseEvent);
    }

    /// <inheritdoc/>
    public override bool OnMouseLeave (MouseEvent mouseEvent)
    {
        if (Application.MouseGrabView != null && Application.MouseGrabView != this)
        {
            Application.UngrabMouse ();
        }

        return base.OnMouseLeave (mouseEvent);
    }

    /// <summary>Only used for a hosted view that will update and redraw the scrollbars.</summary>
    public virtual void Refresh () { ShowHideScrollBars (); }

    // param n is the new position and the max is the positive/negative value that can be scrolled for the new position
    internal bool CanScroll (int n, out int maxToScroll, Orientation orientation = Orientation.Horizontal)
    {
        Rectangle bounds = SuperView?.GetVisibleContentArea () ?? Rectangle.Empty;

        if (bounds.IsEmpty)
        {
            maxToScroll = 0;

            return false;
        }

        int barSize = GetBarSize (orientation);
        int newPosition = Math.Max (Math.Min (Size - barSize, n), 0);

        maxToScroll = Size > barSize + newPosition
                          ? newPosition - _position
                          : Size - (barSize + _position) - (barSize == 0 && _showBothScrollIndicator ? 1 : 0);

        return Size >= barSize + newPosition && maxToScroll != 0;
    }

    private void AdjustContentInViewport (bool refresh = true)
    {
        if (SuperView is null)
        {
            return;
        }

        var pos = 0;
        Rectangle bounds = SuperView?.GetVisibleContentArea () ?? Rectangle.Empty;

        if (KeepContentAlwaysInViewPort
            && _orientation == Orientation.Horizontal
            && _position + bounds.Width > Size + (!IsBuiltIn && _showBothScrollIndicator ? 1 : 0))
        {
            pos = Math.Max (Size - bounds.Width + (!IsBuiltIn && _showBothScrollIndicator ? 1 : 0), 0);
        }

        if (KeepContentAlwaysInViewPort
            && _orientation == Orientation.Vertical
            && _position + bounds.Height > Size + (!IsBuiltIn && _showBothScrollIndicator ? 1 : 0))
        {
            pos = Math.Max (Size - bounds.Height + (!IsBuiltIn && _showBothScrollIndicator ? 1 : 0), 0);
        }

        if (pos != 0)
        {
            Position = pos;
        }

        if (OtherScrollBarView is { } && OtherScrollBarView.KeepContentAlwaysInViewPort != KeepContentAlwaysInViewPort)
        {
            OtherScrollBarView.KeepContentAlwaysInViewPort = KeepContentAlwaysInViewPort;
        }

        if (pos == 0 && refresh)
        {
            Refresh ();
        }
    }

    private bool CheckBothScrollBars (ScrollBarView scrollBarView, bool pending = false)
    {
        int barsize = scrollBarView._orientation == Orientation.Vertical ? scrollBarView.ContentArea.Height : scrollBarView.ContentArea.Width;

        if (barsize == 0 || barsize >= scrollBarView.Size)
        {
            if (scrollBarView._showScrollIndicator)
            {
                scrollBarView.ShowScrollIndicator = false;
            }

            if (scrollBarView.Visible)
            {
                scrollBarView.Visible = false;
            }
        }
        else if (barsize > 0 && barsize == scrollBarView.Size && scrollBarView.OtherScrollBarView is { } && pending)
        {
            if (scrollBarView._showScrollIndicator)
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
        else if (barsize > 0 && barsize == Size && scrollBarView.OtherScrollBarView is { } && !pending)
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

            if (!scrollBarView._showScrollIndicator)
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
        if (IsBuiltIn)
        {
            Driver.SetAttribute (((Adornment)SuperView).Parent.HasFocus ? ColorScheme.Focus : GetNormalColor ());
        }
        else
        {
            Driver.SetAttribute (SuperView.HasFocus ? ColorScheme.Focus : GetNormalColor ());
        }

        // I'm forced to do this here because the Clear method is
        // changing the color attribute and is different of this one
        Driver.FillRect (Driver.Clip);
        e.Cancel = true;
    }

    private void ContentBottomRightCorner_MouseClick (object sender, MouseEventEventArgs me)
    {
        if (me.MouseEvent.Flags == MouseFlags.WheeledDown
            || me.MouseEvent.Flags == MouseFlags.WheeledUp
            || me.MouseEvent.Flags == MouseFlags.WheeledRight
            || me.MouseEvent.Flags == MouseFlags.WheeledLeft)
        {
            MouseEvent (me.MouseEvent);
        }
        else if (me.MouseEvent.Flags == MouseFlags.Button1Clicked)
        {
            if (IsBuiltIn)
            {
                ((Adornment)SuperView).Parent.SetFocus ();
            }
            else
            {
                SuperView.SetFocus ();
            }
        }

        me.Handled = true;
    }

    private void CreateBottomRightCorner (View host)
    {
        if (host != null
            && ((_contentBottomRightCorner is null && OtherScrollBarView is null)
                || (_contentBottomRightCorner is null && OtherScrollBarView is { } && OtherScrollBarView._contentBottomRightCorner is null)))
        {
            if (IsBuiltIn && ((Adornment)host).Parent.ScrollBarType != ScrollBarType.Both)
            {
                return;
            }

            _contentBottomRightCorner = new ContentBottomRightCorner { Visible = host.Visible };
            host.Add (_contentBottomRightCorner);

            if (IsBuiltIn)
            {
                _contentBottomRightCorner.X = Pos.AnchorEnd ();
                _contentBottomRightCorner.Y = Pos.AnchorEnd ();
            }
            else
            {
                _contentBottomRightCorner.X = Pos.AnchorEnd (1);
                _contentBottomRightCorner.Y = Pos.AnchorEnd (1);
            }

            _contentBottomRightCorner.Width = 1;
            _contentBottomRightCorner.Height = 1;
            _contentBottomRightCorner.MouseClick += ContentBottomRightCorner_MouseClick;
            _contentBottomRightCorner.DrawContent += ContentBottomRightCorner_DrawContent;
        }
        else if (host != null
                 && _contentBottomRightCorner == null
                 && OtherScrollBarView != null
                 && OtherScrollBarView._contentBottomRightCorner != null)

        {
            _contentBottomRightCorner = OtherScrollBarView._contentBottomRightCorner;
        }
    }

    private int GetBarSize (Orientation orientation)
    {
        Rectangle bounds = SuperView?.GetVisibleContentArea () ?? Rectangle.Empty;

        if (bounds.IsEmpty)
        {
            return 0;
        }

        if (IsBuiltIn)
        {
            return orientation == Orientation.Vertical ? KeepContentAlwaysInViewPort
                                                             ? bounds.Height
                                                             : 0 :
                   KeepContentAlwaysInViewPort ? bounds.Width : 0;
        }

        return orientation == Orientation.Vertical ? KeepContentAlwaysInViewPort
                                                         ? bounds.Height - (_showBothScrollIndicator ? 1 : 0)
                                                         : 0 :
               KeepContentAlwaysInViewPort ? bounds.Width - (_showBothScrollIndicator ? 1 : 0) : 0;
    }

    private void ManageScrollBarThickness ()
    {
        if (!IsBuiltIn)
        {
            return;
        }

        ((Adornment)SuperView).Thickness = ((Adornment)SuperView).Parent.ScrollBarType switch
                                           {
                                               ScrollBarType.None => new Thickness (0),
                                               ScrollBarType.Vertical => new Thickness (0, 0, ShowScrollIndicator ? 1 : 0, 0),
                                               ScrollBarType.Horizontal => new Thickness (0, 0, 0, ShowScrollIndicator ? 1 : 0),
                                               ScrollBarType.Both => new Thickness (
                                                                                    0,
                                                                                    0,
                                                                                    Orientation == Orientation.Vertical
                                                                                        ? ShowScrollIndicator ? 1 : 0
                                                                                        : OtherScrollBarView?.Orientation == Orientation.Vertical
                                                                                            ? OtherScrollBarView?.ShowScrollIndicator == true ? 1 : 0
                                                                                            : 0,
                                                                                    Orientation == Orientation.Horizontal
                                                                                        ? ShowScrollIndicator ? 1 : 0
                                                                                        : OtherScrollBarView?.Orientation == Orientation.Horizontal
                                                                                            ? OtherScrollBarView?.ShowScrollIndicator == true ? 1 : 0
                                                                                            : 0),
                                               _ => throw new ArgumentOutOfRangeException ()
                                           };
    }

    private void Parent_DrawAdornments (object sender, DrawEventArgs e) { AdjustContentInViewport (); }

    private void Parent_EnabledChanged (object sender, EventArgs e)
    {
        Enabled = SuperView.Enabled;

        if (_otherScrollBarView is { })
        {
            _otherScrollBarView.Enabled = Enabled;
        }

        _contentBottomRightCorner.Enabled = Enabled;
    }

    private void Parent_VisibleChanged (object sender, EventArgs e)
    {
        if (!SuperView.Visible)
        {
            Visible = SuperView.Visible;

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

    private void ScrollBarView_Added (object sender, SuperViewChangedEventArgs e)
    {
        if (IsBuiltIn)
        {
            X = Orientation == Orientation.Vertical ? Pos.AnchorEnd () : 0;
            Y = Orientation == Orientation.Vertical ? 0 : Pos.AnchorEnd ();
        }
        else
        {
            X = Orientation == Orientation.Vertical ? Pos.AnchorEnd (1) : 0;
            Y = Orientation == Orientation.Vertical ? 0 : Pos.AnchorEnd (1);
        }

        if (OtherScrollBarView is { SuperView: null } && !e.Parent.Subviews.Contains (OtherScrollBarView))
        {
            e.Parent.Add (OtherScrollBarView);
        }

        CreateBottomRightCorner (e.Parent);

        View parent = e.Parent is Adornment ? ((Adornment)e.Parent).Parent : e.Parent;

        //e.Parent.CanFocusChanged += Host_CanFocusChanged;
        parent.EnabledChanged += Parent_EnabledChanged;
        parent.VisibleChanged += Parent_VisibleChanged;
        parent.DrawAdornments += Parent_DrawAdornments;
        parent.MouseEnter += (s, e) => OnMouseEnter (e.MouseEvent);
        parent.MouseLeave += (s, e) => OnMouseLeave (e.MouseEvent);

        ManageScrollBarThickness ();
    }

    private void ScrollBarView_Initialized (object sender, EventArgs e)
    {
        SetWidthHeight ();
        ShowHideScrollBars ();

        SetPosition (Position);
    }

    // Helper to assist Initialized event handler
    private void SetPosition (int newPosition)
    {
        if (CanScroll (newPosition, out int max, _orientation))
        {
            if (max == newPosition)
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
        else if (max > 0)
        {
            _position = Math.Max (newPosition, 0);
        }
        else
        {
            // Doesn't change the position
            return;
        }

        OnChangedPosition ();
        SetNeedsDisplay ();
        OtherScrollBarView?.SetNeedsDisplay ();
        _contentBottomRightCorner?.SetNeedsDisplay ();
        OtherScrollBarView?._contentBottomRightCorner?.SetNeedsDisplay ();
    }

    private void SetWidthHeight ()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (_showBothScrollIndicator)
        {
            if (SuperView is { UseContentOffset: true })
            {
                Rectangle bounds = SuperView?.GetVisibleContentArea () ?? Rectangle.Empty;

                X = _orientation == Orientation.Vertical ? bounds.Right - 1 : bounds.Left;
                Y = _orientation == Orientation.Vertical ? bounds.Top : bounds.Bottom - 1;
                Width = _orientation == Orientation.Vertical ? 1 : SuperView is Adornment ? Dim.Fill () : bounds.Width - 1;
                Height = _orientation == Orientation.Vertical ? SuperView is Adornment ? Dim.Fill () : bounds.Height - 1 : 1;

                _otherScrollBarView.X = _otherScrollBarView._orientation == Orientation.Vertical ? bounds.Right - 1 : bounds.Left;
                _otherScrollBarView.Y = _otherScrollBarView._orientation == Orientation.Vertical ? bounds.Top : bounds.Bottom - 1;

                _otherScrollBarView.Width = _otherScrollBarView._orientation == Orientation.Vertical ? 1 :
                                            SuperView is Adornment ? Dim.Fill () : bounds.Width - 1;

                _otherScrollBarView.Height = _otherScrollBarView._orientation == Orientation.Vertical
                                                 ? SuperView is Adornment ? Dim.Fill () : bounds.Height - 1
                                                 : 1;
            }
            else
            {
                Width = _orientation == Orientation.Vertical ? 1 : SuperView is Adornment ? Dim.Fill () : Dim.Fill (1);
                Height = _orientation == Orientation.Vertical ? SuperView is Adornment ? Dim.Fill () : Dim.Fill (1) : 1;

                _otherScrollBarView.Width = _otherScrollBarView._orientation == Orientation.Vertical ? 1 :
                                            SuperView is Adornment ? Dim.Fill () : Dim.Fill (1);

                _otherScrollBarView.Height = _otherScrollBarView._orientation == Orientation.Vertical
                                                 ? SuperView is Adornment ? Dim.Fill () : Dim.Fill (1)
                                                 : 1;
            }
        }
        else if (_showScrollIndicator)
        {
            if (SuperView is { UseContentOffset: true })
            {
                Rectangle bounds = SuperView?.GetVisibleContentArea () ?? Rectangle.Empty;

                X = _orientation == Orientation.Vertical ? bounds.Right - 1 : bounds.Left;
                Y = _orientation == Orientation.Vertical ? bounds.Top : bounds.Bottom - 1;
                Width = _orientation == Orientation.Vertical ? 1 : bounds.Width;
                Height = _orientation == Orientation.Vertical ? bounds.Height : 1;
            }
            else
            {
                Width = _orientation == Orientation.Vertical ? 1 : Dim.Fill ();
                Height = _orientation == Orientation.Vertical ? Dim.Fill () : 1;
            }
        }
        else if (_otherScrollBarView?._showScrollIndicator == true)
        {
            if (SuperView is { UseContentOffset: true })
            {
                Rectangle bounds = SuperView?.GetVisibleContentArea () ?? Rectangle.Empty;

                _otherScrollBarView.X = _otherScrollBarView._orientation == Orientation.Vertical ? bounds.Right - 1 : bounds.Left;
                _otherScrollBarView.Y = _otherScrollBarView._orientation == Orientation.Vertical ? bounds.Top : bounds.Bottom - 1;
                _otherScrollBarView.Width = _otherScrollBarView._orientation == Orientation.Vertical ? 1 : bounds.Width;
                _otherScrollBarView.Height = _otherScrollBarView._orientation == Orientation.Vertical ? bounds.Height : 1;
            }
            else
            {
                _otherScrollBarView.Width = _otherScrollBarView._orientation == Orientation.Vertical ? 1 : Dim.Fill ();
                _otherScrollBarView.Height = _otherScrollBarView._orientation == Orientation.Vertical ? Dim.Fill () : 1;
            }
        }

        if (IsBuiltIn)
        {
            ManageScrollBarThickness ();
            AdjustContentInViewport (false);
        }
    }

    private void ShowHideScrollBars (bool redraw = true)
    {
        if (!IsInitialized)
        {
            return;
        }

        SetRelativeLayout (SuperView?.GetVisibleContentArea () ?? Rectangle.Empty);

        if (AutoHideScrollBars)
        {
            bool pending = CheckBothScrollBars (this);

            if (_otherScrollBarView is { })
            {
                _otherScrollBarView.SetRelativeLayout (SuperView?.GetVisibleContentArea () ?? Rectangle.Empty);
                CheckBothScrollBars (_otherScrollBarView, pending);
            }
        }

        SetWidthHeight ();
        SetRelativeLayout (SuperView?.GetVisibleContentArea () ?? Rectangle.Empty);

        if (_otherScrollBarView is { })
        {
            OtherScrollBarView.SetWidthHeight ();
            OtherScrollBarView.SetRelativeLayout (SuperView?.GetVisibleContentArea () ?? Rectangle.Empty);
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
        else if (!_showScrollIndicator)
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

        if (SuperView?.Visible == true && _showScrollIndicator && !Visible)
        {
            Visible = true;
        }

        if (SuperView?.Visible == true && _otherScrollBarView?._showScrollIndicator == true && !_otherScrollBarView.Visible)
        {
            _otherScrollBarView.Visible = true;
        }
    }

    internal class ContentBottomRightCorner : View
    {
        public ContentBottomRightCorner ()
        {
            ClearOnVisibleFalse = false;
            ColorScheme = ColorScheme;
        }
    }
}
