
using Terminal.Gui.Drawing;

namespace Terminal.Gui;


/// <summary>
/// Describes a pair of <see cref="IFill"/> which cooperate in creating
/// <see cref="Attribute"/>.  One gives foreground color while other gives background.
/// </summary>
public class FillPair
{
    public FillPair (GradientFill fore, SolidFill back)
    {
        Foreground = fore;
        Background = back;
    }

    IFill Foreground { get; set; }
    IFill Background { get; set; }

    internal Attribute? GetAttribute (Point point)
    {
        return new Attribute (
            Foreground.GetColor (point),
            Background.GetColor (point)
            );
    }
}
