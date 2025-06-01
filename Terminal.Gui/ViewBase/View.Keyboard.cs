#nullable enable

namespace Terminal.Gui.ViewBase;

public partial class View // Keyboard APIs
{
    /// <summary>
    ///     Helper to configure all things keyboard related for a View. Called from the View constructor.
    /// </summary>
    private void SetupKeyboard ()
    {
        KeyBindings = new (this);
        KeyBindings.Add (Key.Space, Command.Select);
        KeyBindings.Add (Key.Enter, Command.Accept);

        HotKeyBindings = new (this);

        // Note, setting HotKey will bind HotKey to Command.HotKey
        HotKeySpecifier = (Rune)'_';
        TitleTextFormatter.HotKeyChanged += TitleTextFormatter_HotKeyChanged;
    }

    /// <summary>
    ///     Helper to dispose all things keyboard related for a View. Called from the View Dispose method.
    /// </summary>
    private void DisposeKeyboard () { TitleTextFormatter.HotKeyChanged -= TitleTextFormatter_HotKeyChanged; }

    #region HotKey Support

    /// <summary>Raised when the <see cref="HotKey"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs>? HotKeyChanged;

    private Key _hotKey = new ();
    private void TitleTextFormatter_HotKeyChanged (object? sender, KeyChangedEventArgs e) { HotKeyChanged?.Invoke (this, e); }

    /// <summary>
    ///     Gets or sets the hot key defined for this view. Pressing the hot key on the keyboard while this view has focus will
    ///     invoke <see cref="Command.HotKey"/>. By default, the HotKey is set to the first character of <see cref="Text"/>
    ///     that is prefixed with <see cref="HotKeySpecifier"/>.
    ///     <para>
    ///         A HotKey is a keypress that causes a visible UI item to perform an action. For example, in a Dialog,
    ///         with a Button with the text of "_Text" <c>Alt+T</c> will cause the button to gain focus and to raise its
    ///         <see cref="Accepting"/> event.
    ///         Or, in a
    ///         <see cref="Menu"/> with "_File _Edit", <c>Alt+F</c> will select (show) the "_File" menu. If the "_File" menu
    ///         has a
    ///         sub-menu of "_New" <c>Alt+N</c> or <c>N</c> will ONLY select the "_New" sub-menu if the "_File" menu is already
    ///         opened.
    ///     </para>
    ///     <para>
    ///         View subclasses can use <see cref="View.AddCommand(Command,CommandImplementation)"/> to
    ///         define the
    ///         behavior of the hot key.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     <para>See <see href="../docs/keyboard.md"/> for an overview of Terminal.Gui keyboard APIs.</para>
    ///     <para>
    ///         This is a helper API for configuring a key binding for the hot key. By default, this property is set whenever
    ///         <see cref="Text"/> changes.
    ///     </para>
    ///     <para>
    ///         By default, when the Hot Key is set, key bindings are added for both the base key (e.g.
    ///         <see cref="Key.D3"/>) and the Alt-shifted key (e.g. <see cref="Key.D3"/>.
    ///         <see cref="Key.WithAlt"/>). This behavior can be overriden by overriding
    ///         <see cref="AddKeyBindingsForHotKey"/>.
    ///     </para>
    ///     <para>
    ///         By default, when the HotKey is set to <see cref="Key.A"/> through <see cref="Key.Z"/> key bindings will
    ///         be added for both the un-shifted and shifted versions. This means if the HotKey is <see cref="Key.A"/>, key
    ///         bindings for <c>Key.A</c> and <c>Key.A.WithShift</c> will be added. This behavior can be overriden by
    ///         overriding <see cref="AddKeyBindingsForHotKey"/>.
    ///     </para>
    ///     <para>If the hot key is changed, the <see cref="HotKeyChanged"/> event is fired.</para>
    ///     <para>Set to <see cref="Key.Empty"/> to disable the hot key.</para>
    /// </remarks>
    public Key HotKey
    {
        get => _hotKey;
        set
        {
            if (value is null)
            {
                throw new ArgumentException (
                                             @"HotKey must not be null. Use Key.Empty to clear the HotKey.",
                                             nameof (value)
                                            );
            }

            if (AddKeyBindingsForHotKey (_hotKey, value))
            {
                // This will cause TextFormatter_HotKeyChanged to be called, firing HotKeyChanged
                // BUGBUG: _hotkey should be set BEFORE setting TextFormatter.HotKey
                _hotKey = value;
                TitleTextFormatter.HotKey = value;
            }
        }
    }

