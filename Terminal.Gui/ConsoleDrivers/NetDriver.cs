//
// NetDriver.cs: The System.Console-based .NET driver, works on Windows and Unix, but is not particularly efficient.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Terminal.Gui;
class NetWinVTConsole {
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
	volatile static bool _isEscSeq;
	bool _stopTasks;
#if PROCESS_REQUEST
		bool _neededProcessRequest;
#endif
	public EscSeqRequests EscSeqRequests { get; } = new EscSeqRequests ();

	public NetEvents (ConsoleDriver consoleDriver)
	{
		_consoleDriver = consoleDriver ?? throw new ArgumentNullException (nameof (consoleDriver));
		Task.Run (ProcessInputResultQueue);
		Task.Run (CheckWindowSizeChange);
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
						if (Console.KeyAvailable) continue;
						ProcessRequestResponse (ref newConsoleKeyInfo, ref key, _cki, ref mod);
						_cki = null;
						_isEscSeq = false;
						break;
					} else if (consoleKeyInfo.KeyChar == (char)Key.Esc && _isEscSeq && _cki != null) {
						if (_cki != null) {
							ProcessRequestResponse (ref newConsoleKeyInfo, ref key, _cki, ref mod);
							_cki = null;
						}
						break;
					} else {
						_inputResultQueue.Enqueue (new InputResult {
							EventType = EventType.Key,
							ConsoleKeyInfo = EscSeqUtils.MapConsoleKeyInfo (consoleKeyInfo)
						});
						_isEscSeq = false;
						break;
					}
				}
			}

			_inputReady.Set ();
		}
	}

	void CheckWindowSizeChange ()
	{
		void RequestWindowSize ()
		{
			while (true) {
				// Wait for a while then check if screen has changed sizes
				Task.Delay (500).Wait ();

				if (_stopTasks) {
					return;
				}
				int buffHeight, buffWidth;
				if (((NetDriver)_consoleDriver).IsWinPlatform) {
					buffHeight = Math.Max (Console.BufferHeight, 0);
					buffWidth = Math.Max (Console.BufferWidth, 0);
				} else {
					buffHeight = _consoleDriver.Rows;
					buffWidth = _consoleDriver.Cols;
				}
				if (EnqueueWindowSizeEvent (
				    Math.Max (Console.WindowHeight, 0),
				    Math.Max (Console.WindowWidth, 0),
				    buffHeight,
				    buffWidth)) {

					return;
				}
			}
		}

		while (true) {
			if (_stopTasks) {
				return;
			}
			_winChange.Wait ();
			_winChange.Reset ();
			RequestWindowSize ();
			_inputReady.Set ();
		}
	}

	/// <summary>
	/// Enqueue a window size event if the window size has changed.
	/// </summary>
	/// <param name="winHeight"></param>
	/// <param name="winWidth"></param>
	/// <param name="buffHeight"></param>
	/// <param name="buffWidth"></param>
	/// <returns></returns>
	bool EnqueueWindowSizeEvent (int winHeight, int winWidth, int buffHeight, int buffWidth)
	{
		if (winWidth == _consoleDriver.Cols && winHeight == _consoleDriver.Rows) return false;
		var w = Math.Max (winWidth, 0);
		var h = Math.Max (winHeight, 0);
		_inputResultQueue.Enqueue (new InputResult () {
			EventType = EventType.WindowSize,
			WindowSizeEvent = new WindowSizeEvent () {
				Size = new Size (w, h)
			}
		});
		return true;
	}

	// Process a CSI sequence received by the driver (key pressed, mouse event, or request/response event)
	void ProcessRequestResponse (ref ConsoleKeyInfo newConsoleKeyInfo, ref ConsoleKey key, ConsoleKeyInfo [] cki, ref ConsoleModifiers mod)
	{
		// isMouse is true if it's CSI<, false otherwise
		EscSeqUtils.DecodeEscSeq (EscSeqRequests, ref newConsoleKeyInfo, ref key, cki, ref mod,
		    out var c1Control, out var code, out var values, out var terminating,
		    out var isMouse, out var mouseFlags,
		    out var pos, out var isReq,
		    (f, p) => HandleMouseEvent (MapMouseFlags (f), p));

		if (isMouse) {
			foreach (var mf in mouseFlags) {
				HandleMouseEvent (MapMouseFlags (mf), pos);
			}
			return;
		} else if (isReq) {
			HandleRequestResponseEvent (c1Control, code, values, terminating);
			return;
		}
		HandleKeyboardEvent (newConsoleKeyInfo);
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

	void HandleRequestResponseEvent (string c1Control, string code, string [] values, string terminating)
	{
		switch (terminating) {
		// BUGBUG: I can't find where we send a request for cursor position (ESC[?6n), so I'm not sure if this is needed.
		case EscSeqUtils.CSI_RequestCursorPositionReport_Terminator:
			Point point = new Point {
				X = int.Parse (values [1]) - 1,
				Y = int.Parse (values [0]) - 1
			};
			if (_lastCursorPosition.Y != point.Y) {
				_lastCursorPosition = point;
				var eventType = EventType.WindowPosition;
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

		case EscSeqUtils.CSI_ReportTerminalSizeInChars_Terminator:
			switch (values [0]) {
			case EscSeqUtils.CSI_ReportTerminalSizeInChars_ResponseValue:
				EnqueueWindowSizeEvent (
				    Math.Max (int.Parse (values [1]), 0),
				    Math.Max (int.Parse (values [2]), 0),
				    Math.Max (int.Parse (values [1]), 0),
				    Math.Max (int.Parse (values [2]), 0));
				break;
			default:
				EnqueueRequestResponseEvent (c1Control, code, values, terminating);
				break;
			}
			break;
		default:
			EnqueueRequestResponseEvent (c1Control, code, values, terminating);
			break;
		}

		_inputReady.Set ();
	}

	void EnqueueRequestResponseEvent (string c1Control, string code, string [] values, string terminating)
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

	void HandleMouseEvent (MouseButtonState buttonState, Point pos)
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

	void HandleKeyboardEvent (ConsoleKeyInfo cki)
	{
		InputResult inputResult = new InputResult {
			EventType = EventType.Key,
			ConsoleKeyInfo = cki
		};

		_inputResultQueue.Enqueue (inputResult);
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

	public override bool SupportsTrueColor => Environment.OSVersion.Platform == PlatformID.Unix || (IsWinPlatform && Environment.OSVersion.Version.Build >= 14931);

	public NetWinVTConsole NetWinConsole { get; private set; }
	public bool IsWinPlatform { get; private set; }

	public override void End ()
	{
		_mainLoop?._netEvents.StopTasks ();

		if (IsWinPlatform) {
			NetWinConsole?.Cleanup ();
		}

		StopReportingMouseMoves ();

		if (!RunningUnitTests) {
			Console.ResetColor ();

			//Disable alternative screen buffer.
			Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndActivateAltBufferWithBackscroll);

			//Set cursor key to cursor.
			Console.Out.Write (EscSeqUtils.CSI_ShowCursor);
			Console.Out.Close ();
		}
	}

	public override void Init (Action terminalResized)
	{
		var p = Environment.OSVersion.Platform;
		if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows) {
			IsWinPlatform = true;
			try {
				NetWinConsole = new NetWinVTConsole ();
			} catch (ApplicationException) {
				// Likely running as a unit test, or in a non-interactive session.
			}
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

		TerminalResized = terminalResized;


		if (!RunningUnitTests) {
			Console.TreatControlCAsInput = true;
			
			Cols = Console.WindowWidth;
			Rows = Console.WindowHeight;

			//Enable alternative screen buffer.
			Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);

			//Set cursor key to application.
			Console.Out.Write (EscSeqUtils.CSI_HideCursor);

		} else {
			// We are being run in an environment that does not support a console
			// such as a unit test, or a pipe.
			Cols = 80;
			Rows = 24;
		}

		ResizeScreen ();
		ClearContents ();
		CurrentAttribute = MakeColor (Color.White, Color.Black);
		InitializeColorSchemes ();

		StartReportingMouseMoves ();
	}

	public virtual void ResizeScreen ()
	{
		// Not supported on Unix.
		if (IsWinPlatform) {
			// Can raise an exception while is still resizing.
			try {
#pragma warning disable CA1416
				if (Console.WindowHeight > 0) {
					Console.CursorTop = 0;
					Console.CursorLeft = 0;
					Console.WindowTop = 0;
					Console.WindowLeft = 0;
					if (Console.WindowHeight > Rows) {
						Console.SetWindowSize (Cols, Rows);
					}
					Console.SetBufferSize (Cols, Rows);
				}
#pragma warning restore CA1416
			} catch (System.IO.IOException) {
				Clip = new Rect (0, 0, Cols, Rows);
			} catch (ArgumentOutOfRangeException) {
				Clip = new Rect (0, 0, Cols, Rows);
			}
		} else {
			Console.Out.Write (EscSeqUtils.CSI_SetTerminalWindowSize (Rows, Cols));
		}


		Clip = new Rect (0, 0, Cols, Rows);
	}

	public override void Refresh ()
	{
		UpdateScreen ();
		UpdateCursor ();
	}

	public override void UpdateScreen ()
	{
		if (RunningUnitTests || _winSizeChanging || Console.WindowHeight < 1 || Contents.Length != Rows * Cols || Rows != Console.WindowHeight) {
			return;
		}

		var top = 0;
		var left = 0;
		var rows = Rows;
		var cols = Cols;
		System.Text.StringBuilder output = new System.Text.StringBuilder ();
		Attribute redrawAttr = new Attribute ();
		var lastCol = -1;

		//GetCursorVisibility (out CursorVisibility savedVisibitity);
		//SetCursorVisibility (CursorVisibility.Invisible); 

		for (var row = top; row < rows; row++) {
			if (Console.WindowHeight < 1) {
				return;
			}
			if (!_dirtyLines [row]) {
				continue;
			}
			if (!SetCursorPosition (0, row)) {
				return;
			}
			_dirtyLines [row] = false;
			output.Clear ();
			for (var col = left; col < cols; col++) {
				lastCol = -1;
				var outputWidth = 0;
				for (; col < cols; col++) {
					if (!Contents [row, col].IsDirty) {
						if (output.Length > 0) {
							WriteToConsole (output, ref lastCol, row, ref outputWidth);
						} else if (lastCol == -1) {
							lastCol = col;
						}
						if (lastCol + 1 < cols)
							lastCol++;
						continue;
					}

					if (lastCol == -1) {
						lastCol = col;
					}

					Attribute attr = Contents [row, col].Attribute.Value;
					// Performance: Only send the escape sequence if the attribute has changed.
					if (attr != redrawAttr) {
						redrawAttr = attr;

						if (Force16Colors) {
							output.Append (EscSeqUtils.CSI_SetGraphicsRendition (
								MapColors ((ConsoleColor)attr.Background, false), MapColors ((ConsoleColor)attr.Foreground, true)));
						} else {
							output.Append (EscSeqUtils.CSI_SetForegroundColorRGB (attr.TrueColorForeground.Value.Red, attr.TrueColorForeground.Value.Green, attr.TrueColorForeground.Value.Blue));
							output.Append (EscSeqUtils.CSI_SetBackgroundColorRGB (attr.TrueColorBackground.Value.Red, attr.TrueColorBackground.Value.Green, attr.TrueColorBackground.Value.Blue));
						}

					}
					outputWidth++;
					var rune = (Rune)Contents [row, col].Runes [0];
					output.Append (rune.ToString ());
					if (rune.IsSurrogatePair () && rune.GetColumns () < 2) {
						WriteToConsole (output, ref lastCol, row, ref outputWidth);
						Console.CursorLeft--;
					}
					Contents [row, col].IsDirty = false;
				}
			}
			if (output.Length > 0) {
				SetCursorPosition (lastCol, row);
				Console.Write (output);
			}
		}
		SetCursorPosition (0, 0);

		//SetCursorVisibility (savedVisibitity);

		void WriteToConsole (StringBuilder output, ref int lastCol, int row, ref int outputWidth)
		{
			SetCursorPosition (lastCol, row);
			Console.Write (output);
			output.Clear ();
			lastCol += outputWidth;
			outputWidth = 0;
		}
	}

	#region Color Handling

	// Cache the list of ConsoleColor values.
	private static readonly HashSet<int> ConsoleColorValues = new HashSet<int> (
	    Enum.GetValues (typeof (ConsoleColor)).OfType<ConsoleColor> ().Select (c => (int)c)
	);

	// Dictionary for mapping ConsoleColor values to the values used by System.Net.Console.
	private static Dictionary<ConsoleColor, int> colorMap = new Dictionary<ConsoleColor, int> {
	{ ConsoleColor.Black, COLOR_BLACK },
	{ ConsoleColor.DarkBlue, COLOR_BLUE },
	{ ConsoleColor.DarkGreen, COLOR_GREEN },
	{ ConsoleColor.DarkCyan, COLOR_CYAN },
	{ ConsoleColor.DarkRed, COLOR_RED },
	{ ConsoleColor.DarkMagenta, COLOR_MAGENTA },
	{ ConsoleColor.DarkYellow, COLOR_YELLOW },
	{ ConsoleColor.Gray, COLOR_WHITE },
	{ ConsoleColor.DarkGray, COLOR_BRIGHT_BLACK },
	{ ConsoleColor.Blue, COLOR_BRIGHT_BLUE },
	{ ConsoleColor.Green, COLOR_BRIGHT_GREEN },
	{ ConsoleColor.Cyan, COLOR_BRIGHT_CYAN },
	{ ConsoleColor.Red, COLOR_BRIGHT_RED },
	{ ConsoleColor.Magenta, COLOR_BRIGHT_MAGENTA },
	{ ConsoleColor.Yellow, COLOR_BRIGHT_YELLOW },
	{ ConsoleColor.White, COLOR_BRIGHT_WHITE }
    };

	// Map a ConsoleColor to a platform dependent value.
	int MapColors (ConsoleColor color, bool isForeground = true)
	{
		return colorMap.TryGetValue (color, out var colorValue) ? colorValue + (isForeground ? 0 : 10) : 0;
	}

	/// <remarks>
	/// In the NetDriver, colors are encoded as an int. 
	/// Extracts the foreground and background colors from the encoded value.
	/// Assumes a 4-bit encoded value for both foreground and background colors.
	/// </remarks>
	internal override void GetColors (int value, out Color foreground, out Color background)
	{
		// Assume a 4-bit encoded value for both foreground and background colors.
		foreground = (Color)((value >> 16) & 0xF);
		background = (Color)(value & 0xF);
	}

	/// <remarks>
	/// In the NetDriver, colors are encoded as an int. 
	/// However, the foreground color is stored in the most significant 16 bits, 
	/// and the background color is stored in the least significant 16 bits.
	/// </remarks>
	public override Attribute MakeColor (Color foreground, Color background)
	{
		// Encode the colors into the int value.
		return new Attribute (
		    value: ((((int)foreground) & 0xffff) << 16) | (((int)background) & 0xffff),
		    foreground: foreground,
		    background: background
		);
	}

	#endregion

	#region Cursor Handling
	bool SetCursorPosition (int col, int row)
	{
		//if (IsWinPlatform) {
		// Could happens that the windows is still resizing and the col is bigger than Console.WindowWidth.
		try {
			Console.SetCursorPosition (col, row);
			return true;
		} catch (Exception) {
			return false;
		}
		// BUGBUG: This breaks -usc on WSL; not sure why. But commenting out fixes.
		//} else {
		//	// TODO: Explain why + 1 is needed (and why we do this for non-Windows).
		//	Console.Out.Write (EscSeqUtils.CSI_SetCursorPosition (row + 1, col + 1));
		//	return true;
		//}
	}

	CursorVisibility? _cachedCursorVisibility;

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
		visibility = _cachedCursorVisibility ?? CursorVisibility.Default;
		return visibility == CursorVisibility.Default;
	}

	public override bool SetCursorVisibility (CursorVisibility visibility)
	{
		_cachedCursorVisibility = visibility;
		var isVisible = RunningUnitTests ? visibility == CursorVisibility.Default : Console.CursorVisible = visibility == CursorVisibility.Default;
		//Console.Out.Write (isVisible ? EscSeqUtils.CSI_ShowCursor : EscSeqUtils.CSI_HideCursor);
		return isVisible;
	}

	public override bool EnsureCursorVisibility ()
	{
		if (!(Col >= 0 && Row >= 0 && Col < Cols && Row < Rows)) {
			GetCursorVisibility (out CursorVisibility cursorVisibility);
			_cachedCursorVisibility = cursorVisibility;
			SetCursorVisibility (CursorVisibility.Invisible);
			return false;
		}

		SetCursorVisibility (_cachedCursorVisibility ?? CursorVisibility.Default);
		return _cachedCursorVisibility == CursorVisibility.Default;
	}
	#endregion

	#region Size and Position Handling

	void SetWindowPosition (int col, int row)
	{
		if (!RunningUnitTests) {
			Top = Console.WindowTop;
			Left = Console.WindowLeft;
		} else {
			Top = row;
			Left = col;
		}
	}

	#endregion


	public void StartReportingMouseMoves ()
	{
		if (!RunningUnitTests) {
			Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
		}
	}

	public void StopReportingMouseMoves ()
	{
		if (!RunningUnitTests) {
			Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);
		}
	}

	ConsoleKeyInfo FromVKPacketToKConsoleKeyInfo (ConsoleKeyInfo consoleKeyInfo)
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
		if (key is >= ConsoleKey.F1 and <= ConsoleKey.F12) {
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
		_keyModifiers ??= new KeyModifiers ();
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

		var mLoop = _mainLoop = mainLoop.MainLoopDriver as NetMainLoop;

		// Note: .Net API doesn't support keydown/up events and thus any passed keyDown/UpHandlers will be simulated to be called.
		mLoop.ProcessInput = (e) => ProcessInput (e);
	}

	volatile bool _winSizeChanging;

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
			_winSizeChanging = true;
			Top = 0;
			Left = 0;
			Cols = inputEvent.WindowSizeEvent.Size.Width;
			Rows = Math.Max (inputEvent.WindowSizeEvent.Size.Height, 0); ;
			ResizeScreen ();
			ClearContents ();
			_winSizeChanging = false;
			TerminalResized?.Invoke ();
			break;
		case NetEvents.EventType.RequestResponse:
			// BUGBUG: What is this for? It does not seem to be used anywhere. 
			// It is also not clear what it does. View.Data is documented as "This property is not used internally"
			Application.Top.Data = inputEvent.RequestResponseEvent.ResultTuple;
			break;
		case NetEvents.EventType.WindowPosition:
			break;
		default:
			throw new ArgumentOutOfRangeException ();
		}
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
		NetEvents.InputResult input = new NetEvents.InputResult {
			EventType = NetEvents.EventType.Key,
			ConsoleKeyInfo = new ConsoleKeyInfo (keyChar, key, shift, alt, control)
		};

		try {
			ProcessInput (input);
		} catch (OverflowException) { }
	}


	#region Not Implemented
	public override void Suspend ()
	{
		throw new NotImplementedException ();
	}
	#endregion

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
	/// <exception cref="ArgumentNullException"></exception>
	public NetMainLoop (ConsoleDriver consoleDriver = null)
	{
		if (consoleDriver == null) {
			throw new ArgumentNullException (nameof (consoleDriver));
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
		var now = DateTime.UtcNow.Ticks;

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
	public void TearDown ()
	{
		//throw new NotImplementedException ();
	}
}