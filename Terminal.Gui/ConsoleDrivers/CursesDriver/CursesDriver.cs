//
// Driver.cs: Curses-based Driver
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Terminal.Gui.ConsoleDrivers;
using Unix.Terminal;

namespace Terminal.Gui;

/// <summary>
/// This is the Curses driver for the gui.cs/Terminal framework.
/// </summary>
internal class CursesDriver : ConsoleDriver {

	public override int Cols {
		get => Curses.Cols;
		internal set => Curses.Cols = value;
	}
	public override int Rows {
		get => Curses.Lines;
		internal set => Curses.Lines = value;
	}

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

		CurrentAttribute = new Attribute (ColorName.White, ColorName.Black);

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
	public override Attribute MakeColor (Color foreground, Color background)
	{
		if (!RunningUnitTests) {
			return MakeColor (ColorNameToCursesColorNumber (foreground.ColorName), ColorNameToCursesColorNumber (background.ColorName));
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

				var rune = Contents [row, col].Rune;
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

	static ConsoleDriverKey MapCursesKey (int cursesKey)
	{
		switch (cursesKey) {
		case Curses.KeyF1: return ConsoleDriverKey.F1;
		case Curses.KeyF2: return ConsoleDriverKey.F2;
		case Curses.KeyF3: return ConsoleDriverKey.F3;
		case Curses.KeyF4: return ConsoleDriverKey.F4;
		case Curses.KeyF5: return ConsoleDriverKey.F5;
		case Curses.KeyF6: return ConsoleDriverKey.F6;
		case Curses.KeyF7: return ConsoleDriverKey.F7;
		case Curses.KeyF8: return ConsoleDriverKey.F8;
		case Curses.KeyF9: return ConsoleDriverKey.F9;
		case Curses.KeyF10: return ConsoleDriverKey.F10;
		case Curses.KeyF11: return ConsoleDriverKey.F11;
		case Curses.KeyF12: return ConsoleDriverKey.F12;
		case Curses.KeyUp: return ConsoleDriverKey.CursorUp;
		case Curses.KeyDown: return ConsoleDriverKey.CursorDown;
		case Curses.KeyLeft: return ConsoleDriverKey.CursorLeft;
		case Curses.KeyRight: return ConsoleDriverKey.CursorRight;
		case Curses.KeyHome: return ConsoleDriverKey.Home;
		case Curses.KeyEnd: return ConsoleDriverKey.End;
		case Curses.KeyNPage: return ConsoleDriverKey.PageDown;
		case Curses.KeyPPage: return ConsoleDriverKey.PageUp;
		case Curses.KeyDeleteChar: return ConsoleDriverKey.DeleteChar;
		case Curses.KeyInsertChar: return ConsoleDriverKey.InsertChar;
		case Curses.KeyTab: return ConsoleDriverKey.Tab;
		case Curses.KeyBackTab: return ConsoleDriverKey.Tab | ConsoleDriverKey.ShiftMask;
		case Curses.KeyBackspace: return ConsoleDriverKey.Backspace;
		case Curses.ShiftKeyUp: return ConsoleDriverKey.CursorUp | ConsoleDriverKey.ShiftMask;
		case Curses.ShiftKeyDown: return ConsoleDriverKey.CursorDown | ConsoleDriverKey.ShiftMask;
		case Curses.ShiftKeyLeft: return ConsoleDriverKey.CursorLeft | ConsoleDriverKey.ShiftMask;
		case Curses.ShiftKeyRight: return ConsoleDriverKey.CursorRight | ConsoleDriverKey.ShiftMask;
		case Curses.ShiftKeyHome: return ConsoleDriverKey.Home | ConsoleDriverKey.ShiftMask;
		case Curses.ShiftKeyEnd: return ConsoleDriverKey.End | ConsoleDriverKey.ShiftMask;
		case Curses.ShiftKeyNPage: return ConsoleDriverKey.PageDown | ConsoleDriverKey.ShiftMask;
		case Curses.ShiftKeyPPage: return ConsoleDriverKey.PageUp | ConsoleDriverKey.ShiftMask;
		case Curses.AltKeyUp: return ConsoleDriverKey.CursorUp | ConsoleDriverKey.AltMask;
		case Curses.AltKeyDown: return ConsoleDriverKey.CursorDown | ConsoleDriverKey.AltMask;
		case Curses.AltKeyLeft: return ConsoleDriverKey.CursorLeft | ConsoleDriverKey.AltMask;
		case Curses.AltKeyRight: return ConsoleDriverKey.CursorRight | ConsoleDriverKey.AltMask;
		case Curses.AltKeyHome: return ConsoleDriverKey.Home | ConsoleDriverKey.AltMask;
		case Curses.AltKeyEnd: return ConsoleDriverKey.End | ConsoleDriverKey.AltMask;
		case Curses.AltKeyNPage: return ConsoleDriverKey.PageDown | ConsoleDriverKey.AltMask;
		case Curses.AltKeyPPage: return ConsoleDriverKey.PageUp | ConsoleDriverKey.AltMask;
		case Curses.CtrlKeyUp: return ConsoleDriverKey.CursorUp | ConsoleDriverKey.CtrlMask;
		case Curses.CtrlKeyDown: return ConsoleDriverKey.CursorDown | ConsoleDriverKey.CtrlMask;
		case Curses.CtrlKeyLeft: return ConsoleDriverKey.CursorLeft | ConsoleDriverKey.CtrlMask;
		case Curses.CtrlKeyRight: return ConsoleDriverKey.CursorRight | ConsoleDriverKey.CtrlMask;
		case Curses.CtrlKeyHome: return ConsoleDriverKey.Home | ConsoleDriverKey.CtrlMask;
		case Curses.CtrlKeyEnd: return ConsoleDriverKey.End | ConsoleDriverKey.CtrlMask;
		case Curses.CtrlKeyNPage: return ConsoleDriverKey.PageDown | ConsoleDriverKey.CtrlMask;
		case Curses.CtrlKeyPPage: return ConsoleDriverKey.PageUp | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftCtrlKeyUp: return ConsoleDriverKey.CursorUp | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftCtrlKeyDown: return ConsoleDriverKey.CursorDown | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftCtrlKeyLeft: return ConsoleDriverKey.CursorLeft | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftCtrlKeyRight: return ConsoleDriverKey.CursorRight | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftCtrlKeyHome: return ConsoleDriverKey.Home | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftCtrlKeyEnd: return ConsoleDriverKey.End | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftCtrlKeyNPage: return ConsoleDriverKey.PageDown | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftCtrlKeyPPage: return ConsoleDriverKey.PageUp | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.CtrlMask;
		case Curses.ShiftAltKeyUp: return ConsoleDriverKey.CursorUp | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask;
		case Curses.ShiftAltKeyDown: return ConsoleDriverKey.CursorDown | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask;
		case Curses.ShiftAltKeyLeft: return ConsoleDriverKey.CursorLeft | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask;
		case Curses.ShiftAltKeyRight: return ConsoleDriverKey.CursorRight | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask;
		case Curses.ShiftAltKeyNPage: return ConsoleDriverKey.PageDown | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask;
		case Curses.ShiftAltKeyPPage: return ConsoleDriverKey.PageUp | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask;
		case Curses.ShiftAltKeyHome: return ConsoleDriverKey.Home | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask;
		case Curses.ShiftAltKeyEnd: return ConsoleDriverKey.End | ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask;
		case Curses.AltCtrlKeyNPage: return ConsoleDriverKey.PageDown | ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask;
		case Curses.AltCtrlKeyPPage: return ConsoleDriverKey.PageUp | ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask;
		case Curses.AltCtrlKeyHome: return ConsoleDriverKey.Home | ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask;
		case Curses.AltCtrlKeyEnd: return ConsoleDriverKey.End | ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask;
		default: return ConsoleDriverKey.Unknown;
		}
	}

	internal void ProcessInput ()
	{
		int wch;
		var code = Curses.get_wch (out wch);
		//System.Diagnostics.Debug.WriteLine ($"code: {code}; wch: {wch}");
		if (code == Curses.ERR) {
			return;
		}
		ConsoleDriverKey k = ConsoleDriverKey.Null;

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
					KeyEventArgs kea = null;
					ConsoleKeyInfo [] cki = new ConsoleKeyInfo [] {
							new ConsoleKeyInfo ((char)ConsoleDriverKey.Esc, 0, false, false, false),
							new ConsoleKeyInfo ('[', 0, false, false, false),
							new ConsoleKeyInfo ('<', 0, false, false, false)
						};
					code = 0;
					HandleEscSeqResponse (ref code, ref k, ref wch2, ref kea, ref cki);
				}
				return;
			}
			k = MapCursesKey (wch);
			if (wch >= 277 && wch <= 288) {
				// Shift+(F1 - F12)
				wch -= 12;
				k = ConsoleDriverKey.ShiftMask | MapCursesKey (wch);
			} else if (wch >= 289 && wch <= 300) {
				// Ctrl+(F1 - F12)
				wch -= 24;
				k = ConsoleDriverKey.CtrlMask | MapCursesKey (wch);
			} else if (wch >= 301 && wch <= 312) {
				// Ctrl+Shift+(F1 - F12)
				wch -= 36;
				k = ConsoleDriverKey.CtrlMask | ConsoleDriverKey.ShiftMask | MapCursesKey (wch);
			} else if (wch >= 313 && wch <= 324) {
				// Alt+(F1 - F12)
				wch -= 48;
				k = ConsoleDriverKey.AltMask | MapCursesKey (wch);
			} else if (wch >= 325 && wch <= 327) {
				// Shift+Alt+(F1 - F3)
				wch -= 60;
				k = ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | MapCursesKey (wch);
			}
			OnKeyDown (new KeyEventArgs (k));
			OnKeyUp (new KeyEventArgs (k));
			return;
		}

