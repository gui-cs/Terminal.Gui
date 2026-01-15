namespace Terminal.Gui.Views;

public partial class TextView
{
    private bool _scrollBars;

    /// <summary>
    ///     Gets or sets a value indicating whether scroll bars are enabled.
    /// </summary>
    /// <remarks>
    ///     When set to true, scroll bars will be displayed if the content exceeds the viewable area.
    /// </remarks>
    public bool ScrollBars
    {
        get => _scrollBars;
        set
        {
            if (_scrollBars == value)
            {
                return;
            }

            _scrollBars = value;

            UpdateScrollBars ();
        }
    }

    private void UpdateScrollBars ()
    {
        // Configure scrollbars to use modern Viewport system
        //VerticalScrollBar.AutoShow = false;
        UpdateHorizontalScrollBarVisibility ();
        AdjustViewport ();
    }

    /// <inheritdoc/>
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);
        UpdateHorizontalScrollBarVisibility ();
    }

    /// <summary>
    ///     Updates the content size based on the current text model dimensions.
    /// </summary>
    private void UpdateContentSize ()
    {
        if (!IsInitialized)
        {
            return;
        }

        int contentHeight = _model.Count;
        int contentWidth = WordWrap ? Viewport.Width : _model.GetMaxVisibleLine (0, _model.Count, TabWidth);

        SetContentSize (new Size (contentWidth, contentHeight));
    }

    /// <summary>
    ///     Updates the horizontal scrollbar visibility based on WordWrap state.
    /// </summary>
    private void UpdateHorizontalScrollBarVisibility ()
    {
        if (!IsInitialized)
        {
            return;
        }

        HorizontalScrollBar.Visible = ScrollBars && !WordWrap && Viewport.Width < GetContentSize ().Width;
        VerticalScrollBar.Visible = ScrollBars && Viewport.Height < GetContentSize ().Height;
    }

    private void AdjustViewport ()
    {
        List<Cell> line = GetCurrentLine ();
        bool need = NeedsDraw || _wrapNeeded || !Used;
        (int size, int length) tSize = TextModel.DisplaySize (line, -1, -1, false, TabWidth);
        (int size, int length) dSize = TextModel.DisplaySize (line, Viewport.X, CurrentColumn, true, TabWidth);

        // Handle horizontal scrolling (only when WordWrap is off)
        if (!_wordWrap && CurrentColumn < Viewport.X)
        {
            Viewport = Viewport with { X = CurrentColumn };
            need = true;
        }
        else if (!_wordWrap && (CurrentColumn - Viewport.X + 1 > Viewport.Width || dSize.size + 1 >= Viewport.Width))
        {
            Viewport = Viewport with { X = TextModel.CalculateLeftColumn (line, Viewport.X, CurrentColumn, Viewport.Width, TabWidth) };
            need = true;
        }
        else if ((_wordWrap && Viewport.X > 0) || (dSize.size < Viewport.Width && tSize.size < Viewport.Width))
        {
            if (Viewport.X > 0)
            {
                Viewport = Viewport with { X = 0 };
                need = true;
            }
        }

        // Handle vertical scrolling
        if (CurrentRow < Viewport.Y)
        {
            Viewport = Viewport with { Y = CurrentRow };
            need = true;
        }
        else if (CurrentRow - Viewport.Y >= Viewport.Height)
        {
            Viewport = Viewport with { Y = Math.Min (Math.Max (CurrentRow - Viewport.Height + 1, 0), CurrentRow) };
            need = true;
        }
        else if (Viewport.Y > 0 && CurrentRow < Viewport.Y)
        {
            Viewport = Viewport with { Y = Math.Max (Viewport.Y - 1, 0) };
            need = true;
        }

        if (need)
        {
            if (_wrapNeeded)
            {
                WrapTextModel ();
                _wrapNeeded = false;
            }

            SetNeedsDraw ();
        }
        else
        {
            PositionCursor ();
        }

        OnUnwrappedCursorPosition ();
    }
}
