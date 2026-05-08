#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Bracketed Paste", "Logs bracketed-paste payloads delivered by the terminal")]
[ScenarioCategory ("Input")]
public sealed class BracketedPasteDemo : Scenario
{
    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();

        using Window appWindow = new ()
        {
            Title = GetQuitKeyAndName ()
        };
        appWindow.AssignHotKeys = true;

        Label hint = new ()
        {
            X = 0,
            Y = 0,
            Text = "Paste anything from your system clipboard. Pastes show up as a single delivery if "
                   + "your terminal supports bracketed-paste mode (most modern terminals do)."
        };

        TextField field = new ()
        {
            X = 0,
            Y = Pos.Bottom (hint) + 1,
            Width = Dim.Fill (),
            Title = "Paste into the focused TextField — the default OnPasted inserts the text."
        };

        Label log = new ()
        {
            X = 0,
            Y = Pos.Bottom (field) + 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Text = "Application.Paste log:\n"
        };

        var counter = 0;

        app.Paste += (_, args) =>
                     {
                         counter++;
                         log.Text += $"[{counter}] {args.Text.Length} chars: {Truncate (args.Text)}\n";
                     };

        appWindow.Add (hint, field, log);

        app.Run (appWindow);
    }

    private static string Truncate (string text)
    {
        // Replace control chars so the display stays one line per paste.
        string flattened = text.Replace ("\r", "\\r").Replace ("\n", "\\n").Replace ("\t", "\\t");

        if (flattened.Length <= 60)
        {
            return flattened;
        }

        return flattened [..60] + "…";
    }
}
