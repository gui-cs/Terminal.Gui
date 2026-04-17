#nullable enable

namespace UICatalog.Scenarios;

public partial class Editor
{
    private void ContinueFind (bool next = true, bool replace = false)
    {
        if (_textView is null)
        {
            return;
        }

        switch (replace)
        {
            case false when string.IsNullOrEmpty (_textToFind):
                Find ();

                return;

            case true when string.IsNullOrEmpty (_textToFind) || (_findReplaceWindow is null && string.IsNullOrEmpty (_textToReplace)):
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

    private void Find () => ShowFindReplace ();
    private void FindNext () => ContinueFind ();
    private void FindPrevious () => ContinueFind (false);

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
            MessageBox.Query (_appWindow!.App!,
                              "Replace All",
                              $"All occurrences were replaced for the following specified text: '{_textToReplace}'",
                              Strings.btnOk);
        }
        else
        {
            MessageBox.Query (_appWindow!.App!, "Replace All", $"None of the following specified text was found: '{_textToFind}'", Strings.btnOk);
        }
    }

    private void ReplaceNext () => ContinueFind (true, true);
    private void ReplacePrevious () => ContinueFind (false, true);

    private void SetFindText ()
    {
        if (_textView is null)
        {
            return;
        }

        _textToFind = !string.IsNullOrEmpty (_textView.SelectedText) ? _textView.SelectedText : string.IsNullOrEmpty (_textToFind) ? "" : _textToFind;

        _textToReplace = string.IsNullOrEmpty (_textToReplace) ? "" : _textToReplace;
    }

    private void ShowFindReplace (bool isFind = true)
    {
        _ = isFind; // Parameter retained for future use (selecting Find vs Replace tab)

        if (_findReplaceWindow is null)
        {
            return;
        }

        _findReplaceWindow.Visible = true;
        _findReplaceWindow.SuperView?.MoveSubViewToStart (_findReplaceWindow);
        _findReplaceWindow.FocusDeepest (NavigationDirection.Forward, null);
    }

    private void CreateFindReplace ()
    {
        if (_textView is null || _appWindow is null)
        {
            return;
        }

        _findReplaceWindow = new FindReplaceWindow (_textView);

        // Restored: Tabs with Find and Replace tabs (#4183)
        Tabs tabs = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        View findTab = new () { Title = "_Find" };
        View findView = CreateFindTab ();
        findView.Width = Dim.Fill ();
        findView.Height = Dim.Fill ();
        findTab.Add (findView);

        View replaceTab = new () { Title = "_Replace" };
        View replaceView = CreateReplaceTab ();
        replaceView.Width = Dim.Fill ();
        replaceView.Height = Dim.Fill ();
        replaceTab.Add (replaceView);

        tabs.Add (findTab, replaceTab);
        tabs.Value = findTab;

        _findReplaceWindow.Add (tabs);
        _findReplaceWindow.Visible = false;
        _appWindow.Add (_findReplaceWindow);
    }

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
        txtToFind.HasFocusChanging += (_, _) => { txtToFind.Text = _textToFind; };
        d.Add (txtToFind);

        Button btnFindNext = new ()
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            IsDefault = true,
            Text = "Find _Next"
        };
        btnFindNext.Accepting += (_, _) => { FindNext (); };
        d.Add (btnFindNext);

        Button btnFindPrevious = new ()
        {
            X = Pos.Align (Alignment.Center), Y = Pos.AnchorEnd (), Enabled = !string.IsNullOrEmpty (txtToFind.Text), Text = "Find _Previous"
        };
        btnFindPrevious.Accepting += (_, _) => { FindPrevious (); };
        d.Add (btnFindPrevious);

        txtToFind.TextChanged += (_, _) =>
                                 {
                                     _textToFind = txtToFind.Text;
                                     _textView.FindTextChanged ();
                                     btnFindNext.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnFindPrevious.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                 };

        CheckBox ckbMatchCase = new ()
        {
            X = 0, Y = Pos.Top (txtToFind) + 2, Value = _matchCase ? CheckState.Checked : CheckState.UnChecked, Text = "Match c_ase"
        };
        ckbMatchCase.ValueChanged += (_, e) => { _matchCase = e.NewValue == CheckState.Checked; };
        d.Add (ckbMatchCase);

        CheckBox ckbMatchWholeWord = new ()
        {
            X = 0, Y = Pos.Top (ckbMatchCase) + 1, Value = _matchWholeWord ? CheckState.Checked : CheckState.UnChecked, Text = "Match _whole word"
        };
        ckbMatchWholeWord.ValueChanged += (_, e) => { _matchWholeWord = e.NewValue == CheckState.Checked; };
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
        txtToFind.HasFocusChanging += (_, _) => { txtToFind.Text = _textToFind; };
        d.Add (txtToFind);

        Button btnFindNext = new ()
        {
            X = Pos.Align (Alignment.Center),
            Y = Pos.AnchorEnd (),
            Enabled = !string.IsNullOrEmpty (txtToFind.Text),
            IsDefault = true,
            Text = "Replace _Next"
        };
        btnFindNext.Accepting += (_, _) => { ReplaceNext (); };
        d.Add (btnFindNext);

        label = new Label { X = Pos.Left (label), Y = Pos.Top (label) + 1, Text = "Replace:" };
        d.Add (label);

        SetFindText ();

        TextField txtToReplace = new () { X = Pos.Right (label) + 1, Y = Pos.Top (label), Width = Dim.Fill (1), Text = _textToReplace };
        txtToReplace.TextChanged += (_, _) => { _textToReplace = txtToReplace.Text; };
        d.Add (txtToReplace);

        Button btnFindPrevious = new ()
        {
            X = Pos.Align (Alignment.Center), Y = Pos.AnchorEnd (), Enabled = !string.IsNullOrEmpty (txtToFind.Text), Text = "Replace _Previous"
        };
        btnFindPrevious.Accepting += (_, _) => { ReplacePrevious (); };
        d.Add (btnFindPrevious);

        Button btnReplaceAll = new ()
        {
            X = Pos.Align (Alignment.Center), Y = Pos.AnchorEnd (), Enabled = !string.IsNullOrEmpty (txtToFind.Text), Text = "Replace _All"
        };
        btnReplaceAll.Accepting += (_, _) => { ReplaceAll (); };
        d.Add (btnReplaceAll);

        txtToFind.TextChanged += (_, _) =>
                                 {
                                     _textToFind = txtToFind.Text;
                                     _textView.FindTextChanged ();
                                     btnFindNext.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnFindPrevious.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                     btnReplaceAll.Enabled = !string.IsNullOrEmpty (txtToFind.Text);
                                 };

        CheckBox ckbMatchCase = new ()
        {
            X = 0, Y = Pos.Top (txtToFind) + 2, Value = _matchCase ? CheckState.Checked : CheckState.UnChecked, Text = "Match c_ase"
        };
        ckbMatchCase.ValueChanged += (_, e) => { _matchCase = e.NewValue == CheckState.Checked; };
        d.Add (ckbMatchCase);

        CheckBox ckbMatchWholeWord = new ()
        {
            X = 0, Y = Pos.Top (ckbMatchCase) + 1, Value = _matchWholeWord ? CheckState.Checked : CheckState.UnChecked, Text = "Match _whole word"
        };
        ckbMatchWholeWord.ValueChanged += (_, e) => { _matchWholeWord = e.NewValue == CheckState.Checked; };
        d.Add (ckbMatchWholeWord);

        return d;
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
            Border.LineStyle = LineStyle.Dashed;
            Border.Thickness = new Thickness (0, 1, 0, 0);
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
