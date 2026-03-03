namespace Terminal.Gui.Views;

/// <summary>
///     A dropdown/combo-box control that combines a <see cref="TextField"/> with a popover <see cref="ListView"/>
///     for selecting from a list of items.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="DropDownList"/> provides a modern dropdown control that can operate in two modes:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>ReadOnly mode</b> (<see cref="TextField.ReadOnly"/> = <see langword="true"/>): Acts like a traditional
///                 dropdown where clicking anywhere opens the list. The text field is not editable.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Editable mode</b> (<see cref="TextField.ReadOnly"/> = <see langword="false"/>): Acts like a combo box
///                 where the user can type text or select from the list.
///             </description>
///         </item>
///     </list>
///     <para>
///         <b>Key Features:</b>
///     </para>
///     <list type="bullet">
///         <item><description>Toggle dropdown with button click, F4, or Alt+Down</description></item>
///         <item><description>Pre-selects matching item in list when opening</description></item>
///         <item><description>Returns focus to text field when closed</description></item>
///         <item><description>Supports <see cref="IValue{T}"/> interface for data binding</description></item>
///         <item><description>Auto-registers popover on first use</description></item>
///     </list>
///     <para>
///         <b>Usage Example:</b>
///     </para>
///     <code>
///         var dropdown = new DropDownList
///         {
///             Source = new ListWrapper&lt;string&gt; (["Option 1", "Option 2", "Option 3"]),
///             ReadOnly = true,
///             Text = "Option 1"
///         };
///         dropdown.ValueChanged += (s, e) => MessageBox.Query ("Selected", dropdown.Text, "Ok");
///     </code>
/// </remarks>
public class DropDownList : TextField
{
    private Button? _toggleButton;
    private Popover<ListView, string?>? _listPopover;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DropDownList"/> class.
    /// </summary>
    public DropDownList ()
    {
        ReadOnly = true; // Default to readonly mode

        // Create toggle button with down arrow
        _toggleButton = new Button
        {
            Text = Glyphs.DownArrow.ToString (),
            X = Pos.AnchorEnd (),
            Width = 3,
            Height = 1,
            CanFocus = false,
            TabStop = TabBehavior.NoStop,
            NoPadding = true,
            NoDecorations = true
        };

#if DEBUG
        _toggleButton.Id = "dropDownListToggleButton";
#endif

        _toggleButton.Accepting += ToggleButton_Accepting;

        // Create ListView for popover
        ListView listView = new ()
        {
            Width = Dim.Auto (DimAutoStyle.Content),
            Height = Dim.Auto (DimAutoStyle.Content, minimumContentDim: 1, maximumContentDim: 10),
            CanFocus = true
        };

        // Create popover
        _listPopover = new Popover<ListView, string?> (listView)
        {
            Anchor = GetAnchor
        };

#if DEBUG
        _listPopover.Id = "dropDownListPopover";
#endif

        // Configure result extraction
        _listPopover.ResultExtractor = ExtractResult;

        // Subscribe to result changes
        _listPopover.ResultChanged += ListPopover_ResultChanged;

        // Configure commands to bubble up
        _listPopover.CommandsToBubbleUp = [Command.Activate, Command.Accept];

        // Add toggle button to Padding
        Padding!.Add (_toggleButton);

        // Adjust TextField width to account for toggle button
        Width = Dim.Fill ();

        // Add keyboard bindings
        KeyBindings.Add (Key.F4, Command.Toggle);
        KeyBindings.Add (Key.CursorDown.WithAlt, Command.Toggle);

        // Add command handler for toggle
        AddCommand (Command.Toggle, ToggleDropDown);

        // Subscribe to focus events for auto-registration
        HasFocusChanged += DropDownList_HasFocusChanged;

        Initialized += DropDownList_Initialized;
    }

    private void DropDownList_Initialized (object? sender, EventArgs e)
    {
        // Set target for command bridging
        if (_listPopover is { })
        {
            _listPopover.Target = new WeakReference<View> (this);
        }
    }

    private void DropDownList_HasFocusChanged (object? sender, HasFocusEventArgs e)
    {
        // Auto-register popover when DropDownList gains focus
        if (e.NewValue && _listPopover is { } && App?.Popovers is { })
        {
            if (!App.Popovers.IsRegistered (_listPopover))
            {
                App.Popovers.Register (_listPopover);
            }
        }
    }

