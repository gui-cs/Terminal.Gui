//
// Label.cs: Label control
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NStack;

namespace Terminal.Gui {
	/// <summary>
	/// The Label <see cref="View"/> displays a string at a given position and supports multiple lines separted by newline characters. Multi-line Labels support word wrap.
	/// </summary>
	/// <remarks>
	/// The <see cref="Label"/> view is functionality identical to <see cref="View"/> and is included for API backwards compatibilty.
	/// </remarks>
	public class Label : View {
		/// <inheritdoc/>
		public Label ()
		{
		}

		/// <inheritdoc/>
		public Label (Rect frame) : base (frame)
		{
		}

		/// <inheritdoc/>
		public Label (ustring text) : base (text)
		{
		}

		/// <inheritdoc/>
		public Label (Rect rect, ustring text) : base (rect, text)
		{
		}

		/// <inheritdoc/>
		public Label (int x, int y, ustring text) : base (x, y, text)
		{
		}
	}
}
