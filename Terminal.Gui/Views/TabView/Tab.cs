namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="View"/> subclass that renders as a single tab within a <see cref="Tabs"/> container.
///     Each <see cref="Tab"/> uses <see cref="BorderSettings.Tab"/> to render a tab-style border around
///     its content, with <see cref="LineCanvas"/> auto-join producing connected tab visuals.
/// </summary>
/// <remarks>
///     <para>
///         Tabs are designed to be added as SubViews of a <see cref="Tabs"/> container. The container
///         manages <see cref="TabIndex"/>, <see cref="Border.TabOffset"/>, and
///         <see cref="Border.TabSide"/> automatically.
///     </para>
///     <para>
///         Each Tab's <see cref="View.Title"/> is displayed in the tab header. Use the <c>_</c>
///         hotkey specifier in the title to define a hotkey (e.g., <c>"_File"</c>).
///     </para>
/// </remarks>
public class Tab : View
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Tab"/> class.
    /// </summary>
    public Tab ()
    {
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        BorderStyle = LineStyle.Rounded;

        Border.Settings = BorderSettings.Tab | BorderSettings.Title;

        Border.Thickness = new Thickness (1, 3, 1, 1);

        // Overlapped enables z-order: focused tab renders above unselected tabs
        Arrangement = ViewArrangement.Overlapped;

        Width = Dim.Auto ();
        Height = Dim.Auto ();
    }

    /// <summary>
    ///     Gets the logical index of this tab within its <see cref="Tabs"/> container.
    ///     Updated by <see cref="Tabs"/> when tabs are added or removed.
    /// </summary>
    public int TabIndex { get; internal set; }
}
