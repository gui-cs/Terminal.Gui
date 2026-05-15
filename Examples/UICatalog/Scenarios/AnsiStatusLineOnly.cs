#nullable enable
namespace UICatalog.Scenarios;

[ScenarioMetadata ("ANSI StatusLine Only", "Demonstrates running an entire Terminal.Gui app in the terminal status line.")]
[ScenarioCategory ("Arrangement")]
public sealed class AnsiStatusLineOnly : Scenario
{
    private IApplication? _app;
    private Label? _label;
    private Timer? _timer;
    private int _tick;

    public override void Main ()
    {
        ConfigurationManager.Enable (ConfigLocations.All);

        using IApplication app = Application.Create ();
        app.AppModel = AppModel.StatusLine;
        app.Init ();
        _app = app;

        using Runnable runnable = new () { Width = Dim.Fill (), Height = 1 };
        runnable.IsRunningChanged += RunnableOnIsRunningChanged;

        _label = new Label { X = 0, Y = 0, Text = BuildStatusText () };
        runnable.Add (_label);

        _timer = new Timer (_ => _app?.Invoke (UpdateStatusText), null, 0, 1000);

        app.Run (runnable);
    }

    private string BuildStatusText () => $"StatusLine-only demo tick {_tick} - press Esc to quit";

    private void UpdateStatusText ()
    {
        _tick++;

        if (_label is null)
        {
            return;
        }

        _label.Text = BuildStatusText ();
    }

    private void RunnableOnIsRunningChanged (object? sender, EventArgs<bool> args)
    {
        if (args.Value)
        {
            return;
        }

        _timer?.Dispose ();
        _timer = null;
        _app?.StatusLine.Clear ();

        if (sender is Runnable runnable)
        {
            runnable.IsRunningChanged -= RunnableOnIsRunningChanged;
        }
    }
}
