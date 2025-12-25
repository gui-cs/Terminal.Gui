using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     A multistep user interface for collecting related data. Each <see cref="WizardStep"/> can host arbitrary
///     <see cref="View"/>s and display help text. Navigation buttons enable moving between steps.
/// </summary>
/// <remarks>
///     Can be displayed as a modal (pop-up) or embedded view.
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
/// wizard.AddStep(firstStep);
/// 
/// // Add second step
/// WizardStep secondStep = new () { Title = "User Info" };
/// secondStep.HelpText = "Enter your information.";
/// TextField name = new () { X = 0, Width = 20 };
/// secondStep.Add (new Label { Text = "Name:" }, name);
/// wizard.AddStep (secondStep);
/// 
/// wizard.Accepting += (_, e) =>
/// {
///     MessageBox.Query ("Complete", $"Name: {name.Text}", "Ok");
///     e.Handled = true;
/// };
/// 
/// app.Run (wizard);
/// </code>
/// </example>
public class Wizard : Runnable, IDesignable
{
    private string _wizardTitle = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Wizard"/> class, centered with automatic sizing.
    /// </summary>
    public Wizard ()
    {
        TabStop = TabBehavior.TabGroup;
        X = Pos.Center ();
        Y = Pos.Center ();
        Width = Dim.Auto (minimumContentDim: Dim.Percent (80), maximumContentDim: Dim.Percent (90));
        Height = Dim.Auto (minimumContentDim: Dim.Percent (10), maximumContentDim: Dim.Percent (90));

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
        NextFinishButton.FrameChanged += (_, _) => { Padding!.Thickness = Padding.Thickness with { Bottom = NextFinishButton.Frame.Height }; };

        AddCommand (Command.Quit, QuitHandler);
        KeyBindings.Add (Application.QuitKey, Command.Quit);

        return;

        // Add key binding for Esc when not modal - fires Cancelled event
        bool? QuitHandler (ICommandContext? ctx)
        {
            CancelEventArgs args = new ();
            Cancelled?.Invoke (this, args);

            return args.Cancel;
        }
    }

