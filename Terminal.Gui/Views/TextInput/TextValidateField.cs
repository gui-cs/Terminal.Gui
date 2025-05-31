#nullable enable


namespace Terminal.Gui.Views;

/// <summary>Masked text editor that validates input through a <see cref="ITextValidateProvider"/></summary>
public class TextValidateField : View, IDesignable
{
    private const int DEFAULT_LENGTH = 10;
    private int _cursorPosition;
    private ITextValidateProvider? _provider;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TextValidateField"/> class.
    /// </summary>
    public TextValidateField ()
    {
        Height = Dim.Auto (minimumContentDim: 1);
        CanFocus = true;

        // Things this view knows how to do
        AddCommand (
                    Command.LeftStart,
                    () =>
                    {
                        HomeKeyHandler ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEnd,
                    () =>
                    {
                        EndKeyHandler ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharRight,
                    () =>
                    {
                        DeleteKeyHandler ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharLeft,
                    () =>
                    {
                        BackspaceKeyHandler ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Left,
                    () =>
                    {
                        CursorLeft ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Right,
                    () =>
                    {
                        CursorRight ();

                        return true;
                    }
                   );

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
        get
        {
            if (_provider is null)
            {
                return string.Empty;
            }

            return _provider.Text;
        }
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

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs mouseEvent)
    {
        if (mouseEvent.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            int c = _provider!.Cursor (mouseEvent.Position.X - GetMargins (Viewport.Width).left);

            if (_provider.Fixed == false && TextAlignment == Alignment.End && Text.Length > 0)
            {
                c++;
            }

            _cursorPosition = c;
            SetFocus ();
            SetNeedsDraw ();

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        if (_provider is null)
        {
            Move (0, 0);
            Driver?.AddStr ("Error: ITextValidateProvider not set!");

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
            Driver?.AddRune ((Rune)' ');
        }

        // Content
        SetAttribute (textColor);

        // Content
        for (var i = 0; i < _provider.DisplayText.Length; i++)
        {
            Driver?.AddRune ((Rune)_provider.DisplayText [i]);
        }

        // Right Margin
        SetAttribute (textColor);

        for (var i = 0; i < marginRight; i++)
        {
            Driver?.AddRune ((Rune)' ');
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

        bool inserted = _provider.InsertAt ((char)rune.Value, _cursorPosition);

        if (inserted)
        {
            CursorRight ();

            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        (int left, _) = GetMargins (Viewport.Width);

        // Fixed = true, is for inputs that have fixed width, like masked ones.
        // Fixed = false, is for normal input.
        // When it's right-aligned and it's a normal input, the cursor behaves differently.
        int curPos;

        if (_provider?.Fixed == false && TextAlignment == Alignment.End)
        {
            curPos = _cursorPosition + left - 1;
        }
        else
        {
            curPos = _cursorPosition + left;
        }

        Move (curPos, 0);

        return new (curPos, 0);
    }

    /// <summary>Delete char at cursor position - 1, moving the cursor.</summary>
    /// <returns></returns>
    private bool BackspaceKeyHandler ()
    {
        if (_provider!.Fixed == false && TextAlignment == Alignment.End && _cursorPosition <= 1)
        {
            return false;
        }

        _cursorPosition = _provider.CursorLeft (_cursorPosition);
        _provider.Delete (_cursorPosition);
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

        int current = _cursorPosition;
        _cursorPosition = _provider.CursorLeft (_cursorPosition);
        SetNeedsDraw ();

        return current != _cursorPosition;
    }

    /// <summary>Try to move the cursor to the right.</summary>
    /// <returns>True if moved.</returns>
    private bool CursorRight ()
    {
        if (_provider is null)
        {
            return false;
        }

        int current = _cursorPosition;
        _cursorPosition = _provider.CursorRight (_cursorPosition);
        SetNeedsDraw ();

        return current != _cursorPosition;
    }

    /// <summary>Deletes char at current position.</summary>
    /// <returns></returns>
    private bool DeleteKeyHandler ()
    {
        if (_provider!.Fixed == false && TextAlignment == Alignment.End)
        {
            _cursorPosition = _provider.CursorLeft (_cursorPosition);
        }

        _provider.Delete (_cursorPosition);
        SetNeedsDraw ();

        return true;
    }

    /// <summary>Moves the cursor to the last char.</summary>
    /// <returns></returns>
    private bool EndKeyHandler ()
    {
        _cursorPosition = _provider!.CursorEnd ();
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
        _cursorPosition = _provider!.CursorStart ();
        SetNeedsDraw ();

        return true;
    }

    /// <inheritdoc />
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
