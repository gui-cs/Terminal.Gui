#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UICatalog.Scenarios;

/// <summary>
///     This Scenario demonstrates building a custom control (a class deriving from View) that: - Provides a
///     "Character Map" application (like Windows' charmap.exe). - Helps test unicode character rendering in Terminal.Gui -
///     Illustrates how to do infinite scrolling
/// </summary>
[ScenarioMetadata ("Character Map", "Unicode viewer. Demos infinite content drawing and scrolling.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Drawing")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Layout")]
[ScenarioCategory ("Scrolling")]
public class CharacterMap : Scenario
{
    private Label? _errorLabel;
    private TableView? _categoryList;
    private CharMap? _charMap;

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
            Y = 1,
            Height = Dim.Fill (),
           // SchemeName = "Base"

        };
        top.Add (_charMap);

        var jumpLabel = new Label
        {
            X = Pos.Right (_charMap) + 1,
            Y = Pos.Y (_charMap),
            HotKeySpecifier = (Rune)'_',
            Text = "_Jump To:",
            //SchemeName = "Dialog"
        };
        top.Add (jumpLabel);

        var jumpEdit = new TextField
        {
            X = Pos.Right (jumpLabel) + 1,
            Y = Pos.Y (_charMap),
            Width = 17,
            Caption = "e.g. 01BE3 or ✈",
            //SchemeName = "Dialog"
        };
        top.Add (jumpEdit);

        _charMap.SelectedCodePointChanged += (sender, args) =>
                                             {
                                                 if (Rune.IsValid (args.Value))
                                                 {
                                                     jumpEdit.Text = ((Rune)args.Value).ToString ();
                                                 }
                                                 else
                                                 {
                                                     jumpEdit.Text = $"U+{args.Value:x5}";
                                                 }
                                             };

        _errorLabel = new ()
        {
            X = Pos.Right (jumpEdit) + 1,
            Y = Pos.Y (_charMap),
            SchemeName = "error",
            Text = "err",
            Visible = false
        };
        top.Add (_errorLabel);

        jumpEdit.Accepting += JumpEditOnAccept;

        _categoryList = new () { 
            X = Pos.Right (_charMap), 
            Y = Pos.Bottom (jumpLabel), 
            Height = Dim.Fill (),
            //SchemeName = "Dialog"
        };
        _categoryList.FullRowSelect = true;
        _categoryList.MultiSelect = false;

        _categoryList.Style.ShowVerticalCellLines = false;
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
                                                 jumpEdit.Text = $"U+{_charMap.SelectedCodePoint:x5}";
                                             };

        top.Add (_categoryList);

        var menu = new MenuBarv2
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItemv2 []
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
                     new MenuItemv2 [] { CreateMenuShowWidth () }
                    )
            ]
        };
        top.Add (menu);

        _charMap.Width = Dim.Fill (Dim.Func (v => v!.Frame.Width, _categoryList));

        _charMap.SelectedCodePoint = 0;
        _charMap.SetFocus ();

        Application.Run (top);
        top.Dispose ();
        Application.Shutdown ();

        return;

        void JumpEditOnAccept (object? sender, CommandEventArgs e)
        {
            if (jumpEdit.Text.Length == 0)
            {
                return;
            }

            _errorLabel.Visible = true;

            uint result = 0;

            if (jumpEdit.Text.Length == 1)
            {
                result = (uint)jumpEdit.Text.ToRunes () [0].Value;
            }
            else if (jumpEdit.Text.StartsWith ("U+", StringComparison.OrdinalIgnoreCase) || jumpEdit.Text.StartsWith ("\\u"))
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

            _errorLabel.Visible = false;

            EnumerableTableSource<UnicodeRange> table = (EnumerableTableSource<UnicodeRange>)_categoryList!.Table;

            _categoryList.SelectedRow = table.Data
                                             .Select ((item, index) => new { item, index })
                                             .FirstOrDefault (x => x.item.Start <= result && x.item.End >= result)
                                             ?.index
                                        ?? -1;
            _categoryList.EnsureSelectedCellIsVisible ();

            // Ensure the typed glyph is selected 
            _charMap.SelectedCodePoint = (int)result;
            _charMap.SetFocus ();

            // Cancel the event to prevent ENTER from being handled elsewhere
            e.Handled = true;
        }
    }

    private EnumerableTableSource<UnicodeRange> CreateCategoryTable (int sortByColumn, bool descending)
    {
        Func<UnicodeRange, object> orderBy;
        var categorySort = string.Empty;
        var startSort = string.Empty;
        var endSort = string.Empty;

        string sortIndicator = descending ? Glyphs.DownArrow.ToString () : Glyphs.UpArrow.ToString ();

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

    private MenuItemv2 CreateMenuShowWidth ()
    {
        CheckBox cb = new ()
        {
            Title = "_Show Glyph Width",
            CheckedState = _charMap!.ShowGlyphWidths ? CheckState.Checked : CheckState.None
        };
        var item = new MenuItemv2 { CommandView = cb };
        item.Action += () =>
                       {
                           if (_charMap is { })
                           {
                               _charMap.ShowGlyphWidths = cb.CheckedState == CheckState.Checked;
                           }
                       };

        return item;
    }

    public override List<Key> GetDemoKeyStrokes ()
    {
        List<Key> keys = new ();

        for (var i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorDown);
        }

        // Category table
        keys.Add (Key.Tab.WithShift);

        // Block elements
        keys.Add (Key.B);
        keys.Add (Key.L);

        keys.Add (Key.Tab);

        for (var i = 0; i < 200; i++)
        {
            keys.Add (Key.CursorLeft);
        }

        return keys;
    }
}
