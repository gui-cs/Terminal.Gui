#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a user interface for displaying and selecting flags.
///     Flags can be set from a dictionary or directly from an enum type.
/// </summary>
public class FlagSelector : View, IOrientation, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagSelector"/> class.
    /// </summary>
    public FlagSelector ()
    {
        CanFocus = true;

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Vertical;

        // Accept (Enter key or DoubleClick) - Raise Accept event - DO NOT advance state
        AddCommand (Command.Accept, HandleAcceptCommand);

        CreateSubViews ();
    }

    private bool? HandleAcceptCommand (ICommandContext? ctx) { return RaiseAccepting (ctx); }

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

            CreateSubViews ();
        }
    }

    /// <summary>
    ///     Set the flags and flag names.
    /// </summary>
    /// <param name="flags"></param>
    public virtual void SetFlags (IReadOnlyDictionary<uint, string> flags)
    {
        Flags = flags;
        CreateSubViews ();
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
        // Convert enum names and values to a dictionary
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
    ///        var flagSelector = new FlagSelector ();
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
        // Convert enum values and custom names to a dictionary
        Dictionary<uint, string> flagsDictionary = Enum.GetValues<TEnum> ()
                                                       .ToDictionary (
                                                                      f => Convert.ToUInt32 (f),
                                                                      nameSelector
                                                                     );

        SetFlags (flagsDictionary);
    }

    /// <summary>
    ///     Gets the flag values and names.
    /// </summary>
    public IReadOnlyDictionary<uint, string>? Flags { get; internal set; }

    private TextField? ValueEdit { get; set; }

    private void CreateSubViews ()
    {
        if (Flags is null)
        {
            return;
        }

        foreach (CheckBox cb in RemoveAll<CheckBox> ())
        {
            cb.Dispose ();
        }

        if (Styles.HasFlag (FlagSelectorStyles.ShowNone) && !Flags.ContainsKey (default!))
        {
            Add (CreateCheckBox ("None", default!));
        }

        for (var index = 0; index < Flags.Count; index++)
        {
            if (!Styles.HasFlag (FlagSelectorStyles.ShowNone) && Flags.ElementAt (index).Key == default!)
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
                ReadOnly = true
            };

            Add (ValueEdit);
        }

        SetLayout ();

        return;


    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="flag"></param>
    /// <returns></returns>
    protected virtual CheckBox CreateCheckBox (string name, uint flag)
    {
        var checkbox = new CheckBox
        {
            CanFocus = false,
            Title = name,
            Id = name,
            Data = flag,
            HighlightStyle = HighlightStyle
        };

        checkbox.Selecting += (sender, args) => { RaiseSelecting (args.Context); };

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
    private void SetLayout ()
    {
        foreach (View sv in SubViews)
        {
            if (Orientation == Orientation.Vertical)
            {
                sv.X = 0;
                sv.Y = Pos.Align (Alignment.Start);
            }
            else
            {
                sv.X = Pos.Align (Alignment.Start);
                sv.Y = 0;
                sv.Margin!.Thickness = new (0, 0, 1, 0);
            }
        }
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

            // If this flag is set in Value, check the checkbox. Otherwise, uncheck it.
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


    #region IOrientation

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="RadioGroup"/>. The default is
    ///     <see cref="Orientation.Vertical"/>.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    private readonly OrientationHelper _orientationHelper;

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation) { SetLayout (); }

    #endregion IOrientation

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        Styles = FlagSelectorStyles.All;
        SetFlags<FlagSelectorStyles> (
                                      f => f switch
                                           {
                                               FlagSelectorStyles.None => "_No Style",
                                               FlagSelectorStyles.ShowNone => "Show _None Value Style",
                                               FlagSelectorStyles.ShowValueEdit => "Show _Value Editor Style",
                                               FlagSelectorStyles.All => "_All Styles",
                                               _ => f.ToString ()
                                           });

        return true;
    }
}
