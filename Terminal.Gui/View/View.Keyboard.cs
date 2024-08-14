using System.ComponentModel;
using System.Diagnostics;

namespace Terminal.Gui;

public partial class View  // Keyboard APIs
{
    /// <summary>
    ///  Helper to configure all things keyboard related for a View. Called from the View constructor.
    /// </summary>
    private void SetupKeyboard ()
    {
        KeyBindings = new (this);
        HotKeySpecifier = (Rune)'_';
        TitleTextFormatter.HotKeyChanged += TitleTextFormatter_HotKeyChanged;

        // By default, the HotKey command sets the focus
        AddCommand (Command.HotKey, OnHotKey);

        // By default, the Accept command raises the Accept event
        AddCommand (Command.Accept, OnAccept);
    }

    /// <summary>
    ///    Helper to dispose all things keyboard related for a View. Called from the View Dispose method.
    /// </summary>
    private void DisposeKeyboard ()
    {
        TitleTextFormatter.HotKeyChanged -= TitleTextFormatter_HotKeyChanged;
        Application.RemoveKeyBindings (this);
    }

    #region HotKey Support

    /// <summary>
    /// Called when the HotKey command (<see cref="Command.HotKey"/>) is invoked. Causes this view to be focused.
    /// </summary>
    /// <returns>If <see langword="true"/> the command was canceled.</returns>
    private bool? OnHotKey ()
    {
        if (CanFocus)
        {
            SetFocus ();
            return true;
        }

        return false;
    }

    /// <summary>Invoked when the <see cref="HotKey"/> is changed.</summary>
    public event EventHandler<KeyChangedEventArgs> HotKeyChanged;

    private Key _hotKey = new ();
    private void TitleTextFormatter_HotKeyChanged (object sender, KeyChangedEventArgs e) { HotKeyChanged?.Invoke (this, e); }

    /// <summary>
    ///     Gets or sets the hot key defined for this view. Pressing the hot key on the keyboard while this view has focus will
    ///     invoke the <see cref="Command.HotKey"/> and <see cref="Command.Accept"/> commands. <see cref="Command.HotKey"/>
    ///     causes the view to be focused and <see cref="Command.Accept"/> does nothing. By default, the HotKey is
    ///     automatically set to the first character of <see cref="Text"/> that is prefixed with <see cref="HotKeySpecifier"/>.
    ///     <para>
    ///         A HotKey is a keypress that selects a visible UI item. For selecting items across <see cref="View"/>`s (e.g.a
    ///         <see cref="Button"/> in a <see cref="Dialog"/>) the keypress must include the <see cref="Key.WithAlt"/>
    ///         modifier. For selecting items within a View that are not Views themselves, the keypress can be key without the
    ///         Alt modifier. For example, in a Dialog, a Button with the text of "_Text" can be selected with Alt-T. Or, in a
    ///         <see cref="Menu"/> with "_File _Edit", Alt-F will select (show) the "_File" menu. If the "_File" menu has a
    ///         sub-menu of "_New" `Alt-N` or `N` will ONLY select the "_New" sub-menu if the "_File" menu is already opened.
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
    public virtual Key HotKey
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
    ///         (e.g. <c>Key.D3.WithAlt</c>) This behavior can be overriden by overriding <see cref="AddKeyBindingsForHotKey"/>.
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
    public virtual bool AddKeyBindingsForHotKey (Key prevHotKey, Key hotKey, [CanBeNull] object context = null)
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
        if (KeyBindings.TryGet (prevHotKey, out _))
        {
            KeyBindings.Remove (prevHotKey);
        }

        // Remove the Alt version
        if (KeyBindings.TryGet (prevHotKey.WithAlt, out _))
        {
            KeyBindings.Remove (prevHotKey.WithAlt);
        }

        if (_hotKey.IsKeyCodeAtoZ)
        {
            // Remove the shift version
            if (KeyBindings.TryGet (prevHotKey.WithShift, out _))
            {
                KeyBindings.Remove (prevHotKey.WithShift);
            }

            // Remove alt | shift version
            if (KeyBindings.TryGet (prevHotKey.WithShift.WithAlt, out _))
            {
                KeyBindings.Remove (prevHotKey.WithShift.WithAlt);
            }
        }

