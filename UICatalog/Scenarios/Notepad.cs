using System;
using System.IO;
using Terminal.Gui;

namespace UICatalog.Scenarios {

	[ScenarioMetadata (Name: "Notepad", Description: "Multi-tab text editor uising the TabView control.")]
	[ScenarioCategory ("Controls"), ScenarioCategory ("TabView")]
	public class Notepad : Scenario {
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
					new MenuItem ("_New", "", () => New()),
					new MenuItem ("_Open", "", () => Open()),
					new MenuItem ("_Save", "", () => Save()),
					new MenuItem ("Save _As", "", () => SaveAs()),
					new MenuItem ("_Close", "", () => Close()),
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

			tabView.TabClicked += TabView_TabClicked;

			tabView.Style.ShowBorder = true;
			tabView.ApplyStyleChanges ();

			// Start with only a single view but support splitting to show side by side
			var split = new SplitView {
				X = 0,
				Y = 1,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};
			split.View2.Visible = false;
			split.SetView1 (tabView);
			split.IntegratedBorder = BorderStyle.None;

			Application.Top.Add (split);

			var lenStatusItem = new StatusItem (Key.CharMask, "Len: ", null);
			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),

				// These shortcut keys don't seem to work correctly in linux 
				//new StatusItem(Key.CtrlMask | Key.N, "~^O~ Open", () => Open()),
				//new StatusItem(Key.CtrlMask | Key.N, "~^N~ New", () => New()),

				new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", () => Save()),
				new StatusItem(Key.CtrlMask | Key.W, "~^W~ Close", () => Close()),
				lenStatusItem,
			});

			tabView.SelectedTabChanged += (s, e) => lenStatusItem.Title = $"Len:{(e.NewTab?.View?.Text?.Length ?? 0)}";

			Application.Top.Add (statusBar);

			New ();
		}

		private void TabView_TabClicked (object sender, TabView.TabMouseEventArgs e)
		{
			// we are only interested in right clicks
			if(!e.MouseEvent.Flags.HasFlag(MouseFlags.Button3Clicked)) {
				return;
			}

			MenuBarItem items;

			if (e.Tab == null) {
				items = new MenuBarItem (new MenuItem [] {
					new MenuItem ($"Open", "", () => Open()),
				});

			} else {

				var tv = (TabView)sender;
				var t = (OpenedFile)e.Tab;

				items = new MenuBarItem (new MenuItem [] {
					new MenuItem ($"Save", "", () => Save(e.Tab)),
					new MenuItem ($"Close", "", () => Close(e.Tab)),
					null,
					new MenuItem ($"Split Up", "", () => SplitUp(tv,t)),
					new MenuItem ($"Split Down", "", () => SplitDown(tv,t)),
					new MenuItem ($"Split Right", "", () => SplitRight(tv,t)),
					new MenuItem ($"Split Left", "", () => SplitLeft(tv,t)),
				});
			}

		((View)sender).ViewToScreen (e.MouseEvent.X, e.MouseEvent.Y, out int screenX, out int screenY,true);

		var contextMenu = new ContextMenu (screenX,screenY, items);

			contextMenu.Show ();
			e.MouseEvent.Handled = true;
		}

		private void SplitUp (TabView sender, OpenedFile tab)
		{
			
		}
		private void SplitDown (TabView sender, OpenedFile tab)
		{
			
		}
		private void SplitLeft (TabView sender, OpenedFile tab)
		{
			
		}
		private void SplitRight (TabView sender, OpenedFile tab)
		{
			var split = (SplitView)sender.SuperView;

			// TODO: How can SuperView sometimes be null?!
			if(split == null) {
				throw new NullReferenceException ("Much confusion, sender.SuperView is null");
			}

			split.TrySplitView1 (out var sub);
			sub.Orientation = Terminal.Gui.Graphs.Orientation.Vertical;
			var newTabView = CreateNewTabView ();
			tab.CloneTo (newTabView);
			sub.SetView2 (newTabView);
		}

		private TabView CreateNewTabView ()
		{
			var tv = new TabView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (),
			};

			tv.TabClicked += TabView_TabClicked;
			return tv;
		}

		private void New ()
		{
			Open (null, $"new {numbeOfNewTabs++}");
		}

		private void Close ()
		{
			Close (tabView.SelectedTab);
		}
		private void Close (TabView.Tab tabToClose)
		{
			var tab = tabToClose as OpenedFile;

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

					Open (new FileInfo (path), Path.GetFileName (path));
				}
			}
		}

		/// <summary>
		/// Creates a new tab with initial text
		/// </summary>
		/// <param name="fileInfo">File that was read or null if a new blank document</param>
		private void Open (FileInfo fileInfo, string tabName)
		{
			var tab = new OpenedFile (tabView, tabName, fileInfo);
			tabView.AddTab (tab, true);
		}

		public void Save ()
		{
			Save (tabView.SelectedTab);
		}
		public void Save (TabView.Tab tabToSave)
		{
			var tab = tabToSave as OpenedFile;

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
			public FileInfo File { get; set; }

			/// <summary>
			/// The text of the tab the last time it was saved
			/// </summary>
			/// <value></value>
			public string SavedText { get; set; }

			public bool UnsavedChanges => !string.Equals (SavedText, View.Text.ToString ());

			public OpenedFile (TabView parent, string name, FileInfo file) 
				: base (name, CreateTextView(file))
			{

				File = file;
				SavedText = View.Text.ToString ();
				RegisterTextViewEvents (parent);
			}

			private void RegisterTextViewEvents (TabView parent)
			{
				var textView = (TextView)View;
				// when user makes changes rename tab to indicate unsaved
				textView.KeyUp += (k) => {

					// if current text doesn't match saved text
					var areDiff = this.UnsavedChanges;

					if (areDiff) {
						if (!this.Text.ToString ().EndsWith ('*')) {

							this.Text = this.Text.ToString () + '*';
							parent.SetNeedsDisplay ();
						}
					} else {
						
						if (Text.ToString ().EndsWith ('*')) {

							Text = Text.ToString ().TrimEnd ('*');
							parent.SetNeedsDisplay ();
						}
					}
				};
			}

			private static View CreateTextView (FileInfo file)
			{
				string initialText = string.Empty;
				if(file != null && file.Exists) {
					
					initialText = System.IO.File.ReadAllText (file.FullName);
				}

				return new TextView () {
					X = 0,
					Y = 0,
					Width = Dim.Fill (),
					Height = Dim.Fill (),
					Text = initialText,
					AllowsTab = false,
				};
			}
			public OpenedFile CloneTo(TabView other)
			{
				var newTab = new OpenedFile (other, base.Text.ToString(), File);
				other.AddTab (newTab, true);
				return newTab;
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
