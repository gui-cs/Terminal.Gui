using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

internal class WindowSizeMonitor : IWindowSizeMonitor
{
    private readonly IConsoleOutput _consoleOut;
    private readonly IOutputBuffer _outputBuffer;
    private Size _lastSize = new (0, 0);

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    public event EventHandler<SizeChangedEventArgs> SizeChanging;

    public WindowSizeMonitor (IConsoleOutput consoleOut, IOutputBuffer outputBuffer)
    {
        _consoleOut = consoleOut;
        _outputBuffer = outputBuffer;
    }

    /// <inheritdoc/>
    public bool Poll ()
    {
        if (ConsoleDriver.RunningUnitTests)
        {
            return false;
        }

        Size size = _consoleOut.GetWindowSize ();

        if (size != _lastSize)
        {
            Logging.Logger.LogInformation ($"Console size changes from '{_lastSize}' to {size}");
            Size newSize = size;

            if (_consoleOut.GetType().Name == "WindowsOutput")
            {
                newSize = _consoleOut.SetWindowSize (size);
            }

            _outputBuffer.SetWindowSize (newSize.Width, newSize.Height);
            _lastSize = newSize;
            SizeChanging?.Invoke (this, new (newSize));

            return true;
        }

        return false;
    }
}
