#nullable enable

namespace Terminal.Gui.App;

/// <summary>
///     Abstract base class for popover views in Terminal.Gui.
/// </summary>
/// <remarks>
///     <para>
///         <b>Popover Lifecycle:</b><br/>
///         To display a popover, use <see cref="ApplicationPopover.Show"/>. To hide a popover, either call
///         <see cref="ApplicationPopover.Hide"/>,
///         set <see cref="View.Visible"/> to <see langword="false"/>, or show another popover.
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
public abstract class PopoverBaseImpl : View, IPopover
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="PopoverBaseImpl"/> class.
    /// </summary>
    /// <remarks>
    ///     By default, the popover fills the available screen area and is focusable.
    /// </remarks>
    protected PopoverBaseImpl ()
    {
        Id = "popoverBaseImpl";
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
    public Toplevel? Toplevel { get; set; }

    /// <summary>
    ///     Called when the <see cref="View.Visible"/> property is changing.
    /// </summary>
    /// <remarks>
    ///     When becoming visible, the popover is laid out to fit the screen.
    ///     When becoming hidden, focus is restored to the previous view.
    /// </remarks>
    /// <returns>
    ///     <see langword="true"/> to cancel the visibility change; otherwise, <see langword="false"/>.
    /// </returns>
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
            Layout (Application.Screen.Size);
        }
        else
        {
            // Whenever visible is changing to false, we need to reset the focus
            if (ApplicationNavigation.IsInHierarchy (this, Application.Navigation?.GetFocused ()))
            {
                Application.Navigation?.SetFocused (Application.Top?.MostFocused);
            }
        }

        return ret;
    }
}
