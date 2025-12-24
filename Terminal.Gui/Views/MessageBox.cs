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
///     int? result = MessageBox.Query(app, "Quit Demo", "Are you sure you want to quit?", "Yes", "No");
///     if (result == 0) // User clicked "Yes"
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
///     int? result = MessageBox.Query(ApplicationImpl.Instance, "Quit Demo", "Are you sure?", "Yes", "No");
///     if (result == 0) // User clicked "Yes"
///         Application.RequestStop();
///     
///     Application.Shutdown();
///     </code>
///     </para>
///     <para>
///         The <see cref="Clicked"/> property provides a global variable alternative for web-based consoles
///         without SynchronizationContext. However, using the return value is preferred as it's more thread-safe
///         and follows modern async patterns.
///     </para>
/// </remarks>
public static class MessageBox
{
    private static LineStyle _defaultBorderStyle = LineStyle.Heavy; // Resources/config.json overrides
    private static Alignment _defaultButtonAlignment = Alignment.Center; // Resources/config.json overrides
    private static int _defaultMinimumWidth = 10; // Resources/config.json overrides
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
    ///     The index of the selected button, or <see langword="null"/> if the user pressed <see cref="Application.QuitKey"/>.
    /// </summary>
    /// <remarks>
    ///     This global variable is useful for web-based consoles without a SynchronizationContext or TaskScheduler.
    ///     Warning: Not thread-safe.
    /// </remarks>
    public static int? Clicked { get; private set; }

