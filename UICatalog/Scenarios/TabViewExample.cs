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

		MenuItem miShowTopLine;
		MenuItem miShowBorder;
		MenuItem miTabsOnBottom;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Quit", "", () => Quit()),
				}),
				new MenuBarItem ("_View", new MenuItem [] {
					miShowTopLine = new MenuItem ("_Show Top Line", "", () => ShowTopLine()){
						Checked = true,
						CheckType = MenuItemCheckStyle.Checked
					},
					miShowBorder = new MenuItem ("_Show Border", "", () => ShowBorder()){
						Checked = true,
						CheckType = MenuItemCheckStyle.Checked
					},
					miTabsOnBottom = new MenuItem ("_Tabs On Bottom", "", () => SetTabsOnBottom()){
						Checked = false,
						CheckType = MenuItemCheckStyle.Checked
					}
					
					})
				});
			Top.Add (menu);

			tabView = new TabView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};


			tabView.AddTab(new Tab("Tab1",new Label("hodor!")));
			tabView.AddTab(new Tab("Tab2",new Label("durdur")));
			tabView.AddTab(new Tab("Interactive Tab",GetInteractiveTab()));
			tabView.AddTab(new Tab("Big Text",GetBigTextFileTab()));

			for(int i=0;i<100;i++)
			{
				tabView.AddTab(new Tab($"Tab{i}",new Label($"Welcome to tab {i}")));
			}

			tabView.SelectedTab = tabView.Tabs.First();

			Win.Add (tabView);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);
		}

		private View GetInteractiveTab ()
		{
			
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

			return interactiveTab;
		}


		private View GetBigTextFileTab ()
		{
			
			var text = new TextView(){
				Width = Dim.Fill(),
				Height = Dim.Fill()
			};

			var sb = new System.Text.StringBuilder();

			for(int y=0;y<300;y++){
				for(int x=0;x<500;x++)
				{
					sb.Append((x+y)%2 ==0 ? '1':'0');
				}
				sb.AppendLine();
			}
			text.Text = sb.ToString();

			return text;
		}

		private void ShowTopLine()
		{
			miShowTopLine.Checked = !miShowTopLine.Checked;

			tabView.Style.ShowHeaderOverline = miShowTopLine.Checked;
			tabView.ApplyStyleChanges();
		}
		private void ShowBorder ()
		{
			miShowBorder.Checked = !miShowBorder.Checked;

			tabView.Style.ShowBorder = miShowBorder.Checked;
			tabView.ApplyStyleChanges ();
		}
		private void SetTabsOnBottom ()
		{
			miTabsOnBottom.Checked = !miTabsOnBottom.Checked;

			tabView.Style.TabsOnBottom = miTabsOnBottom.Checked;
			tabView.ApplyStyleChanges ();
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
