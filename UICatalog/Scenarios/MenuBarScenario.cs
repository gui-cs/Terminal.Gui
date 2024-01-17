using System;
using Terminal.Gui;

namespace UICatalog.Scenarios; 

[ScenarioMetadata ("MenuBar", "Demonstrates the MenuBar using the same menu used in unit tests.")]
[ScenarioCategory ("Controls")] [ScenarioCategory ("Menu")]
public class MenuBarScenario : Scenario {
	/// <summary>
	/// This method creates at test menu bar. It is called by the MenuBar unit tests so
	/// it's possible to do both unit testing and user-experience testing with the same setup.
	/// </summary>
	/// <param name="actionFn"></param>
	/// <returns></returns>
	public static MenuBar CreateTestMenu (Func<string, bool> actionFn)
	{
		var mb = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_File", new MenuItem [] {
				new MenuItem ("_New", "", () => actionFn ("New"), null, null, KeyCode.CtrlMask | KeyCode.N),
				new MenuItem ("_Open", "", () => actionFn ("Open"), null, null, KeyCode.CtrlMask | KeyCode.O),
				new MenuItem ("_Save", "", () => actionFn ("Save"), null, null, KeyCode.CtrlMask | KeyCode.S),
				null,
				// Don't use Ctrl-Q so we can disambiguate between quitting and closing the toplevel
				new MenuItem ("_Quit", "", () => actionFn ("Quit"), null, null, KeyCode.AltMask | KeyCode.CtrlMask | KeyCode.Q)
			}),
			new MenuBarItem ("_Edit", new MenuItem [] {
				new MenuItem ("_Copy", "", () => actionFn ("Copy"), null, null, KeyCode.CtrlMask | KeyCode.C),
				new MenuItem ("C_ut", "", () => actionFn ("Cut"), null, null, KeyCode.CtrlMask | KeyCode.X),
				new MenuItem ("_Paste", "", () => actionFn ("Paste"), null, null, KeyCode.CtrlMask | KeyCode.V),
				new MenuBarItem ("_Find and Replace", new MenuItem [] {
					new MenuItem ("F_ind", "", () => actionFn ("Find"), null, null, KeyCode.CtrlMask | KeyCode.F),
					new MenuItem ("_Replace", "", () => actionFn ("Replace"), null, null, KeyCode.CtrlMask | KeyCode.H),
					new MenuBarItem ("_3rd Level", new MenuItem [] {
						new MenuItem ("_1st", "", () => actionFn ("1"), null, null, KeyCode.F1),
						new MenuItem ("_2nd", "", () => actionFn ("2"), null, null, KeyCode.F2),
					}),
					new MenuBarItem ("_4th Level", new MenuItem [] {
						new MenuItem ("_5th", "", () => actionFn ("5"), null, null, KeyCode.CtrlMask | KeyCode.D5),
						new MenuItem ("_6th", "", () => actionFn ("6"), null, null, KeyCode.CtrlMask | KeyCode.D6),
					}),
				}),
				new MenuItem ("_Select All", "", () => actionFn ("Select All"), null, null, KeyCode.CtrlMask | KeyCode.ShiftMask | KeyCode.S),
			}),
			new MenuBarItem ("_About", "Top-Level", () => actionFn ("About"), null, null),
		});
		mb.UseKeysUpDownAsKeysLeftRight = true;
		mb.Key = KeyCode.F9;
		mb.Title = "TestMenuBar";
		return mb;
	}

	// Don't create a Window, just return the top-level view
	public override void Init ()
	{
		Application.Init ();
		Application.Top.ColorScheme = Colors.ColorSchemes ["Base"];
	}

	Label _currentMenuBarItem;
	Label _currentMenuItem;
	Label _lastAction;
	Label _focusedView;
	Label _lastKey;

	public override void Setup ()
	{
		MenuItem mbiCurrent = null;
		MenuItem miCurrent = null;

		var label = new Label () {
			X = 0,
			Y = 10,
			Text = "Last Key: "
		};
		Application.Top.Add (label);

		_lastKey = new Label () {
			X = Pos.Right (label),
			Y = Pos.Top (label),
			Text = ""
		};

		Application.Top.Add (_lastKey);
		label = new Label () {
			X = 0,
			Y = Pos.Bottom (label),
			Text = "Current MenuBarItem: "
		};
		Application.Top.Add (label);

		_currentMenuBarItem = new Label () {
			X = Pos.Right(label),
			Y = Pos.Top (label),
			Text = ""
		};
		Application.Top.Add (_currentMenuBarItem);

		label = new Label () {
			X = 0,
			Y = Pos.Bottom(label),
			Text = "Current MenuItem: "
		};
		Application.Top.Add (label);

		_currentMenuItem = new Label () {
			X = Pos.Right (label),
			Y = Pos.Top (label),
			Text = ""
		};
		Application.Top.Add (_currentMenuItem);

		label = new Label () {
			X = 0,
			Y = Pos.Bottom (label),
			Text = "Last Action: "
		};
		Application.Top.Add (label);

		_lastAction = new Label () {
			X = Pos.Right (label),
			Y = Pos.Top (label),
			Text = ""
		};
		Application.Top.Add (_lastAction);
		
		label = new Label () {
			X = 0,
			Y = Pos.Bottom (label),
			Text = "Focused View: "
		};
		Application.Top.Add (label);

		_focusedView = new Label () {
			X = Pos.Right (label),
			Y = Pos.Top (label),
			Text = ""
		};
		Application.Top.Add (_focusedView);

		var menuBar = CreateTestMenu ((s) => {
			_lastAction.Text = s;
			return true;
		});

		menuBar.MenuOpening += (s, e) => {
			mbiCurrent = e.CurrentMenu;
			SetCurrentMenuBarItem (mbiCurrent);
			SetCurrentMenuItem (miCurrent);
			_lastAction.Text = string.Empty;
		};
		menuBar.MenuOpened += (s, e) => {
			miCurrent = e.MenuItem;
			SetCurrentMenuBarItem (mbiCurrent);
			SetCurrentMenuItem (miCurrent);
		};
		menuBar.MenuClosing += (s, e) => {
			mbiCurrent = null;
			miCurrent = null;
			SetCurrentMenuBarItem (mbiCurrent);
			SetCurrentMenuItem (miCurrent);
		};

		Application.KeyDown += (s, e) => {
			_lastAction.Text = string.Empty;
			_lastKey.Text = e.ToString ();
		};
		
		// There's no focus change event, so this is a bit of a hack.
		menuBar.LayoutComplete += (s, e) => {
			_focusedView.Text = Application.Top.MostFocused?.ToString() ?? "None";
		};

		var openBtn = new Button () {
			X = Pos.Center (),
			Y = 4,
			Text = "_Open Menu",
			IsDefault = true
		};
		openBtn.Clicked += (s, e) => {
			menuBar.OpenMenu ();
		};
		Application.Top.Add (openBtn);

		var hideBtn = new Button () {
			X = Pos.Center (),
			Y = Pos.Bottom(openBtn),
			Text = "Toggle Menu._Visible",
		};
		hideBtn.Clicked += (s, e) => {
			menuBar.Visible = !menuBar.Visible;
		};
		Application.Top.Add (hideBtn);

		var enableBtn = new Button () {
			X = Pos.Center (),
			Y = Pos.Bottom (hideBtn),
			Text = "_Toggle Menu.Enable",
		};
		enableBtn.Clicked += (s, e) => {
			menuBar.Enabled = !menuBar.Enabled;
		};
		Application.Top.Add (enableBtn);

		Application.Top.Add (menuBar);
	}

	void SetCurrentMenuBarItem (MenuItem mbi)
	{
		_currentMenuBarItem.Text = mbi != null ? mbi.Title : "Closed";
	}

	void SetCurrentMenuItem (MenuItem mi)
	{
		_currentMenuItem.Text = mi != null ? mi.Title : "None";
	}

}