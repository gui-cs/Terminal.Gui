using System;
using System.Collections.Generic;
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
			var btnOneFile = new Button ("Select One File") {
				X = 1,
				Y = 1
			};
			btnOneFile.Clicked += BtnOneFile_Clicked;
			Win.Add (btnOneFile);

			var btnManyFiles = new Button ("Select Many Files") {
				X = 1,
				Y = Pos.Bottom(btnOneFile) + 1
			};
			btnManyFiles.Clicked += BtnManyFiles_Clicked;
			Win.Add (btnManyFiles);
		}


		private void BtnOneFile_Clicked ()
		{
			var fd = new FileDialog2 ();
			Application.Run (fd);

			if (fd.Canceled) {
				MessageBox.Query (
					"Dialog Canceled",
					"You canceled file navigation and did not pick anything",
				"Yup!");
			} else {
				MessageBox.Query (
					"File chosen!",
					"You chose " + Environment.NewLine + fd.Path,
					"Oh yeah!");
			}
		}
		private void BtnManyFiles_Clicked ()
		{
			var fd = new FileDialog2 {
				AllowsMultipleSelection = true
			};
			Application.Run (fd);

			if (fd.Canceled) {
				MessageBox.Query (
					"Dialog Canceled",
					"You canceled file navigation and did not pick anything",
				"Yup!");
			} else {
				MessageBox.Query (
					"File chosen!",
					"You chose " + Environment.NewLine + 
					string.Join(Environment.NewLine,fd.MultiSelected.Select(m=>m.FullName)),
					"Oh yeah!");
			}
		}
	}
}
