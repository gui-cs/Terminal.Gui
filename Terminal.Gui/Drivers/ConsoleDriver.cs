//
// Driver.cs: Definition for the Console Driver API
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Mono.Terminal;
using NStack;
using Unix.Terminal;

namespace Terminal.Gui {

	/// <summary>
	/// Basic colors that can be used to set the foreground and background colors in console applications.  These can only be
	/// </summary>
	public enum Color {
		/// <summary>
		/// The black color.
		/// </summary>
		Black,
		/// <summary>
		/// The blue color.
		/// </summary>
		Blue,
		/// <summary>
		/// The green color.
		/// </summary>
		Green,
		/// <summary>
		/// The cyan color.
		/// </summary>
		Cyan,
		/// <summary>
		/// The red color.
		/// </summary>
		Red,
		/// <summary>
		/// The magenta color.
		/// </summary>
		Magenta,
		/// <summary>
		/// The brown color.
		/// </summary>
		Brown,
		/// <summary>
		/// The gray color.
		/// </summary>
		Gray,
		/// <summary>
		/// The dark gray color.
		/// </summary>
		DarkGray,
		/// <summary>
		/// The bright bBlue color.
		/// </summary>
		BrightBlue,
		/// <summary>
		/// The bright green color.
		/// </summary>
		BrightGreen,
		/// <summary>
		/// The brigh cyan color.
		/// </summary>
		BrighCyan,
		/// <summary>
		/// The bright red color.
		/// </summary>
		BrightRed,
		/// <summary>
		/// The bright magenta color.
		/// </summary>
		BrightMagenta,
		/// <summary>
		/// The bright yellow color.
		/// </summary>
		BrightYellow,
		/// <summary>
		/// The White color.
		/// </summary>
		White
	}

	/// <summary>
	/// Attributes are used as elements that contain both a foreground and a background or platform specific features
	/// </summary>
	/// <remarks>
	///   Attributes are needed to map colors to terminal capabilities that might lack colors, on color
	///   scenarios, they encode both the foreground and the background color and are used in the ColorScheme
	///   class to define color schemes that can be used in your application.
	/// </remarks>
	public struct Attribute {
		internal int value;
		internal Color foreground;
		internal Color background;

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (int value, Color foreground = new Color(), Color background = new Color())
		{
			this.value = value;
			this.foreground = foreground;
			this.background = background;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> struct.
		/// </summary>
		/// <param name="foreground">Foreground</param>
		/// <param name="background">Background</param>
		public Attribute (Color foreground = new Color (), Color background = new Color ())
		{
			this.value = value = ((int)foreground | (int)background << 4);
			this.foreground = foreground;
			this.background = background;
		}

		/// <summary>
		/// Implicit conversion from an attribute to the underlying Int32 representation
		/// </summary>
		/// <returns>The integer value stored in the attribute.</returns>
		/// <param name="c">The attribute to convert</param>
		public static implicit operator int (Attribute c) => c.value;

		/// <summary>
		/// Implicitly convert an integer value into an attribute
		/// </summary>
		/// <returns>An attribute with the specified integer value.</returns>
		/// <param name="v">value</param>
		public static implicit operator Attribute (int v) => new Attribute (v);

		/// <summary>
		/// Creates an attribute from the specified foreground and background.
		/// </summary>
		/// <returns>The make.</returns>
		/// <param name="foreground">Foreground color to use.</param>
		/// <param name="background">Background color to use.</param>
		public static Attribute Make (Color foreground, Color background)
		{
			if (Application.Driver == null)
				throw new InvalidOperationException ("The Application has not been initialized");
			return Application.Driver.MakeAttribute (foreground, background);
		}
	}

	/// <summary>
	/// Color scheme definitions, they cover some common scenarios and are used
	/// typically in toplevel containers to set the scheme that is used by all the
	/// views contained inside.
	/// </summary>
	public class ColorScheme {
		Attribute _normal;
		Attribute _focus;
		Attribute _hotNormal;
		Attribute _hotFocus;
		Attribute _disabled;
		internal string caller = "";

