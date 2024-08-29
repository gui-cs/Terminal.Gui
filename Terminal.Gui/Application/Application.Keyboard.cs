#nullable enable
namespace Terminal.Gui;

public static partial class Application // Keyboard handling
{
    private static Key _nextTabGroupKey = Key.F6; // Resources/config.json overrrides
    private static Key _nextTabKey = Key.Tab; // Resources/config.json overrrides

    private static Key _prevTabGroupKey = Key.F6.WithShift; // Resources/config.json overrrides

    private static Key _prevTabKey = Key.Tab.WithShift; // Resources/config.json overrrides

    private static Key _quitKey = Key.Esc; // Resources/config.json overrrides

    static Application () { AddApplicationKeyBindings (); }

    /// <summary>Gets the key bindings for this view.</summary>
    public static KeyBindings KeyBindings { get; internal set; } = new ();

    /// <summary>
    ///     Event fired when the user presses a key. Fired by <see cref="OnKeyDown"/>.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/> and before <see cref="KeyUp"/>.</para>
    /// </remarks>
    public static event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Event fired when the user releases a key. Fired by <see cref="OnKeyUp"/>.
    ///     <para>
    ///         Set <see cref="Key.Handled"/> to <see langword="true"/> to indicate the key was handled and to prevent
    ///         additional processing.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     All drivers support firing the <see cref="KeyDown"/> event. Some drivers (Curses) do not support firing the
    ///     <see cref="KeyDown"/> and <see cref="KeyUp"/> events.
    ///     <para>Fired after <see cref="KeyDown"/>.</para>
    /// </remarks>
    public static event EventHandler<Key>? KeyUp;

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabGroupKey
    {
        get => _nextTabGroupKey;
        set
        {
            if (_nextTabGroupKey != value)
            {
                ReplaceKey (_nextTabGroupKey, value);
                _nextTabGroupKey = value;
            }
        }
    }

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key NextTabKey
    {
        get => _nextTabKey;
        set
        {
            if (_nextTabKey != value)
            {
                ReplaceKey (_nextTabKey, value);
                _nextTabKey = value;
            }
        }
    }

