//
// Driver.cs: Curses-based Driver
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NStack;
using Unix.Terminal;

namespace Terminal.Gui {

	/// <summary>
	/// This is the Curses driver for the gui.cs/Terminal framework.
	/// </summary>
	internal class CursesDriver : ConsoleDriver {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public override int Cols => Curses.Cols;
		public override int Rows => Curses.Lines;

		// Current row, and current col, tracked by Move/AddRune only
		int ccol, crow;
		bool needMove;
		public override void Move (int col, int row)
		{
			ccol = col;
			crow = row;

			if (Clip.Contains (col, row)) {
				Curses.move (row, col);
				needMove = false;
			} else {
				Curses.move (Clip.Y, Clip.X);
				needMove = true;
			}
		}

		static bool sync = false;
		public override void AddRune (Rune rune) 
		{
			if (Clip.Contains (ccol, crow)) {
				if (needMove) {
					Curses.move (crow, ccol);
					needMove = false;
				}
				Curses.addch ((int)(uint)MakePrintable(rune));
			} else
				needMove = true;
			if (sync)
				Application.Driver.Refresh ();
			ccol++;
			var runeWidth = Rune.ColumnWidth (rune);
			if (runeWidth > 1) {
				for (int i = 1; i < runeWidth; i++) {
					ccol++;
				}
			}
		}

		public override void AddStr (ustring str)
		{
			// TODO; optimize this to determine if the str fits in the clip region, and if so, use Curses.addstr directly
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void Refresh () {
			Curses.refresh ();
			if (Curses.CheckWinChange ()) {
				TerminalResized?.Invoke ();
			}
		}
		public override void UpdateCursor () => Refresh ();
		public override void End () => Curses.endwin ();
		public override void UpdateScreen () => window.redrawwin ();
		public override void SetAttribute (Attribute c) => Curses.attrset (c.value);
		public Curses.Window window;

		static short last_color_pair = 16;

		/// <summary>
		/// Creates a curses color from the provided foreground and background colors
		/// </summary>
		/// <param name="foreground">Contains the curses attributes for the foreground (color, plus any attributes)</param>
		/// <param name="background">Contains the curses attributes for the background (color, plus any attributes)</param>
		/// <returns></returns>
		public static Attribute MakeColor (short foreground, short background)
		{
			Curses.InitColorPair (++last_color_pair, foreground, background);
			return new Attribute () { value = Curses.ColorPair (last_color_pair) };
		}

		int [,] colorPairs = new int [16, 16];

		public override void SetColors (ConsoleColor foreground, ConsoleColor background)
		{
			int f = (short)foreground;
			int b = (short)background;
			var v = colorPairs [f, b];
			if ((v & 0x10000) == 0) {
				b = b & 0x7;
				bool bold = (f & 0x8) != 0;
				f = f & 0x7;

				v = MakeColor ((short)f, (short)b) | (bold ? Curses.A_BOLD : 0);
				colorPairs [(int)foreground, (int)background] = v | 0x1000;
			}
			SetAttribute (v & 0xffff);
		}

		Dictionary<int, int> rawPairs = new Dictionary<int, int> ();
		public override void SetColors (short foreColorId, short backgroundColorId)
		{
			int key = (((ushort)foreColorId << 16)) | (ushort)backgroundColorId;
			if (!rawPairs.TryGetValue (key, out var v)) {
				v = MakeColor (foreColorId, backgroundColorId);
				rawPairs [key] = v;
			}
			SetAttribute (v);
		}

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
			default: return Key.Unknown;
			}
		}

		Curses.Event? LastMouseButtonPressed;
		bool IsButtonPressed;
		bool cancelButtonClicked;
		bool canWheeledDown;
		bool isReportMousePosition;
		Point point;

