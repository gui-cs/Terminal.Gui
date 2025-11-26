#nullable enable

using System.Diagnostics;
using System.IO.Pipes;
using System.Reflection;
using System.Runtime.InteropServices;
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

        var button = new Button
        {
            X = Pos.Center (),
            Y = 1,
            Title = "_Open Child In Another Process"
        };

        button.Accepting += async (_, e) =>
                            {
                                // When Accepting is handled, set e.Handled to true to prevent further processing.
                                button.Enabled = false;
                                e.Handled = true;
                                label.Text = await OpenNewTerminalWindowAsync<string> ("EditName") ?? string.Empty;
                                button.Enabled = true;
                            };

        appWindow.Add (button, label);

        Application.Run (appWindow);
        appWindow.Dispose ();

        Application.Shutdown ();
    }

    public static async Task<T?> OpenNewTerminalWindowAsync<T> (string action)
    {
        var pipeName = "RunChildProcess";

        // Start named pipe server before launching child
        var server = new NamedPipeServerStream (pipeName, PipeDirection.In);

        // Launch external console process running UICatalog app again
        var p = new Process ();

        if (OperatingSystem.IsWindows ())
        {
            p.StartInfo.FileName = Environment.ProcessPath!;
            p.StartInfo.Arguments = $"{pipeName} --child --action \"{action}\"";
            p.StartInfo.UseShellExecute = true; // Needed so it opens a new terminal window
        }
        else
        {
            var executable = $"dotnet {Assembly.GetExecutingAssembly ().Location}";
            var arguments = $"{pipeName} --child --action \"{action}\"";
            UnixTerminalHelper.AdjustTerminalProcess (executable, arguments, p);
        }

        try
        {
            p.Start ();
        }
        catch (Exception ex)
        {
            // Catch any other unexpected exception
            Console.WriteLine ($@"Failed to launch terminal: {ex.Message}");

            return default (T?);
        }

        // Wait for connection from child
        await server.WaitForConnectionAsync ();

        using var reader = new StreamReader (server);
        string json = await reader.ReadToEndAsync ();

        return JsonSerializer.Deserialize<T> (json)!;
    }
}

public static class UnixTerminalHelper
{
    private static readonly string [] _knownTerminals =
    {
        // Linux
        "gnome-terminal",
        "konsole",
        "xfce4-terminal",
        "xterm",
        "lxterminal",
        "tilix",
        "mate-terminal",
        "alacritty",
        "terminator",

        // macOS
        "Terminal", "iTerm"
    };

    public static void AdjustTerminalProcess (string executable, string arguments, Process p)
    {
        var command = $"{executable} {arguments}";
        var escaped = $"{command.Replace ("\"", "\\\"")} && exit";
        string script;
        string? terminal = DetectTerminalProcess ();

        if (IsRunningOnWsl ())
        {
            terminal = "cmd.exe";
        }
        else if (terminal is null)
        {
            throw new InvalidOperationException (
                                                 "No supported terminal emulator found. Install gnome-terminal, xterm, konsole, etc.");
        }

        p.StartInfo.FileName = OperatingSystem.IsMacOS () ? "osascript" : terminal;
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardInput = false;
        p.StartInfo.RedirectStandardOutput = false;
        p.StartInfo.RedirectStandardError = false;

        // Use -- <command> <args> to avoid TTY reuse
        switch (terminal)
        {
            case "cmd.exe":
                p.StartInfo.ArgumentList.Add ("/c");
                p.StartInfo.ArgumentList.Add ($"start wsl {command}");

                break;
            case "Terminal":
                script = $"""
                          tell application "Terminal"
                              activate
                              do script "{escaped}"
                          end tell
                          """;

                p.StartInfo.ArgumentList.Add ("-e");
                p.StartInfo.ArgumentList.Add (script);

                break;
            case "iTerm":
                script = $"""

                          tell application "iTerm"
                            create window with default profile
                            tell current session of current window
                              write text "{escaped}"
                            end tell
                          end tell
                          """;

                p.StartInfo.ArgumentList.Add ("-e");
                p.StartInfo.ArgumentList.Add (script);

                break;
            case "gnome-terminal":
            case "tilix":
            case "mate-terminal":
                p.StartInfo.ArgumentList.Add ("--");
                p.StartInfo.ArgumentList.Add ("bash");
                p.StartInfo.ArgumentList.Add ("-c");
                p.StartInfo.ArgumentList.Add (command);

                break;
            case "konsole":
                p.StartInfo.ArgumentList.Add ("-e");
                p.StartInfo.ArgumentList.Add ($"bash -c \"{command}\"");

                break;
            case "xfce4-terminal":
            case "lxterminal":
                p.StartInfo.ArgumentList.Add ("--command");
                p.StartInfo.ArgumentList.Add ($"bash -c \"{command}\"");

                break;
            case "xterm":
                p.StartInfo.ArgumentList.Add ("-e");
                p.StartInfo.ArgumentList.Add ($"bash -c \"{command}\"");

                break;
            default:
                throw new NotSupportedException ($"Terminal detected but unsupported mapping: {terminal}");
        }
    }

    public static string? DetectTerminalProcess ()
    {
        int pid = Process.GetCurrentProcess ().Id;

        while (pid > 1)
        {
            int? ppid = GetParentProcessId (pid);

            if (ppid is null)
            {
                break;
            }

            try
            {
                var parent = Process.GetProcessById (ppid.Value);

                string? match = _knownTerminals
                    .FirstOrDefault (t => parent.ProcessName.Contains (t, StringComparison.OrdinalIgnoreCase));

                if (match is { })
                {
                    return match;
                }

                pid = parent.Id;
            }
            catch
            {
                break;
            }
        }

        return null; // unknown
    }

    public static bool IsRunningOnWsl ()
    {
        if (Environment.GetEnvironmentVariable ("WSL_DISTRO_NAME") != null)
        {
            return true;
        }

        if (File.Exists ("/proc/sys/kernel/osrelease")
            && File.ReadAllText ("/proc/sys/kernel/osrelease")
                   .Contains ("microsoft", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static int? GetParentPidUnix (int pid)
    {
        try
        {
            string output = Process.Start (
                                           new ProcessStartInfo
                                           {
                                               FileName = "ps",
                                               ArgumentList = { "-o", "ppid=", "-p", pid.ToString () },
                                               RedirectStandardOutput = true
                                           })!.StandardOutput.ReadToEnd ();

            return int.TryParse (output.Trim (), out int ppid) ? ppid : null;
        }
        catch
        {
            return null;
        }
    }

    private static int? GetParentProcessId (int pid)
    {
        if (!RuntimeInformation.IsOSPlatform (OSPlatform.Windows))
        {
            return GetParentPidUnix (pid);
        }

        return null;
    }
}
