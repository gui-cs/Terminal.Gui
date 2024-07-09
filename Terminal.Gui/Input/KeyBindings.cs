﻿#nullable enable

using System.Diagnostics;

namespace Terminal.Gui;

/// <summary>
/// Provides a collection of <see cref="KeyBinding"/> objects bound to a <see cref="Key"/>.
/// </summary>
public class KeyBindings
{
    /// <summary>
    ///     Initializes a new instance. This constructor is used when the <see cref="KeyBindings"/> are not bound to a
    ///     <see cref="View"/>, such as in unit tests.
    /// </summary>
    public KeyBindings () { }

    /// <summary>Initializes a new instance bound to <paramref name="boundView"/>.</summary>
    public KeyBindings (View boundView) { BoundView = boundView; }

    /// <summary>
    ///     The view that the <see cref="KeyBindings"/> are bound to.
    /// </summary>
    public View? BoundView { get; }

    // TODO: Add a dictionary comparer that ignores Scope
    // TODO: This should not be public!
    /// <summary>The collection of <see cref="KeyBinding"/> objects.</summary>
    public Dictionary<Key, KeyBinding> Bindings { get; } = new ();

    /// <summary>Adds a <see cref="KeyBinding"/> to the collection.</summary>
    /// <param name="key"></param>
    /// <param name="binding"></param>
    public void Add (Key key, KeyBinding binding)
    {
        if (TryGet (key, out KeyBinding _))
        {
            Bindings [key] = binding;
        }
        else
        {
            Bindings.Add (key, binding);
            if (binding.Scope.HasFlag (KeyBindingScope.Application))
            {
                Application.AddKeyBinding (key, BoundView);
            }
        }
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
        if (key is null || !key.IsValid)
        {
            //throw new ArgumentException ("Invalid Key", nameof (commands));
            return;
        }

        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        if (TryGet (key, out KeyBinding _))
        {
            Bindings [key] = new (commands, scope);
        }
        else
        {
            Add (key, new KeyBinding (commands, scope));
        }
    }

    /// <summary>
    ///     <para>
    ///         Adds a new key combination that will trigger the commands in <paramref name="commands"/> (if supported by the
    ///         View - see <see cref="View.GetSupportedCommands"/>).
    ///     </para>
    ///     <para>
    ///         This is a helper function for <see cref="Add(Key,KeyBindingScope,Terminal.Gui.Command[])"/> for
    ///         <see cref="KeyBindingScope.Focused"/> scoped commands.
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
        Add (key, KeyBindingScope.Focused, commands);
    }

    /// <summary>Removes all <see cref="KeyBinding"/> objects from the collection.</summary>
    public void Clear ()
    {
        Application.ClearKeyBindings (BoundView);

        Bindings.Clear ();
    }

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

    /// <summary>Gets the Key used by a set of commands.</summary>
    /// <remarks></remarks>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>The <see cref="Key"/> used by a <see cref="Command"/></returns>
    /// <exception cref="InvalidOperationException">If no matching set of commands was found.</exception>
    public Key GetKeyFromCommands (params Command [] commands) { return Bindings.First (a => a.Value.Commands.SequenceEqual (commands)).Key; }

    /// <summary>Removes a <see cref="KeyBinding"/> from the collection.</summary>
    /// <param name="key"></param>
    public void Remove (Key key)
    {
        Bindings.Remove (key);
        Application.RemoveKeyBinding (key, BoundView);
    }

    /// <summary>Replaces a key combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldKey">The key to be replaced.</param>
    /// <param name="newKey">The new key to be used.</param>
    public void Replace (Key oldKey, Key newKey)
    {
        if (!TryGet (oldKey, out KeyBinding _))
        {
            return;
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
        if (key.IsValid)
        {
            return Bindings.TryGetValue (key, out binding);
        }

        binding = new (Array.Empty<Command> (), KeyBindingScope.Focused);

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
        if (key.IsValid && Bindings.TryGetValue (key, out binding))
        {
            if (scope.HasFlag (binding.Scope))
            {
                return true;
            }
        }

        binding = new (Array.Empty<Command> (), KeyBindingScope.Focused);

        return false;
    }
}
