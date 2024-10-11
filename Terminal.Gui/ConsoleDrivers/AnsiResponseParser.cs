#nullable enable

namespace Terminal.Gui;
class AnsiResponseParser
{

    public bool ConsumeInput (char character, out string? released)
    {
        // if character is escape

        // start consuming till we see terminator

        released = null;
        return false;
    }

    public void ExpectResponse (char terminator, Action<string> response)
    {
    }
}
