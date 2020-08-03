using NStack;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Terminal.Gui;

namespace UICatalog {
	[ScenarioMetadata (Name: "Dynamic MenuBar", Description: "Demonstrates how to add and remove a MenuBar, Menus and change titles dynamically.")]
	[ScenarioCategory ("Dynamic")]
	class DynamicMenuBar : Scenario {
		public override void Run ()
		{
			Top.Add (new DynamicMenuBarSample (Win.Title));
			base.Run ();
		}
	}

	class DynamicMenuItemList {
		public ustring Title { get; set; }
		public MenuItem MenuItem { get; set; }

		public DynamicMenuItemList () { }

		public DynamicMenuItemList (ustring title, MenuItem menuItem)
		{
			Title = title;
			MenuItem = menuItem;
		}

		public override string ToString () => $"{Title}, {MenuItem}";
	}

	class DynamicMenuItem {
		public ustring title = "_New";
		public ustring help = "";
		public ustring action = "";
		public bool isTopLevel;
		public bool hasSubMenu;
		public MenuItemCheckStyle checkStyle;

		public DynamicMenuItem () { }

		public DynamicMenuItem (ustring title)
		{
			this.title = title;
		}

		public DynamicMenuItem (ustring title, ustring help, ustring action, bool isTopLevel, bool hasSubMenu, MenuItemCheckStyle checkStyle = MenuItemCheckStyle.NoCheck)
		{
			this.title = title;
			this.help = help;
			this.action = action;
			this.isTopLevel = isTopLevel;
			this.hasSubMenu = hasSubMenu;
			this.checkStyle = checkStyle;
		}
	}

	class DynamicMenuBarSample : Window {
		MenuBar _menuBar;
		MenuItem _currentMenuBarItem;
		int _currentSelectedMenuBar;
		MenuItem _currentEditMenuBarItem;

		public DynamicMenuItemModel DataContext { get; set; }

