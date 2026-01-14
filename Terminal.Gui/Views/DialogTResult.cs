namespace Terminal.Gui.Views;

/// <summary>
///     A generic modal dialog window with buttons across the bottom. Derive from this class
///     to create dialogs that return custom result types.
/// </summary>
/// <typeparam name="TResult">
///     The type of result data returned when the dialog closes.
///     Since <see cref="IRunnable{TResult}.Result"/> is <c>TResult?</c>, use non-nullable types
///     (e.g., <c>int</c> not <c>int?</c>) to allow <c>null</c> to indicate cancellation.
/// </typeparam>
/// <remarks>
///     <para>
///         By default, <see cref="Dialog{TResult}"/> is centered with <see cref="Dim.Auto"/> sizing and uses the
///         <see cref="Schemes.Dialog"/> color scheme when running.
///     </para>
///     <para>
///         To run modally, pass the dialog to <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>.
///         The dialog executes until terminated by <see cref="Application.QuitKey"/> (Esc by default) or a button press.
///     </para>
///     <para>
///         Buttons are added via <see cref="AddButton"/> or the <see cref="Buttons"/> property. The last button added
///         becomes the default (<see cref="Button.IsDefault"/>). Button alignment is controlled by
///         <see cref="ButtonAlignment"/> and <see cref="ButtonAlignmentModes"/>.
///     </para>
///     <para>
///         Subclasses should set <see cref="IRunnable{TResult}.Result"/> before calling <see cref="Runnable.RequestStop"/>
///         to return a value. If Result is not set (remains <c>null</c>), the dialog is considered canceled.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // Custom dialog returning a Color
///     public class ColorDialog : Dialog&lt;Color&gt;
///     {
///         private ColorPicker _picker;
///
///         public ColorDialog ()
///         {
///             _picker = new ();
///             Add (_picker);
///             AddButton (new () { Text = "Cancel" });
///             AddButton (new () { Text = "OK" });
///         }
///
///         protected override void OnButtonPressed (int buttonIndex)
///         {
///             if (buttonIndex == 1) // OK
///             {
///                 Result = _picker.SelectedColor;
///             }
///             RequestStop ();
///         }
///     }
///     </code>
/// </example>
public class Dialog<TResult> : Runnable<TResult>, IDesignable
{
    /// <summary>
    ///     The container view that holds the dialog buttons in the Padding area.
    /// </summary>
    protected readonly View? _buttonContainer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Dialog{TResult}"/> class with no buttons.
    /// </summary>
    /// <remarks>
    ///     The dialog is positioned at <see cref="Pos.Center"/> with <see cref="Dim.Auto"/> sizing,
    ///     limited to 100% of <see cref="IApplication.TopRunnableView"/> (or screen dimensions).
    /// </remarks>
    public Dialog ()
    {
        X = Pos.Center ();
        Y = Pos.Center ();

        // Set automatic width and height, with minimums based on content size. Also, subtract
        // Padding thickness in case the scrollbar is visible
        Width = Dim.Auto (
                          minimumContentDim: Dim.Func (_ => GetMinimumDialogWidth () - (VerticalScrollBar.Visible ? 1 : 0)),
                          maximumContentDim: Dim.Percent (100) - 2);

        Height = Dim.Auto (
                           minimumContentDim: Dim.Func (_ => GetMinimumDialogHeight () - _minimumButtonsSize.Height - (HorizontalScrollBar.Visible ? 1 : 0)),
                           maximumContentDim: Dim.Percent (100) - 2);

        ButtonAlignment = Dialog.DefaultButtonAlignment;
        ButtonAlignmentModes = Dialog.DefaultButtonAlignmentModes;

        BorderStyle = Dialog.DefaultBorderStyle;
        base.ShadowStyle = Dialog.DefaultShadow;

        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);

        _buttonContainer = new ()
        {
            Id = "Dialog.ButtonContainer",
            CanFocus = true,
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Height = Dim.Auto ()
        };
        Padding!.Add (_buttonContainer);

        SetStyle ();

