//
// FakeDriver.cs: A fake ConsoleDriver for unit tests. 
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using NStack;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui {
	/// <summary>
	/// Implements a mock ConsoleDriver for unit testing
	/// </summary>
	public class FakeDriver : ConsoleDriver {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		public class Behaviors {

			public bool UseFakeClipboard { get; internal set; }
			public bool FakeClipboardAlwaysThrowsNotSupportedException { get; internal set; }
			public bool FakeClipboardIsSupportedAlwaysFalse { get; internal set; }

			public Behaviors (bool useFakeClipboard = false, bool fakeClipboardAlwaysThrowsNotSupportedException = false, bool fakeClipboardIsSupportedAlwaysTrue = false)
			{
				UseFakeClipboard = useFakeClipboard;
				FakeClipboardAlwaysThrowsNotSupportedException = fakeClipboardAlwaysThrowsNotSupportedException;
				FakeClipboardIsSupportedAlwaysFalse = fakeClipboardIsSupportedAlwaysTrue;

				// double check usage is correct
				Debug.Assert (useFakeClipboard == false && fakeClipboardAlwaysThrowsNotSupportedException == false);
				Debug.Assert (useFakeClipboard == false && fakeClipboardIsSupportedAlwaysTrue == false);
			}
		}

		public static FakeDriver.Behaviors FakeBehaviors = new Behaviors ();

		int _cols, _rows, _left, _top;
		public override int Cols => _cols;
		public override int Rows => _rows;
		// Only handling left here because not all terminals has a horizontal scroll bar.
		public override int Left => 0;
		public override int Top => 0;
		public override bool EnableConsoleScrolling { get; set; }
		private IClipboard _clipboard = null;
		public override IClipboard Clipboard => _clipboard;

		// The format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
		int [,,] _contents;
		bool [] _dirtyLine;

		/// <summary>
		/// Assists with testing, the format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
		/// </summary>
		public override int [,,] Contents => _contents;

		//void UpdateOffscreen ()
		//{
		//	int cols = Cols;
		//	int rows = Rows;

		//	contents = new int [rows, cols, 3];
		//	for (int r = 0; r < rows; r++) {
		//		for (int c = 0; c < cols; c++) {
		//			contents [r, c, 0] = ' ';
		//			contents [r, c, 1] = MakeColor (ConsoleColor.Gray, ConsoleColor.Black);
		//			contents [r, c, 2] = 0;
		//		}
		//	}
		//	dirtyLine = new bool [rows];
		//	for (int row = 0; row < rows; row++)
		//		dirtyLine [row] = true;
		//}

		static bool _sync = false;

		public FakeDriver ()
		{
			if (FakeBehaviors.UseFakeClipboard) {
				_clipboard = new FakeClipboard (FakeBehaviors.FakeClipboardAlwaysThrowsNotSupportedException, FakeBehaviors.FakeClipboardIsSupportedAlwaysFalse);
			} else {
				if (RuntimeInformation.IsOSPlatform (OSPlatform.Windows)) {
					_clipboard = new WindowsClipboard ();
				} else if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX)) {
					_clipboard = new MacOSXClipboard ();
				} else {
					if (CursesDriver.Is_WSL_Platform ()) {
						_clipboard = new WSLClipboard ();
					} else {
						_clipboard = new CursesClipboard ();
					}
				}
			}
		}

		bool _needMove;
		// Current row, and current col, tracked by Move/AddCh only
		int _ccol, _crow;
		public override void Move (int col, int row)
		{
			_ccol = col;
			_crow = row;

			if (Clip.Contains (col, row)) {
				FakeConsole.CursorTop = row;
				FakeConsole.CursorLeft = col;
				_needMove = false;
			} else {
				FakeConsole.CursorTop = Clip.Y;
				FakeConsole.CursorLeft = Clip.X;
				_needMove = true;
			}
		}

		public override void AddRune (Rune rune)
		{
			rune = MakePrintable (rune);
			var runeWidth = Rune.ColumnWidth (rune);
			var validLocation = IsValidLocation (_ccol, _crow);

			if (validLocation) {
				if (_needMove) {
					//MockConsole.CursorLeft = ccol;
					//MockConsole.CursorTop = crow;
					_needMove = false;
				}
				if (runeWidth == 0 && _ccol > 0) {
					var r = _contents [_crow, _ccol - 1, 0];
					var s = new string (new char [] { (char)r, (char)rune });
					string sn;
					if (!s.IsNormalized ()) {
						sn = s.Normalize ();
					} else {
						sn = s;
					}
					var c = sn [0];
					_contents [_crow, _ccol - 1, 0] = c;
					_contents [_crow, _ccol - 1, 1] = CurrentAttribute;
					_contents [_crow, _ccol - 1, 2] = 1;

				} else {
					if (runeWidth < 2 && _ccol > 0
					&& Rune.ColumnWidth ((Rune)_contents [_crow, _ccol - 1, 0]) > 1) {

						_contents [_crow, _ccol - 1, 0] = (int)(uint)' ';

					} else if (runeWidth < 2 && _ccol <= Clip.Right - 1
						&& Rune.ColumnWidth ((Rune)_contents [_crow, _ccol, 0]) > 1) {

						_contents [_crow, _ccol + 1, 0] = (int)(uint)' ';
						_contents [_crow, _ccol + 1, 2] = 1;

					}
					if (runeWidth > 1 && _ccol == Clip.Right - 1) {
						_contents [_crow, _ccol, 0] = (int)(uint)' ';
					} else {
						_contents [_crow, _ccol, 0] = (int)(uint)rune;
					}
					_contents [_crow, _ccol, 1] = CurrentAttribute;
					_contents [_crow, _ccol, 2] = 1;

					_dirtyLine [_crow] = true;
				}
			} else {
				_needMove = true;
			}

			if (runeWidth < 0 || runeWidth > 0) {
				_ccol++;
			}

			if (runeWidth > 1) {
				if (validLocation && _ccol < Clip.Right) {
					_contents [_crow, _ccol, 1] = CurrentAttribute;
					_contents [_crow, _ccol, 2] = 0;
				}
				_ccol++;
			}

			//if (ccol == Cols) {
			//	ccol = 0;
			//	if (crow + 1 < Rows)
			//		crow++;
			//}
			if (_sync) {
				UpdateScreen ();
			}
		}

		public override void AddStr (ustring str)
		{
			foreach (var rune in str) {
				AddRune (rune);
			}
		}

		public override void End ()
		{
			FakeConsole.ResetColor ();
			FakeConsole.Clear ();
		}

		public override Attribute MakeColor (Color foreground, Color background)
		{
			return MakeColor ((ConsoleColor)foreground, (ConsoleColor)background);
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
			FakeConsole.MockKeyPresses.Clear ();

			TerminalResized = terminalResized;

			_cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
			_rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;
			FakeConsole.Clear ();
			ResizeScreen ();
			// Call InitalizeColorSchemes before UpdateOffScreen as it references Colors
			CurrentAttribute = MakeColor (Color.White, Color.Black);
			InitalizeColorSchemes ();
			UpdateOffScreen ();
		}

		public override Attribute MakeAttribute (Color fore, Color back)
		{
			return MakeColor ((ConsoleColor)fore, (ConsoleColor)back);
		}

		int _redrawColor = -1;
		void SetColor (int color)
		{
			_redrawColor = color;
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
			int rows = Math.Min (FakeConsole.WindowHeight + top, Rows);
			int cols = Cols;

			var savedRow = FakeConsole.CursorTop;
			var savedCol = FakeConsole.CursorLeft;
			var savedCursorVisible = FakeConsole.CursorVisible;
			for (int row = top; row < rows; row++) {
				if (!_dirtyLine [row]) {
					continue;
				}
				_dirtyLine [row] = false;
				for (int col = left; col < cols; col++) {
					FakeConsole.CursorTop = row;
					FakeConsole.CursorLeft = col;
					for (; col < cols; col++) {
						if (_contents [row, col, 2] == 0) {
							FakeConsole.CursorLeft++;
							continue;
						}

						var color = _contents [row, col, 1];
						if (color != _redrawColor) {
							SetColor (color);
						}

						Rune rune = _contents [row, col, 0];
						if (Rune.DecodeSurrogatePair (rune, out char [] spair)) {
							FakeConsole.Write (spair);
						} else {
							FakeConsole.Write ((char)rune);
						}
						_contents [row, col, 2] = 0;
					}
				}
			}
			FakeConsole.CursorTop = savedRow;
			FakeConsole.CursorLeft = savedCol;
			FakeConsole.CursorVisible = savedCursorVisible;
		}

		public override void Refresh ()
		{
			UpdateScreen ();
			UpdateCursor ();
		}

		public override void SetAttribute (Attribute c)
		{
			base.SetAttribute (c);
		}

		public ConsoleKeyInfo FromVKPacketToKConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
		{
			if (consoleKeyInfo.Key != ConsoleKey.Packet) {
				return consoleKeyInfo;
			}

			var mod = consoleKeyInfo.Modifiers;
			var shift = (mod & ConsoleModifiers.Shift) != 0;
			var alt = (mod & ConsoleModifiers.Alt) != 0;
			var control = (mod & ConsoleModifiers.Control) != 0;

			var keyChar = ConsoleKeyMapping.GetKeyCharFromConsoleKey (consoleKeyInfo.KeyChar, consoleKeyInfo.Modifiers, out uint virtualKey, out _);

			return new ConsoleKeyInfo ((char)keyChar, (ConsoleKey)virtualKey, shift, alt, control);
		}

		Key MapKey (ConsoleKeyInfo keyInfo)
		{
			switch (keyInfo.Key) {
			case ConsoleKey.Escape:
				return MapKeyModifiers (keyInfo, Key.Esc);
			case ConsoleKey.Tab:
				return keyInfo.Modifiers == ConsoleModifiers.Shift ? Key.BackTab : Key.Tab;
			case ConsoleKey.Clear:
				return MapKeyModifiers (keyInfo, Key.Clear);
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
			case ConsoleKey.PrintScreen:
				return MapKeyModifiers (keyInfo, Key.PrintScreen);

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
				if (keyInfo.KeyChar == 0) {
					return Key.Unknown;
				}

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
				if (keyInfo.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt)) {
					return MapKeyModifiers (keyInfo, (Key)((uint)Key.A + delta));
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
				if (keyInfo.Modifiers == (ConsoleModifiers.Shift | ConsoleModifiers.Alt)) {
					return MapKeyModifiers (keyInfo, (Key)((uint)Key.D0 + delta));
				}
				if ((keyInfo.Modifiers & (ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) {
					if (keyInfo.KeyChar == 0 || keyInfo.KeyChar == 30) {
						return MapKeyModifiers (keyInfo, (Key)((uint)Key.D0 + delta));
					}
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
			if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0) {
				keyMod = Key.ShiftMask;
			}
			if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0) {
				keyMod |= Key.CtrlMask;
			}
			if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0) {
				keyMod |= Key.AltMask;
			}

			return keyMod != Key.Null ? keyMod | key : key;
		}

		Action<KeyEvent> _keyDownHandler;
		Action<KeyEvent> _keyHandler;
		Action<KeyEvent> _keyUpHandler;
		private CursorVisibility _savedCursorVisibility;

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
		{
			_keyDownHandler = keyDownHandler;
			_keyHandler = keyHandler;
			_keyUpHandler = keyUpHandler;

			// Note: Net doesn't support keydown/up events and thus any passed keyDown/UpHandlers will never be called
			(mainLoop.Driver as FakeMainLoop).KeyPressed += (consoleKey) => ProcessInput (consoleKey);
		}

		void ProcessInput (ConsoleKeyInfo consoleKey)
		{
			if (consoleKey.Key == ConsoleKey.Packet) {
				consoleKey = FromVKPacketToKConsoleKeyInfo (consoleKey);
			}
			keyModifiers = new KeyModifiers ();
			if (consoleKey.Modifiers.HasFlag (ConsoleModifiers.Shift)) {
				keyModifiers.Shift = true;
			}
			if (consoleKey.Modifiers.HasFlag (ConsoleModifiers.Alt)) {
				keyModifiers.Alt = true;
			}
			if (consoleKey.Modifiers.HasFlag (ConsoleModifiers.Control)) {
				keyModifiers.Ctrl = true;
			}
			var map = MapKey (consoleKey);
			if (map == (Key)0xffffffff) {
				if ((consoleKey.Modifiers & (ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control)) != 0) {
					_keyDownHandler (new KeyEvent (map, keyModifiers));
					_keyUpHandler (new KeyEvent (map, keyModifiers));
				}
				return;
			}

			_keyDownHandler (new KeyEvent (map, keyModifiers));
			_keyHandler (new KeyEvent (map, keyModifiers));
			_keyUpHandler (new KeyEvent (map, keyModifiers));
		}

		/// <inheritdoc/>
		public override bool GetCursorVisibility (out CursorVisibility visibility)
		{
			visibility = FakeConsole.CursorVisible
				? CursorVisibility.Default
				: CursorVisibility.Invisible;

			return FakeConsole.CursorVisible;
		}

		/// <inheritdoc/>
		public override bool SetCursorVisibility (CursorVisibility visibility)
		{
			_savedCursorVisibility = visibility;
			return FakeConsole.CursorVisible = visibility == CursorVisibility.Default;
		}

		/// <inheritdoc/>
		public override bool EnsureCursorVisibility ()
		{
			if (!(_ccol >= 0 && _crow >= 0 && _ccol < Cols && _crow < Rows)) {
				GetCursorVisibility (out CursorVisibility cursorVisibility);
				_savedCursorVisibility = cursorVisibility;
				SetCursorVisibility (CursorVisibility.Invisible);
				return false;
			}

			SetCursorVisibility (_savedCursorVisibility);
			return FakeConsole.CursorVisible;
		}

		public override void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
		{
			ProcessInput (new ConsoleKeyInfo (keyChar, key, shift, alt, control));
		}

		public void SetBufferSize (int width, int height)
		{
			FakeConsole.SetBufferSize (width, height);
			_cols = width;
			_rows = height;
			if (!EnableConsoleScrolling) {
				SetWindowSize (width, height);
			}
			ProcessResize ();
		}

		public void SetWindowSize (int width, int height)
		{
			FakeConsole.SetWindowSize (width, height);
			if (!EnableConsoleScrolling) {
				if (width != _cols || height != _rows) {
					SetBufferSize (width, height);
					_cols = width;
					_rows = height;
				}
			}
			ProcessResize ();
		}

		public void SetWindowPosition (int left, int top)
		{
			if (EnableConsoleScrolling) {
				_left = Math.Max (Math.Min (left, Cols - FakeConsole.WindowWidth), 0);
				_top = Math.Max (Math.Min (top, Rows - FakeConsole.WindowHeight), 0);
			} else if (_left > 0 || _top > 0) {
				_left = 0;
				_top = 0;
			}
			FakeConsole.SetWindowPosition (_left, _top);
		}

		void ProcessResize ()
		{
			ResizeScreen ();
			UpdateOffScreen ();
			TerminalResized?.Invoke ();
		}

		public override void ResizeScreen ()
		{
			if (!EnableConsoleScrolling) {
				if (FakeConsole.WindowHeight > 0) {
					// Can raise an exception while is still resizing.
					try {
#pragma warning disable CA1416
						FakeConsole.CursorTop = 0;
						FakeConsole.CursorLeft = 0;
						FakeConsole.WindowTop = 0;
						FakeConsole.WindowLeft = 0;
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
					FakeConsole.WindowLeft = Math.Max (Math.Min (_left, Cols - FakeConsole.WindowWidth), 0);
					FakeConsole.WindowTop = Math.Max (Math.Min (_top, Rows - FakeConsole.WindowHeight), 0);
#pragma warning restore CA1416
				} catch (Exception) {
					return;
				}
			}

			ClearClipRegion ();
		}

		public override void UpdateOffScreen ()
		{
			_contents = new int [Rows, Cols, 3];
			_dirtyLine = new bool [Rows];

			// Can raise an exception while is still resizing.
			try {
				for (int row = 0; row < _rows; row++) {
					for (int c = 0; c < _cols; c++) {
						_contents [row, c, 0] = ' ';
						_contents [row, c, 1] = (ushort)Colors.TopLevel.Normal;
						_contents [row, c, 2] = 0;
						_dirtyLine [row] = true;
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
			if (!EnsureCursorVisibility ())
				return;

			// Prevents the exception of size changing during resizing.
			try {
				if (_ccol >= 0 && _ccol < FakeConsole.BufferWidth && _crow >= 0 && _crow < FakeConsole.BufferHeight) {
					FakeConsole.SetCursorPosition (_ccol, _crow);
				}
			} catch (System.IO.IOException) {
			} catch (ArgumentOutOfRangeException) {
			}
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
		

		#endregion

		public class FakeClipboard : ClipboardBase {
			public Exception FakeException = null;

			string _contents = string.Empty;

			bool _isSupportedAlwaysFalse = false;

			public override bool IsSupported => !_isSupportedAlwaysFalse;

			public FakeClipboard (bool fakeClipboardThrowsNotSupportedException = false, bool isSupportedAlwaysFalse = false)
			{
				_isSupportedAlwaysFalse = isSupportedAlwaysFalse;
				if (fakeClipboardThrowsNotSupportedException) {
					FakeException = new NotSupportedException ("Fake clipboard exception");
				}
			}

			protected override string GetClipboardDataImpl ()
			{
				if (FakeException != null) {
					throw FakeException;
				}
				return _contents;
			}

			protected override void SetClipboardDataImpl (string text)
			{
				if (FakeException != null) {
					throw FakeException;
				}
				_contents = text;
			}
		}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}