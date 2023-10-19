//
// FakeDriver.cs: A fake ConsoleDriver for unit tests. 
//
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

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

	public override bool SupportsTrueColor => false;

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

	internal override void End ()
	{
		FakeConsole.ResetColor ();
		FakeConsole.Clear ();
	}

	FakeMainLoop _mainLoopDriver = null;
	internal override MainLoop CreateMainLoop ()
	{
		_mainLoopDriver = new FakeMainLoop (this);
		return new MainLoop (_mainLoopDriver);
	}

	internal override void Init ()
	{
		FakeConsole.MockKeyPresses.Clear ();

		Cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
		Rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;
		FakeConsole.Clear ();
		ResizeScreen ();
		CurrentAttribute = new Attribute (Color.White, Color.Black);
		ClearContents ();
	}


	public override void UpdateScreen ()
	{
		var savedRow = FakeConsole.CursorTop;
		var savedCol = FakeConsole.CursorLeft;
		var savedCursorVisible = FakeConsole.CursorVisible;

		var top = 0;
		var left = 0;
		var rows = Rows;
		var cols = Cols;
		System.Text.StringBuilder output = new System.Text.StringBuilder ();
		Attribute redrawAttr = new Attribute ();
		var lastCol = -1;

		for (var row = top; row < rows; row++) {
			if (!_dirtyLines [row]) {
				continue;
			}

			FakeConsole.CursorTop = row;
			FakeConsole.CursorLeft = 0;

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
						FakeConsole.ForegroundColor = (ConsoleColor)attr.Foreground.ColorName;
						FakeConsole.BackgroundColor = (ConsoleColor)attr.Background.ColorName;
					}
					outputWidth++;
					var rune = (Rune)Contents [row, col].Runes [0];
					output.Append (rune.ToString ());
					if (rune.IsSurrogatePair () && rune.GetColumns () < 2) {
						WriteToConsole (output, ref lastCol, row, ref outputWidth);
						FakeConsole.CursorLeft--;
					}
					Contents [row, col].IsDirty = false;
				}
			}
			if (output.Length > 0) {
				FakeConsole.CursorTop = row;
				FakeConsole.CursorLeft = lastCol;

				foreach (var c in output.ToString ()) {
					FakeConsole.Write (c);
				}
			}
		}
		FakeConsole.CursorTop = 0;
		FakeConsole.CursorLeft = 0;

		//SetCursorVisibility (savedVisibitity);

		void WriteToConsole (StringBuilder output, ref int lastCol, int row, ref int outputWidth)
		{
			FakeConsole.CursorTop = row;
			FakeConsole.CursorLeft = lastCol;
			foreach (var c in output.ToString ()) {
				FakeConsole.Write (c);
			}

			output.Clear ();
			lastCol += outputWidth;
			outputWidth = 0;
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

	///// <remarks>
	///// In the FakeDriver, colors are encoded as an int; same as NetDriver
	///// However, the foreground color is stored in the most significant 16 bits, 
	///// and the background color is stored in the least significant 16 bits.
	///// </remarks>
	//public override Attribute MakeColor (Color foreground, Color background)
	//{
	//	// Encode the colors into the int value.
	//	return new Attribute (
	//		platformColor: 0,//((((int)foreground.ColorName) & 0xffff) << 16) | (((int)background.ColorName) & 0xffff),
	//		foreground: foreground,
	//		background: background
	//	);
	//}

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

	private CursorVisibility _savedCursorVisibility;

	internal override void PrepareToRun ()
	{
		// Note: Net doesn't support keydown/up events and thus any passed keyDown/UpHandlers will never be called
		_mainLoopDriver.KeyPressed = (consoleKey) => ProcessInput (consoleKey);
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
				OnKeyDown(new KeyEventEventArgs(new KeyEvent (map, keyModifiers)));
				OnKeyUp (new KeyEventEventArgs (new KeyEvent (map, keyModifiers)));
			}
			return;
		}

		OnKeyDown (new KeyEventEventArgs (new KeyEvent (map, keyModifiers)));
		OnKeyUp (new KeyEventEventArgs (new KeyEvent (map, keyModifiers)));
		OnKeyPressed (new KeyEventEventArgs (new KeyEvent (map, keyModifiers)));
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
		SetWindowSize (width, height);
		ProcessResize ();
	}

	public void SetWindowSize (int width, int height)
	{
		FakeConsole.SetWindowSize (width, height);
		if (width != Cols || height != Rows) {
			SetBufferSize (width, height);
			Cols = width;
			Rows = height;
		}
		ProcessResize ();
	}

	public void SetWindowPosition (int left, int top)
	{
		if (Left > 0 || Top > 0) {
			Left = 0;
			Top = 0;
		}
		FakeConsole.SetWindowPosition (Left, Top);
	}

	void ProcessResize ()
	{
		ResizeScreen ();
		ClearContents ();
		OnSizeChanged (new SizeChangedEventArgs (new Size (Cols, Rows)));
	}

	public virtual void ResizeScreen ()
	{
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