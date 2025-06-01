#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.Text;

/// <summary>Event args for <see langword="string"/> type property events</summary>
public class StringPropertyEventArgs : CancelEventArgs
{
    /// <summary>Creates a new instance of the <see cref="StringPropertyEventArgs"/> class.</summary>
    public StringPropertyEventArgs (in string? currentString, ref string? newString)
    {
        CurrentString = currentString;
        NewString = newString;
    }

    /// <summary>Gets the current <see cref="String"/>.</summary>
    public string? CurrentString { get; }

    /// <summary>Gets or sets the new <see cref="String"/>.</summary>
    public string? NewString { get; set;  }
}
