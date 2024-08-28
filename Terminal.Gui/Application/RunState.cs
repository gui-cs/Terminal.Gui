#nullable enable

namespace Terminal.Gui;

using System.Numerics;

/// <summary>The execution state for a <see cref="Toplevel"/> view.</summary>
public sealed record RunState (Toplevel Toplevel) : IEqualityOperators<RunState, RunState, bool>
{
    /// <summary>The <see cref="Toplevel"/> belonging to this <see cref="RunState"/>.</summary>
    public Toplevel? Toplevel { get; internal set; } = Toplevel;
}
