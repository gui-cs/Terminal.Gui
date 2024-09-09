#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///     Provides a visual indicator that content can be scrolled. ScrollBars consist of two buttons, one each for scrolling
///     forward or backwards, a Scroll that can be clicked to scroll large amounts, and a ScrollSlider that can be dragged
///     to scroll continuously. ScrollBars can be oriented either horizontally or vertically and support the user dragging
///     and clicking with the mouse to scroll.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Position"/> indicates the current location between zero and <see cref="Size"/>.
///     </para>
///     <para>If the scrollbar is larger than three cells, arrow indicators are drawn.</para>
/// </remarks>
public class ScrollBar : View
{
    /// <inheritdoc/>
    public ScrollBar ()
    {
        _scroll = new ();
        _decrease = new ();
        _increase = new () { NavigationDirection = NavigationDirection.Forward };
        Add (_scroll, _decrease, _increase);

        CanFocus = false;
        Orientation = Orientation.Vertical;
        Width = Dim.Auto (DimAutoStyle.Content, 1);
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        _scroll.PositionChanging += Scroll_PositionChanging;
        _scroll.PositionChanged += Scroll_PositionChanged;
        _scroll.SizeChanged += _scroll_SizeChanged;
    }

    private readonly Scroll _scroll;
    private readonly ScrollButton _decrease;
    private readonly ScrollButton _increase;

    private bool _autoHide = true;
    private bool _showScrollIndicator = true;

    /// <summary>
    ///     Gets or sets whether <see cref="View.Visible"/> will be set to <see langword="false"/> if the dimension of the
    ///     scroll bar is greater than or equal to <see cref="Size"/>.
    /// </summary>
    public bool AutoHide
    {
        get => _autoHide;
        set
        {
            if (_autoHide != value)
            {
                _autoHide = value;
                AdjustAll ();
            }
        }
    }

    /// <summary>Get or sets if the view-port is kept in all visible area of this <see cref="ScrollBar"/>.</summary>
    public bool KeepContentInAllViewport
    {
        get => _scroll.KeepContentInAllViewport;
        set => _scroll.KeepContentInAllViewport = value;
    }

    /// <summary>Gets or sets if a scrollbar is vertical or horizontal.</summary>
    public Orientation Orientation
    {
        get => _scroll.Orientation;
        set
        {
            Resize (value);
            _scroll.Orientation = value;
        }
    }

    /// <summary>Gets or sets the position, relative to <see cref="Size"/>, to set the scrollbar at.</summary>
    /// <value>The position.</value>
    public int Position
    {
        get => _scroll.Position;
        set
        {
            _scroll.Position = value;
            AdjustAll ();
        }
    }

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? PositionChanged;

    /// <summary>
    ///     Raised when the <see cref="Position"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? PositionChanging;

    /// <summary>Gets or sets the visibility for the vertical or horizontal scroll indicator.</summary>
    /// <value><c>true</c> if show vertical or horizontal scroll indicator; otherwise, <c>false</c>.</value>
    public bool ShowScrollIndicator
    {
        get => Visible;
        set
        {
            if (value == _showScrollIndicator)
            {
                return;
            }

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

                AdjustAll ();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the size of the Scroll. This is the total size of the content that can be scrolled through.
    /// </summary>
    public int Size
    {
        get => _scroll.Size;
        set
        {
            _scroll.Size = value;
            AdjustAll ();
        }
    }

    /// <summary>Raised when <see cref="Size"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? SizeChanged;

    /// <inheritdoc/>
    internal override void OnLayoutComplete (LayoutEventArgs args)
    {
        base.OnLayoutComplete (args);

        AdjustAll ();
    }

    private void _scroll_SizeChanged (object? sender, EventArgs<int> e) { SizeChanged?.Invoke (this, e); }

    private void AdjustAll ()
    {
        CheckVisibility ();
        _scroll.AdjustScroll ();
        _decrease.AdjustButton ();
        _increase.AdjustButton ();
    }

    private bool CheckVisibility ()
    {
        if (!AutoHide)
        {
            if (Visible != _showScrollIndicator)
            {
                Visible = _showScrollIndicator;
                SetNeedsDisplay ();
            }

            return _showScrollIndicator;
        }

        int barSize = Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;

        if (barSize == 0 || barSize >= Size)
        {
            if (Visible)
            {
                Visible = false;
                SetNeedsDisplay ();

                return false;
            }
        }
        else
        {
            if (!Visible)
            {
                Visible = true;
                SetNeedsDisplay ();
            }
        }

        return true;
    }

    private void Resize (Orientation orientation)
    {
        switch (orientation)
        {
            case Orientation.Horizontal:

                break;
            case Orientation.Vertical:
                break;
            default:
                throw new ArgumentOutOfRangeException (nameof (orientation), orientation, null);
        }
    }

    private void Scroll_PositionChanged (object? sender, EventArgs<int> e) { PositionChanged?.Invoke (this, e); }

    private void Scroll_PositionChanging (object? sender, CancelEventArgs<int> e) { PositionChanging?.Invoke (this, e); }
}
