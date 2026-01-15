namespace Terminal.Gui.Views;

/// <summary>
///     A dialog that wraps any <see cref="View"/> with Ok/Cancel buttons, extracting a typed result
///     when the user accepts.
/// </summary>
/// <typeparam name="TView">The type of view being wrapped.</typeparam>
/// <typeparam name="TResult">
///     The type of result data returned when the session completes.
///     <para>
///         <strong>Important:</strong> Use nullable types (e.g., <c>Color?</c>, <c>int?</c>, <c>string?</c>)
///         so that <see langword="null"/> can indicate cancellation. Using non-nullable value types
///         (e.g., <c>Color</c>, <c>int</c>) will return their default values on cancellation, making
///         it impossible to distinguish cancellation from a valid result.
///     </para>
/// </typeparam>
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
///         Use the <see cref="PromptExtensions.Prompt{TView,TResult}"/>
///         extension method for a more convenient API.
///     </para>
/// </remarks>
/// <example>
///     <code>
///     // Create a prompt dialog with a DatePicker
///     DatePicker datePicker = new () { Date = new DateTime (1966, 9, 10) };
///     Prompt&lt;DatePicker, DateTime&gt; prompt = new ()
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
public class Prompt<TView, TResult> : Dialog<TResult> where TView : View, new()
{
    private readonly TView? _wrappedView;

    /// <summary>
    ///     Initializes a new instance of <see cref="Prompt{TView, TResult}"/>.
    /// </summary>
    /// <param name="wrappedView">
    ///     The view to wrap. If null, a new instance of TView is created.
    ///     Requires TView to have a parameterless constructor if null.
    /// </param>
    public Prompt (TView? wrappedView = null)
    {
        _wrappedView = wrappedView ?? new TView ();
        AddSubViews ();
    }

    /// <summary>
    /// 
    /// </summary>
    public Prompt ()
    {
        _wrappedView ??= new TView ();
        AddSubViews ();
    }

    private void AddSubViews ()
    {
        Add (_wrappedView);
        // Add default Ok and Cancel buttons
        // Note, users can override these by changing them in an
        // initialization event (e.g. Initialized).
        AddButton (new () { Text = Strings.btnCancel });
        AddButton (new () { Text = Strings.btnOk });
    }

    /// <summary>
    ///     Gets the wrapped view that is displayed in the dialog.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The wrapped view will be added as a subview during construction.
    ///     </para>
    /// </remarks>
    public TView? GetWrappedView ()
    {
        return _wrappedView;
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

        if (ResultExtractor is { } && _wrappedView is {})
        {
            Result = ResultExtractor (_wrappedView);
        }

        return false;
    }
}
