using System.ComponentModel;

namespace Terminal.Gui;

// TODO: I don't love the name Shortcut, but I can't think of a better one right now. Shortcut is a bit overloaded.
// TODO: It can mean "Application-scoped key binding" or "A key binding that is displayed in a visual way".
// TODO: I tried `BarItem` but that's not great either as it implies it can only be used in `Bar`s.

/// <summary>
///     Displays a command, help text, and a key binding. Useful for displaying a command in <see cref="Bar"/> such as a
///     menu, toolbar, or status bar.
/// </summary>
/// <remarks>
///     <para>
///         When the user clicks on the <see cref="Shortcut"/> or presses the key
///         specified by <see cref="Key"/> the <see cref="Command.Accept"/> command is invoked, causing the
///         <see cref="Accept"/> event to be fired
///     </para>
///     <para>
///         If <see cref="KeyBindingScope"/> is <see cref="KeyBindingScope.Application"/>, the <see cref="Command"/>
///         be invoked regardless of what View has focus, enabling an application-wide keyboard shortcut.
///     </para>
///     <para>
///         Set <see cref="View.Title"/> to change the Command text displayed in the <see cref="Shortcut"/>.
///         By default, the <see cref="Command"/> text is the <see cref="View.Title"/> of <see cref="CommandView"/>.
///     </para>
///     <para>
///         Set <see cref="View.Text"/> to change the Help text displayed in the <see cref="Shortcut"/>.
///     </para>
///     <para>
///         The text displayed for the <see cref="Key"/> is the string representation of the <see cref="Key"/>.
///         If the <see cref="Key"/> is <see cref="Key.Empty"/>, the <see cref="Key"/> text is not displayed.
///     </para>
/// </remarks>
public class Shortcut : View
{
    // Hosts the Command, Help, and Key Views. Needed (IIRC - wrote a long time ago) to allow mouse clicks to be handled by the Shortcut.
    internal readonly View _container;