    /// <summary>
    ///     Called by the <see cref="ConsoleDriver"/> when the user presses a key. Fires the <see cref="KeyDown"/> event
    ///     then calls <see cref="View.NewKeyDownEvent"/> on all top level views. Called after <see cref="OnKeyDown"/> and
    ///     before <see cref="OnKeyUp"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="keyEvent"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool OnKeyDown (Key keyEvent)
    {
        //if (!IsInitialized)
        //{
        //    return true;
        //}

        KeyDown?.Invoke (null, keyEvent);

        if (keyEvent.Handled)
        {
            return true;
        }

        if (Current is null)
        {
            foreach (Toplevel topLevel in TopLevels.ToList ())
            {
                if (topLevel.NewKeyDownEvent (keyEvent))
                {
                    return true;
                }

                if (topLevel.Modal)
                {
                    break;
                }
            }
        }
        else
        {
            if (Current.NewKeyDownEvent (keyEvent))
            {
                return true;
            }
        }

        // Invoke any Application-scoped KeyBindings.
        // The first view that handles the key will stop the loop.
        foreach (KeyValuePair<Key, KeyBinding> binding in KeyBindings.Bindings.Where (b => b.Key == keyEvent.KeyCode))
        {
            if (binding.Value.BoundView is { })
            {
                bool? handled = binding.Value.BoundView?.InvokeCommands (binding.Value.Commands, binding.Key, binding.Value);

                if (handled != null && (bool)handled)
                {
                    return true;
                }
            }
            else
            {
                if (!KeyBindings.TryGet (keyEvent, KeyBindingScope.Application, out KeyBinding appBinding))
                {
                    continue;
                }

                bool? toReturn = null;

                foreach (Command command in appBinding.Commands)
                {
                    if (!CommandImplementations!.ContainsKey (command))
                    {
                        throw new NotSupportedException (
                                                         @$"A KeyBinding was set up for the command {command} ({keyEvent}) but that command is not supported by Application."
                                                        );
                    }

                    if (CommandImplementations.TryGetValue (command, out Func<CommandContext, bool?>? implementation))
                    {
                        var context = new CommandContext (command, keyEvent, appBinding); // Create the context here
                        toReturn = implementation (context);
                    }

                    // if ever see a true then that's what we will return
                    if (toReturn ?? false)
                    {
                        toReturn = true;
                    }
                }

                return toReturn ?? true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Called by the <see cref="ConsoleDriver"/> when the user releases a key. Fires the <see cref="KeyUp"/> event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="OnKeyDown"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="a"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool OnKeyUp (Key a)
    {
        if (!IsInitialized)
        {
            return true;
        }

        KeyUp?.Invoke (null, a);

        if (a.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in TopLevels.ToList ())
        {
            if (topLevel.NewKeyUpEvent (a))
            {
                return true;
            }

            if (topLevel.Modal)
            {
                break;
            }
        }

        return false;
    }

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabGroupKey
    {
        get => _prevTabGroupKey;
        set
        {
            if (_prevTabGroupKey != value)
            {
                ReplaceKey (_prevTabGroupKey, value);
                _prevTabGroupKey = value;
            }
        }
    }

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key PrevTabKey
    {
        get => _prevTabKey;
        set
        {
            if (_prevTabKey != value)
            {
                ReplaceKey (_prevTabKey, value);
                _prevTabKey = value;
            }
        }
    }

    /// <summary>Gets or sets the key to quit the application.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    public static Key QuitKey
    {
        get => _quitKey;
        set
        {
            if (_quitKey != value)
            {
                ReplaceKey (_quitKey, value);
                _quitKey = value;
            }
        }
    }

    internal static void AddApplicationKeyBindings ()
    {
        CommandImplementations = new ();

        // Things this view knows how to do
        AddCommand (
                    Command.QuitToplevel, // TODO: IRunnable: Rename to Command.Quit to make more generic.
                    static () =>
                    {
                        if (ApplicationOverlapped.OverlappedTop is { })
                        {
                            RequestStop (Current!);
                        }
                        else
                        {
                            RequestStop ();
                        }

                        return true;
                    }
                   );

        AddCommand (
                    Command.Suspend,
                    static () =>
                    {
                        Driver?.Suspend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.NextView,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop));

        AddCommand (
                    Command.PreviousView,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop));

        AddCommand (
                    Command.NextViewOrTop,
                    static () =>
                    {
                        // TODO: This OverlapppedTop tomfoolery goes away in addressing #2491
                        if (ApplicationOverlapped.OverlappedTop is { })
                        {
                            ApplicationOverlapped.OverlappedMoveNext ();

                            return true;
                        }

                        return Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup);
                    }
                   );

        AddCommand (
                    Command.PreviousViewOrTop,
                    static () =>
                    {
                        // TODO: This OverlapppedTop tomfoolery goes away in addressing #2491
                        if (ApplicationOverlapped.OverlappedTop is { })
                        {
                            ApplicationOverlapped.OverlappedMovePrevious ();

                            return true;
                        }

                        return Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup);
                    }
                   );

        AddCommand (
                    Command.Refresh,
                    static () =>
                    {
                        Refresh ();

                        return true;
                    }
                   );

        KeyBindings.Clear ();

        // Resources/config.json overrrides
        NextTabKey = Key.Tab;
        PrevTabKey = Key.Tab.WithShift;
        NextTabGroupKey = Key.F6;
        PrevTabGroupKey = Key.F6.WithShift;
        QuitKey = Key.Esc;

        KeyBindings.Add (QuitKey, KeyBindingScope.Application, Command.QuitToplevel);

        KeyBindings.Add (Key.CursorRight, KeyBindingScope.Application, Command.NextView);
        KeyBindings.Add (Key.CursorDown, KeyBindingScope.Application, Command.NextView);
        KeyBindings.Add (Key.CursorLeft, KeyBindingScope.Application, Command.PreviousView);
        KeyBindings.Add (Key.CursorUp, KeyBindingScope.Application, Command.PreviousView);
        KeyBindings.Add (NextTabKey, KeyBindingScope.Application, Command.NextView);
        KeyBindings.Add (PrevTabKey, KeyBindingScope.Application, Command.PreviousView);

        KeyBindings.Add (NextTabGroupKey, KeyBindingScope.Application, Command.NextViewOrTop);
        KeyBindings.Add (PrevTabGroupKey, KeyBindingScope.Application, Command.PreviousViewOrTop);

        // TODO: Refresh Key should be configurable
        KeyBindings.Add (Key.F5, KeyBindingScope.Application, Command.Refresh);

        // TODO: Suspend Key should be configurable
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            KeyBindings.Add (Key.Z.WithCtrl, KeyBindingScope.Application, Command.Suspend);
        }

#if UNIX_KEY_BINDINGS
        KeyBindings.Add (Key.L.WithCtrl, Command.Refresh); // Unix
        KeyBindings.Add (Key.F.WithCtrl, Command.NextView); // Unix
        KeyBindings.Add (Key.I.WithCtrl, Command.NextView); // Unix
        KeyBindings.Add (Key.B.WithCtrl, Command.PreviousView); // Unix
#endif
    }

    /// <summary>
    ///     Gets the list of Views that have <see cref="KeyBindingScope.Application"/> key bindings.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to add Application key bindings.
    /// </remarks>
    /// <returns>The list of Views that have Application-scoped key bindings.</returns>
    internal static List<KeyBinding> GetViewKeyBindings ()
    {
        // Get the list of views that do not have Application-scoped key bindings
        return KeyBindings.Bindings
                          .Where (kv => kv.Value.Scope != KeyBindingScope.Application)
                          .Select (kv => kv.Value)
                          .Distinct ()
                          .ToList ();
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
    ///         This version of AddCommand is for commands that do not require a <see cref="CommandContext"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    private static void AddCommand (Command command, Func<bool?> f) { CommandImplementations! [command] = ctx => f (); }

    /// <summary>
    ///     Commands for Application.
    /// </summary>
    private static Dictionary<Command, Func<CommandContext, bool?>>? CommandImplementations { get; set; }

    private static void ReplaceKey (Key oldKey, Key newKey)
    {
        if (KeyBindings.Bindings.Count == 0)
        {
            return;
        }

        if (newKey == Key.Empty)
        {
            KeyBindings.Remove (oldKey);
        }
        else
        {
            KeyBindings.ReplaceKey (oldKey, newKey);
        }
    }
}
