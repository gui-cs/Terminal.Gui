#nullable enable
using System;
using System.Reflection;

namespace Terminal.Gui;

/// <summary>
///     A menu bar is a <see cref="View"/> that snaps to the top of a <see cref="Toplevel"/> displaying set of
///     <see cref="Shortcut"/>s.
/// </summary>
public class MenuBarv2 : Bar
{
    /// <inheritdoc/>
    public MenuBarv2 () : this ([]) { }

    /// <inheritdoc/>
    public MenuBarv2 (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        Y = 0;
        Width = Dim.Fill ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        BorderStyle = LineStyle.Dashed;
        ColorScheme = Colors.ColorSchemes ["Menu"];
        Orientation = Orientation.Horizontal;

        AddCommand (Command.Context,
                   (ctx) =>
                   {
                       if (ctx.Data is Shortcut shortcut)
                       {
                           Rectangle screen = shortcut.FrameToScreen ();
                           Application.Popover = shortcut.TargetView;
                           shortcut.TargetView.X = screen.X;
                           shortcut.TargetView.Y = screen.Y + screen.Height;
                           shortcut.TargetView.Visible = true;

                           return true;
                       }

                       return false;
                   });
    }

    /// <inheritdoc />
    protected override bool OnHighlight (CancelEventArgs<HighlightStyle> args)
    {
        if (args.NewValue.HasFlag (HighlightStyle.Hover))
        {
            if (Application.Popover is { Visible: true } && View.IsInHierarchy (this, Application.Popover))
            {

            }
        }
        return base.OnHighlight (args);
    }

    /// <inheritdoc/>
    public override View Add (View view)
    {
        // Call base first, because otherwise it resets CanFocus to true
        base.Add (view);

        view.CanFocus = true;

        if (view is Shortcut shortcut)
        {
            shortcut.KeyView.Visible = false;
            shortcut.HelpView.Visible = false;

            shortcut.Selecting += (sender, args) =>
                                  {
                                      args.Cancel = InvokeCommand (Command.Context, args.Context) == true;
                                  };

            shortcut.Accepting += (sender, args) =>
                                  {
                                      args.Cancel = InvokeCommand (Command.Context, args.Context) == true;
                                  };
        }

        return view;
    }
}
