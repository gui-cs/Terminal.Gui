
namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Toplevel.Modal"/> <see cref="Window"/>. Supports a simple API for adding <see cref="Button"/>s
///     across the bottom. By default, the <see cref="Dialog"/> is centered and used the <see cref="Schemes.Dialog"/>
///     scheme.
/// </summary>
/// <remarks>
///     <para>
///     To run the <see cref="Dialog"/> modally, create the <see cref="Dialog"/>, and pass it to
///     <see cref="IApplication.Run(Toplevel, Func{Exception, bool})"/>. This will execute the dialog until
///     it terminates via the <see cref="Application.QuitKey"/> (`Esc` by default),
///     or when one of the views or buttons added to the dialog calls
///     <see cref="Application.RequestStop"/>.
///     </para>
///     <para>
///     Dialog implements <see cref="IModalRunnable{TResult}"/> with <c>int?</c> as the result type.
///     The <see cref="Result"/> property contains the index of the button that was clicked, or null if canceled.
///     </para>
/// </remarks>
public class Dialog : Window, IModalRunnable<int?>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Dialog"/> class with no <see cref="Button"/>s.
    /// </summary>
    /// <remarks>
    ///     By default, <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> are
    ///     set
    ///     such that the <see cref="Dialog"/> will be centered in, and no larger than 90% of <see cref="IApplication.Current"/>, if
    ///     there is one. Otherwise,
    ///     it will be bound by the screen dimensions.
    /// </remarks>
    public Dialog ()
    {
        Arrangement = ViewArrangement.Movable | ViewArrangement.Overlapped;
        base.ShadowStyle = DefaultShadow;
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

    private readonly List<Button> _buttons = [];

    private bool _canceled;

    /// <summary>
    ///     Adds a <see cref="Button"/> to the <see cref="Dialog"/>, its layout will be controlled by the
    ///     <see cref="Dialog"/>
    /// </summary>
    /// <param name="button">Button to add.</param>
    public void AddButton (Button button)
    {
        // Use a distinct GroupId so users can use Pos.Align for other views in the Dialog
        button.X = Pos.Align (ButtonAlignment, ButtonAlignmentModes, GetHashCode ());
        button.Y = Pos.AnchorEnd ();

        _buttons.Add (button);
        Add (button);

        // Subscribe to the button's Accept command to set Result
        button.Accepting += Button_Accepting;
    }

    private void Button_Accepting (object? sender, CommandEventArgs e)
    {
        // Set Result to the index of the button that was clicked
        if (sender is Button button)
        {
            int index = _buttons.IndexOf (button);
            if (index >= 0)
            {
                Result = index;
                // For backward compatibility, set Canceled = false
                Canceled = false;
            }
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
            foreach (Button b in value)
            {
                AddButton (b);
            }
        }
    }

    /// <summary>Gets a value indicating whether the <see cref="Dialog"/> was canceled.</summary>
    /// <remarks>
    /// <para>The default value is <see langword="true"/>.</para>
    /// <para>
    /// <b>Obsolete:</b> Use <see cref="Result"/> instead. When <see cref="Result"/> is null, the dialog was canceled.
    /// This property is maintained for backward compatibility.
    /// </para>
    /// </remarks>
    [Obsolete ("Use Result property instead. Result == null indicates the dialog was canceled.")]
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

    /// <summary>
    /// Gets or sets the result of the modal dialog operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contains the zero-based index of the button that was clicked to close the dialog,
    /// or null if the dialog was canceled (e.g., ESC key pressed).
    /// </para>
    /// <para>
    /// The button index corresponds to the order buttons were added via <see cref="AddButton"/> or
    /// the <see cref="Buttons"/> initializer.
    /// </para>
    /// <para>
    /// For backward compatibility with the <see cref="Canceled"/> property:
    /// - <see cref="Result"/> == null means the dialog was canceled (<see cref="Canceled"/> == true)
    /// - <see cref="Result"/> != null means a button was clicked (<see cref="Canceled"/> == false)
    /// </para>
    /// <para>
    /// This property implements <see cref="IModalRunnable{TResult}.Result"/> where TResult is <c>int?</c>.
    /// </para>
    /// </remarks>
    public int? Result { get; set; }

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
    public static int DefaultMinimumHeight { get; set; } = 80;

    /// <summary>
    ///     Defines the default minimum Dialog width, as a percentage of the container width. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static int DefaultMinimumWidth { get; set; } = 80;

    /// <summary>
    ///     Gets or sets whether all <see cref="Window"/>s are shown with a shadow effect by default.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public new static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.Transparent;


    // Dialogs are Modal and Focus is indicated by their Border. The following code ensures the
    // Text of the dialog (e.g. for a MessageBox) is always drawn using the Normal Attribute.
    private bool _drawingText;

    /// <inheritdoc/>
    protected override bool OnDrawingText ()
    {
        _drawingText = true;
        return false;
    }

    /// <inheritdoc/>
    protected override void OnDrewText ()
    {
        _drawingText = false;
    }

    /// <inheritdoc />
    protected override bool OnGettingAttributeForRole (in VisualRole role, ref Attribute currentAttribute)
    {
        if (_drawingText && role is VisualRole.Focus && Border?.Thickness != Thickness.Empty)
        {
            currentAttribute = GetScheme ().Normal;
            return true;
        }

        return false;
    }
}