		MouseEvent ToDriverMouse (Curses.MouseEvent cev)
		{
			MouseFlags mouseFlag = MouseFlags.AllEvents;

			if (LastMouseButtonPressed != null && cev.ButtonState != Curses.Event.ReportMousePosition) {
				LastMouseButtonPressed = null;
				IsButtonPressed = false;
			}


			if ((cev.ButtonState == Curses.Event.Button1Clicked || cev.ButtonState == Curses.Event.Button2Clicked ||
				cev.ButtonState == Curses.Event.Button3Clicked) &&
				LastMouseButtonPressed == null) {

				IsButtonPressed = false;
				mouseFlag = ProcessButtonClickedEvent (cev);

			} else if (((cev.ButtonState == Curses.Event.Button1Pressed || cev.ButtonState == Curses.Event.Button2Pressed ||
				cev.ButtonState == Curses.Event.Button3Pressed) && LastMouseButtonPressed == null) ||
				IsButtonPressed && cev.ButtonState == Curses.Event.ReportMousePosition) {

				mouseFlag = (MouseFlags)cev.ButtonState;
				if (cev.ButtonState != Curses.Event.ReportMousePosition)
					LastMouseButtonPressed = cev.ButtonState;
				IsButtonPressed = true;
				isReportMousePosition = false;

				if (cev.ButtonState == Curses.Event.ReportMousePosition) {
					mouseFlag = (MouseFlags)LastMouseButtonPressed | MouseFlags.ReportMousePosition;
					point = new Point ();
					//cancelButtonClicked = true;
				} else {
					point = new Point () {
						X = cev.X,
						Y = cev.Y
					};
				}

				if ((mouseFlag & MouseFlags.ReportMousePosition) == 0) {
					Task.Run (async () => {
						while (IsButtonPressed && LastMouseButtonPressed != null) {
							await Task.Delay (100);
							var me = new MouseEvent () {
								X = cev.X,
								Y = cev.Y,
								Flags = mouseFlag
							};

							var view = Application.wantContinuousButtonPressedView;
							if (view == null)
								break;
							if (IsButtonPressed && LastMouseButtonPressed != null && (mouseFlag & MouseFlags.ReportMousePosition) == 0) {
								mouseHandler (me);
								//mainLoop.Driver.Wakeup ();
							}
						}
					});
				}


			} else if ((cev.ButtonState == Curses.Event.Button1Released || cev.ButtonState == Curses.Event.Button2Released ||
				cev.ButtonState == Curses.Event.Button3Released)) {

				mouseFlag = ProcessButtonReleasedEvent (cev);
				IsButtonPressed = false;
				canWheeledDown = false;

			} else if (cev.ButtonState == Curses.Event.Button4Pressed) {

				mouseFlag = MouseFlags.WheeledUp;

			} else if (cev.ButtonState == Curses.Event.ReportMousePosition && cev.X == point.X && cev.Y == point.Y &&
				canWheeledDown) {

				mouseFlag = MouseFlags.WheeledDown;
				canWheeledDown = true;

			}
			else if (cev.ButtonState == Curses.Event.ReportMousePosition && !canWheeledDown) {

				mouseFlag = MouseFlags.ReportMousePosition;
				canWheeledDown = true;
				isReportMousePosition = true;

			} else {
				mouseFlag = (MouseFlags)cev.ButtonState;
				canWheeledDown = false;
				if (cev.ButtonState == Curses.Event.ReportMousePosition)
					isReportMousePosition = true;
			}

			point = new Point () {
				X = cev.X,
				Y = cev.Y
			};

			return new MouseEvent () {
				X = cev.X,
				Y = cev.Y,
				//Flags = (MouseFlags)cev.ButtonState
				Flags = mouseFlag
			};
		}

		private MouseFlags ProcessButtonClickedEvent (Curses.MouseEvent cev)
		{
			LastMouseButtonPressed = cev.ButtonState;
			var mf = GetButtonState (cev, true);
			mouseHandler (ProcessButtonState (cev, mf));
			if (LastMouseButtonPressed != null && LastMouseButtonPressed == cev.ButtonState) {
				mf = GetButtonState (cev, false);
				mouseHandler (ProcessButtonState (cev, mf));
				if (LastMouseButtonPressed != null && LastMouseButtonPressed == cev.ButtonState) {
					mf = (MouseFlags)cev.ButtonState;
				}
			}
			LastMouseButtonPressed = null;
			canWheeledDown = false;
			return mf;
		}

		private MouseFlags ProcessButtonReleasedEvent (Curses.MouseEvent cev)
		{
			var mf = (MouseFlags)cev.ButtonState;
			if (!cancelButtonClicked && LastMouseButtonPressed == null && !isReportMousePosition) {
				mouseHandler (ProcessButtonState (cev, mf));
				mf = GetButtonState (cev);
			} else if (isReportMousePosition) {
				mf = MouseFlags.ReportMousePosition;
			}
			cancelButtonClicked = false;
			canWheeledDown = false;
			return mf;
		}

