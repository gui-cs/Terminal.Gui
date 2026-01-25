#nullable enable
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Syntax Highlighting", "Text editor with keyword highlighting using the TextView control.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("TextView")]
public class SyntaxHighlighting : Scenario
{
    private readonly HashSet<string> _keywords = new (StringComparer.CurrentCultureIgnoreCase)
    {
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

    private readonly string _path = "Cells.rce";
    private Runnable? _appWindow;
    private Attribute _blue;
    private Attribute _green;
    private Attribute _magenta;
    private TextView? _textView;
    private Attribute _white;

    /// <summary>
    ///     Reads an object instance from a Json file.
    ///     <para>Object type must have a parameterless constructor.</para>
    /// </summary>
    /// <typeparam name="T">The type of object to read from the file.</typeparam>
    /// <param name="filePath">The file path to read the object instance from.</param>
    /// <returns>Returns a new instance of the object read from the Json file.</returns>
    public static T ReadFromJsonFile<T> (string filePath) where T : new ()
    {
        TextReader? reader = null;

        try
        {
            reader = new StreamReader (filePath);
            string fileContents = reader.ReadToEnd ();

            return JsonSerializer.Deserialize<T> (fileContents) ?? new T ();
        }
        finally
        {
            reader?.Close ();
        }
    }

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        // Init
        using IApplication app = Application.Create ();
        app.Init ();

        // Setup - Create a top-level application window and configure it.
        using Runnable appWindow = new ();
        _appWindow = appWindow;

        MenuBar menu = new ();

        MenuItem wrapMenuItem = CreateWordWrapMenuItem ();

        menu.Add (
                  new MenuBarItem (
                                   "_TextView",
                                   [
                                       wrapMenuItem,
                                       new Line (),
                                       new MenuItem { Title = "_Syntax Highlighting", Action = ApplySyntaxHighlighting },
                                       new Line (),
                                       new MenuItem { Title = "_Load Text Cells", Action = ApplyLoadCells },
                                       new MenuItem { Title = "_Save Text Cells", Action = SaveCells },
                                       new Line (),
                                       new MenuItem { Title = Strings.cmdQuit, Action = Quit }
                                   ]
                                  )
                 );

        _appWindow.Add (menu);

        _textView = new ()
        {
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1)
        };

        ApplySyntaxHighlighting ();

        _appWindow.Add (_textView);

        StatusBar statusBar = new ([new (Application.QuitKey, "Quit", Quit)]);

        _appWindow.Add (statusBar);

