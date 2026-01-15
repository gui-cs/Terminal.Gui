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
///                     <see cref="Prompt{TView,TResult}"/>
///                     on <see cref="IRunnable"/> - For C# with type-safe result extraction and hosting relationship
///                 </description>
///             </item>
///             <item>
///                 <description>
///                     <see cref="Prompt{TView}(IApplication,TView)"/>
///                     on <see cref="IApplication"/> - For scripting languages, returns string?.
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
    /// <param name="view">The view to display in the dialog. If not specified, ...</param>
    /// <param name="resultExtractor">Function that extracts the result from the view when the user accepts.</param>
    /// <param name="input">Optional initial value to pass to the view (for future use with Input property).</param>
    /// <param name="beginInitHandler">Optional callback to customize the dialog before it is displayed.</param>
    /// <returns>
    ///     The extracted result if the user accepted, or <see langword="null"/> if the user canceled.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if <paramref name="host"/> does not have an associated <see cref="IApplication"/>.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The <paramref name="host"/> parameter captures the "hosting relationship" between the caller
    ///         and the prompt. Currently, this is used only to obtain the <see cref="IApplication"/> instance.
    ///         In the future, this will enable prompts to be positioned relative to their host.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    ///     // From within a Window or other Runnable:
    ///     DateTime? date = this.Prompt&lt;DatePicker, DateTime&gt;(
    ///         view: new DatePicker { Date = new DateTime (1966, 9, 10) },
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
        TView? view = null,
        Func<TView, TResult?>? resultExtractor = null,
        TResult? input = default,
        Action<Prompt<TView, TResult>>? beginInitHandler = null)
        where TView : View, new ()
    {
        ArgumentNullException.ThrowIfNull (host);

        IApplication app = (host as View)?.App ?? throw new InvalidOperationException ("Host runnable must have an associated IApplication.");

        using Prompt<TView, TResult> prompt = new (view)
        {
            ResultExtractor = resultExtractor
        };

        // TODO: We need to add a InitBegun event to View that is raised by BeginInit
        // TODO: but for now Initialized will work, but is suboptimal.
        prompt.Initialized += (sender, args) =>
                              {
                                  // Allow customization before initialization
                                  beginInitHandler?.Invoke (prompt);
                              };


        // TODO: In the future, we will add IValue<TResult> that Views can implement to
        // TODO: to have a typed Value property that Input_set and Result_get will use.
        // TODO: This will mean resultExtractor will be optional and only for Views that
        // TODO: don't support IValue<TResult>.

        // TODO: In the future, store host for relative positioning

        app.Run (prompt);

        return prompt.Result;
    }

    /// <summary>
    ///     Shows a view in a modal dialog with Ok/Cancel buttons. For scripting languages.
    /// </summary>
    /// <typeparam name="TView">The type of view to display.</typeparam>
    /// <param name="app">The application instance.</param>
    /// <param name="view">The view to display in the dialog.</param>
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
    public static string? Prompt<TView> (
        this IApplication app,
        TView view)
        where TView : View, new ()
    {
        ArgumentNullException.ThrowIfNull (app);
        ArgumentNullException.ThrowIfNull (view);

        using Prompt<TView, string?> dialog = new (view)
        {
            ResultExtractor = _ => view.Text
        };

        return app.Run (dialog) as string;
    }
}
