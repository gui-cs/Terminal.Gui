using System.Collections.Immutable;

namespace Terminal.Gui.Views;

/// <summary>
///     The abstract base class for <see cref="OptionSelector{TEnum}"/> and <see cref="FlagSelector{TFlagsEnum}"/>.
/// </summary>
public abstract class SelectorBase : View, IOrientation, IValue<int?>
{
    /// <summary>
    ///     Gets or sets the default Highlight Style.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static MouseState DefaultMouseHighlightStates { get; set; } = MouseState.In;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SelectorBase"/> class.
    /// </summary>
    protected SelectorBase ()
    {
        CanFocus = true;

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new OrientationHelper (this);
        _orientationHelper.Orientation = Orientation.Vertical;

        MouseBindings.Remove (MouseFlags.LeftButtonClicked);

        CommandsToBubbleUp = [Command.Activate, Command.Accept];

        KeyBindings.ReplaceCommands (Key.CursorDown, Command.Down);
        KeyBindings.ReplaceCommands (Key.CursorRight, Command.Right);
        KeyBindings.ReplaceCommands (Key.CursorUp, Command.Up);
        KeyBindings.ReplaceCommands (Key.CursorLeft, Command.Left);

        AddCommand (Command.Down, () => MoveNext (Command.Down));
        AddCommand (Command.Right, () => MoveNext (Command.Right));

        AddCommand (Command.Up, () => MovePrevious (Command.Up));
        AddCommand (Command.Left, () => MovePrevious (Command.Left));
    }

    private bool MoveNext (Command command)
    {
        if ((command == Command.Down && Orientation == Orientation.Horizontal) || (command == Command.Right && Orientation == Orientation.Vertical))
        {
            return false;
        }

        if (Focused?.TabStop != ViewBase.TabBehavior.NoStop)
        {
            return false;
        }

        int active = SubViews.OfType<CheckBox> ().ToArray ().IndexOf (Focused);

        if (active < SubViews.OfType<CheckBox> ().Count () - 1)
        {
            active++;
        }
        else
        {
            if (Styles.HasFlag (SelectorStyles.ShowValue))
            {
                _valueField?.SetFocus ();

                return true;
            }
            active = 0;
        }
        SubViews.OfType<CheckBox> ().ToArray ().ElementAt (active).SetFocus ();

        return true;
    }

    private bool MovePrevious (Command command)
    {
        if ((command == Command.Up && Orientation == Orientation.Horizontal) || (command == Command.Left && Orientation == Orientation.Vertical))
        {
            return false;
        }

        if (Focused?.TabStop != ViewBase.TabBehavior.NoStop)
        {
            return false;
        }

        int active = SubViews.OfType<CheckBox> ().ToArray ().IndexOf (Focused);

        switch (active)
        {
            case -1 when Styles.HasFlag (SelectorStyles.ShowValue):
                active = SubViews.OfType<CheckBox> ().Count () - 1;

                break;

            case > 0:
                active--;

                break;

            default:
            {
                if (Styles.HasFlag (SelectorStyles.ShowValue))
                {
                    _valueField?.SetFocus ();

                    return true;
                }
                active = SubViews.OfType<CheckBox> ().Count () - 1;

                break;
            }
        }
        SubViews.OfType<CheckBox> ().ToArray ().ElementAt (active).SetFocus ();

        return true;
    }

