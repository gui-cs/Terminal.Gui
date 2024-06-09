namespace Terminal.Gui;

/// <summary><see cref="EventArgs"/> for <see cref="Slider{T}"/> <see cref="SliderOption{T}"/> events.</summary>
public class SliderOptionEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="SliderOptionEventArgs"/></summary>
    /// <param name="isSet"> indicates whether the option is set</param>
    public SliderOptionEventArgs (bool isSet) { IsSet = isSet; }

    /// <summary>Gets whether the option is set or not.</summary>
    public bool IsSet { get; }
}