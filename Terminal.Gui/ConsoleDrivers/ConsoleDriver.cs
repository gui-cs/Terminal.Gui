//
// ConsoleDriver.cs: Base class for Terminal.Gui ConsoleDriver implementations.
//
using System.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static Terminal.Gui.ColorScheme;

namespace Terminal.Gui;

/// <summary>
/// Base class for Terminal.Gui ConsoleDriver implementations.
/// </summary>
/// <remarks>
/// There are currently four implementations:
/// - <see cref="CursesDriver"/> (for Unix and Mac)
/// - <see cref="WindowsDriver"/>
/// - <see cref="NetDriver"/> that uses the .NET Console API
/// - <see cref="FakeConsole"/> for unit testing.
/// </remarks>
public abstract class ConsoleDriver {
	/// <summary>
	/// Set this to true in any unit tests that attempt to test drivers other than FakeDriver.
	/// <code>
	///  public ColorTests ()
	///  {
	///    ConsoleDriver.RunningUnitTests = true;
	///  }
	/// </code>
	/// </summary>
	internal static bool RunningUnitTests { get; set; }

	#region Setup & Teardown

	/// <summary>
	/// Initializes the driver
	/// </summary>
	/// <returns>Returns an instance of <see cref="MainLoop"/> using the <see cref="IMainLoopDriver"/> for the driver.</returns>
	internal abstract MainLoop Init ();

	/// <summary>
	/// Ends the execution of the console driver.
	/// </summary>
	internal abstract void End ();

	#endregion

	/// <summary>
	/// The event fired when the terminal is resized.
	/// </summary>
	public event EventHandler<SizeChangedEventArgs> SizeChanged;

	/// <summary>
	/// Called when the terminal size changes. Fires the <see cref="SizeChanged"/> event.
	/// </summary>
	/// <param name="args"></param>
	public void OnSizeChanged (SizeChangedEventArgs args) => SizeChanged?.Invoke (this, args);

	/// <summary>
	/// The number of columns visible in the terminal.
	/// </summary>
	public virtual int Cols { get; internal set; }

	/// <summary>
	/// The number of rows visible in the terminal.
	/// </summary>
	public virtual int Rows { get; internal set; }

	/// <summary>
	/// The leftmost column in the terminal.
	/// </summary>
	public virtual int Left { get; internal set; } = 0;

	/// <summary>
	/// The topmost row in the terminal.
	/// </summary>
	public virtual int Top { get; internal set; } = 0;

	/// <summary>
	/// Get the operating system clipboard.
	/// </summary>
	public IClipboard Clipboard { get; internal set; }

	/// <summary>
	/// The contents of the application output. The driver outputs this buffer to the terminal when <see cref="UpdateScreen"/>
	/// is called.
	/// <remarks>
	/// The format of the array is rows, columns, and 3 values on the last column: Rune, Attribute and Dirty Flag
	/// </remarks>
	/// </summary>
	//public int [,,] Contents { get; internal set; }

	///// <summary>
	///// The contents of the application output. The driver outputs this buffer to the terminal when <see cref="UpdateScreen"/>
	///// is called.
	///// <remarks>
	///// The format of the array is rows, columns. The first index is the row, the second index is the column.
	///// </remarks>
	///// </summary>
	public Cell [,] Contents { get; internal set; }

	/// <summary>
	/// Gets the column last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/>
	/// are used by <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
	/// </summary>
	public int Col { get; internal set; }

	/// <summary>
	/// Gets the row last set by <see cref="Move"/>. <see cref="Col"/> and <see cref="Row"/>
	/// are used by <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
	/// </summary>
	public int Row { get; internal set; }

	/// <summary>
	/// Updates <see cref="Col"/> and <see cref="Row"/> to the specified column and row in <see cref="Contents"/>.
	/// Used by <see cref="AddRune(Rune)"/> and <see cref="AddStr"/> to determine where to add content.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This does not move the cursor on the screen, it only updates the internal state of the driver.
	/// </para>
	/// <para>
	/// If <paramref name="col"/> or <paramref name="row"/> are negative or beyond  <see cref="Cols"/> and <see cref="Rows"/>,
	/// the method still sets those properties.
	/// </para>
	/// </remarks>
	/// <param name="col">Column to move to.</param>
	/// <param name="row">Row to move to.</param>
	public virtual void Move (int col, int row)
	{
		Col = col;
		Row = row;
	}

