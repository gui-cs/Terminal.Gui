using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {

	/// <summary>
	/// Defines arguments for the <see cref="MenuBar.MenuOpened"/> event
	/// </summary>
	public class MenuOpenedEventArgs : EventArgs{

		/// <summary>
		/// Creates a new instance of the <see cref="MenuOpenedEventArgs"/> class
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="menuItem"></param>
		public MenuOpenedEventArgs (MenuBarItem parent, MenuItem menuItem)
		{
			Parent = parent;
			MenuItem = menuItem;
		}

		/// <summary>
		/// The parent of <see cref="MenuItem"/>.  Will be null if menu opening
		/// is the root (see <see cref="MenuBarItem.IsTopLevel"/>).
		/// </summary>
		public MenuBarItem Parent { get; }

		/// <summary>
		/// Gets the <see cref="MenuItem"/> being opened.
		/// </summary>
		public MenuItem MenuItem { get; }
	}
}
