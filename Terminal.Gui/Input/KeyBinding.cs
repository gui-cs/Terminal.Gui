#nullable enable

// These classes use a key binding system based on the design implemented in Scintilla.Net which is an
// MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs

namespace Terminal.Gui;

/// <summary>
/// Provides a collection of <see cref="Command"/> objects that are scoped to <see cref="KeyBindingScope"/>.
/// </summary>
public record struct KeyBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this key binding will invoke.</param>
    /// <param name="scope">The scope of the <see cref="Commands"/>.</param>
    /// <param name="context">Arbitrary context that can be associated with this key binding.</param>
    public KeyBinding (Command [] commands, KeyBindingScope scope, object? context = null)
    {
        Commands = commands;
        Scope = scope;
        Context = context;
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands">The commands this key binding will invoke.</param>
    /// <param name="scope">The scope of the <see cref="Commands"/>.</param>
    /// <param name="boundView">The view the key binding is bound to.</param>
    /// <param name="context">Arbitrary context that can be associated with this key binding.</param>
    public KeyBinding (Command [] commands, KeyBindingScope scope, View? boundView, object? context = null)
    {
        Commands = commands;
        Scope = scope;
        BoundView = boundView;
        Context = context;
    }

    /// <summary>The commands this key binding will invoke.</summary>
    public Command [] Commands { get; set; }

    /// <summary>The scope of the <see cref="Commands"/>.</summary>
    public KeyBindingScope Scope { get; set; }

    /// <summary>The view the key binding is bound to.</summary>
    public View? BoundView { get; set; }

    /// <summary>
    ///     Arbitrary context that can be associated with this key binding.
    /// </summary>
    public object? Context { get; set; }
}
