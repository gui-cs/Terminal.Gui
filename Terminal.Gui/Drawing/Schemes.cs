namespace Terminal.Gui.Drawing;

// ReSharper disable InconsistentNaming
/// <summary>
///     The built-in scheme names used by <see cref="SchemeManager"/>.
///     <see cref="SchemeManager.GetSchemeNames"/> returns a collection valid scheme names, based on this enum.
/// </summary>
public enum Schemes
{
    /// <summary>
    ///     The base scheme used for most Views.
    /// </summary>
    Base,

    /// <summary>
    ///     The menu scheme, used for Terminal.Gui.Menu, MenuBar, and StatusBar.
    /// </summary>
    Menu,

    /// <summary>
    ///     The dialog scheme, used for Dialog, MessageBox, and other views dialog-like views.
    /// </summary>
    Dialog,

    /// <summary>
    ///     The scheme used for views that support <see cref="IRunnable"/>.
    /// </summary>
    Runnable,

    /// <summary>
    ///     The scheme for showing errors.
    /// </summary>
    Error
}
