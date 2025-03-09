using Microsoft.Extensions.Logging;

namespace Terminal.Gui;

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
        Size size = _consoleOut.GetWindowSize ();

        if (size != _lastSize)
        {
            Logging.Logger.LogInformation ($"Console size changes from '{_lastSize}' to {size}");
            _outputBuffer.SetWindowSize (size.Width, size.Height);
            _lastSize = size;
            SizeChanging?.Invoke (this, new (size));

            return true;
        }

        return false;
    }
}
