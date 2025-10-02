#nullable enable

using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     Indicates the size of scrollable content and controls the position of the visible content, either vertically or
///     horizontally.
///     Two <see cref="Button"/>s are provided, one to scroll up or left and one to scroll down or right. Between the
///     buttons is a <see cref="ScrollSlider"/> that can be dragged to
///     control the position of the visible content. The ScrollSlier is sized to show the proportion of the scrollable
///     content to the size of the <see cref="View.Viewport"/>.
/// </summary>
/// <remarks>
///     <para>
///         See the <see href="https://gui-cs.github.io/Terminal.Gui/docs/scrolling.html">Scrolling Deep Dive</see>.
///     </para>
///     <para>
///         By default, the built-in View scrollbars (<see cref="View.VerticalScrollBar"/>/
///         <see cref="View.HorizontalScrollBar"/>) have both <see cref="View.Visible"/> and <see cref="AutoShow"/> set to
///         <see langword="false"/>.
///         To enable them, either set <see cref="AutoShow"/> set to <see langword="true"/> or explicitly set
///         <see cref="View.Visible"/>
///         to <see langword="true"/>.
///     </para>
///     <para>
///         By default, this view cannot be focused and does not support keyboard input.
///     </para>
/// </remarks>
public class ScrollBar : View, IOrientation, IDesignable
{
    private readonly Button _decreaseButton;
    private readonly ScrollSlider _slider;
    private readonly Button _increaseButton;

    /// <inheritdoc/>
    public ScrollBar ()
    {
        // Set the default width and height based on the orientation - fill Viewport
        Width = Dim.Auto (
                          DimAutoStyle.Content,
                          Dim.Func (_ => Orientation == Orientation.Vertical ? 1 : SuperView?.Viewport.Width ?? 0));

        Height = Dim.Auto (
                           DimAutoStyle.Content,
                           Dim.Func (_ => Orientation == Orientation.Vertical ? SuperView?.Viewport.Height ?? 0 : 1));

        _decreaseButton = new ()
        {
            CanFocus = false,
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyle.None,
            WantContinuousButtonPressed = true
        };
        _decreaseButton.Accepting += OnDecreaseButtonOnAccept;

        _slider = new ()
        {
            SliderPadding = 2 // For the buttons
        };
        _slider.Scrolled += SliderOnScroll;
        _slider.PositionChanged += SliderOnPositionChanged;

        _increaseButton = new ()
        {
            CanFocus = false,
            NoDecorations = true,
            NoPadding = true,
            ShadowStyle = ShadowStyle.None,
            WantContinuousButtonPressed = true
        };
        _increaseButton.Accepting += OnIncreaseButtonOnAccept;
        Add (_decreaseButton, _slider, _increaseButton);

        CanFocus = false;

        _orientationHelper = new (this); // Do not use object initializer!
        _orientationHelper.Orientation = Orientation.Vertical;

        // This sets the width/height etc...
        OnOrientationChanged (Orientation);

        return;

        void OnDecreaseButtonOnAccept (object? s, CommandEventArgs e)
        {
            Position -= Increment;
            e.Handled = true;
        }

        void OnIncreaseButtonOnAccept (object? s, CommandEventArgs e)
        {
            Position += Increment;
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame) { ShowHide (); }

    private void ShowHide ()
    {
        if (AutoShow)
        {
            Visible = VisibleContentSize < ScrollableContentSize;
        }

        _slider.VisibleContentSize = VisibleContentSize;
        _slider.Size = CalculateSliderSize ();
        _sliderPosition = CalculateSliderPositionFromContentPosition (_position);
        _slider.Position = _sliderPosition.Value;
    }

    private void PositionSubViews ()
    {
        if (Orientation == Orientation.Vertical)
        {
            _decreaseButton.Y = 0;
            _decreaseButton.X = 0;
            _decreaseButton.Width = Dim.Fill ();
            _decreaseButton.Height = 1;
            _decreaseButton.Title = Glyphs.UpArrow.ToString ();

            _slider.X = 0;
            _slider.Y = 1;
            _slider.Width = Dim.Fill ();

            _increaseButton.Y = Pos.AnchorEnd ();
            _increaseButton.X = 0;
            _increaseButton.Width = Dim.Fill ();
            _increaseButton.Height = 1;
            _increaseButton.Title = Glyphs.DownArrow.ToString ();
        }
        else
        {
            _decreaseButton.Y = 0;
            _decreaseButton.X = 0;
            _decreaseButton.Width = 1;
            _decreaseButton.Height = Dim.Fill ();
            _decreaseButton.Title = Glyphs.LeftArrow.ToString ();

            _slider.Y = 0;
            _slider.X = 1;
            _slider.Height = Dim.Fill ();

            _increaseButton.Y = 0;
            _increaseButton.X = Pos.AnchorEnd ();
            _increaseButton.Width = 1;
            _increaseButton.Height = Dim.Fill ();
            _increaseButton.Title = Glyphs.RightArrow.ToString ();
        }
    }

    #region IOrientation members

    private readonly OrientationHelper _orientationHelper;

    /// <inheritdoc/>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;
#pragma warning restore CS0067 // The event is never used

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        TextDirection = Orientation == Orientation.Vertical ? TextDirection.TopBottom_LeftRight : TextDirection.LeftRight_TopBottom;
        TextAlignment = Alignment.Center;
        VerticalTextAlignment = Alignment.Center;
        _slider.Orientation = newOrientation;
        PositionSubViews ();

        OrientationChanged?.Invoke (this, new (newOrientation));
    }

