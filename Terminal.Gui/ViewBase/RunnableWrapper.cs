namespace Terminal.Gui.ViewBase;

/// <summary>
///     Wraps any <see cref="View"/> to make it runnable with a typed result, similar to how
///     <see cref="FlagSelector{TFlagsEnum}"/> wraps <see cref="FlagSelector"/>.
/// </summary>
/// <typeparam name="TView">The type of view being wrapped.</typeparam>
/// <typeparam name="TResult">The type of result data returned when the session completes.</typeparam>
/// <remarks>
///     <para>
///         This class enables any View to be run as a blocking session with
///         <see cref="IApplication.Run(Func{Exception, bool}, string)"/>
///         without requiring the View to implement <see cref="IRunnable{TResult}"/> or derive from
///         <see cref="Runnable{TResult}"/>.
///     </para>
///     <para>
///         Use <see cref="ViewRunnableExtensions.AsRunnable{TView, TResult}"/> for a fluent API approach,
///         or <see cref="ApplicationRunnableExtensions.RunView{TView, TResult}"/> to run directly.
///     </para>
///     <example>
///         <code>
/// // Wrap a TextField to make it runnable with string result
/// var textField = new TextField { Width = 40 };
/// var runnable = new RunnableWrapper&lt;TextField, string&gt; { WrappedView = textField };
/// 
/// // Extract result when stopping
/// runnable.IsRunningChanging += (s, e) =&gt;
/// {
///     if (!e.NewValue) // Stopping
///     {
///         runnable.Result = runnable.WrappedView.Text;
///     }
/// };
/// 
/// app.Run(runnable);
/// Console.WriteLine($"User entered: {runnable.Result}");
/// runnable.Dispose();
/// </code>
///     </example>
/// </remarks>
public class RunnableWrapper<TView, TResult> : Runnable<TResult> where TView : View
{
    /// <summary>
    ///     Initializes a new instance of <see cref="RunnableWrapper{TView, TResult}"/>.
    /// </summary>
    public RunnableWrapper ()
    {
        // Make the wrapper automatically size to fit the wrapped view
        Width = Dim.Fill ();
        Height = Dim.Fill ();
    }

    private TView? _wrappedView;

    /// <summary>
    ///     Gets or sets the wrapped view that is being made runnable.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This property must be set before the wrapper is initialized.
    ///         Access this property to interact with the original view, extract its state,
    ///         or configure result extraction logic.
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

    /// <inheritdoc/>
    public override void EndInit ()
    {
        base.EndInit ();

        // Add the wrapped view as a subview after initialization
        if (_wrappedView is { })
        {
            Add (_wrappedView);
        }
    }
}
