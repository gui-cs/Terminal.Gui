using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui;
/// <summary>
/// Displays a group of labels each with a selected indicator. Only one of those can be selected at a given time.
/// </summary>
public class RadioGroup : View {
	int _selected = -1;
	int _cursor;
	DisplayModeLayout _displayMode;
	int _horizontalSpace = 2;
	List<(int pos, int length)> _horizontal;

	/// <summary>
	/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	public RadioGroup () : this (radioLabels: new string [] { }) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Computed"/> layout.
	/// </summary>
	/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
	/// <param name="selected">The index of the item to be selected, the value is clamped to the number of items.</param>
	public RadioGroup (string [] radioLabels, int selected = 0) : base ()
	{
		SetInitialProperties (Rect.Empty, radioLabels, selected);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Absolute"/> layout.
	/// </summary>
	/// <param name="rect">Boundaries for the radio group.</param>
	/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
	/// <param name="selected">The index of item to be selected, the value is clamped to the number of items.</param>
	public RadioGroup (Rect rect, string [] radioLabels, int selected = 0) : base (rect)
	{
		SetInitialProperties (rect, radioLabels, selected);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RadioGroup"/> class using <see cref="LayoutStyle.Absolute"/> layout.
	/// The <see cref="View"/> frame is computed from the provided radio labels.
	/// </summary>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="radioLabels">The radio labels; an array of strings that can contain hotkeys using an underscore before the letter.</param>
	/// <param name="selected">The item to be selected, the value is clamped to the number of items.</param>
	public RadioGroup (int x, int y, string [] radioLabels, int selected = 0) :
		this (MakeRect (x, y, radioLabels != null ? radioLabels.ToList () : null), radioLabels, selected)
	{ }

	void SetInitialProperties (Rect rect, string [] radioLabels, int selected)
	{
		HotKeySpecifier = new Rune ('_');

		if (radioLabels != null) {
			RadioLabels = radioLabels;
		}

		_selected = selected;
		Frame = rect;
		CanFocus = true;

		// Things this view knows how to do
		AddCommand (Command.LineUp, () => { MoveUp (); return true; });
		AddCommand (Command.LineDown, () => { MoveDown (); return true; });
		AddCommand (Command.TopHome, () => { MoveHome (); return true; });
		AddCommand (Command.BottomEnd, () => { MoveEnd (); return true; });
		AddCommand (Command.Accept, () => { SelectItem (); return true; });

		// Default keybindings for this view
		KeyBindings.Add (KeyCode.CursorUp, Command.LineUp);
		KeyBindings.Add (KeyCode.CursorDown, Command.LineDown);
		KeyBindings.Add (KeyCode.Home, Command.TopHome);
		KeyBindings.Add (KeyCode.End, Command.BottomEnd);
		KeyBindings.Add (KeyCode.Space, Command.Accept);

		LayoutStarted += RadioGroup_LayoutStarted;
	}

	void RadioGroup_LayoutStarted (object sender, EventArgs e)
	{
		SetWidthHeight (_radioLabels);
	}

	/// <summary>
	/// Gets or sets the <see cref="DisplayModeLayout"/> for this <see cref="RadioGroup"/>.
	/// </summary>
	public DisplayModeLayout DisplayMode {
		get { return _displayMode; }
		set {
			if (_displayMode != value) {
				_displayMode = value;
				SetWidthHeight (_radioLabels);
				SetNeedsDisplay ();
			}
		}
	}

	/// <summary>
	/// Gets or sets the horizontal space for this <see cref="RadioGroup"/> if the <see cref="DisplayMode"/> is <see cref="DisplayModeLayout.Horizontal"/>
	/// </summary>
	public int HorizontalSpace {
		get { return _horizontalSpace; }
		set {
			if (_horizontalSpace != value && _displayMode == DisplayModeLayout.Horizontal) {
				_horizontalSpace = value;
				SetWidthHeight (_radioLabels);
				UpdateTextFormatterText ();
				SetNeedsDisplay ();
			}
		}
	}

	void SetWidthHeight (List<string> radioLabels)
	{
		switch (_displayMode) {
		case DisplayModeLayout.Vertical:
			var r = MakeRect (0, 0, radioLabels);
			Bounds = new Rect (Bounds.Location, new Size (r.Width, radioLabels.Count));
			break;

		case DisplayModeLayout.Horizontal:
			CalculateHorizontalPositions ();
			var length = 0;
			foreach (var item in _horizontal) {
				length += item.length;
			}
			var hr = new Rect (0, 0, length, 1);
			if (IsAdded && LayoutStyle == LayoutStyle.Computed) {
				Width = hr.Width;
				Height = 1;
			} else {
				Bounds = new Rect (Bounds.Location, new Size (hr.Width, radioLabels.Count));
			}
			break;
		}
	}

	static Rect MakeRect (int x, int y, List<string> radioLabels)
	{
		if (radioLabels == null) {
			return new Rect (x, y, 0, 0);
		}

		int width = 0;

		foreach (var s in radioLabels) {
			width = Math.Max (s.GetColumns () + 2, width);
		}
		return new Rect (x, y, width, radioLabels.Count);
	}

	List<string> _radioLabels = new List<string> ();

	/// <summary>
	/// The radio labels to display. A key binding will be added for each radio radio enabling the user
	/// to select and/or focus the radio label using the keyboard. See <see cref="View.HotKey"/> for details
	/// on how HotKeys work.
	/// </summary>
	/// <value>The radio labels.</value>
	public string [] RadioLabels {
		get => _radioLabels.ToArray ();
		set {
			// Remove old hot key bindings
			foreach (var label in _radioLabels) {
				if (TextFormatter.FindHotKey (label, HotKeySpecifier, true, out _, out KeyCode hotKey)) {
					AddKeyBindingsForHotKey (hotKey, KeyCode.Null);
				}
			}
			var prevCount = _radioLabels.Count;
			_radioLabels = value.ToList ();
			foreach (var label in _radioLabels) {
				if (TextFormatter.FindHotKey (label, HotKeySpecifier, true, out _, out KeyCode hotKey)) {
					AddKeyBindingsForHotKey (KeyCode.Null, hotKey);
				}
			}
			if (prevCount != _radioLabels.Count) {
				SetWidthHeight (_radioLabels);
			}
			SelectedItem = 0;
			_cursor = 0;
			SetNeedsDisplay ();
		}
	}

	/// <inheritdoc/>
	public override bool? OnInvokingKeyBindings (KeyEventArgs keyEvent)
	{
		// This is a bit of a hack. We want to handle the key bindings for the radio group but
		// InvokeKeyBindings doesn't pass any context so we can't tell if the key binding is for
		// the radio group or for one of the radio buttons. So before we call the base class
		// we set SelectedItem appropriately.

		// Force upper case
		var key = keyEvent.ConsoleDriverKey;
		if (KeyBindings.TryGet (key, out _)) {
			// Search RadioLabels 
			for (int i = 0; i < _radioLabels.Count; i++) {
				if (TextFormatter.FindHotKey (_radioLabels [i], HotKeySpecifier, true, out _, out KeyCode hotKey) 
					&& (key & ~KeyCode.ShiftMask & ~KeyCode.AltMask) == hotKey) {
					SelectedItem = i;
					keyEvent.Scope = KeyBindingScope.HotKey;
					break;
				}
			}

		}
		return base.OnInvokingKeyBindings (keyEvent);
	}

	void CalculateHorizontalPositions ()
	{
		if (_displayMode == DisplayModeLayout.Horizontal) {
			_horizontal = new List<(int pos, int length)> ();
			int start = 0;
			int length = 0;
			for (int i = 0; i < _radioLabels.Count; i++) {
				start += length;
				length = _radioLabels [i].GetColumns () + 2 + (i < _radioLabels.Count - 1 ? _horizontalSpace : 0);
				_horizontal.Add ((start, length));
			}
		}
	}

	///<inheritdoc/>
	public override void OnDrawContent (Rect contentArea)
	{
		base.OnDrawContent (contentArea);

		Driver.SetAttribute (GetNormalColor ());
		for (int i = 0; i < _radioLabels.Count; i++) {
			switch (DisplayMode) {
			case DisplayModeLayout.Vertical:
				Move (0, i);
				break;
			case DisplayModeLayout.Horizontal:
				Move (_horizontal [i].pos, 0);
				break;
			}
			var rl = _radioLabels [i];
			Driver.SetAttribute (GetNormalColor ());
			Driver.AddStr ($"{(i == _selected ? CM.Glyphs.Selected : CM.Glyphs.UnSelected)} ");
			TextFormatter.FindHotKey (rl, HotKeySpecifier, true, out int hotPos, out KeyCode hotKey);
			if (hotPos != -1 && (hotKey != KeyCode.Null || hotKey != KeyCode.Unknown)) {
				var rlRunes = rl.ToRunes ();
				for (int j = 0; j < rlRunes.Length; j++) {
					Rune rune = rlRunes [j];
					if (j == hotPos && i == _cursor) {
						Application.Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : GetHotNormalColor ());
					} else if (j == hotPos && i != _cursor) {
						Application.Driver.SetAttribute (GetHotNormalColor ());
					} else if (HasFocus && i == _cursor) {
						Application.Driver.SetAttribute (ColorScheme.Focus);
					}
					if (rune == HotKeySpecifier && j + 1 < rlRunes.Length) {
						j++;
						rune = rlRunes [j];
						if (i == _cursor) {
							Application.Driver.SetAttribute (HasFocus ? ColorScheme.HotFocus : GetHotNormalColor ());
						} else if (i != _cursor) {
							Application.Driver.SetAttribute (GetHotNormalColor ());
						}
					}
					Application.Driver.AddRune (rune);
					Driver.SetAttribute (GetNormalColor ());
				}
			} else {
				DrawHotString (rl, HasFocus && i == _cursor, ColorScheme);
			}
		}
	}

	///<inheritdoc/>
	public override void PositionCursor ()
	{
		switch (DisplayMode) {
		case DisplayModeLayout.Vertical:
			Move (0, _cursor);
			break;
		case DisplayModeLayout.Horizontal:
			Move (_horizontal [_cursor].pos, 0);
			break;
		}
	}

	/// <summary>
	/// Invoked when the selected radio label has changed.
	/// </summary>
	public event EventHandler<SelectedItemChangedArgs> SelectedItemChanged;

	/// <summary>
	/// The currently selected item from the list of radio labels
	/// </summary>
	/// <value>The selected.</value>
	public int SelectedItem {
		get => _selected;
		set {
			OnSelectedItemChanged (value, SelectedItem);
			_cursor = _selected;
			SetNeedsDisplay ();
		}
	}

	/// <summary>
	/// Allow to invoke the <see cref="SelectedItemChanged"/> after their creation.
	/// </summary>
	public void Refresh ()
	{
		OnSelectedItemChanged (_selected, -1);
	}

	/// <summary>
	/// Called whenever the current selected item changes. Invokes the <see cref="SelectedItemChanged"/> event.
	/// </summary>
	/// <param name="selectedItem"></param>
	/// <param name="previousSelectedItem"></param>
	public virtual void OnSelectedItemChanged (int selectedItem, int previousSelectedItem)
	{
		_selected = selectedItem;
		SelectedItemChanged?.Invoke (this, new SelectedItemChangedArgs (selectedItem, previousSelectedItem));
	}

	void SelectItem ()
	{
		SelectedItem = _cursor;
	}

	void MoveEnd ()
	{
		_cursor = Math.Max (_radioLabels.Count - 1, 0);
	}

	void MoveHome ()
	{
		_cursor = 0;
	}

	void MoveDown ()
	{
		if (_cursor + 1 < _radioLabels.Count) {
			_cursor++;
			SetNeedsDisplay ();
		} else if (_cursor > 0) {
			_cursor = 0;
			SetNeedsDisplay ();
		}
	}

	void MoveUp ()
	{
		if (_cursor > 0) {
			_cursor--;
			SetNeedsDisplay ();
		} else if (_radioLabels.Count - 1 > 0) {
			_cursor = _radioLabels.Count - 1;
			SetNeedsDisplay ();
		}
	}

	///<inheritdoc/>
	public override bool MouseEvent (MouseEvent me)
	{
		if (!me.Flags.HasFlag (MouseFlags.Button1Clicked)) {
			return false;
		}
		if (!CanFocus) {
			return false;
		}
		SetFocus ();

		int boundsX = me.X;
		int boundsY = me.Y;

		var pos = _displayMode == DisplayModeLayout.Horizontal ? boundsX : boundsY;
		var rCount = _displayMode == DisplayModeLayout.Horizontal ? _horizontal.Last ().pos + _horizontal.Last ().length : _radioLabels.Count;

		if (pos < rCount) {
			var c = _displayMode == DisplayModeLayout.Horizontal ? _horizontal.FindIndex ((x) => x.pos <= boundsX && x.pos + x.length - 2 >= boundsX) : boundsY;
			if (c > -1) {
				_cursor = SelectedItem = c;
				SetNeedsDisplay ();
			}
		}
		return true;
	}

	///<inheritdoc/>
	public override bool OnEnter (View view)
	{
		Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

		return base.OnEnter (view);
	}
}

/// <summary>
/// Used for choose the display mode of this <see cref="RadioGroup"/>
/// </summary>
public enum DisplayModeLayout {
	/// <summary>
	/// Vertical mode display. It's the default.
	/// </summary>
	Vertical,
	/// <summary>
	/// Horizontal mode display.
	/// </summary>
	Horizontal
}
