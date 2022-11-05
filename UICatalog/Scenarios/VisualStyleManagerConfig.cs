using NStack;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Terminal.Gui;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Visual Style Manager", Description: "Configures Visual Styles.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("Colors")]
	public class VisualStyleManagerConfig : Scenario {
		TabView tabView;

		private int numbeOfNewTabs = 1;

		// Don't create a Window, just return the top-level view
		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();
			Application.Top.ColorScheme = Colors.Base;
		}

		public override void Setup ()
		{
			var menu = new MenuBar (new MenuBarItem [] {
				new MenuBarItem ("_File", new MenuItem [] {
					new MenuItem ("_Save", "", () => Save()),
					new MenuItem ("_Quit", "", () => Quit()),
				})
				});
			Application.Top.Add (menu);

			tabView = new TabView () {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};

			tabView.Style.ShowBorder = true;
			tabView.ApplyStyleChanges ();

			Application.Top.Add (tabView);

			var lenStatusItem = new StatusItem (Key.CharMask, "Len: ", null);
			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
				new StatusItem(Key.F5, "~F5~ Reload", () => Reload()),
				new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", () => Save()),
				lenStatusItem,
			});

			tabView.SelectedTabChanged += (s, e) => lenStatusItem.Title = $"Len:{(e.NewTab?.View?.Text?.Length ?? 0)}";

			Application.Top.Add (statusBar);

			New ();
		}

		private void New ()
		{
			Open ("", null, $"new {numbeOfNewTabs++}");
			var tab = tabView.SelectedTab as OpenedFile;
			tab.Text = "~/.tui/visualstyle.json";
			tab.SavedText = JsonSerializer.Serialize (Colors.ColorSchemes, new JsonSerializerOptions () { WriteIndented = true });
		}

		private void Reload ()
		{
			var tab = tabView.SelectedTab as OpenedFile;
			tab.Text = "~/.tui/visualstyle.json";
			tab.SavedText = JsonSerializer.Serialize (Colors.ColorSchemes, new JsonSerializerOptions () { WriteIndented = true });
		}

		private void Close ()
		{
			var tab = tabView.SelectedTab as OpenedFile;

			if (tab == null) {
				return;
			}

			if (tab.UnsavedChanges) {

				int result = MessageBox.Query ("Save Changes", $"Save changes to {tab.Text.ToString ().TrimEnd ('*')}", "Yes", "No", "Cancel");

				if (result == -1 || result == 2) {

					// user cancelled
					return;
				}

				if (result == 0) {
					tab.Save ();
				}
			}

			// close and dispose the tab
			tabView.RemoveTab (tab);
			tab.View.Dispose ();
		}

		private void Open ()
		{
			var open = new OpenDialog ("Open", "Open a file") { AllowsMultipleSelection = true };

			Application.Run (open);

			if (!open.Canceled) {

				foreach (var path in open.FilePaths) {

					if (string.IsNullOrEmpty (path) || !File.Exists (path)) {
						return;
					}

					Open (File.ReadAllText (path), new FileInfo (path), Path.GetFileName (path));
				}
			}
		}

		/// <summary>
		/// Creates a new tab with initial text
		/// </summary>
		/// <param name="initialText"></param>
		/// <param name="fileInfo">File that was read or null if a new blank document</param>
		private void Open (string initialText, FileInfo fileInfo, string tabName)
		{
			var textView = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
				Text = initialText
			};

			var tab = new OpenedFile (tabName, fileInfo, textView);
			tabView.AddTab (tab, true);

			// when user makes changes rename tab to indicate unsaved
			textView.KeyUp += (k) => {

				// if current text doesn't match saved text
				var areDiff = tab.UnsavedChanges;

				if (areDiff) {
					if (!tab.Text.ToString ().EndsWith ('*')) {

						tab.Text = tab.Text.ToString () + '*';
						tabView.SetNeedsDisplay ();
					}
				} else {

					if (tab.Text.ToString ().EndsWith ('*')) {

						tab.Text = tab.Text.ToString ().TrimEnd ('*');
						tabView.SetNeedsDisplay ();
					}
				}
			};
		}

		public void Save ()
		{
			var tab = tabView.SelectedTab as OpenedFile;

			if (tab == null) {
				return;
			}

			if (tab.File == null) {
				SaveAs ();
			}

			tab.Save ();
			tabView.SetNeedsDisplay ();
		}

		public bool SaveAs ()
		{
			var tab = tabView.SelectedTab as OpenedFile;

			if (tab == null) {
				return false;
			}

			var fd = new SaveDialog ();
			Application.Run (fd);

			if (string.IsNullOrWhiteSpace (fd.FilePath?.ToString ())) {
				return false;
			}

			tab.File = new FileInfo (fd.FilePath.ToString ());
			tab.Text = fd.FileName.ToString ();
			tab.Save ();

			return true;
		}

		private class OpenedFile : TabView.Tab {
			private string savedText;

			public FileInfo File { get; set; }

			/// <summary>
			/// The text of the tab the last time it was saved
			/// </summary>
			/// <value></value>
			public string SavedText {
				get => savedText;
				set {
					View.Text = ustring.Make (value);
					savedText = View.Text.ToString ();
				}
			}

			public bool UnsavedChanges => !string.Equals (SavedText, View.Text.ToString ());

			public OpenedFile (string name, FileInfo file, TextView control) : base (name, control)
			{
				File = file;
				SavedText = control.Text.ToString ();
			}

			internal void Save ()
			{
				var newText = View.Text.ToString ();

				System.IO.File.WriteAllText (File.FullName, newText);
				SavedText = newText;

				Text = Text.ToString ().TrimEnd ('*');
			}
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}
	}
}
