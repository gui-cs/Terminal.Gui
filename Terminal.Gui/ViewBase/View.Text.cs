namespace Terminal.Gui.ViewBase;

public partial class View // Text Property APIs
{
    private string _text = string.Empty;

    /// <summary>
    ///     Called when the <see cref="Text"/> has changed. Fires the <see cref="TextChanged"/> event.
    /// </summary>
    public void OnTextChanged () => TextChanged?.Invoke (this, EventArgs.Empty);

    /// <summary>
    ///     Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
    ///     or not when <see cref="Text.TextFormatter.WordWrap"/> is enabled.
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when
    ///     <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
    /// </summary>
    public bool PreserveTrailingSpaces
    {
        get => TextFormatter.PreserveTrailingSpaces;
        set
        {
            if (TextFormatter.PreserveTrailingSpaces == value)
            {
                return;
            }
            TextFormatter.PreserveTrailingSpaces = value;
            TextFormatter.NeedsFormat = true;
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     The text displayed by the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The text will be drawn before any subviews are drawn.
    ///     </para>
    ///     <para>
    ///         The text will be drawn starting at the view origin (0, 0) and will be formatted according
    ///         to <see cref="TextAlignment"/> and <see cref="TextDirection"/>.
    ///     </para>
    ///     <para>
    ///         The text will word-wrap to additional lines if it does not fit horizontally. If <see cref="GetContentSize ()"/>
    ///         's height
    ///         is 1, the text will be clipped.
    ///     </para>
    ///     <para>
    ///         If <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>,
    ///         the <see cref="GetContentSize ()"/> will be adjusted to fit the text.
    ///     </para>
    ///     <para>When the text changes, the <see cref="TextChanged"/> is fired.</para>
    /// </remarks>
    public virtual string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;

            UpdateTextFormatterText ();

            if (IsTextDrivenFrameUnchanged ())
            {
                // The new Text produces the same Frame (position and size) as before. The content changed, so it must
                // be reformatted and redrawn, but nothing outside this view can be affected, so the layout pass (and
                // the ancestor propagation SetNeedsLayout would trigger) is skipped. See issue #5499.
                TextFormatter.NeedsFormat = true;
                SetNeedsDraw ();
                OnTextChanged ();

                return;
            }

            SetNeedsLayout ();
            OnTextChanged ();
        }
    }

    /// <summary>
    ///     Determines whether the new <see cref="Text"/> resolves to the same <see cref="Frame"/> (position and size)
    ///     the view has now, so a text change can skip <see cref="SetNeedsLayout"/> and only redraw. See issue #5499.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The prediction reuses <see cref="TryComputeRelativeFrame"/> — the exact computation
    ///         <see cref="SetRelativeLayout"/> runs — so it can never disagree with the Frame a real layout pass would
    ///         produce, and it compares the whole <see cref="Frame"/> (a <see cref="Pos"/> can depend on
    ///         <see cref="Text"/>, so a same-size change can still move the view).
    ///     </para>
    ///     <para>
    ///         The optimization applies only when the view has already been laid out and both <see cref="Width"/> and
    ///         <see cref="Height"/> are either a fixed dimension or exactly <see cref="DimAutoStyle.Text"/>. A
    ///         <see cref="DimAuto"/> with <see cref="DimAutoStyle.Content"/> (including <see cref="DimAutoStyle.Auto"/>)
    ///         lays out subviews while calculating, so resolving it here would have side effects; those views fall back
    ///         to <see cref="SetNeedsLayout"/>.
    ///     </para>
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> if the optimization applies and the resolved <see cref="Frame"/> is unchanged;
    ///     otherwise <see langword="false"/>.
    /// </returns>
    private bool IsTextDrivenFrameUnchanged ()
    {
        if (_frame is null)
        {
            // Not yet laid out; let the normal layout pass establish the Frame.
            return false;
        }

        if (!IsEagerlyResolvable (_width) || !IsEagerlyResolvable (_height))
        {
            return false;
        }

        // Mirror SetRelativeLayout's preamble so the prediction sees the same TextFormatter state.
        SetTextFormatterSize ();

        return TryComputeRelativeFrame (GetContainerSize (), out Rectangle predicted) && predicted == Frame;

        // A dimension is safe to resolve here only when doing so has no side effects beyond this view's own
        // TextFormatter. DimAbsolute is constant; a Text-only DimAuto depends solely on the TextFormatter.
        static bool IsEagerlyResolvable (Dim dim) => dim is DimAbsolute or DimAuto { Style: DimAutoStyle.Text };
    }

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will
    ///     redisplay the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the
    ///         <see cref="GetContentSize ()"/> will be adjusted to fit the text.
    ///     </para>
    /// </remarks>
    /// <value>The text alignment.</value>
    public Alignment TextAlignment
    {
        get => TextFormatter.Alignment;
        set
        {
            TextFormatter.Alignment = value;
            UpdateTextFormatterText ();
            SetNeedsLayout ();
        }
    }