    private void SetStyle ()
    {
        if (IsRunning)
        {
            SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
            BorderStyle = Dialog.DefaultBorderStyle;
            Arrangement |= ViewArrangement.Movable | ViewArrangement.Resizable;
            base.ShadowStyle = Dialog.DefaultShadow;
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
        // Configure Padding
        if (Padding is { })
        {
            // Add buttons to bottom Padding instead of using AddButton
            Padding?.Add (BackButton);
            Padding?.Add (NextFinishButton);
        }

        BackButton.Accepting += BackBtnOnAccepting;
        NextFinishButton.Accepting += NextFinishBtnOnAccepting;

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
    ///     The Back button. Navigates to the previous step and raises <see cref="MovingBack"/>.
    ///     Hidden on the first step.
    /// </summary>
    public Button BackButton { get; }

    private readonly LinkedList<WizardStep> _steps = [];
    private WizardStep? _currentStep;

    /// <summary>Gets or sets the currently active <see cref="WizardStep"/>.</summary>
    public WizardStep? CurrentStep
    {
        get => _currentStep;
        set => GoToStep (value);
    }

    /// <summary>
    ///     The Next/Finish button. On the last step, raises <see cref="View.Accepting"/>.
    ///     On other steps, raises <see cref="MovingNext"/> and navigates forward.
    /// </summary>
    public Button NextFinishButton { get; }

    private Size _maxStepSize = Size.Empty;

    /// <summary>
    ///     Adds a step to the wizard. Steps are navigated in the order added.
    /// </summary>
    /// <param name="newStep">The step to add.</param>
    public void AddStep (WizardStep newStep)
    {
        newStep.EnabledChanged += (_, _) => UpdateButtonsAndTitle ();
        newStep.TitleChanged += (_, _) => UpdateButtonsAndTitle ();
        _steps.AddLast (newStep);
        Add (newStep);

        // Find the step's natural size
        //newStep.SuperViewRendersLineCanvas = true;
        newStep.Width = Dim.Auto ();
        newStep.Height = Dim.Auto ();
        newStep.SetRelativeLayout (App?.Screen.Size ?? new Size (2048, 2048));
        newStep.LayoutSubViews ();

        _maxStepSize = new (
                            Math.Max (_maxStepSize.Width, newStep.Frame.Width),
                            Math.Max (_maxStepSize.Height, newStep.Frame.Height));
        newStep.Width = Dim.Fill ();
        newStep.Height = Dim.Fill ();

        newStep.SetRelativeLayout (App?.Screen.Size ?? new Size (2048, 2048));
        newStep.LayoutSubViews ();
        Width = Dim.Auto (minimumContentDim: _maxStepSize.Width + 2);
        Height = Dim.Auto (minimumContentDim: _maxStepSize.Height + NextFinishButton.Frame.Height);

        UpdateButtonsAndTitle ();
    }

    /// <summary>Raised when the user cancels the wizard by pressing the Esc key.</summary>
    public event EventHandler<CancelEventArgs>? Cancelled;

    /// <summary>Returns the first enabled step.</summary>
    public WizardStep? GetFirstStep () { return _steps.FirstOrDefault (s => s.Enabled); }

    /// <summary>Returns the last enabled step.</summary>
    public WizardStep? GetLastStep () { return _steps.LastOrDefault (s => s.Enabled); }

    /// <summary>
    ///     Returns the next enabled step after <see cref="CurrentStep"/>, skipping disabled steps.
    /// </summary>
    /// <returns>The next enabled step, or <c>null</c> if none exists.</returns>
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
    ///     Returns the previous enabled step before <see cref="CurrentStep"/>, skipping disabled steps.
    /// </summary>
    /// <returns>The previous enabled step, or <c>null</c> if none exists.</returns>
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

    /// <summary>Navigates to the previous enabled step.</summary>
    /// <returns><see langword="true"/> if the transition succeeded; otherwise <see langword="false"/>.</returns>
    public bool GoBack ()
    {
        WizardStep? previous = GetPreviousStep ();

        return previous is { } && GoToStep (previous);
    }

    /// <summary>Navigates to the next enabled step.</summary>
    /// <returns><see langword="true"/> if the transition succeeded; otherwise <see langword="false"/>.</returns>
    public bool GoNext ()
    {
        WizardStep? nextStep = GetNextStep ();

        return nextStep is { } && GoToStep (nextStep);
    }

    /// <summary>
    ///     Raised when the user clicks the Back button. Cancel to prevent navigation.
    /// </summary>
    public event EventHandler<CancelEventArgs>? MovingBack;

    /// <summary>
    ///     Raised when the user clicks Next on a non-final step. Cancel to prevent navigation.
    /// </summary>
    public event EventHandler<CancelEventArgs>? MovingNext;

    /// <summary>Navigates to the specified step.</summary>
    /// <param name="newStep">The step to navigate to.</param>
    /// <returns><see langword="true"/> if the transition succeeded; otherwise <see langword="false"/>.</returns>
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

    /// <summary>Called before changing steps. Raises <see cref="StepChanging"/>.</summary>
    /// <returns><see langword="true"/> to cancel the change.</returns>
    protected virtual bool OnStepChanging (ValueChangingEventArgs<WizardStep?> args) => false;

    /// <summary>Raised before <see cref="CurrentStep"/> changes. Set <c>Handled</c> to cancel.</summary>
    public event EventHandler<ValueChangingEventArgs<WizardStep?>>? StepChanging;

    /// <summary>Called after changing steps. Raises <see cref="StepChanged"/>.</summary>
    protected virtual void OnStepChanged (ValueChangedEventArgs<WizardStep?> args) { }

    /// <summary>Raised after <see cref="CurrentStep"/> changes.</summary>
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
