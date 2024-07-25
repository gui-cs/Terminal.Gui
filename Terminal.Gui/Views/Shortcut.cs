using System.ComponentModel;
using System.Threading.Channels;

namespace Terminal.Gui;

/// <summary>
///     Displays a command, help text, and a key binding. When the key specified by <see cref="Key"/> is pressed, the command will be invoked. Useful for
///     displaying a command in <see cref="Bar"/> such as a
///     menu, toolbar, or status bar.
/// </summary>
/// <remarks>
///     <para>
///         The following user actions will invoke the <see cref="Command.Accept"/>, causing the
///         <see cref="View.Accept"/> event to be fired:
/// - Clicking on the <see cref="Shortcut"/>.
/// - Pressing the key specified by <see cref="Key"/>.
/// - Pressing the HotKey specified by <see cref="CommandView"/>.
///     </para>
///     <para>
///         If <see cref="KeyBindingScope"/> is <see cref="KeyBindingScope.Application"/>, <see cref="Key"/> will invoked <see cref="Command.Accept"/>
///         command regardless of what View has focus, enabling an application-wide keyboard shortcut.
///     </para>
///     <para>
///         By default, a Shortcut displays the command text on the left side, the help text in the middle, and the key
///         binding on the
///         right side. Set <see cref="AlignmentModes"/> to <see cref="AlignmentModes.EndToStart"/> to reverse the order.
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
public class Shortcut : View
{
    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This is a helper API that mimics the V1 API for creating StatusItems.
    ///     </para>
    /// </remarks>
    /// <param name="key"></param>
    /// <param name="commandText"></param>
    /// <param name="action"></param>
    /// <param name="helpText"></param>
    public Shortcut (Key key, string commandText, Action action, string helpText = null)
    {
        Id = "_shortcut";
        HighlightStyle = HighlightStyle.Pressed;
        Highlight += Shortcut_Highlight;
        CanFocus = true;
        Width = GetWidthDimAuto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        AddCommand (Command.HotKey, ctx => OnAccept (ctx));
        AddCommand (Command.Accept, ctx => OnAccept (ctx));
        AddCommand (Command.Select, ctx => OnSelect (ctx));
        KeyBindings.Add (KeyCode.Enter, Command.Accept);
        KeyBindings.Add (KeyCode.Space, Command.Select);

        TitleChanged += Shortcut_TitleChanged; // This needs to be set before CommandView is set

        CommandView = new ()
        {
            Width = Dim.Auto (),
            Height = Dim.Auto (DimAutoStyle.Auto, minimumContentDim: 1)
        };

        HelpView.Id = "_helpView";
        HelpView.CanFocus = false;
        HelpView.Text = helpText;
        Add (HelpView);

        KeyView.Id = "_keyView";
        KeyView.CanFocus = false;
        Add (KeyView);

        // If the user clicks anywhere on the Shortcut, other than the CommandView, invoke the Command
        MouseClick += Shortcut_MouseClick;
        HelpView.MouseClick += Subview_MouseClick;
        KeyView.MouseClick += Subview_MouseClick;
        LayoutStarted += OnLayoutStarted;
        Initialized += OnInitialized;

        if (key is null)
        {
            key = Key.Empty;
        }

        Key = key;
        Title = commandText;
        Action = action;

        return;

        void OnInitialized (object sender, EventArgs e)
        {
            SuperViewRendersLineCanvas = true;
            Border.Settings &= ~BorderSettings.Title;

            ShowHide ();

            // Force Width to DimAuto to calculate natural width and then set it back
            Dim savedDim = Width;
            Width = GetWidthDimAuto ();
            _minimumDimAutoWidth = Frame.Width;
            Width = savedDim;

            SetCommandViewDefaultLayout ();
            SetHelpViewDefaultLayout ();
            SetKeyViewDefaultLayout ();

            SetColors ();
        }

        // Helper to set Width consistently
        Dim GetWidthDimAuto ()
        {
            // TODO: PosAlign.CalculateMinDimension is a hack. Need to figure out a better way of doing this.
            return Dim.Auto (
                             DimAutoStyle.Content,
                             Dim.Func (() => PosAlign.CalculateMinDimension (0, Subviews, Dimension.Width)),
                             Dim.Func (() => PosAlign.CalculateMinDimension (0, Subviews, Dimension.Width)));
        }
    }


    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>.
    /// </summary>
    public Shortcut () : this (Key.Empty, string.Empty, null) { }

