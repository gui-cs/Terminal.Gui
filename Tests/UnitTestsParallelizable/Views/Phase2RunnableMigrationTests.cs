using Xunit;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Terminal.Gui.ViewTests;

/// <summary>
/// Simple tests for Phase 2 of the IRunnable migration.
/// These tests verify the basic interface contracts without complex lifecycle scenarios.
/// </summary>
public class Phase2RunnableMigrationTests
{
    [Fact]
    public void Toplevel_ImplementsIRunnable()
    {
        // Arrange & Act
        Toplevel toplevel = new ();

        // Assert
        Assert.IsAssignableFrom<IRunnable> (toplevel);
        toplevel.Dispose ();
    }

    [Fact]
    public void Dialog_ImplementsIRunnableInt()
    {
        // Arrange & Act
        Dialog dialog = new ();

        // Assert
        Assert.IsAssignableFrom<IRunnable<int?>> (dialog);
        Assert.IsAssignableFrom<IRunnable> (dialog);
        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_Result_DefaultsToNull()
    {
        // Arrange & Act
        Dialog dialog = new ();

        // Assert
        Assert.Null (dialog.Result);
        dialog.Dispose ();
    }

    [Fact]
    public void Dialog_Canceled_DefaultsToFalse()
    {
        // Arrange & Act
        Dialog dialog = new ();

        // Assert
        // Note: The XML doc says default is true, but the field _canceled defaults to false
        Assert.False (dialog.Canceled);
        dialog.Dispose ();
    }

    [Fact]
    public void Wizard_InheritsFromDialog_ImplementsIRunnable()
    {
        // Arrange & Act
        Wizard wizard = new ();

        // Assert
        Assert.IsAssignableFrom<Dialog> (wizard);
        Assert.IsAssignableFrom<IRunnable<int?>> (wizard);
        wizard.Dispose ();
    }

    [Fact]
    public void Wizard_WasFinished_DefaultsToFalse()
    {
        // Arrange & Act
        Wizard wizard = new ();

        // Assert
        Assert.False (wizard.WasFinished);
        wizard.Dispose ();
    }

    [Fact]
    public void MessageBox_Clicked_PropertyExists()
    {
        // Arrange & Act
        int clicked = MessageBox.Clicked;

        // Assert - Just verify the property exists and has the expected type
        Assert.True (clicked is int);
    }

    [Fact]
    public void Toplevel_Modal_PropertyWorks()
    {
        // Arrange
        Toplevel toplevel = new ();

        // Act
        toplevel.Modal = true;
        bool modalValue = toplevel.Modal;

        // Assert
        Assert.True (modalValue);
        toplevel.Dispose ();
    }

    [Fact]
    public void Dialog_HasButtons_Property()
    {
        // Arrange & Act
        Dialog dialog = new ()
        {
            Buttons =
            [
                new Button { Text = "OK" },
                new Button { Text = "Cancel" }
            ]
        };

        // Assert
        Assert.NotNull (dialog.Buttons);
        Assert.Equal (2, dialog.Buttons.Length);
        dialog.Dispose ();
    }

    [Fact]
    public void Wizard_HasNextFinishButton()
    {
        // Arrange & Act
        Wizard wizard = new ();

        // Assert
        Assert.NotNull (wizard.NextFinishButton);
        Assert.NotNull (wizard.BackButton);
        wizard.Dispose ();
    }
}
