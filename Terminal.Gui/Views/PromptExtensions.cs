namespace Terminal.Gui.Views;

/// <summary>
///     Extension methods for prompting users with views in modal dialogs.
/// </summary>
/// <remarks>
///     <para>
///         These extensions provide convenient ways to show any <see cref="View"/> in a modal dialog
///         with Ok/Cancel buttons and get a typed result.
///     </para>
///     <para>
///         Two API styles are provided:
///         <list type="bullet">
///             <item>
///                 <description>
///                     <see cref="Prompt{TView,TResult}(IRunnable,string,TView,Func{TView,TResult},TResult,Action{Prompt{TView,TResult}}?)"/>
///                     on <see cref="IRunnable"/> - For C# with type-safe result extraction and hosting relationship
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="Prompt{TView}(IApplication,string,TView,string,string)"/>
///                     on <see cref="IApplication"/> - For scripting languages, returns bool
///                 </description>
///             </item>
///         </list>
///     </para>
/// </remarks>
public static class PromptExtensions
{
    /// <summary>
    ///     Shows a view in a modal dialog with Ok/Cancel buttons and extracts a typed result.
    /// </summary>
    /// <typeparam name="TView">The type of view to display.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of result data returned when the session completes.
    ///     <para>
    ///         <strong>Important:</strong> Use nullable types (e.g., <c>Color?</c>, <c>int?</c>, <c>string?</c>)
    ///         so that <see langword="null"/> can indicate cancellation. Using non-nullable value types
    ///         (e.g., <c>Color</c>, <c>int</c>) will return their default values on cancellation, making
    ///         it impossible to distinguish cancellation from a valid result.
    ///     </para>
    /// </typeparam>
    /// <param name="host">
    ///     The runnable that is "hosting" this prompt. Currently used only to get the <see cref="IApplication"/>.
    ///     In the future, this will enable positioning the prompt relative to the host.
    /// </param>
    /// <param name="title">The title displayed in the dialog's title bar.</param>
    /// <param name="view">The view to display in the dialog.</param>
    /// <param name="resultExtractor">Function that extracts the result from the view when the user accepts.</param>
    /// <param name="input">Optional initial value to pass to the view (for future use with Input property).</param>
    /// <param name="beginInitHandler">Optional callback to customize the dialog before it is displayed.</param>
    /// <returns>
    ///     The extracted result if the user accepted, or <see langword="null"/> if the user canceled.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="host"/>, <paramref name="view"/>, or <paramref name="resultExtractor"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <paramref name="host"/> does not have an associated <see cref="IApplication"/>.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The <paramref name="host"/> parameter captures the "hosting relationship" between the caller
    ///         and the prompt. Currently this is used only to obtain the <see cref="IApplication"/> instance.
    ///         In the future, this will enable prompts to be positioned relative to their host.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // From within a Window or other Runnable:
    ///     DateTime? date = this.Prompt&lt;DatePicker, DateTime&gt;(
    ///         title: "Select Date",
    ///         view: new DatePicker { Date = DateTime.Now },
    ///         resultExtractor: dp => dp.Date);
    ///
    ///     if (date is not null)
    ///     {
    ///         // User accepted
    ///     }
    ///     </code>
    /// </example>
    public static TResult? Prompt<TView, TResult> (
        this IRunnable host,
        string title,
        TView view,
        Func<TView, TResult?> resultExtractor,
        TResult? input = default,
        Action<Prompt<TView, TResult>>? beginInitHandler = null)
        where TView : View
    {
        ArgumentNullException.ThrowIfNull (host);
        ArgumentNullException.ThrowIfNull (view);
        ArgumentNullException.ThrowIfNull (resultExtractor);

        IApplication app = (host as View)?.App ?? throw new InvalidOperationException ("Host runnable must have an associated IApplication.");

        using Prompt<TView, TResult> dialog = new ()
        {
            Title = title,
            WrappedView = view,
            ResultExtractor = resultExtractor
        };

        // Allow customization before initialization
        beginInitHandler?.Invoke (dialog);

        // TODO: In the future, use input to initialize the view if it supports it
        // TODO: In the future, store host for relative positioning

        app.Run (dialog);

        return dialog.Result;
    }

