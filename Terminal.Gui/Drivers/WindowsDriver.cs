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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mono.Terminal;
using NStack;

namespace Terminal.Gui {

	internal class WindowsConsole {
		public const int STD_OUTPUT_HANDLE = -11;
		public const int STD_INPUT_HANDLE = -10;
		public const int STD_ERROR_HANDLE = -12;

		IntPtr InputHandle, OutputHandle;

		IntPtr ScreenBuffer;

		public WindowsConsole ()
		{
			InputHandle = GetStdHandle (STD_INPUT_HANDLE);
			OutputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
		}

		public CharInfo[] OriginalStdOutChars;

		public bool WriteToConsole (CharInfo[] charInfoBuffer, Coord coords, SmallRect window)
		{
			if (ScreenBuffer == IntPtr.Zero)
			{
				ScreenBuffer = CreateConsoleScreenBuffer (
					DesiredAccess.GenericRead | DesiredAccess.GenericWrite,
					ShareMode.FileShareRead | ShareMode.FileShareWrite,
					IntPtr.Zero,
					1,
					IntPtr.Zero
				);
				if (ScreenBuffer == INVALID_HANDLE_VALUE){
					var err = Marshal.GetLastWin32Error ();

					if (err != 0)
					throw new System.ComponentModel.Win32Exception(err);
				}

				if (!SetConsoleActiveScreenBuffer (ScreenBuffer)){
					var err = Marshal.GetLastWin32Error();
					throw new System.ComponentModel.Win32Exception(err);
				}

				OriginalStdOutChars = new CharInfo[Console.WindowHeight * Console.WindowWidth];

				ReadConsoleOutput (OutputHandle, OriginalStdOutChars, coords, new Coord () { X = 0, Y = 0 }, ref window);
			}

			return WriteConsoleOutput (ScreenBuffer, charInfoBuffer, coords, new Coord () { X = 0, Y = 0 }, ref window);
		}

		public bool SetCursorPosition(Coord position)
		{
			return SetConsoleCursorPosition (ScreenBuffer, position);
		}

		public void PollEvents (Action<InputRecord> inputEventHandler)
		{
			if (OriginalConsoleMode != 0)
				return;

			OriginalConsoleMode = ConsoleMode;

			ConsoleMode |= (uint)ConsoleModes.EnableMouseInput;
			ConsoleMode &= ~(uint)ConsoleModes.EnableQuickEditMode;
			ConsoleMode |= (uint)ConsoleModes.EnableExtendedFlags;

			Task.Run (() =>
			{
				uint numberEventsRead = 0;
				uint length = 1;
				InputRecord[] records = new InputRecord[length];

				while (ContinueListeningForConsoleEvents &&
					ReadConsoleInput(InputHandle, records, length, out numberEventsRead) &&
				       numberEventsRead > 0){
					inputEventHandler (records[0]);
				}
			});
		}

		public void Cleanup ()
		{
			ContinueListeningForConsoleEvents = false;
			ConsoleMode = OriginalConsoleMode;
			OriginalConsoleMode = 0;

			if (!SetConsoleActiveScreenBuffer (OutputHandle)){
				var err = Marshal.GetLastWin32Error ();
				Console.WriteLine("Error: {0}", err);
			}
		}

		private bool ContinueListeningForConsoleEvents = true;

		private uint OriginalConsoleMode = 0;

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
		public enum ConsoleModes : uint
		{
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

		public enum EventType {
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
		enum ShareMode : uint
		{
			FileShareRead = 1,
			FileShareWrite = 2,
		}

