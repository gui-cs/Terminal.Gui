#nullable enable
﻿namespace Terminal.Gui.Views;

/// <summary>Describes a single contiguous rectangular selection region within a <see cref="TableView"/>.</summary>
public class TableSelectionRegion : IEquatable<TableSelectionRegion>
{
    /// <summary>Creates a new selected area starting at the origin corner and covering the provided rectangular area.</summary>
    /// <param name="origin">The corner where the selection began.</param>
    /// <param name="rect">The rectangular area of the selection.</param>
    public TableSelectionRegion (Point origin, Rectangle rect)
    {
        Origin = origin;
        Rectangle = rect;
    }

    /// <summary>
    ///     <see langword="true"/> if the selection was made through <see cref="Command.ToggleExtend"/> (e.g. Ctrl+Click)
    ///     and therefore should persist even through keyboard navigation.
    /// </summary>
    public bool IsExtended { get; set; }

    /// <summary>Corner of the <see cref="Rectangle"/> where selection began.</summary>
    public Point Origin { get; set; }

    /// <summary>Area selected.</summary>
    public Rectangle Rectangle { get; set; }

    /// <inheritdoc/>
    public bool Equals (TableSelectionRegion? other)
    {
        if (other is null)
        {
            return false;
        }

        return Origin == other.Origin
               && Rectangle == other.Rectangle
               && IsExtended == other.IsExtended;
    }

    /// <inheritdoc/>
    public override bool Equals (object? obj) => Equals (obj as TableSelectionRegion);

    /// <inheritdoc/>
    public override int GetHashCode () => HashCode.Combine (Origin, Rectangle, IsExtended);
}

/// <summary>
///     Represents the complete selection state of a <see cref="TableView"/>: the cursor position and all
///     extended selection regions. Used as the <c>T</c> in <see cref="IValue{T}"/>.
/// </summary>
/// <remarks>
///     <para>A <see langword="null"/> <see cref="TableSelection"/> (as the value of <see cref="TableView.Value"/>) means
///     "no selection" — either no <see cref="TableView.Table"/> is assigned or the selection was explicitly cleared.</para>
///     <para>A non-null <see cref="TableSelection"/> always has a non-null <see cref="Cursor"/>.</para>
/// </remarks>
public class TableSelection : IEquatable<TableSelection>
{
    /// <summary>Creates a new <see cref="TableSelection"/> with the specified cursor and regions.</summary>
    /// <param name="cursor">The active cell position (navigation anchor). Must not be <see langword="null"/>.</param>
    /// <param name="regions">All extended selection regions (may be empty for cursor-only selection).</param>
    public TableSelection (Point cursor, IReadOnlyList<TableSelectionRegion> regions)
    {
        Cursor = cursor;
        Regions = regions ?? [];
    }

    /// <summary>Creates a cursor-only <see cref="TableSelection"/> with no extended regions.</summary>
    /// <param name="cursor">The active cell position.</param>
    public TableSelection (Point cursor) : this (cursor, []) { }

    /// <summary>The active cell used for navigation. Always non-null on a non-null <see cref="TableSelection"/>.</summary>
    public Point Cursor { get; }

    /// <summary>All extended selection regions. May be empty if only the cursor cell is selected.</summary>
    public IReadOnlyList<TableSelectionRegion> Regions { get; }

    /// <summary>Returns <see langword="true"/> if the given cell is within any of the <see cref="Regions"/>.</summary>
    public bool Contains (int col, int row)
    {
        for (var i = 0; i < Regions.Count; i++)
        {
            if (Regions [i].Rectangle.Contains (col, row))
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    public bool Equals (TableSelection? other)
    {
        if (other is null)
        {
            return false;
        }

        if (Cursor != other.Cursor)
        {
            return false;
        }

        if (Regions.Count != other.Regions.Count)
        {
            return false;
        }

        for (var i = 0; i < Regions.Count; i++)
        {
            if (!Regions [i].Equals (other.Regions [i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override bool Equals (object? obj) => Equals (obj as TableSelection);

    /// <inheritdoc/>
    public override int GetHashCode ()
    {
        HashCode hash = new ();
        hash.Add (Cursor);

        foreach (TableSelectionRegion region in Regions)
        {
            hash.Add (region);
        }

        return hash.ToHashCode ();
    }
}
