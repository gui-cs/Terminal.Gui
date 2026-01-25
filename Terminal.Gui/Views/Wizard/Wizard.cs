using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A multi-step dialog for collecting related data across sequential steps.
/// </summary>
/// <remarks>
///     <para>
///         Each <see cref="WizardStep"/> can host arbitrary <see cref="View"/>s and display help text.
///         Navigation buttons (Back/Next/Finish) are automatically managed.
///     </para>
///     <para>
///         Can be displayed as a modal dialog or embedded view. When modal, completing the wizard
///         raises <see cref="View.Accepting"/> and sets <see cref="IRunnable.Result"/>.
///     </para>
/// </remarks>
/// <example>
///     <code>
/// using Terminal.Gui;
/// 
/// using IApplication app = Application.Create ();
/// app.Init ();
/// 
/// using Wizard wiz = new () { Title = "Setup Wizard" };
/// 
/// // Add first step
/// WizardStep firstStep = new () { Title = "License Agreement" };
/// firstStep.NextButtonText = "Accept!";
/// firstStep.HelpText = "End User License Agreement text.";
/// wiz.AddStep (firstStep);
/// 
/// // Add second step
/// WizardStep secondStep = new () { Title = "User Info" };
/// secondStep.HelpText = "Enter your information.";
/// TextField name = new () { X = 0, Width = 20 };
/// secondStep.Add (new Label { Text = "Name:" }, name);
/// wiz.AddStep (secondStep);
/// 
/// wiz.Accepting += (_, e) =>
/// {
///     MessageBox.Query ("Complete", $"Name: {name.Text}", "Ok");
///     e.Handled = true;
/// };
/// 
/// app.Run (wiz);
/// </code>
/// </example>
public class Wizard : Dialog, IDesignable
{
    private string _wizardTitle = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Wizard"/> class.
    /// </summary>
    /// <remarks>
    ///     The wizard is centered with automatic sizing and includes Back and Next/Finish buttons.
    ///     Use <see cref="AddStep"/> to add steps.
    /// </remarks>
    public Wizard ()
    {
        TabStop = TabBehavior.TabGroup;
        X = Pos.Center ();
        Y = Pos.Center ();

        ButtonAlignment = Alignment.Fill;

        SetStyle ();

        BackButton = new ()
        {
            Text = Strings.wzBack,
            X = 0,
            Y = Pos.AnchorEnd ()
        };

        NextFinishButton = new ()
        {
            Text = Strings.wzFinish,
            IsDefault = true,
            X = Pos.AnchorEnd (),
            Y = Pos.AnchorEnd ()
        };

        BackButton.Accepting += BackBtnOnAccepting;
        NextFinishButton.Accepting += NextFinishBtnOnAccepting;

        AddButton (BackButton);
        AddButton (NextFinishButton);
    }

