#nullable enable
namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591
public interface IFakeDriverV2 : IConsoleDriver, IConsoleDriverFacade
{
    void SetBufferSize (int width, int height);
}
