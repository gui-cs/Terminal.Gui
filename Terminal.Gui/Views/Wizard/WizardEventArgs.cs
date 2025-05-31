namespace Terminal.Gui.Views;

/// <summary><see cref="EventArgs"/> for <see cref="WizardStep"/> transition events.</summary>
public class WizardButtonEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="WizardButtonEventArgs"/></summary>
    public WizardButtonEventArgs () { Cancel = false; }

    /// <summary>Set to true to cancel the transition to the next step.</summary>
    public bool Cancel { get; set; }
}

/// <summary><see cref="EventArgs"/> for <see cref="WizardStep"/> events.</summary>
public class StepChangeEventArgs : EventArgs
{
    /// <summary>Initializes a new instance of <see cref="StepChangeEventArgs"/></summary>
    /// <param name="oldStep">The current <see cref="WizardStep"/>.</param>
    /// <param name="newStep">The new <see cref="WizardStep"/>.</param>
    public StepChangeEventArgs (WizardStep oldStep, WizardStep newStep)
    {
        OldStep = oldStep;
        NewStep = newStep;
        Cancel = false;
    }

    /// <summary>Event handlers can set to true before returning to cancel the step transition.</summary>
    public bool Cancel { get; set; }

    /// <summary>The <see cref="WizardStep"/> the <see cref="Wizard"/> is changing to or has changed to.</summary>
    public WizardStep NewStep { get; }

    /// <summary>The current (or previous) <see cref="WizardStep"/>.</summary>
    public WizardStep OldStep { get; }
}
