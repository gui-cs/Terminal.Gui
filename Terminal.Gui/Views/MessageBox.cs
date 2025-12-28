using System.Runtime.CompilerServices;

namespace Terminal.Gui.Views;

/// <summary>
///     Displays a modal message box with a title, message, and buttons. Returns the index of the selected button,
///     or <see langword="null"/> if the user cancels with <see cref="Application.QuitKey"/>.
/// </summary>
/// <remarks>
///     <para>
///         MessageBox provides static methods for displaying modal dialogs with customizable buttons and messages.
///         All methods return <see langword="int?"/> where the value is the 0-based index of the button pressed,
///         or <see langword="null"/> if the user pressed <see cref="Application.QuitKey"/> (typically Esc).
///     </para>
///     <para>
///         <see cref="Query(IApplication, string, string, string[])"/> uses the default Dialog color scheme.
///         <see cref="ErrorQuery(IApplication, string, string, string[])"/> uses the Error color scheme.
///     </para>
///     <para>
///         <b>Important:</b> All MessageBox methods require an <see cref="IApplication"/> instance to be passed.
///         This enables proper modal dialog management and respects the application's lifecycle. Pass your
///         application instance (from <see cref="Application.Create"/>) or use the legacy
///         <see cref="Application.Instance"/> if using the static Application pattern.
///     </para>
///     <para>
///         Example using instance-based pattern:
///         <code>
///     IApplication app = Application.Create();
///     app.Init();
///     
///     int? result = MessageBox.Query(app, "Quit Demo", "Are you sure you want to quit?", "_No", "_Yes");
///     if (result == 1) // User clicked "Yes"
///         app.RequestStop();
///     else if (result == null) // User pressed Esc
///         // Handle cancellation
///         
///     app.Shutdown();
///     </code>
///     </para>
///     <para>
///         Example using legacy static pattern:
///         <code>
///     Application.Init();
///     
///     int? result = MessageBox.Query(ApplicationImpl.Instance, "Quit Demo", "Are you sure?", "_No", "_Yes");
///     if (result == 1) // User clicked "Yes"
///         Application.RequestStop();
///     
///     Application.Shutdown();
///     </code>
///     </para>
/// </remarks>
public static class MessageBox
{
    private static LineStyle _defaultBorderStyle = LineStyle.Heavy; // Resources/config.json overrides
    private static Alignment _defaultButtonAlignment = Alignment.Center; // Resources/config.json overrides
    private static int _defaultMinimumWidth = 15; // Resources/config.json overrides
    private static int _defaultMinimumHeight = 10; // Resources/config.json overrides

    /// <summary>
    ///     Defines the default border styling for <see cref="MessageBox"/>. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle
    {
        get => _defaultBorderStyle;
        set => _defaultBorderStyle = value;
    }

    /// <summary>The default <see cref="Alignment"/> for <see cref="Dialog"/>.</summary>
    /// <remarks>This property can be set in a Theme.</remarks>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static Alignment DefaultButtonAlignment
    {
        get => _defaultButtonAlignment;
        set => _defaultButtonAlignment = value;
    }

    /// <summary>
    ///     Defines the default minimum MessageBox width, as a percentage of the screen width. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static int DefaultMinimumWidth
    {
        get => _defaultMinimumWidth;
        set => _defaultMinimumWidth = value;
    }

    /// <summary>
    ///     Defines the default minimum Dialog height, as a percentage of the screen height. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [ConfigurationProperty (Scope = typeof (ThemeScope))]
    public static int DefaultMinimumHeight
    {
        get => _defaultMinimumHeight;
        set => _defaultMinimumHeight = value;
    }

