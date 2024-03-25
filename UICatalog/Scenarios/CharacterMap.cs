#define DRAW_CONTENT

//#define BASE_DRAW_CONTENT
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using Terminal.Gui;
using static Terminal.Gui.SpinnerStyle;

namespace UICatalog.Scenarios;

/// <summary>
///     This Scenario demonstrates building a custom control (a class deriving from View) that: - Provides a
///     "Character Map" application (like Windows' charmap.exe). - Helps test unicode character rendering in Terminal.Gui -
///     Illustrates how to use ScrollView to do infinite scrolling
/// </summary>
[ScenarioMetadata ("Character Map", "Unicode viewer demonstrating the ScrollView control.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("ScrollView")]
public class CharacterMap : Scenario
{
    public Label _errorLabel;
    private TableView _categoryList;
    private CharMap _charMap;

    // Don't create a Window, just return the top-level view
    public override void Init ()
    {
        Application.Init ();
        Top = new ();
        Top.ColorScheme = Colors.ColorSchemes ["Base"];
    }

    public override void Setup ()
    {
        _charMap = new() { X = 0, Y = 1, Height = Dim.Fill () };
        Top.Add (_charMap);

        var jumpLabel = new Label
        {
            X = Pos.Right (_charMap) + 1,
            Y = Pos.Y (_charMap),
            HotKeySpecifier = (Rune)'_',
            Text = "_Jump To Code Point:"
        };
        Top.Add (jumpLabel);

        var jumpEdit = new TextField
        {
            X = Pos.Right (jumpLabel) + 1, Y = Pos.Y (_charMap), Width = 10, Caption = "e.g. 01BE3"
        };
        Top.Add (jumpEdit);

        _errorLabel = new()
        {
            X = Pos.Right (jumpEdit) + 1, Y = Pos.Y (_charMap), ColorScheme = Colors.ColorSchemes ["error"], Text = "err"
        };
        Top.Add (_errorLabel);

#if TEXT_CHANGED_TO_JUMP
        jumpEdit.TextChanged += JumpEdit_TextChanged;
#else
        jumpEdit.Accept += JumpEditOnAccept;

        void JumpEditOnAccept (object sender, CancelEventArgs e)
        {
            JumpEdit_TextChanged (sender, new (jumpEdit.Text, jumpEdit.Text));

            // Cancel the event to prevent ENTER from being handled elsewhere
            e.Cancel = true;
        }
#endif
        _categoryList = new() { X = Pos.Right (_charMap), Y = Pos.Bottom (jumpLabel), Height = Dim.Fill () };

        _categoryList.FullRowSelect = true;

        //jumpList.Style.ShowHeaders = false;
        //jumpList.Style.ShowHorizontalHeaderOverline = false;
        //jumpList.Style.ShowHorizontalHeaderUnderline = false;
        _categoryList.Style.ShowHorizontalBottomline = true;

        //jumpList.Style.ShowVerticalCellLines = false;
        //jumpList.Style.ShowVerticalHeaderLines = false;
        _categoryList.Style.AlwaysShowHeaders = true;

        var isDescending = false;

        _categoryList.Table = CreateCategoryTable (0, isDescending);

        // if user clicks the mouse in TableView
        _categoryList.MouseClick += (s, e) =>
                                    {
                                        _categoryList.ScreenToCell (e.MouseEvent.X, e.MouseEvent.Y, out int? clickedCol);

                                        if (clickedCol != null && e.MouseEvent.Flags.HasFlag (MouseFlags.Button1Clicked))
                                        {
                                            EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;
                                            string prevSelection = table.Data.ElementAt (_categoryList.SelectedRow).Category;
                                            isDescending = !isDescending;

                                            _categoryList.Table = CreateCategoryTable (clickedCol.Value, isDescending);

                                            table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;

                                            _categoryList.SelectedRow = table.Data
                                                                             .Select ((item, index) => new { item, index })
                                                                             .FirstOrDefault (x => x.item.Category == prevSelection)
                                                                             ?.index
                                                                        ?? -1;
                                        }
                                    };

        int longestName = UnicodeRange.Ranges.Max (r => r.Category.GetColumns ());

        _categoryList.Style.ColumnStyles.Add (
                                              0,
                                              new() { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName }
                                             );
        _categoryList.Style.ColumnStyles.Add (1, new() { MaxWidth = 1, MinWidth = 6 });
        _categoryList.Style.ColumnStyles.Add (2, new() { MaxWidth = 1, MinWidth = 6 });

        _categoryList.Width = _categoryList.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 4;

        _categoryList.SelectedCellChanged += (s, args) =>
                                             {
                                                 EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;
                                                 _charMap.StartCodePoint = table.Data.ToArray () [args.NewRow].Start;
                                             };

        Top.Add (_categoryList);

        _charMap.SelectedCodePoint = 0;
        _charMap.SetFocus ();

        // TODO: Replace this with Dim.Auto when that's ready
        _categoryList.Initialized += _categoryList_Initialized;

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new (
                              "_Quit",
                              $"{Application.QuitKey}",
                              () => Application.RequestStop ()
                             )
                     }
                    ),
                new (
                     "_Options",
                     new [] { CreateMenuShowWidth () }
                    )
            ]
        };
        Top.Add (menu);
    }

    private void _categoryList_Initialized (object sender, EventArgs e) { _charMap.Width = Dim.Fill () - _categoryList.Width; }

    private EnumerableTableSource<UnicodeRange> CreateCategoryTable (int sortByColumn, bool descending)
    {
        Func<UnicodeRange, object> orderBy;
        var categorySort = string.Empty;
        var startSort = string.Empty;
        var endSort = string.Empty;

        string sortIndicator = descending ? CM.Glyphs.DownArrow.ToString () : CM.Glyphs.UpArrow.ToString ();

        switch (sortByColumn)
        {
            case 0:
                orderBy = r => r.Category;
                categorySort = sortIndicator;

                break;
            case 1:
                orderBy = r => r.Start;
                startSort = sortIndicator;

                break;
            case 2:
                orderBy = r => r.End;
                endSort = sortIndicator;

                break;
            default:
                throw new ArgumentException ("Invalid column number.");
        }

        IOrderedEnumerable<UnicodeRange> sortedRanges = descending
                                                            ? UnicodeRange.Ranges.OrderByDescending (orderBy)
                                                            : UnicodeRange.Ranges.OrderBy (orderBy);

        return new (
                    sortedRanges,
                    new()
                    {
                        { $"Category{categorySort}", s => s.Category },
                        { $"Start{startSort}", s => $"{s.Start:x5}" },
                        { $"End{endSort}", s => $"{s.End:x5}" }
                    }
                   );
    }

    private MenuItem CreateMenuShowWidth ()
    {
        var item = new MenuItem { Title = "_Show Glyph Width" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _charMap?.ShowGlyphWidths;
        item.Action += () => { _charMap.ShowGlyphWidths = (bool)(item.Checked = !item.Checked); };

        return item;
    }

    private void JumpEdit_TextChanged (object sender, StateEventArgs<string> e)
    {
        var jumpEdit = sender as TextField;

        if (jumpEdit.Text.Length == 0)
        {
            return;
        }

        uint result = 0;

        if (jumpEdit.Text.StartsWith ("U+", StringComparison.OrdinalIgnoreCase) || jumpEdit.Text.StartsWith ("\\u"))
        {
            try
            {
                result = uint.Parse (jumpEdit.Text [2..], NumberStyles.HexNumber);
            }
            catch (FormatException)
            {
                _errorLabel.Text = "Invalid hex value";

                return;
            }
        }
        else if (jumpEdit.Text.StartsWith ("0", StringComparison.OrdinalIgnoreCase) || jumpEdit.Text.StartsWith ("\\u"))
        {
            try
            {
                result = uint.Parse (jumpEdit.Text, NumberStyles.HexNumber);
            }
            catch (FormatException)
            {
                _errorLabel.Text = "Invalid hex value";

                return;
            }
        }
        else
        {
            try
            {
                result = uint.Parse (jumpEdit.Text, NumberStyles.Integer);
            }
            catch (FormatException)
            {
                _errorLabel.Text = "Invalid value";

                return;
            }
        }

        if (result > RuneExtensions.MaxUnicodeCodePoint)
        {
            _errorLabel.Text = "Beyond maximum codepoint";

            return;
        }

        _errorLabel.Text = $"U+{result:x5}";

        EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;

        _categoryList.SelectedRow = table.Data
                                         .Select ((item, index) => new { item, index })
                                         .FirstOrDefault (x => x.item.Start <= result && x.item.End >= result)
                                         ?.index
                                    ?? -1;
        _categoryList.EnsureSelectedCellIsVisible ();

        // Ensure the typed glyph is selected 
        _charMap.SelectedCodePoint = (int)result;
    }
}

