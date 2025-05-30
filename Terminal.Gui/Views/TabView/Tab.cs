#nullable enable

namespace Terminal.Gui.Views;

/// <summary>A single tab in a <see cref="TabView"/>.</summary>
public class Tab : View
{
    private string? _displayText;

    /// <summary>Creates a new unnamed tab with no controls inside.</summary>
    public Tab ()
    {
        BorderStyle = LineStyle.Rounded;
        CanFocus = true;
        TabStop = TabBehavior.NoStop;
    }

    /// <summary>The text to display in a <see cref="TabView"/>.</summary>
    /// <value></value>
    public string DisplayText
    {
        get => _displayText ?? "Unnamed";
        set
        {
            _displayText = value;
            SetNeedsLayout ();
        }
    }

    /// <summary>The control to display when the tab is selected.</summary>
    /// <value></value>
    public View? View { get; set; }
}
