namespace Terminal.Gui;

public interface IPaletteBuilder
{
    List<Color> BuildPalette (List<Color> colors, int maxColors);
}
