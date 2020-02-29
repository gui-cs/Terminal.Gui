//
// WindowsDriver.cs: Windows specific driver 
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//   Nick Van Dyck (vandyck.nick@outlook.com)
//
// Copyright (c) 2018
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Mono.Terminal;
using NStack;

namespace Terminal.Gui {

	internal class WindowsConsole {
		public const int STD_OUTPUT_HANDLE = -11;
		public const int STD_INPUT_HANDLE = -10;
		public const int STD_ERROR_HANDLE = -12;

		internal IntPtr InputHandle, OutputHandle;
		IntPtr ScreenBuffer;
		uint originalConsoleMode;

		public WindowsConsole ()
		{
			InputHandle = GetStdHandle (STD_INPUT_HANDLE);
			OutputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
			originalConsoleMode = ConsoleMode;
			var newConsoleMode = originalConsoleMode;
			newConsoleMode |= (uint)(ConsoleModes.EnableMouseInput | ConsoleModes.EnableExtendedFlags);
			newConsoleMode &= ~(uint)ConsoleModes.EnableQuickEditMode;
			ConsoleMode = newConsoleMode;
		}

		public CharInfo [] OriginalStdOutChars;

		public bool WriteToConsole (CharInfo [] charInfoBuffer, Coord coords, SmallRect window)
		{
			if (ScreenBuffer == IntPtr.Zero) {
				ScreenBuffer = CreateConsoleScreenBuffer (
					DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
					ShareMode.FileShareRead | ShareMode.FileShareWrite,
					IntPtr.Zero,
					1,
					IntPtr.Zero
				);
				if (ScreenBuffer == INVALID_HANDLE_VALUE) {
					var err = Marshal.GetLastWin32Error ();

					if (err != 0)
						throw new System.ComponentModel.Win32Exception (err);
				}

				if (!SetConsoleActiveScreenBuffer (ScreenBuffer)) {
					var err = Marshal.GetLastWin32Error ();
					throw new System.ComponentModel.Win32Exception (err);
				}

				OriginalStdOutChars = new CharInfo [Console.WindowHeight * Console.WindowWidth];

				ReadConsoleOutput (OutputHandle, OriginalStdOutChars, coords, new Coord () { X = 0, Y = 0 }, ref window);
			}

			return WriteConsoleOutput (ScreenBuffer, charInfoBuffer, coords, new Coord () { X = window.Left, Y = window.Top }, ref window);
		}

		public bool SetCursorPosition (Coord position)
		{
			return SetConsoleCursorPosition (ScreenBuffer, position);
		}

		public void Cleanup ()
		{
			ConsoleMode = originalConsoleMode;
			ContinueListeningForConsoleEvents = false;
			if (!SetConsoleActiveScreenBuffer (OutputHandle)) {
				var err = Marshal.GetLastWin32Error ();
				Console.WriteLine ("Error: {0}", err);
			}

			if (ScreenBuffer != IntPtr.Zero)
				CloseHandle(ScreenBuffer);

			ScreenBuffer = IntPtr.Zero;
		}

		private bool ContinueListeningForConsoleEvents = true;

		public uint ConsoleMode {
			get {
				uint v;
				GetConsoleMode (InputHandle, out v);
				return v;
			}

			set {
				SetConsoleMode (InputHandle, value);
			}
		}

		[Flags]
		public enum ConsoleModes : uint {
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
			RightmostButtonPressed = 2,

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
			public Coordinate MousePosition;
			[FieldOffset (4)]
			public ButtonState ButtonState;
			[FieldOffset (8)]
			public ControlKeyState ControlKeyState;
			[FieldOffset (12)]
			public EventFlags EventFlags;

			public override string ToString ()
			{
				return $"[Mouse({MousePosition},{ButtonState},{ControlKeyState},{EventFlags}";
			}
		}

		[StructLayout (LayoutKind.Sequential)]
		public struct Coordinate {
			public short X;
			public short Y;

			public Coordinate (short X, short Y)
			{
				this.X = X;
				this.Y = Y;
			}

			public override string ToString () => $"({X},{Y})";
		};

		internal struct WindowBufferSizeRecord {
			public Coordinate size;

			public WindowBufferSizeRecord (short x, short y)
			{
				this.size = new Coordinate (x, y);
			}

			public override string ToString () => $"[WindowBufferSize{size}";
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

