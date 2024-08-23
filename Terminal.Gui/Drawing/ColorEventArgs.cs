#nullable enable

namespace Terminal.Gui;

/// <summary>Event arguments for the <see cref="Color"/> events.</summary>
public class ColorEventArgs : EventArgs<Color>
{
    /// <summary>Initializes a new instance of <see cref="ColorEventArgs"/>
    /// <paramref name="newColor"/>The value that is being changed to.</summary>
    public ColorEventArgs (Color newColor) :base(newColor) { }
}