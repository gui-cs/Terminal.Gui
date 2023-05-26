//
// Driver.cs: Curses-based Driver
//
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text;
using Unix.Terminal;
using System.Buffers;

namespace Terminal.Gui;

/// <summary>
/// This is the Curses driver for the gui.cs/Terminal framework.
/// </summary>
internal class CursesDriver : ConsoleDriver {
	public override int Cols => Curses.Cols;
	public override int Rows => Curses.Lines;

	CursorVisibility? _initialCursorVisibility = null;
	CursorVisibility? _currentCursorVisibility = null;

	// If true, Move set Col and Row to an invalid location
	bool _atValidLocation;

	public override void Move (int col, int row)
	{
		base.Move (col, row);

		if (IsValidLocation (col, row)) {
			Curses.move (row, col);
			_atValidLocation = true;
		} else {
			// Not a valid location (outside screen or clip region)
			// Move within the clip region, then AddRune will actually move to Col, Row
			Curses.move (Clip.Y, Clip.X);
			_atValidLocation = false;
		}
	}

	public override bool IsRuneSupported (Rune rune)
	{
		// See Issue #2615 - CursesDriver is broken with non-BMP characters
		return base.IsRuneSupported (rune) && rune.IsBmp;
	}

	public override void AddRune (Rune systemRune)
	{
		var rune = systemRune.MakePrintable ();
		var runeWidth = rune.GetColumns ();
		var validLocation = IsValidLocation (Col, Row);

		if (validLocation) {
			if (!_atValidLocation) {
				// Move was called with an invalid location.
				// Since then, the clip changed, and now we are at a valid location.
				Curses.move (Row, Col);
				_atValidLocation = false;
			}
			if (runeWidth == 0 && Col > 0) {
				// This is a combining character, and we are not at the beginning of the line.
				var combined = new String (new char [] { (char)Contents [Row, Col - 1, 0], (char)rune.Value });
				var normalized = !combined.IsNormalized () ? combined.Normalize () : combined;
				Contents [Row, Col - 1, 0] = normalized [0];
				Contents [Row, Col - 1, 1] = CurrentAttribute.Value;
				Contents [Row, Col - 1, 2] = 1;
				Curses.attrset (Contents [Row, Col - 1, 1]);
				Curses.mvaddch (Row, Col - 1, normalized [0]);
			} else {
				Contents [Row, Col, 1] = CurrentAttribute.Value;
				Contents [Row, Col, 2] = 1;

				if (runeWidth < 2 && Col > 0 && ((Rune)(Contents [Row, Col - 1, 0])).GetColumns () > 1) {
					// This is a single-width character, and we are not at the beginning of the line.
					var curAttr = CurrentAttribute;
					Curses.attrset (Contents [Row, Col - 1, 1]);
					Curses.mvaddch (Row, Col - 1, Rune.ReplacementChar.Value);
					Contents [Row, Col - 1, 0] = Rune.ReplacementChar.Value;
					Curses.move (Row, Col);
					Curses.attrset (curAttr.Value);

				} else if (runeWidth < 2 && Col <= Clip.Right - 1 && ((Rune)(Contents [Row, Col, 0])).GetColumns () > 1) {
					// This is a single-width character, and we are not at the end of the line.
					var curAttr = CurrentAttribute;
					Curses.attrset (Contents [Row, Col + 1, 1]);
					Curses.mvaddch (Row, Col + 1, Rune.ReplacementChar.Value);
					Contents [Row, Col + 1, 0] = Rune.ReplacementChar.Value;
					Curses.move (Row, Col);
					Curses.attrset (curAttr.Value);

				}
				if (runeWidth > 1 && Col == Clip.Right - 1) {
					// This is a double-width character, and we are at the end of the line.
					Curses.addch (Rune.ReplacementChar.Value);
					Contents [Row, Col, 0] = Rune.ReplacementChar.Value;
				} else {
					// This is a single-width character, or we are not at the end of the line.

					var curAttr = CurrentAttribute;
					Curses.attrset (Contents [Row, Col, 1]);

					if (rune.IsBmp) {
						Contents [Row, Col, 0] = rune.Value;
						Curses.addch (Contents [Row, Col, 0]);
					} else {
						var column = Col;
						ReadOnlySpan<char> remainingInput = rune.ToString ().AsSpan ();
						while (!remainingInput.IsEmpty) {
							// Decode
							OperationStatus opStatus = Rune.DecodeFromUtf16 (remainingInput, out Rune result, out int charsConsumed);

							if (opStatus != OperationStatus.Done) {
								result = Rune.ReplacementChar;
							}
							Contents [Row, column, 0] = result.Value;
							Contents [Row, column, 1] = CurrentAttribute.Value;

							Curses.attrset (Contents [Row, column, 1]);
							// BUGBUG: workaround curses not supporting non BMP? #
							Curses.mvaddch (Row, column, Rune.ReplacementChar.Value);
							//Curses.mvaddch (Row, column, Contents [Row, column, 0]);

							// Slice and loop again
							remainingInput = remainingInput.Slice (charsConsumed);
							column++;
						}
						Curses.move (Row, Col);
					}
					Curses.attrset (curAttr.Value);
				}
			}
		} else {
			_atValidLocation = false;
		}

		if (runeWidth is < 0 or > 0) {
			Col++;
		}

		if (runeWidth > 1) {
			// This is a double-width character, and we are not at the end of the line.
			if (validLocation && Col < Clip.Right) {
				Contents [Row, Col, 1] = CurrentAttribute.Value;
				Contents [Row, Col, 2] = 0;

				//if (rune.IsBmp) {
				//	// BUGBUG: workaround #2610
				//	Contents [Row, Col, 0] = (char)0x00;
				//	Curses.addch (Contents [Row, Col, 0]);
				//}
			}
			Col++;
		}
	}

