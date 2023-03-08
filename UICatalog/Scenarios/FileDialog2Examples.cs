using System;
using System.Collections;
using System.IO;
using System.Linq;
using Terminal.Gui;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "FileDialog2", Description: "Demonstrates how to the FileDialog2 class")]
	[ScenarioCategory ("Dialogs")]
	public class FileDialog2Examples : Scenario {
		private CheckBox cbMustExist;
		private CheckBox cbIcons;
		private CheckBox cbMonochrome;
		private CheckBox cbCaseSensitive;

		private RadioGroup rgCaption;

		public override void Setup ()
		{
			var y = 0;
			var x = 1;

			cbMustExist = new CheckBox ("Must Exist") { Checked = true, X=x};
			Win.Add (cbMustExist);


			cbIcons = new CheckBox ("Icons") { Checked = true, Y = y++, X=x };
			Win.Add (cbIcons);

			cbMonochrome = new CheckBox ("Monochrome") { Checked = false, Y = y++, X=x};
			Win.Add (cbMonochrome);

			cbCaseSensitive = new CheckBox ("Case Sensitive Search") { Checked = false, Y = y++, X=x };
			Win.Add (cbCaseSensitive);

			y = 0;
			x = 24;

			Win.Add(new Label("Open Caption"){X=x++,Y=y++});

			rgCaption = new RadioGroup{X = x, Y=y};
			rgCaption.RadioLabels = new NStack.ustring[]{"Ok","Open","Save"};
			Win.Add(rgCaption);

			
			x = 1;
			y = 5;

			foreach (var multi in new bool [] { false, true }) {
				foreach (OpenDialog.OpenMode openMode in Enum.GetValues (typeof (OpenDialog.OpenMode))) {
					var btn = new Button ($"Select {(multi ? "Many" : "One")} {openMode}") {
						X = x,
						Y = y
					};
					SetupHandler (btn, openMode, multi);
					y += 2;
					Win.Add (btn);
				}
			}

			y = 5;

			// SubViews[0] is ContentView
			x = Win.Subviews [0].Subviews.OfType<Button> ().Max (b => b.Text.Length + 5);

			foreach (var multi in new bool [] { false, true }) {
				foreach (var strict in new bool [] { false, true }) {
					{
						var btn = new Button ($"Select {(multi ? "Many" : "One")} .csv ({(strict ? "Strict" : "Recommended")})") {
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
			btn.Clicked += () => {
				var fd = new FileDialog2(
						rgCaption.RadioLabels[rgCaption.SelectedItem].ToString()
					) {
					AllowsMultipleSelection = isMulti,
					OpenMode = mode,
					MustExist = cbMustExist.Checked ?? false
				};

				if (cbIcons.Checked ?? false) {
					fd.IconGetter = GetIcon;
				}

				if(cbCaseSensitive.Checked ?? false) {

					fd.SearchMatcher = new CaseSensitiveSearchMatcher ();
				}

				fd.Monochrome = cbMonochrome.Checked ?? false;

				if (csv) {
					fd.AllowedTypes.Add (new FileDialog2.AllowedType ("Data File", ".csv", ".tsv"));
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
				} else {
					MessageBox.Query (
						"Chosen!",
						"You chose:" + Environment.NewLine + fd.Path,
						"Ok");
				}
			};
		}

		private class CaseSensitiveSearchMatcher : FileDialog2.ISearchMatcher {
			private string terms;

			public void Initialize (string terms)
			{
				this.terms = terms;
			}

			public bool IsMatch (FileSystemInfo f)
			{
				return f.Name.Contains (terms, StringComparison.CurrentCulture);
			}
		}

		private string GetIcon (FileSystemInfo arg)
		{
			if (arg is DirectoryInfo) {
				return "\ua909";
			}

			return "\u2630";
		}
	}
}
