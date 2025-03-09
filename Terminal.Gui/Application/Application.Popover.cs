#nullable enable
using System.Diagnostics;
using static Unix.Terminal.Curses;

namespace Terminal.Gui;

public static partial class Application // Popover handling
{
    /// <summary>Gets or sets the Application Popover Host.</summary>
    /// <remarks>
    ///     <para>
    ///         To show or hide a Popover, set it's <see cref="View.Visible"/> property.
    ///     </para>
    /// </remarks>
    public static PopoverHost? PopoverHost { get; internal set; }

    private static void PopoverVisibleChanging (object? sender, CancelEventArgs<bool> e)
    {
        if (PopoverHost is null)
        {
            return;
        }

        if (e.NewValue)
        {
            PopoverHost.Arrangement |= ViewArrangement.Overlapped;

            PopoverHost.ColorScheme ??= Top?.ColorScheme;

            if (PopoverHost.NeedsLayout)
            {
                PopoverHost.SetRelativeLayout (Screen.Size);
            }

            View.GetLocationEnsuringFullVisibility (
                                                    PopoverHost,
                                                    PopoverHost.Frame.X,
                                                    PopoverHost.Frame.Y,
                                                    out int nx,
                                                    out int ny);

            PopoverHost.X = nx;
            PopoverHost.Y = ny;

            PopoverHost.SetRelativeLayout (Screen.Size);

            if (Top is { })
            {
                Top.HasFocus = false;
            }

            PopoverHost?.SetFocus ();
        }
    }
}

/// <summary>
///     
/// </summary>
public class PopoverHost : View
{
    public static void Init ()
    {
        // Setup PopoverHost
        Debug.Assert (Application.PopoverHost is null);
        Application.PopoverHost = new PopoverHost ();
        Application.PopoverHost.BeginInit ();
        Application.PopoverHost.EndInit ();
    }

    public static void Cleanup ()
    {
        Application.PopoverHost?.Dispose ();
        Application.PopoverHost = null;
    }


    /// <summary>
    /// 
    /// </summary>
    public PopoverHost ()
    {
        Id = "popoverHost";
        CanFocus = true;
        ViewportSettings = ViewportSettings.Transparent | ViewportSettings.TransparentMouse;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        base.Visible = false;
    }

    /// <inheritdoc />
    protected override bool OnClearingViewport () { return true; }

    /// <inheritdoc />
    protected override bool OnVisibleChanging ()
    {
        if (!Visible)
        {
            //ColorScheme ??= Application.Top?.ColorScheme;
            //Frame = Application.Screen;

            SetRelativeLayout (Application.Screen.Size);
        }

        return false;
    }

    /// <inheritdoc />
    protected override void OnVisibleChanged ()
    {
        if (Visible)
        {
            SetFocus ();
        }
    }
}
