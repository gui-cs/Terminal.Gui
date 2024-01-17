using System;
using System.Collections.Generic;
using System.Linq;
using UICatalog.Scenarios;
using Xunit;
using Xunit.Abstractions;

//using static Terminal.Gui.ViewTests.MenuTests;

namespace Terminal.Gui.ViewsTests;

public class MenuBarTests {
	readonly ITestOutputHelper _output;

	public MenuBarTests (ITestOutputHelper output)
	{
		_output = output;
	}

	// TODO: Lots of tests in here really test Menu and MenuItem - Move them to MenuTests.cs

	[Fact]
	public void Constuctors_Defaults ()
	{
		var menuBar = new MenuBar ();
		Assert.Equal (KeyCode.F9, menuBar.Key);
		var menu = new Menu (menuBar, 0, 0, new MenuBarItem (), null, menuBar.MenusBorderStyle);
		Assert.Equal (Colors.ColorSchemes ["Menu"], menu.ColorScheme);
		Assert.True (menu.CanFocus);
		Assert.False (menu.WantContinuousButtonPressed);
		Assert.Equal (LineStyle.Single, menuBar.MenusBorderStyle);

		menuBar = new MenuBar ();
		Assert.Equal (0, menuBar.X);
		Assert.Equal (0, menuBar.Y);
		Assert.IsType<Dim.DimFill> (menuBar.Width);
		Assert.Equal (1, menuBar.Height);
		Assert.Empty (menuBar.Menus);
		Assert.Equal (Colors.ColorSchemes ["Menu"], menuBar.ColorScheme);
		Assert.True (menuBar.WantMousePositionReports);
		Assert.False (menuBar.IsMenuOpen);

		menuBar = new MenuBar (new MenuBarItem [] { });
		Assert.Equal (0, menuBar.X);
		Assert.Equal (0, menuBar.Y);
		Assert.IsType<Dim.DimFill> (menuBar.Width);
		Assert.Equal (1, menuBar.Height);
		Assert.Empty (menuBar.Menus);
		Assert.Equal (Colors.ColorSchemes ["Menu"], menuBar.ColorScheme);
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
		Assert.Equal (KeyCode.Null, menuBarItem.Shortcut);

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
		string miAction = "";
		bool isMenuClosed = true;
		bool cancelClosing = false;

		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "Creates new file.", New)
			})
		});
		menu.MenuOpening += (s, e) => {
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
		menu.MenuOpened += (s, e) => {
			var mi = e.MenuItem;

			Assert.Equal ("_Edit", mi.Parent.Title);
			Assert.Equal ("_Copy", mi.Title);
			Assert.Equal ("Copies the selection.", mi.Help);
			Assert.Equal (Copy, mi.Action);
			mi.Action ();
			Assert.Equal ("Copy", miAction);
		};
		menu.MenuClosing += (s, e) => {
			Assert.False (isMenuClosed);
			if (cancelClosing) {
				e.Cancel = true;
				isMenuClosed = false;
			} else {
				isMenuClosed = true;
			}
		};
		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu.IsMenuOpen);
		isMenuClosed = !menu.IsMenuOpen;
		Assert.False (isMenuClosed);
		Application.Top.Draw ();
		string expected = @"
Edit
┌──────────────────────────────┐
│ Copy   Copies the selection. │
└──────────────────────────────┘
";
		TestHelpers.AssertDriverContentsAre (expected, _output);

		cancelClosing = true;
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu.IsMenuOpen);
		Assert.False (isMenuClosed);
		Application.Top.Draw ();
		expected = @"
Edit
┌──────────────────────────────┐
│ Copy   Copies the selection. │
└──────────────────────────────┘
";
		TestHelpers.AssertDriverContentsAre (expected, _output);

		cancelClosing = false;
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.False (menu.IsMenuOpen);
		Assert.True (isMenuClosed);
		Application.Top.Draw ();
		expected = @"
