using System.ComponentModel;

namespace Terminal.Gui;

// TODO: I don't love the name Shortcut, but I can't think of a better one right now. Shortcut is a bit overloaded.
// TODO: It can mean "Application-scoped key binding" or "A key binding that is displayed in a visual way".
// TODO: I tried `BarItem` but that's not great either as it implies it can only be used in `Bar`s.

/// <summary>
///     Displays a command, help text, and a key binding. When the key is pressed, the command will be invoked. Useful for displaying a command in <see cref="Bar"/> such as a
///     menu, toolbar, or status bar.
/// </summary>
/// <remarks>
///     <para>
///         When the user clicks on the <see cref="Shortcut"/> or presses the key
///         specified by <see cref="Key"/> the <see cref="Command.Accept"/> command is invoked, causing the
///         <see cref="Accept"/> event to be fired
///     </para>
///     <para>
///         If <see cref="KeyBindingScope"/> is <see cref="KeyBindingScope.Application"/>, the <see cref="Command.Accept"/> command 
///         be invoked regardless of what View has focus, enabling an application-wide keyboard shortcut.
///     </para>
///     <para>
///         A Shortcut displays the command text on the left side, the help text in the middle, and the key binding on the right side.
///     </para>
///     <para>
///         The command text can be set by setting the <see cref="CommandView"/>'s Text property or by setting <see cref="View.Title"/>.
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
    public Shortcut ()
    {
        Id = "_shortcut";
        HighlightStyle = HighlightStyle.Pressed;
        Highlight += Shortcut_Highlight;
        CanFocus = true;
        Width = GetWidthDimAuto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);

        AddCommand (Gui.Command.HotKey, OnAccept);
        AddCommand (Gui.Command.Accept, OnAccept);
        KeyBindings.Add (KeyCode.Space, Gui.Command.Accept);
        KeyBindings.Add (KeyCode.Enter, Gui.Command.Accept);

        TitleChanged += Shortcut_TitleChanged; // This needs to be set before CommandView is set

        CommandView = new View ();

        HelpView.Id = "_helpView";
        HelpView.CanFocus = false;
        SetHelpViewDefaultLayout ();
        Add (HelpView);

        //        HelpView.TextAlignment = Alignment.End;
        HelpView.MouseClick += Shortcut_MouseClick;

        KeyView.Id = "_keyView";

        // Only the Shortcut should be able to have focus, not any subviews
        KeyView.CanFocus = false;

        // Right align the text in the keyview
        KeyView.TextAlignment = Alignment.End;

        SetKeyViewDefaultLayout ();
        Add (KeyView);

        KeyView.MouseClick += Shortcut_MouseClick;

        MouseClick += Shortcut_MouseClick;

        Initialized += OnInitialized;

        LayoutStarted += OnLayoutStarted;

        return;

        void OnInitialized (object sender, EventArgs e)
        {
            ShowHide (CommandView);
            ShowHide (HelpView);
            ShowHide (KeyView);

            // Force Width to DimAuto to calculate natural width and then set it back
            Dim savedDim = Width;
            Width = GetWidthDimAuto ();
            _naturalWidth = Frame.Width;
            Width = savedDim;

            if (ColorScheme != null)
            {
                var cs = new ColorScheme (ColorScheme)
                {
                    Normal = ColorScheme.HotNormal,
                    HotNormal = ColorScheme.Normal
                };

                KeyView.ColorScheme = cs;
            }
        }

        Dim GetWidthDimAuto ()
        {
            return Dim.Auto (DimAutoStyle.Content, maximumContentDim: Dim.Func (() => PosAlign.CalculateMinDimension (0, Subviews, Dimension.Width)));
        }

    }

    // When one of the subviews is "empty" we don't want to show it. So we
    // Use Add/Remove. We need to be careful to add them in the right order
    // so Pos.Align works correctly.
    private void ShowHide (View subView)
    {
        RemoveAll ();
        if (!string.IsNullOrEmpty (CommandView.Text))
        {
            Add (CommandView);
        }
        if (!string.IsNullOrEmpty (HelpView.Text))
        {
            Add (HelpView);
        }
        if (Key != Key.Empty)
        {
            Add (KeyView);
        }
    }

    private int? _naturalWidth;

    private void OnLayoutStarted (object sender, LayoutEventArgs e)
    {
        if (Width is DimAuto widthAuto)
        {
            _naturalWidth = Frame.Width;
        }
        else
        {
            if (string.IsNullOrEmpty (HelpView.Text))
            {
                return;
            }

            int currentWidth = Frame.Width;

            // If our width is smaller than the natural then reduce width of HelpView.
            if (currentWidth < _naturalWidth)
            {
                int delta = _naturalWidth.Value - currentWidth;
                int maxHelpWidth = int.Max (0, HelpView.Text.GetColumns () + 2 - delta);

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
                        HelpView.Margin.Thickness = new (1, 0, 0, 0);

                        break;

                    default:
                        // Default margin
                        HelpView.Margin.Thickness = new (1, 0, 1, 0);

                        break;
                }

                if (maxHelpWidth > 0)
                {
                    HelpView.X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast);

                    // Leverage Dim.Auto's max:
                    HelpView.Width = Dim.Auto (DimAutoStyle.Text, maximumContentDim: maxHelpWidth);
                    HelpView.Visible = true;
                }
            }
            else
            {
                // Reset to default
                SetHelpViewDefaultLayout ();
            }
        }
    }

    private Color? _savedForeColor;

    private void Shortcut_Highlight (object sender, HighlightEventArgs e)
    {
        if (e.HighlightStyle.HasFlag (HighlightStyle.Pressed))
        {
            if (!_savedForeColor.HasValue)
            {
                _savedForeColor = ColorScheme.Normal.Foreground;
            }

            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (ColorScheme.Normal.Foreground.GetHighlightColor (), ColorScheme.Normal.Background)
            };
            ColorScheme = cs;
        }

        if (e.HighlightStyle == HighlightStyle.None && _savedForeColor.HasValue)
        {
            var cs = new ColorScheme (ColorScheme)
            {
                Normal = new (_savedForeColor.Value, ColorScheme.Normal.Background)
            };
            ColorScheme = cs;
        }

        SuperView?.SetNeedsDisplay ();
        e.Cancel = true;
    }

    private void Shortcut_MouseClick (object sender, MouseEventEventArgs e)
    {
        // When the Shortcut is clicked, we want to invoke the Command and Set focus
        var view = sender as View;
        if (view != CommandView)
        {
            CommandView.InvokeCommand (Command.Accept);
            e.Handled = true;

            return;
        }

        if (!e.Handled)
        {
            // If the subview (likely CommandView) didn't handle the mouse click, invoke the command.
            bool? handled = false;
            handled = InvokeCommand (Command.Accept);

            if (handled.HasValue)
            {
                e.Handled = handled.Value;
            }
        }

        if (CanFocus)
        {
            SetFocus ();
        }

        e.Handled = true;
    }

    /// <inheritdoc/>
    public override ColorScheme ColorScheme
    {
        get
        {
            if (base.ColorScheme == null)
            {
                return SuperView?.ColorScheme ?? base.ColorScheme;
            }

            return base.ColorScheme;
        }
        set
        {
            base.ColorScheme = value;

            if (ColorScheme != null)
            {
                var cs = new ColorScheme (ColorScheme)
                {
                    Normal = ColorScheme.HotNormal,
                    HotNormal = ColorScheme.Normal
                };
                KeyView.ColorScheme = cs;
            }
        }
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

            // TODO: Determine if it makes sense to allow the CommandView to be focusable.
            // Right now, we don't set CanFocus to false here.
            _commandView.CanFocus = false;

            // Bar will set the width of all CommandViews to the width of the widest CommandViews.
            _commandView.Width = Dim.Auto (DimAutoStyle.Text);
            _commandView.Height = Dim.Auto (DimAutoStyle.Text);
            _commandView.X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast);
            _commandView.Y = 0; //Pos.Center ();

            _commandView.MouseClick += Shortcut_MouseClick;
            _commandView.Accept += CommandViewAccept;

            _commandView.Margin.Thickness = new (1, 0, 1, 0);

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
            _commandView.TextChanged += CommandViewTextChanged;

            Remove (HelpView);
            Remove (KeyView);
            Add (_commandView, HelpView, KeyView);

            ShowHide (_commandView);
            UpdateKeyBinding ();

            return;

            void CommandViewMouseEvent (object sender, MouseEventEventArgs e) { e.Handled = true; }

            void CommandViewTextChanged (object sender, StateEventArgs<string> e)
            {
                Title = _commandView.Text;
                ShowHide (_commandView);
            }

            void CommandViewAccept (object sender, CancelEventArgs e)
            {
                // When the CommandView fires its Accept event, we want to act as though the
                // Shortcut was clicked.
                var args = new HandledEventArgs ();
                Accept?.Invoke (this, args);

                if (args.Handled)
                {
                    e.Cancel = args.Handled;
                }

                //e.Cancel = true;
            }
        }
    }

    private void Shortcut_TitleChanged (object sender, StateEventArgs<string> e)
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
        HelpView.Margin.Thickness = new (1, 0, 1, 0);
        HelpView.X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast);
        HelpView.Y = 0; //Pos.Center (),
        HelpView.Width = Dim.Auto (DimAutoStyle.Text);
        HelpView.Height = Dim.Auto (DimAutoStyle.Text);
        HelpView.Visible = true;
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
            if (HelpView != null)
            {
                HelpView.Text = value;
                ShowHide (HelpView);
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
            if (HelpView != null)
            {
                HelpView.Text = value;
                ShowHide (HelpView);
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
            ShowHide (KeyView);
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

    // TODO: Make internal once Bar is done
    /// <summary>
    ///     Gets the subview that displays the key. Internal for unit testing.
    /// </summary>

    public View KeyView { get; } = new ();

    private int _minimumKeyViewSize;
    /// <summary>
    /// 
    /// </summary>
    public int MinimumKeyViewSize
    {
        get => _minimumKeyViewSize;
        set
        {
            if (value == _minimumKeyViewSize)
            {
                //return;
            }
            _minimumKeyViewSize = value;
            SetKeyViewDefaultLayout();
            CommandView.SetNeedsLayout();
            HelpView.SetNeedsLayout ();
            KeyView.SetNeedsLayout ();
            SetSubViewNeedsDisplay ();
        }
    }

    private int GetMinimumKeyViewSize () { return MinimumKeyViewSize; }

    private void SetKeyViewDefaultLayout ()
    {
        KeyView.Margin.Thickness = new (1, 0, 1, 0);
        KeyView.X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast);
        KeyView.Y = 0; //Pos.Center (),
        KeyView.Width = Dim.Auto (DimAutoStyle.Text, minimumContentDim: Dim.Func(GetMinimumKeyViewSize));
        KeyView.Height = Dim.Auto (DimAutoStyle.Text);
        KeyView.Visible = true;
    }

    private void UpdateKeyBinding ()
    {
        if (KeyBindingScope == KeyBindingScope.Application)
        {
            //  return;
        }

        if (Key != null)
        {
            // CommandView holds our command/keybinding
            // Add a key binding for this command to this Shortcut

            CommandView.KeyBindings.Remove (Key);
            CommandView.KeyBindings.Add (Key, KeyBindingScope, Command.Accept);
        }
    }

    #endregion Key

    /// <summary>
    ///     The event fired when the <see cref="Command.Accept"/> command is received. This
    ///     occurs if the user clicks on the Shortcut or presses <see cref="Key"/>.
    /// </summary>
    public new event EventHandler<HandledEventArgs> Accept;

    /// <summary>
    ///     Called when the <see cref="Command.Accept"/> command is received. This
    ///     occurs if the user clicks on the Bar with the mouse or presses the key bound to
    ///     Command.Accept (Space by default).
    /// </summary>
    protected new bool? OnAccept ()
    {
        // TODO: This is not completely thought through.

        if (Key == null || Key == Key.Empty)
        {
            return false;
        }

        var handled = false;
        var keyCopy = new Key (Key);

        //switch (KeyBindingScope)
        //{
        //    case KeyBindingScope.Application:
        //        // Simulate a key down to invoke the Application scoped key binding
        //        handled = Application.OnKeyDown (keyCopy);

        //        break;
        //    case KeyBindingScope.Focused:
        //        handled = InvokeCommand (Command.Value) == true;
        //        handled = false;

        //        break;
        //    case KeyBindingScope.HotKey:
        //        if (Command.HasValue)
        //        {
        //            //handled = _commandView.InvokeCommand (Gui.Command.HotKey) == true;
        //            //handled = false;
        //        }

        //        break;
        //}

        //if (handled == false)
        {
            var args = new HandledEventArgs ();
            Accept?.Invoke (this, args);
            handled = args.Handled;
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool OnEnter (View view)
    {
        // TODO: This is a hack. Need to refine this.
        var cs = new ColorScheme (ColorScheme)
        {
            Normal = ColorScheme.Focus,
            HotNormal = ColorScheme.HotFocus
        };

        // _container.ColorScheme = cs;

        cs = new (ColorScheme)
        {
            Normal = ColorScheme.HotFocus,
            HotNormal = ColorScheme.Focus
        };

        //KeyView.ColorScheme = cs;

        return base.OnEnter (view);
    }

    /// <inheritdoc/>
    public override bool OnLeave (View view)
    {
        // TODO: This is a hack. Need to refine this.
        var cs = new ColorScheme (ColorScheme)
        {
            Normal = ColorScheme.Normal,
            HotNormal = ColorScheme.HotNormal
        };

        //   _container.ColorScheme = cs;

        cs = new (ColorScheme)
        {
            Normal = ColorScheme.HotNormal,
            HotNormal = ColorScheme.Normal
        };

        //KeyView.ColorScheme = cs;

        return base.OnLeave (view);
    }
}

