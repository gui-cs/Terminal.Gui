﻿using NStack;
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Terminal.Gui;
using Terminal.Gui.Configuration;
using static Terminal.Gui.Configuration.ConfigurationManager;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Configuration Editor", Description: "Edits Terminal.Gui Config Files.")]
	[ScenarioCategory ("TabView"), ScenarioCategory ("Colors"), ScenarioCategory ("Files and IO"), ScenarioCategory ("TextView")]
	public class ConfigurationEditor : Scenario {
		TabView tabView;

		private static ColorScheme editorColorScheme = new ColorScheme () {
			Normal = new Attribute (Color.Red, Color.White),
			Focus = new Attribute (Color.Red, Color.Black),
			HotFocus = new Attribute (Color.BrightRed, Color.Black),
			HotNormal = new Attribute (Color.Magenta, Color.White)
		};

		[SerializableConfigurationProperty (Scope = typeof (AppScope))]
		public static ColorScheme EditorColorScheme {
			get => editorColorScheme;
			set {
				editorColorScheme = value;
				EditorColorSchemeChanged?.Invoke ();
			}
		}

		private static Action EditorColorSchemeChanged;

		private string [] configFiles = {
			"~/.tui/UICatalog.config.json",
			"./.tui/UICatalog.config.json",
			"~/.tui/config.json",
			"./.tui/config.json"
		};

		// Don't create a Window, just return the top-level view
		public override void Init (ColorScheme colorScheme)
		{
			Application.Init ();
			Application.Top.ColorScheme = colorScheme;
		}

		public override void Setup ()
		{
			tabView = new TabView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill (1),
			};

			tabView.Style.ShowBorder = true;
			tabView.ApplyStyleChanges ();

			Application.Top.Add (tabView);

			var lenStatusItem = new StatusItem (Key.CharMask, "Len: ", null);
			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} Quit", () => Quit()),
				new StatusItem(Key.F5, "~F5~ Reload", () => Reload()),
				new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", () => Save()),
				lenStatusItem,
			});

			tabView.SelectedTabChanged += (s, e) => lenStatusItem.Title = $"Len:{(e.NewTab?.View?.Text?.Length ?? 0)}";

			Application.Top.Add (statusBar);

			Open ();

			tabView.SelectedTab = tabView.Tabs.ToArray () [0];

			ConfigurationEditor.EditorColorSchemeChanged += () => {
				foreach (var t in tabView.Tabs) {
					t.View.ColorScheme = ConfigurationEditor.EditorColorScheme;
					t.View.SetNeedsDisplay ();
				};
			};

			ConfigurationEditor.EditorColorSchemeChanged.Invoke ();
		}

		private void Open ()
		{
			foreach (var configFile in configFiles) {
				var homeDir = $"{Environment.GetFolderPath (Environment.SpecialFolder.UserProfile)}";
				FileInfo fileInfo = new FileInfo (configFile.Replace ("~", homeDir));
				string json;
				if (!fileInfo.Exists) {
					if (!Directory.Exists (fileInfo.DirectoryName)) {
						// Create dir
						Directory.CreateDirectory (fileInfo.DirectoryName!);
					}

					// Create empty config file
					json = ConfigurationManager.GetEmptyJson ();
				} else {
					json = File.ReadAllText (fileInfo.FullName);
				}

				Open (json, fileInfo, configFile);
			}
		}

		private void Reload ()
		{
			var tab = tabView!.SelectedTab as OpenedFile;
			tab!.SavedText = File.ReadAllText (tab!.File.FullName);
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

			OpenedFile tab = new OpenedFile (tabName, fileInfo, textView);
			tabView?.AddTab (tab, true);

			// when user makes changes rename tab to indicate unsaved
			textView.KeyUp += (k) => {

				// if current text doesn't match saved text
				string tabStr = tab.Text.ToString ()!;
				var areDiff = tab.UnsavedChanges;
				if (areDiff) {
					if (!tabStr.EndsWith ('*')) {

						tab.Text = tabStr + '*';
						tabView?.SetNeedsDisplay ();
					}
				} else {

					if (tabStr.EndsWith ('*')) {

						tab.Text = tabStr.TrimEnd ('*');
						tabView?.SetNeedsDisplay ();
					}
				}
			};
		}

		public void Save ()
		{
			(tabView?.SelectedTab as OpenedFile)!.Save();
			tabView.SetNeedsDisplay ();
		}

		private void Quit ()
		{
			foreach (var t in tabView!.Tabs) {
				var tab = t as OpenedFile;
				if (tab!.UnsavedChanges) {
					int result = MessageBox.Query ("Save Changes", $"Save changes to {tab!.Text.ToString ()!.TrimEnd ('*')}", "Yes", "No", "Cancel");
					if (result == -1 || result == 2) {
						// user cancelled
					}
					if (result == 0) {
						tab.Save ();
					}
				}
			}

			Application.RequestStop ();
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
				savedText = newText;

				Text = Text.ToString ()!.TrimEnd ('*');
			}
		}
	}
}
