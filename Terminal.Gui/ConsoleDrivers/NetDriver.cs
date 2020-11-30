//
// NetDriver.cs: The System.Console-based .NET driver, works on Windows and Unix, but is not particularly efficient.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NStack;

namespace Terminal.Gui {

	internal class NetDriver : ConsoleDriver {
		int cols, rows, top;
		public override int Cols => cols;
		public override int Rows => rows;
		public override int Top => top;
		public override bool HeightAsBuffer { get; set; }

		// The format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
		int [,,] contents;
		bool [] dirtyLine;

		static bool sync = false;

		// Current row, and current col, tracked by Move/AddCh only
		int ccol, crow;
		public override void Move (int col, int row)
		{
			ccol = col;
			crow = row;
		}

		public override void AddRune (Rune rune)
		{
			if (contents.Length != Rows * Cols * 3) {
				return;
			}
			rune = MakePrintable (rune);
			var runeWidth = Rune.ColumnWidth (rune);
			if (Clip.Contains (ccol, crow) && ccol + Math.Max (runeWidth, 1) <= Cols) {
				contents [crow, ccol, 0] = (int)(uint)rune;
				contents [crow, ccol, 1] = currentAttribute;
				contents [crow, ccol, 2] = 1;
				dirtyLine [crow] = true;

				ccol++;
				if (runeWidth > 1) {
					for (int i = 1; i < runeWidth; i++) {
						if (ccol < cols) {
							contents [crow, ccol, 2] = 0;
						} else {
							break;
						}
						ccol++;
					}
				}
			} else if (ccol < cols && crow < rows) {
				contents [crow, ccol, 2] = 1;
				dirtyLine [crow] = true;
			}

			//if (ccol == Cols) {
			//	ccol = 0;
			//	if (crow + 1 < Rows)
			//		crow++;
			//}
			if (sync) {
				UpdateScreen ();
			}
		}

		public override void AddStr (ustring str)
		{
			foreach (var rune in str)
				AddRune (rune);
		}

		public override void End ()
		{
			Console.ResetColor ();
			Clear ();
		}

		void Clear ()
		{
			if (Rows > 0) {
				Console.Clear ();
			}
		}

		static Attribute MakeColor (ConsoleColor f, ConsoleColor b)
		{
			// Encode the colors into the int value.
			return new Attribute () { value = ((((int)f) & 0xffff) << 16) | (((int)b) & 0xffff) };
		}

		bool isWinPlatform;

		public override void Init (Action terminalResized)
		{
			TerminalResized = terminalResized;

			Console.TreatControlCAsInput = true;
			var p = Environment.OSVersion.Platform;
			if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
				isWinPlatform = true;
			}

			cols = Console.WindowWidth;
			rows = Console.WindowHeight;

			Clear ();
			ResizeScreen ();
			UpdateOffScreen ();

			Colors.TopLevel = new ColorScheme ();
			Colors.Base = new ColorScheme ();
			Colors.Dialog = new ColorScheme ();
			Colors.Menu = new ColorScheme ();
			Colors.Error = new ColorScheme ();

			Colors.TopLevel.Normal = MakeColor (ConsoleColor.Green, ConsoleColor.Black);
			Colors.TopLevel.Focus = MakeColor (ConsoleColor.White, ConsoleColor.DarkCyan);
			Colors.TopLevel.HotNormal = MakeColor (ConsoleColor.DarkYellow, ConsoleColor.Black);
			Colors.TopLevel.HotFocus = MakeColor (ConsoleColor.DarkBlue, ConsoleColor.DarkCyan);

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
			Colors.Menu.Disabled = MakeColor (ConsoleColor.DarkGray, ConsoleColor.Cyan);

			Colors.Dialog.Normal = MakeColor (ConsoleColor.Black, ConsoleColor.Gray);
			Colors.Dialog.Focus = MakeColor (ConsoleColor.Black, ConsoleColor.Cyan);
			Colors.Dialog.HotNormal = MakeColor (ConsoleColor.Blue, ConsoleColor.Gray);
			Colors.Dialog.HotFocus = MakeColor (ConsoleColor.Blue, ConsoleColor.Cyan);

