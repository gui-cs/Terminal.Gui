using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <summary>
///     Console input implementation that uses native dotnet methods e.g. <see cref="System.Console"/>.
/// </summary>
public class NetInput : ConsoleInput<ConsoleKeyInfo>, INetInput
{
    private readonly NetWinVTConsole _adjustConsole;

    /// <summary>
    ///     Creates a new instance of the class. Implicitly sends
    ///     console mode settings that enable virtual input (mouse
    ///     reporting etc).
    /// </summary>
    public NetInput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (NetInput)}");

        if (ConsoleDriver.RunningUnitTests)
        {
            return;
        }

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
            }
        }

        //Enable alternative screen buffer.
        Console.Out.Write (EscSeqUtils.CSI_SaveCursorAndActivateAltBufferNoBackscroll);

        //Set cursor key to application.
        Console.Out.Write (EscSeqUtils.CSI_HideCursor);

        Console.Out.Write (EscSeqUtils.CSI_EnableMouseEvents);
        Console.TreatControlCAsInput = true;
    }

    /// <inheritdoc/>
    protected override bool Peek ()
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return false;
        }

        return Console.KeyAvailable;
    }

    /// <inheritdoc/>
    protected override IEnumerable<ConsoleKeyInfo> Read ()
    {
        while (Console.KeyAvailable)
        {
            yield return Console.ReadKey (true);
        }
    }

    /// <inheritdoc/>
    public override void Dispose ()
    {
        base.Dispose ();
        Console.Out.Write (EscSeqUtils.CSI_DisableMouseEvents);

        //Disable alternative screen buffer.
        Console.Out.Write (EscSeqUtils.CSI_RestoreCursorAndRestoreAltBufferWithBackscroll);

        //Set cursor key to cursor.
        Console.Out.Write (EscSeqUtils.CSI_ShowCursor);

        _adjustConsole?.Cleanup ();
    }
}
