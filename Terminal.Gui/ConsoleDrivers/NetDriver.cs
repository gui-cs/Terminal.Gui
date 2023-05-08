//
// NetDriver.cs: The System.Console-based .NET driver, works on Windows and Unix, but is not particularly efficient.
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NStack;

namespace Terminal.Gui {
	internal class NetWinVTConsole {
		IntPtr _inputHandle, _outputHandle, _errorHandle;
		uint _originalInputConsoleMode, _originalOutputConsoleMode, _originalErrorConsoleMode;

		public NetWinVTConsole ()
		{
			_inputHandle = GetStdHandle (STD_INPUT_HANDLE);
			if (!GetConsoleMode (_inputHandle, out uint mode)) {
				throw new ApplicationException ($"Failed to get input console mode, error code: {GetLastError ()}.");
			}
			_originalInputConsoleMode = mode;
			if ((mode & ENABLE_VIRTUAL_TERMINAL_INPUT) < ENABLE_VIRTUAL_TERMINAL_INPUT) {
				mode |= ENABLE_VIRTUAL_TERMINAL_INPUT;
				if (!SetConsoleMode (_inputHandle, mode)) {
					throw new ApplicationException ($"Failed to set input console mode, error code: {GetLastError ()}.");
				}
			}

			_outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
			if (!GetConsoleMode (_outputHandle, out mode)) {
				throw new ApplicationException ($"Failed to get output console mode, error code: {GetLastError ()}.");
			}
			_originalOutputConsoleMode = mode;
			if ((mode & (ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN)) < DISABLE_NEWLINE_AUTO_RETURN) {
				mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;
				if (!SetConsoleMode (_outputHandle, mode)) {
					throw new ApplicationException ($"Failed to set output console mode, error code: {GetLastError ()}.");
				}
			}

			_errorHandle = GetStdHandle (STD_ERROR_HANDLE);
			if (!GetConsoleMode (_errorHandle, out mode)) {
				throw new ApplicationException ($"Failed to get error console mode, error code: {GetLastError ()}.");
			}
			_originalErrorConsoleMode = mode;
			if ((mode & (DISABLE_NEWLINE_AUTO_RETURN)) < DISABLE_NEWLINE_AUTO_RETURN) {
				mode |= DISABLE_NEWLINE_AUTO_RETURN;
				if (!SetConsoleMode (_errorHandle, mode)) {
					throw new ApplicationException ($"Failed to set error console mode, error code: {GetLastError ()}.");
				}
			}
		}

		public void Cleanup ()
		{
			if (!SetConsoleMode (_inputHandle, _originalInputConsoleMode)) {
				throw new ApplicationException ($"Failed to restore input console mode, error code: {GetLastError ()}.");
			}
			if (!SetConsoleMode (_outputHandle, _originalOutputConsoleMode)) {
				throw new ApplicationException ($"Failed to restore output console mode, error code: {GetLastError ()}.");
			}
			if (!SetConsoleMode (_errorHandle, _originalErrorConsoleMode)) {
				throw new ApplicationException ($"Failed to restore error console mode, error code: {GetLastError ()}.");
			}
		}

		const int STD_INPUT_HANDLE = -10;
		const int STD_OUTPUT_HANDLE = -11;
		const int STD_ERROR_HANDLE = -12;

		// Input modes.
		const uint ENABLE_PROCESSED_INPUT = 1;
		const uint ENABLE_LINE_INPUT = 2;
		const uint ENABLE_ECHO_INPUT = 4;
		const uint ENABLE_WINDOW_INPUT = 8;
		const uint ENABLE_MOUSE_INPUT = 16;
		const uint ENABLE_INSERT_MODE = 32;
		const uint ENABLE_QUICK_EDIT_MODE = 64;
		const uint ENABLE_EXTENDED_FLAGS = 128;
		const uint ENABLE_VIRTUAL_TERMINAL_INPUT = 512;

		// Output modes.
		const uint ENABLE_PROCESSED_OUTPUT = 1;
		const uint ENABLE_WRAP_AT_EOL_OUTPUT = 2;
		const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;
		const uint DISABLE_NEWLINE_AUTO_RETURN = 8;
		const uint ENABLE_LVB_GRID_WORLDWIDE = 10;

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetStdHandle (int nStdHandle);

		[DllImport ("kernel32.dll")]
		static extern bool GetConsoleMode (IntPtr hConsoleHandle, out uint lpMode);

		[DllImport ("kernel32.dll")]
		static extern bool SetConsoleMode (IntPtr hConsoleHandle, uint dwMode);

		[DllImport ("kernel32.dll")]
		static extern uint GetLastError ();
	}

	internal class NetEvents {
		ManualResetEventSlim _inputReady = new ManualResetEventSlim (false);
		ManualResetEventSlim _waitForStart = new ManualResetEventSlim (false);
		ManualResetEventSlim _winChange = new ManualResetEventSlim (false);
		Queue<InputResult?> _inputResultQueue = new Queue<InputResult?> ();
		ConsoleDriver _consoleDriver;
		volatile ConsoleKeyInfo [] _cki = null;
		static volatile bool _isEscSeq;
		int _lastWindowHeight;
		bool _stopTasks;
#if PROCESS_REQUEST
		bool _neededProcessRequest;
#endif
		public bool IsTerminalWithOptions { get; set; }
		public EscSeqReqProc EscSeqReqProc { get; } = new EscSeqReqProc ();

		public NetEvents (ConsoleDriver consoleDriver)
		{
			if (consoleDriver == null) {
				throw new ArgumentNullException ("Console driver instance must be provided.");
			}
			_consoleDriver = consoleDriver;
			Task.Run (ProcessInputResultQueue);
			Task.Run (CheckWinChange);
		}

		internal void StopTasks ()
		{
			_stopTasks = true;
		}

