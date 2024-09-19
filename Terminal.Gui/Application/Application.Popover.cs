#nullable enable
namespace Terminal.Gui;

public static partial class Application // Popover handling
{
    /// <summary>The <see cref="View"/> that is currently active as the sole-Application Popover.</summary>
    /// <value>The Popover.</value>
    public static View? Popover { get; internal set; }
    public static bool ShowPopover (View popoverView)
    {
        if (Popover is { })
        {
            Popover.Visible = false;
        }

        if (!popoverView.IsInitialized)
        {
            popoverView.BeginInit ();
            popoverView.EndInit ();
        }

        Popover = popoverView;
        Popover.Visible = true;
        Popover.SetRelativeLayout (Screen.Size);

        return true;
    }

    public static void HidePopover ()
    {
        if (Popover is { })
        {
            Popover.Visible = false;
        }
    }
}