		/// <summary>
		/// The default color for text, when the view is not focused.
		/// </summary>
		public Attribute Normal { get { return _normal; } set { _normal = SetAttribute (value); } }

		/// <summary>
		/// The color for text when the view has the focus.
		/// </summary>
		public Attribute Focus { get { return _focus; } set { _focus = SetAttribute (value); } }

		/// <summary>
		/// The color for the hotkey when a view is not focused
		/// </summary>
		public Attribute HotNormal { get { return _hotNormal; } set { _hotNormal = SetAttribute (value); } }

		/// <summary>
		/// The color for the hotkey when the view is focused.
		/// </summary>
		public Attribute HotFocus { get { return _hotFocus; } set { _hotFocus = SetAttribute (value); } }

		/// <summary>
		/// The default color for text, when the view is disabled.
		/// </summary>
		public Attribute Disabled { get { return _disabled; } set { _disabled = SetAttribute (value); } }

		bool preparingScheme = false;

		Attribute SetAttribute (Attribute attribute, [CallerMemberName]string callerMemberName = null)
		{
			if (!Application._initialized && !preparingScheme)
				return attribute;

			if (preparingScheme)
				return attribute;

			preparingScheme = true;
			switch (caller) {
			case "TopLevel":
				switch (callerMemberName) {
				case "Normal":
					HotNormal = Application.Driver.MakeAttribute (HotNormal.foreground, attribute.background);
					break;
				case "Focus":
					HotFocus = Application.Driver.MakeAttribute (HotFocus.foreground, attribute.background);
					break;
				case "HotNormal":
					HotFocus = Application.Driver.MakeAttribute (attribute.foreground, HotFocus.background);
					break;
				case "HotFocus":
					HotNormal = Application.Driver.MakeAttribute (attribute.foreground, HotNormal.background);
					if (Focus.foreground != attribute.background)
						Focus = Application.Driver.MakeAttribute (Focus.foreground, attribute.background);
					break;
				}
				break;

			case "Base":
				switch (callerMemberName) {
				case "Normal":
					HotNormal = Application.Driver.MakeAttribute (HotNormal.foreground, attribute.background);
					break;
				case "Focus":
					HotFocus = Application.Driver.MakeAttribute (HotFocus.foreground, attribute.background);
					break;
				case "HotNormal":
					HotFocus = Application.Driver.MakeAttribute (attribute.foreground, HotFocus.background);
					Normal = Application.Driver.MakeAttribute (Normal.foreground, attribute.background);
					break;
				case "HotFocus":
					HotNormal = Application.Driver.MakeAttribute (attribute.foreground, HotNormal.background);
					if (Focus.foreground != attribute.background)
						Focus = Application.Driver.MakeAttribute (Focus.foreground, attribute.background);
					break;
				}
				break;

			case "Menu":
				switch (callerMemberName) {
				case "Normal":
					if (Focus.background != attribute.background)
						Focus = Application.Driver.MakeAttribute (attribute.foreground, Focus.background);
					HotNormal = Application.Driver.MakeAttribute (HotNormal.foreground, attribute.background);
					Disabled = Application.Driver.MakeAttribute (Disabled.foreground, attribute.background);
					break;
				case "Focus":
					Normal = Application.Driver.MakeAttribute (attribute.foreground, Normal.background);
					HotFocus = Application.Driver.MakeAttribute (HotFocus.foreground, attribute.background);
					break;
				case "HotNormal":
					if (Focus.background != attribute.background)
						HotFocus = Application.Driver.MakeAttribute (attribute.foreground, HotFocus.background);
					Normal = Application.Driver.MakeAttribute (Normal.foreground, attribute.background);
					Disabled = Application.Driver.MakeAttribute (Disabled.foreground, attribute.background);
					break;
				case "HotFocus":
					HotNormal = Application.Driver.MakeAttribute (attribute.foreground, HotNormal.background);
					if (Focus.foreground != attribute.background)
						Focus = Application.Driver.MakeAttribute (Focus.foreground, attribute.background);
					break;
				case "Disabled":
					if (Focus.background != attribute.background)
						HotFocus = Application.Driver.MakeAttribute (attribute.foreground, HotFocus.background);
					Normal = Application.Driver.MakeAttribute (Normal.foreground, attribute.background);
					HotNormal = Application.Driver.MakeAttribute (HotNormal.foreground, attribute.background);
					break;

				}
				break;

			case "Dialog":
				switch (callerMemberName) {
				case "Normal":
					if (Focus.background != attribute.background)
						Focus = Application.Driver.MakeAttribute (attribute.foreground, Focus.background);
					HotNormal = Application.Driver.MakeAttribute (HotNormal.foreground, attribute.background);
					break;
				case "Focus":
					Normal = Application.Driver.MakeAttribute (attribute.foreground, Normal.background);
					HotFocus = Application.Driver.MakeAttribute (HotFocus.foreground, attribute.background);
					break;
				case "HotNormal":
					if (Focus.background != attribute.background)
						HotFocus = Application.Driver.MakeAttribute (attribute.foreground, HotFocus.background);
					if (Normal.foreground != attribute.background)
						Normal = Application.Driver.MakeAttribute (Normal.foreground, attribute.background);
					break;
				case "HotFocus":
					HotNormal = Application.Driver.MakeAttribute (attribute.foreground, HotNormal.background);
					if (Focus.foreground != attribute.background)
						Focus = Application.Driver.MakeAttribute (Focus.foreground, attribute.background);
					break;
				}
				break;

			case "Error":
				switch (callerMemberName) {
				case "Normal":
					HotNormal = Application.Driver.MakeAttribute (HotNormal.foreground, attribute.background);
					HotFocus = Application.Driver.MakeAttribute (HotFocus.foreground, attribute.background);
					break;
				case "HotNormal":
				case "HotFocus":
					HotFocus = Application.Driver.MakeAttribute (attribute.foreground, attribute.background);
					Normal = Application.Driver.MakeAttribute (Normal.foreground, attribute.background);
					break;
				}
				break;

			}
			preparingScheme = false;
			return attribute;
		}
	}

