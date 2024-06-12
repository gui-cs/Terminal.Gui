namespace Terminal.Gui;

/// <summary><see cref="EventArgs"/> for <see cref="Slider{T}"/> events.</summary>
public class SliderEventArgs<T> : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="SliderEventArgs{T}"/></summary>
    /// <param name="options">The current options.</param>
    /// <param name="focused">Index of the option that is focused. -1 if no option has the focus.</param>
    public SliderEventArgs (Dictionary<int, SliderOption<T>> options, int focused = -1)
    {
        Options = options;
        Focused = focused;
        Cancel = false;
    }

    /// <summary>If set to true, the focus operation will be canceled, if applicable.</summary>
    public bool Cancel { get; set; }

    /// <summary>Gets or sets the index of the option that is focused.</summary>
    public int Focused { get; set; }

    /// <summary>Gets/sets whether the option is set or not.</summary>
    public Dictionary<int, SliderOption<T>> Options { get; set; }
}