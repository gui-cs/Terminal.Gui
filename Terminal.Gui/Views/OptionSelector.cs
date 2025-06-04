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

        _orientationHelper = new (this);
        _orientationHelper.Orientation = Orientation.Vertical;

        // Accept (Enter key or DoubleClick) - Raise Accept event - DO NOT advance state
        AddCommand (Command.Accept, HandleAcceptCommand);

        CreateCheckBoxes ();
    }

    private bool? HandleAcceptCommand (ICommandContext? ctx) { return RaiseAccepting (ctx); }

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
                throw new ArgumentOutOfRangeException (nameof (value), @$"SelectedItem must be between 0 and {SubViews.OfType<CheckBox> ().Count ()-1}");

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
                            e.Result = checkbox.GetAttributeForRole(VisualRole.Normal);
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
            if (checkbox.CheckedState == CheckState.Checked)
            {
                SelectedItem = index;
            }
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
    public bool EnableForDesign ()
    {
        AssignHotKeysToCheckBoxes = true;
        Options = ["Option 1", "Option 2", "Third Option", "Option Quattro"];

        return true;
    }
}
