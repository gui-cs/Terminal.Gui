namespace Terminal.Gui;

public partial class View
{
    private string _text;

    /// <summary>
    ///     Gets or sets whether trailing spaces at the end of word-wrapped lines are preserved or not when
    ///     <see cref="TextFormatter.WordWrap"/> is enabled. If <see langword="true"/> trailing spaces at the end of wrapped
    ///     lines will be removed when <see cref="Text"/> is formatted for display. The default is <see langword="false"/>.
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

    /// <summary>The text displayed by the <see cref="View"/>.</summary>
    /// <remarks>
    ///     <para>The text will be drawn before any subviews are drawn.</para>
    ///     <para>
    ///         The text will be drawn starting at the view origin (0, 0) and will be formatted according to
    ///         <see cref="TextAlignment"/> and <see cref="TextDirection"/>.
    ///     </para>
    ///     <para>
    ///         The text will word-wrap to additional lines if it does not fit horizontally. If <see cref="Viewport"/>'s height
    ///         is 1, the text will be clipped.
    ///     </para>
    ///     <para>If <see cref="AutoSize"/> is <c>true</c>, the <see cref="Viewport"/> will be adjusted to fit the text.</para>
    ///     <para>When the text changes, the <see cref="TextChanged"/> is fired.</para>
    /// </remarks>
    public virtual string Text
    {
        get => _text;
        set
        {
            if (value == _text)
            {
                return;
            }

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
    ///     <para>If <see cref="AutoSize"/> is <c>true</c>, the <see cref="Viewport"/> will be adjusted to fit the text.</para>
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
    ///     <para>If <see cref="AutoSize"/> is <c>true</c>, the <see cref="Viewport"/> will be adjusted to fit the text.</para>
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

    /// <summary>Gets the <see cref="Gui.TextFormatter"/> used to format <see cref="Text"/>.</summary>
    public TextFormatter TextFormatter { get; init; } = new ();

    /// <summary>
    ///     Gets or sets how the View's <see cref="Text"/> is aligned vertically when drawn. Changing this property will
    ///     redisplay the <see cref="View"/>.
    /// </summary>
    /// <remarks>
    ///     <para>If <see cref="AutoSize"/> is <c>true</c>, the <see cref="Viewport"/> will be adjusted to fit the text.</para>
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
    ///     Gets the Frame dimensions required to fit <see cref="Text"/> within <see cref="Viewport"/> using the text
    ///     <see cref="NavigationDirection"/> specified by the <see cref="TextFormatter"/> property and accounting for any
    ///     <see cref="HotKeySpecifier"/> characters.
    /// </summary>
    /// <returns>The <see cref="Size"/> the <see cref="Frame"/> needs to be set to fit the text.</returns>
    public Size GetAutoSize ()
    {
        var x = 0;
        var y = 0;

        if (IsInitialized)
        {
            x = Viewport.X;
            y = Viewport.Y;
        }

        Rectangle rect = TextFormatter.CalcRect (x, y, TextFormatter.Text, TextFormatter.Direction);

        int newWidth = rect.Size.Width
                       - GetHotKeySpecifierLength ()
                       + (Margin == null
                              ? 0
                              : Margin.Thickness.Horizontal
                                + Border.Thickness.Horizontal
                                + Padding.Thickness.Horizontal);

        int newHeight = rect.Size.Height
                        - GetHotKeySpecifierLength (false)
                        + (Margin == null
                               ? 0
                               : Margin.Thickness.Vertical + Border.Thickness.Vertical + Padding.Thickness.Vertical);

        return new (newWidth, newHeight);
    }

    /// <summary>
    ///     Gets the width or height of the <see cref="TextFormatter.HotKeySpecifier"/> characters in the
    ///     <see cref="Text"/> property.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is for <see cref="Text"/>, not <see cref="Title"/>. For <see cref="Text"/> to show the hotkey,
    ///         set <c>View.</c><see cref="TextFormatter.HotKeySpecifier"/> to the desired character.
    ///     </para>
    ///     <para>
    ///         Only the first HotKey specifier found in <see cref="Text"/> is supported.
    ///     </para>
    /// </remarks>
    /// <param name="isWidth">
    ///     If <see langword="true"/> (the default) the width required for the HotKey specifier is returned.
    ///     Otherwise the height is returned.
    /// </param>
    /// <returns>
    ///     The number of characters required for the <see cref="TextFormatter.HotKeySpecifier"/>. If the text direction
    ///     specified by <see cref="TextDirection"/> does not match the <paramref name="isWidth"/> parameter, <c>0</c> is
    ///     returned.
    /// </returns>
    public int GetHotKeySpecifierLength (bool isWidth = true)
    {
        if (isWidth)
        {
            return TextFormatter.IsHorizontalDirection (TextDirection) && TextFormatter.Text?.Contains ((char)TextFormatter.HotKeySpecifier.Value) == true
                       ? Math.Max (TextFormatter.HotKeySpecifier.GetColumns (), 0)
                       : 0;
        }

        return TextFormatter.IsVerticalDirection (TextDirection) && TextFormatter.Text?.Contains ((char)TextFormatter.HotKeySpecifier.Value) == true
                   ? Math.Max (TextFormatter.HotKeySpecifier.GetColumns (), 0)
                   : 0;
    }

    /// <summary>Can be overridden if the <see cref="Terminal.Gui.TextFormatter.Text"/> has different format than the default.</summary>
    protected virtual void UpdateTextFormatterText ()
    {
        if (TextFormatter is { })
        {
            TextFormatter.Text = _text;
        }
    }

    /// <summary>Gets the dimensions required for <see cref="Text"/> ignoring a <see cref="TextFormatter.HotKeySpecifier"/>.</summary>
    /// <returns></returns>
    internal Size GetSizeNeededForTextWithoutHotKey ()
    {
        return new (
                    TextFormatter.Size.Width - GetHotKeySpecifierLength (),
                    TextFormatter.Size.Height - GetHotKeySpecifierLength (false)
                   );
    }

    /// <summary>
    ///     Internal API. Sets <see cref="TextFormatter"/>.Size to the current <see cref="Viewport"/> size, adjusted for
    ///     <see cref="TextFormatter.HotKeySpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     Use this API to set <see cref="TextFormatter.Size"/> when the view has changed such that the size required to
    ///     fit the text has changed. changes.
    /// </remarks>
    /// <returns></returns>
    internal void SetTextFormatterSize ()
    {
        if (!IsInitialized)
        {
            TextFormatter.Size = Size.Empty;

            return;
        }

        if (string.IsNullOrEmpty (TextFormatter.Text))
        {
            TextFormatter.Size = ContentSize;

            return;
        }

        TextFormatter.Size = new (
                                  ContentSize.Width + GetHotKeySpecifierLength (),
                                  ContentSize.Height + GetHotKeySpecifierLength (false)
                                 );
    }

    private bool IsValidAutoSize (out Size autoSize)
    {
        Rectangle rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);

        autoSize = new (
                        rect.Size.Width - GetHotKeySpecifierLength (),
                        rect.Size.Height - GetHotKeySpecifierLength (false)
                       );

        return !((ValidatePosDim && (!(Width is Dim.DimAbsolute) || !(Height is Dim.DimAbsolute)))
                 || _frame.Size.Width != rect.Size.Width - GetHotKeySpecifierLength ()
                 || _frame.Size.Height != rect.Size.Height - GetHotKeySpecifierLength (false));
    }

    private bool IsValidAutoSizeHeight (Dim height)
    {
        Rectangle rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
        int dimValue = height.Anchor (0);

        return !((ValidatePosDim && !(height is Dim.DimAbsolute))
                 || dimValue != rect.Size.Height - GetHotKeySpecifierLength (false));
    }

    private bool IsValidAutoSizeWidth (Dim width)
    {
        Rectangle rect = TextFormatter.CalcRect (_frame.X, _frame.Y, TextFormatter.Text, TextDirection);
        int dimValue = width.Anchor (0);

        return !((ValidatePosDim && !(width is Dim.DimAbsolute))
                 || dimValue != rect.Size.Width - GetHotKeySpecifierLength ());
    }

    /// <summary>Sets the size of the View to the minimum width or height required to fit <see cref="Text"/>.</summary>
    /// <returns>
    ///     <see langword="true"/> if the size was changed; <see langword="false"/> if <see cref="AutoSize"/> ==
    ///     <see langword="true"/> or <see cref="Text"/> will not fit.
    /// </returns>
    /// <remarks>
    ///     Always returns <see langword="false"/> if <see cref="AutoSize"/> is <see langword="true"/> or if
    ///     <see cref="Height"/> (Horizontal) or <see cref="Width"/> (Vertical) are not not set or zero. Does not take into
    ///     account word wrapping.
    /// </remarks>
    private bool SetFrameToFitText ()
    {
        if (AutoSize == false)
        {
            throw new InvalidOperationException ("SetFrameToFitText can only be called when AutoSize is true");
        }

        // BUGBUG: This API is broken - should not assume Frame.Height == Viewport.Height
        // <summary>
        // Gets the minimum dimensions required to fit the View's <see cref="Text"/>, factoring in <see cref="TextDirection"/>.
        // </summary>
        // <param name="sizeRequired">The minimum dimensions required.</param>
        // <returns><see langword="true"/> if the dimensions fit within the View's <see cref="Viewport"/>, <see langword="false"/> otherwise.</returns>
        // <remarks>
        // Always returns <see langword="false"/> if <see cref="AutoSize"/> is <see langword="true"/> or
        // if <see cref="Height"/> (Horizontal) or <see cref="Width"/> (Vertical) are not not set or zero.
        // Does not take into account word wrapping.
        // </remarks>
        bool GetMinimumSizeOfText (out Size sizeRequired)
        {
            if (!IsInitialized)
            {
                sizeRequired = Size.Empty;

                return false;
            }

            sizeRequired = ContentSize;

            if (AutoSize || string.IsNullOrEmpty (TextFormatter.Text))
            {
                return false;
            }

            switch (TextFormatter.IsVerticalDirection (TextDirection))
            {
                case true:
                    int colWidth = TextFormatter.GetColumnsRequiredForVerticalText (new List<string> { TextFormatter.Text }, 0, 1);

                    // TODO: v2 - This uses frame.Width; it should only use Viewport
                    if (_frame.Width < colWidth
                        && (Width is null || (ContentSize.Width >= 0 && Width is Dim.DimAbsolute && Width.Anchor (0) >= 0 && Width.Anchor (0) < colWidth)))
                    {
                        sizeRequired = new (colWidth, ContentSize.Height);

                        return true;
                    }

                    break;
                default:
                    if (_frame.Height < 1 && (Height is null || (Height is Dim.DimAbsolute && Height.Anchor (0) == 0)))
                    {
                        sizeRequired = new (ContentSize.Width, 1);

                        return true;
                    }

                    break;
            }

            return false;
        }

        if (GetMinimumSizeOfText (out Size size))
        {
            // TODO: This is a hack.
            //_width  = size.Width;
            //_height = size.Height;
            SetFrame (new (_frame.Location, size));

            //throw new InvalidOperationException ("This is a hack.");
            return true;
        }

        return false;
    }

    // only called from EndInit
    private void UpdateTextDirection (TextDirection newDirection)
    {
        bool directionChanged = TextFormatter.IsHorizontalDirection (TextFormatter.Direction)
                                != TextFormatter.IsHorizontalDirection (newDirection);
        TextFormatter.Direction = newDirection;

        bool isValidOldAutoSize = AutoSize && IsValidAutoSize (out Size _);

        UpdateTextFormatterText ();

        if ((!ValidatePosDim && directionChanged && AutoSize)
            || (ValidatePosDim && directionChanged && AutoSize && isValidOldAutoSize))
        {
            OnResizeNeeded ();
        }
        else if (AutoSize && directionChanged && IsAdded)
        {
            ResizeViewportToFit (Viewport.Size);
        }

        SetTextFormatterSize ();
        SetNeedsDisplay ();
    }
}
