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
    ///     Show the `None` checkbox. This will add a checkbox with the title "None" that when checked will cause the value ot
    ///     be set to 0.
    ///     The `None` checkbox will be added even if the flags do not contain a value of 0.
    /// </summary>
    ShowNone = 0b_0000_0001,

    // TODO: Implement this.
    /// <summary>
    ///     Show the `All` checkbox.  This will add a checkbox with the title "All" that when checked will
    ///     cause all flags to be set. Unchecking the "All" checkbox will set the value to 0.
    /// </summary>
    ShowAll = 0b_0000_0010,

    // TODO: Make the TextField a TextValidateField so it can be editable and validate the value.
    /// <summary>
    ///     Show the value field. This will add a read-only <see cref="TextField"/> to the <see cref="FlagSelector"/> to allow
    ///     the user to see the value.
    /// </summary>
    ShowValue = 0b_0000_0100,

    /// <summary>
    ///     All styles.
    /// </summary>
    All = ShowNone | ShowAll | ShowValue
}
