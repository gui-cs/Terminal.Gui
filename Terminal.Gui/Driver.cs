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

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Attribute"/> struct.
		/// </summary>
		/// <param name="value">Value.</param>
		public Attribute (int value)
		{
			this.value = value;
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

		/// <summary>
		/// Vertical line character.
		/// </summary>
		VLine,

		/// <summary>
		/// Stipple pattern
		/// </summary>
		Stipple,

		/// <summary>
		/// Diamond character
		/// </summary>
		Diamond,

		/// <summary>
		/// Upper left corner
		/// </summary>
		ULCorner,

		/// <summary>
		/// Lower left corner
		/// </summary>
		LLCorner,

		/// <summary>
		/// Upper right corner
		/// </summary>
		URCorner,

		/// <summary>
		/// Lower right corner
		/// </summary>
		LRCorner,

		/// <summary>
		/// Left tee
		/// </summary>
		LeftTee,

		/// <summary>
		/// Right tee
		/// </summary>
		RightTee,

		/// <summary>
		/// Top tee 
		/// </summary>
		TopTee,

		/// <summary>
		/// The bottom tee.
		/// </summary>
		BottomTee,

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
		public abstract void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<MouseEvent> mouseHandler);

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

		public virtual void DrawFrame (Rect region, int padding, bool fill)
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
			AddRune (ULCorner);
			for (b = 0; b < fwidth - 2; b++)
				AddRune (HLine);
			AddRune (URCorner);
			for (int c = 0; c < padding; c++)
				AddRune (' ');

			for (b = 1 + padding; b < fheight; b++) {
				Move (region.X, region.Y + b);
				for (int c = 0; c < padding; c++)
					AddRune (' ');
				AddRune (VLine);
				if (fill) {
					for (int x = 1; x < fwidth - 1; x++)
						AddRune (' ');
				} else
					Move (region.X + fwidth - 1, region.Y + b);
				AddRune (VLine);
				for (int c = 0; c < padding; c++)
					AddRune (' ');
			}
			Move (region.X, region.Y + fheight);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
			AddRune (LLCorner);
			for (b = 0; b < fwidth - 2; b++)
				AddRune (HLine);
			AddRune (LRCorner);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
			if (padding > 0) {
				Move (region.X, region.Y + height - padding);
				for (int l = 0; l < padding; l++)
					for (b = 0; b < width; b++)
						AddRune (' ');
			}			
		}


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

		/// <summary>
		/// Disables the cooked event processing from the mouse driver.  At startup, it is assumed mouse events are cooked.
		/// </summary>
		public abstract void UncookMouse ();

		/// <summary>
		/// Enables the cooked event processing from the mouse driver
		/// </summary>
		public abstract void CookMouse ();

		/// <summary>
		/// Horizontal line character.
		/// </summary>
		public Rune HLine;

		/// <summary>
		/// Vertical line character.
		/// </summary>
		public Rune VLine;

		/// <summary>
		/// Stipple pattern
		/// </summary>
		public Rune Stipple;

		/// <summary>
		/// Diamond character
		/// </summary>
		public Rune Diamond;

		/// <summary>
		/// Upper left corner
		/// </summary>
		public Rune ULCorner;

		/// <summary>
		/// Lower left corner
		/// </summary>
		public Rune LLCorner;

		/// <summary>
		/// Upper right corner
		/// </summary>
		public Rune URCorner;

		/// <summary>
		/// Lower right corner
		/// </summary>
		public Rune LRCorner;

		/// <summary>
		/// Left tee
		/// </summary>
		public Rune LeftTee;

		/// <summary>
		/// Right tee
		/// </summary>
		public Rune RightTee;

		/// <summary>
		/// Top tee 
		/// </summary>
		public Rune TopTee;

		/// <summary>
		/// The bottom tee.
		/// </summary>
		public Rune BottomTee;
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

		int lastMouseInterval;
		bool mouseGrabbed;

		public override void UncookMouse()
		{
			if (mouseGrabbed)
				return;
			lastMouseInterval = Curses.mouseinterval (0);
			mouseGrabbed = true;
		}

		public override void CookMouse()
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

	internal class NetDriver : ConsoleDriver {
		public override int Cols => Console.WindowWidth;
		public override int Rows => Console.WindowHeight;

		// The format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
		int [,,] contents;
		bool [] dirtyLine;

		void UpdateOffscreen ()
		{
			int cols = Cols;
			int rows = Rows;

			contents = new int [rows, cols, 3];
			for (int r = 0; r < rows; r++) {
				for (int c = 0; c < cols; c++) {
					contents [r, c, 0] = ' ';
					contents [r, c, 1] = MakeColor (ConsoleColor.Gray, ConsoleColor.Black);
					contents [r, c, 2] = 0;
				}
			}
			dirtyLine = new bool [rows];
			for (int row = 0; row < rows; row++)
				dirtyLine [row] = true;
		}

		static bool sync;

		public NetDriver ()
		{
			UpdateOffscreen ();
		}

		bool needMove;
		// Current row, and current col, tracked by Move/AddCh only
		int ccol, crow;
		public override void Move (int col, int row)
		{
			ccol = col;
			crow = row;

			if (Clip.Contains (col, row)) {
				Console.CursorTop = row;
				Console.CursorLeft = col;
				needMove = false;
			} else {
				Console.CursorTop = Clip.Y;
				Console.CursorLeft = Clip.X;
				needMove = true;
			}

		}

		public override void AddRune (Rune rune)
		{
			if (Clip.Contains (ccol, crow)) {
				if (needMove) {
					Console.CursorLeft = ccol;
					Console.CursorTop = crow;
					needMove = false;
				}
				contents [crow, ccol, 0] = (int)(uint)rune;
				contents [crow, ccol, 1] = currentAttribute;
				contents [crow, ccol, 2] = 1;
				dirtyLine [crow] = true;
			} else
				needMove = true;
			ccol++;
			if (ccol == Cols) {
				ccol = 0;
				if (crow + 1 < Rows)
					crow++;
			}
			if (sync)
				RedrawTop ();
		}

		public override void AddStr (ustring str)
		{
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void End ()
		{

		}

		static Attribute MakeColor (ConsoleColor f, ConsoleColor b)
		{
			// Encode the colors into the int value.
			return new Attribute () { value = ((((int)f) & 0xffff) << 16) | (((int)b) & 0xffff) };
		}


		public override void Init (Action terminalResized)
		{
			Colors.Base = new ColorScheme ();
			Colors.Dialog = new ColorScheme ();
			Colors.Menu = new ColorScheme ();
			Colors.Error = new ColorScheme ();
			Clip = new Rect (0, 0, Cols, Rows);

			HLine =  '\u2500';
			VLine =  '\u2502';
			Stipple =  '\u2592';
			Diamond =  '\u25c6';
			ULCorner =  '\u250C';
			LLCorner =  '\u2514';
			URCorner =  '\u2510';
			LRCorner =  '\u2518';
			LeftTee =  '\u251c';
			RightTee =  '\u2524';
			TopTee =  '\u22a4';
			BottomTee =  '\u22a5';
			
			Colors.Base.Normal = MakeColor (ConsoleColor.White, ConsoleColor.Blue);
			Colors.Base.Focus = MakeColor (ConsoleColor.Black, ConsoleColor.Cyan);
			Colors.Base.HotNormal = MakeColor (ConsoleColor.Yellow, ConsoleColor.Blue);
			Colors.Base.HotFocus = MakeColor (ConsoleColor.Yellow, ConsoleColor.Cyan);

			// Focused, 
			//    Selected, Hot: Yellow on Black
			//    Selected, text: white on black
			//    Unselected, hot: yellow on cyan
			//    unselected, text: same as unfocused
			Colors.Menu.HotFocus = MakeColor (ConsoleColor.Yellow, ConsoleColor.Black);
			Colors.Menu.Focus = MakeColor (ConsoleColor.White, ConsoleColor.Black);
			Colors.Menu.HotNormal = MakeColor (ConsoleColor.Yellow, ConsoleColor.Cyan);
			Colors.Menu.Normal = MakeColor (ConsoleColor.White, ConsoleColor.Cyan);

			Colors.Dialog.Normal = MakeColor (ConsoleColor.Black, ConsoleColor.Gray);
			Colors.Dialog.Focus = MakeColor (ConsoleColor.Black, ConsoleColor.Cyan);
			Colors.Dialog.HotNormal = MakeColor (ConsoleColor.Blue, ConsoleColor.Gray);
			Colors.Dialog.HotFocus = MakeColor (ConsoleColor.Blue, ConsoleColor.Cyan);

			Colors.Error.Normal = MakeColor (ConsoleColor.White, ConsoleColor.Red);
			Colors.Error.Focus = MakeColor (ConsoleColor.Black, ConsoleColor.Gray);
			Colors.Error.HotNormal = MakeColor (ConsoleColor.Yellow, ConsoleColor.Red);
			Colors.Error.HotFocus = Colors.Error.HotNormal;
		}

		int redrawColor = -1;
		void SetColor (int color)
		{
			redrawColor = color;
			Console.BackgroundColor = (ConsoleColor)(color & 0xffff);
			Console.ForegroundColor = (ConsoleColor)((color >> 16) & 0xffff);
		}

		public override void RedrawTop ()
		{
			int rows = Rows;
			int cols = Cols;

			Console.CursorTop = 0;
			Console.CursorLeft = 0;
			for (int row = 0; row < rows; row++) {
				dirtyLine [row] = false;
				for (int col = 0; col < cols; col++) {
					contents [row, col, 2] = 0;
					var color = contents [row, col, 1];
					if (color != redrawColor)
						SetColor (color);
					Console.Write ((char)contents [row, col, 0]);
				}
			}
		}

		public override void Refresh ()
		{
			int rows = Rows;
			int cols = Cols;

			var savedRow = Console.CursorTop;
			var savedCol = Console.CursorLeft;
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
						var color = contents [row, col, 1];
						if (color != redrawColor)
							SetColor (color);

						Console.Write ((char)contents [row, col, 0]);
						contents [row, col, 2] = 0;
					}
				}
			}
			Console.CursorTop = savedRow;
			Console.CursorLeft = savedCol;
		}

		public override void StartReportingMouseMoves()
		{
		}

		public override void StopReportingMouseMoves ()
		{
		}

		public override void Suspend ()
		{
		}

		int currentAttribute;
		public override void SetAttribute (Attribute c)
		{
			currentAttribute = c.value;
		}

		Key MapKey (ConsoleKeyInfo keyInfo)
		{
			switch (keyInfo.Key) {
			case ConsoleKey.Tab:
				return Key.Tab;
			case ConsoleKey.Escape:
				return Key.Esc;
			case ConsoleKey.Home:
				return Key.Home;
			case ConsoleKey.End:
				return Key.End;
			case ConsoleKey.LeftArrow:
				return Key.CursorLeft;
			case ConsoleKey.RightArrow:
				return Key.CursorRight;
			case ConsoleKey.UpArrow:
				return Key.CursorUp;
			case ConsoleKey.DownArrow:
				return Key.CursorDown;
			case ConsoleKey.PageUp:
				return Key.PageUp;
			case ConsoleKey.PageDown:
				return Key.PageDown;
			case ConsoleKey.Enter:
				return Key.Enter;
			case ConsoleKey.Spacebar:
				return Key.Space;
			case ConsoleKey.Backspace:
				return Key.Backspace;
			case ConsoleKey.Delete:
				return Key.Delete;
			}

			var key = keyInfo.Key;
			if (key >= ConsoleKey.A && key <= ConsoleKey.Z){
				var delta = key - ConsoleKey.A;
				if (keyInfo.Modifiers == ConsoleModifiers.Control)
					return (Key)((uint)Key.ControlA + delta);
				if (keyInfo.Modifiers == ConsoleModifiers.Alt)
					return (Key) (((uint)Key.AltMask) | ((uint)'A' + delta));
				if (keyInfo.Modifiers == ConsoleModifiers.Shift)
					return (Key)((uint)'A' + delta);
				else
					return (Key)((uint)'a' + delta);
			}
			if (key >= ConsoleKey.F1 && key <= ConsoleKey.F10) {
				var delta = key - ConsoleKey.F1;

				return (Key)(ConsoleKey.F1 + delta);
			}

			return (Key)(0xffffffff);
		}

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<MouseEvent> mouseHandler)
		{
			mainLoop.WindowsKeyPressed = delegate (ConsoleKeyInfo consoleKey) {
				var map = MapKey (consoleKey);
				if (map == (Key) 0xffffffff)
					return;
				keyHandler (new KeyEvent (map));
			};
		}

		public override void SetColors (ConsoleColor foreground, ConsoleColor background)
		{
			throw new NotImplementedException ();
		}

		public override void SetColors (short foregroundColorId, short backgroundColorId)
		{
			throw new NotImplementedException ();
		}

		public override void CookMouse ()
		{
		}

		public override void UncookMouse ()
		{
		}
	}
}
