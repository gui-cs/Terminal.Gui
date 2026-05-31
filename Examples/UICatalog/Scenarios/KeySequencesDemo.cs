using System.Collections.ObjectModel;
using Terminal.Gui.Editor;
using Terminal.Gui.KeySequences;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Key Sequences", "Demonstrates leader-key command sequences.")]
[ScenarioCategory ("Mouse and Keyboard")]
[ScenarioCategory ("Text and Formatting")]
public sealed class KeySequencesDemo : Scenario
{
    private const string Leader = ";";

    private const string SampleCode =
        """
        using Terminal.Gui.App;
        using Terminal.Gui.Views;

        namespace Demo;

        public sealed class MainWindow : Window
        {
            private readonly List<string> _items =
            [
                "alpha",
                "beta",
                "gamma"
            ];

            public MainWindow ()
            {
                Title = "Key sequence buffer";

                Label label = new ()
                {
                    Text = "Try ; 4 j, ; d d, ; 2 y y, ; p, ; u",
                    X = 1,
                    Y = 1
                };

                Button button = new ()
                {
                    Text = "Run",
                    X = Pos.Center (),
                    Y = Pos.Bottom (label) + 1
                };

                button.Accepted += (_, _) => MessageBox.Query (App!, "Done", Render (), "OK");

                Add (label, button);
            }

            private string Render ()
            {
                if (_items.Count == 0)
                {
                    return "No items.";
                }

                return string.Join (", ", _items);
            }
        }
        """;

    private readonly ObservableCollection<string> _log = [];

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window window = new ()
        {
            Title = GetQuitKeyAndName ()
        };

        Label title = new ()
        {
            Text = "Leader key: ;",
            X = 0,
            Y = 0
        };

        Label commands = new ()
        {
            Text = "; h/j/k/l  ; w/b  ; 0/$  ; g g/G  ; d d  ; y y  ; p  ; u  ; Ctrl+R  ; v motion",
            X = 0,
            Y = Pos.Bottom (title)
        };

        Editor editor = new ()
        {
            X = 0,
            Y = Pos.Bottom (commands) + 1,
            Width = Dim.Percent (68),
            Height = Dim.Fill (),
            Text = SampleCode,
            WordWrap = false,
            ConvertTabsToSpaces = true,
            IndentationSize = 4
        };
        editor.BorderStyle = LineStyle.Single;
        editor.Title = "Editor";

        ListView logView = new ()
        {
            X = Pos.Right (editor) + 1,
            Y = Pos.Top (editor),
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Source = new ListWrapper<string> (_log)
        };
        logView.BorderStyle = LineStyle.Single;
        logView.Title = "Vim-ish Sequences";

        window.Add (title, commands, editor, logView);

        AddLog ("Focus is on the editor.");
        AddLog ("Motions: ; h/j/k/l, ; 4 j, ; w, ; b, ; 0, ; $, ; g g, ; G, ; Ctrl+B/F");
        AddLog ("Selection: ; v h/j/k/l/w/b/0/$/g g/G/Ctrl+B/Ctrl+F");
        AddLog ("Edit: ; d d, ; 3 d d, ; y y, ; v y, ; v c, ; p, ; x, ; X, ; u, ; Ctrl+R");

        using IDisposable registration = editor.UseKeySequences (
            bindings =>
            {
                bindings.Timeout = TimeSpan.FromSeconds (5);

                AddMotionBindings (bindings, editor);
                AddSelectionBindings (bindings, editor);
                AddEditingBindings (bindings, editor);

                bindings.StateChanged += (_, e) =>
                {
                    if (e.Result == KeySequenceResult.Started)
                    {
                        AddLog ("Sequence started");
                        return;
                    }

                    if (e.Result == KeySequenceResult.Pending)
                    {
                        AddLog ($"Sequence pending: {FormatKeys (e.Keys)}");
                        return;
                    }

                    if (e.Result == KeySequenceResult.Canceled)
                    {
                        AddLog ("Sequence canceled");
                        return;
                    }

                    if (e.Result == KeySequenceResult.Rejected)
                    {
                        AddLog ("Sequence rejected");
                    }
                };
            },
            KeySequenceInterceptionMode.Preemptive);

