namespace Terminal.Gui.Views;

/// <summary>
///     Provides static methods for prompting the user with any view wrapped in a dialog with Ok and Cancel buttons.
/// </summary>
/// <remarks>
///     <para>
///         <see cref="Prompt"/> enables any view to be shown in a modal dialog with standard Ok/Cancel buttons,
///         similar to <see cref="MessageBox"/> but for interactive input views like <see cref="TextField"/>,
///         <see cref="ColorPicker"/>, <see cref="DatePicker"/>, etc.
///     </para>
///     <para>
///         When the user clicks Ok or presses Enter, the result extractor function is called to get the result
///         from the view. When Cancel is clicked or Esc is pressed, <see langword="null"/> is returned.
///     </para>
///     <para>
///         This class provides a simpler alternative to manually creating a <see cref="Dialog"/>, adding buttons,
///         and handling the result extraction.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// // Prompt for text input
/// string? name = Prompt.Show(
///     app,
///     "Enter Name",
///     new TextField { Width = 30 },
///     tf => tf.Text);
/// 
/// if (name is { })
/// {
///     Console.WriteLine($"Hello, {name}!");
/// }
/// 
/// // Prompt for color selection
/// Color? color = Prompt.Show(
///     app,
///     "Pick a Color",
///     new ColorPicker(),
///     cp => cp.SelectedColor);
/// 
/// if (color.HasValue)
/// {
///     Console.WriteLine($"Selected: {color.Value}");
/// }
/// 
/// // Prompt for date selection
/// DateTime? date = Prompt.Show(
///     app,
///     "Select Date",
///     new DatePicker(),
///     dp => dp.Date);
/// </code>
/// </example>
public static class Prompt
{
    private static LineStyle _defaultBorderStyle = LineStyle.Single;
    private static Alignment _defaultButtonAlignment = Alignment.End;

    /// <summary>
    ///     Gets or sets the default border style for prompt dialogs.
    /// </summary>
    /// <remarks>
    ///     This property can be configured via <see cref="ConfigurationManager"/> and theme files.
    ///     Defaults to <see cref="LineStyle.Single"/>.
    /// </remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle
    {
        get => _defaultBorderStyle;
        set => _defaultBorderStyle = value;
    }

    /// <summary>
    ///     Gets or sets the default button alignment for prompt dialogs.
    /// </summary>
    /// <remarks>
    ///     This property can be configured via <see cref="ConfigurationManager"/> and theme files.
    ///     Defaults to <see cref="Alignment.End"/>.
    /// </remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Alignment DefaultButtonAlignment
    {
        get => _defaultButtonAlignment;
        set => _defaultButtonAlignment = value;
    }

