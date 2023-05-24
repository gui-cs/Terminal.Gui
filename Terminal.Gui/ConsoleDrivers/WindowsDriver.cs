//
// WindowsDriver.cs: Windows specific driver
//
using System.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Terminal.Gui;

internal class WindowsConsole {
	public const int STD_OUTPUT_HANDLE = -11;
	public const int STD_INPUT_HANDLE = -10;
	public const int STD_ERROR_HANDLE = -12;

	IntPtr _inputHandle, _outputHandle;
	IntPtr _screenBuffer;
	readonly uint _originalConsoleMode;
	CursorVisibility? _initialCursorVisibility = null;
	CursorVisibility? _currentCursorVisibility = null;
	CursorVisibility? _pendingCursorVisibility = null;

	public WindowsConsole ()
	{
		_inputHandle = GetStdHandle (STD_INPUT_HANDLE);
		_outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
		_originalConsoleMode = ConsoleMode;
		var newConsoleMode = _originalConsoleMode;
		newConsoleMode |= (uint)(ConsoleModes.EnableMouseInput | ConsoleModes.EnableExtendedFlags);
		newConsoleMode &= ~(uint)ConsoleModes.EnableQuickEditMode;
		newConsoleMode &= ~(uint)ConsoleModes.EnableProcessedInput;
		ConsoleMode = newConsoleMode;
	}

	CharInfo [] _originalStdOutChars;

	public bool WriteToConsole (Size size, CharInfo [] charInfoBuffer, Coord coords, SmallRect window)
	{
		if (_screenBuffer == IntPtr.Zero) {
			ReadFromConsoleOutput (size, coords, ref window);
		}

		return WriteConsoleOutput (_screenBuffer, charInfoBuffer, coords, new Coord () { X = window.Left, Y = window.Top }, ref window);
	}

	public void ReadFromConsoleOutput (Size size, Coord coords, ref SmallRect window)
	{
		_screenBuffer = CreateConsoleScreenBuffer (
			DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
			ShareMode.FileShareRead | ShareMode.FileShareWrite,
			IntPtr.Zero,
			1,
			IntPtr.Zero
		);
		if (_screenBuffer == INVALID_HANDLE_VALUE) {
			var err = Marshal.GetLastWin32Error ();

			if (err != 0) {
				throw new System.ComponentModel.Win32Exception (err);
			}
		}

		if (!_initialCursorVisibility.HasValue && GetCursorVisibility (out CursorVisibility visibility)) {
			_initialCursorVisibility = visibility;
		}

		if (!SetConsoleActiveScreenBuffer (_screenBuffer)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}

		_originalStdOutChars = new CharInfo [size.Height * size.Width];

		if (!ReadConsoleOutput (_screenBuffer, _originalStdOutChars, coords, new Coord () { X = 0, Y = 0 }, ref window)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}
	}

	public bool SetCursorPosition (Coord position)
	{
		return SetConsoleCursorPosition (_screenBuffer, position);
	}

	public void SetInitialCursorVisibility ()
	{
		if (_initialCursorVisibility.HasValue == false && GetCursorVisibility (out CursorVisibility visibility)) {
			_initialCursorVisibility = visibility;
		}
	}

	public bool GetCursorVisibility (out CursorVisibility visibility)
	{
		if (_screenBuffer == IntPtr.Zero) {
			visibility = CursorVisibility.Invisible;
			return false;
		}
		if (!GetConsoleCursorInfo (_screenBuffer, out ConsoleCursorInfo info)) {
			var err = Marshal.GetLastWin32Error ();
			if (err != 0) {
				throw new System.ComponentModel.Win32Exception (err);
			}
			visibility = Gui.CursorVisibility.Default;

			return false;
		}

		if (!info.bVisible) {
			visibility = CursorVisibility.Invisible;
		} else if (info.dwSize > 50) {
			visibility = CursorVisibility.Box;
		} else {
			visibility = CursorVisibility.Underline;
		}

		return true;
	}

	public bool EnsureCursorVisibility ()
	{
		if (_initialCursorVisibility.HasValue && _pendingCursorVisibility.HasValue && SetCursorVisibility (_pendingCursorVisibility.Value)) {
			_pendingCursorVisibility = null;

			return true;
		}

		return false;
	}

	public void ForceRefreshCursorVisibility ()
	{
		if (_currentCursorVisibility.HasValue) {
			_pendingCursorVisibility = _currentCursorVisibility;
			_currentCursorVisibility = null;
		}
	}

	public bool SetCursorVisibility (CursorVisibility visibility)
	{
		if (_initialCursorVisibility.HasValue == false) {
			_pendingCursorVisibility = visibility;

			return false;
		}

		if (_currentCursorVisibility.HasValue == false || _currentCursorVisibility.Value != visibility) {
			ConsoleCursorInfo info = new ConsoleCursorInfo {
				dwSize = (uint)visibility & 0x00FF,
				bVisible = ((uint)visibility & 0xFF00) != 0
			};

			if (!SetConsoleCursorInfo (_screenBuffer, ref info)) {
				return false;
			}

			_currentCursorVisibility = visibility;
		}

		return true;
	}

	public void Cleanup ()
	{
		if (_initialCursorVisibility.HasValue) {
			SetCursorVisibility (_initialCursorVisibility.Value);
		}

		SetConsoleOutputWindow (out _);

		ConsoleMode = _originalConsoleMode;
		//ContinueListeningForConsoleEvents = false;
		if (!SetConsoleActiveScreenBuffer (_outputHandle)) {
			var err = Marshal.GetLastWin32Error ();
			Console.WriteLine ("Error: {0}", err);
		}

		if (_screenBuffer != IntPtr.Zero) {
			CloseHandle (_screenBuffer);
		}

		_screenBuffer = IntPtr.Zero;
	}

	internal Size GetConsoleBufferWindow (out Point position)
	{
		if (_screenBuffer == IntPtr.Zero) {
			position = Point.Empty;
			return Size.Empty;
		}

		var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
		csbi.cbSize = (uint)Marshal.SizeOf (csbi);
		if (!GetConsoleScreenBufferInfoEx (_screenBuffer, ref csbi)) {
			//throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			position = Point.Empty;
			return Size.Empty;
		}
		var sz = new Size (csbi.srWindow.Right - csbi.srWindow.Left + 1,
			csbi.srWindow.Bottom - csbi.srWindow.Top + 1);
		position = new Point (csbi.srWindow.Left, csbi.srWindow.Top);

		return sz;
	}

	internal Size GetConsoleOutputWindow (out Point position)
	{
		var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
		csbi.cbSize = (uint)Marshal.SizeOf (csbi);
		if (!GetConsoleScreenBufferInfoEx (_outputHandle, ref csbi)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}
		var sz = new Size (csbi.srWindow.Right - csbi.srWindow.Left + 1,
			csbi.srWindow.Bottom - csbi.srWindow.Top + 1);
		position = new Point (csbi.srWindow.Left, csbi.srWindow.Top);

		return sz;
	}

	internal Size SetConsoleWindow (short cols, short rows)
	{
		var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
		csbi.cbSize = (uint)Marshal.SizeOf (csbi);
		if (!GetConsoleScreenBufferInfoEx (_screenBuffer, ref csbi)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}
		var maxWinSize = GetLargestConsoleWindowSize (_screenBuffer);
		var newCols = Math.Min (cols, maxWinSize.X);
		var newRows = Math.Min (rows, maxWinSize.Y);
		csbi.dwSize = new Coord (newCols, Math.Max (newRows, (short)1));
		csbi.srWindow = new SmallRect (0, 0, newCols, newRows);
		csbi.dwMaximumWindowSize = new Coord (newCols, newRows);
		if (!SetConsoleScreenBufferInfoEx (_screenBuffer, ref csbi)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}
		var winRect = new SmallRect (0, 0, (short)(newCols - 1), (short)Math.Max (newRows - 1, 0));
		if (!SetConsoleWindowInfo (_outputHandle, true, ref winRect)) {
			//throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
			return new Size (cols, rows);
		}
		SetConsoleOutputWindow (csbi);
		return new Size (winRect.Right + 1, newRows - 1 < 0 ? 0 : winRect.Bottom + 1);
	}

	void SetConsoleOutputWindow (CONSOLE_SCREEN_BUFFER_INFOEX csbi)
	{
		if (_screenBuffer != IntPtr.Zero && !SetConsoleScreenBufferInfoEx (_screenBuffer, ref csbi)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}
	}