    /// <summary>
    ///     Adds key bindings for the specified HotKey. Useful for views that contain multiple items that each have their
    ///     own HotKey such as <see cref="RadioGroup"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         By default, key bindings are added for both the base key (e.g. <see cref="Key.D3"/>) and the Alt-shifted key
    ///         (e.g. <c>Key.D3.WithAlt</c>) This behavior can be overriden by overriding <see cref="AddKeyBindingsForHotKey"/>
    ///         .
    ///     </para>
    ///     <para>
    ///         By default, when <paramref name="hotKey"/> is <see cref="Key.A"/> through <see cref="Key.Z"/> key bindings
    ///         will be added for both the un-shifted and shifted versions. This means if the HotKey is <see cref="Key.A"/>,
    ///         key bindings for <c>Key.A</c> and <c>Key.A.WithShift</c> will be added. This behavior can be overriden by
    ///         overriding <see cref="AddKeyBindingsForHotKey"/>.
    ///     </para>
    /// </remarks>
    /// <param name="prevHotKey">The HotKey <paramref name="hotKey"/> is replacing. Key bindings for this key will be removed.</param>
    /// <param name="hotKey">The new HotKey. If <see cref="Key.Empty"/> <paramref name="prevHotKey"/> bindings will be removed.</param>
    /// <param name="context">Arbitrary context that can be associated with this key binding.</param>
    /// <returns><see langword="true"/> if the HotKey bindings were added.</returns>
    /// <exception cref="ArgumentException"></exception>
    public virtual bool AddKeyBindingsForHotKey (Key prevHotKey, Key hotKey, object? context = null)
    {
        if (_hotKey == hotKey)
        {
            return false;
        }

        Key newKey = hotKey;

        Key baseKey = newKey.NoAlt.NoShift.NoCtrl;

        if (newKey != Key.Empty && (baseKey == Key.Space || Rune.IsControl (baseKey.AsRune)))
        {
            throw new ArgumentException (@$"HotKey must be a printable (and non-space) key ({hotKey}).");
        }

        if (newKey != baseKey)
        {
            if (newKey.IsCtrl)
            {
                throw new ArgumentException (@$"HotKey does not support CtrlMask ({hotKey}).");
            }

            // Strip off the shift mask if it's A...Z
            if (baseKey.IsKeyCodeAtoZ)
            {
                newKey = newKey.NoShift;
            }

            // Strip off the Alt mask
            newKey = newKey.NoAlt;
        }

        // Remove base version
        if (HotKeyBindings.TryGet (prevHotKey, out _))
        {
            HotKeyBindings.Remove (prevHotKey);
        }

        // Remove the Alt version
        if (HotKeyBindings.TryGet (prevHotKey.WithAlt, out _))
        {
            HotKeyBindings.Remove (prevHotKey.WithAlt);
        }

        if (_hotKey.IsKeyCodeAtoZ)
        {
            // Remove the shift version
            if (HotKeyBindings.TryGet (prevHotKey.WithShift, out _))
            {
                HotKeyBindings.Remove (prevHotKey.WithShift);
            }

            // Remove alt | shift version
            if (HotKeyBindings.TryGet (prevHotKey.WithShift.WithAlt, out _))
            {
                HotKeyBindings.Remove (prevHotKey.WithShift.WithAlt);
            }
        }

        // Add the new 
        if (newKey != Key.Empty)
        {
            KeyBinding keyBinding = new ()
            {
                Commands = [Command.HotKey],
                Key = newKey,
                Data = context
            };

            // Add the base and Alt key
            HotKeyBindings.Remove (newKey);
            HotKeyBindings.Add (newKey, keyBinding);
            HotKeyBindings.Remove (newKey.WithAlt);
            HotKeyBindings.Add (newKey.WithAlt, keyBinding);

            // If the Key is A..Z, add ShiftMask and AltMask | ShiftMask
            if (newKey.IsKeyCodeAtoZ)
            {
                HotKeyBindings.Remove (newKey.WithShift);
                HotKeyBindings.Add (newKey.WithShift, keyBinding);
                HotKeyBindings.Remove (newKey.WithShift.WithAlt);
                HotKeyBindings.Add (newKey.WithShift.WithAlt, keyBinding);
            }
        }

        return true;
    }

