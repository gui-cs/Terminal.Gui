namespace Terminal.Gui.Views;

/// <summary>
///     Toplevel views are used for both an application's main view (filling the entire screen and for modal (pop-up)
///     views such as <see cref="Dialog"/>, <see cref="MessageBox"/>, and <see cref="Wizard"/>).
/// </summary>
/// <remarks>
///     <para>
///         Toplevel views can run as modal (popup) views, started by calling
///         <see cref="IApplication.Run(Toplevel, Func{Exception, bool})"/>. They return control to the caller when
///         <see cref="IApplication.RequestStop(Toplevel)"/> has been called (which sets the <see cref="IsRunning"/>
///         property to <c>false</c>).
///     </para>
///     <para>
///         A Toplevel is created when an application initializes Terminal.Gui by calling <see cref="IApplication.Init"/>.
///         The application Toplevel can be accessed via <see cref="IApplication.TopRunnableView"/>. Additional Toplevels can be created
///         and run (e.g. <see cref="Dialog"/>s). To run a Toplevel, create the <see cref="Toplevel"/> and call
///         <see cref="IApplication.Run(Toplevel, Func{Exception, bool})"/>.
///     </para>
/// </remarks>
public partial class Toplevel : Runnable<int?>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Toplevel"/> class,
    ///     defaulting to full screen. The <see cref="View.Width"/> and <see cref="View.Height"/> properties will be set to the
    ///     dimensions of the terminal using <see cref="Dim.Fill(Dim)"/>.
    /// </summary>
    public Toplevel ()
    {
        CanFocus = true;
        TabStop = TabBehavior.TabGroup;
        Arrangement = ViewArrangement.Overlapped;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Toplevel);

        MouseClick += Toplevel_MouseClick;
    }

    #region Keyboard & Mouse

    // TODO: IRunnable: Re-implement - Modal means IRunnable, ViewArrangement.Overlapped where modalView.Z > allOtherViews.Max (v = v.Z), and exclusive key/mouse input.
    /// <summary>
    ///     Determines whether the <see cref="Toplevel"/> is modal or not. If set to <c>false</c> (the default):
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="View.OnKeyDown"/> events will propagate keys upwards.</description>
    ///         </item>
    ///         <item>
    ///             <description>The Toplevel will act as an embedded view (not a modal/pop-up).</description>
    ///         </item>
    ///     </list>
    ///     If set to <c>true</c>:
    ///     <list type="bullet">
    ///         <item>
    ///             <description><see cref="View.OnKeyDown"/> events will NOT propagate keys upwards.</description>
    ///         </item>
    ///         <item>
    ///             <description>The Toplevel will and look like a modal (pop-up) (e.g. see <see cref="Dialog"/>.</description>
    ///         </item>
    ///     </list>
    /// </summary>
    public bool Modal { get; set; }

    private void Toplevel_MouseClick (object? sender, MouseEventArgs e) { e.Handled = InvokeCommand (Command.HotKey) == true; }

    #endregion

    #region SubViews

    //// TODO: Deprecate - Any view can host a menubar in v2
    ///// <summary>Gets the latest <see cref="MenuBar"/> added into this Toplevel.</summary>
    //public MenuBar? MenuBar => (MenuBar?)SubViews?.LastOrDefault (s => s is MenuBar);

    #endregion

    #region Life Cycle

    // TODO: Deprecate. Other than a few tests, this is not used anywhere.
    /// <summary>
    ///     <see langword="true"/> if was already loaded by the <see cref="IApplication.Begin(Toplevel)"/>
    ///     <see langword="false"/>, otherwise.
    /// </summary>
    public bool IsLoaded { get; private set; }

    #endregion

    #region Size / Position Management

    // TODO: Make cancelable?
    internal void OnSizeChanging (SizeChangedEventArgs size) { SizeChanging?.Invoke (this, size); }

    /// <summary>
    ///     Adjusts the location and size of <paramref name="top"/> within this Toplevel. Virtual method enabling
    ///     implementation of specific positions for inherited <see cref="Toplevel"/> views.
    /// </summary>
    /// <param name="top">The Toplevel to adjust.</param>
    public virtual void PositionToplevel (Toplevel? top)
    {
        if (top is null)
        {
            return;
        }

        View? superView = GetLocationEnsuringFullVisibility (
                                                            top,
                                                            top.Frame.X,
                                                            top.Frame.Y,
                                                            out int nx,
                                                            out int ny
                                                           //,
                                                           // out StatusBar? sb
                                                           );

        if (superView is null)
        {
            return;
        }

        //var layoutSubViews = false;
        var maxWidth = 0;

        if (superView.Margin is { } && superView == top.SuperView)
        {
            maxWidth -= superView.GetAdornmentsThickness ().Left + superView.GetAdornmentsThickness ().Right;
        }

        // BUGBUG: The && true is a temp hack
        if ((superView != top || top?.SuperView is { } || (top != App?.TopRunnableView && top!.Modal) || (top == App?.TopRunnableView && top?.SuperView is null))
            && (top!.Frame.X + top.Frame.Width > maxWidth || ny > top.Frame.Y))

        {
            if (top?.X is null or PosAbsolute && top?.Frame.X != nx)
            {
                top!.X = nx;
                //layoutSubViews = true;
            }

            if (top?.Y is null or PosAbsolute && top?.Frame.Y != ny)
            {
                top!.Y = ny;
                //layoutSubViews = true;
            }
        }


        //if (superView.IsLayoutNeeded () || layoutSubViews)
        //{
        //    superView.LayoutSubViews ();
        //}

        //if (IsLayoutNeeded ())
        //{
        //    LayoutSubViews ();
        //}
    }

    /// <summary>Invoked when the terminal has been resized. The new <see cref="Size"/> of the terminal is provided.</summary>
    public event EventHandler<SizeChangedEventArgs>? SizeChanging;

    #endregion
}