	/// <summary>
	/// Tests if the specified rune is supported by the driver.
	/// </summary>
	/// <param name="rune"></param>
	/// <returns><see langword="true"/> if the rune can be properly presented; <see langword="false"/> if the driver
	/// does not support displaying this rune.</returns>
	public virtual bool IsRuneSupported (Rune rune)
	{
		return Rune.IsValid (rune.Value);
	}

	/// <summary>
	/// Adds the specified rune to the display at the current cursor position. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// When the method returns, <see cref="Col"/> will be incremented by the number of columns <paramref name="rune"/> required,
	/// even if the new column value is outside of the <see cref="Clip"/> or screen dimensions defined by <see cref="Cols"/>.
	/// </para>
	/// <para>
	/// If <paramref name="rune"/> requires more than one column, and <see cref="Col"/> plus the number of columns needed
	/// exceeds the <see cref="Clip"/> or screen dimensions, the default Unicode replacement character (U+FFFD) will be added instead.
	/// </para>
	/// </remarks>
	/// <param name="rune">Rune to add.</param>
	public void AddRune (Rune rune)
	{
		int runeWidth = -1;
		var validLocation = IsValidLocation (Col, Row);
		if (validLocation) {
			rune = rune.MakePrintable ();
			runeWidth = rune.GetColumns ();
			if (runeWidth == 0 && rune.IsCombiningMark () && Col > 0) {
				// This is a combining character, and we are not at the beginning of the line.
				// TODO: Remove hard-coded [0] once combining pairs is supported

				// Convert Runes to string and concatenate
				string combined = Contents [Row, Col - 1].Rune.ToString () + rune.ToString ();

				// Normalize to Form C (Canonical Composition)
				string normalized = combined.Normalize (NormalizationForm.FormC);

				Contents [Row, Col - 1].Rune = (Rune)normalized [0]; ;
				Contents [Row, Col - 1].Attribute = CurrentAttribute;
				Contents [Row, Col - 1].IsDirty = true;

				//Col--;
			} else {
				Contents [Row, Col].Attribute = CurrentAttribute;
				Contents [Row, Col].IsDirty = true;

				if (Col > 0) {
					// Check if cell to left has a wide glyph
					if (Contents [Row, Col - 1].Rune.GetColumns () > 1) {
						// Invalidate cell to left
						Contents [Row, Col - 1].Rune = Rune.ReplacementChar;
						Contents [Row, Col - 1].IsDirty = true;
					}
				}


				if (runeWidth < 1) {
					Contents [Row, Col].Rune = Rune.ReplacementChar;

				} else if (runeWidth == 1) {
					Contents [Row, Col].Rune = rune;
					if (Col < Clip.Right - 1) {
						Contents [Row, Col + 1].IsDirty = true;
					}
				} else if (runeWidth == 2) {
					if (Col == Clip.Right - 1) {
						// We're at the right edge of the clip, so we can't display a wide character.
						// TODO: Figure out if it is better to show a replacement character or ' '
						Contents [Row, Col].Rune = Rune.ReplacementChar;
					} else {
						Contents [Row, Col].Rune = rune;
						if (Col < Clip.Right - 1) {
							// Invalidate cell to right so that it doesn't get drawn
							// TODO: Figure out if it is better to show a replacement character or ' '
							Contents [Row, Col + 1].Rune = Rune.ReplacementChar;
							Contents [Row, Col + 1].IsDirty = true;
						}
					}
				} else {
					// This is a non-spacing character, so we don't need to do anything
					Contents [Row, Col].Rune = (Rune)' ';
					Contents [Row, Col].IsDirty = false;
				}
				_dirtyLines [Row] = true;
			}
		}

		if (runeWidth is < 0 or > 0) {
			Col++;
		}

		if (runeWidth > 1) {
			Debug.Assert (runeWidth <= 2);
			if (validLocation && Col < Clip.Right) {
				// This is a double-width character, and we are not at the end of the line.
				// Col now points to the second column of the character. Ensure it doesn't
				// Get rendered.
				Contents [Row, Col].IsDirty = false;
				Contents [Row, Col].Attribute = CurrentAttribute;

				// TODO: Determine if we should wipe this out (for now now)
				//Contents [Row, Col].Rune = (Rune)' ';
			}
			Col++;
		}
	}

