namespace Terminal.Gui.Views;

/// <summary>
///     Supports a simple API for adding <see cref="Button"/>s
///     across the bottom. By default, the <see cref="Dialog"/> is centered and used the <see cref="Schemes.Dialog"/>
///     scheme.
/// </summary>
/// <remarks>
///     To run the <see cref="Dialog"/> modally, create the <see cref="Dialog"/>, and pass it to
///     <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>. This will execute the dialog until
///     it terminates via the <see cref="Application.QuitKey"/> (`Esc` by default),
///     or when one of the views or buttons added to the dialog calls
///     <see cref="IApplication.RequestStop()"/>.
/// </remarks>
public class Dialog : Window, IDesignable
{
    /// <summary>
    ///     Defines the default border styling for <see cref="Dialog"/>. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public new static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Heavy;

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
    public new static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.Transparent;

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
        Width = Dim.Auto (minimumContentDim: Dim.Func (_ => Math.Max (Dim.Percent (DefaultMinimumWidth).GetAnchor(GetContainerSize().Width), GetWidthRequiredForSubViews ())), maximumContentDim: Dim.Percent (90));
        Height = Dim.Auto (minimumContentDim: Dim.Func (_ => Math.Max (Dim.Percent (DefaultMinimumHeight).GetAnchor (GetContainerSize ().Height), GetHeightRequiredForSubViews ())), maximumContentDim: Dim.Percent (90));

        ButtonAlignment = DefaultButtonAlignment;
        ButtonAlignmentModes = DefaultButtonAlignmentModes;

        BorderStyle = DefaultBorderStyle;
        base.ShadowStyle = DefaultShadow;

        SetStyle ();
    }

    private readonly List<Button> _buttons = [];

    private bool _canceled;

    /// <summary>
    ///     Adds a <see cref="Button"/> to the bottom of the <see cref="Dialog"/>. The lifetime and layout will be controlled by the
    ///     <see cref="Dialog"/>. The added buttons will be aligned according to the <see cref="ButtonAlignment"/> and
    ///     <see cref="ButtonAlignmentModes"/>. The last button to be added will be the right-most button and will be treated as
    ///     the default (<see cref="Button.IsDefault"/> will be set to true).
    /// </summary>
    /// <param name="button">Button to add.</param>
    public void AddButton (Button button)
    {
        // Use a distinct GroupId so users can use Pos.Align for other views in the Dialog
        button.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
        button.Y = Pos.AnchorEnd ();

        _buttons.Add (button);

        foreach (Button b in _buttons)
        {
            b.IsDefault = false;
        }
        button.IsDefault = true;

        // Subscribe to FrameChanged to update padding dynamically
        button.FrameChanged += ButtonFrameChanged;

        Padding!.Add (button);
    }

    private void ButtonFrameChanged (object? sender, EventArgs e) { UpdatePaddingBottom (); }

    // TODO: Update button.X = Pos.Justify when alignment changes
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
        init
        {
            foreach (Button b in value)
            {
                AddButton (b);
            }
        }
    }

    /// <summary>Gets a value indicating whether the <see cref="Dialog"/> was canceled.</summary>
    /// <remarks>The default value is <see langword="true"/>.</remarks>
    public bool Canceled
    {
        get { return _canceled; }
        set
        {
#if DEBUG_IDISPOSABLE
            if (EnableDebugIDisposableAsserts && WasDisposed)
            {
                throw new ObjectDisposedException (GetType ().FullName);
            }
#endif
            _canceled = value;
        }
    }

    /// <inheritdoc/>
    protected override void OnIsRunningChanged (bool newIsModal)
    {
        SetStyle ();

        base.OnIsRunningChanged (newIsModal);
    }

    private void SetStyle ()
    {
        if (IsRunning)
        {
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
            Padding?.SetScheme (SchemeManager.GetScheme (Schemes.Base));
            Arrangement |= ViewArrangement.Movable | ViewArrangement.Resizable | ViewArrangement.Overlapped;
        }
        else
        {
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
            Padding?.SetScheme (SchemeManager.GetScheme (Schemes.Dialog));

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

    private void UpdatePaddingBottom ()
    {
        if (Padding is null || _buttons.Count == 0)
        {
            return;
        }

        // Find the maximum button height
        var maxHeight = 1; // Default to minimum height of 1 for buttons (assuming no shadow)

        foreach (Button button in _buttons)
        {
            if (button.Frame.Height > maxHeight)
            {
                maxHeight = button.Frame.Height;
            }
        }

        // Set the bottom padding to match button height
        // Update padding if buttons have been laid out (maxHeight > 1)
        if (maxHeight > 1 || Padding.Thickness.Bottom == 0)
        {
           Padding.Thickness = Padding.Thickness with { Bottom = maxHeight };
        }
    }

    bool IDesignable.EnableForDesign ()
    {
        Title = "Dialog Title";

        Button btnCancel = new ()
        {
            Title = Strings.btnCancel,
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

        AddButton (new ()
        {
            Title = Strings.btnOk,
            // Dialog will automatically set IsDefault to the last button added
        });

        // Add some example content to the dialog
        Label infoLabel = new ()
        {
            Text = "_Example:"
        };
        TextField info = new ()
        {
            X = Pos.Right (infoLabel) + 1,
            Y = Pos.Top (infoLabel),
            Text = "Type and press ENTER to accept.",
            Width = 40
        };
        Add (infoLabel, info);

        return true;
    }

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            // Unsubscribe from button events to prevent memory leaks
            foreach (Button button in _buttons)
            {
                button.FrameChanged -= ButtonFrameChanged;
            }
        }

        base.Dispose (disposing);
    }
}
