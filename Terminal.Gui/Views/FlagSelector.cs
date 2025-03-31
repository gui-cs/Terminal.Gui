#nullable enable
using System;
using System.Collections.ObjectModel;

namespace Terminal.Gui;

/// <summary>Displays the flags in <see cref="TEnum"/> and enables selecting the flags.</summary>
public class FlagSelector : View, IDesignable, IOrientation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FlagSelector{TEnum}"/> class.
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

    private bool? HandleAcceptCommand (ICommandContext? ctx)
    {
        return RaiseAccepting (ctx);
    }

    private uint _value;

    /// <summary>
    ///     Gets or sets the value.
    /// </summary>
    public uint Value
    {
        get => _value;
        set
        {
            if (_value == value)
            {
                return;
            }

            _value = value;

            if (_value == 0)
            {
                UncheckAll ();
            }
            else
            {
                UncheckNone ();
                UpdateChecked ();
            }

            if (ValueEdit is { })
            {
                ValueEdit.Text = value.ToString ();
            }

            RaiseValueChanged ();
        }
    }

    private void RaiseValueChanged ()
    {
        OnValueChanged ();
        ValueChanged?.Invoke (this, new EventArgs<uint> (Value));
    }

    protected virtual void OnValueChanged ()
    {
    }

    public event EventHandler<EventArgs<uint>>? ValueChanged;


    private FlagSelectorStyles _styles;

    /// <summary>
    /// 
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
    /// <param name="names"></param>
    /// <param name="flags"></param>
    public void SetFlags (ReadOnlyCollection<string>? names, ReadOnlyCollection<uint>? flags)
    {
        Names = names;
        Flags = flags;

        CreateSubViews ();
    }

    /// <summary>
    ///     Gets or sets the flags.
    /// </summary>
    public ReadOnlyCollection<uint>? Flags { get; internal set; }

    /// <summary>
    ///     Gets or sets the flag names;
    /// </summary>
    public ReadOnlyCollection<string>? Names { get; internal set; }

    private TextField? ValueEdit { get; set; }

    private void CreateSubViews ()
    {
        if (Flags is null || Names is null)
        {
            return;
        }

        View [] subviews = SubViews.ToArray ();

        RemoveAll ();

        foreach (View v in subviews)
        {
            v.Dispose ();
        }

        if (Styles.HasFlag (FlagSelectorStyles.ShowNone) && !Flags.Contains (0))
        {
            Add (CreateCheckBox ("None", 0));
        }

        for (var index = 0; index < Flags.Count; index++)
        {
            if (!Styles.HasFlag (FlagSelectorStyles.ShowNone) && Flags [index] == 0)
            {
                continue;
            }
            Add (CreateCheckBox (Names [index], Flags [index]));
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

        CheckBox CreateCheckBox (string name, uint flag)
        {
            var checkbox = new CheckBox
            {
                CanFocus = false,
                Title = name,
                Id = name,
                Data = flag,
                HighlightStyle = HighlightStyle
            };

            checkbox.Selecting += (sender, args) =>
                                  {
                                      RaiseSelecting (args.Context);
                                  };


            checkbox.CheckedStateChanged += (sender, args) =>
                                            {
                                                uint newValue = Value;

                                                if (checkbox.CheckedState == CheckState.Checked)
                                                {
                                                    if ((uint)checkbox.Data == 0)
                                                    {
                                                        newValue = 0;
                                                    }
                                                    else
                                                    {
                                                        newValue |= flag;
                                                    }
                                                }
                                                else
                                                {
                                                    newValue &= ~flag;
                                                }

                                                Value = newValue;
                                                //UpdateChecked();
                                            };

            return checkbox;
        }
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
                sv.Margin!.Thickness = new Thickness (0, 0, 1, 0);
            }
        }
    }

    private void UncheckAll ()
    {
        foreach (CheckBox cb in SubViews.Where (sv => sv is CheckBox cb && cb.Title != "None").Cast<CheckBox> ())
        {
            cb.CheckedState = CheckState.UnChecked;
        }
    }

    private void UncheckNone ()
    {
        foreach (CheckBox cb in SubViews.Where (sv => sv is CheckBox { Title: "None" }).Cast<CheckBox> ())
        {
            cb.CheckedState = CheckState.UnChecked;
        }
    }

    private void UpdateChecked ()
    {
        foreach (CheckBox cb in SubViews.Where (sv => sv is CheckBox { }).Cast<CheckBox> ())
        {
            uint flag = (uint)cb.Data;
            // If this flag is set in Value, check the checkbox. Otherwise, uncheck it.
            if ((uint)cb.Data == 0 && Value != 0)
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
    protected override void OnSubViewAdded (View view) { }

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

#pragma warning restore CS0067

    /// <summary>Called when <see cref="Orientation"/> has changed.</summary>
    /// <param name="newOrientation"></param>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        SetLayout ();
    }

    #endregion IOrientation

    [Flags]
    private enum DemoFlags
    {
        None = 0,

        First = 1,

        Second = 2,

        Third = 4,

        Fourth = 8,

        FirstAndFourth = First | Fourth
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        SetFlags (Enum.GetNames<FlagSelectorStyles> ().ToList ().AsReadOnly (), Enum.GetValues<FlagSelectorStyles> ().Select (f => (uint)f).ToList ().AsReadOnly ());

        return true;
    }
}
