using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>
///      Defines an Application-scoped <see cref="KeyBinding"/>, displaying the command, help text, and they key binding.
/// </summary>
/// <remarks>
///     <para>
///         Used by <see cref="Bar"/>.
///     </para>
/// </remarks>
public class Shortcut : View
{
    internal readonly View _container;
    private Command? _command;
    private View _commandView;
    private Key _key;
    private KeyBindingScope _keyBindingScope;

    public Shortcut ()
    {
        CanFocus = true;
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);
        //Height = Dim.Auto (minimumContentDim: 1, maximumContentDim: 1);

        AddCommand (
                    Gui.Command.HotKey,
                    () =>
                    {
                        //SetFocus ();
                        //SuperView?.FocusNext ();
                        return true;
                    });
        AddCommand (Gui.Command.Accept, () => OnAccept ());
        KeyBindings.Add (KeyCode.Space, Gui.Command.Accept);
        KeyBindings.Add (KeyCode.Enter, Gui.Command.Accept);

        _container = new View
        {
            Id = "_container",
            CanFocus = true,
            Width = Dim.Auto (DimAutoStyle.Content, minimumContentDim: 1),
            Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: 1)
        };
        _container.MouseClick += Container_MouseClick;

        _commandView = new View
        {
            Id = "_commandView",
            CanFocus = false,
            X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast | AlignmentModes.AddSpaceBetweenItems),
            Y = 0,// Pos.Center (),
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text),
            HotKeySpecifier = new Rune ('_')
        };
        _container.Add (_commandView);

        HelpView = new View
        {
            Id = "_helpView",
            CanFocus = false,
            X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast | AlignmentModes.AddSpaceBetweenItems),
            Y = 0,// Pos.Center (),
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text),
        };
        _container.Add (HelpView);

//        HelpView.TextAlignment = Alignment.End;
        HelpView.MouseClick += SubView_MouseClick;

        KeyView = new View
        {
            Id = "_keyView", CanFocus = false,
            X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast | AlignmentModes.AddSpaceBetweenItems),
            Y = 0,// Pos.Center (),
            Width = Dim.Auto (DimAutoStyle.Text),
            Height = Dim.Auto (DimAutoStyle.Text),
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

        LayoutStarted += Shortcut_LayoutStarted;
        TitleChanged += Shortcut_TitleChanged;
        Initialized += Shortcut_Initialized;

        Add (_container);
    }

    private void SetSubViewLayout ()
    {

        // if (Width is DimAuto)
        {
            //_container.Width = Dim.Func (() => 1 + CommandView.Text.GetColumns () + 1 + HelpView.Text.GetColumns () + 1 + KeyView.Text.GetColumns () + 1);
        }

        //HelpView.X = Pos.AnchorEnd (KeyView.Text.GetColumns () + 1 + HelpView.Text.GetColumns () + 1) - 2;
        //KeyView.X = Pos.AnchorEnd (KeyView.Text.GetColumns () + 1) - 1;
    }


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
            _commandView.Y = 0;//; Pos.Center ();
            _commandView.MouseClick += SubView_MouseClick;
            _commandView.Accept += SubView_DefaultCommand;
            _commandView.Margin.Thickness = new Thickness (1, 0, 1, 0);

            _commandView.HotKeyChanged += (s, e) =>
                                          {
                                              if (e.NewKey != Key.Empty)
                                              {
                                                  // Add it 
                                                  AddKeyBindingsForHotKey (e.OldKey, e.NewKey);
                                              }
                                          };

            _commandView.HotKeySpecifier = new Rune ('_');
            HelpView.X = Pos.Align (Alignment.End, AlignmentModes.IgnoreFirstOrLast);
            _container.Add (_commandView);
            SetSubViewLayout ();
        }
    }

    public View HelpView { get; }

    /// <summary>
    ///     The shortcut key.
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

    public View KeyView { get; }

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

    /// <summary>
    ///     The event fired when the <see cref="Command.Accept"/> command is received. This
    ///     occurs if the user clicks on the Bar with the mouse or presses the key bound to
    ///     Command.Accept (Space by default).
    /// </summary>
    /// <remarks>
    ///     Client code can hook up to this event, it is
    ///     raised when the button is activated either with
    ///     the mouse or the keyboard.
    /// </remarks>
    public event EventHandler<HandledEventArgs> Accept;

    /// <summary>
    ///     Called when the <see cref="Command.Accept"/> command is received. This
    ///     occurs if the user clicks on the Bar with the mouse or presses the key bound to
    ///     Command.Accept (Space by default).
    /// </summary>
    public virtual bool OnAccept ()
    {
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

    public override bool OnEnter (View view)
    {
        Application.Driver.SetCursorVisibility (CursorVisibility.Invisible);

        var cs = new ColorScheme (ColorScheme)
        {
            Normal = ColorScheme.Focus,
            HotNormal = ColorScheme.HotFocus
        };

        _container.ColorScheme = cs;

        cs = new ColorScheme (ColorScheme)
        {
            Normal = ColorScheme.HotFocus,
            HotNormal = ColorScheme.Focus
        };
        KeyView.ColorScheme = cs;

        return base.OnEnter (view);
    }

    public override bool OnLeave (View view)
    {
        var cs = new ColorScheme (ColorScheme)
        {
            Normal = ColorScheme.Normal,
            HotNormal = ColorScheme.HotNormal
        };

        _container.ColorScheme = cs;

        cs = new ColorScheme (ColorScheme)
        {
            Normal = ColorScheme.HotNormal,
            HotNormal = ColorScheme.Normal
        };
        KeyView.ColorScheme = cs;

        return base.OnLeave (view);
    }

    private void Container_MouseClick (object sender, MouseEventEventArgs e) { e.Handled = OnAccept (); }


    private void Shortcut_Initialized (object sender, EventArgs e)
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

    private void Shortcut_LayoutStarted (object sender, LayoutEventArgs e)
    {
        SetSubViewLayout ();
    }

    private void Shortcut_TitleChanged (object sender, StateEventArgs<string> e) { _commandView.Text = Title; }

    private void SubView_MouseClick (object sender, MouseEventEventArgs e)
    {
        e.Handled = OnAccept ();

        if (!e.Handled && CanFocus)
        {
            SetFocus ();
        }
    }

    private void SubView_DefaultCommand (object sender, CancelEventArgs e)
    {
        e.Cancel = OnAccept ();

        if (!e.Cancel && CanFocus)
        {
            SetFocus ();
        }
    }

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
}
