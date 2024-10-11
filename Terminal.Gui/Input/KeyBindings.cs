﻿#nullable enable

using static System.Formats.Asn1.AsnWriter;

namespace Terminal.Gui;

/// <summary>
///     Provides a collection of <see cref="KeyBinding"/> objects bound to a <see cref="Key"/>.
/// </summary>
/// <seealso cref="Application.KeyBindings"/>
/// <seealso cref="View.KeyBindings"/>
/// <seealso cref="Command"/>
public class KeyBindings
{
    /// <summary>
    ///     Initializes a new instance. This constructor is used when the <see cref="KeyBindings"/> are not bound to a
    ///     <see cref="View"/>. This is used for Application.KeyBindings and unit tests.
    /// </summary>
    public KeyBindings () { }

    /// <summary>Initializes a new instance bound to <paramref name="boundView"/>.</summary>
    public KeyBindings (View? boundView) { BoundView = boundView; }

    /// <summary>Adds a <see cref="KeyBinding"/> to the collection.</summary>
    /// <param name="key"></param>
    /// <param name="binding"></param>
    /// <param name="boundViewForAppScope">Optional View for <see cref="KeyBindingScope.Application"/> bindings.</param>
    public void Add (Key key, KeyBinding binding, View? boundViewForAppScope = null)
    {
        if (BoundView is { } && binding.Scope.FastHasFlags (KeyBindingScope.Application))
        {
            throw new InvalidOperationException ("Application scoped KeyBindings must be added via Application.KeyBindings.Add");
        }

        if (BoundView is { } && boundViewForAppScope is null)
        {
            boundViewForAppScope = BoundView;
        }

        if (TryGet (key, binding.Scope, boundViewForAppScope, out KeyBinding _))
        {
            throw new InvalidOperationException (@$"A key binding for {key} exists ({binding}).");

            //Bindings [key] = binding;
        }

        binding.BoundView = boundViewForAppScope;

        Bindings.Add (key, binding);
    }

    /// <summary>
    ///     <para>Adds a new key combination that will trigger the commands in <paramref name="commands"/>.</para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Commands are only ever applied to the current <see cref="View"/> (i.e. this feature cannot be used to switch
    ///     focus to another view and perform multiple commands there).
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="scope">The scope for the command.</param>
    /// <param name="boundViewForAppScope">Optional View for <see cref="KeyBindingScope.Application"/> bindings.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (Key key, KeyBindingScope scope, View? boundViewForAppScope = null, params Command [] commands)
    {
        if (BoundView is { } && scope.FastHasFlags (KeyBindingScope.Application))
        {
            throw new ArgumentException ("Application scoped KeyBindings must be added via Application.KeyBindings.Add");
        }
        else
        {
            // boundViewForAppScope = BoundView;
        }

        if (key is null || !key.IsValid)
        {
            //throw new ArgumentException ("Invalid Key", nameof (commands));
            return;
        }

        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        if (TryGet (key, scope, boundViewForAppScope, out KeyBinding binding))
        {
            throw new InvalidOperationException (@$"A key binding for {key} exists ({binding}).");
        }

        Add (key, new KeyBinding (commands, scope, boundViewForAppScope), boundViewForAppScope);
    }

    /// <summary>
    ///     <para>Adds a new key combination that will trigger the commands in <paramref name="commands"/>.</para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Commands are only ever applied to the current <see cref="View"/> (i.e. this feature cannot be used to switch
    ///     focus to another view and perform multiple commands there).
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="scope">The scope for the command.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (Key key, KeyBindingScope scope, params Command [] commands)
    {
        if (BoundView is null && !scope.FastHasFlags (KeyBindingScope.Application))
        {
            throw new InvalidOperationException ("BoundView cannot be null.");
        }

        if (BoundView is { } && scope.FastHasFlags (KeyBindingScope.Application))
        {
            throw new InvalidOperationException ("Application scoped KeyBindings must be added via Application.KeyBindings.Add");
        }

        if (key == Key.Empty || !key.IsValid)
        {
            throw new ArgumentException (@"Invalid Key", nameof (commands));
        }

        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        // if BoundView is null, the right thing will happen
        if (TryGet (key, scope, BoundView, out KeyBinding binding))
        {
            throw new InvalidOperationException (@$"A key binding for {key} exists ({binding}).");
        }

        Add (key, new KeyBinding (commands, scope, BoundView), BoundView);
    }

