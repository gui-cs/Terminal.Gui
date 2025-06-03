#nullable enable
using System;
using System.Text;

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
///         If <see cref="Orientation"/> is set to <see cref="Orientation.Vertical"/>, the button will appear
///         at the top/right.
///         If <see cref="Orientation"/> is set to <see cref="Orientation.Horizontal"/>, the button will
///         appear at the
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
        AddCommand (Command.Toggle, Toggle);
        KeyBindings.Add (Key.F4, Command.Toggle);

        Orientation = Orientation.Vertical;

        HighlightStates = Terminal.Gui.ViewBase.MouseState.None;

        Initialized += ExpanderButton_Initialized;

        EnabledChanged += (sender, args) =>
                          {
                              ShowHide ();
                          };
    }

    private void ShowHide ()
    {
        if (!Enabled)
        {
            Visible = false;
        }

        if (SuperView is Border { } border)
        {
            switch (Orientation)
            {
                case Orientation.Vertical:
                    Visible = border.Thickness.Top > 0;

                    break;

                case Orientation.Horizontal:
                    Visible = border.Border?.Thickness.Left > 0;

                    break;
            }
        }
    }

    private void ExpanderButton_Initialized (object? sender, EventArgs e)
    {
        ShadowStyle = ShadowStyle.None;

        ExpandOrCollapse (Collapsed);

        if (SuperView is Border { } border)
        {
            border.ThicknessChanged += (o, args) => ShowHide ();
        }
    }

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
        CancelEventArgs<Orientation> args = new CancelEventArgs<Orientation> (in _orientation, ref newOrientation);
        OrientationChanging?.Invoke (this, args);

        if (!args.Cancel)
        {
            _orientation = newOrientation;

            if (Orientation == Orientation.Vertical)
            {
                X = Pos.AnchorEnd ();
                Y = 0;
                CollapseGlyph = new ('\u21d1'); // ⇑
                ExpandGlyph = new ('\u21d3'); // ⇓
            }
            else
            {
                X = 0;
                Y = Pos.AnchorEnd ();
                CollapseGlyph = new ('\u21d0'); // ⇐
                ExpandGlyph = new ('\u21d2'); // ⇒
            }

            ExpandOrCollapse (Collapsed);
        }

        return args.Cancel;
    }

    /// <summary>
    ///     Fired when the orientation has changed. Can be cancelled.
    /// </summary>
    public event EventHandler<CancelEventArgs<Orientation>>? OrientationChanging;

    /// <summary>
    ///     The glyph that indicates the button will collapse the view.
    /// </summary>
    public Rune CollapseGlyph { get; set; }

    /// <summary>
    ///     The glyph that indicates the button will expand the view.
    /// </summary>
    public Rune ExpandGlyph { get; set; }

    private bool _collapsed;

    /// <summary>
    ///     Gets or sets a value indicating whether the view is collapsed.
    /// </summary>
    public bool Collapsed
    {
        get => _collapsed;
        set => OnCollapsedChanging (value);
    }

    /// <summary>Called when the orientation is changing. Invokes the <see cref="OrientationChanging"/> event.</summary>
    /// <param name="newValue"></param>
    /// <returns>True of the event was cancelled.</returns>
    protected virtual bool OnCollapsedChanging (bool newValue)
    {
        CancelEventArgs<bool> args = new (ref _collapsed, ref newValue);
        CollapsedChanging?.Invoke (this, args);

        if (!args.Cancel)
        {
            _collapsed = args.NewValue;

            ExpandOrCollapse (_collapsed);
        }

        return args.Cancel;
    }

    /// <summary>
    ///     Fired when the orientation has changed. Can be cancelled.
    /// </summary>
    public event EventHandler<CancelEventArgs<bool>>? CollapsedChanging;

    /// <summary>
    ///     Collapses or Expands the view.
    /// </summary>
    /// <returns></returns>
    public bool? Toggle ()
    {
        Collapsed = !Collapsed;

        return true;
    }

    private Dim? _previousDim;

    private void ExpandOrCollapse (bool collapse)
    {
        Text = $"{(Collapsed ? ExpandGlyph : CollapseGlyph)}";

        View? superView = SuperView;

        if (superView is Adornment adornment)
        {
            superView = adornment.Parent;
        }

        if (superView is null)
        {
            return;
        }

        if (collapse)
        {
            // Collapse
            if (Orientation == Orientation.Vertical)
            {
                _previousDim = superView!.Height!;
                superView.Height = 1;
            }
            else
            {
                _previousDim = superView!.Width!;
                superView.Width = 1;
            }
        }
        else
        {
            if (_previousDim is null)
            {
                return;
            }

            // Expand
            if (Orientation == Orientation.Vertical)
            {
                superView.Height = _previousDim;
            }
            else
            {
                superView.Width = _previousDim;
            }
        }

        foreach (View subview in superView.SubViews)
        {
            subview.Visible = !Collapsed;
        }
    }
}