	internal Size SetConsoleOutputWindow (out Point position)
	{
		if (_screenBuffer == IntPtr.Zero) {
			position = Point.Empty;
			return Size.Empty;
		}

		var csbi = new CONSOLE_SCREEN_BUFFER_INFOEX ();
		csbi.cbSize = (uint)Marshal.SizeOf (csbi);
		if (!GetConsoleScreenBufferInfoEx (_screenBuffer, ref csbi)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}
		var sz = new Size (csbi.srWindow.Right - csbi.srWindow.Left + 1,
			Math.Max (csbi.srWindow.Bottom - csbi.srWindow.Top + 1, 0));
		position = new Point (csbi.srWindow.Left, csbi.srWindow.Top);
		SetConsoleOutputWindow (csbi);
		var winRect = new SmallRect (0, 0, (short)(sz.Width - 1), (short)Math.Max (sz.Height - 1, 0));
		if (!SetConsoleScreenBufferInfoEx (_outputHandle, ref csbi)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}
		if (!SetConsoleWindowInfo (_outputHandle, true, ref winRect)) {
			throw new System.ComponentModel.Win32Exception (Marshal.GetLastWin32Error ());
		}

		return sz;
	}

	//bool ContinueListeningForConsoleEvents = true;

	uint ConsoleMode {
		get {
			GetConsoleMode (_inputHandle, out uint v);
			return v;
		}
		set {
			SetConsoleMode (_inputHandle, value);
		}
	}

	[Flags]
	public enum ConsoleModes : uint {
		EnableProcessedInput = 1,
		EnableMouseInput = 16,
		EnableQuickEditMode = 64,
		EnableExtendedFlags = 128,
	}

