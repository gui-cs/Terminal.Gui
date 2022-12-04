using NStack;
using System;
using System.Collections.Generic;

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
			int defaultWidth = 50;
			if (defaultWidth > Application.Driver.Cols / 2) {
				defaultWidth = (int)(Application.Driver.Cols * 0.60f);
			}
			int maxWidthLine = TextFormatter.MaxWidthLine (message);
			if (maxWidthLine > Application.Driver.Cols) {
				maxWidthLine = Application.Driver.Cols;
			}
			if (width == 0) {
				maxWidthLine = Math.Max (maxWidthLine, defaultWidth);
			} else {
				maxWidthLine = width;
			}
			int textWidth = Math.Min (TextFormatter.MaxWidth (message, maxWidthLine), Application.Driver.Cols);
			int textHeight = TextFormatter.MaxLines (message, textWidth); // message.Count (ustring.Make ('\n')) + 1;
			int msgboxHeight = Math.Min (Math.Max (1, textHeight) + 4, Application.Driver.Rows); // textHeight + (top + top padding + buttons + bottom)

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
				d = new Dialog (title, buttonList.ToArray ()) {
					Height = msgboxHeight
				};
			} else {
				d = new Dialog (title, width, Math.Max (height, 4), buttonList.ToArray ());
			}

			if (border != null) {
				d.Border = border;
			}

			if (useErrorColors) {
				d.ColorScheme = Colors.Error;
			}

			if (message != null) {
				var l = new Label (message) {
					LayoutStyle = LayoutStyle.Computed,
					TextAlignment = TextAlignment.Centered,
					X = Pos.Center (),
					Y = Pos.Center (),
					Width = Dim.Fill (),
					Height = Dim.Fill (1),
					AutoSize = false
				};
				d.Add (l);
			}

			if (width == 0 & height == 0) {
				// Dynamically size Width
				d.Width = Math.Min (Math.Max (maxWidthLine, Math.Max (title.ConsoleWidth, Math.Max (textWidth + 2, d.GetButtonsWidth () + d.buttons.Count + 2))), Application.Driver.Cols); // textWidth + (left + padding + padding + right)
			}

			// Setup actions
			Clicked = -1;
			for (int n = 0; n < buttonList.Count; n++) {
				int buttonId = n;
				var b = buttonList [n];
				b.Clicked += () => {
					Clicked = buttonId;
					Application.RequestStop ();
				};
				if (b.IsDefault) {
					b.SetFocus ();
				}
			}

			// Run the modal; do not shutdown the mainloop driver when done
			Application.Run (d);
			return Clicked;
		}

		/// <summary>
		/// The index of the selected button, or -1 if the user pressed ESC to close the dialog.
		/// This is useful for web based console where by default there is no SynchronizationContext or TaskScheduler.
		/// </summary>
		public static int Clicked { get; private set; } = -1;
	}
}