Edit
";
		TestHelpers.AssertDriverContentsAre (expected, _output);

		void New ()
		{
			miAction = "New";
		}
		void Copy ()
		{
			miAction = "Copy";
		}
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuOpened_On_Disabled_MenuItem ()
	{
		MenuItem miCurrent = null;
		Menu mCurrent = null;

		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuBarItem ("_New", new MenuItem [] {
					new MenuItem ("_New doc", "Creates new doc.", null, () => false)
				}),
				null,
				new MenuItem ("_Save", "Saves the file.", null, null)
			})
		});
		menu.MenuOpened += (s, e) => {
			miCurrent = e.MenuItem;
			mCurrent = menu._openMenu;
		};
		menu.UseKeysUpDownAsKeysLeftRight = true;
		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		// open the menu
		Assert.True (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		Assert.True (menu.IsMenuOpen);
		Assert.Equal ("_File", miCurrent.Parent.Title);
		Assert.Equal ("_New", miCurrent.Title);

		Assert.True (mCurrent.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 1,
			Flags = MouseFlags.ReportMousePosition,
			View = mCurrent
		}));
		Assert.True (menu.IsMenuOpen);
		Assert.Equal ("_File", miCurrent.Parent.Title);
		Assert.Equal ("_New", miCurrent.Title);

		Assert.True (mCurrent.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 2,
			Flags = MouseFlags.ReportMousePosition,
			View = mCurrent
		}));
		Assert.True (menu.IsMenuOpen);
		Assert.Equal ("_File", miCurrent.Parent.Title);
		Assert.Equal ("_New", miCurrent.Title);

		Assert.True (mCurrent.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 3,
			Flags = MouseFlags.ReportMousePosition,
			View = mCurrent
		}));
		Assert.True (menu.IsMenuOpen);
		Assert.Equal ("_File", miCurrent.Parent.Title);
		Assert.Equal ("_Save", miCurrent.Title);

		// close the menu
		Assert.True (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		Assert.False (menu.IsMenuOpen);

		// open the menu
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu.IsMenuOpen);
		// The _New doc isn't enabled because it can't execute and so can't be selected
		Assert.Equal ("_File", miCurrent.Parent.Title);
		Assert.Equal ("_New", miCurrent.Title);

		Assert.True (mCurrent.NewKeyDownEvent (new Key (KeyCode.CursorDown)));
		Assert.True (menu.IsMenuOpen);
		Assert.Equal ("_File", miCurrent.Parent.Title);
		Assert.Equal ("_Save", miCurrent.Title);

		Assert.True (mCurrent.NewKeyDownEvent (new Key (KeyCode.CursorUp)));
		Assert.True (menu.IsMenuOpen);
		Assert.Equal ("_File", miCurrent.Parent.Title);
		Assert.Equal ("_New", miCurrent.Title);

		// close the menu
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.False (menu.IsMenuOpen);
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
		menu.MenuOpened += (s, e) => {
			miCurrent = e.MenuItem;
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

	[Theory]
	[AutoInitShutdown]
	[InlineData ("_File", "_New", "", KeyCode.Space | KeyCode.CtrlMask)]
	[InlineData ("Closed", "None", "", KeyCode.Space | KeyCode.CtrlMask, KeyCode.Space | KeyCode.CtrlMask)]
	[InlineData ("_File", "_New", "", KeyCode.F9)]
	[InlineData ("Closed", "None", "", KeyCode.F9, KeyCode.F9)]
	[InlineData ("_File", "_Open", "", KeyCode.F9, KeyCode.CursorDown)]
	[InlineData ("_File", "_Save", "", KeyCode.F9, KeyCode.CursorDown, KeyCode.CursorDown)]
	[InlineData ("_File", "_Quit", "", KeyCode.F9, KeyCode.CursorDown, KeyCode.CursorDown, KeyCode.CursorDown)]
	[InlineData ("_File", "_New", "", KeyCode.F9, KeyCode.CursorDown, KeyCode.CursorDown, KeyCode.CursorDown, KeyCode.CursorDown)]
	[InlineData ("_File", "_New", "", KeyCode.F9, KeyCode.CursorDown, KeyCode.CursorUp)]
	[InlineData ("_File", "_Quit", "", KeyCode.F9, KeyCode.CursorUp)]
	[InlineData ("_File", "_New", "", KeyCode.F9, KeyCode.CursorUp, KeyCode.CursorDown)]
	[InlineData ("Closed", "None", "Open", KeyCode.F9, KeyCode.CursorDown, KeyCode.Enter)]
	[InlineData ("_Edit", "_Copy", "", KeyCode.F9, KeyCode.CursorRight)]
	[InlineData ("_About", "_About", "", KeyCode.F9, KeyCode.CursorLeft)]
	[InlineData ("_Edit", "_Copy", "", KeyCode.F9, KeyCode.CursorLeft, KeyCode.CursorLeft)]
	[InlineData ("_Edit", "_Select All", "", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorUp)]
	[InlineData ("_File", "_New", "", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorDown, KeyCode.CursorLeft)]
	[InlineData ("_About", "_About", "", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorRight)]
	[InlineData ("Closed", "None", "New", KeyCode.F9, KeyCode.Enter)]
	[InlineData ("Closed", "None", "Quit", KeyCode.F9, KeyCode.CursorUp, KeyCode.Enter)]
	[InlineData ("Closed", "None", "Copy", KeyCode.F9, KeyCode.CursorRight, KeyCode.Enter)]
	[InlineData ("Closed", "None", "Find", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorUp, KeyCode.CursorUp, KeyCode.Enter)]
	[InlineData ("Closed", "None", "Replace", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorUp, KeyCode.CursorUp, KeyCode.CursorDown, KeyCode.Enter)]
	[InlineData ("_Edit", "F_ind", "", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorUp, KeyCode.CursorUp, KeyCode.CursorLeft, KeyCode.Enter)]
	[InlineData ("Closed", "None", "About", KeyCode.F9, KeyCode.CursorRight, KeyCode.CursorRight, KeyCode.Enter)]

	// Hotkeys
	[InlineData ("_File", "_New", "", KeyCode.AltMask | KeyCode.F)]
	[InlineData ("Closed", "None", "", KeyCode.AltMask | KeyCode.ShiftMask | KeyCode.F)]
	[InlineData ("Closed", "None", "", KeyCode.AltMask | KeyCode.F, KeyCode.Esc)]
	[InlineData ("Closed", "None", "", KeyCode.AltMask | KeyCode.F, KeyCode.AltMask | KeyCode.F)]
	[InlineData ("Closed", "None", "Open", KeyCode.AltMask | KeyCode.F, KeyCode.O)]
	[InlineData ("_File", "_New", "", KeyCode.AltMask | KeyCode.F, KeyCode.ShiftMask | KeyCode.O)]
	[InlineData ("Closed", "None", "Open", KeyCode.AltMask | KeyCode.F, KeyCode.AltMask | KeyCode.O)]
	[InlineData ("_Edit", "_Copy", "", KeyCode.AltMask | KeyCode.E)]
	[InlineData ("_Edit", "F_ind", "", KeyCode.AltMask | KeyCode.E, KeyCode.F)]
	[InlineData ("_Edit", "F_ind", "", KeyCode.AltMask | KeyCode.E, KeyCode.AltMask | KeyCode.F)]
	[InlineData ("Closed", "None", "Replace", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.R)]
	[InlineData ("Closed", "None", "", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.C)] // BUGBUG: Action should be "Copy"
	[InlineData ("_Edit", "_1st", "", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3)]
	[InlineData ("Closed", "None", "1", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3, KeyCode.D1)]
	[InlineData ("Closed", "None", "1", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3, KeyCode.Enter)]
	[InlineData ("Closed", "None", "", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D3, KeyCode.D4)] // BUGBUG: Should be back on _3rd Level
	[InlineData ("Closed", "None", "5", KeyCode.AltMask | KeyCode.E, KeyCode.F, KeyCode.D4, KeyCode.D5)]
	[InlineData ("_About", "_About", "", KeyCode.AltMask | KeyCode.A)]
	public void KeyBindings_Navigation_Commands (string expectedBarTitle, string expectedItemTitle, string expectedAction, params KeyCode [] keys)
	{
		string miAction = "";
		MenuItem mbiCurrent = null;
		MenuItem miCurrent = null;

		var menu = MenuBarScenario.CreateTestMenu ((s) => {
			miAction = s;
			return true;
		});
		menu.Key = KeyCode.F9;
		menu.MenuOpening += (s, e) => mbiCurrent = e.CurrentMenu;
		menu.MenuOpened += (s, e) => {
			miCurrent = e.MenuItem;
		};
		menu.MenuClosing += (s, e) => {
			mbiCurrent = null;
			miCurrent = null;
		};
		menu.UseKeysUpDownAsKeysLeftRight = true;
		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		foreach (var key in keys) {
			Application.Top.NewKeyDownEvent (new Key (key));
			Application.MainLoop.RunIteration ();
		}
		Assert.Equal (expectedBarTitle, mbiCurrent != null ? mbiCurrent.Title : "Closed");
		Assert.Equal (expectedItemTitle, miCurrent != null ? miCurrent.Title : "None");
		Assert.Equal (expectedAction, miAction);
	}

	[Theory]
	[AutoInitShutdown]
	[InlineData ("New", KeyCode.CtrlMask | KeyCode.N)]
	[InlineData ("Quit", KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Q)]
	[InlineData ("Copy", KeyCode.CtrlMask | KeyCode.C)]
	[InlineData ("Replace", KeyCode.CtrlMask | KeyCode.H)]
	[InlineData ("1", KeyCode.F1)]
	[InlineData ("5", KeyCode.CtrlMask | KeyCode.D5)]
	public void KeyBindings_Shortcut_Commands (string expectedAction, params KeyCode [] keys)
	{
		string miAction = "";
		MenuItem mbiCurrent = null;
		MenuItem miCurrent = null;

		var menu = MenuBarScenario.CreateTestMenu ((s) => {
			miAction = s;
			return true;
		});
		menu.Key = KeyCode.F9;
		menu.MenuOpening += (s, e) => mbiCurrent = e.CurrentMenu;
		menu.MenuOpened += (s, e) => {
			miCurrent = e.MenuItem;
		};
		menu.MenuClosing += (s, e) => {
			mbiCurrent = null;
			miCurrent = null;
		};
		menu.UseKeysUpDownAsKeysLeftRight = true;

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		foreach (var key in keys) {
			Assert.True (Application.Top.NewKeyDownEvent (new Key (key)));
			Application.MainLoop.RunIteration ();
		}
		Assert.Equal (expectedAction, miAction);
	}

	[Fact]
	[AutoInitShutdown]
	public void ShortCut_Activates ()
	{
		bool saveAction = false;
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_Save", "Saves the file.", () => { saveAction = true; }, null, null, KeyCode.S | KeyCode.CtrlMask)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Application.OnKeyDown (new Key (KeyCode.S | KeyCode.CtrlMask));
		Application.MainLoop.RunIteration ();

		Assert.True (saveAction);

	}

	[Fact]
	[AutoInitShutdown]
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

		string expected = @"
┌──────┐
│ One  │
│ Two  │
└──────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 1, 8, 4), pos);
	}

	[Fact]
	[AutoInitShutdown]
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

		string expected = @"
──────┐
 One  │
 Two  │
──────┘
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 7, 4), pos);

		menu.CloseAllMenus ();
		menu.Frame = new Rect (-1, -2, menu.Frame.Width, menu.Frame.Height);
		menu.OpenMenu ();
		Application.Refresh ();

		expected = @"
 One  │
 Two  │
──────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 7, 3), pos);

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

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 1, 7, 4), pos);

		menu.CloseAllMenus ();
		menu.Frame = new Rect (0, 0, menu.Frame.Width, menu.Frame.Height);
		((FakeDriver)Application.Driver).SetBufferSize (7, 3);
		menu.OpenMenu ();
		Application.Refresh ();

		expected = @"
