//
// NetDriver.cs: The System.Console-based .NET driver, works on Windows and Unix, but is not particularly efficient.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using Mono.Terminal;
using NStack;

namespace Terminal.Gui {

	internal class NetDriver : ConsoleDriver {
		int cols, rows;
		public override int Cols => cols;
		public override int Rows => rows;

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
			cols = Console.WindowWidth;
			rows = Console.WindowHeight - 1;
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
					//Console.CursorLeft = ccol;
					//Console.CursorTop = crow;
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
				UpdateScreen ();
		}

		public override void AddStr (ustring str)
		{
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void End ()
		{
			Console.ResetColor ();
			Console.Clear ();
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

			HLine = '\u2500';
			VLine = '\u2502';
			Stipple = '\u2592';
			Diamond = '\u25c6';
			ULCorner = '\u250C';
			LLCorner = '\u2514';
			URCorner = '\u2510';
			LRCorner = '\u2518';
			LeftTee = '\u251c';
			RightTee = '\u2524';
			TopTee = '\u22a4';
			BottomTee = '\u22a5';

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
			Console.Clear ();
		}

		int redrawColor = -1;
		void SetColor (int color)
		{
			redrawColor = color;
			Console.BackgroundColor = (ConsoleColor)(color & 0xffff);
			Console.ForegroundColor = (ConsoleColor)((color >> 16) & 0xffff);
		}

		public override void UpdateScreen ()
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

		public override void UpdateCursor ()
		{
			//
		}

		public override void StartReportingMouseMoves ()
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
			case ConsoleKey.Escape:
				return Key.Esc;
			case ConsoleKey.Tab:
				return Key.Tab;
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

			case ConsoleKey.Oem1:
			case ConsoleKey.Oem2:
			case ConsoleKey.Oem3:
			case ConsoleKey.Oem4:
			case ConsoleKey.Oem5:
			case ConsoleKey.Oem6:
			case ConsoleKey.Oem7:
			case ConsoleKey.Oem8:
			case ConsoleKey.Oem102:
			case ConsoleKey.OemPeriod:
			case ConsoleKey.OemComma:
			case ConsoleKey.OemPlus:
			case ConsoleKey.OemMinus:
				return (Key)((uint)keyInfo.KeyChar);
			}

			var key = keyInfo.Key;
			if (key >= ConsoleKey.A && key <= ConsoleKey.Z) {
				var delta = key - ConsoleKey.A;
				if (keyInfo.Modifiers == ConsoleModifiers.Control)
					return (Key)((uint)Key.ControlA + delta);
				if (keyInfo.Modifiers == ConsoleModifiers.Alt)
					return (Key)(((uint)Key.AltMask) | ((uint)'A' + delta));
				if (keyInfo.Modifiers == ConsoleModifiers.Shift)
					return (Key)((uint)'A' + delta);
				else
					return (Key)((uint)'a' + delta);
			}
			if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9) {
				var delta = key - ConsoleKey.D0;
				if (keyInfo.Modifiers == ConsoleModifiers.Alt)
					return (Key)(((uint)Key.AltMask) | ((uint)'0' + delta));
				if (keyInfo.Modifiers == ConsoleModifiers.Shift)
					return (Key)((uint)keyInfo.KeyChar);
				return (Key)((uint)'0' + delta);
			}
			if (key >= ConsoleKey.F1 && key <= ConsoleKey.F10) {
				var delta = key - ConsoleKey.F1;

				return (Key)((int)Key.F1 + delta);
			}
			return (Key)(0xffffffff);
		}

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<MouseEvent> mouseHandler)
		{
			mainLoop.WindowsKeyPressed = delegate (ConsoleKeyInfo consoleKey) {
				var map = MapKey (consoleKey);
				if (map == (Key)0xffffffff)
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

		//
		// These are for the .NET driver, but running natively on Windows, wont run 
		// on the Mono emulation
		//

	}
}