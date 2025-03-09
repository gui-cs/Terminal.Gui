#nullable enable 
namespace Terminal.Gui;

/// <summary>
///     Represents a basic step that is displayed in a <see cref="Wizard"/>. The <see cref="WizardStep"/> view is
///     divided horizontally in two. On the left is the content view where <see cref="View"/>s can be added,  On the right
///     is the help for the step. Set <see cref="WizardStep.HelpText"/> to set the help text. If the help text is empty the
///     help pane will not be shown. If there are no Views added to the WizardStep the <see cref="HelpText"/> (if not
///     empty) will fill the wizard step.
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

        base.Add (_contentView);

        base.Add (_helpTextView);

        // BUGBUG: v2 - Disabling scrolling for now
        //var scrollBar = new ScrollBarView (helpTextView, true);

        //scrollBar.ChangedPosition += (s,e) => {
        //	helpTextView.TopRow = scrollBar.Position;
        //	if (helpTextView.TopRow != scrollBar.Position) {
        //		scrollBar.Position = helpTextView.TopRow;
        //	}
        //	helpTextView.SetNeedsDraw ();
        //};

        //scrollBar.OtherScrollBarView.ChangedPosition += (s,e) => {
        //	helpTextView.LeftColumn = scrollBar.OtherScrollBarView.Position;
        //	if (helpTextView.LeftColumn != scrollBar.OtherScrollBarView.Position) {
        //		scrollBar.OtherScrollBarView.Position = helpTextView.LeftColumn;
        //	}
        //	helpTextView.SetNeedsDraw ();
        //};

        //scrollBar.VisibleChanged += (s,e) => {
        //	if (scrollBar.Visible && helpTextView.RightOffset == 0) {
        //		helpTextView.RightOffset = 1;
        //	} else if (!scrollBar.Visible && helpTextView.RightOffset == 1) {
        //		helpTextView.RightOffset = 0;
        //	}
        //};

        //scrollBar.OtherScrollBarView.VisibleChanged += (s,e) => {
        //	if (scrollBar.OtherScrollBarView.Visible && helpTextView.BottomOffset == 0) {
        //		helpTextView.BottomOffset = 1;
        //	} else if (!scrollBar.OtherScrollBarView.Visible && helpTextView.BottomOffset == 1) {
        //		helpTextView.BottomOffset = 0;
        //	}
        //};

        //helpTextView.DrawContent += (s,e) => {
        //	scrollBar.Size = helpTextView.Lines;
        //	scrollBar.Position = helpTextView.TopRow;
        //	if (scrollBar.OtherScrollBarView is { }) {
        //		scrollBar.OtherScrollBarView.Size = helpTextView.Maxlength;
        //		scrollBar.OtherScrollBarView.Position = helpTextView.LeftColumn;
        //	}
        //	scrollBar.LayoutSubviews ();
        //	scrollBar.Refresh ();
        //};
        //base.Add (scrollBar);
        ShowHide ();
    }

    /// <summary>Sets or gets the text for the back button. The back button will only be visible on steps after the first step.</summary>
    /// <remarks>The default text is "Back"</remarks>
    public string BackButtonText { get; set; } = string.Empty;

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

        if (_contentView.InternalSubviews.Count < 1)
        {
            CanFocus = false;
        }

        ShowHide ();

        return view;
    }

    /// <summary>Removes all <see cref="View"/>s from the <see cref="WizardStep"/>.</summary>
    /// <remarks></remarks>
    public override void RemoveAll ()
    {
        _contentView.RemoveAll ();
        ShowHide ();
    }

    /// <summary>Does the work to show and hide the contentView and helpView as appropriate</summary>
    internal void ShowHide ()
    {
        _contentView.Height = Dim.Fill ();
        _helpTextView.Height = Dim.Height(_contentView);
        _helpTextView.Width = Dim.Fill ();

        if (_contentView.InternalSubviews?.Count > 0)
        {
            if (_helpTextView.Text.Length > 0)
            {
                _contentView.Width = Dim.Percent (70);
                _helpTextView.X = Pos.Right (_contentView);
                _helpTextView.Width = Dim.Fill ();
            }
            else
            {
                _contentView.Width = Dim.Fill ();
            }
        }
        else
        {
            if (_helpTextView.Text.Length > 0)
            {
                _helpTextView.X = 0;
            }

            // Error - no pane shown
        }

        _contentView.Visible = _contentView.InternalSubviews?.Count > 0;
        _helpTextView.Visible = _helpTextView.Text.Length > 0;
    }
} // end of WizardStep class
