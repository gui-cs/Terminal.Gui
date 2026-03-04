namespace Terminal.Gui.ViewBase;

internal class ArrangerButton : Button
{
    public ArrangerButton ()
    {
        CanFocus = true;
        Width = 1;
        Height = 1;
        NoDecorations = true;
        NoPadding = true;
        base.ShadowStyle = ShadowStyle.None;
        base.Visible = false;
    }

    public ArrangeButtons ButtonType { get; set; }

    /// <inheritdoc/>
    public override string Text
    {
        get =>
            ButtonType switch
            {
                ArrangeButtons.Move => $"{Glyphs.Move}",
                ArrangeButtons.AllSize => $"{Glyphs.SizeBottomRight}",
                ArrangeButtons.LeftSize or ArrangeButtons.RightSize => $"{Glyphs.SizeHorizontal}",
                ArrangeButtons.TopSize or ArrangeButtons.BottomSize => $"{Glyphs.SizeVertical}",
                _ => base.Text
            };
        set => base.Text = value;
    }
}
