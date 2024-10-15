#nullable enable
using System;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class AdornmentsEditor : View
{
    public AdornmentsEditor ()
    {
        //ColorScheme = Colors.ColorSchemes ["Dialog"];
        Title = "AdornmentsEditor";

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        //SuperViewRendersLineCanvas = true;

        CanFocus = true;

        TabStop = TabBehavior.TabGroup;

        _expandButton = new ()
        {
            Orientation = Orientation.Horizontal
        };

        Initialized += AdornmentsEditor_Initialized;
    }

    private readonly ViewDiagnosticFlags _savedDiagnosticFlags = Diagnostics;
    private View? _viewToEdit;

    private Label? _lblView; // Text describing the vi

    private MarginEditor? _marginEditor;
    private BorderEditor? _borderEditor;
    private PaddingEditor? _paddingEditor;

    // TODO: Move Diagnostics to a separate Editor class (DiagnosticsEditor?).
    private CheckBox? _diagPaddingCheckBox;
    private CheckBox? _diagRulerCheckBox;

    /// <summary>
    ///     Gets or sets whether the AdornmentsEditor should automatically select the View to edit
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

    public View? ViewToEdit
    {
        get => _viewToEdit;
        set
        {
            if (_viewToEdit == value)
            {
                return;
            }

            _viewToEdit = value;

            if (_viewToEdit is not Adornment)
            {
                _marginEditor!.AdornmentToEdit = _viewToEdit?.Margin ?? null;
                _borderEditor!.AdornmentToEdit = _viewToEdit?.Border ?? null;
                _paddingEditor!.AdornmentToEdit = _viewToEdit?.Padding ?? null;
            }

            if (_lblView is { })
            {
                _lblView.Text = $"{_viewToEdit?.GetType ().Name}: {_viewToEdit?.Id}" ?? string.Empty;
            }
        }
    }


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

        View view = e.View;

        if (view is { })
        {
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

    /// <inheritdoc/>
    protected override void Dispose (bool disposing)
    {
        Diagnostics = _savedDiagnosticFlags;
        base.Dispose (disposing);
    }

    private readonly ExpanderButton? _expandButton;

    public ExpanderButton? ExpandButton => _expandButton;

    private void AdornmentsEditor_Initialized (object? sender, EventArgs e)
    {
        BorderStyle = LineStyle.Dotted;

        Border.Add (_expandButton!);

        _lblView = new ()
        {
            X = 0,
            Y = 0,
            Height = 2
        };
        _lblView.TextFormatter.WordWrap = true;
        _lblView.TextFormatter.MultiLine = true;
        _lblView.HotKeySpecifier = (Rune)'\uffff';
        Add (_lblView);

        _marginEditor = new ()
        {
            X = 0,
            Y = Pos.Bottom (_lblView),
            SuperViewRendersLineCanvas = true
        };
        Add (_marginEditor);

        _lblView.Width = Dim.Width (_marginEditor);

        _borderEditor = new ()
        {
            X = Pos.Left (_marginEditor),
            Y = Pos.Bottom (_marginEditor),
            SuperViewRendersLineCanvas = true
        };
        Add (_borderEditor);

        _paddingEditor = new ()
        {
            X = Pos.Left (_borderEditor),
            Y = Pos.Bottom (_borderEditor),
            SuperViewRendersLineCanvas = true
        };
        Add (_paddingEditor);

        _diagPaddingCheckBox = new () { Text = "_Diagnostic Padding" };
        _diagPaddingCheckBox.CheckedState = Diagnostics.FastHasFlags (ViewDiagnosticFlags.Padding) ? CheckState.Checked : CheckState.UnChecked;

        _diagPaddingCheckBox.CheckedStateChanging += (s, e) =>
                                       {
                                           if (e.NewValue == CheckState.Checked)
                                           {
                                               Diagnostics |= ViewDiagnosticFlags.Padding;
                                           }
                                           else
                                           {
                                               Diagnostics &= ~ViewDiagnosticFlags.Padding;
                                           }
                                       };

        Add (_diagPaddingCheckBox);
        _diagPaddingCheckBox.Y = Pos.Bottom (_paddingEditor);

        _diagRulerCheckBox = new () { Text = "_Diagnostic Ruler" };
        _diagRulerCheckBox.CheckedState = Diagnostics.FastHasFlags (ViewDiagnosticFlags.Ruler) ? CheckState.Checked : CheckState.UnChecked;

        _diagRulerCheckBox.CheckedStateChanging += (s, e) =>
                                     {
                                         if (e.NewValue == CheckState.Checked)
                                         {
                                             Diagnostics |= ViewDiagnosticFlags.Ruler;
                                         }
                                         else
                                         {
                                             Diagnostics &= ~ViewDiagnosticFlags.Ruler;
                                         }
                                     };

        Add (_diagRulerCheckBox);
        _diagRulerCheckBox.Y = Pos.Bottom (_diagPaddingCheckBox);

        Application.MouseEvent += ApplicationOnMouseEvent;
        Application.Navigation!.FocusedChanged += NavigationOnFocusedChanged;
    }
}