    /// <summary>
    ///     Text changed event, raised when the text has changed.
    /// </summary>
    public event EventHandler? TextChanged;

    /// <summary>
    ///     Gets or sets the direction of the View's <see cref="Text"/>. Changing this property will redisplay the
    ///     <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the
    ///         <see cref="GetContentSize ()"/> will be adjusted to fit the text.
    ///     </para>
    /// </remarks>
    /// <value>The text direction.</value>
    public TextDirection TextDirection { get => TextFormatter.Direction; set => UpdateTextDirection (value); }

    /// <summary>
    ///     Gets or sets the <see cref="Text.TextFormatter"/> used to format <see cref="Text"/>.
    /// </summary>
    public TextFormatter TextFormatter { get; init; } = new ();

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will
    ///     redisplay
    ///     the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="DimAutoStyle.Text"/>, the
    ///         <see cref="GetContentSize ()"/> will be adjusted to fit the text.
    ///     </para>
    /// </remarks>
    /// <value>The vertical text alignment.</value>
    public Alignment VerticalTextAlignment
    {
        get => TextFormatter.VerticalAlignment;
        set
        {
            TextFormatter.VerticalAlignment = value;
            SetNeedsDraw ();
        }
    }

    // TODO: Add a OnUpdateTextFormatterText method that invokes UpdateTextFormatterText so that overrides don't have to call base.
    /// <summary>
    ///     Can be overridden if the <see cref="TextFormatter.Text"/> has
    ///     different format than the default.
    /// </summary>
    /// <remarks>
    ///     Overrides must call <c>base.UpdateTextFormatterText</c> before updating <see cref="TextFormatter.Text"/>.
    /// </remarks>
    protected virtual void UpdateTextFormatterText ()
    {
        TextFormatter.Text = _text;
        TextFormatter.ConstrainToWidth = null;
        TextFormatter.ConstrainToHeight = null;
    }

    /// <summary>
    ///     Internal API. Sets <see cref="TextFormatter"/>.Width/Height.
    /// </summary>
    /// <remarks>
    ///     Use this API to set <see cref="Text.TextFormatter.ConstrainToWidth"/>/Height when the view has changed such that
    ///     the
    ///     size required to fit the text has changed.
    ///     changes.
    /// </remarks>
    /// <returns></returns>
    internal void SetTextFormatterSize ()
    {
        // View subclasses can override UpdateTextFormatterText to modify the Text it holds (e.g. Checkbox and Button).
        // We need to ensure TextFormatter is accurate by calling it here.
        UpdateTextFormatterText ();

        // Use _width & _height instead of Width & Height to avoid debug spew

        if (_width.Has (out DimAuto widthAuto) && widthAuto.Style.FastHasFlags (DimAutoStyle.Text))
        {
            TextFormatter.ConstrainToWidth = null;
        }
        else
        {
            if (_contentWidth is { } w)
            {
                TextFormatter.ConstrainToWidth = w;
            }
        }

        // Use _width & _height instead of Width & Height to avoid debug spew

        if (_height.Has (out DimAuto heightAuto) && heightAuto.Style.FastHasFlags (DimAutoStyle.Text))
        {
            TextFormatter.ConstrainToHeight = null;
        }
        else
        {
            if (_contentHeight is { } h)
            {
                TextFormatter.ConstrainToHeight = h;
            }
        }
    }

    /// <summary>
    ///     Initializes the Text of the View. Called by the constructor.
    /// </summary>
    private void SetupText ()
    {
        Text = string.Empty;
        TextDirection = TextDirection.LeftRight_TopBottom;
    }

    private void UpdateTextDirection (TextDirection newDirection)
    {
        bool directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction) != TextFormatter.IsHorizontalDirection (newDirection);
        TextFormatter.Direction = newDirection;

        UpdateTextFormatterText ();

        if (directionChanged)
        {
            TextFormatter.ConstrainToWidth = null;
            TextFormatter.ConstrainToHeight = null;
            SetNeedsLayout ();
        }

        SetNeedsDraw ();
    }
}
