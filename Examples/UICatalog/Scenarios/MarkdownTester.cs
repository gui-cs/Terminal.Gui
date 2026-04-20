// ReSharper disable AccessToDisposedClosure

using TextMateSharp.Grammars;

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
            BorderStyle = LineStyle.None,
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Accent)
        };

        // --- Source editor (top half) ---
        FrameView editorFrame = new ()
        {
            Title = "Markdown Source",
            TabStop = TabBehavior.TabStop,
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Percent (40)
        };
        editorFrame.Border.Thickness = new Thickness (0, 2, 0, 0);

        TextView editor = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            TabKeyAddsTab = false,
            Text = Markdown.DefaultMarkdownSample
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

        Markdown preview = new ()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            SyntaxHighlighter = new TextMateSyntaxHighlighter (ThemeName.Abbys)
        };

        previewFrame.Add (preview);

        // Update preview when editor text changes
        editor.ContentsChanged += (_, _) => { preview.Text = editor.Text; };

        window.Add (editorFrame, previewFrame);

        StatusBar statusBar = new ();

        Shortcut quitShortcut = new () { Title = "Quit", Key = Key.Esc, Action = app.RequestStop };

        DropDownList<ThemeName> themeDropDown = new () { Value = ThemeName.Abbys, ReadOnly = true, CanFocus = false };

        themeDropDown.ValueChanged += (_, e) =>
                                      {
                                          if (e.Value is not { } themeName)
                                          {
                                              return;
                                          }
                                          preview.SyntaxHighlighter = new TextMateSyntaxHighlighter (themeName);
                                          preview.Text = editor.Text;
                                      };

        Shortcut themeShortcut = new () { Title = "Theme", CommandView = themeDropDown };

        CheckBox themeBgCheckBox = new () { Text = "Theme _BG", Value = preview.UseThemeBackground ? CheckState.Checked : CheckState.UnChecked };

        themeBgCheckBox.ValueChanged += (_, e) => { preview.UseThemeBackground = e.NewValue == CheckState.Checked; };

        Shortcut themeBgShortcut = new () { CommandView = themeBgCheckBox };

        statusBar.Add (themeShortcut, themeBgShortcut, quitShortcut);
        window.Add (statusBar);

        preview.Text = editor.Text;

        app.Run (window);
        window.Dispose ();
    }
}
