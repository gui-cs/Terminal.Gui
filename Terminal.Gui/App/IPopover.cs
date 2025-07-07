#nullable enable

namespace Terminal.Gui.App;

/// <summary>
///     Defines the contract for a popover view in Terminal.Gui.
/// </summary>
/// <remarks>
///     <para>
///         A popover is a transient UI element that appears above other content to display contextual information or UI,
///         such as menus, tooltips, or dialogs.
///         Popovers are managed by <see cref="ApplicationPopover"/> and are typically shown using
///         <see cref="ApplicationPopover.Show"/>.
///     </para>
///     <para>
///         Popovers are not modal; they do not block input to the rest of the application, but they do receive focus and
///         input events while visible.
///         When a popover is shown, it is responsible for handling its own layout and content.
///     </para>
///     <para>
///         Popovers are automatically hidden when:
///         <list type="bullet">
///             <item>The user clicks outside the popover (unless occluded by a subview of the popover).</item>
///             <item>The user presses <see cref="Application.QuitKey"/> (typically <c>Esc</c>).</item>
///             <item>Another popover is shown.</item>
///         </list>
///     </para>
///     <para>
///         <b>Focus and Input:</b><br/>
///         When visible, a popover receives focus and input events. If the user clicks outside the popover (and not on a
///         subview),
///         presses <see cref="Application.QuitKey"/>, or another popover is shown, the popover will be hidden
///         automatically.
///     </para>
///     <para>
///         <b>Layout:</b><br/>
///         When the popover becomes visible, it is automatically laid out to fill the screen by default. You can override
///         this behavior
///         by setting <see cref="View.Width"/> and <see cref="View.Height"/> in your derived class.
///     </para>
///     <para>
///         <b>Mouse:</b><br/>
///         Popovers are transparent to mouse events (see <see cref="ViewportSettingsFlags.TransparentMouse"/>),
///         meaning mouse events in a popover that are not also within a subview of the popover will not be captured.
///     </para>
///     <para>
///         <b>Custom Popovers:</b><br/>
///         To create a custom popover, inherit from <see cref="PopoverBaseImpl"/> and add your own content and logic.
///     </para>
/// </remarks>
public interface IPopover
{
    /// <summary>
    ///     Gets or sets the <see cref="Toplevel"/> that this Popover is associated with. If null, it is not associated with
    ///     any Toplevel and will receive all keyboard
    ///     events from the <see cref="Application"/>. If set, it will only receive keyboard events the Toplevel would normally
    ///     receive.
    ///     When <see cref="ApplicationPopover.Register"/> is called, the <see cref="Toplevel"/> is set to the current
    ///     <see cref="Application.Top"/> if not already set.
    /// </summary>
    Toplevel? Toplevel { get; set; }
}
