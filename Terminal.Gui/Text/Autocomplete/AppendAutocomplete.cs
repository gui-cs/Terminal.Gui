namespace Terminal.Gui;

/// <summary>
///     Autocomplete for a <see cref="TextField"/> which shows suggestions within the box. Displayed suggestions can
///     be completed using the tab key.
/// </summary>
public class AppendAutocomplete : AutocompleteBase
{
    private bool _suspendSuggestions;
    private TextField textField;

    /// <summary>Creates a new instance of the <see cref="AppendAutocomplete"/> class.</summary>
    public AppendAutocomplete (TextField textField)
    {
        this.textField = textField;
        SelectionKey = KeyCode.Tab;

        ColorScheme = new ColorScheme
        {
            Normal = new Attribute (Color.DarkGray, Color.Black),
            Focus = new Attribute (Color.DarkGray, Color.Black),
            HotNormal = new Attribute (Color.DarkGray, Color.Black),
            HotFocus = new Attribute (Color.DarkGray, Color.Black),
            Disabled = new Attribute (Color.DarkGray, Color.Black)
        };
    }

    /// <summary>
    ///     The color used for rendering the appended text. Note that only <see cref="ColorScheme.Normal"/> is used and
    ///     then only <see cref="Attribute.Foreground"/> (Background comes from <see cref="HostControl"/>).
    /// </summary>
    public override ColorScheme ColorScheme { get; set; }

    /// <inheritdoc/>
    public override View HostControl
    {
        get => textField;
        set => textField = (TextField)value;
    }

    /// <inheritdoc/>
    public override void ClearSuggestions ()
    {
        base.ClearSuggestions ();
        textField.SetNeedsDisplay ();
    }

    /// <inheritdoc/>
    public override void GenerateSuggestions (AutocompleteContext context)
    {
        if (_suspendSuggestions)
        {
            _suspendSuggestions = false;

            return;
        }

        base.GenerateSuggestions (context);
    }

    /// <inheritdoc/>
    public override bool OnMouseEvent (MouseEventArgs me, bool fromHost = false) { return false; }

    /// <inheritdoc/>
    public override bool ProcessKey (Key a)
    {
        Key key = a.KeyCode;

        if (key == SelectionKey)
        {
            return AcceptSelectionIfAny ();
        }

        if (key == Key.CursorUp)
        {
            return CycleSuggestion (1);
        }

        if (key == Key.CursorDown)
        {
            return CycleSuggestion (-1);
        }

        if (key == CloseKey && Suggestions.Any ())
        {
            ClearSuggestions ();
            _suspendSuggestions = true;

            return true;
        }

        if (char.IsLetterOrDigit ((char)a))
        {
            _suspendSuggestions = false;
        }

        return false;
    }

    /// <summary>Renders the current suggestion into the <see cref="TextField"/></summary>
    public override void RenderOverlay (Point renderAt)
    {
        if (!MakingSuggestion ())
        {
            return;
        }

        // draw it like it's selected, even though it's not
        Application.Driver?.SetAttribute (
                                         new Attribute (
                                                        ColorScheme.Normal.Foreground,
                                                        textField.ColorScheme.Focus.Background
                                                       )
                                        );
        textField.Move (textField.Text.Length, 0);

        Suggestion suggestion = Suggestions.ElementAt (SelectedIdx);
        string fragment = suggestion.Replacement.Substring (suggestion.Remove);

        int spaceAvailable = textField.Viewport.Width - textField.Text.GetColumns ();
        int spaceRequired = fragment.EnumerateRunes ().Sum (c => c.GetColumns ());

        if (spaceAvailable < spaceRequired)
        {
            fragment = new string (
                                   fragment.TakeWhile (c => (spaceAvailable -= ((Rune)c).GetColumns ()) >= 0)
                                           .ToArray ()
                                  );
        }

        Application.Driver?.AddStr (fragment);
    }

    /// <summary>
    ///     Accepts the current autocomplete suggestion displaying in the text box. Returns true if a valid suggestion was
    ///     being rendered and acceptable or false if no suggestion was showing.
    /// </summary>
    /// <returns></returns>
    internal bool AcceptSelectionIfAny ()
    {
        if (MakingSuggestion ())
        {
            Suggestion insert = Suggestions.ElementAt (SelectedIdx);
            string newText = textField.Text;
            newText = newText.Substring (0, newText.Length - insert.Remove);
            newText += insert.Replacement;
            textField.Text = newText;

            textField.MoveEnd ();

            ClearSuggestions ();

            return true;
        }

        return false;
    }

    internal void SetTextTo (FileSystemInfo fileSystemInfo)
    {
        string newText = fileSystemInfo.FullName;

        if (fileSystemInfo is DirectoryInfo)
        {
            newText += Path.DirectorySeparatorChar;
        }

        textField.Text = newText;
        textField.MoveEnd ();
    }

    private bool CycleSuggestion (int direction)
    {
        if (Suggestions.Count <= 1)
        {
            return false;
        }

        SelectedIdx = (SelectedIdx + direction) % Suggestions.Count;

        if (SelectedIdx < 0)
        {
            SelectedIdx = Suggestions.Count () - 1;
        }

        textField.SetNeedsDisplay ();

        return true;
    }

    /// <summary>
    ///     Returns true if there is a suggestion that can be made and the control is in a state where user would expect
    ///     to see auto-complete (i.e. focused and cursor in right place).
    /// </summary>
    /// <returns></returns>
    private bool MakingSuggestion () { return Suggestions.Any () && SelectedIdx != -1 && textField.HasFocus && textField.CursorIsAtEnd (); }
}
