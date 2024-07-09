﻿using System.Text.Json.Serialization;

namespace Terminal.Gui;

partial class Application
{
    private static Key _alternateForwardKey = Key.Empty; // Defined in config.json

    /// <summary>Alternative key to navigate forwards through views. Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key AlternateForwardKey
    {
        get => _alternateForwardKey;
        set
        {
            if (_alternateForwardKey != value)
            {
                Key oldKey = _alternateForwardKey;
                _alternateForwardKey = value;
                OnAlternateForwardKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnAlternateForwardKeyChanged (KeyChangedEventArgs e)
    {
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnAlternateForwardKeyChanged (e);
        }
    }

    private static Key _alternateBackwardKey = Key.Empty; // Defined in config.json

    /// <summary>Alternative key to navigate backwards through views. Shift+Ctrl+Tab is the primary key.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key AlternateBackwardKey
    {
        get => _alternateBackwardKey;
        set
        {
            if (_alternateBackwardKey != value)
            {
                Key oldKey = _alternateBackwardKey;
                _alternateBackwardKey = value;
                OnAlternateBackwardKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnAlternateBackwardKeyChanged (KeyChangedEventArgs oldKey)
    {
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnAlternateBackwardKeyChanged (oldKey);
        }
    }

    private static Key _quitKey = Key.Empty; // Defined in config.json

    /// <summary>Gets or sets the key to quit the application.</summary>
    [SerializableConfigurationProperty (Scope = typeof (SettingsScope))]
    [JsonConverter (typeof (KeyJsonConverter))]
    public static Key QuitKey
    {
        get => _quitKey;
        set
        {
            if (_quitKey != value)
            {
                Key oldKey = _quitKey;
                _quitKey = value;
                OnQuitKeyChanged (new (oldKey, value));
            }
        }
    }

    private static void OnQuitKeyChanged (KeyChangedEventArgs e)
    {
        // Duplicate the list so if it changes during enumeration we're safe
        foreach (Toplevel top in _topLevels.ToArray ())
        {
            top.OnQuitKeyChanged (e);
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
    public static event EventHandler<Key> KeyDown;

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
        if (!_initialized)
        {
            return true;
        }

        KeyDown?.Invoke (null, keyEvent);

        if (keyEvent.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in _topLevels.ToList ())
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

        // Invoke any global (Application-scoped) KeyBindings.
        // The first view that handles the key will stop the loop.
        foreach (KeyValuePair<Key, List<View>> binding in _keyBindings.Where (b => b.Key == keyEvent.KeyCode))
        {
            foreach (View view in binding.Value)
            {
                if (view is {} && view.KeyBindings.TryGet (binding.Key, (KeyBindingScope)0xFFFF, out KeyBinding kb))
                {
                    //bool? handled = view.InvokeCommands (kb.Commands, binding.Key, kb);
                    bool? handled = view?.OnInvokingKeyBindings (keyEvent, kb.Scope);

                    if (handled != null && (bool)handled)
                    {
                        return true;
                    }

                }
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
    public static event EventHandler<Key> KeyUp;

    /// <summary>
    ///     Called by the <see cref="ConsoleDriver"/> when the user releases a key. Fires the <see cref="KeyUp"/> event
    ///     then calls <see cref="View.NewKeyUpEvent"/> on all top level views. Called after <see cref="OnKeyDown"/>.
    /// </summary>
    /// <remarks>Can be used to simulate key press events.</remarks>
    /// <param name="a"></param>
    /// <returns><see langword="true"/> if the key was handled.</returns>
    public static bool OnKeyUp (Key a)
    {
        if (!_initialized)
        {
            return true;
        }

        KeyUp?.Invoke (null, a);

        if (a.Handled)
        {
            return true;
        }

        foreach (Toplevel topLevel in _topLevels.ToList ())
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

    /// <summary>
    ///     The <see cref="KeyBindingScope.Application"/> key bindings.
    /// </summary>
    private static readonly Dictionary<Key, List<View>> _keyBindings = new ();

    /// <summary>
    /// Gets the list of <see cref="KeyBindingScope.Application"/> key bindings.
    /// </summary>
    public static Dictionary<Key, List<View>> GetKeyBindings () { return _keyBindings; }

    /// <summary>
    ///     Adds an  <see cref="KeyBindingScope.Application"/> scoped key binding.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to add Application key bindings.
    /// </remarks>
    /// <param name="key">The key being bound.</param>
    /// <param name="view">The view that is bound to the key.</param>
    internal static void AddKeyBinding (Key key, View view)
    {
        if (!_keyBindings.ContainsKey (key))
        {
            _keyBindings [key] = [];
        }

        _keyBindings [key].Add (view);
    }

    /// <summary>
    ///     Gets the list of Views that have <see cref="KeyBindingScope.Application"/> key bindings.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to add Application key bindings.
    /// </remarks>
    /// <returns>The list of Views that have Application-scoped key bindings.</returns>
    internal static List<View> GetViewsWithKeyBindings () { return _keyBindings.Values.SelectMany (v => v).ToList (); }

    /// <summary>
    ///     Gets the list of Views that have <see cref="KeyBindingScope.Application"/> key bindings for the specified key.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to add Application key bindings.
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="views">Outputs the list of views bound to <paramref name="key"/></param>
    /// <returns><see langword="True"/> if successful.</returns>
    internal static bool TryGetKeyBindings (Key key, out List<View> views) { return _keyBindings.TryGetValue (key, out views); }

    /// <summary>
    ///     Removes an <see cref="KeyBindingScope.Application"/> scoped key binding.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to remove Application key bindings.
    /// </remarks>
    /// <param name="key">The key that was bound.</param>
    /// <param name="view">The view that is bound to the key.</param>
    internal static void RemoveKeyBinding (Key key, View view)
    {
        if (_keyBindings.TryGetValue (key, out List<View> views))
        {
            views.Remove (view);

            if (views.Count == 0)
            {
                _keyBindings.Remove (key);
            }
        }
    }

    /// <summary>
    ///     Removes all <see cref="KeyBindingScope.Application"/> scoped key bindings for the specified view.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to remove Application key bindings.
    /// </remarks>
    /// <param name="view">The view that is bound to the key.</param>
    internal static void ClearKeyBindings (View view)
    {
        foreach (Key key in _keyBindings.Keys)
        {
            _keyBindings [key].Remove (view);
        }
    }

    /// <summary>
    ///     Removes all <see cref="KeyBindingScope.Application"/> scoped key bindings for the specified view.
    /// </summary>
    /// <remarks>
    ///     This is an internal method used by the <see cref="View"/> class to remove Application key bindings.
    /// </remarks>
    internal static void ClearKeyBindings () { _keyBindings.Clear (); }
}