internal class CharMap : ScrollView
{
    private const CursorVisibility _cursor = CursorVisibility.Default;
    private const int COLUMN_WIDTH = 3;

    private ContextMenu _contextMenu = new ();
    private int _rowHeight = 1;
    private int _selected;
    private int _start;

    public CharMap ()
    {
        ColorScheme = Colors.ColorSchemes ["Dialog"];
        CanFocus = true;

        ContentSize = new (
                           RowWidth,
                           (MaxCodePoint / 16 + (ShowHorizontalScrollIndicator ? 2 : 1)) * _rowHeight
                          );

        AddCommand (
                    Command.ScrollUp,
                    () =>
                    {
                        if (SelectedCodePoint >= 16)
                        {
                            SelectedCodePoint -= 16;
                        }

                        return true;
                    }
                   );

        AddCommand (
                    Command.ScrollDown,
                    () =>
                    {
                        if (SelectedCodePoint < MaxCodePoint - 16)
                        {
                            SelectedCodePoint += 16;
                        }

                        return true;
                    }
                   );

        AddCommand (
                    Command.ScrollLeft,
                    () =>
                    {
                        if (SelectedCodePoint > 0)
                        {
                            SelectedCodePoint--;
                        }

                        return true;
                    }
                   );

        AddCommand (
                    Command.ScrollRight,
                    () =>
                    {
                        if (SelectedCodePoint < MaxCodePoint)
                        {
                            SelectedCodePoint++;
                        }

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        int page = (Bounds.Height / _rowHeight - 1) * 16;
                        SelectedCodePoint -= Math.Min (page, SelectedCodePoint);

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        int page = (Bounds.Height / _rowHeight - 1) * 16;
                        SelectedCodePoint += Math.Min (page, MaxCodePoint - SelectedCodePoint);

                        return true;
                    }
                   );

        AddCommand (
                    Command.TopHome,
                    () =>
                    {
                        SelectedCodePoint = 0;

                        return true;
                    }
                   );

        AddCommand (
                    Command.BottomEnd,
                    () =>
                    {
                        SelectedCodePoint = MaxCodePoint;

                        return true;
                    }
                   );
        KeyBindings.Add (Key.Enter, Command.Accept);

        AddCommand (
                    Command.Accept,
                    () =>
                    {
                        ShowDetails ();

                        return true;
                    }
                   );

        MouseClick += Handle_MouseClick;
    }

