#nullable enable
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

internal sealed class StatusLineDemo : Runnable
{
    private Label? _label;
    private Timer? _timer;
    private int _tick;

    public StatusLineDemo ()
    {
        Width = Dim.Fill ();
        Height = 1;

        _label = new () { X = 0, Y = 0, Text = BuildStatusText () };
        Add (_label);
    }

    private string BuildStatusText () => $"StatusLine-only demo tick {_tick} - press Esc to quit";

    protected override void OnIsRunningChanged (bool newIsRunning)
    {
        base.OnIsRunningChanged (newIsRunning);

        if (newIsRunning)
        {
            _timer = new Timer (_ => App?.Invoke (UpdateStatusText), null, 0, 1000);

            return;
        }

        _timer?.Dispose ();
        _timer = null;
        App?.StatusLine.Clear ();
    }

    private void UpdateStatusText ()
    {
        _tick++;

        if (_label is null)
        {
            return;
        }

        _label.Text = BuildStatusText ();
    }
}
