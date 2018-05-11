using System;
using System.Runtime.InteropServices;

namespace Terminal.Gui {

	internal class WindowsConsole {
		public const int STD_OUTPUT_HANDLE = -11;
		public const int STD_INPUT_HANDLE = -10;
		public const int STD_ERROR_HANDLE = -12;

		[DllImport ("kernel32.dll", SetLastError = true)]
		static extern IntPtr GetStdHandle (int nStdHandle);

		IntPtr inputHandle, outputHandle;

		public WindowsConsole ()
		{
			inputHandle = GetStdHandle (STD_INPUT_HANDLE);
			outputHandle = GetStdHandle (STD_OUTPUT_HANDLE);
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

		[DllImport ("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
		public static extern bool ReadConsoleInput (
			IntPtr hConsoleInput,
			[Out] InputRecord [] lpBuffer,
			uint nLength,
			out uint lpNumberOfEventsRead);

		[DllImport ("kernel32.dll")]
		static extern bool GetConsoleMode (IntPtr hConsoleHandle, out uint lpMode);

		[DllImport ("kernel32.dll")]
		static extern bool SetConsoleMode (IntPtr hConsoleHandle, uint dwMode);

		public uint ConsoleMode {
			get {
				uint v;
				GetConsoleMode (inputHandle, out v);
				return v;
			}

			set {
				SetConsoleMode (inputHandle, value);
			}
		}
	}	
	public class WindowsDriver {
		public WindowsDriver ()
		{
		}
	}
}
