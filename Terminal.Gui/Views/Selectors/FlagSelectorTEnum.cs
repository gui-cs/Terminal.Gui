#nullable enable
namespace Terminal.Gui.Views;

/// <summary>
///     Provides a user interface for displaying and selecting non-mutually-exclusive flags in a type-safe way.
///     <see cref="FlagSelector"/> provides a non-type-safe version. <see cref="TFlagsEnum"/> must be a valid enum type with
///     the '[Flags]' attribute.
/// </summary>
public sealed class FlagSelector<TFlagsEnum> : FlagSelector where TFlagsEnum : struct, Enum
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagSelector{TFlagsEnum}"/> class.
    /// </summary>
    public FlagSelector ()
    {
        SetValuesAndLabels<TFlagsEnum> ();
    }

    /// <summary>
    ///     Gets or sets the value of the selected flags.
    /// </summary>
    public new TFlagsEnum? Value
    {
        get => base.Value.HasValue ? (TFlagsEnum)Enum.ToObject (typeof (TFlagsEnum), base.Value.Value) : (TFlagsEnum?)null;
        set => base.Value = value.HasValue ? Convert.ToInt32 (value.Value) : (int?)null;
    }
}