	/// <summary>
	/// The default ColorSchemes for the application.
	/// </summary>
	public static class Colors {
		static ColorScheme _toplevel;
		static ColorScheme _base;
		static ColorScheme _dialog;
		static ColorScheme _menu;
		static ColorScheme _error;

		/// <summary>
		/// The application toplevel color scheme, for the default toplevel views.
		/// </summary>
		public static ColorScheme TopLevel { get { return _toplevel; } set { _toplevel = SetColorScheme (value); } }

		/// <summary>
		/// The base color scheme, for the default toplevel views.
		/// </summary>
		public static ColorScheme Base { get { return _base; } set { _base = SetColorScheme (value); } }

		/// <summary>
		/// The dialog color scheme, for standard popup dialog boxes
		/// </summary>
		public static ColorScheme Dialog { get { return _dialog; } set { _dialog = SetColorScheme (value); } }

		/// <summary>
		/// The menu bar color
		/// </summary>
		public static ColorScheme Menu { get { return _menu; } set { _menu = SetColorScheme (value); } }

		/// <summary>
		/// The color scheme for showing errors.
		/// </summary>
		public static ColorScheme Error { get { return _error; } set { _error = SetColorScheme (value); } }

		static ColorScheme SetColorScheme (ColorScheme colorScheme, [CallerMemberName]string callerMemberName = null)
		{
			colorScheme.caller = callerMemberName;
			return colorScheme;
		}
	}

	/// <summary>
	/// Special characters that can be drawn with Driver.AddSpecial.
	/// </summary>
	public enum SpecialChar {
		/// <summary>
		/// Horizontal line character.
		/// </summary>
		HLine,

		/// <summary>
		/// Vertical line character.
		/// </summary>
		VLine,

		/// <summary>
		/// Stipple pattern
		/// </summary>
		Stipple,

		/// <summary>
		/// Diamond character
		/// </summary>
		Diamond,

		/// <summary>
		/// Upper left corner
		/// </summary>
		ULCorner,

