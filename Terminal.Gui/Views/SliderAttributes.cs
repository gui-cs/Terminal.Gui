namespace Terminal.Gui;

/// <summary><see cref="Slider{T}"/> Legend Style</summary>
public class SliderAttributes
{
    /// <summary>Attribute for the Legends Container.</summary>
    public Attribute? EmptyAttribute { get; set; }

    /// <summary>Attribute for when the respective Option is NOT Set.</summary>
    public Attribute? NormalAttribute { get; set; }

    /// <summary>Attribute for when the respective Option is Set.</summary>
    public Attribute? SetAttribute { get; set; }
}