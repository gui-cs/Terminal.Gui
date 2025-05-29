#nullable enable
using System.Diagnostics;
using System.Text.Json;

namespace Terminal.Gui.ConfigurationTests;

public class SchemeManagerTests
{

    [Fact]
    public void GetHardCodedSchemes_Gets_HardCodedDefaults ()
    {
        var hardCoded = SchemeManager.GetHardCodedSchemes ();

        // Check that the hardcoded schemes are not null
        Assert.NotNull (hardCoded);
        // Check that the hardcoded schemes are not empty
        Assert.NotEmpty (hardCoded);
    }

    [Fact]
    public void AddScheme_Adds_And_Updates_Scheme ()
    {
        // Arrange
        var scheme = new Scheme (new Attribute (Color.Red, Color.Green));
        string schemeName = "CustomScheme";

        // Act
        SchemeManager.AddScheme (schemeName, scheme);

        // Assert
        Assert.Equal (scheme, SchemeManager.GetScheme (schemeName));

        // Update the scheme
        var updatedScheme = new Scheme (new Attribute (Color.Blue, Color.Yellow));
        SchemeManager.AddScheme (schemeName, updatedScheme);

        Assert.Equal (updatedScheme, SchemeManager.GetScheme (schemeName));

        // Cleanup
        SchemeManager.RemoveScheme (schemeName);
    }

    [Fact]
    public void RemoveScheme_Removes_Custom_Scheme ()
    {
        var scheme = new Scheme (new Attribute (Color.Red, Color.Green));
        string schemeName = "RemovableScheme";
        SchemeManager.AddScheme (schemeName, scheme);

        Assert.Equal (scheme, SchemeManager.GetScheme (schemeName));

        SchemeManager.RemoveScheme (schemeName);

        Assert.Throws<KeyNotFoundException> (() => SchemeManager.GetScheme (schemeName));
    }

    [Fact]
    public void RemoveScheme_Throws_On_BuiltIn_Scheme ()
    {
        // Built-in scheme name
        Assert.Throws<InvalidOperationException> (() => SchemeManager.RemoveScheme ("Base"));
    }

    [Fact]
    public void RemoveScheme_Throws_On_NonExistent_Scheme ()
    {
        Assert.Throws<InvalidOperationException> (() => SchemeManager.RemoveScheme ("DoesNotExist"));
    }

    [Fact]
    public void GetScheme_By_Enum_Returns_Scheme ()
    {
        var scheme = SchemeManager.GetScheme (Schemes.Base);
        Assert.NotNull (scheme);
        Assert.IsType<Scheme> (scheme);
    }

    [Fact]
    public void GetSchemeNames_Returns_All_Scheme_Names ()
    {
        var names = SchemeManager.GetSchemeNames ();
        Assert.NotNull (names);
        Assert.Contains ("Base", names);
        Assert.Contains ("Menu", names);
        Assert.Contains ("Dialog", names);
        Assert.Contains ("Toplevel", names);
        Assert.Contains ("Error", names);
    }

    [Fact]
    public void SchemesToSchemeName_And_SchemeNameToSchemes_RoundTrip ()
    {
        foreach (Schemes s in Enum.GetValues (typeof (Schemes)))
        {
            var name = SchemeManager.SchemesToSchemeName (s);
            Assert.NotNull (name);
            var roundTrip = SchemeManager.SchemeNameToSchemes (name!);
            Assert.NotNull (roundTrip);
            Assert.Equal (name, roundTrip);
        }
    }

    [Fact]
    public void GetScheme_Throws_On_Invalid_Enum ()
    {
        // Use an invalid enum value (not defined in Schemes)
        Assert.Throws<ArgumentException> (() => SchemeManager.GetScheme ((Schemes)999));
    }

    [Fact]
    public void GetScheme_Throws_On_Invalid_String ()
    {
        Assert.Throws<KeyNotFoundException> (() => SchemeManager.GetScheme ("NotAScheme"));
    }

}
