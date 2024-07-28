using System;
using System.Text;
using Terminal.Gui;

namespace UICatalog.Scenarios;

/// <summary>
///     Provides an editor UI for the Margin, Border, and Padding of a View.
/// </summary>
public class AdornmentsEditor : View
{
    private View _viewToEdit;

    private Label _lblView; // Text describing the vi

    private MarginEditor _marginEditor;
    private BorderEditor _borderEditor;
    private PaddingEditor _paddingEditor;

    // TODO: Move Diagnostics to a separate Editor class (DiagnosticsEditor?).
    private CheckBox _diagPaddingCheckBox;
    private CheckBox _diagRulerCheckBox;
    private readonly ViewDiagnosticFlags _savedDiagnosticFlags = Diagnostics;

    public AdornmentsEditor ()
    {
        //ColorScheme = Colors.ColorSchemes ["Dialog"];
        Title = $"AdornmentsEditor";

        Width = Dim.Auto (DimAutoStyle.Content);
        Height = Dim.Auto (DimAutoStyle.Content);

        //SuperViewRendersLineCanvas = true;

        TabStop = TabBehavior.TabGroup;

        Application.MouseEvent += Application_MouseEvent;
        Initialized += AdornmentsEditor_Initialized;
    }

    /// <summary>
    /// Gets or sets whether the AdornmentsEditor should automatically select the View to edit when the mouse is clicked
    /// anywhere outside the editor.
    /// </summary>
    public bool AutoSelectViewToEdit { get; set; }

    private void AdornmentsEditor_Initialized (object sender, EventArgs e)
    {
        BorderStyle = LineStyle.Dotted;

        ExpanderButton expandButton = new ExpanderButton ()
        {
            Orientation = Orientation.Horizontal
        };
        Border.Add (expandButton);

        _lblView = new ()
        {
            X = 0,
            Y = 0,
            Height = 2,
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
        _diagPaddingCheckBox.State = Diagnostics.FastHasFlags (ViewDiagnosticFlags.Padding) ? CheckState.Checked : CheckState.UnChecked;

        _diagPaddingCheckBox.Toggle += (s, e) =>
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
        _diagRulerCheckBox.State = Diagnostics.FastHasFlags(ViewDiagnosticFlags.Ruler) ? CheckState.Checked : CheckState.UnChecked;

        _diagRulerCheckBox.Toggle += (s, e) =>
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
    }

    private void Application_MouseEvent (object sender, MouseEvent e)
    {
        if (!AutoSelectViewToEdit || FrameToScreen ().Contains (e.Position))
        {
            return;
        }

        // TODO: Add a setting (property) so only subviews of a specified view are considered.
        var view = e.View;
        if (view is { } && e.Flags == MouseFlags.Button1Clicked)
        {
            if (view is Adornment adornment)
            {
                ViewToEdit = adornment.Parent;
            }
            else
            {
                ViewToEdit = view;
            }
        }
    }

    /// <inheritdoc />
    protected override void Dispose (bool disposing)
    {
        View.Diagnostics = _savedDiagnosticFlags;
        base.Dispose (disposing);
    }

    public View ViewToEdit
    {
        get => _viewToEdit;
        set
        {
            if (_viewToEdit == value)
            {
                return;
            }

            _viewToEdit = value;


            _marginEditor.AdornmentToEdit = _viewToEdit.Margin ?? null;
            _borderEditor.AdornmentToEdit = _viewToEdit.Border ?? null;
            _paddingEditor.AdornmentToEdit = _viewToEdit.Padding ?? null;

            _lblView.Text = _viewToEdit.ToString ();

            return;
        }
    }
}