#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a user interface for displaying and selecting flags.
///     Flags can be set from a dictionary or directly from an enum type.
/// </summary>
public class FlagSelector : SelectorBase<uint>, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagSelector"/> class.
    /// </summary>
    public FlagSelector ()
    {
    }



    private uint? _value;

    /// <summary>
    /// Gets or sets the value of the selected flags.
    /// </summary>
    public uint? Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;

            if (_value is null)
            {
                UncheckNone ();
                UncheckAll ();
            }
            else
            {
                UpdateChecked ();
            }

            if (ValueEdit is { })
            {
                ValueEdit.Text = _value.ToString ();
            }

            RaiseValueChanged ();
        }
    }

    private void RaiseValueChanged ()
    {
        OnValueChanged ();
        if (Value.HasValue)
        {
            ValueChanged?.Invoke (this, new EventArgs<uint> (Value.Value));
        }
    }

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    protected virtual void OnValueChanged () { }

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs<uint>>? ValueChanged;

    private FlagSelectorStyles _styles;

    /// <summary>
    /// Gets or sets the styles for the flag selector.
    /// </summary>
    public FlagSelectorStyles Styles
    {
        get => _styles;
        set
        {
            if (_styles == value)
            {
                return;
            }

            _styles = value;

            CreateCheckBoxes ();
        }
    }

    /// <summary>
    ///     Set the flags and flag names.
    /// </summary>
    /// <param name="flags"></param>
    public virtual void SetFlags (IReadOnlyDictionary<uint, string> flags)
    {
        Flags = flags;
        CreateCheckBoxes ();
        UpdateChecked ();
    }

    /// <summary>
    ///     Set the flags and flag names from an enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to extract flags from</typeparam>
    /// <remarks>
    ///     This is a convenience method that converts an enum to a dictionary of flag values and names.
    ///     The enum values are converted to uint values and the enum names become the display text.
    /// </remarks>
    public void SetFlags<TEnum> () where TEnum : struct, Enum
    {
        Dictionary<uint, string> flagsDictionary = Enum.GetValues<TEnum> ()
                                                       .ToDictionary (
                                                                      f => Convert.ToUInt32 (f),
                                                                      f => f.ToString ()
                                                                     );

        SetFlags (flagsDictionary);
    }

    /// <summary>
    ///     Set the flags and flag names from an enum type with custom display names.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to extract flags from</typeparam>
    /// <param name="nameSelector">A function that converts enum values to display names</param>
    /// <remarks>
    ///     This is a convenience method that converts an enum to a dictionary of flag values and custom names.
    ///     The enum values are converted to uint values and the display names are determined by the nameSelector function.
    /// </remarks>
    /// <example>
    ///     <code>
    ///        // Use enum values with custom display names
    ///        var flagSelector = new FlagSelector();
    ///        flagSelector.SetFlags&lt;FlagSelectorStyles&gt;
    ///             (f => f switch {
    ///             FlagSelectorStyles.ShowNone => "Show None Value",
    ///             FlagSelectorStyles.ShowValueEdit => "Show Value Editor",
    ///             FlagSelectorStyles.All => "Everything",
    ///             _ => f.ToString()
    ///             });
    ///     </code>
    /// </example>
    public void SetFlags<TEnum> (Func<TEnum, string> nameSelector) where TEnum : struct, Enum
    {
        Dictionary<uint, string> flagsDictionary = Enum.GetValues<TEnum> ()
                                                       .ToDictionary (
                                                                      f => Convert.ToUInt32 (f),
                                                                      nameSelector
                                                                     );

        SetFlags (flagsDictionary);
    }

    private IReadOnlyDictionary<uint, string>? _flags;

    /// <summary>
    ///     Gets the flag values and names.
    /// </summary>
    public IReadOnlyDictionary<uint, string>? Flags
    {
        get => _flags;
        internal set
        {
            _flags = value;

            if (_value is null)
            {
                Value = Convert.ToUInt16 (_flags?.Keys.ElementAt (0));
            }
        }
    }

    private TextField? ValueEdit { get; set; }

    /// <inheritdoc/>
    protected override void CreateCheckBoxes ()
    {
        if (Flags is null)
        {
            return;
        }

        foreach (View sv in RemoveAll<View> ())
        {
            sv.Dispose ();
        }

        if (Styles.HasFlag (FlagSelectorStyles.ShowNone) && !Flags.ContainsKey (0))
        {
            Add (CreateCheckBox ("None", 0));
        }

        for (var index = 0; index < Flags.Count; index++)
        {
            if (!Styles.HasFlag (FlagSelectorStyles.ShowNone) && Flags.ElementAt (index).Key == 0)
            {
                continue;
            }

            Add (CreateCheckBox (Flags.ElementAt (index).Value, Flags.ElementAt (index).Key));
        }

        if (Styles.HasFlag (FlagSelectorStyles.ShowValueEdit))
        {
            ValueEdit = new ()
            {
                Id = "valueEdit",
                CanFocus = false,
                Text = Value.ToString (),
                Width = 5,
                ReadOnly = true,
            };

            Add (ValueEdit);
        }

        SetLayout ();
        UpdateChecked ();
    }

    /// <inheritdoc/>
    protected override CheckBox CreateCheckBox (string name, uint flag)
    {
        var checkbox = base.CreateCheckBox (name, flag);

        checkbox.CheckedStateChanged += (sender, args) =>
        {
            uint? newValue = Value;

            if (checkbox.CheckedState == CheckState.Checked)
            {
                if (flag == default!)
                {
                    newValue = 0;
                }
                else
                {
                    newValue = newValue | flag;
                }
            }
            else
            {
                newValue = newValue & ~flag;
            }

            Value = newValue;
        };

        return checkbox;
    }

    private void UncheckAll ()
    {
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ().Where (sv => (uint)(sv.Data ?? default!) != default!))
        {
            cb.CheckedState = CheckState.UnChecked;
        }
    }

    private void UncheckNone ()
    {
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ().Where (sv => sv.Title != "None"))
        {
            cb.CheckedState = CheckState.UnChecked;
        }
    }

    private void UpdateChecked ()
    {
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            var flag = (uint)(cb.Data ?? throw new InvalidOperationException ("ComboBox.Data must be set"));

            if (flag == 0 && Value != 0)
            {
                cb.CheckedState = CheckState.UnChecked;
            }
            else
            {
                cb.CheckedState = (Value & flag) == flag ? CheckState.Checked : CheckState.UnChecked;
            }
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Styles = FlagSelectorStyles.All;
        SetFlags<FlagSelectorStyles> (
                                      f => f switch
                                      {
                                          FlagSelectorStyles.None => "_No Style",
                                          FlagSelectorStyles.ShowNone => "_Show None Value Style",
                                          FlagSelectorStyles.ShowValueEdit => "Show _Value Editor Style",
                                          FlagSelectorStyles.All => "_All Styles",
                                          _ => f.ToString ()
                                      });

        return true;
    }
}
