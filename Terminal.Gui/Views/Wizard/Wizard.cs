using System.ComponentModel;

namespace Terminal.Gui.Views;

/// <summary>
///     Provides navigation and a user interface (UI) to collect related data across multiple steps. Each step (
///     <see cref="WizardStep"/>) can host arbitrary <see cref="View"/>s, much like a <see cref="Dialog"/>. Each step
///     can display help text in its right <see cref="Padding"/>. Navigation buttons are displayed in the bottom
///     <see cref="Padding"/> of the Wizard, enabling the user to navigate forward and backward through the steps.
/// </summary>
/// <remarks>
///     The Wizard can be displayed either as a modal (pop-up) <see cref="Window"/> (like <see cref="Dialog"/>) or as
///     an embedded <see cref="View"/>.
/// </remarks>
/// <example>
///     <code>
/// using Terminal.Gui;
/// using System.Text;
/// 
/// Application.Init();
/// 
/// var wizard = new Wizard ($"Setup Wizard");
/// 
/// // Add 1st step
/// var firstStep = new WizardStep ("End User License Agreement");
/// wizard.AddStep(firstStep);
/// firstStep.NextButtonText = "Accept!";
/// firstStep.HelpText = "This is the End User License Agreement.";
/// 
/// // Add 2nd step
/// var secondStep = new WizardStep ("Second Step");
/// wizard.AddStep(secondStep);
/// secondStep.HelpText = "This is the help text for the Second Step.";
/// var lbl = new Label () { Text = "Name:" };
/// secondStep.Add(lbl);
/// 
/// var name = new TextField { X = Pos.Right (lbl) + 1, Width = Dim.Fill () - 1 };
/// secondStep.Add(name);
/// 
/// wizard.Finished += (args) =>
/// {
///     MessageBox.Query("Wizard", $"Finished. The Name entered is '{name.Text}'", "Ok");
///     Application.RequestStop();
/// };
/// 
/// Application.TopRunnable.Add (wizard);
/// Application.Run ();
/// Application.Shutdown ();
/// </code>
/// </example>
public class Wizard : Runnable, IDesignable
{
    private string _wizardTitle = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Wizard"/> class.
    /// </summary>
    /// <remarks>
    ///     The Wizard will be vertically and horizontally centered in the container. After initialization use <c>X</c>,
    ///     <c>Y</c>, <c>Width</c>, and <c>Height</c> change size and position.
    /// </remarks>
    public Wizard ()
    {
        SchemeName = SchemeManager.SchemesToSchemeName (Schemes.Dialog);
        BorderStyle = LineStyle.Double;
        Arrangement |= ViewArrangement.Movable | ViewArrangement.Resizable;
        base.ShadowStyle = Dialog.DefaultShadow;

        X = Pos.Center ();
        Y = Pos.Center ();
        Width = Dim.Auto (minimumContentDim: Dim.Percent (80), maximumContentDim: Dim.Percent (90));
        Height = Dim.Auto (minimumContentDim: Dim.Percent (10), maximumContentDim: Dim.Percent (90));

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

        TitleChanged += WizardTitleChanged;

        AddCommand (Command.Quit, QuitHandler);
        KeyBindings.Add (Application.QuitKey, Command.Quit);

        return;

        void WizardTitleChanged (object? sender, EventArgs<string> e)
        {
            if (string.IsNullOrEmpty (_wizardTitle))
            {
                _wizardTitle = e.Value;
            }
        }

        // Add key binding for Esc when not modal - fires Cancelled event
        bool? QuitHandler (ICommandContext? ctx)
        {
            CancelEventArgs args = new ();
            Cancelled?.Invoke (this, args);

            return args.Cancel;
        }
    }

    /// <inheritdoc/>
    public override void EndInit ()
    {
        // Configure Padding
        if (Padding is { })
        {
            Padding.SetScheme (SchemeManager.GetScheme (Schemes.Base));
            Padding.CanFocus = true;
            TabStop = TabBehavior.TabStop;

            // Add buttons to bottom Padding instead of using AddButton
            Padding?.Add (BackButton);
            Padding?.Add (NextFinishButton);
        }

        BackButton.Accepting += BackBtnOnAccepting;
        NextFinishButton.Accepting += NextFinishBtnOnAccepting;

        CurrentStep = GetFirstStep ();
        base.EndInit ();
    }

    /// <summary>
    ///     If the <see cref="CurrentStep"/> is not the first step in the wizard, this button causes the
    ///     <see cref="MovingBack"/> event to be fired and the wizard moves to the previous step.
    /// </summary>
    /// <remarks>Use the <see cref="MovingBack"></see> event to be notified when the user attempts to go back.</remarks>
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
    ///     If the <see cref="CurrentStep"/> is the last step in the wizard, this button causes the
    ///     <see cref="View.Accepting"/>
    ///     event to be raised and the wizard to close. If the step is not the last step, the <see cref="MovingNext"/> event
    ///     will be fired and the wizard will move next step.
    /// </summary>
    /// <remarks>
    ///     Use the <see cref="MovingNext"></see> and <see cref="View.Accepting"></see> events to be notified when the user
    ///     attempts go to the next step or finish the wizard.
    /// </remarks>
    public Button NextFinishButton { get; }

    private Size _maxStepSize = Size.Empty;

    /// <summary>
    ///     Adds a step to the wizard. The Next and Back buttons navigate through the added steps in the order they were
    ///     added.
    /// </summary>
    /// <param name="newStep"></param>
    /// <remarks>The "Next..." button of the last step added will read "Finish" (unless changed from default).</remarks>
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

