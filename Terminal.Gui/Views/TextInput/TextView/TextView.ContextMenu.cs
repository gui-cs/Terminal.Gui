using System.Globalization;

namespace Terminal.Gui.Views;

/// <summary>Context menu functionality</summary>
public partial class TextView
{
    private PopoverMenu CreateContextMenu ()
    {
        PopoverMenu menu = new (
                                new List<View>
                                {
                                    new MenuItem (this, Command.SelectAll, Strings.ctxSelectAll),
                                    new MenuItem (this, Command.DeleteAll, Strings.ctxDeleteAll),
                                    new MenuItem (this, Command.Copy, Strings.ctxCopy),
                                    new MenuItem (this, Command.Cut, Strings.ctxCut),
                                    new MenuItem (this, Command.Paste, Strings.ctxPaste),
                                    new MenuItem (this, Command.Undo, Strings.ctxUndo),
                                    new MenuItem (this, Command.Redo, Strings.ctxRedo)
                                });

        menu.KeyChanged += ContextMenu_KeyChanged;

        return menu;
    }

    private void ShowContextMenu (Point? mousePosition)
    {
        if (!Equals (_currentCulture, Thread.CurrentThread.CurrentUICulture))
        {
            _currentCulture = Thread.CurrentThread.CurrentUICulture;
        }

        if (mousePosition is null)
        {
            mousePosition = ViewportToScreen (new Point (CursorPosition.X, CursorPosition.Y));
        }

        ContextMenu?.MakeVisible (mousePosition);
    }

    private void ContextMenu_KeyChanged (object? sender, KeyChangedEventArgs e) 
    { 
        KeyBindings.Replace (e.OldKey, e.NewKey); 
    }
}
