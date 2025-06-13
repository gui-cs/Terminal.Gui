using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static UICatalog.Scenarios.DynamicMenuBar;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Editor", "A Text Editor using the TextView control.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Dialogs")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Arrangement")]
[ScenarioCategory ("Files and IO")]
[ScenarioCategory ("TextView")]
[ScenarioCategory ("Menus")]
public class Editor : Scenario
{
    private Window _appWindow;
    private List<CultureInfo> _cultureInfos;
    private string _fileName = "demo.txt";
    private bool _forceMinimumPosToZero = true;
    private bool _matchCase;
    private bool _matchWholeWord;
    private MenuItem _miForceMinimumPosToZero;
    private byte [] _originalText;
    private bool _saved = true;
    private TabView _tabView;
    private string _textToFind;
    private string _textToReplace;
    private TextView _textView;
    private FindReplaceWindow _findReplaceWindow;

    public override void Main ()
    {
        // Init
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        _appWindow = new ()
        {
            //Title = GetQuitKeyAndName (),
            Title = _fileName ?? "Untitled",
            BorderStyle = LineStyle.None
        };

        _cultureInfos = Application.SupportedCultures;

        _textView = new ()
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
        };

        CreateDemoFile (_fileName);

        LoadFile ();

        _appWindow.Add (_textView);

        var menu = new MenuBar
        {
            Menus =
            [
                new (
                     "_File",
                     new MenuItem []
                     {
                         new ("_New", "", () => New ()),
                         new ("_Open", "", () => Open ()),
                         new ("_Save", "", () => Save ()),
                         new ("_Save As", "", () => SaveAs ()),
                         new ("_Close", "", () => CloseFile ()),
                         null,
                         new ("_Quit", "", () => Quit ())
                     }
                    ),
                new (
                     "_Edit",
                     new MenuItem []
                     {
                         new (
                              "_Copy",
                              "",
                              () => Copy (),
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.C
                             ),
                         new (
                              "C_ut",
                              "",
                              () => Cut (),
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.W
                             ),
                         new (
                              "_Paste",
                              "",
                              () => Paste (),
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.Y
                             ),
                         null,
                         new (
                              "_Find",
                              "",
                              () => Find (),
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.S
                             ),
                         new (
                              "Find _Next",
                              "",
                              () => FindNext (),
                              null,
                              null,
                              KeyCode.CtrlMask
                              | KeyCode.ShiftMask
                              | KeyCode.S
                             ),
                         new (
                              "Find P_revious",
                              "",
                              () => FindPrevious (),
                              null,
                              null,
                              KeyCode.CtrlMask
                              | KeyCode.ShiftMask
                              | KeyCode.AltMask
                              | KeyCode.S
                             ),
                         new (
                              "_Replace",
                              "",
                              () => Replace (),
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.R
                             ),
                         new (
                              "Replace Ne_xt",
                              "",
                              () => ReplaceNext (),
                              null,
                              null,
                              KeyCode.CtrlMask
                              | KeyCode.ShiftMask
                              | KeyCode.R
                             ),
                         new (
                              "Replace Pre_vious",
                              "",
                              () => ReplacePrevious (),
                              null,
                              null,
                              KeyCode.CtrlMask
                              | KeyCode.ShiftMask
                              | KeyCode.AltMask
                              | KeyCode.R
                             ),
                         new (
                              "Replace _All",
                              "",
                              () => ReplaceAll (),
                              null,
                              null,
                              KeyCode.CtrlMask
                              | KeyCode.ShiftMask
                              | KeyCode.AltMask
                              | KeyCode.A
                             ),
                         null,
                         new (
                              "_Select All",
                              "",
                              () => SelectAll (),
                              null,
                              null,
                              KeyCode.CtrlMask | KeyCode.T
                             )
                     }
                    ),
                new ("_ScrollBarView", CreateKeepChecked ()),
                new ("_Cursor", CreateCursorRadio ()),
                new (
                     "Forma_t",
                     new []
                     {
                         CreateWrapChecked (),
                         CreateAutocomplete (),
                         CreateAllowsTabChecked (),
                         CreateReadOnlyChecked (),
                         CreateUseSameRuneTypeForWords (),
                         CreateSelectWordOnlyOnDoubleClick (),
                         new MenuItem (
                                       "Colors",
                                       "",
                                       () => _textView.PromptForColors (),
                                       null,
                                       null,
                                       KeyCode.CtrlMask | KeyCode.L
                                      )
                     }
                    ),
                new (
                     "_Responder",
                     new [] { CreateCanFocusChecked (), CreateEnabledChecked (), CreateVisibleChecked () }
                    ),
                new (
                     "Conte_xtMenu",
                     new []
                     {
                         _miForceMinimumPosToZero = new (
                                                         "ForceMinimumPosTo_Zero",
                                                         "",
                                                         () =>
                                                         {
                                                             //_miForceMinimumPosToZero.Checked =
                                                             //    _forceMinimumPosToZero =
                                                             //        !_forceMinimumPosToZero;

                                                             //_textView.ContextMenu.ForceMinimumPosToZero =
                                                             //    _forceMinimumPosToZero;
                                                         }
                                                        )
                         {
                             CheckType = MenuItemCheckStyle.Checked,
                             Checked = _forceMinimumPosToZero
                         },
                         new MenuBarItem ("_Languages", GetSupportedCultures ())
                     }
                    )
            ]
        };

