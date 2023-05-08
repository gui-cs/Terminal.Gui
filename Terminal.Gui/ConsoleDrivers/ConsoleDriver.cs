//
// ConsoleDriver.cs: Base class for Terminal.Gui ConsoleDriver implementations.
//
using NStack;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Terminal.Gui {
	/// <summary>
	/// Cursors Visibility that are displayed
	/// </summary>
	// 
	// Hexa value are set as 0xAABBCCDD where :
	//
	//     AA stand for the TERMINFO DECSUSR parameter value to be used under Linux & MacOS
	//     BB stand for the NCurses curs_set parameter value to be used under Linux & MacOS
	//     CC stand for the CONSOLE_CURSOR_INFO.bVisible parameter value to be used under Windows
	//     DD stand for the CONSOLE_CURSOR_INFO.dwSize parameter value to be used under Windows
	//
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

	/// <summary>
	/// ConsoleDriver is an abstract class that defines the requirements for a console driver.  
	/// There are currently three implementations: <see cref="CursesDriver"/> (for Unix and Mac), <see cref="WindowsDriver"/>, and <see cref="NetDriver"/> that uses the .NET Console API.
	/// </summary>
	public abstract class ConsoleDriver {
		/// <summary>
		/// The handler fired when the terminal is resized.
		/// </summary>
		protected Action TerminalResized;

		/// <summary>
		/// The current number of columns in the terminal.
		/// </summary>
		public abstract int Cols { get; }

		/// <summary>
		/// The current number of rows in the terminal.
		/// </summary>
		public abstract int Rows { get; }

		/// <summary>
		/// The current left in the terminal.
		/// </summary>
		public abstract int Left { get; }

		/// <summary>
		/// The current top in the terminal.
		/// </summary>
		public abstract int Top { get; }

		/// <summary>
		/// Get the operation system clipboard.
		/// </summary>
		public abstract IClipboard Clipboard { get; }

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
		public abstract bool EnableConsoleScrolling { get; set; }

		/// <summary>
		/// The format is rows, columns and 3 values on the last column: Rune, Attribute and Dirty Flag
		/// </summary>
		public virtual int [,,] Contents { get; }

		/// <summary>
		/// Initializes the driver
		/// </summary>
		/// <param name="terminalResized">Method to invoke when the terminal is resized.</param>
		public abstract void Init (Action terminalResized);
		/// <summary>
		/// Moves the cursor to the specified column and row.
		/// </summary>
		/// <param name="col">Column to move the cursor to.</param>
		/// <param name="row">Row to move the cursor to.</param>
		public abstract void Move (int col, int row);

		/// <summary>
		/// Adds the specified rune to the display at the current cursor position.
		/// </summary>
		/// <param name="rune">Rune to add.</param>
		public abstract void AddRune (Rune rune);

		/// <summary>
		/// Ensures a Rune is not a control character and can be displayed by translating characters below 0x20
		/// to equivalent, printable, Unicode chars.
		/// </summary>
		/// <param name="c">Rune to translate</param>
		/// <returns></returns>
		public static Rune MakePrintable (Rune c)
		{
			if (c <= 0x1F || (c >= 0X7F && c <= 0x9F)) {
				// ASCII (C0) control characters.
				// C1 control characters (https://www.aivosto.com/articles/control-characters.html#c1)
				return new Rune (c + 0x2400);
			}

			return c;
		}

		/// <summary>
		/// Adds the <paramref name="str"/> to the display at the cursor position.
		/// </summary>
		/// <param name="str">String.</param>
		public abstract void AddStr (ustring str);

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
		/// Updates the screen to reflect all the changes that have been done to the display buffer
		/// </summary>
		public abstract void Refresh ();

		/// <summary>
		/// Updates the location of the cursor position
		/// </summary>
		public abstract void UpdateCursor ();

		/// <summary>
		/// Retreive the cursor caret visibility
		/// </summary>
		/// <param name="visibility">The current <see cref="CursorVisibility"/></param>
		/// <returns>true upon success</returns>
		public abstract bool GetCursorVisibility (out CursorVisibility visibility);

		/// <summary>
		/// Change the cursor caret visibility
		/// </summary>
		/// <param name="visibility">The wished <see cref="CursorVisibility"/></param>
		/// <returns>true upon success</returns>
		public abstract bool SetCursorVisibility (CursorVisibility visibility);

		/// <summary>
		/// Ensure the cursor visibility
		/// </summary>
		/// <returns>true upon success</returns>
		public abstract bool EnsureCursorVisibility ();

		/// <summary>
		/// Ends the execution of the console driver.
		/// </summary>
		public abstract void End ();

		/// <summary>
		/// Resizes the clip area when the screen is resized.
		/// </summary>
		public abstract void ResizeScreen ();

		/// <summary>
		/// Reset and recreate the contents and the driver buffer.
		/// </summary>
		public abstract void UpdateOffScreen ();

		/// <summary>
		/// Redraws the physical screen with the contents that have been queued up via any of the printing commands.
		/// </summary>
		public abstract void UpdateScreen ();

		/// <summary>
		/// The current attribute the driver is using. 
		/// </summary>
		public virtual Attribute CurrentAttribute {
			get => _currentAttribute;
			set {
				if (!value.Initialized && value.HasValidColors && Application.Driver != null) {
					CurrentAttribute = Application.Driver.MakeAttribute (value.Foreground, value.Background);
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
		public virtual void SetAttribute (Attribute c)
		{
			CurrentAttribute = c;
		}
		
		/// <summary>
		/// Gets the foreground and background colors based on the value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="foreground">The foreground.</param>
		/// <param name="background">The background.</param>
		/// <returns></returns>
		public abstract bool GetColors (int value, out Color foreground, out Color background);

		/// <summary>
		/// Allows sending keys without typing on a keyboard.
		/// </summary>
		/// <param name="keyChar">The character key.</param>
		/// <param name="key">The key.</param>
		/// <param name="shift">If shift key is sending.</param>
		/// <param name="alt">If alt key is sending.</param>
		/// <param name="control">If control key is sending.</param>
		public abstract void SendKeys (char keyChar, ConsoleKey key, bool shift, bool alt, bool control);

		/// <summary>
		/// Set the handler when the terminal is resized.
		/// </summary>
		/// <param name="terminalResized"></param>
		public void SetTerminalResized (Action terminalResized)
		{
			TerminalResized = terminalResized;
		}

		/// <summary>
		/// Fills the specified rectangle with the specified rune.
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="rune"></param>
		public virtual void FillRect (Rect rect, System.Rune rune = default)
		{
			for (var r = rect.Y; r < rect.Y + rect.Height; r++) {
				for (var c = rect.X; c < rect.X + rect.Width; c++) {
					Application.Driver.Move (c, r);
					Application.Driver.AddRune (rune == default ? ' ' : rune);
				}
			}
		}

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
		/// Tests whether the specified coordinate are valid for drawing. 
		/// </summary>
		/// <param name="col">The column.</param>
		/// <param name="row">The row.</param>
		/// <returns><see langword="false"/> if the coordinate is outside of the
		/// screen bounds or outside either <see cref="Clip"/> or
		/// <see cref="ClipRegion"/>. <see langword="true"/> otherwise.</returns>
		public bool IsValidLocation (int col, int row) =>
			col >= 0 && row >= 0 &&
			col < Cols && row < Rows &&
			IsInClipRegion (row, col);

		/// <summary>
		/// Gets or sets the clip rectangle that <see cref="AddRune(Rune)"/> and <see cref="AddStr(ustring)"/> are 
		/// subject to. Setting this property is equivalent to calling <see cref="ClearClipRegion()"/>
		/// and <see cref="AddToClipRegion(Rect)"/>.
		/// </summary>
		/// <value>The rectangle describing the bounds of <see cref="ClipRegion"/>.</value>
		public Rect Clip {
			get {
				if (ClipRegion.Count == 0) {
					return new Rect (0, 0, Cols, Rows);
				}

				int minX = ClipRegion.Min (rect => rect.X);
				int minY = ClipRegion.Min (rect => rect.Y);
				int maxX = ClipRegion.Max (rect => rect.X + rect.Width);
				int maxY = ClipRegion.Max (rect => rect.Y + rect.Height);

				return new Rect (minX, minY, maxX - minX, maxY - minY);
			}
			set {
				ClearClipRegion ();
				AddToClipRegion (value);
			}
		}

		List<Rect> _clipRegion = new List<Rect> ();

		/// <summary>
		/// The clipping region that <see cref="AddRune(Rune)"/> and <see cref="AddStr(ustring)"/> are 
		/// subject to. The clip region is an irregularly shaped area defined by the intersection of a set
		/// of rectangles added with <see cref="AddToClipRegion(Rect)"/>. To clear the clip region call <see cref="ClearClipRegion"/>.
		/// </summary>
		/// <value>The clip.</value>
		public List<Rect> ClipRegion {
			get => _clipRegion;
			set => _clipRegion = value;
		}

		/// <summary>
		/// Expands <see cref="ClipRegion"/> to include <paramref name="rect"/>.
		/// </summary>
		/// <param name="rect"></param>
		/// <returns>The updated <see cref="ClipRegion"/>.</returns>
		public List<Rect> AddToClipRegion (Rect rect)
		{
			ClipRegion.Add (rect);
			return ClipRegion;
		}

		/// <summary>
		/// Clears the <see cref="ClipRegion"/>. This has the same effect as expanding the clip
		/// region to include the entire screen.
		/// </summary>
		public void ClearClipRegion ()
		{
			ClipRegion.Clear ();
		}

		/// <summary>
		/// Tests if the specified coordinates are within the <see cref="ClipRegion"/>.
		/// </summary>
		/// <param name="col"></param>
		/// <param name="row"></param>
		/// <returns><see langword="true"/> if <paramref name="col"/> and <paramref name="col"/> is
		/// within the clip region.</returns>
		private bool IsInClipRegion (int col, int row) => ClipRegion.Count == 0 || ClipRegion.Any (rect => rect.Contains (row, col));

		/// <summary>
		/// Start of mouse moves.
		/// </summary>
		public abstract void StartReportingMouseMoves ();

		/// <summary>
		/// Stop reporting mouses moves.
		/// </summary>
		public abstract void StopReportingMouseMoves ();
		
		Attribute _currentAttribute;

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
		/// Gets the current <see cref="Attribute"/>.
		/// </summary>
		/// <returns>The current attribute.</returns>
		public Attribute GetAttribute () => CurrentAttribute;

		/// <summary>
		/// Make the <see cref="Colors"/> for the <see cref="ColorScheme"/>.
		/// </summary>
		/// <param name="foreground">The foreground color.</param>
		/// <param name="background">The background color.</param>
		/// <returns>The attribute for the foreground and background colors.</returns>
		public abstract Attribute MakeColor (Color foreground, Color background);

		/// <summary>
		/// Ensures all <see cref="Attribute"/>s in <see cref="Colors.ColorSchemes"/> are correctly 
		/// initialized by the driver.
		/// </summary>
		/// <param name="supportsColors">Flag indicating if colors are supported (not used).</param>
		public void InitializeColorSchemes (bool supportsColors = true)
		{
			// Ensure all Attributes are initialized by the driver
			foreach (var s in Colors.ColorSchemes) {
				s.Value.Initialize ();
			}
		}
	}

	/// <summary>
	/// Helper class for console drivers to invoke shell commands to interact with the clipboard.
	/// Used primarily by CursesDriver, but also used in Unit tests which is why it is in
	/// ConsoleDriver.cs.
	/// </summary>
	internal static class ClipboardProcessRunner {
		public static (int exitCode, string result) Bash (string commandLine, string inputText = "", bool waitForOutput = false)
		{
			var arguments = $"-c \"{commandLine}\"";
			var (exitCode, result) = Process ("bash", arguments, inputText, waitForOutput);

			return (exitCode, result.TrimEnd ());
		}

		public static (int exitCode, string result) Process (string cmd, string arguments, string input = null, bool waitForOutput = true)
		{
			var output = string.Empty;

			using (Process process = new Process {
				StartInfo = new ProcessStartInfo {
					FileName = cmd,
					Arguments = arguments,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					RedirectStandardInput = true,
					UseShellExecute = false,
					CreateNoWindow = true,
				}
			}) {
				var eventHandled = new TaskCompletionSource<bool> ();
				process.Start ();
				if (!string.IsNullOrEmpty (input)) {
					process.StandardInput.Write (input);
					process.StandardInput.Close ();
				}

				if (!process.WaitForExit (5000)) {
					var timeoutError = $@"Process timed out. Command line: {process.StartInfo.FileName} {process.StartInfo.Arguments}.";
					throw new TimeoutException (timeoutError);
				}

				if (waitForOutput && process.StandardOutput.Peek () != -1) {
					output = process.StandardOutput.ReadToEnd ();
				}

				if (process.ExitCode > 0) {
					output = $@"Process failed to run. Command line: {cmd} {arguments}.
										Output: {output}
										Error: {process.StandardError.ReadToEnd ()}";
				}

				return (process.ExitCode, output);
			}
		}

		public static bool DoubleWaitForExit (this System.Diagnostics.Process process)
		{
			var result = process.WaitForExit (500);
			if (result) {
				process.WaitForExit ();
			}
			return result;
		}

		public static bool FileExists (this string value)
		{
			return !string.IsNullOrEmpty (value) && !value.Contains ("not found");
		}
	}
}
