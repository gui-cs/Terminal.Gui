using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Terminal.Gui.Resources;

namespace Terminal.Gui;

/// <summary>
/// Single-line text entry <see cref="View"/>
/// </summary>
/// <remarks>
/// The <see cref="TextField"/> <see cref="View"/> provides editing functionality and mouse support.
/// </remarks>
public class TextField : View {
	CultureInfo _currentCulture;

	CursorVisibility _desiredCursorVisibility = CursorVisibility.Default;
	int _cursorPosition;
	readonly HistoryText _historyText = new ();
	bool _isButtonPressed;
	bool _isButtonReleased = true;

	bool _isDrawing;

	int _preTextChangedCursorPos;

	CursorVisibility _savedCursorVisibility;
	int _selectedStart = -1; // -1 represents there is no text selection.
	string _selectedText;

	int _start;
	List<Rune> _text;

	CursorVisibility _visibility;

	/// <summary>
	/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
	/// </summary>
	public TextField () : this (string.Empty) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Computed"/> positioning.
	/// </summary>
	/// <param name="text">Initial text contents.</param>
	public TextField (string text) : base (text) => SetInitialProperties (text, text.GetRuneCount () + 1);

	/// <summary>
	/// Initializes a new instance of the <see cref="TextField"/> class using <see cref="LayoutStyle.Absolute"/> positioning.
	/// </summary>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="w">The width.</param>
	/// <param name="text">Initial text contents.</param>
	public TextField (int x, int y, int w, string text) : base (new Rect (x, y, w, 1)) => SetInitialProperties (text, w);

	/// <summary>
	/// Gets or sets the text to render in control when no value has
	/// been entered yet and the <see cref="View"/> does not yet have
	/// input focus.
	/// </summary>
	public string Caption { get; set; }

	/// <summary>
	/// Gets or sets the foreground <see cref="Color"/> to use when
	/// rendering <see cref="Caption"/>.
	/// </summary>
	public Color CaptionColor { get; set; } = new (Color.DarkGray);

	/// <summary>
	/// Tracks whether the text field should be considered "used", that is, that the user has moved in the entry, so new input
	/// should be appended at the cursor position, rather than clearing the entry
	/// </summary>
	public bool Used { get; set; }

	/// <summary>
	/// If set to true its not allow any changes in the text.
	/// </summary>
	public bool ReadOnly { get; set; } = false;

	/// <summary>
	/// Provides autocomplete context menu based on suggestions at the current cursor
	/// position. Configure <see cref="ISuggestionGenerator"/> to enable this feature.
	/// </summary>
	public IAutocomplete Autocomplete { get; set; } = new TextFieldAutocomplete ();

	/// <summary>
	/// Sets or gets the text held by the view.
	/// </summary>
	public new string Text {
		get => StringExtensions.ToString (_text);
		set {
			var oldText = StringExtensions.ToString (_text);

			if (oldText == value) {
				return;
			}

			var newText = OnTextChanging (value.Replace ("\t", "").Split ("\n") [0]);
			if (newText.Cancel) {
				if (_cursorPosition > _text.Count) {
					_cursorPosition = _text.Count;
				}
				return;
			}
			ClearAllSelection ();
			_text = newText.NewText.EnumerateRunes ().ToList ();

			if (!Secret && !_historyText.IsFromHistory) {
				_historyText.Add (new List<List<RuneCell>> { TextModel.ToRuneCellList (oldText) },
					new Point (_cursorPosition, 0));
				_historyText.Add (new List<List<RuneCell>> { TextModel.ToRuneCells (_text) }, new Point (_cursorPosition, 0)
					, HistoryText.LineStatus.Replaced);
			}

			TextChanged?.Invoke (this, new TextChangedEventArgs (oldText));

			ProcessAutocomplete ();

			if (_cursorPosition > _text.Count) {
				_cursorPosition = Math.Max (TextModel.DisplaySize (_text, 0).size - 1, 0);
			}

			Adjust ();
			SetNeedsDisplay ();
		}
	}

	/// <summary>
	/// Sets the secret property.
	/// <remarks>
	/// This makes the text entry suitable for entering passwords.
	/// </remarks>
	/// </summary>
	public bool Secret { get; set; }

	/// <summary>
	/// Sets or gets the current cursor position.
	/// </summary>
	public virtual int CursorPosition {
		get => _cursorPosition;
		set {
			if (value < 0) {
				_cursorPosition = 0;
			} else if (value > _text.Count) {
				_cursorPosition = _text.Count;
			} else {
				_cursorPosition = value;
			}
			PrepareSelection (_selectedStart, _cursorPosition - _selectedStart);
		}
	}

	/// <summary>
	/// Gets the left offset position.
	/// </summary>
	public int ScrollOffset { get; private set; }

	/// <summary>
	/// Indicates whatever the text was changed or not.
	/// <see langword="true"/> if the text was changed <see langword="false"/> otherwise.
	/// </summary>
	public bool IsDirty => _historyText.IsDirty (Text);

	/// <summary>
	/// Indicates whatever the text has history changes or not.
	/// <see langword="true"/> if the text has history changes <see langword="false"/> otherwise.
	/// </summary>
	public bool HasHistoryChanges => _historyText.HasHistoryChanges;

	/// <summary>
	/// Get the <see cref="ContextMenu"/> for this view.
	/// </summary>
	public ContextMenu ContextMenu { get; private set; }

	///<inheritdoc/>
	public override bool CanFocus {
		get => base.CanFocus;
		set => base.CanFocus = value;
	}

	/// <summary>
	/// Start position of the selected text.
	/// </summary>
	public int SelectedStart {
		get => _selectedStart;
		set {
			if (value < -1) {
				_selectedStart = -1;
			} else if (value > _text.Count) {
				_selectedStart = _text.Count;
			} else {
				_selectedStart = value;
			}
			PrepareSelection (_selectedStart, _cursorPosition - _selectedStart);
		}
	}