	[StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	public struct KeyEventRecord {
		[FieldOffset (0), MarshalAs (UnmanagedType.Bool)]
		public bool bKeyDown;
		[FieldOffset (4), MarshalAs (UnmanagedType.U2)]
		public ushort wRepeatCount;
		[FieldOffset (6), MarshalAs (UnmanagedType.U2)]
		public ushort wVirtualKeyCode;
		[FieldOffset (8), MarshalAs (UnmanagedType.U2)]
		public ushort wVirtualScanCode;
		[FieldOffset (10)]
		public char UnicodeChar;
		[FieldOffset (12), MarshalAs (UnmanagedType.U4)]
		public ControlKeyState dwControlKeyState;
	}

	[Flags]
	public enum ButtonState {
		Button1Pressed = 1,
		Button2Pressed = 4,
		Button3Pressed = 8,
		Button4Pressed = 16,
		RightmostButtonPressed = 2
	}

	[Flags]
	public enum ControlKeyState {
		RightAltPressed = 1,
		LeftAltPressed = 2,
		RightControlPressed = 4,
		LeftControlPressed = 8,
		ShiftPressed = 16,
		NumlockOn = 32,
		ScrolllockOn = 64,
		CapslockOn = 128,
		EnhancedKey = 256
	}

	[Flags]
	public enum EventFlags {
		MouseMoved = 1,
		DoubleClick = 2,
		MouseWheeled = 4,
		MouseHorizontalWheeled = 8
	}

	[StructLayout (LayoutKind.Explicit)]
	public struct MouseEventRecord {
		[FieldOffset (0)]
		public Coord MousePosition;
		[FieldOffset (4)]
		public ButtonState ButtonState;
		[FieldOffset (8)]
		public ControlKeyState ControlKeyState;
		[FieldOffset (12)]
		public EventFlags EventFlags;

		public override readonly string ToString () => $"[Mouse({MousePosition},{ButtonState},{ControlKeyState},{EventFlags}";

	}

	public struct WindowBufferSizeRecord {
		public Coord _size;

		public WindowBufferSizeRecord (short x, short y)
		{
			_size = new Coord (x, y);
		}

		public override readonly string ToString () => $"[WindowBufferSize{_size}";
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct MenuEventRecord {
		public uint dwCommandId;
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct FocusEventRecord {
		public uint bSetFocus;
	}

	public enum EventType : ushort {
		Focus = 0x10,
		Key = 0x1,
		Menu = 0x8,
		Mouse = 2,
		WindowBufferSize = 4
	}

	[StructLayout (LayoutKind.Explicit)]
	public struct InputRecord {
		[FieldOffset (0)]
		public EventType EventType;
		[FieldOffset (4)]
		public KeyEventRecord KeyEvent;
		[FieldOffset (4)]
		public MouseEventRecord MouseEvent;
		[FieldOffset (4)]
		public WindowBufferSizeRecord WindowBufferSizeEvent;
		[FieldOffset (4)]
		public MenuEventRecord MenuEvent;
		[FieldOffset (4)]
		public FocusEventRecord FocusEvent;

		public override readonly string ToString ()
		{
			return EventType switch {
				EventType.Focus => FocusEvent.ToString (),
				EventType.Key => KeyEvent.ToString (),
				EventType.Menu => MenuEvent.ToString (),
				EventType.Mouse => MouseEvent.ToString (),
				EventType.WindowBufferSize => WindowBufferSizeEvent.ToString (),
				_ => "Unknown event type: " + EventType
			};
		}
	};

	[Flags]
	enum ShareMode : uint {
		FileShareRead = 1,
		FileShareWrite = 2,
	}

	[Flags]
	enum DesiredAccess : uint {
		GenericRead = 2147483648,
		GenericWrite = 1073741824,
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct ConsoleScreenBufferInfo {
		public Coord dwSize;
		public Coord dwCursorPosition;
		public ushort wAttributes;
		public SmallRect srWindow;
		public Coord dwMaximumWindowSize;
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct Coord {
		public short X;
		public short Y;

		public Coord (short x, short y)
		{
			X = x;
			Y = y;
		}
		public override readonly string ToString () => $"({X},{Y})";
	};

	[StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	public struct CharUnion {
		[FieldOffset (0)] public char UnicodeChar;
		[FieldOffset (0)] public byte AsciiChar;
	}

	[StructLayout (LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	public struct CharInfo {
		[FieldOffset (0)] public CharUnion Char;
		[FieldOffset (2)] public ushort Attributes;
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct SmallRect {
		public short Left;
		public short Top;
		public short Right;
		public short Bottom;

		public SmallRect (short left, short top, short right, short bottom)
		{
			Left = left;
			Top = top;
			Right = right;
			Bottom = bottom;
		}

		public static void MakeEmpty (ref SmallRect rect)
		{
			rect.Left = -1;
		}

		public static void Update (ref SmallRect rect, short col, short row)
		{
			if (rect.Left == -1) {
				rect.Left = rect.Right = col;
				rect.Bottom = rect.Top = row;
				return;
			}
			if (col >= rect.Left && col <= rect.Right && row >= rect.Top && row <= rect.Bottom)
				return;
			if (col < rect.Left)
				rect.Left = col;
			if (col > rect.Right)
				rect.Right = col;
			if (row < rect.Top)
				rect.Top = row;
			if (row > rect.Bottom)
				rect.Bottom = row;
		}

		public override readonly string ToString () => $"Left={Left},Top={Top},Right={Right},Bottom={Bottom}";
	}

	[StructLayout (LayoutKind.Sequential)]
	public struct ConsoleKeyInfoEx {
		public ConsoleKeyInfo ConsoleKeyInfo;
		public bool CapsLock;
		public bool NumLock;
		public bool ScrollLock;

		public ConsoleKeyInfoEx (ConsoleKeyInfo consoleKeyInfo, bool capslock, bool numlock, bool scrolllock)
		{
			ConsoleKeyInfo = consoleKeyInfo;
			CapsLock = capslock;
			NumLock = numlock;
			ScrollLock = scrolllock;
		}
	}

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern IntPtr GetStdHandle (int nStdHandle);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern bool CloseHandle (IntPtr handle);

	[DllImport ("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
	public static extern bool ReadConsoleInput (
		IntPtr hConsoleInput,
		IntPtr lpBuffer,
		uint nLength,
		out uint lpNumberOfEventsRead);

	[DllImport ("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	static extern bool ReadConsoleOutput (
		IntPtr hConsoleOutput,
		[Out] CharInfo [] lpBuffer,
		Coord dwBufferSize,
		Coord dwBufferCoord,
		ref SmallRect lpReadRegion
	);

	// TODO: This API is obsolete. See https://learn.microsoft.com/en-us/windows/console/writeconsoleoutput
	[DllImport ("kernel32.dll", EntryPoint = "WriteConsoleOutputW", SetLastError = true, CharSet = CharSet.Unicode)]
	static extern bool WriteConsoleOutput (
		IntPtr hConsoleOutput,
		CharInfo [] lpBuffer,
		Coord dwBufferSize,
		Coord dwBufferCoord,
		ref SmallRect lpWriteRegion
	);

	[DllImport ("kernel32.dll")]
	static extern bool SetConsoleCursorPosition (IntPtr hConsoleOutput, Coord dwCursorPosition);

	[StructLayout (LayoutKind.Sequential)]
	public struct ConsoleCursorInfo {
		public uint dwSize;
		public bool bVisible;
	}

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern bool SetConsoleCursorInfo (IntPtr hConsoleOutput, [In] ref ConsoleCursorInfo lpConsoleCursorInfo);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern bool GetConsoleCursorInfo (IntPtr hConsoleOutput, out ConsoleCursorInfo lpConsoleCursorInfo);

	[DllImport ("kernel32.dll")]
	static extern bool GetConsoleMode (IntPtr hConsoleHandle, out uint lpMode);

	[DllImport ("kernel32.dll")]
	static extern bool SetConsoleMode (IntPtr hConsoleHandle, uint dwMode);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern IntPtr CreateConsoleScreenBuffer (
		DesiredAccess dwDesiredAccess,
		ShareMode dwShareMode,
		IntPtr secutiryAttributes,
		uint flags,
		IntPtr screenBufferData
	);

	internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr (-1);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern bool SetConsoleActiveScreenBuffer (IntPtr Handle);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern bool GetNumberOfConsoleInputEvents (IntPtr handle, out uint lpcNumberOfEvents);

	public InputRecord [] ReadConsoleInput ()
	{
		const int bufferSize = 1;
		var pRecord = Marshal.AllocHGlobal (Marshal.SizeOf<InputRecord> () * bufferSize);
		try {
			ReadConsoleInput (_inputHandle, pRecord, bufferSize,
				out var numberEventsRead);

			return numberEventsRead == 0
				? null
				: new [] { Marshal.PtrToStructure<InputRecord> (pRecord) };
		} catch (Exception) {
			return null;
		} finally {
			Marshal.FreeHGlobal (pRecord);
		}
	}

#if false      // Not needed on the constructor. Perhaps could be used on resizing. To study.                                                                                     
		[DllImport ("kernel32.dll", ExactSpelling = true)]
		static extern IntPtr GetConsoleWindow ();

		[DllImport ("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		static extern bool ShowWindow (IntPtr hWnd, int nCmdShow);

		public const int HIDE = 0;
		public const int MAXIMIZE = 3;
		public const int MINIMIZE = 6;
		public const int RESTORE = 9;

		internal void ShowWindow (int state)
		{
			IntPtr thisConsole = GetConsoleWindow ();
			ShowWindow (thisConsole, state);
		}
#endif
	// See: https://github.com/gui-cs/Terminal.Gui/issues/357

	[StructLayout (LayoutKind.Sequential)]
	public struct CONSOLE_SCREEN_BUFFER_INFOEX {
		public uint cbSize;
		public Coord dwSize;
		public Coord dwCursorPosition;
		public ushort wAttributes;
		public SmallRect srWindow;
		public Coord dwMaximumWindowSize;
		public ushort wPopupAttributes;
		public bool bFullscreenSupported;

		[MarshalAs (UnmanagedType.ByValArray, SizeConst = 16)]
		public COLORREF [] ColorTable;
	}

	[StructLayout (LayoutKind.Explicit, Size = 4)]
	public struct COLORREF {
		public COLORREF (byte r, byte g, byte b)
		{
			Value = 0;
			R = r;
			G = g;
			B = b;
		}

		public COLORREF (uint value)
		{
			R = 0;
			G = 0;
			B = 0;
			Value = value & 0x00FFFFFF;
		}

		[FieldOffset (0)]
		public byte R;
		[FieldOffset (1)]
		public byte G;
		[FieldOffset (2)]
		public byte B;

		[FieldOffset (0)]
		public uint Value;
	}

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern bool GetConsoleScreenBufferInfoEx (IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX csbi);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern bool SetConsoleScreenBufferInfoEx (IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFOEX ConsoleScreenBufferInfo);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern bool SetConsoleWindowInfo (
		IntPtr hConsoleOutput,
		bool bAbsolute,
		[In] ref SmallRect lpConsoleWindow);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern Coord GetLargestConsoleWindowSize (
		IntPtr hConsoleOutput);
}

internal class WindowsDriver : ConsoleDriver {
	WindowsConsole.CharInfo [] _outputBuffer;
	WindowsConsole.SmallRect _damageRegion;
	Action<KeyEvent> _keyHandler;
	Action<KeyEvent> _keyDownHandler;
	Action<KeyEvent> _keyUpHandler;
	Action<MouseEvent> _mouseHandler;

	public WindowsConsole WinConsole { get; private set; }

	public WindowsDriver ()
	{
		WinConsole = new WindowsConsole ();
		Clipboard = new WindowsClipboard ();
	}

	public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler)
	{
		_keyHandler = keyHandler;
		_keyDownHandler = keyDownHandler;
		_keyUpHandler = keyUpHandler;
		_mouseHandler = mouseHandler;

		var mLoop = mainLoop.Driver as WindowsMainLoop;

		mLoop.ProcessInput = (e) => ProcessInput (e);

		mLoop.WinChanged = (s, e) => {
			ChangeWin (e.Size);
		};
	}

	private void ChangeWin (Size e)
	{
		if (!EnableConsoleScrolling) {
			var w = e.Width;
			if (w == Cols - 3 && e.Height < Rows) {
				w += 3;
			}
			var newSize = WinConsole.SetConsoleWindow (
				(short)Math.Max (w, 16), (short)Math.Max (e.Height, 0));
			Left = 0;
			Top = 0;
			Cols = newSize.Width;
			Rows = newSize.Height;
			ResizeScreen ();
			UpdateOffScreen ();
			TerminalResized.Invoke ();
		}
	}

	void ProcessInput (WindowsConsole.InputRecord inputEvent)
	{
		switch (inputEvent.EventType) {
		case WindowsConsole.EventType.Key:
			var fromPacketKey = inputEvent.KeyEvent.wVirtualKeyCode == (uint)ConsoleKey.Packet;
			if (fromPacketKey) {
				inputEvent.KeyEvent = FromVKPacketToKeyEventRecord (inputEvent.KeyEvent);
			}
			var map = MapKey (ToConsoleKeyInfoEx (inputEvent.KeyEvent));
			//var ke = inputEvent.KeyEvent;
			//System.Diagnostics.Debug.WriteLine ($"fromPacketKey: {fromPacketKey}");
			//if (ke.UnicodeChar == '\0') {
			//	System.Diagnostics.Debug.WriteLine ("UnicodeChar: 0'\\0'");
			//} else if (ke.UnicodeChar == 13) {
			//	System.Diagnostics.Debug.WriteLine ("UnicodeChar: 13'\\n'");
			//} else {
			//	System.Diagnostics.Debug.WriteLine ($"UnicodeChar: {(uint)ke.UnicodeChar}'{ke.UnicodeChar}'");
			//}
			//System.Diagnostics.Debug.WriteLine ($"bKeyDown: {ke.bKeyDown}");
			//System.Diagnostics.Debug.WriteLine ($"dwControlKeyState: {ke.dwControlKeyState}");
			//System.Diagnostics.Debug.WriteLine ($"wRepeatCount: {ke.wRepeatCount}");
			//System.Diagnostics.Debug.WriteLine ($"wVirtualKeyCode: {ke.wVirtualKeyCode}");
			//System.Diagnostics.Debug.WriteLine ($"wVirtualScanCode: {ke.wVirtualScanCode}");

			if (map == (Key)0xffffffff) {
				KeyEvent key = new KeyEvent ();

				// Shift = VK_SHIFT = 0x10
				// Ctrl = VK_CONTROL = 0x11
				// Alt = VK_MENU = 0x12

				if (inputEvent.KeyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.CapslockOn)) {
					inputEvent.KeyEvent.dwControlKeyState &= ~WindowsConsole.ControlKeyState.CapslockOn;
				}

				if (inputEvent.KeyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.ScrolllockOn)) {
					inputEvent.KeyEvent.dwControlKeyState &= ~WindowsConsole.ControlKeyState.ScrolllockOn;
				}

				if (inputEvent.KeyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.NumlockOn)) {
					inputEvent.KeyEvent.dwControlKeyState &= ~WindowsConsole.ControlKeyState.NumlockOn;
				}

				switch (inputEvent.KeyEvent.dwControlKeyState) {
				case WindowsConsole.ControlKeyState.RightAltPressed:
				case WindowsConsole.ControlKeyState.RightAltPressed |
					WindowsConsole.ControlKeyState.LeftControlPressed |
					WindowsConsole.ControlKeyState.EnhancedKey:
				case WindowsConsole.ControlKeyState.EnhancedKey:
					key = new KeyEvent (Key.CtrlMask | Key.AltMask, _keyModifiers);
					break;
				case WindowsConsole.ControlKeyState.LeftAltPressed:
					key = new KeyEvent (Key.AltMask, _keyModifiers);
					break;
				case WindowsConsole.ControlKeyState.RightControlPressed:
				case WindowsConsole.ControlKeyState.LeftControlPressed:
					key = new KeyEvent (Key.CtrlMask, _keyModifiers);
					break;
				case WindowsConsole.ControlKeyState.ShiftPressed:
					key = new KeyEvent (Key.ShiftMask, _keyModifiers);
					break;
				case WindowsConsole.ControlKeyState.NumlockOn:
					break;
				case WindowsConsole.ControlKeyState.ScrolllockOn:
					break;
				case WindowsConsole.ControlKeyState.CapslockOn:
					break;
				default:
					key = inputEvent.KeyEvent.wVirtualKeyCode switch {
						0x10 => new KeyEvent (Key.ShiftMask, _keyModifiers),
						0x11 => new KeyEvent (Key.CtrlMask, _keyModifiers),
						0x12 => new KeyEvent (Key.AltMask, _keyModifiers),
						_ => new KeyEvent (Key.Unknown, _keyModifiers)
					};
					break;
				}

				if (inputEvent.KeyEvent.bKeyDown) {
					_keyDownHandler (key);
				} else {
					_keyUpHandler (key);
				}
			} else {
				if (inputEvent.KeyEvent.bKeyDown) {
					// May occurs using SendKeys
					_keyModifiers ??= new KeyModifiers ();
					// Key Down - Fire KeyDown Event and KeyStroke (ProcessKey) Event
					_keyDownHandler (new KeyEvent (map, _keyModifiers));
					_keyHandler (new KeyEvent (map, _keyModifiers));
				} else {
					_keyUpHandler (new KeyEvent (map, _keyModifiers));
				}
			}
			if (!inputEvent.KeyEvent.bKeyDown && inputEvent.KeyEvent.dwControlKeyState == 0) {
				_keyModifiers = null;
			}
			break;

		case WindowsConsole.EventType.Mouse:
			var me = ToDriverMouse (inputEvent.MouseEvent);
			_mouseHandler (me);
			if (_processButtonClick) {
				_mouseHandler (
					new MouseEvent () {
						X = me.X,
						Y = me.Y,
						Flags = ProcessButtonClick (inputEvent.MouseEvent)
					});
			}
			break;

		case WindowsConsole.EventType.WindowBufferSize:
			var winSize = WinConsole.GetConsoleBufferWindow (out Point pos);
			Left = pos.X;
			Top = pos.Y;
			Cols = inputEvent.WindowBufferSizeEvent._size.X;
			if (EnableConsoleScrolling) {
				Rows = Math.Max (inputEvent.WindowBufferSizeEvent._size.Y, Rows);
			} else {
				Rows = inputEvent.WindowBufferSizeEvent._size.Y;
			}
			//System.Diagnostics.Debug.WriteLine ($"{EnableConsoleScrolling},{cols},{rows}");
			ResizeScreen ();
			UpdateOffScreen ();
			TerminalResized?.Invoke ();
			break;

		case WindowsConsole.EventType.Focus:
			break;
		}
	}

	WindowsConsole.ButtonState? _lastMouseButtonPressed = null;
	bool _isButtonPressed = false;
	bool _isButtonReleased = false;
	bool _isButtonDoubleClicked = false;
	Point? _point;
	Point _pointMove;
	//int _buttonPressedCount;
	bool _isOneFingerDoubleClicked = false;
	bool _processButtonClick;

	MouseEvent ToDriverMouse (WindowsConsole.MouseEventRecord mouseEvent)
	{
		MouseFlags mouseFlag = MouseFlags.AllEvents;

		//System.Diagnostics.Debug.WriteLine (
		//	$"X:{mouseEvent.MousePosition.X};Y:{mouseEvent.MousePosition.Y};ButtonState:{mouseEvent.ButtonState};EventFlags:{mouseEvent.EventFlags}");

		if (_isButtonDoubleClicked || _isOneFingerDoubleClicked) {
			Application.MainLoop.AddIdle (() => {
				Task.Run (async () => await ProcessButtonDoubleClickedAsync ());
				return false;
			});
		}

		// The ButtonState member of the MouseEvent structure has bit corresponding to each mouse button.
		// This will tell when a mouse button is pressed. When the button is released this event will
		// be fired with it's bit set to 0. So when the button is up ButtonState will be 0.
		// To map to the correct driver events we save the last pressed mouse button so we can
		// map to the correct clicked event.
		if ((_lastMouseButtonPressed != null || _isButtonReleased) && mouseEvent.ButtonState != 0) {
			_lastMouseButtonPressed = null;
			//isButtonPressed = false;
			_isButtonReleased = false;
		}

		var p = new Point () {
			X = mouseEvent.MousePosition.X,
			Y = mouseEvent.MousePosition.Y
		};

		if ((mouseEvent.ButtonState != 0 && mouseEvent.EventFlags == 0 && _lastMouseButtonPressed == null && !_isButtonDoubleClicked) ||
			 (_lastMouseButtonPressed == null && mouseEvent.EventFlags.HasFlag (WindowsConsole.EventFlags.MouseMoved) &&
			 mouseEvent.ButtonState != 0 && !_isButtonReleased && !_isButtonDoubleClicked)) {
			switch (mouseEvent.ButtonState) {
			case WindowsConsole.ButtonState.Button1Pressed:
				mouseFlag = MouseFlags.Button1Pressed;
				break;

			case WindowsConsole.ButtonState.Button2Pressed:
				mouseFlag = MouseFlags.Button2Pressed;
				break;

			case WindowsConsole.ButtonState.RightmostButtonPressed:
				mouseFlag = MouseFlags.Button3Pressed;
				break;
			}

			if (_point == null) {
				_point = p;
			}

			if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved) {
				mouseFlag |= MouseFlags.ReportMousePosition;
				_isButtonReleased = false;
				_processButtonClick = false;
			}
			_lastMouseButtonPressed = mouseEvent.ButtonState;
			_isButtonPressed = true;

			if ((mouseFlag & MouseFlags.ReportMousePosition) == 0) {
				Application.MainLoop.AddIdle (() => {
					Task.Run (async () => await ProcessContinuousButtonPressedAsync (mouseFlag));
					return false;
				});
			}

		} else if (_lastMouseButtonPressed != null && mouseEvent.EventFlags == 0
			&& !_isButtonReleased && !_isButtonDoubleClicked && !_isOneFingerDoubleClicked) {
			switch (_lastMouseButtonPressed) {
			case WindowsConsole.ButtonState.Button1Pressed:
				mouseFlag = MouseFlags.Button1Released;
				break;

			case WindowsConsole.ButtonState.Button2Pressed:
				mouseFlag = MouseFlags.Button2Released;
				break;

			case WindowsConsole.ButtonState.RightmostButtonPressed:
				mouseFlag = MouseFlags.Button3Released;
				break;
			}
			_isButtonPressed = false;
			_isButtonReleased = true;
			if (_point != null && (((Point)_point).X == mouseEvent.MousePosition.X && ((Point)_point).Y == mouseEvent.MousePosition.Y)) {
				_processButtonClick = true;
			} else {
				_point = null;
			}
		} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved
			&& !_isOneFingerDoubleClicked && _isButtonReleased && p == _point) {

			mouseFlag = ProcessButtonClick (mouseEvent);

		} else if (mouseEvent.EventFlags.HasFlag (WindowsConsole.EventFlags.DoubleClick)) {
			switch (mouseEvent.ButtonState) {
			case WindowsConsole.ButtonState.Button1Pressed:
				mouseFlag = MouseFlags.Button1DoubleClicked;
				break;

			case WindowsConsole.ButtonState.Button2Pressed:
				mouseFlag = MouseFlags.Button2DoubleClicked;
				break;

			case WindowsConsole.ButtonState.RightmostButtonPressed:
				mouseFlag = MouseFlags.Button3DoubleClicked;
				break;
			}
			_isButtonDoubleClicked = true;
		} else if (mouseEvent.EventFlags == 0 && mouseEvent.ButtonState != 0 && _isButtonDoubleClicked) {
			switch (mouseEvent.ButtonState) {
			case WindowsConsole.ButtonState.Button1Pressed:
				mouseFlag = MouseFlags.Button1TripleClicked;
				break;

			case WindowsConsole.ButtonState.Button2Pressed:
				mouseFlag = MouseFlags.Button2TripleClicked;
				break;

			case WindowsConsole.ButtonState.RightmostButtonPressed:
				mouseFlag = MouseFlags.Button3TripleClicked;
				break;
			}
			_isButtonDoubleClicked = false;
		} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseWheeled) {
			switch ((int)mouseEvent.ButtonState) {
			case int v when v > 0:
				mouseFlag = MouseFlags.WheeledUp;
				break;

			case int v when v < 0:
				mouseFlag = MouseFlags.WheeledDown;
				break;
			}

		} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseWheeled &&
			mouseEvent.ControlKeyState == WindowsConsole.ControlKeyState.ShiftPressed) {
			switch ((int)mouseEvent.ButtonState) {
			case int v when v > 0:
				mouseFlag = MouseFlags.WheeledLeft;
				break;

			case int v when v < 0:
				mouseFlag = MouseFlags.WheeledRight;
				break;
			}

		} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseHorizontalWheeled) {
			switch ((int)mouseEvent.ButtonState) {
			case int v when v < 0:
				mouseFlag = MouseFlags.WheeledLeft;
				break;

			case int v when v > 0:
				mouseFlag = MouseFlags.WheeledRight;
				break;
			}

		} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved) {
			mouseFlag = MouseFlags.ReportMousePosition;
			if (mouseEvent.MousePosition.X != _pointMove.X || mouseEvent.MousePosition.Y != _pointMove.Y) {
				_pointMove = new Point (mouseEvent.MousePosition.X, mouseEvent.MousePosition.Y);
			}
		} else if (mouseEvent.ButtonState == 0 && mouseEvent.EventFlags == 0) {
			mouseFlag = 0;
		}

		mouseFlag = SetControlKeyStates (mouseEvent, mouseFlag);

		//System.Diagnostics.Debug.WriteLine (
		//	$"point.X:{(point != null ? ((Point)point).X : -1)};point.Y:{(point != null ? ((Point)point).Y : -1)}");

		return new MouseEvent () {
			X = mouseEvent.MousePosition.X,
			Y = mouseEvent.MousePosition.Y,
			Flags = mouseFlag
		};
	}

	MouseFlags ProcessButtonClick (WindowsConsole.MouseEventRecord mouseEvent)
	{
		MouseFlags mouseFlag = 0;
		switch (_lastMouseButtonPressed) {
		case WindowsConsole.ButtonState.Button1Pressed:
			mouseFlag = MouseFlags.Button1Clicked;
			break;

		case WindowsConsole.ButtonState.Button2Pressed:
			mouseFlag = MouseFlags.Button2Clicked;
			break;

		case WindowsConsole.ButtonState.RightmostButtonPressed:
			mouseFlag = MouseFlags.Button3Clicked;
			break;
		}
		_point = new Point () {
			X = mouseEvent.MousePosition.X,
			Y = mouseEvent.MousePosition.Y
		};
		_lastMouseButtonPressed = null;
		_isButtonReleased = false;
		_processButtonClick = false;
		_point = null;
		return mouseFlag;
	}

	async Task ProcessButtonDoubleClickedAsync ()
	{
		await Task.Delay (300);
		_isButtonDoubleClicked = false;
		_isOneFingerDoubleClicked = false;
		//buttonPressedCount = 0;
	}

	async Task ProcessContinuousButtonPressedAsync (MouseFlags mouseFlag)
	{
		while (_isButtonPressed) {
			await Task.Delay (100);
			var me = new MouseEvent () {
				X = _pointMove.X,
				Y = _pointMove.Y,
				Flags = mouseFlag
			};

			var view = Application.WantContinuousButtonPressedView;
			if (view == null) {
				break;
			}
			if (_isButtonPressed && (mouseFlag & MouseFlags.ReportMousePosition) == 0) {
				Application.MainLoop.Invoke (() => _mouseHandler (me));
			}
		}
	}

	static MouseFlags SetControlKeyStates (WindowsConsole.MouseEventRecord mouseEvent, MouseFlags mouseFlag)
	{
		if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightControlPressed) ||
			mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftControlPressed)) {
			mouseFlag |= MouseFlags.ButtonCtrl;
		}

		if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.ShiftPressed)) {
			mouseFlag |= MouseFlags.ButtonShift;
		}