    /// <summary>Gets the coordinates of the Cursor based on the SelectedCodePoint in screen coordinates</summary>
    public Point Cursor
    {
        get
        {
            int row = SelectedCodePoint / 16 * _rowHeight + ContentOffset.Y + 1;

            int col = SelectedCodePoint % 16 * COLUMN_WIDTH + ContentOffset.X + RowLabelWidth + 1; // + 1 for padding

            return new (col, row);
        }
        set => throw new NotImplementedException ();
    }

    public static int MaxCodePoint => 0x10FFFF;

    /// <summary>
    ///     Specifies the starting offset for the character map. The default is 0x2500 which is the Box Drawing
    ///     characters.
    /// </summary>
    public int SelectedCodePoint
    {
        get => _selected;
        set
        {
            _selected = value;

            if (IsInitialized)
            {
                int row = SelectedCodePoint / 16 * _rowHeight;
                int col = SelectedCodePoint % 16 * COLUMN_WIDTH;

                int height = Bounds.Height - (ShowHorizontalScrollIndicator ? 2 : 1);

                if (row + ContentOffset.Y < 0)
                {
                    // Moving up.
                    ContentOffset = new (ContentOffset.X, row);
                }
                else if (row + ContentOffset.Y >= height)
                {
                    // Moving down.
                    ContentOffset = new (
                                         ContentOffset.X,
                                         Math.Min (row, row - height + _rowHeight)
                                        );
                }

                int width = Bounds.Width / COLUMN_WIDTH * COLUMN_WIDTH - (ShowVerticalScrollIndicator ? RowLabelWidth + 1 : RowLabelWidth);

                if (col + ContentOffset.X < 0)
                {
                    // Moving left.
                    ContentOffset = new (col, ContentOffset.Y);
                }
                else if (col + ContentOffset.X >= width)
                {
                    // Moving right.
                    ContentOffset = new (
                                         Math.Min (col, col - width + COLUMN_WIDTH),
                                         ContentOffset.Y
                                        );
                }
            }

            SetNeedsDisplay ();
            SelectedCodePointChanged?.Invoke (this, new (SelectedCodePoint, null));
        }
    }

