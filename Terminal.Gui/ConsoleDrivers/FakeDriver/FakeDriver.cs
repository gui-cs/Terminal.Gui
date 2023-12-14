//
// FakeDriver.cs: A fake ConsoleDriver for unit tests. 
//
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Terminal.Gui.ConsoleDrivers;

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

	internal override MainLoop Init ()
	{
		FakeConsole.MockKeyPresses.Clear ();

		Cols = FakeConsole.WindowWidth = FakeConsole.BufferWidth = FakeConsole.WIDTH;
		Rows = FakeConsole.WindowHeight = FakeConsole.BufferHeight = FakeConsole.HEIGHT;
		FakeConsole.Clear ();
		ResizeScreen ();
		CurrentAttribute = new Attribute (Color.White, Color.Black);
		ClearContents ();

		_mainLoopDriver = new FakeMainLoop (this);
		_mainLoopDriver.MockKeyPressed = MockKeyPressedHandler;
		return new MainLoop (_mainLoopDriver);
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
					var rune = (Rune)Contents [row, col].Rune;
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


	KeyCode MapKey (ConsoleKeyInfo keyInfo)
	{
		switch (keyInfo.Key) {
		case ConsoleKey.Escape:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.Esc);
		case ConsoleKey.Tab:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.Tab);
		case ConsoleKey.Clear:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.Clear);
		case ConsoleKey.Home:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.Home);
		case ConsoleKey.End:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.End);
		case ConsoleKey.LeftArrow:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.CursorLeft);
		case ConsoleKey.RightArrow:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.CursorRight);
		case ConsoleKey.UpArrow:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.CursorUp);
		case ConsoleKey.DownArrow:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.CursorDown);
		case ConsoleKey.PageUp:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.PageUp);
		case ConsoleKey.PageDown:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.PageDown);
		case ConsoleKey.Enter:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.Enter);
		case ConsoleKey.Spacebar:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, keyInfo.KeyChar == 0 ? KeyCode.Space : (KeyCode)keyInfo.KeyChar);
		case ConsoleKey.Backspace:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.Backspace);
		case ConsoleKey.Delete:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.DeleteChar);
		case ConsoleKey.Insert:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.InsertChar);
		case ConsoleKey.PrintScreen:
			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, KeyCode.PrintScreen);

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
				return KeyCode.Unknown;
			}

			return ConsoleKeyMapping.MapKeyModifiers (keyInfo, (KeyCode)((uint)keyInfo.KeyChar));
		}

		var key = keyInfo.Key;
		if (key >= ConsoleKey.A && key <= ConsoleKey.Z) {
			var delta = key - ConsoleKey.A;
			if (keyInfo.KeyChar != (uint)key) {
				return ConsoleKeyMapping.MapKeyModifiers (keyInfo, (KeyCode)keyInfo.KeyChar);
			}
			if (keyInfo.Modifiers.HasFlag (ConsoleModifiers.Control)
			|| keyInfo.Modifiers.HasFlag (ConsoleModifiers.Alt)
			|| keyInfo.Modifiers.HasFlag (ConsoleModifiers.Shift)) {
				return ConsoleKeyMapping.MapKeyModifiers (keyInfo, (KeyCode)((uint)KeyCode.A + delta));
			}
			var alphaBase = ((keyInfo.Modifiers != ConsoleModifiers.Shift)) ? 'A' : 'a';
			return (KeyCode)((uint)alphaBase + delta);
		}

		return ConsoleKeyMapping.MapKeyModifiers (keyInfo, (KeyCode)((uint)keyInfo.KeyChar));
	}

	private CursorVisibility _savedCursorVisibility;

	void MockKeyPressedHandler (ConsoleKeyInfo consoleKeyInfo)
	{
		if (consoleKeyInfo.Key == ConsoleKey.Packet) {
			consoleKeyInfo = ConsoleKeyMapping.FromVKPacketToKConsoleKeyInfo (consoleKeyInfo);
		}

		var map = MapKey (consoleKeyInfo);
		OnKeyDown (new KeyEventArgs (map));
		OnKeyUp (new KeyEventArgs (map));
		//OnKeyPressed (new KeyEventArgs (map));
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
		MockKeyPressedHandler (new ConsoleKeyInfo (keyChar, key, shift, alt, control));
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
		} catch (System.IO.IOException) { } catch (ArgumentOutOfRangeException) { }
	}

	#region Not Implemented
	public override void Suspend ()
	{
		return;
		//throw new NotImplementedException ();
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