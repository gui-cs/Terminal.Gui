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
        SetFlags ();
    }

    /// <summary>
    ///     Gets or sets the value of the selected flags.
    /// </summary>
    public new TFlagsEnum? Value
    {
        get => base.Value.HasValue ? (TFlagsEnum)Enum.ToObject (typeof (TFlagsEnum), base.Value.Value) : (TFlagsEnum?)null;
        set => base.Value = value.HasValue ? Convert.ToUInt32 (value.Value) : (uint?)null;
    }

    /// <summary>
    ///     Set the display names for the flags.
    /// </summary>
    /// <param name="nameSelector">A function that converts enum values to display names</param>
    /// <remarks>
    ///     This method allows changing the display names of the flags while keeping the flag values hard-defined by the enum type.
    /// </remarks>
    /// <example>
    ///     <code>
    ///        // Use enum values with custom display names
    ///        var flagSelector = new FlagSelector&lt;FlagSelectorStyles&gt;();
    ///        flagSelector.SetFlagNames(f => f switch {
    ///             FlagSelectorStyles.ShowNone => "Show None Value",
    ///             FlagSelectorStyles.ShowValueEdit => "Show Value Editor",
    ///             FlagSelectorStyles.All => "Everything",
    ///             _ => f.ToString()
    ///        });
    ///     </code>
    /// </example>
    public void SetFlagNames (Func<TFlagsEnum, string> nameSelector)
    {
        Dictionary<uint, string> flagsDictionary = Enum.GetValues<TFlagsEnum> ()
                                                       .ToDictionary (f => Convert.ToUInt32 (f), nameSelector);
        base.SetFlags (flagsDictionary);
    }

    private void SetFlags ()
    {
        Dictionary<uint, string> flagsDictionary = Enum.GetValues<TFlagsEnum> ()
                                                       .ToDictionary (f => Convert.ToUInt32 (f), f => f.ToString ());
        base.SetFlags (flagsDictionary);
    }

    /// <summary>
    ///     Prevents calling the base SetFlags method with arbitrary flag values.
    /// </summary>
    /// <param name="flags"></param>
    public override void SetFlags (IReadOnlyDictionary<uint, string> flags)
    {
        throw new InvalidOperationException ("Setting flag values directly is not allowed. Use SetFlagNames to change display names.");
    }

    /// <inheritdoc />
    protected override CheckBox CreateCheckBox (string name, uint flag)
    {
        CheckBox checkbox = base.CreateCheckBox (name, flag);
        checkbox.CheckedStateChanged += (sender, args) =>
                                        {
                                            TFlagsEnum? newValue = Value;

                                            if (checkbox.CheckedState == CheckState.Checked)
                                            {
                                                if (flag == default!)
                                                {
                                                    newValue = new TFlagsEnum ();
                                                }
                                                else
                                                {
                                                    newValue = (TFlagsEnum)Enum.ToObject (typeof (TFlagsEnum), Convert.ToUInt32 (newValue) | flag);
                                                }
                                            }
                                            else
                                            {
                                                newValue = (TFlagsEnum)Enum.ToObject (typeof (TFlagsEnum), Convert.ToUInt32 (newValue) & ~flag);
                                            }

                                            Value = newValue;
                                        };

        return checkbox;
    }
}
