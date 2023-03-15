using System;

namespace Terminal.Gui {
	/// <summary>
	/// <see cref="EventArgs"/> implementation for the <see cref="Toplevel.Closing"/> event.
	/// </summary>
	public class ToplevelClosingEventArgs : EventArgs {
		/// <summary>
		/// The toplevel requesting stop.
		/// </summary>
		public View RequestingTop { get; }
		/// <summary>
		/// Provides an event cancellation option.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Initializes the event arguments with the requesting toplevel.
		/// </summary>
		/// <param name="requestingTop">The <see cref="RequestingTop"/>.</param>
		public ToplevelClosingEventArgs (Toplevel requestingTop)
		{
			RequestingTop = requestingTop;
		}
	}
}
