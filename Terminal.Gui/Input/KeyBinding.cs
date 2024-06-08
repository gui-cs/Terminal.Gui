#nullable enable
// These classes use a key binding system based on the design implemented in Scintilla.Net which is an
// MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs

namespace Terminal.Gui;

/// <summary>Provides a collection of <see cref="Command"/> objects that are scoped to <see cref="KeyBindingScope"/>.</summary>
public record struct KeyBinding
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="commands"></param>
    /// <param name="scope"></param>
    public KeyBinding (Command [] commands, KeyBindingScope scope)
    {
        Commands = commands;
        Scope = scope;
    }

    /// <summary>The actions which can be performed by the application or bound to keys in a <see cref="View"/> control.</summary>
    public Command [] Commands { get; set; }

    /// <summary>The scope of the <see cref="Commands"/> bound to a key.</summary>
    public KeyBindingScope Scope { get; set; }
}