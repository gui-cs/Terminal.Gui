

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
public class WizardStep : View
{
    // The contentView works like the ContentView in FrameView.
    private readonly View _contentView = new ()
    {
        CanFocus = true,
        TabStop = TabBehavior.TabStop,
        Id = "WizardStep._contentView"
    };
    private readonly TextView _helpTextView = new ()
    {
        CanFocus = true,
        TabStop = TabBehavior.TabStop,
        ReadOnly = true,
        WordWrap = true,
        AllowsTab = false,
        Id = "WizardStep._helpTextView"
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="Wizard"/> class.
    /// </summary>
    public WizardStep ()
    {
        TabStop = TabBehavior.TabStop;
        CanFocus = true;
        BorderStyle = LineStyle.None;

        // Content fills the entire viewport
        _contentView.X = 0;
        _contentView.Y = 0;
        _contentView.Width = Dim.Fill ();
        _contentView.Height = Dim.Fill ();
        base.Add (_contentView);

        // Help text goes in the right Padding
        _helpTextView.X = 0;
        _helpTextView.Y = 0;
        _helpTextView.Width = Dim.Fill ();
        _helpTextView.Height = Dim.Fill ();

        // Enable built-in scrollbars for the help text view
        _helpTextView.VerticalScrollBar.AutoShow = true;
        _helpTextView.HorizontalScrollBar.AutoShow = true;

        // Help will be added to Padding in ShowHide when there's help text
        ShowHide ();
    }

    /// <summary>Sets or gets the text for the back button. The back button will only be visible on steps after the first step.</summary>
    /// <remarks>The default text is "Back"</remarks>
    public string BackButtonText { get; set; } = string.Empty;

    /// <summary>Calculates the width for the help text padding based on the current frame width.</summary>
    /// <returns>The padding width (30% of frame width, minimum 10)</returns>
    private int CalculateHelpPaddingWidth () => Math.Max (10, (int)(Frame.Width * 0.3));

    /// <inheritdoc/>
    protected override void OnFrameChanged (in Rectangle frame)
    {
        base.OnFrameChanged (frame);
        
        // Update padding thickness when frame changes
        if (Padding is not null && _helpTextView.Text.Length > 0)
        {
            Padding.Thickness = new Thickness (0, 0, CalculateHelpPaddingWidth (), 0);
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
            ShowHide ();
            SetNeedsDraw ();
        }
    }

    /// <summary>Sets or gets the text for the next/finish button.</summary>
    /// <remarks>The default text is "Next..." if the Pane is not the last pane. Otherwise it is "Finish"</remarks>
    public string NextButtonText { get; set; } = string.Empty;

    /// <summary>Add the specified <see cref="View"/> to the <see cref="WizardStep"/>.</summary>
    /// <param name="view"><see cref="View"/> to add to this container</param>
    public override View Add (View? view)
    {
        _contentView.Add (view);

        if (view!.CanFocus)
        {
            CanFocus = true;
        }

        ShowHide ();

        return view;
    }

    /// <summary>Removes a <see cref="View"/> from <see cref="WizardStep"/>.</summary>
    /// <remarks></remarks>
    public override View? Remove (View? view)
    {
        SetNeedsDraw ();
        View? container = view?.SuperView;

        if (container == this)
        {
            base.Remove (view);
        }
        else
        {
            container?.Remove (view);
        }

        if (_contentView.InternalSubViews.Count < 1)
        {
            CanFocus = false;
        }

        ShowHide ();

        return view;
    }

    /// <summary>Removes all <see cref="View"/>s from the <see cref="WizardStep"/>.</summary>
    /// <remarks></remarks>
    public override IReadOnlyCollection<View> RemoveAll ()
    {
        IReadOnlyCollection<View> removed = _contentView.RemoveAll ();
        ShowHide ();

        return removed;
    }

    /// <summary>Does the work to show and hide the contentView and helpView as appropriate</summary>
    internal void ShowHide ()
    {
        // Check if views are available (might be null during disposal)
        if (Padding is null || _contentView is null || _helpTextView is null)
        {
            return;
        }

        if (_helpTextView.Text.Length > 0)
        {
            // Help text goes in right Padding - set thickness based on current frame width
            Padding.Thickness = new Thickness (0, 0, CalculateHelpPaddingWidth (), 0);
            
            // Add help to padding if not already there
            if (_helpTextView.SuperView != Padding)
            {
                // Remove from main view if it was there
                if (_helpTextView.SuperView == this)
                {
                    Remove (_helpTextView);
                }
                Padding.Add (_helpTextView);
            }
            
            _helpTextView.Visible = true;
            _contentView.Width = Dim.Fill ();
            _contentView.Height = Dim.Fill ();
        }
        else
        {
            // No help text - no right padding needed
            Padding.Thickness = Thickness.Empty;
            
            // Remove help from padding if it's there
            if (_helpTextView.SuperView == Padding)
            {
                Padding.Remove (_helpTextView);
            }
            
            _helpTextView.Visible = false;
            _contentView.Width = Dim.Fill ();
            _contentView.Height = Dim.Fill ();
        }

        _contentView.Visible = true;
    }
} // end of WizardStep class