┌──────
│ One  
│ Two  
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (0, 0, 7, 3), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void UseSubMenusSingleFrame_False_By_Keyboard ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Numbers", new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuBarItem ("Two", new MenuItem [] {
					new MenuItem ("Sub-Menu 1", "", null),
					new MenuItem ("Sub-Menu 2", "", null)
				}),
				new MenuItem ("Three", "", null)
			})
		});
		menu.UseKeysUpDownAsKeysLeftRight = true;
		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
		Assert.False (menu.UseSubMenusSingleFrame);

		Application.Top.Draw ();
		string expected = @"
 Numbers
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Application.Top.Draw ();
		expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.CursorDown)));
		Application.Top.Draw ();
		expected = @"
 Numbers                 
┌────────┐               
│ One    │               
│ Two   ►│┌─────────────┐
│ Three  ││ Sub-Menu 1  │
└────────┘│ Sub-Menu 2  │
          └─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Assert.True (Application.Top.Subviews [2].NewKeyDownEvent (new Key (KeyCode.CursorLeft)));
		Application.Top.Draw ();
		expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.Esc)));
		Application.Top.Draw ();
		expected = @"
 Numbers
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void UseSubMenusSingleFrame_False_By_Mouse ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Numbers", new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuBarItem ("Two", new MenuItem [] {
					new MenuItem ("Sub-Menu 1", "", null),
					new MenuItem ("Sub-Menu 2", "", null)
				}),
				new MenuItem ("Three", "", null)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
		Assert.False (menu.UseSubMenusSingleFrame);

		Application.Top.Draw ();
		string expected = @"
 Numbers
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 1), pos);

		Assert.True (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 10, 6), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 3,
			Flags = MouseFlags.ReportMousePosition,
			View = Application.Top.Subviews [1]
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers                 
┌────────┐               
│ One    │               
│ Two   ►│┌─────────────┐
│ Three  ││ Sub-Menu 1  │
└────────┘│ Sub-Menu 2  │
          └─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 25, 7), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 2,
			Flags = MouseFlags.ReportMousePosition,
			View = Application.Top.Subviews [1]
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 10, 6), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 70,
			Y = 2,
			Flags = MouseFlags.Button1Clicked,
			View = Application.Top
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 1), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void UseSubMenusSingleFrame_True_By_Keyboard ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Numbers", new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuBarItem ("Two", new MenuItem [] {
					new MenuItem ("Sub-Menu 1", "", null),
					new MenuItem ("Sub-Menu 2", "", null)
				}),
				new MenuItem ("Three", "", null)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
		Assert.False (menu.UseSubMenusSingleFrame);
		menu.UseSubMenusSingleFrame = true;
		Assert.True (menu.UseSubMenusSingleFrame);

		Application.Top.Draw ();
		string expected = @"
 Numbers
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 1), pos);

		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Application.Top.Draw ();
		expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 10, 6), pos);

		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.CursorDown)));
		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.Enter)));
		Application.Top.Draw ();
		expected = @"
 Numbers       
┌─────────────┐
│◄    Two     │
├─────────────┤
│ Sub-Menu 1  │
│ Sub-Menu 2  │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 15, 7), pos);

		Assert.True (Application.Top.Subviews [2].NewKeyDownEvent (new Key (KeyCode.Enter)));
		Application.Top.Draw ();
		expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 10, 6), pos);

		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.Esc)));
		Application.Top.Draw ();
		expected = @"
 Numbers
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 1), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void UseSubMenusSingleFrame_True_By_Mouse ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Numbers", new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuBarItem ("Two", new MenuItem [] {
					new MenuItem ("Sub-Menu 1", "", null),
					new MenuItem ("Sub-Menu 2", "", null)
				}),
				new MenuItem ("Three", "", null)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
		Assert.False (menu.UseSubMenusSingleFrame);
		menu.UseSubMenusSingleFrame = true;
		Assert.True (menu.UseSubMenusSingleFrame);

		Application.Top.Draw ();
		string expected = @"
 Numbers
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 1), pos);

		Assert.True (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 10, 6), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 3,
			Flags = MouseFlags.Button1Clicked,
			View = Application.Top.Subviews [1]
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers       
┌─────────────┐
│◄    Two     │
├─────────────┤
│ Sub-Menu 1  │
│ Sub-Menu 2  │
└─────────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 15, 7), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 2,
			Flags = MouseFlags.Button1Clicked,
			View = Application.Top.Subviews [2]
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers  
┌────────┐
│ One    │
│ Two   ►│
│ Three  │
└────────┘
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 10, 6), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 70,
			Y = 2,
			Flags = MouseFlags.Button1Clicked,
			View = Application.Top
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 1), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void HotKey_MenuBar_OnKeyDown_OnKeyUp_ProcessKeyPressed ()
	{
		bool newAction = false;
		bool copyAction = false;

		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "", () => newAction = true)
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", () => copyAction = true)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.False (newAction);
		Assert.False (copyAction);

#if SUPPORT_ALT_TO_ACTIVATE_MENU
		Assert.False (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.AltMask)));
		Assert.False (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.AltMask)));
		Assert.True (Application.Top.ProcessKeyUp (new KeyEventArgs (Key.AltMask)));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();
		string expected = @"
 File  Edit
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		Assert.Equal (new Rect (1, 0, 11, 1), pos);

		Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.N)));
		Application.MainLoop.RunIteration ();
		Assert.False (newAction); // not yet, hot keys don't work if the item is not visible

		Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.F)));
		Application.MainLoop.RunIteration ();
		Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.N)));
		Application.MainLoop.RunIteration ();
		Assert.True (newAction);
		Application.Top.Draw ();
		expected = @"
 File  Edit
";
		
		Assert.False (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.AltMask)));
		Assert.True (Application.Top.ProcessKeyUp (new KeyEventArgs (Key.AltMask)));
		Assert.True (Application.Top.ProcessKeyUp (new KeyEventArgs (Key.AltMask)));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();
		expected = @"
 File  Edit
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, output);
		Assert.Equal (new Rect (1, 0, 11, 1), pos);

		Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.CursorRight)));
		Assert.True (Application.Top.ProcessKeyDown (new KeyEventArgs (Key.C)));
		Application.MainLoop.RunIteration ();
		Assert.True (copyAction);