    /// <summary>
    ///     Shows a view in a modal dialog with Ok/Cancel buttons and creates it automatically.
    /// </summary>
    /// <typeparam name="TView">The type of view to display. Must have a parameterless constructor.</typeparam>
    /// <typeparam name="TResult">
    ///     The type of result data returned when the session completes.
    ///     <para>
    ///         <strong>Important:</strong> Use nullable types (e.g., <c>Color?</c>, <c>int?</c>, <c>string?</c>)
    ///         so that <see langword="null"/> can indicate cancellation. Using non-nullable value types
    ///         (e.g., <c>Color</c>, <c>int</c>) will return their default values on cancellation, making
    ///         it impossible to distinguish cancellation from a valid result.
    ///     </para>
    /// </typeparam>
    /// <param name="host">
    ///     The runnable that is "hosting" this prompt. Currently used only to get the <see cref="IApplication"/>.
    /// </param>
    /// <param name="title">The title displayed in the dialog's title bar.</param>
    /// <param name="resultExtractor">Function that extracts the result from the view when the user accepts.</param>
    /// <param name="input">Optional initial value to pass to the view (for future use with Input property).</param>
    /// <param name="beginInitHandler">Optional callback to customize the dialog before it is displayed.</param>
    /// <returns>
    ///     The extracted result if the user accepted, or <see langword="null"/> if the user canceled.
    /// </returns>
    /// <example>
    ///     <code>
    ///     // Creates a new DatePicker automatically
    ///     DateTime? date = mainWindow.Prompt&lt;DatePicker, DateTime&gt;(
    ///         title: "Select Date",
    ///         resultExtractor: dp => dp.Date);
    ///     </code>
    /// </example>
    public static TResult? Prompt<TView, TResult> (
        this IRunnable host,
        string title,
        Func<TView, TResult?> resultExtractor,
        TResult? input = default,
        Action<Prompt<TView, TResult>>? beginInitHandler = null)
        where TView : View, new()
    {
        return host.Prompt (title, new TView (), resultExtractor, input, beginInitHandler);
    }

    /// <summary>
    ///     Shows a view in a modal dialog with Ok/Cancel buttons. For scripting languages.
    /// </summary>
    /// <typeparam name="TView">The type of view to display.</typeparam>
    /// <param name="app">The application instance.</param>
    /// <param name="title">The title displayed in the dialog's title bar.</param>
    /// <param name="view">The view to display in the dialog.</param>
    /// <param name="okButtonText">Text for the Ok button. Default is "Ok".</param>
    /// <param name="cancelButtonText">Text for the Cancel button. Default is "Cancel".</param>
    /// <returns>
    ///     <see langword="true"/> if the user accepted (clicked Ok), <see langword="false"/> if canceled.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="app"/> or <paramref name="view"/> is null.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This overload is designed for scripting languages (PowerShell, F#, etc.) where
    ///         <c>Func&lt;TView, TResult?&gt;</c> delegates are difficult to use.
    ///     </para>
    ///     <para>
    ///         Access the view's properties directly after this method returns to get the result.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // PowerShell usage:
    ///     // $datePicker = [DatePicker]::new()
    ///     // $accepted = $app.Prompt("Select Date", $datePicker)
    ///     // if ($accepted) { $datePicker.Date }
    ///
    ///     // C# usage:
    ///     DatePicker datePicker = new ();
    ///     bool accepted = app.Prompt ("Select Date", datePicker);
    ///     if (accepted)
    ///     {
    ///         Console.WriteLine (datePicker.Date);
    ///     }
    ///     </code>
    /// </example>
    public static bool Prompt<TView> (
        this IApplication app,
        string title,
        TView view,
        string okButtonText = "Ok",
        string cancelButtonText = "Cancel")
        where TView : View
    {
        ArgumentNullException.ThrowIfNull (app);
        ArgumentNullException.ThrowIfNull (view);

        using Prompt<TView, bool> dialog = new ()
        {
            Title = title,
            WrappedView = view,
            OkButtonText = okButtonText,
            CancelButtonText = cancelButtonText,
            ResultExtractor = _ => true // Set Result to true when Ok is pressed
        };

        app.Run (dialog);

        // If Result is true, user accepted; otherwise (null/default) user canceled
        return dialog.Result;
    }
}
