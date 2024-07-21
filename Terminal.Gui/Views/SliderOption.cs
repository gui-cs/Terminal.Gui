namespace Terminal.Gui;

/// <summary>Represents an option in a <see cref="Slider{T}"/> .</summary>
/// <typeparam name="T">Data type of the option.</typeparam>
public class SliderOption<T>
{
    /// <summary>Creates a new empty instance of the <see cref="SliderOption{T}"/> class.</summary>
    public SliderOption () { }

    /// <summary>Creates a new instance of the <see cref="SliderOption{T}"/> class with values for each property.</summary>
    public SliderOption (string legend, Rune legendAbbr, T data)
    {
        Legend = legend;
        LegendAbbr = legendAbbr;
        Data = data;
    }

    /// <summary>Event fired when an option has changed.</summary>
    public event EventHandler<SliderOptionEventArgs> Changed;

    /// <summary>Custom data of the option.</summary>
    public T Data { get; set; }

    /// <summary>Legend of the option.</summary>
    public string Legend { get; set; }

    /// <summary>
    ///     Abbreviation of the Legend. When the <see cref="Slider{T}.MinimumInnerSpacing"/> too small to fit
    ///     <see cref="Legend"/>.
    /// </summary>
    public Rune LegendAbbr { get; set; }

    /// <summary>Event Raised when this option is set.</summary>
    public event EventHandler<SliderOptionEventArgs> Set;

    /// <summary>Creates a human-readable string that represents this <see cref="SliderOption{T}"/>.</summary>
    public override string ToString () { return "{Legend=" + Legend + ", LegendAbbr=" + LegendAbbr + ", Data=" + Data + "}"; }

    /// <summary>Event Raised when this option is unset.</summary>
    public event EventHandler<SliderOptionEventArgs> UnSet;

    /// <summary>To Raise the <see cref="Changed"/> event from the Slider.</summary>
    internal void OnChanged (bool isSet) { Changed?.Invoke (this, new (isSet)); }

    /// <summary>To Raise the <see cref="Set"/> event from the Slider.</summary>
    internal void OnSet () { Set?.Invoke (this, new (true)); }

    /// <summary>To Raise the <see cref="UnSet"/> event from the Slider.</summary>
    internal void OnUnSet () { UnSet?.Invoke (this, new (false)); }
}