		public InputResult? ReadConsoleInput ()
		{
			while (true) {
				if (_stopTasks) {
					return null;
				}
				_waitForStart.Set ();
				_winChange.Set ();

				if (_inputResultQueue.Count == 0) {
					_inputReady.Wait ();
					_inputReady.Reset ();
				}
#if PROCESS_REQUEST
				_neededProcessRequest = false;
#endif
				if (_inputResultQueue.Count > 0) {
					return _inputResultQueue.Dequeue ();
				}
			}
		}

		void ProcessInputResultQueue ()
		{
			while (true) {
				_waitForStart.Wait ();
				_waitForStart.Reset ();

				if (_inputResultQueue.Count == 0) {
					GetConsoleKey ();
				}

				_inputReady.Set ();
			}
		}

		void GetConsoleKey ()
		{
			ConsoleKey key = 0;
			ConsoleModifiers mod = 0;
			ConsoleKeyInfo newConsoleKeyInfo = default;

			while (true) {
				ConsoleKeyInfo consoleKeyInfo = Console.ReadKey (true);
				if ((consoleKeyInfo.KeyChar == (char)Key.Esc && !_isEscSeq)
					|| (consoleKeyInfo.KeyChar != (char)Key.Esc && _isEscSeq)) {
					if (_cki == null && consoleKeyInfo.KeyChar != (char)Key.Esc && _isEscSeq) {
						_cki = EscSeqUtils.ResizeArray (new ConsoleKeyInfo ((char)Key.Esc, 0,
							false, false, false), _cki);
					}
					_isEscSeq = true;
					newConsoleKeyInfo = consoleKeyInfo;
					_cki = EscSeqUtils.ResizeArray (consoleKeyInfo, _cki);
					if (!Console.KeyAvailable) {
						DecodeEscSeq (ref newConsoleKeyInfo, ref key, _cki, ref mod);
						_cki = null;
						_isEscSeq = false;
						break;
					}
				} else if (consoleKeyInfo.KeyChar == (char)Key.Esc && _isEscSeq) {
					DecodeEscSeq (ref newConsoleKeyInfo, ref key, _cki, ref mod);
					_cki = null;
					break;
				} else {
					GetConsoleInputType (consoleKeyInfo);
					break;
				}
			}
		}

		void CheckWinChange ()
		{
			while (true) {
				if (_stopTasks) {
					return;
				}
				_winChange.Wait ();
				_winChange.Reset ();
				WaitWinChange ();
				_inputReady.Set ();
			}
		}

		void WaitWinChange ()
		{
			while (true) {
				// HACK: Sleep for 10ms to mitigate high CPU usage (see issue #1502). 10ms was tested to address the problem, but may not be correct.
				Thread.Sleep (10);
				if (_stopTasks) {
					return;
				}
				switch (IsTerminalWithOptions) {
				case false:
					int buffHeight, buffWidth;
					if (((NetDriver)_consoleDriver).IsWinPlatform) {
						buffHeight = Math.Max (Console.BufferHeight, 0);
						buffWidth = Math.Max (Console.BufferWidth, 0);
					} else {
						buffHeight = _consoleDriver.Rows;
						buffWidth = _consoleDriver.Cols;
					}
					if (IsWinChanged (
						Math.Max (Console.WindowHeight, 0),
						Math.Max (Console.WindowWidth, 0),
						buffHeight,
						buffWidth)) {

						return;
					}
					break;
				case true:
					//Request the size of the text area in characters.
					EscSeqReqProc.Add ("t");
					Console.Out.Write ("\x1b[18t");
					break;
				}
			}
		}

		bool IsWinChanged (int winHeight, int winWidth, int buffHeight, int buffWidth)
		{
			if (!_consoleDriver.EnableConsoleScrolling) {
				if (winWidth != _consoleDriver.Cols || winHeight != _consoleDriver.Rows) {
					var w = Math.Max (winWidth, 0);
					var h = Math.Max (winHeight, 0);
					GetWindowSizeEvent (new Size (w, h));
					return true;
				}
			} else {
				if (winWidth != _consoleDriver.Cols || winHeight != _lastWindowHeight
					|| buffWidth != _consoleDriver.Cols || buffHeight != _consoleDriver.Rows) {

					_lastWindowHeight = Math.Max (winHeight, 0);
					GetWindowSizeEvent (new Size (winWidth, _lastWindowHeight));
					return true;
				}
			}
			return false;
		}

		void GetWindowSizeEvent (Size size)
		{
			WindowSizeEvent windowSizeEvent = new WindowSizeEvent () {
				Size = size
			};

			_inputResultQueue.Enqueue (new InputResult () {
				EventType = EventType.WindowSize,
				WindowSizeEvent = windowSizeEvent
			});
		}

		void GetConsoleInputType (ConsoleKeyInfo consoleKeyInfo)
		{
			InputResult inputResult = new InputResult {
				EventType = EventType.Key
			};
			MouseEvent mouseEvent = new MouseEvent ();
			ConsoleKeyInfo newConsoleKeyInfo = EscSeqUtils.GetConsoleInputKey (consoleKeyInfo);
			if (inputResult.EventType == EventType.Key) {
				inputResult.ConsoleKeyInfo = newConsoleKeyInfo;
			} else {
				inputResult.MouseEvent = mouseEvent;
			}

			_inputResultQueue.Enqueue (inputResult);
		}

		void DecodeEscSeq (ref ConsoleKeyInfo newConsoleKeyInfo, ref ConsoleKey key, ConsoleKeyInfo [] cki, ref ConsoleModifiers mod)
		{
			string c1Control, code, terminating;
			string [] values;
			// isKeyMouse is true if it's CSI<, false otherwise
			bool isKeyMouse;
			bool isReq;
			List<MouseFlags> mouseFlags;
			Point pos;
			EscSeqUtils.DecodeEscSeq (EscSeqReqProc, ref newConsoleKeyInfo, ref key, cki, ref mod, out c1Control, out code, out values, out terminating, out isKeyMouse, out mouseFlags, out pos, out isReq, ProcessContinuousButtonPressed);

			if (isKeyMouse) {
				foreach (var mf in mouseFlags) {
					GetMouseEvent (MapMouseFlags (mf), pos);
				}
				return;
			} else if (isReq) {
				GetRequestEvent (c1Control, code, values, terminating);
				return;
			}
			InputResult inputResult = new InputResult {
				EventType = EventType.Key,
				ConsoleKeyInfo = newConsoleKeyInfo
			};

			_inputResultQueue.Enqueue (inputResult);
		}

