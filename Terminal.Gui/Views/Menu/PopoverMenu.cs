#nullable enable
namespace Terminal.Gui;

/// <summary>
/// 
/// </summary>
public class PopoverMenu : View
{
    /// <summary>
    /// 
    /// </summary>
    public PopoverMenu () : this (null)
    {

    }

    /// <summary>
    /// 
    /// </summary>
    public PopoverMenu (Menuv2? root)
    {
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        //ViewportSettings = ViewportSettings.Transparent | ViewportSettings.TransparentMouse;
        //base.Visible = false;
        base.ColorScheme = Colors.ColorSchemes ["Menu"];

        Root = root;

    }

    private Menuv2? _root;

    /// <summary>
    /// 
    /// </summary>
    public Menuv2? Root
    {
        get => _root;
        set
        {
            if (_root == value)
            {
                return;
            }

            if (_root is { })
            {
                base.Remove (_root);
                _root.Accepting -= RootOnAccepting;
                _root.MenuItemCommandInvoked -= RootOnMenuItemCommandInvoked;
            }

            _root = value;

            if (_root is { })
            {
                base.Add (_root);
                _root.Accepting += RootOnAccepting;
                _root.MenuItemCommandInvoked += RootOnMenuItemCommandInvoked;
            }

            return;

            void RootOnMenuItemCommandInvoked (object? sender, CommandEventArgs e)
            {
                Logging.Trace ($"RootOnMenuItemCommandInvoked: {e.Context}");
            }

            void RootOnAccepting (object? sender, CommandEventArgs e)
            {
                Logging.Trace ($"RootOnAccepting: {e.Context}");
            }
        }
    }
}
