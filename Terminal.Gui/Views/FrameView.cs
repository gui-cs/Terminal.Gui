#nullable enable
namespace Terminal.Gui;

// TODO: FrameView is mis-named, really. It's far more about it being a TabGroup than a frame. 

/// <summary>
///     A non-overlapped container for other views with a border and optional title.
/// </summary>
/// <remarks>
///     <para>
///         FrameView has <see cref="View.BorderStyle"/> set to <see cref="LineStyle.Single"/> and
///         inherits it's color scheme from the <see cref="View.SuperView"/>.
///     </para>
///     <para>
///         
///     </para>
/// </remarks>
/// <seealso cref="Window"/>
public class FrameView : View
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Gui.FrameView"/> class.
    ///     layout.
    /// </summary>
    public FrameView ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        BorderStyle = DefaultBorderStyle;
    }

    /// <summary>
    ///     The default <see cref="LineStyle"/> for <see cref="FrameView"/>'s border. The default is
    ///     <see cref="LineStyle.Single"/>.
    /// </summary>
    /// <remarks>
    ///     This property can be set in a Theme to change the default <see cref="LineStyle"/> for all
    ///     <see cref="FrameView"/>s.
    /// </remarks>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;
}
