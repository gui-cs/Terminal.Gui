using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;

namespace Terminal.Gui.Views;

/// <summary>
///     A scrollable map of the Unicode codepoints.
/// </summary>
/// <remarks>
///     See <see href="../docs/CharacterMap.md"/> for details.
/// </remarks>
public class CharMap : View, IDesignable
{
    /// <summary>
    ///     Gets or sets the default cursor style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static CursorStyle DefaultCursorStyle { get; set; } = CursorStyle.BlinkingBlock;

    private const int COLUMN_WIDTH = 3; // Width of each column of glyphs
    private const int HEADER_HEIGHT = 1; // Height of the header

    // ReSharper disable once InconsistentNaming
    private static readonly int MAX_CODE_POINT = UnicodeRange.Ranges.Max (r => r.End);

    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public CharMap ()
    {
        CanFocus = true;

        AddCommand (Command.Up, commandContext => Move (commandContext, -16));
        AddCommand (Command.Down, commandContext => Move (commandContext, 16));
        AddCommand (Command.Left, commandContext => Move (commandContext, -1));
        AddCommand (Command.Right, commandContext => Move (commandContext, 1));

        AddCommand (Command.PageUp, commandContext => Move (commandContext, -(Viewport.Height - HEADER_HEIGHT / _rowHeight) * 16));
        AddCommand (Command.PageDown, commandContext => Move (commandContext, (Viewport.Height - HEADER_HEIGHT / _rowHeight) * 16));
        AddCommand (Command.Start, commandContext => Move (commandContext, -SelectedCodePoint));
        AddCommand (Command.End, commandContext => Move (commandContext, MAX_CODE_POINT - SelectedCodePoint));

        AddCommand (Command.ScrollDown, () => ScrollVertical (1));
        AddCommand (Command.ScrollUp, () => ScrollVertical (-1));
        AddCommand (Command.ScrollRight, () => ScrollHorizontal (1));
        AddCommand (Command.ScrollLeft, () => ScrollHorizontal (-1));

        AddCommand (Command.Accept, HandleAcceptCommand);
        AddCommand (Command.Activate, HandleSelectCommand);
        AddCommand (Command.Context, HandleContextCommand);

        KeyBindings.Add (Key.CursorUp, Command.Up);
        KeyBindings.Add (Key.CursorDown, Command.Down);
        KeyBindings.Add (Key.CursorLeft, Command.Left);
        KeyBindings.Add (Key.CursorRight, Command.Right);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);
        KeyBindings.Add (PopoverMenu.DefaultKey, Command.Context);

        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked, Command.Activate);
        MouseBindings.Add (MouseFlags.LeftButtonDoubleClicked, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.RightButtonClicked, Command.Context);
        MouseBindings.ReplaceCommands (MouseFlags.LeftButtonClicked | MouseFlags.Ctrl, Command.Context);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledLeft, Command.ScrollLeft);
        MouseBindings.Add (MouseFlags.WheeledRight, Command.ScrollRight);

        // Initial content size; height will be corrected by RebuildVisibleRows()
        SetContentSize (new (COLUMN_WIDTH * 16 + RowLabelWidth, HEADER_HEIGHT + _rowHeight));

        // Set up the horizontal scrollbar. Turn off AutoShow since we do it manually.
        HorizontalScrollBar.AutoShow = false;
        HorizontalScrollBar.Increment = COLUMN_WIDTH;

        // This prevents scrolling past the last column
        HorizontalScrollBar.ScrollableContentSize = GetContentSize ().Width - RowLabelWidth;
        HorizontalScrollBar.X = RowLabelWidth;
        HorizontalScrollBar.Y = Pos.AnchorEnd ();
        HorizontalScrollBar.Width = Dim.Fill (1);

        // We want the horizontal scrollbar to only show when needed.
        // We can't use ScrollBar.AutoShow because we are using custom ContentSize
        // So, we do it manually on ViewportChanged events.
        ViewportChanged += (_, _) =>
                           {
                               HorizontalScrollBar.Visible = Viewport.Width < GetContentSize ().Width;
                               UpdateCursor ();
                           };

        // Set up the vertical scrollbar. Turn off AutoShow since it's always visible.
        VerticalScrollBar.AutoShow = true;
        VerticalScrollBar.Visible = false;
        VerticalScrollBar.X = Pos.AnchorEnd ();
        VerticalScrollBar.Y = HEADER_HEIGHT; // Header

        // The scrollbars are in the Padding. VisualRole.Focus/Active are used to draw the
        // CharMap headers. Override Padding to force it to draw to match.
        Padding!.GettingAttributeForRole += PaddingOnGettingAttributeForRole;

        // Build initial visible rows (all rows with at least one valid codepoint)
        RebuildVisibleRows ();

        Cursor = new () { Style = DefaultCursorStyle };
    }

    // Visible rows management: each entry is the starting code point of a 16-wide row
    private readonly List<int> _visibleRowStarts = new ();
    private readonly Dictionary<int, int> _rowStartToVisibleIndex = new ();

    private void RebuildVisibleRows ()
    {
        _visibleRowStarts.Clear ();
        _rowStartToVisibleIndex.Clear ();

        int maxRow = MAX_CODE_POINT / 16;

        for (var row = 0; row <= maxRow; row++)
        {
            int start = row * 16;
            bool anyValid = false;
            bool anyVisible = false;

            for (var col = 0; col < 16; col++)
            {
                int cp = start + col;
                if (cp > RuneExtensions.MaxUnicodeCodePoint)
                {
                    break;
                }

                if (!Rune.IsValid (cp))
                {
                    continue;
                }

                anyValid = true;

                if (!ShowUnicodeCategory.HasValue)
                {
                    // With no filter, a row is displayed if it has any valid codepoint
                    anyVisible = true;
                    break;
                }

                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory (cp);
                if (cat == ShowUnicodeCategory.Value)
                {
                    anyVisible = true;
                    break;
                }
            }

            if (anyValid && (!ShowUnicodeCategory.HasValue ? anyValid : anyVisible))
            {
                _rowStartToVisibleIndex [start] = _visibleRowStarts.Count;
                _visibleRowStarts.Add (start);
            }
        }

        // Update content size to match visible rows
        SetContentSize (new (COLUMN_WIDTH * 16 + RowLabelWidth, _visibleRowStarts.Count * _rowHeight + HEADER_HEIGHT));

        // Keep vertical scrollbar aligned with new content size
        VerticalScrollBar.ScrollableContentSize = GetContentSize ().Height;
    }

    private int VisibleRowIndexForCodePoint (int codePoint)
    {
        int start = codePoint / 16 * 16;
        return _rowStartToVisibleIndex.GetValueOrDefault (start, -1);
    }

    private int _rowHeight = 1; // Height of each row of 16 glyphs - changing this is not tested
    private int _selectedCodepoint; // Currently selected codepoint
    private int _startCodepoint; // The codepoint that will be displayed at the top of the Viewport

    /// <summary>
    ///     Gets or sets the currently selected codepoint. Causes the Viewport to scroll to make the selected code point
    ///     visible.
    /// </summary>
    public int SelectedCodePoint
    {
        get => _selectedCodepoint;
        set
        {
            if (_selectedCodepoint == value)
            {
                return;
            }

            int newSelectedCodePoint = Math.Clamp (value, 0, MAX_CODE_POINT);

            Point offsetToNewCursor = GetCursor (newSelectedCodePoint);

            _selectedCodepoint = newSelectedCodePoint;

            // Ensure the new cursor position is visible
            ScrollToMakeCursorVisible (offsetToNewCursor);

            SetNeedsDraw ();
            UpdateCursor ();
            SelectedCodePointChanged?.Invoke (this, new (SelectedCodePoint));
        }
    }

    /// <summary>
    ///     Raised when the selected code point changes.
    /// </summary>
    public event EventHandler<EventArgs<int>>? SelectedCodePointChanged;

    /// <summary>
    ///     Gets or sets whether the number of columns each glyph is displayed.
    /// </summary>
    public bool ShowGlyphWidths
    {
        get => _rowHeight == 2;
        set
        {
            _rowHeight = value ? 2 : 1;
            // height changed => content height depends on row height
            RebuildVisibleRows ();
            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Specifies the starting offset for the character map. The default is 0x2500 which is the Box Drawing
    ///     characters.
    /// </summary>
    public int StartCodePoint
    {
        get => _startCodepoint;
        set
        {
            _startCodepoint = value;
            SelectedCodePoint = value;
        }
    }


    private UnicodeCategory? _showUnicodeCategory;

    /// <summary>
    ///     When set, only glyphs whose UnicodeCategory matches the value are rendered. If <see langword="null"/> (default),
    ///     all glyphs are rendered.
    /// </summary>
    public UnicodeCategory? ShowUnicodeCategory
    {
        get => _showUnicodeCategory;
        set
        {
            if (_showUnicodeCategory == value)
            {
                return;
            }

            _showUnicodeCategory = value;
            RebuildVisibleRows ();

            // Ensure selection is on a visible row
            int desiredRowStart = SelectedCodePoint / 16 * 16;
            if (!_rowStartToVisibleIndex.ContainsKey (desiredRowStart))
            {
                // Find nearest visible row (prefer next; fallback to last)
                int idx = _visibleRowStarts.FindIndex (s => s >= desiredRowStart);
                if (idx < 0 && _visibleRowStarts.Count > 0)
                {
                    idx = _visibleRowStarts.Count - 1;
                }
                if (idx >= 0)
                {
                    SelectedCodePoint = _visibleRowStarts [idx];
                }
            }

            SetNeedsDraw ();
        }
    }

    private void CopyCodePoint () { App?.Clipboard?.SetClipboardData ($"U+{SelectedCodePoint:x5}"); }
    private void CopyGlyph () { App?.Clipboard?.SetClipboardData ($"{new Rune (SelectedCodePoint)}"); }

    private bool? Move (ICommandContext? commandContext, int cpOffset)
    {
        if (RaiseActivating (commandContext) is true)
        {
            return true;
        }

        SelectedCodePoint += cpOffset;

        return true;
    }

    private void PaddingOnGettingAttributeForRole (object? sender, VisualRoleEventArgs e)
    {
        if (e.Role != VisualRole.Focus && e.Role != VisualRole.Active)
        {
            e.Result = GetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
        }

        e.Handled = true;
    }

    private void ScrollToMakeCursorVisible (Point offsetToNewCursor)
    {
        // Adjust vertical scrolling
        if (offsetToNewCursor.Y < 1) // Header is at Y = 0
        {
            ScrollVertical (offsetToNewCursor.Y - HEADER_HEIGHT);
        }
        else if (offsetToNewCursor.Y >= Viewport.Height)
        {
            ScrollVertical (offsetToNewCursor.Y - Viewport.Height + HEADER_HEIGHT);
        }

        // Adjust horizontal scrolling
        if (offsetToNewCursor.X < RowLabelWidth + 1)
        {
            ScrollHorizontal (offsetToNewCursor.X - (RowLabelWidth + 1));
        }
        else if (offsetToNewCursor.X >= Viewport.Width)
        {
            ScrollHorizontal (offsetToNewCursor.X - Viewport.Width + 1);
        }
    }

    #region Details Dialog

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    private void ShowDetails ()
    {
        if (App is not { Initialized: true })
        {
            // Some unit tests invoke Accept without Init
            return;
        }

        UcdApiClient client = new ();
        var decResponse = string.Empty;
        var getCodePointError = string.Empty;

        Dialog? waitIndicator = new ()
        {
            Title = Strings.charMapCPInfoDlgTitle,
            Buttons = [new () { Text = Strings.btnCancel }]
        };

        var errorLabel = new Label
        {
            Text = UcdApiClient.BaseUrl,
            X = 0,
            Y = 0,
            TextAlignment = Alignment.Center
        };

        var spinner = new SpinnerView
        {
            X = Pos.Center (),
            Y = Pos.Bottom (errorLabel),
            Style = new SpinnerStyle.Aesthetic ()
        };
        spinner.AutoSpin = true;
        waitIndicator.Add (errorLabel);
        waitIndicator.Add (spinner);

        waitIndicator.IsModalChanged += async (s, a) =>
                               {
                                   if (!a.Value)
                                   {
                                       return;
                                   }

                                   try
                                   {
                                       decResponse = await client.GetCodepointDec (SelectedCodePoint).ConfigureAwait (false);
                                       App?.Invoke ((_) => (s as Dialog)?.RequestStop ());
                                   }
                                   catch (HttpRequestException e)
                                   {
                                       getCodePointError = errorLabel.Text = e.Message;
                                       App?.Invoke ((_) => (s as Dialog)?.RequestStop ());
                                   }
                               };
        App?.Run (waitIndicator);
        waitIndicator.Dispose ();

        var name = string.Empty;

        if (!string.IsNullOrEmpty (decResponse))
        {
            using JsonDocument document = JsonDocument.Parse (decResponse);

            JsonElement root = document.RootElement;

            // Get a property by name and output its value
            if (root.TryGetProperty ("name", out JsonElement nameElement))
            {
                name = nameElement.GetString ();
            }

            decResponse = JsonSerializer.Serialize (
                                                    document.RootElement,
                                                    new
                                                        JsonSerializerOptions
                                                    { WriteIndented = true }
                                                   );
        }
        else
        {
            decResponse = getCodePointError;
        }

        var title = $"{ToCamelCase (name!)} - {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}";

        Button copyGlyph = new () { Text = Strings.charMapCopyGlyph };
        Button copyCodepoint = new () { Text = Strings.charMapCopyCP };
        Button cancel = new () { Text = Strings.btnCancel };

        using Dialog dlg = new ();
        dlg.Buttons = [copyGlyph, copyCodepoint, cancel];
        dlg.Title = title;

        var rune = (Rune)SelectedCodePoint;
        var label = new Label { Text = "IsAscii: ", X = 0, Y = 0 };
        dlg.Add (label);

        label = new () { Text = $"{rune.IsAscii}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = ", Bmp: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = $"{rune.IsBmp}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = ", CombiningMark: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = $"{rune.IsCombiningMark ()}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = ", SurrogatePair: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = $"{rune.IsSurrogatePair ()}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = ", Plane: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = $"{rune.Plane}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = "Columns: ", X = 0, Y = Pos.Bottom (label) };
        dlg.Add (label);

        label = new () { Text = $"{rune.GetColumns ()}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = ", Utf16SequenceLength: ", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = $"{rune.Utf16SequenceLength}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new () { Text = "Category: ", X = 0, Y = Pos.Bottom (label) };
        dlg.Add (label);
        Span<char> utf16 = stackalloc char [2];
        int charCount = rune.EncodeToUtf16 (utf16);

        // Get the bidi class for the first code unit
        // For most bidi characters, the first code unit is sufficient
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory (utf16 [0]);

        label = new () { Text = $"{category}", X = Pos.Right (label), Y = Pos.Top (label) };
        dlg.Add (label);

        label = new ()
        {
            Text =
                $"{Strings.charMapInfoDlgInfoLabel} {UcdApiClient.BaseUrl}codepoint/dec/{SelectedCodePoint}:",
            X = 0,
            Y = Pos.Bottom (label)
        };
        dlg.Add (label);

        var json = new TextView
        {
            X = 0,
            Y = Pos.Bottom (label),
            Width = Dim.Fill (0, minimumContentDim: 60),
            Height = Dim.Fill (0, minimumContentDim: 5),
            ReadOnly = true,
            WordWrap = true,
            Text = decResponse
        };

        dlg.Add (json);

        int? result = App?.Run (dlg) as int?;
        switch (result!)
        {
            case 0:
                CopyGlyph ();

                break;

            case 1:
                CopyCodePoint ();

                break;
        }
    }

    #endregion Details Dialog

    #region Cursor

    private Point GetCursor (int codePoint)
    {
        // + 1 for padding between label and first column
        int x = codePoint % 16 * COLUMN_WIDTH + RowLabelWidth + 1 - Viewport.X;

        int visibleRowIndex = VisibleRowIndexForCodePoint (codePoint);
        if (visibleRowIndex < 0)
        {
            // If filtered out, stick to current Y to avoid jumping; caller will clamp
            int fallbackY = HEADER_HEIGHT - Viewport.Y;
            return new (x, fallbackY);
        }

        int y = visibleRowIndex * _rowHeight + HEADER_HEIGHT - Viewport.Y;

        return new (x, y);
    }

    /// <summary>Updates the cursor position based on the selected code point.</summary>
    /// <remarks>
    ///     This method calculates the cursor position and calls <see cref="View.SetCursor"/>.
    ///     The framework automatically handles hiding the cursor when the view loses focus.
    /// </remarks>
    private void UpdateCursor ()
    {
        Point cursor = GetCursor (SelectedCodePoint);

        if (cursor.X >= RowLabelWidth
            && cursor.X < Viewport.Width
            && cursor.Y > 0
            && cursor.Y < Viewport.Height)
        {
            // Convert to Screen coordinates
            Cursor = Cursor with { Position = ViewportToScreen (cursor) };
        }
        else
        {
            // Cursor is scrolled out of view
            Cursor = Cursor with { Position = null };
        }
    }

    #endregion Cursor

    #region Drawing

    private static int RowLabelWidth => $"U+{MAX_CODE_POINT:x5}".Length + 1;

    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Viewport.Height == 0 || Viewport.Width == 0)
        {
            return true;
        }

        int selectedCol = SelectedCodePoint % 16;
        int selectedRowIndex = VisibleRowIndexForCodePoint (SelectedCodePoint);

        // Headers

        // Clear the header area
        Move (0, 0);
        SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
        AddStr (new (' ', Viewport.Width));

        int firstColumnX = RowLabelWidth - Viewport.X;

        // Header
        var x = 0;

        for (var hexDigit = 0; hexDigit < 16; hexDigit++)
        {
            x = firstColumnX + hexDigit * COLUMN_WIDTH;

            if (x > RowLabelWidth - 2)
            {
                Move (x, 0);
                SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
                AddStr (" ");

                // Swap Active/Focus so the selected column is highlighted
                if (hexDigit == selectedCol)

                {
                    SetAttributeForRole (HasFocus ? VisualRole.Active : VisualRole.Focus);
                }

                AddStr ($"{hexDigit:x}");
                SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
                AddStr (" ");
            }
        }

        // Start at 1 because Header.
        for (var y = 1; y < Viewport.Height; y++)
        {
            // Which visible row is this?
            int visibleRow = (y + Viewport.Y - 1) / _rowHeight;

            if (visibleRow < 0 || visibleRow >= _visibleRowStarts.Count)
            {
                // No row at this y; clear label area and continue
                Move (0, y);
                AddStr (new (' ', Viewport.Width));

                continue;
            }

            int rowStart = _visibleRowStarts [visibleRow];

            // Draw the row label (U+XXXX_)
            SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
            Move (0, y);

            // Swap Active/Focus so the selected row is highlighted
            if (visibleRow == selectedRowIndex)
            {
                SetAttributeForRole (HasFocus ? VisualRole.Active : VisualRole.Focus);
            }

            if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
            {
                AddStr ($"U+{rowStart / 16:x5}_");
            }
            else
            {
                AddStr (new (' ', RowLabelWidth));
            }

            // Draw the row
            SetAttributeForRole (VisualRole.Normal);

            for (var col = 0; col < 16; col++)
            {
                x = firstColumnX + COLUMN_WIDTH * col + 1;

                if (x < RowLabelWidth || x > Viewport.Width - 1)
                {
                    continue;
                }

                Move (x, y);

                // If we're at the cursor position highlight the cell
                if (visibleRow == selectedRowIndex && col == selectedCol)
                {
                    SetAttributeForRole (VisualRole.Active);
                }

                int scalar = rowStart + col;

                // Don't render out-of-range scalars
                if (scalar > MAX_CODE_POINT)
                {
                    AddStr (" ");
                    if (visibleRow == selectedRowIndex && col == selectedCol)
                    {
                        SetAttributeForRole (VisualRole.Normal);
                    }
                    continue;
                }

                string grapheme = "?";

                if (Rune.IsValid (scalar))
                {
                    grapheme = new Rune (scalar).ToString ();
                }

                int width = grapheme.GetColumns ();

                // Compute visibility based on ShowUnicodeCategory
                bool isVisible = Rune.IsValid (scalar);
                if (isVisible && ShowUnicodeCategory.HasValue)
                {
                    UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory (scalar);
                    isVisible = cat == ShowUnicodeCategory.Value;
                }

                if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
                {
                    // Glyph row
                    if (isVisible)
                    {
                        RenderGrapheme (grapheme, width, scalar);
                    }
                    else
                    {
                        AddStr (" ");
                    }
                }
                else
                {
                    // Width row (ShowGlyphWidths)
                    if (isVisible)
                    {
                        // Draw the width of the rune faint
                        Attribute attr = GetAttributeForRole (VisualRole.Normal);
                        SetAttribute (attr with { Style = attr.Style | TextStyle.Faint });
                        AddStr ($"{width}");
                    }
                    else
                    {
                        AddStr (" ");
                    }
                }

                // If we're at the cursor position, and we don't have focus
                if (visibleRow == selectedRowIndex && col == selectedCol)
                {
                    SetAttributeForRole (VisualRole.Normal);
                }
            }
        }

        return true;

        void RenderGrapheme (string grapheme, int width, int scalar)
        {
            // Get the UnicodeCategory
            // Get the bidi class for the first code unit
            // For most bidi characters, the first code unit is sufficient
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory (scalar);

            switch (category)
            {
                case UnicodeCategory.OtherNotAssigned:
                    SetAttributeForRole (VisualRole.Highlight);
                    AddStr (Rune.ReplacementChar.ToString ());
                    SetAttributeForRole (VisualRole.Normal);

                    break;

                // Format character that affects the layout of text or the operation of text processes, but is not normally rendered. 
                // These report width of 0 and don't render on their own.
                case UnicodeCategory.Format:
                    SetAttributeForRole (VisualRole.Highlight);
                    AddStr ("F");
                    SetAttributeForRole (VisualRole.Normal);

                    break;

                // Nonspacing character that indicates modifications of a base character.
                case UnicodeCategory.NonSpacingMark:
                // Spacing character that indicates modifications of a base character and affects the width of the glyph for that base character. 
                case UnicodeCategory.SpacingCombiningMark:
                // Enclosing mark character, which is a nonspacing combining character that surrounds all previous characters up to and including a base character.
                case UnicodeCategory.EnclosingMark:
                    if (width > 0)
                    {
                        AddStr (grapheme);
                    }

                    break;

                // These report width of 0, but render as 1
                case UnicodeCategory.Control:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.Surrogate:
                    AddStr (grapheme);

                    break;
                case UnicodeCategory.OtherLetter:
                    AddStr (grapheme);

                    if (width == 0)
                    {
                        AddStr (" ");
                    }

                    break;
                default:

                    // Draw the rune
                    if (width > 0)
                    {
                        AddStr (grapheme);
                    }
                    else
                    {
                        throw new InvalidOperationException ($"The Rune \"{grapheme}\" (U+{Rune.GetRuneAt (grapheme, 0).Value:x6}) has zero width and no special-case UnicodeCategory logic applies.");
                    }

                    break;
            }
        }
    }

    /// <summary>
    ///     Helper to convert a string into camel case.
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToCamelCase (string str)
    {
        if (string.IsNullOrEmpty (str))
        {
            return str;
        }

        TextInfo textInfo = new CultureInfo ("en-US", false).TextInfo;

        str = textInfo.ToLower (str);
        str = textInfo.ToTitleCase (str);

        return str;
    }

    #endregion Drawing

    #region Mouse Handling

    private bool? HandleSelectCommand (ICommandContext? commandContext)
    {
        Point position = GetCursor (SelectedCodePoint);

        if (commandContext is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } } mouseCommandContext)
        {
            // If the mouse is clicked on the headers, map it to the first glyph of the row/col
            position = mouseCommandContext.Binding.MouseEventArgs.Position!.Value;

            if (position.Y == 0)
            {
                position = position with { Y = GetCursor (SelectedCodePoint).Y };
            }

            if (position.X < RowLabelWidth || position.X > RowLabelWidth + 16 * COLUMN_WIDTH - 1)
            {
                position = position with { X = GetCursor (SelectedCodePoint).X };
            }
        }

        if (RaiseActivating (commandContext) is true)
        {
            return true;
        }

        if (!TryGetCodePointFromPosition (position, out int cp))
        {
            return false;
        }

        if (cp != SelectedCodePoint)
        {
            if (!HasFocus && CanFocus)
            {
                SetFocus ();
            }

            SelectedCodePoint = cp;
        }

        return true;
    }

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    private bool? HandleAcceptCommand (ICommandContext? commandContext)
    {
        if (RaiseAccepting (commandContext) is true)
        {
            return true;
        }

        if (commandContext is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } } mouseCommandContext)
        {
            if (!HasFocus && CanFocus)
            {
                SetFocus ();
            }

            if (!TryGetCodePointFromPosition (mouseCommandContext.Binding.MouseEventArgs.Position!.Value, out int cp))
            {
                return false;
            }

            SelectedCodePoint = cp;
        }

        ShowDetails ();

        return true;
    }

    private bool? HandleContextCommand (ICommandContext? commandContext)
    {
        int newCodePoint = SelectedCodePoint;

        if (commandContext is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } } mouseCommandContext)
        {
            if (!TryGetCodePointFromPosition (mouseCommandContext.Binding.MouseEventArgs.Position!.Value, out newCodePoint))
            {
                return false;
            }
        }

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        SelectedCodePoint = newCodePoint;

        // This demonstrates how to create an ephemeral Popover; one that exists
        // ony as long as the popover is visible.
        // Note, for ephemeral Popovers, hotkeys are not supported.
        PopoverMenu? contextMenu = new (
                                        [
                                            new (Strings.charMapCopyGlyph, string.Empty, CopyGlyph),
                                            new (Strings.charMapCopyCP, string.Empty, CopyCodePoint)
                                        ]);

        // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
        // and the context menu is disposed when it is closed.
        App!.Popover?.Register (contextMenu);

        contextMenu?.MakeVisible (ViewportToScreen (GetCursor (SelectedCodePoint)));

        return true;
    }

    private bool TryGetCodePointFromPosition (Point position, out int codePoint)
    {
        if (position.X < RowLabelWidth || position.Y < 1)
        {
            codePoint = 0;

            return false;
        }

        int visibleRow = (position.Y - 1 - -Viewport.Y) / _rowHeight;

        if (visibleRow < 0 || visibleRow >= _visibleRowStarts.Count)
        {
            codePoint = 0;
            return false;
        }

        int col = (position.X - RowLabelWidth - -Viewport.X) / COLUMN_WIDTH;

        if (col > 15)
        {
            col = 15;
        }

        codePoint = _visibleRowStarts [visibleRow] + col;

        if (codePoint > MAX_CODE_POINT)
        {
            return false;
        }

        return true;
    }

    #endregion Mouse Handling
}
