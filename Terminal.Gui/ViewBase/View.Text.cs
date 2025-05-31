#nullable enable


namespace Terminal.Gui.ViewBase;

public partial class View // Text Property APIs
{
    private string _text = string.Empty;

    /// <summary>
    ///     Called when the <see cref="Text"/> has changed. Fires the <see cref="TextChanged"/> event.
    /// </summary>
    public void OnTextChanged () { TextChanged?.Invoke (this, EventArgs.Empty); }

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
            if (TextFormatter.PreserveTrailingSpaces != value)
            {
                TextFormatter.PreserveTrailingSpaces = value;
                TextFormatter.NeedsFormat = true;
                SetNeedsLayout ();
            }
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

            string old = _text;
            _text = value;

            UpdateTextFormatterText ();
            SetNeedsLayout ();
            OnTextChanged ();
        }
    }

    // TODO: Make this non-virtual. Nobody overrides it.
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
    public virtual Alignment TextAlignment
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

    // TODO: Make this non-virtual. Nobody overrides it.
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
    public virtual TextDirection TextDirection
    {
        get => TextFormatter.Direction;
        set => UpdateTextDirection (value);
    }

    /// <summary>
    ///     Gets or sets the <see cref="Text.TextFormatter"/> used to format <see cref="Text"/>.
    /// </summary>
    public TextFormatter TextFormatter { get; init; } = new ();

    // TODO: Make this non-virtual. Nobody overrides it.
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
    public virtual Alignment VerticalTextAlignment
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
    ///     Use this API to set <see cref="Text.TextFormatter.ConstrainToWidth"/>/Height when the view has changed such that the
    ///     size required to fit the text has changed.
    ///     changes.
    /// </remarks>
    /// <returns></returns>
    internal void SetTextFormatterSize ()
    {
        // View subclasses can override UpdateTextFormatterText to modify the Text it holds (e.g. Checkbox and Button).
        // We need to ensure TextFormatter is accurate by calling it here.
        UpdateTextFormatterText ();

        // Default is to use GetContentSize ().
        Size? size = _contentSize;

        // Use _width & _height instead of Width & Height to avoid debug spew
        var widthAuto = _width as DimAuto;
        var heightAuto = _height as DimAuto;

        if (widthAuto is { } && widthAuto.Style.FastHasFlags (DimAutoStyle.Text))
        {
            TextFormatter.ConstrainToWidth = null;
        }
        else
        {
            if (size is { })
            {
                TextFormatter.ConstrainToWidth = size?.Width;
            }
        }

        if (heightAuto is { } && heightAuto.Style.FastHasFlags (DimAutoStyle.Text))
        {
            TextFormatter.ConstrainToHeight = null;
        }
        else
        {
            if (size is { })
            {
                TextFormatter.ConstrainToHeight = size?.Height;
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