		void ProcessContinuousButtonPressed (MouseFlags mouseFlag, Point pos)
		{
			GetMouseEvent (MapMouseFlags (mouseFlag), pos);
		}

		MouseButtonState MapMouseFlags (MouseFlags mouseFlags)
		{
			MouseButtonState mbs = default;
			foreach (var flag in Enum.GetValues (mouseFlags.GetType ())) {
				if (mouseFlags.HasFlag ((MouseFlags)flag)) {
					switch (flag) {
					case MouseFlags.Button1Pressed:
						mbs |= MouseButtonState.Button1Pressed;
						break;
					case MouseFlags.Button1Released:
						mbs |= MouseButtonState.Button1Released;
						break;
					case MouseFlags.Button1Clicked:
						mbs |= MouseButtonState.Button1Clicked;
						break;
					case MouseFlags.Button1DoubleClicked:
						mbs |= MouseButtonState.Button1DoubleClicked;
						break;
					case MouseFlags.Button1TripleClicked:
						mbs |= MouseButtonState.Button1TripleClicked;
						break;
					case MouseFlags.Button2Pressed:
						mbs |= MouseButtonState.Button2Pressed;
						break;
					case MouseFlags.Button2Released:
						mbs |= MouseButtonState.Button2Released;
						break;
					case MouseFlags.Button2Clicked:
						mbs |= MouseButtonState.Button2Clicked;
						break;
					case MouseFlags.Button2DoubleClicked:
						mbs |= MouseButtonState.Button2DoubleClicked;
						break;
					case MouseFlags.Button2TripleClicked:
						mbs |= MouseButtonState.Button2TripleClicked;
						break;
					case MouseFlags.Button3Pressed:
						mbs |= MouseButtonState.Button3Pressed;
						break;
					case MouseFlags.Button3Released:
						mbs |= MouseButtonState.Button3Released;
						break;
					case MouseFlags.Button3Clicked:
						mbs |= MouseButtonState.Button3Clicked;
						break;
					case MouseFlags.Button3DoubleClicked:
						mbs |= MouseButtonState.Button3DoubleClicked;
						break;
					case MouseFlags.Button3TripleClicked:
						mbs |= MouseButtonState.Button3TripleClicked;
						break;
					case MouseFlags.WheeledUp:
						mbs |= MouseButtonState.ButtonWheeledUp;
						break;
					case MouseFlags.WheeledDown:
						mbs |= MouseButtonState.ButtonWheeledDown;
						break;
					case MouseFlags.WheeledLeft:
						mbs |= MouseButtonState.ButtonWheeledLeft;
						break;
					case MouseFlags.WheeledRight:
						mbs |= MouseButtonState.ButtonWheeledRight;
						break;
					case MouseFlags.Button4Pressed:
						mbs |= MouseButtonState.Button4Pressed;
						break;
					case MouseFlags.Button4Released:
						mbs |= MouseButtonState.Button4Released;
						break;
					case MouseFlags.Button4Clicked:
						mbs |= MouseButtonState.Button4Clicked;
						break;
					case MouseFlags.Button4DoubleClicked:
						mbs |= MouseButtonState.Button4DoubleClicked;
						break;
					case MouseFlags.Button4TripleClicked:
						mbs |= MouseButtonState.Button4TripleClicked;
						break;
					case MouseFlags.ButtonShift:
						mbs |= MouseButtonState.ButtonShift;
						break;
					case MouseFlags.ButtonCtrl:
						mbs |= MouseButtonState.ButtonCtrl;
						break;
					case MouseFlags.ButtonAlt:
						mbs |= MouseButtonState.ButtonAlt;
						break;
					case MouseFlags.ReportMousePosition:
						mbs |= MouseButtonState.ReportMousePosition;
						break;
					case MouseFlags.AllEvents:
						mbs |= MouseButtonState.AllEvents;
						break;
					}
				}
			}
			return mbs;
		}

		Point _lastCursorPosition;

		void GetRequestEvent (string c1Control, string code, string [] values, string terminating)
		{
			EventType eventType = new EventType ();
			switch (terminating) {
			case "R": // Reports cursor position as CSI r ; c R
				Point point = new Point {
					X = int.Parse (values [1]) - 1,
					Y = int.Parse (values [0]) - 1
				};
				if (_lastCursorPosition.Y != point.Y) {
					_lastCursorPosition = point;
					eventType = EventType.WindowPosition;
					var winPositionEv = new WindowPositionEvent () {
						CursorPosition = point
					};
					_inputResultQueue.Enqueue (new InputResult () {
						EventType = eventType,
						WindowPositionEvent = winPositionEv
					});
				} else {
					return;
				}
				break;
			case "c":
				try {
					var parent = EscSeqUtils.GetParentProcess (Process.GetCurrentProcess ());
					if (parent == null) { Debug.WriteLine ("Not supported!"); }
				} catch (Exception ex) {
					Debug.WriteLine (ex.Message);
				}

				if (c1Control == "CSI" && values.Length == 2
					&& values [0] == "1" && values [1] == "0") {
					// Reports CSI?1;0c ("VT101 with No Options")
					IsTerminalWithOptions = false;
				} else {
					IsTerminalWithOptions = true;
				}
				break;
			case "t":
				switch (values [0]) {
				case "8":
					IsWinChanged (
						Math.Max (int.Parse (values [1]), 0),
						Math.Max (int.Parse (values [2]), 0),
						Math.Max (int.Parse (values [1]), 0),
						Math.Max (int.Parse (values [2]), 0));
					break;
				default:
					SetRequestedEvent (c1Control, code, values, terminating);
					break;
				}
				break;
			default:
				SetRequestedEvent (c1Control, code, values, terminating);
				break;
			}

			_inputReady.Set ();
		}

