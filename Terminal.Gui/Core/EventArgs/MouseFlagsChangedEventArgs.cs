using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	/// <summary>
	/// Args for events that describe a change in <see cref="MouseFlags"/>
	/// </summary>
	public class MouseFlagsChangedEventArgs : EventArgs{

		/// <summary>
		/// Creates a new instance of the <see cref="MouseFlagsChangedEventArgs"/> class.
		/// </summary>
		/// <param name="oldValue"></param>
		/// <param name="newValue"></param>
		public MouseFlagsChangedEventArgs (MouseFlags oldValue, MouseFlags newValue)
		{
			OldValue = oldValue;
			NewValue = newValue;
		}

		/// <summary>
		/// The old value before event
		/// </summary>
		public MouseFlags OldValue { get; }

		/// <summary>
		/// The new value
		/// </summary>
		public MouseFlags NewValue { get; }
	}
}
