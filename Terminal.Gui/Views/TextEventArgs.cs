//
// TextField.cs: single-line text editor with Emacs keybindings
//
// Authors:
//   Miguel de Icaza (miguel@gnome.org)
//

using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>An <see cref="EventArgs"/> which allows passing a cancelable new text value event.</summary>
public class TextEventArgs : CancelEventArgs
{
    /// <summary>Initializes a new instance of <see cref="TextEventArgs"/></summary>
    /// <param name="newText">The new <see cref="TextField.Text"/> to be replaced.</param>
    public TextEventArgs (string newText) { NewText = newText; }

    /// <summary>The new text to be replaced.</summary>
    public string NewText { get; set; }

    /// <summary>The old text.</summary>
    public string OldText { get; set; }
}