		void SetRequestedEvent (string c1Control, string code, string [] values, string terminating)
		{
			EventType eventType = EventType.RequestResponse;
			var requestRespEv = new RequestResponseEvent () {
				ResultTuple = (c1Control, code, values, terminating)
			};
			_inputResultQueue.Enqueue (new InputResult () {
				EventType = eventType,
				RequestResponseEvent = requestRespEv
			});
		}

		void GetMouseEvent (MouseButtonState buttonState, Point pos)
		{
			MouseEvent mouseEvent = new MouseEvent () {
				Position = pos,
				ButtonState = buttonState,
			};

			_inputResultQueue.Enqueue (new InputResult () {
				EventType = EventType.Mouse,
				MouseEvent = mouseEvent
			});

			_inputReady.Set ();
		}

		public enum EventType {
			Key = 1,
			Mouse = 2,
			WindowSize = 3,
			WindowPosition = 4,
			RequestResponse = 5
		}

		[Flags]
		public enum MouseButtonState {
			Button1Pressed = 0x1,
			Button1Released = 0x2,
			Button1Clicked = 0x4,
			Button1DoubleClicked = 0x8,
			Button1TripleClicked = 0x10,
			Button2Pressed = 0x20,
			Button2Released = 0x40,
			Button2Clicked = 0x80,
			Button2DoubleClicked = 0x100,
			Button2TripleClicked = 0x200,
			Button3Pressed = 0x400,
			Button3Released = 0x800,
			Button3Clicked = 0x1000,
			Button3DoubleClicked = 0x2000,
			Button3TripleClicked = 0x4000,
			ButtonWheeledUp = 0x8000,
			ButtonWheeledDown = 0x10000,
			ButtonWheeledLeft = 0x20000,
			ButtonWheeledRight = 0x40000,
			Button4Pressed = 0x80000,
			Button4Released = 0x100000,
			Button4Clicked = 0x200000,
			Button4DoubleClicked = 0x400000,
			Button4TripleClicked = 0x800000,
			ButtonShift = 0x1000000,
			ButtonCtrl = 0x2000000,
			ButtonAlt = 0x4000000,
			ReportMousePosition = 0x8000000,
			AllEvents = -1
		}

		public struct MouseEvent {
			public Point Position;
			public MouseButtonState ButtonState;
		}

		public struct WindowSizeEvent {
			public Size Size;
		}

		public struct WindowPositionEvent {
			public int Top;
			public int Left;
			public Point CursorPosition;
		}

		public struct RequestResponseEvent {
			public (string c1Control, string code, string [] values, string terminating) ResultTuple;
		}

		public struct InputResult {
			public EventType EventType;
			public ConsoleKeyInfo ConsoleKeyInfo;
			public MouseEvent MouseEvent;
			public WindowSizeEvent WindowSizeEvent;
			public WindowPositionEvent WindowPositionEvent;
			public RequestResponseEvent RequestResponseEvent;
		}
	}

	internal class NetDriver : ConsoleDriver {
		const int COLOR_BLACK = 30;
		const int COLOR_RED = 31;
		const int COLOR_GREEN = 32;
		const int COLOR_YELLOW = 33;
		const int COLOR_BLUE = 34;
		const int COLOR_MAGENTA = 35;
		const int COLOR_CYAN = 36;
		const int COLOR_WHITE = 37;
		const int COLOR_BRIGHT_BLACK = 90;
		const int COLOR_BRIGHT_RED = 91;
		const int COLOR_BRIGHT_GREEN = 92;
		const int COLOR_BRIGHT_YELLOW = 93;
		const int COLOR_BRIGHT_BLUE = 94;
		const int COLOR_BRIGHT_MAGENTA = 95;
		const int COLOR_BRIGHT_CYAN = 96;
		const int COLOR_BRIGHT_WHITE = 97;

		public NetWinVTConsole NetWinConsole { get; }
		public bool IsWinPlatform { get; }

		int _largestBufferHeight;