	public override void Refresh ()
	{
		Curses.raw ();
		Curses.noecho ();
		Curses.refresh ();
		ProcessWinChange ();
	}

	private void ProcessWinChange ()
	{
		if (Curses.CheckWinChange ()) {
			ResizeScreen ();
			UpdateOffScreen ();
			TerminalResized?.Invoke ();
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
			value: Curses.ColorPair (v),
			foreground: CursesColorNumberToColor (foreground),
			background: CursesColorNumberToColor (background));
	}

	/// <remarks>
	/// In the CursesDriver, colors are encoded as an int. 
	/// The foreground color is stored in the most significant 4 bits, 
	/// and the background color is stored in the least significant 4 bits.
	/// The Terminal.GUi Color values are converted to curses color encoding before being encoded.
	/// </remarks>
	public override Attribute MakeColor (Color fore, Color back)
	{
		return MakeColor (ColorToCursesColorNumber (fore), ColorToCursesColorNumber (back));
	}

	static short ColorToCursesColorNumber (Color color)
	{
		switch (color) {
		case Color.Black:
			return Curses.COLOR_BLACK;
		case Color.Blue:
			return Curses.COLOR_BLUE;
		case Color.Green:
			return Curses.COLOR_GREEN;
		case Color.Cyan:
			return Curses.COLOR_CYAN;
		case Color.Red:
			return Curses.COLOR_RED;
		case Color.Magenta:
			return Curses.COLOR_MAGENTA;
		case Color.Brown:
			return Curses.COLOR_YELLOW;
		case Color.Gray:
			return Curses.COLOR_WHITE;
		case Color.DarkGray:
			return Curses.COLOR_GRAY;
		case Color.BrightBlue:
			return Curses.COLOR_BLUE | Curses.COLOR_GRAY;
		case Color.BrightGreen:
			return Curses.COLOR_GREEN | Curses.COLOR_GRAY;
		case Color.BrightCyan:
			return Curses.COLOR_CYAN | Curses.COLOR_GRAY;
		case Color.BrightRed:
			return Curses.COLOR_RED |  Curses.COLOR_GRAY;
		case Color.BrightMagenta:
			return Curses.COLOR_MAGENTA | Curses.COLOR_GRAY;
		case Color.BrightYellow:
			return Curses.COLOR_YELLOW | Curses.COLOR_GRAY;
		case Color.White:
			return Curses.COLOR_WHITE | Curses.COLOR_GRAY;
		}
		throw new ArgumentException ("Invalid color code");
	}

