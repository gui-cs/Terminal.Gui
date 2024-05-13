using static Terminal.Gui.SpinnerStyle;

namespace Terminal.Gui;

public partial class View
{
    private string _text;

    /// <summary>
    ///     Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved
    ///     or not when <see cref="TextFormatter.WordWrap"/> is enabled.
    ///     If <see langword="true"/> trailing spaces at the end of wrapped lines will be removed when
    ///     <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
    /// </summary>
    public virtual bool PreserveTrailingSpaces
    {
        get => TextFormatter.PreserveTrailingSpaces;
        set
        {
            if (TextFormatter.PreserveTrailingSpaces != value)
            {
                TextFormatter.PreserveTrailingSpaces = value;
                TextFormatter.NeedsFormat = true;
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
    ///         The text will word-wrap to additional lines if it does not fit horizontally. If <see cref="ContentSize"/>'s height
    ///         is 1, the text will be clipped.
    ///     </para>
    ///     <para>If <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="Dim.DimAutoStyle.Text"/>,
    ///     the <see cref="ContentSize"/> will be adjusted to fit the text.</para>
    ///     <para>When the text changes, the <see cref="TextChanged"/> is fired.</para>
    /// </remarks>
    public virtual string Text
    {
        get => _text;
        set
        {
            string old = _text;
            _text = value;

            UpdateTextFormatterText ();
            OnResizeNeeded ();
#if DEBUG
            if (_text is { } && string.IsNullOrEmpty (Id))
            {
                Id = _text;
            }
#endif
            OnTextChanged (old, Text);
        }
    }

    /// <summary>
    /// Called when the <see cref="Text"/> has changed. Fires the <see cref="TextChanged"/> event.
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    public void OnTextChanged (string oldValue, string newValue)
    {
        TextChanged?.Invoke (this, new StateEventArgs<string> (oldValue, newValue));
    }

    /// <summary>
    ///     Text changed event, raised when the text has changed.
    /// </summary>
    public event EventHandler<StateEventArgs<string>> TextChanged;

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned horizontally when drawn. Changing this property will
    ///     redisplay the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para> <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="Dim.DimAutoStyle.Text"/>, the <see cref="ContentSize"/> will be adjusted to fit the text.</para>
    /// </remarks>
    /// <value>The text alignment.</value>
    public virtual TextAlignment TextAlignment
    {
        get => TextFormatter.Alignment;
        set
        {
            TextFormatter.Alignment = value;
            UpdateTextFormatterText ();
            OnResizeNeeded ();
        }
    }

    /// <summary>
    ///     Gets or sets the direction of the View's <see cref="Text"/>. Changing this property will redisplay the
    ///     <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para> <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="Dim.DimAutoStyle.Text"/>, the <see cref="ContentSize"/> will be adjusted to fit the text.</para>
    /// </remarks>
    /// <value>The text alignment.</value>
    public virtual TextDirection TextDirection
    {
        get => TextFormatter.Direction;
        set
        {
            UpdateTextDirection (value);
            TextFormatter.Direction = value;
        }
    }

    /// <summary>
    ///     Gets or sets the <see cref="Gui.TextFormatter"/> used to format <see cref="Text"/>.
    /// </summary>
    public TextFormatter TextFormatter { get; init; } = new () { };

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will
    ///     redisplay
    ///     the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para> <see cref="View.Width"/> or <see cref="View.Height"/> are using <see cref="Dim.DimAutoStyle.Text"/>, the <see cref="ContentSize"/> will be adjusted to fit the text.</para>
    /// </remarks>
    /// <value>The text alignment.</value>
    public virtual VerticalTextAlignment VerticalTextAlignment
    {
        get => TextFormatter.VerticalAlignment;
        set
        {
            TextFormatter.VerticalAlignment = value;
            SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Can be overridden if the <see cref="Terminal.Gui.TextFormatter.Text"/> has
    ///     different format than the default.
    /// </summary>
    protected virtual void UpdateTextFormatterText ()
    {
        if (TextFormatter is { })
        {
            TextFormatter.Text = _text;
        }
    }

    /// <summary>
    ///     Gets the dimensions required for <see cref="Text"/> ignoring a <see cref="TextFormatter.HotKeySpecifier"/>.
    /// </summary>
    /// <returns></returns>
    internal Size GetSizeNeededForTextWithoutHotKey ()
    {
        return new Size (
                         TextFormatter.Size.Width - TextFormatter.GetHotKeySpecifierLength (),
                         TextFormatter.Size.Height - TextFormatter.GetHotKeySpecifierLength (false));
    }

    /// <summary>
    ///     Internal API. Sets <see cref="TextFormatter"/>.Size to the current <see cref="Viewport"/> size, adjusted for
    ///     <see cref="TextFormatter.HotKeySpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     Use this API to set <see cref="TextFormatter.Size"/> when the view has changed such that the
    ///     size required to fit the text has changed.
    ///     changes.
    /// </remarks>
    /// <returns></returns>
    internal void SetTextFormatterSize ()
    {
        UpdateTextFormatterText ();

        // TODO: This is a hack. Figure out how to move this into DimDimAuto
        // Use _width & _height instead of Width & Height to avoid debug spew
        Dim.DimAuto widthAuto = _width as Dim.DimAuto;
        Dim.DimAuto heightAuto = _height as Dim.DimAuto;
        if ((widthAuto is {} && widthAuto._style.HasFlag (Dim.DimAutoStyle.Text))
            || (heightAuto is {} && heightAuto._style.HasFlag (Dim.DimAutoStyle.Text)))
        {
            // We always use TF in autosize = false mode
            TextFormatter.AutoSize = false;

            var size = TextFormatter.GetAutoSize ();

            if (widthAuto is null || !widthAuto._style.HasFlag (Dim.DimAutoStyle.Text))
            {
                size.Width = _width.Anchor (0);
            }

            if (heightAuto is null || !heightAuto._style.HasFlag (Dim.DimAutoStyle.Text))
            {
                size.Height = _height.Anchor (0);
            }

            // Whenever DimAutoStyle.Text is set, ContentSize will match TextFormatter.Size.
            //ContentSize = size;//TextFormatter.Size == Size.Empty ? null : TextFormatter.Size;
            TextFormatter.Size = size;
            return;
        }

        // We always use TF in autosize = false mode
        TextFormatter.AutoSize = false;
        TextFormatter.Size = new Size (ContentSize.GetValueOrDefault ().Width, ContentSize.GetValueOrDefault ().Height);
    }

    private void UpdateTextDirection (TextDirection newDirection)
    {
        bool directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction) != TextFormatter.IsHorizontalDirection (newDirection);
        TextFormatter.Direction = newDirection;

        UpdateTextFormatterText ();

        if (directionChanged)
        {
            OnResizeNeeded ();
        }

        SetTextFormatterSize ();
        SetNeedsDisplay ();
    }
}