		if (mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightAltPressed) ||
			 mouseEvent.ControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftAltPressed)) {
			mouseFlag |= MouseFlags.ButtonAlt;
		}
		return mouseFlag;
	}

	KeyModifiers _keyModifiers;

	public WindowsConsole.ConsoleKeyInfoEx ToConsoleKeyInfoEx (WindowsConsole.KeyEventRecord keyEvent)
	{
		var state = keyEvent.dwControlKeyState;

		var shift = (state & WindowsConsole.ControlKeyState.ShiftPressed) != 0;
		var alt = (state & (WindowsConsole.ControlKeyState.LeftAltPressed | WindowsConsole.ControlKeyState.RightAltPressed)) != 0;
		var control = (state & (WindowsConsole.ControlKeyState.LeftControlPressed | WindowsConsole.ControlKeyState.RightControlPressed)) != 0;
		var capsLock = (state & (WindowsConsole.ControlKeyState.CapslockOn)) != 0;
		var numLock = (state & (WindowsConsole.ControlKeyState.NumlockOn)) != 0;
		var scrollLock = (state & (WindowsConsole.ControlKeyState.ScrolllockOn)) != 0;

		_keyModifiers ??= new KeyModifiers ();
		if (shift) {
			_keyModifiers.Shift = true;
		}
		if (alt) {
			_keyModifiers.Alt = true;
		}
		if (control) {
			_keyModifiers.Ctrl = true;
		}
		if (capsLock) {
			_keyModifiers.Capslock = true;
		}
		if (numLock) {
			_keyModifiers.Numlock = true;
		}
		if (scrollLock) {
			_keyModifiers.Scrolllock = true;
		}

		var consoleKeyInfo = new ConsoleKeyInfo (keyEvent.UnicodeChar, (ConsoleKey)keyEvent.wVirtualKeyCode, shift, alt, control);

		return new WindowsConsole.ConsoleKeyInfoEx (consoleKeyInfo, capsLock, numLock, scrollLock);
	}

	public WindowsConsole.KeyEventRecord FromVKPacketToKeyEventRecord (WindowsConsole.KeyEventRecord keyEvent)
	{
		if (keyEvent.wVirtualKeyCode != (uint)ConsoleKey.Packet) {
			return keyEvent;
		}

		var mod = new ConsoleModifiers ();
		if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.ShiftPressed)) {
			mod |= ConsoleModifiers.Shift;
		}
		if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightAltPressed) ||
			keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftAltPressed)) {
			mod |= ConsoleModifiers.Alt;
		}
		if (keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.LeftControlPressed) ||
			keyEvent.dwControlKeyState.HasFlag (WindowsConsole.ControlKeyState.RightControlPressed)) {
			mod |= ConsoleModifiers.Control;
		}
		var keyChar = ConsoleKeyMapping.GetKeyCharFromConsoleKey (keyEvent.UnicodeChar, mod, out uint virtualKey, out uint scanCode);

		return new WindowsConsole.KeyEventRecord {
			UnicodeChar = (char)keyChar,
			bKeyDown = keyEvent.bKeyDown,
			dwControlKeyState = keyEvent.dwControlKeyState,
			wRepeatCount = keyEvent.wRepeatCount,
			wVirtualKeyCode = (ushort)virtualKey,
			wVirtualScanCode = (ushort)scanCode
		};
	}

	public Key MapKey (WindowsConsole.ConsoleKeyInfoEx keyInfoEx)
	{
		var keyInfo = keyInfoEx.ConsoleKeyInfo;
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

		case ConsoleKey.NumPad0:
			return keyInfoEx.NumLock ? Key.D0 : Key.InsertChar;
		case ConsoleKey.NumPad1:
			return keyInfoEx.NumLock ? Key.D1 : Key.End;
		case ConsoleKey.NumPad2:
			return keyInfoEx.NumLock ? Key.D2 : Key.CursorDown;
		case ConsoleKey.NumPad3:
			return keyInfoEx.NumLock ? Key.D3 : Key.PageDown;
		case ConsoleKey.NumPad4:
			return keyInfoEx.NumLock ? Key.D4 : Key.CursorLeft;
		case ConsoleKey.NumPad5:
			return keyInfoEx.NumLock ? Key.D5 : (Key)((uint)keyInfo.KeyChar);
		case ConsoleKey.NumPad6:
			return keyInfoEx.NumLock ? Key.D6 : Key.CursorRight;
		case ConsoleKey.NumPad7:
			return keyInfoEx.NumLock ? Key.D7 : Key.Home;
		case ConsoleKey.NumPad8:
			return keyInfoEx.NumLock ? Key.D8 : Key.CursorUp;
		case ConsoleKey.NumPad9:
			return keyInfoEx.NumLock ? Key.D9 : Key.PageUp;

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
		//var alphaBase = ((keyInfo.Modifiers == ConsoleModifiers.Shift) ^ (keyInfoEx.CapsLock)) ? 'A' : 'a';

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
				if (keyInfo.KeyChar == 0 || (keyInfo.KeyChar != 0 && keyInfo.KeyChar >= 1 && keyInfo.KeyChar <= 26)) {
					return MapKeyModifiers (keyInfo, (Key)((uint)Key.A + delta));
				}
			}
			//return (Key)((uint)alphaBase + delta);
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

	int GetOutputBufferPosition ()
	{
		return Row * Cols + Col;
	}

	public override void AddRune (Rune systemRune)
	{
		if (IsValidLocation (Col, Row)) {
			var rune = systemRune.MakePrintable ();

			if (!rune.IsBmp) {
				rune = Rune.ReplacementChar;
			}
			Contents [Row, Col, 0] = rune.Value;
			Contents [Row, Col, 1] = CurrentAttribute;

			if (Col > 0) {
				var left = new Rune (Contents [Row, Col - 1, 0]);
				if (left.GetColumns () > 1) {
					Contents [Row, Col - 1, 0] = Rune.ReplacementChar.Value;
				}
			}

			if (rune.GetColumns () > 1 && Col == Clip.Right - 1) {
				// This is a double-width character, and we are at the end of the line.
				Contents [Row, Col, 0] = Rune.ReplacementChar.Value;
				Contents [Row, Col, 1] = CurrentAttribute;
				Contents [Row, Col, 2] = 1;
				Col++;
			}

			//var extraColumns = 0;
			//ReadOnlySpan<char> remainingInput = rune.ToString ().AsSpan ();
			//while (!remainingInput.IsEmpty) {
			//	// Decode
			//	OperationStatus opStatus = Rune.DecodeFromUtf16 (remainingInput, out Rune result, out int charsConsumed);

			//	if (opStatus == OperationStatus.DestinationTooSmall || opStatus == OperationStatus.InvalidData) {
			//		result = Rune.ReplacementChar;
			//	}
			//	Contents [Row, Col + extraColumns, 0] = result.Value;
			//	Contents [Row, Col + extraColumns, 1] = CurrentAttribute;

			//	// Slice and loop again
			//	remainingInput = remainingInput.Slice (charsConsumed);
			//	if (runeWidth > 1) {
			//		extraColumns++;
			//		Contents [Row, Col + extraColumns, 0] = ' ';
			//		Contents [Row, Col + extraColumns, 1] = CurrentAttribute;
			//	}
			//}
		}
		//if (runeWidth == 0 && Col > 0) {
		//		// This is a combining character, and we are not at the beginning of the line.
		//		var combined = new String (new char [] { (char)Contents [Row, Col - 1, 0], (char)rune.Value });
		//		var normalized = !combined.IsNormalized () ? combined.Normalize () : combined;
		//		Contents [Row, Col - 1, 0] = normalized [0];
		//		Contents [Row, Col - 1, 1] = CurrentAttribute;
		//		Contents [Row, Col - 1, 2] = 1;
		//	} else {
		//		Contents [Row, Col, 1] = CurrentAttribute;
		//		Contents [Row, Col, 2] = 1;

		//		if (runeWidth < 2 && Col > 0 && ((Rune)(Contents [Row, Col - 1, 0])).GetColumns () > 1) {
		//			// This is a single-width character, and we are not at the beginning of the line.
		//			Contents [Row, Col - 1, 0] = Rune.ReplacementChar.Value;

		//		} else if (runeWidth < 2 && Col <= Clip.Right - 1 && ((Rune)(Contents [Row, Col, 0])).GetColumns () > 1) {
		//			// This is a single-width character, and we are not at the end of the line.
		//			Contents [Row, Col + 1, 0] = Rune.ReplacementChar.Value;
		//			Contents [Row, Col + 1, 2] = 1;
		//		}

		//		if (runeWidth > 1 && Col == Clip.Right - 1) {
		//			// This is a double-width character, and we are at the end of the line.
		//			Contents [Row, Col, 0] = Rune.ReplacementChar.Value;
		//		} else {
		//			// This is a single-width character, or we are not at the end of the line.
		//			// Add the glyph (wide or not)
		//			//if (rune.IsBmp) {
		//			//	Contents [Row, Col, 0] = rune.Value;
		//			//	_outputBuffer [position].Char.UnicodeChar = (char)rune.Value;
		//			//} else {
		//			var column = Col;
		//			ReadOnlySpan<char> remainingInput = rune.ToString ().AsSpan ();
		//			while (!remainingInput.IsEmpty) {
		//				// Decode
		//				OperationStatus opStatus = Rune.DecodeFromUtf16 (remainingInput, out Rune result, out int charsConsumed);

		//				if (opStatus != OperationStatus.Done) {
		//					result = Rune.ReplacementChar;
		//				}
		//				Contents [Row, column, 0] = result.Value;
		//				Contents [Row, column, 1] = CurrentAttribute;

		//				// Slice and loop again
		//				remainingInput = remainingInput.Slice (charsConsumed);
		//				Col = ++column;
		//			}
		//			//// BUGBUG: workaround #2610
		//			//Contents [Row, Col, 0] = (char)Rune.ReplacementChar.Value;
		//			//_outputBuffer [position].Char.UnicodeChar = (char)Rune.ReplacementChar.Value;
		//			//}
		//		}
		//	}
		//}

		////Col++;
		////if (runeWidth is < 0 or > 0) {
		////	// This is a double-width codepoint, and we are not at the end of the line.
		////	// Move to the right one and...
		////	Col++;
		////}

		////if (runeWidth > 1) {
		////	// This is a double-width character, and we are not at the end of the line.
		////	// ...clear the next cell.
		////	if (validLocation && Col < Clip.Right) {
		////		Contents [Row, Col, 1] = CurrentAttribute;
		////		Contents [Row, Col, 2] = 0;

		////		//position = GetOutputBufferPosition ();
		////		//_outputBuffer [position].Attributes = (ushort)CurrentAttribute;

		////		if (rune.IsBmp) {
		////		//	// BUGBUG: workaround #2610 - put a null char in the second position
		////			//Contents [Row, Col, 0] = (char)0x00;
		////		//	_outputBuffer [position].Char.UnicodeChar = (char)0x00;
		////		}
		////	}
		////	Col++;
		////}
		Col++;

	}

	public override void Init (Action terminalResized)
	{
		TerminalResized = terminalResized;

		try {
			// Needed for Windows Terminal
			// Per Issue #2264 using the alternative screen buffer is required for Windows Terminal to not 
			// wipe out the backscroll buffer when the application exits.
			Console.Out.Write (EscSeqUtils.CSI_ActivateAltBufferNoBackscroll);

			var winSize = WinConsole.GetConsoleOutputWindow (out Point pos);
			Cols = winSize.Width;
			Rows = winSize.Height;
			WindowsConsole.SmallRect.MakeEmpty (ref _damageRegion);

		} catch (Win32Exception) {
			// Likely running unit tests. Set WinConsole to null so we can test it elsewhere.
			WinConsole = null;
			//throw new InvalidOperationException ("The Windows Console output window is not available.", e);
		}

		CurrentAttribute = MakeColor (Color.White, Color.Black);
		InitializeColorSchemes ();

		ResizeScreen ();
		UpdateOffScreen ();
	}

	public virtual void ResizeScreen ()
	{
		if (WinConsole == null) {
			return;
		}

		_outputBuffer = new WindowsConsole.CharInfo [Rows * Cols];
		Clip = new Rect (0, 0, Cols, Rows);
		_damageRegion = new WindowsConsole.SmallRect () {
			Top = 0,
			Left = 0,
			Bottom = (short)Rows,
			Right = (short)Cols
		};
		WinConsole.ForceRefreshCursorVisibility ();
		if (!EnableConsoleScrolling) {
			Console.Out.Write (EscSeqUtils.CSI_ClearScreen (EscSeqUtils.ClearScreenOptions.CursorToEndOfScreen));
		}
	}

	public override void UpdateOffScreen ()
	{
		Contents = new int [Rows, Cols, 3];
		WindowsConsole.SmallRect.MakeEmpty (ref _damageRegion);

		for (int row = 0; row < Rows; row++) {
			for (int col = 0; col < Cols; col++) {
				int position = row * Cols + col;
				_outputBuffer [position].Attributes = (ushort)new Attribute (Color.White, Color.Black).Value; ;
				_outputBuffer [position].Char.UnicodeChar = ' ';
				Contents [row, col, 0] = _outputBuffer [position].Char.UnicodeChar;
				Contents [row, col, 1] = _outputBuffer [position].Attributes;
				Contents [row, col, 2] = 0;
				WindowsConsole.SmallRect.Update (ref _damageRegion, (short)col, (short)row);
			}
		}
	}

	public override void UpdateScreen ()
	{
		//if (_damageRegion.Left == -1) {
		//	return;
		//}

		if (!EnableConsoleScrolling) {
			var windowSize = WinConsole.GetConsoleBufferWindow (out _);
			if (!windowSize.IsEmpty && (windowSize.Width != Cols || windowSize.Height != Rows)) {
				return;
			}
		}

		var bufferCoords = new WindowsConsole.Coord () {
			X = (short)Clip.Width,
			Y = (short)Clip.Height
		};

		// BUGBUG: Temp hack to simplify - copy output buffer here instead of AddRune
		WindowsConsole.SmallRect.MakeEmpty (ref _damageRegion);
		for (var row = 0; row < Rows; row++) {
			for (var col = 0; col < Cols; col++) {
				var rune = new Rune (Contents [row, col, 0]);
				var position = row * Cols + col;
				_outputBuffer [position].Char.UnicodeChar = (char)rune.Value;
				_outputBuffer [position].Attributes = (ushort)Contents [row, col, 1];
				WindowsConsole.SmallRect.Update (ref _damageRegion, (short)col, (short)row);
				var width = rune.GetColumns ();
				while (width > 1 && col < Cols - 1) {
					col++;
					position = row * Cols + col;
					_outputBuffer [position].Char.UnicodeChar = (char)0x00;
					_outputBuffer [position].Attributes = (ushort)Contents [row, col, 1];
					WindowsConsole.SmallRect.Update (ref _damageRegion, (short)col, (short)row);
					width--;
				}
			}
		}

		WinConsole.WriteToConsole (new Size (Cols, Rows), _outputBuffer, bufferCoords, _damageRegion);
		WindowsConsole.SmallRect.MakeEmpty (ref _damageRegion);
	}

	public override void Refresh ()
	{
		UpdateScreen ();
		WinConsole.SetInitialCursorVisibility ();
		UpdateCursor ();
	}

	#region Color Handling

	/// <summary>
	/// In the WindowsDriver, colors are encoded as an int. 
	/// The background color is stored in the least significant 4 bits, 
	/// and the foreground color is stored in the next 4 bits. 
	/// </summary>
	public override Attribute MakeColor (Color foreground, Color background)
	{
		// Encode the colors into the int value.
		return new Attribute (
			value: (((int)foreground) | ((int)background << 4)),
			foreground: foreground,
			background: background
		);
	}

	/// <summary>
	/// Extracts the foreground and background colors from the encoded value.
	/// Assumes a 4-bit encoded value for both foreground and background colors.
	/// </summary>
	public override bool GetColors (int value, out Color foreground, out Color background)
	{
		// Assume a 4-bit encoded value for both foreground and background colors.
		foreground = (Color)((value >> 16) & 0xF);
		background = (Color)(value & 0xF);

		//foreground = (Color)(value & 0xF);
		//background = (Color)((value >> 4) & 0xF);
		return true;
	}

	#endregion

	CursorVisibility savedCursorVisibility;

	public override void UpdateCursor ()
	{
		if (Col < 0 || Row < 0 || Col > Cols || Row > Rows) {
			GetCursorVisibility (out CursorVisibility cursorVisibility);
			savedCursorVisibility = cursorVisibility;
			SetCursorVisibility (CursorVisibility.Invisible);
			return;
		}

		SetCursorVisibility (savedCursorVisibility);
		var position = new WindowsConsole.Coord () {
			X = (short)Col,
			Y = (short)Row
		};
		WinConsole.SetCursorPosition (position);
	}

	/// <inheritdoc/>
	public override bool GetCursorVisibility (out CursorVisibility visibility)
	{
		return WinConsole.GetCursorVisibility (out visibility);
	}

	/// <inheritdoc/>
	public override bool SetCursorVisibility (CursorVisibility visibility)
	{
		savedCursorVisibility = visibility;
		return WinConsole.SetCursorVisibility (visibility);
	}

	/// <inheritdoc/>
	public override bool EnsureCursorVisibility ()
	{
		return WinConsole.EnsureCursorVisibility ();
	}

	public override void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
	{
		WindowsConsole.InputRecord input = new WindowsConsole.InputRecord {
			EventType = WindowsConsole.EventType.Key
		};

		WindowsConsole.KeyEventRecord keyEvent = new WindowsConsole.KeyEventRecord {
			bKeyDown = true
		};
		WindowsConsole.ControlKeyState controlKey = new WindowsConsole.ControlKeyState ();
		if (shift) {
			controlKey |= WindowsConsole.ControlKeyState.ShiftPressed;
			keyEvent.UnicodeChar = '\0';
			keyEvent.wVirtualKeyCode = 16;
		}
		if (alt) {
			controlKey |= WindowsConsole.ControlKeyState.LeftAltPressed;
			controlKey |= WindowsConsole.ControlKeyState.RightAltPressed;
			keyEvent.UnicodeChar = '\0';
			keyEvent.wVirtualKeyCode = 18;
		}
		if (control) {
			controlKey |= WindowsConsole.ControlKeyState.LeftControlPressed;
			controlKey |= WindowsConsole.ControlKeyState.RightControlPressed;
			keyEvent.UnicodeChar = '\0';
			keyEvent.wVirtualKeyCode = 17;
		}
		keyEvent.dwControlKeyState = controlKey;

		input.KeyEvent = keyEvent;

		if (shift || alt || control) {
			ProcessInput (input);
		}

		keyEvent.UnicodeChar = keyChar;
		if ((uint)key < 255) {
			keyEvent.wVirtualKeyCode = (ushort)key;
		} else {
			keyEvent.wVirtualKeyCode = '\0';
		}

		input.KeyEvent = keyEvent;

		try {
			ProcessInput (input);
		} catch (OverflowException) { } finally {
			keyEvent.bKeyDown = false;
			input.KeyEvent = keyEvent;
			ProcessInput (input);
		}
	}

	public override void End ()
	{
		WinConsole.Cleanup ();
		WinConsole = null;

		// Needed for Windows Terminal
		// Clear the alternative screen buffer from the cursor to the
		// end of the screen.
		// Note, [3J causes Windows Terminal to wipe out the entire NON ALTERNATIVE
		// backbuffer! So we need to use [0J instead.
		Console.Out.Write (EscSeqUtils.CSI_ClearScreen(0));

		// Disable alternative screen buffer.
		Console.Out.Write (EscSeqUtils.CSI_RestoreAltBufferWithBackscroll);
	}

	#region Not Implemented
	public override void Suspend ()
	{
		throw new NotImplementedException ();
	}
	#endregion
}