			public override string ToString ()
			{
				switch (EventType) {
				case EventType.Focus:
					return FocusEvent.ToString ();
				case EventType.Key:
					return KeyEvent.ToString ();
				case EventType.Menu:
					return MenuEvent.ToString ();
				case EventType.Mouse:
					return MouseEvent.ToString ();
				case EventType.WindowBufferSize:
					return WindowBufferSizeEvent.ToString ();
				default:
					return "Unknown event type: " + EventType;
				}
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

			public Coord (short X, short Y)
			{
				this.X = X;
				this.Y = Y;
			}
			public override string ToString () => $"({X},{Y})";
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

			public static void MakeEmpty (ref SmallRect rect)
			{
				rect.Left = -1;
			}

			public static void Update (ref SmallRect rect, short col, short row)
			{
				if (rect.Left == -1) {
					//System.Diagnostics.Debugger.Log (0, "debug", $"damager From Empty {col},{row}\n");
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
				//System.Diagnostics.Debugger.Log (0, "debug", $"Expanding {rect.ToString ()}\n");
			}

			public override string ToString ()
			{
				return $"Left={Left},Top={Top},Right={Right},Bottom={Bottom}";
			}
		}

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetStdHandle (int nStdHandle);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool CloseHandle(IntPtr handle);

		[DllImport ("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
		public static extern bool ReadConsoleInput (
			IntPtr hConsoleInput,
			[Out] InputRecord [] lpBuffer,
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

		[DllImport ("kernel32.dll", EntryPoint = "WriteConsoleOutput", SetLastError = true, CharSet = CharSet.Unicode)]
		static extern bool WriteConsoleOutput (
			IntPtr hConsoleOutput,
			CharInfo [] lpBuffer,
			Coord dwBufferSize,
			Coord dwBufferCoord,
			ref SmallRect lpWriteRegion
		);

		[DllImport ("kernel32.dll")]
		static extern bool SetConsoleCursorPosition (IntPtr hConsoleOutput, Coord dwCursorPosition);

		[DllImport ("kernel32.dll")]
		static extern bool GetConsoleMode (IntPtr hConsoleHandle, out uint lpMode);


		[DllImport ("kernel32.dll")]
		static extern bool SetConsoleMode (IntPtr hConsoleHandle, uint dwMode);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr CreateConsoleScreenBuffer (
			DesiredAccess dwDesiredAccess,
			ShareMode dwShareMode,
			IntPtr secutiryAttributes,
			UInt32 flags,
			IntPtr screenBufferData
		);

		internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr (-1);


		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleActiveScreenBuffer (IntPtr Handle);

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern bool GetNumberOfConsoleInputEvents (IntPtr handle, out uint lpcNumberOfEvents);
		public uint InputEventCount {
			get {
				uint v;
				GetNumberOfConsoleInputEvents (InputHandle, out v);
				return v;
			}
		}
	}

	internal class WindowsDriver : ConsoleDriver, Mono.Terminal.IMainLoopDriver {
		static bool sync;
		ManualResetEventSlim eventReady = new ManualResetEventSlim(false);
		ManualResetEventSlim waitForProbe = new ManualResetEventSlim(false);
		MainLoop mainLoop;
		WindowsConsole.CharInfo [] OutputBuffer;
		int cols, rows;
		WindowsConsole winConsole;
		WindowsConsole.SmallRect damageRegion;

		public override int Cols => cols;
		public override int Rows => rows;

		public WindowsDriver ()
		{
			winConsole = new WindowsConsole ();

			cols = Console.WindowWidth;
			rows = Console.WindowHeight - 1;
			WindowsConsole.SmallRect.MakeEmpty (ref damageRegion);

			ResizeScreen ();
			UpdateOffScreen ();

			Task.Run ((Action)WindowsInputHandler);
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ConsoleKeyInfoEx {
			public ConsoleKeyInfo consoleKeyInfo;
			public bool CapsLock;
			public bool NumLock;

			public ConsoleKeyInfoEx(ConsoleKeyInfo consoleKeyInfo, bool capslock, bool numlock)
			{
			this.consoleKeyInfo = consoleKeyInfo;
			CapsLock = capslock;
			NumLock = numlock;
			}
		}

		// The records that we keep fetching
		WindowsConsole.InputRecord [] result, records = new WindowsConsole.InputRecord [1];

		void WindowsInputHandler ()
		{
			while (true) {
				waitForProbe.Wait();
				waitForProbe.Reset();

				uint numberEventsRead = 0;

				WindowsConsole.ReadConsoleInput (winConsole.InputHandle, records, 1, out numberEventsRead);
				if (numberEventsRead == 0)
					result = null;
				else
					result = records;

				eventReady.Set();
			}
		}

		void IMainLoopDriver.Setup (MainLoop mainLoop)
		{
			this.mainLoop = mainLoop;
		}

		void IMainLoopDriver.Wakeup ()
		{
			tokenSource.Cancel();
		}

		bool IMainLoopDriver.EventsPending (bool wait)
		{
			long now = DateTime.UtcNow.Ticks;

			int waitTimeout;
			if (mainLoop.timeouts.Count > 0) {
				waitTimeout = (int)((mainLoop.timeouts.Keys [0] - now) / TimeSpan.TicksPerMillisecond);
				if (waitTimeout < 0)
					return true;
			} else
				waitTimeout = -1;

			if (!wait)
				waitTimeout = 0;

			result = null;
			waitForProbe.Set();

			try {
				if(!tokenSource.IsCancellationRequested)
					eventReady.Wait(waitTimeout, tokenSource.Token);
			} catch (OperationCanceledException) {
				return true;
			} finally {
				eventReady.Reset();
			}
			Debug.WriteLine("Events ready");

			if (!tokenSource.IsCancellationRequested)
				return result != null;

			tokenSource.Dispose();
			tokenSource = new CancellationTokenSource();
			return true;
		}

		Action<KeyEvent> keyHandler;
		Action<MouseEvent> mouseHandler;

		public override void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<MouseEvent> mouseHandler)
		{
			this.keyHandler = keyHandler;
			this.mouseHandler = mouseHandler;
		}


		void IMainLoopDriver.MainIteration ()
		{
			if (result == null)
				return;

			var inputEvent = result [0];
			switch (inputEvent.EventType) {
			case WindowsConsole.EventType.Key:
				if (inputEvent.KeyEvent.bKeyDown == false)
					return;
				var map = MapKey (ToConsoleKeyInfoEx (inputEvent.KeyEvent));
				if (inputEvent.KeyEvent.UnicodeChar == 0 && map == (Key)0xffffffff)
					return;
				keyHandler (new KeyEvent (map));
				break;

			case WindowsConsole.EventType.Mouse:
				mouseHandler (ToDriverMouse (inputEvent.MouseEvent));
				break;

			case WindowsConsole.EventType.WindowBufferSize:
				cols = inputEvent.WindowBufferSizeEvent.size.X;
				rows = inputEvent.WindowBufferSizeEvent.size.Y - 1;
				ResizeScreen ();
				UpdateOffScreen ();
				TerminalResized?.Invoke();
				break;
			}
			result = null;
		}

		private WindowsConsole.ButtonState? LastMouseButtonPressed = null;

		private MouseEvent ToDriverMouse (WindowsConsole.MouseEventRecord mouseEvent)
		{
			MouseFlags mouseFlag = MouseFlags.AllEvents;

			// The ButtonState member of the MouseEvent structure has bit corresponding to each mouse button.
			// This will tell when a mouse button is pressed. When the button is released this event will
			// be fired with it's bit set to 0. So when the button is up ButtonState will be 0.
			// To map to the correct driver events we save the last pressed mouse button so we can
			// map to the correct clicked event.
			if (LastMouseButtonPressed != null && mouseEvent.ButtonState != 0) {
				LastMouseButtonPressed = null;
			}

			if (mouseEvent.EventFlags == 0 && LastMouseButtonPressed == null) {
				switch (mouseEvent.ButtonState) {
				case WindowsConsole.ButtonState.Button1Pressed:
					mouseFlag = MouseFlags.Button1Pressed;
					break;

				case WindowsConsole.ButtonState.Button2Pressed:
					mouseFlag = MouseFlags.Button2Pressed;
					break;

				case WindowsConsole.ButtonState.Button3Pressed:
					mouseFlag = MouseFlags.Button3Pressed;
					break;
				}
				LastMouseButtonPressed = mouseEvent.ButtonState;
			} else if (mouseEvent.EventFlags == 0 && LastMouseButtonPressed != null) {
				switch (LastMouseButtonPressed) {
				case WindowsConsole.ButtonState.Button1Pressed:
					mouseFlag = MouseFlags.Button1Clicked;
					break;

				case WindowsConsole.ButtonState.Button2Pressed:
					mouseFlag = MouseFlags.Button2Clicked;
					break;

				case WindowsConsole.ButtonState.Button3Pressed:
					mouseFlag = MouseFlags.Button3Clicked;
					break;
				}
				LastMouseButtonPressed = null;
			} else if (mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved) {
				mouseFlag = MouseFlags.ReportMousePosition;
			}

			return new MouseEvent () {
				X = mouseEvent.MousePosition.X,
				Y = mouseEvent.MousePosition.Y,
				Flags = mouseFlag
			};
		}

		public ConsoleKeyInfoEx ToConsoleKeyInfoEx (WindowsConsole.KeyEventRecord keyEvent)
		{
			var state = keyEvent.dwControlKeyState;

			bool shift = (state & WindowsConsole.ControlKeyState.ShiftPressed) != 0;
			bool alt = (state & (WindowsConsole.ControlKeyState.LeftAltPressed | WindowsConsole.ControlKeyState.RightAltPressed)) != 0;
			bool control = (state & (WindowsConsole.ControlKeyState.LeftControlPressed | WindowsConsole.ControlKeyState.RightControlPressed)) != 0;
			bool capslock = (state & (WindowsConsole.ControlKeyState.CapslockOn)) != 0;
			bool numlock = (state & (WindowsConsole.ControlKeyState.NumlockOn)) != 0;

			var ConsoleKeyInfo = new ConsoleKeyInfo(keyEvent.UnicodeChar, (ConsoleKey)keyEvent.wVirtualKeyCode, shift, alt, control);
			return new ConsoleKeyInfoEx(ConsoleKeyInfo, capslock, numlock);
		}

		public Key MapKey (ConsoleKeyInfoEx keyInfoEx)
		{
			var keyInfo = keyInfoEx.consoleKeyInfo;
			switch (keyInfo.Key) {
			case ConsoleKey.Escape:
				return Key.Esc;
			case ConsoleKey.Tab:
				return keyInfo.Modifiers == ConsoleModifiers.Shift ? Key.BackTab : Key.Tab;
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
				return Key.DeleteChar;

			case ConsoleKey.NumPad0:
				return keyInfoEx.NumLock ? (Key)(uint)'0' : Key.InsertChar;
			case ConsoleKey.NumPad1:
			        return keyInfoEx.NumLock ? (Key)(uint)'1' : Key.End;
			case ConsoleKey.NumPad2:
			        return keyInfoEx.NumLock ? (Key)(uint)'2' : Key.CursorDown;
			case ConsoleKey.NumPad3:
				return keyInfoEx.NumLock ? (Key)(uint)'3' : Key.PageDown;
			case ConsoleKey.NumPad4:
			        return keyInfoEx.NumLock ? (Key)(uint)'4' : Key.CursorLeft;
			case ConsoleKey.NumPad5:
			        return keyInfoEx.NumLock ? (Key)(uint)'5' : (Key)((uint)keyInfo.KeyChar);
			case ConsoleKey.NumPad6:
			        return keyInfoEx.NumLock ? (Key)(uint)'6' : Key.CursorRight;
			case ConsoleKey.NumPad7:
			        return keyInfoEx.NumLock ? (Key)(uint)'7' : Key.Home;
			case ConsoleKey.NumPad8:
			        return keyInfoEx.NumLock ? (Key)(uint)'8' : Key.CursorUp;
			case ConsoleKey.NumPad9:
			        return keyInfoEx.NumLock ? (Key)(uint)'9' : Key.PageUp;

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
		var alphaBase = ((keyInfo.Modifiers == ConsoleModifiers.Shift) ^ (keyInfoEx.CapsLock)) ? 'A' : 'a';

		if (key >= ConsoleKey.A && key <= ConsoleKey.Z) {
				    var delta = key - ConsoleKey.A;
				    if (keyInfo.Modifiers == ConsoleModifiers.Control)
					    return (Key)((uint)Key.ControlA + delta);
				    if (keyInfo.Modifiers == ConsoleModifiers.Alt)
					    return (Key)(((uint)Key.AltMask) | ((uint)'A' + delta));
				    return (Key)((uint)alphaBase + delta);
		}
		if (key >= ConsoleKey.D0 && key <= ConsoleKey.D9) {
		var delta = key - ConsoleKey.D0;
		if (keyInfo.Modifiers == ConsoleModifiers.Alt)
			return (Key)(((uint)Key.AltMask) | ((uint)'0' + delta));

		return (Key)((uint)keyInfo.KeyChar);
		}
		if (key >= ConsoleKey.F1 && key <= ConsoleKey.F10) {
		var delta = key - ConsoleKey.F1;

		return (Key)((int)Key.F1 + delta);
		}
		return (Key)(0xffffffff);
	}

		public override void Init (Action terminalResized)
		{
			TerminalResized = terminalResized;

			Colors.Base = new ColorScheme ();
			Colors.Dialog = new ColorScheme ();
			Colors.Menu = new ColorScheme ();
			Colors.Error = new ColorScheme ();

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

		void ResizeScreen ()
		{
			OutputBuffer = new WindowsConsole.CharInfo [Rows * Cols];
			Clip = new Rect (0, 0, Cols, Rows);
			damageRegion = new WindowsConsole.SmallRect () {
				Top = 0,
				Left = 0,
				Bottom = (short)Rows,
				Right = (short)Cols
			};
		}

		void UpdateOffScreen ()
		{
			for (int row = 0; row < rows; row++)
				for (int col = 0; col < cols; col++) {
					int position = row * cols + col;
					OutputBuffer [position].Attributes = (ushort)MakeColor (ConsoleColor.White, ConsoleColor.Blue);
					OutputBuffer [position].Char.UnicodeChar = ' ';
				}
		}

		int ccol, crow;
		public override void Move (int col, int row)
		{
			ccol = col;
			crow = row;
		}

		public override void AddRune (Rune rune)
		{
			var position = crow * Cols + ccol;

			if (Clip.Contains (ccol, crow)) {
				OutputBuffer [position].Attributes = (ushort)currentAttribute;
				OutputBuffer [position].Char.UnicodeChar = (char)rune;
				WindowsConsole.SmallRect.Update (ref damageRegion, (short)ccol, (short)crow);
			}

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

		int currentAttribute;
		CancellationTokenSource tokenSource = new CancellationTokenSource();

		public override void SetAttribute (Attribute c)
		{
			currentAttribute = c.value;
		}

		private Attribute MakeColor (ConsoleColor f, ConsoleColor b)
		{
			// Encode the colors into the int value.
			return new Attribute () {
				value = ((int)f | (int)b << 4)
			};
		}

		public override Attribute MakeAttribute (Color fore, Color back)
		{
			return MakeColor ((ConsoleColor)fore, (ConsoleColor)back);
		}

		public override void Refresh ()
		{
			UpdateScreen ();
#if false
			var bufferCoords = new WindowsConsole.Coord (){
				X = (short)Clip.Width,
				Y = (short)Clip.Height
			};

			var window = new WindowsConsole.SmallRect (){
				Top = 0,
				Left = 0,
				Right = (short)Clip.Right,
				Bottom = (short)Clip.Bottom
			};

			UpdateCursor();
			winConsole.WriteToConsole (OutputBuffer, bufferCoords, window);
#endif
		}

		public override void UpdateScreen ()
		{
			if (damageRegion.Left == -1)
				return;
			
			var bufferCoords = new WindowsConsole.Coord (){
				X = (short)Clip.Width,
				Y = (short)Clip.Height
			};

			var window = new WindowsConsole.SmallRect (){
				Top = 0,
				Left = 0,
				Right = (short)Clip.Right,
				Bottom = (short)Clip.Bottom
			};

			UpdateCursor();
			winConsole.WriteToConsole (OutputBuffer, bufferCoords, damageRegion);
//			System.Diagnostics.Debugger.Log(0, "debug", $"Region={damageRegion.Right - damageRegion.Left},{damageRegion.Bottom - damageRegion.Top}\n");
			WindowsConsole.SmallRect.MakeEmpty (ref damageRegion);
		}

		public override void UpdateCursor()
		{
			var position = new WindowsConsole.Coord(){
				X = (short)ccol,
				Y = (short)crow
			};
			winConsole.SetCursorPosition(position);
		}
		public override void End ()
		{
			winConsole.Cleanup();
		}

#region Unused
		public override void SetColors (ConsoleColor foreground, ConsoleColor background)
		{
		}

		public override void SetColors (short foregroundColorId, short backgroundColorId)
		{
		}

		public override void Suspend ()
		{
		}

		public override void StartReportingMouseMoves ()
		{
		}

		public override void StopReportingMouseMoves ()
		{
		}

		public override void UncookMouse ()
		{
		}

		public override void CookMouse ()
		{
		}
#endregion

	}

	
}
