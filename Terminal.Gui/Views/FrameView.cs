namespace Terminal.Gui.Views;

// TODO: FrameView is mis-named, really. It's far more about it being a TabGroup than a frame. 

/// <summary>
///     A non-overlapped container for other views with a border and optional title.
/// </summary>
/// <remarks>
///     <para>
///         FrameView has <see cref="View.BorderStyle"/> set to <see cref="float"/> and
///         inherits it's scheme from the <see cref="View.SuperView"/>.
///     </para>
///     <para>
///         
///     </para>
/// </remarks>
/// <seealso cref="Window"/>
public class FrameView : View
{
    private static LineStyle _defaultBorderStyle = LineStyle.Rounded; // Resources/config.json overrides

    /// <summary>
    ///     Initializes a new instance of the <see cref="FrameView"/> class.
    ///     layout.
    /// </summary>
    public FrameView ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        BorderStyle = DefaultBorderStyle;
    }

    /// <summary>
    ///     Defines the default border styling for <see cref="FrameView"/>. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    /// <remarks>
    ///     This property can be set in a Theme to change the default <see cref="LineStyle"/> for all
    ///     <see cref="FrameView"/>s.
    /// </remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle
    {
        get => _defaultBorderStyle;
        set => _defaultBorderStyle = value;
    }
}
