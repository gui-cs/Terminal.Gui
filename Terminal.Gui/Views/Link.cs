using System.Diagnostics;

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

        MouseBindings.Add (MouseFlags.LeftButtonClicked, Command.Accept);
    }

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        // If Link can't focus and is activated, invoke HotKey on next peer
        if (!CanFocus)
        {
            return InvokeCommand (Command.HotKey, args.Context) == true;
        }

        return base.OnActivating (args);
    }

    /// <summary>
    ///     Opens <see cref="Url"/>.
    /// </summary>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);

        OpenUrl (Url);
    }

    /// <summary>
    ///     Opens the specified URL in the default web browser. The implementation is platform-specific:
    /// </summary>
    /// <param name="url"></param>
    public static void OpenUrl (string url)
    {
        if (PlatformDetection.IsWindows ())
        {
            url = url.Replace ("&", "^&");
            Process.Start (new ProcessStartInfo ("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (PlatformDetection.IsMac ())
        {
            Process.Start ("open", url);
        }
        else if (PlatformDetection.IsLinux ())
        {
            using Process process = new ();

            process.StartInfo = new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = url,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            process.Start ();
        }
    }

    /// <summary>
    ///     Updates the text displayed by the text formatter based on the current value of the control's text property.
    /// </summary>
    /// <remarks>
    ///     If the base text is null or empty, the formatter displays the URL instead. Otherwise, the
    ///     base implementation is used. This method is used by the Layout engine to determine the text Size.
    /// </remarks>
    protected override void UpdateTextFormatterText ()
    {
        if (string.IsNullOrEmpty (base.Text))
        {
            TextFormatter.Text = Url;
            TextFormatter.ConstrainToWidth = null;
            TextFormatter.ConstrainToHeight = null;
        }
        else
        {
            base.UpdateTextFormatterText ();
        }
    }

    /// <summary>
    ///     Represents the default URL used when no specific URL is provided.
    /// </summary>
    /// <remarks>
    ///     An empty string indicates that no URL is associated with the link.
    /// </remarks>
    public const string DEFAULT_URL = "";

    private string _url = DEFAULT_URL;

    /// <summary>
    ///     Gets or sets the URL associated with this instance. If <see cref="Text"/> is empty, the URL will be displayed as a
    ///     clickable link. If <see cref="Text"/> is set,
    ///     it will be displayed as a clickable link.
    /// </summary>
    public string Url { get => _url; set => SetUrl (value); }

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
    bool IDesignable.EnableForDesign ()
    {
        Title = "_Link";
        Url = "https://github.com/gui-cs";

        return true;
    }

    /// <summary>Copy the URL to the clipboard contents.</summary>
    public bool Copy ()
    {
        SetClipboard (Url);

        return true;
    }

    private void SetClipboard (string text) => App?.Clipboard?.SetClipboardData (text);

    /// <summary>
    ///     Draws the Link. If <see cref="Text"/> is empty, the <see cref="Url"/> will be drawn; otherwise <see cref="Text"/>
    ///     will be drawn.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected override bool OnDrawingText (DrawContext? context)
    {
        if (Driver is null)
        {
            return base.OnDrawingText (context);
        }

        Rectangle drawRect = new (ContentToScreen (Point.Empty), GetContentSize ());

        Region textDrawRegion = TextFormatter.GetDrawRegion (drawRect);

        // Report the drawn area to the context
        context?.AddDrawnRegion (textDrawRegion);

        Attribute normalAttr = HasFocus ? GetAttributeForRole (VisualRole.Focus) : GetAttributeForRole (VisualRole.Normal);
        Attribute hotAttr = HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal);

        string? url = Url;

        // If the URL is not valid, don't set CurrentUrl, and adjust the attributes to indicate it's not well-formed
        if (!Uri.IsWellFormedUriString (Url, UriKind.Absolute))
        {
            normalAttr = GetAttributeForRole (VisualRole.Disabled);
            normalAttr = normalAttr with { Background = HasFocus ? GetAttributeForRole (VisualRole.Focus).Background : normalAttr.Background };
            url = null;
        }

        // Set the URL in the driver so all cells drawn will have this URL
        Driver.CurrentUrl = url;

        try
        {
            // Draw the Title using TextFormatter - all cells will now have the URL
            TextFormatter.Draw (Driver, drawRect, normalAttr, hotAttr, Rectangle.Empty);
        }
        finally
        {
            // Always clear the URL after drawing
            Driver.CurrentUrl = null;
        }

        // We assume that the text has been drawn over the entire area; ensure that the SubViews are redrawn.
        SetSubViewNeedsDrawDownHierarchy ();

        return true; // We handled the drawing
    }

    private void SetUrl (string value)
    {
        if (_url == value)
        {
            return;
        }
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

        // Indicate the formatter needs formatting, which will cause UpdateTextFormatterText to be invoked
        TextFormatter.NeedsFormat = true;
        SetNeedsLayout ();
    }
}
