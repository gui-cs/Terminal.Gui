using System.Diagnostics;

namespace Terminal.Gui.Views;

/// <summary>
///     Displays a clickable hyperlink with optional display text and a target URL.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Link"/> has three independent text-related properties:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="View.Text"/> — The display text shown to the user. When empty, <see cref="Url"/> is
///                 displayed
///                 instead.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="View.Title"/> — Controls the <see cref="View.HotKey"/>. Set this to include an underscore
///                 prefix
///                 (e.g., <c>"_Link"</c>) to define a keyboard shortcut.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="Url"/> — The hyperlink target. When the link is accepted (clicked or
///                 <see cref="Command.Accept"/>
///                 is invoked), this URL is opened in the default browser via <see cref="OpenUrl"/>.
///             </description>
///         </item>
///     </list>
///     <para>
///         The link renders using OSC 8 hyperlink escape sequences when the terminal supports them, enabling
///         clickable URLs in modern terminal emulators. If <see cref="Url"/> is not a well-formed absolute URI,
///         the link renders with the <see cref="VisualRole.Disabled"/> style and OSC 8 sequences are suppressed.
///     </para>
///     <para>
///         <see cref="Url"/> changes follow the Cancellable Workflow Pattern (CWP): the <see cref="UrlChanging"/> event
///         fires before the change (and can cancel it by setting <see cref="ValueChangingEventArgs{T}.Handled"/> to
///         <see langword="true"/>), and the <see cref="UrlChanged"/> event fires after.
///     </para>
///     <para>
///         When <see cref="View.CanFocus"/> is <see langword="false"/> and the link has a valid <see cref="View.HotKey"/>,
///         pressing the HotKey passes focus to the next peer <see cref="View"/> in the SuperView's SubView list. This
///         enables <see cref="Link"/> to act as a label-like hotkey proxy (similar to <see cref="Label"/>).
///     </para>
///     <para>
///         Both <see cref="View.Width"/> and <see cref="View.Height"/> default to <see cref="DimAutoStyle.Text"/>,
///         so the link auto-sizes to fit whichever text is displayed (<see cref="View.Text"/> or <see cref="Url"/>).
///     </para>
///     <para>Default mouse bindings:</para>
///     <list type="table">
///         <listheader>
///             <term>Mouse Event</term> <description>Action</description>
///         </listheader>
///         <item>
///             <term>Click</term> <description>Accepts the link, opening the URL (<see cref="Command.Accept"/>).</description>
///         </item>
///     </list>
/// </remarks>
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

    /// <summary>
    ///     Handles activation. If <see cref="View.CanFocus"/> is <see langword="false"/>, delegates to the
    ///     <see cref="Command.HotKey"/> command (which passes focus to the next peer view). Otherwise, uses the
    ///     default activation behavior.
    /// </summary>
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
    ///     Called when the link is accepted (e.g., clicked or <see cref="Command.Accept"/> is invoked).
    ///     Opens <see cref="Url"/> in the default browser via <see cref="OpenUrl"/>.
    /// </summary>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        base.OnAccepted (ctx);

        OpenUrl (Url);
    }

    /// <summary>
    ///     Opens the specified URL in the default web browser using a platform-specific mechanism.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         On Windows, uses <c>cmd /c start</c>. On macOS, uses <c>open</c>. On Linux, uses <c>xdg-open</c>.
    ///     </para>
    ///     <para>
    ///         Ampersands in the URL are escaped on Windows to prevent shell interpretation.
    ///     </para>
    /// </remarks>
    /// <param name="url">The URL to open. Should be a well-formed absolute URI.</param>
    public static void OpenUrl (string url)
    {
        if (!Uri.IsWellFormedUriString (url, UriKind.Absolute))
        {
            return;
        }

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
    ///     Updates the text displayed by the <see cref="View.TextFormatter"/> based on the current values of
    ///     <see cref="View.Text"/> and <see cref="Url"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="View.Text"/> is <see langword="null"/> or empty, the formatter displays <see cref="Url"/>
    ///         instead, ensuring the link auto-sizes correctly via <see cref="DimAutoStyle.Text"/>. Otherwise, the
    ///         base implementation is used.
    ///     </para>
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
    ///     The default value for <see cref="Url"/> — an empty string indicating no URL is associated with the link.
    /// </summary>
    public const string DEFAULT_URL = "";

    private string _url = DEFAULT_URL;

    /// <summary>
    ///     Gets or sets the URL (hyperlink target) associated with this <see cref="Link"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Any string value is accepted. URL validation occurs at draw time: if the value is not a well-formed
    ///         absolute URI (per <see cref="Uri.IsWellFormedUriString"/>), the link renders with the
    ///         <see cref="VisualRole.Disabled"/> style and no OSC 8 hyperlink sequence is emitted.
    ///     </para>
    ///     <para>
    ///         When <see cref="View.Text"/> is empty, <see cref="Url"/> is used as the display text.
    ///     </para>
    ///     <para>
    ///         Setting this property follows the Cancellable Workflow Pattern: <see cref="OnUrlChanging"/> and
    ///         <see cref="UrlChanging"/> fire before the change and can cancel it; <see cref="OnUrlChanged"/> and
    ///         <see cref="UrlChanged"/> fire after. Setting the same value is a no-op.
    ///     </para>
    /// </remarks>
    public string Url { get => _url; set => SetUrl (value); }

    /// <summary>
    ///     Raised when <see cref="Url"/> is about to change. Set <see cref="ValueChangingEventArgs{T}.Handled"/> to
    ///     <see langword="true"/> to cancel the change and keep the current value.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<string>>? UrlChanging;

    /// <summary>
    ///     Raised after <see cref="Url"/> has changed. The <see cref="ValueChangedEventArgs{T}.OldValue"/> and
    ///     <see cref="ValueChangedEventArgs{T}.NewValue"/> properties contain the previous and current values.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<string>>? UrlChanged;

    /// <summary>
    ///     Called before <see cref="Url"/> changes. Override in subclasses to implement validation or cancel the change.
    /// </summary>
    /// <param name="args">
    ///     Contains the current and proposed new values. Set <see cref="ValueChangingEventArgs{T}.Handled"/> to
    ///     <see langword="true"/> to cancel the change.
    /// </param>
    /// <returns><see langword="true"/> to cancel the change; <see langword="false"/> to allow it.</returns>
    protected virtual bool OnUrlChanging (ValueChangingEventArgs<string> args) => false;

    /// <summary>
    ///     Called after <see cref="Url"/> has changed. Override in subclasses to react to URL changes.
    /// </summary>
    /// <param name="args">Contains the old and new URL values.</param>
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

    /// <summary>
    ///     Copies the current <see cref="Url"/> to the system clipboard.
    /// </summary>
    /// <returns><see langword="true"/> if the copy operation was initiated.</returns>
    public bool Copy ()
    {
        SetClipboard (Url);

        return true;
    }

    private void SetClipboard (string text) => App?.Clipboard?.SetClipboardData (text);

    /// <summary>
    ///     Draws the link text with OSC 8 hyperlink sequences when the URL is valid.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="View.Text"/> is empty, <see cref="Url"/> is drawn; otherwise <see cref="View.Text"/> is drawn.
    ///         The displayed text is determined by <see cref="UpdateTextFormatterText"/>.
    ///     </para>
    ///     <para>
    ///         If <see cref="Url"/> is a well-formed absolute URI, the driver's <c>CurrentUrl</c> is set so that
    ///         all drawn cells carry the URL (enabling OSC 8 hyperlink output in supporting terminals).
    ///         If the URL is not well-formed, the text is rendered with the <see cref="VisualRole.Disabled"/> style
    ///         and no hyperlink sequence is emitted.
    ///     </para>
    /// </remarks>
    /// <param name="context">The draw context for tracking drawn regions.</param>
    /// <returns><see langword="true"/> — drawing is always handled by <see cref="Link"/>.</returns>
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
