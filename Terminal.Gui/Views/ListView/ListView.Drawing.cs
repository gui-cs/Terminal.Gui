namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <inheritdoc/>
    protected override bool OnDrawingContent (DrawContext? context)
    {
        if (Source is null)
        {
            return base.OnDrawingContent (context);
        }

        var current = Attribute.Default;
        Move (0, 0);
        Rectangle f = Viewport;
        int item = Viewport.Y;
        bool focused = HasFocus;
        int col = ShowMarks ? 2 : 0;
        int start = Viewport.X;

        for (var row = 0; row < f.Height; row++, item++)
        {
            bool isSelected = item == SelectedItem;
            bool isMarked = Source!.IsMarked (item);
            bool hasFocus = focused;

            // Determine visual role based on the 4 combinations of ShowMarks and MarkMultiple
            VisualRole role;
            var applyHighlightStyle = false;

            if (!ShowMarks && !MarkMultiple)
            {
                // Combination 1: Standard selection mode (no marking)
                // Mark glyphs: None (MarkWidth = 0)
                // Visual roles: SelectedItem uses Focus (focused) or Active (not focused)
                role = isSelected ? hasFocus ? VisualRole.Focus : VisualRole.Active : VisualRole.Normal;
            }
            else if (!ShowMarks && MarkMultiple)
            {
                // Combination 2: Hidden marks with visual role indicators
                // Mark glyphs: None (MarkWidth = 0) - marks exist internally
                // Visual roles use Highlight for marked items; compose TextStyle when marked+selected+focused
                if (isSelected && isMarked)
                {
                    role = hasFocus ? VisualRole.Focus : VisualRole.Highlight;
                    applyHighlightStyle = hasFocus; // Apply Highlight's TextStyle to Focus
                }
                else if (isSelected)
                {
                    role = hasFocus ? VisualRole.Focus : VisualRole.Normal;
                }
                else if (isMarked)
                {
                    role = VisualRole.Highlight;
                }
                else
                {
                    role = VisualRole.Normal;
                }
            }
            else if (ShowMarks && !MarkMultiple)
            {
                // Combination 3: Radio button style
                // Mark glyphs: Radio-button style (◉ marked, ○ unmarked)
                // Visual roles: Standard selection (mark glyphs provide visual indication)
                role = isSelected ? hasFocus ? VisualRole.Focus : VisualRole.Active : VisualRole.Normal;
            }
            else // ShowMarks == true && MarkMultiple == true
            {
                // Combination 4: Checkbox style
                // Mark glyphs: Checkbox style (☒ marked, ☐ unmarked)
                // Visual roles: Standard selection (mark glyphs provide visual indication)
                role = isSelected ? hasFocus ? VisualRole.Focus : VisualRole.Active : VisualRole.Normal;
            }

            Attribute newAttribute = GetAttributeForRole (role);

            // Apply Highlight's TextStyle if needed (combination 2 only, when marked+selected+focused)
            if (applyHighlightStyle)
            {
                Attribute highlightAttr = GetAttributeForRole (VisualRole.Highlight);
                newAttribute = newAttribute with { Style = highlightAttr.Style };
            }

            if (newAttribute != current)
            {
                SetAttribute (newAttribute);
                current = newAttribute;
            }

            Move (0, row);

            if (Source is null || item >= Source.Count)
            {
                for (var c = 0; c < f.Width; c++)
                {
                    AddRune ((Rune)' ');
                }
            }
            else
            {
                var rowEventArgs = new ListViewRowEventArgs (item);
                OnRowRender (rowEventArgs);

                if (rowEventArgs.RowAttribute is { } && current != rowEventArgs.RowAttribute)
                {
                    current = (Attribute)rowEventArgs.RowAttribute;
                    SetAttribute (current);
                }

                var markWidth = 0;

                if (ShowMarks)
                {
                    // Try custom mark rendering first
                    bool customRendered = Source.RenderMark (this, item, row, Source.IsMarked (item), MarkMultiple);

                    if (!customRendered)
                    {
                        // Default rendering: marks with Normal attribute for visual clarity
                        Attribute savedAttr = current;
                        Attribute normalAttr = GetAttributeForRole (VisualRole.Normal);

                        if (current != normalAttr)
                        {
                            SetAttribute (normalAttr);
                            current = normalAttr;
                        }

                        AddRune (Source.IsMarked (item) ? MarkMultiple ? Glyphs.CheckStateChecked : Glyphs.Selected :
                                 MarkMultiple ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected);
                        AddRune ((Rune)' ');
                        markWidth = 2;

                        // Restore attribute for content rendering
                        if (current != savedAttr)
                        {
                            SetAttribute (savedAttr);
                            current = savedAttr;
                        }
                    }
                }

                int contentCol = col > 0 ? col : markWidth;
                Source.Render (this, isSelected, item, contentCol, row, f.Width - contentCol, start);
            }
        }

        return true;
    }

    // TODO: RowRender should follow CWP.
    // TODO: All this lets you do is customize the Attribute per row. It should be renamed
    // TODO: to RowAttributeRender or something similar.
    // TODO: Consider adding a set of events for more customization; but that's what's IListDataSource.Render is for
    /// <summary>Virtual method that will invoke the <see cref="RowRender"/>.</summary>
    /// <param name="rowEventArgs"></param>
    public virtual void OnRowRender (ListViewRowEventArgs rowEventArgs) => RowRender?.Invoke (this, rowEventArgs);

    /// <summary>This event is invoked when this <see cref="ListView"/> is being drawn before rendering.</summary>
    public event EventHandler<ListViewRowEventArgs>? RowRender;

}
