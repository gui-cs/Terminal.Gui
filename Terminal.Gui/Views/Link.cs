namespace Terminal.Gui.Views;

/// <summary>
///     Displays a clickable link with text and url.
/// </summary>
public class Link : View, IDesignable
{
    /// <inheritdoc/>
    public Link ()
    {
        Height = Dim.Auto (DimAutoStyle.Text);
        Width = Dim.Auto (DimAutoStyle.Text);

        CanFocus = true;

        // On HotKey, pass it to the next view
        AddCommand (Command.HotKey, InvokeHotKeyOnNextPeer!);
    }

    /// <inheritdoc/>
    public override string Text
    {
        get => string.IsNullOrWhiteSpace (Title) ? Url : Title;
        set
        {
            Title = value;
            base.Text = string.IsNullOrWhiteSpace (value) ? Url : value;
        }
    }

    /// <summary>
    /// Représente l'URL par défaut utilisée lorsque aucune URL spécifique n'est fournie.
    /// </summary>
    /// <remarks>Cette constante peut être utilisée pour initialiser des navigateurs ou des composants Web à
    /// une page vierge ou à un état neutre.</remarks>
    public const string DEFAULT_URL = "about:blank";

    private string _url = DEFAULT_URL;

    /// <summary>
    /// Gets or sets the URL associated with this instance.
    /// </summary>
    public string Url
    {
        get { return _url; }
        set
        {
            // Will throw exception if not a valid URL
            _ = new Uri (value);

            _url = value; 
            OnUrlChanged ();
        }
    }

    /// <summary>
    ///     Text changed event, raised when the text has changed.
    /// </summary>
    public event EventHandler? UrlChanged;

    /// <summary>
    ///     Called when the <see cref="Url"/> has changed. Fires the <see cref="UrlChanged"/> event.
    /// </summary>
    public void OnUrlChanged () => UrlChanged?.Invoke (this, EventArgs.Empty);

    private bool? InvokeHotKeyOnNextPeer (ICommandContext commandContext)
    {
        if (RaiseHandlingHotKey (commandContext) == true)
        {
            return true;
        }

        if (CanFocus)
        {
            SetFocus ();

            // Always return true on hotkey, even if SetFocus fails because
            // hotkeys are always handled by the View (unless RaiseHandlingHotKey cancels).
            // This is the same behavior as the base (View).
            return true;
        }

        if (HotKey.IsValid)
        {
            // If the Link has a hotkey, we need to find the next view in the subview list
            int me = SuperView?.SubViews.IndexOf (this) ?? -1;

            if (me != -1 && me < SuperView?.SubViews.Count - 1)
            {
                return SuperView?.SubViews.ElementAt (me + 1).InvokeCommand (Command.HotKey) == true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        // If Link can't focus and is clicked, invoke HotKey on next peer
        if (!CanFocus)
        {
            return InvokeCommand (Command.HotKey, args.Context) == true;
        }

        return base.OnActivating (args);
    }

    /// <inheritdoc/>
    bool IDesignable.EnableForDesign ()
    {
        Text = "_Link";

        return true;
    }

    /// <summary>Copy the URL to the clipboard contents.</summary>
    public bool Copy ()
    {
        SetClipboard (Url);

        return true;
    }

    private void SetClipboard (string text) => App?.Clipboard?.SetClipboardData (text);

    /// <inheritdoc/>
    protected override bool OnDrawingText (DrawContext? context)
    {
        // Set the URL for cells that will be drawn
        if (!string.IsNullOrEmpty (Url) && Url != DEFAULT_URL && Driver is { })
        {
            Rectangle drawRect = new Rectangle (ContentToScreen (Point.Empty), GetContentSize ());

            // Use GetDrawRegion to get precise drawn areas
            Region textRegion = TextFormatter.GetDrawRegion (drawRect);

            // Report the drawn area to the context
            context?.AddDrawnRegion (textRegion);

            Attribute normalAttr = HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal);
            Attribute hotAttr = HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal);

            // Set the URL in the driver so all cells drawn will have this URL
            Driver.CurrentUrl = Url;

            try
            {
                // Draw the text using TextFormatter - all cells will now have the URL
                TextFormatter.Draw (
                                    Driver,
                                    drawRect,
                                    normalAttr,
                                    hotAttr,
                                    Rectangle.Empty);
            }
            finally
            {
                // Clear the URL after drawing
                Driver.CurrentUrl = null;
            }

            // We assume that the text has been drawn over the entire area; ensure that the SubViews are redrawn.
            SetSubViewNeedsDrawDownHierarchy ();

            return true; // We handled the drawing
        }

        return base.OnDrawingText (context);
    }
}