#endif
	}

	// Defines the expected strings for a Menu. Currently supports 
	//   - MenuBar with any number of MenuItems 
	//   - Each top-level MenuItem can have a SINGLE sub-menu
	//
	// TODO: Enable multiple sub-menus
	// TODO: Enable checked sub-menus
	// TODO: Enable sub-menus with sub-menus (perhaps better to put this in a separate class with focused unit tests?)
	//
	// E.g: 
	//
	// File  Edit
	//  New    Copy
	public class ExpectedMenuBar : MenuBar {
		FakeDriver d = (FakeDriver)Application.Driver;

		// Each MenuBar title has a 1 space pad on each side
		// See `static int leftPadding` and `static int rightPadding` on line 1037 of Menu.cs
		public string MenuBarText {
			get {
				string txt = string.Empty;
				foreach (var m in Menus) {
					txt += " " + m.Title + " ";
				}
				return txt;
			}
		}

		// The expected strings when the menu is closed
		public string ClosedMenuText => MenuBarText + "\n";

		// Padding for the X of the sub menu Frane
		// Menu.cs - Line 1239 in `internal void OpenMenu` is where the Menu is created
		string padding (int i)
		{
			int n = 0;
			while (i > 0) {
				n += Menus [i - 1].TitleLength + 2;
				i--;
			}
			return new string (' ', n);
		}

		// Define expected menu frame
		// "┌──────┐"
		// "│ New  │"
		// "└──────┘"
		// 
		// The width of the Frame is determined in Menu.cs line 144, where `Width` is calculated
		//   1 space before the Title and 2 spaces after the Title/Check/Help
		public string expectedTopRow (int i)
		{
			return $"{CM.Glyphs.ULCorner}{new string (CM.Glyphs.HLine.ToString () [0], Menus [i].Children [0].TitleLength + 3)}{CM.Glyphs.URCorner}  \n";
		}

		// The 3 spaces at end are a result of Menu.cs line 1062 where `pos` is calculated (` + spacesAfterTitle`)
		public string expectedMenuItemRow (int i)
		{
			return $"{CM.Glyphs.VLine} {Menus [i].Children [0].Title}  {CM.Glyphs.VLine}   \n";
		}

		public string expectedBottomRow (int i)
		{
			return $"{CM.Glyphs.LLCorner}{new string (CM.Glyphs.HLine.ToString () [0], Menus [i].Children [0].TitleLength + 3)}{CM.Glyphs.LRCorner}  \n";
		}

		// The fulll expected string for an open sub menu
		public string expectedSubMenuOpen (int i)
		{
			return ClosedMenuText +
				(Menus [i].Children.Length > 0 ?
					padding (i) + expectedTopRow (i) +
					padding (i) + expectedMenuItemRow (i) +
					padding (i) + expectedBottomRow (i)
					:
					"");
		}

		public ExpectedMenuBar (MenuBarItem [] menus) : base (menus) { }
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBar_Submenus_Alignment_Correct ()
	{
		// Define the expected menu
		var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("Really Long Sub Menu", "", null)
			}),
			new MenuBarItem ("123", new MenuItem [] {
				new MenuItem ("Copy", "", null)
			}),
			new MenuBarItem ("Format", new MenuItem [] {
				new MenuItem ("Word Wrap", "", null)
			}),
			new MenuBarItem ("Help", new MenuItem [] {
				new MenuItem ("About", "", null)
			}),
			new MenuBarItem ("1", new MenuItem [] {
				new MenuItem ("2", "", null)
			}),
			new MenuBarItem ("3", new MenuItem [] {
				new MenuItem ("2", "", null)
			}),
			new MenuBarItem ("Last one", new MenuItem [] {
				new MenuItem ("Test", "", null)
			})
		});

		var items = new MenuBarItem [expectedMenu.Menus.Length];
		for (int i = 0; i < expectedMenu.Menus.Length; i++) {
			items [i] = new MenuBarItem (expectedMenu.Menus [i].Title, new MenuItem [] {
				new MenuItem (expectedMenu.Menus [i].Children [0].Title, "", null)
			});
		}
		var menu = new MenuBar (items);

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, _output);

		for (int i = 0; i < expectedMenu.Menus.Length; i++) {
			menu.OpenMenu (i);
			Assert.True (menu.IsMenuOpen);
			Application.Top.Draw ();
			TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (i), _output);
		}
	}

	[Fact]
	[AutoInitShutdown]
	public void HotKey_MenuBar_ProcessKeyPressed_Menu_ProcessKey ()
	{
		bool newAction = false;
		bool copyAction = false;

		// Define the expected menu
		var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("New", "", null)
			}),
			new MenuBarItem ("Edit", new MenuItem [] {
				new MenuItem ("Copy", "", null)
			})
		});

		// The real menu
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_" + expectedMenu.Menus [0].Title, new MenuItem [] {
				new MenuItem ("_" + expectedMenu.Menus [0].Children [0].Title, "", () => newAction = true)
			}),
			new MenuBarItem ("_" + expectedMenu.Menus [1].Title, new MenuItem [] {
				new MenuItem ("_" + expectedMenu.Menus [1].Children [0].Title, "", () => copyAction = true)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.False (newAction);
		Assert.False (copyAction);

		Assert.True (menu.NewKeyDownEvent (new Key (KeyCode.AltMask | KeyCode.F)));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), _output);

		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.N)));
		Application.MainLoop.RunIteration ();
		Assert.True (newAction);

		Assert.True (menu.NewKeyDownEvent (new Key (KeyCode.AltMask | KeyCode.E)));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), _output);

		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.C)));
		Application.MainLoop.RunIteration ();
		Assert.True (copyAction);
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBar_Position_And_Size_With_HotKeys_Is_The_Same_As_Without_HotKeys ()
	{
		// Define the expected menu
		var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("12", "", null)
			}),
			new MenuBarItem ("Edit", new MenuItem [] {
				new MenuItem ("Copy", "", null)
			})
		});

		// Test without HotKeys first
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem (expectedMenu.Menus [0].Title, new MenuItem [] {
				new MenuItem (expectedMenu.Menus [0].Children [0].Title, "", null)
			}),
			new MenuBarItem (expectedMenu.Menus [1].Title, new MenuItem [] {
				new MenuItem (expectedMenu.Menus [1].Children [0].Title, "", null)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		// Open first
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), _output);

		// Open second
		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), _output);

		// Close menu
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.False (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, _output);

		Application.Top.Remove (menu);

		// Now test WITH HotKeys
		menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_" + expectedMenu.Menus [0].Title, new MenuItem [] {
				new MenuItem ("_" + expectedMenu.Menus [0].Children [0].Title, "", null)
			}),
			new MenuBarItem ("_" + expectedMenu.Menus [1].Title, new MenuItem [] {
				new MenuItem ("_" + expectedMenu.Menus [1].Children [0].Title, "", null)
			})
		});

		Application.Top.Add (menu);

		// Open first
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), _output);

		// Open second
		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), _output);

		// Close menu
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.False (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBar_ButtonPressed_Open_The_Menu_ButtonPressed_Again_Close_The_Menu ()
	{
		// Define the expected menu
		var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("Open", "", null)
			}),
			new MenuBarItem ("Edit", new MenuItem [] {
				new MenuItem ("Copy", "", null)
			})
		});

		// Test without HotKeys first
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_" + expectedMenu.Menus [0].Title, new MenuItem [] {
				new MenuItem ("_" + expectedMenu.Menus [0].Children [0].Title, "", null)
			}),
			new MenuBarItem ("_" + expectedMenu.Menus [1].Title, new MenuItem [] {
				new MenuItem ("_" + expectedMenu.Menus [1].Children [0].Title, "", null)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.True (menu.MouseEvent (new MouseEvent () { X = 1, Y = 0, Flags = MouseFlags.Button1Pressed, View = menu }));
		Assert.True (menu.IsMenuOpen);
		Application.Top.Draw ();

		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), _output);

		Assert.True (menu.MouseEvent (new MouseEvent () { X = 1, Y = 0, Flags = MouseFlags.Button1Pressed, View = menu }));
		Assert.False (menu.IsMenuOpen);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, _output);
	}

	[Fact]
	public void UseKeysUpDownAsKeysLeftRight_And_UseSubMenusSingleFrame_Cannot_Be_Both_True ()
	{
		var menu = new MenuBar ();
		Assert.False (menu.UseKeysUpDownAsKeysLeftRight);
		Assert.False (menu.UseSubMenusSingleFrame);

		menu.UseKeysUpDownAsKeysLeftRight = true;
		Assert.True (menu.UseKeysUpDownAsKeysLeftRight);
		Assert.False (menu.UseSubMenusSingleFrame);

		menu.UseSubMenusSingleFrame = true;
		Assert.False (menu.UseKeysUpDownAsKeysLeftRight);
		Assert.True (menu.UseSubMenusSingleFrame);
	}

	[Fact]
	[AutoInitShutdown]
	public void Parent_MenuItem_Stay_Focused_If_Child_MenuItem_Is_Empty_By_Mouse ()
	{
		// File  Edit  Format
		//┌──────┐    ┌───────┐         
		//│ New  │    │ Wrap  │         
		//└──────┘    └───────┘         

		// Define the expected menu
		var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("New", "", null)
			}),
			new MenuBarItem ("Edit", new MenuItem [] { }),
			new MenuBarItem ("Format", new MenuItem [] {
				new MenuItem ("Wrap", "", null)
			})
		});

		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem (expectedMenu.Menus [0].Title, new MenuItem [] {
				new MenuItem (expectedMenu.Menus [0].Children [0].Title, "", null)
			}),
			new MenuBarItem (expectedMenu.Menus [1].Title, new MenuItem [] { }),
			new MenuBarItem (expectedMenu.Menus [2].Title, new MenuItem [] {
				new MenuItem (expectedMenu.Menus [2].Children [0].Title, "", null)
			})
		});

		var tf = new TextField () { Y = 2, Width = 10 };
		Application.Top.Add (menu, tf);
		Application.Begin (Application.Top);

		Assert.True (tf.HasFocus);
		Assert.True (menu.MouseEvent (new MouseEvent () { X = 1, Y = 0, Flags = MouseFlags.Button1Pressed, View = menu }));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), _output);

		Assert.True (menu.MouseEvent (new MouseEvent () { X = 8, Y = 0, Flags = MouseFlags.ReportMousePosition, View = menu }));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), _output);

		Assert.True (menu.MouseEvent (new MouseEvent () { X = 15, Y = 0, Flags = MouseFlags.ReportMousePosition, View = menu }));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (2), _output);

		Assert.True (menu.MouseEvent (new MouseEvent () { X = 8, Y = 0, Flags = MouseFlags.ReportMousePosition, View = menu }));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, _output);

		Assert.True (menu.MouseEvent (new MouseEvent () { X = 1, Y = 0, Flags = MouseFlags.ReportMousePosition, View = menu }));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), _output);

		Assert.True (menu.MouseEvent (new MouseEvent () { X = 8, Y = 0, Flags = MouseFlags.Button1Pressed, View = menu }));
		Assert.False (menu.IsMenuOpen);
		Assert.True (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Parent_MenuItem_Stay_Focused_If_Child_MenuItem_Is_Empty_By_Keyboard ()
	{
		var expectedMenu = new ExpectedMenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("New", "", null)
			}),
			new MenuBarItem ("Edit", Array.Empty<MenuItem> ()),
			new MenuBarItem ("Format", new MenuItem [] {
				new MenuItem ("Wrap", "", null)
			})
		});

		var items = new MenuBarItem [expectedMenu.Menus.Length];
		for (int i = 0; i < expectedMenu.Menus.Length; i++) {
			items [i] = new MenuBarItem (expectedMenu.Menus [i].Title, expectedMenu.Menus [i].Children.Length > 0
				? new MenuItem [] {
					new MenuItem (expectedMenu.Menus [i].Children [0].Title, "", null)
				}
				: Array.Empty<MenuItem> ());
		}
		var menu = new MenuBar (items);

		var tf = new TextField () { Y = 2, Width = 10 };
		Application.Top.Add (menu, tf);

		Application.Begin (Application.Top);
		Assert.True (tf.HasFocus);
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), _output);

		// Right - Edit has no sub menu; this tests that no sub menu shows
		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), _output);

		// Right - Format
		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (2), _output);

		// Left - Edit
		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorLeft)));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (1), _output);

		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorLeft)));
		Assert.True (menu.IsMenuOpen);
		Assert.False (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.expectedSubMenuOpen (0), _output);

		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.False (menu.IsMenuOpen);
		Assert.True (tf.HasFocus);
		Application.Top.Draw ();
		TestHelpers.AssertDriverContentsAre (expectedMenu.ClosedMenuText, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Key_Open_And_Close_The_MenuBar ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("New", "", null)
			})
		});
		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.True (Application.Top.NewKeyDownEvent (menu.Key));
		Assert.True (menu.IsMenuOpen);
		Assert.True (Application.Top.NewKeyDownEvent (menu.Key));
		Assert.False (menu.IsMenuOpen);

		menu.Key = KeyCode.F10 | KeyCode.ShiftMask;
		Assert.False (Application.Top.NewKeyDownEvent (new Key (KeyCode.F9)));
		Assert.False (menu.IsMenuOpen);

		Assert.True (Application.Top.NewKeyDownEvent (new Key (KeyCode.F10 | KeyCode.ShiftMask)));
		Assert.True (menu.IsMenuOpen);
		Assert.True (Application.Top.NewKeyDownEvent (new Key (KeyCode.F10 | KeyCode.ShiftMask)));
		Assert.False (menu.IsMenuOpen);
	}

	[Fact]
	[AutoInitShutdown]
	public void Disabled_MenuItem_Is_Never_Selected ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Menu", new MenuItem [] {
				new MenuItem ("Enabled 1", "", null),
				new MenuItem ("Disabled", "", null, () => false),
				null,
				new MenuItem ("Enabled 2", "", null)
			})
		});

		var top = Application.Top;
		top.Add (menu);
		Application.Begin (top);

		var attributes = new Attribute [] {
			// 0
			menu.ColorScheme.Normal,
			// 1
			menu.ColorScheme.Focus,
			// 2
			menu.ColorScheme.Disabled
		};

		TestHelpers.AssertDriverAttributesAre (@"
00000000000000", Application.Driver, attributes);

		Assert.True (menu.MouseEvent (new MouseEvent {
			X = 0,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		top.Draw ();
		TestHelpers.AssertDriverAttributesAre (@"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000", Application.Driver, attributes);

		Assert.True (top.Subviews [1].MouseEvent (new MouseEvent {
			X = 0,
			Y = 2,
			Flags = MouseFlags.Button1Clicked,
			View = top.Subviews [1]
		}));
		top.Subviews [1].Draw ();
		TestHelpers.AssertDriverAttributesAre (@"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000", Application.Driver, attributes);

		Assert.True (top.Subviews [1].MouseEvent (new MouseEvent {
			X = 0,
			Y = 2,
			Flags = MouseFlags.ReportMousePosition,
			View = top.Subviews [1]
		}));
		top.Subviews [1].Draw ();
		TestHelpers.AssertDriverAttributesAre (@"
11111100000000
00000000000000
01111111111110
02222222222220
00000000000000
00000000000000
00000000000000", Application.Driver, attributes);
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBar_With_Action_But_Without_MenuItems_Not_Throw ()
	{
		var menu = new MenuBar (
			menus: new [] {
				new MenuBarItem { Title = "Test 1", Action = () => { } },
				new MenuBarItem { Title = "Test 2", Action = () => { } }
			});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

#if SUPPORT_ALT_TO_ACTIVATE_MENU
		Assert.True (Application.OnKeyUp (new KeyEventArgs (Key.AltMask))); // changed to true because Alt activates menu bar
#endif
		Assert.True (menu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		Assert.True (menu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBar_In_Window_Without_Other_Views_With_Top_Init_With_Parameterless_Run ()
	{
		var win = new Window ();
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("New", "", null)
			}),
			new MenuBarItem ("Edit", new MenuItem [] {
				new MenuBarItem ("Delete", new MenuItem [] {
					new MenuItem ("All", "", null),
					new MenuItem ("Selected", "", null)
				})
			})
		});
		win.Add (menu);
		var top = Application.Top;
		top.Add (win);

		Application.Iteration += (s, a) => {
			((FakeDriver)Application.Driver).SetBufferSize (40, 8);

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

			Assert.True (win.NewKeyDownEvent (menu.Key));
			top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

			Assert.True (menu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

			Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
			top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘", _output);

			Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
			top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

			Application.RequestStop ();
		};

		Application.Run ();
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBar_In_Window_Without_Other_Views_With_Top_Init ()
	{
		var win = new Window ();
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("New", "", null)
			}),
			new MenuBarItem ("Edit", new MenuItem [] {
				new MenuBarItem ("Delete", new MenuItem [] {
					new MenuItem ("All", "", null),
					new MenuItem ("Selected", "", null)
				})
			})
		});
		win.Add (menu);
		var top = Application.Top;
		top.Add (win);
		Application.Begin (top);
		((FakeDriver)Application.Driver).SetBufferSize (40, 8);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		Assert.True (win.NewKeyDownEvent (menu.Key));
		top.Draw ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		Assert.True (menu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		Application.Refresh ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		top.Draw ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘", _output);

		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		top.Draw ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBar_In_Window_Without_Other_Views_Without_Top_Init ()
	{
		var win = new Window ();
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("New", "", null)
			}),
			new MenuBarItem ("Edit", new MenuItem [] {
				new MenuBarItem ("Delete", new MenuItem [] {
					new MenuItem ("All", "", null),
					new MenuItem ("Selected", "", null)
				})
			})
		});
		win.Add (menu);
		((FakeDriver)Application.Driver).SetBufferSize (40, 8);
		Application.Begin (win);

		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		Assert.True (win.NewKeyDownEvent (menu.Key));
		win.Draw ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		Assert.True (menu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		Application.Refresh ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		win.Draw ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘", _output);

		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
		win.Draw ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBar_In_Window_Without_Other_Views_Without_Top_Init_With_Run_T ()
	{
		((FakeDriver)Application.Driver).SetBufferSize (40, 8);

		Application.Iteration += (s, a) => {
			var top = Application.Top;

			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

			Assert.True (top.NewKeyDownEvent (new Key (KeyCode.F9)));
			top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

			Assert.True (top.Subviews [0].NewKeyDownEvent (new Key (KeyCode.CursorRight)));
			Application.Refresh ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│                     │
│      └─────────┘                     │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

			Assert.True (((MenuBar)top.Subviews [0])._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
			top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│      ┌─────────┐                     │
│      │ Delete ►│┌───────────┐        │
│      └─────────┘│ All       │        │
│                 │ Selected  │        │
│                 └───────────┘        │
└──────────────────────────────────────┘", _output);

			Assert.True (((MenuBar)top.Subviews [0])._openMenu.NewKeyDownEvent (new Key (KeyCode.CursorRight)));
			top.Draw ();
			TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│ File  Edit                           │
│┌──────┐                              │
││ New  │                              │
│└──────┘                              │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

			Application.RequestStop ();
		};

		Application.Run<CustomWindow> ();
	}

	class CustomWindow : Window {
		public CustomWindow ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("File", new MenuItem [] {
					new MenuItem ("New", "", null)
				}),
				new MenuBarItem ("Edit", new MenuItem [] {
					new MenuBarItem ("Delete", new MenuItem [] {
						new MenuItem ("All", "", null),
						new MenuItem ("Selected", "", null)
					})
				})
			});
			Add (menu);
		}
	}

	[Fact]
	[AutoInitShutdown]
	public void AllowNullChecked_Get_Set ()
	{
		var mi = new MenuItem ("Check this out 你", "", null) {
			CheckType = MenuItemCheckStyle.Checked
		};
		mi.Action = mi.ToggleChecked;
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Nullable Checked", new MenuItem [] {
				mi
			})
		});
		new CheckBox ();
		var top = Application.Top;
		top.Add (menu);
		Application.Begin (top);

		Assert.False (mi.Checked);
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.Enter)));
		Application.MainLoop.RunIteration ();
		Assert.True (mi.Checked);
		Assert.True (menu.MouseEvent (new MouseEvent () {
			X = 0,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		Assert.True (menu._openMenu.MouseEvent (new MouseEvent () {
			X = 0,
			Y = 1,
			Flags = MouseFlags.Button1Clicked,
			View = menu._openMenu
		}));
		Application.MainLoop.RunIteration ();
		Assert.False (mi.Checked);

		mi.AllowNullChecked = true;
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.Enter)));
		Application.MainLoop.RunIteration ();
		Assert.Null (mi.Checked);
		Assert.True (menu.MouseEvent (new MouseEvent () {
			X = 0,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		Application.Refresh ();
		TestHelpers.AssertDriverContentsWithFrameAre (@$"
 Nullable Checked       
┌──────────────────────┐
│ {CM.Glyphs.NullChecked} Check this out 你  │
└──────────────────────┘", _output);
		Assert.True (menu._openMenu.MouseEvent (new MouseEvent () {
			X = 0,
			Y = 1,
			Flags = MouseFlags.Button1Clicked,
			View = menu._openMenu
		}));
		Application.MainLoop.RunIteration ();
		Assert.True (mi.Checked);
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu._openMenu.NewKeyDownEvent (new Key (KeyCode.Enter)));
		Application.MainLoop.RunIteration ();
		Assert.False (mi.Checked);
		Assert.True (menu.MouseEvent (new MouseEvent () {
			X = 0,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		Assert.True (menu._openMenu.MouseEvent (new MouseEvent () {
			X = 0,
			Y = 1,
			Flags = MouseFlags.Button1Clicked,
			View = menu._openMenu
		}));
		Application.MainLoop.RunIteration ();
		Assert.Null (mi.Checked);

		mi.AllowNullChecked = false;
		Assert.False (mi.Checked);

		mi.CheckType = MenuItemCheckStyle.NoCheck;
		Assert.Throws<InvalidOperationException> (mi.ToggleChecked);

		mi.CheckType = MenuItemCheckStyle.Radio;
		Assert.Throws<InvalidOperationException> (mi.ToggleChecked);
	}

	[Fact]
	[AutoInitShutdown]
	public void Menu_With_Separator ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("_Open", "Open a file", () => { }, null, null, KeyCode.CtrlMask | KeyCode.O),
				null,
				new MenuItem ("_Quit", "", null)
			})
		});

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		menu.OpenMenu ();
		Application.Refresh ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
 File                         
┌────────────────────────────┐
│ Open   Open a file  Ctrl+O │
├────────────────────────────┤
│ Quit                       │
└────────────────────────────┘", _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Menu_With_Separator_Disabled_Border ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("_Open", "Open a file", () => { }, null, null, KeyCode.CtrlMask | KeyCode.O),
				null,
				new MenuItem ("_Quit", "", null)
			})
		}) { MenusBorderStyle = LineStyle.None };

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		menu.OpenMenu ();
		Application.Refresh ();
		TestHelpers.AssertDriverContentsWithFrameAre (@"
 File                       
 Open   Open a file  Ctrl+O 
────────────────────────────
 Quit                       ", _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void DrawFrame_With_Positive_Positions_Disabled_Border ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem (new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuItem ("Two", "", null)
			})
		}) { MenusBorderStyle = LineStyle.None };

		Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));

		menu.OpenMenu ();
		Application.Begin (Application.Top);

		string expected = @"
 One
 Two
