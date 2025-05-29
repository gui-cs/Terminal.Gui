#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

/// <summary>Event args for draw events</summary>
public class SchemeEventArgs : CancelEventArgs
{
    /// <summary>Creates a new instance of the <see cref="SchemeEventArgs"/> class.</summary>
    public SchemeEventArgs (in Scheme? currentScheme, ref Scheme? newScheme)
    {
        CurrentScheme = currentScheme;
        NewScheme = newScheme;
    }

    /// <summary>Gets the View's current <see cref="Scheme"/>.</summary>
    public Scheme? CurrentScheme { get; }

    /// <summary>Gets or sets the View's new <see cref="Scheme"/>.</summary>
    public Scheme? NewScheme { get; set;  }
}