			Colors.Error.Normal = MakeColor (ConsoleColor.White, ConsoleColor.Red);
			Colors.Error.Focus = MakeColor (ConsoleColor.Black, ConsoleColor.Gray);
			Colors.Error.HotNormal = MakeColor (ConsoleColor.Yellow, ConsoleColor.Red);
			Colors.Error.HotFocus = Colors.Error.HotNormal;
		}

		void ResizeScreen ()
		{
			if (!HeightAsBuffer) {
				if (Console.WindowHeight > 0) {
					// Can raise an exception while is still resizing.
					try {
						// Not supported on Unix.
						if (isWinPlatform) {
#pragma warning disable CA1416
							Console.CursorTop = 0;
							Console.CursorLeft = 0;
							Console.WindowTop = 0;
							Console.WindowLeft = 0;
							Console.SetBufferSize (Cols, Rows);
#pragma warning restore CA1416
						} else {
							//Console.Out.Write ($"\x1b[8;{Console.WindowHeight};{Console.WindowWidth}t");
							Console.Out.Write ($"\x1b[0;0" +
								$";{Rows};{Cols}w");
						}
					} catch (System.IO.IOException) {
						return;
					} catch (ArgumentOutOfRangeException) {
						return;
					}
				}
			} else {
				if (isWinPlatform && Console.WindowHeight > 0) {
					// Can raise an exception while is still resizing.
					try {
#pragma warning disable CA1416
						Console.WindowTop = Math.Max (Math.Min (top, Rows - Console.WindowHeight), 0);
#pragma warning restore CA1416
					} catch (Exception) {
						return;
					}
				} else {
					Console.Out.Write ($"\x1b[{top};{Console.WindowLeft}" +
						$";{Rows};{Cols}w");
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

			winChanging = false;
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
				Console.BackgroundColor = (ConsoleColor)(color & 0xffff);
			}
			if (values.Contains ((color >> 16) & 0xffff)) {
				Console.ForegroundColor = (ConsoleColor)((color >> 16) & 0xffff);
			}
		}

		public override void UpdateScreen ()
		{
			if (winChanging || Console.WindowHeight == 0 || contents.Length != Rows * Cols * 3
				|| (!HeightAsBuffer && Rows != Console.WindowHeight)
				|| (HeightAsBuffer && Rows != Console.BufferHeight)) {
				return;
			}

			int top = Top;
			int rows = Math.Min (Console.WindowHeight + top, Rows);
			int cols = Cols;

			for (int row = top; row < rows; row++) {
				if (!dirtyLine [row]) {
					continue;
				}
				dirtyLine [row] = false;
				for (int col = 0; col < cols; col++) {
					if (contents [row, col, 2] != 1) {
						continue;
					}
					if (Console.WindowHeight > 0) {
						// Could happens that the windows is still resizing and the col is bigger than Console.WindowWidth.
						try {
							Console.SetCursorPosition (col, row);
						} catch (Exception) {
							return;
						}
					}
					for (; col < cols && contents [row, col, 2] == 1; col++) {
						// Needed for the .Net Framework.
						if (row == rows - 1 && col == cols - 1) {
							break;
						}
						var color = contents [row, col, 1];
						if (color != redrawColor) {
							SetColor (color);
						}
						Console.Write ((char)contents [row, col, 0]);
						contents [row, col, 2] = 0;
					}
				}
			}

			UpdateCursor ();
		}

		public override void Refresh ()
		{
			UpdateScreen ();
		}

		public override void UpdateCursor ()
		{
			// Prevents the exception of size changing during resizing.
			try {
				if (ccol >= 0 && ccol <= cols && crow >= 0 && crow <= rows) {
					Console.SetCursorPosition (ccol, crow);
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

		int currentAttribute;
		public override void SetAttribute (Attribute c)
		{
			currentAttribute = c.value;
		}

		Key MapKey (ConsoleKeyInfo keyInfo)
		{
			MapKeyModifiers (keyInfo, (Key)keyInfo.Key);
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
				return MapKeyModifiers (keyInfo, Key.Space);
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
					if (keyInfo.KeyChar == 0 || (keyInfo.KeyChar != 0 && keyInfo.KeyChar >= 1 && keyInfo.KeyChar <= 26)) {
						return MapKeyModifiers (keyInfo, (Key)((uint)Key.A + delta));
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
				return (Key)((uint)keyInfo.KeyChar);
			}

			return (Key)(0xffffffff);
		}

		KeyModifiers keyModifiers;

		Key MapKeyModifiers (ConsoleKeyInfo keyInfo, Key key)
		{
			if (keyModifiers == null) {
				keyModifiers = new KeyModifiers ();
			}
			Key keyMod = new Key ();
			if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0) {
				keyMod = Key.ShiftMask;
				keyModifiers.Shift = true;
			}
			if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0) {
				keyMod |= Key.CtrlMask;
				keyModifiers.Ctrl = true;
			}
			if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0) {
				keyMod |= Key.AltMask;
				keyModifiers.Alt = true;
			}

			return keyMod != Key.Null ? keyMod | key : key;
		}

		bool winChanging;
		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
		{
			var mLoop = mainLoop.Driver as NetMainLoop;

			// Note: Net doesn't support keydown/up events and thus any passed keyDown/UpHandlers will never be called
			mLoop.KeyPressed = (consoleKey) => {
				var map = MapKey (consoleKey);
				if (map == (Key)0xffffffff) {
					return;
				}

				if (map == (Key.Space | Key.CtrlMask) || map == (Key.Space | Key.AltMask)) {
					map = Key.AltMask;
					keyModifiers.Alt = true;
					keyModifiers.Ctrl = false;
					keyDownHandler (new KeyEvent (map, keyModifiers));
					keyUpHandler (new KeyEvent (map, keyModifiers));
				} else {
					keyDownHandler (new KeyEvent (map, keyModifiers));
					keyHandler (new KeyEvent (map, keyModifiers));
					keyUpHandler (new KeyEvent (map, keyModifiers));
				}
				keyModifiers = new KeyModifiers ();
			};

			mLoop.WinChanged = (e) => {
				winChanging = true;
				const int Min_WindowWidth = 14;
				Size size = new Size ();
				if (!HeightAsBuffer) {
					size = new Size (Math.Max (Min_WindowWidth, Console.WindowWidth),
						Console.WindowHeight);
					top = 0;
				} else {
					size = new Size (Console.BufferWidth, Console.BufferHeight);
					top = e;
				}
				cols = size.Width;
				rows = size.Height;
				ResizeScreen ();
				UpdateOffScreen ();
				if (!winChanging) {
					TerminalResized.Invoke ();
				}
			};
		}

		public override void SetColors (ConsoleColor foreground, ConsoleColor background)
		{
		}

		public override void SetColors (short foregroundColorId, short backgroundColorId)
		{
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

	/// <summary>
	/// Mainloop intended to be used with the .NET System.Console API, and can
	/// be used on Windows and Unix, it is cross platform but lacks things like
	/// file descriptor monitoring.
	/// </summary>
	/// <remarks>
	/// This implementation is used for NetDriver.
	/// </remarks>
	internal class NetMainLoop : IMainLoopDriver {
		ManualResetEventSlim keyReady = new ManualResetEventSlim (false);
		ManualResetEventSlim waitForProbe = new ManualResetEventSlim (false);
		ManualResetEventSlim winChange = new ManualResetEventSlim (false);
		Queue<ConsoleKeyInfo?> keyResult = new Queue<ConsoleKeyInfo?> ();
		MainLoop mainLoop;
		ConsoleDriver consoleDriver;
		bool winChanged;
		int newTop;
		CancellationTokenSource tokenSource = new CancellationTokenSource ();

		/// <summary>
		/// Invoked when a Key is pressed.
		/// </summary>
		public Action<ConsoleKeyInfo> KeyPressed;

		/// <summary>
		/// Invoked when the window is changed.
		/// </summary>
		public Action<int> WinChanged;

		/// <summary>
		/// Initializes the class with the console driver.
		/// </summary>
		/// <remarks>
		///   Passing a consoleDriver is provided to capture windows resizing.
		/// </remarks>
		/// <param name="consoleDriver">The console driver used by this Net main loop.</param>
		public NetMainLoop (ConsoleDriver consoleDriver = null)
		{
			if (consoleDriver == null) {
				throw new ArgumentNullException ("Console driver instance must be provided.");
			}
			this.consoleDriver = consoleDriver;
		}

		void KeyReader ()
		{
			while (true) {
				waitForProbe.Wait ();
				waitForProbe.Reset ();
				if (keyResult.Count == 0) {
					keyResult.Enqueue (Console.ReadKey (true));
				}
				keyReady.Set ();
			}
		}

		void CheckWinChange ()
		{
			while (true) {
				winChange.Wait ();
				winChange.Reset ();
				WaitWinChange ();
				winChanged = true;
				keyReady.Set ();
			}
		}

		int lastWindowHeight;
		void WaitWinChange ()
		{
			while (true) {
				if (!consoleDriver.HeightAsBuffer) {
					if (Console.WindowWidth != consoleDriver.Cols || Console.WindowHeight != consoleDriver.Rows) {
						return;
					}
				} else {
					if (Console.BufferWidth != consoleDriver.Cols || Console.BufferHeight != consoleDriver.Rows
						|| Console.WindowTop != consoleDriver.Top
						|| Console.WindowHeight != lastWindowHeight) {
						// Top only working on Windows.
						newTop = Console.WindowTop;
						lastWindowHeight = Console.WindowHeight;
						return;
					}
				}
			}
		}

		void IMainLoopDriver.Setup (MainLoop mainLoop)
		{
			this.mainLoop = mainLoop;
			Task.Run (KeyReader);
			Task.Run (CheckWinChange);
		}

		void IMainLoopDriver.Wakeup ()
		{
			keyReady.Set ();
		}

		bool IMainLoopDriver.EventsPending (bool wait)
		{
			waitForProbe.Set ();
			winChange.Set ();

			if (CheckTimers (wait, out var waitTimeout)) {
				return true;
			}

			try {
				if (!tokenSource.IsCancellationRequested) {
					keyReady.Wait (waitTimeout, tokenSource.Token);
				}
			} catch (OperationCanceledException) {
				return true;
			} finally {
				keyReady.Reset ();
			}

			if (!tokenSource.IsCancellationRequested) {
				return keyResult.Count > 0 || CheckTimers (wait, out _) || winChanged;
			}

			tokenSource.Dispose ();
			tokenSource = new CancellationTokenSource ();
			return true;
		}

		bool CheckTimers (bool wait, out int waitTimeout)
		{
			long now = DateTime.UtcNow.Ticks;

			if (mainLoop.timeouts.Count > 0) {
				waitTimeout = (int)((mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
				if (waitTimeout < 0)
					return true;
			} else {
				waitTimeout = -1;
			}

			if (!wait)
				waitTimeout = 0;

			int ic;
			lock (mainLoop.idleHandlers) {
				ic = mainLoop.idleHandlers.Count;
			}

			return ic > 0;
		}

		void IMainLoopDriver.MainIteration ()
		{
			if (keyResult.Count > 0) {
				KeyPressed?.Invoke (keyResult.Dequeue ().Value);
			}
			if (winChanged) {
				winChanged = false;
				WinChanged?.Invoke (newTop);
			}
		}
	}
}