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
    ///     Defines the default minimum Dialog height, as a percentage of the container width. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static int DefaultMinimumHeight { get; set; } = 50;

    /// <summary>
    ///     Defines the default minimum Dialog width, as a percentage of the container width. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static int DefaultMinimumWidth { get; set; } = 50;

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

        // Set automatic width, with minimums based on content size. Also, subtract
        // Padding thickness in case the Vertical scrollbar is visible
        Width = Dim.Auto (minimumContentDim: Dim.Func (_ => GetMinimumDialogWidth ()), maximumContentDim: Dim.Percent (100) - 2)
                - Dim.Func (_ => VerticalScrollBar.Visible ? Padding!.Thickness.Horizontal : 0);
        Height = Dim.Auto (minimumContentDim: Dim.Func (_ => GetMinimumDialogHeight ()), maximumContentDim: Dim.Percent (100) - 2);

        ButtonAlignment = DefaultButtonAlignment;
        ButtonAlignmentModes = DefaultButtonAlignmentModes;

        BorderStyle = DefaultBorderStyle;
        base.ShadowStyle = DefaultShadow;

        _buttonContainer = new()
        {
            Id = "Dialog.ButtonContainer",
            CanFocus = true,
            X = Pos.Func (_ => Padding!.Thickness.Left),
            Y = Pos.AnchorEnd (),
            Width = Dim.Fill (Dim.Func (_ => Padding!.Thickness.Horizontal / 2)),
            Height = Dim.Auto (),
            SchemeName = "Menu"
        };
        Padding!.Add (_buttonContainer);

        VerticalScrollBar.AutoShow = true;
        HorizontalScrollBar.AutoShow = true;

        SetStyle ();

        AddCommand (
                    Command.Accept,
                    ctx =>
                    {
                        View? isDefaultView = _buttonContainer?.GetSubViews (includePadding: true).FirstOrDefault (v => v is Button { IsDefault: true });

                        if (isDefaultView != this && isDefaultView is Button { IsDefault: true } button)
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

    private int _naturalWidth;
    private int _naturalHeight;

    /// <inheritdoc />
    public override void EndInit ()
    {
        _naturalWidth = GetWidthRequiredForSubViews ();
        _naturalHeight = GetHeightRequiredForSubViews ();
        SetContentSize (new Size (Math.Max (_naturalButtonWidth, _naturalWidth), _naturalHeight));
        base.EndInit ();
    }

    /// <inheritdoc/>
    protected override void OnSubViewAdded (View view)
    {
        _naturalWidth = GetWidthRequiredForSubViews ();
        _naturalHeight = GetHeightRequiredForSubViews ();
        SetContentSize (new Size (Math.Max (_naturalButtonWidth, _naturalWidth), _naturalHeight));
        base.OnSubViewAdded (view);
    }

    /// <summary>
    ///     INTERNAL: Gets the minimum width required for the <see cref="Dialog"/> based on its content.
    /// </summary>
    /// <returns></returns>
    private int GetMinimumDialogWidth ()
    {
        int minSize = Math.Max (
                                Math.Max (
                                          _naturalWidth,
                                          Title.GetColumns () + 4
                                         ),
                                _naturalButtonWidth + GetAdornmentsThickness ().Horizontal
                               );

        return minSize;
    }

    private int GetMinimumDialogHeight ()
    {
        int minSize = Math.Max (
                                4,
                                _buttonContainer!.GetHeightRequiredForSubViews ()
                               );

        return minSize;
    }

    private readonly List<Button> _buttons = [];

    private int _naturalButtonWidth;

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
            // Right = Padding!.Thickness.Right + 2,
            // Left = Padding!.Thickness.Left + 2,
            // Top = Padding!.Thickness.Top + 2,
            Bottom = _buttonContainer!.GetHeightRequiredForSubViews ()
        };

        _naturalButtonWidth = _buttonContainer!.GetWidthRequiredForSubViews ();
        SetContentSize (new Size (Math.Max (_naturalButtonWidth, _naturalWidth), _naturalHeight));
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
    public bool Canceled => Result is null;

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
