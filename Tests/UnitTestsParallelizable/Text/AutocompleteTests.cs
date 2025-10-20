using System.Text.RegularExpressions;

namespace Terminal.Gui.TextTests;

/// <summary>
/// Pure unit tests for Autocomplete functionality that don't require Application or Driver.
/// Integration tests for Autocomplete (popup behavior, rendering) remain in UnitTests.
/// </summary>
public class AutocompleteTests : UnitTests.Parallelizable.ParallelizableBase
{
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
}