	/// <summary>
	/// Adds the specified <see langword="char"/> to the display at the current cursor position. This method
	/// is a convenience method that calls <see cref="AddRune(Rune)"/> with the <see cref="Rune"/> constructor.
	/// </summary>
	/// <param name="c">Character to add.</param>
	public void AddRune (char c) => AddRune (new Rune (c));

	/// <summary>
	/// Adds the <paramref name="str"/> to the display at the cursor position.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When the method returns, <see cref="Col"/> will be incremented by the number of columns <paramref name="str"/> required,
	/// unless the new column value is outside of the <see cref="Clip"/> or screen dimensions defined by <see cref="Cols"/>.
	/// </para>
	/// <para>
	/// If <paramref name="str"/> requires more columns than are available, the output will be clipped.
	/// </para>
	/// </remarks>
	/// <param name="str">String.</param>
	public void AddStr (string str)
	{
		foreach (var rune in str.EnumerateRunes ()) {
			AddRune (rune);
		}
	}

	Rect _clip;

	/// <summary>
	/// Tests whether the specified coordinate are valid for drawing. 
	/// </summary>
	/// <param name="col">The column.</param>
	/// <param name="row">The row.</param>
	/// <returns><see langword="false"/> if the coordinate is outside of the
	/// screen bounds or outside of <see cref="Clip"/>. <see langword="true"/> otherwise.</returns>
	public bool IsValidLocation (int col, int row) =>
		col >= 0 && row >= 0 &&
		col < Cols && row < Rows &&
		Clip.Contains (col, row);

	/// <summary>
	/// Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(string)"/> are 
	/// subject to.
	/// </summary>
	/// <value>The rectangle describing the bounds of <see cref="Clip"/>.</value>
	public Rect Clip {
		get => _clip;
		set => _clip = value;
	}

	/// <summary>
	/// Updates the screen to reflect all the changes that have been done to the display buffer
	/// </summary>
	public abstract void Refresh ();

	/// <summary>
	/// Sets the position of the terminal cursor to <see cref="Col"/> and <see cref="Row"/>.
	/// </summary>
	public abstract void UpdateCursor ();

	/// <summary>
	/// Gets the terminal cursor visibility.
	/// </summary>
	/// <param name="visibility">The current <see cref="CursorVisibility"/></param>
	/// <returns><see langword="true"/> upon success</returns>
	public abstract bool GetCursorVisibility (out CursorVisibility visibility);

	/// <summary>
	/// Sets the terminal cursor visibility.
	/// </summary>
	/// <param name="visibility">The wished <see cref="CursorVisibility"/></param>
	/// <returns><see langword="true"/> upon success</returns>
	public abstract bool SetCursorVisibility (CursorVisibility visibility);

	/// <summary>
	/// Determines if the terminal cursor should be visible or not and sets it accordingly.
	/// </summary>
	/// <returns><see langword="true"/> upon success</returns>
	public abstract bool EnsureCursorVisibility ();

	// As performance is a concern, we keep track of the dirty lines and only refresh those.
	// This is in addition to the dirty flag on each cell.
	internal bool [] _dirtyLines;

	/// <summary>
	/// Clears the <see cref="Contents"/> of the driver.
	/// </summary>
	public void ClearContents ()
	{
		// TODO: This method is really "Clear Contents" now and should not be abstract (or virtual)
		Contents = new Cell [Rows, Cols];
		Clip = new Rect (0, 0, Cols, Rows);
		_dirtyLines = new bool [Rows];

		lock (Contents) {
			// Can raise an exception while is still resizing.
			try {
				for (var row = 0; row < Rows; row++) {
					for (var c = 0; c < Cols; c++) {
						Contents [row, c] = new Cell () {
							Rune = (Rune)' ',
							Attribute = new Attribute (Color.White, Color.Black),
							IsDirty = true
						};
						_dirtyLines [row] = true;
					}
				}
			} catch (IndexOutOfRangeException) { }
		}
	}

	/// <summary>
	/// Redraws the physical screen with the contents that have been queued up via any of the printing commands.
	/// </summary>
	public abstract void UpdateScreen ();

	#region Color Handling

	/// <summary>
	/// Gets whether the <see cref="ConsoleDriver"/> supports TrueColor output.
	/// </summary>
	public virtual bool SupportsTrueColor { get => true; }

