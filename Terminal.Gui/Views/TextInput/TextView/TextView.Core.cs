using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Core functionality - Fields, Constructor, and fundamental properties</summary>
public partial class TextView
{
    #region Fields

    private readonly HistoryText _historyText = new ();
    private bool _allowsReturn = true;
    private bool _allowsTab = true;
    private bool _clickWithSelecting;

    // The column we are tracking, or -1 if we are not tracking any column
    private int _columnTrack = -1;
    private bool _continuousFind;
    private bool _copyWithoutSelection;
    private string? _currentCaller;
    private CultureInfo? _currentCulture;
    private bool _isButtonShift;
    private bool _isButtonReleased;
    private bool _isDrawing;
    private bool _isReadOnly;
    private bool _lastWasKill;
    private int _leftColumn;
    private TextModel _model = new ();
    private bool _multiline = true;
    private Dim? _savedHeight;
    private int _selectionStartColumn, _selectionStartRow;
    private bool _shiftSelecting;
    private int _tabWidth = 4;
    private int _topRow;
    private bool _wordWrap;
    private WordWrapManager? _wrapManager;
    private bool _wrapNeeded;

    private string? _copiedText;
    private List<List<Cell>> _copiedCellsList = [];

    #endregion

    #region Constructor

    /// <summary>
    ///     Initializes a <see cref="TextView"/> on the specified area, with dimensions controlled with the X, Y, Width
    ///     and Height properties.
    /// </summary>
    public TextView ()
    {
        CanFocus = true;
        CursorVisibility = CursorVisibility.Default;
        Used = true;

        // By default, disable hotkeys (in case someone sets Title)
        base.HotKeySpecifier = new ('\xffff');

        _model.LinesLoaded += Model_LinesLoaded!;

        _historyText.ChangeText += HistoryText_ChangeText!;

        Initialized += TextView_Initialized!;

        SuperViewChanged += TextView_SuperViewChanged!;

        SubViewsLaidOut += TextView_LayoutComplete;

        // Things this view knows how to do

        // Note - NewLine is only bound to Enter if Multiline is true
        AddCommand (Command.NewLine, ctx => ProcessEnterKey (ctx));

        AddCommand (
                    Command.PageDown,
                    () =>
                    {
                        ProcessPageDown ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageDownExtend,
                    () =>
                    {
                        ProcessPageDownExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUp,
                    () =>
                    {
                        ProcessPageUp ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PageUpExtend,
                    () =>
                    {
                        ProcessPageUpExtend ();

                        return true;
                    }
                   );

        AddCommand (Command.Down, () => ProcessMoveDown ());

        AddCommand (
                    Command.DownExtend,
                    () =>
                    {
                        ProcessMoveDownExtend ();

                        return true;
                    }
                   );

        AddCommand (Command.Up, () => ProcessMoveUp ());

        AddCommand (
                    Command.UpExtend,
                    () =>
                    {
                        ProcessMoveUpExtend ();

                        return true;
                    }
                   );
        AddCommand (Command.Right, () => ProcessMoveRight ());

        AddCommand (
                    Command.RightExtend,
                    () =>
                    {
                        ProcessMoveRightExtend ();

                        return true;
                    }
                   );
        AddCommand (Command.Left, () => ProcessMoveLeft ());

        AddCommand (
                    Command.LeftExtend,
                    () =>
                    {
                        ProcessMoveLeftExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharLeft,
                    () =>
                    {
                        ProcessDeleteCharLeft ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftStart,
                    () =>
                    {
                        ProcessMoveLeftStart ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.LeftStartExtend,
                    () =>
                    {
                        ProcessMoveLeftStartExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteCharRight,
                    () =>
                    {
                        ProcessDeleteCharRight ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEnd,
                    () =>
                    {
                        ProcessMoveEndOfLine ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.RightEndExtend,
                    () =>
                    {
                        ProcessMoveRightEndExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.CutToEndLine,
                    () =>
                    {
                        KillToEndOfLine ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.CutToStartLine,
                    () =>
                    {
                        KillToLeftStart ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Paste,
                    () =>
                    {
                        ProcessPaste ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.ToggleExtend,
                    () =>
                    {
                        ToggleSelecting ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Copy,
                    () =>
                    {
                        ProcessCopy ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Cut,
                    () =>
                    {
                        ProcessCut ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordLeft,
                    () =>
                    {
                        ProcessMoveWordBackward ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordLeftExtend,
                    () =>
                    {
                        ProcessMoveWordBackwardExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordRight,
                    () =>
                    {
                        ProcessMoveWordForward ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.WordRightExtend,
                    () =>
                    {
                        ProcessMoveWordForwardExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.KillWordForwards,
                    () =>
                    {
                        ProcessKillWordForward ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.KillWordBackwards,
                    () =>
                    {
                        ProcessKillWordBackward ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.End,
                    () =>
                    {
                        MoveBottomEnd ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.EndExtend,
                    () =>
                    {
                        MoveBottomEndExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Start,
                    () =>
                    {
                        MoveTopHome ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.StartExtend,
                    () =>
                    {
                        MoveTopHomeExtend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.SelectAll,
                    () =>
                    {
                        ProcessSelectAll ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.ToggleOverwrite,
                    () =>
                    {
                        ProcessSetOverwrite ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.EnableOverwrite,
                    () =>
                    {
                        SetOverwrite (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.DisableOverwrite,
                    () =>
                    {
                        SetOverwrite (false);

                        return true;
                    }
                   );
        AddCommand (Command.Tab, () => ProcessTab ());
        AddCommand (Command.BackTab, () => ProcessBackTab ());

        AddCommand (
                    Command.Undo,
                    () =>
                    {
                        Undo ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Redo,
                    () =>
                    {
                        Redo ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.DeleteAll,
                    () =>
                    {
                        DeleteAll ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Context,
                    () =>
                    {
                        ShowContextMenu (null);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Open,
                    () =>
                    {
                        PromptForColors ();

                        return true;
                    });

        // Default keybindings for this view
        KeyBindings.Remove (Key.Space);

        KeyBindings.Remove (Key.Enter);
        KeyBindings.Add (Key.Enter, Multiline ? Command.NewLine : Command.Accept);

        KeyBindings.Add (Key.PageDown, Command.PageDown);
        KeyBindings.Add (Key.V.WithCtrl, Command.PageDown);

        KeyBindings.Add (Key.PageDown.WithShift, Command.PageDownExtend);

        KeyBindings.Add (Key.PageUp, Command.PageUp);

        KeyBindings.Add (Key.PageUp.WithShift, Command.PageUpExtend);

        KeyBindings.Add (Key.N.WithCtrl, Command.Down);
        KeyBindings.Add (Key.CursorDown, Command.Down);

        KeyBindings.Add (Key.CursorDown.WithShift, Command.DownExtend);

        KeyBindings.Add (Key.P.WithCtrl, Command.Up);
        KeyBindings.Add (Key.CursorUp, Command.Up);

        KeyBindings.Add (Key.CursorUp.WithShift, Command.UpExtend);

        KeyBindings.Add (Key.F.WithCtrl, Command.Right);
        KeyBindings.Add (Key.CursorRight, Command.Right);

        KeyBindings.Add (Key.CursorRight.WithShift, Command.RightExtend);

        KeyBindings.Add (Key.B.WithCtrl, Command.Left);
        KeyBindings.Add (Key.CursorLeft, Command.Left);

        KeyBindings.Add (Key.CursorLeft.WithShift, Command.LeftExtend);

        KeyBindings.Add (Key.Backspace, Command.DeleteCharLeft);

        KeyBindings.Add (Key.Home, Command.LeftStart);

        KeyBindings.Add (Key.Home.WithShift, Command.LeftStartExtend);

        KeyBindings.Add (Key.Delete, Command.DeleteCharRight);
        KeyBindings.Add (Key.D.WithCtrl, Command.DeleteCharRight);

        KeyBindings.Add (Key.End, Command.RightEnd);
        KeyBindings.Add (Key.E.WithCtrl, Command.RightEnd);

        KeyBindings.Add (Key.End.WithShift, Command.RightEndExtend);

        KeyBindings.Add (Key.K.WithCtrl, Command.CutToEndLine); // kill-to-end

        KeyBindings.Add (Key.Delete.WithCtrl.WithShift, Command.CutToEndLine); // kill-to-end

        KeyBindings.Add (Key.Backspace.WithCtrl.WithShift, Command.CutToStartLine); // kill-to-start

        KeyBindings.Add (Key.Y.WithCtrl, Command.Paste); // Control-y, yank
        KeyBindings.Add (Key.Space.WithCtrl, Command.ToggleExtend);

        KeyBindings.Add (Key.C.WithCtrl, Command.Copy);

        KeyBindings.Add (Key.W.WithCtrl, Command.Cut); // Move to Unix?
        KeyBindings.Add (Key.X.WithCtrl, Command.Cut);

        KeyBindings.Add (Key.CursorLeft.WithCtrl, Command.WordLeft);

        KeyBindings.Add (Key.CursorLeft.WithCtrl.WithShift, Command.WordLeftExtend);

        KeyBindings.Add (Key.CursorRight.WithCtrl, Command.WordRight);

        KeyBindings.Add (Key.CursorRight.WithCtrl.WithShift, Command.WordRightExtend);
        KeyBindings.Add (Key.Delete.WithCtrl, Command.KillWordForwards); // kill-word-forwards
        KeyBindings.Add (Key.Backspace.WithCtrl, Command.KillWordBackwards); // kill-word-backwards

        KeyBindings.Add (Key.End.WithCtrl, Command.End);
        KeyBindings.Add (Key.End.WithCtrl.WithShift, Command.EndExtend);
        KeyBindings.Add (Key.Home.WithCtrl, Command.Start);
        KeyBindings.Add (Key.Home.WithCtrl.WithShift, Command.StartExtend);
        KeyBindings.Add (Key.A.WithCtrl, Command.SelectAll);
        KeyBindings.Add (Key.InsertChar, Command.ToggleOverwrite);
        KeyBindings.Add (Key.Tab, Command.Tab);
        KeyBindings.Add (Key.Tab.WithShift, Command.BackTab);

        KeyBindings.Add (Key.Z.WithCtrl, Command.Undo);
        KeyBindings.Add (Key.R.WithCtrl, Command.Redo);

        KeyBindings.Add (Key.G.WithCtrl, Command.DeleteAll);
        KeyBindings.Add (Key.D.WithCtrl.WithShift, Command.DeleteAll);

        KeyBindings.Add (Key.L.WithCtrl, Command.Open);

#if UNIX_KEY_BINDINGS
        KeyBindings.Add (Key.C.WithAlt, Command.Copy);
        KeyBindings.Add (Key.B.WithAlt, Command.WordLeft);
        KeyBindings.Add (Key.W.WithAlt, Command.Cut);
        KeyBindings.Add (Key.V.WithAlt, Command.PageUp);
        KeyBindings.Add (Key.F.WithAlt, Command.WordRight);
        KeyBindings.Add (Key.K.WithAlt, Command.CutToStartLine); // kill-to-start
#endif

        _currentCulture = Thread.CurrentThread.CurrentUICulture;
    }

    #endregion

    #region Initialization and Configuration


    private void TextView_Initialized (object sender, EventArgs e)
    {
        if (Autocomplete.HostControl is null)
        {
            Autocomplete.HostControl = this;
        }

        ContextMenu = CreateContextMenu ();
        App?.Popover?.Register (ContextMenu);
        KeyBindings.Add (ContextMenu.Key, Command.Context);

        // Configure ScrollBars to use modern View scrolling infrastructure
        ConfigureLayout ();

        OnContentsChanged ();
    }

    private void TextView_SuperViewChanged (object sender, SuperViewChangedEventArgs e)
    {
        if (e.SuperView is { })
        {
            if (Autocomplete.HostControl is null)
            {
                Autocomplete.HostControl = this;
            }
        }
        else
        {
            Autocomplete.HostControl = null;
        }
    }

    private void Model_LinesLoaded (object sender, EventArgs e)
    {
        // This call is not needed. Model_LinesLoaded gets invoked when
        // model.LoadString (value) is called. LoadString is called from one place
        // (Text.set) and historyText.Clear() is called immediately after.
        // If this call happens, HistoryText_ChangeText will get called multiple times
        // when Text is set, which is wrong.
        //historyText.Clear (Text);

        if (!_multiline && !IsInitialized)
        {
            CurrentColumn = Text.GetRuneCount ();
            _leftColumn = CurrentColumn > Viewport.Width + 1 ? CurrentColumn - Viewport.Width + 1 : 0;
        }
    }

    #endregion

    /// <summary>
    ///     INTERNAL: Determines if a redraw is needed based on selection state, word wrap needs, and Used flag.
    ///     If a redraw is needed, calls <see cref="AdjustScrollPosition"/>; otherwise positions the cursor and updates
    ///     the unwrapped cursor position.
    /// </summary>
    private void DoNeededAction ()
    {
        if (!NeedsDraw && (IsSelecting || _wrapNeeded || !Used))
        {
            SetNeedsDraw ();
        }

        if (NeedsDraw)
        {
            AdjustScrollPosition ();
        }
        else
        {
            PositionCursor ();
            OnUnwrappedCursorPosition ();
        }
    }
}