    /// <summary>
    ///     Gets or sets the styles for the flag selector.
    /// </summary>
    public SelectorStyles Styles
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }

            field = value;

            CreateSubViews ();
            UpdateChecked ();
        }
    }

    /// <inheritdoc/>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        if (base.OnAccepting (args))
        {
            return true;
        }

        Logging.Debug ($"{this.ToIdentifyingString ()} ({args})");

        // Per spec: Enter key should Activate AND Accept for both OptionSelector and FlagSelector.
        // Enter only triggers Command.Accept (View's default key binding), so invoke Activate here
        // before continuing with Accept processing. Also handle direct programmatic Accept invocations
        // (Binding is null) by activating the currently focused checkbox.
        bool enterFromCheckBox = args.Context?.Binding is KeyBinding { Key: { } key }
                                 && key == Key.Enter
                                 && args.Context?.Source?.TryGetTarget (out View? enterSource) == true
                                 && enterSource is CheckBox;

        bool directAccept = args.Context?.Binding is null && Focused is CheckBox;

        if (enterFromCheckBox || directAccept)
        {
            // Create a fresh context with Command.Activate (not Accept) and IsBubblingUp=false.
            // The original args.Context may have Command=Accept and IsBubblingUp=true from a bubble,
            // which would cause TryBubbleUp to bubble the wrong command to SuperView.
            // For direct invocations, use the focused CheckBox as the source so OnActivated
            // identifies which item to activate.
            WeakReference<View> source = enterFromCheckBox ? args.Context!.Source! : new WeakReference<View> (Focused!);

            CommandContext activateCtx = new (Command.Activate, source, args.Context?.Binding);
            InvokeCommand (Command.Activate, activateCtx);
        }

        return args.Context?.Binding switch
               {
                   { Source: { } weakSource } when weakSource.TryGetTarget (out View? src) && src == this => true,
                   MouseBinding mouseBinding when mouseBinding.MouseEvent!.Flags.HasFlag (MouseFlags.LeftButtonDoubleClicked) => !DoubleClickAccepts,
                   KeyBinding { Key: { } } keyBinding when keyBinding.Key == Key.Enter => false,
                   null => false,
                   _ => true
               };
    }

    /// <summary>
    ///     Gets or sets the value of the selector. Will be <see langword="null"/> if no value is set.
    /// </summary>
    public virtual int? Value
    {
        get;
        set
        {
            if (value is { } && Values is { } && !Values.Contains ((int)value))
            {
                throw new ArgumentOutOfRangeException (nameof (value), @$"Value must be one of the following: {string.Join (", ", Values)}");
            }

            if (field == value)
            {
                return;
            }

            int? previousValue = field;

            // Raise IValue<int?>.ValueChanging (cancellable)
            if (RaiseValueChanging (previousValue, value))
            {
                return;
            }

            Logging.Debug ($"{this.ToIdentifyingString ()} ({field}->{value})");

            field = value;

            UpdateChecked ();
            RaiseValueChanged (previousValue, field);
        }
    }

    #region IValue<int?> Implementation

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<object?>>? ValueChangedUntyped;

    /// <summary>
    ///     Raises the <see cref="ValueChanging"/> event.
    /// </summary>
    /// <returns><see langword="true"/> if the change was cancelled.</returns>
    protected bool RaiseValueChanging (int? currentValue, int? newValue)
    {
        Logging.Debug ($"{this.ToIdentifyingString ()} ({currentValue}->{newValue})");

        ValueChangingEventArgs<int?> args = new (currentValue, newValue);
        ValueChanging?.Invoke (this, args);

        return args.Handled;
    }

    /// <summary>
    ///     Raises the <see cref="ValueChanged"/> event.
    /// </summary>
    /// <param name="previousValue">The value before the change.</param>
    /// <param name="newValue">The value after the change.</param>
    protected void RaiseValueChanged (int? previousValue, int? newValue)
    {
        _valueField?.Text = Value.ToString ()!;

        OnValueChanged (newValue, previousValue);
        ValueChanged?.Invoke (this, new ValueChangedEventArgs<int?> (previousValue, newValue));
        ValueChangedUntyped?.Invoke (this, new ValueChangedEventArgs<object?> (previousValue, newValue));
    }

    /// <summary>
    ///     Called when <see cref="Value"/> has changed.
    /// </summary>
    protected virtual void OnValueChanged (int? value, int? previousValue) { }

    /// <inheritdoc/>
    public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;

    /// <inheritdoc/>
    public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;

    #endregion

    /// <summary>
    ///     Gets or sets the option values. If <see cref="Values"/> is <see langword="null"/>, get will
    ///     return values based on the <see cref="Labels"/> property.
    /// </summary>
    public virtual IReadOnlyList<int>? Values
    {
        get
        {
            if (field is { })
            {
                return field;
            }

            // Use Labels and assume 0..Labels.Count - 1
            return Labels is { } ? Enumerable.Range (0, Labels.Count).ToList () : null;
        }
        set
        {
            field = value;

            // Ensure Value defaults to the first valid entry in Values if not already set
            if (Value is null && field?.Any () == true)
            {
                Value = field.First ();
            }

            CreateSubViews ();
            UpdateChecked ();
        }
    }

    /// <summary>
    ///     Gets or sets the list of labels for each value in <see cref="Values"/>.
    /// </summary>
    public IReadOnlyList<string>? Labels
    {
        get;
        set
        {
            field = value;

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

    // Note: AssignHotKeys and UsedHotKeys are inherited from the View base class.
    // SelectorBase uses the base class's automatic hotkey assignment feature.

    /// <summary>
    ///     Gets or sets the tab behavior of the checkboxes within the selector. If <see cref="TabBehavior.TabStop"/> (the
    ///     default),
    ///     navigating within and out of the selector will follow the standard superview/subview behavior. If
    ///     <see cref="TabBehavior.NoStop"/>,
    ///     only the arrow keys wil navigate within the selector.
    /// </summary>
    public TabBehavior? TabBehavior
    {
        get
        {
            if (field is { })
            {
                return field;
            }

            return TabStop;
        }
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;
            CreateSubViews ();
        }
    }

    private TextField? _valueField;

    /// <summary>
    ///     Creates the subviews for this selector.
    /// </summary>
    public virtual void CreateSubViews ()
    {
        // Note: UsedHotKeys cleanup is handled by the base class's RaiseSubViewRemoved
        foreach (View sv in RemoveAll ())
        {
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

        for (var index = 0; index < Labels?.Count; index++)
        {
            Add (CreateCheckBox (Labels.ElementAt (index), Values!.ElementAt (index)));
        }

        if (Styles.HasFlag (SelectorStyles.ShowValue))
        {
            _valueField = new TextField
            {
                CanFocus = false,
                Id = "valueField",
                Text = Value.ToString ()!,

                // TODO: Don't hardcode this; base it on max Value
                Width = 5,
                ReadOnly = true,
                TabStop = TabBehavior
            };

            Add (_valueField);
        }

        // Note: Hotkey assignment is now handled automatically by the base class
        // when SubViews are added via Add(). No need to call AssignUniqueHotKeys() here.
        SetLayout ();
    }

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
            MouseHighlightStates = DefaultMouseHighlightStates,
            TabStop = TabBehavior
        };

        return checkbox;
    }

    private int _horizontalSpace = 2;

    /// <summary>
    ///     Gets or sets the horizontal space for this <see cref="OptionSelector"/> if the <see cref="Orientation"/> is
    ///     <see cref="Orientation.Horizontal"/>
    /// </summary>
    public int HorizontalSpace
    {
        get => _horizontalSpace;
        set
        {
            if (_horizontalSpace == value)
            {
                return;
            }
            _horizontalSpace = value;
            SetLayout ();
        }
    }

    /// <summary>
    ///     Updates the layout of the subviews based on <see cref="Orientation"/>.
    /// </summary>
    protected void SetLayout ()
    {
        var maxNaturalCheckBoxWidth = 0;

        if (Values?.Count > 0 && Orientation == Orientation.Vertical)
        {
            maxNaturalCheckBoxWidth = SubViews.OfType<CheckBox> ()
                                              .Max (v =>
                                                    {
                                                        v.SetRelativeLayout (App?.Screen.Size ?? new Size (2048, 2048));

                                                        return v.Frame.Width;
                                                    });
        }

        for (var i = 0; i < SubViews.Count; i++)
        {
            if (Orientation == Orientation.Vertical)
            {
                SubViews.ElementAt (i).X = 0;
                SubViews.ElementAt (i).Y = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd);
                SubViews.ElementAt (i).Margin!.Thickness = new Thickness (0);
                SubViews.ElementAt (i).Width = Dim.Func (_ => maxNaturalCheckBoxWidth);
            }
            else
            {
                SubViews.ElementAt (i).X = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd);
                SubViews.ElementAt (i).Y = 0;
                SubViews.ElementAt (i).Margin!.Thickness = new Thickness (0, 0, i < SubViews.Count - 1 ? _horizontalSpace : 0, 0);
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
    public Orientation Orientation { get => _orientationHelper.Orientation; set => _orientationHelper.Orientation = value; }

    private readonly OrientationHelper _orientationHelper;

#pragma warning disable CS0067 // The event is never used
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067 // The event is never used

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation) => SetLayout ();

    #endregion IOrientation
}
