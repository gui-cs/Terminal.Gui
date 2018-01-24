//
// Driver.cs: Abstract and concrete interfaces to the console (curses or console)
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
	/// Basic colors that can be used to set the foreground and background colors in console applications.  These can only be
	/// </summary>
	public enum Color {
		/// <summary>
		/// The black color.
		/// </summary>
		Black,
		/// <summary>
		/// The blue color.
		/// </summary>
		Blue,
		/// <summary>
		/// The green color.
		/// </summary>
		Green,
		/// <summary>
		/// The cyan color.
		/// </summary>
		Cyan,
		/// <summary>
		/// The red color.
		/// </summary>
		Red,
		/// <summary>
		/// The magenta color.
		/// </summary>
		Magenta,
		/// <summary>
		/// The brown color.
		/// </summary>
		Brown,
		/// <summary>
		/// The gray color.
		/// </summary>
		Gray,
		/// <summary>
		/// The dark gray color.
		/// </summary>
		DarkGray,
		/// <summary>
		/// The bright bBlue color.
		/// </summary>
		BrightBlue,
		/// <summary>
		/// The bright green color.
		/// </summary>
		BrightGreen,
		/// <summary>
		/// The brigh cyan color.
		/// </summary>
		BrighCyan,
		/// <summary>
		/// The bright red color.
		/// </summary>
		BrightRed,
		/// <summary>
		/// The bright magenta color.
		/// </summary>
		BrightMagenta,
		/// <summary>
		/// The bright yellow color.
		/// </summary>
		BrightYellow,
		/// <summary>
		/// The White color.
		/// </summary>
		White
	}

	/// <summary>
	/// Attributes are used as elements that contain both a foreground and a background or platform specific features
	/// </summary>
	/// <remarks>
	///   Attributes are needed to map colors to terminal capabilities that might lack colors, on color
	///   scenarios, they encode both the foreground and the background color and are used in the ColorScheme
	///   class to define color schemes that can be used in your application.
	/// </remarks>
	public struct Attribute {
		internal int value;
		public Attribute (int v)
		{
			value = v;
		}

		public static implicit operator int (Attribute c) => c.value;
		public static implicit operator Attribute (int v) => new Attribute (v);
	}

	/// <summary>
	/// Color scheme definitions, they cover some common scenarios and are used
	/// typically in toplevel containers to set the scheme that is used by all the
	/// views contained inside.
	/// </summary>
	public class ColorScheme {
		/// <summary>
		/// The default color for text, when the view is not focused.
		/// </summary>
		public Attribute Normal;
		/// <summary>
		/// The color for text when the view has the focus.
		/// </summary>
		public Attribute Focus;

		/// <summary>
		/// The color for the hotkey when a view is not focused
		/// </summary>
		public Attribute HotNormal;

		/// <summary>
		/// The color for the hotkey when the view is focused.
		/// </summary>
		public Attribute HotFocus;
	}

	/// <summary>
	/// The default ColorSchemes for the application.
	/// </summary>
	public static class Colors {
		/// <summary>
		/// The base color scheme, for the default toplevel views.
		/// </summary>
		public static ColorScheme Base;
		/// <summary>
		/// The dialog color scheme, for standard popup dialog boxes
		/// </summary>
		public static ColorScheme Dialog;

		/// <summary>
		/// The menu bar color
		/// </summary>
		public static ColorScheme Menu;

		/// <summary>
		/// The color scheme for showing errors.
		/// </summary>
		public static ColorScheme Error;

	}

	/// <summary>
	/// Special characters that can be drawn with Driver.AddSpecial.
	/// </summary>
	public enum SpecialChar {
		/// <summary>
		/// Horizontal line character.
		/// </summary>
		HLine,
	}

	/// <summary>
	/// ConsoleDriver is an abstract class that defines the requirements for a console driver.   One implementation if the CursesDriver, and another one uses the .NET Console one.
	/// </summary>
	public abstract class ConsoleDriver {
		/// <summary>
		/// The current number of columns in the terminal.
		/// </summary>
		public abstract int Cols { get; }
		/// <summary>
		/// The current number of rows in the terminal.
		/// </summary>
		public abstract int Rows { get; }
		/// <summary>
		/// Initializes the driver
		/// </summary>
		/// <param name="terminalResized">Method to invoke when the terminal is resized.</param>
		public abstract void Init (Action terminalResized);
		/// <summary>
		/// Moves the cursor to the specified column and row.
		/// </summary>
		/// <param name="col">Column to move the cursor to.</param>
		/// <param name="row">Row to move the cursor to.</param>
		public abstract void Move (int col, int row);
		/// <summary>
		/// Adds the specified rune to the display at the current cursor position
		/// </summary>
		/// <param name="rune">Rune to add.</param>
		public abstract void AddRune (Rune rune);
		/// <summary>
		/// Adds the specified 
		/// </summary>
		/// <param name="str">String.</param>
		public abstract void AddStr (ustring str);
		public abstract void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> target, Action<MouseEvent> mouse);

		/// <summary>
		/// Updates the screen to reflect all the changes that have been done to the display buffer
		/// </summary>
		public abstract void Refresh ();

		/// <summary>
		/// Ends the execution of the console driver.
		/// </summary>
		public abstract void End ();
		public abstract void RedrawTop ();
		public abstract void SetAttribute (Attribute c);

		// Set Colors from limit sets of colors
		public abstract void SetColors (ConsoleColor foreground, ConsoleColor background);

		// Advanced uses - set colors to any pre-set pairs, you would need to init_color
		// that independently with the R, G, B values.
		/// <summary>
		/// Advanced uses - set colors to any pre-set pairs, you would need to init_color 
		/// that independently with the R, G, B values.
		/// </summary>
		/// <param name="foregroundColorId">Foreground color identifier.</param>
		/// <param name="backgroundColorId">Background color identifier.</param>
		public abstract void SetColors (short foregroundColorId, short backgroundColorId);

		public abstract void DrawFrame (Rect region, int padding, bool fill);
		/// <summary>
		/// Draws a special characters in the screen
		/// </summary>
		/// <param name="ch">Ch.</param>
		public abstract void AddSpecial (SpecialChar ch);

		/// <summary>
		/// Suspend the application, typically needs to save the state, suspend the app and upon return, reset the console driver.
		/// </summary>
		public abstract void Suspend ();

		Rect clip;

		/// <summary>
		/// Controls the current clipping region that AddRune/AddStr is subject to.
		/// </summary>
		/// <value>The clip.</value>
		public Rect Clip {
			get => clip;
			set => this.clip = value;
		}

		public abstract void StartReportingMouseMoves ();
		public abstract void StopReportingMouseMoves ();
	}

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

		static bool sync = false;
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

		public override void AddSpecial (SpecialChar ch)
		{
			switch (ch) {
			case SpecialChar.HLine:
				AddRune (Curses.ACS_HLINE);
				break;
			}
		}

		public override void AddStr (ustring str)
		{
			// TODO; optimize this to determine if the str fits in the clip region, and if so, use Curses.addstr directly
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void Refresh () => Curses.refresh ();
		public override void End () => Curses.endwin ();
		public override void RedrawTop () => window.redrawwin ();
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
				Curses.timeout (100);

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

		public override void DrawFrame (Rect region, int padding, bool fill)
		{
			int width = region.Width;
			int height = region.Height;
			int b;
			int fwidth = width - padding * 2;
			int fheight = height - 1 - padding;

			Move (region.X, region.Y);
			if (padding > 0) {
				for (int l = 0; l < padding; l++)
					for (b = 0; b < width; b++)
						AddRune (' ');
			}
			Move (region.X, region.Y + padding);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
			AddRune (Curses.ACS_ULCORNER);
			for (b = 0; b < fwidth - 2; b++)
				AddRune (Curses.ACS_HLINE);
			AddRune (Curses.ACS_URCORNER);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
				
			for (b = 1+padding; b < fheight; b++) {
				Move (region.X, region.Y + b);
				for (int c = 0; c < padding; c++)
					AddRune (' ');
				AddRune (Curses.ACS_VLINE);
				if (fill) {	
					for (int x = 1; x < fwidth - 1; x++)
						AddRune (' ');
				} else
					Move (region.X + fwidth - 1, region.Y + b);
				AddRune (Curses.ACS_VLINE);
				for (int c = 0; c < padding; c++)
					AddRune (' ');
			}
			Move (region.X, region.Y + fheight);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
			AddRune (Curses.ACS_LLCORNER);
			for (b = 0; b < fwidth - 2; b++)
				AddRune (Curses.ACS_HLINE);
			AddRune (Curses.ACS_LRCORNER);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
			if (padding > 0) {
				Move (region.X, region.Y + height - padding);
				for (int l = 0; l < padding; l++)
					for (b = 0; b < width; b++)
						AddRune (' ');
			}
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

		public override void StartReportingMouseMoves()
		{
			Console.Out.Write ("\x1b[?1003h");
			Console.Out.Flush ();
		}

		public override void StopReportingMouseMoves()
		{
			Console.Out.Write ("\x1b[?1003l");
			Console.Out.Flush ();
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

	internal class NetDriver : ConsoleDriver {
		public override int Cols => Console.WindowWidth;
		public override int Rows => Console.WindowHeight;

		int [,,] contents;
		bool [] dirtyLine;

		static int MakeColor (int fg, int bg)
		{
			return (fg << 16) | bg;
		}

		void UpdateOffscreen ()
		{
			int cols = Cols;
			int rows = Rows;

			contents = new int [cols, rows, 3];
			for (int r = 0; r < rows; r++) {
				for (int c = 0; c < cols; c++) {
					contents [r, c, 0] = ' ';
					contents [r, c, 1] = MakeColor (7, 0);
					contents [r, c, 2] = 0;
				}
			}
			dirtyLine = new bool [rows];
			for (int row = 0; row < rows; row++)
				dirtyLine [row] = true;
		}

		public NetDriver ()
		{
			UpdateOffscreen ();
		}

		// Current row, and current col, tracked by Move/AddCh only
		int ccol, crow;
		public override void Move (int col, int row)
		{
			ccol = col;
			crow = row;
		}

		public override void AddRune (Rune rune)
		{
			if (Clip.Contains (ccol, crow)) {
				contents [crow, ccol, 0] = (int) (uint) rune;
				contents [crow, ccol, 2] = 1;
			}
			ccol++;
			if (ccol == Cols) {
				ccol = 0;
				if (crow + 1 < Rows)
					crow++;
			}
		}

		public override void AddSpecial (SpecialChar ch)
		{
			AddRune ('*');
		}

		public override void AddStr (ustring str)
		{
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void DrawFrame(Rect region, int padding, bool fill)
		{
			int width = region.Width;
			int height = region.Height;
			int b;

			Move (region.X, region.Y);
			AddRune ('+');
			for (b = 0; b < width - 2; b++)
				AddRune ('-');
			AddRune ('+');
			for (b = 1; b < height - 1; b++) {
				Move (region.X, region.Y + b);
				AddRune ('|');
				if (fill) {
					for (int x = 1; x < width - 1; x++)
						AddRune (' ');
				} else
					Move (region.X + width - 1, region.Y + b);
				AddRune ('|');
			}
			Move (region.X, region.Y + height - 1);
			AddRune ('+');
			for (b = 0; b < width - 2; b++)
				AddRune ('-');
			AddRune ('+');
		}

		public override void End()
		{
			
		}

		public override void Init(Action terminalResized)
		{
			
		}

		public override void RedrawTop()
		{
			int rows = Rows;
			int cols = Cols;

			Console.CursorTop = 0;
			Console.CursorLeft = 0;
			for (int row = 0; row < rows; row++) {
				dirtyLine [row] = false;
				for (int col = 0; col < cols; col++) {
					contents [row, col, 2] = 0;
					Console.Write ((char)contents [row, col, 0]);
				}
			}
		}

		public override void Refresh()
		{
			int rows = Rows;
			int cols = Cols;

			for (int row = 0; row < rows; row++) {
				if (!dirtyLine [row])
					continue;
				dirtyLine [row] = false;
				for (int col = 0; col < cols; col++) {
					if (contents [row, col, 2] != 1)
						continue;
					
					Console.CursorTop = row;
					Console.CursorLeft = col;
					for (; col < cols && contents [row, col, 2] == 1; col++) {
						Console.Write ((char)contents [row, col, 0]);
						contents [row, col, 2] = 0;
					}
				}
			}
		}

		public override void StartReportingMouseMoves()
		{
		}

		public override void StopReportingMouseMoves()
		{
		}

		public override void Suspend()
		{
		}

		public override void SetAttribute(Attribute c)
		{
			throw new NotImplementedException();
		}

		public override void PrepareToRun(MainLoop mainLoop, Action<KeyEvent> target, Action<MouseEvent> mouse)
		{
			throw new NotImplementedException();
		}

		public override void SetColors(ConsoleColor foreground, ConsoleColor background)
		{
			throw new NotImplementedException();
		}

		public override void SetColors(short foregroundColorId, short backgroundColorId)
		{
			throw new NotImplementedException();
		}
	}
}