		MouseFlags GetButtonState (Curses.MouseEvent cev, bool pressed = false)
		{
			MouseFlags mf = default;
			switch (cev.ButtonState) {
			case Curses.Event.Button1Clicked:
				if (pressed)
					mf = MouseFlags.Button1Pressed;
				else
					mf = MouseFlags.Button1Released;
				break;

			case Curses.Event.Button2Clicked:
				if (pressed)
					mf = MouseFlags.Button2Pressed;
				else
					mf = MouseFlags.Button2Released;
				break;

			case Curses.Event.Button3Clicked:
				if (pressed)
					mf = MouseFlags.Button3Pressed;
				else
					mf = MouseFlags.Button3Released;
				break;

			case Curses.Event.Button1Released:
				mf = MouseFlags.Button1Clicked;
				break;

			case Curses.Event.Button2Released:
				mf = MouseFlags.Button2Clicked;
				break;

			case Curses.Event.Button3Released:
				mf = MouseFlags.Button3Clicked;
				break;


			}
			return mf;
		}

		MouseEvent ProcessButtonState (Curses.MouseEvent cev, MouseFlags mf)
		{
			return new MouseEvent () {
				X = cev.X,
				Y = cev.Y,
				Flags = mf
			};
		}

		KeyModifiers keyModifiers;

		KeyModifiers MapKeyModifiers (Key key)
		{
			if (keyModifiers == null)
				keyModifiers = new KeyModifiers ();

			if (!keyModifiers.Shift && key.HasFlag (Key.ShiftMask))
				keyModifiers.Shift = true;
			if (!keyModifiers.Alt && key.HasFlag (Key.AltMask))
				keyModifiers.Alt = true;
			if (!keyModifiers.Ctrl && key.HasFlag (Key.CtrlMask))
				keyModifiers.Ctrl = true;

			return keyModifiers;
		}

