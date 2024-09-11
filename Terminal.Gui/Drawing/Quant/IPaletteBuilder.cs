namespace Terminal.Gui.Drawing.Quant;

public interface IPaletteBuilder
{
    List<Color> BuildPalette (List<Color> colors, int maxColors);
}
