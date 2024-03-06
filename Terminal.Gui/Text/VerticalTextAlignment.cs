namespace Terminal.Gui;

/// <summary>Vertical text alignment enumeration, controls how text is displayed.</summary>
public enum VerticalTextAlignment
{
    /// <summary>The text will be top-aligned.</summary>
    Top,

    /// <summary>The text will be bottom-aligned.</summary>
    Bottom,

    /// <summary>The text will centered vertically.</summary>
    Middle,

    /// <summary>
    ///     The text will be justified (spaces will be added to existing spaces such that the text fills the container
    ///     vertically).
    /// </summary>
    Justified
}