#nullable enable
namespace Terminal.Gui.Input;

/// <summary>
///     Abstract class for <see cref="KeyBindings"/> and <see cref="MouseBindings"/>.
/// </summary>
/// <typeparam name="TEvent">The type of the event (e.g. <see cref="Key"/> or <see cref="MouseFlags"/>).</typeparam>
/// <typeparam name="TBinding">The binding type (e.g. <see cref="KeyBinding"/>).</typeparam>
public abstract class InputBindings<TEvent, TBinding> where TBinding : IInputBinding, new () where TEvent : notnull
{
    /// <summary>
    ///     The bindings.
    /// </summary>
    private readonly Dictionary<TEvent, TBinding> _bindings;

    private readonly Func<Command [], TEvent, TBinding> _constructBinding;

    /// <summary>
    ///     Initializes a new instance.
    /// </summary>
    /// <param name="constructBinding"></param>
    /// <param name="equalityComparer"></param>
    protected InputBindings (Func<Command [], TEvent, TBinding> constructBinding, IEqualityComparer<TEvent> equalityComparer)
    {
        _constructBinding = constructBinding;
        _bindings = new (equalityComparer);
    }

    /// <summary>
    ///     Tests whether <paramref name="eventArgs"/> is valid or not.
    /// </summary>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    public abstract bool IsValid (TEvent eventArgs);

    /// <summary>Adds a <typeparamref name="TEvent"/> bound to <typeparamref name="TBinding"/> to the collection.</summary>
    /// <param name="eventArgs"></param>
    /// <param name="binding"></param>
    public void Add (TEvent eventArgs, TBinding binding)
    {
        if (!IsValid (eventArgs))
        {
            throw new ArgumentException (@"Invalid newEventArgs", nameof (eventArgs));
        }

#pragma warning disable CS8601 // Possible null reference assignment.
        if (TryGet (eventArgs, out TBinding _))
        {
            throw new InvalidOperationException (@$"A binding for {eventArgs} exists ({binding}).");
        }
#pragma warning restore CS8601 // Possible null reference assignment.

        // IMPORTANT: Add a COPY of the eventArgs. This is needed because ConfigurationManager.Apply uses DeepMemberWiseCopy 
        // IMPORTANT: update the memory referenced by the key, and Dictionary uses caching for performance, and thus 
        // IMPORTANT: Apply will update the Dictionary with the new eventArgs, but the old eventArgs will still be in the dictionary.
        // IMPORTANT: See the ConfigurationManager.Illustrate_DeepMemberWiseCopy_Breaks_Dictionary test for details.
        _bindings.Add (eventArgs, binding);
    }

    /// <summary>
    ///     <para>Adds a new <typeparamref name="TEvent"/> that will trigger the commands in <paramref name="commands"/>.</para>
    ///     <para>
    ///         If the <typeparamref name="TEvent"/> is already bound to a different set of <see cref="Command"/>s it will be rebound
    ///         <paramref name="commands"/>.
    ///     </para>
    /// </summary>
    /// <param name="eventArgs">The event to check.</param>
    /// <param name="commands">
    ///     The command to invoked on the <see cref="View"/> when <paramref name="eventArgs"/> is received. When
    ///     multiple commands are provided,they will be applied in sequence. The bound <paramref name="eventArgs"/> event
    ///     will be
    ///     consumed if any took effect.
    /// </param>
    public void Add (TEvent eventArgs, params Command [] commands)
    {
        if (commands.Length == 0)
        {
            throw new ArgumentException (@"At least one command must be specified", nameof (commands));
        }

        if (TryGet (eventArgs, out TBinding? binding))
        {
            throw new InvalidOperationException (@$"A binding for {eventArgs} exists ({binding}).");
        }

        Add (eventArgs, _constructBinding (commands, eventArgs));
    }

    /// <summary>
    ///     Gets the bindings.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<KeyValuePair<TEvent, TBinding>> GetBindings () { return _bindings; }

    /// <summary>Removes all <typeparamref name="TEvent"/> objects from the collection.</summary>
    public void Clear () { _bindings.Clear (); }

    /// <summary>
    ///     Removes all bindings that trigger the given command set. Views can have multiple different <typeparamref name="TEvent"/>
    ///     bound to
    ///     the same command sets and this method will clear all of them.
    /// </summary>
    /// <param name="command"></param>
    public void Clear (params Command [] command)
    {
        KeyValuePair<TEvent, TBinding> [] kvps = _bindings
                                                 .Where (kvp => kvp.Value.Commands.SequenceEqual (command))
                                                 .ToArray ();

        foreach (KeyValuePair<TEvent, TBinding> kvp in kvps)
        {
            Remove (kvp.Key);
        }
    }

