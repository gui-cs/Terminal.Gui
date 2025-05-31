#nullable enable
using System;
using System.Diagnostics;
using System.Linq;

namespace UICatalog.Scenarios;

public abstract class EditorBase : View
{
    protected EditorBase ()
    {
        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        CanFocus = true;

        ExpanderButton = new ()
        {
            Orientation = Orientation.Vertical
        };


        TabStop = TabBehavior.TabStop;

        Initialized += OnInitialized;

        void OnInitialized (object? sender, EventArgs e)
        {
            if (Border is { })
            {
                Border.Add (ExpanderButton);

                if (ExpanderButton.Orientation == Orientation.Vertical)
                {
                    ExpanderButton.X = Pos.AnchorEnd () - 1;
                }
                else
                {
                    ExpanderButton.Y = Pos.AnchorEnd () - 1;
                }
            }

            Application.MouseEvent += ApplicationOnMouseEvent;
            Application.Navigation!.FocusedChanged += NavigationOnFocusedChanged;

        }

        AddCommand (Command.Accept, () => true);

        SchemeName = "Dialog";

    }

    private readonly ExpanderButton? _expanderButton;

    public ExpanderButton? ExpanderButton
    {
        get => _expanderButton;
        init
        {
            if (_expanderButton == value)
            {
                return;
            }
            _expanderButton = value;
        }
    }

    public bool UpdatingLayoutSettings { get; private set; } = false;

    private void View_LayoutComplete (object? sender, LayoutEventArgs e)
    {
        UpdatingLayoutSettings = true;

        OnUpdateLayoutSettings ();

        UpdatingLayoutSettings = false;
    }


    private View? _viewToEdit;

    public View? ViewToEdit
    {
        get => _viewToEdit;
        set
        {
            if (_viewToEdit == value)
            {
                return;
            }


            if (value is null && _viewToEdit is { })
            {
                _viewToEdit.SubViewsLaidOut -= View_LayoutComplete;
            }

            _viewToEdit = value;

            if (_viewToEdit is { })
            {
                _viewToEdit.SubViewsLaidOut += View_LayoutComplete;
            }

            OnViewToEditChanged ();
        }
    }

    protected virtual void OnViewToEditChanged () { }

    protected virtual void OnUpdateLayoutSettings () { }

    /// <summary>
    ///     Gets or sets whether the DimEditor should automatically select the View to edit
    ///     based on the values of <see cref="AutoSelectSuperView"/> and <see cref="AutoSelectAdornments"/>.
    /// </summary>
    public bool AutoSelectViewToEdit { get; set; }

    /// <summary>
    ///     Gets or sets the View that will scope the behavior of <see cref="AutoSelectViewToEdit"/>.
    /// </summary>
    public View? AutoSelectSuperView { get; set; }

    /// <summary>
    ///     Gets or sets whether auto select with the mouse will select Adornments or just Views.
    /// </summary>
    public bool AutoSelectAdornments { get; set; }


    private void NavigationOnFocusedChanged (object? sender, EventArgs e)
    {
        if (AutoSelectSuperView is null)
        {
            return;
        }

        if (ApplicationNavigation.IsInHierarchy (this, Application.Navigation!.GetFocused ()))
        {
            return;
        }

        if (!ApplicationNavigation.IsInHierarchy (AutoSelectSuperView, Application.Navigation!.GetFocused ()))
        {
            return;
        }

        ViewToEdit = Application.Navigation!.GetFocused ();
    }

    private void ApplicationOnMouseEvent (object? sender, MouseEventArgs e)
    {
        if (e.Flags != MouseFlags.Button1Clicked || !AutoSelectViewToEdit)
        {
            return;
        }

        if ((AutoSelectSuperView is { } && !AutoSelectSuperView.FrameToScreen ().Contains (e.Position))
            || FrameToScreen ().Contains (e.Position))
        {
            return;
        }

        View? view = e.View;

        if (view is null)
        {
            return;
        }

        if (view is Adornment adornment)
        {
            ViewToEdit = AutoSelectAdornments ? adornment : adornment.Parent;
        }
        else
        {
            ViewToEdit = view;
        }
    }
}