    private Orientation _orientation = Orientation.Horizontal;

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="Shortcut"/>. The default is
    ///     <see cref="Orientation.Horizontal"/>, which is ideal for status bar, menu bar, and tool bar items If set to
    ///     <see cref="Orientation.Vertical"/>,
    ///     the Shortcut will be configured for vertical layout, which is ideal for menu items.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            _orientation = value;

            // TODO: Determine what, if anything, is opinionated about the orientation.
        }
    }

    private AlignmentModes _alignmentModes = AlignmentModes.StartToEnd | AlignmentModes.IgnoreFirstOrLast;

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
        }

        if (HelpView.Visible && !string.IsNullOrEmpty (HelpView.Text))
        {
            Add (HelpView);
        }

        if (KeyView.Visible && Key != Key.Empty)
        {
            Add (KeyView);
        }
    }

    // This is used to calculate the minimum width of the Shortcut when the width is NOT Dim.Auto
    private int? _minimumDimAutoWidth;

    // When layout starts, we need to adjust the layout of the HelpView and KeyView
    private void OnLayoutStarted (object sender, LayoutEventArgs e)
    {
        if (Width is DimAuto widthAuto)
        {
            _minimumDimAutoWidth = Frame.Width;
        }
        else
        {
            if (string.IsNullOrEmpty (HelpView.Text))
            {
                return;
            }

            int currentWidth = Frame.Width;

            // If our width is smaller than the natural width then reduce width of HelpView first.
            // Then KeyView.
            // Don't ever reduce CommandView (it should spill).
            // When Horizontal, Key is first, then Help, then Command.
            // When Vertical, Command is first, then Help, then Key.
            // BUGBUG: This does not do what the above says.
            // TODO: Add Unit tests for this.
            if (currentWidth < _minimumDimAutoWidth)
            {
                int delta = _minimumDimAutoWidth.Value - currentWidth;
                int maxHelpWidth = int.Max (0, HelpView.Text.GetColumns () + Margin.Thickness.Horizontal - delta);

                switch (maxHelpWidth)
                {
                    case 0:
                        // Hide HelpView
                        HelpView.Visible = false;
                        HelpView.X = 0;

                        break;

                    case 1:
                        // Scrunch it by removing margins
                        HelpView.Margin.Thickness = new (0, 0, 0, 0);

                        break;

                    case 2:
                        // Scrunch just the right margin
                        Thickness t = GetMarginThickness ();
                        HelpView.Margin.Thickness = new (t.Right, t.Top, t.Left - 1, t.Bottom);

                        break;

                    default:
                        // Default margin
                        HelpView.Margin.Thickness = GetMarginThickness ();

                        break;
                }

                if (maxHelpWidth > 0)
                {
                    HelpView.X = Pos.Align (Alignment.End, AlignmentModes);

                    // Leverage Dim.Auto's max:
                    HelpView.Width = Dim.Auto (DimAutoStyle.Text, maximumContentDim: maxHelpWidth);
                    HelpView.Visible = true;
                }
            }
            else
            {
                // Reset to default
                //SetCommandViewDefaultLayout();
                SetHelpViewDefaultLayout ();

                //SetKeyViewDefaultLayout ();
            }
        }
    }

    private Thickness GetMarginThickness ()
    {
        if (Orientation == Orientation.Vertical)
        {
            return new (1, 0, 1, 0);
        }

        return new (1, 0, 1, 0);
    }

    private Color? _savedForeColor;

    private void Shortcut_Highlight (object sender, CancelEventArgs<HighlightStyle> e)
    {
        if (e.CurrentValue.HasFlag (HighlightStyle.Pressed))
        {
            if (!_savedForeColor.HasValue)
            {
                _savedForeColor = base.ColorScheme.Normal.Foreground;
            }

            var cs = new ColorScheme (base.ColorScheme)
            {
                Normal = new (ColorScheme.Normal.Foreground.GetHighlightColor (), base.ColorScheme.Normal.Background)
            };
            base.ColorScheme = cs;
        }

        if (e.CurrentValue == HighlightStyle.None && _savedForeColor.HasValue)
        {
            var cs = new ColorScheme (base.ColorScheme)
            {
                Normal = new (_savedForeColor.Value, base.ColorScheme.Normal.Background)
            };
            base.ColorScheme = cs;
        }

        SuperView?.SetNeedsDisplay ();
        e.Cancel = true;
    }

    private void Shortcut_MouseClick (object sender, MouseEventEventArgs e)
    {
        // When the Shortcut is clicked, we want to invoke the Command and Set focus
        var view = sender as View;

        if (!e.Handled)
        {
            // If the subview (likely CommandView) didn't handle the mouse click, invoke the command.
            e.Handled = InvokeCommand (Command.Accept) == true;
        }

        if (CanFocus)
        {
            SetFocus ();
        }
    }

    private void Subview_MouseClick (object sender, MouseEventEventArgs e)
    {
        // TODO: Remove. This does nothing.
        return;
    }

    #region Command

    private View _commandView = new ();

    /// <summary>
    ///     Gets or sets the View that displays the command text and hotkey.
    /// </summary>
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
            if (value == null)
            {
                throw new ArgumentNullException ();
            }

            if (_commandView is { })
            {
                Remove (_commandView);
                _commandView?.Dispose ();
            }

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

            SetCommandViewDefaultLayout ();
            SetHelpViewDefaultLayout ();
            SetKeyViewDefaultLayout ();
            ShowHide ();
            UpdateKeyBinding ();

            return;
        }
    }

    private void SetCommandViewDefaultLayout ()
    {
        CommandView.Margin.Thickness = GetMarginThickness ();
        CommandView.X = Pos.Align (Alignment.End, AlignmentModes);
        CommandView.Y = 0; //Pos.Center ();
    }

    private void Shortcut_TitleChanged (object sender, EventArgs<string> e)
    {
        // If the Title changes, update the CommandView text.
        // This is a helper to make it easier to set the CommandView text.
        // CommandView is public and replaceable, but this is a convenience.
        _commandView.Text = Title;
    }

    #endregion Command

    #region Help

    /// <summary>
    ///     The subview that displays the help text for the command. Internal for unit testing.
    /// </summary>
    internal View HelpView { get; } = new ();

    private void SetHelpViewDefaultLayout ()
    {
        HelpView.Margin.Thickness = GetMarginThickness ();
        HelpView.X = Pos.Align (Alignment.End, AlignmentModes);
        HelpView.Y = 0; //Pos.Center ();
        HelpView.Width = Dim.Auto (DimAutoStyle.Text);
        HelpView.Height = CommandView?.Visible == true ? Dim.Height (CommandView) : 1;

        HelpView.Visible = true;
        HelpView.VerticalTextAlignment = Alignment.Center;
    }

