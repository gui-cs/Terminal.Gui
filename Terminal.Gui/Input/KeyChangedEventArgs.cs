using System;

namespace Terminal.Gui {

	/// <summary>
	/// Event args for when a <see cref="KeyCode"/> is changed from
	/// one value to a new value (e.g. in <see cref="View.HotKeyChanged"/>)
	/// </summary>
	public class KeyChangedEventArgs : EventArgs {

		/// <summary>
		/// Gets the old <see cref="KeyCode"/> that was set before the event.
		/// Use <see cref="KeyCode.Null"/> to check for empty.
		/// </summary>
		public KeyCode OldKey { get; }

		/// <summary>
		/// Gets the new <see cref="KeyCode"/> that is being used.
		/// Use <see cref="KeyCode.Null"/> to check for empty.
		/// </summary>
		public KeyCode NewKey { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="KeyChangedEventArgs"/> class
		/// </summary>
		/// <param name="oldKey"></param>
		/// <param name="newKey"></param>
		public KeyChangedEventArgs (KeyCode oldKey, KeyCode newKey)
		{
			this.OldKey = oldKey;
			this.NewKey = newKey;
		}
	}
}
