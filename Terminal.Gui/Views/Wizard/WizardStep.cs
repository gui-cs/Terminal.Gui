namespace Terminal.Gui.Views;

/// <summary>
///     Represents a basic step that is displayed in a <see cref="Wizard"/>. The <see cref="WizardStep"/> fills the
///     Wizard's content area. <see cref="View"/>s can be added to the step's content area. Help text can be displayed
///     in the right <see cref="Padding"/> by setting <see cref="WizardStep.HelpText"/>. If the help text is empty, the
///     right padding will not be shown and content will fill the entire step.
/// </summary>
/// <remarks>
///     If <see cref="Button"/>s are added, do not set <see cref="Button.IsDefault"/> to true as this will conflict
///     with the Next button of the Wizard. Subscribe to the <see cref="View.VisibleChanged"/> event to be notified when
///     the step is active; see also: <see cref="Wizard.StepChanged"/>. To enable or disable a step from being shown to the
///     user, set <see cref="View.Enabled"/>.
/// </remarks>
public class WizardStep : View, IDesignable
{
    private readonly TextView _helpTextView = new ()
    {
        CanFocus = true,
        TabStop = TabBehavior.TabStop,
        ReadOnly = true,
        WordWrap = true,
        AllowsTab = false,
        X = Pos.AnchorEnd () + 1,
        Height = Dim.Fill (),
#if DEBUG
        Id = "WizardStep._helpTextView"
#endif
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="Wizard"/> class.
    /// </summary>
    public WizardStep ()
    {
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        Width = Dim.Fill ();
        Height = Dim.Fill ();
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        // Help text goes in the right Padding
        // TODO: Enable built-in scrollbars for the help text view once TextView supports
        //_helpTextView.VerticalScrollBar.AutoShow = true;
        //_helpTextView.HorizontalScrollBar.AutoShow = true;
        _helpTextView.Width = Dim.Func (_ => CalculateHelpPaddingWidth ());

        Padding?.Add (_helpTextView);

        ShowHide ();
        base.EndInit ();
    }

    /// <summary>Sets or gets the text for the back button. The back button will only be visible on steps after the first step.</summary>
    /// <remarks>The default text is "Back"</remarks>
    public string BackButtonText { get; set; } = string.Empty;

    /// <summary>Calculates the width for the help text padding based on the current frame width.</summary>
    private int CalculateHelpPaddingWidth () => 25;

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        base.OnFrameChanged (frame);

        // Update padding thickness when frame changes
        if (Padding is { } && _helpTextView.Text.Length > 0)
        {
            Padding.Thickness = Padding.Thickness with { Right = CalculateHelpPaddingWidth () };
            App?.Invoke (() => Layout ());
        }
    }

    /// <summary>
    ///     Sets or gets help text for the <see cref="WizardStep"/>.If <see cref="WizardStep.HelpText"/> is empty the help
    ///     pane will not be visible and the content will fill the entire WizardStep.
    /// </summary>
    /// <remarks>The help text is displayed using a read-only <see cref="TextView"/>.</remarks>
    public string HelpText
    {
        get => _helpTextView.Text;
        set
        {
            _helpTextView.Text = value;
            _helpTextView.MoveHome ();
            ShowHide ();
        }
    }

    /// <summary>Sets or gets the text for the next/finish button.</summary>
    /// <remarks>The default text is "Next..." if the Pane is not the last pane. Otherwise, it is "Finish"</remarks>
    public string NextButtonText { get; set; } = string.Empty;

    /// <summary>Does the work to show and hide the contentView and helpView as appropriate</summary>
    internal void ShowHide ()
    {
        // Check if views are available (might be null during disposal)
        if (Padding is null)
        {
            return;
        }

        if (_helpTextView.Text.Length > 0)
        {
            // Configure Padding

            Padding.CanFocus = true;
            Padding.TabStop = TabBehavior.TabStop;

            // Help text goes in right Padding - set thickness based on current frame width
            Padding.Thickness = Padding.Thickness with { Right = CalculateHelpPaddingWidth () };

            _helpTextView.Visible = true;
            _helpTextView.Enabled = true;
        }
        else
        {
            // Configure Padding

            Padding.CanFocus = false;

            // No help text - no right padding needed
            Padding.Thickness = Padding.Thickness with { Right = 0 };

            _helpTextView.Visible = false;
            _helpTextView.Enabled = false;
        }

        SetNeedsLayout ();
    }

    bool IDesignable.EnableForDesign ()
    {
        Title = "Example Step";

        Label label = new ()
        {
            Title = "_Enter Text:"
        };

        TextField textField = new ()
        {
            X = Pos.Right (label) + 1,
            Width = 20
        };
        Add (label, textField);

        label = new ()
        {
            Title = "    _A List:",
            Y = Pos.Bottom (label) + 1
        };

        ListView listView = new ()
        {
            BorderStyle = LineStyle.Dashed,
            X = Pos.Right (label) + 1,
            Y = Pos.Top (label),
            Height = Dim.Auto (),
            Width = 10,
            Source = new ListWrapper<string> (["Item 1", "Item 2", "Item 3", "Item 4", "Item 5"]),
            SelectedItem = 0
        };
        Add (label, listView);

        HelpText = """
                   This is some help text for the WizardStep. 
                   You can provide instructions or information to guide the user through this step of the wizard.
                   """;

        return true;
    }
} // end of WizardStep class
