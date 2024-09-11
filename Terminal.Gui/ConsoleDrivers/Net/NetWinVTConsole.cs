#nullable enable
using System.Runtime.Versioning;

namespace Terminal.Gui.ConsoleDrivers.Net;

using System.Diagnostics.CodeAnalysis;
using Windows.Interop;
using Resources;
using static Windows.Interop.PInvoke;
using static Windows.Interop.CONSOLE_MODE;
using static System.Runtime.InteropServices.Marshal;

[MustDisposeResource]
internal sealed class NetWinVTConsole : IDisposable
{
    public NetWinVTConsole ()
    {
        _stdinRaw = File.Create ("CONIN$");
        _stdinText = new StreamReader (_stdinRaw, Encoding.UTF8, false, -1, true);

        _stdoutRaw = File.Create ("CONOUT$");
        _stdoutText = new StreamWriter (_stdoutRaw, Encoding.UTF8, -1, true);

        _stderrRaw = File.Create ("CONERR$");
        _stderrText = new StreamWriter (_stderrRaw, Encoding.UTF8, -1, true);

        if (!GetConsoleMode (_stdinRaw.SafeFileHandle, out CONSOLE_MODE mode))
        {
            ThrowGetIoException (Strings.StdIn);
        }

        _originalInputConsoleMode = mode;

        if ((mode & ENABLE_VIRTUAL_TERMINAL_INPUT) == 0U)
        {
            mode |= ENABLE_VIRTUAL_TERMINAL_INPUT;

            if (!SetConsoleMode (_stdinRaw.SafeFileHandle, in mode))
            {
                ThrowSetIoException (Strings.StdIn);
            }
        }

        if (!GetConsoleMode (_stdoutRaw.SafeFileHandle, out mode))
        {
            ThrowGetIoException (Strings.StdOut);
        }

        _originalOutputConsoleMode = mode;

        if ((mode & (ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN)) < DISABLE_NEWLINE_AUTO_RETURN)
        {
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING | DISABLE_NEWLINE_AUTO_RETURN;

            if (!SetConsoleMode (_stdoutRaw.SafeFileHandle, in mode))
            {
                ThrowSetIoException (Strings.StdOut);
            }
        }

        if (!GetConsoleMode (_stderrRaw.SafeFileHandle, out mode))
        {
            ThrowGetIoException (Strings.StdErr);
        }

        _originalErrorConsoleMode = mode;

        if ((mode & DISABLE_NEWLINE_AUTO_RETURN) != DISABLE_NEWLINE_AUTO_RETURN)
        {
            mode |= DISABLE_NEWLINE_AUTO_RETURN;

            if (!SetConsoleMode (_stderrRaw.SafeFileHandle, in mode))
            {
                ThrowSetIoException (Strings.StdErr);
            }
        }

        return;

        [DoesNotReturn]
        static void ThrowSetIoException (string? streamName = "UNKNOWN")
        {
            throw new IOException (
                                   $"""
                                    {Strings.NetWinVtConsole_UnableToSetConsoleMode} {Strings.ForPossessive} {streamName}.
                                    {Strings.ErrorCodeString}: {GetLastPInvokeError ()}
                                    """);
        }

        [DoesNotReturn]
        static void ThrowGetIoException (string? streamName = "UNKNOWN")
        {
            throw new IOException (
                                   $"""
                                    {Strings.NetWinVtConsole_UnableToGetConsoleMode} {Strings.ForPossessive} {streamName}.
                                    {Strings.ErrorCodeString}: {GetLastPInvokeError ()}
                                    """);
        }
    }

    private readonly FileStream _stdinRaw;
    private readonly TextReader _stdinText;
    private readonly FileStream _stdoutRaw;
    private readonly TextWriter _stdoutText;
    private readonly FileStream _stderrRaw;
    private readonly TextWriter _stderrText;
    private readonly CONSOLE_MODE _originalErrorConsoleMode;
    private readonly CONSOLE_MODE _originalInputConsoleMode;
    private readonly CONSOLE_MODE _originalOutputConsoleMode;

    private volatile bool _disposed;

    /// <inheritdoc/>
    public void Dispose ()
    {
        if (_disposed)
        {
            return;
        }

        Dispose (true);
    }

    public void Cleanup ()
    {
        if (!SetConsoleMode (_stdinRaw.SafeFileHandle, in _originalInputConsoleMode))
        {
            ThrowIoException (Strings.StdIn);
        }

        if (!SetConsoleMode (_stdoutRaw.SafeFileHandle, in _originalOutputConsoleMode))
        {
            ThrowIoException (Strings.StdOut);
        }

        if (!SetConsoleMode (_stderrRaw.SafeFileHandle, in _originalErrorConsoleMode))
        {
            ThrowIoException (Strings.StdErr);
        }

        return;

        [DoesNotReturn]
        static void ThrowIoException (string? streamName = "UNKNOWN")
        {
            throw new IOException (
                                   $"""
                                    {Strings.NetWinVtConsole_UnableToRestoreConsoleMode} {Strings.ForPossessive} {streamName}.
                                    {Strings.ErrorCodeString}: {GetLastPInvokeError ()}
                                    """);
        }
    }

    private void Dispose (bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stdinText.Dispose ();
        _stdinRaw.Dispose ();

        _stdoutText.Dispose ();
        _stdoutRaw.Dispose ();

        _stderrText.Dispose ();
        _stderrRaw.Dispose ();
        if (disposing)
        {
            GC.SuppressFinalize (this);
        }
    }

    /// <inheritdoc/>
    ~NetWinVTConsole () { Dispose (false); }
}