        _appWindow.Add (menu);

        var siCursorPosition = new Shortcut (KeyCode.Null, "", null);

        var statusBar = new StatusBar (
                                       new []
                                       {
                                           new (Application.QuitKey, $"Quit", Quit),
                                           new (Key.F2, "Open", Open),
                                           new (Key.F3, "Save", () => Save ()),
                                           new (Key.F4, "Save As", () => SaveAs ()),
                                           new (Key.Empty, $"OS Clipboard IsSupported : {Clipboard.IsSupported}", null),
                                           siCursorPosition,
                                       }
                                      )
        {
            AlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast
        };

        _textView.VerticalScrollBar.AutoShow = false;
        _textView.UnwrappedCursorPosition += (s, e) =>
                                             {
                                                 siCursorPosition.Title = $"Ln {e.Y + 1}, Col {e.X + 1}";
                                             };

        _appWindow.Add (statusBar);

        //_scrollBar = new (_textView, true);

        //_scrollBar.ChangedPosition += (s, e) =>
        //                              {
        //                                  _textView.TopRow = _scrollBar.Position;

        //                                  if (_textView.TopRow != _scrollBar.Position)
        //                                  {
        //                                      _scrollBar.Position = _textView.TopRow;
        //                                  }

        //                                  _textView.SetNeedsDraw ();
        //                              };

        //_scrollBar.OtherScrollBarView.ChangedPosition += (s, e) =>
        //                                                 {
        //                                                     _textView.LeftColumn = _scrollBar.OtherScrollBarView.Position;

        //                                                     if (_textView.LeftColumn != _scrollBar.OtherScrollBarView.Position)
        //                                                     {
        //                                                         _scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
        //                                                     }

        //                                                     _textView.SetNeedsDraw ();
        //                                                 };

        //_textView.DrawingContent += (s, e) =>
        //                         {
        //                             _scrollBar.Size = _textView.Lines;
        //                             _scrollBar.Position = _textView.TopRow;

        //                             if (_scrollBar.OtherScrollBarView != null)
        //                             {
        //                                 _scrollBar.OtherScrollBarView.Size = _textView.Maxlength;
        //                                 _scrollBar.OtherScrollBarView.Position = _textView.LeftColumn;
        //                             }
        //                         };


        _appWindow.Closed += (s, e) => Thread.CurrentThread.CurrentUICulture = new ("en-US");

        CreateFindReplace ();

        // Run - Start the application.
        Application.Run (_appWindow);
        _appWindow.Dispose ();

