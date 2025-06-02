#nullable enable
namespace Terminal.Gui.Views;

/// <summary>
///     Styles for <see cref="FlagSelector"/>.
/// </summary>
[Flags]
public enum FlagSelectorStyles
{
    /// <summary>
    ///     No styles.
    /// </summary>
    None = 0b_0000_0000,

    /// <summary>
    ///     Show the `None` checkbox. This will add a checkbox with the title "None" and a value of 0
    ///     even if the flags do not contain a value of 0.
    /// </summary>
    ShowNone = 0b_0000_0001,

    /// <summary>
    ///     Show the value edit. This will add a read-only <see cref="TextField"/> to the <see cref="FlagSelector"/> to allow
    ///     the user to see the value.
    /// </summary>
    ShowValueEdit = 0b_0000_0010,

    /// <summary>
    ///     All styles.
    /// </summary>
    All = ShowNone | ShowValueEdit
}
