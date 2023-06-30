//
// FakeDriver.cs: A fake ConsoleDriver for unit tests. 
//
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;
using Unix.Terminal;
using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui;
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

	// The format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
	bool [] _dirtyLine;

	public FakeDriver ()
	{
		if (FakeBehaviors.UseFakeClipboard) {
			Clipboard = new FakeClipboard (FakeBehaviors.FakeClipboardAlwaysThrowsNotSupportedException, FakeBehaviors.FakeClipboardIsSupportedAlwaysFalse);
		} else {
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
	}

	public override void AddRune (Rune rune)
	{
		rune = rune.MakePrintable ();
		var runeWidth = rune.GetColumns ();
		var validLocation = IsValidLocation (Col, Row);

		if (validLocation) {
			if (rune.IsCombiningMark () && Col > 0) {
				// Decode the previous rune
				var previousRune = new Rune (Contents [Row, Col - 1, 0]);
				var newCombo = new StringBuilder ();
				ReadOnlySpan<char> remainingInput = previousRune.ToString ().AsSpan ();
				while (!remainingInput.IsEmpty) {
					// Decode
					OperationStatus opStatus = Rune.DecodeFromUtf16 (remainingInput, out Rune result, out int charsConsumed);

					if (opStatus is OperationStatus.DestinationTooSmall or OperationStatus.InvalidData) {
						result = Rune.ReplacementChar;
					}

					newCombo.Append (result);
					// Slice and loop again
					remainingInput = remainingInput [charsConsumed..];
				}
				newCombo.Append (rune);

				var combined = newCombo.ToString ();
				var normalized = !combined.IsNormalized () ? combined.Normalize () : combined;
				Contents [Row, Col - 1, 0] = normalized [0];// BUGBUG: This is wrong, we need to handle the case where the rune is more than one char
				Contents [Row, Col - 1, 1] = CurrentAttribute.Value;
				Contents [Row, Col - 1, 2] = 1;
				Col--;
			} else {
				Contents [Row, Col, 0] = rune.Value;
				Contents [Row, Col, 1] = CurrentAttribute.Value;

				if (Col > 0) {
					var left = new Rune (Contents [Row, Col - 1, 0]);
					if (left.GetColumns () > 1) {
						Contents [Row, Col - 1, 0] = Rune.ReplacementChar.Value;
					}
				}

				if (runeWidth > 1) {
					Col++;
					Contents [Row, Col, 0] = Rune.ReplacementChar.Value;
					Contents [Row, Col, 1] = CurrentAttribute.Value;
					Contents [Row, Col, 2] = 1;
				}
			}
		}
		Col++;
	}

	public override void End ()
	{
		FakeConsole.ResetColor ();
		FakeConsole.Clear ();
	}


	public override void Init (Action terminalResized)
	{
		FakeConsole.MockKeyPresses.Clear ();

		TerminalResized = terminalResized;

		Cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
		Rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;
		FakeConsole.Clear ();
		ResizeScreen ();
		// Call InitializeColorSchemes before UpdateOffScreen as it references Colors
		CurrentAttribute = MakeColor (Color.White, Color.Black);
		InitializeColorSchemes ();
		UpdateOffScreen ();
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
					if (Contents [row, col, 2] == 0) {
						FakeConsole.CursorLeft++;
						continue;
					}

					var color = Contents [row, col, 1];
					// NOTE: In real drivers setting the color can be a performance hit, so we only do it when needed.
					// in fakedriver we don't care about perf.
					SetColor (color);

					var rune = (Rune)Contents [row, col, 0];
					if (rune.Utf16SequenceLength == 1) {
						FakeConsole.Write (rune);
					} else {
						// TODO: Not sure we need to do this. I think we can just write the rune.

						FakeConsole.Write (rune.ToString ());
					}
					//if (Rune.DecodeSurrogatePair (rune, out char [] spair)) {
					//	FakeConsole.Write (spair);
					//} else {
					//	FakeConsole.Write ((char)rune);
					//}
					Contents [row, col, 2] = 0;
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

	#region Color Handling

	// Cache the list of ConsoleColor values.
	private static readonly HashSet<int> ConsoleColorValues = new HashSet<int> (
		Enum.GetValues (typeof (ConsoleColor)).OfType<ConsoleColor> ().Select (c => (int)c)
	);

	void SetColor (int color)
	{
		if (ConsoleColorValues.Contains (color & 0xffff)) {
			FakeConsole.BackgroundColor = (ConsoleColor)(color & 0xffff);
		}
		if (ConsoleColorValues.Contains ((color >> 16) & 0xffff)) {
			FakeConsole.ForegroundColor = (ConsoleColor)((color >> 16) & 0xffff);
		}
	}

	/// <remarks>
	/// In the FakeDriver, colors are encoded as an int; same as NetDriver
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
	/// In the FakeDriver, colors are encoded as an int; same as NetDriver
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
		(mainLoop.MainLoopDriver as FakeMainLoop).KeyPressed += (consoleKey) => ProcessInput (consoleKey);
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
		if (!(Col >= 0 && Row >= 0 && Col < Cols && Row < Rows)) {
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
		Cols = width;
		Rows = height;
		if (!EnableConsoleScrolling) {
			SetWindowSize (width, height);
		}
		ProcessResize ();
	}

	public void SetWindowSize (int width, int height)
	{
		FakeConsole.SetWindowSize (width, height);
		if (!EnableConsoleScrolling) {
			if (width != Cols || height != Rows) {
				SetBufferSize (width, height);
				Cols = width;
				Rows = height;
			}
		}
		ProcessResize ();
	}

	public void SetWindowPosition (int left, int top)
	{
		if (EnableConsoleScrolling) {
			Left = Math.Max (Math.Min (left, Cols - FakeConsole.WindowWidth), 0);
			Top = Math.Max (Math.Min (top, Rows - FakeConsole.WindowHeight), 0);
		} else if (Left > 0 || Top > 0) {
			Left = 0;
			Top = 0;
		}
		FakeConsole.SetWindowPosition (Left, Top);
	}

	void ProcessResize ()
	{
		ResizeScreen ();
		UpdateOffScreen ();
		TerminalResized?.Invoke ();
	}

	public virtual void ResizeScreen ()
	{
		if (!EnableConsoleScrolling) {
			if (FakeConsole.WindowHeight > 0) {
				// Can raise an exception while is still resizing.
				try {
					FakeConsole.CursorTop = 0;
					FakeConsole.CursorLeft = 0;
					FakeConsole.WindowTop = 0;
					FakeConsole.WindowLeft = 0;
				} catch (System.IO.IOException) {
					return;
				} catch (ArgumentOutOfRangeException) {
					return;
				}
			}
		} else {
			try {
#pragma warning disable CA1416
				FakeConsole.WindowLeft = Math.Max (Math.Min (Left, Cols - FakeConsole.WindowWidth), 0);
				FakeConsole.WindowTop = Math.Max (Math.Min (Top, Rows - FakeConsole.WindowHeight), 0);
#pragma warning restore CA1416
			} catch (Exception) {
				return;
			}
		}

		Clip = new Rect (0, 0, Cols, Rows);
	}

	public override void UpdateOffScreen ()
	{
		Contents = new int [Rows, Cols, 3];
		_dirtyLine = new bool [Rows];

		// Can raise an exception while is still resizing.
		try {
			for (int row = 0; row < Rows; row++) {
				for (int c = 0; c < Cols; c++) {
					Contents [row, c, 0] = ' ';
					Contents [row, c, 1] = 0;
					Contents [row, c, 2] = 0;
					_dirtyLine [row] = true;
				}
			}
		} catch (IndexOutOfRangeException) { }
		Clip = new Rect (0, 0, Cols, Rows);
	}

	public override void UpdateCursor ()
	{
		if (!EnsureCursorVisibility ()) {
			return;
		}

		// Prevents the exception of size changing during resizing.
		try {
			// BUGBUG: Why is this using BufferWidth/Height and now Cols/Rows?
			if (Col >= 0 && Col < FakeConsole.BufferWidth && Row >= 0 && Row < FakeConsole.BufferHeight) {
				FakeConsole.SetCursorPosition (Col, Row);
			}
		} catch (System.IO.IOException) {
		} catch (ArgumentOutOfRangeException) {
		}
	}

	#region Not Implemented
	public override void Suspend ()
	{
		throw new NotImplementedException ();
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
			if (text == null) {
				throw new ArgumentNullException (nameof (text));
			}
			if (FakeException != null) {
				throw FakeException;
			}
			_contents = text;
		}
	}

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}