using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	/// <summary>
	/// Args for events that relate to specific <see cref="View"/>
	/// </summary>
	public class ViewEventArgs :EventArgs{

		/// <summary>
		/// Creates a new instance of the <see cref="ViewEventArgs"/> class.
		/// </summary>
		/// <param name="view"></param>
		public ViewEventArgs (View view)
		{
			View = view;
		}

		/// <summary>
		/// The view that the event is about.
		/// </summary>
		/// <remarks>
		/// Can be different from the sender of the <see cref="EventHandler"/>
		/// for example if event describes the adding a child then sender may
		/// be the parent while <see cref="View"/> is the child
		/// being added.
		/// </remarks>
		public View View { get; }
	}
}
