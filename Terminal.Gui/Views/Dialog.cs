namespace Terminal.Gui.Views;

/// <summary>
///     A modal dialog window with buttons across the bottom. When a button is pressed,
///     <see cref="IRunnable{TResult}.Result"/>
///     is set to the button's index (0-based).
/// </summary>
/// <remarks>
///     <para>
///         This is the standard dialog class for simple button-index-based results. For dialogs that need to return
///         custom result types, derive from <see cref="Dialog{TResult}"/> instead.
///     </para>
///     <para>
///         By default, <see cref="Dialog"/> is centered with <see cref="Dim.Auto"/> sizing and uses the
///         <see cref="Schemes.Dialog"/> color scheme when running.
///     </para>
///     <para>
///         To run modally, pass the dialog to <see cref="IApplication.Run(IRunnable, Func{Exception, bool})"/>.
///         The dialog executes until terminated by <see cref="Application.QuitKey"/> (Esc by default),
///         a press of one of the <see cref="Dialog{TResult}.Buttons"/>, or if any subview receives the <see cref="Command.Accept"/>
///         command
///         and does not handle it.
///     </para>
///     <para>
///         Buttons are added via <see cref="Dialog{TResult}.AddButton"/> or the <see cref="Dialog{TResult}.Buttons"/> property. The last button added
///         becomes the default (<see cref="Button.IsDefault"/>). Button alignment is controlled by
///         <see cref="Dialog{TResult}.ButtonAlignment"/> and <see cref="Dialog{TResult}.ButtonAlignmentModes"/>.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     Dialog dialog = new () { Title = "Confirm" };
/// 
///     dialog.AddButton (new () { Title = "_Cancel" });
///     dialog.AddButton (new () { Title = "_Ok" });
/// 
///     Label label = new () { Text = "Are you sure?" };
///     dialog.Add (label);
/// 
///     Application.Run (dialog);
/// 
///     if (dialog.Result == 1) // OK button (second button, index 1)
///     {
///         // User clicked OK
///     }
///     </code>
/// </example>
public class Dialog : Dialog<int>
{
    /// <summary>
    ///     The default border style for new <see cref="Dialog"/> instances. Can be configured via
    ///     <see cref="ConfigurationManager"/> and theme files.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Heavy;

    /// <summary>
    ///     The default button alignment for new <see cref="Dialog"/> instances. Can be configured via theme files.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Alignment DefaultButtonAlignment { get; set; } = Alignment.End;

    /// <summary>
    ///     The default button alignment modes for new <see cref="Dialog"/> instances. Can be configured via theme files.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static AlignmentModes DefaultButtonAlignmentModes { get; set; } = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems;

    /// <summary>
    ///     The default shadow style for new <see cref="Dialog"/> instances. Can be configured via theme files.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static ShadowStyle DefaultShadow { get; set; } = ShadowStyle.Transparent;

    /// <summary>
    ///     Helper property that gets whether the dialog was canceled (Result is <see langword="null"/> or 1).
    /// </summary>
    /// <remarks>
    ///     Returns <see langword="true"/> if <see cref="Result"/> is <see langword="null"/>
    ///     (user pressed Escape or closed the dialog without pressing a button) or if any button
    ///     other than the last one was pressed.
    /// </remarks>
    public bool Canceled => Result is null || Result != Buttons.Length - 1;

    /// <summary>
    ///     Gets or sets the result of the dialog, indicating which button was pressed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The value is the zero-based index of the button that was pressed, or <see langword="null"/>
    ///         if the dialog was dismissed without a button press (e.g., via Escape key).
    ///     </para>
    ///     <para>
    ///         This property shadows the base <see cref="IRunnable{TResult}.Result"/> property to provide
    ///         explicit <c>int?</c> nullability for backward compatibility.
    ///     </para>
    /// </remarks>
    public new int? Result
    {
        get => ((IRunnable)this).Result is int value ? value : null;
        set
        {
            if (value > Buttons.Length - 1 || value < 0)
            {
                throw new ArgumentOutOfRangeException (nameof (value), @"Result value must be a valid button index or null.");
            }

            ((IRunnable)this).Result = value;
        }
    }

    /// <summary>
    ///     Overrides the default Accepting behavior to handle Dialog Button presses.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        if (args.Context?.Source?.TryGetTarget (out View? sourceView) != true || sourceView is not Button sourceButton)
        {
            return false;
        }

        if (!Buttons.Contains (sourceButton))
        {
            return false;
        }

        int buttonIndex = Buttons.IndexOf (sourceButton);
        Result = buttonIndex != -1 ? buttonIndex : Buttons.IndexOf (Buttons.FirstOrDefault (b => b.IsDefault));
        args.Handled = true;
        RequestStop ();
        return true;
    }
}