";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void DrawFrame_With_Negative_Positions_Disabled_Border ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem (new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuItem ("Two", "", null)
			})
		}) {
			X = -2,
			Y = -1,
			MenusBorderStyle = LineStyle.None
		};

		Assert.Equal (new Point (-2, -1), new Point (menu.Frame.X, menu.Frame.Y));

		menu.OpenMenu ();
		Application.Begin (Application.Top);

		string expected = @"
ne
wo
";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		menu.CloseAllMenus ();
		menu.Frame = new Rect (-2, -2, menu.Frame.Width, menu.Frame.Height);
		menu.OpenMenu ();
		Application.Refresh ();

		expected = @"
wo
";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		menu.CloseAllMenus ();
		menu.Frame = new Rect (0, 0, menu.Frame.Width, menu.Frame.Height);
		((FakeDriver)Application.Driver).SetBufferSize (3, 2);
		menu.OpenMenu ();
		Application.Refresh ();

		expected = @"
 On
 Tw
";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		menu.CloseAllMenus ();
		menu.Frame = new Rect (0, 0, menu.Frame.Width, menu.Frame.Height);
		((FakeDriver)Application.Driver).SetBufferSize (3, 1);
		menu.OpenMenu ();
		Application.Refresh ();

		expected = @"
 On
