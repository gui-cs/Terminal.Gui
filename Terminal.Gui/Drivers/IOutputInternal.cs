namespace Terminal.Gui.Drivers;

internal interface IOutputInternal : IOutput
{
    /// <summary>
    /// Get or sets the <see cref="IDriver"/> instance associated with this output.
    /// </summary>
    IDriver? Driver { get; set; }

    /// <summary>
    ///     Gets or sets whether <see cref="IDriver"/> support for virtualized terminal sequences.
    /// </summary>
    bool IsVirtualTerminal { get; init; }
}