    /// <summary>
    ///     Gets or sets the specifier character for the hot key (e.g. '_'). Set to '\xffff' to disable automatic hot key
    ///     setting support for this View instance. The default is '\xffff'.
    /// </summary>
    public virtual Rune HotKeySpecifier
    {
        get => TitleTextFormatter.HotKeySpecifier;
        set
        {
            TitleTextFormatter.HotKeySpecifier = TextFormatter.HotKeySpecifier = value;
            SetHotKeyFromTitle ();
        }
    }

    private void SetHotKeyFromTitle ()
    {
        if (HotKeySpecifier == new Rune ('\xFFFF'))
        {
            return; // throw new InvalidOperationException ("Can't set HotKey unless a TextFormatter has been created");
        }

        if (Terminal.Gui.Text.TextFormatter.FindHotKey (_title, HotKeySpecifier, out _, out Key hk))
        {
            if (_hotKey != hk)
            {
                HotKey = hk;
            }
        }
        else
        {
            HotKey = Key.Empty;
        }
    }

    #endregion HotKey Support

    #region Low-level Key handling

    #region Key Down Event

    /// <summary>
    ///     If the view is enabled, raises the related key down events on the view, and returns <see langword="true"/> if the
    ///     event was
    ///     handled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the view has a sub view that is focused, <see cref="NewKeyDownEvent"/> will be called on the focused view
    ///         first.
    ///     </para>
    ///     <para>
    ///         If a more focused subview does not handle the key press, this method raises <see cref="OnKeyDown"/>/
    ///         <see cref="KeyDown"/> to allow the
    ///         view to pre-process the key press. If <see cref="OnKeyDown"/>/<see cref="KeyDown"/> is not handled any commands
    ///         bound to the key will be invoked.
    ///         Then, only if no key bindings are
    ///         handled, <see cref="OnKeyDownNotHandled"/>/<see cref="KeyDownNotHandled"/> will be raised allowing the view to
    ///         process the key press.
    ///     </para>
    ///     <para>
    ///         Calling this method for a key bound to the view via an Application-scoped keybinding will have no effect.
    ///         Instead,
    ///         use <see cref="Application.RaiseKeyDownEvent"/>.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the event was handled.</returns>
    public bool NewKeyDownEvent (Key key)
    {
        if (!Enabled)
        {
            return false;
        }

        // TODO: We really need an event before recursing into Focused. Without, there's no way for 
        // TODO: SuperViews to prevent SubViews from seeing certain keys. A use-case for this:
        // TODO:    - MenuBar needs to prevent MenuItems from seeing QuitKey if the MenuItem is not visible

        // If there's a Focused subview, give it a chance (this recurses down the hierarchy)
        if (Focused?.NewKeyDownEvent (key) == true)
        {
            return true;
        }

        // Before (fire the cancellable event)
        if (RaiseKeyDown (key) || key.Handled)
        {
            return true;
        }

        // During (this is what can be cancelled)

        // TODO: NewKeyDownEvent returns bool. It should be bool? so state of InvokeCommands can be reflected up stack
        if (InvokeCommands (key) is true || key.Handled)
        {
            return true;
        }

        bool? handled = InvokeCommandsBoundToHotKey (key);

        if (handled is true)
        {
            return true;
        }

        // After
        if (RaiseKeyDownNotHandled (key) || key.Handled)
        {
            return true;
        }

        return key.Handled;

        bool RaiseKeyDown (Key k)
        {
            // Before (fire the cancellable event)
            if (OnKeyDown (k) || k.Handled)
            {
                return true;
            }

            // fire event
            KeyDown?.Invoke (this, k);

            return k.Handled;
        }

        bool RaiseKeyDownNotHandled (Key k)
        {
            if (OnKeyDownNotHandled (k) || k.Handled)
            {
                return true;
            }

            KeyDownNotHandled?.Invoke (this, k);

            return false;
        }
    }

    /// <summary>
    ///     Called when the user presses a key, allowing subscribers to pre-process the key down event. Called
    ///     before key bindings are invoked and <see cref="KeyDownNotHandled"/> is raised. Set
    ///     <see cref="Key.Handled"/>
    ///     to true to
    ///     stop the key from being processed further.
    /// </summary>
    /// <param name="key">The key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the key down event was not handled. <see langword="true"/> if the event was handled
    ///     and processing should stop.
    /// </returns>
    /// <remarks>
    ///     <para>Fires the <see cref="KeyDown"/> event.</para>
    /// </remarks>
    protected virtual bool OnKeyDown (Key key) { return false; }

