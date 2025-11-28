namespace Terminal.Gui.Drivers;

internal interface IDriverInternal : IDriver
{
    /// <summary>
    ///     Gets or sets whether <see cref="IDriver"/> support for virtualized terminal sequences.
    /// </summary>
    public bool IsVirtualTerminal { get; set; }
}
