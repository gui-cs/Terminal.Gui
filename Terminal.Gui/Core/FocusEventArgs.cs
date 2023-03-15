using System;

namespace Terminal.Gui {

	/// <summary>
	/// Defines the event arguments for <see cref="View.SetFocus(View)"/>
	/// </summary>
	public class FocusEventArgs : EventArgs {
		/// <summary>
		/// Constructs.
		/// </summary>
		/// <param name="view">The view that gets or loses focus.</param>
		public FocusEventArgs (View view) { View = view; }
		/// <summary>
		/// Indicates if the current focus event has already been processed and the driver should stop notifying any other event subscriber.
		/// Its important to set this value to true specially when updating any View's layout from inside the subscriber method.
		/// </summary>
		public bool Handled { get; set; }
		/// <summary>
		/// Indicates the current view that gets or loses focus.
		/// </summary>
		public View View { get; set; }
	}

}