    /// <summary>
    ///     Raised when the user presses a key, allowing subscribers to pre-process the key down event. Called
    ///     before key bindings are invoked and <see cref="KeyDownNotHandled"/> is raised. Set
    ///     <see cref="Key.Handled"/>
    ///     to true to
    ///     stop the key from being processed further.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Not all terminals support key distinct up notifications, Applications should avoid depending on distinct
    ///         KeyUp events.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    public event EventHandler<Key>? KeyDown;

    /// <summary>
    ///     Called when the user has pressed key it wasn't handled by <see cref="KeyDown"/> and was not bound to a key binding.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Not all terminals support distinct key up notifications; applications should avoid depending on distinct
    ///         KeyUp events.
    ///     </para>
    /// </remarks>
    /// <param name="key">Contains the details about the key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the key press was not handled. <see langword="true"/> if the keypress was handled
    ///     and no other view should see it.
    /// </returns>
    protected virtual bool OnKeyDownNotHandled (Key key) { return key.Handled; }

    /// <summary>
    ///     Raised when the user has pressed key it wasn't handled by <see cref="KeyDown"/> and was not bound to a key binding.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         SubViews can use the <see cref="KeyDownNotHandled"/> of their super view override the default behavior of when
    ///         key bindings are invoked.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    public event EventHandler<Key>? KeyDownNotHandled;

    #endregion KeyDown Event

    #region KeyUp Event

    /// <summary>
    ///     If the view is enabled, raises the related key up events on the view, and returns <see langword="true"/> if the
    ///     event was
    ///     handled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Not all terminals support key distinct down/up notifications, Applications should avoid depending on distinct
    ///         KeyUp events.
    ///     </para>
    ///     <para>
    ///         If the view has a sub view that is focused, <see cref="NewKeyUpEvent"/> will be called on the focused view
    ///         first.
    ///     </para>
    ///     <para>
    ///         If the focused sub view does not handle the key press, this method raises <see cref="OnKeyUp"/>/
    ///         <see cref="KeyUp"/> to allow the
    ///         view to pre-process the key press.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    /// <param name="key"></param>
    /// <returns><see langword="true"/> if the event was handled.</returns>
    public bool NewKeyUpEvent (Key key)
    {
        if (!Enabled)
        {
            return false;
        }

        // Before
        if (RaiseKeyUp (key) || key.Handled)
        {
            return true;
        }

        // During

        // After

        return false;

        bool RaiseKeyUp (Key k)
        {
            // Before (fire the cancellable event)
            if (OnKeyUp (k) || k.Handled)
            {
                return true;
            }

            // fire event
            KeyUp?.Invoke (this, k);

            return k.Handled;
        }
    }

    /// <summary>Called when a key is released. This method is called from <see cref="NewKeyUpEvent"/>.</summary>
    /// <param name="key">Contains the details about the key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the keys up event was not handled. <see langword="true"/> if no other view should see
    ///     it.
    /// </returns>
    /// <remarks>
    ///     Not all terminals support key distinct down/up notifications, Applications should avoid depending on distinct KeyUp
    ///     events.
    ///     <para>
    ///         Overrides must call into the base and return <see langword="true"/> if the base returns
    ///         <see langword="true"/>.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    public virtual bool OnKeyUp (Key key) { return false; }

    /// <summary>
    ///     Raised when a key is released. Set <see cref="Key.Handled"/> to true to stop the key up event from being processed
    ///     by other views.
    ///     <remarks>
    ///         Not all terminals support key distinct down/up notifications, Applications should avoid depending on
    ///         distinct KeyDown and KeyUp events and instead should use <see cref="KeyDown"/>.
    ///         <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    ///     </remarks>
    /// </summary>
    public event EventHandler<Key>? KeyUp;

    #endregion KeyUp Event

    #endregion Low-level Key handling

    #region Key Bindings

    /// <summary>Gets the bindings for this view that will be invoked only if this view has focus.</summary>
    public KeyBindings KeyBindings { get; internal set; } = null!;

    /// <summary>Gets the bindings for this view that will be invoked regardless of whether this view has focus or not.</summary>
    public KeyBindings HotKeyBindings { get; internal set; } = null!;

