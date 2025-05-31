using System.Diagnostics;

namespace Terminal.Gui.App;

/// <summary>Provides cut, copy, and paste support for the OS clipboard.</summary>
/// <remarks>
///     <para>On Windows, the <see cref="Clipboard"/> class uses the Windows Clipboard APIs via P/Invoke.</para>
///     <para>
///         On Linux, when not running under Windows Subsystem for Linux (WSL), the <see cref="Clipboard"/> class uses
///         the xclip command line tool. If xclip is not installed, the clipboard will not work.
///     </para>
///     <para>
///         On Linux, when running under Windows Subsystem for Linux (WSL), the <see cref="Clipboard"/> class launches
///         Windows' powershell.exe via WSL interop and uses the "Set-Clipboard" and "Get-Clipboard" Powershell CmdLets.
///     </para>
///     <para>
///         On the Mac, the <see cref="Clipboard"/> class uses the MacO OS X pbcopy and pbpaste command line tools and
///         the Mac clipboard APIs vai P/Invoke.
///     </para>
/// </remarks>
public static class Clipboard
{
    private static string _contents = string.Empty;

    /// <summary>Gets (copies from) or sets (pastes to) the contents of the OS clipboard.</summary>
    public static string Contents
    {
        get
        {
            try
            {
                if (IsSupported)
                {
                    string clipData = Application.Driver?.Clipboard.GetClipboardData ();

                    if (clipData is null)
                    {
                        // throw new InvalidOperationException ($"{Application.Driver?.GetType ().Name}.GetClipboardData returned null instead of string.Empty");
                        clipData = string.Empty;
                    }

                    _contents = clipData;
                }
            }
            catch (Exception)
            {
                _contents = string.Empty;
            }

            return _contents;
        }
        set
        {
            try
            {
                if (IsSupported)
                {
                    if (value is null)
                    {
                        value = string.Empty;
                    }

                    Application.Driver?.Clipboard.SetClipboardData (value);
                }

                _contents = value;
            }
            catch (Exception)
            {
                _contents = value;
            }
        }
    }

    /// <summary>Returns true if the environmental dependencies are in place to interact with the OS clipboard.</summary>
    /// <remarks></remarks>
    public static bool IsSupported => Application.Driver?.Clipboard.IsSupported ?? false;

    /// <summary>Copies the _contents of the OS clipboard to <paramref name="result"/> if possible.</summary>
    /// <param name="result">The _contents of the OS clipboard if successful, <see cref="string.Empty"/> if not.</param>
    /// <returns><see langword="true"/> the OS clipboard was retrieved, <see langword="false"/> otherwise.</returns>
    public static bool TryGetClipboardData (out string result)
    {
        if (IsSupported && Application.Driver!.Clipboard.TryGetClipboardData (out result))
        {
            _contents = result;

            return true;
        }

        result = string.Empty;

        return false;
    }

    /// <summary>Pastes the <paramref name="text"/> to the OS clipboard if possible.</summary>
    /// <param name="text">The text to paste to the OS clipboard.</param>
    /// <returns><see langword="true"/> the OS clipboard was set, <see langword="false"/> otherwise.</returns>
    public static bool TrySetClipboardData (string text)
    {
        if (IsSupported && Application.Driver!.Clipboard.TrySetClipboardData (text))
        {
            _contents = text;

            return true;
        }

        return false;
    }
}

/// <summary>
///     Helper class for console drivers to invoke shell commands to interact with the clipboard. Used primarily by
///     CursesDriver, but also used in Unit tests which is why it is in IConsoleDriver.cs.
/// </summary>
internal static class ClipboardProcessRunner
{
    public static (int exitCode, string result) Bash (
        string commandLine,
        string inputText = "",
        bool waitForOutput = false
    )
    {
        var arguments = $"-c \"{commandLine}\"";
        (int exitCode, string result) = Process ("bash", arguments, inputText, waitForOutput);

        return (exitCode, result.TrimEnd ());
    }

    public static bool DoubleWaitForExit (this Process process)
    {
        bool result = process.WaitForExit (500);

        if (result)
        {
            process.WaitForExit ();
        }

        return result;
    }

    public static bool FileExists (this string value) { return !string.IsNullOrEmpty (value) && !value.Contains ("not found"); }

    public static (int exitCode, string result) Process (
        string cmd,
        string arguments,
        string input = null,
        bool waitForOutput = true
    )
    {
            var output = string.Empty;

        using (var process = new Process
               {
                   StartInfo = new()
                   {
                       FileName = cmd,
                       Arguments = arguments,
                       RedirectStandardOutput = true,
                       RedirectStandardError = true,
                       RedirectStandardInput = true,
                       UseShellExecute = false,
                       CreateNoWindow = true
                   }
               })
        {
            TaskCompletionSource<bool> eventHandled = new ();
            process.Start ();

            if (!string.IsNullOrEmpty (input))
            {
                process.StandardInput.Write (input);
                process.StandardInput.Close ();
            }

            if (!process.WaitForExit (5000))
            {
                var timeoutError =
                    $@"Process timed out. Command line: {process.StartInfo.FileName} {process.StartInfo.Arguments}.";

                throw new TimeoutException (timeoutError);
            }

            if (waitForOutput && process.StandardOutput.Peek () != -1)
            {
                output = process.StandardOutput.ReadToEnd ();
            }

            if (process.ExitCode > 0)
            {
                output = $@"Process failed to run. Command line: {cmd} {arguments}.
										Output: {output}
										Error: {process.StandardError.ReadToEnd ()}";
            }

            return (process.ExitCode, output);
        }
    }
}
