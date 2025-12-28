namespace ViewsTests;

[Collection ("Global Test Setup")]
public class WizardTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Initializes_Properties ()
    {
        // Arrange & Act
        Wizard wizard = new ();

        // Assert
        Assert.NotNull (wizard);
        Assert.NotNull (wizard.BackButton);
        Assert.NotNull (wizard.NextFinishButton);
        Assert.Null (wizard.CurrentStep);
        Assert.Equal (LineStyle.Dotted, wizard.BorderStyle);
        Assert.False (wizard.Arrangement.HasFlag (ViewArrangement.Movable));
        Assert.False (wizard.Arrangement.HasFlag (ViewArrangement.Resizable));
    }

    [Fact (Skip = "Disabled in v2_44417-Continuous until Dialog sizing is figured out"))]
    public void Constructor_Sets_Button_Properties ()
    {
        // Arrange & Act
        Wizard wizard = new ();

        // Assert
        Assert.Equal (Strings.wzBack, wizard.BackButton.Text);
        Assert.Equal (Strings.wzFinish, wizard.NextFinishButton.Text);
        Assert.True (wizard.NextFinishButton.IsDefault);
        Assert.Equal (0, wizard.BackButton.X);
        Assert.Equal (Pos.AnchorEnd (), wizard.NextFinishButton.X);
    }

    #endregion Constructor Tests

    #region AddStep Tests

    [Fact]
    public void AddStep_Adds_Step_To_Wizard ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step = new () { Title = "Step 1" };

        // Act
        wizard.AddStep (step);

        // Assert
        Assert.Single (wizard.SubViews);
        Assert.Contains (step, wizard.SubViews);
    }

    [Fact]
    public void AddStep_Multiple_Steps_Maintains_Order ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        WizardStep step3 = new () { Title = "Step 3" };

        // Act
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);

        // Assert
        Assert.Equal (3, wizard.SubViews.Count);
        Assert.Equal (step1, wizard.GetFirstStep ());
        Assert.Equal (step3, wizard.GetLastStep ());
    }

    [Fact]
    public void AddStep_Sets_Width_And_Height_To_Fill ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step = new () { Title = "Step 1" };

        // Act
        wizard.AddStep (step);

        // Assert
        Assert.IsType<DimFill> (step.Width);
        Assert.IsType<DimFill> (step.Height);
    }

    #endregion AddStep Tests

    #region GetFirstStep Tests

    [Fact]
    public void GetFirstStep_Returns_Null_When_No_Steps ()
    {
        // Arrange
        Wizard wizard = new ();

        // Act
        WizardStep? result = wizard.GetFirstStep ();

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void GetFirstStep_Returns_First_Enabled_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);

        // Act
        WizardStep? result = wizard.GetFirstStep ();

        // Assert
        Assert.Equal (step1, result);
    }

    [Fact]
    public void GetFirstStep_Skips_Disabled_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1", Enabled = false };
        WizardStep step2 = new () { Title = "Step 2" };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);

        // Act
        WizardStep? result = wizard.GetFirstStep ();

        // Assert
        Assert.Equal (step2, result);
    }

    [Fact]
    public void GetFirstStep_Returns_Null_When_All_Steps_Disabled ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1", Enabled = false };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        wizard.AddStep (step1);
        wizard.AddStep (step2);

        // Act
        WizardStep? result = wizard.GetFirstStep ();

        // Assert
        Assert.Null (result);
    }

    #endregion GetFirstStep Tests

    #region GetLastStep Tests

    [Fact]
    public void GetLastStep_Returns_Null_When_No_Steps ()
    {
        // Arrange
        Wizard wizard = new ();

        // Act
        WizardStep? result = wizard.GetLastStep ();

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void GetLastStep_Returns_Last_Enabled_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);

        // Act
        WizardStep? result = wizard.GetLastStep ();

        // Assert
        Assert.Equal (step3, result);
    }

    [Fact]
    public void GetLastStep_Skips_Disabled_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        WizardStep step3 = new () { Title = "Step 3", Enabled = false };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);

        // Act
        WizardStep? result = wizard.GetLastStep ();

        // Assert
        Assert.Equal (step2, result);
    }

    [Fact]
    public void GetLastStep_Returns_Null_When_All_Steps_Disabled ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1", Enabled = false };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        wizard.AddStep (step1);
        wizard.AddStep (step2);

        // Act
        WizardStep? result = wizard.GetLastStep ();

        // Assert
        Assert.Null (result);
    }

    #endregion GetLastStep Tests

    #region GetNextStep Tests

    [Fact]
    public void GetNextStep_Returns_Null_When_No_Steps ()
    {
        // Arrange
        Wizard wizard = new ();

        // Act
        WizardStep? result = wizard.GetNextStep ();

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void GetNextStep_Returns_First_Step_When_CurrentStep_Is_Null ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);

        // Act
        WizardStep? result = wizard.GetNextStep ();

        // Assert
        Assert.Equal (step1, result);
    }

    [Fact]
    public void GetNextStep_Returns_Next_Enabled_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        WizardStep? result = wizard.GetNextStep ();

        // Assert
        Assert.Equal (step2, result);
    }

    [Fact]
    public void GetNextStep_Skips_Disabled_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        WizardStep? result = wizard.GetNextStep ();

        // Assert
        Assert.Equal (step3, result);
    }

    [Fact]
    public void GetNextStep_Returns_Null_When_At_Last_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();
        wizard.CurrentStep = step2;

        // Act
        WizardStep? result = wizard.GetNextStep ();

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void GetNextStep_Returns_Null_When_All_Remaining_Steps_Disabled ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        WizardStep step3 = new () { Title = "Step 3", Enabled = false };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        WizardStep? result = wizard.GetNextStep ();

        // Assert
        Assert.Null (result);
    }

    #endregion GetNextStep Tests

    #region GetPreviousStep Tests

    [Fact]
    public void GetPreviousStep_Returns_Null_When_No_Steps ()
    {
        // Arrange
        Wizard wizard = new ();

        // Act
        WizardStep? result = wizard.GetPreviousStep ();

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void GetPreviousStep_Returns_Last_Step_When_CurrentStep_Is_Null ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);

        // Act
        WizardStep? result = wizard.GetPreviousStep ();

        // Assert
        Assert.Equal (step2, result);
    }

    [Fact]
    public void GetPreviousStep_Returns_Previous_Enabled_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();
        wizard.CurrentStep = step3;

        // Act
        WizardStep? result = wizard.GetPreviousStep ();

        // Assert
        Assert.Equal (step2, result);
    }

    [Fact]
    public void GetPreviousStep_Skips_Disabled_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();
        wizard.CurrentStep = step3;

        // Act
        WizardStep? result = wizard.GetPreviousStep ();

        // Assert
        Assert.Equal (step1, result);
    }

    [Fact]
    public void GetPreviousStep_Returns_Null_When_At_First_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        WizardStep? result = wizard.GetPreviousStep ();

        // Assert
        Assert.Null (result);
    }

    [Fact]
    public void GetPreviousStep_Returns_Null_When_All_Previous_Steps_Disabled ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1", Enabled = false };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        WizardStep? result = wizard.GetPreviousStep ();

        // Assert
        Assert.Null (result);
    }

    #endregion GetPreviousStep Tests

    #region GoToStep Tests

    [Fact]
    public void GoToStep_Sets_CurrentStep ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        bool result = wizard.GoToStep (step2);

        // Assert
        Assert.True (result);
        Assert.Equal (step2, wizard.CurrentStep);
    }

    [Fact]
    public void GoToStep_Hides_Other_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        wizard.GoToStep (step2);

        // Assert
        Assert.False (step1.Visible);
        Assert.True (step2.Visible);
        Assert.False (step3.Visible);
    }

    [Fact]
    public void GoToStep_Raises_StepChanging_Event ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        var eventRaised = false;
        WizardStep? oldStep = null;
        WizardStep? newStep = null;

        wizard.StepChanging += (sender, args) =>
                               {
                                   eventRaised = true;
                                   oldStep = args.CurrentValue;
                                   newStep = args.NewValue;
                               };

        // Act
        wizard.GoToStep (step2);

        // Assert
        Assert.True (eventRaised);
        Assert.Equal (step1, oldStep);
        Assert.Equal (step2, newStep);
    }

    [Fact]
    public void GoToStep_Raises_StepChanged_Event ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);

        var eventRaised = false;
        WizardStep? newStep = null;

        wizard.BeginInit ();
        wizard.EndInit ();

        // Subscribe after EndInit to avoid capturing the initial CurrentStep setting
        wizard.StepChanged += (sender, args) =>
                              {
                                  eventRaised = true;
                                  newStep = args.NewValue;
                              };

        // Act
        wizard.GoToStep (step2);

        // Assert
        Assert.True (eventRaised);
        Assert.Same (step2, newStep);
    }

    [Fact]
    public void GoToStep_Can_Be_Cancelled_Via_StepChanging_Event ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        wizard.StepChanging += (sender, args) => { args.Handled = true; };

        // Act
        bool result = wizard.GoToStep (step2);

        // Assert
        Assert.False (result);
        Assert.Equal (step1, wizard.CurrentStep);
    }

    #endregion GoToStep Tests

    #region GoNext Tests

    [Fact]
    public void GoNext_Moves_To_Next_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        bool result = wizard.GoNext ();

        // Assert
        Assert.True (result);
        Assert.Equal (step2, wizard.CurrentStep);
    }

    [Fact]
    public void GoNext_Returns_False_When_No_Next_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        wizard.AddStep (step1);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        bool result = wizard.GoNext ();

        // Assert
        Assert.False (result);
        Assert.Equal (step1, wizard.CurrentStep);
    }

    [Fact]
    public void GoNext_Skips_Disabled_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        bool result = wizard.GoNext ();

        // Assert
        Assert.True (result);
        Assert.Equal (step3, wizard.CurrentStep);
    }

    #endregion GoNext Tests

    #region GoBack Tests

    [Fact]
    public void GoBack_Moves_To_Previous_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();
        wizard.CurrentStep = step2;

        // Act
        bool result = wizard.GoBack ();

        // Assert
        Assert.True (result);
        Assert.Equal (step1, wizard.CurrentStep);
    }

    [Fact]
    public void GoBack_Returns_False_When_No_Previous_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        wizard.AddStep (step1);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        bool result = wizard.GoBack ();

        // Assert
        Assert.False (result);
        Assert.Equal (step1, wizard.CurrentStep);
    }

    [Fact]
    public void GoBack_Skips_Disabled_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();
        wizard.CurrentStep = step3;

        // Act
        bool result = wizard.GoBack ();

        // Assert
        Assert.True (result);
        Assert.Equal (step1, wizard.CurrentStep);
    }

    #endregion GoBack Tests

    #region CurrentStep Tests

    [Fact]
    public void CurrentStep_Setter_Calls_GoToStep ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        wizard.CurrentStep = step2;

        // Assert
        Assert.Equal (step2, wizard.CurrentStep);
    }

    [Fact]
    public void CurrentStep_Can_Be_Set_To_Null ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        wizard.AddStep (step1);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        wizard.CurrentStep = null;

        // Assert
        Assert.Null (wizard.CurrentStep);
    }

    #endregion CurrentStep Tests

    #region Title Tests

    [Fact]
    public void Title_Updates_With_Wizard_And_Step_Title ()
    {
        // Arrange
        Wizard wizard = new () { Title = "Setup" };
        WizardStep step1 = new () { Title = "Step 1" };
        wizard.AddStep (step1);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act & Assert
        Assert.Contains ("Setup", wizard.Title);
        Assert.Contains ("Step 1", wizard.Title);
    }

    [Fact]
    public void Title_Updates_When_Step_Changes ()
    {
        // Arrange
        Wizard wizard = new () { Title = "Setup" };
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        wizard.GoNext ();

        // Assert
        Assert.Contains ("Step 2", wizard.Title);
        Assert.DoesNotContain ("Step 1", wizard.Title);
    }

    #endregion Title Tests

    #region Button Visibility Tests

    [Fact]
    public void BackButton_Not_Visible_On_First_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act & Assert
        Assert.False (wizard.BackButton.Visible);
    }

    [Fact]
    public void BackButton_Visible_On_Non_First_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        wizard.GoNext ();

        // Assert
        Assert.True (wizard.BackButton.Visible);
    }

    [Fact]
    public void NextFinishButton_Shows_Next_On_Non_Last_Steps ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act & Assert
        Assert.Equal (Strings.wzNext, wizard.NextFinishButton.Text);
    }

    [Fact]
    public void NextFinishButton_Shows_Finish_On_Last_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        wizard.GoNext ();

        // Assert
        Assert.Equal (Strings.wzFinish, wizard.NextFinishButton.Text);
    }

    #endregion Button Visibility Tests

    #region Custom Button Text Tests

    [Fact]
    public void BackButton_Uses_Custom_Text_From_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2", BackButtonText = "Go Back" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act
        wizard.GoNext ();

        // Assert
        Assert.Equal ("Go Back", wizard.BackButton.Text);
    }

    [Fact]
    public void NextFinishButton_Uses_Custom_Text_From_Step ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1", NextButtonText = "Proceed" };
        wizard.AddStep (step1);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act & Assert
        Assert.Equal ("Proceed", wizard.NextFinishButton.Text);
    }

    #endregion Custom Button Text Tests

    #region Event Tests

    [Fact]
    public void MovingNext_Event_Raised_When_Next_Button_Clicked ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();

        var eventRaised = false;
        wizard.MovingNext += (sender, args) => { eventRaised = true; };

        // Act
        wizard.NextFinishButton.InvokeCommand (Command.Accept);

        // Assert
        Assert.True (eventRaised);
    }

    [Fact]
    public void MovingBack_Event_Raised_When_Back_Button_Clicked ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.BeginInit ();
        wizard.EndInit ();
        wizard.GoNext ();

        var eventRaised = false;
        wizard.MovingBack += (sender, args) => { eventRaised = true; };

        // Act
        wizard.BackButton.InvokeCommand (Command.Accept);

        // Assert
        Assert.True (eventRaised);
    }

    #endregion Event Tests

    #region Enabled State Tests

    [Fact]
    public void Disabling_Step_Updates_Navigation ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2" };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act - Disable middle step
        step2.Enabled = false;

        // Assert - GetNextStep from step1 should skip to step3
        WizardStep? nextStep = wizard.GetNextStep ();
        Assert.Equal (step3, nextStep);
    }

    [Fact]
    public void Enabling_Step_Updates_Navigation ()
    {
        // Arrange
        Wizard wizard = new ();
        WizardStep step1 = new () { Title = "Step 1" };
        WizardStep step2 = new () { Title = "Step 2", Enabled = false };
        WizardStep step3 = new () { Title = "Step 3" };
        wizard.AddStep (step1);
        wizard.AddStep (step2);
        wizard.AddStep (step3);
        wizard.BeginInit ();
        wizard.EndInit ();

        // Act - Enable middle step
        step2.Enabled = true;

        // Assert - GetNextStep from step1 should now return step2
        WizardStep? nextStep = wizard.GetNextStep ();
        Assert.Equal (step2, nextStep);
    }

    #endregion Enabled State Tests
}
