namespace Terminal.Gui.Views;

/// <summary>Describes the clickable region for a code block copy button in content coordinates.</summary>
internal readonly struct CopyButtonHitTarget (int contentRow, int contentX, int width, CodeBlockRegion region)
{
    /// <summary>The content-space row where the button is drawn.</summary>
    public int ContentRow { get; } = contentRow;

    /// <summary>The content-space X position where the button starts.</summary>
    public int ContentX { get; } = contentX;

    /// <summary>The column width of the button glyph.</summary>
    public int Width { get; } = width;

    /// <summary>The associated code block region whose text should be copied.</summary>
    public CodeBlockRegion Region { get; } = region;
}
