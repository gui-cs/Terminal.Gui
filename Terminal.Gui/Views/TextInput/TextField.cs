using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Single-line text editor.</summary>
/// <remarks>The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.</remarks>
public class TextField : View, IDesignable
{
    private readonly HistoryText _historyText;
    private CultureInfo _currentCulture;

    private bool _isButtonPressed;
    private bool _isButtonReleased;
    private bool _isDrawing;

    /// <summary>
    ///     Caches the cursor position before a text change operation. Used to properly handle text insertion
    ///     and deletion operations, particularly for undo/redo and proper cursor placement after edits.
    /// </summary>
    private int _preTextChangedCursorPos;

    /// <summary>
    ///     The starting position of the text selection, measured as a 0-based index into text elements.
    ///     A value of -1 indicates no active selection. This is the backing field for <see cref="SelectedStart"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         When selecting text:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>This marks where the selection began (the anchor point)</description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="_cursorPosition"/> marks the current end of the selection</description>
    ///             </item>
    ///             <item>
    ///                 <description>Selection can extend in either direction from this point</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         The <see cref="_start"/> field holds the normalized start position (always the lesser of
    ///         <see cref="_selectedStart"/> and <see cref="_cursorPosition"/>), used for drawing and text operations.
    ///     </para>
    /// </remarks>
    private int _selectedStart;

    private string? _selectedText;

    /// <summary>
    ///     The normalized start position of the selection for drawing purposes. Unlike <see cref="_selectedStart"/>
    ///     (the anchor), this is always the leftmost position of the selection range.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This value is computed by <see cref="SetSelectedStartSelectedLength"/> to ensure:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>When selecting left-to-right: <c>_start == _selectedStart</c></description>
    ///             </item>
    ///             <item>
    ///                 <description>When selecting right-to-left: <c>_start == _cursorPosition</c></description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This normalization simplifies rendering logic in <see cref="OnDrawingContent"/> and
    ///         text operations in <see cref="DeleteSelectedText"/>.
    ///     </para>
    /// </remarks>
    private int _start;