        AddCommand (
                    Command.Accept,
                    ctx =>
                    {
                        View? isDefaultView = _buttonContainer?.GetSubViews (includePadding: true).FirstOrDefault (v => v is Button { IsDefault: true });

                        if (isDefaultView == this || isDefaultView is not Button { IsDefault: true })
                        {
                            return RaiseAccepting (ctx);
                        }

                        bool? handled = isDefaultView.InvokeCommand (Command.Accept, ctx);

                        if (handled == true)
                        {
                            return true;
                        }

                        return RaiseAccepting (ctx);
                    });
    }

    private Size _minimumSubViewsSize;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();
        UpdateSizes ();
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        _minimumSubViewsSize = new (GetWidthRequiredForSubViews (), GetHeightRequiredForSubViews ());
        UpdateSizes ();
        base.OnSubViewAdded (view);
    }

    /// <inheritdoc/>
    protected override void OnSubViewLayout (LayoutEventArgs args)
    {
        // HACK: Ensure scrollbars are shown as needed before calculating sizes
        VerticalScrollBar.AutoShow = true;
        HorizontalScrollBar.AutoShow = true;
        UpdateSizes ();
        base.OnSubViewLayout (args);
    }

    private void UpdateSizes ()
    {
        if (SubViews.Count == 0)
        {
            // This is primarily to support MessageBox where there are no subviews but
            // Text is used.
            return;
        }

        int subViewsWidth = _minimumSubViewsSize.Width;

        if (!Width.Has<DimAuto> (out _))
        {
            subViewsWidth = Math.Max (subViewsWidth, Viewport.Width);
        }

        int subViewsHeight = _minimumSubViewsSize.Height;

        if (!Height.Has<DimAuto> (out _))
        {
            subViewsHeight = Math.Max (subViewsHeight, Viewport.Height);
        }

        SetContentSize (
                        new Size (
                                  Math.Max (_minimumButtonsSize.Width, subViewsWidth),
                                  Math.Max (_minimumButtonsSize.Height, subViewsHeight)));
    }

    /// <summary>
    ///     INTERNAL: Gets the minimum width required for the <see cref="Dialog{TResult}"/>. This takes into account
    ///     the width required for the title, the buttons, and any subviews.
    /// </summary>
    /// <returns></returns>
    private int GetMinimumDialogWidth ()
    {
        int minSize = Math.Max (
                                Math.Max (
                                          _minimumSubViewsSize.Width,

                                          // Ensure space for title + borders
                                          Title.GetColumns () + 4
                                         ),
                                _minimumButtonsSize.Width
                               );

        return minSize;
    }

    /// <summary>
    ///     INTERNAL: Gets the minimum height required for the <see cref="Dialog{TResult}"/>. This takes into account
    ///     the height required for the buttons.
    /// </summary>
    /// <returns></returns>
    private int GetMinimumDialogHeight ()
    {
        int minSize = Math.Max (
                                _minimumSubViewsSize.Height,
                                _minimumButtonsSize.Height - Border!.Thickness.Vertical - Margin!.Thickness.Vertical
                               );

        return minSize;
    }

    private readonly List<Button> _buttons = [];

    private Size _minimumButtonsSize;

    /// <summary>
    ///     Adds a <see cref="Button"/> to the bottom of the dialog.
    /// </summary>
    /// <param name="button">The button to add. Its lifetime and layout will be managed by the dialog.</param>
    /// <remarks>
    ///     <para>
    ///         Buttons are positioned according to <see cref="ButtonAlignment"/> and <see cref="ButtonAlignmentModes"/>.
    ///         The last button added becomes the default (<see cref="Button.IsDefault"/> = <see langword="true"/>).
    ///     </para>
    ///     <para>
    ///         When a button is pressed, <see cref="OnButtonPressed"/> is called with the button's index
    ///         (0-based, in order of addition). Subclasses can override this to set <see cref="IRunnable{TResult}.Result"/>.
    ///     </para>
    /// </remarks>
    public void AddButton (Button button)
    {
        // Use a distinct GroupId so users can use Pos.Align for other views in the Dialog
        button.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
        button.Y = 1;

        _buttons.Add (button);

        foreach (Button dialogButton in _buttons)
        {
            dialogButton.IsDefault = false;
            dialogButton.Accepting -= OnDialogButtonOnAccepting;
            dialogButton.Accepting += OnDialogButtonOnAccepting;
        }

        button.IsDefault = true;

        _buttonContainer?.Add (button);
        Padding!.Thickness = Padding!.Thickness with { Bottom = _buttonContainer!.GetHeightRequiredForSubViews () };
        _minimumButtonsSize = new (_buttonContainer?.GetWidthRequiredForSubViews () ?? 0, _buttonContainer?.GetHeightRequiredForSubViews () ?? 0);
    }

#pragma warning disable TGUI001
    /// <summary>
    ///     Handles button acceptance. Calls <see cref="OnButtonPressed"/> with the button's index.
    /// </summary>
    private void OnDialogButtonOnAccepting (object? s, CommandEventArgs e)
    {
        if (e.Handled || !IsRunning)
        {
            return;
        }

        if (s is Button { IsDefault: true })
        {
            e.Handled = RaiseAccepting (e.Context) is true;

            if (e.Handled)
            {
                return;
            }
        }

        e.Handled = IsRunning;
        int buttonIndex = _buttonContainer!.SubViews.IndexOf (s);
        OnButtonPressed (buttonIndex);
    }
