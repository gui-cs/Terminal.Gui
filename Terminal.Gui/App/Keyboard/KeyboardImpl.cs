using System.Collections.Concurrent;

namespace Terminal.Gui.App;

/// <summary>
///     INTERNAL: Implements <see cref="IKeyboard"/> to manage keyboard input and key bindings at the Application level.
///     This implementation is thread-safe for all public operations.
///     <para>
///         This implementation decouples keyboard handling state from the static <see cref="App"/> class,
///         enabling parallelizable unit tests and better testability.
///     </para>
///     <para>
///         See <see cref="IKeyboard"/> for usage details.
///     </para>
/// </summary>
internal class KeyboardImpl : IKeyboard, IDisposable
{
    /// <summary>
    ///     Initializes keyboard bindings and subscribes to Application configuration property events.
    /// </summary>
    public KeyboardImpl ()
    {
        // DON'T access Application static properties here - they trigger ApplicationImpl.Instance
        // which sets ModelUsage to LegacyStatic, breaking parallel tests.
        // These will be initialized from Application static properties in Init() or when accessed.

        // Initialize to reasonable defaults that match Application defaults
        // These will be updated by property change events if Application properties change
        _quitKey = Key.Esc;
        _arrangeKey = Key.F5.WithCtrl;
        _nextTabGroupKey = Key.F6;
        _nextTabKey = Key.Tab;
        _prevTabGroupKey = Key.F6.WithShift;
        _prevTabKey = Key.Tab.WithShift;

        // Subscribe to Application static property change events
        // so we get updated if they change
        Application.QuitKeyChanged += OnQuitKeyChanged;
        Application.ArrangeKeyChanged += OnArrangeKeyChanged;
        Application.NextTabGroupKeyChanged += OnNextTabGroupKeyChanged;
        Application.NextTabKeyChanged += OnNextTabKeyChanged;
        Application.PrevTabGroupKeyChanged += OnPrevTabGroupKeyChanged;
        Application.PrevTabKeyChanged += OnPrevTabKeyChanged;

        AddKeyBindings ();
    }

    /// <summary>
    ///     Commands for Application. Thread-safe for concurrent access.
    /// </summary>
    private readonly ConcurrentDictionary<Command, View.CommandImplementation> _commandImplementations = new ();

    private Key _quitKey;
    private Key _arrangeKey;
    private Key _nextTabGroupKey;
    private Key _nextTabKey;
    private Key _prevTabGroupKey;
    private Key _prevTabKey;

    /// <inheritdoc/>
    public void Dispose ()
    {
        // Unsubscribe from Application static property change events
        Application.QuitKeyChanged -= OnQuitKeyChanged;
        Application.ArrangeKeyChanged -= OnArrangeKeyChanged;
        Application.NextTabGroupKeyChanged -= OnNextTabGroupKeyChanged;
        Application.NextTabKeyChanged -= OnNextTabKeyChanged;
        Application.PrevTabGroupKeyChanged -= OnPrevTabGroupKeyChanged;
        Application.PrevTabKeyChanged -= OnPrevTabKeyChanged;
    }

    /// <inheritdoc/>
    public IApplication? App { get; set; }

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

        if (App?.Popover?.DispatchKeyDown (key) is true)
        {
            return true;
        }

        if (App?.TopRunnableView is null)
        {
            if (App?.SessionStack is { })
            {
                foreach (Toplevel? topLevel in App.SessionStack.Select(r => r.Runnable as Toplevel))
                {
                    if (topLevel!.NewKeyDownEvent (key))
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
            if (App.TopRunnableView.NewKeyDownEvent (key))
            {
                return true;
            }
        }

        bool? commandHandled = InvokeCommandsBoundToKey (key);

        if (commandHandled is true)
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool RaiseKeyUpEvent (Key key)
    {
        if (App?.Initialized != true)
        {
            return true;
        }

        KeyUp?.Invoke (null, key);

        if (key.Handled)
        {
            return true;
        }

        // TODO: Add Popover support

        if (App?.SessionStack is { })
        {
            foreach (Toplevel? topLevel in App.SessionStack.Select (r => r.Runnable as Toplevel))
            {
                if (topLevel!.NewKeyUpEvent (key))
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

    internal void AddKeyBindings ()
    {
        _commandImplementations.Clear ();

        // Things Application knows how to do
        AddCommand (
                    Command.Quit,
                    () =>
                    {
                        App?.RequestStop ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Suspend,
                    () =>
                    {
                        App?.Driver?.Suspend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.NextTabStop,
                    () => App?.Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop));

        AddCommand (
                    Command.PreviousTabStop,
                    () => App?.Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop));

        AddCommand (
                    Command.NextTabGroup,
                    () => App?.Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup));

        AddCommand (
                    Command.PreviousTabGroup,
                    () => App?.Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup));

        AddCommand (
                    Command.Refresh,
                    () =>
                    {
                        App?.LayoutAndDraw (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Arrange,
                    () =>
                    {
                        View? viewToArrange = App?.Navigation?.GetFocused ();

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

        // Need to clear after setting the above to ensure actually clear
        // because set_QuitKey etc. may call Add
        //KeyBindings.Clear ();

        // Use ReplaceCommands instead of Add, because it's possible that
        // during construction the Application static properties changed, and
        // we added those keys already.
        KeyBindings.ReplaceCommands (QuitKey, Command.Quit);
        KeyBindings.ReplaceCommands (NextTabKey, Command.NextTabStop);
        KeyBindings.ReplaceCommands (PrevTabKey, Command.PreviousTabStop);
        KeyBindings.ReplaceCommands (NextTabGroupKey, Command.NextTabGroup);
        KeyBindings.ReplaceCommands (PrevTabGroupKey, Command.PreviousTabGroup);
        KeyBindings.ReplaceCommands (ArrangeKey, Command.Arrange);

        // TODO: Should these be configurable?
        KeyBindings.ReplaceCommands (Key.CursorRight, Command.NextTabStop);
        KeyBindings.ReplaceCommands (Key.CursorDown, Command.NextTabStop);
        KeyBindings.ReplaceCommands (Key.CursorLeft, Command.PreviousTabStop);
        KeyBindings.ReplaceCommands (Key.CursorUp, Command.PreviousTabStop);

        // TODO: Refresh Key should be configurable
        KeyBindings.ReplaceCommands (Key.F5, Command.Refresh);

        // TODO: Suspend Key should be configurable
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            KeyBindings.ReplaceCommands (Key.Z.WithCtrl, Command.Suspend);
        }
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

    private void OnArrangeKeyChanged (object? sender, ValueChangedEventArgs<Key> e) { ArrangeKey = e.NewValue; }

    private void OnNextTabGroupKeyChanged (object? sender, ValueChangedEventArgs<Key> e) { NextTabGroupKey = e.NewValue; }

    private void OnNextTabKeyChanged (object? sender, ValueChangedEventArgs<Key> e) { NextTabKey = e.NewValue; }

    private void OnPrevTabGroupKeyChanged (object? sender, ValueChangedEventArgs<Key> e) { PrevTabGroupKey = e.NewValue; }

    private void OnPrevTabKeyChanged (object? sender, ValueChangedEventArgs<Key> e) { PrevTabKey = e.NewValue; }

    // Event handlers for Application static property changes
    private void OnQuitKeyChanged (object? sender, ValueChangedEventArgs<Key> e) { QuitKey = e.NewValue; }
}
