
namespace Terminal.Gui.Views;

/// <summary>
///     A <see cref="Toplevel.Modal"/> <see cref="Window"/>. Supports a simple API for adding <see cref="Button"/>s
///     across the bottom. By default, the <see cref="Dialog"/> is centered and used the <see cref="Schemes.Dialog"/>
///     scheme.
/// </summary>
/// <remarks>
///     <para>
///         To run the <see cref="Dialog"/> modally, create the <see cref="Dialog"/>, and pass it to
///         <see cref="IApplication.Run(Toplevel, Func{Exception, bool})"/>. This will execute the dialog until
///         it terminates via the <see cref="Application.QuitKey"/> (`Esc` by default),
///         or when one of the views or buttons added to the dialog calls
///         <see cref="Application.RequestStop"/>.
///     </para>
///     <para>
///         <b>Phase 2:</b> <see cref="Dialog"/> now implements <see cref="IRunnable{TResult}"/> with 
///         <c>int?</c> as the result type, returning the index of the clicked button. The <see cref="Result"/>
///         property replaces the need for manual result tracking. A result of <see langword="null"/> indicates
///         the dialog was canceled (ESC pressed, window closed without clicking a button).
///     </para>
/// </remarks>
public class Dialog : Window, IRunnable<int?>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Dialog"/> class with no <see cref="Button"/>s.
    /// </summary>
    /// <remarks>
    ///     By default, <see cref="View.X"/>, <see cref="View.Y"/>, <see cref="View.Width"/>, and <see cref="View.Height"/> are
    ///     set
    ///     such that the <see cref="Dialog"/> will be centered in, and no larger than 90% of <see cref="IApplication.TopRunnable"/>, if
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
    ///     <para>The default value is <see langword="true"/>.</para>
    ///     <para>
    ///         <b>Deprecated:</b> Use <see cref="Result"/> instead. This property is maintained for backward
    ///         compatibility. A <see langword="null"/> <see cref="Result"/> indicates the dialog was canceled.
    ///     </para>
    /// </remarks>
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
    ///     Gets or sets the result data extracted when the dialog was accepted, or <see langword="null"/> if not accepted.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Returns the zero-based index of the button that was clicked, or <see langword="null"/> if the 
    ///         dialog was canceled (ESC pressed, window closed without clicking a button).
    ///     </para>
    ///     <para>
    ///         This property is automatically set in <see cref="OnIsRunningChanging"/> when the dialog is
    ///         closing. The result is extracted by finding which button has focus when the dialog stops.
    ///     </para>
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

    #region IRunnable<int> Implementation

    /// <summary>
    ///     Called when the dialog is about to stop running. Extracts the button result before the dialog is removed
    ///     from the runnable stack.
    /// </summary>
    /// <param name="oldIsRunning">The current value of IsRunning.</param>
    /// <param name="newIsRunning">The new value of IsRunning (true = starting, false = stopping).</param>
    /// <returns><see langword="true"/> to cancel; <see langword="false"/> to proceed.</returns>
    /// <remarks>
    ///     This method is called by the IRunnable infrastructure when the dialog is stopping. It extracts
    ///     which button was clicked (if any) before views are disposed.
    /// </remarks>
    protected virtual bool OnIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        if (!newIsRunning && oldIsRunning) // Stopping
        {
            // Extract result BEFORE disposal - find which button has focus or was last clicked
            Result = null; // Default: canceled (null = no button clicked)

            for (var i = 0; i < _buttons.Count; i++)
            {
                if (_buttons [i].HasFocus)
                {
                    Result = i;
                    _canceled = false;
                    break;
                }
            }

            // If no button has focus, check if any button was the last focused view
            if (Result is null && MostFocused is Button btn && _buttons.Contains (btn))
            {
                Result = _buttons.IndexOf (btn);
                _canceled = false;
            }

            // Update legacy Canceled property for backward compatibility
            if (Result is null)
            {
                _canceled = true;
            }
        }
        else if (newIsRunning) // Starting
        {
            // Clear result when starting
            Result = null;
            _canceled = true; // Default to canceled until a button is clicked
        }

        // Call base implementation (Toplevel.IRunnable.RaiseIsRunningChanging)
        return ((IRunnable)this).RaiseIsRunningChanging (oldIsRunning, newIsRunning);
    }

    // Explicitly implement IRunnable<int> to override the behavior from Toplevel's IRunnable
    bool IRunnable.RaiseIsRunningChanging (bool oldIsRunning, bool newIsRunning)
    {
        // Call our virtual method so subclasses can override
        return OnIsRunningChanging (oldIsRunning, newIsRunning);
    }

    #endregion
}
