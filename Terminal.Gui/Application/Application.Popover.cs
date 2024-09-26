#nullable enable
namespace Terminal.Gui;

public static partial class Application // Popover handling
{
    private static View? _popover;

    /// <summary>Gets or sets the Application Popover View.</summary>
    /// <remarks>
    ///     <para>
    ///         To show or hide the Popover, set it's <see cref="View.Visible"/> property.
    ///     </para>
    /// </remarks>
    public static View? Popover
    {
        get => _popover;
        set
        {
            if (_popover == value)
            {
                return;
            }

            if (_popover is { })
            {
                _popover.Visible = false;
                _popover.VisibleChanging -= PopoverVisibleChanging;
            }

            _popover = value;

            if (_popover is { })
            {
                if (!_popover.IsInitialized)
                {
                    _popover.BeginInit ();
                    _popover.EndInit ();
                }

                _popover.Arrangement |= ViewArrangement.Overlapped;

                if (_popover.ColorScheme is null)
                {
                    _popover.ColorScheme = Top?.ColorScheme;
                }

                _popover.SetRelativeLayout (Screen.Size);

                _popover.VisibleChanging += PopoverVisibleChanging;
            }
        }
    }

    private static void PopoverVisibleChanging (object? sender, CancelEventArgs<bool> e)
    {
        if (Popover is null)
        {
            return;
        }

        if (e.NewValue)
        {
            Popover.Arrangement |= ViewArrangement.Overlapped;

            Popover.ColorScheme ??= Top?.ColorScheme;

            View.GetLocationEnsuringFullVisibility (
                                                    Popover,
                                                    Popover.Frame.X,
                                                    Popover.Frame.Y,
                                                    out int nx,
                                                    out int ny,
                                                    out StatusBar? sb
                                                   );

            Popover.X = nx;
            Popover.Y = ny;

            Popover.SetRelativeLayout (Screen.Size);

            if (Top is { })
            {
                Top.HasFocus = false;
            }

            Popover.SetFocus ();
        }
    }
}
