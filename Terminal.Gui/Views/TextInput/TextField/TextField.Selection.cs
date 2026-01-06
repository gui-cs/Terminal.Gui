namespace Terminal.Gui.Views;

public partial class TextField
{
    private string? _selectedText;

    /// <summary>Gets The selected text.</summary>
    public string? SelectedText => Secret ? null : _selectedText;

    /// <summary>
    ///     Gets or sets whether the word navigation should select only the word itself without spaces around it or with the
    ///     spaces at right.
    ///     Default is <c>false</c> meaning that the spaces at right are included in the selection.
    /// </summary>
    public bool SelectWordOnlyOnDoubleClick { get; set; }

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

    private int _selectedStart;

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
}