/// <summary>
/// Mainloop intended to be used with the <see cref="WindowsDriver"/>, and can
/// only be used on Windows.
/// </summary>
/// <remarks>
/// This implementation is used for WindowsDriver.
/// </remarks>
internal class WindowsMainLoop : IMainLoopDriver {
	ManualResetEventSlim _eventReady = new ManualResetEventSlim (false);
	ManualResetEventSlim _waitForProbe = new ManualResetEventSlim (false);
	ManualResetEventSlim _winChange = new ManualResetEventSlim (false);
	MainLoop _mainLoop;
	ConsoleDriver _consoleDriver;
	WindowsConsole _winConsole;
	bool _winChanged;
	Size _windowSize;
	CancellationTokenSource _tokenSource = new CancellationTokenSource ();

	// The records that we keep fetching
	Queue<WindowsConsole.InputRecord []> _resultQueue = new Queue<WindowsConsole.InputRecord []> ();

	/// <summary>
	/// Invoked when a Key is pressed or released.
	/// </summary>
	public Action<WindowsConsole.InputRecord> ProcessInput;

	/// <summary>
	/// Invoked when the window is changed.
	/// </summary>
	public EventHandler<SizeChangedEventArgs> WinChanged;

	public WindowsMainLoop (ConsoleDriver consoleDriver = null)
	{
		_consoleDriver = consoleDriver ?? throw new ArgumentNullException ("Console driver instance must be provided.");
		_winConsole = ((WindowsDriver)consoleDriver).WinConsole;
	}