	/// <summary>
	/// Length of the selected text.
	/// </summary>
	public int SelectedLength { get; private set; }

	/// <summary>
	/// The selected text.
	/// </summary>
	public string SelectedText {
		get => Secret ? null : _selectedText;
		private set => _selectedText = value;
	}

	/// <summary>
	/// Get / Set the wished cursor when the field is focused
	/// </summary>
	public CursorVisibility DesiredCursorVisibility {
		get => _desiredCursorVisibility;
		set {
			if ((_desiredCursorVisibility != value || _visibility != value) && HasFocus) {
				Application.Driver.SetCursorVisibility (value);
			}

			_desiredCursorVisibility = _visibility = value;
		}
	}

	/// <summary>
	/// Changing event, raised before the <see cref="Text"/> changes and can be canceled or changing the new text.
	/// </summary>
	public event EventHandler<TextChangingEventArgs> TextChanging;

	/// <summary>
	/// Changed event, raised when the text has changed.
	/// <remarks>
	/// This event is raised when the <see cref="Text"/> changes.
	/// The passed <see cref="EventArgs"/> is a <see cref="string"/> containing the old value.
	/// </remarks>
	/// </summary>
	public event EventHandler<TextChangedEventArgs> TextChanged;

	void SetInitialProperties (string text, int w)
	{
		Height = 1;

		if (text == null) {
			text = "";
		}

		_text = text.Split ("\n") [0].EnumerateRunes ().ToList ();
		_cursorPosition = text.GetRuneCount ();
		ScrollOffset = _cursorPosition > w + 1 ? _cursorPosition - w + 1 : 0;
		CanFocus = true;
		Used = true;
		WantMousePositionReports = true;
		_savedCursorVisibility = _desiredCursorVisibility;

		_historyText.ChangeText += HistoryText_ChangeText;

		Initialized += TextField_Initialized;

		LayoutComplete += TextField_LayoutComplete;

		// Things this view knows how to do
		AddCommand (Command.DeleteCharRight, () => {
			DeleteCharRight ();
			return true;
		});
		AddCommand (Command.DeleteCharLeft, () => {
			DeleteCharLeft (false);
			return true;
		});
		AddCommand (Command.LeftHomeExtend, () => {
			MoveHomeExtend ();
			return true;
		});
		AddCommand (Command.RightEndExtend, () => {
			MoveEndExtend ();
			return true;
		});
		AddCommand (Command.LeftHome, () => {
			MoveHome ();
			return true;
		});
		AddCommand (Command.LeftExtend, () => {
			MoveLeftExtend ();
			return true;
		});
		AddCommand (Command.RightExtend, () => {
			MoveRightExtend ();
			return true;
		});
		AddCommand (Command.WordLeftExtend, () => {
			MoveWordLeftExtend ();
			return true;
		});
		AddCommand (Command.WordRightExtend, () => {
			MoveWordRightExtend ();
			return true;
		});
		AddCommand (Command.Left, () => {
			MoveLeft ();
			return true;
		});
		AddCommand (Command.RightEnd, () => {
			MoveEnd ();
			return true;
		});
		AddCommand (Command.Right, () => {
			MoveRight ();
			return true;
		});
		AddCommand (Command.CutToEndLine, () => {
			KillToEnd ();
			return true;
		});
		AddCommand (Command.CutToStartLine, () => {
			KillToStart ();
			return true;
		});
		AddCommand (Command.Undo, () => {
			Undo ();
			return true;
		});
		AddCommand (Command.Redo, () => {
			Redo ();
			return true;
		});
		AddCommand (Command.WordLeft, () => {
			MoveWordLeft ();
			return true;
		});
		AddCommand (Command.WordRight, () => {
			MoveWordRight ();
			return true;
		});
		AddCommand (Command.KillWordForwards, () => {
			KillWordForwards ();
			return true;
		});
		AddCommand (Command.KillWordBackwards, () => {
			KillWordBackwards ();
			return true;
		});
		AddCommand (Command.ToggleOverwrite, () => {
			SetOverwrite (!Used);
			return true;
		});
		AddCommand (Command.EnableOverwrite, () => {
			SetOverwrite (true);
			return true;
		});
		AddCommand (Command.DisableOverwrite, () => {
			SetOverwrite (false);
			return true;
		});
		AddCommand (Command.Copy, () => {
			Copy ();
			return true;
		});
		AddCommand (Command.Cut, () => {
			Cut ();
			return true;
		});
		AddCommand (Command.Paste, () => {
			Paste ();
			return true;
		});
		AddCommand (Command.SelectAll, () => {
			SelectAll ();
			return true;
		});
		AddCommand (Command.DeleteAll, () => {
			DeleteAll ();
			return true;
		});
		AddCommand (Command.ShowContextMenu, () => {
			ShowContextMenu ();
			return true;
		});

		// Default keybindings for this view
		// We follow this as closely as possible: https://en.wikipedia.org/wiki/Table_of_keyboard_shortcuts
		KeyBindings.Add (KeyCode.Delete, Command.DeleteCharRight);
		KeyBindings.Add (KeyCode.D | KeyCode.CtrlMask, Command.DeleteCharRight);

		KeyBindings.Add (KeyCode.Backspace, Command.DeleteCharLeft);

		KeyBindings.Add (KeyCode.Home | KeyCode.ShiftMask, Command.LeftHomeExtend);
		KeyBindings.Add (KeyCode.Home | KeyCode.ShiftMask | KeyCode.CtrlMask, Command.LeftHomeExtend);
		KeyBindings.Add (KeyCode.A | KeyCode.ShiftMask | KeyCode.CtrlMask, Command.LeftHomeExtend);

		KeyBindings.Add (KeyCode.End | KeyCode.ShiftMask, Command.RightEndExtend);
		KeyBindings.Add (KeyCode.End | KeyCode.ShiftMask | KeyCode.CtrlMask, Command.RightEndExtend);
		KeyBindings.Add (KeyCode.E | KeyCode.ShiftMask | KeyCode.CtrlMask, Command.RightEndExtend);

		KeyBindings.Add (KeyCode.Home, Command.LeftHome);
		KeyBindings.Add (KeyCode.Home | KeyCode.CtrlMask, Command.LeftHome);
		KeyBindings.Add (KeyCode.A | KeyCode.CtrlMask, Command.LeftHome);

		KeyBindings.Add (KeyCode.CursorLeft | KeyCode.ShiftMask, Command.LeftExtend);
		KeyBindings.Add (KeyCode.CursorUp | KeyCode.ShiftMask, Command.LeftExtend);

		KeyBindings.Add (KeyCode.CursorRight | KeyCode.ShiftMask, Command.RightExtend);
		KeyBindings.Add (KeyCode.CursorDown | KeyCode.ShiftMask, Command.RightExtend);

		KeyBindings.Add (KeyCode.CursorLeft | KeyCode.ShiftMask | KeyCode.CtrlMask, Command.WordLeftExtend);
		KeyBindings.Add (KeyCode.CursorUp | KeyCode.ShiftMask | KeyCode.CtrlMask, Command.WordLeftExtend);
		KeyBindings.Add ('B' + KeyCode.ShiftMask | KeyCode.AltMask, Command.WordLeftExtend);

		KeyBindings.Add (KeyCode.CursorRight | KeyCode.ShiftMask | KeyCode.CtrlMask, Command.WordRightExtend);
		KeyBindings.Add (KeyCode.CursorDown | KeyCode.ShiftMask | KeyCode.CtrlMask, Command.WordRightExtend);
		KeyBindings.Add ('F' + KeyCode.ShiftMask | KeyCode.AltMask, Command.WordRightExtend);

		KeyBindings.Add (KeyCode.CursorLeft, Command.Left);
		KeyBindings.Add (KeyCode.B | KeyCode.CtrlMask, Command.Left);

		KeyBindings.Add (KeyCode.End, Command.RightEnd);
		KeyBindings.Add (KeyCode.End | KeyCode.CtrlMask, Command.RightEnd);
		KeyBindings.Add (KeyCode.E | KeyCode.CtrlMask, Command.RightEnd);

		KeyBindings.Add (KeyCode.CursorRight, Command.Right);
		KeyBindings.Add (KeyCode.F | KeyCode.CtrlMask, Command.Right);

		KeyBindings.Add (KeyCode.K | KeyCode.CtrlMask, Command.CutToEndLine);
		KeyBindings.Add (KeyCode.K | KeyCode.AltMask, Command.CutToStartLine);

		KeyBindings.Add (KeyCode.Z | KeyCode.CtrlMask, Command.Undo);
		KeyBindings.Add (KeyCode.Backspace | KeyCode.AltMask, Command.Undo);

		KeyBindings.Add (KeyCode.Y | KeyCode.CtrlMask, Command.Redo);

		KeyBindings.Add (KeyCode.CursorLeft | KeyCode.CtrlMask, Command.WordLeft);
		KeyBindings.Add (KeyCode.CursorUp | KeyCode.CtrlMask, Command.WordLeft);
		KeyBindings.Add ('B' + KeyCode.AltMask, Command.WordLeft);

		KeyBindings.Add (KeyCode.CursorRight | KeyCode.CtrlMask, Command.WordRight);
		KeyBindings.Add (KeyCode.CursorDown | KeyCode.CtrlMask, Command.WordRight);
		KeyBindings.Add ('F' + KeyCode.AltMask, Command.WordRight);

		KeyBindings.Add (KeyCode.Delete | KeyCode.CtrlMask, Command.KillWordForwards);
		KeyBindings.Add (KeyCode.Backspace | KeyCode.CtrlMask, Command.KillWordBackwards);
		KeyBindings.Add (KeyCode.Insert, Command.ToggleOverwrite);
		KeyBindings.Add (KeyCode.C | KeyCode.CtrlMask, Command.Copy);
		KeyBindings.Add (KeyCode.X | KeyCode.CtrlMask, Command.Cut);
		KeyBindings.Add (KeyCode.V | KeyCode.CtrlMask, Command.Paste);
		KeyBindings.Add (KeyCode.T | KeyCode.CtrlMask, Command.SelectAll);

		KeyBindings.Add (KeyCode.R | KeyCode.CtrlMask, Command.DeleteAll);
		KeyBindings.Add (KeyCode.D | KeyCode.CtrlMask | KeyCode.ShiftMask, Command.DeleteAll);

		_currentCulture = Thread.CurrentThread.CurrentUICulture;

		ContextMenu = new ContextMenu (this, BuildContextMenuBarItem ());
		ContextMenu.KeyChanged += ContextMenu_KeyChanged;

		KeyBindings.Add (ContextMenu.Key.KeyCode, KeyBindingScope.HotKey, Command.ShowContextMenu);
	}

