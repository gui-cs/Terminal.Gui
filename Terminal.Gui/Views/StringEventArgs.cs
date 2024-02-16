using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>Event args for string-based property change events.</summary>
public class StringEventArgs : CancelEventArgs
{
    /// <summary>The new text to be replaced.</summary>
    public string NewText { get; set; }

    /// <summary>The old text.</summary>
    public string OldText { get; set; }
}
