namespace Terminal.Gui.Views;

/// <summary>Specifies how a <see cref="MenuItem"/> shows selection state.</summary>
[Flags]
public enum MenuItemCheckStyle
{
    /// <summary>The menu item will be shown normally, with no check indicator. The default.</summary>
    NoCheck = 0b_0000_0000,

    /// <summary>The menu item will indicate checked/un-checked state (see <see cref="Checked"/>).</summary>
    Checked = 0b_0000_0001,

    /// <summary>The menu item is part of a menu radio group (see <see cref="Checked"/>) and will indicate selected state.</summary>
    Radio = 0b_0000_0010
}