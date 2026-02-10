using System.Diagnostics;

namespace Terminal.Gui.Views;

/// <summary>
///     Displays a command, help text, and a key binding. When the key specified by <see cref="Key"/> is pressed, the
///     command will be invoked. Useful for
///     displaying a command in <see cref="Bar"/> such as a
///     menu, toolbar, or status bar.
/// </summary>
/// <remarks>
///     <para>
///         The following user actions will invoke the <see cref="Command.Accept"/>, causing the
///         <see cref="View.Accepting"/> event to be fired:
///         - Clicking on the <see cref="Shortcut"/>.
///         - Pressing the key specified by <see cref="Key"/>.
///         - Pressing the HotKey specified by <see cref="CommandView"/>.
///     </para>
///     <para>
///         If <see cref="BindKeyToApplication"/> is <see langword="true"/>, <see cref="Key"/> will invoke
///         <see cref="Command.Accept"/>
///         regardless of what View has focus, enabling an application-wide keyboard shortcut.
///     </para>
///     <para>
///         By default, a Shortcut displays the command text on the left side, the help text in the middle, and the key
///         binding on the
///         right side. Set <see cref="AlignmentModes"/> to <see cref="ViewBase.AlignmentModes.EndToStart"/> to reverse the
///         order.
///     </para>
///     <para>
///         The command text can be set by setting the <see cref="CommandView"/>'s Text property or by setting
///         <see cref="View.Title"/>.
///     </para>
///     <para>
///         The help text can be set by setting the <see cref="HelpText"/> property or by setting <see cref="View.Text"/>.
///     </para>
///     <para>
///         The key text is set by setting the <see cref="Key"/> property.
///         If the <see cref="Key"/> is <see cref="Key.Empty"/>, the <see cref="Key"/> text is not displayed.
///     </para>
///     <para>
///         <see cref="View.MouseHighlightStates"/> defaults to <see cref="MouseState.In"/>, causing the Shortcut to
///         highlight when the mouse is over it.
///     </para>
///     <para>
///         When the <see cref="CommandView"/> raises <see cref="View.Activating"/> (e.g., when clicked), the Shortcut
///         will also raise <see cref="View.Activating"/>. Similarly, when the <see cref="CommandView"/> raises
///         <see cref="View.Accepting"/> (e.g., double-click on a <see cref="CheckBox"/>), the Shortcut will also raise
///         <see cref="View.Accepting"/>.
///     </para>
/// </remarks>
public class Shortcut : View, IOrientation, IDesignable
{
    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>.
    /// </summary>
    public Shortcut () : this (Key.Empty, null, null) { }

    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper API that mimics the V1 API for creating StatusItems.
    ///     </para>
    /// </remarks>
    /// <param name="key"></param>
    /// <param name="commandText">The text to display for the command.</param>
    /// <param name="action"></param>
    /// <param name="helpText">The help text to display.</param>
    public Shortcut (Key key, string? commandText, Action? action, string? helpText = null)
    {
        MouseHighlightStates = MouseState.In;
        CanFocus = true;

        Border?.Settings &= ~BorderSettings.Title;

        Width = GetWidthDimAuto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new OrientationHelper (this);
        _orientationHelper.OrientationChanging += (_, e) => OrientationChanging?.Invoke (this, e);
        _orientationHelper.OrientationChanged += (_, e) => OrientationChanged?.Invoke (this, e);

        CommandsToBubbleUp = [Command.Activate, Command.Accept];

        TitleChanged += Shortcut_TitleChanged; // This needs to be set before CommandView is set

        CommandView = new View
        {
#if DEBUG
            Id = "CommandView",
#endif
            Width = Dim.Auto (),
            Height = Dim.Fill ()
        };
        Title = commandText ?? string.Empty;

#if DEBUG
        HelpView.Id = "_helpView";
#endif
        HelpView.Text = helpText ?? string.Empty;

#if DEBUG
        KeyView.Id = "_keyView";
#endif
        Key = key;

        Action = action;

        ShowHide ();
    }

    // This is used to calculate the minimum width of the Shortcut when Width is NOT Dim.Auto
    // It is calculated by setting Width to DimAuto temporarily and forcing layout.
    // Once Frame.Width gets below this value, LayoutStarted makes HelpView an KeyView smaller.
    private int? _minimumNaturalWidth;

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Title = "_Shortcut";
        HelpText = "Shortcut help";
        Key = Key.F1;

