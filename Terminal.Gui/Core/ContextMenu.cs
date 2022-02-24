using NStack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui {
	/// <summary>
	/// A context menu window derived from <see cref="MenuBar"/> containing menu items
	/// which can be opened in any position.
	/// </summary>
	public sealed class ContextMenu : IDisposable {
		private static MenuBar menuBar;
		private Point position;
		private Key key = Key.F10 | Key.ShiftMask;
		private MouseFlags mouseFlags = MouseFlags.Button3Clicked;
		private Toplevel container;

		/// <summary>
		/// Initialize a context menu with empty menu items.
		/// </summary>
		public ContextMenu () : this (0, 0, new MenuBarItem ()) { }

		/// <summary>
		/// Initialize a context menu with menu items.
		/// </summary>
		/// <param name="x">The left position.</param>
		/// <param name="y">The top position.</param>
		/// <param name="menuItems">The menu items.</param>
		public ContextMenu (int x, int y, MenuBarItem menuItems)
		{
			if (IsShow) {
				Hide ();
			}
			MenuItens = menuItems;
			Position = new Point (x, y);
		}

		private void MenuBar_MenuClosing ()
		{
			Dispose ();
		}

		/// <inheritdoc/>
		public void Dispose ()
		{
			menuBar.Dispose ();
			IsShow = false;
			if (container != null) {
				container.Closing -= Container_Closing;
			}
		}

		/// <summary>
		/// Open the <see cref="MenuItens"/> menu items.
		/// </summary>
		public void Show ()
		{
			if (menuBar != null) {
				Hide ();
			}
			container = Application.Current;
			container.Closing += Container_Closing;
			var frame = container.Frame;
			var rect = Menu.MakeFrame (position.X, position.Y, MenuItens.Children);
			if (rect.Right > frame.Right) {
				position.X = frame.Right - rect.Width;
			}
			if (rect.Bottom > frame.Bottom) {
				position.Y = frame.Bottom - rect.Height - 1;
			}

			menuBar = new MenuBar (new [] { MenuItens }) {
				X = position.X,
				Y = position.Y,
				Width = 0,
				Height = 0
			};

			menuBar.isContextMenuLoading = true;
			menuBar.MenuClosing += MenuBar_MenuClosing;
			IsShow = true;
			menuBar.OpenMenu ();
		}

		private void Container_Closing (ToplevelClosingEventArgs obj)
		{
			Hide ();
		}

		/// <summary>
		/// Close the <see cref="MenuItens"/> menu items.
		/// </summary>
		public void Hide ()
		{
			menuBar.CloseAllMenus ();
			Dispose ();
		}

		/// <summary>
		/// Event invoked when the <see cref="ContextMenu.Key"/> is changed.
		/// </summary>
		public event Action<Key> KeyChanged;

		/// <summary>
		/// Event invoked when the <see cref="ContextMenu.MouseFlags"/> is changed.
		/// </summary>
		public event Action<MouseFlags> MouseFlagsChanged;

		/// <summary>
		/// Gets or set the menu position.
		/// </summary>
		public Point Position {
			get => position;
			set => position = value;
		}

		/// <summary>
		/// Gets or sets the menu items for this context menu.
		/// </summary>
		public MenuBarItem MenuItens { get; set; }

		/// <summary>
		/// The <see cref="Gui.Key"/> used to activate the context menu by keyboard.
		/// </summary>
		public Key Key {
			get => key;
			set {
				var oldKey = key;
				key = value;
				KeyChanged?.Invoke (oldKey);
			}
		}

		/// <summary>
		/// The <see cref="Gui.MouseFlags"/> used to activate the context menu by mouse.
		/// </summary>
		public MouseFlags MouseFlags {
			get => mouseFlags;
			set {
				var oldFlags = mouseFlags;
				mouseFlags = value;
				MouseFlagsChanged?.Invoke (oldFlags);
			}
		}

		/// <summary>
		/// Gets information whether menu is showing or not.
		/// </summary>
		public static bool IsShow { get; private set; }
	}
}
