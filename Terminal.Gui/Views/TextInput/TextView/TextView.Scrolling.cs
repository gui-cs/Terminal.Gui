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
        Adjust ();
    }

    /// <inheritdoc />
    protected override void OnViewportChanged (DrawEventArgs e)
    {
        base.OnViewportChanged (e);
        UpdateHorizontalScrollBarVisibility ();
    }

    /// <summary>
    /// Updates the content size based on the current text model dimensions.
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
    /// Updates the horizontal scrollbar visibility based on WordWrap state.
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
}
