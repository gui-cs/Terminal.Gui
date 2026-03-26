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

        _appWindow = new Window { Title = GetName (), BorderStyle = LineStyle.None };

        Label titleLabel = new () { Text = "_Title:", X = 1, Y = 1 };
        _appWindow.Add (titleLabel);

        TextField titleTextField = new () { X = Pos.Right (titleLabel) + 1, Y = Pos.Top (titleLabel), Width = Dim.Fill () };
        _appWindow.Add (titleTextField);

        Label textLabel = new () { Text = " Te_xt:", X = Pos.Left (titleLabel), Y = Pos.Bottom(titleLabel) };
        _appWindow.Add (textLabel);

        TextField textTextField = new () { X = Pos.Right (textLabel) + 1, Y = Pos.Top(textLabel), Width = Dim.Fill () };
        _appWindow.Add (textTextField);

        Label urlLabel = new () { Text = "  _Url:", X = 1, Y = Pos.Bottom (titleTextField) + 1 };
        _appWindow.Add (urlLabel);

        TextField urlTextField = new () { X = Pos.Right (urlLabel) + 1, Y = Pos.Bottom (titleTextField) + 1, Width = Dim.Fill () };
        _appWindow.Add (urlTextField);

        Label simpleUrlLabel = new () { X = 1, Y = Pos.Bottom (urlTextField) + 2 };
        _appWindow.Add (simpleUrlLabel);

        FrameView linkFrame = new ()
        {
            Title = "_Link Demo",
            X = 0,
            Y = Pos.Bottom (simpleUrlLabel) + 2,
            Width = Dim.Fill(),
            Height = Dim.Auto (),
            AssignHotKeys = true,
            TabStop = TabBehavior.TabStop
        };

        _link = new Link { X = 1, Y = 1, BorderStyle = LineStyle.Dotted };

        _link.TextChanged += (s, e) => simpleUrlLabel.Text = $"This is just a Label with a URL in Text (WT automatically enables URLs) - {_link.Text}";
        titleTextField.ValueChanged += (s, e) => _link.Title = e.NewValue ?? string.Empty;
        textTextField.ValueChanged += (s, e) => _link.Text = e.NewValue ?? string.Empty;
        urlTextField.ValueChanged += (s, e) => _link.Url = e.NewValue ?? string.Empty;
        linkFrame.Add (_link);

        titleTextField.Text = "Title";
        textTextField.Text = "GitHub repo";
        urlTextField.Text = "https://github.com/gui-cs/Terminal.Gui";

        Button copyButton = new () { Title = "_Copy", X = Pos.Center (), Y = Pos.AnchorEnd () };
        copyButton.Accepting += (s, e) => _link.Copy ();

        linkFrame.Add (copyButton);

        _appWindow.Add (linkFrame);

        // StatusBar
        Shortcut urlIndicator = new (Key.Empty, "", null);

        StatusBar statusBar = new ([new Shortcut (Application.GetDefaultKey (Command.Quit), "Quit", Quit), urlIndicator]);
        _link.MouseEnter += (s, e) => urlIndicator.Title = _link.Url;
        _link.MouseLeave += (s, e) => urlIndicator.Title = "";
        _appWindow.Add (statusBar);

        _app.Run (_appWindow);
        _appWindow.Dispose ();
    }

    private void Quit () => _appWindow?.RequestStop ();
}