        // Add the new 
        if (newKey != Key.Empty)
        {
            KeyBinding keyBinding = new ([Command.HotKey], KeyBindingScope.HotKey, context);
            // Add the base and Alt key
            KeyBindings.Remove (newKey);
            KeyBindings.Add (newKey, keyBinding);
            KeyBindings.Remove (newKey.WithAlt);
            KeyBindings.Add (newKey.WithAlt, keyBinding);

            // If the Key is A..Z, add ShiftMask and AltMask | ShiftMask
            if (newKey.IsKeyCodeAtoZ)
            {
                KeyBindings.Remove (newKey.WithShift);
                KeyBindings.Add (newKey.WithShift, keyBinding);
                KeyBindings.Remove (newKey.WithShift.WithAlt);
                KeyBindings.Add (newKey.WithShift.WithAlt, keyBinding);
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
        get
        {
            return TitleTextFormatter.HotKeySpecifier;
        }
        set
        {
            TitleTextFormatter.HotKeySpecifier = TextFormatter.HotKeySpecifier = value;
            SetHotKeyFromTitle ();
        }
    }

    private void SetHotKeyFromTitle ()
    {
        if (TitleTextFormatter == null || HotKeySpecifier == new Rune ('\xFFFF'))
        {
            return; // throw new InvalidOperationException ("Can't set HotKey unless a TextFormatter has been created");
        }

        if (TextFormatter.FindHotKey (_title, HotKeySpecifier, out _, out Key hk))
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
    ///     If the view is enabled, processes a new key down event and returns <see langword="true"/> if the event was
    ///     handled.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If the view has a sub view that is focused, <see cref="NewKeyDownEvent"/> will be called on the focused view
    ///         first.
    ///     </para>
    ///     <para>
    ///         If the focused sub view does not handle the key press, this method calls <see cref="OnKeyDown"/> to allow the
    ///         view to pre-process the key press. If <see cref="OnKeyDown"/> returns <see langword="false"/>, this method then
    ///         calls <see cref="OnInvokingKeyBindings"/> to invoke any key bindings. Then, only if no key bindings are
    ///         handled, <see cref="OnProcessKeyDown"/> will be called allowing the view to process the key press.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    /// <param name="keyEvent"></param>
    /// <returns><see langword="true"/> if the event was handled.</returns>
    public bool NewKeyDownEvent (Key keyEvent)
    {
        if (!Enabled)
        {
            return false;
        }

        // By default the KeyBindingScope is View

        if (Focused?.NewKeyDownEvent (keyEvent) == true)
        {
            return true;
        }

        // Before (fire the cancellable event)
        if (OnKeyDown (keyEvent))
        {
            return true;
        }

        // During (this is what can be cancelled)
        InvokingKeyBindings?.Invoke (this, keyEvent);

        if (keyEvent.Handled)
        {
            return true;
        }

        // TODO: NewKeyDownEvent returns bool. It should be bool? so state of InvokeCommand can be reflected up stack

        bool? handled = OnInvokingKeyBindings (keyEvent, KeyBindingScope.HotKey | KeyBindingScope.Focused);

        if (handled is { } && (bool)handled)
        {
            return true;
        }

        // TODO: The below is not right. OnXXX handlers are supposed to fire the events.
        // TODO: But I've moved it outside of the v-function to test something.
        // After (fire the cancellable event)
        // fire event
        ProcessKeyDown?.Invoke (this, keyEvent);

        if (!keyEvent.Handled && OnProcessKeyDown (keyEvent))
        {
            return true;
        }

        return keyEvent.Handled;
    }

    /// <summary>
    ///     Low-level API called when the user presses a key, allowing a view to pre-process the key down event. This is
    ///     called from <see cref="NewKeyDownEvent"/> before <see cref="OnInvokingKeyBindings"/>.
    /// </summary>
    /// <param name="keyEvent">Contains the details about the key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the key press was not handled. <see langword="true"/> if the keypress was handled
    ///     and no other view should see it.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         For processing <see cref="HotKey"/>s and commands, use <see cref="Command"/> and
    ///         <see cref="KeyBindings.Add(Key, Command[])"/>instead.
    ///     </para>
    ///     <para>Fires the <see cref="KeyDown"/> event.</para>
    /// </remarks>
    public virtual bool OnKeyDown (Key keyEvent)
    {
        // fire event
        KeyDown?.Invoke (this, keyEvent);

        return keyEvent.Handled;
    }

    /// <summary>
    ///     Invoked when the user presses a key, allowing subscribers to pre-process the key down event. This is fired
    ///     from <see cref="OnKeyDown"/> before <see cref="OnInvokingKeyBindings"/>. Set <see cref="Key.Handled"/> to true to
    ///     stop the key from being processed by other views.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Not all terminals support key distinct up notifications, Applications should avoid depending on distinct
    ///         KeyUp events.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    public event EventHandler<Key> KeyDown;

    /// <summary>
    ///     Low-level API called when the user presses a key, allowing views do things during key down events. This is
    ///     called from <see cref="NewKeyDownEvent"/> after <see cref="OnInvokingKeyBindings"/>.
    /// </summary>
    /// <param name="keyEvent">Contains the details about the key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the key press was not handled. <see langword="true"/> if the keypress was handled
    ///     and no other view should see it.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         Override <see cref="OnProcessKeyDown"/> to override the behavior of how the base class processes key down
    ///         events.
    ///     </para>
    ///     <para>
    ///         For processing <see cref="HotKey"/>s and commands, use <see cref="Command"/> and
    ///         <see cref="KeyBindings.Add(Key, Command[])"/>instead.
    ///     </para>
    ///     <para>Fires the <see cref="ProcessKeyDown"/> event.</para>
    ///     <para>
    ///         Not all terminals support distinct key up notifications; applications should avoid depending on distinct
    ///         KeyUp events.
    ///     </para>
    /// </remarks>
    public virtual bool OnProcessKeyDown (Key keyEvent)
    {
        //ProcessKeyDown?.Invoke (this, keyEvent);
        return keyEvent.Handled;
    }

    /// <summary>
    ///     Invoked when the user presses a key, allowing subscribers to do things during key down events. Set
    ///     <see cref="Key.Handled"/> to true to stop the key from being processed by other views. Invoked after
    ///     <see cref="KeyDown"/> and before <see cref="InvokingKeyBindings"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         SubViews can use the <see cref="ProcessKeyDown"/> of their super view override the default behavior of when
    ///         key bindings are invoked.
    ///     </para>
    ///     <para>
    ///         Not all terminals support distinct key up notifications; applications should avoid depending on distinct
    ///         KeyUp events.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    public event EventHandler<Key> ProcessKeyDown;

    #endregion KeyDown Event

    #region KeyUp Event

    /// <summary>
    ///     If the view is enabled, processes a new key up event and returns <see langword="true"/> if the event was
    ///     handled. Called before <see cref="NewKeyDownEvent"/>.
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
    ///         If the focused sub view does not handle the key press, this method calls <see cref="OnKeyUp"/>, which is
    ///         cancellable.
    ///     </para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    /// <param name="keyEvent"></param>
    /// <returns><see langword="true"/> if the event was handled.</returns>
    public bool NewKeyUpEvent (Key keyEvent)
    {
        if (!Enabled)
        {
            return false;
        }

        if (Focused?.NewKeyUpEvent (keyEvent) == true)
        {
            return true;
        }

        // Before (fire the cancellable event)
        if (OnKeyUp (keyEvent))
        {
            return true;
        }

        // During (this is what can be cancelled)
        // TODO: Until there's a clear use-case, we will not define 'during' event (e.g. OnDuringKeyUp). 

        // After (fire the cancellable event InvokingKeyBindings)
        // TODO: Until there's a clear use-case, we will not define an 'after' event (e.g. OnAfterKeyUp). 

        return false;
    }

    /// <summary>Method invoked when a key is released. This method is called from <see cref="NewKeyUpEvent"/>.</summary>
    /// <param name="keyEvent">Contains the details about the key that produced the event.</param>
    /// <returns>
    ///     <see langword="false"/> if the key stroke was not handled. <see langword="true"/> if no other view should see
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
    public virtual bool OnKeyUp (Key keyEvent)
    {
        // fire event
        KeyUp?.Invoke (this, keyEvent);

        if (keyEvent.Handled)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Invoked when a key is released. Set <see cref="Key.Handled"/> to true to stop the key up event from being processed
    ///     by other views.
    ///     <remarks>
    ///         Not all terminals support key distinct down/up notifications, Applications should avoid depending on
    ///         distinct KeyDown and KeyUp events and instead should use <see cref="KeyDown"/>.
    ///         <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    ///     </remarks>
    /// </summary>
    public event EventHandler<Key> KeyUp;

    #endregion KeyUp Event

    #endregion Low-level Key handling

    #region Key Bindings

    /// <summary>Gets the key bindings for this view.</summary>
    public KeyBindings KeyBindings { get; internal set; }

    private Dictionary<Command, Func<CommandContext, bool?>> CommandImplementations { get; } = new ();

    /// <summary>
    ///     Low-level API called when a user presses a key; invokes any key bindings set on the view. This is called
    ///     during <see cref="NewKeyDownEvent"/> after <see cref="OnKeyDown"/> has returned.
    /// </summary>
    /// <remarks>
    ///     <para>Fires the <see cref="InvokingKeyBindings"/> event.</para>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </remarks>
    /// <param name="keyEvent">Contains the details about the key that produced the event.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>
    ///     <see langword="false"/> if the key press was not handled. <see langword="true"/> if the keypress was handled
    ///     and no other view should see it.
    /// </returns>
    public virtual bool? OnInvokingKeyBindings (Key keyEvent, KeyBindingScope scope)
    {
        // fire event only if there's a hotkey binding for the key
        if (KeyBindings.TryGet (keyEvent, scope, out KeyBinding kb))
        {
            InvokingKeyBindings?.Invoke (this, keyEvent);
            if (keyEvent.Handled)
            {
                return true;
            }

        }

        // * If no key binding was found, `InvokeKeyBindings` returns `null`.
        //   Continue passing the event (return `false` from `OnInvokeKeyBindings`).
        // * If key bindings were found, but none handled the key (all `Command`s returned `false`),
        //   `InvokeKeyBindings` returns `false`. Continue passing the event (return `false` from `OnInvokeKeyBindings`)..
        // * If key bindings were found, and any handled the key (at least one `Command` returned `true`),
        //   `InvokeKeyBindings` returns `true`. Continue passing the event (return `false` from `OnInvokeKeyBindings`).
        bool? handled = InvokeKeyBindings (keyEvent, scope);

        if (handled is { } && (bool)handled)
        {
            // Stop processing if any key binding handled the key.
            // DO NOT stop processing if there are no matching key bindings or none of the key bindings handled the key
            return true;
        }

        if (Margin is { } && ProcessAdornmentKeyBindings (Margin, keyEvent, scope, ref handled))
        {
            return true;
        }

        if (Padding is { } && ProcessAdornmentKeyBindings (Padding, keyEvent, scope, ref handled))
        {
            return true;
        }

        if (Border is { } && ProcessAdornmentKeyBindings (Border, keyEvent, scope, ref handled))
        {
            return true;
        }

        if (ProcessSubViewKeyBindings (keyEvent, scope, ref handled))
        {
            return true;
        }

        return handled;
    }

    private bool ProcessAdornmentKeyBindings (Adornment adornment, Key keyEvent, KeyBindingScope scope, ref bool? handled)
    {
        if (adornment?.Subviews is null)
        {
            return false;
        }

        foreach (View subview in adornment.Subviews)
        {
            bool? subViewHandled = subview.OnInvokingKeyBindings (keyEvent, scope);

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

    private bool ProcessSubViewKeyBindings (Key keyEvent, KeyBindingScope scope, ref bool? handled, bool invoke = true)
    {
        // Now, process any key bindings in the subviews that are tagged to KeyBindingScope.HotKey.
        foreach (View subview in Subviews)
        {
            if (subview == Focused)
            {
                continue;
            }
            if (subview.KeyBindings.TryGet (keyEvent, scope, out KeyBinding binding))
            {
                if (binding.Scope == KeyBindingScope.Focused && !subview.HasFocus)
                {
                    continue;
                }

                if (!invoke)
                {
                    return true;
                }

                bool? subViewHandled = subview.OnInvokingKeyBindings (keyEvent, scope);

                if (subViewHandled is { })
                {
                    handled = subViewHandled;
                    if ((bool)subViewHandled)
                    {
                        return true;
                    }
                }
            }

            bool recurse = subview.ProcessSubViewKeyBindings (keyEvent, scope, ref handled, invoke);
            if (recurse || (handled is { } && (bool)handled))
            {
                return true;
            }
        }

        return false;
    }

    // TODO: This is a "prototype" debug check. It may be too annoying vs. useful.
    // TODO: A better approach would be to have Application hold a list of bound Hotkeys, similar to
    // TODO: how Application holds a list of Application Scoped key bindings and then check that list.
    /// <summary>
    /// Returns true if Key is bound in this view hierarchy. For debugging
    /// </summary>
    /// <param name="key">The key to test.</param>
    /// <param name="boundView">Returns the view the key is bound to.</param>
    /// <returns></returns>
    public bool IsHotKeyKeyBound (Key key, out View boundView)
    {
        // recurse through the subviews to find the views that has the key bound
        boundView = null;

        foreach (View subview in Subviews)
        {
            if (subview.KeyBindings.TryGet (key, KeyBindingScope.HotKey, out _))
            {
                boundView = subview;
                return true;
            }

            if (subview.IsHotKeyKeyBound (key, out boundView))
            {
                return true;
            }

        }
        return false;
    }

    /// <summary>
    ///     Invoked when a key is pressed that may be mapped to a key binding. Set <see cref="Key.Handled"/> to true to
    ///     stop the key from being processed by other views.
    /// </summary>
    public event EventHandler<Key> InvokingKeyBindings;

    /// <summary>
    ///     Invokes any binding that is registered on this <see cref="View"/> and matches the <paramref name="key"/>
    ///     <para>See <see href="../docs/keyboard.md">for an overview of Terminal.Gui keyboard APIs.</see></para>
    /// </summary>
    /// <param name="key">The key event passed.</param>
    /// <param name="scope">The scope.</param>
    /// <returns>
    ///     <see langword="null"/> if no command was bound the <paramref name="key"/>. <see langword="true"/> if
    ///     commands were invoked and at least one handled the command. <see langword="false"/> if commands were invoked and at
    ///     none handled the command.
    /// </returns>
    protected bool? InvokeKeyBindings (Key key, KeyBindingScope scope)
    {
        bool? toReturn = null;

        if (!KeyBindings.TryGet (key, scope, out KeyBinding binding))
        {
            return null;
        }

#if DEBUG

        // TODO: Determine if App scope bindings should be fired first or last (currently last).
        if (Application.KeyBindings.TryGet (key, KeyBindingScope.Focused | KeyBindingScope.HotKey, out KeyBinding b))
        {
            //var boundView = views [0];
            //var commandBinding = boundView.KeyBindings.Get (key);
            Debug.WriteLine (
                             $"WARNING: InvokeKeyBindings ({key}) - An Application scope binding exists for this key. The registered view will not invoke Command.");//{commandBinding.Commands [0]}: {boundView}.");
        }

        // TODO: This is a "prototype" debug check. It may be too annoying vs. useful.
        // Scour the bindings up our View hierarchy
        // to ensure that the key is not already bound to a different set of commands.
        if (SuperView?.IsHotKeyKeyBound (key, out View previouslyBoundView) ?? false)
        {
            Debug.WriteLine ($"WARNING: InvokeKeyBindings ({key}) - A subview or peer has bound this Key and will not see it: {previouslyBoundView}.");
        }

#endif


        foreach (Command command in binding.Commands)
        {
            if (!CommandImplementations.ContainsKey (command))
            {
                throw new NotSupportedException (
                                                 @$"A KeyBinding was set up for the command {command} ({key}) but that command is not supported by this View ({GetType ().Name})"
                                                );
            }

            // each command has its own return value
            bool? thisReturn = InvokeCommand (command, key, binding);

            // if we haven't got anything yet, the current command result should be used
            toReturn ??= thisReturn;

            // if ever see a true then that's what we will return
            if (thisReturn ?? false)
            {
                toReturn = true;
            }
        }

        return toReturn;
    }

    /// <summary>
    ///     Invokes the specified commands.
    /// </summary>
    /// <param name="commands"></param>
    /// <param name="key">The key that caused the commands to be invoked, if any.</param>
    /// <param name="keyBinding"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was found.
    ///     <see langword="true"/> if the command was invoked the command was handled.
    ///     <see langword="false"/> if the command was invoked and the command was not handled.
    /// </returns>
    public bool? InvokeCommands (Command [] commands, [CanBeNull] Key key = null, [CanBeNull] KeyBinding? keyBinding = null)
    {
        bool? toReturn = null;

        foreach (Command command in commands)
        {
            if (!CommandImplementations.ContainsKey (command))
            {
                throw new NotSupportedException (@$"{command} is not supported by ({GetType ().Name}).");
            }

            // each command has its own return value
            bool? thisReturn = InvokeCommand (command, key, keyBinding);

            // if we haven't got anything yet, the current command result should be used
            toReturn ??= thisReturn;

            // if ever see a true then that's what we will return
            if (thisReturn ?? false)
            {
                toReturn = true;
            }
        }

        return toReturn;
    }

    /// <summary>Invokes the specified command.</summary>
    /// <param name="command">The command to invoke.</param>
    /// <param name="key">The key that caused the command to be invoked, if any.</param>
    /// <param name="keyBinding"></param>
    /// <returns>
    ///     <see langword="null"/> if no command was found. <see langword="true"/> if the command was invoked, and it
    ///     handled the command. <see langword="false"/> if the command was invoked, and it did not handle the command.
    /// </returns>
    public bool? InvokeCommand (Command command, [CanBeNull] Key key = null, [CanBeNull] KeyBinding? keyBinding = null)
    {
        if (CommandImplementations.TryGetValue (command, out Func<CommandContext, bool?> implementation))
        {
            var context = new CommandContext (command, key, keyBinding); // Create the context here
            return implementation (context);
        }

        return null;
    }

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. Views should call
    ///        AddCommand for each command they support.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="f"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This version of AddCommand is for commands that require <see cref="CommandContext"/>. Use <see cref="AddCommand(Command,Func{System.Nullable{bool}})"/>
    ///     in cases where the command does not require a <see cref="CommandContext"/>.
    /// </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    protected void AddCommand (Command command, Func<CommandContext, bool?> f)
    {
        CommandImplementations [command] = f;
    }

    /// <summary>
    ///     <para>
    ///         Sets the function that will be invoked for a <see cref="Command"/>. Views should call
    ///        AddCommand for each command they support.
    ///     </para>
    ///     <para>
    ///         If AddCommand has already been called for <paramref name="command"/> <paramref name="f"/> will
    ///         replace the old one.
    ///     </para>
    /// </summary>
    /// <remarks>
    /// <para>
    ///     This version of AddCommand is for commands that do not require a <see cref="CommandContext"/>.
    ///     If the command requires context, use <see cref="AddCommand(Command,Func{CommandContext,System.Nullable{bool}})"/>
    /// </para>
    /// </remarks>
    /// <param name="command">The command.</param>
    /// <param name="f">The function.</param>
    protected void AddCommand (Command command, Func<bool?> f)
    {
        CommandImplementations [command] = ctx => f ();
    }

    /// <summary>Returns all commands that are supported by this <see cref="View"/>.</summary>
    /// <returns></returns>
    public IEnumerable<Command> GetSupportedCommands () { return CommandImplementations.Keys; }

    // TODO: Add GetKeysBoundToCommand() - given a Command, return all Keys that would invoke it

    #endregion Key Bindings
}
