
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
		private HashSet<string> keywords = new HashSet<string> (StringComparer.CurrentCultureIgnoreCase){

			"select",
			"distinct",
			"top",
			"from",
			"create",
			"CIPHER",
			"CLASS_ORIGIN",
			"CLIENT",
			"CLOSE",
			"COALESCE",
			"CODE",
			"COLUMNS",
			"COLUMN_FORMAT",
			"COLUMN_NAME",
			"COMMENT",
			"COMMIT",
			"COMPACT",
			"COMPLETION",
			"COMPRESSED",
			"COMPRESSION",
			"CONCURRENT",
			"CONNECT",
			"CONNECTION",
			"CONSISTENT",
			"CONSTRAINT_CATALOG",
			"CONSTRAINT_SCHEMA",
			"CONSTRAINT_NAME",
			"CONTAINS",
			"CONTEXT",
			"CONTRIBUTORS",
			"COPY",
			"CPU",
			"CURSOR_NAME",
			"primary",
			"key",
			"insert",
			"alter",
			"add",
			"update",
			"set",
			"delete",
			"truncate",
			"as",
			"order",
			"by",
			"asc",
			"desc",
			"between",
			"where",
			"and",
			"or",
			"not",
			"limit",
			"null",
			"is",
			"drop",
			"database",
			"table",
			"having",
			"in",
			"join",
			"on",
			"union",
			"exists",
		};
		private ColorScheme blue;
		private ColorScheme magenta;
		private ColorScheme white;
		private ColorScheme green;

		public override void Setup ()
		{
			Win.Title = this.GetName ();

			var menu = new MenuBar (new MenuBarItem [] {
			new MenuBarItem ("_TextView", new MenuItem [] {
				miWrap =  new MenuItem ("_Word Wrap", "", () => WordWrap()){CheckType = MenuItemCheckStyle.Checked},
				null,
				new MenuItem ("_Syntax Highlighting", "", () => ApplySyntaxHighlighting()),
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

			ApplySyntaxHighlighting ();

			Win.Add (textView);

			var statusBar = new StatusBar (new StatusItem [] {
				new StatusItem(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit()),
			});

			Application.Top.Add (statusBar);
		}

		private void ApplySyntaxHighlighting ()
		{
			ClearAllEvents ();

			green = ColorScheme.SetAllAttributesBasedOn (new Attribute (Color.Green, Color.Black));
			blue =  ColorScheme.SetAllAttributesBasedOn (new Attribute (Color.Blue, Color.Black));
			magenta = ColorScheme.SetAllAttributesBasedOn (new Attribute (Color.Magenta, Color.Black));
			white = ColorScheme.SetAllAttributesBasedOn (new Attribute (Color.White, Color.Black));
			textView.ColorScheme = white;
			
			textView.Text = "/*Query to select:\nLots of data*/\nSELECT TOP 100 * \nfrom\n MyDb.dbo.Biochemistry where TestCode = 'blah';";

			textView.Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator () {
				AllSuggestions = keywords.ToList ()
			};
			
			textView.TextChanged += (s,e)=>HighlightTextBasedOnKeywords();
			textView.DrawContent += (s,e)=>HighlightTextBasedOnKeywords();
			textView.DrawContentComplete += (s,e)=>HighlightTextBasedOnKeywords();
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

		private void HighlightTextBasedOnKeywords ()
		{
			// Comment blocks, quote blocks etc
			Dictionary<Rune,ColorScheme> blocks = new Dictionary<Rune, ColorScheme>();

			var comments = new Regex(@"/\*.*?\*/",RegexOptions.Singleline);
			var commentMatches = comments.Matches(textView.Text);

			var singleQuote = new Regex(@"'.*?'",RegexOptions.Singleline);
			var singleQuoteMatches = singleQuote.Matches(textView.Text);

			// Find all keywords (ignoring for now if they are in comments, quotes etc)
			Regex[] keywordRegexes = keywords.Select(k=>new Regex($@"\b{k}\b",RegexOptions.IgnoreCase)).ToArray();
			Match[] keywordMatches = keywordRegexes.SelectMany(r=>r.Matches(textView.Text)).ToArray();

			int pos = 0;

			for (int y = 0; y < textView.Lines; y++) {

				var line = textView.GetLine (y);

				for (int x = 0; x < line.Count; x++) {
					if (commentMatches.Any(m=>ContainsPosition(m,pos))) {
						line [x].ColorScheme = green;
					} else
					if (singleQuoteMatches.Any(m=>ContainsPosition(m,pos))) {
						line [x].ColorScheme = magenta;
					}else
					if (keywordMatches.Any(m=>ContainsPosition(m,pos))) {
						line [x].ColorScheme = blue;
					}  else {
						line [x].ColorScheme = white;
					}

					pos++;
				}

				// for the \n or \r\n that exists in Text but not the returned lines
				pos += Environment.NewLine.Length;
			}
		}

		private bool ContainsPosition (Match m, int pos)
		{
			return pos >= m.Index && pos < m.Index + m.Length;
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
