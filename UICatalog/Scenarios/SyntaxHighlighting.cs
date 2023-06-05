
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Terminal.Gui;
using Attribute = Terminal.Gui.Attribute;

namespace UICatalog.Scenarios {
	[ScenarioMetadata (Name: "Syntax Highlighting", Description: "Text editor with keyword highlighting using the TextView control.")]
	[ScenarioCategory ("Text and Formatting")]
	[ScenarioCategory ("Controls")]
	[ScenarioCategory ("TextView")]
	public class SyntaxHighlighting : Scenario {

		TextView textView;
		MenuItem miWrap;
		private HashSet<string> keywords = new HashSet<string> (StringComparer.CurrentCultureIgnoreCase);
		private ColorScheme blue;
		private ColorScheme magenta;
		private ColorScheme white;

		public override void Setup ()
		{
			Win.Title = this.GetName ();

			var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_TextView", new MenuItem [] {
				miWrap =  new MenuItem ("_Word Wrap", "", () => WordWrap()){CheckType = MenuItemCheckStyle.Checked},
				null,
				new MenuItem ("_Syntax Highlighting", "", () => ApplyHighlighting()),
				new MenuItem ("_In Quotes", "", () => ApplyInQuotes()),
				new MenuItem ("_Load Rune Cells", "", () => ApplyLoadRuneCells()),
				null,
				new MenuItem ("_Quit", "", () => Quit()),
			})
			});
			Application.Top.Add (menu);

			textView = new TextView () {
				X = 0,
				Y = 0,
				Width = Dim.Fill (),
				Height = Dim.Fill ()
			};

			ApplyHighlighting ();

			Win.Add (textView);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});

			Application.Top.Add (statusBar);
		}

		private void ApplyHighlighting ()
		{
			ClearAllEvents ();

			blue = ColorScheme.SetAllAttributesBasedOn (new Attribute (Color.Cyan, Color.Black));
			white = ColorScheme.SetAllAttributesBasedOn (new Attribute (Color.White, Color.Black));
			textView.ColorScheme = white;
			textView.Text = "SELECT TOP 100 * \nfrom\n MyDb.dbo.Biochemistry;";

			keywords.Add ("select");
			keywords.Add ("distinct");
			keywords.Add ("top");
			keywords.Add ("from");
			keywords.Add ("create");
			keywords.Add ("CIPHER");
			keywords.Add ("CLASS_ORIGIN");
			keywords.Add ("CLIENT");
			keywords.Add ("CLOSE");
			keywords.Add ("COALESCE");
			keywords.Add ("CODE");
			keywords.Add ("COLUMNS");
			keywords.Add ("COLUMN_FORMAT");
			keywords.Add ("COLUMN_NAME");
			keywords.Add ("COMMENT");
			keywords.Add ("COMMIT");
			keywords.Add ("COMPACT");
			keywords.Add ("COMPLETION");
			keywords.Add ("COMPRESSED");
			keywords.Add ("COMPRESSION");
			keywords.Add ("CONCURRENT");
			keywords.Add ("CONNECT");
			keywords.Add ("CONNECTION");
			keywords.Add ("CONSISTENT");
			keywords.Add ("CONSTRAINT_CATALOG");
			keywords.Add ("CONSTRAINT_SCHEMA");
			keywords.Add ("CONSTRAINT_NAME");
			keywords.Add ("CONTAINS");
			keywords.Add ("CONTEXT");
			keywords.Add ("CONTRIBUTORS");
			keywords.Add ("COPY");
			keywords.Add ("CPU");
			keywords.Add ("CURSOR_NAME");
			keywords.Add ("primary");
			keywords.Add ("key");
			keywords.Add ("insert");
			keywords.Add ("alter");
			keywords.Add ("add");
			keywords.Add ("update");
			keywords.Add ("set");
			keywords.Add ("delete");
			keywords.Add ("truncate");
			keywords.Add ("as");
			keywords.Add ("order");
			keywords.Add ("by");
			keywords.Add ("asc");
			keywords.Add ("desc");
			keywords.Add ("between");
			keywords.Add ("where");
			keywords.Add ("and");
			keywords.Add ("or");
			keywords.Add ("not");
			keywords.Add ("limit");
			keywords.Add ("null");
			keywords.Add ("is");
			keywords.Add ("drop");
			keywords.Add ("database");
			keywords.Add ("table");
			keywords.Add ("having");
			keywords.Add ("in");
			keywords.Add ("join");
			keywords.Add ("on");
			keywords.Add ("union");
			keywords.Add ("exists");

			textView.Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator () {
				AllSuggestions = keywords.ToList ()
			};

			textView.DrawNormalColor += ProcessHighlighting;
			textView.DrawSelectionColor += ProcessHighlighting;
			textView.DrawReadOnlyColor += ProcessHighlighting;
			textView.DrawUsedColor += ProcessHighlighting;
		}

		private void ApplyInQuotes ()
		{
			ClearAllEvents ();

			magenta = ColorScheme.SetAllAttributesBasedOn (new Attribute (Color.Magenta, Color.Black));
			white = ColorScheme.SetAllAttributesBasedOn (new Attribute (Color.White, Color.Black));
			textView.ColorScheme = white;
			textView.Text = "\"SELECT\" TOP \"100\" * \n\"from\"\n \"MyDb\".\"dbo\".\"Biochemistry;\"";

			textView.DrawContent += (s, e) => ProcessInQuotes ();
		}

		private void ApplyLoadRuneCells ()
		{
			ClearAllEvents ();

			List<RuneCell> runeCells = new List<RuneCell> ();
			foreach (var color in Colors.ColorSchemes) {
				string csName = color.Key;
				foreach (var rune in csName.EnumerateRunes ()) {
					runeCells.Add (new RuneCell { Rune = rune, ColorScheme = color.Value });
				}
				runeCells.Add (new RuneCell { Rune = (Rune)'\n', ColorScheme = color.Value });
			}

			textView.LoadRuneCells (runeCells);
		}

		private void ClearAllEvents ()
		{
			textView.ClearEventHandlers ("DrawContent");

			textView.ClearEventHandlers ("DrawNormalColor");
			textView.ClearEventHandlers ("DrawSelectionColor");
			textView.ClearEventHandlers ("DrawReadOnlyColor");
			textView.ClearEventHandlers ("DrawUsedColor");
		}

		private void ProcessHighlighting (object s, RuneCellEventArgs e)
		{
			if (IsKeyword (e.Line.Select (c => c.Rune).ToList (), e.IdxCol)) {
				e.Line [e.IdxCol].ColorScheme = blue;
			}
		}

		private void ProcessInQuotes ()
		{
			var areInQuotes = 0;
			var quoteRune = new Rune ('"');

			for (int y = 0; y < textView.Lines; y++) {

				var line = textView.GetLine (y);

				for (int x = 0; x < line.Count; x++) {
					bool isQuote;
					if (line [x].Rune == quoteRune) {
						isQuote = true;
						if (areInQuotes == 0) {
							areInQuotes = 1;
						} else {
							areInQuotes = 2;
						}
					} else {
						isQuote = false;
					}
					if (!isQuote && areInQuotes == 1) {
						line [x].ColorScheme = magenta;
					} else {
						line [x].ColorScheme = white;
					}
					if (!isQuote && areInQuotes == 2) {
						areInQuotes = 0;
					}
				}
			}
			textView.SetNeedsDisplay ();
		}

		private void WordWrap ()
		{
			miWrap.Checked = !miWrap.Checked;
			textView.WordWrap = (bool)miWrap.Checked;
		}

		private void Quit ()
		{
			Application.RequestStop ();
		}

		private bool IsKeyword (List<Rune> line, int idx)
		{
			var word = IdxToWord (line, idx);

			if (string.IsNullOrWhiteSpace (word)) {
				return false;
			}

			return keywords.Contains (word, StringComparer.CurrentCultureIgnoreCase);
		}

		private string IdxToWord (List<Rune> line, int idx)
		{
			var words = Regex.Split (
				new string (line.Select (r => (char)r.Value).ToArray ()),
				"\\b");

			int count = 0;
			string current = null;

			foreach (var word in words) {
				current = word;
				count += word.Length;
				if (count > idx) {
					break;
				}
			}

			return current?.Trim ();
		}
	}

	public static class EventExtensions {
		public static void ClearEventHandlers (this object obj, string eventName)
		{
			if (obj == null) {
				return;
			}

			var objType = obj.GetType ();
			var eventInfo = objType.GetEvent (eventName);
			if (eventInfo == null) {
				return;
			}

			var isEventProperty = false;
			var type = objType;
			FieldInfo eventFieldInfo = null;
			while (type != null) {
				/* Find events defined as field */
				eventFieldInfo = type.GetField (eventName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (eventFieldInfo != null && (eventFieldInfo.FieldType == typeof (MulticastDelegate) || eventFieldInfo.FieldType.IsSubclassOf (typeof (MulticastDelegate)))) {
					break;
				}

				/* Find events defined as property { add; remove; } */
				eventFieldInfo = type.GetField ("EVENT_" + eventName.ToUpper (), BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
				if (eventFieldInfo != null) {
					isEventProperty = true;
					break;
				}

				type = type.BaseType;
			}

			if (eventFieldInfo == null) {
				return;
			}

			if (isEventProperty) {
				// Default Events Collection Type
				RemoveHandler<EventHandlerList> (obj, eventFieldInfo);
				return;
			}

			if (!(eventFieldInfo.GetValue (obj) is Delegate eventDelegate)) {
				return;
			}

			// Remove Field based event handlers
			foreach (var d in eventDelegate.GetInvocationList ()) {
				eventInfo.RemoveEventHandler (obj, d);
			}
		}

		private static void RemoveHandler<T> (object obj, FieldInfo eventFieldInfo)
		{
			var objType = obj.GetType ();
			var eventPropertyValue = eventFieldInfo.GetValue (obj);

			if (eventPropertyValue == null) {
				return;
			}

			var propertyInfo = objType.GetProperties (BindingFlags.NonPublic | BindingFlags.Instance)
						  .FirstOrDefault (p => p.Name == "Events" && p.PropertyType == typeof (T));
			if (propertyInfo == null) {
				return;
			}

			var eventList = propertyInfo?.GetValue (obj, null);
			switch (eventList) {
			case null:
				return;
			}
		}
	}
}