    private void SetStyle ()
    {
        if (IsRunning)
        {
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
            Arrangement |= ViewArrangement.Movable | ViewArrangement.Resizable;
        }
        else
        {
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Base);
            BorderStyle = LineStyle.Dotted;

            // strip out movable and resizable
            Arrangement &= ~(ViewArrangement.Movable | ViewArrangement.Resizable);
            base.ShadowStyle = ShadowStyle.None;
        }
    }

    /// <inheritdoc/>
    protected override void OnTitleChanged ()
    {
        if (string.IsNullOrEmpty (_wizardTitle))
        {
            _wizardTitle = Title;
        }
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        CurrentStep = GetFirstStep ();
        base.EndInit ();
    }

    /// <inheritdoc/>
    protected override void OnIsModalChanged (bool newIsModal)
    {
        SetStyle ();

        base.OnIsModalChanged (newIsModal);
    }

    /// <summary>
    ///     The Back button. Navigates to the previous step.
    /// </summary>
    /// <remarks>
    ///     Automatically hidden on the first step. Raises <see cref="MovingBack"/> when pressed.
    /// </remarks>
    public Button BackButton { get; }

    private readonly LinkedList<WizardStep> _steps = [];
    private WizardStep? _currentStep;

    /// <summary>Gets or sets the currently displayed step.</summary>
    /// <remarks>
    ///     Setting this property calls <see cref="GoToStep"/> and may be canceled via <see cref="StepChanging"/>.
    /// </remarks>
    public WizardStep? CurrentStep
    {
        get => _currentStep;
        set => GoToStep (value);
    }

    /// <summary>
    ///     The Next/Finish button. Advances to the next step or completes the wizard.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         On the last step, displays "Finish" text and raises <see cref="View.Accepting"/> when pressed.
    ///     </para>
    ///     <para>
    ///         On other steps, displays "Next" text and raises <see cref="MovingNext"/> when pressed.
    ///     </para>
    /// </remarks>
    public Button NextFinishButton { get; }

    private Size _maxStepSize = Size.Empty;

    /// <summary>
    ///     Adds a step to the wizard.
    /// </summary>
    /// <param name="newStep">The step to add. Steps are navigated in the order added.</param>
    /// <remarks>
    ///     The wizard automatically sizes to accommodate the largest step. All steps are resized to match
    ///     the maximum width and height.
    /// </remarks>
    public void AddStep (WizardStep newStep)
    {
        newStep.EnabledChanged += (_, _) => UpdateButtonsAndTitle ();
        newStep.TitleChanged += (_, _) => UpdateButtonsAndTitle ();
        _steps.AddLast (newStep);

        // Find the step's natural size
        //newStep.SuperViewRendersLineCanvas = true;
        newStep.Width = Dim.Auto ();
        newStep.Height = Dim.Auto ();
        newStep.SetRelativeLayout (App?.Screen.Size ?? new Size (2048, 2048));
        newStep.LayoutSubViews ();

        _maxStepSize = new (
                            Math.Max (_maxStepSize.Width, newStep.Frame.Width),
                            Math.Max (_maxStepSize.Height, newStep.Frame.Height));

        // Go through all steps to ensure they are the same size
        foreach (WizardStep step in _steps)
        {
            step.Width = Dim.Fill (0, _maxStepSize.Width);
            step.Height = Dim.Fill (0, _maxStepSize.Height);
        }

        Add (newStep);

        //newStep.SetRelativeLayout (App?.Screen.Size ?? new Size (2048, 2048));
        //newStep.LayoutSubViews ();
        //Width = Dim.Auto (minimumContentDim: _maxStepSize.Width + 2);
        //Height = Dim.Auto (minimumContentDim: _maxStepSize.Height);

        UpdateButtonsAndTitle ();
    }

    /// <summary>
    ///     Gets the first enabled step in the wizard.
    /// </summary>
    /// <returns>The first enabled step, or <see langword="null"/> if no enabled steps exist.</returns>
    public WizardStep? GetFirstStep () { return _steps.FirstOrDefault (s => s.Enabled); }

    /// <summary>
    ///     Gets the last enabled step in the wizard.
    /// </summary>
    /// <returns>The last enabled step, or <see langword="null"/> if no enabled steps exist.</returns>
    public WizardStep? GetLastStep () { return _steps.LastOrDefault (s => s.Enabled); }

    /// <summary>
    ///     Gets the next enabled step after <see cref="CurrentStep"/>.
    /// </summary>
    /// <returns>The next enabled step, or <see langword="null"/> if none exists.</returns>
    /// <remarks>
    ///     Disabled steps are automatically skipped.
    /// </remarks>
    public WizardStep? GetNextStep ()
    {
        LinkedListNode<WizardStep>? step;

        if (CurrentStep is null)
        {
            // Get first step, assume it is next
            step = _steps.First;
        }
        else
        {
            // Get the step after current
            step = _steps.Find (CurrentStep);
            step = step?.Next;
        }

        // step now points to the potential next step
        while (step is { })
        {
            if (step.Value.Enabled)
            {
                return step.Value;
            }

            step = step.Next;
        }

        return null;
    }

    /// <summary>
    ///     Gets the previous enabled step before <see cref="CurrentStep"/>.
    /// </summary>
    /// <returns>The previous enabled step, or <see langword="null"/> if none exists.</returns>
    /// <remarks>
    ///     Disabled steps are automatically skipped.
    /// </remarks>
    public WizardStep? GetPreviousStep ()
    {
        LinkedListNode<WizardStep>? step;

        if (CurrentStep is null)
        {
            // Get last step, assume it is previous
            step = _steps.Last;
        }
        else
        {
            // Get the step before current
            step = _steps.Find (CurrentStep);
            step = step?.Previous;
        }

        // step now points to the potential previous step
        while (step is { })
        {
            if (step.Value.Enabled)
            {
                return step.Value;
            }

            step = step.Previous;
        }

        return null;
    }

    /// <summary>
    ///     Navigates to the previous enabled step.
    /// </summary>
    /// <returns><see langword="true"/> if navigation succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    ///     Raises <see cref="MovingBack"/> and <see cref="StepChanging"/>. Navigation can be canceled
    ///     by handling these events.
    /// </remarks>
    public bool GoBack ()
    {
        WizardStep? previous = GetPreviousStep ();

        return previous is { } && GoToStep (previous);
    }

    /// <summary>
    ///     Navigates to the next enabled step.
    /// </summary>
    /// <returns><see langword="true"/> if navigation succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    ///     Raises <see cref="MovingNext"/> and <see cref="StepChanging"/>. Navigation can be canceled
    ///     by handling these events.
    /// </remarks>
    public bool GoNext ()
    {
        WizardStep? nextStep = GetNextStep ();

        return nextStep is { } && GoToStep (nextStep);
    }

    /// <summary>
    ///     Raised when the Back button is pressed. Set <c>Cancel</c> to prevent navigation.
    /// </summary>
    public event EventHandler<CancelEventArgs>? MovingBack;

    /// <summary>
    ///     Raised when the Next button is pressed on a non-final step. Set <c>Cancel</c> to prevent navigation.
    /// </summary>
    public event EventHandler<CancelEventArgs>? MovingNext;

    /// <summary>
    ///     Navigates to the specified step.
    /// </summary>
    /// <param name="newStep">The step to navigate to.</param>
    /// <returns><see langword="true"/> if navigation succeeded; otherwise <see langword="false"/>.</returns>
    /// <remarks>
    ///     Raises <see cref="StepChanging"/> and <see cref="StepChanged"/>. Navigation can be canceled
    ///     via the <see cref="StepChanging"/> event.
    /// </remarks>
    public bool GoToStep (WizardStep? newStep)
    {
        return CWPPropertyHelper.ChangeProperty (
                                                 this,
                                                 ref _currentStep,
                                                 newStep,
                                                 OnStepChanging,
                                                 StepChanging,
                                                 newValue =>
                                                 {
                                                     // BUGBUG: the CWP helper already invokes OnStepChanging and StepChanging
                                                     ValueChangingEventArgs<WizardStep?> args = new (_currentStep, newValue);
                                                     StepChanging?.Invoke (this, args);

                                                     if (args.Handled)
                                                     {
                                                         return;
                                                     }

                                                     // Hide all but the new step
                                                     foreach (WizardStep step in _steps)
                                                     {
                                                         step.Visible = step == newValue;

                                                         step.ShowHide ();
                                                     }

                                                     _currentStep = newValue;
                                                     UpdateButtonsAndTitle ();
                                                 },
                                                 OnStepChanged,
                                                 StepChanged,
                                                 out _);
    }

    /// <summary>
    ///     Called before <see cref="CurrentStep"/> changes. Override to add custom validation.
    /// </summary>
    /// <param name="args">Event arguments containing old and new step values.</param>
    /// <returns><see langword="true"/> to cancel the step change; otherwise <see langword="false"/>.</returns>
    protected virtual bool OnStepChanging (ValueChangingEventArgs<WizardStep?> args) => false;

    /// <summary>
    ///     Raised before <see cref="CurrentStep"/> changes. Set <c>Handled</c> to cancel navigation.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<WizardStep?>>? StepChanging;

    /// <summary>
    ///     Called after <see cref="CurrentStep"/> changes. Override to respond to step transitions.
    /// </summary>
    /// <param name="args">Event arguments containing old and new step values.</param>
    protected virtual void OnStepChanged (ValueChangedEventArgs<WizardStep?> args) { }

    /// <summary>
    ///     Raised after <see cref="CurrentStep"/> changes.
    /// </summary>
    public event EventHandler<ValueChangedEventArgs<WizardStep?>>? StepChanged;

    private void BackBtnOnAccepting (object? sender, CommandEventArgs e)
    {
        CancelEventArgs args = new ();
        MovingBack?.Invoke (this, args);

        if (args.Cancel)
        {
            return;
        }

        e.Handled = true;
        GoBack ();
    }

    private void NextFinishBtnOnAccepting (object? sender, CommandEventArgs e)
    {
        if (CurrentStep == GetLastStep ())
        {
            if (RaiseAccepting (e.Context) is false)
            {
                e.Handled = true;
                RequestStop ();
            }
        }
        else
        {
            CancelEventArgs args = new ();
            MovingNext?.Invoke (this, new ());

            if (!args.Cancel)
            {
                e.Handled = GoNext ();
            }
        }
    }

    private void UpdateButtonsAndTitle ()
    {
        if (CurrentStep is null)
        {
            return;
        }

        Title = $"{_wizardTitle}{(_steps.Count > 0 ? " - " + CurrentStep.Title : string.Empty)}";

        // Configure the Back button
        BackButton.Text = CurrentStep.BackButtonText != string.Empty
                              ? CurrentStep.BackButtonText
                              : Strings.wzBack; // "_Back";
        BackButton.Visible = CurrentStep != GetFirstStep ();

        // Configure the Next/Finished button
        if (CurrentStep == GetLastStep ())
        {
            NextFinishButton.Text = CurrentStep.NextButtonText != string.Empty
                                        ? CurrentStep.NextButtonText
                                        : Strings.wzFinish; // "Fi_nish";
        }
        else
        {
            NextFinishButton.Text = CurrentStep.NextButtonText != string.Empty
                                        ? CurrentStep.NextButtonText
                                        : Strings.wzNext; // "_Next...";
        }
    }

    bool IDesignable.EnableForDesign ()
    {
        Title = "Wizard Title";

        WizardStep firstStep = new ();
        (firstStep as IDesignable).EnableForDesign ();
        AddStep (firstStep);

        Label schemeLabel = new ()
        {
            Title = "_Scheme:"
        };

        OptionSelector<Schemes> selector = new ()
        {
            X = Pos.Right (schemeLabel) + 1,
            Title = "Select Scheme"
        };

        selector.ValueChanged += (_, _) =>
                                 {
                                     if (selector.Value is { } scheme)
                                     {
                                         SchemeName = SchemeManager.SchemesToSchemeName (scheme);
                                     }
                                 };

        Label borderStyleLabel = new ()
        {
            Title = "_Border Style:",
            X = Pos.Right (selector) + 2
        };

        OptionSelector<LineStyle> borderStyleSelector = new ()
        {
            X = Pos.Right (borderStyleLabel) + 1,
            Title = "Select Border Style"
        };

        borderStyleSelector.ValueChanged += (_, _) =>
                                            {
                                                if (borderStyleSelector.Value is { } style)
                                                {
                                                    BorderStyle = style;
                                                }
                                            };

        WizardStep secondStep = new ()
        {
            Title = "Second Step",
            HelpText = "This is the help text for the Second Step."
        };
        secondStep.Add (schemeLabel, selector, borderStyleLabel, borderStyleSelector);

        AddStep (secondStep);

        return true;
    }
}
