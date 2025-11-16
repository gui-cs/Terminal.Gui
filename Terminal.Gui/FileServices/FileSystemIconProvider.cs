using System.IO.Abstractions;

namespace Terminal.Gui.FileServices;

/// <summary>Determines which symbol to use to represent files and directories.</summary>
public class FileSystemIconProvider
{
    private readonly NerdFonts _nerd = new ();
    private bool _useNerdIcons = NerdFonts.Enable;
    private bool _useUnicodeCharacters;

    /// <summary>
    ///     Returns the character to use to represent <paramref name="fileSystemInfo"/> or an empty space if no icon
    ///     should be used.
    /// </summary>
    /// <param name="fileSystemInfo">The file or directory requiring an icon.</param>
    /// <returns></returns>
    public Rune GetIcon (IFileSystemInfo? fileSystemInfo)
    {
        if (UseNerdIcons)
        {
            return new (
                        _nerd.GetNerdIcon (
                                           fileSystemInfo,
                                           fileSystemInfo is IDirectoryInfo dir && IsOpenGetter (dir)
                                          )
                       );
        }

        if (fileSystemInfo is IDirectoryInfo)
        {
            return UseUnicodeCharacters ? Glyphs.Folder : new (Path.DirectorySeparatorChar);
        }

        return UseUnicodeCharacters ? Glyphs.File : new (' ');
    }

    /// <summary>
    ///     Returns <see cref="GetIcon(IFileSystemInfo)"/> with an extra space on the end if icon is likely to overlap
    ///     adjacent cells.
    /// </summary>
    public string GetIconWithOptionalSpace (IFileSystemInfo? fileSystemInfo)
    {
        string space = UseNerdIcons ? " " : "";

        return GetIcon (fileSystemInfo!) + space;
    }

    /// <summary>
    ///     Gets or sets the delegate to be used to determine opened state of directories when resolving
    ///     <see cref="GetIcon(IFileSystemInfo)"/>.  Defaults to always false.
    /// </summary>
    public Func<IDirectoryInfo, bool> IsOpenGetter { get; set; } = d => false;

    /// <summary>
    ///     <para>
    ///         Gets or sets a flag indicating whether to use Nerd Font icons. Defaults to <see cref="NerdFonts.Enable"/>
    ///         which can be configured by end users from their <c>./.tui/config.json</c> via
    ///         <see cref="ConfigurationManager"/>.
    ///     </para>
    ///     <remarks>Enabling <see cref="UseNerdIcons"/> implicitly disables <see cref="UseUnicodeCharacters"/>.</remarks>
    /// </summary>
    public bool UseNerdIcons
    {
        get => _useNerdIcons;
        set
        {
            _useNerdIcons = value;

            if (value)
            {
                UseUnicodeCharacters = false;
            }
        }
    }

    /// <summary>Gets or sets a flag indicating whether to use common unicode characters for file/directory icons.</summary>
    public bool UseUnicodeCharacters
    {
        get => _useUnicodeCharacters;
        set
        {
            _useUnicodeCharacters = value;

            if (value)
            {
                UseNerdIcons = false;
            }
        }
    }
}
