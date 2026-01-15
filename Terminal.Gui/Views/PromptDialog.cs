namespace Terminal.Gui.Views;

/// <summary>
///     A dialog that wraps any <see cref="View"/> with Ok/Cancel buttons, extracting a typed result
///     when the user accepts.
/// </summary>
/// <typeparam name="TView">The type of view being wrapped.</typeparam>
/// <typeparam name="TResult">The type of result data returned when the user accepts.</typeparam>
/// <remarks>
///     <para>
///         This class provides a convenient way to prompt the user with any view and get a typed result.
///         The wrapped view is displayed in the dialog, and when the user clicks Ok (or presses Enter),
///         the <see cref="ResultExtractor"/> function is called to extract the result from the view.
///     </para>
///     <para>
///         If the user cancels (clicks Cancel, presses Escape, or closes the dialog),
///         <see cref="IRunnable{TResult}.Result"/> remains <see langword="null"/>.
///     </para>
///     <para>
///         Use the <see cref="PromptExtensions.Prompt{TView,TResult}(IRunnable,string,TView,Func{TView,TResult},TResult,Action{PromptDialog{TView,TResult}}?)"/>
///         extension method for a more convenient API.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // Create a prompt dialog with a DatePicker
///     DatePicker datePicker = new () { Date = DateTime.Now };
///     PromptDialog&lt;DatePicker, DateTime&gt; prompt = new ()
///     {
///         Title = "Select Date",
///         WrappedView = datePicker,
///         ResultExtractor = dp => dp.Date
///     };
///
///     if (app.Run (prompt) is DateTime selectedDate)
///     {
///         Console.WriteLine ($"Selected: {selectedDate}");
///     }
///     else
///     {
///         Console.WriteLine ("Canceled");
///     }
///     </code>
/// </example>
public class PromptDialog<TView, TResult> : Dialog<TResult> where TView : View
{
    private TView? _wrappedView;

    /// <summary>
    ///     Initializes a new instance of <see cref="PromptDialog{TView, TResult}"/>.
    /// </summary>
    public PromptDialog ()
    {
        // Add default Ok and Cancel buttons
        AddButton (new () { Text = Strings.btnCancel });
        AddButton (new () { Text = Strings.btnOk });
    }

    /// <summary>
    ///     Gets or sets the wrapped view that is displayed in the dialog.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property must be set before the dialog is initialized.
    ///         The wrapped view will be added as a subview during <see cref="EndInit"/>.
    ///     </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if the property is set after initialization.</exception>
    public required TView WrappedView
    {
        get => _wrappedView ?? throw new InvalidOperationException ("WrappedView must be set before use.");
        init
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException ("WrappedView cannot be changed after initialization.");
            }

            _wrappedView = value;
        }
    }

    /// <summary>
    ///     Gets or sets the function that extracts the result from the wrapped view.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This function is called when the user accepts the dialog (clicks Ok or presses Enter).
    ///         The return value is stored in <see cref="IRunnable{TResult}.Result"/>.
    ///     </para>
    ///     <para>
    ///         If this property is <see langword="null"/>, <see cref="IRunnable{TResult}.Result"/>
    ///         will remain <see langword="null"/> even when the user accepts.
    ///     </para>
    /// </remarks>
    public Func<TView, TResult?>? ResultExtractor { get; init; }

    /// <summary>
    ///     Gets or sets the text displayed on the Ok button.
    /// </summary>
    /// <remarks>
    ///     Default is the localized "Ok" string from <see cref="Strings.btnOk"/>.
    ///     Set this before initialization to customize the button text.
    /// </remarks>
    public string OkButtonText { get; init; } = Strings.btnOk;

    /// <summary>
    ///     Gets or sets the text displayed on the Cancel button.
    /// </summary>
    /// <remarks>
    ///     Default is the localized "Cancel" string from <see cref="Strings.btnCancel"/>.
    ///     Set this before initialization to customize the button text.
    /// </remarks>
    public string CancelButtonText { get; init; } = Strings.btnCancel;

    /// <inheritdoc/>
    public override void EndInit ()
    {
        // Update button text if customized
        if (Buttons.Length >= 2)
        {
            Buttons [0].Text = CancelButtonText;
            Buttons [1].Text = OkButtonText;
        }

        // Add the wrapped view as a subview
        if (_wrappedView is { })
        {
            Add (_wrappedView);
        }

        base.EndInit ();
    }

    /// <inheritdoc/>
    /// <remarks>
    ///     When the Ok button (the default button) is pressed, extracts the result using <see cref="ResultExtractor"/>
    ///     and stores it in <see cref="IRunnable{TResult}.Result"/> before closing.
    ///     When Cancel is pressed, closes without setting a result.
    /// </remarks>
    protected override bool OnAccepting (CommandEventArgs args)
    {
        if (base.OnAccepting (args))
        {
            return true;
        }

        // Only extract result if the default button (Ok) was pressed
        Button? defaultButton = Buttons.FirstOrDefault (b => b.IsDefault);

        if (args.Context?.Source == defaultButton && ResultExtractor is { } && _wrappedView is { })
        {
            Result = ResultExtractor (_wrappedView);
        }

        return false;
    }
}
