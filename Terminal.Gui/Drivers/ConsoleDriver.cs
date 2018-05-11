//
// Driver.cs: Definition for the Console Driver API
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

		/// <summary>
		/// Initializes a new instance of the <see cref="T:Terminal.Gui.Attribute"/> struct.
		/// </summary>
		/// <param name="value">Value.</param>
		public Attribute (int value)
		{
			this.value = value;
		}

		public static implicit operator int (Attribute c) => c.value;
		public static implicit operator Attribute (int v) => new Attribute (v);
	}

	/// <summary>
	/// Color scheme definitions, they cover some common scenarios and are used
	/// typically in toplevel containers to set the scheme that is used by all the
	/// views contained inside.
	/// </summary>
	public class ColorScheme {
		/// <summary>
		/// The default color for text, when the view is not focused.
		/// </summary>
		public Attribute Normal;
		/// <summary>
		/// The color for text when the view has the focus.
		/// </summary>
		public Attribute Focus;

		/// <summary>
		/// The color for the hotkey when a view is not focused
		/// </summary>
		public Attribute HotNormal;

		/// <summary>
		/// The color for the hotkey when the view is focused.
		/// </summary>
		public Attribute HotFocus;
	}

	/// <summary>
	/// The default ColorSchemes for the application.
	/// </summary>
	public static class Colors {
		/// <summary>
		/// The base color scheme, for the default toplevel views.
		/// </summary>
		public static ColorScheme Base;
		/// <summary>
		/// The dialog color scheme, for standard popup dialog boxes
		/// </summary>
		public static ColorScheme Dialog;

		/// <summary>
		/// The menu bar color
		/// </summary>
		public static ColorScheme Menu;

		/// <summary>
		/// The color scheme for showing errors.
		/// </summary>
		public static ColorScheme Error;

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
		public abstract void PrepareToRun (MainLoop mainLoop, Action<KeyEvent> keyHandler, Action<MouseEvent> mouseHandler);

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

		// Set Colors from limit sets of colors
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
		/// Draws a frame on the specified region with the specified padding around the frame.
		/// </summary>
		/// <param name="region">Region where the frame will be drawn..</param>
		/// <param name="padding">Padding to add on the sides.</param>
		/// <param name="fill">If set to <c>true</c> it will clear the contents with the current color, otherwise the contents will be left untouched.</param>
		public virtual void DrawFrame (Rect region, int padding, bool fill)
		{
			int width = region.Width;
			int height = region.Height;
			int b;
			int fwidth = width - padding * 2;
			int fheight = height - 1 - padding;

			Move (region.X, region.Y);
			if (padding > 0) {
				for (int l = 0; l < padding; l++)
					for (b = 0; b < width; b++)
						AddRune (' ');
			}
			Move (region.X, region.Y + padding);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
			AddRune (ULCorner);
			for (b = 0; b < fwidth - 2; b++)
				AddRune (HLine);
			AddRune (URCorner);
			for (int c = 0; c < padding; c++)
				AddRune (' ');

			for (b = 1 + padding; b < fheight; b++) {
				Move (region.X, region.Y + b);
				for (int c = 0; c < padding; c++)
					AddRune (' ');
				AddRune (VLine);
				if (fill) {
					for (int x = 1; x < fwidth - 1; x++)
						AddRune (' ');
				} else
					Move (region.X + fwidth - 1, region.Y + b);
				AddRune (VLine);
				for (int c = 0; c < padding; c++)
					AddRune (' ');
			}
			Move (region.X, region.Y + fheight);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
			AddRune (LLCorner);
			for (b = 0; b < fwidth - 2; b++)
				AddRune (HLine);
			AddRune (LRCorner);
			for (int c = 0; c < padding; c++)
				AddRune (' ');
			if (padding > 0) {
				Move (region.X, region.Y + height - padding);
				for (int l = 0; l < padding; l++)
					for (b = 0; b < width; b++)
						AddRune (' ');
			}			
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

		public abstract void StartReportingMouseMoves ();
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
	}
}
