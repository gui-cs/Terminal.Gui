using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Terminal.Gui;
using Terminal.Gui.TextValidateProviders;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Text Input Controls", "Tests all text input controls")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Text and Formatting")]
public class Text : Scenario
{
    private Label _labelMirroringTimeField;
    private TimeField _timeField;

    public override void Main ()
    {
        Application.Init ();
        var win = new Window { Title = GetQuitKeyAndName () };
        // TextField is a simple, single-line text input control
        var label = new Label { Text = "_TextField:" };
        win.Add (label);

        var textField = new TextField
        {
            X = Pos.Right (label) + 1,
            Y = 0,
            Width = Dim.Percent (50) - 1,
            Text = "TextField with test text. Unicode shouldn't 𝔹Aℝ𝔽!"
        };

        var singleWordGenerator = new SingleWordSuggestionGenerator ();
        textField.Autocomplete.SuggestionGenerator = singleWordGenerator;
        textField.TextChanging += TextField_TextChanging;

        void TextField_TextChanging (object sender, CancelEventArgs<string> e)
        {
            singleWordGenerator.AllSuggestions = Regex.Matches (e.NewValue, "\\w+")
                                                      .Select (s => s.Value)
                                                      .Distinct ()
                                                      .ToList ();
        }

        win.Add (textField);

        var labelMirroringTextField = new Label
        {
            X = Pos.Right (textField) + 1,
            Y = Pos.Top (textField),

            Width = Dim.Fill (1) - 1,
            Height = 1,
            Text = textField.Text
        };
        win.Add (labelMirroringTextField);
        textField.TextChanged += (s, prev) => { labelMirroringTextField.Text = textField.Text; };

        // TextView is a rich (as in functionality, not formatting) text editing control
        label = new() { Text = "T_extView:", Y = Pos.Bottom (label) + 1 };
        win.Add (label);

        var textView = new TextView
        {
            X = Pos.Right (label) + 1, 
            Y = Pos.Top (label), 
            Width = Dim.Percent (50) - 1, 
            Height = Dim.Percent (20)
        };
        textView.Text = "TextView with some more test text. Unicode shouldn't 𝔹Aℝ𝔽!";
        textView.DrawingContent += TextView_DrawContent;

        // This shows how to enable autocomplete in TextView.
        void TextView_DrawContent (object sender, DrawEventArgs e)
        {
            singleWordGenerator.AllSuggestions = Regex.Matches (textView.Text, "\\w+")
                                                      .Select (s => s.Value)
                                                      .Distinct ()
                                                      .ToList ();
        }

        win.Add (textView);

        var labelMirroringTextView = new Label
        {
            X = Pos.Right (textView) + 1,
            Y = Pos.Top (textView),

            Width = Dim.Fill (1) - 1,
            Height = Dim.Height (textView) - 1
        };
        win.Add (labelMirroringTextView);

        // Use ContentChanged to detect if the user has typed something in a TextView.
        // The TextChanged property is only fired if the TextView.Text property is
        // explicitly set
        textView.ContentsChanged += (s, a) =>
                                    {
                                        labelMirroringTextView.Enabled = !labelMirroringTextView.Enabled;
                                        labelMirroringTextView.Text = textView.Text;
                                    };

        // By default TextView is a multi-line control. It can be forced to 
        // single-line mode.
        var chxMultiline = new CheckBox
        {
            X = Pos.Left (textView), Y = Pos.Bottom (textView), CheckedState = textView.Multiline ? CheckState.Checked : CheckState.UnChecked, Text = "_Multiline"
        };
        win.Add (chxMultiline);

        var chxWordWrap = new CheckBox
        {
            X = Pos.Right (chxMultiline) + 2,
            Y = Pos.Top (chxMultiline),
            CheckedState = textView.WordWrap ? CheckState.Checked : CheckState.UnChecked,
            Text = "_Word Wrap"
        };
        chxWordWrap.CheckedStateChanging += (s, e) => textView.WordWrap = e.NewValue == CheckState.Checked;
        win.Add (chxWordWrap);

        // TextView captures Tabs (so users can enter /t into text) by default;
        // This means using Tab to navigate doesn't work by default. This shows
        // how to turn tab capture off.
        var chxCaptureTabs = new CheckBox
        {
            X = Pos.Right (chxWordWrap) + 2,
            Y = Pos.Top (chxWordWrap),
            CheckedState = textView.AllowsTab ? CheckState.Checked : CheckState.UnChecked,
            Text = "_Capture Tabs"
        };

        chxMultiline.CheckedStateChanging += (s, e) =>
                                {
                                    textView.Multiline = e.NewValue == CheckState.Checked;

                                    if (!textView.Multiline && chxWordWrap.CheckedState == CheckState.Checked)
                                    {
                                        chxWordWrap.CheckedState = CheckState.UnChecked;
                                    }

                                    if (!textView.Multiline && chxCaptureTabs.CheckedState == CheckState.Checked)
                                    {
                                        chxCaptureTabs.CheckedState = CheckState.UnChecked;
                                    }
                                };

        Key keyTab = textView.KeyBindings.GetFirstFromCommands (Command.Tab);
        Key keyBackTab = textView.KeyBindings.GetFirstFromCommands (Command.BackTab);

        chxCaptureTabs.CheckedStateChanging += (s, e) =>
                                  {
                                      if (e.NewValue == CheckState.Checked)
                                      {
                                          textView.KeyBindings.Add (keyTab, Command.Tab);
                                          textView.KeyBindings.Add (keyBackTab, Command.BackTab);
                                      }
                                      else
                                      {
                                          textView.KeyBindings.Remove (keyTab);
                                          textView.KeyBindings.Remove (keyBackTab);
                                      }

                                      textView.AllowsTab = e.NewValue == CheckState.Checked;
                                  };
        win.Add (chxCaptureTabs);

        // Hex editor
        label = new() { Text = "_HexView:", Y = Pos.Bottom (chxMultiline) + 1 };
        win.Add (label);

        var hexEditor =
            new HexView (
                         new MemoryStream (Encoding.UTF8.GetBytes ("HexEditor Unicode that shouldn't 𝔹Aℝ𝔽!"))
                        )
            {
                X = Pos.Right (label) + 1, Y = Pos.Bottom (chxMultiline) + 1, Width = Dim.Percent (50) - 1, Height = Dim.Percent (30),
            };
        win.Add (hexEditor);

        var labelMirroringHexEditor = new Label
        {
            X = Pos.Right (hexEditor) + 1,
            Y = Pos.Top (hexEditor),

            Width = Dim.Fill (1) - 1,
            Height = Dim.Height (hexEditor) - 1
        };
        byte [] array = ((MemoryStream)hexEditor.Source).ToArray ();
        labelMirroringHexEditor.Text = Encoding.UTF8.GetString (array, 0, array.Length);

        hexEditor.Edited += (s, kv) =>
                            {
                                hexEditor.ApplyEdits ();
                                byte [] array = ((MemoryStream)hexEditor.Source).ToArray ();
                                labelMirroringHexEditor.Text = Encoding.UTF8.GetString (array, 0, array.Length);
                            };
        win.Add (labelMirroringHexEditor);

        // DateField
        label = new() { Text = "_DateField:", Y = Pos.Bottom (hexEditor) + 1 };
        win.Add (label);

        var dateField = new DateField (DateTime.Now) { X = Pos.Right (label) + 1, Y = Pos.Bottom (hexEditor) + 1, Width = 20 };
        win.Add (dateField);

        var labelMirroringDateField = new Label
        {
            X = Pos.Right (dateField) + 1,
            Y = Pos.Top (dateField),

            Width = Dim.Width (dateField),
            Height = Dim.Height (dateField),
            Text = dateField.Text
        };
        win.Add (labelMirroringDateField);

        dateField.TextChanged += (s, prev) => { labelMirroringDateField.Text = dateField.Text; };

        // TimeField
        label = new() { Text = "T_imeField:", Y = Pos.Top (dateField), X = Pos.Right (labelMirroringDateField) + 5 };
        win.Add (label);

        _timeField = new()
        {
            X = Pos.Right (label) + 1,
            Y = Pos.Top (dateField),
            Width = 20,
            IsShortFormat = false,
            Time = DateTime.Now.TimeOfDay
        };
        win.Add (_timeField);

        _labelMirroringTimeField = new()
        {
            X = Pos.Right (_timeField) + 1,
            Y = Pos.Top (_timeField),

            Width = Dim.Width (_timeField),
            Height = Dim.Height (_timeField),
            Text = _timeField.Text
        };
        win.Add (_labelMirroringTimeField);

        _timeField.TimeChanged += TimeChanged;

        // MaskedTextProvider - uses .NET MaskedTextProvider
        var netProviderLabel = new Label
        {
            X = Pos.Left (dateField),
            Y = Pos.Bottom (dateField) + 1,
            Text = "_NetMaskedTextProvider [ 999 000 LLL >LLL |AAA aaa ]:"
        };
        win.Add (netProviderLabel);

        var netProvider = new NetMaskedTextProvider ("999 000 LLL >LLL |AAA aaa");

        var netProviderField = new TextValidateField
        {
            X = Pos.Right (netProviderLabel) + 1, Y = Pos.Y (netProviderLabel), Provider = netProvider
        };
        win.Add (netProviderField);

        var labelMirroringNetProviderField = new Label
        {
            X = Pos.Right (netProviderField) + 1,
            Y = Pos.Top (netProviderField),

            Width = Dim.Width (netProviderField),
            Height = Dim.Height (netProviderField),
            Text = netProviderField.Text
        };
        win.Add (labelMirroringNetProviderField);

        netProviderField.Provider.TextChanged += (s, prev) => { labelMirroringNetProviderField.Text = netProviderField.Text; };

        // TextRegexProvider - Regex provider implemented by Terminal.Gui
        var regexProvider = new Label
        {
            X = Pos.Left (netProviderLabel),
            Y = Pos.Bottom (netProviderLabel) + 1,
            Text = "Text_RegexProvider [ ^([0-9]?[0-9]?[0-9]|1000)$ ]:"
        };
        win.Add (regexProvider);

        var provider2 = new TextRegexProvider ("^([0-9]?[0-9]?[0-9]|1000)$") { ValidateOnInput = false };

        var regexProviderField = new TextValidateField
        {
            X = Pos.Right (regexProvider) + 1,
            Y = Pos.Y (regexProvider),
            Width = 30,
            TextAlignment = Alignment.Center,
            Provider = provider2
        };
        win.Add (regexProviderField);

        var labelMirroringRegexProviderField = new Label
        {
            X = Pos.Right (regexProviderField) + 1,
            Y = Pos.Top (regexProviderField),

            Width = Dim.Width (regexProviderField),
            Height = Dim.Height (regexProviderField),
            Text = regexProviderField.Text
        };
        win.Add (labelMirroringRegexProviderField);

        regexProviderField.Provider.TextChanged += (s, prev) => { labelMirroringRegexProviderField.Text = regexProviderField.Text; };

        var labelAppendAutocomplete = new Label
        {
            Y = Pos.Y (regexProviderField) + 2, X = 1, Text = "_Append Autocomplete:"
        };

        var appendAutocompleteTextField = new TextField
        {
            X = Pos.Right (labelAppendAutocomplete) + 1, Y = Pos.Top (labelAppendAutocomplete), Width = Dim.Fill ()
        };
        appendAutocompleteTextField.Autocomplete = new AppendAutocomplete (appendAutocompleteTextField);

        appendAutocompleteTextField.Autocomplete.SuggestionGenerator = new SingleWordSuggestionGenerator
        {
            AllSuggestions = new()
            {
                "fish",
                "flipper",
                "fin",
                "fun",
                "the",
                "at",
                "there",
                "some",
                "my",
                "of",
                "be",
                "use",
                "her",
                "than",
                "and",
                "this",
                "an",
                "would",
                "first",
                "have",
                "each",
                "make",
                "water",
                "to",
                "from",
                "which",
                "like",
                "been",
                "in",
                "or",
                "she",
                "him",
                "call",
                "is",
                "one",
                "do",
                "into",
                "who",
                "you",
                "had",
                "how",
                "time",
                "oil",
                "that",
                "by",
                "their",
                "has",
                "its",
                "it",
                "word",
                "if",
                "look",
                "now",
                "he",
                "but",
                "will",
                "two",
                "find",
                "was",
                "not",
                "up",
                "more",
                "long",
                "for",
                "what",
                "other",
                "write",
                "down",
                "on",
                "all",
                "about",
                "go",
                "day",
                "are",
                "were",
                "out",
                "see",
                "did",
                "as",
                "we",
                "many",
                "number",
                "get",
                "with",
                "when",
                "then",
                "no",
                "come",
                "his",
                "your",
                "them",
                "way",
                "made",
                "they",
                "can",
                "these",
                "could",
                "may",
                "said",
                "so",
                "people",
                "part"
            }
        };

        win.Add (labelAppendAutocomplete);
        win.Add (appendAutocompleteTextField);

        Label acceptView = new ()
        {
            X = Pos.Center (),
            Y = Pos.AnchorEnd (),
        };

        win.Add (acceptView);

        win.Accepting += WinOnAccept;

        Application.Run (win);
        win.Dispose ();
        Application.Shutdown ();

        return;

        void WinOnAccept (object sender, CommandEventArgs e)
        {
            e.Cancel = true; // Don't let it close

            acceptView.Text = $"Accept was Invoked via {win.Focused.GetType().Name}";

            // Start a task that will set acceptView.Text to an empty string after 1 second
            System.Threading.Tasks.Task.Run (async () =>
            {
                await System.Threading.Tasks.Task.Delay (1000);
                Application.Invoke (() => acceptView.Text = "");
            });
        }
    }


    private void TimeChanged (object sender, DateTimeEventArgs<TimeSpan> e) { _labelMirroringTimeField.Text = _timeField.Text; }
}