/// <summary>
///     Gets or sets the help text displayed in the middle of the Shortcut. Identical in function to <see cref="HelpText"/>
///     .
/// </summary>
public override string Text
{
    get => HelpView?.Text;
    set
    {
        if (HelpView is {})
        {
            HelpView.Text = value;
            ShowHide ();
        }
    }
}

/// <summary>
///     Gets or sets the help text displayed in the middle of the Shortcut.
/// </summary>
public string HelpText
{
    get => HelpView?.Text;
    set
    {
        if (HelpView is {})
        {
            HelpView.Text = value;
            ShowHide ();
        }
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
            if (value == null)
            {
                throw new ArgumentNullException ();
            }

            _key = value;

            UpdateKeyBinding ();

            KeyView.Text = Key == Key.Empty ? string.Empty : $"{Key}";
            ShowHide ();
        }
    }

    private KeyBindingScope _keyBindingScope = KeyBindingScope.HotKey;

    /// <summary>
    ///     Gets or sets the scope for the key binding for how <see cref="Key"/> is bound to <see cref="Command"/>.
    /// </summary>
    public KeyBindingScope KeyBindingScope
    {
        get => _keyBindingScope;
        set
        {
            _keyBindingScope = value;

            UpdateKeyBinding ();
        }
    }

    /// <summary>
    ///     Gets the subview that displays the key. Internal for unit testing.
    /// </summary>

    internal View KeyView { get; } = new ();

    private int _minimumKeyTextSize;

    /// <summary>
    /// Gets or sets the minimum size of the key text. Useful for aligning the key text with other <see cref="Shortcut"/>s.
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
            CommandView.SetNeedsLayout ();
            HelpView.SetNeedsLayout ();
            KeyView.SetNeedsLayout ();
            SetSubViewNeedsDisplay ();
        }
    }

    private int GetMinimumKeyViewSize () { return MinimumKeyTextSize; }

    private void SetKeyViewDefaultLayout ()
    {
        KeyView.Margin.Thickness = GetMarginThickness ();
        KeyView.X = Pos.Align (Alignment.End, AlignmentModes);
        KeyView.Y = 0; //Pos.Center ();
        KeyView.Width = Dim.Auto (DimAutoStyle.Text, Dim.Func (GetMinimumKeyViewSize));
        KeyView.Height = CommandView?.Visible == true ? Dim.Height (CommandView) : 1;

        KeyView.Visible = true;

        // Right align the text in the keyview
        KeyView.TextAlignment = Alignment.End;
        KeyView.VerticalTextAlignment = Alignment.Center;
        KeyView.KeyBindings.Clear ();
    }

    private void UpdateKeyBinding ()
    {
        if (Key != null)
        {
            // Disable the command view key bindings
            CommandView.KeyBindings.Remove (Key);
            CommandView.KeyBindings.Remove (CommandView.HotKey);

            if (KeyBindingScope.FastHasFlags (KeyBindingScope.Application))
            {
                Application.KeyBindings.Remove (Key);
                Application.KeyBindings.Add (Key, this, Command.Accept);
            }
            else
            {
                KeyBindings.Remove (Key);
                KeyBindings.Add (Key, KeyBindingScope | KeyBindingScope.HotKey, Command.Accept);
            }
        }
    }

    #endregion Key

    #region Accept Handling

    /// <summary>
    ///     Called when the <see cref="Command.Accept"/> command is received. This
    ///     occurs
    ///     - if the user clicks anywhere on the shortcut with the mouse
    ///     - if the user presses Key
    ///     - if the user presses the HotKey specified by CommandView
    ///     - if HasFocus and the user presses Space or Enter (or any other key bound to Command.Accept).
    /// </summary>
    protected bool? OnAccept (CommandContext ctx)
    {
        var cancel = false;

        switch (ctx.KeyBinding?.Scope)
        {
            case KeyBindingScope.Application:
                cancel = base.OnAccept () == true;

                break;

            case KeyBindingScope.Focused:
                base.OnAccept ();

                // cancel if we're focused
                cancel = true;

                break;

            case KeyBindingScope.HotKey:
                cancel = base.OnAccept () == true;

                if (CanFocus)
                {
                    SetFocus ();
                    cancel = true;
                }

                break;

            default:
                // Mouse
                cancel = base.OnAccept () == true;

                break;
        }

        CommandView.InvokeCommand (Command.Accept, ctx.Key, ctx.KeyBinding);

        if (Action is { })
        {
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
    ///     Note, the <see cref="View.Accept"/> event is fired first, and if cancelled, the event will not be invoked.
    /// </remarks>
    [CanBeNull]
    public Action Action { get; set; }

    #endregion Accept Handling

    private bool? OnSelect (CommandContext ctx)
    {
        if (CommandView.GetSupportedCommands ().Contains (Command.Select))
        {
            return CommandView.InvokeCommand (Command.Select, ctx.Key, ctx.KeyBinding);
        }
        return false;

    }


    #region Focus

    /// <inheritdoc/>
    public override ColorScheme ColorScheme
    {
        get => base.ColorScheme;
        set
        {
            base.ColorScheme = value;
            SetColors ();
        }
    }

    /// <summary>
    /// </summary>
    internal void SetColors ()
    {
        // Border should match superview.
        Border.ColorScheme = SuperView?.ColorScheme;

        if (HasFocus)
        {
            // When we have focus, we invert the colors
            base.ColorScheme = new (base.ColorScheme)
            {
                Normal = base.ColorScheme.Focus,
                HotNormal = base.ColorScheme.HotFocus,
                HotFocus = base.ColorScheme.HotNormal,
                Focus = base.ColorScheme.Normal
            };
        }
        else
        {
            base.ColorScheme = SuperView?.ColorScheme ?? base.ColorScheme;
        }

        // Set KeyView's colors to show "hot"
        if (IsInitialized && base.ColorScheme is { })
        {
            var cs = new ColorScheme (base.ColorScheme)
            {
                Normal = base.ColorScheme.HotNormal,
                HotNormal = base.ColorScheme.Normal
            };
            KeyView.ColorScheme = cs;
        }
    }

    View _lastFocusedView;
    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        SetColors ();
        _lastFocusedView = view;

        return base.OnEnter (view);
    }

    /// <inheritdoc/>
    public override bool OnLeave (View view)
    {
        SetColors ();
        _lastFocusedView = this;

        return base.OnLeave (view);
    }

    #endregion Focus

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            if (CommandView?.IsAdded == false)
            {
                CommandView.Dispose ();
            }

            if (HelpView?.IsAdded == false)
            {
                HelpView.Dispose ();
            }

            if (KeyView?.IsAdded == false)
            {
                KeyView.Dispose ();
            }
        }

        base.Dispose (disposing);
    }
}
