#nullable enable
namespace Terminal.Gui;

public static partial class Application // Keyboard handling
{
    /// <summary>
    ///     Called when the user presses a key (by the <see cref="ConsoleDriver"/>). Raises the cancelable
    ///     <see cref="KeyDown"/> event, then calls <see cref="View.NewKeyDownEvent"/> on all top level views, and finally
    ///     if the key was not handled, invokes any Application-scoped <see cref="KeyBindings"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool RaiseKeyDownEvent (Key key)
    {
        KeyDown?.Invoke (null, key);

        if (key.Handled)
        {
            return true;
        }

        if (Top is null)
        {
            foreach (Toplevel topLevel in TopLevels.ToList ())
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
        else
        {
            if (Top.NewKeyDownEvent (key))
            {
                return true;
            }
        }

        // Invoke any Application-scoped KeyBindings.
        // The first view that handles the key will stop the loop.
        foreach (KeyValuePair<Key, KeyBinding> binding in KeyBindings.Bindings.Where (b => b.Key == key.KeyCode))
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
                if (!KeyBindings.TryGet (key, KeyBindingScope.Application, out KeyBinding appBinding))
                {
                    continue;
                }

                bool? toReturn = null;

                foreach (Command command in appBinding.Commands)
                {
                    toReturn = InvokeCommand (command, key, appBinding);
                }

                return toReturn ?? true;
            }
        }

        return false;

        static bool? InvokeCommand (Command command, Key key, KeyBinding appBinding)
        {
            if (!CommandImplementations!.ContainsKey (command))
            {
                throw new NotSupportedException (
                                                 @$"A KeyBinding was set up for the command {command} ({key}) but that command is not supported by Application."
                                                );
            }

            if (CommandImplementations.TryGetValue (command, out View.CommandImplementation? implementation))
            {
                var context = new CommandContext (command, key, appBinding); // Create the context here

                return implementation (context);
            }

            return false;
        }
    }

    /// <summary>
    ///     Raised when the user presses a key.
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
    ///     Called when the user releases a key (by the <see cref="ConsoleDriver"/>). Raises the cancelable <see cref="KeyUp"/>
    ///     event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="RaiseKeyDownEvent"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key release events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool RaiseKeyUpEvent (Key key)
    {
        if (!IsInitialized)
        {
            return true;
        }

        KeyUp?.Invoke (null, key);

        if (key.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in TopLevels.ToList ())
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

        return false;
    }

    #region Application-scoped KeyBindings

    static Application () { AddApplicationKeyBindings (); }

    /// <summary>Gets the Application-scoped key bindings.</summary>
    public static KeyBindings KeyBindings { get; internal set; } = new ();

    internal static void AddApplicationKeyBindings ()
    {
        CommandImplementations = new ();

        // Things this view knows how to do
        AddCommand (
                    Command.Quit,
                    static () =>
                    {
                        RequestStop ();

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
                    Command.NextTabStop,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop));

        AddCommand (
                    Command.PreviousTabStop,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabStop));

        AddCommand (
                    Command.NextTabGroup,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabGroup));

        AddCommand (
                    Command.PreviousTabGroup,
                    static () => Navigation?.AdvanceFocus (NavigationDirection.Backward, TabBehavior.TabGroup));

        AddCommand (
                    Command.Refresh,
                    static () =>
                    {
                        Refresh ();

                        return true;
                    }
                   );

        AddCommand (
                    Command.Edit,
                    static () =>
                    {
                        View? viewToArrange = Navigation?.GetFocused ();

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

        KeyBindings.Clear ();

        // Resources/config.json overrides
        NextTabKey = Key.Tab;
        PrevTabKey = Key.Tab.WithShift;
        NextTabGroupKey = Key.F6;
        PrevTabGroupKey = Key.F6.WithShift;
        QuitKey = Key.Esc;
        ArrangeKey = Key.F5.WithCtrl;

        KeyBindings.Add (QuitKey, KeyBindingScope.Application, Command.Quit);

        KeyBindings.Add (Key.CursorRight, KeyBindingScope.Application, Command.NextTabStop);
        KeyBindings.Add (Key.CursorDown, KeyBindingScope.Application, Command.NextTabStop);
        KeyBindings.Add (Key.CursorLeft, KeyBindingScope.Application, Command.PreviousTabStop);
        KeyBindings.Add (Key.CursorUp, KeyBindingScope.Application, Command.PreviousTabStop);
        KeyBindings.Add (NextTabKey, KeyBindingScope.Application, Command.NextTabStop);
        KeyBindings.Add (PrevTabKey, KeyBindingScope.Application, Command.PreviousTabStop);

        KeyBindings.Add (NextTabGroupKey, KeyBindingScope.Application, Command.NextTabGroup);
        KeyBindings.Add (PrevTabGroupKey, KeyBindingScope.Application, Command.PreviousTabGroup);

        KeyBindings.Add (ArrangeKey, KeyBindingScope.Application, Command.Edit);

        // TODO: Refresh Key should be configurable
        KeyBindings.Add (Key.F5, KeyBindingScope.Application, Command.Refresh);

        // TODO: Suspend Key should be configurable
        if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            KeyBindings.Add (Key.Z.WithCtrl, KeyBindingScope.Application, Command.Suspend);
        }
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


    #endregion Application-scoped KeyBindings

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
    private static Dictionary<Command, View.CommandImplementation>? CommandImplementations { get; set; }

}