    /// <summary>
    ///     <para>
    ///         Adds a new key combination that will trigger the commands in <paramref name="commands"/> (if supported by the
    ///         View - see <see cref="View.GetSupportedCommands"/>).
    ///     </para>
    ///     <para>
    ///         This is a helper function for <see cref="Add(Key,KeyBinding,View?)"/>. If used for a View (
    ///         <see cref="BoundView"/> is set), the scope will be set to <see cref="KeyBindingScope.Focused"/>.
    ///         Otherwise, it will be set to <see cref="KeyBindingScope.Application"/>.
    ///     </para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Commands are only ever applied to the current <see cref="View"/> (i.e. this feature cannot be used to switch
    ///     focus to another view and perform multiple commands there).
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="boundViewForAppScope">Optional View for <see cref="KeyBindingScope.Application"/> bindings.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (Key key, View? boundViewForAppScope = null, params Command [] commands)
    {
        if (BoundView is null && boundViewForAppScope is null)
        {
            throw new ArgumentException (@"Application scoped KeyBindings must provide a bound view to Add.", nameof (boundViewForAppScope));
        }

        Add (key, BoundView is { } ? KeyBindingScope.Focused : KeyBindingScope.Application, boundViewForAppScope, commands);
    }

    /// <summary>
    ///     <para>
    ///         Adds a new key combination that will trigger the commands in <paramref name="commands"/> (if supported by the
    ///         View - see <see cref="View.GetSupportedCommands"/>).
    ///     </para>
    ///     <para>
    ///         This is a helper function for <see cref="Add(Key,KeyBinding,View?)"/>. If used for a View (
    ///         <see cref="BoundView"/> is set), the scope will be set to <see cref="KeyBindingScope.Focused"/>.
    ///         Otherwise, it will be set to <see cref="KeyBindingScope.Application"/>.
    ///     </para>
    ///     <para>
    ///         If the key is already bound to a different array of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     Commands are only ever applied to the current <see cref="View"/> (i.e. this feature cannot be used to switch
    ///     focus to another view and perform multiple commands there).
    /// </remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="key"/> is pressed. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="key"/> strike will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (Key key, params Command [] commands)
    {
        if (BoundView is null)
        {
            throw new ArgumentException (@"Application scoped KeyBindings must provide a boundViewForAppScope to Add.");
        }

        Add (key, BoundView is { } ? KeyBindingScope.Focused : KeyBindingScope.Application, null, commands);
    }

    // TODO: Add a dictionary comparer that ignores Scope
    // TODO: This should not be public!
    /// <summary>The collection of <see cref="KeyBinding"/> objects.</summary>
    public Dictionary<Key, KeyBinding> Bindings { get; } = new ();

    /// <summary>
    ///     Gets the keys that are bound.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Key> GetBoundKeys ()
    {
        return Bindings.Keys;
    }

    /// <summary>
    ///     The view that the <see cref="KeyBindings"/> are bound to.
    /// </summary>
    /// <remarks>
    ///     If <see langword="null"/> the KeyBindings object is being used for Application.KeyBindings.
    /// </remarks>
    internal View? BoundView { get; }

    /// <summary>Removes all <see cref="KeyBinding"/> objects from the collection.</summary>
    public void Clear () { Bindings.Clear (); }

