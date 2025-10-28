#nullable enable
using System.Drawing;

namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591
public class FakeSizeMonitor (IConsoleOutput consoleOut, IOutputBuffer _) : IConsoleSizeMonitor
{
    /// <inheritdoc />
    public event EventHandler<SizeChangedEventArgs>? SizeChanged;

    /// <inheritdoc/>
    public bool Poll () { return false; }

    /// <summary>
    ///     Raises the <see cref="SizeChanged"/> event.
    /// </summary>
    /// <param name="newSize"></param>
    public void RaiseSizeChanged (Size newSize) { SizeChanged?.Invoke (this, new (newSize)); }
}