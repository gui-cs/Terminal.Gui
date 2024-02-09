namespace Terminal.Gui;

/// <summary>
///     MessageBox displays a modal message to the user, with a title, a message and a series of options that the user can
///     choose from.
/// </summary>
/// <para>
///     The difference between the <see cref="Query(string, string, string[])"/> and
///     <see cref="ErrorQuery(string, string, string[])"/> method is the default set of colors used for the message box.
/// </para>
/// <para>
///     The following example pops up a <see cref="MessageBox"/> with the specified title and text, plus two
///     <see cref="Button"/>s. The value -1 is returned when the user cancels the <see cref="MessageBox"/> by pressing the
///     ESC key.
/// </para>
/// <example>
///     <code lang="c#">
/// var n = MessageBox.Query ("Quit Demo", "Are you sure you want to quit this demo?", "Yes", "No");
/// if (n == 0)
///    quit = true;
/// else
///    quit = false;
/// </code>
/// </example>
public static class MessageBox {
    /// <summary>
    ///     The index of the selected button, or -1 if the user pressed ESC to close the dialog. This is useful for web based
    ///     console where by default there is no SynchronizationContext or TaskScheduler.
    /// </summary>
    public static int Clicked { get; private set; } = -1;

    /// <summary>
    ///     Defines the default border styling for <see cref="Dialog"/>. Can be configured via
    ///     <see cref="ConfigurationManager"/>.
    /// </summary>
    [SerializableConfigurationProperty (Scope = typeof (ThemeScope))]
    public static LineStyle DefaultBorderStyle { get; set; } = LineStyle.Single;

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="width">Width for the window.</param>
    /// <param name="height">Height for the window.</param>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     Use <see cref="ErrorQuery(string, string, string[])"/> instead; it automatically sizes the MessageBox based on the
    ///     contents.
    /// </remarks>
    public static int ErrorQuery (int width, int height, string title, string message, params string[] buttons) =>
        QueryFull (true, width, height, title, message, 0, true, buttons);

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     The message box will be vertically and horizontally centered in the container and the size will be automatically
    ///     determined from the size of the title, message. and buttons.
    /// </remarks>
    public static int ErrorQuery (string title, string message, params string[] buttons) =>
        QueryFull (true, 0, 0, title, message, 0, true, buttons);

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="width">Width for the window.</param>
    /// <param name="height">Height for the window.</param>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     Use <see cref="ErrorQuery(string, string, string[])"/> instead; it automatically sizes the MessageBox based on the
    ///     contents.
    /// </remarks>
    public static int ErrorQuery (
        int width,
        int height,
        string title,
        string message,
        int defaultButton = 0,
        params string[] buttons
    ) => QueryFull (true, width, height, title, message, defaultButton, true, buttons);

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     The message box will be vertically and horizontally centered in the container and the size will be automatically
    ///     determined from the size of the title, message. and buttons.
    /// </remarks>
    public static int ErrorQuery (string title, string message, int defaultButton = 0, params string[] buttons) =>
        QueryFull (true, 0, 0, title, message, defaultButton, true, buttons);

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="width">Width for the window.</param>
    /// <param name="height">Height for the window.</param>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button.</param>
    /// <param name="wrapMessagge">If wrap the message or not.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     Use <see cref="ErrorQuery(string, string, string[])"/> instead; it automatically sizes the MessageBox based on the
    ///     contents.
    /// </remarks>
    public static int ErrorQuery (
        int width,
        int height,
        string title,
        string message,
        int defaultButton = 0,
        bool wrapMessagge = true,
        params string[] buttons
    ) => QueryFull (true, width, height, title, message, defaultButton, wrapMessagge, buttons);

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button.</param>
    /// <param name="wrapMessagge">If wrap the message or not.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     The message box will be vertically and horizontally centered in the container and the size will be automatically
    ///     determined from the size of the title, message. and buttons.
    /// </remarks>
    public static int ErrorQuery (
        string title,
        string message,
        int defaultButton = 0,
        bool wrapMessagge = true,
        params string[] buttons
    ) => QueryFull (true, 0, 0, title, message, defaultButton, wrapMessagge, buttons);

    /// <summary>
    ///     Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="width">Width for the window.</param>
    /// <param name="height">Height for the window.</param>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     Use <see cref="Query(string, string, string[])"/> instead; it automatically sizes the MessageBox based on the
    ///     contents.
    /// </remarks>
    public static int Query (int width, int height, string title, string message, params string[] buttons) =>
        QueryFull (false, width, height, title, message, 0, true, buttons);

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     The message box will be vertically and horizontally centered in the container and the size will be automatically
    ///     determined from the size of the message and buttons.
    /// </remarks>
    public static int Query (string title, string message, params string[] buttons) =>
        QueryFull (false, 0, 0, title, message, 0, true, buttons);

    /// <summary>
    ///     Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="width">Width for the window.</param>
    /// <param name="height">Height for the window.</param>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     Use <see cref="Query(string, string, string[])"/> instead; it automatically sizes the MessageBox based on the
    ///     contents.
    /// </remarks>
    public static int Query (
        int width,
        int height,
        string title,
        string message,
        int defaultButton = 0,
        params string[] buttons
    ) => QueryFull (false, width, height, title, message, defaultButton, true, buttons);

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     The message box will be vertically and horizontally centered in the container and the size will be automatically
    ///     determined from the size of the message and buttons.
    /// </remarks>
    public static int Query (string title, string message, int defaultButton = 0, params string[] buttons) =>
        QueryFull (false, 0, 0, title, message, defaultButton, true, buttons);