		void ProcessInput (Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
		{
			int wch;
			var code = Curses.get_wch (out wch);
			if (code == Curses.ERR)
				return;

			keyModifiers = new KeyModifiers ();
			Key k;

			if (code == Curses.KEY_CODE_YES) {
				if (wch == Curses.KeyResize) {
					if (Curses.CheckWinChange ()) {
						TerminalResized?.Invoke ();
						return;
					}
				}
				if (wch == Curses.KeyMouse) {
					Curses.MouseEvent ev;
					Curses.getmouse (out ev);
					mouseHandler (ToDriverMouse (ev));
					return;
				}
				keyHandler (new KeyEvent (MapCursesKey (wch), keyModifiers));
				keyUpHandler (new KeyEvent (MapCursesKey (wch), keyModifiers));
				return;
			}

			// Special handling for ESC, we want to try to catch ESC+letter to simulate alt-letter as well as Alt-Fkey
			if (wch == 27) {
				Curses.timeout (200);

				code = Curses.get_wch (out int wch2);

				if (code == Curses.KEY_CODE_YES) {
					k = Key.AltMask | MapCursesKey (wch);
					keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
				}
				if (code == 0) {
					KeyEvent key;

					// The ESC-number handling, debatable.
					// Simulates the AltMask itself by pressing Alt + Space.
					if (wch2 == (int)Key.Space) {
						k = Key.AltMask;
						key = new KeyEvent (k, MapKeyModifiers (k));
					} else if (wch2 - (int)Key.Space >= 'A' && wch2 - (int)Key.Space <= 'Z') {
						k = (Key)((uint)Key.AltMask + (wch2 - (int)Key.Space));
						key = new KeyEvent (k, MapKeyModifiers (k));
					} else if (wch2 >= '1' && wch <= '9') {
						k = (Key)((int)Key.F1 + (wch2 - '0' - 1));
						key = new KeyEvent (k, MapKeyModifiers (k));
					} else if (wch2 == '0') {
						k = Key.F10;
						key = new KeyEvent (k, MapKeyModifiers (k));
					} else if (wch2 == 27) {
						k = (Key)wch2;
						key = new KeyEvent (k, MapKeyModifiers (k));
					} else {
						k = Key.AltMask | (Key)wch2;
						key = new KeyEvent (k, MapKeyModifiers (k));
					}
					keyHandler (key);
				} else {
					k = Key.Esc;
					keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
				}
			} else if (wch == Curses.KeyTab) {
				k = MapCursesKey (wch);
				keyDownHandler (new KeyEvent (k, MapKeyModifiers (k)));
				keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
			} else {
				k = (Key)wch;
				keyDownHandler (new KeyEvent (k, MapKeyModifiers (k)));
				keyHandler (new KeyEvent (k, MapKeyModifiers (k)));
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

		Action<MouseEvent> mouseHandler;
		MainLoop mainLoop;

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
		{
			// Note: Curses doesn't support keydown/up events and thus any passed keyDown/UpHandlers will never be called
			Curses.timeout (0);
			this.mouseHandler = mouseHandler;
			this.mainLoop = mainLoop;

			(mainLoop.Driver as UnixMainLoop).AddWatch (0, UnixMainLoop.Condition.PollIn, x => {
				ProcessInput (keyHandler, keyDownHandler, keyUpHandler, mouseHandler);
				return true;
			});

		}

		Curses.Event oldMouseEvents, reportableMouseEvents;
		public override void Init (Action terminalResized)
		{
			if (window != null)
				return;

			try {
				window = Curses.initscr ();
			} catch (Exception e) {
				Console.WriteLine ("Curses failed to initialize, the exception is: " + e);
			}
			Curses.raw ();
			Curses.noecho ();

			Curses.Window.Standard.keypad (true);
			reportableMouseEvents = Curses.mousemask (Curses.Event.AllEvents | Curses.Event.ReportMousePosition, out oldMouseEvents);
			TerminalResized = terminalResized;
			if (reportableMouseEvents.HasFlag (Curses.Event.ReportMousePosition))
				StartReportingMouseMoves ();

			HLine = Curses.ACS_HLINE;
			VLine = Curses.ACS_VLINE;
			Stipple = Curses.ACS_CKBOARD;
			Diamond = Curses.ACS_DIAMOND;
			ULCorner = Curses.ACS_ULCORNER;
			LLCorner = Curses.ACS_LLCORNER;
			URCorner = Curses.ACS_URCORNER;
			LRCorner = Curses.ACS_LRCORNER;
			LeftTee = Curses.ACS_LTEE;
			RightTee = Curses.ACS_RTEE;
			TopTee = Curses.ACS_TTEE;
			BottomTee = Curses.ACS_BTEE;
			Checked = '\u221a';
			UnChecked = ' ';
			Selected = '\u25cf';
			UnSelected = '\u25cc';
			RightArrow = Curses.ACS_RARROW;
			LeftArrow = Curses.ACS_LARROW;
			UpArrow = Curses.ACS_UARROW;
			DownArrow = Curses.ACS_DARROW;
			LeftDefaultIndicator = '\u25e6';
			RightDefaultIndicator = '\u25e6';
			LeftBracket = '[';
			RightBracket = ']';
			OnMeterSegment = '\u258c';
			OffMeterSegement = ' ';

			Colors.TopLevel = new ColorScheme ();
			Colors.Base = new ColorScheme ();
			Colors.Dialog = new ColorScheme ();
			Colors.Menu = new ColorScheme ();
			Colors.Error = new ColorScheme ();
			Clip = new Rect (0, 0, Cols, Rows);
			if (Curses.HasColors) {
				Curses.StartColor ();
				Curses.UseDefaultColors ();

				Colors.TopLevel.Normal = MakeColor (Curses.COLOR_GREEN, Curses.COLOR_BLACK);
				Colors.TopLevel.Focus = MakeColor (Curses.COLOR_WHITE, Curses.COLOR_CYAN);
				Colors.TopLevel.HotNormal = MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLACK);
				Colors.TopLevel.HotFocus = MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);

				Colors.Base.Normal = MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLUE);
				Colors.Base.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
				Colors.Base.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLUE);
				Colors.Base.HotFocus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);

