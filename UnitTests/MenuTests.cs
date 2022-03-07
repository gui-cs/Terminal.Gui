﻿using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Terminal.Gui.Views {
	public class MenuTests {
		readonly ITestOutputHelper output;

		public MenuTests (ITestOutputHelper output)
		{
			this.output = output;
		}

		[Fact]
		public void Constuctors_Defaults ()
		{
			var menu = new Menu (new MenuBar (), 0, 0, new MenuBarItem ());
			Assert.Equal (Colors.Menu, menu.ColorScheme);
			Assert.True (menu.CanFocus);
			Assert.False (menu.WantContinuousButtonPressed);

			var menuBar = new MenuBar ();
			Assert.Equal (0, menuBar.X);
			Assert.Equal (0, menuBar.Y);
			Assert.IsType<Dim.DimFill> (menuBar.Width);
			Assert.Equal (1, menuBar.Height);
			Assert.Empty (menuBar.Menus);
			Assert.Equal (Colors.Menu, menuBar.ColorScheme);
			Assert.True (menuBar.WantMousePositionReports);
			Assert.False (menuBar.IsMenuOpen);

			menuBar = new MenuBar (new MenuBarItem [] { });
			Assert.Equal (0, menuBar.X);
			Assert.Equal (0, menuBar.Y);
			Assert.IsType<Dim.DimFill> (menuBar.Width);
			Assert.Equal (1, menuBar.Height);
			Assert.Empty (menuBar.Menus);
			Assert.Equal (Colors.Menu, menuBar.ColorScheme);
			Assert.True (menuBar.WantMousePositionReports);
			Assert.False (menuBar.IsMenuOpen);

			var menuBarItem = new MenuBarItem ();
			Assert.Equal ("", menuBarItem.Title);
			Assert.Null (menuBarItem.Parent);
			Assert.Empty (menuBarItem.Children);

			menuBarItem = new MenuBarItem (new MenuBarItem [] { });
			Assert.Equal ("", menuBarItem.Title);
			Assert.Null (menuBarItem.Parent);
			Assert.Empty (menuBarItem.Children);

			menuBarItem = new MenuBarItem ("Test", new MenuBarItem [] { });
			Assert.Equal ("Test", menuBarItem.Title);
			Assert.Null (menuBarItem.Parent);
			Assert.Empty (menuBarItem.Children);

			menuBarItem = new MenuBarItem ("Test", new List<MenuItem []> ());
			Assert.Equal ("Test", menuBarItem.Title);
			Assert.Null (menuBarItem.Parent);
			Assert.Empty (menuBarItem.Children);

			menuBarItem = new MenuBarItem ("Test", "Help", null);
			Assert.Equal ("Test", menuBarItem.Title);
			Assert.Equal ("Help", menuBarItem.Help);
			Assert.Null (menuBarItem.Action);
			Assert.Null (menuBarItem.CanExecute);
			Assert.Null (menuBarItem.Parent);
			Assert.Equal (Key.Null, menuBarItem.Shortcut);

			var menuItem = new MenuItem ();
			Assert.Equal ("", menuItem.Title);
			Assert.Equal ("", menuItem.Help);
			Assert.Null (menuItem.Action);
			Assert.Null (menuItem.CanExecute);
			Assert.Null (menuItem.Parent);
			Assert.Equal (Key.Null, menuItem.Shortcut);

			menuItem = new MenuItem ("Test", "Help", Run, () => { return true; }, new MenuItem (), Key.F1);
			Assert.Equal ("Test", menuItem.Title);
			Assert.Equal ("Help", menuItem.Help);
			Assert.Equal (Run, menuItem.Action);
			Assert.NotNull (menuItem.CanExecute);
			Assert.NotNull (menuItem.Parent);
			Assert.Equal (Key.F1, menuItem.Shortcut);

			void Run () { }
		}

		[Fact]
		public void Exceptions ()
		{
			Assert.Throws<ArgumentNullException> (() => new MenuBarItem ("Test", (MenuItem [])null, null));
			Assert.Throws<ArgumentNullException> (() => new MenuBarItem ("Test", (List<MenuItem []>)null, null));
		}

		[Fact]
		[AutoInitShutdown]
		public void MenuOpening_MenuOpened_MenuClosing_Events ()
		{
			var miAction = "";
			var isMenuClosed = true;
			var cancelClosing = false;

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "Creates new file.", New)
				})
			});
			menu.MenuOpening += (e) => {
				Assert.Equal ("_File", e.CurrentMenu.Title);
				Assert.Equal ("_New", e.CurrentMenu.Children [0].Title);
				Assert.Equal ("Creates new file.", e.CurrentMenu.Children [0].Help);
				Assert.Equal (New, e.CurrentMenu.Children [0].Action);
				e.CurrentMenu.Children [0].Action ();
				Assert.Equal ("New", miAction);
				e.NewMenuBarItem = new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "Copies the selection.", Copy)
				});
			};
			menu.MenuOpened += (e) => {
				Assert.Equal ("_Edit", e.Parent.Title);
				Assert.Equal ("_Copy", e.Title);
				Assert.Equal ("Copies the selection.", e.Help);
				Assert.Equal (Copy, e.Action);
				e.Action ();
				Assert.Equal ("Copy", miAction);
			};
			menu.MenuClosing += (e) => {
				Assert.False (isMenuClosed);
				if (cancelClosing) {
					e.Cancel = true;
					isMenuClosed = false;
				} else {
					isMenuClosed = true;
				}
			};
			Application.Top.Add (menu);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			isMenuClosed = !menu.IsMenuOpen;
			Assert.False (isMenuClosed);
			Application.Top.Redraw (Application.Top.Bounds);
			var expected = @"
