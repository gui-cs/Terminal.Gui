#nullable enable

using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Text.Json;

namespace UICatalog.Scenarios;

[ScenarioMetadata ("OpenChildInAnotherProcess", "Open Child In Another Process")]
[ScenarioCategory ("Application")]
public sealed class OpenChildInAnotherProcess : Scenario
{
    public override void Main ()
    {
        // Only work with legacy
        Application.Init ();

        // Setup - Create a top-level application window and configure it.
        Window appWindow = new ()
        {
            Title = GetQuitKeyAndName (),
            BorderStyle = LineStyle.None
        };

        var label = new Label { X = Pos.Center (), Y = 3 };

        var button = new Button ()
        {
            X = Pos.Center (),
            Y = 1,
            Title = "_Open Child In Another Process",
        };

        button.Accepting += async (_, e) =>
                            {
                                // When Accepting is handled, set e.Handled to true to prevent further processing.
                                button.Enabled = false;
                                e.Handled = true;
                                label.Text = await OpenNewTerminalWindowAsync<string> ("EditName");
                                button.Enabled = true;
                            };

        appWindow.Add (button, label);

        Application.Run (appWindow);
        appWindow.Dispose ();

        Application.Shutdown ();
    }

    public async Task<T> OpenNewTerminalWindowAsync<T> (string action)
    {
        string pipeName = "RunChildProcess";

        // Start named pipe server before launching child
        var server = new NamedPipeServerStream (pipeName, PipeDirection.In);

        // Launch external console process running UICatalog app again
        var p = new Process ();

        if (OperatingSystem.IsWindows ())
        {
            p.StartInfo.FileName = Environment.ProcessPath!;
            p.StartInfo.Arguments = $"{pipeName} --child --action \"{action}\"";
            p.StartInfo.UseShellExecute = true;     // Needed so it opens a new terminal window
        }
        else
        {
            p.StartInfo.FileName = "gnome-terminal";
            // Use -- <command> <args> to avoid TTY reuse
            p.StartInfo.ArgumentList.Add ("--");
            p.StartInfo.ArgumentList.Add ("bash");
            p.StartInfo.ArgumentList.Add ("-c");
            p.StartInfo.ArgumentList.Add ($"dotnet {Assembly.GetExecutingAssembly ().Location} {pipeName} --child --action \"{action}\"");
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = false;
            p.StartInfo.RedirectStandardOutput = false;
            p.StartInfo.RedirectStandardError = false;
        }

        p.Start ();

        // Wait for connection from child
        await server.WaitForConnectionAsync ();

        using var reader = new StreamReader (server);
        string json = await reader.ReadToEndAsync ();

        return JsonSerializer.Deserialize<T> (json)!;
    }
}
