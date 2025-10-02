#nullable enable
namespace Terminal.Gui.Views;

/// <summary>
///     Provides a user interface for displaying and selecting non-mutually-exclusive flags.
///     Flags can be set from a dictionary or directly from an enum type.
/// </summary>
public sealed class FlagSelector<TEnum> : FlagSelector where TEnum : struct, Enum
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagSelector{TEnum}"/> class.
    /// </summary>
    public FlagSelector ()
    {
        SetFlags ();
    }

    /// <summary>
    /// Gets or sets the value of the selected flags.
    /// </summary>
    public new TEnum? Value
    {
        get => base.Value.HasValue ? (TEnum)Enum.ToObject (typeof (TEnum), base.Value.Value) : (TEnum?)null;
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
    public void SetFlagNames (Func<TEnum, string> nameSelector)
    {
        Dictionary<uint, string> flagsDictionary = Enum.GetValues<TEnum> ()
                                                       .ToDictionary (f => Convert.ToUInt32 (f), nameSelector);
        base.SetFlags (flagsDictionary);
    }

    private void SetFlags ()
    {
        Dictionary<uint, string> flagsDictionary = Enum.GetValues<TEnum> ()
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
        var checkbox = base.CreateCheckBox (name, flag);
        checkbox.CheckedStateChanged += (sender, args) =>
                                        {
                                            TEnum? newValue = Value;

                                            if (checkbox.CheckedState == CheckState.Checked)
                                            {
                                                if (flag == default!)
                                                {
                                                    newValue = new TEnum ();
                                                }
                                                else
                                                {
                                                    newValue = (TEnum)Enum.ToObject (typeof (TEnum), Convert.ToUInt32 (newValue) | flag);
                                                }
                                            }
                                            else
                                            {
                                                newValue = (TEnum)Enum.ToObject (typeof (TEnum), Convert.ToUInt32 (newValue) & ~flag);
                                            }

                                            Value = newValue;
                                        };

        return checkbox;
    }

}
