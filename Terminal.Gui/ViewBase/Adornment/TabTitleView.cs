namespace Terminal.Gui.ViewBase;

/// <summary>
///     A lightweight View that renders tab title text using the parent View's focus-appropriate
///     attributes. Because this View never has focus itself, the base <see cref="View.DrawText"/>
///     would always use Normal/HotNormal. This override uses the owning View's <see cref="View.HasFocus"/>
///     to select Focus/HotFocus when appropriate.
/// </summary>
internal sealed class TabTitleView : View
{
    /// <summary>The View whose focus state determines which attributes to use.</summary>
    internal View? OwnerView { get; init; }

    /// <summary>Sync <see cref="View.HotKeySpecifier"/> to <see cref="TextFormatter"/> (same as Label).</summary>
    public override Rune HotKeySpecifier { get => base.HotKeySpecifier; set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value; }

    /// <inheritdoc/>
    protected override bool OnDrawingText (DrawContext? context)
    {
        if (Driver is null)
        {
            return false;
        }

        bool ownerHasFocus = OwnerView?.HasFocus ?? false;

        Rectangle drawRect = new (ContentToScreen (Point.Empty), GetContentSize ());
        Region textRegion = TextFormatter.GetDrawRegion (drawRect);
        context?.AddDrawnRegion (textRegion);

        TextFormatter.Draw (Driver,
                            drawRect,
                            ownerHasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal),
                            ownerHasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal),
                            Rectangle.Empty);

        SetSubViewNeedsDrawDownHierarchy ();

        return true;
    }
}
