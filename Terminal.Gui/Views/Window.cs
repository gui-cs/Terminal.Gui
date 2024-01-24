using System.Linq;
using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
/// A <see cref="Toplevel"/> <see cref="View"/> with <see cref="View.BorderStyle"/> set to
/// <see cref="LineStyle.Single"/>. Provides a container for other views. 
/// </summary>
/// <remarks>
/// <para>
/// If any subview is a button and the <see cref="Button.IsDefault"/> property is set to true, the Enter key
/// will invoke the <see cref="Command.Accept"/> command on that subview.
/// </para>
/// </remarks>
public class Window : Toplevel {
	/// <summary>
	/// Initializes a new instance of the <see cref="Window"/> class using
	/// <see cref="LayoutStyle.Computed"/> positioning.
	/// </summary>
	public Window () => SetInitialProperties ();

	/// <summary>
	/// Initializes a new instance of the <see cref="Window"/> class using
	/// <see cref="LayoutStyle.Computed"/> positioning.
	/// </summary>
	public Window (Rect frame) : base (frame) => SetInitialProperties ();

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
	/// The default <see cref="LineStyle"/> for <see cref="Window"/>'s border. The default is
	/// <see cref="LineStyle.Single"/>.
	/// </summary>
	/// <remarks>
	/// This property can be set in a Theme to change the default <see cref="LineStyle"/> for all
	/// <see cref="Window"/>s.
	/// </remarks>
	[SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
	[JsonConverter (typeof (JsonStringEnumConverter))]
	public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;

	void SetInitialProperties ()
	{
		CanFocus = true;
		ColorScheme = Colors.ColorSchemes ["Base"]; // TODO: make this a theme property
		BorderStyle = DefaultBorderStyle;

		// This enables the default button to be activated by the Enter key.
		AddCommand (Command.Accept, () => {
			// TODO: Perhaps all views should support the concept of being default?
			if (Subviews.FirstOrDefault (v => v is Button { IsDefault: true, Enabled: true }) is Button defaultBtn) {
				defaultBtn.InvokeCommand (Command.Accept);
				return true;
			}
			return false;
		});

		KeyBindings.Add (Key.Enter, Command.Accept);
	}
}