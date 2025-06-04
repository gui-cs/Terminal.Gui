#nullable enable

namespace Terminal.Gui.App;

public static partial class Application // Keyboard handling
{
    /// <summary>
    ///     Called when the user presses a key (by the <see cref="IConsoleDriver"/>). Raises the cancelable
    ///     <see cref="KeyDown"/> event, then calls <see cref="View.NewKeyDownEvent"/> on all top level views, and finally
    ///     if the key was not handled, invokes any Application-scoped <see cref="KeyBindings"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool RaiseKeyDownEvent (Key key)
    {
        Logging.Debug ($"{key}");

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

        if (Popover?.DispatchKeyDown (key) is true)
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

        bool? commandHandled = InvokeCommandsBoundToKey (key);
        if(commandHandled is true)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Invokes any commands bound at the Application-level to <paramref name="key"/>.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should stop.
    /// </returns>
    public static bool? InvokeCommandsBoundToKey (Key key)
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

    /// <summary>
    ///     Invokes an Application-bound command.
    /// </summary>
    /// <param name="command">The Command to invoke</param>
    /// <param name="key">The Application-bound Key that was pressed.</param>
    /// <param name="binding">Describes the binding.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was found; input processing should continue.
    ///     <see langword="false"/> if the command was invoked and was not handled (or cancelled); input processing should continue.
    ///     <see langword="true"/> if the command was invoked the command was handled (or cancelled); input processing should stop.
    /// </returns>
    /// <exception cref="NotSupportedException"></exception>
    public static bool? InvokeCommand (Command command, Key key, KeyBinding binding)
    {
        if (!_commandImplementations!.ContainsKey (command))
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
    ///     Called when the user releases a key (by the <see cref="IConsoleDriver"/>). Raises the cancelable
    ///     <see cref="KeyUp"/>
    ///     event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="RaiseKeyDownEvent"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key release events.</remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool RaiseKeyUpEvent (Key key)
    {
        if (!Initialized)
        {
            return true;
        }

        KeyUp?.Invoke (null, key);

        if (key.Handled)
        {
            return true;
        }


        // TODO: Add Popover support

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

    static Application ()
    {
        AddKeyBindings ();
    }

    /// <summary>Gets the Application-scoped key bindings.</summary>
    public static KeyBindings KeyBindings { get; internal set; } = new (null);

    internal static void AddKeyBindings ()
    {
        _commandImplementations.Clear ();

        // Things Application knows how to do
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
                        LayoutAndDraw (true);

                        return true;
                    }
                   );

        AddCommand (
                    Command.Arrange,
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
    ///         This version of AddCommand is for commands that do not require a <see cref="ICommandContext"/>.
    ///     </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    private static void AddCommand (Command command, Func<bool?> f) { _commandImplementations! [command] = ctx => f (); }

    /// <summary>
    ///     Commands for Application.
    /// </summary>
    private static readonly Dictionary<Command, View.CommandImplementation> _commandImplementations = new ();
}
