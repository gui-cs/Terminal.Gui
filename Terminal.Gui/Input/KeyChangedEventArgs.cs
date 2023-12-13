using System;

namespace Terminal.Gui {

	/// <summary>
	/// Event args for when a <see cref="ConsoleDriverKey"/> is changed from
	/// one value to a new value (e.g. in <see cref="View.HotKeyChanged"/>)
	/// </summary>
	public class KeyChangedEventArgs : EventArgs {

		/// <summary>
		/// Gets the old <see cref="ConsoleDriverKey"/> that was set before the event.
		/// Use <see cref="ConsoleDriverKey.Null"/> to check for empty.
		/// </summary>
		public ConsoleDriverKey OldKey { get; }

		/// <summary>
		/// Gets the new <see cref="ConsoleDriverKey"/> that is being used.
		/// Use <see cref="ConsoleDriverKey.Null"/> to check for empty.
		/// </summary>
		public ConsoleDriverKey NewKey { get; }

		/// <summary>
		/// Creates a new instance of the <see cref="KeyChangedEventArgs"/> class
		/// </summary>
		/// <param name="oldKey"></param>
		/// <param name="newKey"></param>
		public KeyChangedEventArgs (ConsoleDriverKey oldKey, ConsoleDriverKey newKey)
		{
			this.OldKey = oldKey;
			this.NewKey = newKey;
		}
	}
}