        // Shutdown - Calling Application.Shutdown is required.
        Application.Shutdown ();

    }

    private bool CanCloseFile ()
    {
        if (_textView.Text == Encoding.Unicode.GetString (_originalText))
        {
            //System.Diagnostics.Debug.Assert (!_textView.IsDirty);
            return true;
        }

        Debug.Assert (_textView.IsDirty);

        int r = MessageBox.ErrorQuery (
                                       "Save File",
                                       $"Do you want save changes in {_appWindow.Title}?",
                                       "Yes",
                                       "No",
                                       "Cancel"
                                      );

        if (r == 0)
        {
            return Save ();
        }

        if (r == 1)
        {
            return true;
        }

        return false;
    }

    private void CloseFile ()
    {
        if (!CanCloseFile ())
        {
            return;
        }

        try
        {
            _textView.CloseFile ();
            New (false);
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery ("Error", ex.Message, "Ok");
        }
    }

    private void ContinueFind (bool next = true, bool replace = false)
    {
        if (!replace && string.IsNullOrEmpty (_textToFind))
        {
            Find ();

            return;
        }

        if (replace
            && (string.IsNullOrEmpty (_textToFind)
                || (_findReplaceWindow == null && string.IsNullOrEmpty (_textToReplace))))
        {
            Replace ();

            return;
        }

        bool found;
        bool gaveFullTurn;

        if (next)
        {
            if (!replace)
            {
                found = _textView.FindNextText (
                                                _textToFind,
                                                out gaveFullTurn,
                                                _matchCase,
                                                _matchWholeWord
                                               );
            }
            else
            {
                found = _textView.FindNextText (
                                                _textToFind,
                                                out gaveFullTurn,
                                                _matchCase,
                                                _matchWholeWord,
                                                _textToReplace,
                                                true
                                               );
            }
        }
        else
        {
            if (!replace)
            {
                found = _textView.FindPreviousText (
                                                    _textToFind,
                                                    out gaveFullTurn,
                                                    _matchCase,
                                                    _matchWholeWord
                                                   );
            }
            else
            {
                found = _textView.FindPreviousText (
                                                    _textToFind,
                                                    out gaveFullTurn,
                                                    _matchCase,
                                                    _matchWholeWord,
                                                    _textToReplace,
                                                    true
                                                   );
            }
        }

        if (!found)
        {
            MessageBox.Query ("Find", $"The following specified text was not found: '{_textToFind}'", "Ok");
        }
        else if (gaveFullTurn)
        {
            MessageBox.Query (
                              "Find",
                              $"No more occurrences were found for the following specified text: '{_textToFind}'",
                              "Ok"
                             );
        }
    }

    private void Copy ()
    {
        if (_textView != null)
        {
            _textView.Copy ();
        }
    }

    private MenuItem CreateAllowsTabChecked ()
    {
        var item = new MenuItem { Title = "Allows Tab" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _textView.AllowsTab;
        item.Action += () => { _textView.AllowsTab = (bool)(item.Checked = !item.Checked); };

        return item;
    }

    private MenuItem CreateAutocomplete ()
    {
        var singleWordGenerator = new SingleWordSuggestionGenerator ();
        _textView.Autocomplete.SuggestionGenerator = singleWordGenerator;

        var auto = new MenuItem ();
        auto.Title = "Autocomplete";
        auto.CheckType |= MenuItemCheckStyle.Checked;
        auto.Checked = false;

        auto.Action += () =>
                       {
                           if ((bool)(auto.Checked = !auto.Checked))
                           {
                               // setup autocomplete with all words currently in the editor
                               singleWordGenerator.AllSuggestions =
                                   Regex.Matches (_textView.Text, "\\w+")
                                        .Select (s => s.Value)
                                        .Distinct ()
                                        .ToList ();
                           }
                           else
                           {
                               singleWordGenerator.AllSuggestions.Clear ();
                           }
                       };

        return auto;
    }

    private MenuItem CreateCanFocusChecked ()
    {
        var item = new MenuItem { Title = "CanFocus" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _textView.CanFocus;

        item.Action += () =>
                       {
                           _textView.CanFocus = (bool)(item.Checked = !item.Checked);

                           if (_textView.CanFocus)
                           {
                               _textView.SetFocus ();
                           }
                       };

        return item;
    }

    private MenuItem [] CreateCursorRadio ()
    {
        List<MenuItem> menuItems = new ();

        menuItems.Add (
                       new ("_Invisible", "", () => SetCursor (CursorVisibility.Invisible))
                       {
                           CheckType = MenuItemCheckStyle.Radio,
                           Checked = _textView.CursorVisibility
                                     == CursorVisibility.Invisible
                       }
                      );

        menuItems.Add (
                       new ("_Box", "", () => SetCursor (CursorVisibility.Box))
                       {
                           CheckType = MenuItemCheckStyle.Radio,
                           Checked = _textView.CursorVisibility == CursorVisibility.Box
                       }
                      );

        menuItems.Add (
                       new ("_Underline", "", () => SetCursor (CursorVisibility.Underline))
                       {
                           CheckType = MenuItemCheckStyle.Radio,
                           Checked = _textView.CursorVisibility
                                     == CursorVisibility.Underline
                       }
                      );
        menuItems.Add (new ("", "", () => { }, () => false));
        menuItems.Add (new ("xTerm :", "", () => { }, () => false));
        menuItems.Add (new ("", "", () => { }, () => false));

        menuItems.Add (
                       new ("  _Default", "", () => SetCursor (CursorVisibility.Default))
                       {
                           CheckType = MenuItemCheckStyle.Radio,
                           Checked = _textView.CursorVisibility
                                     == CursorVisibility.Default
                       }
                      );

        menuItems.Add (
                       new ("  _Vertical", "", () => SetCursor (CursorVisibility.Vertical))
                       {
                           CheckType = MenuItemCheckStyle.Radio,
                           Checked = _textView.CursorVisibility
                                     == CursorVisibility.Vertical
                       }
                      );

        menuItems.Add (
                       new ("  V_ertical Fix", "", () => SetCursor (CursorVisibility.VerticalFix))
                       {
                           CheckType = MenuItemCheckStyle.Radio,
                           Checked = _textView.CursorVisibility == CursorVisibility.VerticalFix
                       }
                      );

        menuItems.Add (
                       new ("  B_ox Fix", "", () => SetCursor (CursorVisibility.BoxFix))
                       {
                           CheckType = MenuItemCheckStyle.Radio,
                           Checked = _textView.CursorVisibility
                                     == CursorVisibility.BoxFix
                       }
                      );

        menuItems.Add (
                       new ("  U_nderline Fix", "", () => SetCursor (CursorVisibility.UnderlineFix))
                       {
                           CheckType = MenuItemCheckStyle.Radio,
                           Checked = _textView.CursorVisibility == CursorVisibility.UnderlineFix
                       }
                      );

        void SetCursor (CursorVisibility visibility)
        {
            _textView.CursorVisibility = visibility;
            var title = "";

            switch (visibility)
            {
                case CursorVisibility.Default:
                    title = "  _Default";

                    break;
                case CursorVisibility.Invisible:
                    title = "_Invisible";

                    break;
                case CursorVisibility.Underline:
                    title = "_Underline";

                    break;
                case CursorVisibility.UnderlineFix:
                    title = "  U_nderline Fix";

                    break;
                case CursorVisibility.Vertical:
                    title = "  _Vertical";

                    break;
                case CursorVisibility.VerticalFix:
                    title = "  V_ertical Fix";

                    break;
                case CursorVisibility.Box:
                    title = "_Box";

                    break;
                case CursorVisibility.BoxFix:
                    title = "  B_ox Fix";

                    break;
            }

            foreach (MenuItem menuItem in menuItems)
            {
                menuItem.Checked = menuItem.Title.Equals (title) && visibility == _textView.CursorVisibility;
            }
        }

        return menuItems.ToArray ();
    }

    private void CreateDemoFile (string fileName)
    {
        var sb = new StringBuilder ();

        // FIXED: BUGBUG: #279 TextView does not know how to deal with \r\n, only \r
        sb.Append ("Hello world.\n");
        sb.Append ("This is a test of the Emergency Broadcast System.\n");

        for (var i = 0; i < 30; i++)
        {
            sb.Append (
                       $"{i} - This is a test with a very long line and many lines to test the ScrollViewBar against the TextView. - {i}\n"
                      );
        }

        StreamWriter sw = File.CreateText (fileName);
        sw.Write (sb.ToString ());
        sw.Close ();
    }

    private MenuItem CreateEnabledChecked ()
    {
        var item = new MenuItem { Title = "Enabled" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _textView.Enabled;

        item.Action += () =>
                       {
                           _textView.Enabled = (bool)(item.Checked = !item.Checked);

                           if (_textView.Enabled)
                           {
                               _textView.SetFocus ();
                           }
                       };

        return item;
    }

    private class FindReplaceWindow : Window
    {
        private TextView _textView;
        public FindReplaceWindow (TextView textView)
        {
            Title = "Find and Replace";

            _textView = textView;
            X = Pos.AnchorEnd () - 1;
            Y = 2;
            Width = 57;
            Height = 11;
            Arrangement = ViewArrangement.Movable;

            KeyBindings.Add (Key.Esc, Command.Cancel);
            AddCommand (Command.Cancel, () =>
                                        {
                                            Visible = false;

                                            return true;
                                        });
            VisibleChanged += FindReplaceWindow_VisibleChanged;
            Initialized += FindReplaceWindow_Initialized;

            //var btnCancel = new Button
            //{
            //    X = Pos.AnchorEnd (),
            //    Y = Pos.AnchorEnd (),
            //    Text = "Cancel"
            //};
            //btnCancel.Accept += (s, e) => { Visible = false; };
            //Add (btnCancel);
        }

        private void FindReplaceWindow_VisibleChanged (object sender, EventArgs e)
        {
            if (Visible == false)
            {
                _textView.SetFocus ();
            }
            else
            {
                FocusDeepest (NavigationDirection.Forward, null);
            }
        }

        private void FindReplaceWindow_Initialized (object sender, EventArgs e)
        {
            Border.LineStyle = LineStyle.Dashed;
            Border.Thickness = new (0, 1, 0, 0);
        }
    }

    private void ShowFindReplace (bool isFind = true)
    {
        _findReplaceWindow.Visible = true;
        _findReplaceWindow.SuperView.MoveSubViewToStart (_findReplaceWindow);
        _tabView.SetFocus ();
        _tabView.SelectedTab = isFind ? _tabView.Tabs.ToArray () [0] : _tabView.Tabs.ToArray () [1];
        _tabView.SelectedTab.View.FocusDeepest (NavigationDirection.Forward, null);
    }

    private void CreateFindReplace ()
    {
        _findReplaceWindow = new (_textView);
        _tabView = new ()
        {
            X = 0, Y = 0,
            Width = Dim.Fill (), Height = Dim.Fill (0)
        };

        _tabView.AddTab (new () { DisplayText = "Find", View = CreateFindTab () }, true);
        _tabView.AddTab (new () { DisplayText = "Replace", View = CreateReplaceTab () }, false);
        _tabView.SelectedTabChanged += (s, e) => _tabView.SelectedTab.View.FocusDeepest (NavigationDirection.Forward, null);
        _findReplaceWindow.Add (_tabView);

//        _tabView.SelectedTab.View.FocusLast (null); // Hack to get the first tab to be focused
        _findReplaceWindow.Visible = false;
        _appWindow.Add (_findReplaceWindow);
    }

    private MenuItem [] CreateKeepChecked ()
    {
        var item = new MenuItem ();
        item.Title = "Keep Content Always In Viewport";
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = true;
        //item.Action += () => _scrollBar.KeepContentAlwaysInViewport = (bool)(item.Checked = !item.Checked);

        return new [] { item };
    }

    private MenuItem CreateSelectWordOnlyOnDoubleClick ()
    {
        var item = new MenuItem { Title = "SelectWordOnlyOnDoubleClick" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _textView.SelectWordOnlyOnDoubleClick;
        item.Action += () => _textView.SelectWordOnlyOnDoubleClick = (bool)(item.Checked = !item.Checked);

        return item;
    }

    private MenuItem CreateUseSameRuneTypeForWords ()
    {
        var item = new MenuItem { Title = "UseSameRuneTypeForWords" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _textView.UseSameRuneTypeForWords;
        item.Action += () => _textView.UseSameRuneTypeForWords = (bool)(item.Checked = !item.Checked);

        return item;
    }

    private MenuItem CreateReadOnlyChecked ()
    {
        var item = new MenuItem { Title = "Read Only" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _textView.ReadOnly;
        item.Action += () => _textView.ReadOnly = (bool)(item.Checked = !item.Checked);

        return item;
    }

    private MenuItem CreateVisibleChecked ()
    {
        var item = new MenuItem { Title = "Visible" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _textView.Visible;

        item.Action += () =>
                       {
                           _textView.Visible = (bool)(item.Checked = !item.Checked);

                           if (_textView.Visible)
                           {
                               _textView.SetFocus ();
                           }
                       };

        return item;
    }

    private MenuItem CreateWrapChecked ()
    {
        var item = new MenuItem { Title = "Word Wrap" };
        item.CheckType |= MenuItemCheckStyle.Checked;
        item.Checked = _textView.WordWrap;

        item.Action += () =>
                       {
                           _textView.WordWrap = (bool)(item.Checked = !item.Checked);

                           if (_textView.WordWrap)
                           {
                               //_scrollBar.OtherScrollBarView.ShowScrollIndicator = false;
                           }
                       };

        return item;
    }

    private void Cut ()
    {
        if (_textView != null)
        {
            _textView.Cut ();
        }
    }

    private void Find () { ShowFindReplace (true); }
    private void FindNext () { ContinueFind (); }
    private void FindPrevious () { ContinueFind (false); }

    private View CreateFindTab ()
    {
        var d = new View ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        int lblWidth = "Replace:".Length;

        var label = new Label
        {
            Width = lblWidth,
            TextAlignment = Alignment.End,

            Text = "Find:"
        };
        d.Add (label);

        SetFindText ();

        var txtToFind = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (1),
            Text = _textToFind
        };
        txtToFind.HasFocusChanging += (s, e) => txtToFind.Text = _textToFind;
        d.Add (txtToFind);

        var btnFindNext = new Button
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            IsDefault = true,

            Text = "Find _Next"
        };
        btnFindNext.Accepting += (s, e) => FindNext ();
        d.Add (btnFindNext);

        var btnFindPrevious = new Button
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            Text = "Find _Previous"
        };
        btnFindPrevious.Accepting += (s, e) => FindPrevious ();
        d.Add (btnFindPrevious);

        txtToFind.TextChanged += (s, e) =>
                                 {
                                     _textToFind = txtToFind.Text;
                                     _textView.FindTextChanged ();
                                     btnFindNext.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnFindPrevious.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                 };

        var ckbMatchCase = new CheckBox
        {
            X = 0, Y = Pos.Top (txtToFind) + 2, CheckedState = _matchCase ? CheckState.Checked : CheckState.UnChecked, Text = "Match c_ase"
        };
        ckbMatchCase.CheckedStateChanging += (s, e) => _matchCase = e.Result == CheckState.Checked;
        d.Add (ckbMatchCase);

        var ckbMatchWholeWord = new CheckBox
        {
            X = 0, Y = Pos.Top (ckbMatchCase) + 1, CheckedState = _matchWholeWord ? CheckState.Checked : CheckState.UnChecked, Text = "Match _whole word"
        };
        ckbMatchWholeWord.CheckedStateChanging += (s, e) => _matchWholeWord = e.Result == CheckState.Checked;
        d.Add (ckbMatchWholeWord);
        return d;
    }

    private MenuItem [] GetSupportedCultures ()
    {
        List<MenuItem> supportedCultures = new ();
        int index = -1;

        foreach (CultureInfo c in _cultureInfos)
        {
            var culture = new MenuItem { CheckType = MenuItemCheckStyle.Checked };

            if (index == -1)
            {
                culture.Title = "_English";
                culture.Help = "en-US";
                culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == "en-US";
                CreateAction (supportedCultures, culture);
                supportedCultures.Add (culture);
                index++;
                culture = new () { CheckType = MenuItemCheckStyle.Checked };
            }

            culture.Title = $"_{c.Parent.EnglishName}";
            culture.Help = c.Name;
            culture.Checked = Thread.CurrentThread.CurrentUICulture.Name == c.Name;
            CreateAction (supportedCultures, culture);
            supportedCultures.Add (culture);
        }

        return supportedCultures.ToArray ();

        void CreateAction (List<MenuItem> supportedCultures, MenuItem culture)
        {
            culture.Action += () =>
                              {
                                  Thread.CurrentThread.CurrentUICulture = new (culture.Help);
                                  culture.Checked = true;

                                  foreach (MenuItem item in supportedCultures)
                                  {
                                      item.Checked = item.Help == Thread.CurrentThread.CurrentUICulture.Name;
                                  }
                              };
        }
    }

    private void LoadFile ()
    {
        if (_fileName != null)
        {
            // FIXED: BUGBUG: #452 TextView.LoadFile keeps file open and provides no way of closing it
            _textView.Load (_fileName);

            //_textView.Text = System.IO.File.ReadAllText (_fileName);
            _originalText = Encoding.Unicode.GetBytes (_textView.Text);
            _appWindow.Title = _fileName;
            _saved = true;
        }
    }

    private void New (bool checkChanges = true)
    {
        if (checkChanges && !CanCloseFile ())
        {
            return;
        }

        _appWindow.Title = "Untitled.txt";
        _fileName = null;
        _originalText = new MemoryStream ().ToArray ();
        _textView.Text = Encoding.Unicode.GetString (_originalText);
    }

    private void Open ()
    {
        if (!CanCloseFile ())
        {
            return;
        }

        List<IAllowedType> aTypes = new ()
        {
            new AllowedType (
                             "Text",
                             ".txt;.bin;.xml;.json",
                             ".txt",
                             ".bin",
                             ".xml",
                             ".json"
                            ),
            new AllowedTypeAny ()
        };
        var d = new OpenDialog { Title = "Open", AllowedTypes = aTypes, AllowsMultipleSelection = false };
        Application.Run (d);

        if (!d.Canceled && d.FilePaths.Count > 0)
        {
            _fileName = d.FilePaths [0];
            LoadFile ();
        }

        d.Dispose ();
    }

    private void Paste ()
    {
        if (_textView != null)
        {
            _textView.Paste ();
        }
    }

    private void Quit ()
    {
        if (!CanCloseFile ())
        {
            return;
        }

        Application.RequestStop ();
    }

    private void Replace () { ShowFindReplace (false); }

    private void ReplaceAll ()
    {
        if (string.IsNullOrEmpty (_textToFind) || (string.IsNullOrEmpty (_textToReplace) && _findReplaceWindow == null))
        {
            Replace ();

            return;
        }

        if (_textView.ReplaceAllText (_textToFind, _matchCase, _matchWholeWord, _textToReplace))
        {
            MessageBox.Query (
                              "Replace All",
                              $"All occurrences were replaced for the following specified text: '{_textToReplace}'",
                              "Ok"
                             );
        }
        else
        {
            MessageBox.Query (
                              "Replace All",
                              $"None of the following specified text was found: '{_textToFind}'",
                              "Ok"
                             );
        }
    }

    private void ReplaceNext () { ContinueFind (true, true); }
    private void ReplacePrevious () { ContinueFind (false, true); }

    private View CreateReplaceTab ()
    {
        var d = new View ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        int lblWidth = "Replace:".Length;

        var label = new Label
        {
            Width = lblWidth,
            TextAlignment = Alignment.End,
            Text = "Find:"
        };
        d.Add (label);

        SetFindText ();

        var txtToFind = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (1),
            Text = _textToFind
        };
        txtToFind.HasFocusChanging += (s, e) => txtToFind.Text = _textToFind;
        d.Add (txtToFind);

        var btnFindNext = new Button
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            IsDefault = true,
            Text = "Replace _Next"
        };
        btnFindNext.Accepting += (s, e) => ReplaceNext ();
        d.Add (btnFindNext);

        label = new ()
        {
            X = Pos.Left (label),
            Y = Pos.Top (label) + 1,
            Text = "Replace:"
        };
        d.Add (label);

        SetFindText ();

        var txtToReplace = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Width = Dim.Fill (1),
            Text = _textToReplace
        };
        txtToReplace.TextChanged += (s, e) => _textToReplace = txtToReplace.Text;
        d.Add (txtToReplace);

        var btnFindPrevious = new Button
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            Text = "Replace _Previous"
        };
        btnFindPrevious.Accepting += (s, e) => ReplacePrevious ();
        d.Add (btnFindPrevious);

        var btnReplaceAll = new Button
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            Text = "Replace _All"
        };
        btnReplaceAll.Accepting += (s, e) => ReplaceAll ();
        d.Add (btnReplaceAll);

        txtToFind.TextChanged += (s, e) =>
                                 {
                                     _textToFind = txtToFind.Text;
                                     _textView.FindTextChanged ();
                                     btnFindNext.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnFindPrevious.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnReplaceAll.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                 };

        var ckbMatchCase = new CheckBox
        {
            X = 0, Y = Pos.Top (txtToFind) + 2, CheckedState = _matchCase ? CheckState.Checked : CheckState.UnChecked, Text = "Match c_ase"
        };
        ckbMatchCase.CheckedStateChanging += (s, e) => _matchCase = e.Result == CheckState.Checked;
        d.Add (ckbMatchCase);

        var ckbMatchWholeWord = new CheckBox
        {
            X = 0, Y = Pos.Top (ckbMatchCase) + 1, CheckedState = _matchWholeWord ? CheckState.Checked : CheckState.UnChecked, Text = "Match _whole word"
        };
        ckbMatchWholeWord.CheckedStateChanging += (s, e) => _matchWholeWord = e.Result == CheckState.Checked;
        d.Add (ckbMatchWholeWord);

        return d;
    }

    private bool Save ()
    {
        if (_fileName != null)
        {
            // FIXED: BUGBUG: #279 TextView does not know how to deal with \r\n, only \r 
            // As a result files saved on Windows and then read back will show invalid chars.
            return SaveFile (_appWindow.Title, _fileName);
        }

        return SaveAs ();
    }

    private bool SaveAs ()
    {
        List<IAllowedType> aTypes = new ()
        {
            new AllowedType ("Text Files", ".txt", ".bin", ".xml"), new AllowedTypeAny ()
        };
        var sd = new SaveDialog { Title = "Save file", AllowedTypes = aTypes };

        sd.Path = _appWindow.Title;
        Application.Run (sd);
        bool canceled = sd.Canceled;
        string path = sd.Path;
        string fileName = sd.FileName;
        sd.Dispose ();

        if (!canceled)
        {
            if (File.Exists (path))
            {
                if (MessageBox.Query (
                                      "Save File",
                                      "File already exists. Overwrite any way?",
                                      "No",
                                      "Ok"
                                     )
                    == 1)
                {
                    return SaveFile (fileName, path);
                }

                _saved = false;

                return _saved;
            }

            return SaveFile (fileName, path);
        }

        _saved = false;

        return _saved;
    }

    private bool SaveFile (string title, string file)
    {
        try
        {
            _appWindow.Title = title;
            _fileName = file;
            File.WriteAllText (_fileName, _textView.Text);
            _originalText = Encoding.Unicode.GetBytes (_textView.Text);
            _saved = true;
            _textView.ClearHistoryChanges ();
            MessageBox.Query ("Save File", "File was successfully saved.", "Ok");
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery ("Error", ex.Message, "Ok");

            return false;
        }

        return true;
    }

    private void SelectAll () { _textView.SelectAll (); }

    private void SetFindText ()
    {
        _textToFind = !string.IsNullOrEmpty (_textView.SelectedText) ? _textView.SelectedText :
                      string.IsNullOrEmpty (_textToFind) ? "" : _textToFind;

        _textToReplace = string.IsNullOrEmpty (_textToReplace) ? "" : _textToReplace;
    }
}
