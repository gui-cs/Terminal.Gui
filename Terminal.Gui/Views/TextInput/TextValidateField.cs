namespace Terminal.Gui.Views;

/// <summary>Masked text editor that validates input through a <see cref="ITextValidateProvider"/></summary>
public class TextValidateField : View, IDesignable
{
    private const int DEFAULT_LENGTH = 10;

    private ITextValidateProvider? _provider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TextValidateField"/> class.
    /// </summary>
    public TextValidateField ()
    {
        Height = Dim.Auto (minimumContentDim: 1);
        CanFocus = true;

        // Things this view knows how to do
        AddCommand (Command.LeftStart, () => HomeKeyHandler ());
        AddCommand (Command.RightEnd, () => EndKeyHandler ());
        AddCommand (Command.DeleteCharRight, () => DeleteKeyHandler ());
        AddCommand (Command.DeleteCharLeft, () => BackspaceKeyHandler ());
        AddCommand (Command.Left, () => CursorLeft ());
        AddCommand (Command.Right, () => CursorRight ());

        // Default keybindings for this view
        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.Delete, Command.DeleteCharRight);
        KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
    }

    /// <summary>This property returns true if the input is valid.</summary>
    public virtual bool IsValid
    {
        get
        {
            if (_provider is null)
            {
                return false;
            }

            return _provider.IsValid;
        }
    }

    /// <summary>Provider</summary>
    public ITextValidateProvider? Provider
    {
        get => _provider;
        set
        {
            _provider = value;

            if (_provider!.Fixed)
            {
                Width = _provider.DisplayText == string.Empty
                            ? DEFAULT_LENGTH
                            : _provider.DisplayText.Length;
            }

            // HomeKeyHandler already call SetNeedsDisplay
            HomeKeyHandler ();
        }
    }

    /// <summary>Text</summary>
    public new string Text
    {
        get => _provider is null ? string.Empty : _provider.Text;
        set
        {
            if (_provider is null)
            {
                return;
            }

            _provider.Text = value;

            SetNeedsDraw ();
        }
    }

    private int _insertionPoint;
    private int InsertionPoint
    {
        get => _insertionPoint;
        set
        {
            if (_insertionPoint == value)
            {
                return;
            }

            _insertionPoint = value;
            (int left, _) = GetMargins (Viewport.Width);

            // Fixed = true, is for inputs that have fixed width, like masked ones.
            // Fixed = false, is for normal input.
            // When it's right-aligned and it's a normal input, the cursor behaves differently.
            int curPos;

            if (_provider?.Fixed == false && TextAlignment == Alignment.End)
            {
                curPos = _insertionPoint + left - 1;
            }
            else
            {
                curPos = _insertionPoint + left;
            }

            SetCursor (Cursor with
            {
                Position = ViewportToScreen (new Point (curPos, 0)),
                Shape = CursorShape.Default
            });
        }
    }
    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse mouse)
    {
        if (!mouse.Flags.HasFlag (MouseFlags.LeftButtonPressed))
        {
            return false;
        }

        int c = _provider!.Cursor (mouse.Position!.Value.X - GetMargins (Viewport.Width).left);

        if (!_provider.Fixed && TextAlignment == Alignment.End && Text.Length > 0)
        {
            c++;
        }

        InsertionPoint = c;
        SetFocus ();
        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (_provider is null)
        {
            Move (0, 0);
            AddStr ("Error: ITextValidateProvider not set!");

            return true;
        }

        VisualRole role = HasFocus ? VisualRole.Focus : VisualRole.Editable;
        Attribute textColor = IsValid ? GetAttributeForRole (role) : SchemeManager.GetScheme (Schemes.Error).GetAttributeForRole (role);

        (int marginLeft, int marginRight) = GetMargins (Viewport.Width);

        Move (0, 0);

        // Left Margin
        SetAttribute (textColor);

        for (var i = 0; i < marginLeft; i++)
        {
            AddRune ((Rune)' ');
        }

        // Content
        SetAttribute (textColor);

        // Content
        foreach (char t in _provider.DisplayText)
        {
            AddRune ((Rune)t);
        }

        // Right Margin
        SetAttribute (textColor);

        for (var i = 0; i < marginRight; i++)
        {
            AddRune ((Rune)' ');
        }

        return true;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key key)
    {
        if (_provider is null)
        {
            return false;
        }

        if (key.AsRune == default (Rune) || key == Application.QuitKey)
        {
            return false;
        }

        Rune rune = key.AsRune;

        bool inserted = _provider.InsertAt ((char)rune.Value, InsertionPoint);

        if (inserted)
        {
            CursorRight ();

            return true;
        }

        return false;
    }

    /// <summary>Delete char at cursor position - 1, moving the cursor.</summary>
    /// <returns></returns>
    private bool BackspaceKeyHandler ()
    {
        if (!_provider!.Fixed && TextAlignment == Alignment.End && InsertionPoint <= 1)
        {
            return false;
        }

        _insertionPoint = _provider.CursorLeft (InsertionPoint);
        _provider.Delete (InsertionPoint);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Try to move the cursor to the left.</summary>
    /// <returns>True if moved.</returns>
    private bool CursorLeft ()
    {
        if (_provider is null)
        {
            return false;
        }

        int current = _insertionPoint;
        InsertionPoint = _provider.CursorLeft (InsertionPoint);
        SetNeedsDraw ();

        return current != InsertionPoint;
    }

    /// <summary>Try to move the cursor to the right.</summary>
    /// <returns>True if moved.</returns>
    private bool CursorRight ()
    {
        if (_provider is null)
        {
            return false;
        }

        int current = InsertionPoint;
        InsertionPoint = _provider.CursorRight (InsertionPoint);
        SetNeedsDraw ();

        return current != InsertionPoint;
    }

    /// <summary>Deletes char at current position.</summary>
    /// <returns></returns>
    private bool DeleteKeyHandler ()
    {
        if (!_provider!.Fixed && TextAlignment == Alignment.End)
        {
            InsertionPoint = _provider.CursorLeft (InsertionPoint);
        }

        _provider.Delete (InsertionPoint);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Moves the cursor to the last char.</summary>
    /// <returns></returns>
    private bool EndKeyHandler ()
    {
        InsertionPoint = _provider!.CursorEnd ();
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Margins for text alignment.</summary>
    /// <param name="width">Total width</param>
    /// <returns>Left and right margins</returns>
    private (int left, int right) GetMargins (int width)
    {
        int count = Text.Length;
        int total = width - count;

        return TextAlignment switch
        {
            Alignment.Start => (0, total),
            Alignment.Center => (total / 2, total / 2 + total % 2),
            Alignment.End => (total, 0),
            _ => (0, total)
        };
    }

    /// <summary>Moves the cursor to first char.</summary>
    /// <returns></returns>
    private bool HomeKeyHandler ()
    {
        InsertionPoint = _provider!.CursorStart ();
        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        TextRegexProvider provider = new ("^([0-9]?[0-9]?[0-9]|1000)$") { ValidateOnInput = false };

        BorderStyle = LineStyle.Single;
        Title = provider.Pattern;
        Provider = provider;

        Text = "999";

        return true;
    }
}
