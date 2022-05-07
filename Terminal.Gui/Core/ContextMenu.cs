using System;

namespace Terminal.Gui {
	/// <summary>
	/// A context menu window derived from <see cref="MenuBar"/> containing menu items
	/// which can be opened in any position.
	/// </summary>
	public sealed class ContextMenu : IDisposable {
		private static MenuBar menuBar;
		private Key key = Key.F10 | Key.ShiftMask;
		private MouseFlags mouseFlags = MouseFlags.Button3Clicked;
		private Toplevel container;

		/// <summary>
		/// Initialize a context menu with empty menu items.
		/// </summary>
		public ContextMenu () : this (0, 0, new MenuBarItem ()) { }

		/// <summary>
		/// Initialize a context menu with menu items from a host <see cref="View"/>.
		/// </summary>
		/// <param name="host">The host view.</param>
		/// <param name="menuItems">The menu items.</param>
		public ContextMenu (View host, MenuBarItem menuItems) :
			this (host.Frame.X, host.Frame.Y, menuItems)
		{
			Host = host;
		}

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
			MenuItems = menuItems;
			Position = new Point (x, y);
		}

		private void MenuBar_MenuAllClosed ()
		{
			Dispose ();
		}

		/// <inheritdoc/>
		public void Dispose ()
		{
			if (IsShow) {
				menuBar.MenuAllClosed -= MenuBar_MenuAllClosed;
				menuBar.Dispose ();
				menuBar = null;
				IsShow = false;
			}
			if (container != null) {
				container.Closing -= Container_Closing;
				container.Resized -= Container_Resized;
			}
		}

		/// <summary>
		/// Open the <see cref="MenuItems"/> menu items.
		/// </summary>
		public void Show ()
		{
			if (menuBar != null) {
				Hide ();
			}
			container = Application.Current;
			container.Closing += Container_Closing;
			container.Resized += Container_Resized;
			var frame = container.Frame;
			var position = Position;
			if (Host != null) {
				Host.ViewToScreen (container.Frame.X, container.Frame.Y, out int x, out int y);
				var pos = new Point (x, y);
				pos.Y += Host.Frame.Height - 1;
				if (position != pos) {
					Position = position = pos;
				}
			}
			var rect = Menu.MakeFrame (position.X, position.Y, MenuItems.Children);
			if (rect.Right >= frame.Right) {
				if (frame.Right - rect.Width >= 0 || !ForceMinimumPosToZero) {
					position.X = frame.Right - rect.Width;
				} else if (ForceMinimumPosToZero) {
					position.X = 0;
				}
			} else if (ForceMinimumPosToZero && position.X < 0) {
				position.X = 0;
			}
			if (rect.Bottom >= frame.Bottom) {
				if (frame.Bottom - rect.Height - 1 >= 0 || !ForceMinimumPosToZero) {
					if (Host == null) {
						position.Y = frame.Bottom - rect.Height - 1;
					} else {
						Host.ViewToScreen (container.Frame.X, container.Frame.Y, out int x, out int y);
						var pos = new Point (x, y);
						position.Y = pos.Y - rect.Height - 1;
					}
				} else if (ForceMinimumPosToZero) {
					position.Y = 0;
				}
			} else if (ForceMinimumPosToZero && position.Y < 0) {
				position.Y = 0;
			}

			menuBar = new MenuBar (new [] { MenuItems }) {
				X = position.X,
				Y = position.Y,
				Width = 0,
				Height = 0,
				UseSubMenusSingleFrame = UseSubMenusSingleFrame
			};

			menuBar.isContextMenuLoading = true;
			menuBar.MenuAllClosed += MenuBar_MenuAllClosed;
			IsShow = true;
			menuBar.OpenMenu ();
		}

		private void Container_Resized (Size obj)
		{
			if (IsShow) {
				Show ();
			}
		}

		private void Container_Closing (ToplevelClosingEventArgs obj)
		{
			Hide ();
		}

		/// <summary>
		/// Close the <see cref="MenuItems"/> menu items.
		/// </summary>
		public void Hide ()
		{
			menuBar.CleanUp ();
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
		public Point Position { get; set; }

		/// <summary>
		/// Gets or sets the menu items for this context menu.
		/// </summary>
		public MenuBarItem MenuItems { get; set; }

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

		/// <summary>
		/// The host <see cref="View "/> which position will be used,
		/// otherwise if it's null the container will be used.
		/// </summary>
		public View Host { get; set; }

		/// <summary>
		/// Gets or sets whether forces the minimum position to zero
		/// if the left or right position are negative.
		/// </summary>
		public bool ForceMinimumPosToZero { get; set; } = true;

		/// <summary>
		/// Gets the <see cref="Gui.MenuBar"/> that is hosting this context menu.
		/// </summary>
		public MenuBar MenuBar { get => menuBar; }

		/// <summary>
		/// Gets or sets if the sub-menus must be displayed in a single or multiple frames.
		/// </summary>
		public bool UseSubMenusSingleFrame { get; set; }
	}
}