        // Run - Start the application.
        app.Run (_appWindow);
    }

    private MenuItem CreateWordWrapMenuItem ()
    {
        CheckBox checkBox = new ()
        {
            Title = "_Word Wrap",
            CheckedState = _textView?.WordWrap == true ? CheckState.Checked : CheckState.UnChecked
        };

        checkBox.CheckedStateChanged += (_, _) =>
                                        {
                                            if (_textView is not null)
                                            {
                                                _textView.WordWrap = checkBox.CheckedState == CheckState.Checked;
                                            }
                                        };

        MenuItem item = new () { CommandView = checkBox };

        item.Accepting += (_, e) =>
                          {
                              checkBox.AdvanceCheckState ();
                              e.Handled = true;
                          };

        return item;
    }

    /// <summary>
    ///     Writes the given object instance to a Json file.
    ///     <para>Object type must have a parameterless constructor.</para>
    ///     <para>
    ///         Only Public properties and variables will be written to the file. These can be any type though, even other
    ///         classes.
    ///     </para>
    ///     <para>
    ///         If there are public properties/variables that you do not want written to the file, decorate them with the
    ///         [JsonIgnore] attribute.
    ///     </para>
    /// </summary>
    /// <typeparam name="T">The type of object being written to the file.</typeparam>
    /// <param name="filePath">The file path to write the object instance to.</param>
    /// <param name="objectToWrite">The object instance to write to the file.</param>
    /// <param name="append">
    ///     If false the file will be overwritten if it already exists. If true the contents will be appended
    ///     to the file.
    /// </param>
    public static void WriteToJsonFile<T> (string filePath, T objectToWrite, bool append = false) where T : new ()
    {
        TextWriter? writer = null;

        try
        {
            string contentsToWriteToFile = JsonSerializer.Serialize (objectToWrite);
            writer = new StreamWriter (filePath, append);
            writer.Write (contentsToWriteToFile);
        }
        finally
        {
            writer?.Close ();
        }
    }

    private void ApplyLoadCells ()
    {
        ClearAllEvents ();

        List<Cell> cells = new ();

        foreach (KeyValuePair<string, Scheme?> color in SchemeManager.GetSchemesForCurrentTheme ())
        {
            if (color.Value is null)
            {
                continue;
            }

            string csName = color.Key;

            cells.AddRange (Cell.ToCellList (csName, color.Value.Normal));

            cells.Add (new () { Grapheme = "\n", Attribute = color.Value.Focus });
        }

        if (_textView is null)
        {
            return;
        }

        if (File.Exists (_path))
        {
            //Reading the file
            List<List<Cell>> fileCells = ReadFromJsonFile<List<List<Cell>>> (_path);
            _textView.Load (fileCells);
        }
        else
        {
            _textView.Load (cells);
        }

        _textView.Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator ();
    }

    private void ApplySyntaxHighlighting ()
    {
        ClearAllEvents ();

        if (_textView is null)
        {
            return;
        }

        _green = new (Color.Green, Color.Black);
        _blue = new (Color.Blue, Color.Black);
        _magenta = new (Color.Magenta, Color.Black);
        _white = new (Color.White, Color.Black);
        _textView.SetScheme (new () { Focus = _white });

        _textView.Text =
            "/*Query to select:\nLots of data*/\nSELECT TOP 100 * \nfrom\n MyDb.dbo.Biochemistry where TestCode = 'blah';";

        _textView.Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator
        {
            AllSuggestions = _keywords.ToList ()
        };

        // DrawingText happens before DrawingContent so we use it to highlight
        _textView.DrawingText += (_, _) => HighlightTextBasedOnKeywords ();
    }

    private void ClearAllEvents ()
    {
        if (_textView is null)
        {
            return;
        }

        _textView.ClearEventHandlers ("DrawingText");
        _textView.InheritsPreviousAttribute = false;
    }

    private bool ContainsPosition (Match m, int pos) => pos >= m.Index && pos < m.Index + m.Length;

    private void HighlightTextBasedOnKeywords ()
    {
        if (_textView is null)
        {
            return;
        }

        // Comment blocks, quote blocks etc
        Dictionary<Rune, Scheme> blocks = new ();

        Regex comments = new (@"/\*.*?\*/", RegexOptions.Singleline);
        MatchCollection commentMatches = comments.Matches (_textView.Text);

        Regex singleQuote = new (@"'.*?'", RegexOptions.Singleline);
        MatchCollection singleQuoteMatches = singleQuote.Matches (_textView.Text);

        // Find all keywords (ignoring for now if they are in comments, quotes etc)
        Regex [] keywordRegexes =
            _keywords.Select (k => new Regex ($@"\b{k}\b", RegexOptions.IgnoreCase)).ToArray ();
        Match [] keywordMatches = keywordRegexes.SelectMany (r => r.Matches (_textView.Text)).ToArray ();

        var pos = 0;

        for (var y = 0; y < _textView.Lines; y++)
        {
            List<Cell> line = _textView.GetLine (y);

            for (var x = 0; x < line.Count; x++)
            {
                Cell cell = line [x];

                if (commentMatches.Any (m => ContainsPosition (m, pos)))
                {
                    cell.Attribute = _green;
                }
                else if (singleQuoteMatches.Any (m => ContainsPosition (m, pos)))
                {
                    cell.Attribute = _magenta;
                }
                else if (keywordMatches.Any (m => ContainsPosition (m, pos)))
                {
                    cell.Attribute = _blue;
                }
                else
                {
                    cell.Attribute = _white;
                }

                line [x] = cell;
                pos++;
            }

            // for the \n or \r\n that exists in Text but not the returned lines
            pos += Environment.NewLine.Length;
        }
    }

    private string? IdxToWord (List<Rune> line, int idx)
    {
        string [] words = Regex.Split (
                                       new (line.Select (r => (char)r.Value).ToArray ()),
                                       "\\b"
                                      );

        var count = 0;
        string? current = null;

        foreach (string word in words)
        {
            current = word;
            count += word.Length;

            if (count > idx)
            {
                break;
            }
        }

        return current?.Trim ();
    }

    private void Quit () { _appWindow?.RequestStop (); }

    private void SaveCells ()
    {
        if (_textView is null)
        {
            return;
        }

        //Writing to file
        List<List<Cell>> cells = _textView.GetAllLines ();
        WriteToJsonFile (_path, cells);
    }
}

public static class EventExtensions
{
    public static void ClearEventHandlers (this object obj, string eventName)
    {
        if (obj == null)
        {
            return;
        }

        Type objType = obj.GetType ();
        EventInfo? eventInfo = objType.GetEvent (eventName);

        if (eventInfo == null)
        {
            return;
        }

        var isEventProperty = false;
        Type? type = objType;
        FieldInfo? eventFieldInfo = null;

        while (type != null)
        {
            /* Find events defined as field */
            eventFieldInfo = type.GetField (
                                            eventName,
                                            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                           );

            if (eventFieldInfo != null
                && (eventFieldInfo.FieldType == typeof (MulticastDelegate)
                    || eventFieldInfo.FieldType.IsSubclassOf (
                                                              typeof (MulticastDelegate)
                                                             )))
            {
                break;
            }

            /* Find events defined as property { add; remove; } */
            eventFieldInfo = type.GetField (
                                            "EVENT_" + eventName.ToUpper (),
                                            BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic
                                           );

            if (eventFieldInfo != null)
            {
                isEventProperty = true;

                break;
            }

            type = type.BaseType;
        }

        if (eventFieldInfo == null)
        {
            return;
        }

        if (isEventProperty)
        {
            // Default Events Collection Type
            RemoveHandler<EventHandlerList> (obj, eventFieldInfo);

            return;
        }

        if (!(eventFieldInfo.GetValue (obj) is Delegate eventDelegate))
        {
            return;
        }

        // Remove Field based event handlers
        foreach (Delegate d in eventDelegate.GetInvocationList ())
        {
            eventInfo.RemoveEventHandler (obj, d);
        }
    }

    private static void RemoveHandler<T> (object obj, FieldInfo eventFieldInfo)
    {
        Type objType = obj.GetType ();
        object? eventPropertyValue = eventFieldInfo.GetValue (obj);

        if (eventPropertyValue == null)
        {
            return;
        }

        PropertyInfo? propertyInfo = objType.GetProperties (BindingFlags.NonPublic | BindingFlags.Instance)
                                            .FirstOrDefault (p => p.Name == "Events" && p.PropertyType == typeof (T));

        if (propertyInfo == null)
        {
            return;
        }

        object? eventList = propertyInfo.GetValue (obj, null);

        switch (eventList)
        {
            case null:
                return;
        }
    }
}