    /// <summary>
    ///     INTERNAL API: Invokes any commands bound to <paramref name="key"/> on this view, adornments, and subviews.
    /// </summary>
    /// <param name="key"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was invoked or there was no matching key binding; input processing should
    ///     continue.
    ///     <see langword="false"/> if a command was invoked and was not handled (or cancelled); input processing should
    ///     continue.
    ///     <see langword="true"/> if at least one command was invoked and handled (or
    ///     cancelled); input processing should stop.
    /// </returns>
    internal bool? InvokeCommands (Key key)
    {
        // * If no key binding was found, `InvokeKeyBindings` returns `null`.
        //   Continue passing the event (return `false` from `OnInvokeKeyBindings`).
        // * If key bindings were found, but none handled the key (all `Command`s returned `false`),
        //   `InvokeKeyBindings` returns `false`. Continue passing the event (return `false` from `OnInvokeKeyBindings`)..
        // * If key bindings were found, and any handled the key (at least one `Command` returned `true`),
        //   `InvokeKeyBindings` returns `true`. Continue passing the event (return `false` from `OnInvokeKeyBindings`).
        bool? handled = DoInvokeCommands (key);

        if (handled is true)
        {
            // Stop processing if any key binding handled the key.
            // DO NOT stop processing if there are no matching key bindings or none of the key bindings handled the key
            return handled;
        }

        if (Margin is { } && InvokeCommandsBoundToKeyOnAdornment (Margin, key, ref handled))
        {
            return true;
        }

        if (Padding is { } && InvokeCommandsBoundToKeyOnAdornment (Padding, key, ref handled))
        {
            return true;
        }

        if (Border is { } && InvokeCommandsBoundToKeyOnAdornment (Border, key, ref handled))
        {
            return true;
        }

        return handled;
    }

    private static bool InvokeCommandsBoundToKeyOnAdornment (Adornment adornment, Key key, ref bool? handled)
    {
        if (!adornment.Enabled)
        {
            return false;
        }

        bool? adornmentHandled = adornment.InvokeCommands (key);

        if (adornmentHandled is true)
        {
            return true;
        }

        if (adornment?.InternalSubViews is null)
        {
            return false;
        }

        foreach (View subview in adornment.InternalSubViews)
        {
            bool? subViewHandled = subview.InvokeCommands (key);

            if (subViewHandled is { })
            {
                handled = subViewHandled;

                if ((bool)subViewHandled)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // BUGBUG: This will miss any hotkeys in subviews of Adornments.
    /// <summary>
    ///     Invokes any commands bound to <paramref name="hotKey"/> on this view and subviews.
    /// </summary>
    /// <param name="hotKey"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was invoked; input processing should continue.
    ///     <see langword="false"/> if at least one command was invoked and was not handled (or cancelled); input processing
    ///     should continue.
    ///     <see langword="true"/> if at least one command was invoked and handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    internal bool? InvokeCommandsBoundToHotKey (Key hotKey)
    {
        if (!Enabled)
        {
            return false;
        }

        bool? handled = null;
        // Process this View
        if (HotKeyBindings.TryGet (hotKey, out KeyBinding binding))
        {
            if (binding.Key is null || binding.Key == Key.Empty)
            {
                binding.Key = hotKey;
            }

            if (InvokeCommands (binding.Commands, binding) is true)
            {
                return true;
            }
        }

        // Now, process any HotKey bindings in the subviews
        foreach (View subview in InternalSubViews.ToList())
        {
            if (subview == Focused)
            {
                continue;
            }

            bool? recurse = subview.InvokeCommandsBoundToHotKey (hotKey);

            if (recurse is true || (handled is { } && (bool)handled))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Invokes the Commands bound to <paramref name="key"/>.
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </summary>
    /// <param name="key">The key event passed.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was invoked; input processing should continue.
    ///     <see langword="false"/> if at least one command was invoked and was not handled (or cancelled); input processing
    ///     should continue.
    ///     <see langword="true"/> if at least one command was invoked and handled (or cancelled); input processing should
    ///     stop.
    /// </returns>
    protected bool? DoInvokeCommands (Key key)
    {
        if (!KeyBindings.TryGet (key, out KeyBinding binding))
        {
            return null;
        }

        // TODO: Should we set binding.Key = key if it's not set?
        if (binding is {} && (binding.Key is null || !binding.Key.IsValid))
        {
            binding.Key = key;
        }

        return InvokeCommands (binding.Commands, binding);
    }

    #endregion Key Bindings
}