	void IMainLoopDriver.Setup (MainLoop mainLoop)
	{
		_mainLoop = mainLoop;
		Task.Run (WindowsInputHandler);
		Task.Run (CheckWinChange);
	}

	void WindowsInputHandler ()
	{
		while (true) {
			_waitForProbe.Wait ();
			_waitForProbe.Reset ();

			if (_resultQueue?.Count == 0) {
				_resultQueue.Enqueue (_winConsole.ReadConsoleInput ());
			}

			_eventReady.Set ();
		}
	}

	void CheckWinChange ()
	{
		while (true) {
			_winChange.Wait ();
			_winChange.Reset ();
			WaitWinChange ();
			_winChanged = true;
			_eventReady.Set ();
		}
	}

	void WaitWinChange ()
	{
		while (true) {
			Thread.Sleep (100);
			if (!_consoleDriver.EnableConsoleScrolling) {
				_windowSize = _winConsole.GetConsoleBufferWindow (out _);
				//System.Diagnostics.Debug.WriteLine ($"{consoleDriver.EnableConsoleScrolling},{windowSize.Width},{windowSize.Height}");
				if (_windowSize != Size.Empty && _windowSize.Width != _consoleDriver.Cols
					|| _windowSize.Height != _consoleDriver.Rows) {
					return;
				}
			}
		}
	}

