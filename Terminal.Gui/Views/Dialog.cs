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

        AddCommand (Command.QuitToplevel, () =>
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

    /// <summary>Determines how the <see cref="Dialog"/> <see cref="Button"/>s are aligned along the bottom of the dialog.</summary>
    public Justification ButtonAlignment { get; set; }

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

    /// <summary>The default <see cref="Justification"/> for <see cref="Dialog"/>.</summary>
    /// <remarks>This property can be set in a Theme.</remarks>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    [JsonConverter (typeof (JsonStringEnumConverter))]
    public static Justification DefaultButtonAlignment { get; set; } = Justification.Centered;

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

        //button.AutoSize = false; // BUGBUG: v2 - Hack to get around autosize not accounting for Margin?
        _buttons.Add (button);
        Add (button);

        SetNeedsDisplay ();

        if (IsInitialized)
        {
            LayoutSubviews ();
        }
    }

    /// <inheritdoc/>
    public override void LayoutSubviews ()
    {
        if (_inLayout)
        {
            return;
        }

        _inLayout = true;
        LayoutButtons ();
        base.LayoutSubviews ();
        _inLayout = false;
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

    private void LayoutButtons ()
    {
        if (_buttons.Count == 0 || !IsInitialized)
        {
            return;
        }

        var shiftLeft = 0;

        int buttonsWidth = GetButtonsWidth ();

        switch (ButtonAlignment)
        {
            case Justification.Centered:
                // Center Buttons
                shiftLeft = (Viewport.Width - buttonsWidth - _buttons.Count - 1) / 2 + 1;

                for (int i = _buttons.Count - 1; i >= 0; i--)
                {
                    Button button = _buttons [i];
                    shiftLeft += button.Frame.Width + (i == _buttons.Count - 1 ? 0 : 1);

                    if (shiftLeft > -1)
                    {
                        button.X = Pos.AnchorEnd (shiftLeft);
                    }
                    else
                    {
                        button.X = Viewport.Width - shiftLeft;
                    }

                    button.Y = Pos.AnchorEnd (1);
                }

                break;

            case Justification.Justified:
                // Justify Buttons
                // leftmost and rightmost buttons are hard against edges. The rest are evenly spaced.

                var spacing = (int)Math.Ceiling ((double)(Viewport.Width - buttonsWidth) / (_buttons.Count - 1));

                for (int i = _buttons.Count - 1; i >= 0; i--)
                {
                    Button button = _buttons [i];

                    if (i == _buttons.Count - 1)
                    {
                        shiftLeft += button.Frame.Width;
                        button.X = Pos.AnchorEnd (shiftLeft);
                    }
                    else
                    {
                        if (i == 0)
                        {
                            // first (leftmost) button 
                            int left = Viewport.Width;
                            button.X = Pos.AnchorEnd (left);
                        }
                        else
                        {
                            shiftLeft += button.Frame.Width + spacing;
                            button.X = Pos.AnchorEnd (shiftLeft);
                        }
                    }

                    button.Y = Pos.AnchorEnd (1);
                }

                break;

            case Justification.Left:
                // Left Align Buttons
                Button prevButton = _buttons [0];
                prevButton.X = 0;
                prevButton.Y = Pos.AnchorEnd (1);

                for (var i = 1; i < _buttons.Count; i++)
                {
                    Button button = _buttons [i];
                    button.X = Pos.Right (prevButton) + 1;
                    button.Y = Pos.AnchorEnd (1);
                    prevButton = button;
                }

                break;

            case Justification.Right:
                // Right align buttons
                shiftLeft = _buttons [_buttons.Count - 1].Frame.Width;
                _buttons [_buttons.Count - 1].X = Pos.AnchorEnd (shiftLeft);
                _buttons [_buttons.Count - 1].Y = Pos.AnchorEnd (1);

                for (int i = _buttons.Count - 2; i >= 0; i--)
                {
                    Button button = _buttons [i];
                    shiftLeft += button.Frame.Width + 1;
                    button.X = Pos.AnchorEnd (shiftLeft);
                    button.Y = Pos.AnchorEnd (1);
                }

                break;
        }
    }
}