	static Color CursesColorNumberToColor (short color)
	{
		switch (color) {
		case Curses.COLOR_BLACK:
			return Color.Black;
		case Curses.COLOR_BLUE:
			return Color.Blue;
		case Curses.COLOR_GREEN:
			return Color.Green;
		case Curses.COLOR_CYAN:
			return Color.Cyan;
		case Curses.COLOR_RED:
			return Color.Red;
		case Curses.COLOR_MAGENTA:
			return Color.Magenta;
		case Curses.COLOR_YELLOW:
			return Color.Brown;
		case Curses.COLOR_WHITE:
			return Color.Gray;
		case Curses.COLOR_GRAY:
			return Color.DarkGray;
		case Curses.COLOR_BLUE | Curses.COLOR_GRAY:
			return Color.BrightBlue;
		case Curses.COLOR_GREEN | Curses.COLOR_GRAY:
			return Color.BrightGreen;
		case Curses.COLOR_CYAN | Curses.COLOR_GRAY:
			return Color.BrightCyan;
		case Curses.COLOR_RED | Curses.COLOR_GRAY:
			return Color.BrightRed;
		case Curses.COLOR_MAGENTA | Curses.COLOR_GRAY:
			return Color.BrightMagenta;
		case Curses.COLOR_YELLOW | Curses.COLOR_GRAY:
			return Color.BrightYellow;
		case Curses.COLOR_WHITE | Curses.COLOR_GRAY:
			return Color.White;
		}
		throw new ArgumentException ("Invalid curses color code");
	}

	/// <remarks>
	/// In the CursesDriver, colors are encoded as an int. 
	/// The foreground color is stored in the most significant 4 bits, 
	/// and the background color is stored in the least significant 4 bits.
	/// The Terminal.GUI Color values are converted to curses color encoding before being encoded.
	/// </remarks>
	internal override void GetColors (int value, out Color foreground, out Color background)
	{
		// Assume a 4-bit encoded value for both foreground and background colors.
		foreground = CursesColorNumberToColor ((short)((value >> 4) & 0xF));
		background = CursesColorNumberToColor ((short)(value & 0xF));
	}

	#endregion

	public override void UpdateCursor () => Refresh ();

	public override void End ()
	{
		StopReportingMouseMoves ();
		SetCursorVisibility (CursorVisibility.Default);

		Curses.endwin ();
	}

	public override void UpdateScreen () => _window.redrawwin ();

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

