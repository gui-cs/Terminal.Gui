#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

/// <summary>
///     A scrollable map of the Unicode codepoints.
/// </summary>
/// <remarks>
///     See <see href="../docs/CharacterMap.md"/> for details.
/// </remarks>
public class CharMap : View, IDesignable
{
    private const int COLUMN_WIDTH = 3; // Width of each column of glyphs
    private const int HEADER_HEIGHT = 1; // Height of the header
    private int _rowHeight = 1; // Height of each row of 16 glyphs - changing this is not tested

    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    public CharMap ()
    {
        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;

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
        AddCommand (Command.Select, HandleSelectCommand);
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

        MouseBindings.Add (MouseFlags.Button1DoubleClicked, Command.Accept);
        MouseBindings.ReplaceCommands (MouseFlags.Button3Clicked, Command.Context);
        MouseBindings.ReplaceCommands (MouseFlags.Button1Clicked | MouseFlags.ButtonCtrl, Command.Context);
        MouseBindings.Add (MouseFlags.WheeledDown, Command.ScrollDown);
        MouseBindings.Add (MouseFlags.WheeledUp, Command.ScrollUp);
        MouseBindings.Add (MouseFlags.WheeledLeft, Command.ScrollLeft);
        MouseBindings.Add (MouseFlags.WheeledRight, Command.ScrollRight);

        SetContentSize (new (COLUMN_WIDTH * 16 + RowLabelWidth, MAX_CODE_POINT / 16 * _rowHeight + HEADER_HEIGHT));

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
        ViewportChanged += (sender, args) =>
                           {
                               if (Viewport.Width < GetContentSize ().Width)
                               {
                                   HorizontalScrollBar.Visible = true;
                               }
                               else
                               {
                                   HorizontalScrollBar.Visible = false;
                               }
                           };

        // Set up the vertical scrollbar. Turn off AutoShow since it's always visible.
        VerticalScrollBar.AutoShow = true;
        VerticalScrollBar.Visible = false;
        VerticalScrollBar.X = Pos.AnchorEnd ();
        VerticalScrollBar.Y = HEADER_HEIGHT; // Header

        // The scrollbars are in the Padding. VisualRole.Focus/Active are used to draw the
        // CharMap headers. Override Padding to force it to draw to match.
        Padding!.GettingAttributeForRole += PaddingOnGettingAttributeForRole;
    }

    private void PaddingOnGettingAttributeForRole (object? sender, VisualRoleEventArgs e)
    {
        if (e.Role != VisualRole.Focus && e.Role != VisualRole.Active)
        {
            e.NewValue = GetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
        }

        e.Cancel = true;
    }