    /// <summary>
    ///     Gets or sets the data source for the dropdown list.
    /// </summary>
    /// <remarks>
    ///     This property delegates to the <see cref="ListView.Source"/> property of the internal <see cref="ListView"/>.
    /// </remarks>
    public IListDataSource? Source
    {
        get => _listPopover?.ContentView?.Source;
        set
        {
            if (_listPopover?.ContentView is { })
            {
                _listPopover.ContentView.Source = value;
            }
        }
    }

    /// <summary>
    ///     Provides the anchor rectangle for positioning the popover below the DropDownList.
    /// </summary>
    private Rectangle? GetAnchor ()
    {
        Rectangle frame = FrameToScreen ();

        return new Rectangle (frame.X, frame.Y, frame.Width, frame.Height);
    }

    /// <summary>
    ///     Extracts the selected string from the ListView.
    /// </summary>
    private string? ExtractResult (ListView listView)
    {
        if (listView.SelectedItem is not { } selectedIndex || listView.Source is null)
        {
            return null;
        }

        System.Collections.IList? items = listView.Source.ToList ();

        if (items is null || selectedIndex < 0 || selectedIndex >= items.Count)
        {
            return null;
        }

        return items [selectedIndex]?.ToString ();
    }

    /// <summary>
    ///     Handles result changes from the popover.
    /// </summary>
    private void ListPopover_ResultChanged (object? sender, ValueChangedEventArgs<string?> e)
    {
        if (e.NewValue is { })
        {
            Text = e.NewValue;
        }
    }

    /// <summary>
    ///     Handles accepting event from the toggle button.
    /// </summary>
    private void ToggleButton_Accepting (object? sender, CommandEventArgs e)
    {
        ToggleDropDown ();
        e.Handled = true;
    }

    /// <summary>
    ///     Toggles the dropdown list visibility.
    /// </summary>
    private bool? ToggleDropDown ()
    {
        if (_listPopover is null)
        {
            return false;
        }

        if (_listPopover.Visible)
        {
            App?.Popovers?.Hide (_listPopover);
        }
        else
        {
            OpenDropDown ();
        }

        return true;
    }

    /// <summary>
    ///     Opens the dropdown list and pre-selects the current value.
    /// </summary>
    private void OpenDropDown ()
    {
        if (_listPopover?.ContentView is not { } listView)
        {
            return;
        }

        // Pre-select matching item
        if (!string.IsNullOrEmpty (Text) && listView.Source is { })
        {
            System.Collections.IList? items = listView.Source.ToList ();

            if (items is { })
            {
                for (var i = 0; i < items.Count; i++)
                {
                    if (items [i]?.ToString () == Text)
                    {
                        listView.SelectedItem = i;

                        break;
                    }
                }
            }
        }

        // Register if needed
        if (App?.Popovers is { } && !App.Popovers.IsRegistered (_listPopover))
        {
            App.Popovers.Register (_listPopover);
        }

        // Show the popover
        _listPopover.MakeVisible ();
    }

    /// <inheritdoc/>
    protected override void OnActivated (ICommandContext? ctx)
    {
        // If activation came from the ListView, update text
        if (_listPopover?.ContentView is { } contentView && 
            ctx?.Source?.TryGetTarget (out View? sourceView) == true &&
            sourceView == contentView && 
            _listPopover.Result is { })
        {
            Text = _listPopover.Result;
        }

        base.OnActivated (ctx);
    }

    /// <inheritdoc/>
    protected override void OnAccepted (ICommandContext? ctx)
    {
        // Accept from either button or list should close the dropdown
        if (_listPopover is { Visible: true })
        {
            App?.Popovers?.Hide (_listPopover);
        }

        base.OnAccepted (ctx);
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            if (_toggleButton is { })
            {
                _toggleButton.Accepting -= ToggleButton_Accepting;
                _toggleButton.Dispose ();
                _toggleButton = null;
            }

            if (_listPopover is { })
            {
                _listPopover.ResultChanged -= ListPopover_ResultChanged;
                App?.Popovers?.DeRegister (_listPopover);
                _listPopover.Dispose ();
                _listPopover = null;
            }

            HasFocusChanged -= DropDownList_HasFocusChanged;
            Initialized -= DropDownList_Initialized;
        }

        base.Dispose (disposing);
    }
}