	void IMainLoopDriver.Wakeup ()
	{
		//tokenSource.Cancel ();
		_eventReady.Set ();
	}

	bool IMainLoopDriver.EventsPending (bool wait)
	{
		_waitForProbe.Set ();
		_winChange.Set ();

		if (CheckTimers (wait, out var waitTimeout)) {
			return true;
		}

		try {
			if (!_tokenSource.IsCancellationRequested) {
				_eventReady.Wait (waitTimeout, _tokenSource.Token);
			}
		} catch (OperationCanceledException) {
			return true;
		} finally {
			_eventReady.Reset ();
		}

		if (!_tokenSource.IsCancellationRequested) {
			return _resultQueue.Count > 0 || CheckTimers (wait, out _) || _winChanged;
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
			if (waitTimeout < 0)
				return true;
		} else {
			waitTimeout = -1;
		}

		if (!wait)
			waitTimeout = 0;

		int ic;
		lock (_mainLoop.idleHandlers) {
			ic = _mainLoop.idleHandlers.Count;
		}

		return ic > 0;
	}

	void IMainLoopDriver.Iteration ()
	{
		while (_resultQueue.Count > 0) {
			var inputRecords = _resultQueue.Dequeue ();
			if (inputRecords != null && inputRecords.Length > 0) {
				var inputEvent = inputRecords [0];
				ProcessInput?.Invoke (inputEvent);
			}
		}
		if (_winChanged) {
			_winChanged = false;
			WinChanged?.Invoke (this, new SizeChangedEventArgs (_windowSize));
		}
	}
}

