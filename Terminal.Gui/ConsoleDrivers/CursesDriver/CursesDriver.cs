//
// Driver.cs: Curses-based Driver
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Unix.Terminal;

namespace Terminal.Gui;

/// <summary>
/// This is the Curses driver for the gui.cs/Terminal framework.
/// </summary>
internal class CursesDriver : ConsoleDriver {

	public override int Cols => Curses.Cols;
	public override int Rows => Curses.Lines;

	CursorVisibility? _initialCursorVisibility = null;
	CursorVisibility? _currentCursorVisibility = null;

	public override string GetVersionInfo () => $"{Curses.curses_version ()}";
	UnixMainLoop _mainLoopDriver = null;
	public override bool SupportsTrueColor => false;

	object _processInputToken;

	internal override MainLoop Init ()
	{
		_mainLoopDriver = new UnixMainLoop (this);
		if (!RunningUnitTests) {

			_window = Curses.initscr ();
			Curses.set_escdelay (10);

			// Ensures that all procedures are performed at some previous closing.
			Curses.doupdate ();

			// 
			// We are setting Invisible as default so we could ignore XTerm DECSUSR setting
			//
			switch (Curses.curs_set (0)) {
			case 0:
				_currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Invisible;
				break;

			case 1:
				_currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Underline;
				Curses.curs_set (1);
				break;

			case 2:
				_currentCursorVisibility = _initialCursorVisibility = CursorVisibility.Box;
				Curses.curs_set (2);
				break;

			default:
				_currentCursorVisibility = _initialCursorVisibility = null;
				break;
			}
			if (!Curses.HasColors) {
				throw new InvalidOperationException ("V2 - This should never happen. File an Issue if it does.");
			}

			Curses.raw ();
			Curses.noecho ();

			Curses.Window.Standard.keypad (true);

			Curses.StartColor ();
			Curses.UseDefaultColors ();

			if (!RunningUnitTests) {
				Curses.timeout (0);
			}

			_processInputToken = _mainLoopDriver?.AddWatch (0, UnixMainLoop.Condition.PollIn, x => {
				ProcessInput ();
				return true;
			});
		}

		CurrentAttribute = MakeColor (ColorName.White, ColorName.Black);

		if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
			Clipboard = new FakeDriver.FakeClipboard ();
		} else {
			if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
				Clipboard = new MacOSXClipboard ();
			} else {
				if (Is_WSL_Platform ()) {
					Clipboard = new WSLClipboard ();
				} else {
					Clipboard = new CursesClipboard ();
				}
			}
		}

		ClearContents ();
		StartReportingMouseMoves ();

		if (!RunningUnitTests) {
			Curses.CheckWinChange ();
			Curses.refresh ();
		}
		return new MainLoop (_mainLoopDriver);
	}

	public override void Move (int col, int row)
	{
		base.Move (col, row);

		if (RunningUnitTests) {
			return;
		}

		if (IsValidLocation (col, row)) {
			Curses.move (row, col);
		} else {
			// Not a valid location (outside screen or clip region)
			// Move within the clip region, then AddRune will actually move to Col, Row
			Curses.move (Clip.Y, Clip.X);
		}
	}

	public override bool IsRuneSupported (Rune rune)
	{
		// See Issue #2615 - CursesDriver is broken with non-BMP characters
		return base.IsRuneSupported (rune) && rune.IsBmp;
	}

	public override void Refresh ()
	{
		UpdateScreen ();
		UpdateCursor ();
	}

	internal void ProcessWinChange ()
	{
		if (!RunningUnitTests && Curses.CheckWinChange ()) {
			ClearContents ();
			OnSizeChanged (new SizeChangedEventArgs (new Size (Cols, Rows)));
		}
	}

	#region Color Handling

	/// <summary>
	/// Creates an Attribute from the provided curses-based foreground and background color numbers
	/// </summary>
	/// <param name="foreground">Contains the curses color number for the foreground (color, plus any attributes)</param>
	/// <param name="background">Contains the curses color number for the background (color, plus any attributes)</param>
	/// <returns></returns>
	static Attribute MakeColor (short foreground, short background)
	{
		var v = (short)((int)foreground | background << 4);

		// TODO: for TrueColor - Use InitExtendedPair
		Curses.InitColorPair (v, foreground, background);
		return new Attribute (
			platformColor: Curses.ColorPair (v),
			foreground: CursesColorNumberToColorName (foreground),
			background: CursesColorNumberToColorName (background));
	}

	/// <remarks>
	/// In the CursesDriver, colors are encoded as an int. 
	/// The foreground color is stored in the most significant 4 bits, 
	/// and the background color is stored in the least significant 4 bits.
	/// The Terminal.GUi Color values are converted to curses color encoding before being encoded.
	/// </remarks>
	private Attribute MakeColor (ColorName foregroundName, ColorName backgroundName)
	{
		if (!RunningUnitTests) {
			return MakeColor (ColorNameToCursesColorNumber (foregroundName), ColorNameToCursesColorNumber (backgroundName));
		} else {
			return new Attribute (
				platformColor: 0,
				foreground: ColorNameToCursesColorNumber (foregroundName),
				background: ColorNameToCursesColorNumber (backgroundName));
		}
	}


	/// <remarks>
	/// In the CursesDriver, colors are encoded as an int. 
	/// The foreground color is stored in the most significant 4 bits, 
	/// and the background color is stored in the least significant 4 bits.
	/// The Terminal.GUi Color values are converted to curses color encoding before being encoded.
	/// </remarks>
	public override Attribute MakeColor (Color foreground, Color background)
	{
		if (!RunningUnitTests) {
			return MakeColor (foreground.ColorName, background.ColorName);
		} else {
			return new Attribute (
				platformColor: 0,
				foreground: foreground,
				background: background);
		}
	}

	static short ColorNameToCursesColorNumber (ColorName color)
	{
		switch (color) {
		case ColorName.Black:
			return Curses.COLOR_BLACK;
		case ColorName.Blue:
			return Curses.COLOR_BLUE;
		case ColorName.Green:
			return Curses.COLOR_GREEN;
		case ColorName.Cyan:
			return Curses.COLOR_CYAN;
		case ColorName.Red:
			return Curses.COLOR_RED;
		case ColorName.Magenta:
			return Curses.COLOR_MAGENTA;
		case ColorName.Yellow:
			return Curses.COLOR_YELLOW;
		case ColorName.Gray:
			return Curses.COLOR_WHITE;
		case ColorName.DarkGray:
			return Curses.COLOR_GRAY;
		case ColorName.BrightBlue:
			return Curses.COLOR_BLUE | Curses.COLOR_GRAY;
		case ColorName.BrightGreen:
			return Curses.COLOR_GREEN | Curses.COLOR_GRAY;
		case ColorName.BrightCyan:
			return Curses.COLOR_CYAN | Curses.COLOR_GRAY;
		case ColorName.BrightRed:
			return Curses.COLOR_RED | Curses.COLOR_GRAY;
		case ColorName.BrightMagenta:
			return Curses.COLOR_MAGENTA | Curses.COLOR_GRAY;
		case ColorName.BrightYellow:
			return Curses.COLOR_YELLOW | Curses.COLOR_GRAY;
		case ColorName.White:
			return Curses.COLOR_WHITE | Curses.COLOR_GRAY;
		}
		throw new ArgumentException ("Invalid color code");
	}

	static ColorName CursesColorNumberToColorName (short color)
	{
		switch (color) {
		case Curses.COLOR_BLACK:
			return ColorName.Black;
		case Curses.COLOR_BLUE:
			return ColorName.Blue;
		case Curses.COLOR_GREEN:
			return ColorName.Green;
		case Curses.COLOR_CYAN:
			return ColorName.Cyan;
		case Curses.COLOR_RED:
			return ColorName.Red;
		case Curses.COLOR_MAGENTA:
			return ColorName.Magenta;
		case Curses.COLOR_YELLOW:
			return ColorName.Yellow;
		case Curses.COLOR_WHITE:
			return ColorName.Gray;
		case Curses.COLOR_GRAY:
			return ColorName.DarkGray;
		case Curses.COLOR_BLUE | Curses.COLOR_GRAY:
			return ColorName.BrightBlue;
		case Curses.COLOR_GREEN | Curses.COLOR_GRAY:
			return ColorName.BrightGreen;
		case Curses.COLOR_CYAN | Curses.COLOR_GRAY:
			return ColorName.BrightCyan;
		case Curses.COLOR_RED | Curses.COLOR_GRAY:
			return ColorName.BrightRed;
		case Curses.COLOR_MAGENTA | Curses.COLOR_GRAY:
			return ColorName.BrightMagenta;
		case Curses.COLOR_YELLOW | Curses.COLOR_GRAY:
			return ColorName.BrightYellow;
		case Curses.COLOR_WHITE | Curses.COLOR_GRAY:
			return ColorName.White;
		}
		throw new ArgumentException ("Invalid curses color code");
	}

	#endregion

	public override void UpdateCursor ()
	{
		EnsureCursorVisibility ();

		if (!RunningUnitTests && Col >= 0 && Col < Cols && Row >= 0 && Row < Rows) {
			Curses.move (Row, Col);
		}
	}

	internal override void End ()
	{
		StopReportingMouseMoves ();
		SetCursorVisibility (CursorVisibility.Default);

		if (_mainLoopDriver != null) {
			_mainLoopDriver.RemoveWatch (_processInputToken);
		}

		if (RunningUnitTests) {
			return;
		}
		// throws away any typeahead that has been typed by
		// the user and has not yet been read by the program.
		Curses.flushinp ();

		Curses.endwin ();
	}

	public override void UpdateScreen ()
	{
		for (int row = 0; row < Rows; row++) {
			if (!_dirtyLines [row]) {
				continue;
			}
			_dirtyLines [row] = false;

			for (int col = 0; col < Cols; col++) {
				if (Contents [row, col].IsDirty == false) {
					continue;
				}
				if (RunningUnitTests) {
					// In unit tests, we don't want to actually write to the screen.
					continue;
				}
				Curses.attrset (Contents [row, col].Attribute.GetValueOrDefault ().PlatformColor);

				var rune = Contents [row, col].Runes [0];
				if (rune.IsBmp) {
					// BUGBUG: CursesDriver doesn't render CharMap correctly for wide chars (and other Unicode) - Curses is doing something funky with glyphs that report GetColums() of 1 yet are rendered wide. E.g. 0x2064 (invisible times) is reported as 1 column but is rendered as 2. WindowsDriver & NetDriver correctly render this as 1 column, overlapping the next cell.
					if (rune.GetColumns () < 2) {
						Curses.mvaddch (row, col, rune.Value);
					} else /*if (col + 1 < Cols)*/ {
						Curses.mvaddwstr (row, col, rune.ToString ());
					}

				} else {
					Curses.mvaddwstr (row, col, rune.ToString ());
					if (rune.GetColumns () > 1 && col + 1 < Cols) {
						// TODO: This is a hack to deal with non-BMP and wide characters.
						//col++;
						Curses.mvaddch (row, ++col, '*');
					}
				}
			}
		}

		if (!RunningUnitTests) {
			Curses.move (Row, Col);
			_window.wrefresh ();
		}
	}

	public Curses.Window _window;

	static Key MapCursesKey (int cursesKey)
	{
		switch (cursesKey) {
		case Curses.KeyF1: return Key.F1;
		case Curses.KeyF2: return Key.F2;
		case Curses.KeyF3: return Key.F3;
		case Curses.KeyF4: return Key.F4;
		case Curses.KeyF5: return Key.F5;
		case Curses.KeyF6: return Key.F6;
		case Curses.KeyF7: return Key.F7;
		case Curses.KeyF8: return Key.F8;
		case Curses.KeyF9: return Key.F9;
		case Curses.KeyF10: return Key.F10;
		case Curses.KeyF11: return Key.F11;
		case Curses.KeyF12: return Key.F12;
		case Curses.KeyUp: return Key.CursorUp;
		case Curses.KeyDown: return Key.CursorDown;
		case Curses.KeyLeft: return Key.CursorLeft;
		case Curses.KeyRight: return Key.CursorRight;
		case Curses.KeyHome: return Key.Home;
		case Curses.KeyEnd: return Key.End;
		case Curses.KeyNPage: return Key.PageDown;
		case Curses.KeyPPage: return Key.PageUp;
		case Curses.KeyDeleteChar: return Key.DeleteChar;
		case Curses.KeyInsertChar: return Key.InsertChar;
		case Curses.KeyTab: return Key.Tab;
		case Curses.KeyBackTab: return Key.BackTab;
		case Curses.KeyBackspace: return Key.Backspace;
		case Curses.ShiftKeyUp: return Key.CursorUp | Key.ShiftMask;
		case Curses.ShiftKeyDown: return Key.CursorDown | Key.ShiftMask;
		case Curses.ShiftKeyLeft: return Key.CursorLeft | Key.ShiftMask;
		case Curses.ShiftKeyRight: return Key.CursorRight | Key.ShiftMask;
		case Curses.ShiftKeyHome: return Key.Home | Key.ShiftMask;
		case Curses.ShiftKeyEnd: return Key.End | Key.ShiftMask;
		case Curses.ShiftKeyNPage: return Key.PageDown | Key.ShiftMask;
		case Curses.ShiftKeyPPage: return Key.PageUp | Key.ShiftMask;
		case Curses.AltKeyUp: return Key.CursorUp | Key.AltMask;
		case Curses.AltKeyDown: return Key.CursorDown | Key.AltMask;
		case Curses.AltKeyLeft: return Key.CursorLeft | Key.AltMask;
		case Curses.AltKeyRight: return Key.CursorRight | Key.AltMask;
		case Curses.AltKeyHome: return Key.Home | Key.AltMask;
		case Curses.AltKeyEnd: return Key.End | Key.AltMask;
		case Curses.AltKeyNPage: return Key.PageDown | Key.AltMask;
		case Curses.AltKeyPPage: return Key.PageUp | Key.AltMask;
		case Curses.CtrlKeyUp: return Key.CursorUp | Key.CtrlMask;
		case Curses.CtrlKeyDown: return Key.CursorDown | Key.CtrlMask;
		case Curses.CtrlKeyLeft: return Key.CursorLeft | Key.CtrlMask;
		case Curses.CtrlKeyRight: return Key.CursorRight | Key.CtrlMask;
		case Curses.CtrlKeyHome: return Key.Home | Key.CtrlMask;
		case Curses.CtrlKeyEnd: return Key.End | Key.CtrlMask;
		case Curses.CtrlKeyNPage: return Key.PageDown | Key.CtrlMask;
		case Curses.CtrlKeyPPage: return Key.PageUp | Key.CtrlMask;
		case Curses.ShiftCtrlKeyUp: return Key.CursorUp | Key.ShiftMask | Key.CtrlMask;
		case Curses.ShiftCtrlKeyDown: return Key.CursorDown | Key.ShiftMask | Key.CtrlMask;
		case Curses.ShiftCtrlKeyLeft: return Key.CursorLeft | Key.ShiftMask | Key.CtrlMask;
		case Curses.ShiftCtrlKeyRight: return Key.CursorRight | Key.ShiftMask | Key.CtrlMask;
		case Curses.ShiftCtrlKeyHome: return Key.Home | Key.ShiftMask | Key.CtrlMask;
		case Curses.ShiftCtrlKeyEnd: return Key.End | Key.ShiftMask | Key.CtrlMask;
		case Curses.ShiftCtrlKeyNPage: return Key.PageDown | Key.ShiftMask | Key.CtrlMask;
		case Curses.ShiftCtrlKeyPPage: return Key.PageUp | Key.ShiftMask | Key.CtrlMask;
		case Curses.ShiftAltKeyUp: return Key.CursorUp | Key.ShiftMask | Key.AltMask;
		case Curses.ShiftAltKeyDown: return Key.CursorDown | Key.ShiftMask | Key.AltMask;
		case Curses.ShiftAltKeyLeft: return Key.CursorLeft | Key.ShiftMask | Key.AltMask;
		case Curses.ShiftAltKeyRight: return Key.CursorRight | Key.ShiftMask | Key.AltMask;
		case Curses.ShiftAltKeyNPage: return Key.PageDown | Key.ShiftMask | Key.AltMask;
		case Curses.ShiftAltKeyPPage: return Key.PageUp | Key.ShiftMask | Key.AltMask;
		case Curses.ShiftAltKeyHome: return Key.Home | Key.ShiftMask | Key.AltMask;
		case Curses.ShiftAltKeyEnd: return Key.End | Key.ShiftMask | Key.AltMask;
		case Curses.AltCtrlKeyNPage: return Key.PageDown | Key.AltMask | Key.CtrlMask;
		case Curses.AltCtrlKeyPPage: return Key.PageUp | Key.AltMask | Key.CtrlMask;
		case Curses.AltCtrlKeyHome: return Key.Home | Key.AltMask | Key.CtrlMask;
		case Curses.AltCtrlKeyEnd: return Key.End | Key.AltMask | Key.CtrlMask;
		default: return Key.Unknown;
		}
	}

	KeyModifiers _keyModifiers;

	KeyModifiers MapKeyModifiers (Key key)
	{
		if (_keyModifiers == null) {
			_keyModifiers = new KeyModifiers ();
		}

		if (!_keyModifiers.Shift && (key & Key.ShiftMask) != 0) {
			_keyModifiers.Shift = true;
		}
		if (!_keyModifiers.Alt && (key & Key.AltMask) != 0) {
			_keyModifiers.Alt = true;
		}
		if (!_keyModifiers.Ctrl && (key & Key.CtrlMask) != 0) {
			_keyModifiers.Ctrl = true;
		}

		return _keyModifiers;
	}

	internal void ProcessInput ()
	{
		int wch;
		var code = Curses.get_wch (out wch);
		//System.Diagnostics.Debug.WriteLine ($"code: {code}; wch: {wch}");
		if (code == Curses.ERR) {
			return;
		}

		_keyModifiers = new KeyModifiers ();
		Key k = Key.Null;

		if (code == Curses.KEY_CODE_YES) {
			while (code == Curses.KEY_CODE_YES && wch == Curses.KeyResize) {
				ProcessWinChange ();
				code = Curses.get_wch (out wch);
			}
			if (wch == 0) {
				return;
			}
			if (wch == Curses.KeyMouse) {
				int wch2 = wch;

				while (wch2 == Curses.KeyMouse) {
					KeyEvent key = null;
					ConsoleKeyInfo [] cki = new ConsoleKeyInfo [] {
							new ConsoleKeyInfo ((char)Key.Esc, 0, false, false, false),
							new ConsoleKeyInfo ('[', 0, false, false, false),
							new ConsoleKeyInfo ('<', 0, false, false, false)
						};
					code = 0;
					HandleEscSeqResponse (ref code, ref k, ref wch2, ref key, ref cki);
				}
				return;
			}
			k = MapCursesKey (wch);
			if (wch >= 277 && wch <= 288) {
				// Shift+(F1 - F12)
				wch -= 12;
				k = Key.ShiftMask | MapCursesKey (wch);
			} else if (wch >= 289 && wch <= 300) {
				// Ctrl+(F1 - F12)
				wch -= 24;
				k = Key.CtrlMask | MapCursesKey (wch);
			} else if (wch >= 301 && wch <= 312) {
				// Ctrl+Shift+(F1 - F12)
				wch -= 36;
				k = Key.CtrlMask | Key.ShiftMask | MapCursesKey (wch);
			} else if (wch >= 313 && wch <= 324) {
				// Alt+(F1 - F12)
				wch -= 48;
				k = Key.AltMask | MapCursesKey (wch);
			} else if (wch >= 325 && wch <= 327) {
				// Shift+Alt+(F1 - F3)
				wch -= 60;
				k = Key.ShiftMask | Key.AltMask | MapCursesKey (wch);
			}
			OnKeyDown (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
			OnKeyUp (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
			OnKeyPressed (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
			return;
		}

		// Special handling for ESC, we want to try to catch ESC+letter to simulate alt-letter as well as Alt-Fkey
		if (wch == 27) {
			Curses.timeout (10);

			code = Curses.get_wch (out int wch2);

			if (code == Curses.KEY_CODE_YES) {
				k = Key.AltMask | MapCursesKey (wch);
			}
			if (code == 0) {
				KeyEvent key = null;

				// The ESC-number handling, debatable.
				// Simulates the AltMask itself by pressing Alt + Space.
				if (wch2 == (int)Key.Space) {
					k = Key.AltMask;
				} else if (wch2 - (int)Key.Space >= (uint)Key.A && wch2 - (int)Key.Space <= (uint)Key.Z) {
					k = (Key)((uint)Key.AltMask + (wch2 - (int)Key.Space));
				} else if (wch2 >= (uint)Key.A - 64 && wch2 <= (uint)Key.Z - 64) {
					k = (Key)((uint)(Key.AltMask | Key.CtrlMask) + (wch2 + 64));
				} else if (wch2 >= (uint)Key.D0 && wch2 <= (uint)Key.D9) {
					k = (Key)((uint)Key.AltMask + (uint)Key.D0 + (wch2 - (uint)Key.D0));
				} else if (wch2 == Curses.KeyCSI) {
					ConsoleKeyInfo [] cki = new ConsoleKeyInfo [] {
							new ConsoleKeyInfo ((char)Key.Esc, 0, false, false, false),
							new ConsoleKeyInfo ('[', 0, false, false, false)
						};
					HandleEscSeqResponse (ref code, ref k, ref wch2, ref key, ref cki);
					return;
				} else {
					// Unfortunately there are no way to differentiate Ctrl+Alt+alfa and Ctrl+Shift+Alt+alfa.
					if (((Key)wch2 & Key.CtrlMask) != 0) {
						_keyModifiers.Ctrl = true;
					}
					if (wch2 == 0) {
						k = Key.CtrlMask | Key.AltMask | Key.Space;
					} else if (wch >= (uint)Key.A && wch <= (uint)Key.Z) {
						_keyModifiers.Shift = true;
						_keyModifiers.Alt = true;
					} else if (wch2 < 256) {
						k = (Key)wch2;
						_keyModifiers.Alt = true;
					} else {
						k = (Key)((uint)(Key.AltMask | Key.CtrlMask) + wch2);
					}
				}
				key = new KeyEvent (k, MapKeyModifiers (k));
				OnKeyDown (new KeyEventEventArgs (key));
				OnKeyUp (new KeyEventEventArgs (key));
				OnKeyPressed (new KeyEventEventArgs (key));
			} else {
				k = Key.Esc;
				OnKeyPressed (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
			}
		} else if (wch == Curses.KeyTab) {
			k = MapCursesKey (wch);
			OnKeyDown (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
			OnKeyUp (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
			OnKeyPressed (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
		} else {
			// Unfortunately there are no way to differentiate Ctrl+alfa and Ctrl+Shift+alfa.
			k = (Key)wch;
			if (wch == 0) {
				k = Key.CtrlMask | Key.Space;
			} else if (wch >= (uint)Key.A - 64 && wch <= (uint)Key.Z - 64) {
				if ((Key)(wch + 64) != Key.J) {
					k = Key.CtrlMask | (Key)(wch + 64);
				}
			} else if (wch >= (uint)Key.A && wch <= (uint)Key.Z) {
				_keyModifiers.Shift = true;
			}
			OnKeyDown (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
			OnKeyUp (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
			OnKeyPressed (new KeyEventEventArgs (new KeyEvent (k, MapKeyModifiers (k))));
		}
		// Cause OnKeyUp and OnKeyPressed. Note that the special handling for ESC above 
		// will not impact KeyUp.
		// This is causing ESC firing even if another keystroke was handled.
		//if (wch == Curses.KeyTab) {
		//	keyUpHandler (new KeyEvent (MapCursesKey (wch), keyModifiers));
		//} else {
		//	keyUpHandler (new KeyEvent ((Key)wch, keyModifiers));
		//}
	}

	void HandleEscSeqResponse (ref int code, ref Key k, ref int wch2, ref KeyEvent key, ref ConsoleKeyInfo [] cki)
	{
		ConsoleKey ck = 0;
		ConsoleModifiers mod = 0;
		while (code == 0) {
			code = Curses.get_wch (out wch2);
			var consoleKeyInfo = new ConsoleKeyInfo ((char)wch2, 0, false, false, false);
			if (wch2 == 0 || wch2 == 27 || wch2 == Curses.KeyMouse) {
				EscSeqUtils.DecodeEscSeq (null, ref consoleKeyInfo, ref ck, cki, ref mod, out _, out _, out _, out _, out bool isKeyMouse, out List<MouseFlags> mouseFlags, out Point pos, out _, ProcessMouseEvent);
				if (isKeyMouse) {
					foreach (var mf in mouseFlags) {
						ProcessMouseEvent (mf, pos);
					}
					cki = null;
					if (wch2 == 27) {
						cki = EscSeqUtils.ResizeArray (new ConsoleKeyInfo ((char)Key.Esc, 0,
							false, false, false), cki);
					}
				} else {
					k = ConsoleKeyMapping.MapConsoleKeyToKey (consoleKeyInfo.Key, out _);
					k = ConsoleKeyMapping.MapKeyModifiers (consoleKeyInfo, k);
					key = new KeyEvent (k, MapKeyModifiers (k));
					OnKeyDown (new KeyEventEventArgs (key));
					OnKeyPressed (new KeyEventEventArgs (key));
				}
			} else {
				cki = EscSeqUtils.ResizeArray (consoleKeyInfo, cki);
			}
		}
	}

	MouseFlags _lastMouseFlags;

	void ProcessMouseEvent (MouseFlags mouseFlag, Point pos)
	{
		bool WasButtonReleased (MouseFlags flag)
		{
			return flag.HasFlag (MouseFlags.Button1Released) ||
				flag.HasFlag (MouseFlags.Button2Released) ||
				flag.HasFlag (MouseFlags.Button3Released) ||
				flag.HasFlag (MouseFlags.Button4Released);
		}

		bool IsButtonNotPressed (MouseFlags flag)
		{
			return !flag.HasFlag (MouseFlags.Button1Pressed) &&
				!flag.HasFlag (MouseFlags.Button2Pressed) &&
				!flag.HasFlag (MouseFlags.Button3Pressed) &&
				!flag.HasFlag (MouseFlags.Button4Pressed);
		}

		bool IsButtonClickedOrDoubleClicked (MouseFlags flag)
		{
			return flag.HasFlag (MouseFlags.Button1Clicked) ||
				flag.HasFlag (MouseFlags.Button2Clicked) ||
				flag.HasFlag (MouseFlags.Button3Clicked) ||
				flag.HasFlag (MouseFlags.Button4Clicked) ||
				flag.HasFlag (MouseFlags.Button1DoubleClicked) ||
				flag.HasFlag (MouseFlags.Button2DoubleClicked) ||
				flag.HasFlag (MouseFlags.Button3DoubleClicked) ||
				flag.HasFlag (MouseFlags.Button4DoubleClicked);
		}

		if ((WasButtonReleased (mouseFlag) && IsButtonNotPressed (_lastMouseFlags)) ||
			(IsButtonClickedOrDoubleClicked (mouseFlag) && _lastMouseFlags == 0)) {
			return;
		}

		_lastMouseFlags = mouseFlag;

		var me = new MouseEvent () {
			Flags = mouseFlag,
			X = pos.X,
			Y = pos.Y
		};
		OnMouseEvent (new MouseEventEventArgs (me));
	}


	public static bool Is_WSL_Platform ()
	{
		// xclip does not work on WSL, so we need to use the Windows clipboard vis Powershell
		//if (new CursesClipboard ().IsSupported) {
		//	// If xclip is installed on Linux under WSL, this will return true.
		//	return false;
		//}
		var (exitCode, result) = ClipboardProcessRunner.Bash ("uname -a", waitForOutput: true);
		if (exitCode == 0 && result.Contains ("microsoft") && result.Contains ("WSL")) {
			return true;
		}
		return false;
	}

	public override void Suspend ()
	{
		StopReportingMouseMoves ();
		if (!RunningUnitTests) {
			Platform.Suspend ();
			Curses.Window.Standard.redrawwin ();
			Curses.refresh ();
		}
		StartReportingMouseMoves ();
	}

	public void StartReportingMouseMoves ()
	{
		if (!RunningUnitTests) {
			Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
		}
	}

	public void StopReportingMouseMoves ()
	{
		if (!RunningUnitTests) {
			Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);
		}
	}

	/// <inheritdoc/>
	public override bool GetCursorVisibility (out CursorVisibility visibility)
	{
		visibility = CursorVisibility.Invisible;

		if (!_currentCursorVisibility.HasValue)
			return false;

		visibility = _currentCursorVisibility.Value;

		return true;
	}

	/// <inheritdoc/>
	public override bool SetCursorVisibility (CursorVisibility visibility)
	{
		if (_initialCursorVisibility.HasValue == false) {
			return false;
		}

		if (!RunningUnitTests) {
			Curses.curs_set (((int)visibility >> 16) & 0x000000FF);
		}

		if (visibility != CursorVisibility.Invisible) {
			Console.Out.Write (EscSeqUtils.CSI_SetCursorStyle ((EscSeqUtils.DECSCUSR_Style)(((int)visibility >> 24) & 0xFF)));
		}

		_currentCursorVisibility = visibility;

		return true;
	}

	/// <inheritdoc/>
	public override bool EnsureCursorVisibility ()
	{
		return false;
	}

	public override void SendKeys (char keyChar, ConsoleKey consoleKey, bool shift, bool alt, bool control)
	{
		Key key;

		if (consoleKey == ConsoleKey.Packet) {
			ConsoleModifiers mod = new ConsoleModifiers ();
			if (shift) {
				mod |= ConsoleModifiers.Shift;
			}
			if (alt) {
				mod |= ConsoleModifiers.Alt;
			}
			if (control) {
				mod |= ConsoleModifiers.Control;
			}
			var kchar = ConsoleKeyMapping.GetKeyCharFromConsoleKey (keyChar, mod, out uint ckey, out _);
			key = ConsoleKeyMapping.MapConsoleKeyToKey ((ConsoleKey)ckey, out bool mappable);
			if (mappable) {
				key = (Key)kchar;
			}
		} else {
			key = (Key)keyChar;
		}

		KeyModifiers km = new KeyModifiers ();
		if (shift) {
			if (keyChar == 0) {
				key |= Key.ShiftMask;
			}
			km.Shift = shift;
		}
		if (alt) {
			key |= Key.AltMask;
			km.Alt = alt;
		}
		if (control) {
			key |= Key.CtrlMask;
			km.Ctrl = control;
		}
		OnKeyDown (new KeyEventEventArgs (new KeyEvent (key, km)));
		OnKeyPressed (new KeyEventEventArgs (new KeyEvent (key, km)));
		OnKeyUp (new KeyEventEventArgs (new KeyEvent (key, km)));
	}


}

internal static class Platform {
	[DllImport ("libc")]
	static extern int uname (IntPtr buf);

	[DllImport ("libc")]
	static extern int killpg (int pgrp, int pid);

	static int _suspendSignal;

	static int GetSuspendSignal ()
	{
		if (_suspendSignal != 0) {
			return _suspendSignal;
		}

		IntPtr buf = Marshal.AllocHGlobal (8192);
		if (uname (buf) != 0) {
			Marshal.FreeHGlobal (buf);
			_suspendSignal = -1;
			return _suspendSignal;
		}
		try {
			switch (Marshal.PtrToStringAnsi (buf)) {
			case "Darwin":
			case "DragonFly":
			case "FreeBSD":
			case "NetBSD":
			case "OpenBSD":
				_suspendSignal = 18;
				break;
			case "Linux":
				// TODO: should fetch the machine name and
				// if it is MIPS return 24
				_suspendSignal = 20;
				break;
			case "Solaris":
				_suspendSignal = 24;
				break;
			default:
				_suspendSignal = -1;
				break;
			}
			return _suspendSignal;
		} finally {
			Marshal.FreeHGlobal (buf);
		}
	}

	/// <summary>
	/// Suspends the process by sending SIGTSTP to itself
	/// </summary>
	/// <returns>The suspend.</returns>
	static public bool Suspend ()
	{
		int signal = GetSuspendSignal ();
		if (signal == -1) {
			return false;
		}
		killpg (0, signal);
		return true;
	}
}