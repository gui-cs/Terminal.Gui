#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

public partial class View
{
    private string? _schemeName;

    /// <summary>
    ///     Gets or sets the name of the scheme to use for this <see cref="View"/>. If set, it overrides the scheme
    ///     inherited from the <see cref="SuperView"/>. If a scheme was explicitly set (<see cref="HasScheme"/> is
    ///     true), this property is ignored.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Setting this property raises pre- and post-change events via <see cref="CWPPropertyHelper"/>,
    ///         allowing customization or cancellation of the change. The <see cref="SchemeNameChanging"/> event
    ///         is raised before the change, and <see cref="SchemeNameChanged"/> is raised after.
    ///     </para>
    /// </remarks>
    /// <value>The scheme name, or null if no scheme name is set.</value>
    /// <seealso cref="SchemeNameChanging"/>
    /// <seealso cref="SchemeNameChanged"/>
    public string? SchemeName
    {
        get => _schemeName;
        set
        {
            bool changed = CWPPropertyHelper.ChangeProperty (
                _schemeName,
                value,
                OnSchemeNameChanging,
                SchemeNameChanging,
                OnSchemeNameChanged,
                SchemeNameChanged,
                out string? finalValue);

            if (changed)
            {
                _schemeName = finalValue;
            }
        }
    }

    /// <summary>
    ///     Called before the <see cref="SchemeName"/> property changes, allowing subclasses to cancel or modify the change.
    /// </summary>
    /// <param name="args">The event arguments containing the current and proposed new scheme name.</param>
    /// <returns>True to cancel the change, false to proceed.</returns>
    protected virtual bool OnSchemeNameChanging (ValueChangingEventArgs<string?> args)
    {
        return false;
    }

    /// <summary>
    ///     Called after the <see cref="SchemeName"/> property changes, allowing subclasses to react to the change.
    /// </summary>
    /// <param name="args">The event arguments containing the old and new scheme name.</param>
    protected virtual void OnSchemeNameChanged (ValueChangedEventArgs<string?> args)
    {
    }

    /// <summary>
    ///     Raised before the <see cref="SchemeName"/> property changes, allowing handlers to modify or cancel the change.
    /// </summary>
    /// <remarks>
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to true to cancel the change or modify
    ///     <see cref="ValueChangingEventArgs{T}.NewValue"/> to adjust the proposed value.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.SchemeNameChanging += (sender, args) =>
    ///         {
    ///             if (args.NewValue == "InvalidScheme")
    ///             {
    ///                 args.Handled = true;
    ///                 Console.WriteLine("Invalid scheme name cancelled.");
    ///             }
    ///         };
    ///     </code>
    /// </example>
    public event EventHandler<ValueChangingEventArgs<string?>>? SchemeNameChanging;

    /// <summary>
    ///     Raised after the <see cref="SchemeName"/> property changes, notifying handlers of the completed change.
    /// </summary>
    /// <remarks>
    ///     Provides the old and new scheme name via <see cref="ValueChangedEventArgs{T}.OldValue"/> and
    ///     <see cref="ValueChangedEventArgs{T}.NewValue"/>, which may be null.
    /// </remarks>
    /// <example>
    ///     <code>
    ///         view.SchemeNameChanged += (sender, args) =>
    ///         {
    ///             Console.WriteLine($"SchemeName changed from {args.OldValue ?? "none"} to {args.NewValue ?? "none"}.");
    ///         };
    ///     </code>
    /// </example>
    public event EventHandler<ValueChangedEventArgs<string?>>? SchemeNameChanged;

    // Both holds the set Scheme and is used to determine if a Scheme has been set or not
    private Scheme? _scheme;

    /// <summary>
    ///     Gets whether a Scheme has been explicitly set for this View, or if it will inherit the Scheme from its
    ///     <see cref="SuperView"/>.
    /// </summary>
    public bool HasScheme => _scheme is { };

    /// <summary>
    ///     Gets the Scheme for the View. If the Scheme has not been explicitly set (see <see cref="HasScheme"/>), gets
    ///     <see cref="SuperView"/>'s Scheme.
    /// </summary>
    /// <returns></returns>
    public Scheme GetScheme ()
    {
        if (OnGettingScheme (out Scheme? newScheme))
        {
            return newScheme!;
        }

        var args = new ResultEventArgs<Scheme?> (newScheme);
        GettingScheme?.Invoke (this, args);

        if (args.Handled)
        {
            return args.Result!;
        }

        if (!HasScheme && !string.IsNullOrEmpty (SchemeName))
        {
            return SchemeManager.GetScheme (SchemeName);
        }

        if (!HasScheme)
        {
            return SuperView?.GetScheme () ?? SchemeManager.GetScheme (Schemes.Base);
        }

        return _scheme!;
    }

    /// <summary>
    ///     Called when the <see cref="Scheme"/> for the View is being retrieved. Overrides can return <see langword="true"/>
    ///     to
    ///     stop further processing and optionally set <paramref name="scheme"/> to a different value.
    /// </summary>
    /// <returns><see langword="true"/> to stop default behavior.</returns>
    protected virtual bool OnGettingScheme (out Scheme? scheme)
    {
        scheme = null;

        return false;
    }

    /// <summary>
    ///     Raised when the <see cref="Scheme"/> for the View is being retrieved. Overrides can return <see langword="true"/>
    ///     to
    ///     stop further processing and optionally set the <see cref="Scheme"/> in the event args to a different value.
    /// </summary>
    /// <returns>
    ///     Set `Cancel` to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<ResultEventArgs<Scheme?>>? GettingScheme;

    /// <summary>
    ///     Sets the Scheme for the View. Raises <see cref="SettingScheme"/> event before setting the scheme.
    /// </summary>
    /// <param name="scheme">
    ///     The scheme to set. If <see langword="null"/> <see cref="HasScheme"/> will be
    ///     <see langword="false"/>.
    /// </param>
    /// <returns><see langword="true"/> if the scheme was set.</returns>
    public bool SetScheme (Scheme? scheme)
    {
        if (_scheme == scheme)
        {
            return false;
        }

        if (OnSettingScheme (in scheme))
        {
            return false;
        }

        var args = new CancelEventArgs ();
        SettingScheme?.Invoke (this, args);

        if (args.Cancel)
        {
            return false;
        }

        _scheme = scheme;

        SetNeedsDraw ();

        return true;
    }

    /// <summary>
    ///     Called when the <see cref="Scheme"/> for the View is to be set.
    /// </summary>
    /// <param name="scheme"></param>
    /// <returns><see langword="true"/> to stop default behavior.</returns>
    protected virtual bool OnSettingScheme (in Scheme? scheme) { return false; }

    /// <summary>Raised when the <see cref="Scheme"/> for the View is to be set.</summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<CancelEventArgs>? SettingScheme;
}
