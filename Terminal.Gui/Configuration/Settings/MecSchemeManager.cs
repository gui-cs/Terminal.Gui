namespace Terminal.Gui.Configuration;

/// <summary>
///     MEC-backed implementation of <see cref="ISchemeManager"/>.
///     During the transition period, this delegates to the existing static <see cref="SchemeManager"/>
///     for scheme data.
/// </summary>
public class MecSchemeManager : ISchemeManager
{
    /// <inheritdoc/>
    public IReadOnlyList<string> SchemeNames
    {
        get
        {
            try
            {
                return SchemeManager.GetSchemeNames ().ToList ();
            }
            catch
            {
                return [];
            }
        }
    }

    /// <inheritdoc/>
    public Scheme? GetScheme (string name)
    {
        try
        {
            return SchemeManager.GetScheme (name);
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public void AddScheme (string name, Scheme scheme)
    {
        SchemeManager.AddScheme (name, scheme);
    }
}