Edit
┌─────────────────────────────┐
│ Copy  Copies the selection. │
└─────────────────────────────┘
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			cancelClosing = true;
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.False (isMenuClosed);
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
Edit
┌─────────────────────────────┐
│ Copy  Copies the selection. │
└─────────────────────────────┘
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			cancelClosing = false;
			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.True (isMenuClosed);
			Application.Top.Redraw (Application.Top.Bounds);
			expected = @"
Edit
";
			GraphViewTests.AssertDriverContentsAre (expected, output);

			void New () => miAction = "New";
			void Copy () => miAction = "Copy";
		}

		[Fact]
		[AutoInitShutdown]
		public void MouseEvent_Test ()
		{
			MenuItem miCurrent = null;
			Menu mCurrent = null;
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", null),
					new MenuItem ("_Open", "", null),
					new MenuItem ("_Save", "", null)
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", null),
					new MenuItem ("C_ut", "", null),
					new MenuItem ("_Paste", "", null)
				})
			});
			menu.MenuOpened += (e) => {
				miCurrent = e;
				mCurrent = menu.openCurrentMenu;
			};
			Application.Top.Add (menu);

			Assert.True (menu.MouseEvent (new MouseEvent () {
				X = 10,
				Y = 0,
				Flags = MouseFlags.Button1Pressed,
				View = menu
			}));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", miCurrent.Parent.Title);
			Assert.Equal ("_Copy", miCurrent.Title);

			Assert.True (mCurrent.MouseEvent (new MouseEvent () {
				X = 10,
				Y = 3,
				Flags = MouseFlags.ReportMousePosition,
				View = mCurrent
			}));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", miCurrent.Parent.Title);
			Assert.Equal ("_Paste", miCurrent.Title);

			for (int i = 2; i >= -1; i--) {
				if (i == -1) {
					Assert.False (mCurrent.MouseEvent (new MouseEvent () {
						X = 10,
						Y = i,
						Flags = MouseFlags.ReportMousePosition,
						View = menu
					}));
				} else {
					Assert.True (mCurrent.MouseEvent (new MouseEvent () {
						X = 10,
						Y = i,
						Flags = MouseFlags.ReportMousePosition,
						View = mCurrent
					}));
				}
				Assert.True (menu.IsMenuOpen);
				if (i == 2) {
					Assert.Equal ("_Edit", miCurrent.Parent.Title);
					Assert.Equal ("C_ut", miCurrent.Title);
				} else if (i == 1) {
					Assert.Equal ("_Edit", miCurrent.Parent.Title);
					Assert.Equal ("_Copy", miCurrent.Title);
				} else if (i == 0) {
					Assert.Equal ("_Edit", miCurrent.Parent.Title);
					Assert.Equal ("_Copy", miCurrent.Title);
				} else {
					Assert.Equal ("_Edit", miCurrent.Parent.Title);
					Assert.Equal ("_Copy", miCurrent.Title);
				}
			}
		}

		[Fact]
		[AutoInitShutdown]
		public void KeyBindings_Command ()
		{
			var miAction = "";
			MenuItem mbiCurrent = null;
			MenuItem miCurrent = null;
			Menu mCurrent = null;

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_New", "", () => miAction ="New"),
					new MenuItem ("_Open", "", () => miAction ="Open"),
					new MenuItem ("_Save", "", () => miAction ="Save"),
					null,
					new MenuItem ("_Quit", "", () => miAction ="Quit"),
				}),
				new MenuBarItem ("_Edit", new MenuItem [] {
					new MenuItem ("_Copy", "", () => miAction ="Copy"),
					new MenuItem ("C_ut", "", () => miAction ="Cut"),
					new MenuItem ("_Paste", "", () => miAction ="Paste"),
					new MenuBarItem ("_Find and Replace", new MenuItem [] {
						new MenuItem ("F_ind", "", null),
						new MenuItem ("_Replace", "", null)
					}),
					new MenuItem ("_Select All", "", () => miAction ="Select All")
				}),
				new MenuBarItem ("_About", "Top-Level", () => miAction ="About")
			});
			menu.MenuOpening += (e) => mbiCurrent = e.CurrentMenu;
			menu.MenuOpened += (e) => {
				miCurrent = e;
				mCurrent = menu.openCurrentMenu;
			};
			menu.MenuClosing += (_) => {
				mbiCurrent = null;
				miCurrent = null;
				mCurrent = null;
			};
			Application.Top.Add (menu);
			Application.Begin (Application.Top);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_About", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (menu.ProcessKey (new KeyEvent (Key.C | Key.CtrlMask, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (menu.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.False (mCurrent.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.True (Application.Top.ProcessKey (new KeyEvent (Key.Tab, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Quit", GetCurrentMenuTitle ());

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_About", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());

			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.Esc, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());
			Application.MainLoop.MainIteration ();
			Assert.Equal ("New", miAction);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_About", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());
			Application.MainLoop.MainIteration ();
			Assert.Equal ("About", miAction);

			Assert.True (menu.ProcessHotKey (new KeyEvent (Key.F9, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_File", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_New", GetCurrentMenuTitle ());
			Assert.True (menu.ProcessKey (new KeyEvent (Key.CursorRight, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Copy", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("C_ut", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Paste", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("F_ind", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Replace", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("F_ind", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorLeft, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorDown, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Select All", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("F_ind", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Find and Replace", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Edit", GetCurrenParenttMenuItemTitle ());
			Assert.Equal ("_Paste", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("C_ut", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.CursorUp, new KeyModifiers ())));
			Assert.True (menu.IsMenuOpen);
			Assert.Equal ("_Edit", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("_Copy", GetCurrentMenuTitle ());
			Assert.True (mCurrent.ProcessKey (new KeyEvent (Key.Enter, new KeyModifiers ())));
			Assert.False (menu.IsMenuOpen);
			Assert.Equal ("Closed", GetCurrentMenuBarItemTitle ());
			Assert.Equal ("None", GetCurrentMenuTitle ());
			Application.MainLoop.MainIteration ();
			Assert.Equal ("Copy", miAction);


			string GetCurrentMenuBarItemTitle ()
			{
				return mbiCurrent != null ? mbiCurrent.Title.ToString () : "Closed";
			}

			string GetCurrenParenttMenuItemTitle ()
			{
				return miCurrent?.Parent != null ? miCurrent.Parent.Title.ToString () : "None";
			}

			string GetCurrentMenuTitle ()
			{
				return miCurrent != null ? miCurrent.Title.ToString () : "None";
			}
		}

		[Fact, AutoInitShutdown]
		public void DrawFrame_With_Positive_Positions ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			});

			Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));

			menu.OpenMenu ();
			Application.Begin (Application.Top);

			var expected = @"
┌──────┐
│ One  │
│ Two  │
└──────┘
";

			var pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (0, 1), pos);
		}

		[Fact, AutoInitShutdown]
		public void DrawFrame_With_Negative_Positions ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem (new MenuItem [] {
					new MenuItem ("One", "", null),
					new MenuItem ("Two", "", null)
				})
			}) {
				X = -1,
				Y = -1
			};

			Assert.Equal (new Point (-1, -1), new Point (menu.Frame.X, menu.Frame.Y));

			menu.OpenMenu ();
			Application.Begin (Application.Top);

			var expected = @"
──────┐
 One  │
 Two  │
──────┘
";

			var pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (0, 0), pos);

			menu.CloseAllMenus ();
			menu.Frame = new Rect (-1, -2, menu.Frame.Width, menu.Frame.Height);
			menu.OpenMenu ();
			Application.Refresh ();

			expected = @"
 One  │
 Two  │
──────┘
";

			pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (1, 0), pos);

			menu.CloseAllMenus ();
			menu.Frame = new Rect (0, 0, menu.Frame.Width, menu.Frame.Height);
			((FakeDriver)Application.Driver).SetBufferSize (7, 5);
			menu.OpenMenu ();
			Application.Refresh ();

			expected = @"
┌──────
│ One  
│ Two  
└──────
";

			pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (0, 1), pos);

			menu.CloseAllMenus ();
			menu.Frame = new Rect (0, 0, menu.Frame.Width, menu.Frame.Height);
			((FakeDriver)Application.Driver).SetBufferSize (7, 4);
			menu.OpenMenu ();
			Application.Refresh ();

			expected = @"
┌──────
│ One  
│ Two  
";

			pos = GraphViewTests.AssertDriverContentsWithPosAre (expected, output);
			Assert.Equal (new Point (0, 1), pos);
		}
	}
}
