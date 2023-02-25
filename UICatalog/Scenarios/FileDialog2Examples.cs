using System;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "FileDialog2", Description: "Demonstrates how to the FileDialog2 class")]
	[ScenarioCategory ("Dialogs")]
	public class FileDialog2Examples : Scenario {
		private CheckBox cbMustExist;

		public override void Setup ()
		{
			var y = 1;
			var x = 1;

			cbMustExist = new CheckBox ("Must Exist") { Checked = true };
			Win.Add (cbMustExist);

			foreach(var multi in new bool [] {false, true }) {
				foreach (OpenDialog.OpenMode openMode in Enum.GetValues (typeof (OpenDialog.OpenMode))) {
					var btn = new Button ($"Select {(multi?"Many": "One")} {openMode}") {
						X = x,
						Y = y
					};
					SetupHandler (btn, openMode, multi);
					y += 2;
					Win.Add (btn);
				}
			}

			y = 1;
			// SubViews[0] is ContentView
			x = Win.Subviews [0].Subviews.OfType<Button> ().Max (b => b.Text.Length + 5);


			foreach (var multi in new bool [] { false, true }) {
				foreach (var strict in new bool [] { false, true }) {
					{
						var btn = new Button ($"Select {(multi ? "Many" : "One")} .csv ({(strict?"Strict":"Recommended")})") {
							X = x,
							Y = y
						};

						SetupHandler (btn, OpenDialog.OpenMode.File, multi, true, strict);
						y += 2;
						Win.Add (btn);
					}
				};
			}
		}

		private void SetupHandler (Button btn, OpenDialog.OpenMode mode, bool isMulti, bool csv = false, bool strict = false)
		{
			btn.Clicked += ()=>{
				var fd = new FileDialog2 {
					AllowsMultipleSelection = isMulti,
					OpenMode = mode,
					MustExist = cbMustExist.Checked
				};

				if (csv) {
					fd.AllowedTypes.Add (new FileDialog2.AllowedType ("Data File", ".csv",".tsv"));
					fd.AllowedTypesIsStrict = strict;
				}

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
						string.Join (Environment.NewLine, fd.MultiSelected.Select (m => m)),
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
