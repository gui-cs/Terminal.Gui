namespace Terminal.Gui.ViewBase;

/// <summary>
///     A lightweight View that renders tab title text using the parent View's focus-appropriate
///     attributes. Because this View never has focus itself, the base <see cref="View.DrawText"/>
///     would always use Normal/HotNormal. This override uses the owning View's <see cref="View.HasFocus"/>
///     to select Focus/HotFocus when appropriate.
/// </summary>
internal sealed class TabTitleView : View
{
    public TabTitleView ()
    {
        CanFocus = true;
        TabStop = TabBehavior.NoStop;
        Border.Settings = BorderSettings.None;
        SuperViewRendersLineCanvas = true;
    }

    /// <summary>Sync <see cref="View.HotKeySpecifier"/> to <see cref="TextFormatter"/> (same as Label).</summary>
    public override Rune HotKeySpecifier { get => base.HotKeySpecifier; set => TextFormatter.HotKeySpecifier = base.HotKeySpecifier = value; }

    /// <inheritdoc/>
    protected override bool OnDrawingText (DrawContext? context)
    {
        if (Driver is null)
        {
            return false;
        }

        // TODO: Determine best look for focus.
        // TODO: Option 1: Only draw focused if the TabTitleView has focused. The tab border underline would be the only indication of focus.
        // TODO: Option 2: Draw focused if the TabTitleView has focus OR the owning BorderView has focus. Double indication: the tab border underline and the focused text attribute.
        // TODO: Choose option 1 for now.
        bool ownerHasFocus = HasFocus; // (SuperView as BorderView)?.Adornment?.Parent?.HasFocus ?? false;

        Rectangle drawRect = ViewportToScreen ();

        // Add the entire content area to the drawn region so that it is not transparent
        context?.AddDrawnRectangle (drawRect);

        TextFormatter.Draw (Driver,
                            drawRect,
                            ownerHasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal),
                            ownerHasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal),
                            Rectangle.Empty);

        SetSubViewNeedsDrawDownHierarchy ();

        return true;
    }
}