class WindowsClipboard : ClipboardBase {
	public WindowsClipboard ()
	{
		IsSupported = IsClipboardFormatAvailable (_cfUnicodeText);
	}

	public override bool IsSupported { get; }

	protected override string GetClipboardDataImpl ()
	{
		try {
			if (!OpenClipboard (IntPtr.Zero)) {
				return string.Empty;
			}

			IntPtr handle = GetClipboardData (_cfUnicodeText);
			if (handle == IntPtr.Zero) {
				return string.Empty;
			}

			IntPtr pointer = IntPtr.Zero;

			try {
				pointer = GlobalLock (handle);
				if (pointer == IntPtr.Zero) {
					return string.Empty;
				}

				int size = GlobalSize (handle);
				byte [] buff = new byte [size];

				Marshal.Copy (pointer, buff, 0, size);

				return System.Text.Encoding.Unicode.GetString (buff).TrimEnd ('\0');
			} finally {
				if (pointer != IntPtr.Zero) {
					GlobalUnlock (handle);
				}
			}
		} finally {
			CloseClipboard ();
		}
	}

	protected override void SetClipboardDataImpl (string text)
	{
		OpenClipboard ();

		EmptyClipboard ();
		IntPtr hGlobal = default;
		try {
			var bytes = (text.Length + 1) * 2;
			hGlobal = Marshal.AllocHGlobal (bytes);

			if (hGlobal == default) {
				ThrowWin32 ();
			}

			var target = GlobalLock (hGlobal);

			if (target == default) {
				ThrowWin32 ();
			}

			try {
				Marshal.Copy (text.ToCharArray (), 0, target, text.Length);
			} finally {
				GlobalUnlock (target);
			}

			if (SetClipboardData (_cfUnicodeText, hGlobal) == default) {
				ThrowWin32 ();
			}

			hGlobal = default;
		} finally {
			if (hGlobal != default) {
				Marshal.FreeHGlobal (hGlobal);
			}

			CloseClipboard ();
		}
	}

	void OpenClipboard ()
	{
		var num = 10;
		while (true) {
			if (OpenClipboard (default)) {
				break;
			}

			if (--num == 0) {
				ThrowWin32 ();
			}

			Thread.Sleep (100);
		}
	}

	const uint _cfUnicodeText = 13;

	void ThrowWin32 ()
	{
		throw new Win32Exception (Marshal.GetLastWin32Error ());
	}

	[DllImport ("User32.dll", SetLastError = true)]
	[return: MarshalAs (UnmanagedType.Bool)]
	static extern bool IsClipboardFormatAvailable (uint format);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern int GlobalSize (IntPtr handle);

	[DllImport ("kernel32.dll", SetLastError = true)]
	static extern IntPtr GlobalLock (IntPtr hMem);

	[DllImport ("kernel32.dll", SetLastError = true)]
	[return: MarshalAs (UnmanagedType.Bool)]
	static extern bool GlobalUnlock (IntPtr hMem);

	[DllImport ("user32.dll", SetLastError = true)]
	[return: MarshalAs (UnmanagedType.Bool)]
	static extern bool OpenClipboard (IntPtr hWndNewOwner);

	[DllImport ("user32.dll", SetLastError = true)]
	[return: MarshalAs (UnmanagedType.Bool)]
	static extern bool CloseClipboard ();

	[DllImport ("user32.dll", SetLastError = true)]
	static extern IntPtr SetClipboardData (uint uFormat, IntPtr data);

	[DllImport ("user32.dll")]
	static extern bool EmptyClipboard ();

	[DllImport ("user32.dll", SetLastError = true)]
	static extern IntPtr GetClipboardData (uint uFormat);
}