#pragma warning restore

    /// <summary>
    ///     Called when a dialog button is pressed. Override this method to set <see cref="IRunnable{TResult}.Result"/>
    ///     based on which button was pressed.
    /// </summary>
    /// <param name="buttonIndex">The 0-based index of the button that was pressed (in order of addition).</param>
    /// <remarks>
    ///     <para>
    ///         The default implementation calls <see cref="Runnable.RequestStop"/> without setting a result,
    ///         which means the dialog is considered canceled. Subclasses should override this to set
    ///         <see cref="IRunnable{TResult}.Result"/> before calling <see cref="Runnable.RequestStop"/>.
    ///     </para>
    ///     <para>
    ///         The non-generic <see cref="Dialog"/> class overrides this to set Result to the button index.
    ///     </para>
    /// </remarks>
    protected virtual void OnButtonPressed (int buttonIndex) { RequestStop (); }

    /// <summary>
    ///     Determines how buttons are aligned horizontally at the bottom of the dialog.
    /// </summary>
    /// <remarks>
    ///     Default is <see cref="Dialog.DefaultButtonAlignment"/> (typically <see cref="Alignment.End"/>).
    /// </remarks>
    public Alignment ButtonAlignment
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;
            foreach (Button dialogButton in _buttons)
            {
                dialogButton.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
            }
        }
    }

    /// <summary>
    ///     Controls button spacing and alignment behavior.
    /// </summary>
    /// <remarks>
    ///     Default is <see cref="Dialog.DefaultButtonAlignmentModes"/> (typically
    ///     <see cref="AlignmentModes.StartToEnd"/> | <see cref="AlignmentModes.AddSpaceBetweenItems"/>).
    /// </remarks>
    public AlignmentModes ButtonAlignmentModes
    {
        get;
        set
        {
            if (field == value)
            {
                return;
            }
            field = value;
            foreach (Button dialogButton in _buttons)
            {
                dialogButton.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
            }
            UpdateSizes ();
        }
    }

    /// <summary>
    ///     Gets or sets the buttons displayed at the bottom of the dialog.
    /// </summary>
    /// <remarks>
    ///     Setting this property calls <see cref="AddButton"/> for each button in the array.
    ///     Getting returns a copy of the internal button list.
    /// </remarks>
    public Button [] Buttons
    {
        get => _buttons.ToArray ();
        set
        {
            foreach (Button b in value)
            {
                AddButton (b);
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnIsRunningChanged (bool newIsModal)
    {
        if (newIsModal)
        {
            SetStyle ();
        }

        base.OnIsRunningChanged (newIsModal);
    }

    private void SetStyle ()
    {
        if (IsRunning)
        {
            //Padding!.SetScheme (SchemeManager.GetScheme (Schemes.Base));
            Arrangement |= ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped;
        }
        else
        {
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);

            // strip out movable and resizable
            Arrangement &= ~(ViewArrangement.Movable | ViewArrangement.Resizable);
        }
    }

    // Dialogs are Modal and Focus is indicated by their Border. The following code ensures the
    // Text of the dialog (e.g. for a MessageBox) is always drawn using the Normal Attribute
    // instead of the Focus attribute.
    private bool _drawingText;

    /// <inheritdoc/>
    protected override bool OnDrawingText ()
    {
        _drawingText = true;

        return false;
    }

    /// <inheritdoc/>
    protected override void OnDrewText () { _drawingText = false; }

    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (!IsRunning)
        {
            return false;
        }

        if (!_drawingText || role is not VisualRole.Focus || Border?.Thickness == Thickness.Empty)
        {
            return false;
        }

        currentAttribute = GetScheme ().Normal;

        return true;
    }

    bool IDesignable.EnableForDesign ()
    {
        Title = "Dialog Title";

        Button btnCancel = new ()
        {
            Id = "btnCancel",
            Title = Strings.btnCancel
        };

        btnCancel.Accepting += (s, e) =>
                               {
                                   if (!IsRunning)
                                   {
                                       return;
                                   }

                                   (s as View)!.App?.RequestStop ();
                                   e.Handled = true;
                               };

        AddButton (btnCancel);

        AddButton (
                   new ()
                   {
                       Id = "btnOk",
                       Title = Strings.btnOk

                       // Dialog will automatically set IsDefault to the last button added
                   });

        // Add some example content to the dialog
        Label infoLabel = new ()
        {
            Id = "infoLabel",
            Text = "_Example:"
        };

        TextField info = new ()
        {
            Id = "info",
            X = Pos.Right (infoLabel) + 1,
            Y = Pos.Top (infoLabel),
            Text = "Type and press ENTER to accept.",
            Width = 40
        };
        Add (infoLabel, info);

        return true;
    }
}