    private bool? Move (ICommandContext? commandContext, int cpOffset)
    {
        if (RaiseSelecting (commandContext) is true)
        {
            return true;
        }

        SelectedCodePoint += cpOffset;

        return true;
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

    #region Cursor

    private Point GetCursor (int codePoint)
    {
        // + 1 for padding between label and first column
        int x = codePoint % 16 * COLUMN_WIDTH + RowLabelWidth + 1 - Viewport.X;
        int y = codePoint / 16 * _rowHeight + HEADER_HEIGHT - Viewport.Y;

        return new (x, y);
    }

    /// <inheritdoc/>
    public override Point? PositionCursor ()
    {
        Point cursor = GetCursor (SelectedCodePoint);

        if (HasFocus
            && cursor.X >= RowLabelWidth
            && cursor.X < Viewport.Width
            && cursor.Y > 0
            && cursor.Y < Viewport.Height)
        {
            Move (cursor.X, cursor.Y);
        }
        else
        {
            return null;
        }

        return cursor;
    }

    #endregion Cursor

    // ReSharper disable once InconsistentNaming
    private static readonly int MAX_CODE_POINT = UnicodeRange.Ranges.Max (r => r.End);
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
            SelectedCodePointChanged?.Invoke (this, new (SelectedCodePoint));
        }
    }

    /// <summary>
    ///     Raised when the selected code point changes.
    /// </summary>
    public event EventHandler<EventArgs<int>>? SelectedCodePointChanged;

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

    /// <summary>
    ///     Gets or sets whether the number of columns each glyph is displayed.
    /// </summary>
    public bool ShowGlyphWidths
    {
        get => _rowHeight == 2;
        set
        {
            _rowHeight = value ? 2 : 1;
            SetNeedsDraw ();
        }
    }

    private void CopyCodePoint () { Clipboard.Contents = $"U+{SelectedCodePoint:x5}"; }
    private void CopyGlyph () { Clipboard.Contents = $"{new Rune (SelectedCodePoint)}"; }

    #region Drawing

    private static int RowLabelWidth => $"U+{MAX_CODE_POINT:x5}".Length + 1;

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        if (Viewport.Height == 0 || Viewport.Width == 0)
        {
            return true;
        }

        int selectedCol = SelectedCodePoint % 16;
        int selectedRow = SelectedCodePoint / 16;

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
            // What row is this?
            int row = (y + Viewport.Y - 1) / _rowHeight;
            int val = row * 16;

            // Draw the row label (U+XXXX_)
            SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Active);
            Move (0, y);

            // Swap Active/Focus so the selected row is highlighted
            if (y + Viewport.Y - 1 == selectedRow)
            {
                SetAttributeForRole (HasFocus ? VisualRole.Active : VisualRole.Focus);
            }

            if (val > MAX_CODE_POINT)
            {
                // No row
                Move (0, y);
                AddStr (new (' ', RowLabelWidth));

                continue;
            }

            if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
            {
                AddStr ($"U+{val / 16:x5}_");
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
                if (row == selectedRow && col == selectedCol)
                {
                    SetAttributeForRole (VisualRole.Active);
                }

                int scalar = val + col;
                var rune = (Rune)'?';

                if (Rune.IsValid (scalar))
                {
                    rune = new (scalar);
                }

                int width = rune.GetColumns ();

                if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
                {
                    // Draw the rune
                    if (width > 0)
                    {
                        AddRune (rune);
                    }
                    else
                    {
                        if (rune.IsCombiningMark ())
                        {
                            // This is a hack to work around the fact that combining marks
                            // a) can't be rendered on their own
                            // b) that don't normalize are not properly supported in 
                            //    any known terminal (esp Windows/AtlasEngine). 
                            // See Issue #2616
                            var sb = new StringBuilder ();
                            sb.Append ('a');
                            sb.Append (rune);

                            // Try normalizing after combining with 'a'. If it normalizes, at least 
                            // it'll show on the 'a'. If not, just show the replacement char.
                            string normal = sb.ToString ().Normalize (NormalizationForm.FormC);

                            if (normal.Length == 1)
                            {
                                AddRune ((Rune)normal [0]);
                            }
                            else
                            {
                                AddRune (Rune.ReplacementChar);
                            }
                        }
                    }
                }
                else
                {
                    // Draw the width of the rune faint
                    Attribute attr = GetAttributeForRole (VisualRole.Normal);
                    SetAttribute (attr with { Style = attr.Style | TextStyle.Faint });
                    AddStr ($"{width}");
                }

                // If we're at the cursor position, and we don't have focus
                if (row == selectedRow && col == selectedCol)
                {
                    SetAttributeForRole (VisualRole.Normal);
                }
            }
        }

        return true;
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

    // TODO: Use this to demonstrate using a popover to show glyph info on hover
    // public event EventHandler<ListViewItemEventArgs>? Hover;

    private bool? HandleSelectCommand (ICommandContext? commandContext)
    {
        Point position = GetCursor (SelectedCodePoint);

        if (commandContext is CommandContext<MouseBinding> { Binding.MouseEventArgs: { } } mouseCommandContext)
        {
            // If the mouse is clicked on the headers, map it to the first glyph of the row/col
            position = mouseCommandContext.Binding.MouseEventArgs.Position;

            if (position.Y == 0)
            {
                position = position with { Y = GetCursor (SelectedCodePoint).Y };
            }

            if (position.X < RowLabelWidth || position.X > RowLabelWidth + 16 * COLUMN_WIDTH - 1)
            {
                position = position with { X = GetCursor (SelectedCodePoint).X };
            }
        }

        if (RaiseSelecting (commandContext) is true)
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

            if (!TryGetCodePointFromPosition (mouseCommandContext.Binding.MouseEventArgs.Position, out int cp))
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
            if (!TryGetCodePointFromPosition (mouseCommandContext.Binding.MouseEventArgs.Position, out newCodePoint))
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
        Application.Popover?.Register (contextMenu);

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

        int row = (position.Y - 1 - -Viewport.Y) / _rowHeight; // -1 for header
        int col = (position.X - RowLabelWidth - -Viewport.X) / COLUMN_WIDTH;

        if (col > 15)
        {
            col = 15;
        }

        codePoint = row * 16 + col;

        if (codePoint > MAX_CODE_POINT)
        {
            return false;
        }

        return true;
    }

    #endregion Mouse Handling

    #region Details Dialog

    [RequiresUnreferencedCode ("AOT")]
    [RequiresDynamicCode ("AOT")]
    private void ShowDetails ()
    {
        if (!Application.Initialized)
        {
            // Some unit tests invoke Accept without Init
            return;
        }

        UcdApiClient? client = new ();
        var decResponse = string.Empty;
        var getCodePointError = string.Empty;

        Dialog? waitIndicator = new ()
        {
            Title = Strings.charMapCPInfoDlgTitle,
            X = Pos.Center (),
            Y = Pos.Center (),
            Width = 40,
            Height = 10,
            Buttons = [new () { Text = Strings.btnCancel }]
        };

        var errorLabel = new Label
        {
            Text = UcdApiClient.BaseUrl,
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (3),
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

        waitIndicator.Ready += async (s, a) =>
                               {
                                   try
                                   {
                                       decResponse = await client.GetCodepointDec (SelectedCodePoint).ConfigureAwait (false);
                                       Application.Invoke (() => waitIndicator.RequestStop ());
                                   }
                                   catch (HttpRequestException e)
                                   {
                                       getCodePointError = errorLabel.Text = e.Message;
                                       Application.Invoke (() => waitIndicator.RequestStop ());
                                   }
                               };
        Application.Run (waitIndicator);
        waitIndicator.Dispose ();

        if (!string.IsNullOrEmpty (decResponse))
        {
            var name = string.Empty;

            using (JsonDocument document = JsonDocument.Parse (decResponse))
            {
                JsonElement root = document.RootElement;

                // Get a property by name and output its value
                if (root.TryGetProperty ("name", out JsonElement nameElement))
                {
                    name = nameElement.GetString ();
                }

                //// Navigate to a nested property and output its value
                //if (root.TryGetProperty ("property3", out JsonElement property3Element)
                //&& property3Element.TryGetProperty ("nestedProperty", out JsonElement nestedPropertyElement)) {
                //	Console.WriteLine (nestedPropertyElement.GetString ());
                //}
                decResponse = JsonSerializer.Serialize (
                                                        document.RootElement,
                                                        new
                                                            JsonSerializerOptions
                                                            { WriteIndented = true }
                                                       );
            }

            var title = $"{ToCamelCase (name!)} - {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}";

            Button? copyGlyph = new () { Text = Strings.charMapCopyGlyph };
            Button? copyCodepoint = new () { Text = Strings.charMapCopyCP };
            Button? cancel = new () { Text = Strings.btnCancel };

            var dlg = new Dialog { Title = title, Buttons = [copyGlyph, copyCodepoint, cancel] };

            copyGlyph.Accepting += (s, a) =>
                                   {
                                       CopyGlyph ();
                                       dlg!.RequestStop ();
                                   };

            copyCodepoint.Accepting += (s, a) =>
                                       {
                                           CopyCodePoint ();
                                           dlg!.RequestStop ();
                                       };
            cancel.Accepting += (s, a) => dlg!.RequestStop ();

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
                Width = Dim.Fill (),
                Height = Dim.Fill (2),
                ReadOnly = true,
                Text = decResponse
            };

            dlg.Add (json);

            Application.Run (dlg);
            dlg.Dispose ();
        }
        else
        {
            MessageBox.ErrorQuery (
                                   Strings.error,
                                   $"{UcdApiClient.BaseUrl}codepoint/dec/{SelectedCodePoint} {Strings.failedGetting}{Environment.NewLine}{new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}.",
                                   Strings.btnOk
                                  );
        }

        // BUGBUG: This is a workaround for some weird ScrollView related mouse grab bug
        Application.GrabMouse (this);
    }

    #endregion Details Dialog
}
