namespace Terminal.Gui;

/// <summary>Args for events that relate to a specific <see cref="RuneCell"/>.</summary>
public class RuneCellEventArgs {
    /// <summary>Creates a new instance of the <see cref="RuneCellEventArgs"/> class.</summary>
    /// <param name="line">The line.</param>
    /// <param name="col">The col index.</param>
    /// <param name="unwrappedPosition">The unwrapped row and col index.</param>
    public RuneCellEventArgs (List<RuneCell> line, int col, (int Row, int Col) unwrappedPosition) {
        Line = line;
        Col = col;
        UnwrappedPosition = unwrappedPosition;
    }

    /// <summary>
    ///     The unwrapped row and column index into the text containing the RuneCell. Unwrapped means the text without word
    ///     wrapping or other visual formatting having been applied.
    /// </summary>
    public (int Row, int Col) UnwrappedPosition { get; }

    /// <summary>The index of the RuneCell in the line.</summary>
    public int Col { get; }

    /// <summary>The list of runes the RuneCell is part of.</summary>
    public List<RuneCell> Line { get; }
}
