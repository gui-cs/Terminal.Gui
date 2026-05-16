#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("ANSI StatusLine", "Demonstrates pushing text to the terminal status line while a full-screen app runs.")]
[ScenarioCategory ("Arrangement")]
public sealed class AnsiStatusLine : Scenario
{
    private IApplication? _app;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.Init ();
        _app = app;

        using Window appWindow = new () { Title = GetQuitKeyAndName () };
        appWindow.IsRunningChanged += AppWindowOnIsRunningChanged;

        Label description = new ()
        {
            X = 1,
            Y = 1,
            Text = "This full-screen app writes status text to the terminal title/status-line area."
        };
        appWindow.Add (description);

        Label capability = new ()
        {
            X = 1,
            Y = Pos.Bottom (description) + 1,
            Text = app.StatusLine.IsSupported ? "StatusLine output: available for this driver." : "StatusLine output: unavailable; calls are no-ops."
        };
        appWindow.Add (capability);

        Label prompt = new () { X = 1, Y = Pos.Bottom (capability) + 1, Text = "Text:" };
        appWindow.Add (prompt);

        TextField statusText = new ()
        {
            X = Pos.Right (prompt) + 1,
            Y = Pos.Top (prompt),
            Width = 50,
            Text = "Terminal.Gui status line"
        };
        appWindow.Add (statusText);

        Button updateButton = new ()
        {
            X = 1,
            Y = Pos.Bottom (statusText) + 1,
            Text = "_Update StatusLine"
        };
        updateButton.Accepting += (_, _) => app.StatusLine.SetText (statusText.Text, 2);
        appWindow.Add (updateButton);

        Button clearButton = new ()
        {
            X = Pos.Right (updateButton) + 2,
            Y = Pos.Top (updateButton),
            Text = "_Clear"
        };
        clearButton.Accepting += (_, _) => app.StatusLine.Clear ();
        appWindow.Add (clearButton);

        Label statusLineOnlyHint = new ()
        {
            X = 1,
            Y = Pos.Bottom (updateButton) + 2,
            Text = "Run Examples/AnsiStatusLineOnly to see an entire app render there."
        };
        appWindow.Add (statusLineOnlyHint);

        app.StatusLine.SetText (statusText.Text, 2);
        app.Run (appWindow);
    }

    private void AppWindowOnIsRunningChanged (object? sender, EventArgs<bool> args)
    {
        if (args.Value)
        {
            return;
        }

        _app?.StatusLine.Clear ();

        if (sender is Window appWindow)
        {
            appWindow.IsRunningChanged -= AppWindowOnIsRunningChanged;
        }
    }
}