    public bool ShowGlyphWidths
    {
        get => _rowHeight == 2;
        set
        {
            _rowHeight = value ? 2 : 1;
            SetNeedsDisplay ();
        }
    }

    /// <summary>
    ///     Specifies the starting offset for the character map. The default is 0x2500 which is the Box Drawing
    ///     characters.
    /// </summary>
    public int StartCodePoint
    {
        get => _start;
        set
        {
            _start = value;
            SelectedCodePoint = value;
            SetNeedsDisplay ();
        }
    }

    private static int RowLabelWidth => $"U+{MaxCodePoint:x5}".Length + 1;
    private static int RowWidth => RowLabelWidth + COLUMN_WIDTH * 16;
    public event EventHandler<ListViewItemEventArgs> Hover;

    public override void OnDrawContent (Rectangle contentArea)
    {
        if (contentArea.Height == 0 || contentArea.Width == 0)
        {
            return;
        }

        // Call the base (ScrollView) to draw the scrollbars. Do this ahead of our own drawing so that
        // any wide or tall glyphs actually render over the scrollbars (on platforms like Windows Terminal) that 
        // does this correctly.
        base.OnDrawContent (contentArea);

        Rectangle viewport = new (
                                  ContentOffset,
                                  new (
                                       Math.Max (Bounds.Width - (ShowVerticalScrollIndicator ? 1 : 0), 0),
                                       Math.Max (Bounds.Height - (ShowHorizontalScrollIndicator ? 1 : 0), 0)
                                      )
                                 );

        Rectangle oldClip = ClipToBounds ();

        if (ShowHorizontalScrollIndicator)
        {
            // ClipToBounds doesn't know about the scroll indicators, so if off, subtract one from height
            Driver.Clip = new (Driver.Clip.Location, new (Driver.Clip.Size.Width, Driver.Clip.Size.Height - 1));
        }

        if (ShowVerticalScrollIndicator)
        {
            // ClipToBounds doesn't know about the scroll indicators, so if off, subtract one from width
            Driver.Clip = new (Driver.Clip.Location, new (Driver.Clip.Size.Width - 1, Driver.Clip.Size.Height));
        }

        int cursorCol = Cursor.X - ContentOffset.X - RowLabelWidth - 1;
        int cursorRow = Cursor.Y - ContentOffset.Y - 1;

        Driver.SetAttribute (GetHotNormalColor ());
        Move (0, 0);
        Driver.AddStr (new (' ', RowLabelWidth + 1));

        for (var hexDigit = 0; hexDigit < 16; hexDigit++)
        {
            int x = ContentOffset.X + RowLabelWidth + hexDigit * COLUMN_WIDTH;

            if (x > RowLabelWidth - 2)
            {
                Move (x, 0);
                Driver.SetAttribute (GetHotNormalColor ());
                Driver.AddStr (" ");

                Driver.SetAttribute (
                                     HasFocus && cursorCol + ContentOffset.X + RowLabelWidth == x
                                         ? ColorScheme.HotFocus
                                         : GetHotNormalColor ()
                                    );
                Driver.AddStr ($"{hexDigit:x}");
                Driver.SetAttribute (GetHotNormalColor ());
                Driver.AddStr (" ");
            }
        }

        int firstColumnX = viewport.X + RowLabelWidth;

        // Even though the Clip is set to prevent us from drawing on the row potentially occupied by the horizontal
        // scroll bar, we do the smart thing and not actually draw that row if not necessary.
        for (var y = 1; y < Bounds.Height - (ShowHorizontalScrollIndicator ? 1 : 0); y++)
        {
            // What row is this?
            int row = (y - ContentOffset.Y - 1) / _rowHeight;

            int val = row * 16;

            if (val > MaxCodePoint)
            {
                continue;
            }

            Move (firstColumnX + COLUMN_WIDTH, y);
            Driver.SetAttribute (GetNormalColor ());

            // Note, this code naïvely draws all columns, even if the viewport is smaller than
            // the needed width. We rely on Clip to ensure we don't draw past the viewport.
            // If we were *really* worried about performance, we'd optimize this code to only draw the
            // parts of the row that are actually visible in the viewport.
            for (var col = 0; col < 16; col++)
            {
                int x = firstColumnX + COLUMN_WIDTH * col + 1;

                Move (x, y);

                if (cursorRow + ContentOffset.Y + 1 == y && cursorCol + ContentOffset.X + firstColumnX + 1 == x && !HasFocus)
                {
                    Driver.SetAttribute (GetFocusColor ());
                }

                int scalar = val + col;
                var rune = (Rune)'?';

                if (Rune.IsValid (scalar))
                {
                    rune = new (scalar);
                }

                int width = rune.GetColumns ();

                // are we at first row of the row?
                if (!ShowGlyphWidths || (y - ContentOffset.Y) % _rowHeight > 0)
                {
                    if (width > 0)
                    {
                        Driver.AddRune (rune);
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
                                Driver.AddRune (normal [0]);
                            }
                            else
                            {
                                Driver.AddRune (Rune.ReplacementChar);
                            }
                        }
                    }
                }
                else
                {
                    Driver.SetAttribute (ColorScheme.HotNormal);
                    Driver.AddStr ($"{width}");
                }

                if (cursorRow + ContentOffset.Y + 1 == y && cursorCol + ContentOffset.X + firstColumnX + 1 == x && !HasFocus)
                {
                    Driver.SetAttribute (GetNormalColor ());
                }
            }

