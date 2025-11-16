#nullable disable
﻿#nullable enable
using System.Diagnostics;

namespace Terminal.Gui.App;

/// <summary>
///     INTERNAL: Implements <see cref="IKeyboard"/> to manage keyboard input and key bindings at the Application level.
///     <para>
///         This implementation decouples keyboard handling state from the static <see cref="Application"/> class,
///         enabling parallelizable unit tests and better testability.
///     </para>
///     <para>
///         See <see cref="IKeyboard"/> for usage details.
///     </para>
/// </summary>
internal class KeyboardImpl : IKeyboard
{
    private Key _quitKey = Key.Esc; // Resources/config.json overrides
    private Key _arrangeKey = Key.F5.WithCtrl; // Resources/config.json overrides
    private Key _nextTabGroupKey = Key.F6; // Resources/config.json overrides
    private Key _nextTabKey = Key.Tab; // Resources/config.json overrides
    private Key _prevTabGroupKey = Key.F6.WithShift; // Resources/config.json overrides
    private Key _prevTabKey = Key.Tab.WithShift; // Resources/config.json overrides

    /// <summary>
    ///     Commands for Application.
    /// </summary>
    private readonly Dictionary<Command, View.CommandImplementation> _commandImplementations = new ();

    /// <inheritdoc/>
    public IApplication? Application { get; set; }

    /// <inheritdoc/>
    public KeyBindings KeyBindings { get; internal set; } = new (null);

    /// <inheritdoc/>
    public Key QuitKey
    {
        get => _quitKey;
        set
        {
            KeyBindings.Replace (_quitKey, value);
            _quitKey = value;
        }
    }

    /// <inheritdoc/>
    public Key ArrangeKey
    {
        get => _arrangeKey;
        set
        {
            KeyBindings.Replace (_arrangeKey, value);
            _arrangeKey = value;
        }
    }

    /// <inheritdoc/>
    public Key NextTabGroupKey
    {
        get => _nextTabGroupKey;
        set
        {
            KeyBindings.Replace (_nextTabGroupKey, value);
            _nextTabGroupKey = value;
        }
    }

    /// <inheritdoc/>
    public Key NextTabKey
    {
        get => _nextTabKey;
        set
        {
            KeyBindings.Replace (_nextTabKey, value);
            _nextTabKey = value;
        }
    }

    /// <inheritdoc/>
    public Key PrevTabGroupKey
    {
        get => _prevTabGroupKey;
        set
        {
            KeyBindings.Replace (_prevTabGroupKey, value);
            _prevTabGroupKey = value;
        }
    }

    /// <inheritdoc/>
    public Key PrevTabKey
    {
        get => _prevTabKey;
        set
        {
            KeyBindings.Replace (_prevTabKey, value);
            _prevTabKey = value;
        }
    }

    /// <inheritdoc/>
    public event EventHandler<Key>? KeyDown;

    /// <inheritdoc/>
    public event EventHandler<Key>? KeyUp;

    /// <summary>
    ///     Initializes keyboard bindings.
    /// </summary>
    public KeyboardImpl ()
    {
        AddKeyBindings ();
    }

