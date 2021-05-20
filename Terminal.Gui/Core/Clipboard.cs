using NStack;
using System;

namespace Terminal.Gui {
	/// <summary>
	/// Provides cut, copy, and paste support for the clipboard with OS interaction.
	/// </summary>
	public static class Clipboard {
		static ustring contents;

		/// <summary>
		/// Get or sets the operation system clipboard, otherwise the contents field.
		/// </summary>
		public static ustring Contents {
			get {
				try {
					return Application.Driver.Clipboard.GetClipboardData ();
				} catch (Exception) {
					return contents;
				}
			}
			set {
				try {
					Application.Driver.Clipboard.SetClipboardData (value.ToString ());
					contents = value;
				} catch (Exception) {
					contents = value;
				}
			}
		}
	}
}
