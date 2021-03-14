using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static UICatalog.Scenario;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Notepad", Description: "Multi tab text editor")]
	[ScenarioCategory ("Controls")]
	class Notepad : Scenario {

		TabView tabView;

		public override void Setup ()
		{
			Win.Title = this.GetName ();
			Win.Y = 1; // menu
			Win.Height = Dim.Fill (1); // status bar
			Top.LayoutSubviews ();

			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Open", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					new MenuItem ("_Save As", "", () => SaveAs()),
					new MenuItem ("_Close", "", () => Close()),
					new MenuItem ("_Quit", "", () => Quit()),
				})
				});
			Top.Add (menu);

			tabView = new TabView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (2),
			};

			tabView.Style.ShowBorder = false;
			tabView.Style.ShowHeaderOverline = false;

			Win.Add (tabView);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			});
			Top.Add (statusBar);
		}

		private void Close ()
		{
			
		}

		private void Open ()
		{
			var open = new FileDialog();
			
			Application.Run(open);

			var path = open.FilePath?.ToString();

			if(string.IsNullOrEmpty(path) || !File.Exists(path)){
				return;
			}

			var textView = new TextView(){
				X = 0,
				Y = 0,
				Width = Dim.Fill(),
				Height = Dim.Fill(),
			};

			var filename = Path.GetFileName(path);
			textView.Text = File.ReadAllText(path);

			var tab = new Tab(filename,textView);

			tabView.AddTab(tab);

			// when user makes changes rename tab to indicate that
			textView.TextChanged += ()=> {
				if(!tab.Text.ToString().EndsWith('*')){
					tab.Text.Append((uint)'*');

					tabView.SetNeedsDisplay();
				}
			};


		}

		public void Save()
		{

		}

		public void SaveAs()
		{
			
		}

		private class OpenedFile{

			public string Path {get;set;}
			public Tab Tab {get;set;}
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
