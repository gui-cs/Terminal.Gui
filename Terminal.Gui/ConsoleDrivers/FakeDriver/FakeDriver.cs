//
// FakeDriver.cs: A fake ConsoleDriver for unit tests. 
//
// Authors:
//   Charlie Kindel (github.com/tig)
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using NStack;
// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui {
	/// <summary>
	/// Implements a mock ConsoleDriver for unit testing
	/// </summary>
	public class FakeDriver : ConsoleDriver {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		int cols, rows, left, top;
		public override int Cols => cols;
		public override int Rows => rows;
		// Only handling left here because not all terminals has a horizontal scroll bar.
		public override int Left => 0;
		public override int Top => 0;
		public override bool HeightAsBuffer { get; set; }
		public override IClipboard Clipboard { get; }

		// The format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
		int [,,] contents;
		bool [] dirtyLine;

		/// <summary>
		/// Assists with testing, the format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
		/// </summary>
		internal override int [,,] Contents => contents;

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

		static bool sync = false;

		public FakeDriver ()
		{
			if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
				Clipboard = new WindowsClipboard ();
			} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
				Clipboard = new MacOSXClipboard ();
			} else {
				if (CursesDriver.Is_WSL_Platform ()) {
					Clipboard = new WSLClipboard ();
				} else {
					Clipboard = new CursesClipboard ();
				}
			}
		}

		bool needMove;
		// Current row, and current col, tracked by Move/AddCh only
		int ccol, crow;
		public override void Move (int col, int row)
		{
			ccol = col;
			crow = row;

			if (Clip.Contains (col, row)) {
				FakeConsole.CursorTop = row;
				FakeConsole.CursorLeft = col;
				needMove = false;
			} else {
				FakeConsole.CursorTop = Clip.Y;
				FakeConsole.CursorLeft = Clip.X;
				needMove = true;
			}

		}

		public override void AddRune (Rune rune)
		{
			rune = MakePrintable (rune);
			if (Clip.Contains (ccol, crow)) {
				if (needMove) {
					//MockConsole.CursorLeft = ccol;
					//MockConsole.CursorTop = crow;
					needMove = false;
				}
				contents [crow, ccol, 0] = (int)(uint)rune;
				contents [crow, ccol, 1] = currentAttribute;
				contents [crow, ccol, 2] = 1;
				dirtyLine [crow] = true;
			} else
				needMove = true;
			ccol++;
			//if (ccol == Cols) {
			//	ccol = 0;
			//	if (crow + 1 < Rows)
			//		crow++;
			//}
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
			FakeConsole.ResetColor ();
			FakeConsole.Clear ();
		}

		static Attribute MakeColor (ConsoleColor f, ConsoleColor b)
		{
			// Encode the colors into the int value.
			return new Attribute (
				value: ((((int)f) & 0xffff) << 16) | (((int)b) & 0xffff),
				foreground: (Color)f,
				background: (Color)b
				);
		}

		public override void Init (Action terminalResized)
		{
			TerminalResized = terminalResized;

			cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
			rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;
			FakeConsole.Clear ();
			ResizeScreen ();
			UpdateOffScreen ();

			Colors.TopLevel = new ColorScheme ();
			Colors.Base = new ColorScheme ();
			Colors.Dialog = new ColorScheme ();
			Colors.Menu = new ColorScheme ();
			Colors.Error = new ColorScheme ();
			Clip = new Rect (0, 0, Cols, Rows);

			Colors.TopLevel.Normal = MakeColor (ConsoleColor.Green, ConsoleColor.Black);
			Colors.TopLevel.Focus = MakeColor (ConsoleColor.White, ConsoleColor.DarkCyan);
			Colors.TopLevel.HotNormal = MakeColor (ConsoleColor.DarkYellow, ConsoleColor.Black);
			Colors.TopLevel.HotFocus = MakeColor (ConsoleColor.DarkBlue, ConsoleColor.DarkCyan);
			Colors.TopLevel.Disabled = MakeColor (ConsoleColor.DarkGray, ConsoleColor.Black);

			Colors.Base.Normal = MakeColor (ConsoleColor.White, ConsoleColor.Blue);
			Colors.Base.Focus = MakeColor (ConsoleColor.Black, ConsoleColor.Cyan);
			Colors.Base.HotNormal = MakeColor (ConsoleColor.Yellow, ConsoleColor.Blue);
			Colors.Base.HotFocus = MakeColor (ConsoleColor.Yellow, ConsoleColor.Cyan);
			Colors.Base.Disabled = MakeColor (ConsoleColor.DarkGray, ConsoleColor.DarkBlue);

			// Focused,
			//    Selected, Hot: Yellow on Black
			//    Selected, text: white on black
			//    Unselected, hot: yellow on cyan
			//    unselected, text: same as unfocused
			Colors.Menu.HotFocus = MakeColor (ConsoleColor.Yellow, ConsoleColor.Black);
			Colors.Menu.Focus = MakeColor (ConsoleColor.White, ConsoleColor.Black);
			Colors.Menu.HotNormal = MakeColor (ConsoleColor.Yellow, ConsoleColor.Cyan);
			Colors.Menu.Normal = MakeColor (ConsoleColor.White, ConsoleColor.Cyan);
			Colors.Menu.Disabled = MakeColor (ConsoleColor.DarkGray, ConsoleColor.Cyan);

			Colors.Dialog.Normal = MakeColor (ConsoleColor.Black, ConsoleColor.Gray);
			Colors.Dialog.Focus = MakeColor (ConsoleColor.Black, ConsoleColor.Cyan);
			Colors.Dialog.HotNormal = MakeColor (ConsoleColor.Blue, ConsoleColor.Gray);
			Colors.Dialog.HotFocus = MakeColor (ConsoleColor.Blue, ConsoleColor.Cyan);
			Colors.Dialog.Disabled = MakeColor (ConsoleColor.DarkGray, ConsoleColor.Gray);

			Colors.Error.Normal = MakeColor (ConsoleColor.White, ConsoleColor.Red);
			Colors.Error.Focus = MakeColor (ConsoleColor.Black, ConsoleColor.Gray);
			Colors.Error.HotNormal = MakeColor (ConsoleColor.Yellow, ConsoleColor.Red);
			Colors.Error.HotFocus = Colors.Error.HotNormal;
			Colors.Error.Disabled = MakeColor (ConsoleColor.DarkGray, ConsoleColor.White);

			//MockConsole.Clear ();
		}

		public override Attribute MakeAttribute (Color fore, Color back)
		{
			return MakeColor ((ConsoleColor)fore, (ConsoleColor)back);
		}

		int redrawColor = -1;
		void SetColor (int color)
		{
			redrawColor = color;
			IEnumerable<int> values = Enum.GetValues (typeof (ConsoleColor))
			      .OfType<ConsoleColor> ()
			      .Select (s => (int)s);
			if (values.Contains (color & 0xffff)) {
				FakeConsole.BackgroundColor = (ConsoleColor)(color & 0xffff);
			}
			if (values.Contains ((color >> 16) & 0xffff)) {
				FakeConsole.ForegroundColor = (ConsoleColor)((color >> 16) & 0xffff);
			}
		}

		public override void UpdateScreen ()
		{
			int top = Top;
			int left = Left;
			int rows = Math.Min (Console.WindowHeight + top, Rows);
			int cols = Cols;

			FakeConsole.CursorTop = 0;
			FakeConsole.CursorLeft = 0;
			for (int row = top; row < rows; row++) {
				dirtyLine [row] = false;
				for (int col = left; col < cols; col++) {
					contents [row, col, 2] = 0;
					var color = contents [row, col, 1];
					if (color != redrawColor)
						SetColor (color);
					FakeConsole.Write ((char)contents [row, col, 0]);
				}
			}
		}

		public override void Refresh ()
		{
			int rows = Rows;
			int cols = Cols;

			var savedRow = FakeConsole.CursorTop;
			var savedCol = FakeConsole.CursorLeft;
			for (int row = 0; row < rows; row++) {
				if (!dirtyLine [row])
					continue;
				dirtyLine [row] = false;
				for (int col = 0; col < cols; col++) {
					if (contents [row, col, 2] != 1)
						continue;

					FakeConsole.CursorTop = row;
					FakeConsole.CursorLeft = col;
					for (; col < cols && contents [row, col, 2] == 1; col++) {
						var color = contents [row, col, 1];
						if (color != redrawColor)
							SetColor (color);

						FakeConsole.Write ((char)contents [row, col, 0]);
						contents [row, col, 2] = 0;
					}
				}
			}
			FakeConsole.CursorTop = savedRow;
			FakeConsole.CursorLeft = savedCol;
		}

		Attribute currentAttribute;
		public override void SetAttribute (Attribute c)
		{
			currentAttribute = c;
		}

		Key MapKey (ConsoleKeyInfo keyInfo)
		{
			switch (keyInfo.Key) {
			case ConsoleKey.Escape:
				return MapKeyModifiers (keyInfo, Key.Esc);
			case ConsoleKey.Tab:
				return keyInfo.Modifiers == ConsoleModifiers.Shift ? Key.BackTab : Key.Tab;
			case ConsoleKey.Home:
				return MapKeyModifiers (keyInfo, Key.Home);
			case ConsoleKey.End:
				return MapKeyModifiers (keyInfo, Key.End);
			case ConsoleKey.LeftArrow:
				return MapKeyModifiers (keyInfo, Key.CursorLeft);
			case ConsoleKey.RightArrow:
				return MapKeyModifiers (keyInfo, Key.CursorRight);
			case ConsoleKey.UpArrow:
				return MapKeyModifiers (keyInfo, Key.CursorUp);
			case ConsoleKey.DownArrow:
				return MapKeyModifiers (keyInfo, Key.CursorDown);
			case ConsoleKey.PageUp:
				return MapKeyModifiers (keyInfo, Key.PageUp);
			case ConsoleKey.PageDown:
				return MapKeyModifiers (keyInfo, Key.PageDown);
			case ConsoleKey.Enter:
				return MapKeyModifiers (keyInfo, Key.Enter);
			case ConsoleKey.Spacebar:
				return MapKeyModifiers (keyInfo, keyInfo.KeyChar == 0 ? Key.Space : (Key)keyInfo.KeyChar);
			case ConsoleKey.Backspace:
				return MapKeyModifiers (keyInfo, Key.Backspace);
			case ConsoleKey.Delete:
				return MapKeyModifiers (keyInfo, Key.DeleteChar);
			case ConsoleKey.Insert:
				return MapKeyModifiers (keyInfo, Key.InsertChar);

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
				if (keyInfo.KeyChar == 0)
					return Key.Unknown;

				return (Key)((uint)keyInfo.KeyChar);
			}

			var key = keyInfo.Key;
			if (key >= ConsoleKey.A && key <= ConsoleKey.Z) {
				var delta = key - ConsoleKey.A;
				if (keyInfo.Modifiers == ConsoleModifiers.Control) {
					return (Key)(((uint)Key.CtrlMask) | ((uint)Key.A + delta));
				}
				if (keyInfo.Modifiers == ConsoleModifiers.Alt) {
					return (Key)(((uint)Key.AltMask) | ((uint)Key.A + delta));
				}
				if ((keyInfo.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) {
					if (keyInfo.KeyChar == 0) {
						return (Key)(((uint)Key.AltMask | (uint)Key.CtrlMask) | ((uint)Key.A + delta));
					} else {
						return (Key)((uint)keyInfo.KeyChar);
					}
				}
				return (Key)((uint)keyInfo.KeyChar);
			}
			if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9) {
				var delta = key - ConsoleKey.D0;
				if (keyInfo.Modifiers == ConsoleModifiers.Alt) {
					return (Key)(((uint)Key.AltMask) | ((uint)Key.D0 + delta));
				}
				if (keyInfo.Modifiers == ConsoleModifiers.Control) {
					return (Key)(((uint)Key.CtrlMask) | ((uint)Key.D0 + delta));
				}
				if (keyInfo.KeyChar == 0 || keyInfo.KeyChar == 30) {
					return MapKeyModifiers (keyInfo, (Key)((uint)Key.D0 + delta));
				}
				return (Key)((uint)keyInfo.KeyChar);
			}
			if (key >= ConsoleKey.F1 && key <= ConsoleKey.F12) {
				var delta = key - ConsoleKey.F1;
				if ((keyInfo.Modifiers & (ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) {
					return MapKeyModifiers (keyInfo, (Key)((uint)Key.F1 + delta));
				}

				return (Key)((uint)Key.F1 + delta);
			}
			if (keyInfo.KeyChar != 0) {
				return MapKeyModifiers (keyInfo, (Key)((uint)keyInfo.KeyChar));
			}

			return (Key)(0xffffffff);
		}

		KeyModifiers keyModifiers;

		private Key MapKeyModifiers (ConsoleKeyInfo keyInfo, Key key)
		{
			Key keyMod = new Key ();
			if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0)
				keyMod = Key.ShiftMask;
			if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
				keyMod |= Key.CtrlMask;
			if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0)
				keyMod |= Key.AltMask;

			return keyMod != Key.Null ? keyMod | key : key;
		}

		Action<KeyEvent> keyHandler;
		Action<KeyEvent> keyUpHandler;

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
		{
			this.keyHandler = keyHandler;
			this.keyUpHandler = keyUpHandler;

			// Note: Net doesn't support keydown/up events and thus any passed keyDown/UpHandlers will never be called
			(mainLoop.Driver as FakeMainLoop).KeyPressed += (consoleKey) => ProcessInput (consoleKey);
		}

		void ProcessInput (ConsoleKeyInfo consoleKey)
		{
			keyModifiers = new KeyModifiers ();
			var map = MapKey (consoleKey);
			if (map == (Key)0xffffffff)
				return;

			if (consoleKey.Modifiers.HasFlag (ConsoleModifiers.Alt)) {
				keyModifiers.Alt = true;
			}
			if (consoleKey.Modifiers.HasFlag (ConsoleModifiers.Shift)) {
				keyModifiers.Shift = true;
			}
			if (consoleKey.Modifiers.HasFlag (ConsoleModifiers.Control)) {
				keyModifiers.Ctrl = true;
			}

			keyHandler (new KeyEvent (map, keyModifiers));
			keyUpHandler (new KeyEvent (map, keyModifiers));
		}

		public override Attribute GetAttribute ()
		{
			return currentAttribute;
		}

		/// <inheritdoc/>
		public override bool GetCursorVisibility (out CursorVisibility visibility)
		{
			if (FakeConsole.CursorVisible) {
				visibility = CursorVisibility.Default;
			} else {
				visibility = CursorVisibility.Invisible;
			}

			return false;
		}

		/// <inheritdoc/>
		public override bool SetCursorVisibility (CursorVisibility visibility)
		{
			if (visibility == CursorVisibility.Invisible) {
				FakeConsole.CursorVisible = false;
			} else {
				FakeConsole.CursorVisible = true;
			}

			return false;
		}

		/// <inheritdoc/>
		public override bool EnsureCursorVisibility ()
		{
			return false;
		}

		public override void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
		{
			ProcessInput (new ConsoleKeyInfo (keyChar, key, shift, alt, control));
		}

		public void SetBufferSize (int width, int height)
		{
			cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = width;
			rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = height;
			ProcessResize ();
		}

		public void SetWindowSize (int width, int height)
		{
			FakeConsole.WindowWidth = width;
			FakeConsole.WindowHeight = height;
			if (width > cols || !HeightAsBuffer) {
				cols = FakeConsole.BufferWidth = width;
			}
			if (height > rows || !HeightAsBuffer) {
				rows = FakeConsole.BufferHeight = height;
			}
			ProcessResize ();
		}

		public void SetWindowPosition (int left, int top)
		{
			if (HeightAsBuffer) {
				this.left = FakeConsole.WindowLeft = Math.Max (Math.Min (left, Cols - FakeConsole.WindowWidth), 0);
				this.top = FakeConsole.WindowTop = Math.Max (Math.Min (top, Rows - Console.WindowHeight), 0);
			} else if (this.left > 0 || this.top > 0) {
				this.left = FakeConsole.WindowLeft = 0;
				this.top = FakeConsole.WindowTop = 0;
			}
		}

		void ProcessResize ()
		{
			ResizeScreen ();
			UpdateOffScreen ();
			TerminalResized?.Invoke ();
		}

		void ResizeScreen ()
		{
			if (!HeightAsBuffer) {
				if (Console.WindowHeight > 0) {
					// Can raise an exception while is still resizing.
					try {
#pragma warning disable CA1416
						Console.CursorTop = 0;
						Console.CursorLeft = 0;
						Console.WindowTop = 0;
						Console.WindowLeft = 0;
#pragma warning restore CA1416
					} catch (System.IO.IOException) {
						return;
					} catch (ArgumentOutOfRangeException) {
						return;
					}
				}
			} else {
				try {
#pragma warning disable CA1416
					Console.WindowLeft = Math.Max (Math.Min (left, Cols - Console.WindowWidth), 0);
					Console.WindowTop = Math.Max (Math.Min (top, Rows - Console.WindowHeight), 0);
#pragma warning restore CA1416
				} catch (Exception) {
					return;
				}
			}

			Clip = new Rect (0, 0, Cols, Rows);

			contents = new int [Rows, Cols, 3];
			dirtyLine = new bool [Rows];
		}

		void UpdateOffScreen ()
		{
			// Can raise an exception while is still resizing.
			try {
				for (int row = 0; row < rows; row++) {
					for (int c = 0; c < cols; c++) {
						contents [row, c, 0] = ' ';
						contents [row, c, 1] = (ushort)Colors.TopLevel.Normal;
						contents [row, c, 2] = 0;
						dirtyLine [row] = true;
					}
				}
			} catch (IndexOutOfRangeException) { }
		}

		public override bool GetColors (int value, out Color foreground, out Color background)
		{
			bool hasColor = false;
			foreground = default;
			background = default;
			IEnumerable<int> values = Enum.GetValues (typeof (ConsoleColor))
			      .OfType<ConsoleColor> ()
			      .Select (s => (int)s);
			if (values.Contains (value & 0xffff)) {
				hasColor = true;
				background = (Color)(ConsoleColor)(value & 0xffff);
			}
			if (values.Contains ((value >> 16) & 0xffff)) {
				hasColor = true;
				foreground = (Color)(ConsoleColor)((value >> 16) & 0xffff);
			}
			return hasColor;
		}

		#region Unused
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

		public override void SetColors (ConsoleColor foreground, ConsoleColor background)
		{
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

		#endregion
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}