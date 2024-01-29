using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///         The FrameView is a container frame that draws a frame around the contents. It is similar to
///         a GroupBox in Windows.
/// </summary>
public class FrameView : View {
	/// <summary>
	///         Initializes a new instance of the <see cref="Gui.FrameView" /> class using <see cref="LayoutStyle.Computed" />
	///         layout.
	/// </summary>
	public FrameView ()
	{
		Border.Thickness = new Thickness (1);
		Border.LineStyle = DefaultBorderStyle;
		//Border.ColorScheme = ColorScheme;
		Border.Data = "Border";
	}

	/// <summary>
	///         The default <see cref="LineStyle" /> for <see cref="FrameView" />'s border. The default is
	///         <see cref="LineStyle.Single" />.
	/// </summary>
	/// <remarks>
	///         This property can be set in a Theme to change the default <see cref="LineStyle" /> for all
	///         <see cref="FrameView" />s.
	/// </remarks>
	[SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
	[JsonConverter (typeof (JsonStringEnumConverter))]
	public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;

	/// <inheritdoc />
	public override bool OnEnter (View view)
	{
		if (Subviews.Count == 0 || !Subviews.Any (subview => subview.CanFocus)) {
			Application.Driver?.SetCursorVisibility (CursorVisibility.Invisible);
		}

		return base.OnEnter (view);
	}
}