    #endregion

    /// <summary>
    ///     Gets or sets the amount each mouse wheel event, or click on the increment/decrement buttons, will
    ///     incremenet/decrement the <see cref="Position"/>.
    /// </summary>
    /// <remarks>
    ///     The default is 1.
    /// </remarks>
    public int Increment { get; set; } = 1;

    // AutoShow should be false by default. Views should not be hidden by default.
    private bool _autoShow;

    /// <summary>
    ///     Gets or sets whether <see cref="View.Visible"/> will be set to <see langword="true"/> if the dimension of the
    ///     scroll bar is less than  <see cref="ScrollableContentSize"/> and <see langword="false"/> if greater than or equal
    ///     to.
    /// </summary>
    /// <remarks>
    ///     The default is <see langword="false"/>.
    /// </remarks>
    public bool AutoShow
    {
        get => _autoShow;
        set
        {
            if (_autoShow != value)
            {
                _autoShow = value;

                if (!AutoShow)
                {
                    Visible = true;
                }

                ShowHide ();
                SetNeedsLayout ();
            }
        }
    }

    private int? _visibleContentSize;

    /// <summary>
    ///     Gets or sets the size of the visible viewport into the content being scrolled, bounded by
    ///     <see cref="ScrollableContentSize"/>.
    /// </summary>
    /// <remarks>
    ///     If not explicitly set, the visible content size will be appropriate dimension of the ScrollBar's Frame.
    /// </remarks>
    public int VisibleContentSize
    {
        get
        {
            if (_visibleContentSize.HasValue)
            {
                return _visibleContentSize.Value;
            }

            return Orientation == Orientation.Vertical ? Frame.Height : Frame.Width;
        }
        set
        {
            _visibleContentSize = value;
            _slider.Size = CalculateSliderSize ();
            ShowHide ();
        }
    }

    private int? _scrollableContentSize;

    /// <summary>
    ///     Gets or sets the size of the content that can be scrolled. This is typically set to
    ///     <see cref="View.GetContentSize()"/>.
    /// </summary>
    public int ScrollableContentSize
    {
        get
        {
            if (_scrollableContentSize.HasValue)
            {
                return _scrollableContentSize.Value;
            }

            return Orientation == Orientation.Vertical ? SuperView?.GetContentSize ().Height ?? 0 : SuperView?.GetContentSize ().Width ?? 0;
        }
        set
        {
            if (value == _scrollableContentSize || value < 0)
            {
                return;
            }

            _scrollableContentSize = value;
            _slider.Size = CalculateSliderSize ();
            ShowHide ();

            if (!Visible)
            {
                return;
            }

            OnSizeChanged (value);
            ScrollableContentSizeChanged?.Invoke (this, new (in value));
            SetNeedsLayout ();
        }
    }

    /// <summary>Called when <see cref="ScrollableContentSize"/> has changed. </summary>
    protected virtual void OnSizeChanged (int size) { }

    /// <summary>Raised when <see cref="ScrollableContentSize"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? ScrollableContentSizeChanged;

    #region Position

    private int _position;

