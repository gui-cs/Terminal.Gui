namespace Terminal.Gui;

/// <summary>Text alignment enumeration, controls how text is displayed.</summary>
public enum TextAlignment
{
    /// <summary>The text will be left-aligned.</summary>
    Left,

    /// <summary>The text will be right-aligned.</summary>
    Right,

    /// <summary>The text will be centered horizontally.</summary>
    Centered,

    /// <summary>
    ///     The text will be justified (spaces will be added to existing spaces such that the text fills the container
    ///     horizontally).
    /// </summary>
    Justified
}