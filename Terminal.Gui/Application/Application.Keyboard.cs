#nullable enable
using System.Text.Json.Serialization;

namespace Terminal.Gui;

public static partial class Application // Keyboard handling
{
    private static Key _nextTabKey = Key.Tab; // Resources/config.json overrrides

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

    private static Key _prevTabKey = Key.Tab.WithShift; // Resources/config.json overrrides

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

    private static Key _nextTabGroupKey = Key.F6; // Resources/config.json overrrides

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

    private static Key _prevTabGroupKey = Key.F6.WithShift; // Resources/config.json overrrides

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

    private static Key _quitKey = Key.Esc; // Resources/config.json overrrides

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

    /// <summary>Gets the key bindings for this view.</summary>
    public static KeyBindings KeyBindings { get; internal set; } = new ();

    /// <summary>
    ///     Commands for Application.
    /// </summary>
    private static Dictionary<Command, Func<CommandContext, bool?>>? CommandImplementations { get; set; }

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

    static Application () { AddApplicationKeyBindings (); }

    internal static void AddApplicationKeyBindings ()
    {
        CommandImplementations = new ();

        // Things this view knows how to do
        AddCommand (
                    Command.QuitToplevel, // TODO: IRunnable: Rename to Command.Quit to make more generic.
                    () =>
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
                    () =>
                    {
                        Driver?.Suspend ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.NextView,
                    () =>
                    {
                        ApplicationNavigation.MoveNextView ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PreviousView,
                    () =>
                    {
                        ApplicationNavigation.MovePreviousView ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.NextViewOrTop,
                    () =>
                    {
                        ApplicationNavigation.MoveNextViewOrTop ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.PreviousViewOrTop,
                    () =>
                    {
                        ApplicationNavigation.MovePreviousViewOrTop ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Refresh,
                    () =>
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

        KeyBindings.Add (NextTabGroupKey, KeyBindingScope.Application, Command.NextViewOrTop); // Needed on Unix
        KeyBindings.Add (PrevTabGroupKey, KeyBindingScope.Application, Command.PreviousViewOrTop); // Needed on Unix

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
}
