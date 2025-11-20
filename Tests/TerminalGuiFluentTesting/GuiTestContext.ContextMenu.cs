using System.Diagnostics;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace TerminalGuiFluentTesting;

public partial class GuiTestContext
{
    /// <summary>
    ///     Registers a right click handler on the <see cref="LastView"/> added view (or root view) that
    ///     will open the supplied <paramref name="contextMenu"/>.
    /// </summary>
    /// <param name="contextMenu"></param>
    /// <returns></returns>
    public GuiTestContext WithContextMenu (PopoverMenu? contextMenu)
    {
        if (contextMenu?.App is null)
        {
            Fail (@"PopoverMenu's must have their App property set.");
        }
        LastView.MouseEvent += (_, e) =>
                               {
                                   if (e.Flags.HasFlag (MouseFlags.Button3Clicked))
                                   {
                                       // Registering with the PopoverManager will ensure that the context menu is closed when the view is no longer focused
                                       // and the context menu is disposed when it is closed.
                                       App?.Popover?.Register (contextMenu);
                                       contextMenu?.MakeVisible (e.ScreenPosition);
                                   }
                               };

        return this;
    }
}