		public NetDriver ()
		{
			var p = Environment.OSVersion.Platform;
			if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
				IsWinPlatform = true;
				NetWinConsole = new NetWinVTConsole ();
			}
			if (IsWinPlatform) {
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

		bool [] _dirtyLine;

		public override void AddRune (Rune rune)
		{
			if (Contents.Length != Rows * Cols * 3) {
				// BUGBUG: Shouldn't this throw an exception? Doing so to see what happens
				throw new InvalidOperationException ("Driver contents are wrong size");
				return;
			}

			rune = MakePrintable (rune);
			var runeWidth = Rune.ColumnWidth (rune);
			var validLocation = IsValidLocation (Col, Row);

			if (validLocation) {
				if (runeWidth == 0 && Col > 0) {
					// Single column glyph beyond first col
					var r = Contents [Row, Col - 1, 0];
					var s = new string (new char [] { (char)r, (char)rune });
					var sn = !s.IsNormalized () ? s.Normalize () : s;
					var c = sn [0];
					Contents [Row, Col - 1, 0] = c;
					Contents [Row, Col - 1, 1] = CurrentAttribute;
					Contents [Row, Col - 1, 2] = 1;
				} else {
					if (runeWidth < 2 && Col > 0
						&& Rune.ColumnWidth ((char)Contents [Row, Col - 1, 0]) > 1) {

						Contents [Row, Col - 1, 0] = (char)System.Text.Rune.ReplacementChar.Value;

					} else if (runeWidth < 2 && Col <= Clip.Right - 1
						&& Rune.ColumnWidth ((char)Contents [Row, Col, 0]) > 1) {

						Contents [Row, Col + 1, 0] = (char)System.Text.Rune.ReplacementChar.Value;
						Contents [Row, Col + 1, 2] = 1;

					}
					if (runeWidth > 1 && Col == Clip.Right - 1) {
						Contents [Row, Col, 0] = (char)System.Text.Rune.ReplacementChar.Value;
					} else {
						Contents [Row, Col, 0] = (int)(uint)rune;
					}
					Contents [Row, Col, 1] = CurrentAttribute;
					Contents [Row, Col, 2] = 1;

				}
				_dirtyLine [Row] = true;
			}

			if (runeWidth < 0 || runeWidth > 0) {
				Col++;
			}

			if (runeWidth > 1) {
				if (validLocation && Col < Clip.Right) {
					Contents [Row, Col, 1] = CurrentAttribute;
					Contents [Row, Col, 2] = 0;
				}
				Col++;
			}
		}

		public override void End ()
		{
			_mainLoop._netEvents.StopTasks ();

			if (IsWinPlatform) {
				NetWinConsole.Cleanup ();
			}

			StopReportingMouseMoves ();
			Console.ResetColor ();

			//Disable alternative screen buffer.
			Console.Out.Write ("\x1b[?1049l");

			//Set cursor key to cursor.
			Console.Out.Write ("\x1b[?25h");

			Console.Out.Close ();
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
			TerminalResized = terminalResized;

			//Enable alternative screen buffer.
			Console.Out.Write ("\x1b[?1049h");

			//Set cursor key to application.
			Console.Out.Write ("\x1b[?25l");

			Console.TreatControlCAsInput = true;

			if (EnableConsoleScrolling) {
				_largestBufferHeight = Console.BufferHeight;
			} else {
				_largestBufferHeight = Console.WindowHeight;
			}

			Cols = Console.WindowWidth;
			Rows = _largestBufferHeight;

			CurrentAttribute = MakeColor (Color.White, Color.Black);
			InitializeColorSchemes ();

			CurrentAttribute = MakeColor (Color.White, Color.Black);
			InitializeColorSchemes ();

			ResizeScreen ();
			UpdateOffScreen ();

			StartReportingMouseMoves ();
		}

		public virtual void ResizeScreen ()
		{
			if (!EnableConsoleScrolling && Console.WindowHeight > 0) {
				// Not supported on Unix.
				if (IsWinPlatform) {
					// Can raise an exception while is still resizing.
					try {
#pragma warning disable CA1416
						Console.CursorTop = 0;
						Console.CursorLeft = 0;
						Console.WindowTop = 0;
						Console.WindowLeft = 0;
						if (Console.WindowHeight > Rows) {
							Console.SetWindowSize (Cols, Rows);
						}
						Console.SetBufferSize (Cols, Rows);
#pragma warning restore CA1416
					} catch (System.IO.IOException) {
						Clip = new Rect (0, 0, Cols, Rows);
					} catch (ArgumentOutOfRangeException) {
						Clip = new Rect (0, 0, Cols, Rows);
					}
				} else {
					Console.Out.Write ($"\x1b[8;{Rows};{Cols}t");
				}
			} else {
				if (IsWinPlatform) {
					if (Console.WindowHeight > 0) {
						// Can raise an exception while is still resizing.
						try {
#pragma warning disable CA1416
							Console.CursorTop = 0;
							Console.CursorLeft = 0;
							if (Console.WindowHeight > Rows) {
								Console.SetWindowSize (Cols, Rows);
							}
							Console.SetBufferSize (Cols, Rows);
#pragma warning restore CA1416
						} catch (System.IO.IOException) {
							Clip = new Rect (0, 0, Cols, Rows);
						} catch (ArgumentOutOfRangeException) {
							Clip = new Rect (0, 0, Cols, Rows);
						}
					}
				} else {
					Console.Out.Write ($"\x1b[30;{Rows};{Cols}t");
				}
			}
			Clip = new Rect (0, 0, Cols, Rows);
		}

		public override void UpdateOffScreen ()
		{
			Contents = new int [Rows, Cols, 3];
			_dirtyLine = new bool [Rows];

			lock (Contents) {
				// Can raise an exception while is still resizing.
				try {
					for (int row = 0; row < Rows; row++) {
						for (int c = 0; c < Cols; c++) {
							Contents [row, c, 0] = ' ';
							Contents [row, c, 1] = (ushort)Colors.TopLevel.Normal;
							Contents [row, c, 2] = 0;
							_dirtyLine [row] = true;
						}
					}
				} catch (IndexOutOfRangeException) { }
			}
		}

		public override void Refresh ()
		{
			UpdateScreen ();
			UpdateCursor ();
		}

		public override void UpdateScreen ()
		{
			if (winChanging || Console.WindowHeight < 1 || Contents.Length != Rows * Cols * 3
				|| (!EnableConsoleScrolling && Rows != Console.WindowHeight)
				|| (EnableConsoleScrolling && Rows != _largestBufferHeight)) {
				return;
			}

			int top = 0;
			int left = 0;
			int rows = Rows;
			int cols = Cols;
			System.Text.StringBuilder output = new System.Text.StringBuilder ();
			int redrawAttr = -1;
			var lastCol = -1;

			Console.CursorVisible = false;

			for (int row = top; row < rows; row++) {
				if (Console.WindowHeight < 1) {
					return;
				}
				if (!_dirtyLine [row]) {
					continue;
				}
				if (!SetCursorPosition (0, row)) {
					return;
				}
				_dirtyLine [row] = false;
				output.Clear ();
				for (int col = left; col < cols; col++) {
					lastCol = -1;
					var outputWidth = 0;
					for (; col < cols; col++) {
						if (Contents [row, col, 2] == 0) {
							if (output.Length > 0) {
								SetCursorPosition (lastCol, row);
								Console.Write (output);
								output.Clear ();
								lastCol += outputWidth;
								outputWidth = 0;
							} else if (lastCol == -1) {
								lastCol = col;
							}
							if (lastCol + 1 < cols)
								lastCol++;
							continue;
						}

						if (lastCol == -1)
							lastCol = col;

						var attr = Contents [row, col, 1];
						if (attr != redrawAttr) {
							redrawAttr = attr;
							output.Append (WriteAttributes (attr));
						}
						outputWidth++;
						var rune = Contents [row, col, 0];
						char [] spair;
						if (Rune.DecodeSurrogatePair ((uint)rune, out spair)) {
							output.Append (spair);
						} else {
							output.Append ((char)rune);
						}
						Contents [row, col, 2] = 0;
					}
				}
				if (output.Length > 0) {
					SetCursorPosition (lastCol, row);
					Console.Write (output);
				}
			}
			SetCursorPosition (0, 0);
		}

		void SetVirtualCursorPosition (int col, int row)
		{
			Console.Out.Write ($"\x1b[{row + 1};{col + 1}H");
		}

		System.Text.StringBuilder WriteAttributes (int attr)
		{
			const string CSI = "\x1b[";
			int bg = 0;
			int fg = 0;
			System.Text.StringBuilder sb = new System.Text.StringBuilder ();

			IEnumerable<int> values = Enum.GetValues (typeof (ConsoleColor))
				  .OfType<ConsoleColor> ()
				  .Select (s => (int)s);
			if (values.Contains (attr & 0xffff)) {
				bg = MapColors ((ConsoleColor)(attr & 0xffff), false);
			}
			if (values.Contains ((attr >> 16) & 0xffff)) {
				fg = MapColors ((ConsoleColor)((attr >> 16) & 0xffff));
			}
			sb.Append ($"{CSI}{bg};{fg}m");

			return sb;
		}

		int MapColors (ConsoleColor color, bool isForeground = true)
		{
			switch (color) {
			case ConsoleColor.Black:
				return isForeground ? COLOR_BLACK : COLOR_BLACK + 10;
			case ConsoleColor.DarkBlue:
				return isForeground ? COLOR_BLUE : COLOR_BLUE + 10;
			case ConsoleColor.DarkGreen:
				return isForeground ? COLOR_GREEN : COLOR_GREEN + 10;
			case ConsoleColor.DarkCyan:
				return isForeground ? COLOR_CYAN : COLOR_CYAN + 10;
			case ConsoleColor.DarkRed:
				return isForeground ? COLOR_RED : COLOR_RED + 10;
			case ConsoleColor.DarkMagenta:
				return isForeground ? COLOR_MAGENTA : COLOR_MAGENTA + 10;
			case ConsoleColor.DarkYellow:
				return isForeground ? COLOR_YELLOW : COLOR_YELLOW + 10;
			case ConsoleColor.Gray:
				return isForeground ? COLOR_WHITE : COLOR_WHITE + 10;
			case ConsoleColor.DarkGray:
				return isForeground ? COLOR_BRIGHT_BLACK : COLOR_BRIGHT_BLACK + 10;
			case ConsoleColor.Blue:
				return isForeground ? COLOR_BRIGHT_BLUE : COLOR_BRIGHT_BLUE + 10;
			case ConsoleColor.Green:
				return isForeground ? COLOR_BRIGHT_GREEN : COLOR_BRIGHT_GREEN + 10;
			case ConsoleColor.Cyan:
				return isForeground ? COLOR_BRIGHT_CYAN : COLOR_BRIGHT_CYAN + 10;
			case ConsoleColor.Red:
				return isForeground ? COLOR_BRIGHT_RED : COLOR_BRIGHT_RED + 10;
			case ConsoleColor.Magenta:
				return isForeground ? COLOR_BRIGHT_MAGENTA : COLOR_BRIGHT_MAGENTA + 10;
			case ConsoleColor.Yellow:
				return isForeground ? COLOR_BRIGHT_YELLOW : COLOR_BRIGHT_YELLOW + 10;
			case ConsoleColor.White:
				return isForeground ? COLOR_BRIGHT_WHITE : COLOR_BRIGHT_WHITE + 10;
			}
			return 0;
		}

		bool SetCursorPosition (int col, int row)
		{
			if (IsWinPlatform) {
				// Could happens that the windows is still resizing and the col is bigger than Console.WindowWidth.
				try {
					Console.SetCursorPosition (col, row);
					return true;
				} catch (Exception) {
					return false;
				}
			} else {
				SetVirtualCursorPosition (col, row);
				return true;
			}
		}

		private void SetWindowPosition (int col, int row)
		{
			if (IsWinPlatform && EnableConsoleScrolling) {
				var winTop = Math.Max (Rows - Console.WindowHeight - row, 0);
				winTop = Math.Min (winTop, Rows - Console.WindowHeight + 1);
				winTop = Math.Max (winTop, 0);
				if (winTop != Console.WindowTop) {
					try {
						if (!EnsureBufferSize ()) {
							return;
						}
#pragma warning disable CA1416
						Console.SetWindowPosition (col, winTop);
#pragma warning restore CA1416
					} catch (System.IO.IOException) {

					} catch (System.ArgumentOutOfRangeException) { }
				}
			}
			Top = Console.WindowTop;
			Left = Console.WindowLeft;
		}

		private bool EnsureBufferSize ()
		{
#pragma warning disable CA1416
			if (IsWinPlatform && Console.BufferHeight < Rows) {
				try {
					Console.SetBufferSize (Console.WindowWidth, Rows);
				} catch (Exception) {
					return false;
				}
			}
#pragma warning restore CA1416
			return true;
		}

		private CursorVisibility? savedCursorVisibility;

		public override void UpdateCursor ()
		{
			EnsureCursorVisibility ();

			if (Col >= 0 && Col < Cols && Row >= 0 && Row < Rows) {
				SetCursorPosition (Col, Row);
				SetWindowPosition (0, Row);
			}
		}

		public override bool GetCursorVisibility (out CursorVisibility visibility)
		{
			visibility = savedCursorVisibility ?? CursorVisibility.Default;
			return visibility == CursorVisibility.Default;
		}

		public override bool SetCursorVisibility (CursorVisibility visibility)
		{
			savedCursorVisibility = visibility;
			return Console.CursorVisible = visibility == CursorVisibility.Default;
		}

		public override bool EnsureCursorVisibility ()
		{
			if (!(Col >= 0 && Row >= 0 && Col < Cols && Row < Rows)) {
				GetCursorVisibility (out CursorVisibility cursorVisibility);
				savedCursorVisibility = cursorVisibility;
				SetCursorVisibility (CursorVisibility.Invisible);
				return false;
			}

			SetCursorVisibility (savedCursorVisibility ?? CursorVisibility.Default);
			return savedCursorVisibility == CursorVisibility.Default;
		}
		
		public override void StartReportingMouseMoves ()
		{
			Console.Out.Write (EscSeqUtils.EnableMouseEvents);
		}

		public override void StopReportingMouseMoves ()
		{
			Console.Out.Write (EscSeqUtils.DisableMouseEvents);
		}

		#region Not Implemented
		public override void Suspend ()
		{
			throw new NotImplementedException ();
		}
		#endregion

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
					if (keyInfo.KeyChar == 0 || keyInfo.KeyChar == 30 || keyInfo.KeyChar == ((uint)Key.D0 + delta)) {
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

		KeyModifiers _keyModifiers;

		Key MapKeyModifiers (ConsoleKeyInfo keyInfo, Key key)
		{
			if (_keyModifiers == null) {
				_keyModifiers = new KeyModifiers ();
			}
			Key keyMod = new Key ();
			if ((keyInfo.Modifiers & ConsoleModifiers.Shift) != 0) {
				keyMod = Key.ShiftMask;
				_keyModifiers.Shift = true;
			}
			if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0) {
				keyMod |= Key.CtrlMask;
				_keyModifiers.Ctrl = true;
			}
			if ((keyInfo.Modifiers & ConsoleModifiers.Alt) != 0) {
				keyMod |= Key.AltMask;
				_keyModifiers.Alt = true;
			}

			return keyMod != Key.Null ? keyMod | key : key;
		}

		Action<KeyEvent> _keyHandler;
		Action<KeyEvent> _keyDownHandler;
		Action<KeyEvent> _keyUpHandler;
		Action<MouseEvent> _mouseHandler;
		NetMainLoop _mainLoop;

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
		{
			_keyHandler = keyHandler;
			_keyDownHandler = keyDownHandler;
			_keyUpHandler = keyUpHandler;
			_mouseHandler = mouseHandler;

			var mLoop = _mainLoop = mainLoop.Driver as NetMainLoop;

			// Note: Net doesn't support keydown/up events and thus any passed keyDown/UpHandlers will be simulated to be called.
			mLoop.ProcessInput = (e) => ProcessInput (e);

			// Check if terminal supports requests
			_mainLoop._netEvents.EscSeqReqProc.Add ("c");
			Console.Out.Write ("\x1b[0c");
		}

		void ProcessInput (NetEvents.InputResult inputEvent)
		{
			switch (inputEvent.EventType) {
			case NetEvents.EventType.Key:
				ConsoleKeyInfo consoleKeyInfo = inputEvent.ConsoleKeyInfo;
				if (consoleKeyInfo.Key == ConsoleKey.Packet) {
					consoleKeyInfo = FromVKPacketToKConsoleKeyInfo (consoleKeyInfo);
				}
				_keyModifiers = new KeyModifiers ();
				var map = MapKey (consoleKeyInfo);
				if (map == (Key)0xffffffff) {
					return;
				}
				if (map == Key.Null) {
					_keyDownHandler (new KeyEvent (map, _keyModifiers));
					_keyUpHandler (new KeyEvent (map, _keyModifiers));
				} else {
					_keyDownHandler (new KeyEvent (map, _keyModifiers));
					_keyHandler (new KeyEvent (map, _keyModifiers));
					_keyUpHandler (new KeyEvent (map, _keyModifiers));
				}
				break;
			case NetEvents.EventType.Mouse:
				_mouseHandler (ToDriverMouse (inputEvent.MouseEvent));
				break;
			case NetEvents.EventType.WindowSize:
				ChangeWin (inputEvent.WindowSizeEvent.Size);
				break;
			case NetEvents.EventType.RequestResponse:
				Application.Top.Data = inputEvent.RequestResponseEvent.ResultTuple;
				break;
			}
		}

		volatile bool winChanging;

		void ChangeWin (Size size)
		{
			winChanging = true;
			if (!EnableConsoleScrolling) {
				_largestBufferHeight = Math.Max (size.Height, 0);
			} else {
				_largestBufferHeight = Math.Max (size.Height, _largestBufferHeight);
			}
			Top = 0;
			Left = 0;
			Cols = size.Width;
			Rows = _largestBufferHeight;
			ResizeScreen ();
			UpdateOffScreen ();
			winChanging = false;
			TerminalResized?.Invoke ();
		}

		MouseEvent ToDriverMouse (NetEvents.MouseEvent me)
		{
			//System.Diagnostics.Debug.WriteLine ($"X: {me.Position.X}; Y: {me.Position.Y}; ButtonState: {me.ButtonState}");

			MouseFlags mouseFlag = 0;

			if ((me.ButtonState & NetEvents.MouseButtonState.Button1Pressed) != 0) {
				mouseFlag |= MouseFlags.Button1Pressed;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button1Released) != 0) {
				mouseFlag |= MouseFlags.Button1Released;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button1Clicked) != 0) {
				mouseFlag |= MouseFlags.Button1Clicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button1DoubleClicked) != 0) {
				mouseFlag |= MouseFlags.Button1DoubleClicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button1TripleClicked) != 0) {
				mouseFlag |= MouseFlags.Button1TripleClicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button2Pressed) != 0) {
				mouseFlag |= MouseFlags.Button2Pressed;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button2Released) != 0) {
				mouseFlag |= MouseFlags.Button2Released;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button2Clicked) != 0) {
				mouseFlag |= MouseFlags.Button2Clicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button2DoubleClicked) != 0) {
				mouseFlag |= MouseFlags.Button2DoubleClicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button2TripleClicked) != 0) {
				mouseFlag |= MouseFlags.Button2TripleClicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button3Pressed) != 0) {
				mouseFlag |= MouseFlags.Button3Pressed;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button3Released) != 0) {
				mouseFlag |= MouseFlags.Button3Released;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button3Clicked) != 0) {
				mouseFlag |= MouseFlags.Button3Clicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button3DoubleClicked) != 0) {
				mouseFlag |= MouseFlags.Button3DoubleClicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button3TripleClicked) != 0) {
				mouseFlag |= MouseFlags.Button3TripleClicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.ButtonWheeledUp) != 0) {
				mouseFlag |= MouseFlags.WheeledUp;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.ButtonWheeledDown) != 0) {
				mouseFlag |= MouseFlags.WheeledDown;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.ButtonWheeledLeft) != 0) {
				mouseFlag |= MouseFlags.WheeledLeft;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.ButtonWheeledRight) != 0) {
				mouseFlag |= MouseFlags.WheeledRight;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button4Pressed) != 0) {
				mouseFlag |= MouseFlags.Button4Pressed;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button4Released) != 0) {
				mouseFlag |= MouseFlags.Button4Released;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button4Clicked) != 0) {
				mouseFlag |= MouseFlags.Button4Clicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button4DoubleClicked) != 0) {
				mouseFlag |= MouseFlags.Button4DoubleClicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.Button4TripleClicked) != 0) {
				mouseFlag |= MouseFlags.Button4TripleClicked;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.ReportMousePosition) != 0) {
				mouseFlag |= MouseFlags.ReportMousePosition;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.ButtonShift) != 0) {
				mouseFlag |= MouseFlags.ButtonShift;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.ButtonCtrl) != 0) {
				mouseFlag |= MouseFlags.ButtonCtrl;
			}
			if ((me.ButtonState & NetEvents.MouseButtonState.ButtonAlt) != 0) {
				mouseFlag |= MouseFlags.ButtonAlt;
			}

			return new MouseEvent () {
				X = me.Position.X,
				Y = me.Position.Y,
				Flags = mouseFlag
			};
		}


		public override void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
		{
			NetEvents.InputResult input = new NetEvents.InputResult ();
			input.EventType = NetEvents.EventType.Key;
			input.ConsoleKeyInfo = new ConsoleKeyInfo (keyChar, key, shift, alt, control);

			try {
				ProcessInput (input);
			} catch (OverflowException) { }
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
		ManualResetEventSlim _keyReady = new ManualResetEventSlim (false);
		ManualResetEventSlim _waitForProbe = new ManualResetEventSlim (false);
		Queue<NetEvents.InputResult?> _inputResult = new Queue<NetEvents.InputResult?> ();
		MainLoop _mainLoop;
		CancellationTokenSource _tokenSource = new CancellationTokenSource ();
		internal NetEvents _netEvents;

		/// <summary>
		/// Invoked when a Key is pressed.
		/// </summary>
		internal Action<NetEvents.InputResult> ProcessInput;

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
			_netEvents = new NetEvents (consoleDriver);
		}

		void NetInputHandler ()
		{
			while (true) {
				_waitForProbe.Wait ();
				_waitForProbe.Reset ();
				if (_inputResult.Count == 0) {
					_inputResult.Enqueue (_netEvents.ReadConsoleInput ());
				}
				try {
					while (_inputResult.Peek () == null) {
						_inputResult.Dequeue ();
					}
					if (_inputResult.Count > 0) {
						_keyReady.Set ();
					}
				} catch (InvalidOperationException) { }
			}
		}

		void IMainLoopDriver.Setup (MainLoop mainLoop)
		{
			_mainLoop = mainLoop;
			Task.Run (NetInputHandler);
		}

		void IMainLoopDriver.Wakeup ()
		{
			_keyReady.Set ();
		}

		bool IMainLoopDriver.EventsPending (bool wait)
		{
			_waitForProbe.Set ();

			if (CheckTimers (wait, out var waitTimeout)) {
				return true;
			}

			try {
				if (!_tokenSource.IsCancellationRequested) {
					_keyReady.Wait (waitTimeout, _tokenSource.Token);
				}
			} catch (OperationCanceledException) {
				return true;
			} finally {
				_keyReady.Reset ();
			}

			if (!_tokenSource.IsCancellationRequested) {
				return _inputResult.Count > 0 || CheckTimers (wait, out _);
			}

			_tokenSource.Dispose ();
			_tokenSource = new CancellationTokenSource ();
			return true;
		}

		bool CheckTimers (bool wait, out int waitTimeout)
		{
			long now = DateTime.UtcNow.Ticks;

			if (_mainLoop.timeouts.Count > 0) {
				waitTimeout = (int)((_mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
				if (waitTimeout < 0) {
					return true;
				}
			} else {
				waitTimeout = -1;
			}

			if (!wait) {
				waitTimeout = 0;
			}

			int ic;
			lock (_mainLoop.idleHandlers) {
				ic = _mainLoop.idleHandlers.Count;
			}

			return ic > 0;
		}

		void IMainLoopDriver.Iteration ()
		{
			while (_inputResult.Count > 0) {
				ProcessInput?.Invoke (_inputResult.Dequeue ().Value);
			}
		}
	}
}