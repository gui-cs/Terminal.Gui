using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Terminal.Gui;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Syntax Highlighting", "Text editor with keyword highlighting using the TextView control.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TextView")]
public class SyntaxHighlighting : Scenario {
	readonly HashSet<string> _keywords = new (StringComparer.CurrentCultureIgnoreCase) {
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
		"exists"
	};

	readonly string _path = "RuneCells.rce";
	ColorScheme _blue;
	ColorScheme _green;

	ColorScheme _magenta;
	MenuItem _miWrap;

	TextView _textView;
	ColorScheme _white;

	public override void Setup ()
	{
		Win.Title = GetName ();

		var menu = new MenuBar {
			Menus = [
				new MenuBarItem ("_TextView", new [] {
					_miWrap = new MenuItem ("_Word Wrap", "", () => WordWrap ())
						{ CheckType = MenuItemCheckStyle.Checked },
					null,
					new("_Syntax Highlighting", "", () => ApplySyntaxHighlighting ()),
					null,
					new("_Load Rune Cells", "", () => ApplyLoadRuneCells ()),
					new("_Save Rune Cells", "", () => SaveRuneCells ()),
					null,
					new("_Quit", "", () => Quit ())
				})
			]
		};
		Application.Top.Add (menu);

		_textView = new TextView {
			X = 0,
			Y = 0,
			Width = Dim.Fill (),
			Height = Dim.Fill ()
		};

		ApplySyntaxHighlighting ();

		Win.Add (_textView);

		var statusBar = new StatusBar (new StatusItem [] {
			new(Application.QuitKey, $"{Application.QuitKey} to Quit", () => Quit ())
		});

		Application.Top.Add (statusBar);
	}

	void ApplySyntaxHighlighting ()
	{
		ClearAllEvents ();

		_green = new ColorScheme (new Attribute (Color.Green, Color.Black));
		_blue = new ColorScheme (new Attribute (Color.Blue, Color.Black));
		_magenta = new ColorScheme (new Attribute (Color.Magenta, Color.Black));
		_white = new ColorScheme (new Attribute (Color.White, Color.Black));
		_textView.ColorScheme = _white;

		_textView.Text =
			"/*Query to select:\nLots of data*/\nSELECT TOP 100 * \nfrom\n MyDb.dbo.Biochemistry where TestCode = 'blah';";

		_textView.Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator {
			AllSuggestions = _keywords.ToList ()
		};

		_textView.TextChanged += (s, e) => HighlightTextBasedOnKeywords ();
		_textView.DrawContent += (s, e) => HighlightTextBasedOnKeywords ();
		_textView.DrawContentComplete += (s, e) => HighlightTextBasedOnKeywords ();
	}

	void ApplyLoadRuneCells ()
	{
		ClearAllEvents ();

		var runeCells = new List<RuneCell> ();
		foreach (var color in Colors.ColorSchemes) {
			var csName = color.Key;
			foreach (var rune in csName.EnumerateRunes ()) {
				runeCells.Add (new RuneCell { Rune = rune, ColorScheme = color.Value });
			}

			runeCells.Add (new RuneCell { Rune = (Rune)'\n', ColorScheme = color.Value });
		}

		if (File.Exists (_path)) {
			//Reading the file  
			var cells = ReadFromJsonFile<List<List<RuneCell>>> (_path);
			_textView.Load (cells);
		} else {
			_textView.Load (runeCells);
		}

		_textView.Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator ();
	}

	void SaveRuneCells ()
	{
		//Writing to file  
		var cells = _textView.GetAllLines ();
		WriteToJsonFile (_path, cells);
	}

	void ClearAllEvents ()
	{
		_textView.ClearEventHandlers ("TextChanged");
		_textView.ClearEventHandlers ("DrawContent");
		_textView.ClearEventHandlers ("DrawContentComplete");

		_textView.InheritsPreviousColorScheme = false;
	}

