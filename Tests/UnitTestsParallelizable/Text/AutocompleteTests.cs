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
}
