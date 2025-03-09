using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("TableEditor", "Implements data table editor using the TableView control.")]
[ScenarioCategory ("TableView")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
public class TableEditor : Scenario
{
    private readonly HashSet<FileSystemInfo> _checkedFileSystemInfos = new ();
    private readonly List<IDisposable> _toDispose = new ();

    private readonly List<UnicodeRange> Ranges = new ()
    {
        new (
             0x0000,
             0x001F,
             "ASCII Control Characters"
            ),
        new (0x0080, 0x009F, "C0 Control Characters"),
        new (
             0x1100,
             0x11ff,
             "Hangul Jamo"
            ), // This is where wide chars tend to start
        new (0x20A0, 0x20CF, "Currency Symbols"),
        new (0x2100, 0x214F, "Letterlike Symbols"),
        new (0x2190, 0x21ff, "Arrows"),
        new (0x2200, 0x22ff, "Mathematical symbols"),
        new (
             0x2300,
             0x23ff,
             "Miscellaneous Technical"
            ),
        new (
             0x2500,
             0x25ff,
             "Box Drawing & Geometric Shapes"
            ),
        new (0x2600, 0x26ff, "Miscellaneous Symbols"),
        new (0x2700, 0x27ff, "Dingbats"),
        new (0x2800, 0x28ff, "Braille"),
        new (
             0x2b00,
             0x2bff,
             "Miscellaneous Symbols and Arrows"
            ),
        new (
             0xFB00,
             0xFb4f,
             "Alphabetic Presentation Forms"
            ),
        new (
             0x12400,
             0x1240f,
             "Cuneiform Numbers and Punctuation"
            ),
        new (
             (uint)(Terminal.Gui.UnicodeRange.Ranges.Max (r => r.End) - 16),
             (uint)Terminal.Gui.UnicodeRange.Ranges.Max (r => r.End),
             "End"
            ),
        new (0x0020, 0x007F, "Basic Latin"),
        new (0x00A0, 0x00FF, "Latin-1 Supplement"),
        new (0x0100, 0x017F, "Latin Extended-A"),
        new (0x0180, 0x024F, "Latin Extended-B"),
        new (0x0250, 0x02AF, "IPA Extensions"),
        new (
             0x02B0,
             0x02FF,
             "Spacing Modifier Letters"
            ),
        new (
             0x0300,
             0x036F,
             "Combining Diacritical Marks"
            ),
        new (0x0370, 0x03FF, "Greek and Coptic"),
        new (0x0400, 0x04FF, "Cyrillic"),
        new (0x0500, 0x052F, "Cyrillic Supplementary"),
        new (0x0530, 0x058F, "Armenian"),
        new (0x0590, 0x05FF, "Hebrew"),
        new (0x0600, 0x06FF, "Arabic"),
        new (0x0700, 0x074F, "Syriac"),
        new (0x0780, 0x07BF, "Thaana"),
        new (0x0900, 0x097F, "Devanagari"),
        new (0x0980, 0x09FF, "Bengali"),
        new (0x0A00, 0x0A7F, "Gurmukhi"),
        new (0x0A80, 0x0AFF, "Gujarati"),
        new (0x0B00, 0x0B7F, "Oriya"),
        new (0x0B80, 0x0BFF, "Tamil"),
        new (0x0C00, 0x0C7F, "Telugu"),
        new (0x0C80, 0x0CFF, "Kannada"),
        new (0x0D00, 0x0D7F, "Malayalam"),
        new (0x0D80, 0x0DFF, "Sinhala"),
        new (0x0E00, 0x0E7F, "Thai"),
        new (0x0E80, 0x0EFF, "Lao"),
        new (0x0F00, 0x0FFF, "Tibetan"),
        new (0x1000, 0x109F, "Myanmar"),
        new (0x10A0, 0x10FF, "Georgian"),
        new (0x1100, 0x11FF, "Hangul Jamo"),
        new (0x1200, 0x137F, "Ethiopic"),
        new (0x13A0, 0x13FF, "Cherokee"),
        new (
             0x1400,
             0x167F,
             "Unified Canadian Aboriginal Syllabics"
            ),
        new (0x1680, 0x169F, "Ogham"),
        new (0x16A0, 0x16FF, "Runic"),
        new (0x1700, 0x171F, "Tagalog"),
        new (0x1720, 0x173F, "Hanunoo"),
        new (0x1740, 0x175F, "Buhid"),
        new (0x1760, 0x177F, "Tagbanwa"),
        new (0x1780, 0x17FF, "Khmer"),
        new (0x1800, 0x18AF, "Mongolian"),
        new (0x1900, 0x194F, "Limbu"),
        new (0x1950, 0x197F, "Tai Le"),
        new (0x19E0, 0x19FF, "Khmer Symbols"),
        new (0x1D00, 0x1D7F, "Phonetic Extensions"),
        new (
             0x1E00,
             0x1EFF,
             "Latin Extended Additional"
            ),
        new (0x1F00, 0x1FFF, "Greek Extended"),
        new (0x2000, 0x206F, "General Punctuation"),
        new (
             0x2070,
             0x209F,
             "Superscripts and Subscripts"
            ),
        new (0x20A0, 0x20CF, "Currency Symbols"),
        new (
             0x20D0,
             0x20FF,
             "Combining Diacritical Marks for Symbols"
            ),
        new (0x2100, 0x214F, "Letterlike Symbols"),
        new (0x2150, 0x218F, "Number Forms"),
        new (0x2190, 0x21FF, "Arrows"),
        new (0x2200, 0x22FF, "Mathematical Operators"),
        new (
             0x2300,
             0x23FF,
             "Miscellaneous Technical"
            ),
        new (0x2400, 0x243F, "Control Pictures"),
        new (
             0x2440,
             0x245F,
             "Optical Character Recognition"
            ),
        new (0x2460, 0x24FF, "Enclosed Alphanumerics"),
        new (0x2500, 0x257F, "Box Drawing"),
        new (0x2580, 0x259F, "Block Elements"),
        new (0x25A0, 0x25FF, "Geometric Shapes"),
        new (0x2600, 0x26FF, "Miscellaneous Symbols"),
        new (0x2700, 0x27BF, "Dingbats"),
        new (
             0x27C0,
             0x27EF,
             "Miscellaneous Mathematical Symbols-A"
            ),
        new (0x27F0, 0x27FF, "Supplemental Arrows-A"),
        new (0x2800, 0x28FF, "Braille Patterns"),
        new (0x2900, 0x297F, "Supplemental Arrows-B"),
        new (
             0x2980,
             0x29FF,
             "Miscellaneous Mathematical Symbols-B"
            ),
        new (
             0x2A00,
             0x2AFF,
             "Supplemental Mathematical Operators"
            ),
        new (
             0x2B00,
             0x2BFF,
             "Miscellaneous Symbols and Arrows"
            ),
        new (
             0x2E80,
             0x2EFF,
             "CJK Radicals Supplement"
            ),
        new (0x2F00, 0x2FDF, "Kangxi Radicals"),
        new (
             0x2FF0,
             0x2FFF,
             "Ideographic Description Characters"
            ),
        new (
             0x3000,
             0x303F,
             "CJK Symbols and Punctuation"
            ),
        new (0x3040, 0x309F, "Hiragana"),
        new (0x30A0, 0x30FF, "Katakana"),
        new (0x3100, 0x312F, "Bopomofo"),
        new (
             0x3130,
             0x318F,
             "Hangul Compatibility Jamo"
            ),
        new (0x3190, 0x319F, "Kanbun"),
        new (0x31A0, 0x31BF, "Bopomofo Extended"),
        new (
             0x31F0,
             0x31FF,
             "Katakana Phonetic Extensions"
            ),
        new (
             0x3200,
             0x32FF,
             "Enclosed CJK Letters and Months"
            ),
        new (0x3300, 0x33FF, "CJK Compatibility"),
        new (
             0x3400,
             0x4DBF,
             "CJK Unified Ideographs Extension A"
            ),
        new (
             0x4DC0,
             0x4DFF,
             "Yijing Hexagram Symbols"
            ),
        new (0x4E00, 0x9FFF, "CJK Unified Ideographs"),
        new (0xA000, 0xA48F, "Yi Syllables"),
        new (0xA490, 0xA4CF, "Yi Radicals"),
        new (0xAC00, 0xD7AF, "Hangul Syllables"),
        new (0xD800, 0xDB7F, "High Surrogates"),
        new (
             0xDB80,
             0xDBFF,
             "High Private Use Surrogates"
            ),
        new (0xDC00, 0xDFFF, "Low Surrogates"),
        new (0xE000, 0xF8FF, "Private Use Area"),
        new (
             0xF900,
             0xFAFF,
             "CJK Compatibility Ideographs"
            ),
        new (
             0xFB00,
             0xFB4F,
             "Alphabetic Presentation Forms"
            ),
        new (
             0xFB50,
             0xFDFF,
             "Arabic Presentation Forms-A"
            ),
        new (0xFE00, 0xFE0F, "Variation Selectors"),
        new (0xFE20, 0xFE2F, "Combining Half Marks"),
        new (
             0xFE30,
             0xFE4F,
             "CJK Compatibility Forms"
            ),
        new (0xFE50, 0xFE6F, "Small Form Variants"),
        new (
             0xFE70,
             0xFEFF,
             "Arabic Presentation Forms-B"
            ),
        new (
             0xFF00,
             0xFFEF,
             "Halfwidth and Fullwidth Forms"
            ),
        new (0xFFF0, 0xFFFF, "Specials"),
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
        new (
             0x1D300,
             0x1D35F,
             "Tai Xuan Jing Symbols"
            ),
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

    private ColorScheme _alternatingColorScheme;
    private DataTable _currentTable;
    private MenuItem _miAlternatingColors;
    private MenuItem _miAlwaysShowHeaders;
    private MenuItem _miAlwaysUseNormalColorForVerticalCellLines;
    private MenuItem _miBottomline;
    private MenuItem _miCellLines;
    private MenuItem _miCheckboxes;
    private MenuItem _miCursor;
    private MenuItem _miExpandLastColumn;
    private MenuItem _miFullRowSelect;
    private MenuItem _miHeaderMidline;
    private MenuItem _miHeaderOverline;
    private MenuItem _miHeaderUnderline;
    private MenuItem _miRadioboxes;
    private MenuItem _miShowHeaders;
    private MenuItem _miShowHorizontalScrollIndicators;
    private MenuItem _miSmoothScrolling;
    private ColorScheme _redColorScheme;
    private ColorScheme _redColorSchemeAlt;
    private TableView _tableView;

    /// <summary>
    ///     Builds a simple table in which cell values contents are the index of the cell.  This helps testing that
    ///     scrolling etc is working correctly and not skipping out any rows/columns when paging
    /// </summary>
    /// <param name="cols"></param>
    /// <param name="rows"></param>
    /// <returns></returns>
    public static DataTable BuildSimpleDataTable (int cols, int rows)
    {
        var dt = new DataTable ();

        for (var c = 0; c < cols; c++)
        {
            dt.Columns.Add ("Col" + c);
        }

        for (var r = 0; r < rows; r++)
        {
            DataRow newRow = dt.NewRow ();

            for (var c = 0; c < cols; c++)
            {
                newRow [c] = $"R{r}C{c}";
            }

            dt.Rows.Add (newRow);
        }

        return dt;
    }

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Toplevel appWindow = new ();

        _tableView = new () { X = 0, Y = 1, Width = Dim.Fill (), Height = Dim.Fill (1) };

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new (
                              "_OpenBigExample",
                              "",
                              () => OpenExample (true)
                             ),
                         new (
                              "_OpenSmallExample",
                              "",
                              () => OpenExample (false)
                             ),
                         new (
                              "OpenCharacter_Map",
                              "",
                              () => OpenUnicodeMap ()
                             ),
                         new (
                              "OpenTreeExample",
                              "",
                              () => OpenTreeExample ()
                             ),
                         new (
                              "_CloseExample",
                              "",
                              () => CloseExample ()
                             ),
                         new ("_Quit", "", () => Quit ())
                     }
                    ),
                new (
                     "_View",
                     new []
                     {
                         _miShowHeaders =
                             new (
                                  "_ShowHeaders",
                                  "",
                                  () => ToggleShowHeaders ()
                                 )
                             {
                                 Checked = _tableView.Style.ShowHeaders,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         _miAlwaysShowHeaders =
                             new (
                                  "_AlwaysShowHeaders",
                                  "",
                                  () => ToggleAlwaysShowHeaders ()
                                 )
                             {
                                 Checked = _tableView.Style.AlwaysShowHeaders,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         _miHeaderOverline =
                             new (
                                  "_HeaderOverLine",
                                  "",
                                  () => ToggleOverline ()
                                 )
                             {
                                 Checked = _tableView.Style
                                                     .ShowHorizontalHeaderOverline,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         _miHeaderMidline = new (
                                                 "_HeaderMidLine",
                                                 "",
                                                 () => ToggleHeaderMidline ()
                                                )
                         {
                             Checked = _tableView.Style
                                                 .ShowVerticalHeaderLines,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miHeaderUnderline = new (
                                                   "_HeaderUnderLine",
                                                   "",
                                                   () => ToggleUnderline ()
                                                  )
                         {
                             Checked = _tableView.Style
                                                 .ShowHorizontalHeaderUnderline,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miBottomline = new (
                                              "_BottomLine",
                                              "",
                                              () => ToggleBottomline ()
                                             )
                         {
                             Checked = _tableView.Style
                                                 .ShowHorizontalBottomline,
                             CheckType = MenuItemCheckStyle
                                 .Checked
                         },
                         _miShowHorizontalScrollIndicators =
                             new (
                                  "_HorizontalScrollIndicators",
                                  "",
                                  () =>
                                      ToggleHorizontalScrollIndicators ()
                                 )
                             {
                                 Checked = _tableView.Style
                                                     .ShowHorizontalScrollIndicators,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         _miFullRowSelect = new (
                                                 "_FullRowSelect",
                                                 "",
                                                 () => ToggleFullRowSelect ()
                                                )
                         {
                             Checked = _tableView.FullRowSelect,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miCellLines = new (
                                             "_CellLines",
                                             "",
                                             () => ToggleCellLines ()
                                            )
                         {
                             Checked = _tableView.Style
                                                 .ShowVerticalCellLines,
                             CheckType = MenuItemCheckStyle
                                 .Checked
                         },
                         _miExpandLastColumn =
                             new (
                                  "_ExpandLastColumn",
                                  "",
                                  () => ToggleExpandLastColumn ()
                                 )
                             {
                                 Checked = _tableView.Style.ExpandLastColumn,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         _miAlwaysUseNormalColorForVerticalCellLines =
                             new (
                                  "_AlwaysUseNormalColorForVerticalCellLines",
                                  "",
                                  () =>
                                      ToggleAlwaysUseNormalColorForVerticalCellLines ()
                                 )
                             {
                                 Checked = _tableView.Style
                                                     .AlwaysUseNormalColorForVerticalCellLines,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         _miSmoothScrolling =
                             new (
                                  "_SmoothHorizontalScrolling",
                                  "",
                                  () => ToggleSmoothScrolling ()
                                 )
                             {
                                 Checked = _tableView.Style
                                                     .SmoothHorizontalScrolling,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         new ("_AllLines", "", () => ToggleAllCellLines ()),
                         new ("_NoLines", "", () => ToggleNoCellLines ()),
                         _miCheckboxes = new (
                                              "_Checkboxes",
                                              "",
                                              () => ToggleCheckboxes (false)
                                             )
                         {
                             Checked = false,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miRadioboxes = new (
                                              "_Radioboxes",
                                              "",
                                              () => ToggleCheckboxes (true)
                                             )
                         {
                             Checked = false,
                             CheckType = MenuItemCheckStyle.Checked
                         },
                         _miAlternatingColors =
                             new (
                                  "Alternating Colors",
                                  "",
                                  () => ToggleAlternatingColors ()
                                 ) { CheckType = MenuItemCheckStyle.Checked },
                         _miCursor =
                             new (
                                  "Invert Selected Cell First Character",
                                  "",
                                  () =>
                                      ToggleInvertSelectedCellFirstCharacter ()
                                 )
                             {
                                 Checked = _tableView.Style
                                                     .InvertSelectedCellFirstCharacter,
                                 CheckType = MenuItemCheckStyle.Checked
                             },
                         new (
                              "_ClearColumnStyles",
                              "",
                              () => ClearColumnStyles ()
                             ),
                         new ("Sho_w All Columns", "", () => ShowAllColumns ())
                     }
                    ),
                new (
                     "_Column",
                     new MenuItem []
                     {
                         new ("_Set Max Width", "", SetMaxWidth),
                         new ("_Set Min Width", "", SetMinWidth),
                         new (
                              "_Set MinAcceptableWidth",
                              "",
                              SetMinAcceptableWidth
                             ),
                         new (
                              "_Set All MinAcceptableWidth=1",
                              "",
                              SetMinAcceptableWidthToOne
                             )
                     }
                    )
            ]
        };

        appWindow.Add (menu);

        var selectedCellLabel = new Label
        {
            Text = "0,0"
        };

        var statusBar = new StatusBar (
                                       [
                                           new (
                                                Application.QuitKey,
                                                "Quit",
                                                Quit
                                               ),
                                           new (
                                                Key.F2,
                                                "OpenExample",
                                                () => OpenExample (true)
                                               ),
                                           new (
                                                Key.F3,
                                                "CloseExample",
                                                CloseExample
                                               ),
                                           new (
                                                Key.F4,
                                                "OpenSimple",
                                                () => OpenSimple (true)
                                               ),
                                           new ()
                                           {
                                               HelpText = "Cell:",
                                               CommandView = selectedCellLabel
                                           }
                                       ]
                                      )
        {
            AlignmentModes = AlignmentModes.IgnoreFirstOrLast
        };
        appWindow.Add (statusBar);

        appWindow.Add (_tableView);

        _tableView.SelectedCellChanged += (s, e) => { selectedCellLabel.Text = $"{_tableView.SelectedRow},{_tableView.SelectedColumn}"; };
        _tableView.CellActivated += EditCurrentCell;
        _tableView.KeyDown += TableViewKeyPress;

        //SetupScrollBar ();

        _redColorScheme = new ()
        {
            Disabled = appWindow.ColorScheme.Disabled,
            HotFocus = appWindow.ColorScheme.HotFocus,
            Focus = appWindow.ColorScheme.Focus,
            Normal = new (Color.Red, appWindow.ColorScheme.Normal.Background)
        };

        _alternatingColorScheme = new ()
        {
            Disabled = appWindow.ColorScheme.Disabled,
            HotFocus = appWindow.ColorScheme.HotFocus,
            Focus = appWindow.ColorScheme.Focus,
            Normal = new (Color.White, Color.BrightBlue)
        };

        _redColorSchemeAlt = new ()
        {
            Disabled = appWindow.ColorScheme.Disabled,
            HotFocus = appWindow.ColorScheme.HotFocus,
            Focus = appWindow.ColorScheme.Focus,
            Normal = new (Color.Red, Color.BrightBlue)
        };

        // if user clicks the mouse in TableView
        _tableView.MouseClick += (s, e) =>
                                 {
                                     if (_currentTable == null)
                                     {
                                         return;
                                     }

                                     _tableView.ScreenToCell (e.Position, out int? clickedCol);

                                     if (clickedCol != null)
                                     {
                                         if (e.Flags.HasFlag (MouseFlags.Button1Clicked))
                                         {
                                             // left click in a header
                                             SortColumn (clickedCol.Value);
                                         }
                                         else if (e.Flags.HasFlag (MouseFlags.Button3Clicked))
                                         {
                                             // right click in a header
                                             ShowHeaderContextMenu (clickedCol.Value, e);
                                         }
                                     }
                                 };

        _tableView.KeyBindings.ReplaceCommands (Key.Space, Command.Accept);

        // Run - Start the application.
        Application.Run (appWindow);
        appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();
    }

    protected override void Dispose (bool disposing)
    {
        base.Dispose (disposing);

        foreach (IDisposable d in _toDispose)
        {
            d.Dispose ();
        }
    }

    private DataTable BuildUnicodeMap ()
    {
        var dt = new DataTable ();

        // add cols called 0 to 9
        for (var i = 0; i < 10; i++)
        {
            DataColumn col = dt.Columns.Add (i.ToString (), typeof (uint));
            ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (col.Ordinal);
            style.RepresentationGetter = o => new Rune ((uint)o).ToString ();
        }

        // add cols called a to z
        for (int i = 'a'; i < 'a' + 26; i++)
        {
            DataColumn col = dt.Columns.Add (((char)i).ToString (), typeof (uint));
            ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (col.Ordinal);
            style.RepresentationGetter = o => new Rune ((uint)o).ToString ();
        }

        // now add table contents
        List<uint> runes = new ();

        foreach (UnicodeRange range in Ranges)
        {
            for (uint i = range.Start; i <= range.End; i++)
            {
                runes.Add (i);
            }
        }

        DataRow dr = null;

        for (var i = 0; i < runes.Count; i++)
        {
            if (dr == null || i % dt.Columns.Count == 0)
            {
                dr = dt.Rows.Add ();
            }

            dr [i % dt.Columns.Count] = runes [i].ToString ();
        }

        return dt;
    }

    private void CheckOrUncheckFile (FileSystemInfo info, bool check)
    {
        if (check)
        {
            _checkedFileSystemInfos.Add (info);
        }
        else
        {
            _checkedFileSystemInfos.Remove (info);
        }
    }

    private void ClearColumnStyles ()
    {
        _tableView.Style.ColumnStyles.Clear ();
        _tableView.Update ();
    }

    private void CloseExample () { _tableView.Table = null; }

    private void EditCurrentCell (object sender, CellActivatedEventArgs e)
    {
        if (e.Table is not DataTableSource || _currentTable == null)
        {
            return;
        }

        int tableCol = ToTableCol (e.Col);

        if (tableCol < 0)
        {
            return;
        }

        object o = _currentTable.Rows [e.Row] [tableCol];

        string title = o is uint u ? GetUnicodeCategory (u) + $"(0x{o:X4})" : "Enter new value";

        var oldValue = _currentTable.Rows [e.Row] [tableCol].ToString ();
        var okPressed = false;

        var ok = new Button { Text = "Ok", IsDefault = true };

        ok.Accepting += (s, e) =>
                     {
                         okPressed = true;
                         Application.RequestStop ();
                     };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accepting += (s, e) => { Application.RequestStop (); };
        var d = new Dialog { Title = title, Buttons = [ok, cancel] };

        var lbl = new Label { X = 0, Y = 1, Text = _tableView.Table.ColumnNames [e.Col] };

        var tf = new TextField { Text = oldValue, X = 0, Y = 2, Width = Dim.Fill () };

        d.Add (lbl, tf);
        tf.SetFocus ();

        Application.Run (d);
        d.Dispose ();

        if (okPressed)
        {
            try
            {
                _currentTable.Rows [e.Row] [tableCol] =
                    string.IsNullOrWhiteSpace (tf.Text) ? DBNull.Value : tf.Text;
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery (60, 20, "Failed to set text", ex.Message, "Ok");
            }

            _tableView.Update ();
        }
    }

    private IEnumerable<FileSystemInfo> GetChildren (FileSystemInfo arg)
    {
        try
        {
            return arg is DirectoryInfo d ? d.GetFileSystemInfos () : Enumerable.Empty<FileSystemInfo> ();
        }
        catch (Exception)
        {
            // Permission denied etc
            return Enumerable.Empty<FileSystemInfo> ();
        }
    }

    private int? GetColumn ()
    {
        if (_tableView.Table == null)
        {
            return null;
        }

        if (_tableView.SelectedColumn < 0 || _tableView.SelectedColumn > _tableView.Table.Columns)
        {
            return null;
        }

        return _tableView.SelectedColumn;
    }

    private string GetHumanReadableFileSize (FileSystemInfo fsi)
    {
        if (fsi is not FileInfo fi)
        {
            return null;
        }

        long value = fi.Length;
        CultureInfo culture = CultureInfo.CurrentUICulture;

        return GetHumanReadableFileSize (value, culture);
    }

    private string GetHumanReadableFileSize (long value, CultureInfo culture)
    {
        const long ByteConversion = 1024;
        string [] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        if (value < 0)
        {
            return "-" + GetHumanReadableFileSize (-value, culture);
        }

        if (value == 0)
        {
            return "0.0 bytes";
        }

        var mag = (int)Math.Log (value, ByteConversion);
        double adjustedSize = value / Math.Pow (1000, mag);

        return string.Format (culture.NumberFormat, "{0:n2} {1}", adjustedSize, SizeSuffixes [mag]);
    }

    private string GetProposedNewSortOrder (int clickedCol, out bool isAsc)
    {
        // work out new sort order
        string sort = _currentTable.DefaultView.Sort;
        string colName = _tableView.Table.ColumnNames [clickedCol];

        if (sort?.EndsWith ("ASC") ?? false)
        {
            sort = $"{colName} DESC";
            isAsc = false;
        }
        else
        {
            sort = $"{colName} ASC";
            isAsc = true;
        }

        return sort;
    }

    private string GetUnicodeCategory (uint u) { return Ranges.FirstOrDefault (r => u >= r.Start && u <= r.End)?.Category ?? "Unknown"; }
    private bool HasCheckboxes () { return _tableView.Table is CheckBoxTableSourceWrapperBase; }

    private void HideColumn (int clickedCol)
    {
        ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (clickedCol);
        style.Visible = false;
        _tableView.Update ();
    }

    private void OpenExample (bool big)
    {
        SetTable (TableView.BuildDemoDataTable (big ? 30 : 5, big ? 1000 : 5));
        SetDemoTableStyles ();
    }

    private void OpenSimple (bool big) { SetTable (BuildSimpleDataTable (big ? 30 : 5, big ? 1000 : 5)); }

    private void OpenTreeExample ()
    {
        _tableView.Style.ColumnStyles.Clear ();

        TreeView<FileSystemInfo> tree = new ()
        {
            AspectGetter = f => f.Name, TreeBuilder = new DelegateTreeBuilder<FileSystemInfo> (GetChildren)
        };

        TreeTableSource<FileSystemInfo> source = new (
                                                      _tableView,
                                                      "Name",
                                                      tree,
                                                      new ()
                                                      {
                                                          { "Extension", f => f.Extension },
                                                          { "CreationTime", f => f.CreationTime },
                                                          { "FileSize", GetHumanReadableFileSize }
                                                      }
                                                     );

        HashSet<string> seen = new ();

        try
        {
            foreach (string path in Environment.GetLogicalDrives ())
            {
                tree.AddObject (new DirectoryInfo (path));
            }
        }
        catch (Exception e)
        {
            MessageBox.ErrorQuery ("Could not find local drives", e.Message, "Ok");
        }

        _tableView.Table = source;

        _toDispose.Add (tree);
    }

    private void OpenUnicodeMap ()
    {
        SetTable (BuildUnicodeMap ());
        _tableView.Update ();
    }

    private void Quit () { Application.RequestStop (); }

    private void RunColumnWidthDialog (
        int? col,
        string prompt,
        Action<ColumnStyle, int> setter,
        Func<ColumnStyle, int> getter
    )
    {
        if (col == null)
        {
            return;
        }

        var accepted = false;
        var ok = new Button { Text = "Ok", IsDefault = true };

        ok.Accepting += (s, e) =>
                     {
                         accepted = true;
                         Application.RequestStop ();
                     };
        var cancel = new Button { Text = "Cancel" };
        cancel.Accepting += (s, e) => { Application.RequestStop (); };
        var d = new Dialog
        {
            Title = prompt,
            Buttons = [ok, cancel]
        };

        ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (col.Value);

        var lbl = new Label { X = 0, Y = 0, Text = $"{_tableView.Table.ColumnNames [col.Value]}: " };
        var tf = new TextField { Text = getter (style).ToString (), X = Pos.Right (lbl), Y = 0, Width = 20 };

        d.Add (lbl, tf);
        tf.SetFocus ();

        Application.Run (d);
        d.Dispose ();

        if (accepted)
        {
            try
            {
                setter (style, int.Parse (tf.Text));
            }
            catch (Exception ex)
            {
                MessageBox.ErrorQuery (60, 20, "Failed to set", ex.Message, "Ok");
            }

            _tableView.Update ();
        }
    }

    private void SetDemoTableStyles ()
    {
        _tableView.Style.ColumnStyles.Clear ();

        var alignMid = new ColumnStyle { Alignment = Alignment.Center };
        var alignRight = new ColumnStyle { Alignment = Alignment.End };

        var dateFormatStyle = new ColumnStyle
        {
            Alignment = Alignment.End,
            RepresentationGetter = v =>
                                       v is DateTime d ? d.ToString ("yyyy-MM-dd") : v.ToString ()
        };

        var negativeRight = new ColumnStyle
        {
            Format = "0.##",
            MinWidth = 10,
            AlignmentGetter = v => v is double d
                                       ?

                                       // align negative values right
                                       d < 0
                                           ? Alignment.End
                                           :

                                           // align positive values left
                                           Alignment.Start
                                       :

                                       // not a double
                                       Alignment.Start,
            ColorGetter = a => a.CellValue is double d
                                   ?

                                   // color 0 and negative values red
                                   d <= 0.0000001
                                       ? a.RowIndex % 2 == 0
                                         && _miAlternatingColors.Checked == true
                                             ? _redColorSchemeAlt
                                             : _redColorScheme
                                       :

                                       // use normal scheme for positive values
                                       null
                                   :

                                   // not a double
                                   null
        };

        _tableView.Style.ColumnStyles.Add (_currentTable.Columns ["DateCol"].Ordinal, dateFormatStyle);
        _tableView.Style.ColumnStyles.Add (_currentTable.Columns ["DoubleCol"].Ordinal, negativeRight);
        _tableView.Style.ColumnStyles.Add (_currentTable.Columns ["NullsCol"].Ordinal, alignMid);
        _tableView.Style.ColumnStyles.Add (_currentTable.Columns ["IntCol"].Ordinal, alignRight);

        _tableView.Update ();
    }

    private void SetMaxWidth ()
    {
        int? col = GetColumn ();
        RunColumnWidthDialog (col, "MaxWidth", (s, v) => s.MaxWidth = v, s => s.MaxWidth);
    }

    private void SetMinAcceptableWidth ()
    {
        int? col = GetColumn ();

        RunColumnWidthDialog (
                              col,
                              "MinAcceptableWidth",
                              (s, v) => s.MinAcceptableWidth = v,
                              s => s.MinAcceptableWidth
                             );
    }

    private void SetMinAcceptableWidthToOne ()
    {
        for (var i = 0; i < _tableView.Table.Columns; i++)
        {
            ColumnStyle style = _tableView.Style.GetOrCreateColumnStyle (i);
            style.MinAcceptableWidth = 1;
        }
    }

    private void SetMinWidth ()
    {
        int? col = GetColumn ();
        RunColumnWidthDialog (col, "MinWidth", (s, v) => s.MinWidth = v, s => s.MinWidth);
    }

    private void SetTable (DataTable dataTable) { _tableView.Table = new DataTableSource (_currentTable = dataTable); }

    //private void SetupScrollBar ()
    //{
    //    var scrollBar = new ScrollBarView (_tableView, true);

    //    scrollBar.ChangedPosition += (s, e) =>
    //                                 {
    //                                     _tableView.RowOffset = scrollBar.Position;

    //                                     if (_tableView.RowOffset != scrollBar.Position)
    //                                     {
    //                                         scrollBar.Position = _tableView.RowOffset;
    //                                     }

    //                                     _tableView.SetNeedsDraw ();
    //                                 };
    //    /*
    //    scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
    //        tableView.LeftItem = scrollBar.OtherScrollBarView.Position;
    //        if (tableView.LeftItem != scrollBar.OtherScrollBarView.Position) {
    //            scrollBar.OtherScrollBarView.Position = tableView.LeftItem;
    //        }
    //        tableView.SetNeedsDraw ();
    //    };*/

    //    _tableView.DrawingContent += (s, e) =>
    //                              {
    //                                  scrollBar.Size = _tableView.Table?.Rows ?? 0;
    //                                  scrollBar.Position = _tableView.RowOffset;

    //                                  //scrollBar.OtherScrollBarView.Size = tableView.Maxlength - 1;
    //                                  //scrollBar.OtherScrollBarView.Position = tableView.LeftItem;
    //                                  scrollBar.Refresh ();
    //                              };
    //}

    private void ShowAllColumns ()
    {
        foreach (KeyValuePair<int, ColumnStyle> colStyle in _tableView.Style.ColumnStyles)
        {
            colStyle.Value.Visible = true;
        }

        _tableView.Update ();
    }

    private void ShowHeaderContextMenu (int clickedCol, MouseEventArgs e)
    {
        if (HasCheckboxes () && clickedCol == 0)
        {
            return;
        }

        string sort = GetProposedNewSortOrder (clickedCol, out bool isAsc);
        string colName = _tableView.Table.ColumnNames [clickedCol];

        var contextMenu = new ContextMenu
        {
            Position = new (e.Position.X + 1, e.Position.Y + 1)
        };

        MenuBarItem menuItems = new (
                                     [
                                         new (
                                              $"Hide {TrimArrows (colName)}",
                                              "",
                                              () => HideColumn (clickedCol)
                                             ),
                                         new (
                                              $"Sort {StripArrows (sort)}",
                                              "",
                                              () => SortColumn (
                                                                clickedCol,
                                                                sort,
                                                                isAsc
                                                               )
                                             )
                                     ]
                                    );
        contextMenu.Show (menuItems);
    }

    private void SortColumn (int clickedCol)
    {
        string sort = GetProposedNewSortOrder (clickedCol, out bool isAsc);

        // don't try to sort on the toggled column
        if (HasCheckboxes () && clickedCol == 0)
        {
            return;
        }

        SortColumn (clickedCol, sort, isAsc);
    }

    private void SortColumn (int clickedCol, string sort, bool isAsc)
    {
        if (_currentTable == null)
        {
            return;
        }

        // set a sort order
        _currentTable.DefaultView.Sort = sort;

        // copy the rows from the view
        DataTable sortedCopy = _currentTable.DefaultView.ToTable ();
        _currentTable.Rows.Clear ();

        foreach (DataRow r in sortedCopy.Rows)
        {
            _currentTable.ImportRow (r);
        }

        foreach (DataColumn col in _currentTable.Columns)
        {
            // remove any lingering sort indicator
            col.ColumnName = TrimArrows (col.ColumnName);

            // add a new one if this the one that is being sorted
            if (col.Ordinal == clickedCol)
            {
                col.ColumnName += isAsc ? Glyphs.UpArrow : Glyphs.DownArrow;
            }
        }

        _tableView.Update ();
    }

    private string StripArrows (string columnName) { return columnName.Replace ($"{Glyphs.DownArrow}", "").Replace ($"{Glyphs.UpArrow}", ""); }

    private void TableViewKeyPress (object sender, Key e)
    {
        if (_currentTable == null)
        {
            return;
        }

        if (e.KeyCode == KeyCode.Delete)
        {
            if (_tableView.FullRowSelect)
            {
                // Delete button deletes all rows when in full row mode
                foreach (int toRemove in _tableView.GetAllSelectedCells ()
                                                   .Select (p => p.Y)
                                                   .Distinct ()
                                                   .OrderByDescending (i => i))
                {
                    _currentTable.Rows.RemoveAt (toRemove);
                }
            }
            else
            {
                // otherwise set all selected cells to null
                foreach (Point pt in _tableView.GetAllSelectedCells ())
                {
                    _currentTable.Rows [pt.Y] [pt.X] = DBNull.Value;
                }
            }

            _tableView.Update ();
            e.Handled = true;
        }
    }

    private void ToggleAllCellLines ()
    {
        _tableView.Style.ShowHorizontalHeaderOverline = true;
        _tableView.Style.ShowVerticalHeaderLines = true;
        _tableView.Style.ShowHorizontalHeaderUnderline = true;
        _tableView.Style.ShowVerticalCellLines = true;

        _miHeaderOverline.Checked = true;
        _miHeaderMidline.Checked = true;
        _miHeaderUnderline.Checked = true;
        _miCellLines.Checked = true;

        _tableView.Update ();
    }

    private void ToggleAlternatingColors ()
    {
        //toggle menu item
        _miAlternatingColors.Checked = !_miAlternatingColors.Checked;

        if (_miAlternatingColors.Checked == true)
        {
            _tableView.Style.RowColorGetter = a => { return a.RowIndex % 2 == 0 ? _alternatingColorScheme : null; };
        }
        else
        {
            _tableView.Style.RowColorGetter = null;
        }

        _tableView.SetNeedsDraw ();
    }

    private void ToggleAlwaysShowHeaders ()
    {
        _miAlwaysShowHeaders.Checked = !_miAlwaysShowHeaders.Checked;
        _tableView.Style.AlwaysShowHeaders = (bool)_miAlwaysShowHeaders.Checked;
        _tableView.Update ();
    }

    private void ToggleAlwaysUseNormalColorForVerticalCellLines ()
    {
        _miAlwaysUseNormalColorForVerticalCellLines.Checked =
            !_miAlwaysUseNormalColorForVerticalCellLines.Checked;

        _tableView.Style.AlwaysUseNormalColorForVerticalCellLines =
            (bool)_miAlwaysUseNormalColorForVerticalCellLines.Checked;

        _tableView.Update ();
    }

    private void ToggleBottomline ()
    {
        _miBottomline.Checked = !_miBottomline.Checked;
        _tableView.Style.ShowHorizontalBottomline = (bool)_miBottomline.Checked;
        _tableView.Update ();
    }

    private void ToggleCellLines ()
    {
        _miCellLines.Checked = !_miCellLines.Checked;
        _tableView.Style.ShowVerticalCellLines = (bool)_miCellLines.Checked;
        _tableView.Update ();
    }

    private void ToggleCheckboxes (bool radio)
    {
        if (_tableView.Table is CheckBoxTableSourceWrapperBase wrapper)
        {
            // unwrap it to remove check boxes
            _tableView.Table = wrapper.Wrapping;

            _miCheckboxes.Checked = false;
            _miRadioboxes.Checked = false;

            // if toggling off checkboxes/radio
            if (wrapper.UseRadioButtons == radio)
            {
                return;
            }
        }

        ITableSource source;

        // Either toggling on checkboxes/radio or switching from radio to checkboxes (or vice versa)
        if (_tableView.Table is TreeTableSource<FileSystemInfo> treeSource)
        {
            source = new CheckBoxTableSourceWrapperByObject<FileSystemInfo> (
                                                                             _tableView,
                                                                             treeSource,
                                                                             _checkedFileSystemInfos.Contains,
                                                                             CheckOrUncheckFile
                                                                            )
            { UseRadioButtons = radio };
        }
        else
        {
            source = new CheckBoxTableSourceWrapperByIndex (_tableView, _tableView.Table) { UseRadioButtons = radio };
        }

        _tableView.Table = source;

        if (radio)
        {
            _miRadioboxes.Checked = true;
            _miCheckboxes.Checked = false;
        }
        else
        {
            _miRadioboxes.Checked = false;
            _miCheckboxes.Checked = true;
        }
    }

    private void ToggleExpandLastColumn ()
    {
        _miExpandLastColumn.Checked = !_miExpandLastColumn.Checked;
        _tableView.Style.ExpandLastColumn = (bool)_miExpandLastColumn.Checked;

        _tableView.Update ();
    }

    private void ToggleFullRowSelect ()
    {
        _miFullRowSelect.Checked = !_miFullRowSelect.Checked;
        _tableView.FullRowSelect = (bool)_miFullRowSelect.Checked;
        _tableView.Update ();
    }

    private void ToggleHeaderMidline ()
    {
        _miHeaderMidline.Checked = !_miHeaderMidline.Checked;
        _tableView.Style.ShowVerticalHeaderLines = (bool)_miHeaderMidline.Checked;
        _tableView.Update ();
    }

    private void ToggleHorizontalScrollIndicators ()
    {
        _miShowHorizontalScrollIndicators.Checked = !_miShowHorizontalScrollIndicators.Checked;
        _tableView.Style.ShowHorizontalScrollIndicators = (bool)_miShowHorizontalScrollIndicators.Checked;
        _tableView.Update ();
    }

    private void ToggleInvertSelectedCellFirstCharacter ()
    {
        //toggle menu item
        _miCursor.Checked = !_miCursor.Checked;
        _tableView.Style.InvertSelectedCellFirstCharacter = (bool)_miCursor.Checked;
        _tableView.SetNeedsDraw ();
    }

    private void ToggleNoCellLines ()
    {
        _tableView.Style.ShowHorizontalHeaderOverline = false;
        _tableView.Style.ShowVerticalHeaderLines = false;
        _tableView.Style.ShowHorizontalHeaderUnderline = false;
        _tableView.Style.ShowVerticalCellLines = false;

        _miHeaderOverline.Checked = false;
        _miHeaderMidline.Checked = false;
        _miHeaderUnderline.Checked = false;
        _miCellLines.Checked = false;

        _tableView.Update ();
    }

    private void ToggleOverline ()
    {
        _miHeaderOverline.Checked = !_miHeaderOverline.Checked;
        _tableView.Style.ShowHorizontalHeaderOverline = (bool)_miHeaderOverline.Checked;
        _tableView.Update ();
    }

    private void ToggleShowHeaders ()
    {
        _miShowHeaders.Checked = !_miShowHeaders.Checked;
        _tableView.Style.ShowHeaders = (bool)_miShowHeaders.Checked;
        _tableView.Update ();
    }

    private void ToggleSmoothScrolling ()
    {
        _miSmoothScrolling.Checked = !_miSmoothScrolling.Checked;
        _tableView.Style.SmoothHorizontalScrolling = (bool)_miSmoothScrolling.Checked;

        _tableView.Update ();
    }

    private void ToggleUnderline ()
    {
        _miHeaderUnderline.Checked = !_miHeaderUnderline.Checked;
        _tableView.Style.ShowHorizontalHeaderUnderline = (bool)_miHeaderUnderline.Checked;
        _tableView.Update ();
    }

    private int ToTableCol (int col)
    {
        if (HasCheckboxes ())
        {
            return col - 1;
        }

        return col;
    }

    private string TrimArrows (string columnName)
    {
        return columnName.TrimEnd (
                                   (char)Glyphs.UpArrow.Value,
                                   (char)Glyphs.DownArrow.Value
                                  );
    }

    public class UnicodeRange (uint start, uint end, string category)
    {
        public readonly string Category = category;
        public readonly uint End = end;
        public readonly uint Start = start;
    }
}
