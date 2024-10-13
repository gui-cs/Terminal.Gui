using System;
using System.ComponentModel;
using System.Reflection;

namespace Terminal.Gui;

/// <summary>
/// </summary>
public class Menuv2 : Bar
{
    /// <inheritdoc/>
    public Menuv2 () : this ([]) { }

    /// <inheritdoc/>
    public Menuv2 (IEnumerable<Shortcut> shortcuts) : base (shortcuts)
    {
        Orientation = Orientation.Vertical;
        Width = Dim.Auto ();
        Height = Dim.Auto (DimAutoStyle.Content, 1);
        Initialized += Menuv2_Initialized;
        VisibleChanged += OnVisibleChanged;
    }

    private void OnVisibleChanged (object sender, EventArgs e)
    {
        if (Visible)
        {
            //Application.GrabMouse(this);
        }
        else
        {
            if (Application.MouseGrabView == this)
            {
                //Application.UngrabMouse ();
            }
        }
    }

    private void Menuv2_Initialized (object sender, EventArgs e)
    {
        Border.Thickness = new Thickness (1, 1, 1, 1);
        Border.LineStyle = LineStyle.Single;
        ColorScheme = Colors.ColorSchemes ["Menu"];
    }

    // Menuv2 arranges the items horizontally.
    // The first item has no left border, the last item has no right border.
    // The Shortcuts are configured with the command, help, and key views aligned in reverse order (EndToStart).
    internal override void OnLayoutStarted (LayoutEventArgs args)
    {
        for (int index = 0; index < Subviews.Count; index++)
        {
            View barItem = Subviews [index];

            if (!barItem.Visible)
            {
                continue;
            }

        }
        base.OnLayoutStarted (args);
    }

    /// <inheritdoc/>
    public override View Add (View view)
    {
        base.Add (view);

        if (view is Shortcut shortcut)
        {
            shortcut.CanFocus = true;
            shortcut.Orientation = Orientation.Vertical;
            shortcut.HighlightStyle |= HighlightStyle.Hover;

            shortcut.Accepting += ShortcutOnAccepting;

            AddCommand (shortcut.Command, (ctx) =>
                                          {
                                              return RaiseShortcutCommandInvoked (ctx);
                                          });


            void ShortcutOnAccepting (object sender, CommandEventArgs e)
            {
                if (Arrangement.HasFlag (ViewArrangement.Overlapped) && Visible)
                {
                    Visible = false;
//                    e.Cancel = true;

                    return;
                }
            }
        }

        return view;
    }


    protected bool? RaiseShortcutCommandInvoked (CommandContext ctx)
    {
        CommandEventArgs args = new () { Context = ctx };

        // Best practice is to invoke the virtual method first.
        // This allows derived classes to handle the event and potentially cancel it.
        args.Cancel = OnShortcutCommandInvoked (args) || args.Cancel;

        if (!args.Cancel)
        {
            // If the event is not canceled by the virtual method, raise the event to notify any external subscribers.
            ShortcutCommandInvoked?.Invoke (this, args);
        }

        return ShortcutCommandInvoked is null ? null : args.Cancel;
    }

    /// <summary>
    ///     Called when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked. Set CommandEventArgs.Cancel to
    ///     <see langword="true"/> and return <see langword="true"/> to stop processing.
    /// </summary>
    /// <remarks>
    /// <para>
    ///    See <see cref="ShortcutCommandInvoked"/> for more information.
    /// </para>
    /// </remarks>
    /// <param name="args"></param>
    /// <returns><see langword="true"/> to stop processing.</returns>
    protected virtual bool OnShortcutCommandInvoked (CommandEventArgs args) { return false; }

    /// <summary>
    ///     Cancelable event raised when the user is accepting the state of the View and the <see cref="Command.Accept"/> has been invoked. Set
    ///     CommandEventArgs.Cancel to cancel the event.
    /// </summary>
    /// <remarks>
    /// <para>
    ///    See <see cref="RaiseShortcutCommandInvoked"/> for more information.
    /// </para>
    /// </remarks>
    public event EventHandler<CommandEventArgs>? ShortcutCommandInvoked;
}
