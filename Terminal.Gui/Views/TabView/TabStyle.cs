namespace Terminal.Gui.Views;

/// <summary>Describes render stylistic selections of a <see cref="TabView"/></summary>
public class TabStyle
{
    /// <summary>True to show a solid box around the edge of the control.  Defaults to true.</summary>
    public bool ShowBorder { get; set; } = true;

    /// <summary>
    ///     True to show the top lip of tabs.  False to directly begin with tab text during rendering.  When true header
    ///     line occupies 3 rows, when false only 2. Defaults to true.
    ///     <para>When <see cref="TabsSide"/> is enabled this instead applies to the bottommost line of the control</para>
    /// </summary>
    public bool ShowTopLine { get; set; } = true;

    /// <summary>Gets or sets the tabs side to render.</summary>
    public TabSide TabsSide { get; set; }
}
