#nullable enable
using Microsoft.Extensions.Logging;

namespace Terminal.Gui.Drivers;

/// <inheritdoc />
internal class SizeMonitorImpl (IOutput consoleOut) : ISizeMonitor
{
    private Size _lastSize = Size.Empty;

    /// <summary>Invoked when the terminal's size changed. The new size of the terminal is provided.</summary>
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <inheritdoc/>
    public bool Poll ()
    {
        Size size = consoleOut.GetSize ();

        if (size != _lastSize)
        {
            Logging.Logger.LogInformation ($"Console size changes from '{_lastSize}' to {size}");
            _lastSize = size;
            SizeChanged?.Invoke (this, new (size));

            return true;
        }

        return false;
    }
}
