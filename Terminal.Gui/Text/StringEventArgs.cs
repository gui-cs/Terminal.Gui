﻿using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>Cancellable event args for string-based property change events.</summary>
public class StringEventArgs : CancelEventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="StringEventArgs"/>
    /// </summary>
    public StringEventArgs () {}

    /// <summary>Initializes a new instance of <see cref="StringEventArgs"/></summary>
    /// <param name="oldString">The old string.</param>
    /// <param name="newString">The new string.</param>
    public StringEventArgs (string oldString, string newString)
    {
        OldValue = oldString;
        NewValue = newString;
    }
    /// <summary>The new string.</summary>
    public string NewValue { get; set; }

    /// <summary>The old string.</summary>
    public string OldValue { get; set; }
}