            Move (0, y);

            Driver.SetAttribute (
                                 HasFocus && cursorRow + ContentOffset.Y + 1 == y
                                     ? ColorScheme.HotFocus
                                     : ColorScheme.HotNormal
                                );

            if (!ShowGlyphWidths || (y - ContentOffset.Y) % _rowHeight > 0)
            {
                Driver.AddStr ($"U+{val / 16:x5}_ ");
            }
            else
            {
                Driver.AddStr (new (' ', RowLabelWidth));
            }
        }

        Driver.Clip = oldClip;
    }

    public override bool OnEnter (View view)
    {
        if (IsInitialized)
        {
            Application.Driver.SetCursorVisibility (_cursor);
        }

        return base.OnEnter (view);
    }

    public override bool OnLeave (View view)
    {
        Driver.SetCursorVisibility (CursorVisibility.Invisible);

        return base.OnLeave (view);
    }

    public override void PositionCursor ()
    {
        if (HasFocus
            && Cursor.X >= RowLabelWidth
            && Cursor.X < Bounds.Width - (ShowVerticalScrollIndicator ? 1 : 0)
            && Cursor.Y > 0
            && Cursor.Y < Bounds.Height - (ShowHorizontalScrollIndicator ? 1 : 0))
        {
            Driver.SetCursorVisibility (_cursor);
            Move (Cursor.X, Cursor.Y);
        }
        else
        {
            Driver.SetCursorVisibility (CursorVisibility.Invisible);
        }
    }

    public event EventHandler<ListViewItemEventArgs> SelectedCodePointChanged;

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

    private void CopyCodePoint () { Clipboard.Contents = $"U+{SelectedCodePoint:x5}"; }
    private void CopyGlyph () { Clipboard.Contents = $"{new Rune (SelectedCodePoint)}"; }

    private void Handle_MouseClick (object sender, MouseEventEventArgs args)
    {
        MouseEvent me = args.MouseEvent;

        if (me.Flags != MouseFlags.ReportMousePosition && me.Flags != MouseFlags.Button1Clicked && me.Flags != MouseFlags.Button1DoubleClicked)
        {
            return;
        }

        if (me.Y == 0)
        {
            me.Y = Cursor.Y;
        }

        if (me.Y > 0)
        { }

        if (me.X < RowLabelWidth || me.X > RowLabelWidth + 16 * COLUMN_WIDTH - 1)
        {
            me.X = Cursor.X;
        }

        int row = (me.Y - 1 - ContentOffset.Y) / _rowHeight; // -1 for header
        int col = (me.X - RowLabelWidth - ContentOffset.X) / COLUMN_WIDTH;

        if (col > 15)
        {
            col = 15;
        }

        int val = row * 16 + col;

        if (val > MaxCodePoint)
        {
            return;
        }

        if (me.Flags == MouseFlags.ReportMousePosition)
        {
            Hover?.Invoke (this, new (val, null));
        }

        if (me.Flags == MouseFlags.Button1Clicked)
        {
            SelectedCodePoint = val;

            return;
        }

        if (me.Flags == MouseFlags.Button1DoubleClicked)
        {
            SelectedCodePoint = val;
            ShowDetails ();

            return;
        }

        if (me.Flags == _contextMenu.MouseFlags)
        {
            SelectedCodePoint = val;

            _contextMenu = new()
            {
                Position = new (me.X + 1, me.Y + 1),
                MenuItems = new (
                                 new MenuItem []
                                 {
                                     new (
                                          "_Copy Glyph",
                                          "",
                                          CopyGlyph,
                                          null,
                                          null,
                                          (KeyCode)Key.C.WithCtrl
                                         ),
                                     new (
                                          "Copy Code _Point",
                                          "",
                                          CopyCodePoint,
                                          null,
                                          null,
                                          (KeyCode)Key.C.WithCtrl
                                                      .WithShift
                                         )
                                 }
                                )
            };
            _contextMenu.Show ();
        }
    }

    private void ShowDetails ()
    {
        var client = new UcdApiClient ();
        var decResponse = string.Empty;

        var waitIndicator = new Dialog
        {
            Title = "Getting Code Point Information",
            X = Pos.Center (),
            Y = Pos.Center (),
            Height = 7,
            Width = 50,
            Buttons = [new() { Text = "Cancel" }]
        };

        var errorLabel = new Label
        {
            Text = UcdApiClient.BaseUrl,
            AutoSize = false,
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            TextAlignment = TextAlignment.Centered
        };
        var spinner = new SpinnerView { X = Pos.Center (), Y = Pos.Center (), Style = new Aesthetic () };
        spinner.AutoSpin = true;
        waitIndicator.Add (errorLabel);
        waitIndicator.Add (spinner);

        waitIndicator.Ready += async (s, a) =>
                               {
                                   try
                                   {
                                       decResponse = await client.GetCodepointDec (SelectedCodePoint);
                                   }
                                   catch (HttpRequestException e)
                                   {
                                       (s as Dialog).Text = e.Message;

                                       Application.Invoke (
                                                           () =>
                                                           {
                                                               spinner.Visible = false;
                                                               errorLabel.Text = e.Message;
                                                               errorLabel.ColorScheme = Colors.ColorSchemes ["Error"];
                                                               errorLabel.Visible = true;
                                                           }
                                                          );
                                   }

                                   (s as Dialog)?.RequestStop ();
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

            var title = $"{ToCamelCase (name)} - {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}";

            var copyGlyph = new Button { Text = "Copy _Glyph" };
            var copyCP = new Button { Text = "Copy Code _Point" };
            var cancel = new Button { Text = "Cancel" };

            var dlg = new Dialog { Title = title, Buttons = [copyGlyph, copyCP, cancel] };

            copyGlyph.Accept += (s, a) =>
                                {
                                    CopyGlyph ();
                                    dlg.RequestStop ();
                                };

            copyCP.Accept += (s, a) =>
                             {
                                 CopyCodePoint ();
                                 dlg.RequestStop ();
                             };
            cancel.Accept += (s, a) => dlg.RequestStop ();

            var rune = (Rune)SelectedCodePoint;
            var label = new Label { Text = "IsAscii: ", X = 0, Y = 0 };
            dlg.Add (label);

            label = new() { Text = $"{rune.IsAscii}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = ", Bmp: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = $"{rune.IsBmp}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = ", CombiningMark: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = $"{rune.IsCombiningMark ()}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = ", SurrogatePair: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = $"{rune.IsSurrogatePair ()}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = ", Plane: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = $"{rune.Plane}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = "Columns: ", X = 0, Y = Pos.Bottom (label) };
            dlg.Add (label);

            label = new() { Text = $"{rune.GetColumns ()}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = ", Utf16SequenceLength: ", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new() { Text = $"{rune.Utf16SequenceLength}", X = Pos.Right (label), Y = Pos.Top (label) };
            dlg.Add (label);

            label = new()
            {
                Text =
                    $"Code Point Information from {UcdApiClient.BaseUrl}codepoint/dec/{SelectedCodePoint}:",
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
                                   "Code Point API",
                                   $"{UcdApiClient.BaseUrl}codepoint/dec/{SelectedCodePoint} did not return a result for\r\n {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}.",
                                   "Ok"
                                  );
        }

        // BUGBUG: This is a workaround for some weird ScrollView related mouse grab bug
        Application.GrabMouse (this);
    }
}

public class UcdApiClient
{
    public const string BaseUrl = "https://ucdapi.org/unicode/latest/";
    private static readonly HttpClient _httpClient = new ();

    public async Task<string> GetChars (string chars)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}chars/{Uri.EscapeDataString (chars)}");
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ();
    }

    public async Task<string> GetCharsName (string chars)
    {
        HttpResponseMessage response =
            await _httpClient.GetAsync ($"{BaseUrl}chars/{Uri.EscapeDataString (chars)}/name");
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ();
    }

    public async Task<string> GetCodepointDec (int dec)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}codepoint/dec/{dec}");
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ();
    }

    public async Task<string> GetCodepointHex (string hex)
    {
        HttpResponseMessage response = await _httpClient.GetAsync ($"{BaseUrl}codepoint/hex/{hex}");
        response.EnsureSuccessStatusCode ();

        return await response.Content.ReadAsStringAsync ();
    }
}

