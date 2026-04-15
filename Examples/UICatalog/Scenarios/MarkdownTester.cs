// ReSharper disable AccessToDisposedClosure
namespace UICatalog.Scenarios;

[ScenarioMetadata ("Markdown Tester", "Edit Markdown in a TextView and see it rendered in a MarkdownView.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Text and Formatting")]
public class MarkdownTester : Scenario
{
    public override void Main ()
    {
        using IApplication app = Application.Create ();
        app.Init ();

        Window window = new ()
        {
            Title = "Markdown Tester",
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            BorderStyle = LineStyle.None
        };

        // --- Source editor (top half) ---
        FrameView editorFrame = new ()
        {
            Title = "Markdown Source",
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Percent (40),
        };
        editorFrame.Border.Thickness = new Thickness (0, 2, 0, 0);

        TextView editor = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            TabKeyAddsTab = false,
            Text = MarkdownView.DefaultMarkdownSample
        };

        editorFrame.Add (editor);

        // --- Preview (bottom half) ---
        FrameView previewFrame = new ()
        {
            Title = "Rendered Preview",
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = Pos.Bottom (editorFrame),
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };
        previewFrame.Border.Thickness = new Thickness (0, 2, 0, 0);

        MarkdownView preview = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Markdown = MarkdownView.DefaultMarkdownSample,
            SyntaxHighlighter = new Terminal.Gui.SyntaxHighlighting.TextMateSyntaxHighlighter (TextMateSharp.Grammars.ThemeName.DarkPlus)
        };

        previewFrame.Add (preview);

        // Update preview when editor text changes
        editor.ContentsChanged += (_, _) =>
                                  {
                                      preview.Markdown = editor.Text;
                                  };

        window.Add (editorFrame, previewFrame);

        StatusBar statusBar = new ();

        Shortcut quitShortcut = new ()
        {
            Title = "Quit",
            Key = Key.Esc,
            Action = app.RequestStop
        };

        statusBar.Add (quitShortcut);
        window.Add (statusBar);

        app.Run (window);
        window.Dispose ();
    }
}
