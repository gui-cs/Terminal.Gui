namespace Terminal.Gui.Views;

/// <summary>
///     Provides a modal dialog window with buttons across the bottom. When accepted, the <see cref="IRunnable.Result"/>
///     will be the index of the button pressed.
///     By default, the <see cref="Dialog"/> is centered and used the <see cref="Schemes.Dialog"/>
///     scheme.
/// </summary>
/// <remarks>
///     To run the <see cref="Dialog"/> modally, create the <see cref="Dialog"/>, and pass it to
///     <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>. This will execute the dialog until
///     it terminates via the <see cref="Application.QuitKey"/> (`Esc` by default),
///     or when one of its buttons is pressed, which sets the <see cref="IRunnable.Result"/> to the index of the button
///     pressed.
/// </remarks>
public class Dialog : Runnable<int?>, IDesignable
{
    /// <summary>
    ///     Defines the default border styling for <see cref="Dialog"/>. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Heavy;

    /// <summary>The default <see cref="Alignment"/> for <see cref="Dialog"/>.</summary>
    /// <remarks>This property can be set in a Theme.</remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Alignment DefaultButtonAlignment { get; set; } = Alignment.End;

    /// <summary>The default <see cref="AlignmentModes"/> for <see cref="Dialog"/>.</summary>
    /// <remarks>This property can be set in a Theme.</remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static AlignmentModes DefaultButtonAlignmentModes { get; set; } = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems;

    /// <summary>
    ///     Gets or sets whether all <see cref="Window"/>s are shown with a shadow effect by default.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.Transparent;

    private readonly View? _buttonContainer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Dialog"/> class with no <see cref="Button"/>s.
    /// </summary>
    /// <remarks>
    ///     By default, the <see cref="Dialog"/> will be centered in, and no larger than 90% of
    ///     <see cref="IApplication.TopRunnableView"/>, if there is one. Otherwise,
    ///     it will be bound by the screen dimensions.
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

        ButtonAlignment = DefaultButtonAlignment;
        ButtonAlignmentModes = DefaultButtonAlignmentModes;

        BorderStyle = DefaultBorderStyle;
        base.ShadowStyle = DefaultShadow;

        _buttonContainer = new ()
        {
            Id = "Dialog.ButtonContainer",
            CanFocus = true,
            X = 0,
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (),
            Height = Dim.Auto (),
            SchemeName = "Menu"
        };
        Padding!.Add (_buttonContainer);

        SetStyle ();

        AddCommand (
                    Command.Accept,
                    ctx =>
                    {
                        View? isDefaultView = _buttonContainer?.GetSubViews (includePadding: true).FirstOrDefault (v => v is Button { IsDefault: true });

                        if (isDefaultView != this && isDefaultView is Button { IsDefault: true })
                        {
                            bool? handled = isDefaultView.InvokeCommand (Command.Accept, ctx);

                            if (handled == true)
                            {
                                return true;
                            }
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
    ///     INTERNAL: Gets the minimum width required for the <see cref="Dialog"/>. This takes into account
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
    ///     INTERNAL: Gets the minimum height required for the <see cref="Dialog"/>. This takes into account
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
    ///     Adds a <see cref="Button"/> to the bottom of the <see cref="Dialog"/>. The lifetime and layout will be controlled
    ///     by the
    ///     <see cref="Dialog"/>. The added buttons will be aligned according to the <see cref="ButtonAlignment"/> and
    ///     <see cref="ButtonAlignmentModes"/>. The last button to be added will be the right-most button and will be treated
    ///     as
    ///     the default (<see cref="Button.IsDefault"/> will be set to true).
    /// </summary>
    /// <param name="button">Button to add.</param>
    public void AddButton (Button button)
    {
        // Use a distinct GroupId so users can use Pos.Align for other views in the Dialog
        button.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
        button.Y = 1;

        _buttons.Add (button);

        foreach (Button dialogButton in _buttons)
        {
            dialogButton.IsDefault = false;
            dialogButton.Accepting += OnDialogButtonOnAccepting;
        }

        button.IsDefault = true;

        _buttonContainer?.Add (button);

        Padding!.Thickness = Padding!.Thickness with
        {
            // Add 3 to padding just for testing
            //Right = Padding!.Thickness.Right + 2,
            //Left = Padding!.Thickness.Left + 2,
            //Top = Padding!.Thickness.Top + 2,
            Bottom = _buttonContainer!.GetHeightRequiredForSubViews ()
        };

        _minimumButtonsSize = new (_buttonContainer?.GetWidthRequiredForSubViews () ?? 0, _buttonContainer?.GetHeightRequiredForSubViews () ?? 0);
    }

    private void OnDialogButtonOnAccepting (object? s, CommandEventArgs e)
    {
        if (e.Handled || !IsRunning)
        {
            return;
        }

        e.Handled = IsRunning;
        Result = _buttonContainer!.SubViews.IndexOf (s);
        RequestStop ();
    }

    /// <summary>Determines how the <see cref="Dialog"/> <see cref="Button"/>s are aligned along the bottom of the dialog.</summary>
    public Alignment ButtonAlignment { get; set; }

    /// <summary>
    ///     Gets or sets the alignment modes for the dialog's buttons.
    /// </summary>
    public AlignmentModes ButtonAlignmentModes { get; set; }

    /// <summary>Optional buttons to lay out at the bottom of the dialog.</summary>
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

    /// <summary>Gets a value indicating whether the <see cref="Dialog"/> was canceled.</summary>
    public bool Canceled => Result is null or 1; // Cancel button is index 1

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
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
            Padding!.SetScheme (SchemeManager.GetScheme (Schemes.Base));
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
