//
// ConsoleDriver.cs: Base class for Terminal.Gui ConsoleDriver implementations.
//
using System.Text;
using System;
using System.Diagnostics;

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
	/// Prepare the driver and set the key and mouse events handlers.
	/// </summary>
	/// <param name="mainLoop">The main loop.</param>
	/// <param name="keyHandler">The handler for ProcessKey</param>
	/// <param name="keyDownHandler">The handler for key down events</param>
	/// <param name="keyUpHandler">The handler for key up events</param>
	/// <param name="mouseHandler">The handler for mouse events</param>
	public abstract void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<KeyEvent> keyDownHandler, Action<KeyEvent> keyUpHandler, Action<MouseEvent> mouseHandler);

	/// <summary>
	/// The handler fired when the terminal is resized.
	/// </summary>
	protected Action TerminalResized;

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
	/// <para>
	/// If <see langword="false"/> (the default) the height of the Terminal.Gui application (<see cref="Rows"/>) 
	/// tracks to the height of the visible console view when the console is resized. In this case 
	/// scrolling in the console will be disabled and all <see cref="Rows"/> will remain visible.
	/// </para>
	/// <para>
	/// If <see langword="true"/> then height of the Terminal.Gui application <see cref="Rows"/> only tracks 
	/// the height of the visible console view when the console is made larger (the application will only grow in height, never shrink). 
	/// In this case console scrolling is enabled and the contents (<see cref="Rows"/> high) will scroll
	/// as the console scrolls. 
	/// </para>
	/// </summary>
	/// <remarks>
	/// NOTE: This functionaliy is currently broken on Windows Terminal.
	/// </remarks>
	public bool EnableConsoleScrolling { get; set; }

	/// <summary>
	/// The contents of the application output. The driver outputs this buffer to the terminal when <see cref="UpdateScreen"/>
	/// is called.
	/// <remarks>
	/// The format of the array is rows, columns, and 3 values on the last column: Rune, Attribute and Dirty Flag
	/// </remarks>
	/// </summary>
	public int [,,] Contents { get; internal set; }

	/// <summary>
	/// Initializes the driver
	/// </summary>
	/// <param name="terminalResized">Method to invoke when the terminal is resized.</param>
	public abstract void Init (Action terminalResized);

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
		if (rune.Value > RuneExtensions.MaxUnicodeCodePoint) {
			return false;
		}
		return true;
	}

	/// <summary>
	/// Adds the specified rune to the display at the current cursor position. 
	/// </summary>
	/// <remarks>
	/// <para>
	/// When the method returns, <see cref="Col"/> will be incremented by the number of columns <paramref name="rune"/> required,
	/// unless the new column value is outside of the <see cref="Clip"/> or screen dimensions defined by <see cref="Cols"/>.
	/// </para>
	/// <para>
	/// If <paramref name="rune"/> requires more than one column, and <see cref="Col"/> plus the number of columns needed
	/// exceeds the <see cref="Clip"/> or screen dimensions, the default Unicode replacement character (U+FFFD) will be added instead.
	/// </para>
	/// </remarks>
	/// <param name="rune">Rune to add.</param>
	public abstract void AddRune (Rune rune); 

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
		foreach (var rune in str.EnumerateRunes()) {
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

	/// <summary>
	/// Clears the <see cref="Contents"/>buffer and the driver buffer.
	/// </summary>
	public abstract void UpdateOffScreen ();

	/// <summary>
	/// Redraws the physical screen with the contents that have been queued up via any of the printing commands.
	/// </summary>
	public abstract void UpdateScreen ();

	#region Color Handling
	
	Attribute _currentAttribute;

	/// <summary>
	/// The <see cref="Attribute"/> that will be used for the next <see cref="AddRune(Rune)"/> or <see cref="AddStr"/> call.
	/// </summary>
	public Attribute CurrentAttribute {
		get => _currentAttribute;
		set {
			if (value is { Initialized: false, HasValidColors: true } && Application.Driver != null) {
				_currentAttribute = Application.Driver.MakeAttribute (value.Foreground, value.Background);
				return;
			}
			if (!value.Initialized) Debug.WriteLine ("ConsoleDriver.CurrentAttribute: Attributes must be initialized before use.");

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
	/// Make the attribute for the foreground and background colors.
	/// </summary>
	/// <param name="fore">Foreground.</param>
	/// <param name="back">Background.</param>
	/// <returns></returns>
	public virtual Attribute MakeAttribute (Color fore, Color back)
	{
		return MakeColor (fore, back);
	}

	/// <summary>
	/// Gets the foreground and background colors based on a platform-dependent color value.
	/// </summary>
	/// <param name="value">The platform-dependent color value.</param>
	/// <param name="foreground">The foreground.</param>
	/// <param name="background">The background.</param>
	internal abstract void GetColors (int value, out Color foreground, out Color background);

	/// <summary>
	/// Gets the current <see cref="Attribute"/>.
	/// </summary>
	/// <returns>The current attribute.</returns>
	public Attribute GetAttribute () => CurrentAttribute;

	/// <summary>
	/// Makes an <see cref="Attribute"/>.
	/// </summary>
	/// <param name="foreground">The foreground color.</param>
	/// <param name="background">The background color.</param>
	/// <returns>The attribute for the foreground and background colors.</returns>
	public abstract Attribute MakeColor (Color foreground, Color background);

	/// <summary>
	/// Ensures all <see cref="Attribute"/>s in <see cref="Colors.ColorSchemes"/> are correctly 
	/// initialized by the driver.
	/// </summary>
	public void InitializeColorSchemes ()
	{
		// Ensure all Attributes are initialized by the driver
		foreach (var s in Colors.ColorSchemes) {
			s.Value.Initialize ();
		}
	}

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

	/// <summary>
	/// Simulates a key press.
	/// </summary>
	/// <param name="keyChar">The key character.</param>
	/// <param name="key">The key.</param>
	/// <param name="shift">If <see langword="true"/> simulates the Shift key being pressed.</param>
	/// <param name="alt">If <see langword="true"/> simulates the Alt key being pressed.</param>
	/// <param name="ctrl">If <see langword="true"/> simulates the Ctrl key being pressed.</param>
	public abstract void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool ctrl);

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
	/// Ends the execution of the console driver.
	/// </summary>
	public abstract void End ();
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