    /// <summary>Gets the <typeparamref name="TBinding"/> for the specified <typeparamref name="TEvent"/>.</summary>
    /// <param name="eventArgs"></param>
    /// <returns></returns>
    public TBinding? Get (TEvent eventArgs)
    {
        if (TryGet (eventArgs, out TBinding? binding))
        {
            return binding;
        }

        throw new InvalidOperationException ($"{eventArgs} is not bound.");
    }

    /// <summary>Gets the commands bound with the specified <typeparamref name="TEvent"/>.</summary>
    /// <remarks></remarks>
    /// <param name="eventArgs">The <typeparamref name="TEvent"/> to check.</param>
    /// <param name="binding">
    ///     When this method returns, contains the commands bound with the <typeparamref name="TEvent"/>, if the <typeparamref name="TEvent"/> is
    ///     not
    ///     found; otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns><see langword="true"/> if the <typeparamref name="TEvent"/> is bound; otherwise <see langword="false"/>.</returns>
    public bool TryGet (TEvent eventArgs, out TBinding? binding) { return _bindings.TryGetValue (eventArgs, out binding); }

    /// <summary>Gets the array of <see cref="Command"/>s bound to <paramref name="eventArgs"/> if it exists.</summary>
    /// <param name="eventArgs">The <typeparamref name="TEvent"/> to check.</param>
    /// <returns>
    ///     The array of <see cref="Command"/>s if <paramref name="eventArgs"/> is bound. An empty <see cref="Command"/> array
    ///     if not.
    /// </returns>
    public Command [] GetCommands (TEvent eventArgs)
    {
        if (TryGet (eventArgs, out TBinding? bindings))
        {
            return bindings!.Commands;
        }

        return [];
    }

    /// <summary>
    ///     Gets the first matching <typeparamref name="TEvent"/> bound to the set of commands specified by
    ///     <paramref name="commands"/>.
    /// </summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>
    ///     The first matching <typeparamref name="TEvent"/> bound to the set of commands specified by
    ///     <paramref name="commands"/>. <see langword="null"/> if the set of commands was not found.
    /// </returns>
    public TEvent? GetFirstFromCommands (params Command [] commands) { return _bindings.FirstOrDefault (a => a.Value.Commands.SequenceEqual (commands)).Key; }

    /// <summary>Gets all <typeparamref name="TEvent"/> bound to the set of commands specified by <paramref name="commands"/>.</summary>
    /// <param name="commands">The set of commands to search.</param>
    /// <returns>
    ///     The <typeparamref name="TEvent"/>s bound to the set of commands specified by <paramref name="commands"/>. An empty list if
    ///     the
    ///     set of commands was not found.
    /// </returns>
    public IEnumerable<TEvent> GetAllFromCommands (params Command [] commands)
    {
        return _bindings.Where (a => a.Value.Commands.SequenceEqual (commands)).Select (a => a.Key);
    }

    /// <summary>Replaces a <typeparamref name="TEvent"/> combination already bound to a set of <see cref="Command"/>s.</summary>
    /// <remarks></remarks>
    /// <param name="oldEventArgs">The <typeparamref name="TEvent"/> to be replaced.</param>
    /// <param name="newEventArgs">
    ///     The new <typeparamref name="TEvent"/> to be used.
    /// </param>
    public void Replace (TEvent oldEventArgs, TEvent newEventArgs)
    {
        if (!IsValid (newEventArgs))
        {
            throw new ArgumentException (@"Invalid newEventArgs", nameof (newEventArgs));
        }

        if (TryGet (oldEventArgs, out TBinding? binding))
        {
            Remove (oldEventArgs);
            Add (newEventArgs, binding!);
        }
        else
        {
            Add (newEventArgs, binding!);
        }
    }

    /// <summary>Replaces the commands already bound to a combination of <typeparamref name="TEvent"/>.</summary>
    /// <remarks>
    ///     <para>
    ///         If the of <typeparamref name="TEvent"/> is not already bound, it will be added.
    ///     </para>
    /// </remarks>
    /// <param name="eventArgs">The combination of <typeparamref name="TEvent"/> bound to the command to be replaced.</param>
    /// <param name="newCommands">The set of commands to replace the old ones with.</param>
    public void ReplaceCommands (TEvent eventArgs, params Command [] newCommands)
    {
#pragma warning disable CS8601 // Possible null reference assignment.
        if (TryGet (eventArgs, out TBinding _))
        {
            Remove (eventArgs);
            Add (eventArgs, newCommands);
        }
        else
        {
            Add (eventArgs, newCommands);
        }
#pragma warning restore CS8601 // Possible null reference assignment.
    }

    /// <summary>Removes a <typeparamref name="TEvent"/> from the collection.</summary>
    /// <param name="eventArgs"></param>
    public void Remove (TEvent eventArgs)
    {
        if (!TryGet (eventArgs, out _))
        {
            return;
        }

        _bindings.Remove (eventArgs);
    }
}
