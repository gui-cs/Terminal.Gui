#define OTHER_CONTROLS

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
///     Illustrates how to do infinite scrolling
/// </summary>
[ScenarioMetadata ("Character Map", "Unicode viewer demonstrating infinite content, scrolling, and Unicode.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Scrolling")]

public class CharacterMap : Scenario
{
    public Label _errorLabel;
    private TableView _categoryList;
    private CharMap _charMap;

    // Don't create a Window, just return the top-level view
    public override void Main ()
    {
        Application.Init ();

        var top = new Window
        {
            BorderStyle = LineStyle.None
        };

        _charMap = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        top.Add (_charMap);

#if OTHER_CONTROLS
        _charMap.Y = 1;

        var jumpLabel = new Label
        {
            X = Pos.Right (_charMap) + 1,
            Y = Pos.Y (_charMap),
            HotKeySpecifier = (Rune)'_',
            Text = "_Jump To Code Point:"
        };
        top.Add (jumpLabel);

        var jumpEdit = new TextField
        {
            X = Pos.Right (jumpLabel) + 1, Y = Pos.Y (_charMap), Width = 10, Caption = "e.g. 01BE3"
        };
        top.Add (jumpEdit);

        _errorLabel = new ()
        {
            X = Pos.Right (jumpEdit) + 1, Y = Pos.Y (_charMap), ColorScheme = Colors.ColorSchemes ["error"], Text = "err"
        };
        top.Add (_errorLabel);

        jumpEdit.Accepting += JumpEditOnAccept;

        _categoryList = new () { X = Pos.Right (_charMap), Y = Pos.Bottom (jumpLabel), Height = Dim.Fill () };
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
                                        _categoryList.ScreenToCell (e.Position, out int? clickedCol);

                                        if (clickedCol != null && e.Flags.HasFlag (MouseFlags.Button1Clicked))
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
                                              new () { MaxWidth = longestName, MinWidth = longestName, MinAcceptableWidth = longestName }
                                             );
        _categoryList.Style.ColumnStyles.Add (1, new () { MaxWidth = 1, MinWidth = 6 });
        _categoryList.Style.ColumnStyles.Add (2, new () { MaxWidth = 1, MinWidth = 6 });

        _categoryList.Width = _categoryList.Style.ColumnStyles.Sum (c => c.Value.MinWidth) + 4;

        _categoryList.SelectedCellChanged += (s, args) =>
                                             {
                                                 EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList.Table;
                                                 _charMap.StartCodePoint = table.Data.ToArray () [args.NewRow].Start;
                                             };

        top.Add (_categoryList);

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
        top.Add (menu);
#endif // OTHER_CONTROLS

        _charMap.SelectedCodePoint = 0;
        _charMap.SetFocus ();

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();

        return;

        void JumpEditOnAccept (object sender, CommandEventArgs e)
        {
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


            // Cancel the event to prevent ENTER from being handled elsewhere
            e.Cancel = true;
        }
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
                    new ()
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

}

internal class CharMap : View
{
    private const int COLUMN_WIDTH = 3;

    private ContextMenu _contextMenu = new ();
    private int _rowHeight = 1;
    private int _selected;
    private int _start;

