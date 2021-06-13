
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using static UICatalog.Scenario;


namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Syntax Highlighting", Description: "Text editor with keyword highlighting")]
	[ScenarioCategory ("Controls")]
	class SyntaxHighlighting : Scenario {

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

				var textView = new SqlTextView () {
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill (1),
				};


				Win.Add (textView);

				var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),

			});


				Top.Add (statusBar);
			}


			private void Quit ()
			{
				Application.RequestStop ();
			}

		private class SqlTextView : TextView{



			protected override void ColorNormal (List<System.Rune> line, int idx)
			{
				Driver.SetAttribute (Driver.MakeAttribute (Color.Green, Color.Black));				
			}
		}
	}
}
