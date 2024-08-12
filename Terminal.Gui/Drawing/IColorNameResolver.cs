namespace Terminal.Gui;

public interface IColorNameResolver
{
    IEnumerable<string> GetColorNames ();
    bool TryParseColor (string name, out Color color);
}
