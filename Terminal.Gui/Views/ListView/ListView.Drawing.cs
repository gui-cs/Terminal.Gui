namespace Terminal.Gui.Views;

public partial class ListView
{
    /// <summary>Virtual method that will invoke the <see cref="RowRender"/>.</summary>
    /// <param name="rowEventArgs"></param>
    public virtual void OnRowRender (ListViewRowEventArgs rowEventArgs) => RowRender?.Invoke (this, rowEventArgs);

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
        int col = AllowsMarking ? 2 : 0;
        int start = Viewport.X;

        for (var row = 0; row < f.Height; row++, item++)
        {
            bool isSelected = item == SelectedItem;
            bool isMultiSelected = MultiSelectedItems.Contains (item);

            // Determine visual role based on selection state
            VisualRole role;

            if (focused && isSelected)
            {
                role = VisualRole.Focus; // Focused + SelectedItem (cursor position)
            }
            else if (!Source!.IsMarked (item) && isMultiSelected)
            {
                role = VisualRole.Highlight; // In MultiSelectedItems (selection highlight)
            }
            else if (!Source.IsMarked (item) && isSelected)
            {
                role = VisualRole.Active; // SelectedItem without focus
            }
            else
            {
                role = VisualRole.Normal; // Not selected
            }

            Attribute newAttribute = GetAttributeForRole (role);

            if (newAttribute != current)
            {
                if (!Source!.IsMarked (item) && isMultiSelected)
                {
                    newAttribute = newAttribute with
                    {
                        Foreground = GetAttributeForRole (VisualRole.Highlight).Foreground, Style = GetAttributeForRole (VisualRole.Highlight).Style
                    };
                }
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

                if (AllowsMarking)
                {
                    // Try custom mark rendering first
                    bool customRendered = Source.RenderMark (this, item, row, Source.IsMarked (item), AllowsMultipleSelection);

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

                        AddRune (Source.IsMarked (item) ? AllowsMultipleSelection ? Glyphs.CheckStateChecked : Glyphs.Selected :
                                 AllowsMultipleSelection ? Glyphs.CheckStateUnChecked : Glyphs.UnSelected);
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
}