		public DynamicMenuBarSample (ustring title) : base (title)
		{
			DataContext = new DynamicMenuItemModel ();

			var _frmMenu = new FrameView ("Menus:") {
				Y = 7,
				Width = Dim.Percent (50),
				Height = Dim.Fill ()
			};

			var _btnAddMenuBar = new Button ("Add a MenuBar") {
				Y = 1,
			};
			_frmMenu.Add (_btnAddMenuBar);

			var _btnMenuBarUp = new Button ("^") {
				X = Pos.Center ()
			};
			_frmMenu.Add (_btnMenuBarUp);

			var _btnMenuBarDown = new Button ("v") {
				X = Pos.Center (),
				Y = Pos.Bottom (_btnMenuBarUp)
			};
			_frmMenu.Add (_btnMenuBarDown);

			var _btnRemoveMenuBar = new Button ("Remove a MenuBar") {
				Y = 1
			};
			_btnRemoveMenuBar.X = Pos.AnchorEnd () - (Pos.Right (_btnRemoveMenuBar) - Pos.Left (_btnRemoveMenuBar));
			_frmMenu.Add (_btnRemoveMenuBar);

			var _btnPrevious = new Button ("<") {
				X = Pos.Left (_btnAddMenuBar),
				Y = Pos.Top (_btnAddMenuBar) + 2
			};
			_frmMenu.Add (_btnPrevious);

			var _btnAdd = new Button (" Add  ") {
				Y = Pos.Top (_btnPrevious) + 2,
			};
			_btnAdd.X = Pos.AnchorEnd () - (Pos.Right (_btnAdd) - Pos.Left (_btnAdd));
			_frmMenu.Add (_btnAdd);

			var _btnNext = new Button (">") {
				X = Pos.X (_btnAdd),
				Y = Pos.Top (_btnPrevious),
			};
			_frmMenu.Add (_btnNext);

			var _lblMenuBar = new Label () {
				ColorScheme = Colors.Dialog,
				TextAlignment = TextAlignment.Centered,
				X = Pos.Right (_btnPrevious) + 1,
				Y = Pos.Top (_btnPrevious),
				Width = Dim.Fill () - Dim.Width (_btnAdd) - 1,
				Height = 1
			};
			_frmMenu.Add (_lblMenuBar);
			_lblMenuBar.WantMousePositionReports = true;
			_lblMenuBar.CanFocus = true;

			var _lblParent = new Label () {
				TextAlignment = TextAlignment.Centered,
				X = Pos.Right (_btnPrevious) + 1,
				Y = Pos.Top (_btnPrevious) + 1,
				Width = Dim.Fill () - Dim.Width (_btnAdd) - 1
			};
			_frmMenu.Add (_lblParent);

			var _btnPreviowsParent = new Button ("..") {
				X = Pos.Left (_btnAddMenuBar),
				Y = Pos.Top (_btnPrevious) + 1
			};
			_frmMenu.Add (_btnPreviowsParent);

			var _lstMenus = new ListView (new List<DynamicMenuItemList> ()) {
				ColorScheme = Colors.Dialog,
				X = Pos.Right (_btnPrevious) + 1,
				Y = Pos.Top (_btnPrevious) + 2,
				Width = _lblMenuBar.Width,
				Height = Dim.Fill (),
			};
			_frmMenu.Add (_lstMenus);

			_lblMenuBar.TabIndex = _btnPrevious.TabIndex + 1;
			_lstMenus.TabIndex = _lblMenuBar.TabIndex + 1;
			_btnNext.TabIndex = _lstMenus.TabIndex + 1;
			_btnAdd.TabIndex = _btnNext.TabIndex + 1;

			var _btnRemove = new Button ("Remove") {
				X = Pos.Left (_btnAdd),
				Y = Pos.Top (_btnAdd) + 1
			};
			_frmMenu.Add (_btnRemove);

			var _btnUp = new Button ("^") {
				X = Pos.Right (_lstMenus) + 2,
				Y = Pos.Top (_btnRemove) + 2
			};
			_frmMenu.Add (_btnUp);

			var _btnDown = new Button ("v") {
				X = Pos.Right (_lstMenus) + 2,
				Y = Pos.Top (_btnUp) + 1
			};
			_frmMenu.Add (_btnDown);

			Add (_frmMenu);

			var _frmMenuDetails = new FrameView ("Menu Details:") {
				X = Pos.Right (_frmMenu),
				Y = Pos.Top (_frmMenu),
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			var _lblTitle = new Label ("Title:") {
				Y = 1
			};
			_frmMenuDetails.Add (_lblTitle);

			var _txtTitle = new TextField () {
				X = Pos.Right (_lblTitle) + 2,
				Y = Pos.Top (_lblTitle),
				Width = Dim.Fill ()
			};
			_frmMenuDetails.Add (_txtTitle);

			var _lblHelp = new Label ("Help:") {
				X = Pos.Left (_lblTitle),
				Y = Pos.Bottom (_lblTitle) + 1
			};
			_frmMenuDetails.Add (_lblHelp);

			var _txtHelp = new TextField () {
				X = Pos.Left (_txtTitle),
				Y = Pos.Top (_lblHelp),
				Width = Dim.Fill ()
			};
			_frmMenuDetails.Add (_txtHelp);

			var _lblAction = new Label ("Action:") {
				X = Pos.Left (_lblTitle),
				Y = Pos.Bottom (_lblHelp) + 1
			};
			_frmMenuDetails.Add (_lblAction);

			var _txtAction = new TextView () {
				ColorScheme = Colors.Dialog,
				X = Pos.Left (_txtTitle),
				Y = Pos.Top (_lblAction),
				Width = Dim.Fill (),
				Height = 5
			};
			_frmMenuDetails.Add (_txtAction);

			var _ckbIsTopLevel = new CheckBox ("IsTopLevel") {
				X = Pos.Left (_lblTitle),
				Y = Pos.Bottom (_lblAction) + 5
			};
			_frmMenuDetails.Add (_ckbIsTopLevel);

			var _ckbSubMenu = new CheckBox ("Has sub-menus") {
				X = Pos.Left (_lblTitle),
				Y = Pos.Bottom (_ckbIsTopLevel)
			};
			_frmMenuDetails.Add (_ckbSubMenu);
			_ckbIsTopLevel.Toggled = (e) => {
				if (_ckbIsTopLevel.Checked && _currentEditMenuBarItem.Parent != null) {
					MessageBox.ErrorQuery ("Invalid IsTopLevel", "Only menu bar can have top level menu item!", "Ok");
					_ckbIsTopLevel.Checked = false;
					return;
				}
				if (_ckbIsTopLevel.Checked) {
					_ckbSubMenu.Checked = false;
					_ckbSubMenu.SetNeedsDisplay ();
					_txtAction.ReadOnly = false;
				} else {
					_txtAction.ReadOnly = true;
				}
			};
			_ckbSubMenu.Toggled = (e) => {
				if (_ckbSubMenu.Checked) {
					_ckbIsTopLevel.Checked = false;
					_ckbIsTopLevel.SetNeedsDisplay ();
					_txtAction.ReadOnly = true;
				} else {
					_txtAction.ReadOnly = false;
				}
			};

			var _rChkLabels = new ustring [] { "NoCheck", "Checked", "Radio" };
			var _rbChkStyle = new RadioGroup (_rChkLabels) {
				X = Pos.Left (_lblTitle),
				Y = Pos.Bottom (_ckbSubMenu) + 1,
			};
			_frmMenuDetails.Add (_rbChkStyle);

			var _btnOk = new Button ("Ok") {
				X = Pos.Left (_lblTitle) + 20,
				Y = Pos.Bottom (_rbChkStyle) + 1,
				Clicked = () => {
					if (ustring.IsNullOrEmpty (_txtTitle.Text) && _currentEditMenuBarItem != null) {
						MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
					} else if (_currentEditMenuBarItem != null) {
						var menuItem = new DynamicMenuItem (_txtTitle.Text, _txtHelp.Text, _txtAction.Text, _ckbIsTopLevel != null ? _ckbIsTopLevel.Checked : false, _ckbSubMenu != null ? _ckbSubMenu.Checked : false, _rbChkStyle.SelectedItem == 0 ? MenuItemCheckStyle.NoCheck : _rbChkStyle.SelectedItem == 1 ? MenuItemCheckStyle.Checked : MenuItemCheckStyle.Radio);
						UpdateMenuItem (_currentEditMenuBarItem, menuItem, _lstMenus.SelectedItem);
					}
				}
			};
			_frmMenuDetails.Add (_btnOk);

			var _btnCancel = new Button ("Cancel") {
				X = Pos.Right (_btnOk) + 3,
				Y = Pos.Top (_btnOk),
				Clicked = () => {
					_txtTitle.Text = ustring.Empty;
				}
			};
			_frmMenuDetails.Add (_btnCancel);

			Add (_frmMenuDetails);

			_btnAdd.Clicked = () => {
				if (MenuBar == null) {
					MessageBox.ErrorQuery ("Menu Bar Error", "Must add a MenuBar first!", "Ok");
					_btnAddMenuBar.SetFocus ();
					return;
				}

				var item = EnterMenuItem (_currentMenuBarItem);
				if (ustring.IsNullOrEmpty (item.title)) {
					return;
				}

				if (!(_currentMenuBarItem is MenuBarItem)) {
					var parent = _currentMenuBarItem.Parent as MenuBarItem;
					var idx = parent.GetChildrenIndex (_currentMenuBarItem);
					_currentMenuBarItem = new MenuBarItem (_currentMenuBarItem.Title, new MenuItem [] { new MenuItem ("_New", "", CreateAction (_currentEditMenuBarItem, new DynamicMenuItem ())) }, _currentMenuBarItem.Parent);
					_currentMenuBarItem.CheckType = item.checkStyle;
					parent.Children [idx] = _currentMenuBarItem;
				} else {
					MenuItem newMenu = CreateNewMenu (item, _currentMenuBarItem);
					var menuBarItem = _currentMenuBarItem as MenuBarItem;
					if (menuBarItem == null) {
						menuBarItem = new MenuBarItem (_currentMenuBarItem.Title, new MenuItem [] { newMenu }, _currentMenuBarItem.Parent);
					} else if (menuBarItem.Children == null) {
						menuBarItem.Children = new MenuItem [] { newMenu };
					} else {
						var childrens = menuBarItem.Children;
						Array.Resize (ref childrens, childrens.Length + 1);
						childrens [childrens.Length - 1] = newMenu;
						menuBarItem.Children = childrens;
					}
					DataContext.Menus.Add (new DynamicMenuItemList (newMenu.Title, newMenu));
					_lstMenus.MoveDown ();
				}
			};

			_btnRemove.Clicked = () => {
				var menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [_lstMenus.SelectedItem].MenuItem : null;
				if (menuItem != null) {
					var childrens = ((MenuBarItem)_currentMenuBarItem).Children;
					childrens [_lstMenus.SelectedItem] = null;
					int i = 0;
					foreach (var c in childrens) {
						if (c != null) {
							childrens [i] = c;
							i++;
						}
					}
					Array.Resize (ref childrens, childrens.Length - 1);
					if (childrens.Length == 0) {
						if (_currentMenuBarItem.Parent == null) {
							((MenuBarItem)_currentMenuBarItem).Children = null;
							_currentMenuBarItem.Action = CreateAction (_currentEditMenuBarItem, new DynamicMenuItem (_currentMenuBarItem.Title));
						} else {
							_currentMenuBarItem = new MenuItem (_currentMenuBarItem.Title, _currentMenuBarItem.Help, CreateAction (_currentEditMenuBarItem, new DynamicMenuItem (_currentEditMenuBarItem.Title)), null, _currentMenuBarItem.Parent);
						}
					} else {
						((MenuBarItem)_currentMenuBarItem).Children = childrens;
					}
					DataContext.Menus.RemoveAt (_lstMenus.SelectedItem);
				}
			};

			_btnMenuBarUp.Clicked = () => {
				var i = _currentSelectedMenuBar;
				var menuItem = _menuBar != null && _menuBar.Menus.Length > 0 ? _menuBar.Menus [i] : null;
				if (menuItem != null) {
					var menus = _menuBar.Menus;
					if (i > 0) {
						menus [i] = menus [i - 1];
						menus [i - 1] = menuItem;
						_currentSelectedMenuBar = i - 1;
						_menuBar.SetNeedsDisplay ();
					}
				}
			};

			_btnMenuBarDown.Clicked = () => {
				var i = _currentSelectedMenuBar;
				var menuItem = _menuBar != null && _menuBar.Menus.Length > 0 ? _menuBar.Menus [i] : null;
				if (menuItem != null) {
					var menus = _menuBar.Menus;
					if (i < menus.Length - 1) {
						menus [i] = menus [i + 1];
						menus [i + 1] = menuItem;
						_currentSelectedMenuBar = i + 1;
						_menuBar.SetNeedsDisplay ();
					}
				}
			};

			_btnUp.Clicked = () => {
				var i = _lstMenus.SelectedItem;
				var menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [i].MenuItem : null;
				if (menuItem != null) {
					var childrens = ((MenuBarItem)_currentMenuBarItem).Children;
					if (i > 0) {
						childrens [i] = childrens [i - 1];
						childrens [i - 1] = menuItem;
						DataContext.Menus [i] = DataContext.Menus [i - 1];
						DataContext.Menus [i - 1] = new DynamicMenuItemList (menuItem.Title, menuItem);
						_lstMenus.SelectedItem = i - 1;
					}
				}
			};

			_btnDown.Clicked = () => {
				var i = _lstMenus.SelectedItem;
				var menuItem = DataContext.Menus.Count > 0 ? DataContext.Menus [i].MenuItem : null;
				if (menuItem != null) {
					var childrens = ((MenuBarItem)_currentMenuBarItem).Children;
					if (i < childrens.Length - 1) {
						childrens [i] = childrens [i + 1];
						childrens [i + 1] = menuItem;
						DataContext.Menus [i] = DataContext.Menus [i + 1];
						DataContext.Menus [i + 1] = new DynamicMenuItemList (menuItem.Title, menuItem);
						_lstMenus.SelectedItem = i + 1;
					}
				}
			};

			_btnAddMenuBar.Clicked = () => {
				var item = EnterMenuItem (null);
				if (ustring.IsNullOrEmpty (item.title)) {
					return;
				}

				if (MenuBar == null) {
					_menuBar = new MenuBar ();
					Add (_menuBar);
				}
				var newMenu = CreateNewMenu (item) as MenuBarItem;

				var menus = _menuBar.Menus;
				Array.Resize (ref menus, menus.Length + 1);
				menus [^1] = newMenu;
				_menuBar.Menus = menus;
				_currentMenuBarItem = newMenu;
				_currentMenuBarItem.CheckType = item.checkStyle;
				_currentSelectedMenuBar = menus.Length - 1;
				_menuBar.Menus [_currentSelectedMenuBar] = newMenu;
				_lblMenuBar.Text = newMenu.Title;
				SetListViewSource (_currentMenuBarItem, true);
				EditMenuBarItem (_menuBar.Menus [_currentSelectedMenuBar]);
				_menuBar.SetNeedsDisplay ();
			};

			_btnRemoveMenuBar.Clicked = () => {
				if (_menuBar != null && _menuBar.Menus.Length > 0) {
					_menuBar.Menus [_currentSelectedMenuBar] = null;
					int i = 0;
					foreach (var m in _menuBar.Menus) {
						if (m != null) {
							_menuBar.Menus [i] = m;
							i++;
						}
					}
					var menus = _menuBar.Menus;
					Array.Resize (ref menus, menus.Length - 1);
					_menuBar.Menus = menus;
					if (_currentSelectedMenuBar - 1 >= 0 && _menuBar.Menus.Length > 0) {
						_currentSelectedMenuBar--;
					}
					_currentMenuBarItem = _menuBar.Menus?.Length > 0 ? _menuBar.Menus [_currentSelectedMenuBar] : null;
				}
				if (MenuBar != null && _currentMenuBarItem == null && _menuBar.Menus.Length == 0) {
					Remove (_menuBar);
					_menuBar = null;
					DataContext.Menus = new List<DynamicMenuItemList> ();
					_currentMenuBarItem = null;
					_currentSelectedMenuBar = -1;
					_lblMenuBar.Text = ustring.Empty;
				} else {
					_lblMenuBar.Text = _menuBar.Menus [_currentSelectedMenuBar].Title;
				}
				SetListViewSource (_currentMenuBarItem, true);
				EditMenuBarItem (null);
			};

			_lblMenuBar.Enter = (e) => {
				if (_menuBar?.Menus != null) {
					_currentMenuBarItem = _menuBar.Menus [_currentSelectedMenuBar];
					EditMenuBarItem (_menuBar.Menus [_currentSelectedMenuBar]);
				}
			};

			_btnPrevious.Clicked = () => {
				if (_currentSelectedMenuBar - 1 > -1) {
					_currentSelectedMenuBar--;
				}
				SelectCurrentMenuBarItem ();
			};

			_btnNext.Clicked = () => {
				if (_menuBar != null && _currentSelectedMenuBar + 1 < _menuBar.Menus.Length) {
					_currentSelectedMenuBar++;
				}
				SelectCurrentMenuBarItem ();
			};

			_lstMenus.SelectedItemChanged = (e) => {
				var menuBarItem = DataContext.Menus.Count > 0 ? DataContext.Menus [e.Item].MenuItem : null;
				EditMenuBarItem (menuBarItem);
			};

			_lstMenus.OpenSelectedItem = (e) => {
				_currentMenuBarItem = DataContext.Menus [e.Item].MenuItem;
				DataContext.Parent = _currentMenuBarItem.Title;
				DataContext.Menus = new List<DynamicMenuItemList> ();
				SetListViewSource (_currentMenuBarItem, true);
				var menuBarItem = DataContext.Menus.Count > 0 ? DataContext.Menus [0].MenuItem : null;
				EditMenuBarItem (menuBarItem);
			};

			_btnPreviowsParent.Clicked = () => {
				if (_currentMenuBarItem != null && _currentMenuBarItem.Parent != null) {
					var mi = _currentMenuBarItem;
					_currentMenuBarItem = _currentMenuBarItem.Parent as MenuBarItem;
					SetListViewSource (_currentMenuBarItem, true);
					var i = ((MenuBarItem)_currentMenuBarItem).GetChildrenIndex (mi);
					if (i > -1) {
						_lstMenus.SelectedItem = i;
					}
					if (_currentMenuBarItem.Parent != null) {
						DataContext.Parent = _currentMenuBarItem.Title;
					} else {
						DataContext.Parent = ustring.Empty;
					}
				} else {
					DataContext.Parent = ustring.Empty;
				}
			};

			var ustringConverter = new UStringValueConverter ();
			var listWrapperConverter = new ListWrapperConverter ();

			var lblMenuBar = new Binding (this, "MenuBar", _lblMenuBar, "Text", ustringConverter);
			var lblParent = new Binding (this, "Parent", _lblParent, "Text", ustringConverter);
			var lstMenus = new Binding (this, "Menus", _lstMenus, "Source", listWrapperConverter);


			ustring GetTargetAction (Action action)
			{
				var me = action.Target;

				if (me == null) {
					throw new ArgumentException ();
				}
				object v = new object ();
				foreach (var field in me.GetType ().GetFields ()) {
					if (field.Name == "item") {
						v = field.GetValue (me);
					}
				}
				return v == null || !(v is DynamicMenuItem item) ? ustring.Empty : item.action;
			}

			Action CreateAction (MenuItem menuItem, DynamicMenuItem item)
			{
				switch (menuItem.CheckType) {
				case MenuItemCheckStyle.NoCheck:
					return new Action (() => MessageBox.ErrorQuery (item.title, item.action, "Ok"));
				case MenuItemCheckStyle.Checked:
					return new Action (() => menuItem.Checked = !menuItem.Checked);
				case MenuItemCheckStyle.Radio:
					break;
				}
				return new Action (() => {
					menuItem.Checked = true;
					var parent = menuItem?.Parent as MenuBarItem;
					if (parent != null) {
						var childrens = parent.Children;
						for (int i = 0; i < childrens.Length; i++) {
							var child = childrens [i];
							if (child != menuItem) {
								child.Checked = false;
							}
						}
					}
				});
			}

			void SetListViewSource (MenuItem _currentMenuBarItem, bool fill = false)
			{
				DataContext.Menus = new List<DynamicMenuItemList> ();
				var menuBarItem = _currentMenuBarItem as MenuBarItem;
				if (menuBarItem != null && menuBarItem?.Children == null) {
					return;
				}
				if (!fill) {
					return;
				}
				if (menuBarItem != null) {
					foreach (var child in menuBarItem?.Children) {
						var m = new DynamicMenuItemList (child.Title, child);
						DataContext.Menus.Add (m);
					}
				}
			}

			void EditMenuBarItem (MenuItem menuBarItem)
			{
				if (menuBarItem == null) {
					_frmMenuDetails.CanFocus = false;
				} else {
					_frmMenuDetails.CanFocus = true;
				}
				_currentEditMenuBarItem = menuBarItem;
				_txtTitle.Text = menuBarItem?.Title ?? "";
				_txtHelp.Text = menuBarItem?.Help ?? "";
				_txtAction.Text = menuBarItem != null && menuBarItem.Action != null ? GetTargetAction (menuBarItem.Action) : ustring.Empty;
				_ckbIsTopLevel.Checked = IsTopLevel (menuBarItem);
				_ckbSubMenu.Checked = HasSubMenus (menuBarItem);
				_rbChkStyle.SelectedItem = (int)(menuBarItem?.CheckType ?? MenuItemCheckStyle.NoCheck);
			}

			void UpdateMenuItem (MenuItem _currentEditMenuBarItem, DynamicMenuItem menuItem, int index)
			{
				_currentEditMenuBarItem.Title = menuItem.title;
				_currentEditMenuBarItem.Help = menuItem.help;
				_currentEditMenuBarItem.CheckType = menuItem.checkStyle;
				var parent = _currentEditMenuBarItem.Parent as MenuBarItem;
				if (parent != null && parent.Children.Length == 1 && _currentEditMenuBarItem.CheckType == MenuItemCheckStyle.Radio) {
					_currentEditMenuBarItem.Checked = true;
				}
				if (menuItem.isTopLevel && _currentEditMenuBarItem is MenuBarItem) {
					((MenuBarItem)_currentEditMenuBarItem).Children = null;
					_currentEditMenuBarItem.Action = CreateAction (_currentEditMenuBarItem, menuItem);
					SetListViewSource (_currentEditMenuBarItem, true);
				} else if (menuItem.hasSubMenu) {
					_currentEditMenuBarItem.Action = null;
					if (_currentEditMenuBarItem is MenuBarItem && ((MenuBarItem)_currentEditMenuBarItem).Children == null) {
						((MenuBarItem)_currentEditMenuBarItem).Children = new MenuItem [] { new MenuItem ("_New", "", CreateAction (_currentEditMenuBarItem, new DynamicMenuItem ())) };
					} else if (_currentEditMenuBarItem.Parent != null) {
						UpdateParent (ref _currentEditMenuBarItem);
					} else {
						_currentEditMenuBarItem = new MenuBarItem (_currentEditMenuBarItem.Title, new MenuItem [] { new MenuItem ("_New", "", CreateAction (_currentEditMenuBarItem, new DynamicMenuItem ())) }, _currentEditMenuBarItem.Parent);
					}
					SetListViewSource (_currentEditMenuBarItem, true);
				} else if (_currentEditMenuBarItem is MenuBarItem && _currentEditMenuBarItem.Parent != null) {
					UpdateParent (ref _currentEditMenuBarItem);
					_currentEditMenuBarItem = new MenuItem (menuItem.title, menuItem.help, CreateAction (_currentEditMenuBarItem, menuItem), null, _currentEditMenuBarItem.Parent);
				} else {
					if (_currentEditMenuBarItem is MenuBarItem) {
						((MenuBarItem)_currentEditMenuBarItem).Children = null;
						DataContext.Menus = new List<DynamicMenuItemList> ();
					}
					_currentEditMenuBarItem.Action = CreateAction (_currentEditMenuBarItem, menuItem);
				}

				if (_currentEditMenuBarItem.Parent == null) {
					DataContext.MenuBar = _currentEditMenuBarItem.Title;
				} else {
					DataContext.Menus [index] = new DynamicMenuItemList (_currentEditMenuBarItem.Title, _currentEditMenuBarItem);
				}
				_currentEditMenuBarItem.CheckType = menuItem.checkStyle;
				EditMenuBarItem (_currentEditMenuBarItem);
			}

			void UpdateParent (ref MenuItem menuItem)
			{
				var parent = menuItem.Parent as MenuBarItem;
				var idx = parent.GetChildrenIndex (menuItem);
				if (!(menuItem is MenuBarItem)) {
					menuItem = new MenuBarItem (menuItem.Title, new MenuItem [] { new MenuItem ("_New", "", CreateAction (menuItem, new DynamicMenuItem ())) }, menuItem.Parent);
					if (idx > -1) {
						parent.Children [idx] = menuItem;
					}
				} else {
					menuItem = new MenuItem (menuItem.Title, menuItem.Help, CreateAction (menuItem, new DynamicMenuItem ()), null, menuItem.Parent);
					if (idx > -1) {
						parent.Children [idx] = menuItem;
					}
				}
			}

			bool IsTopLevel (MenuItem menuItem)
			{
				var topLevel = menuItem as MenuBarItem;
				if (topLevel != null && topLevel.Parent == null && (topLevel.Children == null || topLevel.Children.Length == 0)) {
					return true;
				} else {
					return false;
				}
			}

			bool HasSubMenus (MenuItem menuItem)
			{
				var menuBarItem = menuItem as MenuBarItem;
				if (menuBarItem != null && menuBarItem.Children != null && menuBarItem.Children.Length > 0) {
					return true;
				} else {
					return false;
				}
			}

			void SelectCurrentMenuBarItem ()
			{
				MenuBarItem menuBarItem = null;
				if (_menuBar?.Menus != null) {
					menuBarItem = _menuBar.Menus [_currentSelectedMenuBar];
					_lblMenuBar.Text = menuBarItem.Title;
				}
				EditMenuBarItem (menuBarItem);
				_currentMenuBarItem = menuBarItem;
				DataContext.Menus = new List<DynamicMenuItemList> ();
				SetListViewSource (_currentMenuBarItem, true);
				_lblParent.Text = ustring.Empty;
			}

			DynamicMenuItem EnterMenuItem (MenuItem menuItem)
			{
				var _lblTitle = new Label (1, 3, "Title:");
				var _txtTitle = new TextField ("_New") {
					X = Pos.Right (_lblTitle) + 2,
					Y = Pos.Top (_lblTitle),
					Width = Dim.Fill (),
				};
				var _lblHelp = new Label ("Help:") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_lblTitle) + 1
				};
				var _txtHelp = new TextField () {
					X = Pos.Left (_txtTitle),
					Y = Pos.Top (_lblHelp),
					Width = Dim.Fill (),
				};
				var _lblAction = new Label ("Action:") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_lblHelp) + 1
				};
				var _txtAction = new TextView () {
					ColorScheme = Colors.Menu,
					X = Pos.Left (_txtTitle),
					Y = Pos.Top (_lblAction),
					Width = Dim.Fill (),
					Height = 5,
					ReadOnly = true
				};
				var _ckbIsTopLevel = new CheckBox ("IsTopLevel") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_lblAction) + 5
				};
				var _ckbSubMenu = new CheckBox ("Has sub-menus") {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_ckbIsTopLevel),
					Checked = menuItem == null
				};
				_ckbIsTopLevel.Toggled = (e) => {
					if (_ckbIsTopLevel.Checked && menuItem != null) {
						MessageBox.ErrorQuery ("Invalid IsTopLevel", "Only menu bar can have top level menu item!", "Ok");
						_ckbIsTopLevel.Checked = false;
						return;
					}
					if (_ckbIsTopLevel.Checked) {
						_ckbSubMenu.Checked = false;
						_ckbSubMenu.SetNeedsDisplay ();
						_txtAction.ReadOnly = false;
					} else {
						_txtAction.ReadOnly = true;
					}
				};
				_ckbSubMenu.Toggled = (e) => {
					if (_ckbSubMenu.Checked) {
						_ckbIsTopLevel.Checked = false;
						_ckbIsTopLevel.SetNeedsDisplay ();
						_txtAction.ReadOnly = true;
					} else {
						_txtAction.ReadOnly = false;
					}
				};
				var _rChkLabels = new ustring [] { "NoCheck", "Checked", "Radio" };
				var _rbChkStyle = new RadioGroup (_rChkLabels) {
					X = Pos.Left (_lblTitle),
					Y = Pos.Bottom (_ckbSubMenu) + 1,
				};
				var _btnOk = new Button ("Ok") {
					IsDefault = true,
					Clicked = () => {
						if (ustring.IsNullOrEmpty (_txtTitle.Text)) {
							MessageBox.ErrorQuery ("Invalid title", "Must enter a valid title!.", "Ok");
						} else {
							Application.RequestStop ();
						}
					}
				};
				var _btnCancel = new Button ("Cancel") {
					Clicked = () => {
						_txtTitle.Text = ustring.Empty;
						Application.RequestStop ();
					}
				};
				var _dialog = new Dialog ("Please enter the menu details.", _btnOk, _btnCancel);
				_dialog.Add (_lblTitle, _txtTitle, _lblHelp, _txtHelp, _lblAction, _txtAction, _ckbIsTopLevel, _ckbSubMenu, _rbChkStyle);
				_txtTitle.SetFocus ();
				Application.Run (_dialog);

				return new DynamicMenuItem (_txtTitle.Text, _txtHelp.Text, _txtAction.Text, _ckbIsTopLevel != null ? _ckbIsTopLevel.Checked : false, _ckbSubMenu != null ? _ckbSubMenu.Checked : false, _rbChkStyle.SelectedItem == 0 ? MenuItemCheckStyle.NoCheck : _rbChkStyle.SelectedItem == 1 ? MenuItemCheckStyle.Checked : MenuItemCheckStyle.Radio);
			}

			MenuItem CreateNewMenu (DynamicMenuItem item, MenuItem parent = null)
			{
				MenuItem newMenu;
				if (item.hasSubMenu) {
					newMenu = new MenuBarItem (item.title, new MenuItem [] { new MenuItem ("_New", "", null) }, parent);
					((MenuBarItem)newMenu).Children [0].Action = CreateAction (newMenu, new DynamicMenuItem ());
				} else if (parent != null) {
					newMenu = new MenuItem (item.title, item.help, null, null, parent);
					newMenu.CheckType = item.checkStyle;
					newMenu.Action = CreateAction (newMenu, item);
				} else {
					newMenu = new MenuBarItem (item.title, item.help, null);
					((MenuBarItem)newMenu).Children [0].Action = CreateAction (newMenu, item);
				}

				return newMenu;
			}
		}
	}

	class DynamicMenuItemModel : INotifyPropertyChanged {
		public event PropertyChangedEventHandler PropertyChanged;

		private ustring menuBar;
		private ustring parent;
		private List<DynamicMenuItemList> menus;

		public ustring MenuBar {
			get => menuBar;
			set {
				if (value != menuBar) {
					menuBar = value;
					PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
				}
			}
		}

		public ustring Parent {
			get => parent;
			set {
				if (value != parent) {
					parent = value;
					PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
				}
			}
		}

		public List<DynamicMenuItemList> Menus {
			get => menus;
			set {
				if (value != menus) {
					menus = value;
					PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (GetPropertyName ()));
				}
			}
		}

		public DynamicMenuItemModel ()
		{
			Menus = new List<DynamicMenuItemList> ();
		}

		public string GetPropertyName ([CallerMemberName] string propertyName = null)
		{
			return propertyName;
		}
	}

	public interface IValueConverter {
		object Convert (object value, object parameter = null);
	}

	public class Binding {
		public View Target { get; private set; }
		public View Source { get; private set; }

		public string SourcePropertyName { get; private set; }
		public string TargetPropertyName { get; private set; }

		private object sourceDataContext;
		private PropertyInfo sourceBindingProperty;
		private IValueConverter valueConverter;

		public Binding (View source, string sourcePropertyName, View target, string targetPropertyName, IValueConverter valueConverter = null)
		{
			Target = target;
			Source = source;
			SourcePropertyName = sourcePropertyName;
			TargetPropertyName = targetPropertyName;
			sourceDataContext = Source.GetType ().GetProperty ("DataContext").GetValue (Source);
			sourceBindingProperty = sourceDataContext.GetType ().GetProperty (SourcePropertyName);
			this.valueConverter = valueConverter;
			UpdateTarget ();

			var notifier = ((INotifyPropertyChanged)sourceDataContext);
			if (notifier != null) {
				notifier.PropertyChanged += (s, e) => {
					if (e.PropertyName == SourcePropertyName) {
						UpdateTarget ();
					}
				};
			}
		}

		private void UpdateTarget ()
		{
			try {
				var sourceValue = sourceBindingProperty.GetValue (sourceDataContext);
				if (sourceValue == null) {
					return;
				}

				var finalValue = valueConverter?.Convert (sourceValue) ?? sourceValue;

				var targetProperty = Target.GetType ().GetProperty (TargetPropertyName);
				targetProperty.SetValue (Target, finalValue);
			} catch (Exception ex) {
				MessageBox.ErrorQuery ("Binding Error", $"Binding failed: {ex}.", "Ok");
			}
		}
	}

	public class ListWrapperConverter : IValueConverter {
		public object Convert (object value, object parameter = null)
		{
			return new ListWrapper ((IList)value);
		}
	}

	public class UStringValueConverter : IValueConverter {
		public object Convert (object value, object parameter = null)
		{
			var data = Encoding.ASCII.GetBytes (value.ToString ());
			return ustring.Make (data);
		}
	}
}
