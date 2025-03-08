#nullable enable
namespace Terminal.Gui;

internal record AnsiResponseExpectation (string? Terminator, Action<IHeld> Response, Action? Abandoned)
{
    public bool Matches (string? cur) { return cur!.EndsWith (Terminator!); }
}