";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void UseSubMenusSingleFrame_False_Disabled_Border ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Numbers", new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuBarItem ("Two", new MenuItem [] {
					new MenuItem ("Sub-Menu 1", "", null),
					new MenuItem ("Sub-Menu 2", "", null)
				}),
				new MenuItem ("Three", "", null)
			})
		}) { MenusBorderStyle = LineStyle.None };

		menu.UseKeysUpDownAsKeysLeftRight = true;
		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
		Assert.False (menu.UseSubMenusSingleFrame);

		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Application.Top.Draw ();
		string expected = @"
 Numbers
 One    
 Two   ►
 Three  ";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.CursorDown)));
		Application.Top.Draw ();
		expected = @"
 Numbers           
 One               
 Two   ► Sub-Menu 1
 Three   Sub-Menu 2";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void UseSubMenusSingleFrame_True_Disabled_Border ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Numbers", new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuBarItem ("Two", new MenuItem [] {
					new MenuItem ("Sub-Menu 1", "", null),
					new MenuItem ("Sub-Menu 2", "", null)
				}),
				new MenuItem ("Three", "", null)
			})
		}) { MenusBorderStyle = LineStyle.None };

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
		Assert.False (menu.UseSubMenusSingleFrame);
		menu.UseSubMenusSingleFrame = true;
		Assert.True (menu.UseSubMenusSingleFrame);


		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Application.Top.Draw ();
		string expected = @"
 Numbers
 One    
 Two   ►
 Three  ";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);

		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.CursorDown)));
		Assert.True (Application.Top.Subviews [1].NewKeyDownEvent (new Key (KeyCode.Enter)));
		Application.Top.Draw ();
		expected = @"
 Numbers     
