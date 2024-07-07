namespace Terminal.Gui;

/// <summary><see cref="EventArgs"/> for <see cref="Orientation"/> events.</summary>
public class OrientationEventArgs : EventArgs
{
    /// <summary>Constructs a new instance.</summary>
    /// <param name="orientation">the new orientation</param>
    public OrientationEventArgs (Orientation orientation)
    {
        Orientation = orientation;
        Cancel = false;
    }

    /// <summary>If set to true, the orientation change operation will be canceled, if applicable.</summary>
    public bool Cancel { get; set; }

    /// <summary>The new orientation.</summary>
    public Orientation Orientation { get; set; }
}