				// Focused,
				//    Selected, Hot: Yellow on Black
				//    Selected, text: white on black
				//    Unselected, hot: yellow on cyan
				//    unselected, text: same as unfocused
				Colors.Menu.HotFocus = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_BLACK);
				Colors.Menu.Focus = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_BLACK);
				Colors.Menu.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_CYAN);
				Colors.Menu.Normal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_CYAN);
				Colors.Menu.Disabled = MakeColor (Curses.COLOR_WHITE, Curses.COLOR_CYAN);

				Colors.Dialog.Normal = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
				Colors.Dialog.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
				Colors.Dialog.HotNormal = MakeColor (Curses.COLOR_BLUE, Curses.COLOR_WHITE);
				Colors.Dialog.HotFocus = MakeColor (Curses.COLOR_BLUE, Curses.COLOR_CYAN);

				Colors.Error.Normal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_RED);
				Colors.Error.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
				Colors.Error.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_RED);
				Colors.Error.HotFocus = Colors.Error.HotNormal;
			} else {
				Colors.TopLevel.Normal = Curses.COLOR_GREEN;
				Colors.TopLevel.Focus = Curses.COLOR_WHITE;
				Colors.TopLevel.HotNormal = Curses.COLOR_YELLOW;
				Colors.TopLevel.HotFocus = Curses.COLOR_YELLOW;
				Colors.Base.Normal = Curses.A_NORMAL;
				Colors.Base.Focus = Curses.A_REVERSE;
				Colors.Base.HotNormal = Curses.A_BOLD;
				Colors.Base.HotFocus = Curses.A_BOLD | Curses.A_REVERSE;
				Colors.Menu.Normal = Curses.A_REVERSE;
				Colors.Menu.Focus = Curses.A_NORMAL;
				Colors.Menu.HotNormal = Curses.A_BOLD;
				Colors.Menu.HotFocus = Curses.A_NORMAL;
				Colors.Dialog.Normal = Curses.A_REVERSE;
				Colors.Dialog.Focus = Curses.A_NORMAL;
				Colors.Dialog.HotNormal = Curses.A_BOLD;
				Colors.Dialog.HotFocus = Curses.A_NORMAL;
				Colors.Error.Normal = Curses.A_BOLD;
				Colors.Error.Focus = Curses.A_BOLD | Curses.A_REVERSE;
				Colors.Error.HotNormal = Curses.A_BOLD | Curses.A_REVERSE;
				Colors.Error.HotFocus = Curses.A_REVERSE;
			}
		}

		static int MapColor (Color color)
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
				return Curses.COLOR_BLACK | Curses.A_BOLD;
			case Color.BrightBlue:
				return Curses.COLOR_BLUE | Curses.A_BOLD;
			case Color.BrightGreen:
				return Curses.COLOR_GREEN | Curses.A_BOLD;
			case Color.BrighCyan:
				return Curses.COLOR_CYAN | Curses.A_BOLD;
			case Color.BrightRed:
				return Curses.COLOR_RED | Curses.A_BOLD;
			case Color.BrightMagenta:
				return Curses.COLOR_MAGENTA | Curses.A_BOLD;
			case Color.BrightYellow:
				return Curses.COLOR_YELLOW | Curses.A_BOLD;
			case Color.White:
				return Curses.COLOR_WHITE | Curses.A_BOLD;
			}
			throw new ArgumentException ("Invalid color code");
		}

		public override Attribute MakeAttribute (Color fore, Color back)
		{
			var f = MapColor (fore);
			return MakeColor ((short)(f & 0xffff), (short)MapColor (back)) | ((f & Curses.A_BOLD) != 0 ? Curses.A_BOLD : 0);
		}

		public override void Suspend ()
		{
			if (reportableMouseEvents.HasFlag (Curses.Event.ReportMousePosition))
				StopReportingMouseMoves ();
			Platform.Suspend ();
			Curses.Window.Standard.redrawwin ();
			Curses.refresh ();
			if (reportableMouseEvents.HasFlag (Curses.Event.ReportMousePosition))
				StartReportingMouseMoves ();
		}

		public override void StartReportingMouseMoves ()
		{
			Console.Out.Write ("\x1b[?1003h");
			Console.Out.Flush ();
		}

		public override void StopReportingMouseMoves ()
		{
			Console.Out.Write ("\x1b[?1003l");
			Console.Out.Flush ();
		}

		//int lastMouseInterval;
		//bool mouseGrabbed;

		public override void UncookMouse ()
		{
			//if (mouseGrabbed)
			//	return;
			//lastMouseInterval = Curses.mouseinterval (0);
			//mouseGrabbed = true;
		}

		public override void CookMouse ()
		{
			//mouseGrabbed = false;
			//Curses.mouseinterval (lastMouseInterval);
		}
	}

	internal static class Platform {
		[DllImport ("libc")]
		static extern int uname (IntPtr buf);

		[DllImport ("libc")]
		static extern int killpg (int pgrp, int pid);

		static int suspendSignal;

		static int GetSuspendSignal ()
		{
			if (suspendSignal != 0)
				return suspendSignal;

			IntPtr buf = Marshal.AllocHGlobal (8192);
			if (uname (buf) != 0) {
				Marshal.FreeHGlobal (buf);
				suspendSignal = -1;
				return suspendSignal;
			}
			try {
				switch (Marshal.PtrToStringAnsi (buf)) {
				case "Darwin":
				case "DragonFly":
				case "FreeBSD":
				case "NetBSD":
				case "OpenBSD":
					suspendSignal = 18;
					break;
				case "Linux":
					// TODO: should fetch the machine name and
					// if it is MIPS return 24
					suspendSignal = 20;
					break;
				case "Solaris":
					suspendSignal = 24;
					break;
				default:
					suspendSignal = -1;
					break;
				}
				return suspendSignal;
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
			if (signal == -1)
				return false;
			killpg (0, signal);
			return true;
		}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

}
