#nullable enable

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>ScrollBars are views that display a 1-character scrollbar, either horizontal or vertical</summary>
/// <remarks>
///     <para>
///         The scrollbar is drawn to be a representation of the Size, assuming that the scroll position is set at
///         Position.
///     </para>
///     <para>If the region to display the scrollbar is larger than three characters, arrow indicators are drawn.</para>
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

    /// <summary>Defines if a scrollbar is vertical or horizontal.</summary>
    public Orientation Orientation
    {
        get => _scroll.Orientation;
        set
        {
            Resize (value);
            _scroll.Orientation = value;
        }
    }

    /// <summary>The position, relative to <see cref="Size"/>, to set the scrollbar at.</summary>
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
        _scroll.AdjustScroll ();
        _decrease.AdjustButton ();
        _increase.AdjustButton ();
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
