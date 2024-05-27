using System;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     A Button that can expand or collapse a view.
/// </summary>
/// <remarks>
///     <para>
///         Add this button to a view's Border to allow the user to expand or collapse the view via either the keyboard
///         (F4) or mouse.
///     </para>
///     <para>
///         If <see cref="Orientation"/> is set to <see cref="Terminal.Gui.Orientation.Vertical"/>, the button will appear
///         at the top/right.
///         If <see cref="Orientation"/> is set to <see cref="Orientation.Horizontal"/>, the button will appear at the
///         bottom/left.
///     </para>
/// </remarks>
/// <example>
///     private void MyView_Initialized (object sender, EventArgs e)
///     {
///     Border.Add(new ExpanderButton ());
///     ...
/// </example>
public class ExpanderButton : Button
{
    public ExpanderButton ()
    {
        CanFocus = false;

        Width = 1;
        Height = 1;
        NoDecorations = true;
        NoPadding = true;

        AddCommand (Command.HotKey, Toggle);
        AddCommand (Command.ToggleExpandCollapse, Toggle);
        KeyBindings.Add (Key.F4, Command.ToggleExpandCollapse);

        Orientation = Orientation.Vertical;

        Initialized += ExpanderButton_Initialized;
    }

    private void ExpanderButton_Initialized (object sender, EventArgs e) { Orient (); }

    private Orientation _orientation = Orientation.Horizontal;

    /// <summary>Orientation.</summary>
    /// <remarks>
    ///     <para>
    ///         If <see cref="Orientation"/> is set to <see cref="Orientation.Vertical"/>, the button will appear at the
    ///         top/right.
    ///         If <see cref="Orientation"/> is set to <see cref="Orientation.Horizontal"/>, the button will appear at the
    ///         bottom/left.
    ///     </para>
    /// </remarks>
    public Orientation Orientation
    {
        get => _orientation;
        set => OnOrientationChanging (value);
    }

    /// <summary>Called when the orientation is changing. Invokes the <see cref="OrientationChanging"/> event.</summary>
    /// <param name="newOrientation"></param>
    /// <returns>True of the event was cancelled.</returns>
    protected virtual bool OnOrientationChanging (Orientation newOrientation)
    {
        var args = new OrientationEventArgs (newOrientation);
        OrientationChanging?.Invoke (this, args);

        if (!args.Cancel)
        {
            _orientation = newOrientation;

            Orient ();
        }

        return args.Cancel;
    }

    /// <summary>
    ///     Fired when the orientation has changed. Can be cancelled by setting
    ///     <see cref="OrientationEventArgs.Cancel"/> to true.
    /// </summary>
    public event EventHandler<OrientationEventArgs> OrientationChanging;

    /// <summary>
    ///     The glyph to display when the view is collapsed.
    /// </summary>
    public Rune CollapsedGlyph { get; set; }

    /// <summary>
    ///     The glyph to display when the view is expanded.
    /// </summary>
    public Rune ExpandedGlyph { get; set; }

    private bool _collapsed = true;

    /// <summary>
    ///     Gets or sets a value indicating whether the view is collapsed.
    /// </summary>
    public bool Collapsed
    {
        get => _collapsed;
        set => OnCollapsedChanging (value);
    }

    /// <summary>Called when the orientation is changing. Invokes the <see cref="OrientationChanging"/> event.</summary>
    /// <param name="newOrientation"></param>
    /// <returns>True of the event was cancelled.</returns>
    protected virtual bool OnCollapsedChanging (bool newValue)
    {
        StateEventArgs<bool> args = new (Collapsed, newValue);
        CollapsedChanging?.Invoke (this, args);

        if (!args.Cancel)
        {
            _collapsed = newValue;

            View superView = SuperView;

            if (superView is Adornment adornment)
            {
                superView = adornment.Parent;
            }

            bool expanded = Orientation == Orientation.Vertical ? superView.Viewport.Height > 0 : superView.Viewport.Width > 0;
            Orient ();

            foreach (View subview in superView.Subviews)
            {
                subview.Visible = !expanded;
            }

            Text = $"{(Collapsed ? CollapsedGlyph : ExpandedGlyph)}";
        }

        return args.Cancel;
    }

    /// <summary>
    ///     Fired when the orientation has changed. Can be cancelled by setting
    ///     <see cref="OrientationEventArgs.Cancel"/> to true.
    /// </summary>
    public event EventHandler<StateEventArgs<bool>> CollapsedChanging;

    // TODO: This is a workaround for Dim.Auto() not working as expected.
    /// <summary>
    ///     Gets or sets the widest/tallest dimension of the view.
    /// </summary>
    public int Widest { get; set; }

    /// <summary>
    ///     Collapses or Expands the view.
    /// </summary>
    /// <returns></returns>
    public bool? Toggle ()
    {
        Collapsed = !Collapsed;

        return true;
    }

    private void Orient ()
    {
        if (!IsInitialized)
        {
            return;
        }

        View superView = SuperView;

        if (superView is Adornment adornment)
        {
            superView = adornment.Parent;
        }

        bool expanded = Orientation == Orientation.Vertical ? superView.Viewport.Height > 0 : superView.Viewport.Width > 0;

        if (expanded)
        {
            if (Orientation == Orientation.Vertical)
            {
                Widest = superView.ContentSize.Width;
                superView.Height = 1;
            }
            else
            {
                Widest = superView.ContentSize.Height;
                superView.Width = 1;
            }
        }
        else
        {
            if (Orientation == Orientation.Vertical)
            {
                superView.Height = Dim.Auto ();
            }
            else
            {
                superView.Width = Dim.Auto ();
            }
        }

        if (Orientation == Orientation.Vertical)
        {
            X = Pos.AnchorEnd ();
            Y = 0;
            CollapsedGlyph = new ('\u21d1'); // ⇑
            ExpandedGlyph = new ('\u21d3'); // ⇓
        }
        else
        {
            X = 0;
            Y = Pos.AnchorEnd ();
            CollapsedGlyph = new ('\u21d0'); // ⇐
            ExpandedGlyph = new ('\u21d2'); // ⇒
        }

        Text = $"{(Collapsed ? CollapsedGlyph : ExpandedGlyph)}";
    }
}
