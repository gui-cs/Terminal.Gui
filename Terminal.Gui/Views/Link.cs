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
    /// Represents the default URL used when no specific URL is provided.
    /// </summary>
    /// <remarks>
    /// An empty string indicates that no URL is associated with the link.
    /// </remarks>
    public const string DEFAULT_URL = "";

    private string _url = DEFAULT_URL;

    /// <summary>
    /// Gets or sets the URL associated with this instance.
    /// </summary>
    public string Url
    {
        get => _url;
        set => SetUrl (value);
    }

    /// <summary>
    ///     Raised when <see cref="Url"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<string>>? UrlChanging;

    /// <summary>
    ///     URL changed event, raised when the URL has changed.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<string>>? UrlChanged;

    /// <summary>
    ///     Called before <see cref="Url"/> changes. Return <see langword="true"/> to cancel the change.
    /// </summary>
    protected virtual bool OnUrlChanging (ValueChangingEventArgs<string> args) => false;

    /// <summary>
    ///     Called after <see cref="Url"/> has changed.
    /// </summary>
    protected virtual void OnUrlChanged (ValueChangedEventArgs<string> args) { }

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
        // Set the URL for cells that will be drawn (only if URL is not empty)
        if (!string.IsNullOrEmpty (Url) && Driver is { })
        {
            Rectangle drawRect = new (ContentToScreen (Point.Empty), GetContentSize ());

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

    private void SetUrl(string value)
    {
        // Do not crash on invalid URLs, instead default to a blank page
        if (!Uri.TryCreate (value, UriKind.Absolute, out _))
        {
            value = DEFAULT_URL;
        }

        if (_url != value)
        {
            string oldValue = _url;

            // CWP: Fire ValueChanging (allows cancellation)
            ValueChangingEventArgs<string> changingArgs = new (oldValue, value);

            if (OnUrlChanging (changingArgs) || changingArgs.Handled)
            {
                return;
            }

            UrlChanging?.Invoke (this, changingArgs);

            if (changingArgs.Handled)
            {
                return;
            }

            // Do the work
            _url = value;

            // CWP: Fire ValueChanged
            ValueChangedEventArgs<string> changedArgs = new (oldValue, value);
            OnUrlChanged (changedArgs);
            UrlChanged?.Invoke (this, changedArgs);

            // Mark as needing redraw since URL changed
            SetNeedsDraw ();
        }
    }
}

