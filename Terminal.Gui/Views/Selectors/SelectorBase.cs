#nullable enable
using System.Collections.Immutable;

namespace Terminal.Gui.Views;

/// <summary>
///     The abstract base class for <see cref="OptionSelector{TEnum}"/> and <see cref="FlagSelector{TFlagsEnum}"/>.
/// </summary>
public abstract class SelectorBase : View, IOrientation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SelectorBase"/> class.
    /// </summary>
    protected SelectorBase ()
    {
        CanFocus = true;

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Vertical;

        AddCommand (Command.Accept, HandleAcceptCommand);
        //AddCommand (Command.HotKey, HandleHotKeyCommand);

        //CreateSubViews ();
    }

    /// <inheritdoc />
    protected override bool OnClearingViewport ()
    {
        //SetAttributeForRole (HasFocus ? VisualRole.Focus : VisualRole.Normal);
        return base.OnClearingViewport ();
    }

    private SelectorStyles _styles;

    /// <summary>
    ///     Gets or sets the styles for the flag selector.
    /// </summary>
    public SelectorStyles Styles
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
            UpdateChecked ();
        }
    }

    private bool? HandleAcceptCommand (ICommandContext? ctx)
    {
        if (!DoubleClickAccepts
            && ctx is CommandContext<MouseBinding> mouseCommandContext
            && mouseCommandContext.Binding.MouseEventArgs!.Flags.HasFlag (MouseFlags.Button1DoubleClicked))
        {
            return false;
        }

        return RaiseAccepting (ctx);
    }

    /// <inheritdoc />
    protected override bool OnHandlingHotKey (CommandEventArgs args)
    {
        // If the command did not come from a keyboard event, ignore it
        if (args.Context is not CommandContext<KeyBinding> keyCommandContext)
        {
            return base.OnHandlingHotKey (args);
        }

        if ((HasFocus || !CanFocus) && HotKey == keyCommandContext.Binding.Key?.NoAlt.NoCtrl.NoShift!)
        {
            // It's this.HotKey OR Another View (Label?) forwarded the hotkey command to us - Act just like `Space` (Select)
            return Focused?.InvokeCommand (Command.Select, args.Context) is true;
        }
        return base.OnHandlingHotKey (args);
    }

    /// <inheritdoc />
    protected override bool OnSelecting (CommandEventArgs args)
    {
        return base.OnSelecting (args);
    }

    private int? _value;

    /// <summary>
    ///     Gets or sets the value of the selector. Will be <see langword="null"/> if no value is set.
    /// </summary>
    public virtual int? Value
    {
        get => _value;
        set
        {
            if (value is { } && Values is { } && !Values.Contains (value ?? -1))
            {
                throw new ArgumentOutOfRangeException (nameof (value), @$"Value must be one of the following: {string.Join (", ", Values)}");
            }

            if (_value == value)
            {
                return;
            }

            int? previousValue = _value;
            _value = value;

            UpdateChecked ();
            RaiseValueChanged (previousValue);
        }
    }


    /// <summary>
    ///     Raised the <see cref="ValueChanged"/> event.
    /// </summary>
    /// <param name="previousValue"></param>
    protected void RaiseValueChanged (int? previousValue)
    {
        if (_valueField is { })
        {
            _valueField.Text = Value.ToString ();
        }

        OnValueChanged (Value, previousValue);

        if (Value.HasValue)
        {
            ValueChanged?.Invoke (this, new (Value.Value));
        }
    }

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    protected virtual void OnValueChanged (int? value, int? previousValue) { }

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    public event EventHandler<EventArgs<int?>>? ValueChanged;

    private IReadOnlyList<int>? _values;

    /// <summary>
    ///     Gets or sets the option values. If <see cref="Values"/> is <see langword="null"/>, get will
    ///     return values based on the <see cref="Labels"/> property.
    /// </summary>
    public virtual IReadOnlyList<int>? Values
    {
        get
        {
            if (_values is { })
            {
                return _values;
            }

            // Use Labels and assume 0..Labels.Count - 1
            return Labels is { }
                       ? Enumerable.Range (0, Labels.Count).ToList ()
                       : null;
        }
        set
        {
            _values = value;

            // Ensure Value defaults to the first valid entry in Values if not already set
            if (Value is null && _values?.Any () == true)
            {
                Value = _values.First ();
            }

            CreateSubViews ();
            UpdateChecked ();
        }
    }

    private IReadOnlyList<string>? _labels;

    /// <summary>
    ///     Gets or sets the list of labels for each value in <see cref="Values"/>.
    /// </summary>
    public IReadOnlyList<string>? Labels
    {
        get => _labels;
        set
        {
            _labels = value;

            CreateSubViews ();
            UpdateChecked ();
        }
    }

    /// <summary>
    ///     Set <see cref="Values"/> and <see cref="Labels"/> from an enum type.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to extract from</typeparam>
    /// <remarks>
    ///     This is a convenience method that converts an enum to a dictionary of values and labels.
    ///     The enum values are converted to int values and the enum names become the labels.
    /// </remarks>
    public void SetValuesAndLabels<TEnum> () where TEnum : struct, Enum
    {
        IEnumerable<int> values = Enum.GetValues<TEnum> ().Select (f => Convert.ToInt32 (f));
        Values = values.ToImmutableList ().AsReadOnly ();
        Labels = Enum.GetNames<TEnum> ();
    }

    private bool _assignHotKeys;

    /// <summary>
    ///     If <see langword="true"/> each label will automatically be assigned a unique hotkey.
    ///     <see cref="UsedHotKeys"/> will be used to ensure unique keys are assigned. Set <see cref="UsedHotKeys"/>
    ///     before setting <see cref="Labels"/> with any hotkeys that may conflict with other Views.
    /// </summary>
    public bool AssignHotKeys
    {
        get => _assignHotKeys;
        set
        {
            if (_assignHotKeys == value)
            {
                return;
            }

            _assignHotKeys = value;

            CreateSubViews ();
            UpdateChecked ();
        }
    }

    /// <summary>
    ///     Gets or sets the set of hotkeys that are already used by labels or should not be used when
    ///     <see cref="AssignHotKeys"/> is enabled.
    ///     <para>
    ///         This property is used to ensure that automatically assigned hotkeys do not conflict with
    ///         hotkeys used elsewhere in the application. Set <see cref="UsedHotKeys"/> before setting
    ///         <see cref="Labels"/> if there are hotkeys that may conflict with other views.
    ///     </para>
    /// </summary>
    public HashSet<Key> UsedHotKeys { get; set; } = [];

    private TextField? _valueField;

    /// <summary>
    ///     Creates the subviews for this selector.
    /// </summary>
    public void CreateSubViews ()
    {
        foreach (View sv in RemoveAll ())
        {
            if (AssignHotKeys)
            {
                UsedHotKeys.Remove (sv.HotKey);
            }

            sv.Dispose ();
        }

        if (Labels is null)
        {
            return;
        }

        if (Labels?.Count != Values?.Count)
        {
            return;
        }

        OnCreatingSubViews ();

        for (var index = 0; index < Labels?.Count; index++)
        {
            Add (CreateCheckBox (Labels.ElementAt (index), Values!.ElementAt (index)));
        }

        if (Styles.HasFlag (SelectorStyles.ShowValue))
        {
            _valueField = new ()
            {
                Id = "valueField",
                Text = Value.ToString (),

                // TODO: Don't hardcode this; base it on max Value
                Width = 5,
                ReadOnly = true
            };

            Add (_valueField);
        }

        OnCreatedSubViews ();

        AssignUniqueHotKeys ();
        SetLayout ();
    }

    /// <summary>
    ///     Called before <see cref="CreateSubViews"/> creates the default subviews (Checkboxes and ValueField).
    /// </summary>
    protected virtual void OnCreatingSubViews () { }

    /// <summary>
    ///     Called after <see cref="CreateSubViews"/> creates the default subviews (Checkboxes and ValueField).
    /// </summary>
    protected virtual void OnCreatedSubViews () { }

    /// <summary>
    ///     INTERNAL: Creates a checkbox subview
    /// </summary>
    protected CheckBox CreateCheckBox (string label, int value)
    {
        var checkbox = new CheckBox
        {
            CanFocus = true,
            Title = label,
            Id = label,
            Data = value,
            HighlightStates = MouseState.In,
        };

        return checkbox;
    }

    /// <summary>
    ///     Assigns unique hotkeys to the labels of the subviews created by <see cref="CreateSubViews"/>.
    /// </summary>
    private void AssignUniqueHotKeys ()
    {
        if (!AssignHotKeys || Labels is null)
        {
            return;
        }

        foreach (View subView in SubViews)
        {
            string label = subView.Title ?? string.Empty;

            Rune [] runes = label.EnumerateRunes ().ToArray ();

            for (var i = 0; i < runes.Count (); i++)
            {
                Rune lower = Rune.ToLowerInvariant (runes [i]);
                var newKey = new Key (lower.Value);

                if (UsedHotKeys.Contains (newKey))
                {
                    continue;
                }

                if (!newKey.IsValid || newKey == Key.Empty || newKey == Key.Space || Rune.IsControl (newKey.AsRune))
                {
                    continue;
                }

                if (TextFormatter.FindHotKey (label, HotKeySpecifier, out int hotKeyPos, out Key hotKey))
                {
                    label = TextFormatter.RemoveHotKeySpecifier (label, hotKeyPos, HotKeySpecifier);
                }

                subView.Title = label.Insert (i, HotKeySpecifier.ToString ());
                subView.HotKey = newKey;
                UsedHotKeys.Add (subView.HotKey);

                break;
            }
        }
    }

    private int _horizontalSpace = 2;

    /// <summary>
    ///     Gets or sets the horizontal space for this <see cref="RadioGroup"/> if the <see cref="Orientation"/> is
    ///     <see cref="Orientation.Horizontal"/>
    /// </summary>
    public int HorizontalSpace
    {
        get => _horizontalSpace;
        set
        {
            if (_horizontalSpace != value)
            {
                _horizontalSpace = value;
                SetLayout ();
                // Pos.Align requires extra layout; good practice to call
                // Layout to ensure Pos.Align gets updated
                // TODO: See https://github.com/gui-cs/Terminal.Gui/issues/3951 which, if fixed, will 
                // TODO: negate need for this hack
                Layout ();
            }
        }
    }

    private void SetLayout ()
    {
        int maxNaturalCheckBoxWidth = 0;
        if (Values?.Count > 0 && Orientation == Orientation.Vertical)
        {
            // TODO: See https://github.com/gui-cs/Terminal.Gui/issues/3951 which, if fixed, will 
            // TODO: negate need for this hack
            maxNaturalCheckBoxWidth = SubViews.OfType<CheckBox> ().Max (
                                                             v =>
                                                             {
                                                                 v.SetRelativeLayout (Application.Screen.Size);
                                                                 v.Layout ();
                                                                 return v.Frame.Width;
                                                             });
        }

        for (var i = 0; i < SubViews.Count; i++)
        {
            if (Orientation == Orientation.Vertical)
            {
                SubViews.ElementAt (i).X = 0;
                SubViews.ElementAt (i).Y = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd);
                SubViews.ElementAt (i).Margin!.Thickness = new (0);
                SubViews.ElementAt (i).Width = Dim.Func (_ => Math.Max (Viewport.Width, maxNaturalCheckBoxWidth));
            }
            else
            {
                SubViews.ElementAt (i).X = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd);
                SubViews.ElementAt (i).Y = 0;
                SubViews.ElementAt (i).Margin!.Thickness = new (0, 0, (i < SubViews.Count - 1) ? _horizontalSpace : 0, 0);
                SubViews.ElementAt (i).Width = Dim.Auto ();
            }
        }
    }

    /// <summary>
    ///     Called when the checked state of the checkboxes needs to be updated.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public abstract void UpdateChecked ();


    /// <summary>
    ///     Gets or sets whether double-clicking on an Item will cause the <see cref="View.Accepting"/> event to be
    ///     raised.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         If <see langword="false"/> and Accept is not handled, the Accept event on the <see cref="View.SuperView"/> will
    ///         be raised. The default is
    ///         <see langword="true"/>.
    ///     </para>
    /// </remarks>
    public bool DoubleClickAccepts { get; set; } = true;

    #region IOrientation

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="SelectorBase"/>. The default is
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
    public void OnOrientationChanged (Orientation newOrientation)
    {
        SetLayout ();
        // Pos.Align requires extra layout; good practice to call
        // Layout to ensure Pos.Align gets updated
        // TODO: See https://github.com/gui-cs/Terminal.Gui/issues/3951 which, if fixed, will
        // TODO: negate need for this hack
        Layout ();
    }

    #endregion IOrientation
}
