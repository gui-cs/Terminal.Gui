#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

public partial class View
{
    /// <summary>Selects the specified attribute as the attribute to use for future calls to AddRune and AddString.</summary>
    /// <remarks></remarks>
    /// <param name="attribute">THe Attribute to set.</param>
    public Attribute SetAttribute (Attribute attribute)
    {
        return Driver?.SetAttribute (attribute) ?? Attribute.Default;
    }

    /// <summary>Gets the current <see cref="Attribute"/>.</summary>
    /// <returns>The current attribute.</returns>
    public Attribute GetAttribute () { return Driver?.GetAttribute () ?? Attribute.Default; }


}
