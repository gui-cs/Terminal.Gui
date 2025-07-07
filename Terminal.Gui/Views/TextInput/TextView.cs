#nullable enable

// TextView.cs: multi-line text editing
using System.Globalization;
using System.Runtime.CompilerServices;
using static Unix.Terminal.Delegates;

namespace Terminal.Gui.Views;

/// <summary>Fully featured multi-line text editor</summary>
/// <remarks>
///     <list type="table">
///         <listheader>
///             <term>Shortcut</term> <description>Action performed</description>
///         </listheader>
///         <item>
///             <term>Left cursor, Control-b</term> <description>Moves the editing point left.</description>
///         </item>
///         <item>
///             <term>Right cursor, Control-f</term> <description>Moves the editing point right.</description>
///         </item>
///         <item>
///             <term>Alt-b</term> <description>Moves one word back.</description>
///         </item>
///         <item>
///             <term>Alt-f</term> <description>Moves one word forward.</description>
///         </item>
///         <item>
///             <term>Up cursor, Control-p</term> <description>Moves the editing point one line up.</description>
///         </item>
///         <item>
///             <term>Down cursor, Control-n</term> <description>Moves the editing point one line down</description>
///         </item>
///         <item>
///             <term>Home key, Control-a</term> <description>Moves the cursor to the beginning of the line.</description>
///         </item>
///         <item>
///             <term>End key, Control-e</term> <description>Moves the cursor to the end of the line.</description>
///         </item>
///         <item>
///             <term>Control-Home</term> <description>Scrolls to the first line and moves the cursor there.</description>
///         </item>
///         <item>
///             <term>Control-End</term> <description>Scrolls to the last line and moves the cursor there.</description>
///         </item>
///         <item>
///             <term>Delete, Control-d</term> <description>Deletes the character in front of the cursor.</description>
///         </item>
///         <item>
///             <term>Backspace</term> <description>Deletes the character behind the cursor.</description>
///         </item>
///         <item>
///             <term>Control-k</term>
///             <description>
///                 Deletes the text until the end of the line and replaces the kill buffer with the deleted text.
///                 You can paste this text in a different place by using Control-y.
///             </description>
///         </item>
///         <item>
///             <term>Control-y</term>
///             <description>Pastes the content of the kill ring into the current position.</description>
///         </item>
///         <item>
///             <term>Alt-d</term>
///             <description>
///                 Deletes the word above the cursor and adds it to the kill ring. You can paste the contents of
///                 the kill ring with Control-y.
///             </description>
///         </item>
///         <item>
///             <term>Control-q</term>
///             <description>
///                 Quotes the next input character, to prevent the normal processing of key handling to take
///                 place.
///             </description>
///         </item>
///     </list>
/// </remarks>
public class TextView : View, IDesignable
{
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
        HotKeySpecifier = new ('\xffff');

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
                        ShowContextMenu (true);

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