		/// <summary>
		/// Lower left corner
		/// </summary>
		LLCorner,

		/// <summary>
		/// Upper right corner
		/// </summary>
		URCorner,

		/// <summary>
		/// Lower right corner
		/// </summary>
		LRCorner,

		/// <summary>
		/// Left tee
		/// </summary>
		LeftTee,

		/// <summary>
		/// Right tee
		/// </summary>
		RightTee,

		/// <summary>
		/// Top tee
		/// </summary>
		TopTee,

		/// <summary>
		/// The bottom tee.
		/// </summary>
		BottomTee,

	}

	/// <summary>
	/// ConsoleDriver is an abstract class that defines the requirements for a console driver.   One implementation if the CursesDriver, and another one uses the .NET Console one.
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
		/// Adds the specified rune to the display at the current cursor position
		/// </summary>
		/// <param name="rune">Rune to add.</param>
		public abstract void AddRune (Rune rune);
		/// <summary>
		/// Adds the specified
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
		/// Ends the execution of the console driver.
		/// </summary>
		public abstract void End ();

		/// <summary>
		/// Redraws the physical screen with the contents that have been queued up via any of the printing commands.
		/// </summary>
		public abstract void UpdateScreen ();

		/// <summary>
		/// Selects the specified attribute as the attribute to use for future calls to AddRune, AddString.
		/// </summary>
		/// <param name="c">C.</param>
		public abstract void SetAttribute (Attribute c);

		/// <summary>
		/// Set Colors from limit sets of colors.
		/// </summary>
		/// <param name="foreground">Foreground.</param>
		/// <param name="background">Background.</param>
		public abstract void SetColors (ConsoleColor foreground, ConsoleColor background);

		// Advanced uses - set colors to any pre-set pairs, you would need to init_color
		// that independently with the R, G, B values.
		/// <summary>
		/// Advanced uses - set colors to any pre-set pairs, you would need to init_color
		/// that independently with the R, G, B values.
		/// </summary>
		/// <param name="foregroundColorId">Foreground color identifier.</param>
		/// <param name="backgroundColorId">Background color identifier.</param>
		public abstract void SetColors (short foregroundColorId, short backgroundColorId);

		/// <summary>
		/// Set the handler when the terminal is resized.
		/// </summary>
		/// <param name="terminalResized"></param>
		public void SetTerminalResized(Action terminalResized)
		{
			TerminalResized = terminalResized;
		}

		// Useful for debugging (e.g. change to `*`
		const char clearChar = ' ';

		/// <summary>
		/// Draws the title for a Window-style view incorporating padding. 
		/// </summary>
		/// <param name="region">Screen relative region where the frame will be drawn.</param>
		/// <param name="title">The title for the window. The title will only be drawn if <c>title</c> is not null or empty and paddingTop is greater than 0.</param>
		/// <param name="paddingLeft">Number of columns to pad on the left (if 0 the border will not appear on the left).</param>
		/// <param name="paddingTop">Number of rows to pad on the top (if 0 the border and title will not appear on the top).</param>
		/// <param name="paddingRight">Number of columns to pad on the right (if 0 the border will not appear on the right).</param>
		/// <param name="paddingBottom">Number of rows to pad on the bottom (if 0 the border will not appear on the bottom).</param>
		/// <remarks></remarks>
		public void DrawWindowTitle (Rect region, ustring title, int paddingLeft, int paddingTop, int paddingRight, int paddingBottom)
		{
			var width = region.Width - (paddingLeft + 2) * 2;
			if (!ustring.IsNullOrEmpty(title) && width > 4) {
				
				Move (region.X + 1 + paddingLeft, region.Y + paddingTop);
				AddRune (' ');
				var str = title.Length >= width ? title [0, width - 2] : title;
				AddStr (str);
				AddRune (' ');
			}
		}

