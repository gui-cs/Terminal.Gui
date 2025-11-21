namespace Terminal.Gui.ViewBase;

/// <summary>
///     Extension methods for making any <see cref="View"/> runnable with typed results.
/// </summary>
/// <remarks>
///     These extensions provide a fluent API for wrapping views in <see cref="RunnableWrapper{TView, TResult}"/>,
///     enabling any View to be run as a blocking session without implementing <see cref="IRunnable{TResult}"/>.
/// </remarks>
public static class ViewRunnableExtensions
{
    /// <summary>
    ///     Converts any View into a runnable with typed result extraction.
    /// </summary>
    /// <typeparam name="TView">The type of view to make runnable.</typeparam>
    /// <typeparam name="TResult">The type of result data to extract.</typeparam>
    /// <param name="view">The view to wrap. Cannot be null.</param>
    /// <param name="resultExtractor">
    ///     Function that extracts the result from the view when stopping.
    ///     Called automatically when the runnable session ends.
    /// </param>
    /// <returns>A <see cref="RunnableWrapper{TView, TResult}"/> that wraps the view.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="view"/> or <paramref name="resultExtractor"/> is
    ///     null.
    /// </exception>
    /// <remarks>
    ///     <para>
    ///         This method wraps the view in a <see cref="RunnableWrapper{TView, TResult}"/> and automatically
    ///         subscribes to <see cref="IRunnable.IsRunningChanging"/> to extract the result when the session stops.
    ///     </para>
    ///     <para>
    ///         The result is extracted before the view is disposed, ensuring all data is still accessible.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Make a TextField runnable with string result
    /// var runnable = new TextField { Width = 40 }
    ///     .AsRunnable(tf =&gt; tf.Text);
    /// 
    /// app.Run(runnable);
    /// Console.WriteLine($"User entered: {runnable.Result}");
    /// runnable.Dispose();
    /// 
    /// // Make a ColorPicker runnable with Color? result
    /// var colorRunnable = new ColorPicker()
    ///     .AsRunnable(cp =&gt; cp.SelectedColor);
    /// 
    /// app.Run(colorRunnable);
    /// Console.WriteLine($"Selected: {colorRunnable.Result}");
    /// colorRunnable.Dispose();
    /// 
    /// // Make a FlagSelector runnable with enum result
    /// var flagsRunnable = new FlagSelector&lt;SelectorStyles&gt;()
    ///     .AsRunnable(fs =&gt; fs.Value);
    /// 
    /// app.Run(flagsRunnable);
    /// Console.WriteLine($"Selected styles: {flagsRunnable.Result}");
    /// flagsRunnable.Dispose();
    /// </code>
    /// </example>
    public static RunnableWrapper<TView, TResult> AsRunnable<TView, TResult> (
        this TView view,
        Func<TView, TResult?> resultExtractor
    )
        where TView : View
    {
        if (view is null)
        {
            throw new ArgumentNullException (nameof (view));
        }

        if (resultExtractor is null)
        {
            throw new ArgumentNullException (nameof (resultExtractor));
        }

        RunnableWrapper<TView, TResult> wrapper = new (view);

        // Subscribe to IsRunningChanging to extract result when stopping
        wrapper.IsRunningChanging += (s, e) =>
                                     {
                                         if (!e.NewValue) // Stopping
                                         {
                                             wrapper.Result = resultExtractor (view);
                                         }
                                     };

        return wrapper;
    }

    /// <summary>
    ///     Converts any View into a runnable without result extraction.
    /// </summary>
    /// <typeparam name="TView">The type of view to make runnable.</typeparam>
    /// <param name="view">The view to wrap. Cannot be null.</param>
    /// <returns>A <see cref="RunnableWrapper{TView, Object}"/> that wraps the view.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="view"/> is null.</exception>
    /// <remarks>
    ///     <para>
    ///         Use this overload when you don't need to extract a typed result, but still want to
    ///         run the view as a blocking session. The wrapped view can still be accessed via
    ///         <see cref="RunnableWrapper{TView, TResult}.WrappedView"/> after running.
    ///     </para>
    /// </remarks>
    /// <example>
    ///     <code>
    /// // Make a view runnable without result extraction
    /// var colorPicker = new ColorPicker();
    /// var runnable = colorPicker.AsRunnable();
    /// 
    /// app.Run(runnable);
    /// 
    /// // Access the wrapped view directly to get the result
    /// Console.WriteLine($"Selected: {runnable.WrappedView.SelectedColor}");
    /// runnable.Dispose();
    /// </code>
    /// </example>
    public static RunnableWrapper<TView, object> AsRunnable<TView> (this TView view)
        where TView : View
    {
        if (view is null)
        {
            throw new ArgumentNullException (nameof (view));
        }

        return new (view);
    }
}