    /// <summary>
    ///     The text content stored as a list of strings, where each string represents a single text element
    ///     (grapheme cluster). This allows proper handling of Unicode combining characters and emoji.
    /// </summary>
    private List<string> _text;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TextField"/> class.
    /// </summary>
    public TextField ()
    {
        _historyText = new ();
        _isButtonReleased = true;
        _selectedStart = -1;
        _text = new ();

        ReadOnly = false;
        Autocomplete = new TextFieldAutocomplete ();
        Height = Dim.Auto (DimAutoStyle.Text, 1);

        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;
        Used = true;
        MousePositionTracking = true;

        _historyText.ChangeText += HistoryText_ChangeText;

        Initialized += TextField_Initialized;

        // Things this view knows how to do
        AddCommand (
                    Command.DeleteCharRight,
                    () =>
                    {
                        DeleteCharRight ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharLeft,
                    () =>
                    {
                        DeleteCharLeft (false);

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftStartExtend,
                    () =>
                    {
                        MoveHomeExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEndExtend,
                    () =>
                    {
                        MoveEndExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftStart,
                    () =>
                    {
                        MoveHome ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftExtend,
                    () =>
                    {
                        MoveLeftExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightExtend,
                    () =>
                    {
                        MoveRightExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordLeftExtend,
                    () =>
                    {
                        MoveWordLeftExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordRightExtend,
                    () =>
                    {
                        MoveWordRightExtend ();

                        return true;
                    }
                   );

        AddCommand (Command.Left, () => MoveLeft ());

        AddCommand (
                    Command.RightEnd,
                    () =>
                    {
                        MoveEnd ();

                        return true;
                    }
                   );

        AddCommand (Command.Right, () => MoveRight ());

        AddCommand (
                    Command.CutToEndLine,
                    () =>
                    {
                        KillToEnd ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.CutToStartLine,
                    () =>
                    {
                        KillToStart ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Undo,
                    () =>
                    {
                        Undo ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Redo,
                    () =>
                    {
                        Redo ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordLeft,
                    () =>
                    {
                        MoveWordLeft ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordRight,
                    () =>
                    {
                        MoveWordRight ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.KillWordForwards,
                    () =>
                    {
                        KillWordForwards ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.KillWordBackwards,
                    () =>
                    {
                        KillWordBackwards ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.ToggleOverwrite,
                    () =>
                    {
                        SetOverwrite (!Used);

                        return true;
                    }
                   );

        AddCommand (
                    Command.EnableOverwrite,
                    () =>
                    {
                        SetOverwrite (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.DisableOverwrite,
                    () =>
                    {
                        SetOverwrite (false);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Copy,
                    () =>
                    {
                        Copy ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Cut,
                    () =>
                    {
                        Cut ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Paste,
                    () =>
                    {
                        Paste ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.SelectAll,
                    () =>
                    {
                        SelectAll ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteAll,
                    () =>
                    {
                        DeleteAll ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Context,
                    () =>
                    {
                        ShowContextMenu (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.HotKey,
                    ctx =>
                    {
                        if (RaiseHandlingHotKey (ctx) is true)
                        {
                            return true;
                        }

                        // If we have focus, then ignore the hotkey because the user
                        // means to enter it
                        if (HasFocus)
                        {
                            return false;
                        }

                        // This is what the default HotKey handler does:
                        SetFocus ();

                        // Always return true on hotkey, even if SetFocus fails because
                        // hotkeys are always handled by the View (unless RaiseHandlingHotKey cancels).
                        return true;
                    });

        // Default keybindings for this view
        // We follow this as closely as possible: https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts
        KeyBindings.Add (Key.Delete, Command.DeleteCharRight);
        KeyBindings.Add (Key.D.WithCtrl, Command.DeleteCharRight);

        KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);

        KeyBindings.Add (Key.Home.WithShift, Command.LeftStartExtend);
        KeyBindings.Add (Key.Home.WithShift.WithCtrl, Command.LeftStartExtend);
        KeyBindings.Add (Key.A.WithShift.WithCtrl, Command.LeftStartExtend);

        KeyBindings.Add (Key.End.WithShift, Command.RightEndExtend);
        KeyBindings.Add (Key.End.WithShift.WithCtrl, Command.RightEndExtend);
        KeyBindings.Add (Key.E.WithShift.WithCtrl, Command.RightEndExtend);

        KeyBindings.Add (Key.Home, Command.LeftStart);
        KeyBindings.Add (Key.Home.WithCtrl, Command.LeftStart);

        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);
        KeyBindings.Add (Key.CursorUp.WithShift, Command.LeftExtend);

        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);
        KeyBindings.Add (Key.CursorDown.WithShift, Command.RightExtend);

        KeyBindings.Add (Key.CursorLeft.WithShift.WithCtrl, Command.WordLeftExtend);
        KeyBindings.Add (Key.CursorUp.WithShift.WithCtrl, Command.WordLeftExtend);

        KeyBindings.Add (Key.CursorRight.WithShift.WithCtrl, Command.WordRightExtend);
        KeyBindings.Add (Key.CursorDown.WithShift.WithCtrl, Command.WordRightExtend);

        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.B.WithCtrl, Command.Left);

        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.End.WithCtrl, Command.RightEnd);
        KeyBindings.Add (Key.E.WithCtrl, Command.RightEnd);

        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.F.WithCtrl, Command.Right);

        KeyBindings.Add (Key.K.WithCtrl, Command.CutToEndLine);
        KeyBindings.Add (Key.K.WithCtrl.WithShift, Command.CutToStartLine);

        KeyBindings.Add (Key.Z.WithCtrl, Command.Undo);

        KeyBindings.Add (Key.Y.WithCtrl, Command.Redo);

        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.WordLeft);
        KeyBindings.Add (Key.CursorUp.WithCtrl, Command.WordLeft);

        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.WordRight);
        KeyBindings.Add (Key.CursorDown.WithCtrl, Command.WordRight);

#if UNIX_KEY_BINDINGS
        KeyBindings.Add (Key.F.WithShift.WithAlt, Command.WordRightExtend);
        KeyBindings.Add (Key.K.WithAlt, Command.CutToStartLine);
        KeyBindings.Add (Key.B.WithShift.WithAlt, Command.WordLeftExtend);
        KeyBindings.Add (Key.B.WithAlt, Command.WordLeft);
        KeyBindings.Add (Key.F.WithAlt, Command.WordRight);
        KeyBindings.Add (Key.Backspace.WithAlt, Command.Undo);
#endif

        KeyBindings.Add (Key.Delete.WithCtrl, Command.KillWordForwards);
        KeyBindings.Add (Key.Backspace.WithCtrl, Command.KillWordBackwards);
        KeyBindings.Add (Key.InsertChar, Command.ToggleOverwrite);
        KeyBindings.Add (Key.C.WithCtrl, Command.Copy);
        KeyBindings.Add (Key.X.WithCtrl, Command.Cut);
        KeyBindings.Add (Key.V.WithCtrl, Command.Paste);
        KeyBindings.Add (Key.A.WithCtrl, Command.SelectAll);

        KeyBindings.Add (Key.R.WithCtrl, Command.DeleteAll);
        KeyBindings.Add (Key.D.WithCtrl.WithShift, Command.DeleteAll);

        KeyBindings.Remove (Key.Space);

        _currentCulture = Thread.CurrentThread.CurrentUICulture;
    }

    /// <summary>
    ///     Provides autocomplete context menu based on suggestions at the current cursor position. Configure
    ///     <see cref="ISuggestionGenerator"/> to enable this feature.
    /// </summary>
    public IAutocomplete Autocomplete { get; set; }

    /// <summary>Get the Context Menu for this view.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    /// <summary>
    ///     The internal cursor position within the text, measured as a 0-based index into the text elements
    ///     (graphemes/runes), not screen columns. This is the backing field for <see cref="CursorPosition"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This value represents the logical cursor position in the text, where 0 is before the first character
    ///         and <c>_text.Count</c> is after the last character. For example, in the text "Hello":
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Position 0 = cursor is before 'H'</description>
    ///             </item>
    ///             <item>
    ///                 <description>Position 5 = cursor is after 'o' (at the end)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This differs from screen position because:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Wide characters (e.g., CJK) occupy multiple screen columns but are a single text element</description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="ScrollOffset"/> shifts the visible portion of text</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Use <see cref="PositionCursor()"/> to convert this logical position to screen coordinates.
    ///     </para>
    /// </remarks>
    private int _cursorPosition;

    /// <summary>
    ///     Gets or sets the current cursor position within the text, measured as a 0-based index into text elements.
    /// </summary>
    /// <value>
    ///     The cursor position, clamped to the range [0, Text.Length]. Position 0 is before the first character;
    ///     position equal to the text length is after the last character.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This property provides access to the logical cursor position within the text. The value is automatically
    ///         clamped to valid bounds: values less than 0 become 0, and values greater than the text length become
    ///         the text length.
    ///     </para>
    ///     <para>
    ///         <b>Relationship to <see cref="PositionCursor()"/>:</b>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><see cref="CursorPosition"/>: Logical position in text elements (0-based index)</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <see cref="PositionCursor()"/>: Converts logical position to screen coordinates, accounting for
    ///                     <see cref="ScrollOffset"/> and wide characters
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> For text "Hello世界" (Hello + 2 CJK characters):
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>CursorPosition = 0: Before 'H'</description>
    ///             </item>
    ///             <item>
    ///                 <description>CursorPosition = 5: Before '世'</description>
    ///             </item>
    ///             <item>
    ///                 <description>CursorPosition = 7: After '界' (end of text)</description>
    ///             </item>
    ///         </list>
    ///         Note that screen columns would differ because '世' and '界' each occupy 2 columns.
    ///     </para>
    ///     <para>
    ///         Setting this property also updates the text selection via <see cref="PrepareSelection"/>.
    ///     </para>
    /// </remarks>
    /// <seealso cref="PositionCursor()"/>
    /// <seealso cref="ScrollOffset"/>
    public virtual int CursorPosition
    {
        get => _cursorPosition;
        set
        {
            if (value < 0)
            {
                _cursorPosition = 0;
            }
            else if (value > _text.Count)
            {
                _cursorPosition = _text.Count;
            }
            else
            {
                _cursorPosition = value;
            }

            PrepareSelection (_selectedStart, _cursorPosition - _selectedStart);
        }
    }

    /// <summary>
    ///     Indicates whatever the text has history changes or not. <see langword="true"/> if the text has history changes
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool HasHistoryChanges => _historyText.HasHistoryChanges;

    /// <summary>
    ///     Indicates whatever the text was changed or not. <see langword="true"/> if the text was changed
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool IsDirty => _historyText.IsDirty ([Cell.StringToCells (Text)]);

    /// <summary>If set to true its not allow any changes in the text.</summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     Gets the horizontal scroll offset, representing the index of the first visible text element.
    /// </summary>
    /// <value>
    ///     A 0-based index into the text elements indicating which element appears at the left edge of the viewport.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         When the text is longer than the viewport width, this property tracks how far the view has scrolled.
    ///         The <see cref="Adjust"/> method automatically updates this value to keep the cursor visible.
    ///     </para>
    ///     <para>
    ///         <b>Relationship to cursor positioning:</b>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><see cref="CursorPosition"/>: Absolute position in the text (0 to text length)</description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="ScrollOffset"/>: Index of first visible character</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Screen column = <see cref="CursorPosition"/> - <see cref="ScrollOffset"/> (approximately,
    ///                     adjusted for wide chars)
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> For text "Hello World" with viewport width 5:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>ScrollOffset = 0: Shows "Hello"</description>
    ///             </item>
    ///             <item>
    ///                 <description>ScrollOffset = 6: Shows "World"</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    /// <seealso cref="CursorPosition"/>
    /// <seealso cref="PositionCursor()"/>
    public int ScrollOffset { get; private set; }

    /// <summary>
    ///     Sets the secret property.
    ///     <remarks>This makes the text entry suitable for entering passwords.</remarks>
    /// </summary>
    public bool Secret { get; set; }

    /// <summary>
    ///     Gets the length of the selected text in text elements.
    /// </summary>
    /// <value>
    ///     The number of text elements (graphemes) currently selected. Returns 0 when no text is selected.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This value represents the absolute length of the selection, regardless of selection direction.
    ///         Use in combination with <see cref="SelectedStart"/> and <see cref="SelectedText"/> to work with selections.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SelectedStart"/>
    /// <seealso cref="SelectedText"/>
    public int SelectedLength { get; private set; }

    /// <summary>
    ///     Gets or sets the anchor position where text selection began, measured as a 0-based index into text elements.
    /// </summary>
    /// <value>
    ///     The starting position of the selection, or -1 if no selection is active.
    ///     The value is clamped to the range [-1, Text.Length].
    /// </value>
    /// <remarks>
    ///     <para>
    ///         <b>Selection model:</b> TextField uses an anchor-based selection model:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     <see cref="SelectedStart"/>: The anchor point where selection began (can be before or
    ///                     after cursor)
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="CursorPosition"/>: The current end of the selection</description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="SelectedLength"/>: The absolute length of the selection</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> In text "Hello World", selecting "World" by shift+clicking:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>
    ///                     If cursor was at position 6 and user shift-clicks at position 11: SelectedStart=6,
    ///                     CursorPosition=11
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     If cursor was at position 11 and user shift-clicks at position 6: SelectedStart=11,
    ///                     CursorPosition=6
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>In both cases, SelectedLength=5 and SelectedText="World"</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Setting this property triggers <see cref="PrepareSelection"/> to update the selection state.
    ///     </para>
    /// </remarks>
    /// <seealso cref="SelectedLength"/>
    /// <seealso cref="SelectedText"/>
    /// <seealso cref="CursorPosition"/>
    public int SelectedStart
    {
        get => _selectedStart;
        set
        {
            if (value < -1)
            {
                _selectedStart = -1;
            }
            else if (value > _text.Count)
            {
                _selectedStart = _text.Count;
            }
            else
            {
                _selectedStart = value;
            }

            PrepareSelection (_selectedStart, _cursorPosition - _selectedStart);
        }
    }

    /// <summary>Gets The selected text.</summary>
    public string? SelectedText => Secret ? null : _selectedText;

    /// <summary>Sets or gets the text held by the view.</summary>
    public new string Text
    {
        get => StringExtensions.ToString (_text);
        set
        {
            var oldText = StringExtensions.ToString (_text);

            if (oldText == value)
            {
                return;
            }

            string newText = value.Replace ("\t", "").Split ("\n") [0];
            ResultEventArgs<string> args = new (newText);
            RaiseTextChanging (args);

            if (args.Handled)
            {
                if (_cursorPosition > _text.Count)
                {
                    _cursorPosition = _text.Count;
                }

                return;
            }

            ClearAllSelection ();

            // Note we use NewValue here; TextChanging subscribers may have changed it
            _text = args.Result!.ToStringList ();

            if (!Secret && !_historyText.IsFromHistory)
            {
                _historyText.Add (
                                  [Cell.ToCellList (oldText)],
                                  new (_cursorPosition, 0)
                                 );

                _historyText.Add (
                                  [Cell.ToCells (_text)],
                                  new (_cursorPosition, 0),
                                  TextEditingLineStatus.Replaced
                                 );
            }

            OnTextChanged ();

            ProcessAutocomplete ();

            if (_cursorPosition > _text.Count)
            {
                _cursorPosition = Math.Max (TextModel.DisplaySize (_text, 0).size - 1, 0);
            }

            Adjust ();
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

    /// <summary>
    ///     Gets or sets whether the word forward and word backward navigation should use the same or equivalent rune type.
    ///     Default is <c>false</c> meaning using equivalent rune type.
    /// </summary>
    public bool UseSameRuneTypeForWords { get; set; }

    /// <summary>
    ///     Gets or sets whether the word navigation should select only the word itself without spaces around it or with the
    ///     spaces at right.
    ///     Default is <c>false</c> meaning that the spaces at right are included in the selection.
    /// </summary>
    public bool SelectWordOnlyOnDoubleClick { get; set; }

    /// <summary>Clear the selected text.</summary>
    public void ClearAllSelection ()
    {
        if (_selectedStart == -1 && SelectedLength == 0 && string.IsNullOrEmpty (_selectedText))
        {
            return;
        }

        _selectedStart = -1;
        SelectedLength = 0;
        _selectedText = null;
        _start = 0;
        SelectedLength = 0;
        SetNeedsDraw ();
    }

    /// <summary>Clears the history.</summary>
    public void ClearHistoryChanges () { _historyText.Clear ([Cell.StringToCells (Text)]); }

    /// <summary>Copy the selected text to the clipboard.</summary>
    public virtual void Copy ()
    {
        if (Secret || SelectedLength == 0 || SelectedText is null)
        {
            return;
        }

        App?.Clipboard?.SetClipboardData (SelectedText);
    }

    /// <summary>Cut the selected text to the clipboard.</summary>
    public virtual void Cut ()
    {
        if (ReadOnly || Secret || SelectedLength == 0 || SelectedText is null)
        {
            return;
        }

        App?.Clipboard?.SetClipboardData (SelectedText);
        List<string> newText = DeleteSelectedText ();
        Text = StringExtensions.ToString (newText);
        Adjust ();
    }

    /// <summary>Deletes all text.</summary>
    public void DeleteAll ()
    {
        if (_text.Count == 0)
        {
            return;
        }

        _selectedStart = 0;
        MoveEndExtend ();
        DeleteCharLeft (false);
        SetNeedsDraw ();
    }

    /// <summary>Deletes the character to the left.</summary>
    /// <param name="usePreTextChangedCursorPos">
    ///     If set to <see langword="true">true</see> use the cursor position cached ;
    ///     otherwise use <see cref="CursorPosition"/>. use .
    /// </param>
    public virtual void DeleteCharLeft (bool usePreTextChangedCursorPos)
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Add (
                          new () { Cell.ToCells (_text) },
                          new (_cursorPosition, 0)
                         );

        if (SelectedLength == 0)
        {
            if (_cursorPosition == 0)
            {
                return;
            }

            if (!usePreTextChangedCursorPos)
            {
                _preTextChangedCursorPos = _cursorPosition;
            }

            _cursorPosition--;

            if (_preTextChangedCursorPos < _text.Count)
            {
                SetText (
                         _text.GetRange (0, _preTextChangedCursorPos - 1)
                              .Concat (
                                       _text.GetRange (
                                                       _preTextChangedCursorPos,
                                                       _text.Count - _preTextChangedCursorPos
                                                      )
                                      )
                        );
            }
            else
            {
                SetText (_text.GetRange (0, _preTextChangedCursorPos - 1));
            }

            Adjust ();
        }
        else
        {
            List<string> newText = DeleteSelectedText ();
            Text = StringExtensions.ToString (newText);
            Adjust ();
        }
    }

    /// <summary>Deletes the character to the right.</summary>
    public virtual void DeleteCharRight ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Add (
                          new () { Cell.ToCells (_text) },
                          new (_cursorPosition, 0)
                         );

        if (SelectedLength == 0)
        {
            if (_text.Count == 0 || _text.Count == _cursorPosition)
            {
                return;
            }

            SetText (
                     _text.GetRange (0, _cursorPosition)
                          .Concat (_text.GetRange (_cursorPosition + 1, _text.Count - (_cursorPosition + 1)))
                    );
            Adjust ();
        }
        else
        {
            List<string> newText = DeleteSelectedText ();
            Text = StringExtensions.ToString (newText);
            Adjust ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role == VisualRole.Normal)
        {
            currentAttribute = GetAttributeForRole (VisualRole.Focus);

            return true;
        }

        return base.OnGettingAttributeForRole (role, ref currentAttribute);
    }

    /// <summary>
    ///     Inserts the given <paramref name="toAdd"/> text at the current cursor position exactly as if the user had just
    ///     typed it
    /// </summary>
    /// <param name="toAdd">Text to add</param>
    /// <param name="useOldCursorPos">Use the previous cursor position.</param>
    public void InsertText (string toAdd, bool useOldCursorPos = true)
    {
        foreach (Rune rune in toAdd.EnumerateRunes ())
        {
            // All rune can be mapped to a Key and no exception will throw here because
            // EnumerateRunes will replace a surrogate char with the Rune.ReplacementChar
            Key key = rune.Value;
            InsertText (key, useOldCursorPos);
        }
    }

    /// <summary>Deletes word backwards.</summary>
    public virtual void KillWordBackwards ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordBackward (_cursorPosition, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return;
        }

        if (newPos.Value.col != -1)
        {
            SetText (
                     _text.GetRange (0, newPos.Value.col)
                          .Concat (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition))
                    );
            _cursorPosition = newPos.Value.col;
        }

        Adjust ();
    }

    /// <summary>Deletes word forwards.</summary>
    public virtual void KillWordForwards ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordForward (_cursorPosition, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return;
        }

        if (newPos.Value.col != -1)
        {
            SetText (
                     _text.GetRange (0, _cursorPosition)
                          .Concat (_text.GetRange (newPos.Value.col, _text.Count - newPos.Value.col))
                    );
        }

        Adjust ();
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (Mouse ev)
    {
        if (ev is { IsPressed: false, IsReleased: false }
            && !ev.Flags.HasFlag (MouseFlags.PositionReport)
            && !ev.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked)
            && !ev.Flags.HasFlag (MouseFlags.LeftButtonTripleClicked)
            && !ev.Flags.HasFlag (ContextMenu!.MouseFlags))
        {
            return false;
        }

        if (!CanFocus)
        {
            return true;
        }

        if (!HasFocus && ev.Flags != MouseFlags.PositionReport)
        {
            SetFocus ();
        }

        // Give autocomplete first opportunity to respond to mouse clicks
        if (SelectedLength == 0 && Autocomplete.OnMouseEvent (ev, true))
        {
            return true;
        }

        if (ev.Flags == MouseFlags.LeftButtonPressed)
        {
            EnsureHasFocus ();
            PositionCursor (ev);

            if (_isButtonReleased)
            {
                ClearAllSelection ();
            }

            _isButtonReleased = true;
            _isButtonPressed = true;
        }
        else if (ev.Flags == (MouseFlags.LeftButtonPressed | MouseFlags.PositionReport) && _isButtonPressed)
        {
            int x = PositionCursor (ev);
            _isButtonReleased = false;
            PrepareSelection (x);

            if (App?.Mouse.MouseGrabView is null)
            {
                App?.Mouse.GrabMouse (this);
            }
        }
        else if (ev.Flags == MouseFlags.LeftButtonReleased)
        {
            _isButtonReleased = true;
            _isButtonPressed = false;
            App?.Mouse.UngrabMouse ();
        }
        else if (ev.Flags == MouseFlags.LeftButtonDoubleClicked)
        {
            EnsureHasFocus ();
            int x = PositionCursor (ev);
            (int startCol, int col, int row)? newPos = GetModel ().ProcessDoubleClickSelection (x, x, 0, UseSameRuneTypeForWords, SelectWordOnlyOnDoubleClick);

            if (newPos is null)
            {
                return true;
            }

            SelectedStart = newPos.Value.startCol;
            CursorPosition = newPos.Value.col;
        }
        else if (ev.Flags == MouseFlags.LeftButtonTripleClicked)
        {
            EnsureHasFocus ();
            PositionCursor (0);
            ClearAllSelection ();
            PrepareSelection (0, _text.Count);
        }
        else if (ev.Flags == ContextMenu!.MouseFlags)
        {
            PositionCursor (ev);
            ShowContextMenu (false);
        }

        //SetNeedsDraw ();

        return true;

        void EnsureHasFocus ()
        {
            if (!HasFocus)
            {
                SetFocus ();
            }
        }
    }

    /// <summary>Moves cursor to the end of the typed text.</summary>
    public void MoveEnd ()
    {
        ClearAllSelection ();
        _cursorPosition = _text.Count;
        Adjust ();
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        _isDrawing = true;

        // Cache attributes as GetAttributeForRole might raise events
        var selectedAttribute = new Attribute (GetAttributeForRole (VisualRole.Active));
        Attribute readonlyAttribute = GetAttributeForRole (VisualRole.ReadOnly);
        Attribute normalAttribute = GetAttributeForRole (VisualRole.Editable);

        SetSelectedStartSelectedLength ();

        SetAttribute (GetAttributeForRole (VisualRole.Normal));
        Move (0, 0);

        int p = ScrollOffset;
        var col = 0;
        int width = Viewport.Width;
        int tcount = _text.Count;

        for (int idx = p; idx < tcount; idx++)
        {
            string text = _text [idx];
            int cols = text.GetColumns ();

            if (!Enabled)
            {
                // Disabled
                SetAttributeForRole (VisualRole.Disabled);
            }
            else if (idx == _cursorPosition && HasFocus && !Used && SelectedLength == 0 && !ReadOnly)
            {
                // Selected text
                SetAttribute (selectedAttribute);
            }
            else if (ReadOnly)
            {
                SetAttribute (
                              idx >= _start && SelectedLength > 0 && idx < _start + SelectedLength
                                  ? selectedAttribute
                                  : readonlyAttribute
                             );
            }
            else if (!HasFocus && Enabled)
            {
                // Normal text
                SetAttribute (normalAttribute);
            }
            else
            {
                SetAttribute (
                              idx >= _start && SelectedLength > 0 && idx < _start + SelectedLength
                                  ? selectedAttribute
                                  : normalAttribute
                             );
            }

            if (col + cols <= width)
            {
                AddStr (Secret ? Glyphs.Dot.ToString () : text);
            }

            if (!TextModel.SetCol (ref col, width, cols))
            {
                break;
            }

            if (idx + 1 < tcount && col + _text [idx + 1].GetColumns () > width)
            {
                break;
            }
        }

        SetAttribute (normalAttribute);

        // Fill rest of line with spaces
        for (int i = col; i < width; i++)
        {
            AddRune ((Rune)' ');
        }

        PositionCursor ();

        RenderCaption ();

        _isDrawing = false;

        return true;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (App?.Mouse.MouseGrabView is { } && App?.Mouse.MouseGrabView == this)
        {
            App?.Mouse.UngrabMouse ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        // Give autocomplete first opportunity to respond to key presses
        if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (key))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        // Remember the cursor position because the new calculated cursor position is needed
        // to be set BEFORE the TextChanged event is triggered.
        // Needed for the Elmish Wrapper issue https://github.com/DieselMeister/Terminal.Gui.Elmish/issues/2
        _preTextChangedCursorPos = _cursorPosition;

        // Ignore other control characters.
        if (a is { IsKeyCodeAtoZ: false, KeyCode: < KeyCode.Space or > KeyCode.CharMask })
        {
            return false;
        }

        if (ReadOnly)
        {
            return true;
        }

        InsertText (a, true);

        return true;
    }

    /// <summary>Raises the <see cref="TextChanging"/> event, enabling canceling the change or adjusting the text.</summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> if the event was cancelled or the text was adjusted by the event.</returns>
    public bool RaiseTextChanging (ResultEventArgs<string> args)
    {
        // TODO: CWP: Add an OnTextChanging protected virtual method that can be overridden to handle text changing events.

        TextChanging?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>Raised before <see cref="Text"/> changes. The change can be canceled the text adjusted.</summary>
    public event EventHandler<ResultEventArgs<string>>? TextChanging;

    /// <summary>Paste the selected text from the clipboard.</summary>
    public virtual void Paste ()
    {
        if (ReadOnly)
        {
            return;
        }

        string? cbTxt = App?.Clipboard?.GetClipboardData ().Split ("\n") [0];

        if (string.IsNullOrEmpty (cbTxt))
        {
            return;
        }

        SetSelectedStartSelectedLength ();
        int selStart = _start == -1 ? CursorPosition : _start;

        Text = StringExtensions.ToString (_text.GetRange (0, selStart))
               + cbTxt
               + StringExtensions.ToString (
                                            _text.GetRange (
                                                            selStart + SelectedLength,
                                                            _text.Count - (selStart + SelectedLength)
                                                           )
                                           );

        _cursorPosition = Math.Min (selStart + cbTxt.GetRuneCount (), _text.Count);
        ClearAllSelection ();
        SetNeedsDraw ();
        Adjust ();
    }

    /// <summary>
    ///     Converts the logical <see cref="CursorPosition"/> to screen coordinates and positions the terminal cursor.
    /// </summary>
    /// <returns>
    ///     A <see cref="Point"/> representing the cursor's screen position within the viewport, where X is the column
    ///     and Y is always 0 (since TextField is single-line).
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method performs the critical translation between logical text position and physical screen position:
    ///         <list type="number">
    ///             <item>
    ///                 <description>Starts from <see cref="ScrollOffset"/> (first visible character)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Iterates through visible text elements up to <see cref="CursorPosition"/></description>
    ///             </item>
    ///             <item>
    ///                 <description>Accumulates screen column widths (accounting for wide characters)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Calls <see cref="View.Move"/> to position the terminal cursor</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Coordinate spaces:</b>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><see cref="CursorPosition"/>: Logical position (0 to text length)</description>
    ///             </item>
    ///             <item>
    ///                 <description>Returned Point: Screen position within viewport (0 to viewport width)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> For text "Hi世界" with ScrollOffset=0:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>CursorPosition=0 → Screen column 0 (before 'H')</description>
    ///             </item>
    ///             <item>
    ///                 <description>CursorPosition=2 → Screen column 2 (before '世')</description>
    ///             </item>
    ///             <item>
    ///                 <description>CursorPosition=3 → Screen column 4 (before '界', because '世' is 2 columns wide)</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         This method also triggers <see cref="ProcessAutocomplete"/> to update autocomplete suggestions.
    ///     </para>
    /// </remarks>
    /// <seealso cref="CursorPosition"/>
    /// <seealso cref="ScrollOffset"/>
    public override Point? PositionCursor ()
    {
        ProcessAutocomplete ();

        var col = 0;

        for (int idx = ScrollOffset < 0 ? 0 : ScrollOffset; idx < _text.Count; idx++)
        {
            if (idx == _cursorPosition)
            {
                break;
            }

            int cols = Math.Max (_text [idx].GetColumns (), 1);

            TextModel.SetCol (ref col, Viewport.Width - 1, cols);
        }

        int pos = col + Math.Min (Viewport.X, 0);
        Move (pos, 0);

        return new Point (pos, 0);
    }

    /// <summary>Redoes the latest changes.</summary>
    public void Redo ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Redo ();
    }

    /// <summary>Selects all text.</summary>
    public void SelectAll ()
    {
        if (_text.Count == 0)
        {
            return;
        }

        _selectedStart = 0;
        MoveEndExtend ();
        SetNeedsDraw ();
    }

    ///// <summary>
    /////     Changed event, raised when the text has changed.
    /////     <remarks>
    /////         This event is raised when the <see cref="Text"/> changes. The passed <see cref="EventArgs"/> is a
    /////         <see cref="string"/> containing the old value.
    /////     </remarks>
    ///// </summary>
    //public event EventHandler<StateEventArgs<string>> TextChanged;

    /// <summary>Undoes the latest changes.</summary>
    public void Undo ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Undo ();
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the current cursor position is at the end of the <see cref="Text"/>. This
    ///     includes when it is empty.
    /// </summary>
    /// <returns></returns>
    internal bool CursorIsAtEnd () { return CursorPosition == Text.Length; }

    /// <summary>Returns <see langword="true"/> if the current cursor position is at the start of the <see cref="TextField"/>.</summary>
    /// <returns></returns>
    internal bool CursorIsAtStart () { return CursorPosition <= 0; }

    /// <summary>
    ///     Adjusts the <see cref="ScrollOffset"/> to ensure the cursor remains visible within the viewport,
    ///     and triggers a redraw if necessary.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method maintains the invariant that the cursor is always visible by adjusting <see cref="ScrollOffset"/>:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>If <see cref="CursorPosition"/> is to the left of the visible area, scrolls left</description>
    ///             </item>
    ///             <item>
    ///                 <description>If <see cref="CursorPosition"/> is to the right of the visible area, scrolls right</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Called automatically after cursor movement or text changes to keep the cursor in view.
    ///         If scrolling occurred or a redraw is needed, calls <see cref="View.SetNeedsDraw()"/>;
    ///         otherwise, calls <see cref="PositionCursor()"/> to update the terminal cursor position.
    ///     </para>
    /// </remarks>
    private void Adjust ()
    {
        if (SuperView is null)
        {
            return;
        }

        bool need = NeedsDraw || !Used;

        // If cursor is before the visible area, scroll left to show it
        if (_cursorPosition < ScrollOffset)
        {
            ScrollOffset = _cursorPosition;
            need = true;
        }

        // If cursor is beyond the visible area, scroll right to show it
        else if (Viewport.Width > 0
                 && (ScrollOffset + _cursorPosition - Viewport.Width == 0
                     || TextModel.DisplaySize (_text, ScrollOffset, _cursorPosition).size >= Viewport.Width))
        {
            ScrollOffset = Math.Max (
                                     TextModel.CalculateLeftColumn (
                                                                    _text,
                                                                    ScrollOffset,
                                                                    _cursorPosition,
                                                                    Viewport.Width
                                                                   ),
                                     0
                                    );
            need = true;
        }

        if (need)
        {
            SetNeedsDraw ();
        }
        else
        {
            PositionCursor ();
        }
    }

    private void CreateContextMenu ()
    {
        DisposeContextMenu ();

        PopoverMenu menu = new (
                                new List<MenuItem>
                                {
                                    new (this, Command.SelectAll, Strings.ctxSelectAll),
                                    new (this, Command.DeleteAll, Strings.ctxDeleteAll),
                                    new (this, Command.Copy, Strings.ctxCopy),
                                    new (this, Command.Cut, Strings.ctxCut),
                                    new (this, Command.Paste, Strings.ctxPaste),
                                    new (this, Command.Undo, Strings.ctxUndo),
                                    new (this, Command.Redo, Strings.ctxRedo)
                                });

        HotKeyBindings.Remove (menu.Key);
        HotKeyBindings.Add (menu.Key, Command.Context);
        menu.KeyChanged += ContextMenu_KeyChanged;

        ContextMenu = menu;
        App?.Popover?.Register (ContextMenu);
    }

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) { KeyBindings.Replace (e.OldKey.KeyCode, e.NewKey.KeyCode); }

    private List<string> DeleteSelectedText ()
    {
        SetSelectedStartSelectedLength ();
        int selStart = SelectedStart > -1 ? _start : _cursorPosition;

        string newText = StringExtensions.ToString (_text.GetRange (0, selStart))
                         + StringExtensions.ToString (
                                                      _text.GetRange (
                                                                      selStart + SelectedLength,
                                                                      _text.Count - (selStart + SelectedLength)
                                                                     )
                                                     );

        ClearAllSelection ();
        _cursorPosition = selStart >= newText.GetRuneCount () ? newText.GetRuneCount () : selStart;

        return newText.ToStringList ();
    }

    private void GenerateSuggestions ()
    {
        List<Cell> currentLine = Cell.ToCellList (Text);
        int cursorPosition = Math.Min (CursorPosition, currentLine.Count);

        Autocomplete.Context = new (
                                    currentLine,
                                    cursorPosition,
                                    Autocomplete.Context?.Canceled ?? false
                                   );

        Autocomplete.GenerateSuggestions (Autocomplete.Context);
    }

    private TextModel GetModel ()
    {
        TextModel model = new ();
        model.LoadString (Text);

        return model;
    }

    private void HistoryText_ChangeText (object? sender, HistoryTextItemEventArgs? obj)
    {
        if (obj is null)
        {
            return;
        }

        Text = Cell.ToString (obj.Lines [obj.CursorPosition.Y]);
        CursorPosition = obj.CursorPosition.X;
        Adjust ();
    }

    private void InsertText (Key a, bool usePreTextChangedCursorPos)
    {
        _historyText.Add (
                          [Cell.ToCells (_text)],
                          new (_cursorPosition, 0)
                         );

        List<string> newText = _text;

        if (SelectedLength > 0)
        {
            newText = DeleteSelectedText ();
            _preTextChangedCursorPos = _cursorPosition;
        }

        if (!usePreTextChangedCursorPos)
        {
            _preTextChangedCursorPos = _cursorPosition;
        }

        StringRuneEnumerator enumeratedRunes = a.AsRune.ToString ().EnumerateRunes ();

        if (Used)
        {
            _cursorPosition++;

            if (_cursorPosition == newText.Count + 1)
            {
                SetText (newText.Concat (enumeratedRunes.Select (r => r.ToString ())).ToList ());
            }
            else
            {
                if (_preTextChangedCursorPos > newText.Count)
                {
                    _preTextChangedCursorPos = newText.Count;
                }

                SetText (
                         newText.GetRange (0, _preTextChangedCursorPos)
                                .Concat (enumeratedRunes.Select (r => r.ToString ()))
                                .Concat (
                                         newText.GetRange (
                                                           _preTextChangedCursorPos,
                                                           Math.Min (
                                                                     newText.Count - _preTextChangedCursorPos,
                                                                     newText.Count
                                                                    )
                                                          )
                                        )
                        );
            }
        }
        else
        {
            SetText (
                     newText.GetRange (0, _preTextChangedCursorPos)
                            .Concat (enumeratedRunes.Select (r => r.ToString ()))
                            .Concat (
                                     newText.GetRange (
                                                       Math.Min (_preTextChangedCursorPos + 1, newText.Count),
                                                       Math.Max (newText.Count - _preTextChangedCursorPos - 1, 0)
                                                      )
                                    )
                    );
            _cursorPosition++;
        }

        Adjust ();
    }

    private void KillToEnd ()
    {
        if (ReadOnly)
        {
            return;
        }

        ClearAllSelection ();

        if (_cursorPosition >= _text.Count)
        {
            return;
        }

        SetClipboard (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition));
        SetText (_text.GetRange (0, _cursorPosition));
        Adjust ();
    }

    private void KillToStart ()
    {
        if (ReadOnly)
        {
            return;
        }

        ClearAllSelection ();

        if (_cursorPosition == 0)
        {
            return;
        }

        SetClipboard (_text.GetRange (0, _cursorPosition));
        SetText (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition));
        _cursorPosition = 0;
        Adjust ();
    }

    private void MoveEndExtend ()
    {
        if (_cursorPosition <= _text.Count)
        {
            int x = _cursorPosition;
            _cursorPosition = _text.Count;
            PrepareSelection (x, _cursorPosition - x);
        }
    }

    private void MoveHome ()
    {
        ClearAllSelection ();
        _cursorPosition = 0;
        Adjust ();
    }

    private void MoveHomeExtend ()
    {
        if (_cursorPosition > 0)
        {
            int x = _cursorPosition;
            _cursorPosition = 0;
            PrepareSelection (x, _cursorPosition - x);
        }
    }

    /// <summary>
    ///     Moves the cursor +/- the given <paramref name="distance"/>, clearing
    ///     any selection and returning true if any meaningful changes were made.
    /// </summary>
    /// <param name="distance">
    ///     Distance to move the cursor, will be clamped to
    ///     text length. Positive for right, Negative for left.
    /// </param>
    /// <returns></returns>
    private bool Move (int distance)
    {
        int oldCursorPosition = _cursorPosition;
        bool hadSelection = _selectedText != null && _selectedText.Length > 0;

        _cursorPosition = Math.Min (_text.Count, Math.Max (0, _cursorPosition + distance));
        ClearAllSelection ();
        Adjust ();

        return _cursorPosition != oldCursorPosition || hadSelection;
    }

    private bool MoveLeft () { return Move (-1); }

    private void MoveLeftExtend ()
    {
        if (_cursorPosition > 0)
        {
            PrepareSelection (_cursorPosition--, -1);
        }
    }

    private bool MoveRight () { return Move (1); }

    private void MoveRightExtend ()
    {
        if (_cursorPosition < _text.Count)
        {
            PrepareSelection (_cursorPosition++, 1);
        }
    }

    private void MoveWordLeft ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordBackward (_cursorPosition, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return;
        }

        if (newPos.Value.col != -1)
        {
            _cursorPosition = newPos.Value.col;
        }

        Adjust ();
    }

    private void MoveWordLeftExtend ()
    {
        if (_cursorPosition > 0)
        {
            int x = Math.Min (
                              _start > -1 && _start > _cursorPosition ? _start : _cursorPosition,
                              _text.Count
                             );

            if (x > 0)
            {
                (int col, int row)? newPos = GetModel ().WordBackward (x, 0, UseSameRuneTypeForWords);

                if (newPos is null)
                {
                    return;
                }

                if (newPos.Value.col != -1)
                {
                    _cursorPosition = newPos.Value.col;
                }

                PrepareSelection (x, newPos.Value.col - x);
            }
        }
    }

    private void MoveWordRight ()
    {
        ClearAllSelection ();
        (int col, int row)? newPos = GetModel ().WordForward (_cursorPosition, 0, UseSameRuneTypeForWords);

        if (newPos is null)
        {
            return;
        }

        if (newPos.Value.col != -1)
        {
            _cursorPosition = newPos.Value.col;
        }

        Adjust ();
    }

    private void MoveWordRightExtend ()
    {
        if (_cursorPosition < _text.Count)
        {
            int x = _start > -1 && _start > _cursorPosition ? _start : _cursorPosition;
            (int col, int row)? newPos = GetModel ().WordForward (x, 0, UseSameRuneTypeForWords);

            if (newPos is null)
            {
                return;
            }

            if (newPos.Value.col != -1)
            {
                _cursorPosition = newPos.Value.col;
            }

            PrepareSelection (x, newPos.Value.col - x);
        }
    }

    /// <summary>
    ///     Positions the cursor based on a mouse event by converting the mouse's screen X coordinate
    ///     to a logical text position.
    /// </summary>
    /// <param name="mouse">The mouse event containing the screen position.</param>
    /// <returns>The resulting <see cref="CursorPosition"/> after positioning.</returns>
    private int PositionCursor (Mouse mouse) { return PositionCursor (TextModel.GetColFromX (_text, ScrollOffset, mouse.Position!.Value.X), false); }

    /// <summary>
    ///     Positions the cursor based on a screen column or text index.
    /// </summary>
    /// <param name="x">
    ///     Either a screen column (if <paramref name="getX"/> is true) or a text index
    ///     (if <paramref name="getX"/> is false).
    /// </param>
    /// <param name="getX">
    ///     If true, <paramref name="x"/> is treated as a screen column and converted to a text index.
    ///     If false, <paramref name="x"/> is used directly as a text index offset from <see cref="ScrollOffset"/>.
    /// </param>
    /// <returns>The resulting <see cref="CursorPosition"/> after positioning.</returns>
    /// <remarks>
    ///     <para>
    ///         This method handles the conversion from screen coordinates to logical text position:
    ///         <list type="number">
    ///             <item>
    ///                 <description>
    ///                     If <paramref name="getX"/> is true, converts screen column to text index using
    ///                     <see cref="TextModel.GetColFromX(List{string},int,int,int)"/>
    ///                 </description>
    ///             </item>
    ///             <item>
    ///                 <description>Adds <see cref="ScrollOffset"/> to get the absolute text position</description>
    ///             </item>
    ///             <item>
    ///                 <description>Clamps the result to valid bounds [0, text length]</description>
    ///             </item>
    ///         </list>
    ///     </para>
    /// </remarks>
    private int PositionCursor (int x, bool getX = true)
    {
        int pX = x;

        if (getX)
        {
            // Convert screen column to text index (relative to ScrollOffset)
            pX = TextModel.GetColFromX (_text, ScrollOffset, x);
        }

        // Convert relative position to absolute and clamp to valid range
        if (ScrollOffset + pX > _text.Count)
        {
            _cursorPosition = _text.Count;
        }
        else if (ScrollOffset + pX < ScrollOffset)
        {
            _cursorPosition = 0;
        }
        else
        {
            _cursorPosition = ScrollOffset + pX;
        }

        return _cursorPosition;
    }

    /// <summary>
    ///     Updates the text selection state based on the anchor position and selection direction.
    /// </summary>
    /// <param name="x">The anchor position (where selection started) as a text element index.</param>
    /// <param name="direction">
    ///     The direction and distance of selection change. Positive values extend right,
    ///     negative values extend left. A value of 0 indicates position-based selection (e.g., from mouse click).
    /// </param>
    /// <remarks>
    ///     <para>
    ///         This method manages the selection state:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>Sets <see cref="_selectedStart"/> if not already set and position is valid</description>
    ///             </item>
    ///             <item>
    ///                 <description>Calculates <see cref="SelectedLength"/> based on anchor and direction</description>
    ///             </item>
    ///             <item>
    ///                 <description>Extracts <see cref="SelectedText"/> from the text</description>
    ///             </item>
    ///             <item>
    ///                 <description>Adjusts <see cref="ScrollOffset"/> if selection extends beyond visible area</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Called by cursor movement methods (with direction) and mouse handling (with direction=0).
    ///     </para>
    /// </remarks>
    private void PrepareSelection (int x, int direction = 0)
    {
        x = x + ScrollOffset < -1 ? 0 : x;

        _selectedStart = _selectedStart == -1 && _text.Count > 0 && x >= 0 && x <= _text.Count
                             ? x
                             : _selectedStart;

        if (_selectedStart > -1)
        {
            SelectedLength = Math.Abs (
                                       x + direction <= _text.Count
                                           ? x + direction - _selectedStart
                                           : _text.Count - _selectedStart
                                      );
            SetSelectedStartSelectedLength ();

            if (_start > -1 && SelectedLength > 0)
            {
                _selectedText = SelectedLength > 0
                                    ? StringExtensions.ToString (
                                                                 _text.GetRange (
                                                                                 _start < 0 ? 0 : _start,
                                                                                 SelectedLength > _text.Count
                                                                                     ? _text.Count
                                                                                     : SelectedLength
                                                                                )
                                                                )
                                    : "";

                if (ScrollOffset > _start)
                {
                    ScrollOffset = _start;
                }
            }
            else if (_start > -1 && SelectedLength == 0)
            {
                _selectedText = null;
            }

            SetNeedsDraw ();
        }
        else if (SelectedLength > 0 || _selectedText is { })
        {
            ClearAllSelection ();
        }

        Adjust ();
    }

    private void ProcessAutocomplete ()
    {
        if (_isDrawing)
        {
            return;
        }

        if (SelectedLength > 0)
        {
            return;
        }

        GenerateSuggestions ();

        DrawAutocomplete ();
    }

    private void DrawAutocomplete ()
    {
        if (SelectedLength > 0)
        {
            return;
        }

        if (Autocomplete.Context == null)
        {
            return;
        }

        var renderAt = new Point (
                                  Autocomplete.Context.CursorPosition,
                                  0
                                 );

        Autocomplete.RenderOverlay (renderAt);
    }

    private void RenderCaption ()
    {
        if (HasFocus
            || string.IsNullOrEmpty (Title)
            || Text.Length > 0)
        {
            return;
        }

        // Ensure TitleTextFormatter has the current Title text
        // (should already be set by the Title property setter, but being defensive)
        if (TitleTextFormatter.Text != Title)
        {
            TitleTextFormatter.Text = Title;
        }

        var captionAttribute = new Attribute (
                                              GetAttributeForRole (VisualRole.Editable).Foreground.GetDimColor (),
                                              GetAttributeForRole (VisualRole.Editable).Background);

        var hotKeyAttribute = new Attribute (
                                             GetAttributeForRole (VisualRole.Editable).Foreground.GetDimColor (),
                                             GetAttributeForRole (VisualRole.Editable).Background,
                                             GetAttributeForRole (VisualRole.Editable).Style | TextStyle.Underline);

        // Use TitleTextFormatter to render the caption with hotkey support
        TitleTextFormatter.Draw (Driver, ViewportToScreen (new Rectangle (0, 0, Viewport.Width, 1)), captionAttribute, hotKeyAttribute);
    }

    private void SetClipboard (IEnumerable<string> text)
    {
        if (!Secret && App?.Clipboard is { })
        {
            App.Clipboard.SetClipboardData (StringExtensions.ToString (text.ToList ()));
        }
    }

    private void SetOverwrite (bool overwrite)
    {
        Used = overwrite;
        SetNeedsDraw ();
    }

    private void SetSelectedStartSelectedLength ()
    {
        if (SelectedStart > -1 && _cursorPosition < SelectedStart)
        {
            _start = _cursorPosition;
        }
        else
        {
            _start = SelectedStart;
        }
    }

    private void SetText (List<string> newText) { Text = StringExtensions.ToString (newText); }
    private void SetText (IEnumerable<string> newText) { SetText (newText.ToList ()); }

    private void ShowContextMenu (bool keyboard)
    {
        if (!Equals (_currentCulture, Thread.CurrentThread.CurrentUICulture))
        {
            _currentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        if (keyboard)
        {
            ContextMenu?.MakeVisible (ViewportToScreen (new Point (_cursorPosition - ScrollOffset, 1)));
        }
        else
        {
            ContextMenu?.MakeVisible ();
        }
    }

    /// <inheritdoc/>
    protected override void OnSuperViewChanged (ValueChangedEventArgs<View?> args)
    {
        base.OnSuperViewChanged (args);

        if (SuperView is { })
        {
            if (Autocomplete.HostControl is null)
            {
                Autocomplete.HostControl = this;
                Autocomplete.PopupInsideContainer = false;
            }
        }
        else
        {
            Autocomplete.HostControl = null;
        }
    }

    private void TextField_Initialized (object? sender, EventArgs e)
    {
        _cursorPosition = Text.GetRuneCount ();

        if (Viewport.Width > 0)
        {
            ScrollOffset = _cursorPosition > Viewport.Width + 1 ? _cursorPosition - Viewport.Width + 1 : 0;
        }

        if (Autocomplete.HostControl is null)
        {
            Autocomplete.HostControl = this;
            Autocomplete.PopupInsideContainer = false;
        }

        CreateContextMenu ();

        if (ContextMenu?.Key is { })
        {
            KeyBindings.Add (ContextMenu.Key, Command.Context);
        }
    }

    private void DisposeContextMenu ()
    {
        if (ContextMenu is { })
        {
            ContextMenu.Visible = false;
            ContextMenu.KeyChanged -= ContextMenu_KeyChanged;
            ContextMenu.Dispose ();
            ContextMenu = null;
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Text = "This is a test.";
        Title = "Caption";

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            DisposeContextMenu ();
        }

        base.Dispose (disposing);
    }
}

/// <summary>
///     Renders an overlay on another view at a given point that allows selecting from a range of 'autocomplete'
///     options. An implementation on a TextField.
/// </summary>
public class TextFieldAutocomplete : PopupAutocomplete
{
    /// <inheritdoc/>
    protected override void DeleteTextBackwards () { ((TextField)HostControl).DeleteCharLeft (false); }

    /// <inheritdoc/>
    protected override void InsertText (string accepted) { ((TextField)HostControl).InsertText (accepted, false); }

    /// <inheritdoc/>
    protected override void SetCursorPosition (int column) { ((TextField)HostControl).CursorPosition = column; }
}
