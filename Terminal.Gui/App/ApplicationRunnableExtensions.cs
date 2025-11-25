namespace Terminal.Gui.App;

/// <summary>
///     Extension methods for <see cref="IApplication"/> that enable running any <see cref="View"/> as a runnable session.
/// </summary>
/// <remarks>
///     These extensions provide convenience methods for wrapping views in <see cref="RunnableWrapper{TView, TResult}"/>
///     and running them in a single call, similar to how <see cref="IApplication.Run(Func{Exception, bool}, string)"/> works.
/// </remarks>
public static class ApplicationRunnableExtensions
{
    /// <summary>
    ///     Runs any View as a runnable session, extracting a typed result via a function.
    /// </summary>
    /// <typeparam name="TView">The type of view to run.</typeparam>
    /// <typeparam name="TResult">The type of result data to extract.</typeparam>
    /// <param name="app">The application instance. Cannot be null.</param>
    /// <param name="view">The view to run as a blocking session. Cannot be null.</param>
    /// <param name="resultExtractor">
    ///     Function that extracts the result from the view when stopping.
    ///     Called automatically when the runnable session ends.
    /// </param>
    /// <param name="errorHandler">Optional handler for unhandled exceptions during the session.</param>
    /// <returns>The extracted result, or null if the session was canceled.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="app"/>, <paramref name="view"/>, or <paramref name="resultExtractor"/> is null.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method wraps the view in a <see cref="RunnableWrapper{TView, TResult}"/>, runs it as a blocking
    ///         session, and returns the extracted result. The wrapper is NOT disposed automatically;
    ///         the caller is responsible for disposal.
    ///     </para>
    ///     <para>
    ///         The result is extracted before the view is disposed, ensuring all data is still accessible.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var app = Application.Create();
    /// app.Init();
    /// 
    /// // Run a TextField and get the entered text
    /// var text = app.RunView(
    ///     new TextField { Width = 40 },
    ///     tf =&gt; tf.Text);
    /// Console.WriteLine($"You entered: {text}");
    /// 
    /// // Run a ColorPicker and get the selected color
    /// var color = app.RunView(
    ///     new ColorPicker(),
    ///     cp =&gt; cp.SelectedColor);
    /// Console.WriteLine($"Selected color: {color}");
    /// 
    /// // Run a FlagSelector and get the selected flags
    /// var flags = app.RunView(
    ///     new FlagSelector&lt;SelectorStyles&gt;(),
    ///     fs =&gt; fs.Value);
    /// Console.WriteLine($"Selected styles: {flags}");
    /// 
    /// app.Shutdown();
    /// </code>
    /// </example>
    public static TResult? RunView<TView, TResult> (
        this IApplication app,
        TView view,
        Func<TView, TResult?> resultExtractor,
        Func<Exception, bool>? errorHandler = null)
        where TView : View
    {
        if (app is null)
        {
            throw new ArgumentNullException (nameof (app));
        }

        if (view is null)
        {
            throw new ArgumentNullException (nameof (view));
        }

        if (resultExtractor is null)
        {
            throw new ArgumentNullException (nameof (resultExtractor));
        }

        var wrapper = new RunnableWrapper<TView, TResult> { WrappedView = view };

        // Subscribe to IsRunningChanging to extract result when stopping
        wrapper.IsRunningChanging += (s, e) =>
        {
            if (!e.NewValue) // Stopping
            {
                wrapper.Result = resultExtractor (view);
            }
        };

        app.Run (wrapper, errorHandler);

        return wrapper.Result;
    }

    /// <summary>
    ///     Runs any View as a runnable session without result extraction.
    /// </summary>
    /// <typeparam name="TView">The type of view to run.</typeparam>
    /// <param name="app">The application instance. Cannot be null.</param>
    /// <param name="view">The view to run as a blocking session. Cannot be null.</param>
    /// <param name="errorHandler">Optional handler for unhandled exceptions during the session.</param>
    /// <returns>The view that was run, allowing access to its state after the session ends.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="app"/> or <paramref name="view"/> is null.</exception>
    /// <remarks>
    ///     <para>
    ///         This method wraps the view in a <see cref="RunnableWrapper{TView, Object}"/> and runs it as a blocking
    ///         session. The wrapper is NOT disposed automatically; the caller is responsible for disposal.
    ///     </para>
    ///     <para>
    ///         Use this overload when you don't need automatic result extraction, but still want the view
    ///         to run as a blocking session. Access the view's properties directly after running.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// var app = Application.Create();
    /// app.Init();
    /// 
    /// // Run a ColorPicker without automatic result extraction
    /// var colorPicker = new ColorPicker();
    /// app.RunView(colorPicker);
    /// 
    /// // Access the view's state directly
    /// Console.WriteLine($"Selected: {colorPicker.SelectedColor}");
    /// 
    /// app.Shutdown();
    /// </code>
    /// </example>
    public static TView RunView<TView> (
        this IApplication app,
        TView view,
        Func<Exception, bool>? errorHandler = null)
        where TView : View
    {
        if (app is null)
        {
            throw new ArgumentNullException (nameof (app));
        }

        if (view is null)
        {
            throw new ArgumentNullException (nameof (view));
        }

        var wrapper = new RunnableWrapper<TView, object> { WrappedView = view };

        app.Run (wrapper, errorHandler);

        return view;
    }
}