◄    Two     
─────────────
 Sub-Menu 1  
 Sub-Menu 2  ";

		_ = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
	}

	[Fact]
	[AutoInitShutdown]
	public void Draw_A_Menu_Over_A_Dialog ()
	{
		var top = Application.Top;
		var win = new Window ();
		top.Add (win);
		Application.Begin (top);
		((FakeDriver)Application.Driver).SetBufferSize (40, 15);

		Assert.Equal (new Rect (0, 0, 40, 15), win.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		var items = new List<string> { "New", "Open", "Close", "Save", "Save As", "Delete" };
		var dialog = new Dialog () { X = 2, Y = 2, Width = 15, Height = 4 };
		var menu = new MenuBar () { X = Pos.Center (), Width = 10 };
		menu.Menus = new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem (items [0], "Create a new file", () => ChangeMenuTitle ("New"), null, null, KeyCode.CtrlMask | KeyCode.N),
				new MenuItem (items [1], "Open a file", () => ChangeMenuTitle ("Open"), null, null, KeyCode.CtrlMask | KeyCode.O),
				new MenuItem (items [2], "Close a file", () => ChangeMenuTitle ("Close"), null, null, KeyCode.CtrlMask | KeyCode.C),
				new MenuItem (items [3], "Save a file", () => ChangeMenuTitle ("Save"), null, null, KeyCode.CtrlMask | KeyCode.S),
				new MenuItem (items [4], "Save a file as", () => ChangeMenuTitle ("Save As"), null, null, KeyCode.CtrlMask | KeyCode.A),
				new MenuItem (items [5], "Delete a file", () => ChangeMenuTitle ("Delete"), null, null, KeyCode.CtrlMask | KeyCode.A)
			})
		};
		dialog.Add (menu);

		void ChangeMenuTitle (string title)
		{
			menu.Menus [0].Title = title;
			menu.SetNeedsDisplay ();
		}

		var rs = Application.Begin (dialog);

		Assert.Equal (new Rect (2, 2, 15, 4), dialog.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│                                      │
│ ┌─────────────┐                      │
│ │  File       │                      │
│ │             │                      │
│ └─────────────┘                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		Assert.Equal ("File", menu.Menus [0].Title);
		menu.OpenMenu ();
		bool firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│                                      │
│ ┌─────────────┐                      │
│ │  File       │                      │
│ │ ┌──────────────────────────────────┐
│ └─│ New    Create a new file  Ctrl+N │
│   │ Open         Open a file  Ctrl+O │
│   │ Close       Close a file  Ctrl+C │
│   │ Save         Save a file  Ctrl+S │
│   │ Save As   Save a file as  Ctrl+A │
│   │ Delete     Delete a file  Ctrl+A │
│   └──────────────────────────────────┘
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
			X = 20,
			Y = 4,
			Flags = MouseFlags.Button1Clicked
		}));

		firstIteration = false;
		// Need to fool MainLoop into thinking it's running
		Application.MainLoop.Running = true;
		Application.RunIteration (ref rs, ref firstIteration);
		Assert.Equal (items [0], menu.Menus [0].Title);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────────────────────────┐
│                                      │
│ ┌─────────────┐                      │
│ │  New        │                      │
│ │             │                      │
│ └─────────────┘                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
│                                      │
└──────────────────────────────────────┘", _output);

		for (int i = 1; i < items.Count; i++) {
			menu.OpenMenu ();

			Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
				X = 20,
				Y = 4 + i,
				Flags = MouseFlags.Button1Clicked
			}));

			firstIteration = false;
			Application.RunIteration (ref rs, ref firstIteration);
			Assert.Equal (items [i], menu.Menus [0].Title);
		}

		((FakeDriver)Application.Driver).SetBufferSize (20, 15);
		menu.OpenMenu ();
		firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
┌──────────────────┐
│                  │
│ ┌─────────────┐  │
│ │  Delete     │  │
│ │ ┌───────────────
│ └─│ New    Create 
│   │ Open         O
│   │ Close       Cl
│   │ Save         S
│   │ Save As   Save
│   │ Delete     Del
│   └───────────────
│                  │
│                  │
└──────────────────┘", _output);

		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void Draw_A_Menu_Over_A_Top_Dialog ()
	{
		((FakeDriver)Application.Driver).SetBufferSize (40, 15);

		Assert.Equal (new Rect (0, 0, 40, 15), Application.Driver.Clip);
		TestHelpers.AssertDriverContentsWithFrameAre (@"", _output);

		var items = new List<string> { "New", "Open", "Close", "Save", "Save As", "Delete" };
		var dialog = new Dialog () { X = 2, Y = 2, Width = 15, Height = 4 };
		var menu = new MenuBar () { X = Pos.Center (), Width = 10 };
		menu.Menus = new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem (items [0], "Create a new file", () => ChangeMenuTitle ("New"), null, null, KeyCode.CtrlMask | KeyCode.N),
				new MenuItem (items [1], "Open a file", () => ChangeMenuTitle ("Open"), null, null, KeyCode.CtrlMask | KeyCode.O),
				new MenuItem (items [2], "Close a file", () => ChangeMenuTitle ("Close"), null, null, KeyCode.CtrlMask | KeyCode.C),
				new MenuItem (items [3], "Save a file", () => ChangeMenuTitle ("Save"), null, null, KeyCode.CtrlMask | KeyCode.S),
				new MenuItem (items [4], "Save a file as", () => ChangeMenuTitle ("Save As"), null, null, KeyCode.CtrlMask | KeyCode.A),
				new MenuItem (items [5], "Delete a file", () => ChangeMenuTitle ("Delete"), null, null, KeyCode.CtrlMask | KeyCode.A)
			})
		};
		dialog.Add (menu);

		void ChangeMenuTitle (string title)
		{
			menu.Menus [0].Title = title;
			menu.SetNeedsDisplay ();
		}

		var rs = Application.Begin (dialog);

		Assert.Equal (new Rect (2, 2, 15, 4), dialog.Frame);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
  ┌─────────────┐
  │  File       │
  │             │
  └─────────────┘", _output);

		Assert.Equal ("File", menu.Menus [0].Title);
		menu.OpenMenu ();
		bool firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
  ┌─────────────┐                       
  │  File       │                       
  │ ┌──────────────────────────────────┐
  └─│ New    Create a new file  Ctrl+N │
    │ Open         Open a file  Ctrl+O │
    │ Close       Close a file  Ctrl+C │
    │ Save         Save a file  Ctrl+S │
    │ Save As   Save a file as  Ctrl+A │
    │ Delete     Delete a file  Ctrl+A │
    └──────────────────────────────────┘", _output);

		Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
			X = 20,
			Y = 5,
			Flags = MouseFlags.Button1Clicked
		}));

		firstIteration = false;
		// Need to fool MainLoop into thinking it's running
		Application.MainLoop.Running = true;
		Application.RunIteration (ref rs, ref firstIteration);
		Assert.Equal (items [0], menu.Menus [0].Title);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
  ┌─────────────┐
  │  New        │
  │             │
  └─────────────┘", _output);

		for (int i = 1; i < items.Count; i++) {
			menu.OpenMenu ();

			Application.OnMouseEvent (new MouseEventEventArgs (new MouseEvent () {
				X = 20,
				Y = 5 + i,
				Flags = MouseFlags.Button1Clicked
			}));

			firstIteration = false;
			Application.RunIteration (ref rs, ref firstIteration);
			Assert.Equal (items [i], menu.Menus [0].Title);
		}

		((FakeDriver)Application.Driver).SetBufferSize (20, 15);
		menu.OpenMenu ();
		firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
  ┌─────────────┐   
  │  Delete     │   
  │ ┌───────────────
  └─│ New    Create 
    │ Open         O
    │ Close       Cl
    │ Save         S
    │ Save As   Save
    │ Delete     Del
    └───────────────", _output);

		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void Resizing_Close_Menus ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("Open", "Open a file", () => { }, null, null, KeyCode.CtrlMask | KeyCode.O)
			})
		});
		Application.Top.Add (menu);
		var rs = Application.Begin (Application.Top);

		menu.OpenMenu ();
		bool firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
 File                         
