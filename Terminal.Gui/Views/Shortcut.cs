#nullable enable
using System.ComponentModel;

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
///         right side. Set <see cref="AlignmentModes"/> to <see cref="ViewBase.AlignmentModes.EndToStart"/> to reverse the order.
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
/// </remarks>
public class Shortcut : View, IDesignable
{
    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>.
    /// </summary>
    public Shortcut () : this (Key.Empty, null, null, null) { }

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
        HighlightStates = ViewBase.MouseState.None;
        CanFocus = true;

        if (Border is { })
        {
            Border.Settings &= ~BorderSettings.Title;
        }

        Width = GetWidthDimAuto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        AddCommands ();

        TitleChanged += Shortcut_TitleChanged; // This needs to be set before CommandView is set

        CommandView = new ()
        {
            Id = "CommandView",
            Width = Dim.Auto (),
            Height = Dim.Fill ()
        };
        Title = commandText ?? string.Empty;

        HelpView.Id = "_helpView";
        //HelpView.CanFocus = false;
        HelpView.Text = helpText ?? string.Empty;

        KeyView.Id = "_keyView";
        //KeyView.CanFocus = false;
        key ??= Key.Empty;
        Key = key;

        Action = action;

        ShowHide ();
    }

    /// <inheritdoc />
    protected override bool OnClearingViewport ()
    {
        SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Normal);
        return base.OnClearingViewport ();
    }

    // Helper to set Width consistently
    internal Dim GetWidthDimAuto ()
    {
        return Dim.Auto (
                         DimAutoStyle.Content,
                         minimumContentDim: Dim.Func (_ => _minimumNaturalWidth ?? 0),
                         maximumContentDim: Dim.Func (_ => _minimumNaturalWidth ?? 0))!;
    }

    private AlignmentModes _alignmentModes = AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast;

    // This is used to calculate the minimum width of the Shortcut when Width is NOT Dim.Auto
    // It is calculated by setting Width to DimAuto temporarily and forcing layout.
    // Once Frame.Width gets below this value, LayoutStarted makes HelpView an KeyView smaller.
    private int? _minimumNaturalWidth;

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
        get => _alignmentModes;
        set
        {
            _alignmentModes = value;
            SetCommandViewDefaultLayout ();
            SetHelpViewDefaultLayout ();
            SetKeyViewDefaultLayout ();
        }
    }

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
        CommandView.SetRelativeLayout (Application.Screen.Size);
        HelpView.SetRelativeLayout (Application.Screen.Size);
        KeyView.SetRelativeLayout (Application.Screen.Size);

        _minimumNaturalWidth = PosAlign.CalculateMinDimension (0, SubViews, Dimension.Width);

        // Reset our relative layout
        SetRelativeLayout (SuperView?.GetContentSize () ?? Application.Screen.Size);
    }

    // TODO: Enable setting of the margin thickness
    private Thickness GetMarginThickness ()
    {
        return new (1, 0, 1, 0);
    }

    // When layout starts, we need to adjust the layout of the HelpView and KeyView
    /// <inheritdoc />
    protected override void OnSubViewLayout (LayoutEventArgs e)
    {
        base.OnSubViewLayout (e);

        ShowHide ();
        ForceCalculateNaturalWidth ();

        if (Width is DimAuto widthAuto || HelpView!.Margin is null)
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
                    HelpView.Margin!.Thickness = new (t.Right - 1, t.Top, t.Left - 1, t.Bottom);

                    break;

                case 2:

                    // Scrunch just the right margin
                    HelpView.Margin!.Thickness = new (t.Right, t.Top, t.Left - 1, t.Bottom);

                    break;
            }
        }
        else
        {
            // Reset to default
            HelpView.Margin!.Thickness = GetMarginThickness ();
        }
    }


    #region Accept/Activate/HotKey Command Handling

    private void AddCommands ()
    {
        // Accept (Enter key) -
        //AddCommand (Command.Accept, DispatchCommand);
        // Hotkey -
        //AddCommand (Command.HotKey, DispatchCommand);
        // Activate (Space key or click) -
        // AddCommand (Command.Activate, DispatchCommand);
    }

    /// <inheritdoc />
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        return base.OnHandlingHotKey (args);
    }

    /// <inheritdoc />
    protected override bool OnActivating (CommandEventArgs args)
    {

        return base.OnActivating (args);
    }

    /// <summary>
    ///     Dispatches the Command in the <paramref name="commandContext"/> (Raises Activating, then Accepting, then invoke the Action, if any).
    ///     Called when Command.Activate, Accept, or HotKey has been invoked on this Shortcut.
    /// </summary>
    /// <param name="commandContext"></param>
    /// <returns>
    ///     <see langword="null"/> if no event was raised; input processing should continue.
    ///     <see langword="false"/> if the event was raised and was not handled (or cancelled); input processing should continue.
    ///     <see langword="true"/> if the event was raised and handled (or cancelled); input processing should stop.
    /// </returns>
    internal virtual bool? DispatchCommand (ICommandContext? commandContext)
    {
        CommandContext<KeyBinding>? keyCommandContext = commandContext as CommandContext<KeyBinding>? ?? default (CommandContext<KeyBinding>);

        Logging.Debug ($"{Title} ({commandContext?.Source?.Title}) Command: {commandContext?.Command}");

        //if (keyCommandContext?.Binding.Data != this)
        //{
        //    // TODO: Optimize this to only do this if CommandView is custom (non View)
        //    // Invoke Activate on the CommandView to cause it to change state if it wants to
        //    // If this causes CommandView to raise Accept, we eat it
        //    keyCommandContext = keyCommandContext!.Value with { Binding = keyCommandContext.Value.Binding with { Data = this } };

        //    Logging.Debug ($"{Title} ({commandContext?.Source?.Title}) - Invoking Activate on CommandView ({CommandView.GetType ().Name}).");

        //    if (CommandView.InvokeCommand (Command.Activate, keyCommandContext) is true)
        //    {
        //        return true;
        //    }
        //}

        Logging.Debug ($"{Title} ({commandContext?.Source?.Title}) - RaiseActivating ...");

        if (RaiseActivating (commandContext) is true)
        {
            return true;
        }

        if (CanFocus && SuperView is { CanFocus: true })
        {
            // The default HotKey handler sets Focus
            Logging.Debug ($"{Title} ({commandContext?.Source?.Title}) - SetFocus...");
            SetFocus ();
        }

        var cancel = false;

        //if (commandContext is { Source: null })
        //{
        //    commandContext.Source = this;
        //}
        //Logging.Debug ($"{Title} ({commandContext?.Source?.Title}) - Calling RaiseAccepting...");
        //cancel = RaiseAccepting (commandContext) is true;

        //if (cancel)
        //{
        //    return true;
        //}

        if (Action is { })
        {
            Logging.Debug ($"{Title} ({commandContext?.Source?.Title}) - Invoke Action...");
            Action.Invoke ();

            // Assume if there's a subscriber to Action, it's handled.
            cancel = true;
        }

        return cancel;
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
    ///         <see cref="Application.Force16Colors"/> property.
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
    ///         Application.Force16Colors = cb!.Checked == true;
    ///         Application.Refresh();
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

            if (value == null)
            {
                throw new ArgumentNullException ();
            }

            // Clean up old 
            _commandView.Activating -= CommandViewOnActivating;
            _commandView.Accepting -= CommandViewOnAccepted;
            Remove (_commandView);
            _commandView?.Dispose ();

            // Set new
            _commandView = value;
            _commandView.Id = "_commandView";

            // The default behavior is for CommandView to not get focus. I
            // If you want it to get focus, you need to set it.
            _commandView.CanFocus = false;

            _commandView.HotKeyChanged += (s, e) =>
            {
                if (e.NewKey != Key.Empty)
                {
                    // Add it
                    AddKeyBindingsForHotKey (e.OldKey, e.NewKey);
                }
            };

            _commandView.HotKeySpecifier = new ('_');

            Title = _commandView.Text;

            _commandView.Activating += CommandViewOnActivating;
            _commandView.Accepting += CommandViewOnAccepted;

            ShowHide ();
            UpdateKeyBindings (Key.Empty);

            return;

            void CommandViewOnAccepted (object? sender, CommandEventArgs e)
            {
                // Always eat CommandView.Accept
                e.Handled = true;
            }

            void CommandViewOnActivating (object? sender, CommandEventArgs e)
            {
                if ((e.Context is CommandContext<KeyBinding> keyCommandContext && keyCommandContext.Binding.Data != this) ||
                    e.Context is CommandContext<MouseBinding>)
                {
                    // Forward command to ourselves
                    //InvokeCommand<KeyBinding> (Command.Activate, new ([Command.Activate], null, this));
                }

                // e.Handled = true;
            }
        }
    }

    private void SetCommandViewDefaultLayout ()
    {
        if (CommandView.Margin is { })
        {
            CommandView.Margin!.Thickness = GetMarginThickness ();
            // strip off ViewportSettings.TransparentMouse
            CommandView.Margin!.ViewportSettings &= ~ViewportSettingsFlags.TransparentMouse;
        }

        CommandView.X = Pos.Align (Alignment.End, AlignmentModes);

        CommandView.VerticalTextAlignment = Alignment.Center;
        CommandView.TextAlignment = Alignment.Start;
        CommandView.TextFormatter.WordWrap = false;
        //CommandView.HighlightStates = HighlightStates.None;
        //if (CommandView.InvertFocusAttribute is null)
        //{
        //    CommandView.InvertFocusAttribute = CanFocus;
        //}

        //CommandView.GettingAttributeForRole += SubViewOnGettingAttributeForRole;
    }

    private void SubViewOnGettingAttributeForRole (object? sender, VisualRoleEventArgs e)
    {
        //if (!HasFocus)
        {
            return;
        }

        switch (e.Role)
        {
            case VisualRole.Normal:
                //if (HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.Focus);
                }
                break;

            case VisualRole.HotNormal:
                // if (HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.HotFocus);
                }
                break;

            case VisualRole.Focus:
                //if (HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.Normal);
                }
                break;

            case VisualRole.HotFocus:
                // if (HasFocus)
                {
                    e.Handled = true;
                    e.Result = GetAttributeForRole (VisualRole.HotNormal);
                }
                break;

        }
    }


    private void Shortcut_TitleChanged (object? sender, EventArgs<string> e)
    {
        // If the Title changes, update the CommandView text.
        // This is a helper to make it easier to set the CommandView text.
        // CommandView is public and replaceable, but this is a convenience.
        _commandView.Text = Title;
        //_commandView.Title = Title;
    }

    #endregion Command

    #region Help

    // The maximum width of the HelpView. Calculated in OnLayoutStarted and used in HelpView.Width (Dim.Auto/Func).
    private int _maxHelpWidth = 0;

    /// <summary>
    ///     The subview that displays the help text for the command. Internal for unit testing.
    /// </summary>
    public View HelpView { get; } = new ();

    private void SetHelpViewDefaultLayout ()
    {
        if (HelpView.Margin is { })
        {
            HelpView.Margin!.Thickness = GetMarginThickness ();
            // strip off ViewportSettings.TransparentMouse
            HelpView.Margin!.ViewportSettings &= ~ViewportSettingsFlags.TransparentMouse;
        }

        HelpView.X = Pos.Align (Alignment.End, AlignmentModes);
        _maxHelpWidth = HelpView.Text.GetColumns ();
        HelpView.Width = Dim.Auto (DimAutoStyle.Text, maximumContentDim: Dim.Func ((_ => _maxHelpWidth)));
        HelpView.Height = Dim.Fill ();

        HelpView.Visible = true;
        HelpView.VerticalTextAlignment = Alignment.Center;
        HelpView.TextAlignment = Alignment.Start;
        HelpView.TextFormatter.WordWrap = true;
        HelpView.HighlightStates = ViewBase.MouseState.None;
        //HelpView.InvertFocusAttribute = true;

        //HelpView.GettingAttributeForRole += SubViewOnGettingAttributeForRole;
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

    private Key _key = Key.Empty;

    /// <summary>
    ///     Gets or sets the <see cref="Key"/> that will be bound to the <see cref="Command.Accept"/> command.
    /// </summary>
    public Key Key
    {
        get => _key;
        set
        {
            ArgumentNullException.ThrowIfNull (value);

            Key oldKey = _key;
            _key = value;

            UpdateKeyBindings (oldKey);

            KeyView.Text = Key == Key.Empty ? string.Empty : $"{Key}";
            ShowHide ();
        }
    }

    private bool _bindKeyToApplication = false;

    /// <summary>
    ///     Gets or sets whether <see cref="Key"/> is bound to <see cref="Command"/> via <see cref="View.HotKeyBindings"/> or <see cref="Application.KeyBindings"/>.
    /// </summary>
    public bool BindKeyToApplication
    {
        get => _bindKeyToApplication;
        set
        {
            if (value == _bindKeyToApplication)
            {
                return;
            }

            if (_bindKeyToApplication)
            {
                Application.KeyBindings.Remove (Key);
            }
            else
            {
                HotKeyBindings.Remove (Key);
            }

            _bindKeyToApplication = value;

            UpdateKeyBindings (Key.Empty);
        }
    }

    /// <summary>
    ///     Gets the subview that displays the key. Is drawn with Normal and HotNormal colors reversed.
    /// </summary>

    public View KeyView { get; } = new ();

    private int _minimumKeyTextSize;

    /// <summary>
    ///     Gets or sets the minimum size of the key text. Useful for aligning the key text with other <see cref="Shortcut"/>s.
    /// </summary>
    public int MinimumKeyTextSize
    {
        get => _minimumKeyTextSize;
        set
        {
            if (value == _minimumKeyTextSize)
            {
                //return;
            }

            _minimumKeyTextSize = value;
            SetKeyViewDefaultLayout ();
        }
    }


    private void SetKeyViewDefaultLayout ()
    {
        if (KeyView.Margin is { })
        {
            KeyView.Margin!.Thickness = GetMarginThickness ();
            // strip off ViewportSettings.TransparentMouse
            KeyView.Margin!.ViewportSettings &= ~ViewportSettingsFlags.TransparentMouse;
        }

        // KeyView is sized to hold JUST it's text so that only the text is drawn using HotNormal/HotFocus
        KeyView.X = Pos.Align (Alignment.End, AlignmentModes);
        KeyView.Y = Pos.Center ();
        KeyView.Width = Dim.Auto (DimAutoStyle.Text, minimumContentDim: Dim.Func (_ => MinimumKeyTextSize));
        KeyView.Height = 1;

        KeyView.Visible = true;

        // Right align the text in the keyview
        KeyView.TextAlignment = Alignment.End;
        KeyView.VerticalTextAlignment = Alignment.Center;
        KeyView.KeyBindings.Clear ();
        KeyView.HighlightStates = ViewBase.MouseState.None;
        //KeyView.InvertFocusAttribute = true;

        KeyView.DrawingText += (sender, args) =>
        {
            var drawRect = new Rectangle (KeyView.ContentToScreen (Point.Empty), KeyView.GetContentSize ());

            Region textRegion = KeyView.TextFormatter.GetDrawRegion (drawRect);
            KeyView.TextFormatter?.Draw (
                                         drawRect,
                                         HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal),
                                         HasFocus ? GetAttributeForRole (VisualRole.HotFocus) : GetAttributeForRole (VisualRole.HotNormal),
                                         Rectangle.Empty
                                        );

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
                Application.KeyBindings.Remove (oldKey);
            }

            Application.KeyBindings.Remove (Key);
            Application.KeyBindings.Add (Key, this, Command.HotKey);
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

    /// <inheritdoc />
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        //if (!HasFocus)
        {
            return base.OnGettingAttributeForRole (role, ref currentAttribute);
        }


        if (role == VisualRole.Normal)
        {
            currentAttribute = GetAttributeForRole (VisualRole.Focus);

            return true;
        }
        if (role == VisualRole.HotNormal)
        {
            currentAttribute = GetAttributeForRole (VisualRole.HotFocus);

            return true;
        }

        return base.OnGettingAttributeForRole (role, ref currentAttribute);
    }

    #endregion Focus

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Title = "_Shortcut";
        HelpText = "Shortcut help";
        Key = Key.F1;

        return true;
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
}