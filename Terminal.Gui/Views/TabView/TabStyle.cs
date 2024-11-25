namespace Terminal.Gui.Views;

/// <summary>Describes render stylistic selections of a <see cref="TabView"/></summary>
public class TabStyle
{
    /// <summary>True to show a solid box around the edge of the control.  Defaults to true.</summary>
    public bool ShowBorder { get; set; } = true;

    /// <summary>
    ///     True to show the top lip of tabs.  False to directly begin with tab text during rendering. Defaults to true.
    ///     When true and <see cref="TabSide.Top"/> or <see cref="TabSide.Bottom"/>, header
    ///     line occupies 3 rows, when false only 2.
    ///     <para>When <see cref="TabSide.Bottom"/> is enabled this instead applies to the bottommost line of the control</para>
    ///     When true and <see cref="TabSide.Left"/> or <see cref="TabSide.Right"/>, header
    ///     line occupies 1 more column, when false 1 column less.
    ///     <para>When <see cref="TabSide.Right"/> is enabled this instead applies to the rightmost column of the control</para>
    /// </summary>
    public bool ShowInitialLine { get; set; } = true;

    /// <summary>Gets or sets the tabs side to render.</summary>
    public TabSide TabsSide { get; set; }
}
