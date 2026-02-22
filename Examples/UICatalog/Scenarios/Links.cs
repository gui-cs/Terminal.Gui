#nullable enable

namespace UICatalog.Scenarios;

[ScenarioMetadata ("Links", "Demonstrates how Links work.")]
[ScenarioCategory ("Controls")]
[ScenarioCategory ("Mouse and Keyboard")]
public class Links : Scenario
{
    private IApplication? _app;
    private Window? _appWindow;
    private Link? _link;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);
        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        _appWindow = new ()
        {
            Title = GetName (),
            BorderStyle = LineStyle.None
        };

        Label textLabel = new ()
        {
            Text = "_Text:",
            X = 1,
            Y = 1
        };
        _appWindow.Add (textLabel);

        TextField textField = new ()
        {
            X = Pos.Right (textLabel) + 2,
            Y = 1,
            Width = 20
        };
        _appWindow.Add (textField);

        Label urlLabel = new ()
        {
            Text = "_Url:",
            X = 1,
            Y = Pos.Bottom (textField) + 1
        };
        _appWindow.Add (urlLabel);

        TextField urlField = new ()
        {
            X = Pos.Right (urlLabel) + 2,
            Y = Pos.Bottom (textField) + 1,
            Width = 64
        };
        _appWindow.Add (urlField);

        Label simpleUrlLabel = new ()
        {
            X = 1,
            Y = Pos.Bottom (urlField) + 2
        };
        _appWindow.Add (simpleUrlLabel);

        FrameView linkFrame = new ()
        {
            Title = "_Link rendering",
            X = 0,
            Y = Pos.Bottom (simpleUrlLabel) + 2,
            Width = 64,
            Height = 8,
            AssignHotKeys = true
        };

        _link = new ()
        {
            X = 1,
            Y = 1,
            Height = 1,
            Width = 64
        };

        _link.UrlChanged += (s, e) => simpleUrlLabel.Text = _link.Url;
        textField.ValueChanged += (s, e) => _link.Text = e.NewValue ?? _link.Url;
        urlField.ValueChanged += (s, e) => _link.Url = e.NewValue ?? Link.DEFAULT_URL;
        linkFrame.Add (_link);

        textField.Text = "GitHub repo";
        urlField.Text = "https://github.com/gui-cs/Terminal.Gui";

        Button copyButton = new ()
        {
            Title = "_Copy",
            X = Pos.Center (),
            Y = Pos.Bottom (_link) + 2,
            
        };
        copyButton.Accepting += (s, e) => _link.Copy ();

        linkFrame.Add (copyButton);

        _appWindow.Add (linkFrame);

        // StatusBar
        Shortcut urlIndicator = new (Key.Empty, "", null);

        StatusBar statusBar = new ([
            new (Application.QuitKey, "Quit", Quit),
            urlIndicator
        ]);
        _link.MouseEnter += (s, e) => urlIndicator.Title = _link.Url;
        _link.MouseLeave += (s, e) => urlIndicator.Title = "";
        _appWindow.Add (statusBar);

        _app.Run (_appWindow);
        _appWindow.Dispose ();
    }

    private void Quit () { _appWindow?.RequestStop (); }
}
