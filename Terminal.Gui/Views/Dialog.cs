using System.Text.Json.Serialization;

namespace Terminal.Gui;

/// <summary>
///     The <see cref="Dialog"/> <see cref="View"/> is a <see cref="Window"/> that by default is centered and contains
///     one or more <see cref="Button"/>s. It defaults to the <c>Colors.ColorSchemes ["Dialog"]</c> color scheme and has a
///     1 cell padding around the edges.
/// </summary>
/// <remarks>
///     To run the <see cref="Dialog"/> modally, create the <see cref="Dialog"/>, and pass it to
///     <see cref="Application.Run(Toplevel, Func{Exception, bool}, ConsoleDriver)"/>. This will execute the dialog until it terminates via the
///     [ESC] or [CTRL-Q] key, or when one of the views or buttons added to the dialog calls
///     <see cref="Application.RequestStop"/>.
/// </remarks>
public class Dialog : Window
{

    // TODO: Reenable once border/borderframe design is settled
    /// <summary>
    ///     Defines the default border styling for <see cref="Dialog"/>. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>

    //[SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    //public static Border DefaultBorder { get; set; } = new Border () {
    //	LineStyle = LineStyle.Single,
    //};
    private readonly List<Button> _buttons = new ();

    private bool _inLayout;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Dialog"/> class using <see cref="LayoutStyle.Computed"/>
    ///     positioning with no <see cref="Button"/>s.
    /// </summary>
    /// <remarks>
    ///     By default, <see cref="View.X"/> and <see cref="View.Y"/> are set to <c>Pos.Center ()</c> and
    ///     <see cref="View.Width"/> and <see cref="View.Height"/> are set to <c>Width = Dim.Percent (85)</c>, centering the
    ///     Dialog vertically and horizontally.
    /// </remarks>
    public Dialog ()
    {
        Arrangement = ViewArrangement.Movable;
        X = Pos.Center ();
        Y = Pos.Center ();
        ValidatePosDim = true;

        Width = Dim.Percent (85);
        Height = Dim.Percent (85);
        ColorScheme = Colors.ColorSchemes ["Dialog"];

        Modal = true;
        ButtonAlignment = DefaultButtonAlignment;

        AddCommand (
                    Command.QuitToplevel,
                    () =>
                    {
                        Canceled = true;
                        RequestStop ();

                        return true;
                    });
        KeyBindings.Add (Key.Esc, Command.QuitToplevel);
    }


    private bool _canceled;

    /// <summary>Gets a value indicating whether the <see cref="Dialog"/> was canceled.</summary>
    /// <remarks>The default value is <see langword="true"/>.</remarks>
    public bool Canceled
    {
        get
        {
#if DEBUG_IDISPOSABLE
            if (WasDisposed)
            {
                throw new ObjectDisposedException (GetType ().FullName);
            }
#endif
            return _canceled;
        }
        set
        {
#if DEBUG_IDISPOSABLE
            if (WasDisposed)
            {
                throw new ObjectDisposedException (GetType ().FullName);
            }
#endif
            _canceled = value;

            return;
        }
    }

    // TODO: Update button.X = Pos.Justify when alignment changes
    /// <summary>Determines how the <see cref="Dialog"/> <see cref="Button"/>s are aligned along the bottom of the dialog.</summary>
    public Alignment ButtonAlignment { get; set; }

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

    /// <summary>The default <see cref="Alignment"/> for <see cref="Dialog"/>.</summary>
    /// <remarks>This property can be set in a Theme.</remarks>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    [JsonConverter (typeof (JsonStringEnumConverter))]
    public static Alignment DefaultButtonAlignment { get; set; } = Alignment.Right;

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

        button.X = Pos.Align (ButtonAlignment);
        button.Y = Pos.AnchorEnd () - 1;

        _buttons.Add (button);
        Add (button);

        SetNeedsDisplay ();

        if (IsInitialized)
        {
            LayoutSubviews ();
        }
    }

    // Get the width of all buttons, not including any Margin.
    internal int GetButtonsWidth ()
    {
        if (_buttons.Count == 0)
        {
            return 0;
        }

        //var widths = buttons.Select (b => b.TextFormatter.GetFormattedSize ().Width + b.BorderFrame.Thickness.Horizontal + b.Padding.Thickness.Horizontal);
        IEnumerable<int> widths = _buttons.Select (b => b.Frame.Width);

        return widths.Sum ();
    }
}
