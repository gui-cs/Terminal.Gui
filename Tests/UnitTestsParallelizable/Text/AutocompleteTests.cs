using System.Text.RegularExpressions;
using TerminalGuiFluentTesting;
using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.TextTests;

/// <summary>
/// Pure unit tests for Autocomplete functionality that don't require Application or Driver.
/// Integration tests for Autocomplete (popup behavior, rendering) remain in UnitTests.
/// </summary>
public class AutocompleteTests : UnitTests.Parallelizable.ParallelizableBase
{
    private readonly ITestOutputHelper _output;
    
    public AutocompleteTests (ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact]
    public void Test_GenerateSuggestions_Simple ()
    {
        var ac = new TextViewAutocomplete ();

        ((SingleWordSuggestionGenerator)ac.SuggestionGenerator).AllSuggestions =
            new () { "fish", "const", "Cobble" };

        var tv = new TextView ();
        tv.InsertText ("co");

        ac.HostControl = tv;

        ac.GenerateSuggestions (
                                new (
                                     Cell.ToCellList (tv.Text),
                                     2
                                    )
                               );

        Assert.Equal (2, ac.Suggestions.Count);
        Assert.Equal ("const", ac.Suggestions [0].Title);
        Assert.Equal ("Cobble", ac.Suggestions [1].Title);
    }

    [Fact]
    public void TestSettingSchemeOnAutocomplete ()
    {
        var tv = new TextView ();

        // to begin with we should be using the default menu scheme
        Assert.Same (SchemeManager.GetSchemes () ["Menu"], tv.Autocomplete.Scheme);

        // allocate a new custom scheme
        tv.Autocomplete.Scheme = new ()
        {
            Normal = new (Color.Black, Color.Blue), Focus = new (Color.Black, Color.Cyan)
        };

        // should be separate instance
        Assert.NotSame (SchemeManager.GetSchemes () ["Menu"], tv.Autocomplete.Scheme);

        // with the values we set on it
        Assert.Equal (new (Color.Black), tv.Autocomplete.Scheme.Normal.Foreground);
        Assert.Equal (new (Color.Blue), tv.Autocomplete.Scheme.Normal.Background);

        Assert.Equal (new (Color.Black), tv.Autocomplete.Scheme.Focus.Foreground);
        Assert.Equal (new (Color.Cyan), tv.Autocomplete.Scheme.Focus.Background);
    }

    /// <summary>
    /// Proof-of-concept: This test demonstrates that TextFormatter.Draw() can be used in parallel tests
    /// by passing a local driver instance instead of relying on Application.Driver.
    /// 
    /// This proves that the 18 TextFormatterTests in UnitTests that use [SetupFakeDriver] + Draw() + DriverAssert
    /// can be migrated to Parallelizable by using a local driver.
    /// </summary>
    [Theory]
    [InlineData ("A", 0, "")]
    [InlineData ("A", 1, "A")]
    [InlineData ("A", 2, "A")]
    [InlineData ("A", 3, " A")]
    [InlineData ("AB", 1, "A")]
    [InlineData ("AB", 2, "AB")]
    [InlineData ("ABC", 3, "ABC")]
    [InlineData ("ABC", 4, "ABC")]
    [InlineData ("ABC", 5, " ABC")]
    [InlineData ("ABC", 6, " ABC")]
    [InlineData ("ABC", 9, "   ABC")]
    public void ProofOfConcept_TextFormatter_Draw_With_Local_Driver (string text, int width, string expectedText)
    {
        // Create a local driver instance (not Application.Driver!)
        var driverFactory = new FakeDriverFactory ();
        var driver = driverFactory.Create ();
        driver.SetBufferSize (width > 0 ? width : 1, 1);
        
        // Create TextFormatter
        TextFormatter tf = new ()
        {
            Text = text,
            Alignment = Alignment.Center,
            ConstrainToWidth = width,
            ConstrainToHeight = 1
        };
        
        // Call Draw with the LOCAL driver (not Application.Driver)
        tf.Draw (new (0, 0, width, 1), Attribute.Default, Attribute.Default, default, driver);
        
        // Use DriverAssert to verify the output
        DriverAssert.AssertDriverContentsWithFrameAre (expectedText, _output, driver);
    }
}