        //newStep.BorderStyle = LineStyle.None;
        //newStep.Border!.Thickness = new(0, 0, 0, 1);
        //newStep.Padding!.Thickness = newStep.Padding!.Thickness with { Left = 1, Right = 1 };

        // newStep.X = -1;
        newStep.Width = Dim.Fill ();
        newStep.Height = Dim.Fill ();

        newStep.SetRelativeLayout (App?.Screen.Size ?? new Size (2048, 2048));
        newStep.LayoutSubViews ();
        Width = Dim.Auto (minimumContentDim: _maxStepSize.Width + 2);
        Height = Dim.Auto (minimumContentDim: _maxStepSize.Height + NextFinishButton.Frame.Height + 3);

        UpdateButtonsAndTitle ();
    }

    /// <summary>
    ///     Raised when the user has cancelled the <see cref="Wizard"/> by pressing the Esc key.
    /// </summary>
    public event EventHandler<CancelEventArgs>? Cancelled;

    /// <summary>Returns the first enabled step in the Wizard</summary>
    /// <returns>The last enabled step</returns>
    public WizardStep? GetFirstStep () { return _steps.FirstOrDefault (s => s.Enabled); }

    /// <summary>Returns the last enabled step in the Wizard</summary>
    /// <returns>The last enabled step</returns>
    public WizardStep? GetLastStep () { return _steps.LastOrDefault (s => s.Enabled); }

    /// <summary>
    ///     Returns the next enabled <see cref="WizardStep"/> after the current step. Takes into account steps which are
    ///     disabled. If <see cref="CurrentStep"/> is <c>null</c> returns the first enabled step.
    /// </summary>
    /// <returns>
    ///     The next step after the current step, if there is one; otherwise returns <c>null</c>, which indicates either
    ///     there are no enabled steps or the current step is the last enabled step.
    /// </returns>
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
    ///     Returns the first enabled <see cref="WizardStep"/> before the current step. Takes into account steps which are
    ///     disabled. If <see cref="CurrentStep"/> is <c>null</c> returns the last enabled step.
    /// </summary>
    /// <returns>
    ///     The first step ahead of the current step, if there is one; otherwise returns <c>null</c>, which indicates
    ///     either there are no enabled steps or the current step is the first enabled step.
    /// </returns>
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
    ///     Causes the wizard to move to the previous enabled step (or first step if <see cref="CurrentStep"/> is not set).
    ///     If there is no previous step, does nothing.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the transition to the step succeeded. <see langword="false"/> if the step was not found
    ///     or the operation was cancelled.
    /// </returns>
    public bool GoBack ()
    {
        WizardStep? previous = GetPreviousStep ();

        return previous is { } && GoToStep (previous);
    }

    /// <summary>
    ///     Causes the wizard to move to the next enabled step (or last step if <see cref="CurrentStep"/> is not set). If
    ///     there is no previous step, does nothing.
    /// </summary>
    /// <returns>
    ///     <see langword="true"/> if the transition to the step succeeded. <see langword="false"/> if the step was not found
    ///     or the operation was cancelled.
    /// </returns>
    public bool GoNext ()
    {
        WizardStep? nextStep = GetNextStep ();

        return nextStep is { } && GoToStep (nextStep);
    }

    /// <summary>
    ///     Raised when the Back button in the <see cref="Wizard"/> is clicked. The Back button is always the first button
    ///     in the array of Buttons passed to the <see cref="Wizard"/> constructor, if any.
    /// </summary>
    public event EventHandler<CancelEventArgs>? MovingBack;

    /// <summary>
    ///     Raised when the Next/Finish button in the <see cref="Wizard"/> is clicked (or the user presses Enter). The
    ///     Next/Finish button is always the last button in the array of Buttons passed to the <see cref="Wizard"/>
    ///     constructor, if any. This event is only raised if the <see cref="CurrentStep"/> is the last Step in the Wizard flow
    ///     (otherwise the <see cref="View.Accepting"/> event is raised).
    /// </summary>
    public event EventHandler<CancelEventArgs>? MovingNext;

    /// <summary>Changes to the specified <see cref="WizardStep"/>.</summary>
    /// <param name="newStep">The step to go to.</param>
    /// <returns>
    ///     <see langword="true"/> if the transition to the step succeeded. <see langword="false"/> if the step was not found
    ///     or the operation was cancelled.
    /// </returns>
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

    /// <summary>
    ///     Called when the <see cref="Wizard"/> is about to transition to another <see cref="WizardStep"/>. Fires the
    ///     <see cref="StepChanging"/> event.
    /// </summary>
    /// <returns>True if the change is to be cancelled.</returns>
    protected virtual bool OnStepChanging (ValueChangingEventArgs<WizardStep?> args) { return false; }

    /// <summary>
    ///     This event is raised when the current <see cref="CurrentStep"/>) is about to change. Use
    ///     <see cref="ValueChangingEventArgs{T}.Handled"/> to abort the transition.
    /// </summary>
    public event EventHandler<ValueChangingEventArgs<WizardStep?>>? StepChanging;

    /// <summary>
    ///     Called when the <see cref="Wizard"/> has completed transition to a new <see cref="WizardStep"/>. Fires the
    ///     <see cref="StepChanged"/> event.
    /// </summary>
    protected virtual void OnStepChanged (ValueChangedEventArgs<WizardStep?> args) { }

    /// <summary>This event is raised after the <see cref="Wizard"/> has changed the <see cref="CurrentStep"/>.</summary>
    public event EventHandler<ValueChangedEventArgs<WizardStep?>>? StepChanged;

    private void BackBtnOnAccepting (object? sender, CommandEventArgs e)
    {
        var args = new CancelEventArgs ();
        MovingBack?.Invoke (this, args);

        e.Handled = args.Cancel || GoBack ();
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
