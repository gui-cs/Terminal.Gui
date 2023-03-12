using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {

	/// <summary>
	/// Event args for draw events
	/// </summary>
	public class DrawEventArgs : EventArgs{

		/// <summary>
		/// Creates a new instance of the <see cref="DrawEventArgs"/> class.
		/// </summary>
		/// <param name="rect">Gets the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.</param>
		public DrawEventArgs (Rect rect)
		{
			Rect = rect;
		}

		/// <summary>
		/// Gets the view-relative rectangle describing the currently visible viewport into the <see cref="View"/>.
		/// </summary>
		public Rect Rect { get; }
	}
}
