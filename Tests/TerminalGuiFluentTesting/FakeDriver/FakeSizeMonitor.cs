#nullable enable
using System.Drawing;

namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591
public class FakeSizeMonitor : IWindowSizeMonitor
{
    /// <inheritdoc/>
    public event EventHandler<SizeChangedEventArgs>? SizeChanging;

    /// <inheritdoc/>
    public bool Poll () { return false; }

    /// <summary>
    ///     Raises the <see cref="SizeChanging"/> event.
    /// </summary>
    /// <param name="newSize"></param>
    public void RaiseSizeChanging (Size newSize) { SizeChanging?.Invoke (this, new (newSize)); }
}