    /// <summary>
    ///     Presents a normal <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="width">Width for the window.</param>
    /// <param name="height">Height for the window.</param>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button.</param>
    /// <param name="wrapMessagge">If wrap the message or not.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     Use <see cref="Query(string, string, string[])"/> instead; it automatically sizes the MessageBox based on the
    ///     contents.
    /// </remarks>
    public static int Query (
        int width,
        int height,
        string title,
        string message,
        int defaultButton = 0,
        bool wrapMessagge = true,
        params string[] buttons
    ) => QueryFull (false, width, height, title, message, defaultButton, wrapMessagge, buttons);

    /// <summary>
    ///     Presents an error <see cref="MessageBox"/> with the specified title and message and a list of buttons to show to
    ///     the user.
    /// </summary>
    /// <returns>The index of the selected button, or -1 if the user pressed ESC to close the dialog.</returns>
    /// <param name="title">Title for the query.</param>
    /// <param name="message">Message to display, might contain multiple lines.</param>
    /// <param name="defaultButton">Index of the default button.</param>
    /// <param name="wrapMessage">If wrap the message or not.</param>
    /// <param name="buttons">Array of buttons to add.</param>
    /// <remarks>
    ///     The message box will be vertically and horizontally centered in the container and the size will be automatically
    ///     determined from the size of the message and buttons.
    /// </remarks>
    public static int Query (
        string title,
        string message,
        int defaultButton = 0,
        bool wrapMessage = true,
        params string[] buttons
    ) => QueryFull (false, 0, 0, title, message, defaultButton, wrapMessage, buttons);

    private static int QueryFull (
        bool useErrorColors,
        int width,
        int height,
        string title,
        string message,
        int defaultButton = 0,
        bool wrapMessage = true,
        params string[] buttons
    ) {
        // Create button array for Dialog
        var count = 0;
        List<Button> buttonList = new ();
        if (buttons != null) {
            if (defaultButton > buttons.Length - 1) {
                defaultButton = buttons.Length - 1;
            }

            foreach (string s in buttons) {
                var b = new Button { Text = s };
                if (count == defaultButton) {
                    b.IsDefault = true;
                }

                buttonList.Add (b);
                count++;
            }
        }

        Dialog d;
        d = new Dialog {
            Buttons = buttonList.ToArray (),
            Title = title,
            BorderStyle = DefaultBorderStyle,
            Width = Dim.Percent (60),
            Height = 5 // Border + one line of text + vspace + buttons
        };

        if (width != 0) {
            d.Width = width;
        }

        if (height != 0) {
            d.Height = height;
        }

        if (useErrorColors) {
            d.ColorScheme = Colors.ColorSchemes["Error"];
        } else {
            d.ColorScheme = Colors.ColorSchemes["Dialog"];
        }

        var messageLabel = new Label {
            AutoSize = !wrapMessage,
            Text = message,
            TextAlignment = TextAlignment.Centered,
            X = Pos.Center (),
            Y = 0
        };

        if (!messageLabel.AutoSize) {
            messageLabel.Width = Dim.Fill ();
            messageLabel.Height = Dim.Fill (1);
        }

        messageLabel.TextFormatter.WordWrap = wrapMessage;
        messageLabel.TextFormatter.MultiLine = !wrapMessage;
        d.Add (messageLabel);

        d.Loaded += (s, e) => {
            if (width != 0 || height != 0) {
                return;
            }

            // TODO: replace with Dim.Fit when implemented
            Rect maxBounds = d.SuperView?.Bounds ?? Application.Top.Bounds;
            if (wrapMessage) {
                messageLabel.TextFormatter.Size = new Size (
                    maxBounds.Size.Width
                    - d.GetAdornmentsThickness ().Horizontal,
                    maxBounds.Size.Height
                    - d.GetAdornmentsThickness ().Vertical
                );
            }

            string msg = messageLabel.TextFormatter.Format ();
            Size messageSize = messageLabel.TextFormatter.FormatAndGetSize ();

            // Ensure the width fits the text + buttons
            int newWidth = Math.Max (
                width,
                Math.Max (
                    messageSize.Width + d.GetAdornmentsThickness ().Horizontal,
                    d.GetButtonsWidth () + d.Buttons.Length + d.GetAdornmentsThickness ().Horizontal
                )
            );
            if (newWidth > d.Frame.Width) {
                d.Width = newWidth;
            }

            // Ensure height fits the text + vspace + buttons
            if (messageSize.Height == 0) {
                d.Height = Math.Max (height, 3 + d.GetAdornmentsThickness ().Vertical);
            } else {
                string lastLine = messageLabel.TextFormatter.GetLines ()[^1];
                d.Height = Math.Max (
                    height,
                    messageSize.Height
                    + (lastLine.EndsWith ("\r\n") || lastLine.EndsWith ('\n') ? 1 : 2)
                    + d.GetAdornmentsThickness ().Vertical
                );
            }

            d.SetRelativeLayout (d.SuperView?.Frame ?? Application.Top.Frame);
        };

        // Setup actions
        Clicked = -1;
        for (var n = 0; n < buttonList.Count; n++) {
            int buttonId = n;
            Button b = buttonList[n];
            b.Clicked += (s, e) => {
                Clicked = buttonId;
                Application.RequestStop ();
            };
            if (b.IsDefault) {
                b.SetFocus ();
            }
        }

        // Run the modal; do not shutdown the mainloop driver when done
        Application.Run (d);

        return Clicked;
    }
}
