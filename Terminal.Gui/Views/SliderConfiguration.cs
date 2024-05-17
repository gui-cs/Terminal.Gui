namespace Terminal.Gui;

/// <summary>All <see cref="Slider{T}"/> configuration are grouped in this class.</summary>
internal class SliderConfiguration
{
    internal bool _allowEmpty;
    internal int _endSpacing;
    internal int _minInnerSpacing = 1;
    internal int _cachedInnerSpacing; // Currently calculated
    internal Orientation _legendsOrientation = Orientation.Horizontal;
    internal bool _rangeAllowSingle;
    internal bool _showEndSpacing;
    internal bool _showLegends;
    internal bool _showLegendsAbbr;
    internal Orientation _sliderOrientation = Orientation.Horizontal;
    internal int _startSpacing;
    internal SliderType _type = SliderType.Single;
    internal bool _useMinimumSize;
}