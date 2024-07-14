namespace Terminal.Gui;

/// <summary>Event arguments for the <see cref="Color"/> events.</summary>
public class ColorEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="ColorEventArgs"/></summary>
    public ColorEventArgs () { }

    /// <summary>The new Thickness.</summary>
    public Color Color { get; set; }

    /// <summary>The previous Thickness.</summary>
    public Color PreviousColor { get; set; }
}