┌────────────────────────────┐
│ Open   Open a file  Ctrl+O │
└────────────────────────────┘", _output);

		((FakeDriver)Application.Driver).SetBufferSize (20, 15);
		firstIteration = false;
		Application.RunIteration (ref rs, ref firstIteration);
		TestHelpers.AssertDriverContentsWithFrameAre (@"
 File", _output);

		Application.End (rs);
	}

	[Fact]
	[AutoInitShutdown]
	public void UseSubMenusSingleFrame_True_Without_Border ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Numbers", new MenuItem [] {
				new MenuItem ("One", "", null),
				new MenuBarItem ("Two", new MenuItem [] {
					new MenuItem ("Sub-Menu 1", "", null),
					new MenuItem ("Sub-Menu 2", "", null)
				}),
				new MenuItem ("Three", "", null)
			})
		}) { UseSubMenusSingleFrame = true, MenusBorderStyle = LineStyle.None };

		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.Equal (Point.Empty, new Point (menu.Frame.X, menu.Frame.Y));
		Assert.True (menu.UseSubMenusSingleFrame);
		Assert.Equal (LineStyle.None, menu.MenusBorderStyle);

		Application.Top.Draw ();
		string expected = @"
 Numbers
";

		var pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 1), pos);

		Assert.True (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 0,
			Flags = MouseFlags.Button1Pressed,
			View = menu
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers
 One    
 Two   ►
 Three  
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 4), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 2,
			Flags = MouseFlags.Button1Clicked,
			View = Application.Top.Subviews [1]
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers     
◄    Two     
─────────────
 Sub-Menu 1  
 Sub-Menu 2  
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 13, 5), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 1,
			Y = 1,
			Flags = MouseFlags.Button1Clicked,
			View = Application.Top.Subviews [2]
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers
 One    
 Two   ►
 Three  
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 4), pos);

		Assert.False (menu.MouseEvent (new MouseEvent () {
			X = 70,
			Y = 2,
			Flags = MouseFlags.Button1Clicked,
			View = Application.Top
		}));
		Application.Top.Draw ();
		expected = @"
 Numbers
";

		pos = TestHelpers.AssertDriverContentsWithFrameAre (expected, _output);
		Assert.Equal (new Rect (1, 0, 8, 1), pos);
	}

	[Fact]
	[AutoInitShutdown]
	public void MenuBarItem_Children_Null_Does_Not_Throw ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("Test", "", null)
		});
		Application.Top.Add (menu);

		var exception = Record.Exception (() => menu.NewKeyDownEvent (new Key (KeyCode.Space)));
		Assert.Null (exception);
	}

	[Fact]
	[AutoInitShutdown]
	public void CanExecute_HotKey ()
	{
		Window win = null;
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "", New, CanExecuteNew),
				new MenuItem ("_Close", "", Close, CanExecuteClose)
			})
		});
		var top = Application.Top;
		top.Add (menu);

		bool CanExecuteNew ()
		{
			return win == null;
		}

		void New ()
		{
			win = new Window ();
		}

		bool CanExecuteClose ()
		{
			return win != null;
		}

		void Close ()
		{
			win = null;
		}

		Application.Begin (top);

		Assert.Null (win);
		Assert.True (CanExecuteNew ());
		Assert.False (CanExecuteClose ());

		Assert.True (top.NewKeyDownEvent (new Key (KeyCode.F | KeyCode.AltMask)));
		Assert.True (top.NewKeyDownEvent (new Key (KeyCode.N | KeyCode.AltMask)));
		Application.MainLoop.RunIteration ();
		Assert.NotNull (win);
		Assert.False (CanExecuteNew ());
		Assert.True (CanExecuteClose ());
	}

	[Fact]
	[AutoInitShutdown]
	public void Visible_False_Key_Does_Not_Open_And_Close_All_Opened_Menus ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("New", "", null)
			})
		});
		Application.Top.Add (menu);
		Application.Begin (Application.Top);

		Assert.True (menu.Visible);
		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.True (menu.IsMenuOpen);

		menu.Visible = false;
		Assert.False (menu.IsMenuOpen);

		Assert.True (menu.NewKeyDownEvent (menu.Key));
		Assert.False (menu.IsMenuOpen);
	}

	[Fact]
	public void Separators_Does_Not_Throws_Pressing_Menu_Shortcut ()
	{
		var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("File", new MenuItem [] {
				new MenuItem ("_New", "", null),
				null,
				new MenuItem ("_Quit", "", null)
			})
		});

		var exception = Record.Exception (() => Assert.True (menu.NewKeyDownEvent (new Key (KeyCode.AltMask | KeyCode.Q))));
		Assert.Null (exception);
	}

	[Fact]
	public void RemoveAndThenAddMenuBar_ShouldNotChangeWidth ()
	{
		MenuBar menuBar;
		MenuBar menuBar2;

		// TODO: When https: //github.com/gui-cs/Terminal.Gui/issues/3136 is fixed, 
		// TODO: Change this to Window
		var w = new View ();
		menuBar2 = new Terminal.Gui.MenuBar ();
		menuBar = new Terminal.Gui.MenuBar ();
		w.Width = Dim.Fill (0);
		w.Height = Dim.Fill (0);
		w.X = 0;
		w.Y = 0;

		w.Visible = true;
		// TODO: When https: //github.com/gui-cs/Terminal.Gui/issues/3136 is fixed, 
		// TODO: uncomment this.
		//w.Modal = false;
		w.Title = "";
		menuBar.Width = Dim.Fill (0);
		menuBar.Height = 1;
		menuBar.X = 0;
		menuBar.Y = 0;
		menuBar.Visible = true;
		w.Add (menuBar);

		menuBar2.Width = Dim.Fill (0);
		menuBar2.Height = 1;
		menuBar2.X = 0;
		menuBar2.Y = 4;
		menuBar2.Visible = true;
		w.Add (menuBar2);


		var menuBars = w.Subviews.OfType<MenuBar> ().ToArray ();
		Assert.Equal (2, menuBars.Length);

		Assert.Equal (Dim.Fill (0), menuBars [0].Width);
		Assert.Equal (Dim.Fill (0), menuBars [1].Width);

		// Goes wrong here
		w.Remove (menuBar);
		w.Remove (menuBar2);

		w.Add (menuBar);
		w.Add (menuBar2);

		// These assertions fail
		Assert.Equal (Dim.Fill (0), menuBars [0].Width);
		Assert.Equal (Dim.Fill (0), menuBars [1].Width);
	}
}