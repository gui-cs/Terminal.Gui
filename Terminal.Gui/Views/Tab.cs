namespace Terminal.Gui; 

/// <summary>
/// A single tab in a <see cref="TabView"/>.
/// </summary>
public class Tab : View {
	private string _displayText;

	/// <summary>
	/// The text to display in a <see cref="TabView"/>.
	/// </summary>
	/// <value></value>
	public string DisplayText { get => _displayText ?? "Unamed"; set => _displayText = value; }

	/// <summary>
	/// The control to display when the tab is selected.
	/// </summary>
	/// <value></value>
	public View View { get; set; }

	/// <summary>
	/// Creates a new unamed tab with no controls inside.
	/// </summary>
	public Tab ()
	{
		BorderStyle = LineStyle.Rounded;
		CanFocus = true;
	}
}