        return true;
    }

    /// <summary>
    ///     Gets or sets the <see cref="AlignmentModes"/> for this <see cref="Shortcut"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The default is <see cref="AlignmentModes.StartToEnd"/>. This means that the CommandView will be on the left,
    ///         HelpView in the middle, and KeyView on the right.
    ///     </para>
    /// </remarks>
    public AlignmentModes AlignmentModes
    {
        get;
        set
        {
            field = value;
            SetCommandViewDefaultLayout ();
            SetHelpViewDefaultLayout ();
            SetKeyViewDefaultLayout ();
        }
    } = AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();
        App ??= SuperView?.App; // HACK: Remove once legacy static Application is gone
        Debug.Assert (App is { });
        UpdateKeyBindings (Key.Empty);
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            TitleChanged -= Shortcut_TitleChanged;

            if (CommandView.SuperView is null)
            {
                CommandView.Dispose ();
            }

            if (HelpView.SuperView is null)
            {
                HelpView.Dispose ();
            }

            if (KeyView.SuperView is null)
            {
                KeyView.Dispose ();
            }
        }

        base.Dispose (disposing);
    }

    // When layout starts, we need to adjust the layout of the HelpView and KeyView
    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs e)
    {
        base.OnSubViewLayout (e);

        ShowHide ();
        ForceCalculateNaturalWidth ();

        if (Width.Has<DimAuto> (out _) || HelpView.Margin is null)
        {
            return;
        }

        // Frame.Width is smaller than the natural width. Reduce width of HelpView.
        _maxHelpWidth = int.Max (0, GetContentSize ().Width - CommandView.Frame.Width - KeyView.Frame.Width);

        if (_maxHelpWidth < 3)
        {
            Thickness t = GetMarginThickness ();

            switch (_maxHelpWidth)
            {
                case 0:
                case 1:
                    // Scrunch it by removing both margins
                    HelpView.Margin!.Thickness = new Thickness (t.Right - 1, t.Top, t.Left - 1, t.Bottom);

                    break;

                case 2:

                    // Scrunch just the right margin
                    HelpView.Margin!.Thickness = new Thickness (t.Right, t.Top, t.Left - 1, t.Bottom);

                    break;
            }
        }
        else
        {
            // Reset to default
            HelpView.Margin!.Thickness = GetMarginThickness ();

            // Margin must be transparent to mouse, so clicks pass through to Shortcut
            HelpView.Margin!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        }
    }

    // Helper to set Width consistently
    internal Dim GetWidthDimAuto () => Dim.Auto (DimAutoStyle.Content, Dim.Func (_ => _minimumNaturalWidth ?? 0), Dim.Func (_ => _minimumNaturalWidth ?? 0));

    // When one of the subviews is "empty" we don't want to show it. So we
    // Use Add/Remove. We need to be careful to add them in the right order
    // so Pos.Align works correctly.
    internal void ShowHide ()
    {
        RemoveAll ();

        if (CommandView.Visible)
        {
            Add (CommandView);
            SetCommandViewDefaultLayout ();
        }

        if (HelpView.Visible && !string.IsNullOrEmpty (HelpView.Text))
        {
            Add (HelpView);
            SetHelpViewDefaultLayout ();
        }

        if (KeyView.Visible && (Key != Key.Empty || KeyView.Text != string.Empty))
        {
            Add (KeyView);
            SetKeyViewDefaultLayout ();
        }
    }

    // Force Width to DimAuto to calculate natural width and then set it back
    private void ForceCalculateNaturalWidth ()
    {
        // Get the natural size of each subview
        Size screenSize = App?.Screen.Size ?? new Size (2048, 2048);
        CommandView.SetRelativeLayout (screenSize);
        HelpView.SetRelativeLayout (screenSize);
        KeyView.SetRelativeLayout (screenSize);

        _minimumNaturalWidth = PosAlign.CalculateMinDimension (0, SubViews, Dimension.Width);

        // Reset our relative layout
        SetRelativeLayout (SuperView?.GetContentSize () ?? screenSize);
    }

    // TODO: Enable setting of the margin thickness
    private Thickness GetMarginThickness () => new (1, 0, 1, 0);

    #region Accept/Select/HotKey Command Handling

    /// <inheritdoc/>
    protected override bool OnActivating (CommandEventArgs args)
    {
        if (base.OnActivating (args))
        {
            return true;
        }
        Logging.Debug ($"{this.ToIdentifyingString ()} ({args})");

        // If the USER did something to activate the CommandView dispatch it back to the
        // CommandView and re-invoke on self.
        if (DispatchCommandFromSubview (CommandView, args.Context!))
        {
            return true;
        }

        // If the USER did something to activate us or one of our non-CommandView subviews,
        // dispatch it to the CommandView and re-invoke on self.
        if (DispatchCommandFromSelf (CommandView, args.Context!))
        {
            return true;
        }

        // If we got here, the Accept came from another view or was directly invoked.
        Logging.Debug ($"{this.ToIdentifyingString ()} ({args}) - returning false.");

        return false;
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        base.OnActivated (ctx);
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - Invoke Action...");
        Action?.Invoke ();
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        if (base.OnAccepting (args))
        {
            return true;
        }
        Logging.Debug ($"{this.ToIdentifyingString ()} ({args})");

        // If the USER did something to accept the CommandView dispatch it back to the
        // CommandView and re-invoke on self.
        if (DispatchCommandFromSubview (CommandView, args.Context!))
        {
            return true;
        }

        // If the USER did something to accept us or one of our non-CommandView subviews,
        // dispatch it to the CommandView and re-invoke on self.
        if (DispatchCommandFromSelf (CommandView, args.Context!))
        {
            return true;
        }

        // If we got here, the Accept came from another view or was directly invoked.
        Logging.Debug ($"{this.ToIdentifyingString ()} ({args}) - returning false.");

        return false;
    }

    /// <inheritdoc/>
    protected override void OnAccepted (CommandEventArgs args) => Action?.Invoke ();

    /// <inheritdoc/>
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        if (base.OnHandlingHotKey (args) is true)
        {
            return true;
        }

        return InvokeCommand (Command.Activate, args.Context) is true;
    }

    /// <summary>
    ///     Dispatches the command to a specified subview if the command binding source was not `this`.
    /// </summary>
    /// <param name="subViewToDispatch"></param>
    /// <param name="ctx"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool DispatchCommandFromSelf (View subViewToDispatch, ICommandContext ctx)
    {
        if (!IsBindingFromSelf (ctx))
        {
            return false;
        }
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx})");

        // The user did something in the Shortcut/KeyView/HelpView.
        // - Disable Bubbling & Invoke on the command view so that it can update its state if needed (e.g., toggle a CheckBox).
        //   Ignore the return.
        // - Re-enable Bubbling.
        // - Invoke command on this, with no Binding.Source set. Ignore the return value.
        // - Return true to stop processing.

        // Disable bubbling
        IReadOnlyList<Command> tempCommandsToBubbleUp = CommandsToBubbleUp;
        CommandsToBubbleUp = [];
        ICommandContext context = new CommandContext (ctx.Command, null, null);

        if (subViewToDispatch.InvokeCommand (ctx.Command, context) is true)
        {
            // This is not expected;
            throw new InvalidOperationException ("subViewToDispatch.InvokeCommand() returned true unexpectedly.");
        }
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - Back from subViewToDispatch.InvokeCommand");
        CommandsToBubbleUp = tempCommandsToBubbleUp;

        return false;
    }

    /// <summary>
    ///     Dispatches the command to the specified subview, if the command was not from the subview.
    /// </summary>
    /// <param name="subView"></param>
    /// <param name="ctx"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool DispatchCommandFromSubview (View subView, ICommandContext ctx)
    {
        if (!IsFromCommandView (ctx))
        {
            return false;
        }
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - IsFromCommandView");

        // The user did something in the CommandView (e.g., clicked on it or pressed its hotkey). We got here because CommandsToBubbleUp
        // includes Command.Activate, so the event bubbled up to Shortcut.
        // We're going to cancel the CommandView's Activating and raise our own so that the Shortcut gets focus and can update its state if needed.

        // Set the context source to the Shortcut (this) so that when we invoke Activate below on the CommandView, and the command bubbles to us again,
        // we can detect that it originated from us and not from the CommandView with IsInvocationFromShortcut below.
        ICommandContext context = new CommandContext (ctx.Command, new WeakReference<View> (this), ctx.Binding);

        // Disable bubbling
        // TODO: Make API for CommandsToBubbleUp richer. Support adding & removing commands
        IReadOnlyList<Command> tempCommandsToBubbleUp = CommandsToBubbleUp;
        CommandsToBubbleUp = [];

        if (subView.InvokeCommand (ctx.Command, context) is true)
        {
            // This is not expected;
            throw new InvalidOperationException ("subView.InvokeCommand() returned true unexpectedly.");
        }
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - Back from subView.InvokeCommand");
        CommandsToBubbleUp = tempCommandsToBubbleUp;

        // By setting the Binding source to null, neither IsFromCommandView nor IsBindingFromShortcut will return true,
        // letting the resulting call to this method to fall through; returning false.
        InvokeCommand (ctx.Command, new CommandContext (ctx.Command, new WeakReference<View> (subView), new CommandBinding ([ctx.Command])));
        Logging.Debug ($"{this.ToIdentifyingString ()} ({ctx}) - Back from InvokeCommand");

        // When the above InvokeCommand returns, the CommandView should have changed state (e.g., a CheckBox may have toggled its checked state).
        // The call to this OnActivating should signal the event was handled, so the state doesn't change again.

        return true;
    }

    /// <summary>
    ///     Gets or sets the action to be invoked when the shortcut key is pressed or the shortcut is clicked on with the
    ///     mouse.
    /// </summary>
    /// <remarks>
    ///     Note, the <see cref="View.Accepting"/> event is fired first, and if cancelled, the event will not be invoked.
    /// </remarks>
    public Action? Action { get; set; }

    #endregion Accept/Select/HotKey Command Handling

    #region IOrientation members

    private readonly OrientationHelper _orientationHelper;

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="Bar"/>. The default is
    ///     <see cref="Orientation.Horizontal"/>.
    /// </summary>
    /// <remarks>
    /// </remarks>
    public Orientation Orientation { get => _orientationHelper.Orientation; set => _orientationHelper.Orientation = value; }

    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation) =>

        // TODO: Determine what, if anything, is opinionated about the orientation.
        SetNeedsLayout ();

    #endregion

    #region Command

    private View _commandView = new ();

    /// <summary>
    ///     Gets or sets the View that displays the command text and hotkey.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <remarks>
    ///     <para>
    ///         By default, the <see cref="View.Title"/> of the <see cref="CommandView"/> is displayed as the Shortcut's
    ///         command text.
    ///     </para>
    ///     <para>
    ///         By default, the CommandView is a <see cref="View"/> with <see cref="View.CanFocus"/> set to
    ///         <see langword="false"/>.
    ///     </para>
    ///     <para>
    ///         Setting the <see cref="CommandView"/> will add it to the <see cref="Shortcut"/> and remove any existing
    ///         <see cref="CommandView"/>.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <para>
    ///         This example illustrates how to add a <see cref="Shortcut"/> to a <see cref="StatusBar"/> that toggles the
    ///         <see cref="IDriver.Force16Colors"/> property.
    ///     </para>
    ///     <code>
    ///     var force16ColorsShortcut = new Shortcut
    ///     {
    ///         Key = Key.F6,
    ///         KeyBindingScope = KeyBindingScope.HotKey,
    ///         CommandView = new CheckBox { Text = "Force 16 Colors" }
    ///     };
    ///     var cb = force16ColorsShortcut.CommandView as CheckBox;
    ///     cb.Checked = Application.Force16Colors;
    /// 
    ///     cb.Toggled += (s, e) =>
    ///     {
    ///         var cb = s as CheckBox;
    ///         App.Driver.Force16Colors = cb!.Checked == true;
    ///         App.river.Refresh();
    ///     };
    ///     StatusBar.Add(force16ColorsShortcut);
    /// </code>
    /// </example>
    public View CommandView
    {
        get => _commandView;
        set
        {
            ArgumentNullException.ThrowIfNull (value);

            // Clean up old
            _commandView.HotKeyChanged -= OnCommandViewOnHotKeyChanged;
            Remove (_commandView);
            _commandView.Dispose ();

            // Set new
            _commandView = value;

#if DEBUG
            if (string.IsNullOrEmpty (_commandView.Id))
            {
                _commandView.Id = "_commandView";
            }
#endif
            _commandView.HotKeyChanged += OnCommandViewOnHotKeyChanged;

            _commandView.HotKeySpecifier = new Rune ('_');

            Title = _commandView.Text;

            UpdateKeyBindings (Key.Empty);
            ShowHide ();

            return;

            void OnCommandViewOnHotKeyChanged (object? _, KeyChangedEventArgs e)
            {
                if (e.NewKey != Key.Empty)
                {
                    // Add it
                    AddKeyBindingsForHotKey (e.OldKey, e.NewKey);
                }
            }
        }
    }

    private bool IsBindingFromKeyView (ICommandContext ctx) =>

        // Source == this means the event originated from clicking on Shortcut (not CommandView)
        ctx.Binding?.Source is { } sourceView && sourceView == KeyView;

    private bool IsBindingFromHelpView (ICommandContext ctx) =>

        // Source == this means the event originated from clicking on Shortcut (not CommandView)
        ctx.Binding?.Source is { } sourceView && sourceView == HelpView;

    // Helper to check if command originated from Shortcut, the HelpView, or the KeyView mouse or keyboard binding
    private bool IsBindingFromSelf (ICommandContext ctx)
    {
        if (IsBindingFromKeyView (ctx))
        {
            return true;
        }

        if (IsBindingFromHelpView (ctx))
        {
            return true;
        }

        // Source == this means the event originated from clicking on Shortcut (not CommandView)
        return ctx.Binding?.Source is { } sourceView && sourceView == this;
    }

    // Helper to check if command context originated from the CommandView
    // Both the command source and binding source must be from the CommandView
    private bool IsFromCommandView (ICommandContext? ctx) =>
        ctx?.TryGetSource (out View? ctxSource) is true && ctxSource == CommandView && ctx.Binding?.Source == CommandView;

    private void SetCommandViewDefaultLayout ()
    {
        if (CommandView.Margin is { })
        {
            CommandView.Margin!.Thickness = GetMarginThickness ();

            // Margin must be transparent to mouse, so clicks pass through to Shortcut
            CommandView.Margin!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        }

        CommandView.X = Pos.Align (Alignment.End, AlignmentModes);

        CommandView.VerticalTextAlignment = Alignment.Center;
        CommandView.TextAlignment = Alignment.Start;
        CommandView.TextFormatter.WordWrap = false;

        CommandView.MouseHighlightStates = MouseState.None;
        CommandView.GettingAttributeForRole += SubViewOnGettingAttributeForRole;
    }

    private void SubViewOnGettingAttributeForRole (object? sender, VisualRoleEventArgs e)
    {
        switch (e.Role)
        {
            case VisualRole.Normal:
                if (HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.Focus);
                }

                break;

            case VisualRole.HotNormal:
                if (HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.HotFocus);
                }

                break;
        }
    }

    private void Shortcut_TitleChanged (object? sender, EventArgs<string> e) =>

        // If the Title changes, update the CommandView text.
        // This is a helper to make it easier to set the CommandView text.
        // CommandView is public and replaceable, but this is a convenience.
        _commandView.Text = Title;

    #endregion Command

    #region Help

    // The maximum width of the HelpView. Calculated in OnLayoutStarted and used in HelpView.Width (Dim.Auto/Func).
    private int _maxHelpWidth;

    /// <summary>
    ///     The subview that displays the help text for the command. Internal for unit testing.
    /// </summary>
    public View HelpView { get; } = new ();

    private void SetHelpViewDefaultLayout ()
    {
        if (HelpView.Margin is { })
        {
            HelpView.Margin!.Thickness = GetMarginThickness ();

            // Margin must be transparent to mouse, so clicks pass through to Shortcut
            HelpView.Margin!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        }

        HelpView.X = Pos.Align (Alignment.End, AlignmentModes);
        _maxHelpWidth = HelpView.Text.GetColumns ();
        HelpView.Width = Dim.Auto (DimAutoStyle.Text, maximumContentDim: Dim.Func (_ => _maxHelpWidth));
        HelpView.Height = Dim.Fill ();

        HelpView.Visible = true;
        HelpView.VerticalTextAlignment = Alignment.Center;
        HelpView.TextAlignment = Alignment.Start;
        HelpView.TextFormatter.WordWrap = false;
        HelpView.MouseHighlightStates = MouseState.None;

        HelpView.GettingAttributeForRole += SubViewOnGettingAttributeForRole;
    }

    /// <summary>
    ///     Gets or sets the help text displayed in the middle of the Shortcut. Identical in function to <see cref="HelpText"/>
    ///     .
    /// </summary>
    public override string Text
    {
        get => HelpView.Text;
        set
        {
            HelpView.Text = value;
            ShowHide ();
        }
    }

    /// <summary>
    ///     Gets or sets the help text displayed in the middle of the Shortcut.
    /// </summary>
    public string HelpText
    {
        get => HelpView.Text;
        set
        {
            HelpView.Text = value;
            ShowHide ();
        }
    }

    #endregion Help

    #region Key

    /// <summary>
    ///     Gets or sets the <see cref="Key"/> that will be bound to the <see cref="Command.Accept"/> command.
    /// </summary>
    public Key Key
    {
        get => field ?? Key.Empty;
        set
        {
            Key oldKey = field ?? Key.Empty;
            field = value;

            UpdateKeyBindings (oldKey);

            KeyView.Text = Key == Key.Empty ? string.Empty : $"{Key}";
            ShowHide ();
        }
    }

    /// <summary>
    ///     Gets or sets whether <see cref="Key"/> is bound to <see cref="Command"/> via <see cref="View.HotKeyBindings"/> or
    ///     <see cref="KeyBindings"/>.
    /// </summary>
    public bool BindKeyToApplication
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            if (field)
            {
                App?.Keyboard.KeyBindings.Remove (Key);
            }
            else
            {
                HotKeyBindings.Remove (Key);
            }

            field = value;

            UpdateKeyBindings (Key.Empty);
        }
    }

    /// <summary>
    ///     Gets the subview that displays the key. Is drawn with Normal and HotNormal colors reversed.
    /// </summary>

    public View KeyView { get; } = new ();

    /// <summary>
    ///     Gets or sets the minimum size of the key text. Useful for aligning the key text with other <see cref="Shortcut"/>s.
    /// </summary>
    public int MinimumKeyTextSize
    {
        get;
        set
        {
            if (value == field)
            {
                return;
            }

            field = value;
            SetKeyViewDefaultLayout ();
        }
    }

    private void SetKeyViewDefaultLayout ()
    {
        if (KeyView.Margin is { })
        {
            KeyView.Margin!.Thickness = GetMarginThickness ();

            // Margin must be transparent to mouse, so clicks pass through to Shortcut
            KeyView.Margin!.ViewportSettings |= ViewportSettingsFlags.TransparentMouse;
        }

        KeyView.X = Pos.Align (Alignment.End, AlignmentModes);
        KeyView.Width = Dim.Auto (DimAutoStyle.Text, Dim.Func (_ => MinimumKeyTextSize));
        KeyView.Height = Dim.Fill ();

        KeyView.Visible = true;

        // Right align the text
        KeyView.TextAlignment = Alignment.End;
        KeyView.VerticalTextAlignment = Alignment.Center;
        KeyView.KeyBindings.Clear ();
        KeyView.MouseHighlightStates = MouseState.None;

        KeyView.GettingAttributeForRole += (_, args) =>
                                           {
                                               if (args.Role != VisualRole.Normal)
                                               {
                                                   return;
                                               }

                                               args.Result = SuperView?.GetAttributeForRole (HasFocus ? VisualRole.HotFocus : VisualRole.HotNormal)
                                                             ?? Attribute.Default;
                                               args.Handled = true;
                                           };

        KeyView.ClearingViewport += (_, args) =>
                                    {
                                        // Do not clear; otherwise spaces will be printed with underlines
                                        args.Cancel = true;
                                    };
    }

    private void UpdateKeyBindings (Key oldKey)
    {
        if (!Key.IsValid)
        {
            return;
        }

        if (BindKeyToApplication)
        {
            if (oldKey != Key.Empty)
            {
                App?.Keyboard.KeyBindings.Remove (oldKey);
            }

            App?.Keyboard.KeyBindings.Remove (Key);

            // Use the form of Add that provides target since this is an app-level hotkey
            App?.Keyboard.KeyBindings.Add (Key, this, Command.HotKey);
        }
        else
        {
            if (oldKey != Key.Empty)
            {
                HotKeyBindings.Remove (oldKey);
            }

            HotKeyBindings.Remove (Key);
            HotKeyBindings.Add (Key, Command.HotKey);
        }
    }

    #endregion Key

    #region Focus

    /// <summary>
    ///     TODO: IS this needed?
    /// </summary>
    public bool ForceFocusColors
    {
        get;
        set
        {
            field = value;
            SetNeedsDraw ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (!HasFocus)
        {
            return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }

        switch (role)
        {
            case VisualRole.Normal:
                currentAttribute = GetAttributeForRole (VisualRole.Focus);

                return true;

            case VisualRole.HotNormal:
                currentAttribute = GetAttributeForRole (VisualRole.HotFocus);

                return true;

            default: return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }
    }

    #endregion Focus
}
