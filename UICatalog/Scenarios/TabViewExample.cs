using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static UICatalog.Scenario;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Tab View", Description: "Demos TabView control")]
	[ScenarioCategory ("Controls")]
	class TabViewExample : Scenario {

		TabView tabView;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Quit()),
				})
				});
			Top.Add (menu);

			tabView = new TabView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};

			var interactiveTab = new View(){
				Width = Dim.Fill(),
				Height = Dim.Fill()
			};
			var lblName = new Label("Name:");
			interactiveTab.Add(lblName);

			var tbName = new TextField(){
				X = Pos.Right(lblName),
				Width = 10};
			interactiveTab.Add(tbName);

			var lblAddr = new Label("Address:")
			{
				Y=1
			};
			interactiveTab.Add(lblAddr);

			var tbAddr = new TextField()
			{
				X = Pos.Right(lblAddr),
				Y=1,
				Width = 10
			};
			interactiveTab.Add(tbAddr);

			tabView.Tabs.Add(new Tab("Tab1",new Label("hodor!")));
			tabView.Tabs.Add(new Tab("Tab2",new Label("durdur")));
			tabView.Tabs.Add(new Tab("Interactive Tab",interactiveTab));

			tabView.SelectedTab = tabView.Tabs[0];

			Win.Add (tabView);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