    /// <summary>
    ///     Displays an auto-sized error <see cref="MessageBox"/>.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines and will be word-wrapped.</param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.QuitKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons.
    /// </remarks>
    public static int? ErrorQuery (IApplication app, string title, string message, params string [] buttons)
    {
        return QueryFull (
                          app,
                          true,
                          title,
                          message,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays an auto-sized error <see cref="MessageBox"/> with a default button and word-wrap control.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button (0-based).</param>
    /// <param name="wrapMessage">
    ///     If <see langword="true"/>, word-wraps the message; otherwise displays as-is with multi-line
    ///     support.
    /// </param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.QuitKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons.
    /// </remarks>
    public static int? ErrorQuery (
        IApplication app,
        string title,
        string message,
        int defaultButton = 0,
        bool wrapMessage = true,
        params string [] buttons
    )
    {
        return QueryFull (
                          app,
                          true,
                          title,
                          message,
                          wrapMessage,
                          buttons);
    }

    /// <summary>
    ///     Displays a <see cref="MessageBox"/> with fixed dimensions.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="width">Width for the MessageBox.</param>
    /// <param name="height">Height for the MessageBox.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines and will be word-wrapped.</param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.QuitKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     Consider using <see cref="Query(IApplication, string, string, string[])"/> which automatically sizes the
    ///     MessageBox.
    /// </remarks>
    public static int? Query (IApplication app, int width, int height, string title, string message, params string [] buttons)
    {
        return QueryFull (
                          app,
                          false,
                          title,
                          message,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays an auto-sized <see cref="MessageBox"/>.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines and will be word-wrapped.</param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.QuitKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons.
    /// </remarks>
    public static int? Query (IApplication app, string title, string message, params string [] buttons)
    {
        return QueryFull (
                          app,
                          false,
                          title,
                          message,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays an auto-sized <see cref="MessageBox"/> with a default button.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines and will be word-wrapped.</param>
    /// <param name="defaultButton">Index of the default button (0-based).</param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.QuitKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons.
    /// </remarks>
    public static int? Query (IApplication app, string title, string message, int defaultButton = 0, params string [] buttons)
    {
        return QueryFull (
                          app,
                          false,
                          title,
                          message,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays an auto-sized <see cref="MessageBox"/> with a default button and word-wrap control.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="title">Title for the MessageBox.</param>
    /// <param name="message">Message to display. May contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button (0-based).</param>
    /// <param name="wrapMessage">
    ///     If <see langword="true"/>, word-wraps the message; otherwise displays as-is with multi-line
    ///     support.
    /// </param>
    /// <param name="buttons">Array of button labels.</param>
    /// <returns>
    ///     The index of the selected button, or <see langword="null"/> if the user pressed
    ///     <see cref="Application.QuitKey"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The MessageBox is centered and auto-sized based on title, message, and buttons.
    /// </remarks>
    public static int? Query (
        IApplication app,
        string title,
        string message,
        int defaultButton = 0,
        bool wrapMessage = true,
        params string [] buttons
    )
    {
        return QueryFull (
                          app,
                          false,
                          title,
                          message,
                          wrapMessage,
                          buttons);
    }

    private static int? QueryFull (
        IApplication app,
        bool useErrorScheme,
        string title,
        string message,
        bool wrapMessage = true,
        params string [] buttons
    )
    {
        List<Button> buttonList = [];
        buttonList.AddRange (buttons.Select (buttonText => new Button { Text = buttonText, }));

        using Dialog dialog = new ();
        dialog.Title = title;
        dialog.ButtonAlignment = DefaultButtonAlignment;
        dialog.ButtonAlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems;
        dialog.BorderStyle = DefaultBorderStyle;

        //dialog.SetMinimumWidthFunc (GetMinimumMessageBoxWidth);
        //dialog.SetMinimumHeightFunc (GetMinimumMessageBoxHeight);

        dialog.SchemeName = useErrorScheme ? SchemeManager.SchemesToSchemeName (Schemes.Error) : SchemeManager.SchemesToSchemeName (Schemes.Dialog);

        dialog.HotKeySpecifier = new ('\xFFFF');
        dialog.Text = message;
        dialog.TextAlignment = Alignment.Center;
        dialog.VerticalTextAlignment = Alignment.Start;
        dialog.TextFormatter.WordWrap = wrapMessage;
        dialog.TextFormatter.MultiLine = !wrapMessage;

        dialog.Buttons = buttonList.ToArray ();

        Button? defaultButton = dialog.Padding!.SubViews.OfType<Button> ().FirstOrDefault (b => b.IsDefault);
        defaultButton?.SetFocus ();
        // Run the modal
        int? result = app.Run (dialog) as int?;

        return result;

        //int GetMinimumMessageBoxWidth ()
        //{
        //    int minSize = Math.Max (
        //                            Dim.Percent (DefaultMinimumWidth).GetAnchor (dialog.GetContainerSize ().Width) - dialog.GetAdornmentsThickness ().Horizontal,
        //                            Dim.Auto ().Calculate (0, dialog.Padding!.GetContainerSize ().Width, dialog.Padding, Dimension.Width)
        //                            - dialog.GetAdornmentsThickness ().Horizontal);

        //    return minSize;
        //}

        //int GetMinimumMessageBoxHeight ()
        //{
        //    int minSize = Math.Max (
        //                            Dim.Percent (DefaultMinimumHeight).GetAnchor (dialog.GetContainerSize ().Height) - dialog.GetAdornmentsThickness ().Vertical,
        //                            Dim.Auto ().Calculate (0, dialog.Padding!.GetContainerSize ().Height, dialog, Dimension.Height)
        //                            - dialog.GetAdornmentsThickness ().Vertical);
        //    return minSize;
        //}
    }
}