		/// <summary>
		/// Draws a frame for a window with padding aand n optional visible border inside the padding. 
		/// </summary>
		/// <param name="region">Screen relative region where the frame will be drawn.</param>
		/// <param name="paddingLeft">Number of columns to pad on the left (if 0 the border will not appear on the left).</param>
		/// <param name="paddingTop">Number of rows to pad on the top (if 0 the border and title will not appear on the top).</param>
		/// <param name="paddingRight">Number of columns to pad on the right (if 0 the border will not appear on the right).</param>
		/// <param name="paddingBottom">Number of rows to pad on the bottom (if 0 the border will not appear on the bottom).</param>
		/// <param name="border">If set to <c>true</c> and any padding dimension is > 0 the border will be drawn.</param>
		/// <param name="fill">If set to <c>true</c> it will clear the content area (the area inside the padding) with the current color, otherwise the content area will be left untouched.</param>
		public void DrawWindowFrame (Rect region, int paddingLeft = 0, int paddingTop = 0, int paddingRight = 0, int paddingBottom = 0, bool border = true, bool fill = false)
		{
			void AddRuneAt (int col, int row, Rune ch)
			{
				Move (col, row);
				AddRune (ch);
			}

			int fwidth = (int)(region.Width - (paddingRight + paddingLeft));
			int fheight = (int)(region.Height - (paddingBottom + paddingTop));
			int fleft = region.X + paddingLeft;
			int fright = fleft + fwidth + 1;
			int ftop = region.Y + paddingTop;
			int fbottom = ftop + fheight + 1;

			Rune hLine = border ? HLine : clearChar;
			Rune vLine = border ? VLine : clearChar;
			Rune uRCorner = border ? URCorner : clearChar;
			Rune uLCorner = border ? ULCorner : clearChar;
			Rune lLCorner = border ? LLCorner : clearChar;
			Rune lRCorner = border ? LRCorner : clearChar;

			// Outside top
			if (paddingTop > 1) {
				for (int r = region.Y; r < ftop; r++) {
					for (int c = region.X; c <= fright + paddingRight; c++) {
						AddRuneAt (c, r, clearChar);
					}
				}
			}

			// Outside top-left
			for (int c = region.X; c <= fleft; c++) {
				AddRuneAt (c, ftop, clearChar);
			}

			// Frame top-left corner
			AddRuneAt (fleft, ftop, paddingTop >= 0 ? (paddingLeft >= 0 ? uLCorner : hLine) : clearChar);

			// Frame top
			for (int c = fleft + 1; c <= fright; c++) {
				AddRuneAt (c, ftop, paddingTop > 0 ? hLine : clearChar);
			}

			// Frame top-right corner
			if (fright > fleft) {
				AddRuneAt (fright, ftop, paddingTop >= 0 ? (paddingRight >= 0 ? uRCorner : hLine) : clearChar);
			}

			// Outside top-right corner
			for (int c = fright + 1; c < fright + paddingRight; c++) {
				AddRuneAt (c, ftop, clearChar);
			}

			// Left, Fill, Right
			if (fbottom > ftop) {
				for (int r = ftop + 1; r < fbottom; r++) {
					// Outside left
					for (int c = region.X; c < fleft; c++) {
						AddRuneAt (c, r, clearChar);
					}

					// Frame left
					AddRuneAt (fleft, r, paddingLeft > 0 ? vLine : clearChar);

					// Fill
					if (fill) {
						for (int x = fleft + 1; x < fright; x++) {
							AddRuneAt (x, r, clearChar);
						}
					}

					// Frame right
					if (fright > fleft) {
						AddRuneAt (fright, r, paddingRight > 0 ? vLine : clearChar);
					}

					// Outside right
					for (int c = fright + 1; c < fright + paddingRight; c++) {
						AddRuneAt (c, r, clearChar);
					}
				}

				// Outside Bottom
				for (int c = region.X; c < fleft; c++) {
					AddRuneAt (c, fbottom, clearChar);
				}

				// Frame bottom-left
				AddRuneAt (fleft, fbottom, paddingLeft > 0 ? lLCorner : clearChar);

				if (fright > fleft) {
					// Frame bottom
					for (int c = fleft + 1; c < fright; c++) {
						AddRuneAt (c, fbottom, paddingBottom > 0 ? hLine : clearChar);
					}

					// Frame bottom-right
					AddRuneAt (fright, fbottom, paddingRight > 0 ? (paddingBottom > 0 ? lRCorner : hLine) : clearChar);
				}

				// Outside right
				for (int c = fright + 1; c < fright + paddingRight; c++) {
					AddRuneAt (c, fbottom, clearChar);
				}
			}

			// Out bottom - ensure top is always drawn if we overlap
			if (paddingBottom > 0) {
				for (int r = fbottom + 1; r < fbottom + paddingBottom; r++) {
					for (int c = region.X; c <= fright + paddingRight; c++) {
						AddRuneAt (c, r, clearChar);
					}
				}
			}
		}

