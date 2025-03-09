#nullable enable
namespace Terminal.Gui;

/// <summary>
///     An overlapped container for other views with a border and optional title.
/// </summary>
/// <remarks>
///     <para>
///         Window has <see cref="View.BorderStyle"/> set to <see cref="LineStyle.Single"/>, <see cref="View.Arrangement"/>
///         set to <see cref="ViewArrangement.Overlapped"/>, and
///         uses the Base <see cref="Colors.ColorSchemes"/> color scheme by default.
///     </para>
///     <para>
///         To enable Window to be sized and moved by the user, adjust <see cref="View.Arrangement"/>.
///     </para>
/// </remarks>
/// <seealso cref="FrameView"/>
public class Window : Toplevel
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Window"/> class.
    /// </summary>
    public Window ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Arrangement = ViewArrangement.Overlapped;
        base.ColorScheme = Colors.ColorSchemes ["Base"]; // TODO: make this a theme property
        BorderStyle = DefaultBorderStyle;
        base.ShadowStyle = DefaultShadow;
    }

    /// <summary>
    ///     Gets or sets whether all <see cref="Window"/>s are shown with a shadow effect by default.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.None;

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
