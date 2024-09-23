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
                _popover.VisibleChanged -= PopoverVisibleChanged;
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

                _popover.VisibleChanged += PopoverVisibleChanged;
            }
        }
    }

    private static void PopoverVisibleChanged (object? sender, EventArgs e)
    {
        if (Popover is null)
        {
            return;
        }

        if (Popover.Visible)
        {
            Popover.Arrangement |= ViewArrangement.Overlapped;

            if (Popover.ColorScheme is null)
            {
                Popover.ColorScheme = Top?.ColorScheme;
            }

            Popover.SetRelativeLayout (Screen.Size);
            Popover.SetFocus ();
        }
    }
}
