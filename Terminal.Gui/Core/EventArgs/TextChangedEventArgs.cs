using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {

	/// <summary>
	/// Event args for events where text is changed
	/// </summary>
	public class TextChangedEventArgs : EventArgs {

		/// <summary>
		/// Creates a new instance of the <see cref="TextChangedEventArgs"/> class
		/// </summary>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		public TextChangedEventArgs (ustring oldValue, ustring newValue)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}

		/// <summary>
		/// The old value before the text changed
		/// </summary>
		public ustring OldValue { get; }

		/// <summary>
		/// The new value
		/// </summary>
		public ustring NewValue { get; }
	}
}
