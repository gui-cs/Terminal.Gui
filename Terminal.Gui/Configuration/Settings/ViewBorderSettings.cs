namespace Terminal.Gui.Configuration;

/// <summary>
///     Settings POCO for views that only have a DefaultBorderStyle ThemeScope property.
///     Covers: FrameView, HexView, Menu, MenuBar, SelectorBase, StatusBar, TextField, TextView, CharMap.
/// </summary>
public class ViewBorderSettings
{
    /// <summary>Gets or sets the default border style.</summary>
    public LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;

    /// <summary>
    ///     The static facade instance for <see cref="Views.FrameView"/>.
    /// </summary>
    public static ViewBorderSettings FrameViewDefaults { get; set; } = new ();

    /// <summary>
    ///     The static facade instance for <see cref="HexView"/>.
    /// </summary>
    public static ViewBorderSettings HexViewDefaults { get; set; } = new ();

    /// <summary>
    ///     The static facade instance for <see cref="Views.Menu"/>.
    /// </summary>
    public static ViewBorderSettings MenuDefaults { get; set; } = new ();

    /// <summary>
    ///     The static facade instance for <see cref="Views.MenuBar"/>.
    /// </summary>
    public static ViewBorderSettings MenuBarDefaults { get; set; } = new () { DefaultBorderStyle = LineStyle.None };

    /// <summary>
    ///     The static facade instance for <see cref="Views.SelectorBase"/>.
    /// </summary>
    public static ViewBorderSettings SelectorBaseDefaults { get; set; } = new ();

    /// <summary>
    ///     The static facade instance for <see cref="Views.StatusBar"/>.
    /// </summary>
    public static ViewBorderSettings StatusBarDefaults { get; set; } = new () { DefaultBorderStyle = LineStyle.None };

    /// <summary>
    ///     The static facade instance for <see cref="Views.TextField"/>.
    /// </summary>
    public static ViewBorderSettings TextFieldDefaults { get; set; } = new ();

    /// <summary>
    ///     The static facade instance for <see cref="Views.TextView"/>.
    /// </summary>
    public static ViewBorderSettings TextViewDefaults { get; set; } = new ();

    /// <summary>
    ///     The static facade instance for <see cref="Views.CharMap"/>.
    /// </summary>
    public static ViewBorderSettings CharMapDefaults { get; set; } = new ();
}
