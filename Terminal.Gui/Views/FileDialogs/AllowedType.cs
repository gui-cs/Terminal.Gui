
namespace Terminal.Gui.Views;

/// <summary>Interface for <see cref="FileDialog"/> restrictions on which file type(s) the user is allowed to select/enter.</summary>
public interface IAllowedType
{
    /// <summary>
    ///     Returns true if the file at <paramref name="path"/> is compatible with this allow option.  Note that the file
    ///     may not exist (e.g. in the case of saving).
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    bool IsAllowed (string path);
}

/// <summary><see cref="IAllowedType"/> that allows selection of any types (*.*).</summary>
public class AllowedTypeAny : IAllowedType
{
    /// <inheritdoc/>
    public bool IsAllowed (string path) { return true; }

    /// <summary>Returns a string representation of this <see cref="AllowedTypeAny"/>.</summary>
    /// <returns></returns>
    public override string ToString () { return Strings.fdAnyFiles + "(*.*)"; }
}

/// <summary>
///     Describes a requirement on what <see cref="FileInfo"/> can be selected. This can be combined with other
///     <see cref="IAllowedType"/> in a <see cref="FileDialog"/> to for example show only .csv files but let user change to
///     open any if they want.
/// </summary>
public class AllowedType : IAllowedType
{
    /// <summary>Initializes a new instance of the <see cref="AllowedType"/> class.</summary>
    /// <param name="description">The human-readable text to display.</param>
    /// <param name="extensions">Extension(s) to match e.g. .csv.</param>
    public AllowedType (string description, params string [] extensions)
    {
        if (extensions.Length == 0)
        {
            throw new ArgumentException ("You must supply at least one extension");
        }

        Description = description;
        Extensions = extensions;
    }

    /// <summary>Gets or Sets the human-readable description for the file type e.g. "Comma Separated Values".</summary>
    public string Description { get; set; }

    /// <summary>Gets or Sets the permitted file extension(s) (e.g. ".csv").</summary>
    public string [] Extensions { get; set; }

    /// <inheritdoc/>
    public bool IsAllowed (string path)
    {
        if (string.IsNullOrWhiteSpace (path))
        {
            return false;
        }

        string extension = Path.GetExtension (path);

        if (Extensions.Any (e => path.EndsWith (e, StringComparison.InvariantCultureIgnoreCase)))
        {
            return true;
        }

        // There is a requirement to have a particular extension and we have none
        if (string.IsNullOrEmpty (extension))
        {
            return false;
        }

        return Extensions.Any (e => e.Equals (extension, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>Returns <see cref="Description"/> plus all <see cref="Extensions"/> separated by semicolons.</summary>
    public override string ToString ()
    {
        const int maxLength = 30;

        var desc = $"{Description} ({string.Join (";", Extensions.Select (e => '*' + e).ToArray ())})";

        if (desc.Length > maxLength)
        {
            return desc.Substring (0, maxLength - 2) + "…";
        }

        return desc;
    }
}