    public CharMap ()
    {
        ColorScheme = Colors.ColorSchemes ["Dialog"];
        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;

        SetContentSize (new (RowWidth, (MaxCodePoint / 16 + 2) * _rowHeight));

        AddCommand (
                    Command.ScrollUp,
                    () =>
                    {
                        if (SelectedCodePoint >= 16)
                        {
                            SelectedCodePoint -= 16;
                        }

                        ScrollVertical (-_rowHeight);

                        return true;
                    }
                   );

        AddCommand (
                    Command.ScrollDown,
                    () =>
                    {
                        if (SelectedCodePoint <= MaxCodePoint - 16)
                        {
                            SelectedCodePoint += 16;
                        }

                        if (Cursor.Y >= Viewport.Height)
                        {
                            ScrollVertical (_rowHeight);
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

                        if (Cursor.X > RowLabelWidth + 1)
                        {
                            ScrollHorizontal (-COLUMN_WIDTH);
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

                        if (Cursor.X >= Viewport.Width)
                        {
                            ScrollHorizontal (COLUMN_WIDTH);
                        }

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        int page = (Viewport.Height - 1 / _rowHeight) * 16;
                        SelectedCodePoint -= Math.Min (page, SelectedCodePoint);
                        Viewport = Viewport with { Y = SelectedCodePoint / 16 * _rowHeight };

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        int page = (Viewport.Height - 1 / _rowHeight) * 16;
                        SelectedCodePoint += Math.Min (page, MaxCodePoint - SelectedCodePoint);
                        Viewport = Viewport with { Y = SelectedCodePoint / 16 * _rowHeight };

                        return true;
                    }
                   );

        AddCommand (
                    Command.Start,
                    () =>
                    {
                        SelectedCodePoint = 0;

                        return true;
                    }
                   );

        AddCommand (
                    Command.End,
                    () =>
                    {
                        SelectedCodePoint = MaxCodePoint;
                        Viewport = Viewport with { Y = SelectedCodePoint / 16 * _rowHeight };

                        return true;
                    }
                   );

        AddCommand (
                    Command.Accept,
                    () =>
                    {
                        ShowDetails ();

                        return true;
                    }
                   );

        KeyBindings.Add (Key.CursorUp, Command.ScrollUp);
        KeyBindings.Add (Key.CursorDown, Command.ScrollDown);
        KeyBindings.Add (Key.CursorLeft, Command.ScrollLeft);
        KeyBindings.Add (Key.CursorRight, Command.ScrollRight);
        KeyBindings.Add (Key.PageUp, Command.PageUp);
        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Add (Key.End, Command.End);

        MouseClick += Handle_MouseClick;
        MouseEvent += Handle_MouseEvent;

        // Prototype scrollbars
        Padding.Thickness = new (0, 0, 1, 1);

        var up = new Button
        {
            X = Pos.AnchorEnd (1),
            Y = 0,
            Height = 1,
            Width = 1,
            NoPadding = true,
            NoDecorations = true,
            Title = CM.Glyphs.UpArrow.ToString (),
            WantContinuousButtonPressed = true,
            ShadowStyle = ShadowStyle.None,
            CanFocus = false
        };
        up.Accepting += (sender, args) => { args.Cancel = ScrollVertical (-1) == true; };

        var down = new Button
        {
            X = Pos.AnchorEnd (1),
            Y = Pos.AnchorEnd (2),
            Height = 1,
            Width = 1,
            NoPadding = true,
            NoDecorations = true,
            Title = CM.Glyphs.DownArrow.ToString (),
            WantContinuousButtonPressed = true,
            ShadowStyle = ShadowStyle.None,
            CanFocus = false
        };
        down.Accepting += (sender, args) => { ScrollVertical (1); };

        var left = new Button
        {
            X = 0,
            Y = Pos.AnchorEnd (1),
            Height = 1,
            Width = 1,
            NoPadding = true,
            NoDecorations = true,
            Title = CM.Glyphs.LeftArrow.ToString (),
            WantContinuousButtonPressed = true,
            ShadowStyle = ShadowStyle.None,
            CanFocus = false
        };
        left.Accepting += (sender, args) => { ScrollHorizontal (-1); };

        var right = new Button
        {
            X = Pos.AnchorEnd (2),
            Y = Pos.AnchorEnd (1),
            Height = 1,
            Width = 1,
            NoPadding = true,
            NoDecorations = true,
            Title = CM.Glyphs.RightArrow.ToString (),
            WantContinuousButtonPressed = true,
            ShadowStyle = ShadowStyle.None,
            CanFocus = false
        };
        right.Accepting += (sender, args) => { ScrollHorizontal (1); };

        Padding.Add (up, down, left, right);
    }

    private void Handle_MouseEvent (object sender, MouseEventArgs e)
    {
        if (e.Flags == MouseFlags.WheeledDown)
        {
            ScrollVertical (1);
            e.Handled = true;

            return;
        }

        if (e.Flags == MouseFlags.WheeledUp)
        {
            ScrollVertical (-1);
            e.Handled = true;

            return;
        }

        if (e.Flags == MouseFlags.WheeledRight)
        {
            ScrollHorizontal (1);
            e.Handled = true;

            return;
        }

        if (e.Flags == MouseFlags.WheeledLeft)
        {
            ScrollHorizontal (-1);
            e.Handled = true;
        }
    }

    /// <summary>Gets the coordinates of the Cursor based on the SelectedCodePoint in screen coordinates</summary>
    public Point Cursor
    {
        get
        {
            int row = SelectedCodePoint / 16 * _rowHeight - Viewport.Y + 1;

            int col = SelectedCodePoint % 16 * COLUMN_WIDTH - Viewport.X + RowLabelWidth + 1; // + 1 for padding between label and first column

            return new (col, row);
        }
        set => throw new NotImplementedException ();
    }

    public static int MaxCodePoint = UnicodeRange.Ranges.Max (r => r.End);

    /// <summary>
    ///     Specifies the starting offset for the character map. The default is 0x2500 which is the Box Drawing
    ///     characters.
    /// </summary>
    public int SelectedCodePoint
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;

            if (IsInitialized)
            {
                int row = SelectedCodePoint / 16 * _rowHeight;
                int col = SelectedCodePoint % 16 * COLUMN_WIDTH;

                if (row - Viewport.Y < 0)
                {
                    // Moving up.
                    Viewport = Viewport with { Y = row };
                }
                else if (row - Viewport.Y >= Viewport.Height)
                {
                    // Moving down.
                    Viewport = Viewport with { Y = row - Viewport.Height };
                }

                int width = Viewport.Width / COLUMN_WIDTH * COLUMN_WIDTH - RowLabelWidth;

                if (col - Viewport.X < 0)
                {
                    // Moving left.
                    Viewport = Viewport with { X = col };
                }
                else if (col - Viewport.X >= width)
                {
                    // Moving right.
                    Viewport = Viewport with { X = col - width };
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
            Viewport = Viewport with { Y = SelectedCodePoint / 16 * _rowHeight };
            SetNeedsDisplay ();
        }
    }

    private static int RowLabelWidth => $"U+{MaxCodePoint:x5}".Length + 1;
    private static int RowWidth => RowLabelWidth + COLUMN_WIDTH * 16;
    public event EventHandler<ListViewItemEventArgs> Hover;

    public override void OnDrawContent (Rectangle viewport)
    {
        if (viewport.Height == 0 || viewport.Width == 0)
        {
            return;
        }

        Clear ();

        int cursorCol = Cursor.X + Viewport.X - RowLabelWidth - 1;
        int cursorRow = Cursor.Y + Viewport.Y - 1;

        Driver.SetAttribute (GetHotNormalColor ());
        Move (0, 0);
        Driver.AddStr (new (' ', RowLabelWidth + 1));

        int firstColumnX = RowLabelWidth - Viewport.X;

        // Header
        for (var hexDigit = 0; hexDigit < 16; hexDigit++)
        {
            int x = firstColumnX + hexDigit * COLUMN_WIDTH;

            if (x > RowLabelWidth - 2)
            {
                Move (x, 0);
                Driver.SetAttribute (GetHotNormalColor ());
                Driver.AddStr (" ");
                Driver.SetAttribute (HasFocus && cursorCol + firstColumnX == x ? ColorScheme.HotFocus : GetHotNormalColor ());
                Driver.AddStr ($"{hexDigit:x}");
                Driver.SetAttribute (GetHotNormalColor ());
                Driver.AddStr (" ");
            }
        }

        // Even though the Clip is set to prevent us from drawing on the row potentially occupied by the horizontal
        // scroll bar, we do the smart thing and not actually draw that row if not necessary.
        for (var y = 1; y < Viewport.Height; y++)
        {
            // What row is this?
            int row = (y + Viewport.Y - 1) / _rowHeight;

            int val = row * 16;

            if (val > MaxCodePoint)
            {
                break;
            }

            Move (firstColumnX + COLUMN_WIDTH, y);
            Driver.SetAttribute (GetNormalColor ());

            for (var col = 0; col < 16; col++)
            {
                int x = firstColumnX + COLUMN_WIDTH * col + 1;

                if (x < 0 || x > Viewport.Width - 1)
                {
                    continue;
                }

                Move (x, y);

                // If we're at the cursor position, and we don't have focus, invert the colors.
                if (row == cursorRow && x == cursorCol && !HasFocus)
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

                if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
                {
                    // Draw the rune
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
                    // Draw the width of the rune
                    Driver.SetAttribute (ColorScheme.HotNormal);
                    Driver.AddStr ($"{width}");
                }

                // If we're at the cursor position, and we don't have focus, revert the colors to normal
                if (row == cursorRow && x == cursorCol && !HasFocus)
                {
                    Driver.SetAttribute (GetNormalColor ());
                }
            }

            // Draw row label (U+XXXX_)
            Move (0, y);

            Driver.SetAttribute (HasFocus && y + Viewport.Y - 1 == cursorRow ? ColorScheme.HotFocus : ColorScheme.HotNormal);

            if (!ShowGlyphWidths || (y + Viewport.Y) % _rowHeight > 0)
            {
                Driver.AddStr ($"U+{val / 16:x5}_ ");
            }
            else
            {
                Driver.AddStr (new (' ', RowLabelWidth));
            }
        }
    }

    public override Point? PositionCursor ()
    {
        if (HasFocus
            && Cursor.X >= RowLabelWidth
            && Cursor.X < Viewport.Width
            && Cursor.Y > 0
            && Cursor.Y < Viewport.Height)
        {
            Move (Cursor.X, Cursor.Y);
        }
        else
        {
            return null;
        }

        return Cursor;
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

    private void Handle_MouseClick (object sender, MouseEventArgs me)
    {
        if (me.Flags != MouseFlags.ReportMousePosition && me.Flags != MouseFlags.Button1Clicked && me.Flags != MouseFlags.Button1DoubleClicked)
        {
            return;
        }

        if (me.Position.Y == 0)
        {
            me.Position = me.Position with { Y = Cursor.Y };
        }

        if (me.Position.X < RowLabelWidth || me.Position.X > RowLabelWidth + 16 * COLUMN_WIDTH - 1)
        {
            me.Position = me.Position with { X = Cursor.X };
        }

        int row = (me.Position.Y - 1 - -Viewport.Y) / _rowHeight; // -1 for header
        int col = (me.Position.X - RowLabelWidth - -Viewport.X) / COLUMN_WIDTH;

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

        if (!HasFocus && CanFocus)
        {
            SetFocus ();
        }

        me.Handled = true;

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

            _contextMenu = new ()
            {
                Position = new (me.Position.X + 1, me.Position.Y + 1)
            };

            MenuBarItem menuItems = new (
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
                                        );
            _contextMenu.Show (menuItems);
        }
    }

    private void ShowDetails ()
    {
        var client = new UcdApiClient ();
        var decResponse = string.Empty;
        var getCodePointError = string.Empty;

        var waitIndicator = new Dialog
        {
            Title = "Getting Code Point Information",
            X = Pos.Center (),
            Y = Pos.Center (),
            Height = 7,
            Width = 50,
            Buttons = [new () { Text = "Cancel" }]
        };

        var errorLabel = new Label
        {
            Text = UcdApiClient.BaseUrl,
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            TextAlignment = Alignment.Center
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

            var title = $"{ToCamelCase (name)} - {new Rune (SelectedCodePoint)} U+{SelectedCodePoint:x5}";

            var copyGlyph = new Button { Text = "Copy _Glyph" };
            var copyCP = new Button { Text = "Copy Code _Point" };
            var cancel = new Button { Text = "Cancel" };

            var dlg = new Dialog { Title = title, Buttons = [copyGlyph, copyCP, cancel] };

            copyGlyph.Accepting += (s, a) =>
                                {
                                    CopyGlyph ();
                                    dlg.RequestStop ();
                                };

            copyCP.Accepting += (s, a) =>
                             {
                                 CopyCodePoint ();
                                 dlg.RequestStop ();
                             };
            cancel.Accepting += (s, a) => dlg.RequestStop ();

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
                    $"Code Point Information from {UcdApiClient.BaseUrl}codepoint/dec/{SelectedCodePoint}:",
                X = 0,
                Y = Pos.Bottom (label)
            };
            dlg.Add (label);

            var json = new TextView ()
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