    /// <summary>
    ///     Removes all key bindings that trigger the given command set. Views can have multiple different keys bound to
    ///     the same command sets and this method will clear all of them.
    /// </summary>
    /// <param name="command"></param>
    public void Clear (params Command [] command)
    {
        KeyValuePair<Key, KeyBinding> [] kvps = Bindings
                                                .Where (kvp => kvp.Value.Commands.SequenceEqual (command))
                                                .ToArray ();

        foreach (KeyValuePair<Key, KeyBinding> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <see cref="KeyBinding"/> for the specified <see cref="Key"/>.</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public KeyBinding Get (Key key)
    {
        if (TryGet (key, out KeyBinding binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"Key {key} is not bound.");
    }

    /// <summary>Gets the <see cref="KeyBinding"/> for the specified <see cref="Key"/>.</summary>
    /// <param name="key"></param>
    /// <param name="scope"></param>
    /// <returns></returns>
    public KeyBinding Get (Key key, KeyBindingScope scope)
    {
        if (TryGet (key, scope, out KeyBinding binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"Key {key}/{scope} is not bound.");
    }

    /// <summary>Gets the array of <see cref="Command"/>s bound to <paramref name="key"/> if it exists.</summary>
    /// <param name="key">The key to check.</param>
    /// <returns>
    ///     The array of <see cref="Command"/>s if <paramref name="key"/> is bound. An empty <see cref="Command"/> array
    ///     if not.
    /// </returns>
    public Command [] GetCommands (Key key)
    {
        if (TryGet (key, out KeyBinding bindings))
        {
            return bindings.Commands;
        }

        return Array.Empty<Command> ();
    }

    /// <summary>Gets the first Key bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The first <see cref="Key"/> bound to the set of commands specified by <paramref name="commands"/>. <see langword="null"/> if the set of caommands was not found.</returns>
    public Key? GetKeyFromCommands (params Command [] commands)
    {
        return Bindings.FirstOrDefault (a => a.Value.Commands.SequenceEqual (commands)).Key;
    }

    /// <summary>Gets Keys bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The <see cref="Key"/>s bound to the set of commands specified by <paramref name="commands"/>. An empty list if the set of caommands was not found.</returns>
    public IEnumerable<Key> GetKeysFromCommands (params Command [] commands)
    {
        return Bindings.Where (a => a.Value.Commands.SequenceEqual (commands)).Select (a => a.Key);
    }

    /// <summary>Removes a <see cref="KeyBinding"/> from the collection.</summary>
    /// <param name="key"></param>
    /// <param name="boundViewForAppScope">Optional View for <see cref="KeyBindingScope.Application"/> bindings.</param>
    public void Remove (Key key, View? boundViewForAppScope = null)
    {
        KeyBindingScope scope = KeyBindingScope.Disabled;
        if (boundViewForAppScope is null)
        {
            boundViewForAppScope = BoundView;
            scope = KeyBindingScope.HotKey | KeyBindingScope.Focused;
        }
        else
        {
            scope = KeyBindingScope.Application;
        }
        if (!TryGet (key, scope, boundViewForAppScope, out KeyBinding binding))
        {
            return;
        }

        if (boundViewForAppScope is { } && binding.BoundView == boundViewForAppScope)
        {
            Bindings.Remove (key);
        }
    }

    /// <summary>Replaces the commands already bound to a key.</summary>
    /// <remarks>
    ///     <para>
    ///         If the key is not already bound, it will be added.
    ///     </para>
    /// </remarks>
    /// <param name="key">The key bound to the command to be replaced.</param>
    /// <param name="commands">The set of commands to replace the old ones with.</param>
    public void ReplaceCommands (Key key, params Command [] commands)
    {
        if (TryGet (key, out KeyBinding binding))
        {
            binding.Commands = commands;
        }
        else
        {
            Add (key, commands);
        }
    }

    /// <summary>Replaces a key combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldKey">The key to be replaced.</param>
    /// <param name="newKey">The new key to be used. If <see cref="Key.Empty"/> no action will be taken.</param>
    public void ReplaceKey (Key oldKey, Key newKey)
    {
        if (!TryGet (oldKey, out KeyBinding _))
        {
            throw new InvalidOperationException ($"Key {oldKey} is not bound.");
        }

        if (!newKey.IsValid)
        {
            throw new InvalidOperationException ($"Key {newKey} is is not valid.");
        }

        KeyBinding value = Bindings [oldKey];
        Remove (oldKey);
        Add (newKey, value);
    }

    /// <summary>Gets the commands bound with the specified Key.</summary>
    /// <remarks></remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified Key, if the Key is
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the Key is bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (Key key, out KeyBinding binding)
    {
        //if (BoundView is null)
        //{
        //    throw new InvalidOperationException ("KeyBindings must be bound to a View to use this method.");
        //}

        binding = new (Array.Empty<Command> (), KeyBindingScope.Disabled, null);

        if (key.IsValid)
        {
            return Bindings.TryGetValue (key, out binding);
        }

        return false;
    }

    /// <summary>Gets the commands bound with the specified Key that are scoped to a particular scope.</summary>
    /// <remarks></remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="scope">the scope to filter on</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified Key, if the Key is
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the Key is bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (Key key, KeyBindingScope scope, out KeyBinding binding)
    {
        //if (BoundView is null)
        //{
        //    throw new InvalidOperationException ("KeyBindings must be bound to a View to use this method.");
        //}

        binding = new (Array.Empty<Command> (), KeyBindingScope.Disabled, null);

        if (key.IsValid && Bindings.TryGetValue (key, out binding))
        {
            if (scope.HasFlag (binding.Scope))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Gets the commands bound with the specified Key that are scoped to a particular scope and bound View.</summary>
    /// <remarks></remarks>
    /// <param name="key">The key to check.</param>
    /// <param name="scope">the scope to filter on</param>
    /// <param name="boundView">The view the binding is bound to</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the specified Key, if the Key is
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the Key is bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (Key key, KeyBindingScope scope, View? boundView, out KeyBinding binding)
    {
        binding = new (Array.Empty<Command> (), KeyBindingScope.Disabled, null);

        if (key.IsValid && Bindings.TryGetValue (key, out binding))
        {
            if (binding.BoundView == boundView && scope.HasFlag (binding.Scope))
            {
                return true;
            }
        }

        return false;
    }
}
