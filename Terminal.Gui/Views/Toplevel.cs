namespace Terminal.Gui.Views;

public partial class Toplevel : Runnable<object?>
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

    private void Toplevel_MouseClick (object? sender, MouseEventArgs e) { e.Handled = InvokeCommand (Command.HotKey) == true; }

    #endregion

}
