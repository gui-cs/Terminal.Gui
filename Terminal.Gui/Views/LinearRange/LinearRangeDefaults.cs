namespace Terminal.Gui.Views;

/// <summary>
///     Theme-scoped defaults shared by all <see cref="LinearRangeViewBase{TOption,TValue}"/>
///     subclasses (<see cref="LinearSelector{T}"/>, <see cref="LinearMultiSelector{T}"/>,
///     <see cref="LinearRange{T}"/>).
/// </summary>
public static class LinearRangeDefaults
{
    /// <summary>Gets or sets the default cursor style applied to a new linear range view.</summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static CursorStyle DefaultCursorStyle
    {
        get => LinearRangeSettings.Defaults.DefaultCursorStyle;
        set => LinearRangeSettings.Defaults.DefaultCursorStyle = value;
    }
}
