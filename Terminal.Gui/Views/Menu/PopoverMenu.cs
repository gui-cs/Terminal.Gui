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
    public PopoverMenu (Menuv2 root)
    {
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        ViewportSettings = ViewportSettings.Transparent | ViewportSettings.TransparentMouse;
        base.Visible = false;
        base.ColorScheme = Colors.ColorSchemes ["Menu"];

        Root = root;
        base.Add (root);

        root.Accepting += RootOnAccepting;
        root.MenuItemCommandInvoked += RootOnMenuItemCommandInvoked;


        return;

        void RootOnMenuItemCommandInvoked (object? sender, CommandEventArgs e)
        {
            Logging.Trace ($"RootOnMenuItemCommandInvoked: {e.Context}");
        }

        void RootOnAccepting (object? sender, CommandEventArgs e)
        {
            Logging.Trace($"RootOnAccepting: {e.Context}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public Menuv2 Root { get; init; }
}