    /// <summary>
    ///     Creates a new instance of <see cref="Shortcut"/>.
    /// </summary>
    public Shortcut ()
    {
        CanFocus = true;
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        //Height = Dim.Auto (minimumContentDim: 1, maximumContentDim: 1);

        AddCommand (Gui.Command.HotKey, () => true);
        AddCommand (Gui.Command.Accept, OnAccept);
        KeyBindings.Add (KeyCode.Space, Gui.Command.Accept);
        KeyBindings.Add (KeyCode.Enter, Gui.Command.Accept);

        _container = new ()
        {
            Id = "_container",
            CanFocus = true,
            Width = Dim.Auto (DimAutoStyle.Content, 1),
            Height = Dim.Auto (DimAutoStyle.Content, 1)
        };
        _container.MouseClick += OnContainerMouseClick;

        _commandView = new ()
        {
            Id = "_commandView",
            CanFocus = false,
            X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast | AlignmentModes.AddSpaceBetweenItems),
            Y = 0, // Pos.Center (),
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text),
            HotKeySpecifier = new ('_')
        };
        _container.Add (_commandView);

        HelpView = new ()
        {
            Id = "_helpView",
            CanFocus = false,
            X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast | AlignmentModes.AddSpaceBetweenItems),
            Y = 0, // Pos.Center (),
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text)
        };
        _container.Add (HelpView);

        //        HelpView.TextAlignment = Alignment.End;
        HelpView.MouseClick += SubView_MouseClick;

        KeyView = new ()
        {
            Id = "_keyView", CanFocus = false,
            X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast | AlignmentModes.AddSpaceBetweenItems),
            Y = 0, // Pos.Center (),
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text)
        };
        _container.Add (KeyView);

        KeyView.MouseClick += SubView_MouseClick;

        //CommandView.Margin.Thickness = new Thickness (1, 0, 1, 0);
        //HelpView.Margin.Thickness = new Thickness (1, 0, 1, 0);
        //KeyView.Margin.Thickness = new Thickness (1, 0, 1, 0);

        //CommandView.CanFocus = CanFocus;
        //HelpView.CanFocus = CanFocus;
        //KeyView.CanFocus = CanFocus;

        //_commandView.MouseClick += SubView_MouseClick;

        TitleChanged += Shortcut_TitleChanged;
        Initialized += OnInitialized;

        Add (_container);

        return;

        void OnInitialized (object sender, EventArgs e)
        {
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

        // When _container is clicked, we want to invoke the Accept command.
        // This can happen if the subviews are aligned such that they don't cover the
        // entire area of the Shortcut.
        // TODO: Figure out why we can't just do the same thing using the MouseClick event on the Shortcut itself.
        void OnContainerMouseClick (object sender, MouseEventEventArgs e)
        {
            bool? handled = OnAccept ();

            if (handled.HasValue)
            {
                e.Handled = handled.Value;
            }
        }
    }

    // Intercept any mouse clicks on the subviews and invoke the Accept command.
    // TODO: Figure out why we can't just subscribe to Accept on the subviews.
    private void SubView_MouseClick (object sender, MouseEventEventArgs e)
    {
        bool? handled = OnAccept ();

        if (handled.HasValue)
        {
            e.Handled = handled.Value;
        }

        if (!e.Handled && CanFocus)
        {
            SetFocus ();
        }
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

    private Command? _command;

    /// <summary>
    ///     Gets or sets the <see cref="Command"/> that will be invoked when the user clicks on the <see cref="Shortcut"/> or
    ///     presses <see cref="Key"/>.
    /// </summary>
    public Command? Command
    {
        get => _command;
        set
        {
            if (value != null)
            {
                _command = value.Value;
                UpdateKeyBinding ();
            }
        }
    }

    private View _commandView;

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
    ///         Command = Command.Accept,
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
                _container.Remove (_commandView);
                _commandView?.Dispose ();
            }

            _commandView = value;
            _commandView.Id = "_commandView";
            _commandView.Width = Dim.Auto (DimAutoStyle.Text);
            _commandView.Height = Dim.Auto (DimAutoStyle.Text);
            _commandView.X = X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast | AlignmentModes.AddSpaceBetweenItems);
            _commandView.Y = 0; //; Pos.Center ();
            _commandView.MouseClick += SubView_MouseClick;
            _commandView.Accept += SubView_DefaultCommand;
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
            HelpView.X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast);
            _container.Add (_commandView);
        }
    }

    private void Shortcut_TitleChanged (object sender, StateEventArgs<string> e)
    {
        // If the Title changes, update the CommandView text. This is a helper to make it easier to set the CommandView text.
        // CommandView is public and replaceable, but this is a convenience.
        _commandView.Text = Title;
    }

    private void SubView_DefaultCommand (object sender, CancelEventArgs e)
    {
        bool? handled = OnAccept ();

        if (handled.HasValue)
        {
            e.Cancel = handled.Value;
        }

        if (!e.Cancel && CanFocus)
        {
            SetFocus ();
        }
    }

    #endregion Command

    #region Help

    /// <summary>
    ///     The subview that displays the help text for the command. Internal for unit testing.
    /// </summary>
    internal View HelpView { get; set; }

    /// <summary>
    ///     Gets or sets the help text displayed in the middle of the Shortcut.
    /// </summary>
    public override string Text
    {
        get => base.Text;
        set
        {
            //base.Text = value;
            if (HelpView != null)
            {
                HelpView.Text = value;
            }
        }
    }

    #endregion Help

    #region Key

    private Key _key;

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

            if (Command != null)
            {
                UpdateKeyBinding ();
            }

            KeyView.Text = $"{Key}";
            KeyView.Visible = Key != Key.Empty;
        }
    }

    private KeyBindingScope _keyBindingScope;

    /// <summary>
    ///     Gets or sets the scope for the key binding for how <see cref="Key"/> is bound to <see cref="Command"/>.
    /// </summary>
    public KeyBindingScope KeyBindingScope
    {
        get => _keyBindingScope;
        set
        {
            _keyBindingScope = value;

            if (Command != null)
            {
                UpdateKeyBinding ();
            }
        }
    }

    /// <summary>
    ///     Gets the subview that displays the key. Internal for unit testing.
    /// </summary>

    internal View KeyView { get; }

    private void UpdateKeyBinding ()
    {
        if (KeyBindingScope == KeyBindingScope.Application)
        {
            return;
        }

        if (Command != null && Key != null && Key != Key.Empty)
        {
            // Add a command and key binding for this command to this Shortcut
            if (!GetSupportedCommands ().Contains (Command.Value))
            {
                // The action that will be taken will be to fire the OnClicked
                // event. 
                AddCommand (Command.Value, () => OnAccept ());
            }

            KeyBindings.Remove (Key);
            KeyBindings.Add (Key, KeyBindingScope, Command.Value);
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

        switch (KeyBindingScope)
        {
            case KeyBindingScope.Application:
                // Simulate a key down to invoke the Application scoped key binding
                handled = Application.OnKeyDown (keyCopy);

                break;
            case KeyBindingScope.Focused:
                //throw new InvalidOperationException ();
                handled = false;

                break;
            case KeyBindingScope.HotKey:
                handled = _commandView.InvokeCommand (Gui.Command.Accept) == true;

                break;
        }

        if (handled == false)
        {
            var args = new HandledEventArgs ();
            Accept?.Invoke (this, args);
            handled = args.Handled;
        }

        return handled;
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

        _container.ColorScheme = cs;

        cs = new (ColorScheme)
        {
            Normal = ColorScheme.HotFocus,
            HotNormal = ColorScheme.Focus
        };
        KeyView.ColorScheme = cs;

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

        _container.ColorScheme = cs;

        cs = new (ColorScheme)
        {
            Normal = ColorScheme.HotNormal,
            HotNormal = ColorScheme.Normal
        };
        KeyView.ColorScheme = cs;

        return base.OnLeave (view);
    }
}
