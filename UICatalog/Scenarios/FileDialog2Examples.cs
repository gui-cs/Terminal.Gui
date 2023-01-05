using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "FileDialog2", Description: "Demonstrates how to the FileDialog2 class")]
	[ScenarioCategory ("Dialogs")]
	public class FileDialog2Examples : Scenario {
		public override void Setup ()
		{
			var y = 1;

			foreach(var multi in new bool [] {false, true }) {
				foreach (OpenDialog.OpenMode openMode in Enum.GetValues (typeof (OpenDialog.OpenMode))) {
					var btn = new Button ($"Select {(multi?"Many": "One")} {openMode}") {
						X = 1,
						Y = y
					};
					SetupHandler (btn, openMode, multi);
					y += 2;
					Win.Add (btn);
				}
			}
		}

		private void SetupHandler (Button btn, OpenDialog.OpenMode mode, bool isMulti)
		{
			btn.Clicked += ()=>{
				var fd = new FileDialog2 {
					AllowsMultipleSelection = isMulti,
					OpenMode = mode,
				};

				Application.Run (fd);

				if (fd.Canceled) {
					MessageBox.Query (
						"Canceled",
						"You canceled navigation and did not pick anything",
					"Ok");
				} else if (isMulti) {
					MessageBox.Query (
						"Chosen!",
						"You chose:" + Environment.NewLine +
						string.Join (Environment.NewLine, fd.MultiSelected.Select (m => m.FullName)),
						"Ok");
				}
				else{
					MessageBox.Query (
						"Chosen!",
						"You chose:" + Environment.NewLine + fd.Path,
						"Ok");
				}
			};
		}
	}
}