	void HighlightTextBasedOnKeywords ()
	{
		// Comment blocks, quote blocks etc
		var blocks = new Dictionary<Rune, ColorScheme> ();

		var comments = new Regex (@"/\*.*?\*/", RegexOptions.Singleline);
		var commentMatches = comments.Matches (_textView.Text);

		var singleQuote = new Regex (@"'.*?'", RegexOptions.Singleline);
		var singleQuoteMatches = singleQuote.Matches (_textView.Text);

		// Find all keywords (ignoring for now if they are in comments, quotes etc)
		var keywordRegexes =
			_keywords.Select (k => new Regex ($@"\b{k}\b", RegexOptions.IgnoreCase)).ToArray ();
		var keywordMatches = keywordRegexes.SelectMany (r => r.Matches (_textView.Text)).ToArray ();

		var pos = 0;

		for (var y = 0; y < _textView.Lines; y++) {
			var line = _textView.GetLine (y);

			for (var x = 0; x < line.Count; x++) {
				if (commentMatches.Any (m => ContainsPosition (m, pos))) {
					line [x].ColorScheme = _green;
				} else if (singleQuoteMatches.Any (m => ContainsPosition (m, pos))) {
					line [x].ColorScheme = _magenta;
				} else if (keywordMatches.Any (m => ContainsPosition (m, pos))) {
					line [x].ColorScheme = _blue;
				} else {
					line [x].ColorScheme = _white;
				}

				pos++;
			}

			// for the \n or \r\n that exists in Text but not the returned lines
			pos += Environment.NewLine.Length;
		}
	}

	bool ContainsPosition (Match m, int pos) => pos >= m.Index && pos < m.Index + m.Length;

	void WordWrap ()
	{
		_miWrap.Checked = !_miWrap.Checked;
		_textView.WordWrap = (bool)_miWrap.Checked;
	}

	void Quit () => Application.RequestStop ();

	bool IsKeyword (List<Rune> line, int idx)
	{
		var word = IdxToWord (line, idx);

		if (string.IsNullOrWhiteSpace (word)) {
			return false;
		}

		return _keywords.Contains (word, StringComparer.CurrentCultureIgnoreCase);
	}

	string IdxToWord (List<Rune> line, int idx)
	{
		var words = Regex.Split (
			new string (line.Select (r => (char)r.Value).ToArray ()),
			"\\b");

		var count = 0;
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

	/// <summary>
	///         Writes the given object instance to a Json file.
	///         <para>Object type must have a parameterless constructor.</para>
	///         <para>
	///                 Only Public properties and variables will be written to the file. These can be any type though, even
	///                 other classes.
	///         </para>
	///         <para>
	///                 If there are public properties/variables that you do not want written to the file, decorate them with
	///                 the
	///                 [JsonIgnore] attribute.
	///         </para>
	/// </summary>
	/// <typeparam name="T">The type of object being written to the file.</typeparam>
	/// <param name="filePath">The file path to write the object instance to.</param>
	/// <param name="objectToWrite">The object instance to write to the file.</param>
	/// <param name="append">
	///         If false the file will be overwritten if it already exists. If true the contents will be appended
	///         to the file.
	/// </param>
	public static void WriteToJsonFile<T> (string filePath, T objectToWrite, bool append = false) where T : new()
	{
		TextWriter writer = null;
		try {
			var contentsToWriteToFile = JsonSerializer.Serialize (objectToWrite);
			writer = new StreamWriter (filePath, append);
			writer.Write (contentsToWriteToFile);
		} finally {
			if (writer != null) {
				writer.Close ();
			}
		}
	}

	/// <summary>
	///         Reads an object instance from an Json file.
	///         <para>Object type must have a parameterless constructor.</para>
	/// </summary>
	/// <typeparam name="T">The type of object to read from the file.</typeparam>
	/// <param name="filePath">The file path to read the object instance from.</param>
	/// <returns>Returns a new instance of the object read from the Json file.</returns>
	public static T ReadFromJsonFile<T> (string filePath) where T : new()
	{
		TextReader reader = null;
		try {
			reader = new StreamReader (filePath);
			var fileContents = reader.ReadToEnd ();
			return (T)JsonSerializer.Deserialize (fileContents, typeof (T));
		} finally {
			if (reader != null) {
				reader.Close ();
			}
		}
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
			eventFieldInfo = type.GetField (eventName,
				BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public |
				BindingFlags.NonPublic);
			if (eventFieldInfo != null && (eventFieldInfo.FieldType == typeof (MulticastDelegate) ||
						       eventFieldInfo.FieldType.IsSubclassOf (
							       typeof (MulticastDelegate)))) {
				break;
			}

			/* Find events defined as property { add; remove; } */
			eventFieldInfo = type.GetField ("EVENT_" + eventName.ToUpper (),
				BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
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

	static void RemoveHandler<T> (object obj, FieldInfo eventFieldInfo)
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