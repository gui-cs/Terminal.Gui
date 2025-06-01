namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Toplevel.Modal"/> <see cref="Window"/>. Supports a simple API for adding <see cref="Button"/>s
///     across the bottom. By default, the <see cref="Dialog"/> is centered and used the <see cref="Schemes.Dialog"/>
///     scheme.
/// </summary>
/// <remarks>
///     To run the <see cref="Dialog"/> modally, create the <see cref="Dialog"/>, and pass it to
///     <see cref="Application.Run(Toplevel, Func{Exception, bool})"/>. This will execute the dialog until
///     it terminates via the <see cref="Application.QuitKey"/> (`Esc` by default),
///     or when one of the views or buttons added to the dialog calls
///     <see cref="Application.RequestStop"/>.
/// </remarks>
public class Dialog : Window
{
    /// <summary>The default <see cref="Alignment"/> for <see cref="Dialog"/>.</summary>
    /// <remarks>This property can be set in a Theme.</remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Alignment DefaultButtonAlignment { get; set; } = Alignment.End;

    /// <summary>The default <see cref="AlignmentModes"/> for <see cref="Dialog"/>.</summary>
    /// <remarks>This property can be set in a Theme.</remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static AlignmentModes DefaultButtonAlignmentModes { get; set; } = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems;

    /// <summary>
    ///     Defines the default minimum Dialog width, as a percentage of the container width. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static int DefaultMinimumWidth { get; set; } = 80;

    /// <summary>
    ///     Defines the default minimum Dialog height, as a percentage of the container width. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static int DefaultMinimumHeight { get; set; } = 80;

    /// <summary>
    ///     Gets or sets whether all <see cref="Window"/>s are shown with a shadow effect by default.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public new static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.Transparent;

    /// <summary>
    ///     Defines the default border styling for <see cref="Dialog"/>. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>

    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public new static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Heavy;

    private readonly List<Button> _buttons = new ();

    /// <summary>
    ///     Initializes a new instance of the <see cref="Dialog"/> class with no <see cref="Button"/>s.
    /// </summary>
    /// <remarks>
    ///     By default, <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> are
    ///     set
    ///     such that the <see cref="Dialog"/> will be centered in, and no larger than 90% of <see cref="Application.Top"/>, if
    ///     there is one. Otherwise,
    ///     it will be bound by the screen dimensions.
    /// </remarks>
    public Dialog ()
    {
        Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped;
        ShadowStyle = DefaultShadow;
        BorderStyle = DefaultBorderStyle;

        X = Pos.Center ();
        Y = Pos.Center ();
        Width = Dim.Auto (DimAutoStyle.Auto, Dim.Percent (DefaultMinimumWidth), Dim.Percent (90));
        Height = Dim.Auto (DimAutoStyle.Auto, Dim.Percent (DefaultMinimumHeight), Dim.Percent (90));

        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);

        Modal = true;
        ButtonAlignment = DefaultButtonAlignment;
        ButtonAlignmentModes = DefaultButtonAlignmentModes;
    }

    // BUGBUG: We override GetNormal/FocusColor because "Dialog" Scheme is goofy.
    // BUGBUG: By defn, a Dialog is Modal, and thus HasFocus is always true. OnDrawContent
    // BUGBUG: Calls these methods.
    // TODO: Fix this in https://github.com/gui-cs/Terminal.Gui/issues/2381

    /// <inheritdoc/>
    /// <inheritdoc/>
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (role == VisualRole.Normal || role == VisualRole.Focus)
        {
            currentAttribute = GetScheme ().Normal;

            return true;
        }

        return base.OnGettingAttributeForRole (role, ref currentAttribute);
    }

    private bool _canceled;

    /// <summary>Gets a value indicating whether the <see cref="Dialog"/> was canceled.</summary>
    /// <remarks>The default value is <see langword="true"/>.</remarks>
    public bool Canceled
    {
        get
        {
#if DEBUG_IDISPOSABLE
            if (EnableDebugIDisposableAsserts && WasDisposed)
            {
                throw new ObjectDisposedException (GetType ().FullName);
            }
#endif
            return _canceled;
        }
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
            if (value is null)
            {
                return;
            }

            foreach (Button b in value)
            {
                AddButton (b);
            }
        }
    }

    /// <summary>
    ///     Adds a <see cref="Button"/> to the <see cref="Dialog"/>, its layout will be controlled by the
    ///     <see cref="Dialog"/>
    /// </summary>
    /// <param name="button">Button to add.</param>
    public void AddButton (Button button)
    {
        if (button is null)
        {
            return;
        }

        // Use a distinct GroupId so users can use Pos.Align for other views in the Dialog
        button.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
        button.Y = Pos.AnchorEnd ();

        _buttons.Add (button);
        Add (button);
    }
}
