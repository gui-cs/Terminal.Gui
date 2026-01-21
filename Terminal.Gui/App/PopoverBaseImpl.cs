
using Terminal.Gui.Views;

namespace Terminal.Gui.App;

/// <summary>
///     Abstract base class for popover views in Terminal.Gui. 
///     Implements <see cref="IPopover"/> and inherits from <see cref="Runnable"/> to provide transparent, runnable behavior.
/// </summary>
/// <remarks>
///     <para>
///         <b>IMPORTANT:</b> Popovers must be registered with <see cref="Application.Popover"/> using
///         <see cref="ApplicationPopover.Register"/> before they can be shown.
///     </para>
///     <para>
///         <b>Default Behavior:</b><br/>
///         This base class provides:
///     </para>
///     <list type="bullet">
///         <item>Inherits from <see cref="Runnable"/>, which fills the screen by default, can focus, and implements <see cref="IRunnable"/> for session management.</item>
///         <item>Transparent viewport settings (<see cref="ViewportSettingsFlags.Transparent"/> and <see cref="ViewportSettingsFlags.TransparentMouse"/>) for visual and mouse transparency.</item>
///         <item>Automatic layout when becoming visible.</item>
///         <item>Focus restoration when hidden.</item>
///         <item>Default <see cref="Command.Quit"/> implementation that hides the popover.</item>
///     </list>
///     <para>
///         <b>Lifecycle:</b><br/>
///         Use <see cref="ApplicationPopover.Show"/> to display and <see cref="ApplicationPopover.Hide"/> or
///         set <see cref="View.Visible"/> to <see langword="false"/> to hide.
///     </para>
/// </remarks>
public abstract class PopoverBaseImpl : Runnable, IPopover
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverBaseImpl"/> class.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Sets up default popover behavior:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Inherits runnable behavior from <see cref="Runnable"/> (fills the screen, can focus, etc.).</item>
    ///         <item>Configures <see cref="View.ViewportSettings"/> with <see cref="ViewportSettingsFlags.Transparent"/> and <see cref="ViewportSettingsFlags.TransparentMouse"/> for visual and mouse transparency.</item>
    ///         <item>Adds <see cref="Command.Quit"/> bound to <see cref="Application.QuitKey"/> which hides the popover when invoked.</item>
    ///     </list>
    /// </remarks>
    protected PopoverBaseImpl ()
    {
        Id = "popoverBaseImpl";
        // Make the popover transparent (visually and to the mouse)
        ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;

        // Add Command.Quit to hide the popover when user presses QuitKey
        AddCommand (Command.Quit, Quit);
        KeyBindings.Add (Application.QuitKey, Command.Quit);

        return;

        bool? Quit (ICommandContext? ctx)
        {
            if (!Visible)
            {
                return false;
            }

            Visible = false;

            return true;
        }
    }

    private IRunnable? _current;

    /// <inheritdoc/>
    public IRunnable? Current
    {
        get => _current;
        set
        {
            _current = value;
            App ??= (_current as View)?.App;
        }
    }

    /// <summary>
    ///     Called when the <see cref="View.Visible"/> property is changing. Handles layout and focus management.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> to cancel the visibility change; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         <b>When becoming visible:</b> Lays out the popover to fit the screen.
    ///     </para>
    ///     <para>
    ///         <b>When becoming hidden:</b> Restores focus to the previously focused view in the view hierarchy.
    ///     </para>
    /// </remarks>
    protected override bool OnVisibleChanging ()
    {
        bool ret = base.OnVisibleChanging ();

        if (ret)
        {
            return ret;
        }

        if (!Visible)
        {
            // Whenever visible is changing to true, we need to resize;
            // it's our only chance because we don't get laid out until we're visible
            if (App is { })
            {
                Layout (App.Screen.Size);
            }
        }
        else
        {
            // Whenever visible is changing to false, we need to reset the focus
            if (ApplicationNavigation.IsInHierarchy (this, App?.Navigation?.GetFocused ()))
            {
                App?.Navigation?.SetFocused (App?.TopRunnableView?.MostFocused);
            }
        }

        return ret;
    }
}
