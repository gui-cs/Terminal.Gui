//#nullable enable
//using System.Diagnostics;
//using System.Net.Mime;
//using static System.Net.Mime.MediaTypeNames;

//namespace Terminal.Gui;

///// <summary>
/////     Singleton View that hosts Views to be shown as Popovers. The host covers the whole screen and is transparent both
/////     visually and to the mouse. Set <see cref="View.Visible"/> to show or hide the Popovers.
///// </summary>
///// <remarks>
/////     <para>
/////         If the user clicks anywhere not occulded by a SubView of the PopoverHost, the PopoverHost will be hidden.
/////     </para>
///// </remarks>
//public sealed class PopoverHost : View
//{
//    /// <summary>
//    ///     Initializes <see cref="Application.Popover"/>.
//    /// </summary>
//    internal static void Init ()
//    {
//        // Setup PopoverHost
//        if (Application.Popover is { })
//        {
//            throw new InvalidOperationException (@"PopoverHost is a singleton; Init and Cleanup must be balanced.");
//        }

//        Application.Popover = new PopoverHost ();

//        // TODO: Add a diagnostic setting for this?
//        Application.Popover.TextFormatter.VerticalAlignment = Alignment.End;
//        Application.Popover.TextFormatter.Alignment = Alignment.End;
//        Application.Popover.Text = "popoverHost";

//        Application.Popover.BeginInit ();
//        Application.Popover.EndInit ();
//    }

//    /// <summary>
//    ///     Cleans up <see cref="Application.Popover"/>.
//    /// </summary>
//    internal static void Cleanup ()
//    {
//        Application.Popover?.Dispose ();
//        Application.Popover = null;
//    }


//    /// <summary>
//    ///     Creates a new instance.
//    /// </summary>
//    public PopoverHost ()
//    {
//        Id = "popoverHost";
//        CanFocus = true;
//        ViewportSettings = ViewportSettings.Transparent | ViewportSettings.TransparentMouse;
//        Width = Dim.Fill ();
//        Height = Dim.Fill ();
//        base.Visible = false;


//        AddCommand (Command.Quit, Quit);

//        bool? Quit (ICommandContext? ctx)
//        {
//            if (Visible)
//            {
//                Visible = false;

//                return true;
//            }

//            return null;
//        }

//        KeyBindings.Add (Application.QuitKey, Command.Quit);
//    }

//    /// <inheritdoc />
//    protected override bool OnClearingViewport () { return true; }

//    /// <inheritdoc />
//    protected override bool OnVisibleChanging ()
//    {
//        if (!Visible)
//        {
//            //ColorScheme ??= Application.Top?.ColorScheme;
//            //Frame = Application.Screen;

//            SetRelativeLayout (Application.Screen.Size);
//        }

//        return false;
//    }

//    /// <inheritdoc />
//    protected override void OnVisibleChanged ()
//    {
//        if (Visible)
//        {
//            SetFocus ();
//        }
//    }
//}