	void TextField_LayoutComplete (object sender, LayoutEventArgs e)
	{
		// Don't let height > 1
		if (Frame.Height > 1) {
			Height = 1;
		}
	}


	MenuBarItem BuildContextMenuBarItem () => new (new MenuItem [] {
		new (Strings.ctxSelectAll, "", () => SelectAll (), null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.SelectAll)),
		new (Strings.ctxDeleteAll, "", () => DeleteAll (), null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.DeleteAll)),
		new (Strings.ctxCopy, "", () => Copy (), null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Copy)),
		new (Strings.ctxCut, "", () => Cut (), null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Cut)),
		new (Strings.ctxPaste, "", () => Paste (), null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Paste)),
		new (Strings.ctxUndo, "", () => Undo (), null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Undo)),
		new (Strings.ctxRedo, "", () => Redo (), null, null, (KeyCode)KeyBindings.GetKeyFromCommands (Command.Redo))
	});

	void ContextMenu_KeyChanged (object sender, KeyChangedEventArgs e) => KeyBindings.Replace (e.OldKey.KeyCode, e.NewKey.KeyCode);

	void HistoryText_ChangeText (object sender, HistoryText.HistoryTextItem obj)
	{
		if (obj == null) {
			return;
		}

		Text = TextModel.ToString (obj?.Lines [obj.CursorPosition.Y]);
		CursorPosition = obj.CursorPosition.X;
		Adjust ();
	}

	void TextField_Initialized (object sender, EventArgs e)
	{
		Autocomplete.HostControl = this;
		Autocomplete.PopupInsideContainer = false;
	}

	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		if (IsInitialized) {
			Application.Driver.SetCursorVisibility (DesiredCursorVisibility);
		}

		return base.OnEnter (view);
	}

	///<inheritdoc/>
	public override bool OnLeave (View view)
	{
		if (Application.MouseGrabView != null && Application.MouseGrabView == this) {
			Application.UngrabMouse ();
		}
		//if (SelectedLength != 0 && !(Application.MouseGrabView is MenuBar))
		//	ClearAllSelection ();

		return base.OnLeave (view);
	}

	/// <summary>
	/// Sets the cursor position.
	/// </summary>
	public override void PositionCursor ()
	{
		if (!IsInitialized) {
			return;
		}
		ProcessAutocomplete ();

		var col = 0;
		for (var idx = ScrollOffset < 0 ? 0 : ScrollOffset; idx < _text.Count; idx++) {
			if (idx == _cursorPosition) {
				break;
			}
			var cols = _text [idx].GetColumns ();
			TextModel.SetCol (ref col, Frame.Width - 1, cols);
		}
		var pos = _cursorPosition - ScrollOffset + Math.Min (Frame.X, 0);
		var offB = OffSetBackground ();
		var containerFrame = SuperView?.BoundsToScreen (SuperView.Bounds) ?? default;
		var thisFrame = BoundsToScreen (Bounds);
		if (pos > -1 && col >= pos && pos < Frame.Width + offB
		    && containerFrame.IntersectsWith (thisFrame)) {
			RestoreCursorVisibility ();
			Move (col, 0);
		} else {
			HideCursorVisibility ();
			if (pos < 0) {
				Move (pos, 0);
			} else {
				Move (pos - offB, 0);
			}
		}
	}

	void HideCursorVisibility ()
	{
		if (_desiredCursorVisibility != CursorVisibility.Invisible) {
			DesiredCursorVisibility = CursorVisibility.Invisible;
		}
	}

	void RestoreCursorVisibility ()
	{
		Application.Driver.GetCursorVisibility (out _visibility);
		if (_desiredCursorVisibility != _savedCursorVisibility || _visibility != _savedCursorVisibility) {
			DesiredCursorVisibility = _savedCursorVisibility;
		}
	}

	///<inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		_isDrawing = true;

		var selColor = new Attribute (ColorScheme.Focus.Background, ColorScheme.Focus.Foreground);
		SetSelectedStartSelectedLength ();

		Driver.SetAttribute (GetNormalColor ());
		Move (0, 0);

		var p = ScrollOffset;
		var col = 0;
		var width = Frame.Width + OffSetBackground ();
		var tcount = _text.Count;
		var roc = GetReadOnlyColor ();
		for (var idx = p; idx < tcount; idx++) {
			var rune = _text [idx];
			var cols = rune.GetColumns ();
			if (idx == _cursorPosition && HasFocus && !Used && SelectedLength == 0 && !ReadOnly) {
				Driver.SetAttribute (selColor);
			} else if (ReadOnly) {
				Driver.SetAttribute (idx >= _start && SelectedLength > 0 && idx < _start + SelectedLength ? selColor : roc);
			} else if (!HasFocus && Enabled) {
				Driver.SetAttribute (ColorScheme.Focus);
			} else if (!Enabled) {
				Driver.SetAttribute (roc);
			} else {
				Driver.SetAttribute (idx >= _start && SelectedLength > 0 && idx < _start + SelectedLength ? selColor : ColorScheme.Focus);
			}
			if (col + cols <= width) {
				Driver.AddRune (Secret ? Glyphs.Dot : rune);
			}
			if (!TextModel.SetCol (ref col, width, cols)) {
				break;
			}
			if (idx + 1 < tcount && col + _text [idx + 1].GetColumns () > width) {
				break;
			}
		}

		Driver.SetAttribute (ColorScheme.Focus);
		for (var i = col; i < width; i++) {
			Driver.AddRune ((Rune)' ');
		}

		PositionCursor ();

		RenderCaption ();

		ProcessAutocomplete ();

		_isDrawing = false;
	}

	void ProcessAutocomplete ()
	{
		if (_isDrawing) {
			return;
		}
		if (SelectedLength > 0) {
			return;
		}

		// draw autocomplete
		GenerateSuggestions ();

		var renderAt = new Point (
			Autocomplete.Context.CursorPosition, 0);

		Autocomplete.RenderOverlay (renderAt);
	}

	void RenderCaption ()
	{
		if (HasFocus || Caption == null || Caption.Length == 0
		    || Text?.Length > 0) {
			return;
		}

		var color = new Attribute (CaptionColor, GetNormalColor ().Background);
		Driver.SetAttribute (color);

		Move (0, 0);
		var render = Caption;

		if (render.GetColumns () > Bounds.Width) {
			render = render [..Bounds.Width];
		}

		Driver.AddStr (render);
	}

	void GenerateSuggestions ()
	{
		var currentLine = TextModel.ToRuneCellList (Text);
		var cursorPosition = Math.Min (CursorPosition, currentLine.Count);
		Autocomplete.Context = new AutocompleteContext (currentLine, cursorPosition,
			Autocomplete.Context != null ? Autocomplete.Context.Canceled : false);

		Autocomplete.GenerateSuggestions (
			Autocomplete.Context);
	}

	/// <inheritdoc/>
	public override Attribute GetNormalColor () => Enabled ? ColorScheme.Focus : ColorScheme.Disabled;

	Attribute GetReadOnlyColor ()
	{
		if (ColorScheme.Disabled.Foreground == ColorScheme.Focus.Background) {
			return new Attribute (ColorScheme.Focus.Foreground, ColorScheme.Focus.Background);
		}
		return new Attribute (ColorScheme.Disabled.Foreground, ColorScheme.Focus.Background);
	}

	void Adjust ()
	{
		if (!IsAdded) {
			return;
		}

		var offB = OffSetBackground ();
		var need = NeedsDisplay || !Used;
		if (_cursorPosition < ScrollOffset) {
			ScrollOffset = _cursorPosition;
			need = true;
		} else if (Frame.Width > 0 && (ScrollOffset + _cursorPosition - (Frame.Width + offB) == 0 ||
					       TextModel.DisplaySize (_text, ScrollOffset, _cursorPosition).size >= Frame.Width + offB)) {

			ScrollOffset = Math.Max (TextModel.CalculateLeftColumn (_text, ScrollOffset,
				_cursorPosition, Frame.Width + offB), 0);
			need = true;
		}
		if (need) {
			SetNeedsDisplay ();
		} else {
			PositionCursor ();
		}
	}

	int OffSetBackground ()
	{
		var offB = 0;
		if (SuperView?.Frame.Right - Frame.Right < 0) {
			offB = SuperView.Frame.Right - Frame.Right - 1;
		}

		return offB;
	}

	void SetText (List<Rune> newText) => Text = StringExtensions.ToString (newText);

	void SetText (IEnumerable<Rune> newText) => SetText (newText.ToList ());

	void SetClipboard (IEnumerable<Rune> text)
	{
		if (!Secret) {
			Clipboard.Contents = StringExtensions.ToString (text.ToList ());
		}
	}

	///<inheritdoc/>
	public override bool? OnInvokingKeyBindings (Key a)
	{
		// Give autocomplete first opportunity to respond to key presses
		if (SelectedLength == 0 && Autocomplete.Suggestions.Count > 0 && Autocomplete.ProcessKey (a)) {
			return true;
		}
		return base.OnInvokingKeyBindings (a);
	}

	/// TODO: Flush out these docs
	/// <summary>
	/// Processes key presses for the <see cref="TextField"/>.
	/// <remarks>
	/// The <see cref="TextField"/> control responds to the following keys:
	/// <list type="table">
	///         <listheader>
	///                 <term>Keys</term>
	///                 <description>Function</description>
	///         </listheader>
	///         <item>
	///                 <term><see cref="Key.Delete"/>, <see cref="Key.Backspace"/></term>
	///                 <description>Deletes the character before cursor.</description>
	///         </item>
	/// </list>
	/// </remarks>
	/// </summary>
	/// <param name="a"></param>
	/// <returns></returns>
	public override bool OnProcessKeyDown (Key a)
	{
		// Remember the cursor position because the new calculated cursor position is needed
		// to be set BEFORE the TextChanged event is triggered.
		// Needed for the Elmish Wrapper issue https://github.com/DieselMeister/Terminal.Gui.Elmish/issues/2
		_preTextChangedCursorPos = _cursorPosition;

		// Ignore other control characters.
		if (!a.IsKeyCodeAtoZ && (a.KeyCode < KeyCode.Space || a.KeyCode > KeyCode.CharMask)) {
			return false;
		}

		if (ReadOnly) {
			return true;
		}

		InsertText (a, true);

		return true;
	}

	void InsertText (Key a, bool usePreTextChangedCursorPos)
	{
		_historyText.Add (new List<List<RuneCell>> { TextModel.ToRuneCells (_text) }, new Point (_cursorPosition, 0));

		var newText = _text;
		if (SelectedLength > 0) {
			newText = DeleteSelectedText ();
			_preTextChangedCursorPos = _cursorPosition;
		}
		if (!usePreTextChangedCursorPos) {
			_preTextChangedCursorPos = _cursorPosition;
		}
		var kbstr = a.AsRune.ToString ().EnumerateRunes ();
		if (Used) {
			_cursorPosition++;
			if (_cursorPosition == newText.Count + 1) {
				SetText (newText.Concat (kbstr).ToList ());
			} else {
				if (_preTextChangedCursorPos > newText.Count) {
					_preTextChangedCursorPos = newText.Count;
				}
				SetText (newText.GetRange (0, _preTextChangedCursorPos).Concat (kbstr).Concat (newText.GetRange (_preTextChangedCursorPos, Math.Min (newText.Count - _preTextChangedCursorPos, newText.Count))));
			}
		} else {
			SetText (newText.GetRange (0, _preTextChangedCursorPos).Concat (kbstr).Concat (newText.GetRange (Math.Min (_preTextChangedCursorPos + 1, newText.Count), Math.Max (newText.Count - _preTextChangedCursorPos - 1, 0))));
			_cursorPosition++;
		}
		Adjust ();
	}

	void SetOverwrite (bool overwrite)
	{
		Used = overwrite;
		SetNeedsDisplay ();
	}

	TextModel GetModel ()
	{
		var model = new TextModel ();
		model.LoadString (Text);
		return model;
	}

	/// <summary>
	/// Deletes word backwards.
	/// </summary>
	public virtual void KillWordBackwards ()
	{
		ClearAllSelection ();
		var newPos = GetModel ().WordBackward (_cursorPosition, 0);
		if (newPos == null) {
			return;
		}
		if (newPos.Value.col != -1) {
			SetText (_text.GetRange (0, newPos.Value.col).Concat (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition)));
			_cursorPosition = newPos.Value.col;
		}
		Adjust ();
	}

	/// <summary>
	/// Deletes word forwards.
	/// </summary>
	public virtual void KillWordForwards ()
	{
		ClearAllSelection ();
		var newPos = GetModel ().WordForward (_cursorPosition, 0);
		if (newPos == null) {
			return;
		}
		if (newPos.Value.col != -1) {
			SetText (_text.GetRange (0, _cursorPosition).Concat (_text.GetRange (newPos.Value.col, _text.Count - newPos.Value.col)));
		}
		Adjust ();
	}

	void MoveWordRight ()
	{
		ClearAllSelection ();
		var newPos = GetModel ().WordForward (_cursorPosition, 0);
		if (newPos == null) {
			return;
		}
		if (newPos.Value.col != -1) {
			_cursorPosition = newPos.Value.col;
		}
		Adjust ();
	}

	void MoveWordLeft ()
	{
		ClearAllSelection ();
		var newPos = GetModel ().WordBackward (_cursorPosition, 0);
		if (newPos == null) {
			return;
		}
		if (newPos.Value.col != -1) {
			_cursorPosition = newPos.Value.col;
		}
		Adjust ();
	}

	/// <summary>
	/// Redoes the latest changes.
	/// </summary>
	public void Redo ()
	{
		if (ReadOnly) {
			return;
		}

		_historyText.Redo ();

		//if (string.IsNullOrEmpty (Clipboard.Contents))
		//	return true;
		//var clip = TextModel.ToRunes (Clipboard.Contents);
		//if (clip == null)
		//	return true;

		//if (point == text.Count) {
		//	point = text.Count;
		//	SetText(text.Concat(clip).ToList());
		//} else {
		//	point += clip.Count;
		//	SetText(text.GetRange(0, oldCursorPos).Concat(clip).Concat(text.GetRange(oldCursorPos, text.Count - oldCursorPos)));
		//}
		//Adjust ();
	}

	/// <summary>
	/// Undoes the latest changes.
	/// </summary>
	public void Undo ()
	{
		if (ReadOnly) {
			return;
		}

		_historyText.Undo ();
	}

	void KillToStart ()
	{
		if (ReadOnly) {
			return;
		}

		ClearAllSelection ();
		if (_cursorPosition == 0) {
			return;
		}
		SetClipboard (_text.GetRange (0, _cursorPosition));
		SetText (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition));
		_cursorPosition = 0;
		Adjust ();
	}

	void KillToEnd ()
	{
		if (ReadOnly) {
			return;
		}

		ClearAllSelection ();
		if (_cursorPosition >= _text.Count) {
			return;
		}
		SetClipboard (_text.GetRange (_cursorPosition, _text.Count - _cursorPosition));
		SetText (_text.GetRange (0, _cursorPosition));
		Adjust ();
	}

	void MoveRight ()
	{
		ClearAllSelection ();
		if (_cursorPosition == _text.Count) {
			return;
		}
		_cursorPosition++;
		Adjust ();
	}

	/// <summary>
	/// Moves cursor to the end of the typed text.
	/// </summary>
	public void MoveEnd ()
	{
		ClearAllSelection ();
		_cursorPosition = _text.Count;
		Adjust ();
	}

	void MoveLeft ()
	{
		ClearAllSelection ();
		if (_cursorPosition > 0) {
			_cursorPosition--;
			Adjust ();
		}
	}

	void MoveWordRightExtend ()
	{
		if (_cursorPosition < _text.Count) {
			var x = _start > -1 && _start > _cursorPosition ? _start : _cursorPosition;
			var newPos = GetModel ().WordForward (x, 0);
			if (newPos == null) {
				return;
			}
			if (newPos.Value.col != -1) {
				_cursorPosition = newPos.Value.col;
			}
			PrepareSelection (x, newPos.Value.col - x);
		}
	}

	void MoveWordLeftExtend ()
	{
		if (_cursorPosition > 0) {
			var x = Math.Min (_start > -1 && _start > _cursorPosition ? _start : _cursorPosition, _text.Count);
			if (x > 0) {
				var newPos = GetModel ().WordBackward (x, 0);
				if (newPos == null) {
					return;
				}
				if (newPos.Value.col != -1) {
					_cursorPosition = newPos.Value.col;
				}
				PrepareSelection (x, newPos.Value.col - x);
			}
		}
	}

	void MoveRightExtend ()
	{
		if (_cursorPosition < _text.Count) {
			PrepareSelection (_cursorPosition++, 1);
		}
	}

	void MoveLeftExtend ()
	{
		if (_cursorPosition > 0) {
			PrepareSelection (_cursorPosition--, -1);
		}
	}

	void MoveHome ()
	{
		ClearAllSelection ();
		_cursorPosition = 0;
		Adjust ();
	}

	void MoveEndExtend ()
	{
		if (_cursorPosition <= _text.Count) {
			var x = _cursorPosition;
			_cursorPosition = _text.Count;
			PrepareSelection (x, _cursorPosition - x);
		}
	}

	void MoveHomeExtend ()
	{
		if (_cursorPosition > 0) {
			var x = _cursorPosition;
			_cursorPosition = 0;
			PrepareSelection (x, _cursorPosition - x);
		}
	}

	/// <summary>
	/// Deletes the character to the left.
	/// </summary>
	/// <param name="usePreTextChangedCursorPos">
	/// If set to <see langword="true">true</see> use the cursor position cached
	/// ; otherwise use <see cref="CursorPosition"/>.
	/// use .
	/// </param>
	public virtual void DeleteCharLeft (bool usePreTextChangedCursorPos)
	{
		if (ReadOnly) {
			return;
		}

		_historyText.Add (new List<List<RuneCell>> { TextModel.ToRuneCells (_text) }, new Point (_cursorPosition, 0));

		if (SelectedLength == 0) {
			if (_cursorPosition == 0) {
				return;
			}

			if (!usePreTextChangedCursorPos) {
				_preTextChangedCursorPos = _cursorPosition;
			}
			_cursorPosition--;
			if (_preTextChangedCursorPos < _text.Count) {
				SetText (_text.GetRange (0, _preTextChangedCursorPos - 1).Concat (_text.GetRange (_preTextChangedCursorPos, _text.Count - _preTextChangedCursorPos)));
			} else {
				SetText (_text.GetRange (0, _preTextChangedCursorPos - 1));
			}
			Adjust ();
		} else {
			var newText = DeleteSelectedText ();
			Text = StringExtensions.ToString (newText);
			Adjust ();
		}
	}

	/// <summary>
	/// Deletes the character to the right.
	/// </summary>
	public virtual void DeleteCharRight ()
	{
		if (ReadOnly) {
			return;
		}

		_historyText.Add (new List<List<RuneCell>> { TextModel.ToRuneCells (_text) }, new Point (_cursorPosition, 0));

		if (SelectedLength == 0) {
			if (_text.Count == 0 || _text.Count == _cursorPosition) {
				return;
			}

			SetText (_text.GetRange (0, _cursorPosition).Concat (_text.GetRange (_cursorPosition + 1, _text.Count - (_cursorPosition + 1))));
			Adjust ();
		} else {
			var newText = DeleteSelectedText ();
			Text = StringExtensions.ToString (newText);
			Adjust ();
		}
	}

	void ShowContextMenu ()
	{
		if (_currentCulture != Thread.CurrentThread.CurrentUICulture) {

			_currentCulture = Thread.CurrentThread.CurrentUICulture;

			ContextMenu.MenuItems = BuildContextMenuBarItem ();
		}
		ContextMenu.Show ();
	}

	/// <summary>
	/// Selects all text.
	/// </summary>
	public void SelectAll ()
	{
		if (_text.Count == 0) {
			return;
		}

		_selectedStart = 0;
		MoveEndExtend ();
		SetNeedsDisplay ();
	}

	/// <summary>
	/// Deletes all text.
	/// </summary>
	public void DeleteAll ()
	{
		if (_text.Count == 0) {
			return;
		}

		_selectedStart = 0;
		MoveEndExtend ();
		DeleteCharLeft (false);
		SetNeedsDisplay ();
	}

	///<inheritdoc/>
	public override bool MouseEvent (MouseEvent ev)
	{
		if (!ev.Flags.HasFlag (MouseFlags.Button1Pressed) && !ev.Flags.HasFlag (MouseFlags.ReportMousePosition) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1Released) && !ev.Flags.HasFlag (MouseFlags.Button1DoubleClicked) &&
		    !ev.Flags.HasFlag (MouseFlags.Button1TripleClicked) && !ev.Flags.HasFlag (ContextMenu.MouseFlags)) {
			return false;
		}

		if (!CanFocus) {
			return true;
		}

		if (!HasFocus && ev.Flags != MouseFlags.ReportMousePosition) {
			SetFocus ();
		}

		// Give autocomplete first opportunity to respond to mouse clicks
		if (SelectedLength == 0 && Autocomplete.MouseEvent (ev, true)) {
			return true;
		}

		if (ev.Flags == MouseFlags.Button1Pressed) {
			EnsureHasFocus ();
			PositionCursor (ev);
			if (_isButtonReleased) {
				ClearAllSelection ();
			}
			_isButtonReleased = true;
			_isButtonPressed = true;
		} else if (ev.Flags == (MouseFlags.Button1Pressed | MouseFlags.ReportMousePosition) && _isButtonPressed) {
			var x = PositionCursor (ev);
			_isButtonReleased = false;
			PrepareSelection (x);
			if (Application.MouseGrabView == null) {
				Application.GrabMouse (this);
			}
		} else if (ev.Flags == MouseFlags.Button1Released) {
			_isButtonReleased = true;
			_isButtonPressed = false;
			Application.UngrabMouse ();
		} else if (ev.Flags == MouseFlags.Button1DoubleClicked) {
			EnsureHasFocus ();
			var x = PositionCursor (ev);
			var sbw = x;
			if (x == _text.Count || x > 0 && (char)_text [x - 1].Value != ' '
					     || x > 0 && (char)_text [x].Value == ' ') {

				var newPosBw = GetModel ().WordBackward (x, 0);
				if (newPosBw == null) {
					return true;
				}
				sbw = newPosBw.Value.col;
			}
			if (sbw != -1) {
				x = sbw;
				PositionCursor (x);
			}
			var newPosFw = GetModel ().WordForward (x, 0);
			if (newPosFw == null) {
				return true;
			}
			ClearAllSelection ();
			if (newPosFw.Value.col != -1 && sbw != -1) {
				_cursorPosition = newPosFw.Value.col;
			}
			PrepareSelection (sbw, newPosFw.Value.col - sbw);
		} else if (ev.Flags == MouseFlags.Button1TripleClicked) {
			EnsureHasFocus ();
			PositionCursor (0);
			ClearAllSelection ();
			PrepareSelection (0, _text.Count);
		} else if (ev.Flags == ContextMenu.MouseFlags) {
			ShowContextMenu ();
		}

		SetNeedsDisplay ();
		return true;

		void EnsureHasFocus ()
		{
			if (!HasFocus) {
				SetFocus ();
			}
		}
	}

	int PositionCursor (MouseEvent ev)
	{
		// We could also set the cursor position.
		int x;
		var pX = TextModel.GetColFromX (_text, ScrollOffset, ev.X);
		if (_text.Count == 0) {
			x = pX - ev.OfX;
		} else {
			x = pX;
		}
		return PositionCursor (x, false);
	}

	int PositionCursor (int x, bool getX = true)
	{
		var pX = x;
		if (getX) {
			pX = TextModel.GetColFromX (_text, ScrollOffset, x);
		}
		if (ScrollOffset + pX > _text.Count) {
			_cursorPosition = _text.Count;
		} else if (ScrollOffset + pX < ScrollOffset) {
			_cursorPosition = 0;
		} else {
			_cursorPosition = ScrollOffset + pX;
		}

		return _cursorPosition;
	}

	void PrepareSelection (int x, int direction = 0)
	{
		x = x + ScrollOffset < -1 ? 0 : x;
		_selectedStart = _selectedStart == -1 && _text.Count > 0 && x >= 0 && x <= _text.Count ? x : _selectedStart;
		if (_selectedStart > -1) {
			SelectedLength = Math.Abs (x + direction <= _text.Count ? x + direction - _selectedStart : _text.Count - _selectedStart);
			SetSelectedStartSelectedLength ();
			if (_start > -1 && SelectedLength > 0) {
				_selectedText = SelectedLength > 0 ? StringExtensions.ToString (_text.GetRange (
					_start < 0 ? 0 : _start, SelectedLength > _text.Count ? _text.Count : SelectedLength)) : "";
				if (ScrollOffset > _start) {
					ScrollOffset = _start;
				}
			} else if (_start > -1 && SelectedLength == 0) {
				_selectedText = null;
			}
			SetNeedsDisplay ();
		} else if (SelectedLength > 0 || _selectedText != null) {
			ClearAllSelection ();
		}
		Adjust ();
	}

	/// <summary>
	/// Clear the selected text.
	/// </summary>
	public void ClearAllSelection ()
	{
		if (_selectedStart == -1 && SelectedLength == 0 && string.IsNullOrEmpty (_selectedText)) {
			return;
		}

		_selectedStart = -1;
		SelectedLength = 0;
		_selectedText = null;
		_start = 0;
		SelectedLength = 0;
		SetNeedsDisplay ();
	}

	void SetSelectedStartSelectedLength ()
	{
		if (SelectedStart > -1 && _cursorPosition < SelectedStart) {
			_start = _cursorPosition;
		} else {
			_start = SelectedStart;
		}
	}

	/// <summary>
	/// Copy the selected text to the clipboard.
	/// </summary>
	public virtual void Copy ()
	{
		if (Secret || SelectedLength == 0) {
			return;
		}

		Clipboard.Contents = SelectedText;
	}

	/// <summary>
	/// Cut the selected text to the clipboard.
	/// </summary>
	public virtual void Cut ()
	{
		if (ReadOnly || Secret || SelectedLength == 0) {
			return;
		}

		Clipboard.Contents = SelectedText;
		var newText = DeleteSelectedText ();
		Text = StringExtensions.ToString (newText);
		Adjust ();
	}

	List<Rune> DeleteSelectedText ()
	{
		SetSelectedStartSelectedLength ();
		var selStart = SelectedStart > -1 ? _start : _cursorPosition;
		var newText = StringExtensions.ToString (_text.GetRange (0, selStart)) +
			      StringExtensions.ToString (_text.GetRange (selStart + SelectedLength, _text.Count - (selStart + SelectedLength)));

		ClearAllSelection ();
		_cursorPosition = selStart >= newText.GetRuneCount () ? newText.GetRuneCount () : selStart;
		return newText.ToRuneList ();
	}

	/// <summary>
	/// Paste the selected text from the clipboard.
	/// </summary>
	public virtual void Paste ()
	{
		if (ReadOnly || string.IsNullOrEmpty (Clipboard.Contents)) {
			return;
		}

		SetSelectedStartSelectedLength ();
		var selStart = _start == -1 ? CursorPosition : _start;
		var cbTxt = Clipboard.Contents.Split ("\n") [0] ?? "";
		Text = StringExtensions.ToString (_text.GetRange (0, selStart)) +
		       cbTxt +
		       StringExtensions.ToString (_text.GetRange (selStart + SelectedLength, _text.Count - (selStart + SelectedLength)));

			_cursorPosition = Math.Min (selStart + cbTxt.GetRuneCount (), _text.Count);
			ClearAllSelection ();
			SetNeedsDisplay ();
			Adjust ();
		}

	/// <summary>
	/// Virtual method that invoke the <see cref="TextChanging"/> event if it's defined.
	/// </summary>
	/// <param name="newText">The new text to be replaced.</param>
	/// <returns>Returns the <see cref="TextChangingEventArgs"/></returns>
	public virtual TextChangingEventArgs OnTextChanging (string newText)
	{
		var ev = new TextChangingEventArgs (newText);
		TextChanging?.Invoke (this, ev);
		return ev;
	}

	/// <summary>
	/// Inserts the given <paramref name="toAdd"/> text at the current cursor position
	/// exactly as if the user had just typed it
	/// </summary>
	/// <param name="toAdd">Text to add</param>
	/// <param name="useOldCursorPos">Use the previous cursor position.</param>
	public void InsertText (string toAdd, bool useOldCursorPos = true)
	{
		foreach (var ch in toAdd) {

			KeyCode key;

			try {
				key = (KeyCode)ch;
			} catch (Exception) {

				throw new ArgumentException ($"Cannot insert character '{ch}' because it does not map to a Key");
			}

			InsertText (new Key { KeyCode = key }, useOldCursorPos);
		}
	}

	/// <summary>
	/// Allows clearing the <see cref="HistoryText.HistoryTextItem"/> items updating the original text.
	/// </summary>
	public void ClearHistoryChanges () => _historyText.Clear (Text);

	/// <summary>
	/// Returns <see langword="true"/> if the current cursor position is
	/// at the end of the <see cref="Text"/>. This includes when it is empty.
	/// </summary>
	/// <returns></returns>
	internal bool CursorIsAtEnd () => CursorPosition == Text.Length;

	/// <summary>
	/// Returns <see langword="true"/> if the current cursor position is
	/// at the start of the <see cref="TextField"/>.
	/// </summary>
	/// <returns></returns>
	internal bool CursorIsAtStart () => CursorPosition <= 0;
}

/// <summary>
/// Renders an overlay on another view at a given point that allows selecting
/// from a range of 'autocomplete' options.
/// An implementation on a TextField.
/// </summary>
public class TextFieldAutocomplete : PopupAutocomplete {

	/// <inheritdoc/>
	protected override void DeleteTextBackwards () => ((TextField)HostControl).DeleteCharLeft (false);

	/// <inheritdoc/>
	protected override void InsertText (string accepted) => ((TextField)HostControl).InsertText (accepted, false);

	/// <inheritdoc/>
	protected override void SetCursorPosition (int column) => ((TextField)HostControl).CursorPosition = column;
}