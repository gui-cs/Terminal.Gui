using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.Core {
	/// <summary>
	/// The <see cref="Canvas"/> is a <see cref="Responder"/> that can be used to draw on the screen. 
	/// It is the base class of <see cref="View"/> and <see cref="Frame"/>.
	/// </summary>
	public class Canvas : Responder {
		/// <summary>
		/// Initializes a new instance of the <see cref="Canvas"/> class.
		/// </summary>
		public Canvas () : this (Rect.Empty) {}

		/// <summary>
		/// Initializes a new instance of the <see cref="Canvas"/> class.
		/// </summary>
		/// <param name="frame">The <see cref="Rect"/> that defines the position and size of the <see cref="Canvas"/> on the screen.</param>
		/// relative coordinates.</param>
		public Canvas (Rect frame) 
		{
		}
	}
}
