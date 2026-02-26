
namespace Terminal.Gui.App;

/// <summary>
///     Abstract base class for popover views in Terminal.Gui. Implements <see cref="IPopover"/>.
/// </summary>
/// <remarks>
///     <para>
///         <b>IMPORTANT:</b> Popovers must be registered with <see cref="Application.Popover"/> using
///         <see cref="ApplicationPopover.Register"/> before they can be shown.
///     </para>
///     <para>
///         <b>Requirements:</b><br/>
///         Derived classes must:
///     </para>
///     <list type="bullet">
///         <item>Set <see cref="View.ViewportSettings"/> to include <see cref="ViewportSettingsFlags.Transparent"/> and <see cref="ViewportSettingsFlags.TransparentMouse"/>.</item>
///         <item>Add a key binding for <see cref="Command.Quit"/> (typically bound to <see cref="Application.QuitKey"/>).</item>
///     </list>
///     <para>
///         <b>Default Behavior:</b><br/>
///         This base class provides:
///     </para>
///     <list type="bullet">
///         <item>
///             Sets <see cref="View.Width"/> and <see cref="View.Height"/> to <see cref="Dim.Fill()"/> by default.
///             This is a <em>default</em>, not a requirement — override <see cref="View.Width"/> and
///             <see cref="View.Height"/> in derived classes for fixed-size popovers such as autocomplete dropdowns
///             or tooltips. <see cref="ViewportSettingsFlags.TransparentMouse"/> ensures click-outside-to-dismiss
///             works at any size. Fullscreen is only necessary when SubViews must extend beyond a fixed frame
///             (e.g., <see cref="PopoverMenu"/> cascading submenus).
///         </item>
///         <item>Transparent viewport settings for proper mouse event handling.</item>
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
public abstract class PopoverBaseImpl : View, IPopover
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverBaseImpl"/> class.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Sets up default popover behavior:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>Sets <see cref="View.Width"/> to <see cref="Dim.Fill()"/> and <see cref="View.Height"/> to <see cref="Dim.Fill()"/> as defaults (can be overridden by derived classes).</item>
    ///         <item>Sets <see cref="View.CanFocus"/> to <see langword="true"/>.</item>
    ///         <item>Configures <see cref="View.ViewportSettings"/> with <see cref="ViewportSettingsFlags.Transparent"/> and <see cref="ViewportSettingsFlags.TransparentMouse"/>.</item>
    ///         <item>Adds <see cref="Command.Quit"/> bound to <see cref="Application.QuitKey"/> which hides the popover when invoked.</item>
    ///     </list>
    /// </remarks>
    protected PopoverBaseImpl ()
    {
#if DEBUG
        Id = "popoverBaseImpl";
#endif
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        ViewportSettings = ViewportSettingsFlags.Transparent | ViewportSettingsFlags.TransparentMouse;

        // TODO: Add a diagnostic setting for this?
        //TextFormatter.VerticalAlignment = Alignment.End;
        //TextFormatter.Alignment = Alignment.End;
        //base.Text = "popover";

        AddCommand (Command.Quit, Quit);
        KeyBindings.Add (Application.QuitKey, Command.Quit);
        KeyBindings.Remove (Key.Enter);

        // Clear all mouse bindings so there's no conflict with subviews
        MouseBindings.Clear ();

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

    /// <inheritdoc/>
    public IRunnable? Owner
    {
        get;
        set
        {
            field = value;
            App ??= (field as View)?.App;
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
