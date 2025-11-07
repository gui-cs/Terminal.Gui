using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
///     <see cref="IInput{TInputRecord}"/> implementation that uses native dotnet methods e.g. <see cref="System.Console"/>.
///     The <see cref="Peek"/> and <see cref="Read"/> methods are executed
///     on the input thread created by <see cref="MainLoopCoordinator{TInputRecord}.StartInputTask"/>.
/// </summary>
public class NetInput : InputImpl<ConsoleKeyInfo>, ITestableInput<ConsoleKeyInfo>, IDisposable
{
    /// <summary>
    ///     Creates a new instance of the class. Implicitly sends
    ///     console mode settings that enable virtual input (mouse
    ///     reporting etc).
    /// </summary>
    public NetInput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (NetInput)}");

        PlatformID p = Environment.OSVersion.Platform;

        if (p == PlatformID.Win32NT || p == PlatformID.Win32S || p == PlatformID.Win32Windows)
        {
            try
            {
                _adjustConsole = new ();
            }
            catch (ApplicationException ex)
            {
                // Likely running as a unit test, or in a non-interactive session.
                Logging.Logger.LogCritical (
                                            ex,
                                            "NetWinVTConsole could not be constructed i.e. could not configure terminal modes. May indicate running in non-interactive session e.g. unit testing CI");

                return;
            }
        }

        //Enable alternative screen buffer.
        Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);

        //Set cursor key to application.
        Console.Out.Write (EscSeqUtils.CSI_HideCursor);

        Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
        Console.TreatControlCAsInput = true;
    }

    private readonly NetWinVTConsole _adjustConsole;

    /// <inheritdoc/>
    public override void Dispose ()
    {
        base.Dispose ();

        if (Application.RunningUnitTests)
        {
            return;
        }

        // Disable mouse events first
        Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);

        //Disable alternative screen buffer.
        Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

        //Set cursor key to cursor.
        Console.Out.Write (EscSeqUtils.CSI_ShowCursor);

        _adjustConsole?.Cleanup ();

        // Flush any pending input so no stray events appear
        FlushConsoleInput ();
    }

    /// <inheritdoc />
    public void AddInput (ConsoleKeyInfo input) { throw new NotImplementedException (); }

    /// <inheritdoc/>
    public override bool Peek ()
    {
        try
        {
            return Console.KeyAvailable;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public override IEnumerable<ConsoleKeyInfo> Read ()
    {
        while (Console.KeyAvailable)
        {
            yield return Console.ReadKey (true);
        }
    }

    private void FlushConsoleInput ()
    {
        while (Console.KeyAvailable)
        {
            Console.ReadKey (true);
        }
    }
}
