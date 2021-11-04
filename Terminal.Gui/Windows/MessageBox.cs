using NStack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Terminal.Gui {
	/// <summary>
	/// MessageBox displays a modal message to the user, with a title, a message and a series of options that the user can choose from.
	/// </summary>
	/// <para>
	///   The difference between the <see cref="Query(ustring, ustring, ustring[])"/> and <see cref="ErrorQuery(ustring, ustring, ustring[])"/> 
	///   method is the default set of colors used for the message box.
	/// </para>
	/// <para>
	/// The following example pops up a <see cref="MessageBox"/> with the specified title and text, plus two <see cref="Button"/>s.
	/// The value -1 is returned when the user cancels the <see cref="MessageBox"/> by pressing the ESC key.
	/// </para>
	/// <example>
	/// <code lang="c#">
	/// var n = MessageBox.Query ("Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
	/// if (n == 0)
	///    quit = true;
	/// else
	///    quit = false;
	/// </code>
	/// </example>
	public static class MessageBox {
		/// <summary>
		/// Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="Query(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int Query (int width, int height, ustring title, ustring message, params ustring [] buttons)
		{
			return QueryFull (false, width, height, title, message, 0, null, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the message and buttons.
		/// </remarks>
		public static int Query (ustring title, ustring message, params ustring [] buttons)
		{
			return QueryFull (false, 0, 0, title, message, 0, null, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="ErrorQuery(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int ErrorQuery (int width, int height, ustring title, ustring message, params ustring [] buttons)
		{
			return QueryFull (true, width, height, title, message, 0, null, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the title, message. and buttons.
		/// </remarks>
		public static int ErrorQuery (ustring title, ustring message, params ustring [] buttons)
		{
			return QueryFull (true, 0, 0, title, message, 0, null, buttons);
		}

		/// <summary>
		/// Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="Query(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int Query (int width, int height, ustring title, ustring message, int defaultButton = 0, params ustring [] buttons)
		{
			return QueryFull (false, width, height, title, message, defaultButton, null, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the message and buttons.
		/// </remarks>
		public static int Query (ustring title, ustring message, int defaultButton = 0, params ustring [] buttons)
		{
			return QueryFull (false, 0, 0, title, message, defaultButton, null, buttons);
		}

		/// <summary>
		/// Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="border">The border settings.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="Query(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int Query (int width, int height, ustring title, ustring message, int defaultButton = 0, Border border = null, params ustring [] buttons)
		{
			return QueryFull (false, width, height, title, message, defaultButton, border, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="border">The border settings.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the message and buttons.
		/// </remarks>
		public static int Query (ustring title, ustring message, int defaultButton = 0, Border border = null, params ustring [] buttons)
		{
			return QueryFull (false, 0, 0, title, message, defaultButton, border, buttons);
		}


		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="ErrorQuery(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int ErrorQuery (int width, int height, ustring title, ustring message, int defaultButton = 0, params ustring [] buttons)
		{
			return QueryFull (true, width, height, title, message, defaultButton, null, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the title, message. and buttons.
		/// </remarks>
		public static int ErrorQuery (ustring title, ustring message, int defaultButton = 0, params ustring [] buttons)
		{
			return QueryFull (true, 0, 0, title, message, defaultButton, null, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="border">The border settings.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// Use <see cref="ErrorQuery(ustring, ustring, ustring[])"/> instead; it automatically sizes the MessageBox based on the contents.
		/// </remarks>
		public static int ErrorQuery (int width, int height, ustring title, ustring message, int defaultButton = 0, Border border = null, params ustring [] buttons)
		{
			return QueryFull (true, width, height, title, message, defaultButton, border, buttons);
		}

		/// <summary>
		/// Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines.</param>
		/// <param name="defaultButton">Index of the default button.</param>
		/// <param name="border">The border settings.</param>
		/// <param name="buttons">Array of buttons to add.</param>
		/// <remarks>
		/// The message box will be vertically and horizontally centered in the container and the size will be automatically determined
		/// from the size of the title, message. and buttons.
		/// </remarks>
		public static int ErrorQuery (ustring title, ustring message, int defaultButton = 0, Border border = null, params ustring [] buttons)
		{
			return QueryFull (true, 0, 0, title, message, defaultButton, border, buttons);
		}

		static int QueryFull (bool useErrorColors, int width, int height, ustring title, ustring message,
			int defaultButton = 0, Border border = null, params ustring [] buttons)
		{
			const int defaultWidth = 50;
			int textWidth = TextFormatter.MaxWidth (message, width == 0 ? defaultWidth : width);
			int textHeight = TextFormatter.MaxLines (message, textWidth); // message.Count (ustring.Make ('\n')) + 1;
			int msgboxHeight = Math.Max (1, textHeight) + 3; // textHeight + (top + top padding + buttons + bottom)

			// Create button array for Dialog
			int count = 0;
			List<Button> buttonList = new List<Button> ();
			if (buttons != null && defaultButton > buttons.Length - 1) {
				defaultButton = buttons.Length - 1;
			}
			foreach (var s in buttons) {
				var b = new Button (s);
				if (count == defaultButton) {
					b.IsDefault = true;
				}
				buttonList.Add (b);
				count++;
			}

			// Create Dialog (retain backwards compat by supporting specifying height/width)
			Dialog d;
			if (width == 0 & height == 0) {
				d = new Dialog (title, buttonList.ToArray ());
				d.Height = msgboxHeight;
			} else {
				d = new Dialog (title, Math.Max (width, textWidth) + 4, height, buttonList.ToArray ());
			}

			if (border != null) {
				d.Border = border;
			}

			if (useErrorColors) {
				d.ColorScheme = Colors.Error;
			}

			if (message != null) {
				var l = new Label (textWidth > width ? 0 : (width - 4 - textWidth) / 2, 1, message);
				l.LayoutStyle = LayoutStyle.Computed;
				l.TextAlignment = TextAlignment.Centered;
				l.X = Pos.Center ();
				l.Y = Pos.Center ();
				l.Width = Dim.Fill (2);
				l.Height = Dim.Fill (1);
				d.Add (l);
			}

			// Dynamically size Width
			int msgboxWidth = Math.Max (defaultWidth, Math.Max (title.RuneCount + 8, Math.Max (textWidth + 4, d.GetButtonsWidth ()) + 8)); // textWidth + (left + padding + padding + right)
			d.Width = msgboxWidth;

			// Setup actions
			int clicked = -1;
			for (int n = 0; n < buttonList.Count; n++) {
				int buttonId = n;
				var b = buttonList [n];
				b.Clicked += () => {
					clicked = buttonId;
					Application.RequestStop ();
				};
				if (b.IsDefault) {
					b.SetFocus ();
				}
			}

			// Run the modal; do not shutdown the mainloop driver when done
			Application.Run (d);
			return clicked;
		}
	}
}