internal class UnicodeRange
{
    public static List<UnicodeRange> Ranges = GetRanges ();

    public string Category;
    public int End;
    public int Start;

    public UnicodeRange (int start, int end, string category)
    {
        Start = start;
        End = end;
        Category = category;
    }

    public static List<UnicodeRange> GetRanges ()
    {
        IEnumerable<UnicodeRange> ranges =
            from r in typeof (UnicodeRanges).GetProperties (BindingFlags.Static | BindingFlags.Public)
            let urange = r.GetValue (null) as System.Text.Unicode.UnicodeRange
            let name = string.IsNullOrEmpty (r.Name)
                           ? $"U+{urange.FirstCodePoint:x5}-U+{urange.FirstCodePoint + urange.Length:x5}"
                           : r.Name
            where name != "None" && name != "All"
            select new UnicodeRange (urange.FirstCodePoint, urange.FirstCodePoint + urange.Length, name);

        // .NET 8.0 only supports BMP in UnicodeRanges: https://learn.microsoft.com/en-us/dotnet/api/system.text.unicode.unicoderanges?view=net-8.0
        List<UnicodeRange> nonBmpRanges = new ()
        {
            new (
                 0x1F130,
                 0x1F149,
                 "Squared Latin Capital Letters"
                ),
            new (
                 0x12400,
                 0x1240f,
                 "Cuneiform Numbers and Punctuation"
                ),
            new (0x10000, 0x1007F, "Linear B Syllabary"),
            new (0x10080, 0x100FF, "Linear B Ideograms"),
            new (0x10100, 0x1013F, "Aegean Numbers"),
            new (0x10300, 0x1032F, "Old Italic"),
            new (0x10330, 0x1034F, "Gothic"),
            new (0x10380, 0x1039F, "Ugaritic"),
            new (0x10400, 0x1044F, "Deseret"),
            new (0x10450, 0x1047F, "Shavian"),
            new (0x10480, 0x104AF, "Osmanya"),
            new (0x10800, 0x1083F, "Cypriot Syllabary"),
            new (
                 0x1D000,
                 0x1D0FF,
                 "Byzantine Musical Symbols"
                ),
            new (0x1D100, 0x1D1FF, "Musical Symbols"),
            new (0x1D300, 0x1D35F, "Tai Xuan Jing Symbols"),
            new (
                 0x1D400,
                 0x1D7FF,
                 "Mathematical Alphanumeric Symbols"
                ),
            new (0x1F600, 0x1F532, "Emojis Symbols"),
            new (
                 0x20000,
                 0x2A6DF,
                 "CJK Unified Ideographs Extension B"
                ),
            new (
                 0x2F800,
                 0x2FA1F,
                 "CJK Compatibility Ideographs Supplement"
                ),
            new (0xE0000, 0xE007F, "Tags")
        };

        return ranges.Concat (nonBmpRanges).OrderBy (r => r.Category).ToList ();
    }
}