    /// <inheritdoc/>
    public bool RaiseKeyDownEvent (Key key)
    {
        //ebug.Assert (App.Application.MainThreadId == Thread.CurrentThread.ManagedThreadId);
        //Logging.Debug ($"{key}");

        // TODO: Add a way to ignore certain keys, esp for debugging.
        //#if DEBUG
        //        if (key == Key.Empty.WithAlt || key == Key.Empty.WithCtrl)
        //        {
        //            Logging.Debug ($"Ignoring {key}");
        //            return false;
        //        }
        //#endif

        // TODO: This should match standard event patterns
        KeyDown?.Invoke (null, key);

        if (key.Handled)
        {
            return true;
        }

        if (Application?.Popover?.DispatchKeyDown (key) is true)
        {
            return true;
        }

        if (Application?.Current is null)
        {
            if (Application?.SessionStack is { })
            {
                foreach (Toplevel topLevel in Application.SessionStack.ToList ())
                {
                    if (topLevel.NewKeyDownEvent (key))
                    {
                        return true;
                    }

                    if (topLevel.Modal)
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            if (Application.Current.NewKeyDownEvent (key))
            {
                return true;
            }
        }

        bool? commandHandled = InvokeCommandsBoundToKey (key);
        if(commandHandled is true)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool RaiseKeyUpEvent (Key key)
    {
        if (Application?.Initialized != true)
        {
            return true;
        }

        KeyUp?.Invoke (null, key);

        if (key.Handled)
        {
            return true;
        }


        // TODO: Add Popover support

        if (Application?.SessionStack is { })
        {
            foreach (Toplevel topLevel in Application.SessionStack.ToList ())
            {
                if (topLevel.NewKeyUpEvent (key))
                {
                    return true;
                }

                if (topLevel.Modal)
                {
                    break;
                }
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public bool? InvokeCommandsBoundToKey (Key key)
    {
        bool? handled = null;
        // Invoke any Application-scoped KeyBindings.
        // The first view that handles the key will stop the loop.
        // foreach (KeyValuePair<Key, KeyBinding> binding in KeyBindings.GetBindings (key))
        if (KeyBindings.TryGet (key, out KeyBinding binding))
        {
            if (binding.Target is { })
            {
                if (!binding.Target.Enabled)
                {
                    return null;
                }

                handled = binding.Target?.InvokeCommands (binding.Commands, binding);
            }
            else
            {
                bool? toReturn = null;

                foreach (Command command in binding.Commands)
                {
                    toReturn = InvokeCommand (command, key, binding);
                }

                handled = toReturn ?? true;
            }
        }

        return handled;
    }

    /// <inheritdoc/>
    public bool? InvokeCommand (Command command, Key key, KeyBinding binding)
    {
        if (!_commandImplementations.ContainsKey (command))
        {
            throw new NotSupportedException (
                                             @$"A KeyBinding was set up for the command {command} ({key}) but that command is not supported by Application."
                                            );
        }

        if (_commandImplementations.TryGetValue (command, out View.CommandImplementation? implementation))
        {
            CommandContext<KeyBinding> context = new (command, null, binding); // Create the context here

            return implementation (context);
        }

        return null;
    }

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="f"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This version of AddCommand is for commands that do not require a <see cref="ICommandContext"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    private void AddCommand (Command command, Func<bool?> f) { _commandImplementations [command] = ctx => f (); }

    internal void AddKeyBindings ()
    {
        _commandImplementations.Clear ();

        // Things Application knows how to do
        AddCommand (
                    Command.Quit,
                    () =>
                    {
                        Application?.RequestStop ();

                        return true;
                    }
                   );
        AddCommand (
                    Command.Suspend,
                    () =>
                    {
                        Application?.Driver?.Suspend ();

                        return true;
                    }
                   );
        AddCommand (
                    Command.NextTabStop,
                    () => Application?.Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop));

        AddCommand (
                    Command.PreviousTabStop,
                    () => Application?.Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop));

        AddCommand (
                    Command.NextTabGroup,
                    () => Application?.Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup));

        AddCommand (
                    Command.PreviousTabGroup,
                    () => Application?.Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup));

        AddCommand (
                    Command.Refresh,
                    () =>
                    {
                        Application?.LayoutAndDraw (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Arrange,
                    () =>
                    {
                        View? viewToArrange = Application?.Navigation?.GetFocused ();

                        // Go up the superview hierarchy and find the first that is not ViewArrangement.Fixed
                        while (viewToArrange is { SuperView: { }, Arrangement: ViewArrangement.Fixed })
                        {
                            viewToArrange = viewToArrange.SuperView;
                        }

                        if (viewToArrange is { })
                        {
                            return viewToArrange.Border?.EnterArrangeMode (ViewArrangement.Fixed);
                        }

                        return false;
                    });

        //SetKeysToHardCodedDefaults ();

        // Need to clear after setting the above to ensure actually clear
        // because set_QuitKey etc.. may call Add
        KeyBindings.Clear ();

        KeyBindings.Add (QuitKey, Command.Quit);
        KeyBindings.Add (NextTabKey, Command.NextTabStop);
        KeyBindings.Add (PrevTabKey, Command.PreviousTabStop);
        KeyBindings.Add (NextTabGroupKey, Command.NextTabGroup);
        KeyBindings.Add (PrevTabGroupKey, Command.PreviousTabGroup);
        KeyBindings.Add (ArrangeKey, Command.Arrange);

        KeyBindings.Add (Key.CursorRight, Command.NextTabStop);
        KeyBindings.Add (Key.CursorDown, Command.NextTabStop);
        KeyBindings.Add (Key.CursorLeft, Command.PreviousTabStop);
        KeyBindings.Add (Key.CursorUp, Command.PreviousTabStop);

        // TODO: Refresh Key should be configurable
        KeyBindings.Add (Key.F5, Command.Refresh);

        // TODO: Suspend Key should be configurable
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            KeyBindings.Add (Key.Z.WithCtrl, Command.Suspend);
        }
    }
}
