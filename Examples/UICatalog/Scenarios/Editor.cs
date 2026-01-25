#nullable enable

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
    private IApplication? _app;
    private Window? _appWindow;
    private List<CultureInfo>? _cultureInfos;
    private string _fileName = "demo.txt";
    private bool _forceMinimumPosToZero = true;
    private bool _matchCase;
    private bool _matchWholeWord;
    private CheckBox? _miForceMinimumPosToZeroCheckBox;
    private byte []? _originalText;
    private bool _saved = true;
    private TabView? _tabView;
    private string _textToFind = string.Empty;
    private string _textToReplace = string.Empty;
    private TextView? _textView;
    private FindReplaceWindow? _findReplaceWindow;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        _appWindow = new Window { Title = _fileName ?? "Untitled", BorderStyle = LineStyle.None };

        _cultureInfos = Application.SupportedCultures?.ToList ();

        _textView = new TextView
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            ScrollBars = true
        };

        CreateDemoFile (_fileName!);

        LoadFile ();

        _appWindow.Add (_textView);

        // MenuBar
        MenuBar menu = new ();

        menu.Add (new MenuBarItem (Strings.menuFile,
                                   [
                                       new MenuItem { Title = Strings.cmdNew, Action = () => New () },
                                       new MenuItem { Title = Strings.cmdOpen, Action = Open },
                                       new MenuItem { Title = Strings.cmdSave, Action = () => Save () },
                                       new MenuItem { Title = Strings.cmdSaveAs, Action = () => SaveAs () },
                                       new MenuItem { Title = Strings.cmdClose, Action = CloseFile },
                                       new MenuItem { Title = Strings.cmdQuit, Action = Quit }
                                   ]));

        menu.Add (new MenuBarItem ("_Edit",
                                   [
                                       new MenuItem { Title = Strings.cmdCopy, Key = Key.C.WithCtrl, Action = Copy },
                                       new MenuItem { Title = Strings.cmdCut, Key = Key.W.WithCtrl, Action = Cut },
                                       new MenuItem { Title = Strings.cmdPaste, Key = Key.Y.WithCtrl, Action = Paste },
                                       new MenuItem { Title = Strings.cmdFind, Key = Key.S.WithCtrl, Action = Find },
                                       new MenuItem { Title = "Find _Next", Key = Key.S.WithCtrl.WithShift, Action = FindNext },
                                       new MenuItem { Title = "Find P_revious", Key = Key.S.WithCtrl.WithShift.WithAlt, Action = FindPrevious },
                                       new MenuItem { Title = "_Replace", Key = Key.R.WithCtrl, Action = Replace },
                                       new MenuItem { Title = "Replace Ne_xt", Key = Key.R.WithCtrl.WithShift, Action = ReplaceNext },
                                       new MenuItem { Title = "Replace Pre_vious", Key = Key.R.WithCtrl.WithShift.WithAlt, Action = ReplacePrevious },
                                       new MenuItem { Title = "Replace _All", Key = Key.A.WithCtrl.WithShift.WithAlt, Action = ReplaceAll },
                                       new MenuItem { Title = Strings.ctxSelectAll, Key = Key.T.WithCtrl, Action = SelectAll }
                                   ]));

        menu.Add (new MenuBarItem ("_ScrollBars", CreateScrollBarsMenu ()));

        menu.Add (new MenuBarItem ("Forma_t",
                                   [
                                       CreateWrapChecked (),
                                       CreateAutocomplete (),
                                       CreateAllowsTabChecked (),
                                       CreateReadOnlyChecked (),
                                       CreateUseSameRuneTypeForWords (),
                                       CreateSelectWordOnlyOnDoubleClick (),
                                       new MenuItem { Title = "Colors", Key = Key.L.WithCtrl, Action = () => _textView?.PromptForColors () }
                                   ]));

        menu.Add (new MenuBarItem ("_View", [CreateCanFocusChecked (), CreateEnabledChecked (), CreateVisibleChecked ()]));

        _miForceMinimumPosToZeroCheckBox = new CheckBox
        {
            Title = "ForceMinimumPosTo_Zero", CheckedState = _forceMinimumPosToZero ? CheckState.Checked : CheckState.UnChecked
        };

        _miForceMinimumPosToZeroCheckBox.CheckedStateChanging += (s, e) =>
                                                                 {
                                                                     _forceMinimumPosToZero = e.Result == CheckState.Checked;

                                                                     // Note: PopoverMenu.ForceMinimumPosToZero property doesn't exist in v2
                                                                     // if (_textView?.ContextMenu is not null)
                                                                     // {
                                                                     //     _textView.ContextMenu.ForceMinimumPosToZero = _forceMinimumPosToZero;
                                                                     // }
                                                                 };

        menu.Add (new MenuBarItem ("Conte_xtMenu",
                                   [new MenuItem { CommandView = _miForceMinimumPosToZeroCheckBox }, new MenuBarItem ("_Languages", GetSupportedCultures ())]));

        _appWindow.Add (menu);

        Shortcut siCursorPosition = new (Key.Empty, "", null);

        StatusBar statusBar =
            new ([
                     new Shortcut (Application.QuitKey, "Quit", Quit),
                     new Shortcut (Key.F2, "Open", Open),
                     new Shortcut (Key.F3, "Save", () => Save ()),
                     new Shortcut (Key.F4, "Save As", () => SaveAs ()),
                     new Shortcut (Key.Empty, $"OS Clipboard IsSupported : {app.Clipboard!.IsSupported}", null),
                     siCursorPosition
                 ]) { AlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast };

        _textView.UnwrappedCursorPosition += (s, e) => { siCursorPosition.Title = $"Ln {e.Y + 1}, Col {e.X + 1}"; };

        _appWindow.Add (statusBar);

        _appWindow.IsRunningChanged += (s, e) =>
                                       {
                                           if (!e.Value)
                                           {
                                               // BUGBUG: This should restore the original culture info
                                               Thread.CurrentThread.CurrentUICulture = new CultureInfo ("en-US");
                                           }
                                       };

        CreateFindReplace ();

        // Run - Start the application.
        app.Run (_appWindow);
        _appWindow.Dispose ();
    }

    private bool CanCloseFile ()
    {
        if (_textView is null || _originalText is null || _appWindow is null)
        {
            return true;
        }

        if (_textView.Text == Encoding.Unicode.GetString (_originalText))
        {
            return true;
        }

        Debug.Assert (_textView.IsDirty);

        int? r = MessageBox.ErrorQuery (_appWindow!.App!, "Save File", $"Do you want save changes in {_appWindow.Title}?", Strings.btnCancel, Strings.btnNo, Strings.btnYes);

        return r switch
               {
                   2 => Save (),
                   1 => true,
                   _ => false
               };
    }

    private void CloseFile ()
    {
        if (!CanCloseFile () || _textView is null)
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
            MessageBox.ErrorQuery (_appWindow!.App!, "Error", ex.Message, Strings.btnOk);
        }
    }

    private void ContinueFind (bool next = true, bool replace = false)
    {
        if (_textView is null)
        {
            return;
        }

        if (!replace && string.IsNullOrEmpty (_textToFind))
        {
            Find ();

            return;
        }

        if (replace && (string.IsNullOrEmpty (_textToFind) || (_findReplaceWindow is null && string.IsNullOrEmpty (_textToReplace))))
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
                found = _textView.FindNextText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord);
            }
            else
            {
                found = _textView.FindNextText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord, _textToReplace, true);
            }
        }
        else
        {
            if (!replace)
            {
                found = _textView.FindPreviousText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord);
            }
            else
            {
                found = _textView.FindPreviousText (_textToFind, out gaveFullTurn, _matchCase, _matchWholeWord, _textToReplace, true);
            }
        }

        if (!found)
        {
            MessageBox.Query (_appWindow!.App!, "Find", $"The following specified text was not found: '{_textToFind}'", Strings.btnOk);
        }
        else if (gaveFullTurn)
        {
            MessageBox.Query (_appWindow!.App!, "Find", $"No more occurrences were found for the following specified text: '{_textToFind}'", Strings.btnOk);
        }
    }

    private void Copy () => _textView?.Copy ();

    private MenuItem [] CreateScrollBarsMenu ()
    {
        if (_textView is null)
        {
            return [];
        }

        List<MenuItem> menuItems = [];

        CheckBox scrollBarCheckBox = new () { Title = "_Scroll Bars", CheckedState = _textView.ScrollBars ? CheckState.Checked : CheckState.UnChecked };

        scrollBarCheckBox.CheckedStateChanged += (s, e) => { _textView.ScrollBars = scrollBarCheckBox.CheckedState == CheckState.Checked; };

        MenuItem verticalItem = new () { CommandView = scrollBarCheckBox };

        verticalItem.Accepting += (s, e) =>
                                  {
                                      scrollBarCheckBox.AdvanceCheckState ();
                                      e.Handled = true;
                                  };

        menuItems.Add (verticalItem);

        return [.. menuItems];
    }

    private MenuItem [] GetSupportedCultures ()
    {
        if (_cultureInfos is null)
        {
            return [];
        }

        List<MenuItem> supportedCultures = [];
        List<CheckBox> allCheckBoxes = [];
        int index = -1;

        void CreateCultureMenuItem (string title, string cultureName, bool isChecked)
        {
            CheckBox checkBox = new () { Title = title, CheckedState = isChecked ? CheckState.Checked : CheckState.UnChecked };

            allCheckBoxes.Add (checkBox);

            checkBox.CheckedStateChanging += (s, e) =>
                                             {
                                                 if (e.Result == CheckState.Checked)
                                                 {
                                                     Thread.CurrentThread.CurrentUICulture = new CultureInfo (cultureName);

                                                     foreach (CheckBox cb in allCheckBoxes)
                                                     {
                                                         cb.CheckedState = cb == checkBox ? CheckState.Checked : CheckState.UnChecked;
                                                     }
                                                 }
                                             };

            MenuItem item = new () { CommandView = checkBox };

            item.Accepting += (s, e) =>
                              {
                                  checkBox.AdvanceCheckState ();
                                  e.Handled = true;
                              };

            supportedCultures.Add (item);
        }

        foreach (CultureInfo c in _cultureInfos)
        {
            if (index == -1)
            {
                CreateCultureMenuItem ("_English", "en-US", Thread.CurrentThread.CurrentUICulture.Name == "en-US");
                index++;
            }

            CreateCultureMenuItem ($"_{c.Parent.EnglishName}", c.Name, Thread.CurrentThread.CurrentUICulture.Name == c.Name);
        }

        return [.. supportedCultures];
    }

    private MenuItem CreateWrapChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Word Wrap" };
        }

        CheckBox checkBox = new () { Title = "_Word Wrap", CheckedState = _textView.WordWrap ? CheckState.Checked : CheckState.UnChecked };

        checkBox.CheckedStateChanged += (s, e) => { _textView.WordWrap = checkBox.CheckedState == CheckState.Checked; };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateAutocomplete ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Autocomplete" };
        }

        SingleWordSuggestionGenerator singleWordGenerator = new ();
        _textView.Autocomplete.SuggestionGenerator = singleWordGenerator;

        CheckBox checkBox = new () { Title = "Autocomplete", CheckedState = CheckState.UnChecked };

        checkBox.CheckedStateChanged += (s, e) =>
                                        {
                                            if (checkBox.CheckedState == CheckState.Checked)
                                            {
                                                singleWordGenerator.AllSuggestions =
                                                    Regex.Matches (_textView.Text, "\\w+").Select (s => s.Value).Distinct ().ToList ();
                                            }
                                            else
                                            {
                                                singleWordGenerator.AllSuggestions.Clear ();
                                            }
                                        };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateAllowsTabChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Tab Enters Tab" };
        }

        CheckBox checkBox = new () { Title = "Tab Enters Tab", CheckedState = _textView.TabKeyAddsTab ? CheckState.Checked : CheckState.UnChecked };

        checkBox.CheckedStateChanged += (s, e) => { _textView.TabKeyAddsTab = checkBox.CheckedState == CheckState.Checked; };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateReadOnlyChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Read Only" };
        }

        CheckBox checkBox = new () { Title = "Read Only", CheckedState = _textView.ReadOnly ? CheckState.Checked : CheckState.UnChecked };

        checkBox.CheckedStateChanged += (s, e) => { _textView.ReadOnly = checkBox.CheckedState == CheckState.Checked; };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateUseSameRuneTypeForWords ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "UseSameRuneTypeForWords" };
        }

        CheckBox checkBox = new ()
        {
            Title = "UseSameRuneTypeForWords", CheckedState = _textView.UseSameRuneTypeForWords ? CheckState.Checked : CheckState.UnChecked
        };

        checkBox.CheckedStateChanged += (s, e) => { _textView.UseSameRuneTypeForWords = checkBox.CheckedState == CheckState.Checked; };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateSelectWordOnlyOnDoubleClick ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "SelectWordOnlyOnDoubleClick" };
        }

        CheckBox checkBox = new ()
        {
            Title = "SelectWordOnlyOnDoubleClick", CheckedState = _textView.SelectWordOnlyOnDoubleClick ? CheckState.Checked : CheckState.UnChecked
        };

        checkBox.CheckedStateChanged += (s, e) => { _textView.SelectWordOnlyOnDoubleClick = checkBox.CheckedState == CheckState.Checked; };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateCanFocusChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "CanFocus" };
        }

        CheckBox checkBox = new () { Title = "CanFocus", CheckedState = _textView.CanFocus ? CheckState.Checked : CheckState.UnChecked };

        checkBox.CheckedStateChanged += (s, e) =>
                                        {
                                            _textView.CanFocus = checkBox.CheckedState == CheckState.Checked;

                                            if (_textView.CanFocus)
                                            {
                                                _textView.SetFocus ();
                                            }
                                        };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateEnabledChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Enabled" };
        }

        CheckBox checkBox = new () { Title = "Enabled", CheckedState = _textView.Enabled ? CheckState.Checked : CheckState.UnChecked };

        checkBox.CheckedStateChanged += (s, e) =>
                                        {
                                            _textView.Enabled = checkBox.CheckedState == CheckState.Checked;

                                            if (_textView.Enabled)
                                            {
                                                _textView.SetFocus ();
                                            }
                                        };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private MenuItem CreateVisibleChecked ()
    {
        if (_textView is null)
        {
            return new MenuItem { Title = "Visible" };
        }

        CheckBox checkBox = new () { Title = "Visible", CheckedState = _textView.Visible ? CheckState.Checked : CheckState.UnChecked };

        checkBox.CheckedStateChanged += (s, e) =>
                                        {
                                            _textView.Visible = checkBox.CheckedState == CheckState.Checked;

                                            if (_textView.Visible)
                                            {
                                                _textView.SetFocus ();
                                            }
                                        };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (s, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    private void CreateDemoFile (string fileName)
    {
        StringBuilder sb = new ();

        sb.Append ("Hello world.\n");
        sb.Append ("This is a test of the Emergency Broadcast System.\n");

        for (var i = 0; i < 30; i++)
        {
            sb.Append ($"{i} - This is a test with a very long line and many lines to test the ScrollViewBar against the TextView. - {i}\n");
        }

        StreamWriter sw = File.CreateText (fileName);
        sw.Write (sb.ToString ());
        sw.Close ();
    }

    private void LoadFile ()
    {
        if (_fileName is null || _textView is null || _appWindow is null)
        {
            return;
        }

        _textView.Load (_fileName);
        _originalText = Encoding.Unicode.GetBytes (_textView.Text);
        _appWindow.Title = _fileName;
        _saved = true;
    }

    private void New (bool checkChanges = true)
    {
        if (_appWindow is null || _textView is null)
        {
            return;
        }

        if (checkChanges && !CanCloseFile ())
        {
            return;
        }

        _appWindow.Title = "Untitled.txt";
        _fileName = null!;
        _originalText = new MemoryStream ().ToArray ();
        _textView.Text = Encoding.Unicode.GetString (_originalText);
    }

    private void Open ()
    {
        if (!CanCloseFile ())
        {
            return;
        }

        List<IAllowedType> aTypes = [new AllowedType ("Text", ".txt;.bin;.xml;.json", ".txt", ".bin", ".xml", ".json"), new AllowedTypeAny ()];

        OpenDialog d = new () { Title = "Open", AllowedTypes = aTypes, AllowsMultipleSelection = false };
        _app?.Run (d);

        if (!d.Canceled && d.FilePaths.Count > 0)
        {
            _fileName = d.FilePaths [0];
            LoadFile ();
        }

        d.Dispose ();
    }

    private void Paste () => _textView?.Paste ();

    private void Quit ()
    {
        if (!CanCloseFile ())
        {
            return;
        }

        _appWindow?.RequestStop ();
    }

    private void Replace () => ShowFindReplace (false);

    private void ReplaceAll ()
    {
        if (_textView is null)
        {
            return;
        }

        if (string.IsNullOrEmpty (_textToFind) || (string.IsNullOrEmpty (_textToReplace) && _findReplaceWindow is null))
        {
            Replace ();

            return;
        }

        if (_textView.ReplaceAllText (_textToFind, _matchCase, _matchWholeWord, _textToReplace))
        {
            MessageBox.Query (_appWindow!.App!, "Replace All", $"All occurrences were replaced for the following specified text: '{_textToReplace}'", Strings.btnOk);
        }
        else
        {
            MessageBox.Query (_appWindow!.App!, "Replace All", $"None of the following specified text was found: '{_textToFind}'", Strings.btnOk);
        }
    }

    private void ReplaceNext () => ContinueFind (true, true);
    private void ReplacePrevious () => ContinueFind (false, true);

    private View CreateFindTab ()
    {
        if (_textView is null)
        {
            return new View ();
        }

        View d = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        int lblWidth = "Replace:".Length;

        Label label = new () { Width = lblWidth, TextAlignment = Alignment.End, Text = "Find:" };
        d.Add (label);

        SetFindText ();

        TextField txtToFind = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Fill (1), Text = _textToFind };
        txtToFind.HasFocusChanging += (s, e) => { txtToFind.Text = _textToFind; };
        d.Add (txtToFind);

        Button btnFindNext = new ()
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            IsDefault = true,
            Text = "Find _Next"
        };
        btnFindNext.Accepting += (s, e) => { FindNext (); };
        d.Add (btnFindNext);

        Button btnFindPrevious = new ()
        {
            X = Pos.Align (Alignment.Center), Y = Pos.AnchorEnd (), Enabled = !string.IsNullOrEmpty (txtToFind.Text), Text = "Find _Previous"
        };
        btnFindPrevious.Accepting += (s, e) => { FindPrevious (); };
        d.Add (btnFindPrevious);

        txtToFind.TextChanged += (s, e) =>
                                 {
                                     _textToFind = txtToFind.Text;
                                     _textView.FindTextChanged ();
                                     btnFindNext.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnFindPrevious.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                 };

        CheckBox ckbMatchCase = new ()
        {
            X = 0, Y = Pos.Top (txtToFind) + 2, CheckedState = _matchCase ? CheckState.Checked : CheckState.UnChecked, Text = "Match c_ase"
        };
        ckbMatchCase.CheckedStateChanging += (s, e) => { _matchCase = e.Result == CheckState.Checked; };
        d.Add (ckbMatchCase);

        CheckBox ckbMatchWholeWord = new ()
        {
            X = 0, Y = Pos.Top (ckbMatchCase) + 1, CheckedState = _matchWholeWord ? CheckState.Checked : CheckState.UnChecked, Text = "Match _whole word"
        };
        ckbMatchWholeWord.CheckedStateChanging += (s, e) => { _matchWholeWord = e.Result == CheckState.Checked; };
        d.Add (ckbMatchWholeWord);

        return d;
    }

    private View CreateReplaceTab ()
    {
        if (_textView is null)
        {
            return new View ();
        }

        View d = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        int lblWidth = "Replace:".Length;

        Label label = new () { Width = lblWidth, TextAlignment = Alignment.End, Text = "Find:" };
        d.Add (label);

        SetFindText ();

        TextField txtToFind = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Fill (1), Text = _textToFind };
        txtToFind.HasFocusChanging += (s, e) => { txtToFind.Text = _textToFind; };
        d.Add (txtToFind);

        Button btnFindNext = new ()
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            IsDefault = true,
            Text = "Replace _Next"
        };
        btnFindNext.Accepting += (s, e) => { ReplaceNext (); };
        d.Add (btnFindNext);

        label = new Label { X = Pos.Left (label), Y = Pos.Top (label) + 1, Text = "Replace:" };
        d.Add (label);

        SetFindText ();

        TextField txtToReplace = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Fill (1), Text = _textToReplace };
        txtToReplace.TextChanged += (s, e) => { _textToReplace = txtToReplace.Text; };
        d.Add (txtToReplace);

        Button btnFindPrevious = new ()
        {
            X = Pos.Align (Alignment.Center), Y = Pos.AnchorEnd (), Enabled = !string.IsNullOrEmpty (txtToFind.Text), Text = "Replace _Previous"
        };
        btnFindPrevious.Accepting += (s, e) => { ReplacePrevious (); };
        d.Add (btnFindPrevious);

        Button btnReplaceAll = new ()
        {
            X = Pos.Align (Alignment.Center), Y = Pos.AnchorEnd (), Enabled = !string.IsNullOrEmpty (txtToFind.Text), Text = "Replace _All"
        };
        btnReplaceAll.Accepting += (s, e) => { ReplaceAll (); };
        d.Add (btnReplaceAll);

        txtToFind.TextChanged += (s, e) =>
                                 {
                                     _textToFind = txtToFind.Text;
                                     _textView.FindTextChanged ();
                                     btnFindNext.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnFindPrevious.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnReplaceAll.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                 };

        CheckBox ckbMatchCase = new ()
        {
            X = 0, Y = Pos.Top (txtToFind) + 2, CheckedState = _matchCase ? CheckState.Checked : CheckState.UnChecked, Text = "Match c_ase"
        };
        ckbMatchCase.CheckedStateChanging += (s, e) => { _matchCase = e.Result == CheckState.Checked; };
        d.Add (ckbMatchCase);

        CheckBox ckbMatchWholeWord = new ()
        {
            X = 0, Y = Pos.Top (ckbMatchCase) + 1, CheckedState = _matchWholeWord ? CheckState.Checked : CheckState.UnChecked, Text = "Match _whole word"
        };
        ckbMatchWholeWord.CheckedStateChanging += (s, e) => { _matchWholeWord = e.Result == CheckState.Checked; };
        d.Add (ckbMatchWholeWord);

        return d;
    }

    private bool Save ()
    {
        if (_fileName is { } && _appWindow is { })
        {
            return SaveFile (_appWindow.Title, _fileName);
        }

        return SaveAs ();
    }

    private bool SaveAs ()
    {
        if (_appWindow is null)
        {
            return false;
        }

        List<IAllowedType> aTypes = [new AllowedType ("Text Files", ".txt", ".bin", ".xml"), new AllowedTypeAny ()];

        SaveDialog sd = new () { Title = "Save file", AllowedTypes = aTypes };

        sd.Path = _appWindow.Title;
        _app?.Run (sd);
        bool canceled = sd.Canceled;
        string path = sd.Path;
        string fileName = sd.FileName;
        sd.Dispose ();

        if (!canceled)
        {
            if (File.Exists (path))
            {
                if (MessageBox.Query (_app!, "Save File", "File already exists. Overwrite any way?", Strings.btnNo, Strings.btnYes) == 1)
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
        if (_appWindow is null || _textView is null)
        {
            return false;
        }

        try
        {
            _appWindow.Title = title;
            _fileName = file;
            File.WriteAllText (_fileName, _textView.Text);
            _originalText = Encoding.Unicode.GetBytes (_textView.Text);
            _saved = true;
            _textView.ClearHistoryChanges ();
            MessageBox.Query (_appWindow.App!, "Save File", "File was successfully saved.", Strings.btnOk);
        }
        catch (Exception ex)
        {
            MessageBox.ErrorQuery (_appWindow.App!, "Error", ex.Message, Strings.btnOk);

            return false;
        }

        return true;
    }

    private void SelectAll () => _textView?.SelectAll ();

    private void SetFindText ()
    {
        if (_textView is null)
        {
            return;
        }

        _textToFind = !string.IsNullOrEmpty (_textView.SelectedText) ? _textView.SelectedText : string.IsNullOrEmpty (_textToFind) ? "" : _textToFind;

        _textToReplace = string.IsNullOrEmpty (_textToReplace) ? "" : _textToReplace;
    }

    private void Cut () => _textView?.Cut ();

    private void Find () => ShowFindReplace ();
    private void FindNext () => ContinueFind ();
    private void FindPrevious () => ContinueFind (false);

    private void ShowFindReplace (bool isFind = true)
    {
        if (_findReplaceWindow is null || _tabView is null)
        {
            return;
        }

        _findReplaceWindow.Visible = true;
        _findReplaceWindow.SuperView?.MoveSubViewToStart (_findReplaceWindow);
        _tabView.SetFocus ();
        _tabView.SelectedTab = isFind ? _tabView.Tabs.ToArray () [0] : _tabView.Tabs.ToArray () [1];
        _tabView.SelectedTab?.View?.FocusDeepest (NavigationDirection.Forward, null);
    }

    private void CreateFindReplace ()
    {
        if (_textView is null || _appWindow is null)
        {
            return;
        }

        _findReplaceWindow = new FindReplaceWindow (_textView);

        _tabView = new TabView { X = 0, Y = 0, Width = Dim.Fill (), Height = Dim.Fill (0) };

        _tabView.AddTab (new Tab { DisplayText = "Find", View = CreateFindTab () }, true);
        _tabView.AddTab (new Tab { DisplayText = "Replace", View = CreateReplaceTab () }, false);

        _tabView.SelectedTabChanged += (s, e) => { _tabView.SelectedTab?.View?.FocusDeepest (NavigationDirection.Forward, null); };

        _findReplaceWindow.Add (_tabView);
        _findReplaceWindow.Visible = false;
        _appWindow.Add (_findReplaceWindow);
    }

    private class FindReplaceWindow : Window
    {
        private readonly TextView _textView;

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

            AddCommand (Command.Cancel,
                        () =>
                        {
                            Visible = false;

                            return true;
                        });

            VisibleChanged += FindReplaceWindow_VisibleChanged;
            Initialized += FindReplaceWindow_Initialized;
        }

        private void FindReplaceWindow_Initialized (object? sender, EventArgs e)
        {
            if (Border is { })
            {
                Border.LineStyle = LineStyle.Dashed;
                Border.Thickness = new Thickness (0, 1, 0, 0);
            }
        }

        private void FindReplaceWindow_VisibleChanged (object? sender, EventArgs e)
        {
            if (!Visible)
            {
                _textView.SetFocus ();
            }
            else
            {
                FocusDeepest (NavigationDirection.Forward, null);
            }
        }
    }
}
