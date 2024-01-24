using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata (Name: "Tab View", Description: "Demos TabView control with limited screen space in Absolute layout.")]
[ScenarioCategory ("Controls"), ScenarioCategory ("TabView")]
public class TabViewExample : Scenario {

	TabView _tabView;

	MenuItem _miShowTopLine;
	MenuItem _miShowBorder;
	MenuItem _miTabsOnBottom;
	MenuItem _miShowTabViewBorder;

	public override void Setup ()
	{
		Win.Title = this.GetName ();
		Win.Y = 1; // menu
		Win.Height = Dim.Fill (1); // status bar

		var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {

					new MenuItem ("_Add Blank Tab", "", () => AddBlankTab()),

					new MenuItem ("_Clear SelectedTab", "", () => _tabView.SelectedTab=null),
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					_miShowTopLine = new MenuItem ("_Show Top Line", "", () => ShowTopLine()){
						Checked = true,
						CheckType = MenuItemCheckStyle.Checked
					},
					_miShowBorder = new MenuItem ("_Show Border", "", () => ShowBorder()){
						Checked = true,
						CheckType = MenuItemCheckStyle.Checked
					},
					_miTabsOnBottom = new MenuItem ("_Tabs On Bottom", "", () => SetTabsOnBottom()){
						Checked = false,
						CheckType = MenuItemCheckStyle.Checked
					},
					_miShowTabViewBorder = new MenuItem ("_Show TabView Border", "", () => ShowTabViewBorder()){
						Checked = true,
						CheckType = MenuItemCheckStyle.Checked
					}

					})
				});
		Application.Top.Add (menu);

		_tabView = new TabView () {
			X = 0,
			Y = 0,
			Width = 60,
			Height = 20,
			BorderStyle = LineStyle.Single
		};

		_tabView.AddTab (new Tab () { DisplayText = "Tab1", View = new Label ("hodor!") }, false);
		_tabView.AddTab (new Tab () { DisplayText = "Tab2", View = new TextField ("durdur") }, false);
		_tabView.AddTab (new Tab () { DisplayText = "Interactive Tab", View = GetInteractiveTab () }, false);
		_tabView.AddTab (new Tab () { DisplayText = "Big Text", View = GetBigTextFileTab () }, false);
		_tabView.AddTab (new Tab () {
			DisplayText = "Long name Tab, I mean seriously long.  Like you would not believe how long this tab's name is its just too much really woooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooooowwww thats long",
			View = new Label ("This tab has a very long name which should be truncated.  See TabView.MaxTabTextWidth")
		}, false);
		_tabView.AddTab (new Tab () { DisplayText = "Les Mise" + '\u0301' + "rables", View = new Label ("This tab name is unicode") }, false);
		_tabView.AddTab (new Tab () { DisplayText = "Les Mise" + '\u0328' + '\u0301' + "rables", View = new Label ("This tab name has two combining marks. Only one will show due to Issue #2616.") }, false);
		for (int i = 0; i < 100; i++) {
			_tabView.AddTab (new Tab () { DisplayText = $"Tab{i}", View = new Label($"Welcome to tab {i}") }, false);
		}

		_tabView.SelectedTab = _tabView.Tabs.First ();

		Win.Add (_tabView);

		var frameRight = new FrameView ("About") {
			X = Pos.Right (_tabView),
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill (),
		};

		frameRight.Add (new TextView () {
			Text = "This demos the tabs control\nSwitch between tabs using cursor keys",
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		});

		Win.Add (frameRight);

		var frameBelow = new FrameView ("Bottom Frame") {
			X = 0,
			Y = Pos.Bottom (_tabView),
			Width = _tabView.Width,
			Height = Dim.Fill (),
		};

		frameBelow.Add (new TextView () {
			Text = "This frame exists to check you can still tab here\nand that the tab control doesn't overspill it's bounds",
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		});

		Win.Add (frameBelow);

		var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});
		Application.Top.Add (statusBar);
	}

	private void AddBlankTab ()
	{
		_tabView.AddTab (new Tab (), false);
	}

	private View GetInteractiveTab ()
	{

		var interactiveTab = new View () {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};
		var lblName = new Label ("Name:");
		interactiveTab.Add (lblName);

		var tbName = new TextField () {
			X = Pos.Right (lblName),
			Width = 10
		};
		interactiveTab.Add (tbName);

		var lblAddr = new Label ("Address:") {
			Y = 1
		};
		interactiveTab.Add (lblAddr);

		var tbAddr = new TextField () {
			X = Pos.Right (lblAddr),
			Y = 1,
			Width = 10
		};
		interactiveTab.Add (tbAddr);

		return interactiveTab;
	}

	private View GetBigTextFileTab ()
	{

		var text = new TextView () {
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};

		var sb = new System.Text.StringBuilder ();

		for (int y = 0; y < 300; y++) {
			for (int x = 0; x < 500; x++) {
				sb.Append ((x + y) % 2 == 0 ? '1' : '0');
			}
			sb.AppendLine ();
		}
		text.Text = sb.ToString ();

		return text;
	}

	private void ShowTopLine ()
	{
		_miShowTopLine.Checked = !_miShowTopLine.Checked;

		_tabView.Style.ShowTopLine = (bool)_miShowTopLine.Checked;
		_tabView.ApplyStyleChanges ();
	}
	private void ShowBorder ()
	{
		_miShowBorder.Checked = !_miShowBorder.Checked;

		_tabView.Style.ShowBorder = (bool)_miShowBorder.Checked;
		_tabView.ApplyStyleChanges ();
	}
	private void SetTabsOnBottom ()
	{
		_miTabsOnBottom.Checked = !_miTabsOnBottom.Checked;

		_tabView.Style.TabsOnBottom = (bool)_miTabsOnBottom.Checked;
		_tabView.ApplyStyleChanges ();
	}

	private void ShowTabViewBorder ()
	{
		_miShowTabViewBorder.Checked = !_miShowTabViewBorder.Checked;

		_tabView.BorderStyle = _miShowTabViewBorder.Checked == true ? _tabView.BorderStyle = LineStyle.Single
			: LineStyle.None;
		_tabView.ApplyStyleChanges ();
	}

	private void Quit ()
	{
		Application.RequestStop ();
	}
}
