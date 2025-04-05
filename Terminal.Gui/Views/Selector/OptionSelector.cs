#nullable enable
namespace Terminal.Gui;

/// <summary>
///     Provides a user interface for displaying and selecting a single item from a list of options.
///     Each option is represented by a checkbox, but only one can be selected at a time.
/// </summary>
public class OptionSelector : SelectorBase<int>, IDesignable, IOrientation
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OptionSelector"/> class.
    /// </summary>
    public OptionSelector ()
    {
        AddCommand (Command.HotKey, HandleHotKeyCommand);
    }


    private bool? HandleHotKeyCommand (ICommandContext? ctx)
    {
        // If the command did not come from a keyboard event, ignore it
        if (ctx is not CommandContext<KeyBinding> keyCommandContext)
        {
            return false;
        }

        SetFocus ();
        if (SelectedItem == null)
        {
            SelectedItem = 0;

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    protected override bool OnSelecting (CommandEventArgs args)
    {
        CheckBox? source = args.Context?.Source as CheckBox;

        if (args.Context is CommandContext<KeyBinding> keyCommandContext)
        {
            if (keyCommandContext.Binding.Key is { } && keyCommandContext.Binding.Key == Key.Space)
            {
                if (source?.CheckedState == CheckState.Checked)
                {
                    // If the checkbox is already checked, we want move to next and check that one
                    if (AdvanceFocus (NavigationDirection.Forward, TabBehavior.TabStop))
                    {
                        if (Focused is CheckBox { CheckedState: CheckState.UnChecked } focused)
                        {
                            focused.CheckedState = CheckState.Checked;
                        }
                    }
                    args.Cancel = true;
                }
            }
        }

        if (args.Context is CommandContext<MouseBinding> mouseCommandContext)
        {
            if (source?.CheckedState == CheckState.Checked)
            {
                args.Cancel = true;
            }
        }
        return base.OnSelecting (args);
    }

    /// <summary>
    /// Gets or sets the index of the selected item.
    /// </summary>
    public int? SelectedItem
    {
        get => GetSelectedItem ();
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException ();
            }
            int? previousSelectedItem = GetSelectedItem ();
            if (previousSelectedItem == value)
            {
                return;
            }

            if (value is null)
            {
                UncheckAll ();
            }
            else
            {
                if (value < 0 || value >= SubViews.Count)
                {
                    return;
                }
                SubViews.OfType<CheckBox> ().ElementAt (value.Value).CheckedState = CheckState.Checked;
            }

            //RaiseSelectedItemChanged (value, previousSelectedItem);
        }
    }

    private int? GetSelectedItem ()
    {
        CheckBox? selectedCheckBox = SubViews.OfType<CheckBox> ().FirstOrDefault (cb => cb.CheckedState == CheckState.Checked);
        if (selectedCheckBox is null)
        {
            return null;
        }
        return SubViews.IndexOf (selectedCheckBox);
    }

    private void RaiseSelectedItemChanged (int? selectedItem, int? previousSelectedItem)
    {
        OnSelectedItemChanged (SelectedItem, previousSelectedItem);
        if (selectedItem.HasValue)
        {
            SelectedItemChanged?.Invoke (this, new (selectedItem, previousSelectedItem));
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

    /// <inheritdoc/>
    protected override void CreateCheckBoxes ()
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

        if (_options is { Count: > 0 })
        {
            SubViews.OfType<CheckBox> ().ElementAt (0).CheckedState = CheckState.Checked;
        }

        int? previousSelectedItem = null;
        foreach (CheckBox checkbox in SubViews.OfType<CheckBox> ())
        {
            checkbox.CheckedStateChanging += (sender, args) =>
                                             {
                                                 if (args.CurrentValue == CheckState.Checked)
                                                 {
                                                     previousSelectedItem = SubViews.OfType<CheckBox> ().ToList ().IndexOf (checkbox);
                                                 }
                                             };
            checkbox.CheckedStateChanged += (sender, args) =>
                                            {
                                                if (checkbox.CheckedState == CheckState.Checked)
                                                {
                                                    int? selectedItem = SubViews.IndexOf (checkbox);

                                                    UncheckOthers (checkbox);

                                                    if (selectedItem != previousSelectedItem)
                                                    {
                                                        if (RaiseSelecting (null) is true)
                                                        {
                                                            return;
                                                        }
                                                        RaiseSelectedItemChanged (selectedItem, previousSelectedItem);
                                                    }
                                                }
                                            };

            checkbox.Selecting += (sender, args) =>
                                  {
                                      // Raise selecting event, which will be canceled if the checkbox is already checked
                                      if (RaiseSelecting (args.Context) is true)
                                      {
                                          if (checkbox.CheckedState == CheckState.Checked)
                                          {
                                              args.Cancel = true;
                                          }

                                          return;
                                      }
                                  };

            checkbox.Accepting += (sender, args) =>
                                  {
                                      if (args.Context is CommandContext<MouseBinding> mouseCommandContext)
                                      {
                                          if (mouseCommandContext.Binding.MouseEventArgs.IsDoubleClicked)
                                          {
                                              //args.Cancel = true;
                                          }
                                      }
                                  };
        }


        SetLayout ();
    }

    /// <inheritdoc/>
    protected override CheckBox CreateCheckBox (string name, int index)
    {
        var checkbox = base.CreateCheckBox (name, index);
        checkbox.RadioStyle = true;
        checkbox.MouseBindings.Remove(MouseFlags.Button1DoubleClicked);

        return checkbox;
    }

    private void UncheckOthers (CheckBox checkedCheckBox)
    {
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            if (cb == checkedCheckBox)
            {
                continue;
            }
            cb.CheckedState = CheckState.UnChecked;
        }
    }

    private void UncheckAll ()
    {
        foreach (CheckBox cb in SubViews.OfType<CheckBox> ())
        {
            cb.CheckedState = CheckState.UnChecked;
        }
    }

    /// <inheritdoc/>
    public bool EnableForDesign ()
    {
        AssignHotKeysToCheckBoxes = true;
        Options = new [] { "Option 1", "Option 2", "Option 3" };

        return true;
    }
}
