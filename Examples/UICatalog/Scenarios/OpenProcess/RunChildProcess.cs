#nullable enable

using System.IO.Pipes;
using System.Text.Json;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("RunChildProcess", "Run Child Process from Open Child In Another Process")]
[ScenarioCategory ("Application")]
public sealed class RunChildProcess : Scenario
{
    /// <inheritdoc />
    public override void Main ()
    {
        IApplication app = Application.Create ();
        app.Init ();
        app.Run ();
        app.TopRunnable?.Dispose ();
        app.Shutdown ();
    }

    public static async Task RunChildAsync (string pipeName, string action)
    {
        // Run your Terminal.Gui UI
        object result = await RunMyDialogAsync (action);

        // Send result back
        await using var client = new NamedPipeClientStream (".", pipeName, PipeDirection.Out);
        await client.ConnectAsync ();

        string json = JsonSerializer.Serialize (result);
        await using var writer = new StreamWriter (client);
        await writer.WriteAsync (json);
        await writer.FlushAsync ();
    }

    public static Task<string> RunMyDialogAsync (string action)
    {
        TaskCompletionSource<string> tcs = new ();
        string? result = null;

        IApplication app = Application.Create ();

        app.Init ();

        var win = new Window ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill (),
            Title = $"Child Window: {action}"
        };

        var input = new TextField
        {
            X = 1,
            Y = 1,
            Width = 30
        };

        var ok = new Button
        {
            X = 1,
            Y = 3,
            Text = "Ok",
            IsDefault = true
        };
        ok.Accepting += (_, e) =>
                      {
                          result = input.Text;
                          app.RequestStop ();
                          e.Handled = true;
                      };

        win.Add (input, ok);

        app.Run (win);
        win.Dispose ();
        app.Shutdown ();

        tcs.SetResult (result ?? string.Empty);

        return tcs.Task;
    }
}
