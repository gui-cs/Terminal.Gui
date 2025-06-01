#nullable enable
using System.ComponentModel;

namespace Terminal.Gui.ViewBase;

public partial class View
{
    private string? _schemeName;

    /// <summary>
    ///     Gets or sets the name of the Scheme to use for this View. If set, it will override the scheme inherited from the
    ///     SuperView. If a Scheme was explicitly set (<see cref="HasScheme"/> is <see langword="true"/>),
    ///     this property will be ignored.
    /// </summary>
    public string? SchemeName
    {
        get => _schemeName;
        set
        {
            if (_schemeName == value)
            {
                return;
            }

            if (OnSettingSchemeName (in _schemeName, ref value))
            {
                _schemeName = value;
                return;
            }

            StringPropertyEventArgs args = new (in _schemeName, ref value);
            SettingSchemeName?.Invoke (this, args);

            if (args.Cancel)
            {
                _schemeName = args.NewString;

                return;
            }

            _schemeName = value;
        }
    }

    /// <summary>
    ///     Called when the <see cref="Scheme"/> for the View is to be set.
    /// </summary>
    /// <param name="currentName"></param>
    /// <param name="newName"></param>
    /// <returns><see langword="true"/> to stop default behavior.</returns>
    protected virtual bool OnSettingSchemeName (in string? currentName, ref string? newName) { return false; }

    /// <summary>Raised when the <see cref="Scheme"/> for the View is to be set.</summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<StringPropertyEventArgs>? SettingSchemeName;

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

        var args = new SchemeEventArgs (in _scheme, ref newScheme);
        GettingScheme?.Invoke (this, args);

        if (args.Cancel)
        {
            return args.NewScheme!;
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
    public event EventHandler<SchemeEventArgs>? GettingScheme;

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

        // BUGBUG: This should be in Border.cs somehow
        if (Border is { } && Border.LineStyle != LineStyle.None && Border.HasScheme)
        {
            Border.SetScheme (_scheme);
        }

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