	/// <summary>
	/// Gets or sets whether the <see cref="ConsoleDriver"/> should use 16 colors instead of the default TrueColors. See <see cref="Application.Force16Colors"/>
	/// to change this setting via <see cref="ConfigurationManager"/>.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Will be forced to <see langword="true"/> if <see cref="ConsoleDriver.SupportsTrueColor"/> is  <see langword="false"/>, indicating
	/// that the <see cref="ConsoleDriver"/> cannot support TrueColor.
	/// </para>
	/// </remarks>
	internal virtual bool Force16Colors {
		get => Application.Force16Colors || !SupportsTrueColor;
		set => Application.Force16Colors = (value || !SupportsTrueColor);
	}

	Attribute _currentAttribute;

	/// <summary>
	/// The <see cref="Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or <see cref="AddStr"/> call.
	/// </summary>
	public Attribute CurrentAttribute {
		get => _currentAttribute;
		set {
			if (Application.Driver != null) {
				_currentAttribute = new Attribute (value.Foreground, value.Background);
				return;
			}

			_currentAttribute = value;
		}
	}

	/// <summary>
	/// Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.
	/// </summary>
	/// <remarks>
	/// Implementations should call <c>base.SetAttribute(c)</c>.
	/// </remarks>
	/// <param name="c">C.</param>
	public Attribute SetAttribute (Attribute c)
	{
		var prevAttribute = CurrentAttribute;
		CurrentAttribute = c;
		return prevAttribute;
	}

	/// <summary>
	/// Gets the current <see cref="Attribute"/>.
	/// </summary>
	/// <returns>The current attribute.</returns>
	public Attribute GetAttribute () => CurrentAttribute;

	// TODO: This is only overridden by CursesDriver. Once CursesDriver supports 24-bit color, this virtual method can be
	// removed (and Attribute can lose the platformColor property).
	/// <summary>
	/// Makes an <see cref="Attribute"/>.
	/// </summary>
	/// <param name="foreground">The foreground color.</param>
	/// <param name="background">The background color.</param>
	/// <returns>The attribute for the foreground and background colors.</returns>
	public virtual Attribute MakeColor (Color foreground, Color background)
	{
		// Encode the colors into the int value.
		return new Attribute (
			platformColor: 0, // only used by cursesdriver!
			foreground: foreground,
			background: background
		);
	}


	#endregion

	#region Mouse and Keyboard

	/// <summary>
	/// Event fired after a key has been pressed and released.
	/// </summary>
	public event EventHandler<KeyEventEventArgs> KeyPressed;

	/// <summary>
	/// Called after a key has been pressed and released. Fires the <see cref="KeyPressed"/> event.
	/// </summary>
	/// <param name="a"></param>
	public void OnKeyPressed (KeyEventEventArgs a) => KeyPressed?.Invoke(this, a);

	/// <summary>
	/// Event fired when a key is released.
	/// </summary>
	public event EventHandler<KeyEventEventArgs> KeyUp;

	/// <summary>
	/// Called when a key is released. Fires the <see cref="KeyUp"/> event.
	/// </summary>
	/// <param name="a"></param>
	public void OnKeyUp (KeyEventEventArgs a) => KeyUp?.Invoke (this, a);

	/// <summary>
	/// Event fired when a key is pressed.
	/// </summary>
	public event EventHandler<KeyEventEventArgs> KeyDown;

	/// <summary>
	/// Called when a key is pressed. Fires the <see cref="KeyDown"/> event.
	/// </summary>
	/// <param name="a"></param>
	public void OnKeyDown (KeyEventEventArgs a) => KeyDown?.Invoke (this, a);
	
	/// <summary>
	/// Event fired when a mouse event occurs.
	/// </summary>
	public event EventHandler<MouseEventEventArgs> MouseEvent;

	/// <summary>
	/// Called when a mouse event occurs. Fires the <see cref="MouseEvent"/> event.
	/// </summary>
	/// <param name="a"></param>
	public void OnMouseEvent (MouseEventEventArgs a) => MouseEvent?.Invoke (this, a);

