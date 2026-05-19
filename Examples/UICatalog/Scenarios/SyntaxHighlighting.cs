#nullable enable
using Terminal.Gui.Document;
using Terminal.Gui.Editor;
using Terminal.Gui.Highlighting;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Syntax Highlighting", "Text editor with keyword highlighting using the Editor control.")]
[ScenarioCategory ("Text and Formatting")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Editor")]
public class SyntaxHighlighting : Scenario
{
    private Runnable? _appWindow;
    private Editor? _editor;

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
                                   "_Editor",
                                   [
                                       wrapMenuItem,
                                       new Line (),
                                       new MenuItem { Title = Strings.cmdQuit, Action = Quit }
                                   ]
                                  )
                 );

        _appWindow.Add (menu);

        _editor = new ()
        {
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill (1),
            GutterOptions = GutterOptions.LineNumbers
        };

        ApplySyntaxHighlighting ();

        _appWindow.Add (_editor);

        StatusBar statusBar = new ([new (Application.GetDefaultKey (Command.Quit), "Quit", Quit)]);

        _appWindow.Add (statusBar);

        // Run - Start the application.
        app.Run (_appWindow);
    }

    private void ApplySyntaxHighlighting ()
    {
        if (_editor is null)
        {
            return;
        }

        // Editor handles syntax highlighting natively via HighlightingDefinition.
        _editor.HighlightingDefinition = HighlightingManager.Instance.GetDefinition ("TSQL");

        _editor.Document = new TextDocument (
                                             "/*Query to select:\nLots of data*/\nSELECT TOP 100 * \nfrom\n MyDb.dbo.Biochemistry where TestCode = 'blah';"
                                            );
    }

    private MenuItem CreateWordWrapMenuItem ()
    {
        CheckBox checkBox = new ()
        {
            Title = "_Word Wrap",
            Value = _editor?.WordWrap == true ? CheckState.Checked : CheckState.UnChecked
        };

        checkBox.ValueChanged += (_, _) =>
                                 {
                                     if (_editor is not null)
                                     {
                                         _editor.WordWrap = checkBox.Value == CheckState.Checked;
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

    private void Quit () { _appWindow?.RequestStop (); }
}