		// Special handling for ESC, we want to try to catch ESC+letter to simulate alt-letter as well as Alt-Fkey
		if (wch == 27) {
			Curses.timeout (10);

			code = Curses.get_wch (out int wch2);

			if (code == Curses.KEY_CODE_YES) {
				k = ConsoleDriverKey.AltMask | MapCursesKey (wch);
			}
			KeyEventArgs key = null;
			if (code == 0) {

				// The ESC-number handling, debatable.
				// Simulates the AltMask itself by pressing Alt + Space.
				if (wch2 == (int)ConsoleDriverKey.Space) {
					k = ConsoleDriverKey.AltMask;
				} else if (wch2 - (int)ConsoleDriverKey.Space >= (uint)ConsoleDriverKey.A && wch2 - (int)ConsoleDriverKey.Space <= (uint)ConsoleDriverKey.Z) {
					k = (ConsoleDriverKey)((uint)ConsoleDriverKey.AltMask + (wch2 - (int)ConsoleDriverKey.Space));
				} else if (wch2 >= (uint)ConsoleDriverKey.A - 64 && wch2 <= (uint)ConsoleDriverKey.Z - 64) {
					k = (ConsoleDriverKey)((uint)(ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask) + (wch2 + 64));
				} else if (wch2 >= (uint)ConsoleDriverKey.D0 && wch2 <= (uint)ConsoleDriverKey.D9) {
					k = (ConsoleDriverKey)((uint)ConsoleDriverKey.AltMask + (uint)ConsoleDriverKey.D0 + (wch2 - (uint)ConsoleDriverKey.D0));
				} else if (wch2 == Curses.KeyCSI) {
					ConsoleKeyInfo [] cki = new ConsoleKeyInfo [] {
							new ConsoleKeyInfo ((char)ConsoleDriverKey.Esc, 0, false, false, false),
							new ConsoleKeyInfo ('[', 0, false, false, false)
						};
					HandleEscSeqResponse (ref code, ref k, ref wch2, ref key, ref cki);
					return;
				} else {
					// Unfortunately there are no way to differentiate Ctrl+Alt+alfa and Ctrl+Shift+Alt+alfa.
					if (((ConsoleDriverKey)wch2 & ConsoleDriverKey.CtrlMask) != 0) {
						k = (ConsoleDriverKey)((uint)ConsoleDriverKey.CtrlMask + (wch2 & ~((int)ConsoleDriverKey.CtrlMask)));
					}
					if (wch2 == 0) {
						k = ConsoleDriverKey.CtrlMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Space;
					} else if (wch >= (uint)ConsoleDriverKey.A && wch <= (uint)ConsoleDriverKey.Z) {
						k = ConsoleDriverKey.ShiftMask | ConsoleDriverKey.AltMask | ConsoleDriverKey.Space;
					} else if (wch2 < 256) {
						k = (ConsoleDriverKey)wch2 | ConsoleDriverKey.AltMask;
					} else {
						k = (ConsoleDriverKey)((uint)(ConsoleDriverKey.AltMask | ConsoleDriverKey.CtrlMask) + wch2);
					}
				}
				key = new KeyEventArgs (k);
			} else {
				key = new KeyEventArgs (ConsoleDriverKey.Esc);
			}
			OnKeyDown (key);
			OnKeyUp (key);
		} else if (wch == Curses.KeyTab) {
			k = MapCursesKey (wch);
			OnKeyDown (new KeyEventArgs (k));
			OnKeyUp (new KeyEventArgs (k));
		} else {
			// Unfortunately there are no way to differentiate Ctrl+alfa and Ctrl+Shift+alfa.
			k = (ConsoleDriverKey)wch;
			if (wch == 0) {
				k = ConsoleDriverKey.CtrlMask | ConsoleDriverKey.Space;
			} else if (wch >= (uint)ConsoleDriverKey.A - 64 && wch <= (uint)ConsoleDriverKey.Z - 64) {
				if ((ConsoleDriverKey)(wch + 64) != ConsoleDriverKey.J) {
					k = ConsoleDriverKey.CtrlMask | (ConsoleDriverKey)(wch + 64);
				}
			} else if (wch >= (uint)ConsoleDriverKey.A && wch <= (uint)ConsoleDriverKey.Z) {
				k = (ConsoleDriverKey)wch | ConsoleDriverKey.ShiftMask;
			} else if (wch <= 'z') {
				k = (ConsoleDriverKey)wch & ~ConsoleDriverKey.Space;
			} 
			OnKeyDown (new KeyEventArgs (k));
			OnKeyUp (new KeyEventArgs (k));
		}
		// Cause OnKeyUp and OnKeyPressed. Note that the special handling for ESC above 
		// will not impact KeyUp.
		// This is causing ESC firing even if another keystroke was handled.
		//if (wch == Curses.KeyTab) {
		//	keyUpHandler (new (MapCursesKey (wch), keyModifiers));
		//} else {
		//	keyUpHandler (new ((Key)wch, keyModifiers));
		//}
	}

	void HandleEscSeqResponse (ref int code, ref ConsoleDriverKey k, ref int wch2, ref KeyEventArgs keyEventArgs, ref ConsoleKeyInfo [] cki)
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
						cki = EscSeqUtils.ResizeArray (new ConsoleKeyInfo ((char)ConsoleDriverKey.Esc, 0,
							false, false, false), cki);
					}
				} else {
					k = ConsoleKeyMapping.MapConsoleKeyToKey (consoleKeyInfo.Key, out _);
					k = ConsoleKeyMapping.MapKeyModifiers (consoleKeyInfo, k);
					keyEventArgs = new (k);
					OnKeyDown (keyEventArgs);
					//OnKeyPressed (key);
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
		ConsoleDriverKey key;

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
				key = (ConsoleDriverKey)kchar;
			}
		} else {
			key = (ConsoleDriverKey)keyChar;
		}

		OnKeyDown (new KeyEventArgs (key));
		OnKeyUp (new KeyEventArgs (key));
		//OnKeyPressed (new KeyEventArgsEventArgs (key));
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