	/// <summary>
	/// Simulates a key press.
	/// </summary>
	/// <param name="keyChar">The key character.</param>
	/// <param name="key">The key.</param>
	/// <param name="shift">If <see langword="true"/> simulates the Shift key being pressed.</param>
	/// <param name="alt">If <see langword="true"/> simulates the Alt key being pressed.</param>
	/// <param name="ctrl">If <see langword="true"/> simulates the Ctrl key being pressed.</param>
	public abstract void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool ctrl);

	#endregion

	/// <summary>
	/// Enables diagnostic functions
	/// </summary>
	[Flags]
	public enum DiagnosticFlags : uint {
		/// <summary>
		/// All diagnostics off
		/// </summary>
		Off = 0b_0000_0000,
		/// <summary>
		/// When enabled, <see cref="Frame.OnDrawFrames"/> will draw a 
		/// ruler in the frame for any side with a padding value greater than 0.
		/// </summary>
		FrameRuler = 0b_0000_0001,
		/// <summary>
		/// When enabled, <see cref="Frame.OnDrawFrames"/> will draw a 
		/// 'L', 'R', 'T', and 'B' when clearing <see cref="Thickness"/>'s instead of ' '.
		/// </summary>
		FramePadding = 0b_0000_0010,
	}

	/// <summary>
	/// Set flags to enable/disable <see cref="ConsoleDriver"/> diagnostics.
	/// </summary>
	public static DiagnosticFlags Diagnostics { get; set; }

	/// <summary>
	/// Suspends the application (e.g. on Linux via SIGTSTP) and upon resume, resets the console driver.
	/// </summary>
	/// <remarks>This is only implemented in <see cref="CursesDriver"/>.</remarks>
	public abstract void Suspend ();

	// TODO: Move FillRect to ./Drawing	
	/// <summary>
	/// Fills the specified rectangle with the specified rune.
	/// </summary>
	/// <param name="rect"></param>
	/// <param name="rune"></param>
	public void FillRect (Rect rect, Rune rune = default)
	{
		for (var r = rect.Y; r < rect.Y + rect.Height; r++) {
			for (var c = rect.X; c < rect.X + rect.Width; c++) {
				Application.Driver.Move (c, r);
				Application.Driver.AddRune (rune == default ? new Rune (' ') : rune);
			}
		}
	}

	/// <summary>
	/// Fills the specified rectangle with the specified <see langword="char"/>. This method
	/// is a convenience method that calls <see cref="FillRect(Rect, Rune)"/>.
	/// </summary>
	/// <param name="rect"></param>
	/// <param name="c"></param>
	public void FillRect (Rect rect, char c) => FillRect (rect, new Rune (c));

	/// <summary>
	/// Returns the name of the driver and relevant library version information.
	/// </summary>
	/// <returns></returns>
	public virtual string GetVersionInfo () => GetType ().Name;
}


/// <summary>
/// Terminal Cursor Visibility settings.
/// </summary>
/// <remarks>
/// Hex value are set as 0xAABBCCDD where :
///
///     AA stand for the TERMINFO DECSUSR parameter value to be used under Linux and MacOS
///     BB stand for the NCurses curs_set parameter value to be used under Linux and MacOS
///     CC stand for the CONSOLE_CURSOR_INFO.bVisible parameter value to be used under Windows
///     DD stand for the CONSOLE_CURSOR_INFO.dwSize parameter value to be used under Windows
///</remarks>
public enum CursorVisibility {
	/// <summary>
	///	Cursor caret has default
	/// </summary>
	/// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/>. This default directly depends of the XTerm user configuration settings so it could be Block, I-Beam, Underline with possible blinking.</remarks>
	Default = 0x00010119,

	/// <summary>
	///	Cursor caret is hidden
	/// </summary>
	Invisible = 0x03000019,

	/// <summary>
	///	Cursor caret is normally shown as a blinking underline bar _
	/// </summary>
	Underline = 0x03010119,

	/// <summary>
	///	Cursor caret is normally shown as a underline bar _
	/// </summary>
	/// <remarks>Under Windows, this is equivalent to <see ref="UnderscoreBlinking"/></remarks>
	UnderlineFix = 0x04010119,

	/// <summary>
	///	Cursor caret is displayed a blinking vertical bar |
	/// </summary>
	/// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/></remarks>
	Vertical = 0x05010119,

	/// <summary>
	///	Cursor caret is displayed a blinking vertical bar |
	/// </summary>
	/// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Underscore"/></remarks>
	VerticalFix = 0x06010119,

	/// <summary>
	///	Cursor caret is displayed as a blinking block ▉
	/// </summary>
	Box = 0x01020164,

	/// <summary>
	///	Cursor caret is displayed a block ▉
	/// </summary>
	/// <remarks>Works under Xterm-like terminal otherwise this is equivalent to <see ref="Block"/></remarks>
	BoxFix = 0x02020164,
}