		[Flags]
		enum DesiredAccess : uint
		{
			GenericRead = 2147483648,
			GenericWrite = 1073741824,
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ConsoleScreenBufferInfo
		{
			public Coord dwSize;
			public Coord dwCursorPosition;
			public ushort wAttributes;
			public SmallRect srWindow;
			public Coord dwMaximumWindowSize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Coord
		{
			public short X;
			public short Y;

			public Coord(short X, short Y)
			{
				this.X = X;
				this.Y = Y;
			}
		};

		[StructLayout(LayoutKind.Explicit, CharSet=CharSet.Unicode)]
		public struct CharUnion
		{
			[FieldOffset(0)] public char UnicodeChar;
			[FieldOffset(0)] public byte AsciiChar;
		}

		[StructLayout(LayoutKind.Explicit, CharSet=CharSet.Unicode)]
		public struct CharInfo
		{
			[FieldOffset(0)] public CharUnion Char;
			[FieldOffset(2)] public ushort Attributes;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SmallRect
		{
			public short Left;
			public short Top;
			public short Right;
			public short Bottom;
		}

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetStdHandle (int nStdHandle);

		[DllImport ("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
		public static extern bool ReadConsoleInput (
			IntPtr hConsoleInput,
			[Out] InputRecord [] lpBuffer,
			uint nLength,
			out uint lpNumberOfEventsRead);

		[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
		static extern bool ReadConsoleOutput(
			IntPtr hConsoleOutput,
			[Out] CharInfo[] lpBuffer,
			Coord dwBufferSize,
			Coord dwBufferCoord,
			ref SmallRect lpReadRegion
		);
		
		[DllImport("kernel32.dll", EntryPoint="WriteConsoleOutput", SetLastError=true, CharSet=CharSet.Unicode)]
		static extern bool WriteConsoleOutput(
			IntPtr hConsoleOutput,
			CharInfo[] lpBuffer,
			Coord dwBufferSize,
			Coord dwBufferCoord,
			ref SmallRect lpWriteRegion
		);

		[DllImport ("kernel32.dll")]
		static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, Coord dwCursorPosition);

		[DllImport ("kernel32.dll")]
		static extern bool GetConsoleMode (IntPtr hConsoleHandle, out uint lpMode);


		[DllImport ("kernel32.dll")]
		static extern bool SetConsoleMode (IntPtr hConsoleHandle, uint dwMode);

		[DllImport("kernel32.dll", SetLastError = true)]
		static extern IntPtr CreateConsoleScreenBuffer(
			DesiredAccess dwDesiredAccess,
			ShareMode dwShareMode,
			IntPtr secutiryAttributes,
			UInt32 flags,
			IntPtr screenBufferData
		);

		internal static IntPtr INVALID_HANDLE_VALUE = new IntPtr (-1);


		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetConsoleActiveScreenBuffer(IntPtr Handle);

	}

	internal class WindowsDriver : ConsoleDriver {

		Action TerminalResized;

		WindowsConsole WinConsole;

		WindowsConsole.CharInfo[] OutputBuffer;

		int cols, rows;

		public override int Cols => cols;

		public override int Rows => rows;

		static bool sync;

		public WindowsDriver ()
		{
			WinConsole = new WindowsConsole();
			cols = Console.WindowWidth;
			rows = Console.WindowHeight - 1;
			ResizeScreen ();
			UpdateOffScreen ();
		}

		private WindowsConsole.ButtonState? LastMouseButtonPressed = null;

		private MouseEvent ToDriverMouse(WindowsConsole.MouseEventRecord mouseEvent)
		{
			MouseFlags mouseFlag = MouseFlags.AllEvents;

			// The ButtonState member of the MouseEvent structure has bit corresponding to each mouse button.
			// This will tell when a mouse button is pressed. When the button is released this event will
			// be fired with it's bit set to 0. So when the button is up ButtonState will be 0.
			// To map to the correct driver events we save the last pressed mouse button so we can
			// map to the correct clicked event.
			if (LastMouseButtonPressed != null && mouseEvent.ButtonState != 0)
			{
				LastMouseButtonPressed = null;
			}

			if (mouseEvent.EventFlags == 0 && LastMouseButtonPressed == null){
				switch (mouseEvent.ButtonState){
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
			} else if (mouseEvent.EventFlags == 0 && LastMouseButtonPressed != null){
				switch (LastMouseButtonPressed){
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
			} else if(mouseEvent.EventFlags == WindowsConsole.EventFlags.MouseMoved){
				mouseFlag = MouseFlags.ReportMousePosition;
			}

			return new MouseEvent () {
				X = mouseEvent.MousePosition.X,
				Y = mouseEvent.MousePosition.Y,
				Flags = mouseFlag
			};
		}

		private ConsoleKeyInfo ToConsoleKeyInfo (WindowsConsole.KeyEventRecord keyEvent)
		{
			var state = keyEvent.dwControlKeyState;

			bool shift = (state & WindowsConsole.ControlKeyState.ShiftPressed) != 0;
			bool alt = (state & (WindowsConsole.ControlKeyState.LeftAltPressed | WindowsConsole.ControlKeyState.RightAltPressed)) != 0;
			bool control = (state & (WindowsConsole.ControlKeyState.LeftControlPressed | WindowsConsole.ControlKeyState.RightControlPressed)) != 0;

			return new ConsoleKeyInfo(keyEvent.UnicodeChar, (ConsoleKey)keyEvent.wVirtualKeyCode, shift, alt, control);
		}

		public Key MapKey (ConsoleKeyInfo keyInfo)
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
				return Key.DeleteChar;

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
			WinConsole.PollEvents (inputEvent =>
			{
				switch(inputEvent.EventType){
				case WindowsConsole.EventType.Key:
					if (inputEvent.KeyEvent.bKeyDown == false)
						return;
					var map = MapKey (ToConsoleKeyInfo (inputEvent.KeyEvent));
					if (map == (Key) 0xffffffff)
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
					TerminalResized ();
					break;
				}
			});
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
			OutputBuffer = new WindowsConsole.CharInfo[Rows * Cols];
			Clip = new Rect (0, 0, Cols, Rows);
		}

		void UpdateOffScreen ()
		{
			for (int row = 0; row < rows; row++)
				for (int col = 0; col < cols; col++){
					int position = row * cols + col;
					OutputBuffer[position].Attributes = (ushort)MakeColor(ConsoleColor.White, ConsoleColor.Blue);
					OutputBuffer[position].Char.UnicodeChar = ' ';
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

			if (Clip.Contains (ccol, crow)){
				OutputBuffer[position].Attributes = (ushort)currentAttribute;
				OutputBuffer[position].Char.UnicodeChar = (char)rune;
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
		public override void SetAttribute (Attribute c)
		{
			currentAttribute = c.value;
		}

		private Attribute MakeColor (ConsoleColor f, ConsoleColor b)
		{
			// Encode the colors into the int value.
			return new Attribute (){
				value = ((int)f | (int)b << 4)
			};
		}

		public override void Refresh()
		{
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
			WinConsole.WriteToConsole (OutputBuffer, bufferCoords, window);
		}

		public override void UpdateScreen ()
		{
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
			WinConsole.WriteToConsole (OutputBuffer, bufferCoords, window);
		}

		public override void UpdateCursor()
		{
			var position = new WindowsConsole.Coord(){
				X = (short)ccol,
				Y = (short)crow
			};
			WinConsole.SetCursorPosition(position);
		}
		public override void End ()
		{
			WinConsole.Cleanup();
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
