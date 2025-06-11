#nullable enable
using System.Diagnostics;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides a user interface for displaying and selecting a single item from a list of options.
///     Each option is represented by a checkbox, but only one can be selected at a time.
/// </summary>
public class OptionSelector : View, IOrientation, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OptionSelector"/> class.
    /// </summary>
    public OptionSelector ()
    {
        CanFocus = true;

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Vertical;

        // Enter key - Accept the currently selected item
        // DoubleClick - Activate (focus) and Accept the item under the mouse
        // Space key - Toggle the currently selected item
        // Click - Activate (focus) and Activate the item under the mouse
        // Not Focused:
        //  HotKey - Activate (focus). Do NOT change state.
        //  Item HotKey - Toggle the item (Do NOT Activate)
        // Focused:
        //  HotKey - Toggle the currently selected item
        //  Item HotKey - Toggle the item.
        AddCommand (Command.Activate, HandleActivateCommand);
        AddCommand (Command.HotKey, HandleHotKeyCommand);

        CreateCheckBoxes ();
    }

    private bool? HandleActivateCommand (ICommandContext? ctx)
    {
        return RaiseActivating (ctx);
    }

    private bool? HandleHotKeyCommand (ICommandContext? ctx)
    {
        // If the command did not come from a keyboard event, ignore it
        if (ctx is not CommandContext<KeyBinding> keyCommandContext)
        {
            return false;
        }

        if (HasFocus)
        {
            if (HotKey == keyCommandContext.Binding.Key?.NoAlt.NoCtrl.NoShift!)
            {
                // It's this.HotKey OR Another View (Label?) forwarded the hotkey command to us - Act just like `Space` (Select)
                return InvokeCommand (Command.Activate, ctx);
            }
        }


        if (RaiseHandlingHotKey (ctx) == true)
        {
            return true;
        }


        // Default Command.Hotkey sets focus
        SetFocus ();

        return false;
    }

    private int? _selectedItem;

    /// <summary>
    /// Gets or sets the index of the selected item. Will be <see langword="null"/> if no item is selected.
    /// </summary>
    public int? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (value < 0 || value >= SubViews.OfType<CheckBox> ().Count ())
            {
                throw new ArgumentOutOfRangeException (nameof (value), @$"SelectedItem must be between 0 and {SubViews.OfType<CheckBox> ().Count () - 1}");

            }
            if (_selectedItem == value)
            {
                return;
            }

            int? previousSelectedItem = _selectedItem;
            _selectedItem = value;

            UpdateChecked ();

            RaiseSelectedItemChanged (previousSelectedItem);
        }
    }

    private void RaiseSelectedItemChanged (int? previousSelectedItem)
    {
        OnSelectedItemChanged (SelectedItem, previousSelectedItem);
        if (SelectedItem.HasValue)
        {
            SelectedItemChanged?.Invoke (this, new (SelectedItem, previousSelectedItem));
        }
    }

    /// <summary>
    ///     Called when <see cref="SelectedItem"/> has changed.
    /// </summary>
    protected virtual void OnSelectedItemChanged (int? selectedItem, int? previousSelectedItem) { }

    /// <summary>
    ///     Raised when <see cref="SelectedItem"/> has changed.
    /// </summary>
    public event EventHandler<SelectedItemChangedArgs>? SelectedItemChanged;

    private IReadOnlyList<string>? _options;

    /// <summary>
    ///     Gets or sets the list of options.
    /// </summary>
    public IReadOnlyList<string>? Options
    {
        get => _options;
        set
        {
            _options = value;
            CreateCheckBoxes ();
        }
    }

    private bool _assignHotKeysToCheckBoxes;

    /// <summary>
    ///     If <see langword="true"/> the CheckBoxes will each be automatically assigned a hotkey.
    ///     <see cref="UsedHotKeys"/> will be used to ensure unique keys are assigned. Set <see cref="UsedHotKeys"/>
    ///     before setting <see cref="Options"/> with any hotkeys that may conflict with other Views.
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
    public List<Key> UsedHotKeys { get; } = new ();

    private void CreateCheckBoxes ()
    {
        if (Options is null)
        {
            return;
        }

        foreach (CheckBox cb in RemoveAll<CheckBox> ())
        {
            cb.Dispose ();
        }

        for (var index = 0; index < Options.Count; index++)
        {
            Add (CreateCheckBox (Options [index], index));
        }

        SetLayout ();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    protected virtual CheckBox CreateCheckBox (string name, int index)
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
            Data = index,
            //HighlightStates = HighlightStates.Hover,
            RadioStyle = true
        };

        checkbox.GettingAttributeForRole += (_, e) =>
        {
            //if (SuperView is { HasFocus: false })
            //{
            //    return;
            //}

            //switch (e.Role)
            //{
            //    case VisualRole.Normal:
            //        e.Handled = true;

            //        if (!HasFocus && !CanFocus)
            //        {
            //            e.Result = GetAttributeForRole (VisualRole.Focus);
            //        }
            //        else
            //        {
            //            // If _scheme was set, it's because of Hover
            //            if (checkbox.HasScheme)
            //            {
            //                e.Result = checkbox.GetAttributeForRole (VisualRole.Normal);
            //            }
            //            else
            //            {
            //                e.Result = GetAttributeForRole (VisualRole.Normal);
            //            }
            //        }

            //        break;

            //    case VisualRole.HotNormal:
            //        e.Handled = true;

            //        if (!HasFocus && !CanFocus)
            //        {
            //            e.Result = GetAttributeForRole (VisualRole.HotFocus);
            //        }
            //        else
            //        {
            //            e.Result = GetAttributeForRole (VisualRole.HotNormal);
            //        }

            //        break;
            //}
        };

        checkbox.Activating += (sender, args) =>
                               {
                                   // Activating doesn't normally propogate, so we do it here
                                   if (RaiseActivating (args.Context) is true)
                                   {
                                       args.Handled = true;

                                       return;
                                   }

                                   CommandContext<KeyBinding>? keyCommandContext = args.Context as CommandContext<KeyBinding>?;
                                   if (keyCommandContext is null && (int)checkbox.Data == SelectedItem)
                                   {
                                       // Mouse should not change the state
                                       checkbox.CheckedState = CheckState.Checked;
                                   }

                                   if (keyCommandContext is { } && (int)checkbox.Data == SelectedItem)
                                   {
                                       Cycle ();
                                   }
                                   else
                                   {
                                       SelectedItem = (int)checkbox.Data;

                                       if (HasFocus)
                                       {
                                           SubViews.OfType<CheckBox> ().ToArray () [SelectedItem!.Value].SetFocus ();
                                       }

                                   }

                                   //if (!CanFocus && RaiseAccepting (args.Context) is true)
                                   //{
                                   //    args.Handled = true;
                                   //}
                               };

        checkbox.Accepting += (sender, args) =>
                              {
                                  SelectedItem = (int)checkbox.Data;
                              };


        return checkbox;
    }

    private void Cycle ()
    {
        if (SelectedItem == SubViews.OfType<CheckBox> ().Count () - 1)
        {
            SelectedItem = 0;
        }
        else
        {
            SelectedItem++;
        }

        if (HasFocus)
        {
            SubViews.OfType<CheckBox> ().ToArray () [SelectedItem!.Value].SetFocus ();
        }
    }

    private void SetLayout ()
    {
        foreach (View sv in SubViews)
        {
            if (Orientation == Orientation.Vertical)
            {
                sv.X = 0;
                sv.Y = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd);
            }
            else
            {
                sv.X = Pos.Align (Alignment.Start, AlignmentModes.StartToEnd);
                sv.Y = 0;
                sv.Margin!.Thickness = new (0, 0, 1, 0);
            }
        }
    }

    private void UpdateChecked ()
    {
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            var index = (int)(cb.Data ?? throw new InvalidOperationException ("CheckBox.Data must be set"));

            cb.CheckedState = index == SelectedItem ? CheckState.Checked : CheckState.UnChecked;
        }
    }

    #region IOrientation

    /// <summary>
    ///     Gets or sets the <see cref="Orientation"/> for this <see cref="OptionSelector"/>. The default is
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
    protected override bool OnAdvancingFocus (NavigationDirection direction, TabBehavior? behavior)
    {
        if (behavior is { } && behavior != TabStop)
        {
            return false;
        }

        return false;
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        AssignHotKeysToCheckBoxes = true;
        Options = ["Option 1", "Option 2", "Third Option", "Option Quattro"];

        return true;
    }
}