		/// <summary>
		/// Draws a frame on the specified region with the specified padding around the frame.
		/// </summary>
		/// <param name="region">Region where the frame will be drawn..</param>
		/// <param name="padding">Padding to add on the sides.</param>
		/// <param name="fill">If set to <c>true</c> it will clear the contents with the current color, otherwise the contents will be left untouched.</param>
		/// <remarks>This is a legacy/depcrecated API. Use <see cref="DrawWindowFrame(Rect, int, int, int, int, bool, bool)"/>.</remarks>
		/// <remarks>A padding value of 0 means there is actually a 1 cell border.</remarks>
		public virtual void DrawFrame (Rect region, int padding, bool fill)
		{
			// DrawFrame assumes the frame is always at least one row/col thick
			// DrawWindowFrame assumes a padding of 0 means NO padding
			padding++;
			DrawWindowFrame (new Rect (region.X - 1, region.Y - 1, region.Width, region.Height), padding, padding, padding, padding, fill: fill);
		}


		/// <summary>
		/// Suspend the application, typically needs to save the state, suspend the app and upon return, reset the console driver.
		/// </summary>
		public abstract void Suspend ();

		Rect clip;

		/// <summary>
		/// Controls the current clipping region that AddRune/AddStr is subject to.
		/// </summary>
		/// <value>The clip.</value>
		public Rect Clip {
			get => clip;
			set => this.clip = value;
		}

		/// <summary>
		/// Start of mouse moves.
		/// </summary>
		public abstract void StartReportingMouseMoves ();

		/// <summary>
		/// Stop reporting mouses moves.
		/// </summary>
		public abstract void StopReportingMouseMoves ();

		/// <summary>
		/// Disables the cooked event processing from the mouse driver.  At startup, it is assumed mouse events are cooked.
		/// </summary>
		public abstract void UncookMouse ();

		/// <summary>
		/// Enables the cooked event processing from the mouse driver
		/// </summary>
		public abstract void CookMouse ();

		/// <summary>
		/// Horizontal line character.
		/// </summary>
		public Rune HLine;

		/// <summary>
		/// Vertical line character.
		/// </summary>
		public Rune VLine;

		/// <summary>
		/// Stipple pattern
		/// </summary>
		public Rune Stipple;

		/// <summary>
		/// Diamond character
		/// </summary>
		public Rune Diamond;

		/// <summary>
		/// Upper left corner
		/// </summary>
		public Rune ULCorner;

		/// <summary>
		/// Lower left corner
		/// </summary>
		public Rune LLCorner;

		/// <summary>
		/// Upper right corner
		/// </summary>
		public Rune URCorner;

		/// <summary>
		/// Lower right corner
		/// </summary>
		public Rune LRCorner;

		/// <summary>
		/// Left tee
		/// </summary>
		public Rune LeftTee;

		/// <summary>
		/// Right tee
		/// </summary>
		public Rune RightTee;

		/// <summary>
		/// Top tee
		/// </summary>
		public Rune TopTee;

		/// <summary>
		/// The bottom tee.
		/// </summary>
		public Rune BottomTee;

		/// <summary>
		/// Make the attribute for the foreground and background colors.
		/// </summary>
		/// <param name="fore">Foreground.</param>
		/// <param name="back">Background.</param>
		/// <returns></returns>
		public abstract Attribute MakeAttribute (Color fore, Color back);
	}
}