	void ProcessInput ()
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
			if (wch == Curses.KeyResize) {
				ProcessWinChange ();
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
			_keyDownHandler (new KeyEvent (k, MapKeyModifiers (k)));
			_keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
			_keyUpHandler (new KeyEvent (k, MapKeyModifiers (k)));
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
				_keyDownHandler (key);
				_keyHandler (key);
			} else {
				k = Key.Esc;
				_keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
			}
		} else if (wch == Curses.KeyTab) {
			k = MapCursesKey (wch);
			_keyDownHandler (new KeyEvent (k, MapKeyModifiers (k)));
			_keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
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
			_keyDownHandler (new KeyEvent (k, MapKeyModifiers (k)));
			_keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
			_keyUpHandler (new KeyEvent (k, MapKeyModifiers (k)));
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
				EscSeqUtils.DecodeEscSeq (null, ref consoleKeyInfo, ref ck, cki, ref mod, out _, out _, out _, out _, out bool isKeyMouse, out List<MouseFlags> mouseFlags, out Point pos, out _, ProcessContinuousButtonPressed);
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
					_keyDownHandler (key);
					_keyHandler (key);
				}
			} else {
				cki = EscSeqUtils.ResizeArray (consoleKeyInfo, cki);
			}
		}
	}

	void ProcessMouseEvent (MouseFlags mouseFlag, Point pos)
	{
		var me = new MouseEvent () {
			Flags = mouseFlag,
			X = pos.X,
			Y = pos.Y
		};
		_mouseHandler (me);
	}

	void ProcessContinuousButtonPressed (MouseFlags mouseFlag, Point pos)
	{
		ProcessMouseEvent (mouseFlag, pos);
	}

	Action<KeyEvent> _keyHandler;
	Action<KeyEvent> _keyDownHandler;
	Action<KeyEvent> _keyUpHandler;
	Action<MouseEvent> _mouseHandler;

	public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
	{
		// Note: Curses doesn't support keydown/up events and thus any passed keyDown/UpHandlers will never be called
		Curses.timeout (0);
		this._keyHandler = keyHandler;
		this._keyDownHandler = keyDownHandler;
		this._keyUpHandler = keyUpHandler;
		this._mouseHandler = mouseHandler;

		var mLoop = mainLoop.MainLoopDriver as UnixMainLoop;

		mLoop.AddWatch (0, UnixMainLoop.Condition.PollIn, x => {
			ProcessInput ();
			return true;
		});

		mLoop.WinChanged += () => {
			ProcessWinChange ();
		};
	}

	public override void Init (Action terminalResized)
	{
		if (_window != null) {
			return;
		}

		try {
			_window = Curses.initscr ();
			Curses.set_escdelay (10);
		} catch (Exception e) {
			throw new Exception ($"Curses failed to initialize, the exception is: {e.Message}");
		}

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

		if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
			Clipboard = new MacOSXClipboard ();
		} else {
			if (Is_WSL_Platform ()) {
				Clipboard = new WSLClipboard ();
			} else {
				Clipboard = new CursesClipboard ();
			}
		}

		Curses.raw ();
		Curses.noecho ();

		Curses.Window.Standard.keypad (true);
		TerminalResized = terminalResized;
		StartReportingMouseMoves ();

		CurrentAttribute = MakeColor (Color.White, Color.Black);

		if (Curses.HasColors) {
			Curses.StartColor ();
			Curses.UseDefaultColors ();

			InitializeColorSchemes ();
		} else {
			throw new InvalidOperationException ("V2 - This should never happen. File an Issue if it does.");
		}

		ResizeScreen ();
		UpdateOffScreen ();

	}

	public virtual void ResizeScreen ()
	{
		Clip = new Rect (0, 0, Cols, Rows);
		Curses.refresh ();
	}

	public override void UpdateOffScreen ()
	{
		Contents = new int [Rows, Cols, 3];
		for (int row = 0; row < Rows; row++) {
			for (int col = 0; col < Cols; col++) {
				Contents [row, col, 0] = ' ';
				Contents [row, col, 1] = Colors.TopLevel.Normal.Value;
				Contents [row, col, 2] = 0;
			}
		}
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
		Platform.Suspend ();
		Curses.Window.Standard.redrawwin ();
		Curses.refresh ();
		StartReportingMouseMoves ();
	}

	public void StartReportingMouseMoves ()
	{
		Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
	}

	public void StopReportingMouseMoves ()
	{
		Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);
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

		Curses.curs_set (((int)visibility >> 16) & 0x000000FF);

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
		_keyDownHandler (new KeyEvent (key, km));
		_keyHandler (new KeyEvent (key, km));
		_keyUpHandler (new KeyEvent (key, km));
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