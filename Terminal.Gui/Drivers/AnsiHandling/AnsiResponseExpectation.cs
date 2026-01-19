namespace Terminal.Gui.Drivers;

internal record AnsiResponseExpectation (string? Terminator, Action<IHeld> Response, Action? Abandoned)
{
    public bool Matches (string? cur) => cur!.EndsWith (Terminator!, StringComparison.Ordinal);
}
