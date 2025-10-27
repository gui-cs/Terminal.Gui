#nullable enable
namespace Terminal.Gui.Drivers;

#pragma warning disable CS1591
public interface IFakeConsoleDriver : IConsoleDriver, IConsoleDriverFacade
{
    void SetBufferSize (int width, int height);
    
    // SetScreenSize is already in IConsoleDriver, but we want to be explicit that IFakeConsoleDriver supports it
}
