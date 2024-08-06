using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     The FrameView is a container View with a border around it. 
/// </summary>
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
        Border.Thickness = new Thickness (1);
        Border.LineStyle = DefaultBorderStyle;

        //Border.ColorScheme = ColorScheme;
        Border.Data = "Border";
        MouseClick += FrameView_MouseClick;
    }

    private void FrameView_MouseClick (object sender, MouseEventEventArgs e)
    {
        e.Handled = InvokeCommand (Command.HotKey) == true;
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
    [JsonConverter (typeof (JsonStringEnumConverter<LineStyle>))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;
}