        ContextMenu = CreateContextMenu ();
        KeyBindings.Add (ContextMenu.Key, Command.Context);
    }

    // BUGBUG: AllowsReturn is mis-named. It should be EnterKeyAccepts.
    /// <summary>
    ///     Gets or sets whether pressing ENTER in a <see cref="TextView"/> creates a new line of text
    ///     in the view or invokes the <see cref="View.Accepting"/> event.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property alters <see cref="Multiline"/>.
    ///         If <see cref="AllowsReturn"/> is set to <see langword="true"/>, then <see cref="Multiline"/> is also set to
    ///         `true` and
    ///         vice-versa.
    ///     </para>
    ///     <para>
    ///         If <see cref="AllowsReturn"/> is set to <see langword="false"/>, then <see cref="AllowsTab"/> gets set to
    ///         <see langword="false"/>.
    ///     </para>
    /// </remarks>
    public bool AllowsReturn
    {
        get => _allowsReturn;
        set
        {
            _allowsReturn = value;

            if (_allowsReturn && !_multiline)
            {
                // BUGBUG: Setting properties should not have side-effects like this. Multiline and AllowsReturn should be independent.
                Multiline = true;
            }

            if (!_allowsReturn && _multiline)
            {
                Multiline = false;

                // BUGBUG: Setting properties should not have side-effects like this. Multiline and AllowsTab should be independent.
                AllowsTab = false;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets whether the <see cref="TextView"/> inserts a tab character into the text or ignores tab input. If
    ///     set to `false` and the user presses the tab key (or shift-tab) the focus will move to the next view (or previous
    ///     with shift-tab). The default is `true`; if the user presses the tab key, a tab character will be inserted into the
    ///     text.
    /// </summary>
    public bool AllowsTab
    {
        get => _allowsTab;
        set
        {
            _allowsTab = value;

            if (_allowsTab && _tabWidth == 0)
            {
                _tabWidth = 4;
            }

            if (_allowsTab && !_multiline)
            {
                Multiline = true;
            }

            if (!_allowsTab && _tabWidth > 0)
            {
                _tabWidth = 0;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Provides autocomplete context menu based on suggestions at the current cursor position. Configure
    ///     <see cref="IAutocomplete.SuggestionGenerator"/> to enable this feature
    /// </summary>
    public IAutocomplete Autocomplete { get; protected set; } = new TextViewAutocomplete ();

    /// <summary>Get the Context Menu.</summary>
    public PopoverMenu? ContextMenu { get; private set; }

    /// <summary>Gets the cursor column.</summary>
    /// <value>The cursor column.</value>
    public int CurrentColumn { get; private set; }

    /// <summary>Gets the current cursor row.</summary>
    public int CurrentRow { get; private set; }

    /// <summary>Sets or gets the current cursor position.</summary>
    public Point CursorPosition
    {
        get => new (CurrentColumn, CurrentRow);
        set
        {
            List<Cell> line = _model.GetLine (Math.Max (Math.Min (value.Y, _model.Count - 1), 0));

            CurrentColumn = value.X < 0 ? 0 :
                            value.X > line.Count ? line.Count : value.X;

            CurrentRow = value.Y < 0 ? 0 :
                         value.Y > _model.Count - 1 ? Math.Max (_model.Count - 1, 0) : value.Y;
            SetNeedsDraw ();
            Adjust ();
        }
    }

    /// <summary>
    ///     Indicates whatever the text has history changes or not. <see langword="true"/> if the text has history changes
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool HasHistoryChanges => _historyText.HasHistoryChanges;

    /// <summary>
    ///     If <see langword="true"/> and the current <see cref="Cell.Attribute"/> is null will inherit from the
    ///     previous, otherwise if <see langword="false"/> (default) do nothing. If the text is load with
    ///     <see cref="Load(List{Cell})"/> this property is automatically sets to <see langword="true"/>.
    /// </summary>
    public bool InheritsPreviousAttribute { get; set; }

    /// <summary>
    ///     Indicates whatever the text was changed or not. <see langword="true"/> if the text was changed
    ///     <see langword="false"/> otherwise.
    /// </summary>
    public bool IsDirty
    {
        get => _historyText.IsDirty (_model.GetAllLines ());
        set => _historyText.Clear (_model.GetAllLines ());
    }

    /// <summary>Gets or sets the left column.</summary>
    public int LeftColumn
    {
        get => _leftColumn;
        set
        {
            if (value > 0 && _wordWrap)
            {
                return;
            }

            _leftColumn = Math.Max (Math.Min (value, Maxlength - 1), 0);
        }
    }

    /// <summary>Gets the number of lines.</summary>
    public int Lines => _model.Count;

    /// <summary>Gets the maximum visible length line.</summary>
    public int Maxlength => _model.GetMaxVisibleLine (_topRow, _topRow + Viewport.Height, TabWidth);

    /// <summary>Gets or sets a value indicating whether this <see cref="TextView"/> is a multiline text view.</summary>
    public bool Multiline
    {
        get => _multiline;
        set
        {
            _multiline = value;

            if (_multiline && !_allowsTab)
            {
                AllowsTab = true;
            }

            if (_multiline && !_allowsReturn)
            {
                AllowsReturn = true;
            }

            if (!_multiline)
            {
                AllowsReturn = false;
                AllowsTab = false;
                WordWrap = false;
                CurrentColumn = 0;
                CurrentRow = 0;
                _savedHeight = Height;

                Height = Dim.Auto (DimAutoStyle.Text, 1);

                if (!IsInitialized)
                {
                    _model.LoadString (Text);
                }

                SetNeedsDraw ();
            }
            else if (_multiline && _savedHeight is { })
            {
                Height = _savedHeight;
                SetNeedsDraw ();
            }

            KeyBindings.Remove (Key.Enter);
            KeyBindings.Add (Key.Enter, Multiline ? Command.NewLine : Command.Accept);
        }
    }

    /// <summary>Gets or sets whether the <see cref="TextView"/> is in read-only mode or not</summary>
    /// <value>Boolean value(Default false)</value>
    public bool ReadOnly
    {
        get => _isReadOnly;
        set
        {
            if (value != _isReadOnly)
            {
                _isReadOnly = value;

                SetNeedsDraw ();
                WrapTextModel ();
                Adjust ();
            }
        }
    }

    /// <summary>Length of the selected text.</summary>
    public int SelectedLength => GetSelectedLength ();

    /// <summary>
    ///     Gets the selected text as
    ///     <see>
    ///         <cref>List{List{Cell}}</cref>
    ///     </see>
    /// </summary>
    public List<List<Cell>> SelectedCellsList
    {
        get
        {
            GetRegion (out List<List<Cell>> selectedCellsList);

            return selectedCellsList;
        }
    }

    /// <summary>The selected text.</summary>
    public string SelectedText
    {
        get
        {
            if (!IsSelecting || (_model.Count == 1 && _model.GetLine (0).Count == 0))
            {
                return string.Empty;
            }

            return GetSelectedRegion ();
        }
    }

    /// <summary>Get or sets whether the user is currently selecting text.</summary>
    public bool IsSelecting { get; set; }

    /// <summary>Start column position of the selected text.</summary>
    public int SelectionStartColumn
    {
        get => _selectionStartColumn;
        set
        {
            List<Cell> line = _model.GetLine (_selectionStartRow);

            _selectionStartColumn = value < 0 ? 0 :
                                    value > line.Count ? line.Count : value;
            IsSelecting = true;
            SetNeedsDraw ();
            Adjust ();
        }
    }

    /// <summary>Start row position of the selected text.</summary>
    public int SelectionStartRow
    {
        get => _selectionStartRow;
        set
        {
            _selectionStartRow = value < 0 ? 0 :
                                 value > _model.Count - 1 ? Math.Max (_model.Count - 1, 0) : value;
            IsSelecting = true;
            SetNeedsDraw ();
            Adjust ();
        }
    }

    /// <summary>Gets or sets a value indicating the number of whitespace when pressing the TAB key.</summary>
    public int TabWidth
    {
        get => _tabWidth;
        set
        {
            _tabWidth = Math.Max (value, 0);

            if (_tabWidth > 0 && !AllowsTab)
            {
                AllowsTab = true;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>Sets or gets the text in the <see cref="TextView"/>.</summary>
    /// <remarks>
    ///     The <see cref="View.TextChanged"/> event is fired whenever this property is set. Note, however, that Text is not
    ///     set by <see cref="TextView"/> as the user types.
    /// </remarks>
    public override string Text
    {
        get
        {
            if (_wordWrap)
            {
                return _wrapManager!.Model.ToString ();
            }

            return _model.ToString ();
        }
        set
        {
            ResetPosition ();
            _model.LoadString (value);

            if (_wordWrap)
            {
                _wrapManager = new (_model);
                _model = _wrapManager.WrapModel (Viewport.Width, out _, out _, out _, out _);
            }

            OnTextChanged ();
            SetNeedsDraw ();

            _historyText.Clear (_model.GetAllLines ());
        }
    }

    /// <summary>Gets or sets the top row.</summary>
    public int TopRow
    {
        get => _topRow;
        set => _topRow = Math.Max (Math.Min (value, Lines - 1), 0);
    }

    /// <summary>
    ///     Tracks whether the text view should be considered "used", that is, that the user has moved in the entry, so
    ///     new input should be appended at the cursor position, rather than clearing the entry
    /// </summary>
    public bool Used { get; set; }

    /// <summary>Allows word wrap the to fit the available container width.</summary>
    public bool WordWrap
    {
        get => _wordWrap;
        set
        {
            if (value == _wordWrap)
            {
                return;
            }

            if (value && !_multiline)
            {
                return;
            }

            _wordWrap = value;
            ResetPosition ();

            if (_wordWrap)
            {
                _wrapManager = new (_model);
                WrapTextModel ();
            }
            else if (!_wordWrap && _wrapManager is { })
            {
                _model = _wrapManager.Model;
            }

            SetNeedsDraw ();
        }
    }

    /// <summary>
    ///     Gets or sets whether the word forward and word backward navigation should use the same or equivalent rune type.
    ///     Default is <c>false</c> meaning using equivalent rune type.
    /// </summary>
    public bool UseSameRuneTypeForWords { get; set; }

    /// <summary>
    ///     Gets or sets whether the word navigation should select only the word itself without spaces around it or with the
    ///     spaces at right.
    ///     Default is <c>false</c> meaning that the spaces at right are included in the selection.
    /// </summary>
    public bool SelectWordOnlyOnDoubleClick { get; set; }

    /// <summary>Allows clearing the <see cref="HistoryTextItemEventArgs"/> items updating the original text.</summary>
    public void ClearHistoryChanges () { _historyText?.Clear (_model.GetAllLines ()); }

    /// <summary>Closes the contents of the stream into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if stream was closed, <c>false</c> otherwise.</returns>
    public bool CloseFile ()
    {
        SetWrapModel ();
        bool res = _model.CloseFile ();
        ResetPosition ();
        SetNeedsDraw ();
        UpdateWrapModel ();

        return res;
    }

    /// <summary>Raised when the contents of the <see cref="TextView"/> are changed.</summary>
    /// <remarks>
    ///     Unlike the <see cref="View.TextChanged"/> event, this event is raised whenever the user types or otherwise changes
    ///     the contents of the <see cref="TextView"/>.
    /// </remarks>
    public event EventHandler<ContentsChangedEventArgs>? ContentsChanged;

    internal void ApplyCellsAttribute (Attribute attribute)
    {
        if (!ReadOnly && SelectedLength > 0)
        {
            int startRow = Math.Min (SelectionStartRow, CurrentRow);
            int endRow = Math.Max (CurrentRow, SelectionStartRow);
            int startCol = SelectionStartRow <= CurrentRow ? SelectionStartColumn : CurrentColumn;
            int endCol = CurrentRow >= SelectionStartRow ? CurrentColumn : SelectionStartColumn;
            List<List<Cell>> selectedCellsOriginal = [];
            List<List<Cell>> selectedCellsChanged = [];

            for (int r = startRow; r <= endRow; r++)
            {
                List<Cell> line = GetLine (r);

                selectedCellsOriginal.Add ([.. line]);

                for (int c = r == startRow ? startCol : 0;
                     c < (r == endRow ? endCol : line.Count);
                     c++)
                {
                    Cell cell = line [c]; // Copy value to a new variable
                    cell.Attribute = attribute; // Modify the copy
                    line [c] = cell; // Assign the modified copy back
                }

                selectedCellsChanged.Add ([.. GetLine (r)]);
            }

            GetSelectedRegion ();
            IsSelecting = false;

            _historyText.Add (
                              [.. selectedCellsOriginal],
                              new Point (startCol, startRow)
                             );

            _historyText.Add (
                              [.. selectedCellsChanged],
                              new Point (startCol, startRow),
                              TextEditingLineStatus.Attribute
                             );
        }
    }

    private Attribute? GetSelectedCellAttribute ()
    {
        List<Cell> line;

        if (SelectedLength > 0)
        {
            line = GetLine (SelectionStartRow);

            if (line [Math.Min (SelectionStartColumn, line.Count - 1)].Attribute is { } attributeSel)
            {
                return new (attributeSel);
            }

            return GetAttributeForRole (VisualRole.Active);
        }

        line = GetCurrentLine ();

        if (line [Math.Min (CurrentColumn, line.Count - 1)].Attribute is { } attribute)
        {
            return new (attribute);
        }

        return GetAttributeForRole (VisualRole.Active);
    }

    /// <summary>
    ///     Open a dialog to set the foreground and background colors.
    /// </summary>
    public void PromptForColors ()
    {
        if (!ColorPicker.Prompt (
                                 "Colors",
                                 GetSelectedCellAttribute (),
                                 out Attribute newAttribute
                                ))
        {
            return;
        }

        var attribute = new Attribute (
                                       newAttribute.Foreground,
                                       newAttribute.Background,
                                       newAttribute.Style
                                      );

        ApplyCellsAttribute (attribute);
    }

    private string? _copiedText;
    private List<List<Cell>> _copiedCellsList = [];

    /// <summary>Copy the selected text to the clipboard contents.</summary>
    public void Copy ()
    {
        SetWrapModel ();

        if (IsSelecting)
        {
            _copiedText = GetRegion (out _copiedCellsList);
            SetClipboard (_copiedText);
            _copyWithoutSelection = false;
        }
        else
        {
            List<Cell> currentLine = GetCurrentLine ();
            _copiedCellsList.Add (currentLine);
            _copiedText = Cell.ToString (currentLine);
            SetClipboard (_copiedText);
            _copyWithoutSelection = true;
        }

        UpdateWrapModel ();
        DoNeededAction ();
    }

    /// <summary>Cut the selected text to the clipboard contents.</summary>
    public void Cut ()
    {
        SetWrapModel ();
        _copiedText = GetRegion (out _copiedCellsList);
        SetClipboard (_copiedText);

        if (!_isReadOnly)
        {
            ClearRegion ();

            _historyText.Add (
                              [new (GetCurrentLine ())],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
        OnContentsChanged ();
    }

    /// <summary>Deletes all text.</summary>
    public void DeleteAll ()
    {
        if (Lines == 0)
        {
            return;
        }

        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        MoveBottomEndExtend ();
        DeleteCharLeft ();
        SetNeedsDraw ();
    }

    /// <summary>Deletes all the selected or a single character at left from the position of the cursor.</summary>
    public void DeleteCharLeft ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        if (IsSelecting)
        {
            _historyText.Add (new () { new (GetCurrentLine ()) }, CursorPosition);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        if (DeleteTextBackwards ())
        {
            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();
    }

    /// <summary>Deletes all the selected or a single character at right from the position of the cursor.</summary>
    public void DeleteCharRight ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        if (IsSelecting)
        {
            _historyText.Add (new () { new (GetCurrentLine ()) }, CursorPosition);

            ClearSelectedRegion ();

            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        if (DeleteTextForwards ())
        {
            UpdateWrapModel ();
            OnContentsChanged ();

            return;
        }

        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();
    }

    /// <summary>Invoked when the normal color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawNormalColor;

    /// <summary>Invoked when the ready only color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawReadOnlyColor;

    /// <summary>Invoked when the selection color is drawn.</summary>
    public event EventHandler<CellEventArgs>? DrawSelectionColor;

    /// <summary>
    ///     Invoked when the used color is drawn. The Used Color is used to indicate if the <see cref="Key.InsertChar"/>
    ///     was pressed and enabled.
    /// </summary>
    public event EventHandler<CellEventArgs>? DrawUsedColor;

    /// <summary>Find the next text based on the match case with the option to replace it.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="gaveFullTurn"><c>true</c>If all the text was forward searched.<c>false</c>otherwise.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <param name="replace"><c>true</c>If is replacing.<c>false</c>otherwise.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool FindNextText (
        string textToFind,
        out bool gaveFullTurn,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null,
        bool replace = false
    )
    {
        if (_model.Count == 0)
        {
            gaveFullTurn = false;

            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos =
            _model.FindNextText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

        return SetFoundText (textToFind, foundPos, textToReplace, replace);
    }

    /// <summary>Find the previous text based on the match case with the option to replace it.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="gaveFullTurn"><c>true</c>If all the text was backward searched.<c>false</c>otherwise.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <param name="replace"><c>true</c>If the text was found.<c>false</c>otherwise.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool FindPreviousText (
        string textToFind,
        out bool gaveFullTurn,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null,
        bool replace = false
    )
    {
        if (_model.Count == 0)
        {
            gaveFullTurn = false;

            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos =
            _model.FindPreviousText (textToFind, out gaveFullTurn, matchCase, matchWholeWord);

        return SetFoundText (textToFind, foundPos, textToReplace, replace);
    }

    /// <summary>Reset the flag to stop continuous find.</summary>
    public void FindTextChanged () { _continuousFind = false; }

    /// <summary>Gets all lines of characters.</summary>
    /// <returns></returns>
    public List<List<Cell>> GetAllLines () { return _model.GetAllLines (); }

    /// <summary>
    ///     Returns the characters on the current line (where the cursor is positioned). Use <see cref="CurrentColumn"/>
    ///     to determine the position of the cursor within that line
    /// </summary>
    /// <returns></returns>
    public List<Cell> GetCurrentLine () { return _model.GetLine (CurrentRow); }

    /// <summary>Returns the characters on the <paramref name="line"/>.</summary>
    /// <param name="line">The intended line.</param>
    /// <returns></returns>
    public List<Cell> GetLine (int line) { return _model.GetLine (line); }

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role == VisualRole.Normal)
        {
            currentAttribute = GetAttributeForRole (VisualRole.Editable);

            return true;
        }

        return base.OnGettingAttributeForRole (role, ref currentAttribute);
    }

    /// <summary>
    ///     Inserts the given <paramref name="toAdd"/> text at the current cursor position exactly as if the user had just
    ///     typed it
    /// </summary>
    /// <param name="toAdd">Text to add</param>
    public void InsertText (string toAdd)
    {
        foreach (char ch in toAdd)
        {
            Key key;

            try
            {
                key = new (ch);
            }
            catch (Exception)
            {
                throw new ArgumentException (
                                             $"Cannot insert character '{ch}' because it does not map to a Key"
                                            );
            }

            InsertText (key);

            if (NeedsDraw)
            {
                Adjust ();
            }
            else
            {
                PositionCursor ();
            }
        }
    }

    /// <summary>Loads the contents of the file into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if file was loaded, <c>false</c> otherwise.</returns>
    /// <param name="path">Path to the file to load.</param>
    public bool Load (string path)
    {
        SetWrapModel ();
        bool res;

        try
        {
            SetWrapModel ();
            res = _model.LoadFile (path);
            _historyText.Clear (_model.GetAllLines ());
            ResetPosition ();
        }
        finally
        {
            UpdateWrapModel ();
            SetNeedsDraw ();
            Adjust ();
        }

        UpdateWrapModel ();

        return res;
    }

    /// <summary>Loads the contents of the stream into the <see cref="TextView"/>.</summary>
    /// <returns><c>true</c>, if stream was loaded, <c>false</c> otherwise.</returns>
    /// <param name="stream">Stream to load the contents from.</param>
    public void Load (Stream stream)
    {
        SetWrapModel ();
        _model.LoadStream (stream);
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        SetNeedsDraw ();
        UpdateWrapModel ();
    }

    /// <summary>Loads the contents of the <see cref="Cell"/> list into the <see cref="TextView"/>.</summary>
    /// <param name="cells">Rune cells list to load the contents from.</param>
    public void Load (List<Cell> cells)
    {
        SetWrapModel ();
        _model.LoadCells (cells, GetAttributeForRole (VisualRole.Focus));
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        SetNeedsDraw ();
        UpdateWrapModel ();
        InheritsPreviousAttribute = true;
    }

    /// <summary>Loads the contents of the list of <see cref="Cell"/> list into the <see cref="TextView"/>.</summary>
    /// <param name="cellsList">List of rune cells list to load the contents from.</param>
    public void Load (List<List<Cell>> cellsList)
    {
        SetWrapModel ();
        InheritsPreviousAttribute = true;
        _model.LoadListCells (cellsList, GetAttributeForRole (VisualRole.Focus));
        _historyText.Clear (_model.GetAllLines ());
        ResetPosition ();
        SetNeedsDraw ();
        UpdateWrapModel ();
    }

    /// <inheritdoc/>
    protected override bool OnMouseEvent (MouseEventArgs ev)
    {
        if (ev is { IsSingleDoubleOrTripleClicked: false, IsPressed: false, IsReleased: false, IsWheel: false }
            && !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition)
            && !ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift)
            && !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked | MouseFlags.ButtonShift)
            && !ev.Flags.HasFlag (ContextMenu!.MouseFlags))
        {
            return false;
        }

        if (!CanFocus)
        {
            return true;
        }

        if (!HasFocus)
        {
            SetFocus ();
        }

        _continuousFind = false;

        // Give autocomplete first opportunity to respond to mouse clicks
        if (SelectedLength == 0 && Autocomplete.OnMouseEvent (ev, true))
        {
            return true;
        }

        if (ev.Flags == MouseFlags.Button1Clicked)
        {
            if (_isButtonReleased)
            {
                _isButtonReleased = false;

                if (SelectedLength == 0)
                {
                    StopSelecting ();
                }

                return true;
            }

            if (_shiftSelecting && !_isButtonShift)
            {
                StopSelecting ();
            }

            ProcessMouseClick (ev, out _);

            if (Used)
            {
                PositionCursor ();
            }
            else
            {
                SetNeedsDraw ();
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (ev.Flags == MouseFlags.WheeledDown)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_topRow + 1);
        }
        else if (ev.Flags == MouseFlags.WheeledUp)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_topRow - 1);
        }
        else if (ev.Flags == MouseFlags.WheeledRight)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_leftColumn + 1, false);
        }
        else if (ev.Flags == MouseFlags.WheeledLeft)
        {
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            ScrollTo (_leftColumn - 1, false);
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition))
        {
            ProcessMouseClick (ev, out List<Cell> line);
            PositionCursor ();

            if (_model.Count > 0 && _shiftSelecting && IsSelecting)
            {
                if (CurrentRow - _topRow >= Viewport.Height - 1 && _model.Count > _topRow + CurrentRow)
                {
                    ScrollTo (_topRow + Viewport.Height);
                }
                else if (_topRow > 0 && CurrentRow <= _topRow)
                {
                    ScrollTo (_topRow - Viewport.Height);
                }
                else if (ev.Position.Y >= Viewport.Height)
                {
                    ScrollTo (_model.Count);
                }
                else if (ev.Position.Y < 0 && _topRow > 0)
                {
                    ScrollTo (0);
                }

                if (CurrentColumn - _leftColumn >= Viewport.Width - 1 && line.Count > _leftColumn + CurrentColumn)
                {
                    ScrollTo (_leftColumn + Viewport.Width, false);
                }
                else if (_leftColumn > 0 && CurrentColumn <= _leftColumn)
                {
                    ScrollTo (_leftColumn - Viewport.Width, false);
                }
                else if (ev.Position.X >= Viewport.Width)
                {
                    ScrollTo (line.Count, false);
                }
                else if (ev.Position.X < 0 && _leftColumn > 0)
                {
                    ScrollTo (0, false);
                }
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed | MouseFlags.ButtonShift))
        {
            if (!_shiftSelecting)
            {
                _isButtonShift = true;
                StartSelecting ();
            }

            ProcessMouseClick (ev, out _);
            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1Pressed))
        {
            if (_shiftSelecting)
            {
                _clickWithSelecting = true;
                StopSelecting ();
            }

            ProcessMouseClick (ev, out _);
            PositionCursor ();

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            _lastWasKill = false;
            _columnTrack = CurrentColumn;

            if (Application.MouseGrabHandler.MouseGrabView is null)
            {
                Application.MouseGrabHandler.GrabMouse (this);
            }
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1Released))
        {
            _isButtonReleased = true;
            Application.MouseGrabHandler.UngrabMouse ();
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked))
        {
            if (ev.Flags.HasFlag (MouseFlags.ButtonShift))
            {
                if (!IsSelecting)
                {
                    StartSelecting ();
                }
            }
            else if (IsSelecting)
            {
                StopSelecting ();
            }

            ProcessMouseClick (ev, out List<Cell> line);

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            (int startCol, int col, int row)? newPos = _model.ProcessDoubleClickSelection (SelectionStartColumn, CurrentColumn, CurrentRow, UseSameRuneTypeForWords, SelectWordOnlyOnDoubleClick);

            if (newPos.HasValue)
            {
                SelectionStartColumn = newPos.Value.startCol;
                CurrentColumn = newPos.Value.col;
                CurrentRow = newPos.Value.row;
            }

            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            SetNeedsDraw ();
        }
        else if (ev.Flags.HasFlag (MouseFlags.Button1TripleClicked))
        {
            if (IsSelecting)
            {
                StopSelecting ();
            }

            ProcessMouseClick (ev, out List<Cell> line);
            CurrentColumn = 0;

            if (!IsSelecting)
            {
                StartSelecting ();
            }

            CurrentColumn = line.Count;
            PositionCursor ();
            _lastWasKill = false;
            _columnTrack = CurrentColumn;
            SetNeedsDraw ();
        }
        else if (ev.Flags == ContextMenu!.MouseFlags)
        {
            ContextMenu!.X = ev.ScreenPosition.X;
            ContextMenu!.Y = ev.ScreenPosition.Y;

            ShowContextMenu (false);

            //ContextMenu.Position = ViewportToScreen ((Viewport with { X = ev.Position.X, Y = ev.Position.Y }).Location);
            //ShowContextMenu ();
        }

        OnUnwrappedCursorPosition ();

        return true;
    }

    /// <summary>Will scroll the <see cref="TextView"/> to the last line and position the cursor there.</summary>
    public void MoveEnd ()
    {
        CurrentRow = _model.Count - 1;
        List<Cell> line = GetCurrentLine ();
        CurrentColumn = line.Count;
        TrackColumn ();
        DoNeededAction ();
    }

    /// <summary>Will scroll the <see cref="TextView"/> to the first line and position the cursor there.</summary>
    public void MoveHome ()
    {
        CurrentRow = 0;
        _topRow = 0;
        CurrentColumn = 0;
        _leftColumn = 0;
        TrackColumn ();
        DoNeededAction ();
    }

    /// <summary>
    ///     Called when the contents of the TextView change. E.g. when the user types text or deletes text. Raises the
    ///     <see cref="ContentsChanged"/> event.
    /// </summary>
    public virtual void OnContentsChanged ()
    {
        ContentsChanged?.Invoke (this, new (CurrentRow, CurrentColumn));

        ProcessInheritsPreviousScheme (CurrentRow, CurrentColumn);
        ProcessAutocomplete ();
    }

    /// <inheritdoc/>
    protected override bool OnDrawingContent ()
    {
        _isDrawing = true;

        SetAttributeForRole (Enabled ? VisualRole.Editable : VisualRole.Disabled);

        (int width, int height) offB = OffSetBackground ();
        int right = Viewport.Width + offB.width;
        int bottom = Viewport.Height + offB.height;
        var row = 0;

        for (int idxRow = _topRow; idxRow < _model.Count; idxRow++)
        {
            List<Cell> line = _model.GetLine (idxRow);
            int lineRuneCount = line.Count;
            var col = 0;

            Move (0, row);

            for (int idxCol = _leftColumn; idxCol < lineRuneCount; idxCol++)
            {
                Rune rune = idxCol >= lineRuneCount ? (Rune)' ' : line [idxCol].Rune;
                int cols = rune.GetColumns ();

                if (idxCol < line.Count && IsSelecting && PointInSelection (idxCol, idxRow))
                {
                    OnDrawSelectionColor (line, idxCol, idxRow);
                }
                else if (idxCol == CurrentColumn && idxRow == CurrentRow && !IsSelecting && !Used && HasFocus && idxCol < lineRuneCount)
                {
                    OnDrawUsedColor (line, idxCol, idxRow);
                }
                else if (ReadOnly)
                {
                    OnDrawReadOnlyColor (line, idxCol, idxRow);
                }
                else
                {
                    OnDrawNormalColor (line, idxCol, idxRow);
                }

                if (rune.Value == '\t')
                {
                    cols += TabWidth + 1;

                    if (col + cols > right)
                    {
                        cols = right - col;
                    }

                    for (var i = 0; i < cols; i++)
                    {
                        if (col + i < right)
                        {
                            AddRune (col + i, row, (Rune)' ');
                        }
                    }
                }
                else
                {
                    AddRune (col, row, rune);

                    // Ensures that cols less than 0 to be 1 because it will be converted to a printable rune
                    cols = Math.Max (cols, 1);
                }

                if (!TextModel.SetCol (ref col, Viewport.Right, cols))
                {
                    break;
                }

                if (idxCol + 1 < lineRuneCount && col + line [idxCol + 1].Rune.GetColumns () > right)
                {
                    break;
                }
            }

            if (col < right)
            {
                SetAttributeForRole (ReadOnly ? VisualRole.ReadOnly : VisualRole.Editable);
                ClearRegion (col, row, right, row + 1);
            }

            row++;
        }

        if (row < bottom)
        {
            SetAttributeForRole (ReadOnly ? VisualRole.ReadOnly : VisualRole.Editable);
            ClearRegion (Viewport.Left, row, right, bottom);
        }

        _isDrawing = false;

        return false;
    }

    /// <inheritdoc/>
    protected override void OnHasFocusChanged (bool newHasFocus, View? previousFocusedView, View? view)
    {
        if (Application.MouseGrabHandler.MouseGrabView is { } && Application.MouseGrabHandler.MouseGrabView == this)
        {
            Application.MouseGrabHandler.UngrabMouse ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnKeyDown (Key key)
    {
        if (!key.IsValid)
        {
            return false;
        }

        // Give autocomplete first opportunity to respond to key presses
        if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (key))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnKeyDownNotHandled (Key a)
    {
        if (!CanFocus)
        {
            return true;
        }

        ResetColumnTrack ();

        // Ignore control characters and other special keys
        if (!a.IsKeyCodeAtoZ && (a.KeyCode < KeyCode.Space || a.KeyCode > KeyCode.CharMask))
        {
            return false;
        }

        InsertText (a);
        DoNeededAction ();

        return true;
    }

    /// <inheritdoc/>
    public override bool OnKeyUp (Key key)
    {
        if (key == Key.Space.WithCtrl)
        {
            return true;
        }

        return false;
    }

    /// <summary>Invoke the <see cref="UnwrappedCursorPosition"/> event with the unwrapped <see cref="CursorPosition"/>.</summary>
    public virtual void OnUnwrappedCursorPosition (int? cRow = null, int? cCol = null)
    {
        int? row = cRow ?? CurrentRow;
        int? col = cCol ?? CurrentColumn;

        if (cRow is null && cCol is null && _wordWrap)
        {
            row = _wrapManager!.GetModelLineFromWrappedLines (CurrentRow);
            col = _wrapManager.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
        }

        UnwrappedCursorPosition?.Invoke (this, new (col.Value, row.Value));
    }

    /// <summary>Paste the clipboard contents into the current selected position.</summary>
    public void Paste ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();
        string? contents = Clipboard.Contents;

        if (_copyWithoutSelection && contents.FirstOrDefault (x => x is '\n' or '\r') == 0)
        {
            List<Cell> runeList = contents is null ? [] : Cell.ToCellList (contents);
            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add ([new (currentLine)], CursorPosition);

            List<List<Cell>> addedLine = [new (currentLine), runeList];

            _historyText.Add (
                              [.. addedLine],
                              CursorPosition,
                              TextEditingLineStatus.Added
                             );

            _model.AddLine (CurrentRow, runeList);
            CurrentRow++;

            _historyText.Add (
                              [new (GetCurrentLine ())],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            SetNeedsDraw ();
            OnContentsChanged ();
        }
        else
        {
            if (IsSelecting)
            {
                ClearRegion ();
            }

            _copyWithoutSelection = false;
            InsertAllText (contents, true);

            if (IsSelecting)
            {
                _historyText.ReplaceLast (
                                          [new (GetCurrentLine ())],
                                          CursorPosition,
                                          TextEditingLineStatus.Original
                                         );
            }

            SetNeedsDraw ();
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
    }

    /// <summary>Positions the cursor on the current row and column</summary>
    public override Point? PositionCursor ()
    {
        ProcessAutocomplete ();

        if (!CanFocus || !Enabled || Application.Driver is null)
        {
            return null;
        }

        if (Application.MouseGrabHandler.MouseGrabView == this && IsSelecting)
        {
            // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
            //var minRow = Math.Min (Math.Max (Math.Min (selectionStartRow, currentRow) - topRow, 0), Viewport.Height);
            //var maxRow = Math.Min (Math.Max (Math.Max (selectionStartRow, currentRow) - topRow, 0), Viewport.Height);
            //SetNeedsDraw (new (0, minRow, Viewport.Width, maxRow));
            SetNeedsDraw ();
        }

        List<Cell> line = _model.GetLine (CurrentRow);
        var col = 0;

        if (line.Count > 0)
        {
            for (int idx = _leftColumn; idx < line.Count; idx++)
            {
                if (idx >= CurrentColumn)
                {
                    break;
                }

                int cols = line [idx].Rune.GetColumns ();

                if (line [idx].Rune.Value == '\t')
                {
                    cols += TabWidth + 1;
                }
                else
                {
                    // Ensures that cols less than 0 to be 1 because it will be converted to a printable rune
                    cols = Math.Max (cols, 1);
                }

                if (!TextModel.SetCol (ref col, Viewport.Width, cols))
                {
                    col = CurrentColumn;

                    break;
                }
            }
        }

        int posX = CurrentColumn - _leftColumn;
        int posY = CurrentRow - _topRow;

        if (posX > -1 && col >= posX && posX < Viewport.Width && _topRow <= CurrentRow && posY < Viewport.Height)
        {
            Move (col, CurrentRow - _topRow);

            return new (col, CurrentRow - _topRow);
        }

        return null; // Hide cursor
    }

    /// <summary>Redoes the latest changes.</summary>
    public void Redo ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Redo ();
    }

    /// <summary>Replaces all the text based on the match case.</summary>
    /// <param name="textToFind">The text to find.</param>
    /// <param name="matchCase">The match case setting.</param>
    /// <param name="matchWholeWord">The match whole word setting.</param>
    /// <param name="textToReplace">The text to replace.</param>
    /// <returns><c>true</c>If the text was found.<c>false</c>otherwise.</returns>
    public bool ReplaceAllText (
        string textToFind,
        bool matchCase = false,
        bool matchWholeWord = false,
        string? textToReplace = null
    )
    {
        if (_isReadOnly || _model.Count == 0)
        {
            return false;
        }

        SetWrapModel ();
        ResetContinuousFind ();

        (Point current, bool found) foundPos =
            _model.ReplaceAllText (textToFind, matchCase, matchWholeWord, textToReplace);

        return SetFoundText (textToFind, foundPos, textToReplace, false, true);
    }

    /// <summary>
    ///     Will scroll the <see cref="TextView"/> to display the specified row at the top if <paramref name="isRow"/> is
    ///     true or will scroll the <see cref="TextView"/> to display the specified column at the left if
    ///     <paramref name="isRow"/> is false.
    /// </summary>
    /// <param name="idx">
    ///     Row that should be displayed at the top or Column that should be displayed at the left, if the value
    ///     is negative it will be reset to zero
    /// </param>
    /// <param name="isRow">If true (default) the <paramref name="idx"/> is a row, column otherwise.</param>
    public void ScrollTo (int idx, bool isRow = true)
    {
        if (idx < 0)
        {
            idx = 0;
        }

        if (isRow)
        {
            _topRow = Math.Max (idx > _model.Count - 1 ? _model.Count - 1 : idx, 0);
        }
        else if (!_wordWrap)
        {
            int maxlength =
                _model.GetMaxVisibleLine (_topRow, _topRow + Viewport.Height, TabWidth);
            _leftColumn = Math.Max (!_wordWrap && idx > maxlength - 1 ? maxlength - 1 : idx, 0);
        }

        SetNeedsDraw ();
    }

    /// <summary>Select all text.</summary>
    public void SelectAll ()
    {
        if (_model.Count == 0)
        {
            return;
        }

        StartSelecting ();
        _selectionStartColumn = 0;
        _selectionStartRow = 0;
        CurrentColumn = _model.GetLine (_model.Count - 1).Count;
        CurrentRow = _model.Count - 1;
        SetNeedsDraw ();
    }

    ///// <summary>Raised when the <see cref="Text"/> property of the <see cref="TextView"/> changes.</summary>
    ///// <remarks>
    /////     The <see cref="Text"/> property of <see cref="TextView"/> only changes when it is explicitly set, not as the
    /////     user types. To be notified as the user changes the contents of the TextView see <see cref="IsDirty"/>.
    ///// </remarks>
    //public event EventHandler? TextChanged;

    /// <summary>Undoes the latest changes.</summary>
    public void Undo ()
    {
        if (ReadOnly)
        {
            return;
        }

        _historyText.Undo ();
    }

    /// <summary>Invoked with the unwrapped <see cref="CursorPosition"/>.</summary>
    public event EventHandler<Point>? UnwrappedCursorPosition;

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute"/> Defaults to <see cref="Scheme.Normal"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawNormalColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawNormalColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            SetAttribute ((Attribute)attribute!);
        }
        else
        {
            SetAttribute (GetAttributeForRole (VisualRole.Normal));
        }
    }

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.Focus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawReadOnlyColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawReadOnlyColor?.Invoke (this, ev);

        Attribute? cellAttribute = line [idxCol].Attribute is { } ? line [idxCol].Attribute : GetAttributeForRole (VisualRole.ReadOnly);

        if (cellAttribute!.Value.Foreground == cellAttribute.Value.Background)
        {
            SetAttribute (new (cellAttribute.Value.Foreground, cellAttribute.Value.Background, cellAttribute.Value.Style));
        }
        else
        {
            SetAttributeForRole (VisualRole.ReadOnly);
        }
    }

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.Focus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawSelectionColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawSelectionColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            Attribute? active = GetAttributeForRole (VisualRole.Active);
            SetAttribute (new (active!.Value.Foreground, active.Value.Background, attribute!.Value.Style));
        }
        else
        {
            SetAttributeForRole (VisualRole.Active);
        }
    }

    /// <summary>
    ///     Sets the <see cref="View.Driver"/> to an appropriate color for rendering the given <paramref name="idxCol"/>
    ///     of the current <paramref name="line"/>. Override to provide custom coloring by calling
    ///     <see cref="View.SetAttribute(Attribute)"/> Defaults to <see cref="Scheme.HotFocus"/>.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="idxCol">The col index.</param>
    /// ///
    /// <param name="idxRow">The row index.</param>
    protected virtual void OnDrawUsedColor (List<Cell> line, int idxCol, int idxRow)
    {
        (int Row, int Col) unwrappedPos = GetUnwrappedPosition (idxRow, idxCol);
        var ev = new CellEventArgs (line, idxCol, unwrappedPos);
        DrawUsedColor?.Invoke (this, ev);

        if (line [idxCol].Attribute is { })
        {
            Attribute? attribute = line [idxCol].Attribute;
            SetValidUsedColor (attribute!);
        }
        else
        {
            SetValidUsedColor (GetAttributeForRole (VisualRole.Focus));
        }
    }

    private void Adjust ()
    {
        (int width, int height) offB = OffSetBackground ();
        List<Cell> line = GetCurrentLine ();
        bool need = NeedsDraw || _wrapNeeded || !Used;
        (int size, int length) tSize = TextModel.DisplaySize (line, -1, -1, false, TabWidth);
        (int size, int length) dSize = TextModel.DisplaySize (line, _leftColumn, CurrentColumn, true, TabWidth);

        if (!_wordWrap && CurrentColumn < _leftColumn)
        {
            _leftColumn = CurrentColumn;
            need = true;
        }
        else if (!_wordWrap
                 && (CurrentColumn - _leftColumn + 1 > Viewport.Width + offB.width || dSize.size + 1 >= Viewport.Width + offB.width))
        {
            _leftColumn = TextModel.CalculateLeftColumn (
                                                         line,
                                                         _leftColumn,
                                                         CurrentColumn,
                                                         Viewport.Width + offB.width,
                                                         TabWidth
                                                        );
            need = true;
        }
        else if ((_wordWrap && _leftColumn > 0) || (dSize.size < Viewport.Width + offB.width && tSize.size < Viewport.Width + offB.width))
        {
            if (_leftColumn > 0)
            {
                _leftColumn = 0;
                need = true;
            }
        }

        if (CurrentRow < _topRow)
        {
            _topRow = CurrentRow;
            need = true;
        }
        else if (CurrentRow - _topRow >= Viewport.Height + offB.height)
        {
            _topRow = Math.Min (Math.Max (CurrentRow - Viewport.Height + 1, 0), CurrentRow);
            need = true;
        }
        else if (_topRow > 0 && CurrentRow < _topRow)
        {
            _topRow = Math.Max (_topRow - 1, 0);
            need = true;
        }

        if (need)
        {
            if (_wrapNeeded)
            {
                WrapTextModel ();
                _wrapNeeded = false;
            }

            SetNeedsDraw ();
        }
        else
        {
            if (IsInitialized)
            {
                PositionCursor ();
            }
        }

        OnUnwrappedCursorPosition ();
    }

    private void AppendClipboard (string text) { Clipboard.Contents += text; }

    private PopoverMenu CreateContextMenu ()
    {
        PopoverMenu menu = new (
                                new List<View>
                                {
                                    new MenuItemv2 (this, Command.SelectAll, Strings.ctxSelectAll),
                                    new MenuItemv2 (this, Command.DeleteAll, Strings.ctxDeleteAll),
                                    new MenuItemv2 (this, Command.Copy, Strings.ctxCopy),
                                    new MenuItemv2 (this, Command.Cut, Strings.ctxCut),
                                    new MenuItemv2 (this, Command.Paste, Strings.ctxPaste),
                                    new MenuItemv2 (this, Command.Undo, Strings.ctxUndo),
                                    new MenuItemv2 (this, Command.Redo, Strings.ctxRedo)
                                });

        menu.KeyChanged += ContextMenu_KeyChanged;

        return menu;
    }

    private void ClearRegion (int left, int top, int right, int bottom)
    {
        for (int row = top; row < bottom; row++)
        {
            Move (left, row);

            for (int col = left; col < right; col++)
            {
                AddRune (col, row, (Rune)' ');
            }
        }
    }

    //
    // Clears the contents of the selected region
    //
    private void ClearRegion ()
    {
        SetWrapModel ();

        long start, end;
        long currentEncoded = ((long)(uint)CurrentRow << 32) | (uint)CurrentColumn;
        GetEncodedRegionBounds (out start, out end);
        var startRow = (int)(start >> 32);
        var maxrow = (int)(end >> 32);
        var startCol = (int)(start & 0xffffffff);
        var endCol = (int)(end & 0xffffffff);
        List<Cell> line = _model.GetLine (startRow);

        _historyText.Add (new () { new (line) }, new (startCol, startRow));

        List<List<Cell>> removedLines = new ();

        if (startRow == maxrow)
        {
            removedLines.Add (new (line));

            line.RemoveRange (startCol, endCol - startCol);
            CurrentColumn = startCol;

            if (_wordWrap)
            {
                SetNeedsDraw ();
            }
            else
            {
                //QUESTION: Is the below comment still relevant?
                // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
                //SetNeedsDraw (new (0, startRow - topRow, Viewport.Width, startRow - topRow + 1));
                SetNeedsDraw ();
            }

            _historyText.Add (
                              new (removedLines),
                              CursorPosition,
                              TextEditingLineStatus.Removed
                             );

            UpdateWrapModel ();

            return;
        }

        removedLines.Add (new (line));

        line.RemoveRange (startCol, line.Count - startCol);
        List<Cell> line2 = _model.GetLine (maxrow);
        line.AddRange (line2.Skip (endCol));

        for (int row = startRow + 1; row <= maxrow; row++)
        {
            removedLines.Add (new (_model.GetLine (startRow + 1)));

            _model.RemoveLine (startRow + 1);
        }

        if (currentEncoded == end)
        {
            CurrentRow -= maxrow - startRow;
        }

        CurrentColumn = startCol;

        _historyText.Add (
                          new (removedLines),
                          CursorPosition,
                          TextEditingLineStatus.Removed
                         );

        UpdateWrapModel ();

        SetNeedsDraw ();
    }

    private void ClearSelectedRegion ()
    {
        SetWrapModel ();

        if (!_isReadOnly)
        {
            ClearRegion ();
        }

        UpdateWrapModel ();
        IsSelecting = false;
        DoNeededAction ();
    }

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) { KeyBindings.Replace (e.OldKey, e.NewKey); }

    private bool DeleteTextBackwards ()
    {
        SetWrapModel ();

        if (CurrentColumn > 0)
        {
            // Delete backwards 
            List<Cell> currentLine = GetCurrentLine ();

            _historyText.Add (new () { new (currentLine) }, CursorPosition);

            currentLine.RemoveAt (CurrentColumn - 1);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn--;

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (CurrentColumn < _leftColumn)
            {
                _leftColumn--;
                SetNeedsDraw ();
            }
            else
            {
                // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
                //SetNeedsDraw (new (0, currentRow - topRow, 1, Viewport.Width));
                SetNeedsDraw ();
            }
        }
        else
        {
            // Merges the current line with the previous one.
            if (CurrentRow == 0)
            {
                return true;
            }

            int prowIdx = CurrentRow - 1;
            List<Cell> prevRow = _model.GetLine (prowIdx);

            _historyText.Add (new () { new (prevRow) }, CursorPosition);

            List<List<Cell>> removedLines = new () { new (prevRow) };

            removedLines.Add (new (GetCurrentLine ()));

            _historyText.Add (
                              removedLines,
                              new (CurrentColumn, prowIdx),
                              TextEditingLineStatus.Removed
                             );

            int prevCount = prevRow.Count;
            _model.GetLine (prowIdx).AddRange (GetCurrentLine ());
            _model.RemoveLine (CurrentRow);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentRow--;

            _historyText.Add (
                              new () { GetCurrentLine () },
                              new (CurrentColumn, prowIdx),
                              TextEditingLineStatus.Replaced
                             );

            CurrentColumn = prevCount;
            SetNeedsDraw ();
        }

        UpdateWrapModel ();

        return false;
    }

    private bool DeleteTextForwards ()
    {
        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        if (CurrentColumn == currentLine.Count)
        {
            if (CurrentRow + 1 == _model.Count)
            {
                UpdateWrapModel ();

                return true;
            }

            _historyText.Add (new () { new (currentLine) }, CursorPosition);

            List<List<Cell>> removedLines = new () { new (currentLine) };

            List<Cell> nextLine = _model.GetLine (CurrentRow + 1);

            removedLines.Add (new (nextLine));

            _historyText.Add (removedLines, CursorPosition, TextEditingLineStatus.Removed);

            currentLine.AddRange (nextLine);
            _model.RemoveLine (CurrentRow + 1);

            _historyText.Add (
                              new () { new (currentLine) },
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, CurrentRow - _topRow + 1));
        }
        else
        {
            _historyText.Add ([ [.. currentLine]], CursorPosition);

            currentLine.RemoveAt (CurrentColumn);

            _historyText.Add (
                              [ [.. currentLine]],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            DoSetNeedsDraw (
                            new (
                                 CurrentColumn - _leftColumn,
                                 CurrentRow - _topRow,
                                 Viewport.Width,
                                 Math.Max (CurrentRow - _topRow + 1, 0)
                                )
                           );
        }

        UpdateWrapModel ();

        return false;
    }

    private void DoNeededAction ()
    {
        if (!NeedsDraw && (IsSelecting || _wrapNeeded || !Used))
        {
            SetNeedsDraw ();
        }

        if (NeedsDraw)
        {
            Adjust ();
        }
        else
        {
            PositionCursor ();
            OnUnwrappedCursorPosition ();
        }
    }

    private void DoSetNeedsDraw (Rectangle rect)
    {
        if (_wrapNeeded)
        {
            SetNeedsDraw ();
        }
        else
        {
            // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
            //SetNeedsDraw (rect);
            SetNeedsDraw ();
        }
    }

    private IEnumerable<(int col, int row, Cell rune)> ForwardIterator (int col, int row)
    {
        if (col < 0 || row < 0)
        {
            yield break;
        }

        if (row >= _model.Count)
        {
            yield break;
        }

        List<Cell> line = GetCurrentLine ();

        if (col >= line.Count)
        {
            yield break;
        }

        while (row < _model.Count)
        {
            for (int c = col; c < line.Count; c++)
            {
                yield return (c, row, line [c]);
            }

            col = 0;
            row++;
            line = GetCurrentLine ();
        }
    }

    private void GenerateSuggestions ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        int cursorPosition = Math.Min (CurrentColumn, currentLine.Count);

        Autocomplete.Context = new (
                                    currentLine,
                                    cursorPosition,
                                    Autocomplete.Context != null
                                        ? Autocomplete.Context.Canceled
                                        : false
                                   );

        Autocomplete.GenerateSuggestions (
                                          Autocomplete.Context
                                         );
    }

    // Returns an encoded region start..end (top 32 bits are the row, low32 the column)
    private void GetEncodedRegionBounds (
        out long start,
        out long end,
        int? startRow = null,
        int? startCol = null,
        int? cRow = null,
        int? cCol = null
    )
    {
        long selection;
        long point;

        if (startRow is null || startCol is null || cRow is null || cCol is null)
        {
            selection = ((long)(uint)_selectionStartRow << 32) | (uint)_selectionStartColumn;
            point = ((long)(uint)CurrentRow << 32) | (uint)CurrentColumn;
        }
        else
        {
            selection = ((long)(uint)startRow << 32) | (uint)startCol;
            point = ((long)(uint)cRow << 32) | (uint)cCol;
        }

        if (selection > point)
        {
            start = point;
            end = selection;
        }
        else
        {
            start = selection;
            end = point;
        }
    }

    //
    // Returns a string with the text in the selected 
    // region.
    //
    internal string GetRegion (
        out List<List<Cell>> cellsList,
        int? sRow = null,
        int? sCol = null,
        int? cRow = null,
        int? cCol = null,
        TextModel? model = null
    )
    {
        GetEncodedRegionBounds (out long start, out long end, sRow, sCol, cRow, cCol);

        cellsList = [];

        if (start == end)
        {
            return string.Empty;
        }

        var startRow = (int)(start >> 32);
        var maxRow = (int)(end >> 32);
        var startCol = (int)(start & 0xffffffff);
        var endCol = (int)(end & 0xffffffff);
        List<Cell> line = model is null ? _model.GetLine (startRow) : model.GetLine (startRow);
        List<Cell> cells;

        if (startRow == maxRow)
        {
            cells = line.GetRange (startCol, endCol - startCol);
            cellsList.Add (cells);

            return StringFromRunes (cells);
        }

        cells = line.GetRange (startCol, line.Count - startCol);
        cellsList.Add (cells);
        string res = StringFromRunes (cells);

        for (int row = startRow + 1; row < maxRow; row++)
        {
            cellsList.AddRange ([]);
            cells = model == null ? _model.GetLine (row) : model.GetLine (row);
            cellsList.Add (cells);

            res = res
                  + Environment.NewLine
                  + StringFromRunes (cells);
        }

        line = model is null ? _model.GetLine (maxRow) : model.GetLine (maxRow);
        cellsList.AddRange ([]);
        cells = line.GetRange (0, endCol);
        cellsList.Add (cells);
        res = res + Environment.NewLine + StringFromRunes (cells);

        return res;
    }

    private int GetSelectedLength () { return SelectedText.Length; }

    private string GetSelectedRegion ()
    {
        int cRow = CurrentRow;
        int cCol = CurrentColumn;
        int startRow = _selectionStartRow;
        int startCol = _selectionStartColumn;
        TextModel model = _model;

        if (_wordWrap)
        {
            cRow = _wrapManager!.GetModelLineFromWrappedLines (CurrentRow);
            cCol = _wrapManager.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
            startRow = _wrapManager.GetModelLineFromWrappedLines (_selectionStartRow);
            startCol = _wrapManager.GetModelColFromWrappedLines (_selectionStartRow, _selectionStartColumn);
            model = _wrapManager.Model;
        }

        OnUnwrappedCursorPosition (cRow, cCol);

        return GetRegion (out _, startRow, startCol, cRow, cCol, model);
    }

    private (int Row, int Col) GetUnwrappedPosition (int line, int col)
    {
        if (WordWrap)
        {
            return new ValueTuple<int, int> (
                                             _wrapManager!.GetModelLineFromWrappedLines (line),
                                             _wrapManager.GetModelColFromWrappedLines (line, col)
                                            );
        }

        return new ValueTuple<int, int> (line, col);
    }

    private void HistoryText_ChangeText (object sender, HistoryTextItemEventArgs obj)
    {
        SetWrapModel ();

        if (obj is { })
        {
            int startLine = obj.CursorPosition.Y;

            if (obj.RemovedOnAdded is { })
            {
                int offset;

                if (obj.IsUndoing)
                {
                    offset = Math.Max (obj.RemovedOnAdded.Lines.Count - obj.Lines.Count, 1);
                }
                else
                {
                    offset = obj.RemovedOnAdded.Lines.Count - 1;
                }

                for (var i = 0; i < offset; i++)
                {
                    if (Lines > obj.RemovedOnAdded.CursorPosition.Y)
                    {
                        _model.RemoveLine (obj.RemovedOnAdded.CursorPosition.Y);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            for (var i = 0; i < obj.Lines.Count; i++)
            {
                if (i == 0 || obj.LineStatus == TextEditingLineStatus.Original || obj.LineStatus == TextEditingLineStatus.Attribute)
                {
                    _model.ReplaceLine (startLine, obj.Lines [i]);
                }
                else if (obj is { IsUndoing: true, LineStatus: TextEditingLineStatus.Removed }
                                or { IsUndoing: false, LineStatus: TextEditingLineStatus.Added })
                {
                    _model.AddLine (startLine, obj.Lines [i]);
                }
                else if (Lines > obj.CursorPosition.Y + 1)
                {
                    _model.RemoveLine (obj.CursorPosition.Y + 1);
                }

                startLine++;
            }

            CursorPosition = obj.FinalCursorPosition;
        }

        UpdateWrapModel ();

        Adjust ();
        OnContentsChanged ();
    }

    private void Insert (Cell cell)
    {
        List<Cell> line = GetCurrentLine ();

        if (Used)
        {
            line.Insert (Math.Min (CurrentColumn, line.Count), cell);
        }
        else
        {
            if (CurrentColumn < line.Count)
            {
                line.RemoveAt (CurrentColumn);
            }

            line.Insert (Math.Min (CurrentColumn, line.Count), cell);
        }

        int prow = CurrentRow - _topRow;

        if (!_wrapNeeded)
        {
            // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
            //SetNeedsDraw (new (0, prow, Math.Max (Viewport.Width, 0), Math.Max (prow + 1, 0)));
            SetNeedsDraw ();
        }
    }

    private void InsertAllText (string text, bool fromClipboard = false)
    {
        if (string.IsNullOrEmpty (text))
        {
            return;
        }

        List<List<Cell>> lines;

        if (fromClipboard && text == _copiedText)
        {
            lines = _copiedCellsList;
        }
        else
        {
            // Get selected attribute
            Attribute? attribute = GetSelectedAttribute (CurrentRow, CurrentColumn);
            lines = Cell.StringToLinesOfCells (text, attribute);
        }

        if (lines.Count == 0)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> line = GetCurrentLine ();

        _historyText.Add ([new (line)], CursorPosition);

        // Optimize single line
        if (lines.Count == 1)
        {
            line.InsertRange (CurrentColumn, lines [0]);
            CurrentColumn += lines [0].Count;

            _historyText.Add (
                              [new (line)],
                              CursorPosition,
                              TextEditingLineStatus.Replaced
                             );

            if (!_wordWrap && CurrentColumn - _leftColumn > Viewport.Width)
            {
                _leftColumn = Math.Max (CurrentColumn - Viewport.Width + 1, 0);
            }

            if (_wordWrap)
            {
                SetNeedsDraw ();
            }
            else
            {
                // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
                //SetNeedsDraw (new (0, currentRow - topRow, Viewport.Width, Math.Max (currentRow - topRow + 1, 0)));
                SetNeedsDraw ();
            }

            UpdateWrapModel ();

            OnContentsChanged ();

            return;
        }

        List<Cell>? rest = null;
        var lastPosition = 0;

        if (_model.Count > 0 && line.Count > 0 && !_copyWithoutSelection)
        {
            // Keep a copy of the rest of the line
            int restCount = line.Count - CurrentColumn;
            rest = line.GetRange (CurrentColumn, restCount);
            line.RemoveRange (CurrentColumn, restCount);
        }

        // First line is inserted at the current location, the rest is appended
        line.InsertRange (CurrentColumn, lines [0]);

        //model.AddLine (currentRow, lines [0]);

        List<List<Cell>> addedLines = [new (line)];

        for (var i = 1; i < lines.Count; i++)
        {
            _model.AddLine (CurrentRow + i, lines [i]);

            addedLines.Add ([.. lines [i]]);
        }

        if (rest is { })
        {
            List<Cell> last = _model.GetLine (CurrentRow + lines.Count - 1);
            lastPosition = last.Count;
            last.InsertRange (last.Count, rest);

            addedLines.Last ().InsertRange (addedLines.Last ().Count, rest);
        }

        _historyText.Add (addedLines, CursorPosition, TextEditingLineStatus.Added);

        // Now adjust column and row positions
        CurrentRow += lines.Count - 1;
        CurrentColumn = rest is { } ? lastPosition : lines [^1].Count;
        Adjust ();

        _historyText.Add (
                          [new (line)],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();
        OnContentsChanged ();
    }

    private bool InsertText (Key a, Attribute? attribute = null)
    {
        //So that special keys like tab can be processed
        if (_isReadOnly)
        {
            return true;
        }

        SetWrapModel ();

        _historyText.Add ([new (GetCurrentLine ())], CursorPosition);

        if (IsSelecting)
        {
            ClearSelectedRegion ();
        }

        if ((uint)a.KeyCode == '\n')
        {
            _model.AddLine (CurrentRow + 1, []);
            CurrentRow++;
            CurrentColumn = 0;
        }
        else if ((uint)a.KeyCode == '\r')
        {
            CurrentColumn = 0;
        }
        else
        {
            if (Used)
            {
                Insert (new () { Rune = a.AsRune, Attribute = attribute });
                CurrentColumn++;

                if (CurrentColumn >= _leftColumn + Viewport.Width)
                {
                    _leftColumn++;
                    SetNeedsDraw ();
                }
            }
            else
            {
                Insert (new () { Rune = a.AsRune, Attribute = attribute });
                CurrentColumn++;
            }
        }

        _historyText.Add (
                          [new (GetCurrentLine ())],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();
        OnContentsChanged ();

        return true;
    }

    private void KillToEndOfLine ()
    {
        if (_isReadOnly)
        {
            return;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == currentLine.Count)
        {
            UpdateWrapModel ();

            DeleteTextForwards ();

            return;
        }

        _historyText.Add (new () { new (currentLine) }, CursorPosition);

        if (currentLine.Count == 0)
        {
            if (CurrentRow < _model.Count - 1)
            {
                List<List<Cell>> removedLines = new () { new (currentLine) };

                _model.RemoveLine (CurrentRow);

                removedLines.Add (new (GetCurrentLine ()));

                _historyText.Add (
                                  new (removedLines),
                                  CursorPosition,
                                  TextEditingLineStatus.Removed
                                 );
            }

            if (_model.Count > 0 || _lastWasKill)
            {
                string val = Environment.NewLine;

                if (_lastWasKill)
                {
                    AppendClipboard (val);
                }
                else
                {
                    SetClipboard (val);
                }
            }

            if (_model.Count == 0)
            {
                // Prevents from adding line feeds if there is no more lines.
                setLastWasKill = false;
            }
        }
        else
        {
            int restCount = currentLine.Count - CurrentColumn;
            List<Cell> rest = currentLine.GetRange (CurrentColumn, restCount);
            var val = string.Empty;
            val += StringFromRunes (rest);

            if (_lastWasKill)
            {
                AppendClipboard (val);
            }
            else
            {
                SetClipboard (val);
            }

            currentLine.RemoveRange (CurrentColumn, restCount);
        }

        _historyText.Add (
                          [ [.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));

        _lastWasKill = setLastWasKill;
        DoNeededAction ();
    }

    private void KillToLeftStart ()
    {
        if (_isReadOnly)
        {
            return;
        }

        if (_model.Count == 1 && GetCurrentLine ().Count == 0)
        {
            // Prevents from adding line feeds if there is no more lines.
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();
        var setLastWasKill = true;

        if (currentLine.Count > 0 && CurrentColumn == 0)
        {
            UpdateWrapModel ();

            DeleteTextBackwards ();

            return;
        }

        _historyText.Add ([ [.. currentLine]], CursorPosition);

        if (currentLine.Count == 0)
        {
            if (CurrentRow > 0)
            {
                _model.RemoveLine (CurrentRow);

                if (_model.Count > 0 || _lastWasKill)
                {
                    string val = Environment.NewLine;

                    if (_lastWasKill)
                    {
                        AppendClipboard (val);
                    }
                    else
                    {
                        SetClipboard (val);
                    }
                }

                if (_model.Count == 0)
                {
                    // Prevents from adding line feeds if there is no more lines.
                    setLastWasKill = false;
                }

                CurrentRow--;
                currentLine = _model.GetLine (CurrentRow);

                List<List<Cell>> removedLine =
                [
                    [..currentLine],
                    []
                ];

                _historyText.Add (
                                  [.. removedLine],
                                  CursorPosition,
                                  TextEditingLineStatus.Removed
                                 );

                CurrentColumn = currentLine.Count;
            }
        }
        else
        {
            int restCount = CurrentColumn;
            List<Cell> rest = currentLine.GetRange (0, restCount);
            var val = string.Empty;
            val += StringFromRunes (rest);

            if (_lastWasKill)
            {
                AppendClipboard (val);
            }
            else
            {
                SetClipboard (val);
            }

            currentLine.RemoveRange (0, restCount);
            CurrentColumn = 0;
        }

        _historyText.Add (
                          [ [.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));

        _lastWasKill = setLastWasKill;
        DoNeededAction ();
    }

    private void KillWordBackward ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([ [.. GetCurrentLine ()]], CursorPosition);

        if (CurrentColumn == 0)
        {
            DeleteTextBackwards ();

            _historyText.ReplaceLast (
                                      [ [.. GetCurrentLine ()]],
                                      CursorPosition,
                                      TextEditingLineStatus.Replaced
                                     );

            UpdateWrapModel ();

            return;
        }

        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            int restCount = CurrentColumn - newPos.Value.col;
            currentLine.RemoveRange (newPos.Value.col, restCount);

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn = newPos.Value.col;
        }
        else if (newPos.HasValue)
        {
            int restCount;

            if (newPos.Value.row == CurrentRow)
            {
                restCount = currentLine.Count - CurrentColumn;
                currentLine.RemoveRange (CurrentColumn, restCount);
            }
            else
            {
                while (CurrentRow != newPos.Value.row)
                {
                    restCount = currentLine.Count;
                    currentLine.RemoveRange (0, restCount);

                    CurrentRow--;
                    currentLine = GetCurrentLine ();
                }
            }

            if (_wordWrap)
            {
                _wrapNeeded = true;
            }

            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        _historyText.Add (
                          [ [.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));
        DoNeededAction ();
    }

    private void KillWordForward ()
    {
        if (_isReadOnly)
        {
            return;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add ([ [.. GetCurrentLine ()]], CursorPosition);

        if (currentLine.Count == 0 || CurrentColumn == currentLine.Count)
        {
            DeleteTextForwards ();

            _historyText.ReplaceLast (
                                      [ [.. GetCurrentLine ()]],
                                      CursorPosition,
                                      TextEditingLineStatus.Replaced
                                     );

            UpdateWrapModel ();

            return;
        }

        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);
        var restCount = 0;

        if (newPos.HasValue && CurrentRow == newPos.Value.row)
        {
            restCount = newPos.Value.col - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
        }
        else if (newPos.HasValue)
        {
            restCount = currentLine.Count - CurrentColumn;
            currentLine.RemoveRange (CurrentColumn, restCount);
        }

        if (_wordWrap)
        {
            _wrapNeeded = true;
        }

        _historyText.Add (
                          [ [.. GetCurrentLine ()]],
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        UpdateWrapModel ();

        DoSetNeedsDraw (new (0, CurrentRow - _topRow, Viewport.Width, Viewport.Height));
        DoNeededAction ();
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

    private void MoveBottomEnd ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveEnd ();
    }

    private void MoveBottomEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveEnd ();
    }

    private bool MoveDown ()
    {
        if (CurrentRow + 1 < _model.Count)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow++;

            if (CurrentRow >= _topRow + Viewport.Height)
            {
                _topRow++;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }
        else if (CurrentRow > Viewport.Height)
        {
            Adjust ();
        }
        else
        {
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private void MoveEndOfLine ()
    {
        List<Cell> currentLine = GetCurrentLine ();
        CurrentColumn = currentLine.Count;
        DoNeededAction ();
    }

    private bool MoveLeft ()
    {
        if (CurrentColumn > 0)
        {
            CurrentColumn--;
        }
        else
        {
            if (CurrentRow > 0)
            {
                CurrentRow--;

                if (CurrentRow < _topRow)
                {
                    _topRow--;
                    SetNeedsDraw ();
                }

                List<Cell> currentLine = GetCurrentLine ();
                CurrentColumn = Math.Max (currentLine.Count - (ReadOnly ? 1 : 0), 0);
            }
            else
            {
                return false;
            }
        }

        DoNeededAction ();

        return true;
    }

    private void MovePageDown ()
    {
        int nPageDnShift = Viewport.Height - 1;

        if (CurrentRow >= 0 && CurrentRow < _model.Count)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow = CurrentRow + nPageDnShift > _model.Count
                             ? _model.Count > 0 ? _model.Count - 1 : 0
                             : CurrentRow + nPageDnShift;

            if (_topRow < CurrentRow - nPageDnShift)
            {
                _topRow = CurrentRow >= _model.Count
                              ? CurrentRow - nPageDnShift
                              : _topRow + nPageDnShift;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }

        DoNeededAction ();
    }

    private void MovePageUp ()
    {
        int nPageUpShift = Viewport.Height - 1;

        if (CurrentRow > 0)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow = CurrentRow - nPageUpShift < 0 ? 0 : CurrentRow - nPageUpShift;

            if (CurrentRow < _topRow)
            {
                _topRow = _topRow - nPageUpShift < 0 ? 0 : _topRow - nPageUpShift;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }

        DoNeededAction ();
    }

    private bool MoveRight ()
    {
        List<Cell> currentLine = GetCurrentLine ();

        if ((ReadOnly ? CurrentColumn + 1 : CurrentColumn) < currentLine.Count)
        {
            CurrentColumn++;
        }
        else
        {
            if (CurrentRow + 1 < _model.Count)
            {
                CurrentRow++;
                CurrentColumn = 0;

                if (CurrentRow >= _topRow + Viewport.Height)
                {
                    _topRow++;
                    SetNeedsDraw ();
                }
            }
            else
            {
                return false;
            }
        }

        DoNeededAction ();

        return true;
    }

    private void MoveLeftStart ()
    {
        if (_leftColumn > 0)
        {
            SetNeedsDraw ();
        }

        CurrentColumn = 0;
        _leftColumn = 0;
        DoNeededAction ();
    }

    private void MoveTopHome ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveHome ();
    }

    private void MoveTopHomeExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveHome ();
    }

    private bool MoveUp ()
    {
        if (CurrentRow > 0)
        {
            if (_columnTrack == -1)
            {
                _columnTrack = CurrentColumn;
            }

            CurrentRow--;

            if (CurrentRow < _topRow)
            {
                _topRow--;
                SetNeedsDraw ();
            }

            TrackColumn ();
            PositionCursor ();
        }
        else
        {
            return false;
        }

        DoNeededAction ();

        return true;
    }

    private void MoveWordBackward ()
    {
        (int col, int row)? newPos = _model.WordBackward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        DoNeededAction ();
    }

    private void MoveWordForward ()
    {
        (int col, int row)? newPos = _model.WordForward (CurrentColumn, CurrentRow, UseSameRuneTypeForWords);

        if (newPos.HasValue)
        {
            CurrentColumn = newPos.Value.col;
            CurrentRow = newPos.Value.row;
        }

        DoNeededAction ();
    }

    private (int width, int height) OffSetBackground ()
    {
        var w = 0;
        var h = 0;

        if (SuperView?.Viewport.Right - Viewport.Right < 0)
        {
            w = SuperView!.Viewport.Right - Viewport.Right - 1;
        }

        if (SuperView?.Viewport.Bottom - Viewport.Bottom < 0)
        {
            h = SuperView!.Viewport.Bottom - Viewport.Bottom - 1;
        }

        return (w, h);
    }

    private bool PointInSelection (int col, int row)
    {
        long start, end;
        GetEncodedRegionBounds (out start, out end);
        long q = ((long)(uint)row << 32) | (uint)col;

        return q >= start && q <= end - 1;
    }

    private void ProcessAutocomplete ()
    {
        if (_isDrawing)
        {
            return;
        }

        if (_clickWithSelecting)
        {
            _clickWithSelecting = false;

            return;
        }

        if (SelectedLength > 0)
        {
            return;
        }

        // draw autocomplete
        GenerateSuggestions ();

        var renderAt = new Point (
                                  Autocomplete.Context.CursorPosition,
                                  Autocomplete.PopupInsideContainer
                                      ? CursorPosition.Y + 1 - TopRow
                                      : 0
                                 );

        Autocomplete.RenderOverlay (renderAt);
    }

    private bool ProcessBackTab ()
    {
        ResetColumnTrack ();

        if (!AllowsTab || _isReadOnly)
        {
            return false;
        }

        if (CurrentColumn > 0)
        {
            SetWrapModel ();

            List<Cell> currentLine = GetCurrentLine ();

            if (currentLine.Count > 0 && currentLine [CurrentColumn - 1].Rune.Value == '\t')
            {
                _historyText.Add (new () { new (currentLine) }, CursorPosition);

                currentLine.RemoveAt (CurrentColumn - 1);
                CurrentColumn--;

                _historyText.Add (
                                  new () { new (GetCurrentLine ()) },
                                  CursorPosition,
                                  TextEditingLineStatus.Replaced
                                 );
            }

            SetNeedsDraw ();

            UpdateWrapModel ();
        }

        DoNeededAction ();

        return true;
    }

    private void ProcessCopy ()
    {
        ResetColumnTrack ();
        Copy ();
    }

    private void ProcessCut ()
    {
        ResetColumnTrack ();
        Cut ();
    }

    private void ProcessDeleteCharLeft ()
    {
        ResetColumnTrack ();
        DeleteCharLeft ();
    }

    private void ProcessDeleteCharRight ()
    {
        ResetColumnTrack ();
        DeleteCharRight ();
    }

    private Attribute? GetSelectedAttribute (int row, int col)
    {
        if (!InheritsPreviousAttribute || (Lines == 1 && GetLine (Lines).Count == 0))
        {
            return null;
        }

        List<Cell> line = GetLine (row);
        int foundRow = row;

        while (line.Count == 0)
        {
            if (foundRow == 0 && line.Count == 0)
            {
                return null;
            }

            foundRow--;
            line = GetLine (foundRow);
        }

        int foundCol = foundRow < row ? line.Count - 1 : Math.Min (col, line.Count - 1);

        Cell cell = line [foundCol];

        return cell.Attribute;
    }

    // If InheritsPreviousScheme is enabled this method will check if the rune cell on
    // the row and col location and around has a not null scheme. If it's null will set it with
    // the very most previous valid scheme.
    private void ProcessInheritsPreviousScheme (int row, int col)
    {
        if (!InheritsPreviousAttribute || (Lines == 1 && GetLine (Lines).Count == 0))
        {
            return;
        }

        List<Cell> line = GetLine (row);
        List<Cell> lineToSet = line;

        while (line.Count == 0)
        {
            if (row == 0 && line.Count == 0)
            {
                return;
            }

            row--;
            line = GetLine (row);
            lineToSet = line;
        }

        int colWithColor = Math.Max (Math.Min (col - 2, line.Count - 1), 0);
        Cell cell = line [colWithColor];
        int colWithoutColor = Math.Max (col - 1, 0);

        Cell lineTo = lineToSet [colWithoutColor];

        if (cell.Attribute is { } && colWithColor == 0 && lineTo.Attribute is { })
        {
            for (int r = row - 1; r > -1; r--)
            {
                List<Cell> l = GetLine (r);

                for (int c = l.Count - 1; c > -1; c--)
                {
                    Cell cell1 = l [c];

                    if (cell1.Attribute is null)
                    {
                        cell1.Attribute = cell.Attribute;
                        l [c] = cell1;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            return;
        }

        if (cell.Attribute is null)
        {
            for (int r = row; r > -1; r--)
            {
                List<Cell> l = GetLine (r);

                colWithColor = l.FindLastIndex (
                                                colWithColor > -1 ? colWithColor : l.Count - 1,
                                                c => c.Attribute != null
                                               );

                if (colWithColor > -1 && l [colWithColor].Attribute is { })
                {
                    cell = l [colWithColor];

                    break;
                }
            }
        }
        else
        {
            int cRow = row;

            while (cell.Attribute is null)
            {
                if ((colWithColor == 0 || cell.Attribute is null) && cRow > 0)
                {
                    line = GetLine (--cRow);
                    colWithColor = line.Count - 1;
                    cell = line [colWithColor];
                }
                else if (cRow == 0 && colWithColor < line.Count)
                {
                    cell = line [colWithColor + 1];
                }
            }
        }

        if (cell.Attribute is { } && colWithColor > -1 && colWithoutColor < lineToSet.Count && lineTo.Attribute is null)
        {
            while (lineTo.Attribute is null)
            {
                lineTo.Attribute = cell.Attribute;
                lineToSet [colWithoutColor] = lineTo;
                colWithoutColor--;

                if (colWithoutColor == -1 && row > 0)
                {
                    lineToSet = GetLine (--row);
                    colWithoutColor = lineToSet.Count - 1;
                }
            }
        }
    }

    private void ProcessKillWordBackward ()
    {
        ResetColumnTrack ();
        KillWordBackward ();
    }

    private void ProcessKillWordForward ()
    {
        ResetColumnTrack ();
        StopSelecting ();
        KillWordForward ();
    }

    private void ProcessMouseClick (MouseEventArgs ev, out List<Cell> line)
    {
        List<Cell>? r = null;

        if (_model.Count > 0)
        {
            int maxCursorPositionableLine = Math.Max (_model.Count - 1 - _topRow, 0);

            if (Math.Max (ev.Position.Y, 0) > maxCursorPositionableLine)
            {
                CurrentRow = maxCursorPositionableLine + _topRow;
            }
            else
            {
                CurrentRow = Math.Max (ev.Position.Y + _topRow, 0);
            }

            r = GetCurrentLine ();
            int idx = TextModel.GetColFromX (r, _leftColumn, Math.Max (ev.Position.X, 0), TabWidth);

            if (idx - _leftColumn >= r.Count)
            {
                CurrentColumn = Math.Max (r.Count - _leftColumn - (ReadOnly ? 1 : 0), 0);
            }
            else
            {
                CurrentColumn = idx + _leftColumn;
            }
        }

        line = r!;
    }

    private bool ProcessMoveDown ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveDown ();
    }

    private void ProcessMoveDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveDown ();
    }

    private void ProcessMoveEndOfLine ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveEndOfLine ();
    }

    private void ProcessMoveRightEndExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveEndOfLine ();
    }

    private bool ProcessMoveLeft ()
    {
        // if the user presses Left (without any control keys) and they are at the start of the text
        if (CurrentColumn == 0 && CurrentRow == 0)
        {
            if (IsSelecting)
            {
                StopSelecting ();

                return true;
            }

            // do not respond (this lets the key press fall through to navigation system - which usually changes focus backward)
            return false;
        }

        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveLeft ();

        return true;
    }

    private void ProcessMoveLeftExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveLeft ();
    }

    private bool ProcessMoveRight ()
    {
        // if the user presses Right (without any control keys)
        // determine where the last cursor position in the text is
        int lastRow = _model.Count - 1;
        int lastCol = _model.GetLine (lastRow).Count;

        // if they are at the very end of all the text do not respond (this lets the key press fall through to navigation system - which usually changes focus forward)
        if (CurrentColumn == lastCol && CurrentRow == lastRow)
        {
            // Unless they have text selected
            if (IsSelecting)
            {
                // In which case clear
                StopSelecting ();

                return true;
            }

            return false;
        }

        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveRight ();

        return true;
    }

    private void ProcessMoveRightExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveRight ();
    }

    private void ProcessMoveLeftStart ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveLeftStart ();
    }

    private void ProcessMoveLeftStartExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveLeftStart ();
    }

    private bool ProcessMoveUp ()
    {
        ResetContinuousFindTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        return MoveUp ();
    }

    private void ProcessMoveUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MoveUp ();
    }

    private void ProcessMoveWordBackward ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveWordBackward ();
    }

    private void ProcessMoveWordBackwardExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveWordBackward ();
    }

    private void ProcessMoveWordForward ()
    {
        ResetAllTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MoveWordForward ();
    }

    private void ProcessMoveWordForwardExtend ()
    {
        ResetAllTrack ();
        StartSelecting ();
        MoveWordForward ();
    }

    private void ProcessPageDown ()
    {
        ResetColumnTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MovePageDown ();
    }

    private void ProcessPageDownExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MovePageDown ();
    }

    private void ProcessPageUp ()
    {
        ResetColumnTrack ();

        if (_shiftSelecting && IsSelecting)
        {
            StopSelecting ();
        }

        MovePageUp ();
    }

    private void ProcessPageUpExtend ()
    {
        ResetColumnTrack ();
        StartSelecting ();
        MovePageUp ();
    }

    private void ProcessPaste ()
    {
        ResetColumnTrack ();

        if (_isReadOnly)
        {
            return;
        }

        Paste ();
    }

    private bool ProcessEnterKey (ICommandContext? commandContext)
    {
        ResetColumnTrack ();

        if (_isReadOnly)
        {
            return false;
        }

        if (!AllowsReturn)
        {
            // By Default pressing ENTER should be ignored (OnAccept will return false or null). Only cancel if the
            // event was fired and set Cancel = true.
            return RaiseAccepting (commandContext) is null or false;
        }

        SetWrapModel ();

        List<Cell> currentLine = GetCurrentLine ();

        _historyText.Add (new () { new (currentLine) }, CursorPosition);

        if (IsSelecting)
        {
            ClearSelectedRegion ();
            currentLine = GetCurrentLine ();
        }

        int restCount = currentLine.Count - CurrentColumn;
        List<Cell> rest = currentLine.GetRange (CurrentColumn, restCount);
        currentLine.RemoveRange (CurrentColumn, restCount);

        List<List<Cell>> addedLines = new () { new (currentLine) };

        _model.AddLine (CurrentRow + 1, rest);

        addedLines.Add (new (_model.GetLine (CurrentRow + 1)));

        _historyText.Add (addedLines, CursorPosition, TextEditingLineStatus.Added);

        CurrentRow++;

        var fullNeedsDraw = false;

        if (CurrentRow >= _topRow + Viewport.Height)
        {
            _topRow++;
            fullNeedsDraw = true;
        }

        CurrentColumn = 0;

        _historyText.Add (
                          new () { new (GetCurrentLine ()) },
                          CursorPosition,
                          TextEditingLineStatus.Replaced
                         );

        if (!_wordWrap && CurrentColumn < _leftColumn)
        {
            fullNeedsDraw = true;
            _leftColumn = 0;
        }

        if (fullNeedsDraw)
        {
            SetNeedsDraw ();
        }
        else
        {
            // BUGBUG: customized rect aren't supported now because the Redraw isn't using the Intersect method.
            //SetNeedsDraw (new (0, currentRow - topRow, 2, Viewport.Height));
            SetNeedsDraw ();
        }

        UpdateWrapModel ();

        DoNeededAction ();
        OnContentsChanged ();

        return true;
    }

    private void ProcessSelectAll ()
    {
        ResetColumnTrack ();
        SelectAll ();
    }

    private void ProcessSetOverwrite ()
    {
        ResetColumnTrack ();
        SetOverwrite (!Used);
    }

    private bool ProcessTab ()
    {
        ResetColumnTrack ();

        if (!AllowsTab || _isReadOnly)
        {
            return false;
        }

        InsertText (new Key ((KeyCode)'\t'));
        DoNeededAction ();

        return true;
    }

    private void ResetAllTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _columnTrack = -1;
        _continuousFind = false;
    }

    private void ResetColumnTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _columnTrack = -1;
    }

    private void ResetContinuousFind ()
    {
        if (!_continuousFind)
        {
            int col = IsSelecting ? _selectionStartColumn : CurrentColumn;
            int row = IsSelecting ? _selectionStartRow : CurrentRow;
            _model.ResetContinuousFind (new (col, row));
        }
    }

    private void ResetContinuousFindTrack ()
    {
        // Handle some state here - whether the last command was a kill
        // operation and the column tracking (up/down)
        _lastWasKill = false;
        _continuousFind = false;
    }

    private void ResetPosition ()
    {
        _topRow = _leftColumn = CurrentRow = CurrentColumn = 0;
        StopSelecting ();
    }

    private void SetClipboard (string text)
    {
        if (text is { })
        {
            Clipboard.Contents = text;
        }
    }

    private bool SetFoundText (
        string text,
        (Point current, bool found) foundPos,
        string? textToReplace = null,
        bool replace = false,
        bool replaceAll = false
    )
    {
        if (foundPos.found)
        {
            StartSelecting ();
            _selectionStartColumn = foundPos.current.X;
            _selectionStartRow = foundPos.current.Y;

            if (!replaceAll)
            {
                CurrentColumn = _selectionStartColumn + text.GetRuneCount ();
            }
            else
            {
                CurrentColumn = _selectionStartColumn + textToReplace!.GetRuneCount ();
            }

            CurrentRow = foundPos.current.Y;

            if (!_isReadOnly && replace)
            {
                Adjust ();
                ClearSelectedRegion ();
                InsertAllText (textToReplace!);
                StartSelecting ();
                _selectionStartColumn = CurrentColumn - textToReplace!.GetRuneCount ();
            }
            else
            {
                UpdateWrapModel ();
                SetNeedsDraw ();
                Adjust ();
            }

            _continuousFind = true;

            return foundPos.found;
        }

        UpdateWrapModel ();
        _continuousFind = false;

        return foundPos.found;
    }

    private void SetOverwrite (bool overwrite)
    {
        Used = overwrite;
        SetNeedsDraw ();
        DoNeededAction ();
    }

    private void SetValidUsedColor (Attribute? attribute)
    {
        // BUGBUG: (v2 truecolor) This code depends on 8-bit color names; disabling for now
        //if ((scheme!.HotNormal.Foreground & scheme.Focus.Background) == scheme.Focus.Foreground) {
        SetAttribute (new (attribute!.Value.Background, attribute!.Value.Foreground, attribute!.Value.Style));
    }

    /// <summary>Restore from original model.</summary>
    private void SetWrapModel ([CallerMemberName] string? caller = null)
    {
        if (_currentCaller is { })
        {
            return;
        }

        if (_wordWrap)
        {
            _currentCaller = caller;

            CurrentColumn = _wrapManager!.GetModelColFromWrappedLines (CurrentRow, CurrentColumn);
            CurrentRow = _wrapManager.GetModelLineFromWrappedLines (CurrentRow);

            _selectionStartColumn =
                _wrapManager.GetModelColFromWrappedLines (_selectionStartRow, _selectionStartColumn);
            _selectionStartRow = _wrapManager.GetModelLineFromWrappedLines (_selectionStartRow);
            _model = _wrapManager.Model;
        }
    }

    private void ShowContextMenu (bool keyboard)
    {
        if (!Equals (_currentCulture, Thread.CurrentThread.CurrentUICulture))
        {
            _currentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        ContextMenu?.MakeVisible (ViewportToScreen (new Point (CursorPosition.X, CursorPosition.Y)));
    }

    private void StartSelecting ()
    {
        if (_shiftSelecting && IsSelecting)
        {
            return;
        }

        _shiftSelecting = true;
        IsSelecting = true;
        _selectionStartColumn = CurrentColumn;
        _selectionStartRow = CurrentRow;
    }

    private void StopSelecting ()
    {
        if (IsSelecting)
        {
            SetNeedsDraw ();
        }

        _shiftSelecting = false;
        IsSelecting = false;
        _isButtonShift = false;
    }

    private string StringFromRunes (List<Cell> cells)
    {
        if (cells is null)
        {
            throw new ArgumentNullException (nameof (cells));
        }

        var size = 0;

        foreach (Cell cell in cells)
        {
            size += cell.Rune.GetEncodingLength ();
        }

        var encoded = new byte [size];
        var offset = 0;

        foreach (Cell cell in cells)
        {
            offset += cell.Rune.Encode (encoded, offset);
        }

        return StringExtensions.ToString (encoded);
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

    private void TextView_Initialized (object sender, EventArgs e)
    {
        if (Autocomplete.HostControl is null)
        {
            Autocomplete.HostControl = this;
        }

        OnContentsChanged ();
    }

    private void TextView_LayoutComplete (object? sender, LayoutEventArgs e)
    {
        WrapTextModel ();
        Adjust ();
    }

    private void ToggleSelecting ()
    {
        ResetColumnTrack ();
        IsSelecting = !IsSelecting;
        _selectionStartColumn = CurrentColumn;
        _selectionStartRow = CurrentRow;
    }

    // Tries to snap the cursor to the tracking column
    private void TrackColumn ()
    {
        // Now track the column
        List<Cell> line = GetCurrentLine ();

        if (line.Count < _columnTrack)
        {
            CurrentColumn = line.Count;
        }
        else if (_columnTrack != -1)
        {
            CurrentColumn = _columnTrack;
        }
        else if (CurrentColumn > line.Count)
        {
            CurrentColumn = line.Count;
        }

        Adjust ();
    }

    /// <summary>Update the original model.</summary>
    private void UpdateWrapModel ([CallerMemberName] string? caller = null)
    {
        if (_currentCaller is { } && _currentCaller != caller)
        {
            return;
        }

        if (_wordWrap)
        {
            _currentCaller = null;

            _wrapManager!.UpdateModel (
                                       _model,
                                       out int nRow,
                                       out int nCol,
                                       out int nStartRow,
                                       out int nStartCol,
                                       CurrentRow,
                                       CurrentColumn,
                                       _selectionStartRow,
                                       _selectionStartColumn,
                                       true
                                      );
            CurrentRow = nRow;
            CurrentColumn = nCol;
            _selectionStartRow = nStartRow;
            _selectionStartColumn = nStartCol;
            _wrapNeeded = true;

            SetNeedsDraw ();
        }

        if (_currentCaller is { })
        {
            throw new InvalidOperationException (
                                                 $"WordWrap settings was changed after the {_currentCaller} call."
                                                );
        }
    }

    private void WrapTextModel ()
    {
        if (_wordWrap && _wrapManager is { })
        {
            _model = _wrapManager.WrapModel (
                                             Math.Max (Viewport.Width - (ReadOnly ? 0 : 1), 0), // For the cursor on the last column of a line
                                             out int nRow,
                                             out int nCol,
                                             out int nStartRow,
                                             out int nStartCol,
                                             CurrentRow,
                                             CurrentColumn,
                                             _selectionStartRow,
                                             _selectionStartColumn,
                                             _tabWidth
                                            );
            CurrentRow = nRow;
            CurrentColumn = nCol;
            _selectionStartRow = nStartRow;
            _selectionStartColumn = nStartCol;
            SetNeedsDraw ();
        }
    }

    /// <inheritdoc />
    public bool EnableForDesign ()
    {
        Text = """
               TextView provides a fully featured multi-line text editor.
               It supports word wrap and history for undo.
               """;

        return true;
    }


    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing && ContextMenu is { })
        {
            ContextMenu.Visible = false;
            ContextMenu.Dispose ();
            ContextMenu = null;
        }

        base.Dispose (disposing);
    }
}

/// <summary>
///     Renders an overlay on another view at a given point that allows selecting from a range of 'autocomplete'
///     options. An implementation on a TextView.
/// </summary>
public class TextViewAutocomplete : PopupAutocomplete
{
    /// <inheritdoc/>
    protected override void DeleteTextBackwards () { ((TextView)HostControl).DeleteCharLeft (); }

    /// <inheritdoc/>
    protected override void InsertText (string accepted) { ((TextView)HostControl).InsertText (accepted); }

    /// <inheritdoc/>
    protected override void SetCursorPosition (int column)
    {
        ((TextView)HostControl).CursorPosition =
            new (column, ((TextView)HostControl).CurrentRow);
    }
}
