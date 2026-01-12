// CoPilot - GitHub Copilot Agent
using Xunit.Abstractions;

namespace ViewsTests.PromptTests;

/// <summary>
///     Tests for the <see cref="Prompt"/> static class.
/// </summary>
[Collection ("Application Tests")]
public class PromptTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Show_ThrowsArgumentNullException_WhenAppIsNull ()
    {
        // Arrange
        IApplication? app = null;
        TextField textField = new () { Width = 30 };

        // Act & Assert
        Assert.Throws<ArgumentNullException> (() => Prompt.Show (app!, "Test", textField, tf => tf.Text));
    }

    [Fact]
    public void Show_ThrowsArgumentNullException_WhenViewIsNull ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();
        TextField? textField = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException> (() => Prompt.Show (app, "Test", textField!, tf => tf.Text));

        // Cleanup
        app.Dispose ();
    }

    [Fact]
    public void Show_ThrowsArgumentNullException_WhenResultExtractorIsNull ()
    {
        // Arrange
        IApplication app = Application.Create ();
        app.Init ();
        TextField textField = new () { Width = 30 };
        Func<TextField, string?>? extractor = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException> (() => Prompt.Show (app, "Test", textField, extractor!));

        // Cleanup
        app.Dispose ();
    }

    [Fact]
    public void DefaultBorderStyle_CanBeSetAndGet ()
    {
        // Arrange
        LineStyle originalStyle = Prompt.DefaultBorderStyle;

        // Act
        Prompt.DefaultBorderStyle = LineStyle.Double;

        // Assert
        Assert.Equal (LineStyle.Double, Prompt.DefaultBorderStyle);

        // Cleanup - restore original value
        Prompt.DefaultBorderStyle = originalStyle;
    }

    [Fact]
    public void DefaultButtonAlignment_CanBeSetAndGet ()
    {
        // Arrange
        Alignment originalAlignment = Prompt.DefaultButtonAlignment;

        // Act
        Prompt.DefaultButtonAlignment = Alignment.Center;

        // Assert
        Assert.Equal (Alignment.Center, Prompt.DefaultButtonAlignment);

        // Cleanup - restore original value
        Prompt.DefaultButtonAlignment = originalAlignment;
    }
}