    /// <summary>
    ///     Shows a modal dialog with the specified view and Ok/Cancel buttons, returning the extracted result.
    /// </summary>
    /// <typeparam name="TView">The type of view to display.</typeparam>
    /// <typeparam name="TResult">The type of result to extract from the view.</typeparam>
    /// <param name="app">The application instance. Cannot be <see langword="null"/>.</param>
    /// <param name="title">The title to display in the dialog.</param>
    /// <param name="view">The view to display in the dialog. Cannot be <see langword="null"/>.</param>
    /// <param name="resultExtractor">
    ///     Function that extracts the result from the view when Ok is clicked.
    ///     Cannot be <see langword="null"/>.
    /// </param>
    /// <param name="okButtonText">Optional text for the Ok button. Defaults to "Ok".</param>
    /// <param name="cancelButtonText">Optional text for the Cancel button. Defaults to "Cancel".</param>
    /// <returns>
    ///     The extracted result if the user clicked Ok, or <see langword="null"/> if Cancel was clicked
    ///     or <see cref="Application.QuitKey"/> was pressed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="app"/>, <paramref name="view"/>, or <paramref name="resultExtractor"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The dialog is automatically sized to fit the view and buttons, centered on the screen.
    ///         The view is automatically disposed when the dialog closes.
    ///     </para>
    ///     <para>
    ///         The result extractor is only called if the user clicks Ok. If Cancel is clicked or Esc is pressed,
    ///         <see langword="null"/> is returned without calling the extractor.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Prompt for a file path
    /// string? path = Prompt.Show(
    ///     app,
    ///     "Enter File Path",
    ///     new TextField { Width = 50 },
    ///     tf => tf.Text);
    /// 
    /// if (path is { })
    /// {
    ///     File.WriteAllText(path, "Hello, World!");
    /// }
    /// </code>
    /// </example>
    public static TResult? Show<TView, TResult> (
        IApplication app,
        string title,
        TView view,
        Func<TView, TResult?> resultExtractor,
        string okButtonText = "Ok",
        string cancelButtonText = "Cancel")
        where TView : View
    {
        ArgumentNullException.ThrowIfNull (app);
        ArgumentNullException.ThrowIfNull (view);
        ArgumentNullException.ThrowIfNull (resultExtractor);

        Dialog dialog = new ()
        {
            Title = title,
            BorderStyle = DefaultBorderStyle,
            ButtonAlignment = DefaultButtonAlignment
        };

        Button btnCancel = new () { Text = cancelButtonText };
        Button btnOk = new () { Text = okButtonText, IsDefault = true };

        dialog.Add (view);
        dialog.AddButton (btnCancel);
        dialog.AddButton (btnOk);

        int? buttonIndex = app.Run (dialog) as int?;
        dialog.Dispose ();

        // Check if OK was pressed (index 1, since Cancel is added first)
        if (buttonIndex == 1)
        {
            return resultExtractor (view);
        }

        return default;
    }

    /// <summary>
    ///     Shows a modal dialog with the specified view and Ok/Cancel buttons, without result extraction.
    /// </summary>
    /// <typeparam name="TView">The type of view to display.</typeparam>
    /// <param name="app">The application instance. Cannot be <see langword="null"/>.</param>
    /// <param name="title">The title to display in the dialog.</param>
    /// <param name="view">The view to display in the dialog. Cannot be <see langword="null"/>.</param>
    /// <param name="okButtonText">Optional text for the Ok button. Defaults to "Ok".</param>
    /// <param name="cancelButtonText">Optional text for the Cancel button. Defaults to "Cancel".</param>
    /// <returns>
    ///     <see langword="true"/> if the user clicked Ok, <see langword="false"/> if Cancel was clicked
    ///     or <see cref="Application.QuitKey"/> was pressed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="app"/> or <paramref name="view"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         The dialog is automatically sized to fit the view and buttons, centered on the screen.
    ///         The view is NOT automatically disposed; the caller is responsible for disposal if needed.
    ///     </para>
    ///     <para>
    ///         Use this overload when you want to access the view's state directly after the dialog closes
    ///         rather than extracting a specific result.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var textField = new TextField { Width = 50 };
    /// bool accepted = Prompt.Show(app, "Enter Name", textField);
    /// 
    /// if (accepted)
    /// {
    ///     Console.WriteLine($"Name: {textField.Text}");
    /// }
    /// </code>
    /// </example>
    public static bool Show<TView> (
        IApplication app,
        string title,
        TView view,
        string okButtonText = "Ok",
        string cancelButtonText = "Cancel")
        where TView : View
    {
        ArgumentNullException.ThrowIfNull (app);
        ArgumentNullException.ThrowIfNull (view);

        Dialog dialog = new ()
        {
            Title = title,
            BorderStyle = DefaultBorderStyle,
            ButtonAlignment = DefaultButtonAlignment
        };

        Button btnCancel = new () { Text = cancelButtonText };
        Button btnOk = new () { Text = okButtonText, IsDefault = true };

        dialog.Add (view);
        dialog.AddButton (btnCancel);
        dialog.AddButton (btnOk);

        int? buttonIndex = app.Run (dialog) as int?;
        dialog.Dispose ();

        // Check if OK was pressed (index 1, since Cancel is added first)
        return buttonIndex == 1;
    }
}
