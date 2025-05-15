#nullable enable
using System.ComponentModel;

namespace Terminal.Gui;

public partial class View
{
    /// <summary>
    ///     INTERNAL: Gets the hard-coded set of <see cref="Scheme"/>s. Used for generating the built-in config.json and for
    ///     unit tests that don't depend on ConfigurationManager.
    /// </summary>
    /// <returns></returns>
    internal static Dictionary<string, Scheme?> GetHardCodedSchemes ()
    {
        return new (StringComparer.InvariantCultureIgnoreCase)
        {
            {
                SchemeManager.SchemesToSchemeName (Schemes.Base)!,
                new (
                     new (new Color ("White"), new Color ("Blue")),
                     new (new Color ("DarkBlue"), new Color ("LightGray")),
                     new (new Color ("BrightCyan"), new Color ("Blue")),
                     hotFocus: new (new Color ("BrightBlue"), new Color ("LightGray")),
                     disabled: new (new Color ("DarkGray"), new Color ("Blue"))
                    )
            },
            {
                SchemeManager.SchemesToSchemeName (Schemes.Dialog)!,
                new (
                               new (new Color ("Black"), new Color ("LightGray")),
                     new (new Color ("DarkGray"), new Color ("LightGray")),
                     new (new Color ("Blue"), new Color ("LightGray")),
                     hotFocus: new (new Color ("BrightBlue"), new Color ("LightGray")),
                     disabled: new (new Color ("Gray"), new Color ("DarkGray"))
                    )
            },
            {
                SchemeManager.SchemesToSchemeName (Schemes.Error)!,
                new (
                              new (new Color ("Red"), new Color ("Pink")),
                     new (new Color ("White"), new Color ("BrightRed")),
                     new (new Color ("Black"), new Color ("Pink")),
                     hotFocus: new (new Color ("Pink"), new Color ("BrightRed")),
                     disabled: new (new Color ("DarkGray"), new Color ("White"))
                    )
            },
            {
                SchemeManager.SchemesToSchemeName (Schemes.Menu)!,
                new (
                     new (new Color ("White"), new Color ("DarkBlue")),
                     new (new Color ("DarkBlue"), new Color ("White")),
                     new (new Color ("Yellow"), new Color ("DarkBlue")),
                     hotFocus: new (new Color ("Blue"), new Color ("White")),
                     disabled: new (new Color ("Gray"), new Color ("DarkGray"))
                    )
            },
            {
                SchemeManager.SchemesToSchemeName (Schemes.Toplevel)!,
                new (
                     normal: new (new Color ("White"), new Color ("DarkSlateGray")),
                     hotNormal: new (new Color ("Yellow"), new Color ("DarkSlateGray")),
                     focus: new (new Color ("White"), new Color ("DimGray")),
                     hotFocus: new (new Color ("Yellow"), new Color ("DarkSlateGray")),
                     disabled: new (new Color ("DarkGray"), new Color ("DarkSlateGray"))
                    )
            },
        };
    }

    /// <summary>
    ///     Gets or sets the name of the Scheme to use for this View. If set, it will override the scheme inherited from the
    ///     SuperView. If <see cref="Scheme"/> was explicitly set (<see cref="HasScheme"/> is <see langword="true"/>),
    ///     this property will be ignored.
    /// </summary>
    public string? SchemeName { get; set; }

    // Both holds the set Scheme and is used to determine if a Scheme has been set or not
    private Scheme? _scheme;

    /// <summary>
    ///     Gets whether <see cref="Scheme"/> has been explicitly set for this View.
    /// </summary>
    public bool HasScheme => _scheme is { };

    /// <summary>
    ///     Gets or sets the Scheme for this view.
    ///     <para>
    ///         If the Scheme has not been explicitly set (<see cref="HasScheme"/> is <see langword="false"/>), this property
    ///         gets
    ///         <see cref="SuperView"/>'s Scheme.
    ///     </para>
    /// </summary>
    public Scheme Scheme
    {
        get => GetScheme ();
        set => SetScheme (value);
    }

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
            return SchemeManager.GetCurrentSchemes () [SchemeName]!;
        }

        if (!HasScheme)
        {
            return SuperView?.GetScheme () ?? SchemeManager.GetCurrentSchemes () ["Base"]!;
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
    ///     Set <see cref="SchemeEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
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
            Border.Scheme = _scheme;
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

    #region VisualRole

    /// <summary>Raised when the <see cref="Scheme"/> for the View is to be set.</summary>
    /// <returns>
    ///     Set <see cref="CancelEventArgs.Cancel"/> to <see langword="true"/> to stop default behavior.
    /// </returns>
    public event EventHandler<CancelEventArgs>? SettingScheme;

    #endregion VisualRole
}
