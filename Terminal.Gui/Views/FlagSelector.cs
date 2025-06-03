#nullable enable

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a user interface for displaying and selecting non-mutually-exclusive flags.
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

        CreateCheckBoxes ();
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

    private bool _assignHotKeysToCheckBoxes;

    /// <summary>
    ///     If <see langword="true"/> the CheckBoxes will each be automatically assigned a hotkey.
    ///     <see cref="UsedHotKeys"/> will be used to ensure unique keys are assigned. Set <see cref="UsedHotKeys"/>
    ///     before setting <see cref="Flags"/> with any hotkeys that may conflict with other Views.
    /// </summary>
    public bool AssignHotKeysToCheckBoxes
    {
        get => _assignHotKeysToCheckBoxes;
        set
        {
            if (_assignHotKeysToCheckBoxes == value)
            {
                return;
            }
            _assignHotKeysToCheckBoxes = value;
            CreateCheckBoxes ();
            UpdateChecked ();
        }
    }

    /// <summary>
    ///     Gets the list of hotkeys already used by the CheckBoxes or that should not be used if
    ///     <see cref="AssignHotKeysToCheckBoxes"/>
    ///     is enabled.
    /// </summary>
    public List<Key> UsedHotKeys { get; } = [];

    private void CreateCheckBoxes ()
    {
        if (Flags is null)
        {
            return;
        }

        foreach (CheckBox cb in RemoveAll<CheckBox> ())
        {
            cb.Dispose ();
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
        string nameWithHotKey = name;
        if (AssignHotKeysToCheckBoxes)
        {
            // Find the first char in label that is [a-z], [A-Z], or [0-9]
            for (var i = 0; i < name.Length; i++)
            {
                char c = char.ToLowerInvariant (name [i]);
                if (UsedHotKeys.Contains (new (c)) || !char.IsAsciiLetterOrDigit (c))
                {
                    continue;
                }

                if (char.IsAsciiLetterOrDigit (c))
                {
                    char? hotChar = c;
                    nameWithHotKey = name.Insert (i, HotKeySpecifier.ToString ());
                    UsedHotKeys.Add (new (hotChar));

                    break;
                }
            }
        }

        var checkbox = new CheckBox
        {
            CanFocus = true,
            Title = nameWithHotKey,
            Id = name,
            Data = flag,
            HighlightStates = ViewBase.MouseState.In
        };

        checkbox.GettingAttributeForRole += (_, e) =>
                                       {
                                           if (SuperView is { HasFocus: false })
                                           {
                                               return;
                                           }

                                           switch (e.Role)
                                           {
                                               case VisualRole.Normal:
                                                   e.Handled = true;

                                                   if (!HasFocus)
                                                   {
                                                       e.Result = GetAttributeForRole (VisualRole.Focus);
                                                   }
                                                   else
                                                   {
                                                       // If _scheme was set, it's because of Hover
                                                       if (checkbox.HasScheme)
                                                       {
                                                           e.Result = checkbox.GetAttributeForRole (VisualRole.Normal);
                                                       }
                                                       else
                                                       {
                                                           e.Result = GetAttributeForRole (VisualRole.Normal);
                                                       }
                                                   }

                                                   break;

                                               case VisualRole.HotNormal:
                                                   e.Handled = true;
                                                   if (!HasFocus)
                                                   {
                                                       e.Result = GetAttributeForRole (VisualRole.HotFocus);
                                                   }
                                                   else
                                                   {
                                                       e.Result = GetAttributeForRole (VisualRole.HotNormal);
                                                   }

                                                   break;
                                           }
                                       };

        //checkbox.GettingFocusColor += (_, e) =>
        //                                  {
        //                                      if (SuperView is { HasFocus: true })
        //                                      {
        //                                          e.Cancel = true;
        //                                          if (!HasFocus)
        //                                          {
        //                                              e.NewValue = GetAttributeForRole (VisualRole.Normal);
        //                                          }
        //                                          else
        //                                          {
        //                                              e.NewValue = GetAttributeForRole (VisualRole.Focus);
        //                                          }
        //                                      }
        //                                  };

        checkbox.Selecting += (sender, args) =>
                              {
                                  if (RaiseSelecting (args.Context) is true)
                                  {
                                      args.Handled = true;

                                      return;
                                  }
                                  ;

                                  if (RaiseAccepting (args.Context) is true)
                                  {
                                      args.Handled = true;
                                  }
                              };

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
                                               FlagSelectorStyles.ShowNone => "_Show None Value Style",
                                               FlagSelectorStyles.ShowValueEdit => "Show _Value Editor Style",
                                               FlagSelectorStyles.All => "_All Styles",
                                               _ => f.ToString ()
                                           });

        return true;
    }
}