    /// <summary>
    ///     Gets or sets the position of the slider relative to <see cref="ScrollableContentSize"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The content position is clamped to 0 and <see cref="ScrollableContentSize"/> minus
    ///         <see cref="VisibleContentSize"/>.
    ///     </para>
    ///     <para>
    ///         Setting will result in the <see cref="PositionChanging"/> and <see cref="PositionChanged"/>
    ///         events being raised.
    ///     </para>
    /// </remarks>
    public int Position
    {
        get => _position;
        set
        {
            if (value == _position || !Visible)
            {
                return;
            }

            // Clamp the value between 0 and Size - VisibleContentSize
            int newContentPosition = Math.Clamp (value, 0, Math.Max (0, ScrollableContentSize - VisibleContentSize));
            NavigationDirection direction = newContentPosition >= _position ? NavigationDirection.Forward : NavigationDirection.Backward;

            if (OnPositionChanging (_position, newContentPosition))
            {
                return;
            }

            CancelEventArgs<int> args = new (ref _position, ref newContentPosition);
            PositionChanging?.Invoke (this, args);

            if (args.Cancel)
            {
                return;
            }

            int distance = newContentPosition - _position;

            if (_position == newContentPosition)
            {
                return;
            }

            _position = newContentPosition;

            _sliderPosition = CalculateSliderPositionFromContentPosition (_position, direction);

            if (_slider.Position != _sliderPosition)
            {
                _slider.Position = _sliderPosition.Value;
            }

            OnPositionChanged (_position);
            PositionChanged?.Invoke (this, new (in _position));

            OnScrolled (distance);
            Scrolled?.Invoke (this, new (in distance));
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Called when <see cref="Position"/> is changing. Return true to cancel the change.
    /// </summary>
    protected virtual bool OnPositionChanging (int currentPos, int newPos) { return false; }

    /// <summary>
    ///     Raised when the <see cref="Position"/> is changing. Set <see cref="CancelEventArgs.Cancel"/> to
    ///     <see langword="true"/> to prevent the position from being changed.
    /// </summary>
    public event EventHandler<CancelEventArgs<int>>? PositionChanging;

    /// <summary>Called when <see cref="Position"/> has changed.</summary>
    protected virtual void OnPositionChanged (int position) { }

    /// <summary>Raised when the <see cref="Position"/> has changed.</summary>
    public event EventHandler<EventArgs<int>>? PositionChanged;

    /// <summary>Called when <see cref="Position"/> has changed. Indicates how much to scroll.</summary>
    protected virtual void OnScrolled (int distance) { }

    /// <summary>Raised when the <see cref="Position"/> has changed. Indicates how much to scroll.</summary>
    public event EventHandler<EventArgs<int>>? Scrolled;

    /// <summary>
    ///     INTERNAL API (for unit tests) - Calculates the position within the <see cref="ScrollableContentSize"/> based on the
    ///     slider position.
    /// </summary>
    /// <remarks>
    ///     Clamps the sliderPosition, ensuring the returned content position is always less than
    ///     <see cref="ScrollableContentSize"/> - <see cref="VisibleContentSize"/>.
    /// </remarks>
    /// <param name="sliderPosition"></param>
    /// <returns></returns>
    internal int CalculatePositionFromSliderPosition (int sliderPosition)
    {
        int scrollBarSize = Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;

        return ScrollSlider.CalculateContentPosition (ScrollableContentSize, VisibleContentSize, sliderPosition, scrollBarSize - _slider.SliderPadding);
    }

    #endregion ContentPosition

    #region Slider Management

    private int? _sliderPosition;

    /// <summary>
    ///     INTERNAL (for unit tests). Calculates the size of the slider based on the Orientation, VisibleContentSize, the
    ///     actual Viewport, and Size.
    /// </summary>
    /// <returns></returns>
    internal int CalculateSliderSize ()
    {
        int maxSliderSize = (Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width) - 2;

        return ScrollSlider.CalculateSize (ScrollableContentSize, VisibleContentSize, maxSliderSize);
    }

    private void SliderOnPositionChanged (object? sender, EventArgs<int> e)
    {
        if (VisibleContentSize == 0)
        {
            return;
        }

        RaiseSliderPositionChangeEvents (_sliderPosition, e.Value);
    }

    private void SliderOnScroll (object? sender, EventArgs<int> e)
    {
        if (VisibleContentSize == 0)
        {
            return;
        }

        int calculatedSliderPos = CalculateSliderPositionFromContentPosition (
                                                                              _position,
                                                                              e.Value >= 0 ? NavigationDirection.Forward : NavigationDirection.Backward);

        if (calculatedSliderPos == _sliderPosition)
        {
            return;
        }

        int sliderScrolledAmount = e.Value;
        int calculatedPosition = CalculatePositionFromSliderPosition (calculatedSliderPos + sliderScrolledAmount);

        Position = calculatedPosition;
    }

    /// <summary>
    ///     Gets or sets the position of the start of the Scroll slider, within the Viewport.
    /// </summary>
    public int GetSliderPosition () { return CalculateSliderPositionFromContentPosition (_position); }

    private void RaiseSliderPositionChangeEvents (int? currentSliderPosition, int newSliderPosition)
    {
        if (currentSliderPosition == newSliderPosition)
        {
            return;
        }

        _sliderPosition = newSliderPosition;

        OnSliderPositionChanged (newSliderPosition);
        SliderPositionChanged?.Invoke (this, new (in newSliderPosition));
    }

    /// <summary>Called when the slider position has changed.</summary>
    protected virtual void OnSliderPositionChanged (int position) { }

    /// <summary>Raised when the slider position has changed.</summary>
    public event EventHandler<EventArgs<int>>? SliderPositionChanged;

    /// <summary>
    ///     INTERNAL API (for unit tests) - Calculates the position of the slider based on the content position.
    /// </summary>
    /// <param name="contentPosition"></param>
    /// <param name="direction"></param>
    /// <returns></returns>
    internal int CalculateSliderPositionFromContentPosition (int contentPosition, NavigationDirection direction = NavigationDirection.Forward)
    {
        int scrollBarSize = Orientation == Orientation.Vertical ? Viewport.Height : Viewport.Width;

        return ScrollSlider.CalculatePosition (ScrollableContentSize, VisibleContentSize, contentPosition, scrollBarSize - 2, direction);
    }

    #endregion Slider Management

    /// <inheritdoc/>
    protected override bool OnClearingViewport ()
    {
        if (Orientation == Orientation.Vertical)
        {
            FillRect (Viewport with { Y = Viewport.Y + 1, Height = Viewport.Height - 2 }, Glyphs.Stipple);
        }
        else
        {
            FillRect (Viewport with { X = Viewport.X + 1, Width = Viewport.Width - 2 }, Glyphs.Stipple);
        }

        SetNeedsDraw ();

        return true;
    }

    // TODO: Change this to work OnMouseEvent with continuouse press and grab so it's continous.
    /// <inheritdoc/>
    protected override bool OnMouseClick (MouseEventArgs args)
    {
        // Check if the mouse click is a single click
        if (!args.IsSingleClicked)
        {
            return false;
        }

        int sliderCenter;
        int distanceFromCenter;

        if (Orientation == Orientation.Vertical)
        {
            sliderCenter = 1 + _slider.Frame.Y + _slider.Frame.Height / 2;
            distanceFromCenter = args.Position.Y - sliderCenter;
        }
        else
        {
            sliderCenter = 1 + _slider.Frame.X + _slider.Frame.Width / 2;
            distanceFromCenter = args.Position.X - sliderCenter;
        }

#if PROPORTIONAL_SCROLL_JUMP
        // TODO: This logic mostly works to provide a proportional jump. However, the math
        // TODO: falls apart in edge cases. Most other scroll bars (e.g. Windows) do not do proportional
        // TODO: Thus, this is disabled; we just jump a page each click.
        // Ratio of the distance to the viewport dimension
        double ratio = (double)Math.Abs (distanceFromCenter) / (VisibleContentSize);
        // Jump size based on the ratio and the total content size
        int jump = (int)(ratio * (Size - VisibleContentSize));
#else
        int jump = VisibleContentSize;
#endif

        // Adjust the content position based on the distance
        if (distanceFromCenter < 0)
        {
            Position = Math.Max (0, Position - jump);
        }
        else
        {
            Position = Math.Min (ScrollableContentSize - _slider.VisibleContentSize, Position + jump);
        }

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (SuperView is null)
        {
            return false;
        }

        if (!mouseEvent.IsWheel)
        {
            return false;
        }

        if (Orientation == Orientation.Vertical)
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledDown))
            {
                Position += Increment;
            }

            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledUp))
            {
                Position -= Increment;
            }
        }
        else
        {
            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledRight))
            {
                Position += Increment;
            }

            if (mouseEvent.Flags.HasFlag (MouseFlags.WheeledLeft))
            {
                Position -= Increment;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        OrientationChanged += (sender, args) =>
                              {
                                  if (args.Value == Orientation.Vertical)
                                  {
                                      Width = 1;
                                      Height = Dim.Fill ();
                                  }
                                  else
                                  {
                                      Width = Dim.Fill ();
                                      Height = 1;
                                  }
                              };

        Width = 1;
        Height = Dim.Fill ();
        ScrollableContentSize = 250;

        return true;
    }
}
