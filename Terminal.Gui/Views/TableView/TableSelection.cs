namespace Terminal.Gui.Views;

/// <summary>Describes a selected region of the table</summary>
public class TableSelection
{
    /// <summary>Creates a new selected area starting at the origin corner and covering the provided rectangular area</summary>
    /// <param name="origin"></param>
    /// <param name="rect"></param>
    public TableSelection (Point origin, Rectangle rect)
    {
        Origin = origin;
        Rectangle = rect;
    }

    /// <summary>
    ///     True if the selection was made through <see cref="Command.Select"/> and therefore should persist even
    ///     through keyboard navigation.
    /// </summary>
    public bool IsToggled { get; set; }

    /// <summary>Corner of the <see cref="Rectangle"/> where selection began</summary>
    /// <value></value>
    public Point Origin { get; set; }

    /// <summary>Area selected</summary>
    /// <value></value>
    public Rectangle Rectangle { get; set; }
}
