using System;
namespace Terminal.Gui {

	/// <summary>
	/// Message box displays a modal message to the user, with a title, a message and a series of options that the user can choose from.
	/// </summary>
	/// <para>
	///   The difference between the Query and ErrorQuery method is the default set of colors used for the message box.
	/// </para>
	/// <para>
	/// The following example pops up a Message Box with 50 columns, and 7 lines, with the specified title and text, plus two buttons.
	/// The value -1 is returned when the user cancels the dialog by pressing the ESC key.
	/// </para>
	/// <example>
	/// <code lang="c#">
	/// var n = MessageBox.Query (50, 7, "Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
	/// if (n == 0)
	///    quit = true;
	/// else
	///    quit = false;
	/// 
	/// </code>
	/// </example>
	public static class MessageBox {
		/// <summary>
		/// Presents a message with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines..</param>
		/// <param name="buttons">Array of buttons to add.</param>
		public static int Query (int width, int height, string title, string message, params string [] buttons)
		{
			return QueryFull (false, width, height, title, message, buttons);
		}

		/// <summary>
		/// Presents an error message box with the specified title and message and a list of buttons to show to the user.
		/// </summary>
		/// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
		/// <param name="width">Width for the window.</param>
		/// <param name="height">Height for the window.</param>
		/// <param name="title">Title for the query.</param>
		/// <param name="message">Message to display, might contain multiple lines..</param>
		/// <param name="buttons">Array of buttons to add.</param>
		public static int ErrorQuery (int width, int height, string title, string message, params string [] buttons)
		{
			return QueryFull (true, width, height, title, message, buttons);
		}

		static int QueryFull (bool useErrorColors, int width, int height, string title, string message, params string [] buttons)
		{
			int lines = Label.MeasureLines (message, width);
			int clicked = -1, count = 0;

			var d = new Dialog (title, width, height);
			if (useErrorColors)
				d.ColorScheme = Colors.Error;
			
			foreach (var s in buttons) {
				int n = count++;
				var b = new Button (s);
				b.Clicked += delegate {
					clicked = n;
					d.Running = false;
				};
				d.AddButton (b);
			}
			if (message != null) {
				var l = new Label ((width - 4 - message.Length) / 2, 0, message);
				d.Add (l);
			}

			Application.Run (d);
			return clicked;
		}
	}
}
