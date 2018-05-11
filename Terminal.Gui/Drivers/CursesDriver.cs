//
// Driver.cs: Curses-based Driver
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mono.Terminal;
using NStack;
using Unix.Terminal;

namespace Terminal.Gui {

	/// <summary>
	/// This is the Curses driver for the gui.cs/Terminal framework.
	/// </summary>
	internal class CursesDriver : ConsoleDriver {
		Action terminalResized;

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

		static bool sync;
		public override void AddRune (Rune rune)
		{
			if (Clip.Contains (ccol, crow)) {
				if (needMove) {
					Curses.move (crow, ccol);
					needMove = false;
				}
				Curses.addch ((int)(uint)rune);
			} else
				needMove = true;
			if (sync)
				Application.Driver.Refresh ();
			ccol++;
		}

		public override void AddStr (ustring str)
		{
			// TODO; optimize this to determine if the str fits in the clip region, and if so, use Curses.addstr directly
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void Refresh () => Curses.refresh ();
		public override void UpdateCursor () => Curses.refresh ();
		public override void End () => Curses.endwin ();
		public override void UpdateScreen () => window.redrawwin ();
		public override void SetAttribute (Attribute c) => Curses.attrset (c.value);
		public Curses.Window window;

		static short last_color_pair = 16;
		static Attribute MakeColor (short f, short b)
		{
			Curses.InitColorPair (++last_color_pair, f, b);
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
			case Curses.KeyBackTab: return Key.BackTab;
			case Curses.KeyBackspace: return Key.Backspace;
			default: return Key.Unknown;
			}
		}

		static MouseEvent ToDriverMouse (Curses.MouseEvent cev)
		{
			return new MouseEvent () {
				X = cev.X,
				Y = cev.Y,
				Flags = (MouseFlags)cev.ButtonState
			};
		}

		void ProcessInput (Action<KeyEvent> keyHandler, Action<MouseEvent> mouseHandler)
		{
			int wch;
			var code = Curses.get_wch (out wch);
			if (code == Curses.ERR)
				return;
			if (code == Curses.KEY_CODE_YES) {
				if (wch == Curses.KeyResize) {
					if (Curses.CheckWinChange ()) {
						terminalResized ();
						return;
					}
				}
				if (wch == Curses.KeyMouse) {
					Curses.MouseEvent ev;
					Curses.getmouse (out ev);
					mouseHandler (ToDriverMouse (ev));
					return;
				}
				keyHandler (new KeyEvent (MapCursesKey (wch)));
				return;
			}

			// Special handling for ESC, we want to try to catch ESC+letter to simulate alt-letter as well as Alt-Fkey
			if (wch == 27) {
				Curses.timeout (200);

				code = Curses.get_wch (out wch);
				if (code == Curses.KEY_CODE_YES)
					keyHandler (new KeyEvent (Key.AltMask | MapCursesKey (wch)));
				if (code == 0) {
					KeyEvent key;

					// The ESC-number handling, debatable.
					if (wch >= '1' && wch <= '9')
						key = new KeyEvent ((Key)((int)Key.F1 + (wch - '0' - 1)));
					else if (wch == '0')
						key = new KeyEvent (Key.F10);
					else if (wch == 27)
						key = new KeyEvent ((Key)wch);
					else
						key = new KeyEvent (Key.AltMask | (Key)wch);
					keyHandler (key);
				} else
					keyHandler (new KeyEvent (Key.Esc));
			} else
				keyHandler (new KeyEvent ((Key)wch));
		}

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<MouseEvent> mouseHandler)
		{
			Curses.timeout (-1);

			mainLoop.AddWatch (0, Mono.Terminal.MainLoop.Condition.PollIn, x => {
				ProcessInput (keyHandler, mouseHandler);
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
			this.terminalResized = terminalResized;
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

			Colors.Base = new ColorScheme ();
			Colors.Dialog = new ColorScheme ();
			Colors.Menu = new ColorScheme ();
			Colors.Error = new ColorScheme ();
			Clip = new Rect (0, 0, Cols, Rows);
			if (Curses.HasColors) {
				Curses.StartColor ();
				Curses.UseDefaultColors ();

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

				Colors.Dialog.Normal = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
				Colors.Dialog.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_CYAN);
				Colors.Dialog.HotNormal = MakeColor (Curses.COLOR_BLUE, Curses.COLOR_WHITE);
				Colors.Dialog.HotFocus = MakeColor (Curses.COLOR_BLUE, Curses.COLOR_CYAN);

				Colors.Error.Normal = Curses.A_BOLD | MakeColor (Curses.COLOR_WHITE, Curses.COLOR_RED);
				Colors.Error.Focus = MakeColor (Curses.COLOR_BLACK, Curses.COLOR_WHITE);
				Colors.Error.HotNormal = Curses.A_BOLD | MakeColor (Curses.COLOR_YELLOW, Curses.COLOR_RED);
				Colors.Error.HotFocus = Colors.Error.HotNormal;
			} else {
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

		public override void Suspend ()
		{
			StopReportingMouseMoves ();
			Platform.Suspend ();
			Curses.Window.Standard.redrawwin ();
			Curses.refresh ();
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

		int lastMouseInterval;
		bool mouseGrabbed;

		public override void UncookMouse ()
		{
			if (mouseGrabbed)
				return;
			lastMouseInterval = Curses.mouseinterval (0);
			mouseGrabbed = true;
		}

		public override void CookMouse ()
		{
			mouseGrabbed = false;
			Curses.mouseinterval (lastMouseInterval);
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
	}

}
