#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Markdown", "Demonstrates MarkdownView read-only rendering.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
public class Markdown : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        Window window = new ()
        {
            Title = GetName (),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        MarkdownView markdownView = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        markdownView.Markdown =
            """
            # MarkdownView

            This is a **read-only** markdown renderer with *wrapping*.

            Visit [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui).

            > Block quotes are supported.

            - [x] Implement parser + layout + draw
            - [ ] Add more syntax highlighting

            | Column | Value |
            |--------|-------|
            | A      | 1     |
            | B      | 2     |

            ```csharp
            Console.WriteLine ("Code blocks are horizontal-scrollable");
            ```
            """;

        Shortcut status = new (Key.Empty, "", null);
        StatusBar statusBar = new ([new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", () => window.RequestStop ()), status]);

        markdownView.LinkClicked += (_, e) => status.Title = e.Url;

        window.Add (markdownView, statusBar);

        app.Run (window);
    }
}
