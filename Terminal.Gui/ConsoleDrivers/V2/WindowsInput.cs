using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using static Terminal.Gui.WindowsConsole;

namespace Terminal.Gui;

internal class WindowsInput : ConsoleInput<InputRecord>, IWindowsInput
{
    private readonly nint _inputHandle;

    [DllImport ("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
    public static extern bool ReadConsoleInput (
        nint hConsoleInput,
        nint lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsRead
    );

    [DllImport ("kernel32.dll", EntryPoint = "PeekConsoleInputW", CharSet = CharSet.Unicode)]
    public static extern bool PeekConsoleInput (
        nint hConsoleInput,
        nint lpBuffer,
        uint nLength,
        out uint lpNumberOfEventsRead
    );

    [DllImport ("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle (int nStdHandle);

    [DllImport ("kernel32.dll")]
    private static extern bool GetConsoleMode (nint hConsoleHandle, out uint lpMode);

    [DllImport ("kernel32.dll")]
    private static extern bool SetConsoleMode (nint hConsoleHandle, uint dwMode);

    private readonly uint _originalConsoleMode;

    public WindowsInput ()
    {
        Logging.Logger.LogInformation ($"Creating {nameof (WindowsInput)}");
        _inputHandle = GetStdHandle (STD_INPUT_HANDLE);

        GetConsoleMode (_inputHandle, out uint v);
        _originalConsoleMode = v;

        uint newConsoleMode = _originalConsoleMode;
        newConsoleMode |= (uint)(ConsoleModes.EnableMouseInput | ConsoleModes.EnableExtendedFlags);
        newConsoleMode &= ~(uint)ConsoleModes.EnableQuickEditMode;
        newConsoleMode &= ~(uint)ConsoleModes.EnableProcessedInput;
        SetConsoleMode (_inputHandle, newConsoleMode);
    }

    protected override bool Peek ()
    {
        const int bufferSize = 1; // We only need to check if there's at least one event
        nint pRecord = Marshal.AllocHGlobal (Marshal.SizeOf<InputRecord> () * bufferSize);

        try
        {
            // Use PeekConsoleInput to inspect the input buffer without removing events
            if (PeekConsoleInput (_inputHandle, pRecord, bufferSize, out uint numberOfEventsRead))
            {
                // Return true if there's at least one event in the buffer
                return numberOfEventsRead > 0;
            }
            else
            {
                // Handle the failure of PeekConsoleInput
                throw new InvalidOperationException ("Failed to peek console input.");
            }
        }
        catch (Exception ex)
        {
            // Optionally log the exception
            Console.WriteLine ($"Error in Peek: {ex.Message}");

            return false;
        }
        finally
        {
            // Free the allocated memory
            Marshal.FreeHGlobal (pRecord);
        }
    }

    protected override IEnumerable<InputRecord> Read ()
    {
        const int bufferSize = 1;
        nint pRecord = Marshal.AllocHGlobal (Marshal.SizeOf<InputRecord> () * bufferSize);

        try
        {
            ReadConsoleInput (
                              _inputHandle,
                              pRecord,
                              bufferSize,
                              out uint numberEventsRead);

            return numberEventsRead == 0
                       ? []
                       : new [] { Marshal.PtrToStructure<InputRecord> (pRecord) };
        }
        catch (Exception)
        {
            return [];
        }
        finally
        {
            Marshal.FreeHGlobal (pRecord);
        }
    }

    public override void Dispose () { SetConsoleMode (_inputHandle, _originalConsoleMode); }
}
