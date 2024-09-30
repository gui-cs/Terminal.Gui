namespace Terminal.Gui;

/// <summary>
///     A <see cref="Toplevel"/> <see cref="View"/> with <see cref="View.BorderStyle"/> set to
///     <see cref="LineStyle.Single"/>. Provides a container for other views.
/// </summary>
/// <remarks>
///     <para>
///         If any subview is a button and the <see cref="Button.IsDefault"/> property is set to true, the Enter key will
///         invoke the <see cref="Command.Accept"/> command on that subview.
///     </para>
/// </remarks>
public class Window : Toplevel
{

    /// <summary>
    /// Gets or sets whether all <see cref="Window"/>s are shown with a shadow effect by default.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.None;


    /// <summary>
    ///     Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    public Window ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped | ViewArrangement.Resizable;
        ColorScheme = Colors.ColorSchemes ["Base"]; // TODO: make this a theme property
        BorderStyle = DefaultBorderStyle;
        ShadowStyle = DefaultShadow;
    }

    // TODO: enable this
    ///// <summary>
    ///// The default <see cref="LineStyle"/> for <see cref="Window"/>'s border. The default is <see cref="LineStyle.Single"/>.
    ///// </summary>
    ///// <remarks>
    ///// This property can be set in a Theme to change the default <see cref="LineStyle"/> for all <see cref="Window"/>s. 
    ///// </remarks>
    /////[SerializableConfigurationProperty (Scope = typeof (ThemeScope)), JsonConverter (typeof (JsonStringEnumConverter))]
    ////public static ColorScheme DefaultColorScheme { get; set; } = Colors.ColorSchemes ["Base"];

    /// <summary>
    ///     The default <see cref="LineStyle"/> for <see cref="Window"/>'s border. The default is
    ///     <see cref="LineStyle.Single"/>.
    /// </summary>
    /// <remarks>
    ///     This property can be set in a Theme to change the default <see cref="LineStyle"/> for all <see cref="Window"/>
    ///     s.
    /// </remarks>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;
}
