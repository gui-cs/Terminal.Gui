#nullable enable
namespace Terminal.Gui;

public interface IConsoleDriver
{
    IAnsiResponseParser GetParser ();
    void RawWrite (string str);
}
