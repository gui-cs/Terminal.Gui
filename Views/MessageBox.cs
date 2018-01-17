using System;
namespace Terminal {

	/// <summary>
	/// Message box displays a modal message to the user, with a title, a message and a series of options that the user can choose from.
	/// </summary>
	public class MessageBox {
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
