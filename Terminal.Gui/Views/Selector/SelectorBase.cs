#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a base class for selectors that display and select items.
/// </summary>
/// <typeparam name="T">The type of the data associated with each item.</typeparam>
/// <seealso cref="FlagSelector"/>
/// <seealso cref="FlagSelector{TEnum}"/>
/// <seealso cref="OptionSelector"/>
public abstract class SelectorBase<T> : View, IOrientation, IDesignable
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SelectorBase{T}"/> class.
    /// </summary>
    protected SelectorBase ()
    {
        CanFocus = true;
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        // ReSharper disable once UseObjectOrCollectionInitializer
        _orientationHelper = new (this);// Do not use object initializer!
        _orientationHelper.Orientation = Orientation.Vertical;

        AddCommand (Command.Accept, HandleAcceptCommand);

        AddCommand (Command.Start, () => HasFocus && MoveHome ());
        AddCommand (Command.End, () => HasFocus && MoveEnd ());

        MouseBindings.Add (MouseFlags.Button1DoubleClicked, Command.Accept);
        SetupKeyBindings ();
    }

    private void SetupKeyBindings ()
    {
        // Default keybindings for this view
        if (Orientation == Orientation.Vertical)
        {
            KeyBindings.Remove (Key.CursorUp);
            KeyBindings.Add (Key.CursorUp, Command.Up);
            KeyBindings.Remove (Key.CursorDown);
            KeyBindings.Add (Key.CursorDown, Command.Down);
        }
        else
        {
            KeyBindings.Remove (Key.CursorLeft);
            KeyBindings.Add (Key.CursorLeft, Command.Up);
            KeyBindings.Remove (Key.CursorRight);
            KeyBindings.Add (Key.CursorRight, Command.Down);
        }

        KeyBindings.Remove (Key.Home);
        KeyBindings.Add (Key.Home, Command.Start);
        KeyBindings.Remove (Key.End);
        KeyBindings.Add (Key.End, Command.End);
    }
    /// <summary>
    ///     Gets or sets the Item index for the cursor. The cursor may or may not be the selected
    ///     Item.
    /// </summary>
    public int Cursor
    {
        get => SubViews.IndexOf (Focused);
        set
        {
            if (value < 0 || value >= SubViews.Count)
            {
                return;
            }
            SubViews.ElementAt (value).SetFocus ();
        }
    }

    private bool MoveEnd ()
    {
        if (Cursor == SubViews.Count - 1)
        {
            return false;
        }

        Cursor = Math.Max (SubViews.OfType<CheckBox> ().Count () - 1, 0);

        return true;
    }

    private bool MoveHome ()
    {
        if (Cursor != 0)
        {
            Cursor = 0;

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override void EndInit ()
    {
        if (SubViews.Count == 0)
        {
            CreateCheckBoxes ();
        }

        base.EndInit ();
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


    /// <summary>
    ///     Gets or sets whether double-clicking on an item will cause the <see cref="View.Accepting"/> event to be
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

    /// <summary>
    ///     Creates the checkboxes for the selector.
    /// </summary>
    protected abstract void CreateCheckBoxes ();

    /// <summary>
    ///     Creates a checkbox with the specified name and data.
    /// </summary>
    /// <param name="name">The name of the checkbox.</param>
    /// <param name="data">The data associated with the checkbox.</param>
    /// <returns>The created checkbox.</returns>
    protected virtual CheckBox CreateCheckBox (string name, T data)
    {
        string nameWithHotKey = name;
        if (AssignHotKeysToCheckBoxes)
        {
            for (var i = 0; i < name.GetRuneCount (); i++)
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
            Data = data,
            HighlightStyle = HighlightStyle.Hover
        };

        checkbox.GettingNormalColor += (_, e) =>
        {
            if (SuperView is { HasFocus: true })
            {
                e.Cancel = true;

                if (!HasFocus)
                {
                    e.NewValue = GetFocusColor ();
                }
                else
                {
                    if (checkbox._colorScheme is { })
                    {
                        e.NewValue = checkbox._colorScheme.Normal;
                    }
                    else
                    {
                        e.NewValue = GetNormalColor ();
                    }
                }
            }
        };

        checkbox.GettingHotNormalColor += (_, e) =>
        {
            if (SuperView is { HasFocus: true })
            {
                e.Cancel = true;
                if (!HasFocus)
                {
                    e.NewValue = GetHotFocusColor ();
                }
                else
                {
                    if (checkbox._colorScheme is { })
                    {
                        e.NewValue = checkbox._colorScheme.HotNormal;
                    }
                    else
                    {
                        e.NewValue = GetHotNormalColor ();
                    }
                }
            }
        };

        return checkbox;
    }

    private int _horizontalSpace = 2;

    /// <summary>
    ///     Gets or sets the horizontal space that will be added between Items if the <see cref="Orientation"/> is
    ///     <see cref="Orientation.Horizontal"/>
    /// </summary>
    public int HorizontalSpace
    {
        get => _horizontalSpace;
        set
        {
            if (_horizontalSpace != value && Orientation == Orientation.Horizontal)
            {
                _horizontalSpace = value;
                SetLayout();
            }
        }
    }

    /// <summary>
    ///     Sets the layout of the checkboxes.
    /// </summary>
    protected void SetLayout ()
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

                if (sv != SubViews.ElementAt (SubViews.Count - 1))
                {
                    sv.Margin!.Thickness = new (0, 0, HorizontalSpace, 0);
                }
            }
        }

        // BUGBUG: PosAlign requires this for some reason
        Layout ();
    }

    #region IOrientation

    /// <summary>
    ///     Gets or sets the orientation of the selector.
    /// </summary>
    public Orientation Orientation
    {
        get => _orientationHelper.Orientation;
        set => _orientationHelper.Orientation = value;
    }

    private readonly OrientationHelper _orientationHelper;

#pragma warning disable CS0067
    /// <inheritdoc/>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;
    /// <inheritdoc/>
    public event EventHandler<EventArgs<Orientation>>? OrientationChanged;
#pragma warning restore CS0067

    /// <inheritdoc/>
    public void OnOrientationChanged (Orientation newOrientation)
    {
        SetupKeyBindings ();
        SetLayout ();
    }

    #endregion IOrientation

    /// <summary>
    ///     If <see langword="true"/> the CheckBoxes will each be automatically assigned a hotkey.
    ///     <see cref="UsedHotKeys"/> will be used to ensure unique keys are assigned. Set <see cref="UsedHotKeys"/>
    ///     before setting options with any hotkeys that may conflict with other Views.
    /// </summary>
    public bool AssignHotKeysToCheckBoxes { get; set; }

    /// <summary>
    ///     Gets the list of hotkeys already used by the CheckBoxes or that should not be used if
    ///     <see cref="AssignHotKeysToCheckBoxes"/> is enabled.
    /// </summary>
    public List<Key> UsedHotKeys { get; } = new ();
}