    /// <summary>
    ///     Displays an error <see cref="MessageBox"/> with fixed dimensions.
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
    ///     Consider using <see cref="ErrorQuery(IApplication, string, string, string[])"/> which automatically sizes the
    ///     MessageBox.
    /// </remarks>
    public static int? ErrorQuery (
        IApplication app,
        int width,
        int height,
        string title,
        string message,
        params string [] buttons
    )
    {
        return QueryFull (
                          app,
                          true,
                          width,
                          height,
                          title,
                          message,
                          0,
                          true,
                          buttons);
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
                          0,
                          0,
                          title,
                          message,
                          0,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays an error <see cref="MessageBox"/> with fixed dimensions and a default button.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="width">Width for the MessageBox.</param>
    /// <param name="height">Height for the MessageBox.</param>
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
    ///     Consider using <see cref="ErrorQuery(IApplication, string, string, int, string[])"/> which automatically sizes the
    ///     MessageBox.
    /// </remarks>
    public static int? ErrorQuery (
        IApplication app,
        int width,
        int height,
        string title,
        string message,
        int defaultButton = 0,
        params string [] buttons
    )
    {
        return QueryFull (
                          app,
                          true,
                          width,
                          height,
                          title,
                          message,
                          defaultButton,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays an auto-sized error <see cref="MessageBox"/> with a default button.
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
    public static int? ErrorQuery (IApplication app, string title, string message, int defaultButton = 0, params string [] buttons)
    {
        return QueryFull (
                          app,
                          true,
                          0,
                          0,
                          title,
                          message,
                          defaultButton,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays an error <see cref="MessageBox"/> with fixed dimensions, a default button, and word-wrap control.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="width">Width for the MessageBox.</param>
    /// <param name="height">Height for the MessageBox.</param>
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
    ///     Consider using <see cref="ErrorQuery(IApplication, string, string, int, bool, string[])"/> which automatically
    ///     sizes the MessageBox.
    /// </remarks>
    public static int? ErrorQuery (
        IApplication app,
        int width,
        int height,
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
                          width,
                          height,
                          title,
                          message,
                          defaultButton,
                          wrapMessage,
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
                          0,
                          0,
                          title,
                          message,
                          defaultButton,
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
                          width,
                          height,
                          title,
                          message,
                          0,
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
                          0,
                          0,
                          title,
                          message,
                          0,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays a <see cref="MessageBox"/> with fixed dimensions and a default button.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="width">Width for the MessageBox.</param>
    /// <param name="height">Height for the MessageBox.</param>
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
    ///     Consider using <see cref="Query(IApplication, string, string, int, string[])"/> which automatically sizes the
    ///     MessageBox.
    /// </remarks>
    public static int? Query (
        IApplication app,
        int width,
        int height,
        string title,
        string message,
        int defaultButton = 0,
        params string [] buttons
    )
    {
        return QueryFull (
                          app,
                          false,
                          width,
                          height,
                          title,
                          message,
                          defaultButton,
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
                          0,
                          0,
                          title,
                          message,
                          defaultButton,
                          true,
                          buttons);
    }

    /// <summary>
    ///     Displays a <see cref="MessageBox"/> with fixed dimensions, a default button, and word-wrap control.
    /// </summary>
    /// <param name="app">The application instance. If <see langword="null"/>, uses <see cref="IApplication.TopRunnableView"/>.</param>
    /// <param name="width">Width for the MessageBox.</param>
    /// <param name="height">Height for the MessageBox.</param>
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
    ///     Consider using <see cref="Query(IApplication, string, string, int, bool, string[])"/> which automatically sizes
    ///     the MessageBox.
    /// </remarks>
    public static int? Query (
        IApplication app,
        int width,
        int height,
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
                          width,
                          height,
                          title,
                          message,
                          defaultButton,
                          wrapMessage,
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
                          0,
                          0,
                          title,
                          message,
                          defaultButton,
                          wrapMessage,
                          buttons);
    }

    private static int? QueryFull (
        IApplication app,
        bool useErrorColors,
        int width,
        int height,
        string title,
        string message,
        int defaultButton = 0,
        bool wrapMessage = true,
        params string [] buttons
    )
    {
        var count = 0;
        List<Button> buttonList = [];
        Clicked = null;

        if (buttons.Length > 0)
        {
            if (defaultButton > buttons.Length - 1)
            {
                defaultButton = buttons.Length - 1;
            }

            foreach (string buttonText in buttons)
            {
                var b = new Button
                {
                    Text = buttonText,
                    Data = count
                };

                b.Accepting += (s, e) =>
                               {
                                   Button? button = s as Button;
                                   Clicked = (int)button!.Data!;
                                   e.Handled = true;

                                   button.App?.RequestStop ();
                               };

                buttonList.Add (b);
                count++;
            }
        }

        Dialog dialog = new ()
        {
            Title = title,
            ButtonAlignment = DefaultButtonAlignment,
            ButtonAlignmentModes = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems,
            BorderStyle = DefaultBorderStyle,
            Buttons = buttonList.ToArray ()
        };

        dialog.Width = Dim.Auto (minimumContentDim: Dim.Func (_ => Math.Max (Dim.Percent (DefaultMinimumWidth).GetAnchor (dialog.GetContainerSize ().Width), dialog.GetWidthRequiredForSubViews ())),
                                 maximumContentDim: Dim.Percent (90));
        dialog.Height = Dim.Auto (minimumContentDim: Dim.Func (_ => Math.Max (Dim.Percent (DefaultMinimumHeight).GetAnchor (dialog.GetContainerSize ().Height), dialog.GetHeightRequiredForSubViews ())),
                                  maximumContentDim: Dim.Percent (90));

        if (width != 0)
        {
            dialog.Width = width;
        }

        if (height != 0)
        {
            dialog.Height = height;
        }

        dialog.SchemeName = useErrorColors ? SchemeManager.SchemesToSchemeName (Schemes.Error) : SchemeManager.SchemesToSchemeName (Schemes.Dialog);

        dialog.HotKeySpecifier = new ('\xFFFF');
        dialog.Text = message;
        dialog.TextAlignment = Alignment.Center;
        dialog.VerticalTextAlignment = Alignment.Start;
        dialog.TextFormatter.WordWrap = wrapMessage;
        dialog.TextFormatter.MultiLine = !wrapMessage;

        // Run the modal
        app.Run (dialog);
        dialog.Dispose ();

        return Clicked;
    }
}
