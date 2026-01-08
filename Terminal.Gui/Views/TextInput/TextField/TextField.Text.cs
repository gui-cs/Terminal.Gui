using System.Globalization;

namespace Terminal.Gui.Views;

public partial class TextField
{
    private CultureInfo _currentCulture;

    /// <summary>Raised before <see cref="Text"/> changes. The change can be canceled the text adjusted.</summary>
    public event EventHandler<ResultEventArgs<string>>? TextChanging;

    /// <summary>
    ///     Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

    private TextModel GetModel ()
    {
        TextModel model = new ();
        model.LoadString (Text);

        return model;
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

    /// <summary>Raises the <see cref="TextChanging"/> event, enabling canceling the change or adjusting the text.</summary>
    /// <param name="args">The event arguments.</param>
    /// <returns><see langword="true"/> if the event was cancelled or the text was adjusted by the event.</returns>
    public bool RaiseTextChanging (ResultEventArgs<string> args)
    {
        // TODO: CWP: Add an OnTextChanging protected virtual method that can be overridden to handle text changing events.

        TextChanging?.Invoke (this, args);

        return args.Handled;
    }

    private List<string> DeleteSelectedText ()
    {
        SetSelectedStartSelectedLength ();
        int selStart = SelectedStart > -1 ? _selectionStart : InsertionPoint;

        string newText = StringExtensions.ToString (_text.GetRange (0, selStart))
                         + StringExtensions.ToString (
                                                      _text.GetRange (
                                                                      selStart + SelectedLength,
                                                                      _text.Count - (selStart + SelectedLength)
                                                                     )
                                                     );

        ClearAllSelection ();
        InsertionPoint = selStart >= newText.GetRuneCount () ? newText.GetRuneCount () : selStart;

        return newText.ToStringList ();
    }

    private void InsertText (Key a, bool usePreTextChangedCursorPos)
    {
        _historyText.Add (
                          [Cell.ToCells (_text)],
                          new (InsertionPoint, 0)
                         );

        List<string> newText = _text;

        if (SelectedLength > 0)
        {
            newText = DeleteSelectedText ();
            _preChangeInsertionPoint = InsertionPoint;
        }

        if (!usePreTextChangedCursorPos)
        {
            _preChangeInsertionPoint = InsertionPoint;
        }

        StringRuneEnumerator enumeratedRunes = a.AsRune.ToString ().EnumerateRunes ();

        if (Used)
        {
            _insertionPoint++;

            if (InsertionPoint == newText.Count + 1)
            {
                SetText (newText.Concat (enumeratedRunes.Select (r => r.ToString ())).ToList ());
            }
            else
            {
                if (_preChangeInsertionPoint > newText.Count)
                {
                    _preChangeInsertionPoint = newText.Count;
                }

                SetText (
                         newText.GetRange (0, _preChangeInsertionPoint)
                                .Concat (enumeratedRunes.Select (r => r.ToString ()))
                                .Concat (
                                         newText.GetRange (
                                                           _preChangeInsertionPoint,
                                                           Math.Min (
                                                                     newText.Count - _preChangeInsertionPoint,
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
                     newText.GetRange (0, _preChangeInsertionPoint)
                            .Concat (enumeratedRunes.Select (r => r.ToString ()))
                            .Concat (
                                     newText.GetRange (
                                                       Math.Min (_preChangeInsertionPoint + 1, newText.Count),
                                                       Math.Max (newText.Count - _preChangeInsertionPoint - 1, 0)
                                                      )
                                    )
                    );
            InsertionPoint++;
        }

        Adjust ();
    }

    /// <summary>
    ///     Caches the cursor position before a text change operation. Used to properly handle text insertion
    ///     and deletion operations, particularly for undo/redo and proper cursor placement after edits.
    /// </summary>
    private int _preChangeInsertionPoint;

    /// <summary>
    ///     The text content stored as a list of strings, where each string represents a single text element
    ///     (grapheme cluster). This allows proper handling of Unicode combining characters and emoji.
    /// </summary>
    private List<string> _text;

    private void SetText (List<string> newText) { Text = StringExtensions.ToString (newText); }
    private void SetText (IEnumerable<string> newText) { SetText (newText.ToList ()); }

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
                if (InsertionPoint > _text.Count)
                {
                    InsertionPoint = _text.Count;
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
                                  new (InsertionPoint, 0)
                                 );

                _historyText.Add (
                                  [Cell.ToCells (_text)],
                                  new (InsertionPoint, 0),
                                  TextEditingLineStatus.Replaced
                                 );
            }

            OnTextChanged ();

            ProcessAutocomplete ();

            if (InsertionPoint > _text.Count)
            {
                InsertionPoint = Math.Max (TextModel.DisplaySize (_text, 0).size - 1, 0);
            }

            Adjust ();
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Returns <see langword="true"/> if the current cursor position is at the end of the <see cref="Text"/>. This
    ///     includes when it is empty.
    /// </summary>
    /// <returns></returns>
    internal bool InsertionPointIsAtEnd () => InsertionPoint == Text.Length;

    /// <summary>Returns <see langword="true"/> if the current cursor position is at the start of the <see cref="TextField"/>.</summary>
    /// <returns></returns>
    internal bool InsertionPointIsAtStart () => InsertionPoint <= 0;

    /// <summary>
    ///     Adjusts the <see cref="ScrollOffset"/> to ensure the cursor remains visible within the viewport,
    ///     and triggers a redraw if necessary.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This method maintains the invariant that the cursor is always visible by adjusting <see cref="ScrollOffset"/>:
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>If <see cref="InsertionPoint"/> is to the left of the visible area, scrolls left</description>
    ///             </item>
    ///             <item>
    ///                 <description>If <see cref="InsertionPoint"/> is to the right of the visible area, scrolls right</description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         Called automatically after cursor movement or text changes to keep the cursor in view.
    ///         If scrolling occurred or a redraw is needed, calls <see cref="View.SetNeedsDraw()"/>;
    ///         otherwise, calls <see cref="UpdateCursor"/> to update the terminal cursor position.
    ///     </para>
    /// </remarks>
    private void Adjust ()
    {
        bool need = NeedsDraw || !Used;

        // If cursor is before the visible area, scroll left to show it
        if (InsertionPoint < ScrollOffset)
        {
            ScrollOffset = InsertionPoint;
            need = true;
        }

        // If cursor is beyond the visible area, scroll right to show it
        else if (Viewport.Width > 0
                 && (ScrollOffset + InsertionPoint - Viewport.Width == 0
                     || TextModel.DisplaySize (_text, ScrollOffset, InsertionPoint).size >= Viewport.Width))
        {
            ScrollOffset = Math.Max (
                                     TextModel.CalculateLeftColumn (
                                                                    _text,
                                                                    ScrollOffset,
                                                                    InsertionPoint,
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
        UpdateCursor ();
    }

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
    /// <returns>The resulting <see cref="InsertionPoint"/> after positioning.</returns>
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
    private int SetInsertionPointFromScreen (int x, bool getX = true)
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
            InsertionPoint = _text.Count;
        }
        else if (ScrollOffset + pX < ScrollOffset)
        {
            InsertionPoint = 0;
        }
        else
        {
            InsertionPoint = ScrollOffset + pX;
        }

        return InsertionPoint;
    }

    private int _insertionPoint;

    /// <summary>
    ///     Gets or sets the insertion point within the text, measured as a 0-based index into text elements.
    /// </summary>
    /// <value>
    ///     The insertion point position, clamped to the range [0, Text.Length]. Position 0 is before the first character;
    ///     position equal to the text length is after the last character.
    /// </value>
    /// <remarks>
    ///     <para>
    ///         This property provides access to the logical insertion point within the text. The value is automatically
    ///         clamped to valid bounds: values less than 0 become 0, and values greater than the text length become
    ///         the text length.
    ///     </para>
    ///     <para>
    ///         <b>Relationship to <see cref="View.Cursor"/>:</b>
    ///         <list type="bullet">
    ///             <item>
    ///                 <description><see cref="InsertionPoint"/>: Logical position in text elements (0-based index)</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     <see cref="UpdateCursor"/>: Converts logical position to screen coordinates, accounting for
    ///                     <see cref="ScrollOffset"/> and wide characters
    ///                 </description>
    ///             </item>
    ///         </list>
    ///     </para>
    ///     <para>
    ///         <b>Example:</b> For text "Hello世界" (Hello + 2 CJK characters):
    ///         <list type="bullet">
    ///             <item>
    ///                 <description>InsertionPoint = 0: Before 'H'</description>
    ///             </item>
    ///             <item>
    ///                 <description>InsertionPoint = 5: Before '世'</description>
    ///             </item>
    ///             <item>
    ///                 <description>InsertionPoint = 7: After '界' (end of text)</description>
    ///             </item>
    ///         </list>
    ///         Note that screen columns would differ because '世' and '界' each occupy 2 columns.
    ///     </para>
    ///     <para>
    ///         Setting this property also updates the text selection via <see cref="PrepareSelection"/>.
    ///     </para>
    /// </remarks>
    /// <seealso cref="ScrollOffset"/>
    public virtual int InsertionPoint
    {
        get => _insertionPoint;
        set
        {
            int previousInsertionPoint = _insertionPoint;
            if (value < 0)
            {
                _insertionPoint = 0;
            }
            else if (value > _text.Count)
            {
                _insertionPoint = _text.Count;
            }
            else
            {
                _insertionPoint = value;
            }

            PrepareSelection (_selectionAnchor, _insertionPoint - _selectionAnchor);

            if (_insertionPoint != previousInsertionPoint)
            {
                UpdateCursor ();
            }
        }
    }

    /// <summary>Updates the cursor position.</summary>
    /// <remarks>
    ///     This method calculates the cursor position and calls <see cref="View.SetCursor"/>.
    /// </remarks>
    private void UpdateCursor ()
    {
        ProcessAutocomplete ();

        if (!HasFocus)
        {
            Cursor = new () { Position = null };

            return;
        }

        var col = 0;

        for (int idx = ScrollOffset < 0 ? 0 : ScrollOffset; idx < _text.Count; idx++)
        {
            if (idx == InsertionPoint)
            {
                break;
            }

            int cols = Math.Max (_text [idx].GetColumns (), 1);

            TextModel.SetCol (ref col, Viewport.Width - 1, cols);
        }

        int pos = col + Math.Min (Viewport.X, 0);

        Cursor = new ()
        {
            Position = ViewportToScreen (new Point (pos, 0)),
            Style = CursorStyle.Default
        };
    }

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
    ///                 <description><see cref="InsertionPoint"/>: Absolute position in the text (0 to text length)</description>
    ///             </item>
    ///             <item>
    ///                 <description><see cref="ScrollOffset"/>: Index of first visible character</description>
    ///             </item>
    ///             <item>
    ///                 <description>
    ///                     Screen column = <see cref="InsertionPoint"/> - <see cref="ScrollOffset"/> (approximately,
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
    /// <seealso cref="InsertionPoint"/>
    public int ScrollOffset { get; private set; }
}