        editor.SetFocus ();
        app.Run (window);
    }

    private void AddMotionBindings (KeySequenceBindings bindings, Editor editor)
    {
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> h", Command.Left, "h");
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> l", Command.Right, "l");
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> k", Command.Up, "k");
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> j", Command.Down, "j");
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> b", Command.WordLeft, "b");
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> w", Command.WordRight, "w");
        AddImmediateCommand (bindings, editor, '0', Command.LeftStart, "0");
        AddCommand (bindings, editor, $"{Leader} $", Command.RightEnd, "$");
        AddCommand (bindings, editor, $"{Leader} g g", Command.Start, "g g");
        AddCommand (bindings, editor, $"{Leader} G", Command.End, "G");
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> Ctrl+B", Command.PageUp, "Ctrl+B");
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> Ctrl+F", Command.PageDown, "Ctrl+F");
    }

    private void AddSelectionBindings (KeySequenceBindings bindings, Editor editor)
    {
        AddRepeatedCommand (bindings, editor, $"{Leader} v <count> h", Command.LeftExtend, "v h");
        AddRepeatedCommand (bindings, editor, $"{Leader} v <count> l", Command.RightExtend, "v l");
        AddRepeatedCommand (bindings, editor, $"{Leader} v <count> k", Command.UpExtend, "v k");
        AddRepeatedCommand (bindings, editor, $"{Leader} v <count> j", Command.DownExtend, "v j");
        AddRepeatedCommand (bindings, editor, $"{Leader} v <count> b", Command.WordLeftExtend, "v b");
        AddRepeatedCommand (bindings, editor, $"{Leader} v <count> w", Command.WordRightExtend, "v w");
        AddCommand (bindings, editor, $"{Leader} v 0", Command.LeftStartExtend, "v 0");
        AddCommand (bindings, editor, $"{Leader} v $", Command.RightEndExtend, "v $");
        AddCommand (bindings, editor, $"{Leader} v g g", Command.StartExtend, "v g g");
        AddCommand (bindings, editor, $"{Leader} v G", Command.EndExtend, "v G");
        AddRepeatedCommand (bindings, editor, $"{Leader} v <count> Ctrl+B", Command.PageUpExtend, "v Ctrl+B");
        AddRepeatedCommand (bindings, editor, $"{Leader} v <count> Ctrl+F", Command.PageDownExtend, "v Ctrl+F");
    }

    private void AddEditingBindings (KeySequenceBindings bindings, Editor editor)
    {
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> x", Command.DeleteCharRight, "x");
        AddRepeatedCommand (bindings, editor, $"{Leader} <count> X", Command.DeleteCharLeft, "X");
        AddCommand (bindings, editor, $"{Leader} p", Command.Paste, "p");
        AddCommand (bindings, editor, $"{Leader} u", Command.Undo, "u");
        AddCommand (bindings, editor, $"{Leader} Ctrl+R", Command.Redo, "Ctrl+R");
        AddCommand (bindings, editor, $"{Leader} a", Command.SelectAll, "a");
        AddCommand (bindings, editor, $"{Leader} c", Command.Cut, "c");
        AddCommand (bindings, editor, $"{Leader} v y", Command.Copy, "v y");
        AddCommand (bindings, editor, $"{Leader} v c", Command.Cut, "v c");
        AddCountedLineCommand (bindings, editor, $"{Leader} <count> d d", Command.Cut, "d d");
        AddCountedLineCommand (bindings, editor, $"{Leader} <count> y y", Command.Copy, "y y");
    }

    private void AddRepeatedCommand (KeySequenceBindings bindings, Editor editor, string pattern, Command command, string display)
    {
        bindings.Add (pattern, context =>
        {
            InvokeRepeated (editor, command, context.Count);
            AddLog ($"{FormatSequence (context)} -> {display} x{context.Count}");
            return true;
        });
    }

    private void AddCommand (KeySequenceBindings bindings, Editor editor, string pattern, Command command, string display)
    {
        bindings.Add (pattern, context =>
        {
            editor.InvokeCommand (command);
            AddLog ($"{FormatSequence (context)} -> {display}");
            return true;
        });
    }

    private void AddImmediateCommand (KeySequenceBindings bindings, Editor editor, char key, Command command, string display)
    {
        KeySequencePattern pattern = KeySequencePattern.Leader (Leader [0]).Then (key);
        pattern.MatchMode = KeySequenceMatchMode.Immediate;
        pattern.AllowZeroCount = true;

        bindings.Add (pattern, context =>
        {
            editor.InvokeCommand (command);
            AddLog ($"{FormatSequence (context)} -> {display}");
            return true;
        });
    }

    private void AddCountedLineCommand (KeySequenceBindings bindings, Editor editor, string pattern, Command command, string display)
    {
        bindings.Add (pattern, context =>
        {
            editor.InvokeCommand (Command.LeftStart);
            InvokeRepeated (editor, Command.DownExtend, context.Count);
            editor.InvokeCommand (command);
            AddLog ($"{FormatSequence (context)} -> {display} x{context.Count}");
            return true;
        });
    }

    private static void InvokeRepeated (Editor editor, Command command, int count)
    {
        int repeatCount = Math.Max (1, count);

        for (int i = 0; i < repeatCount; i++)
        {
            editor.InvokeCommand (command);
        }
    }

    private static string FormatKeys (IReadOnlyList<Key> keys)
    {
        if (keys.Count == 0)
        {
            return "";
        }

        return string.Join (" ", keys.Select (key => key.ToString ()));
    }

    private static string FormatSequence (KeySequenceContext context) => $"{context.LeaderKey} {FormatKeys (context.Keys)}";

    private void AddLog (string message)
    {
        _log.Add ($"{DateTime.Now:T}  {message}");

        while (_log.Count > 100)
        {
            _log.RemoveAt (0);
        }
    }
}
