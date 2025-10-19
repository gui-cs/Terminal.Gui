#nullable enable
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using static Terminal.Gui.Configuration.ConfigurationManager;

namespace Terminal.Gui.ConfigurationTests;

public class SchemeManagerTests
{
    [Fact]
    public void GetSchemes_Not_Enabled_Gets_Schemes ()
    {
        Disable (true);

        Dictionary<string, Scheme?>? schemes = SchemeManager.GetSchemesForCurrentTheme ();
        Assert.NotNull (schemes);
        Assert.NotNull (schemes ["Base"]);
        Assert.True (schemes!.ContainsKey ("Base"));
        Assert.True (schemes.ContainsKey ("base"));

        Assert.Equal (SchemeManager.GetSchemes (), schemes);
    }

    [Fact]
    public void GetSchemes_Enabled_Gets_Current ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);

            Dictionary<string, Scheme?>? schemes = SchemeManager.GetSchemesForCurrentTheme ();
            Assert.NotNull (schemes);
            Assert.NotNull (schemes ["Base"]);
            Assert.True (schemes!.ContainsKey ("Base"));
            Assert.True (schemes.ContainsKey ("base"));

            Assert.Equal (SchemeManager.GetSchemes (), schemes);

        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void GetSchemes_Get_Schemes_After_Load ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);
            Load (ConfigLocations.All);
            Apply ();

            Assert.Equal (SchemeManager.GetSchemes (), SchemeManager.GetSchemesForCurrentTheme ());
        }
        finally
        {
            Disable (true);
        }
    }


    [Fact]
    public void GetHardCodedSchemes_Gets_HardCoded_Theme_Schemes ()
    {
        ImmutableSortedDictionary<string, Scheme?>? hardCoded = SchemeManager.GetHardCodedSchemes ();

        Assert.Equal (Scheme.GetHardCodedSchemes (), actual: hardCoded!);
    }

    [Fact]
    public void GetHardCodedSchemes_Have_Expected_Normal_Attributes ()
    {
        var schemes = SchemeManager.GetHardCodedSchemes ();
        Assert.NotNull (schemes);

        // Base
        var baseScheme = schemes! ["Base"];
        Assert.NotNull (baseScheme);
        Assert.Equal (new Attribute (StandardColor.LightBlue, StandardColor.RaisinBlack), baseScheme!.Normal);

        // Dialog
        var dialogScheme = schemes ["Dialog"];
        Assert.NotNull (dialogScheme);
        Assert.Equal (new Attribute (StandardColor.LightSkyBlue, StandardColor.OuterSpace), dialogScheme!.Normal);

        // Error
        var errorScheme = schemes ["Error"];
        Assert.NotNull (errorScheme);
        Assert.Equal (new Attribute (StandardColor.IndianRed, StandardColor.RaisinBlack), errorScheme!.Normal);

        // Menu (Bold style)
        var menuScheme = schemes ["Menu"];
        Assert.NotNull (menuScheme);
        Assert.Equal (new Attribute (StandardColor.Charcoal, StandardColor.LightBlue, TextStyle.Bold), menuScheme!.Normal);

        // Toplevel
        var toplevelScheme = schemes ["Toplevel"];
        Assert.NotNull (toplevelScheme);
        Assert.Equal (new Attribute (StandardColor.CadetBlue, StandardColor.Charcoal).ToString (), toplevelScheme!.Normal.ToString ());
    }


    [Fact]
    public void GetHardCodedSchemes_Have_Expected_Normal_Attributes_LoadHardCodedDefaults ()
    {
        LoadHardCodedDefaults ();
        var schemes = SchemeManager.GetHardCodedSchemes ();

        Assert.NotNull (schemes);

        // Base
        var baseScheme = schemes! ["Base"];
        Assert.NotNull (baseScheme);
        Assert.Equal (new Attribute (StandardColor.LightBlue, StandardColor.RaisinBlack), baseScheme!.Normal);

        // Dialog
        var dialogScheme = schemes ["Dialog"];
        Assert.NotNull (dialogScheme);
        Assert.Equal (new Attribute (StandardColor.LightSkyBlue, StandardColor.OuterSpace), dialogScheme!.Normal);

        // Error
        var errorScheme = schemes ["Error"];
        Assert.NotNull (errorScheme);
        Assert.Equal (new Attribute (StandardColor.IndianRed, StandardColor.RaisinBlack), errorScheme!.Normal);

        // Menu (Bold style)
        var menuScheme = schemes ["Menu"];
        Assert.NotNull (menuScheme);
        Assert.Equal (new Attribute (StandardColor.Charcoal, StandardColor.LightBlue, TextStyle.Bold), menuScheme!.Normal);

        // Toplevel
        var toplevelScheme = schemes ["Toplevel"];
        Assert.NotNull (toplevelScheme);
        Assert.Equal (new Attribute (StandardColor.CadetBlue, StandardColor.Charcoal).ToString (), toplevelScheme!.Normal.ToString ());
    }
    [Fact]
    public void Not_Case_Sensitive_Disabled ()
    {
        Assert.False (IsEnabled);
        Dictionary<string, Scheme?>? current = SchemeManager.GetSchemesForCurrentTheme ();
        Assert.NotNull (current);

        Assert.True (current!.ContainsKey ("Base"));
        Assert.True (current.ContainsKey ("base"));
    }

    [Fact]
    public void Not_Case_Sensitive_Enabled ()
    {
        Assert.False (IsEnabled);

        try
        {
            Enable (ConfigLocations.HardCoded);

            Assert.True (SchemeManager.GetSchemesForCurrentTheme ()!.ContainsKey ("Base"));
            Assert.True (SchemeManager.GetSchemesForCurrentTheme ()!.ContainsKey ("base"));

            ResetToHardCodedDefaults ();
            Dictionary<string, Scheme?>? current = SchemeManager.GetSchemesForCurrentTheme ();
            Assert.NotNull (current);

            Assert.True (current!.ContainsKey ("Base"));
            Assert.True (current.ContainsKey ("base"));
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Load_Adds ()
    {
        // arrange
        Enable (ConfigLocations.HardCoded);

        var theme = new ThemeScope ();
        theme.LoadHardCodedDefaults ();
        Assert.NotEmpty (theme);

        Assert.Equal (5, SchemeManager.GetSchemes ().Count);

        theme ["Schemes"].PropertyValue = SchemeManager.GetSchemes ();

        Dictionary<string, Scheme> schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!;
        Assert.Equal (SchemeManager.GetSchemes ().Count, schemes.Count);

        var newTheme = new ThemeScope ();
        newTheme.LoadHardCodedDefaults ();

        var scheme = new Scheme
        {
            // note: Scheme's can't be partial; default for each attribute
            // is always White/Black
            Normal = new (Color.Red, Color.Green),
            Focus = new (Color.Cyan, Color.BrightCyan),
            HotNormal = new (Color.Yellow, Color.BrightYellow),
            HotFocus = new (Color.Green, Color.BrightGreen),
            Disabled = new (Color.Gray, Color.DarkGray)
        };

        newTheme ["Schemes"].PropertyValue = SchemeManager.GetSchemesForCurrentTheme ();
        Assert.Equal (5, SchemeManager.GetSchemes ().Count);

        // add a new Scheme to the newTheme
        ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!) ["Test"] = scheme;

        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!;
        Assert.Equal (SchemeManager.GetSchemes ().Count, schemes.Count);

        // Act
        theme.UpdateFrom (newTheme);

        // Assert
        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!;
        Assert.Equal (schemes ["Test"].Normal, scheme.Normal);
        Assert.Equal (schemes ["Test"].Focus, scheme.Focus);
        Disable (true);
    }

    [Fact]
    public void Load_Changes ()
    {
        // arrange
        Enable (ConfigLocations.HardCoded);

        var theme = new ThemeScope ();
        theme.LoadHardCodedDefaults ();
        Assert.NotEmpty (theme);

        var scheme = new Scheme
        {
            // note: Scheme's can't be partial; default for each attribute
            // is always White/Black
            Normal = new (Color.Red, Color.Green),
            Focus = new (Color.Cyan, Color.BrightCyan),
            HotNormal = new (Color.Yellow, Color.BrightYellow),
            HotFocus = new (Color.Green, Color.BrightGreen),
            Disabled = new (Color.Gray, Color.DarkGray)
        };
        theme ["Schemes"].PropertyValue = SchemeManager.GetSchemesForCurrentTheme ();

        ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!)! ["Test"] = scheme;

        Dictionary<string, Scheme>? schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!;
        Assert.Equal (scheme.Normal, schemes ["Test"].Normal);
        Assert.Equal (scheme.Focus, schemes ["Test"].Focus);

        // Change just Normal
        var newTheme = new ThemeScope ();
        newTheme.LoadHardCodedDefaults ();

        var newScheme = new Scheme
        {
            Normal = new (Color.Blue, Color.BrightBlue),
            Focus = scheme.Focus,
            HotNormal = scheme.HotNormal,
            HotFocus = scheme.HotFocus,
            Disabled = scheme.Disabled
        };
        newTheme ["Schemes"].PropertyValue = SchemeManager.GetSchemesForCurrentTheme ();
        ((Dictionary<string, Scheme>)newTheme ["Schemes"].PropertyValue!)! ["Test"] = newScheme;

        // Act
        theme.UpdateFrom (newTheme);

        // Assert
        schemes = (Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!;

        // Normal should have changed
        Assert.Equal (new (Color.Blue, Color.BrightBlue), schemes ["Test"].Normal);
        Assert.Equal (Color.BrightBlue, schemes ["Test"].Normal.Background);
        Assert.Equal (Color.Cyan, schemes ["Test"].Focus.Foreground);
        Assert.Equal (Color.BrightCyan, schemes ["Test"].Focus.Background);
        Disable (true);
    }

    [Fact (Skip = "TODO: This should throw an exception")]
    public void Load_Null_Scheme_Throws ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            // Create a test theme
            RuntimeConfig = """
                            {
                                 "Theme": "TestTheme",
                                 "Themes": [
                                   {
                                     "TestTheme": {
                                     }
                                   }
                                 ]
                            }
                            """;

            // Load the test theme
            // TODO: This should throw an exception!
            Assert.Throws<JsonException> (() => Load (ConfigLocations.Runtime));
            Assert.Contains ("TestTheme", ThemeManager.Themes!);
            Assert.Equal ("TestTheme", ThemeManager.Theme);
            Assert.Throws<System.Collections.Generic.KeyNotFoundException> (SchemeManager.GetSchemes);

        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Load_Empty_Scheme_Throws ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            // Create a test theme
            RuntimeConfig = """
                            {
                                 "Theme": "TestTheme",
                                 "Themes": [
                                   {
                                     "TestTheme": {
                                       "Schemes": []
                                     }
                                   }
                                 ]
                            }
                            """;

            // Load the test theme
            Load (ConfigLocations.Runtime);
            Assert.Equal ("TestTheme", ThemeManager.Theme);

            // Now reset everything and reload
            ResetToHardCodedDefaults ();

            // Verify we're back to default
            Assert.Equal ("Default", ThemeManager.Theme);
        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Load_Custom_Scheme_Loads ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            // Create a test theme
            RuntimeConfig = """
                            {
                                 "Theme": "TestTheme",
                                 "Themes": [
                                   {
                                     "TestTheme": {
                                       "Schemes": [
                                                   {
                                          "TopLevel": {
                                            "Normal": {
                                              "Foreground": "AntiqueWhite",
                                              "Background": "DimGray"
                                            },
                                            "Focus": {
                                              "Foreground": "White",
                                              "Background": "DarkGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Wheat",
                                              "Background": "DarkGray",
                                              "Style": "Underline"
                                            },
                                            "HotFocus": {
                                              "Foreground": "LightYellow",
                                              "Background": "DimGray",
                                              "Style": "Underline"
                                            },
                                            "Disabled": {
                                              "Foreground": "Black",
                                              "Background": "DimGray"
                                            }
                                          }
                                        },
                                        {
                                          "Base": {
                                            "Normal": {
                                              "Foreground": "White",
                                              "Background": "Blue"
                                            },
                                            "Focus": {
                                              "Foreground": "DarkBlue",
                                              "Background": "LightGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "BrightCyan",
                                              "Background": "Blue"
                                            },
                                            "HotFocus": {
                                              "Foreground": "BrightBlue",
                                              "Background": "LightGray"
                                            },
                                            "Disabled": {
                                              "Foreground": "DarkGray",
                                              "Background": "Blue"
                                            }
                                          }
                                        },
                                        {
                                          "Dialog": {
                                            "Normal": {
                                              "Foreground": "Black",
                                              "Background": "LightGray"
                                            },
                                            "Focus": {
                                              "Foreground": "DarkGray",
                                              "Background": "LightGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Blue",
                                              "Background": "LightGray"
                                            },
                                            "HotFocus": {
                                              "Foreground": "BrightBlue",
                                              "Background": "LightGray"
                                            },
                                            "Disabled": {
                                              "Foreground": "Gray",
                                              "Background": "DarkGray"
                                            }
                                          }
                                        },
                                        {
                                          "Menu": {
                                            "Normal": {
                                              "Foreground": "White",
                                              "Background": "DarkBlue",
                                              "Style": "Reverse" // Not default Bold
                                            },
                                            "Focus": {
                                            "Foreground": "White",
                                            "Background": "DarkBlue",
                                              "Style": "Bold,Reverse"
                                            },
                                            "HotNormal": {
                                              "Foreground": "BrightYellow",
                                              "Background": "DarkBlue",
                                              "Style": "Bold,Underline"
                                            },
                                            "HotFocus": {
                                              "Foreground": "Blue",
                                              "Background": "White",
                                              "Style": "Bold,Underline"
                                            },
                                            "Disabled": {
                                              "Foreground": "Gray",
                                              "Background": "DarkGray",
                                              "Style": "Faint"
                                            }
                                          }
                                        },
                                        {
                                          "Error": {
                                            "Normal": {
                                              "Foreground": "Red",
                                              "Background": "Pink"
                                            },
                                            "Focus": {
                                              "Foreground": "White",
                                              "Background": "BrightRed"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Black",
                                              "Background": "Pink"
                                            },
                                            "HotFocus": {
                                              "Foreground": "Pink",
                                              "Background": "BrightRed"
                                            },
                                            "Disabled": {
                                              "Foreground": "DarkGray",
                                              "Background": "White"
                                            }
                                          }
                                        }
                                       ]
                                     }
                                   }
                                 ]
                            }
                            """;

            // Capture hardCoded hard-coded scheme colors
            ImmutableSortedDictionary<string, Scheme> hardCodedSchemes = SchemeManager.GetHardCodedSchemes ()!;

            Color hardCodedTopLevelNormalFg = hardCodedSchemes ["TopLevel"].Normal.Foreground;
            Assert.Equal (new Color (StandardColor.CadetBlue).ToString (), hardCodedTopLevelNormalFg.ToString ());

            Assert.Equal (hardCodedSchemes ["Menu"].Normal.Style, SchemeManager.GetSchemesForCurrentTheme () ["Menu"]!.Normal.Style);

            // Capture current scheme colors
            Dictionary<string, Scheme> currentSchemes = SchemeManager.GetSchemes ()!;

            Color currentTopLevelNormalFg = currentSchemes ["TopLevel"].Normal.Foreground;

            Assert.Equal (new Color (StandardColor.CadetBlue).ToString (), currentTopLevelNormalFg.ToString ());

            // Load the test theme
            Load (ConfigLocations.Runtime);
            Assert.Equal ("TestTheme", ThemeManager.Theme);
            Assert.Equal (TextStyle.Reverse, SchemeManager.GetSchemesForCurrentTheme () ["Menu"]!.Normal.Style);

            currentSchemes = SchemeManager.GetSchemesForCurrentTheme ()!;
            currentTopLevelNormalFg = currentSchemes ["TopLevel"].Normal.Foreground;
            Assert.NotEqual (hardCodedTopLevelNormalFg.ToString (), currentTopLevelNormalFg.ToString ());

            // Now reset everything and reload
            ResetToHardCodedDefaults ();

            // Verify we're back to default
            Assert.Equal ("Default", ThemeManager.Theme);

            currentSchemes = SchemeManager.GetSchemes ()!;
            currentTopLevelNormalFg = currentSchemes ["TopLevel"].Normal.Foreground;
            Assert.Equal (hardCodedTopLevelNormalFg.ToString (), currentTopLevelNormalFg.ToString ());

        }
        finally
        {
            Disable (true);
        }
    }

    [Fact]
    public void Load_Modified_Default_Scheme_Loads ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);
            ThrowOnJsonErrors = true;

            // Create a test theme
            RuntimeConfig = """
                            {
                                 "Theme": "Default",
                                 "Themes": [
                                   {
                                     "Default": {
                                       "Schemes": [
                                                   {
                                          "TopLevel": {
                                            "Normal": {
                                              "Foreground": "AntiqueWhite",
                                              "Background": "DimGray"
                                            },
                                            "Focus": {
                                              "Foreground": "White",
                                              "Background": "DarkGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Wheat",
                                              "Background": "DarkGray",
                                              "Style": "Underline"
                                            },
                                            "HotFocus": {
                                              "Foreground": "LightYellow",
                                              "Background": "DimGray",
                                              "Style": "Underline"
                                            },
                                            "Disabled": {
                                              "Foreground": "Black",
                                              "Background": "DimGray"
                                            }
                                          }
                                        },
                                        {
                                          "Base": {
                                            "Normal": {
                                              "Foreground": "White",
                                              "Background": "Blue"
                                            },
                                            "Focus": {
                                              "Foreground": "DarkBlue",
                                              "Background": "LightGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "BrightCyan",
                                              "Background": "Blue"
                                            },
                                            "HotFocus": {
                                              "Foreground": "BrightBlue",
                                              "Background": "LightGray"
                                            },
                                            "Disabled": {
                                              "Foreground": "DarkGray",
                                              "Background": "Blue"
                                            }
                                          }
                                        },
                                        {
                                          "Dialog": {
                                            "Normal": {
                                              "Foreground": "Black",
                                              "Background": "LightGray"
                                            },
                                            "Focus": {
                                              "Foreground": "DarkGray",
                                              "Background": "LightGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Blue",
                                              "Background": "LightGray"
                                            },
                                            "HotFocus": {
                                              "Foreground": "BrightBlue",
                                              "Background": "LightGray"
                                            },
                                            "Disabled": {
                                              "Foreground": "Gray",
                                              "Background": "DarkGray"
                                            }
                                          }
                                        },
                                        {
                                          "Menu": {
                                            "Normal": {
                                              "Foreground": "White",
                                              "Background": "DarkBlue",
                                              "Style": "Reverse" // Not default Bold
                                            },
                                            "Focus": {
                                            "Foreground": "White",
                                            "Background": "DarkBlue",
                                              "Style": "Bold,Reverse"
                                            },
                                            "HotNormal": {
                                              "Foreground": "BrightYellow",
                                              "Background": "DarkBlue",
                                              "Style": "Bold,Underline"
                                            },
                                            "HotFocus": {
                                              "Foreground": "Blue",
                                              "Background": "White",
                                              "Style": "Bold,Underline"
                                            },
                                            "Disabled": {
                                              "Foreground": "Gray",
                                              "Background": "DarkGray",
                                              "Style": "Faint"
                                            }
                                          }
                                        },
                                        {
                                          "Error": {
                                            "Normal": {
                                              "Foreground": "Red",
                                              "Background": "Pink"
                                            },
                                            "Focus": {
                                              "Foreground": "White",
                                              "Background": "BrightRed"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Black",
                                              "Background": "Pink"
                                            },
                                            "HotFocus": {
                                              "Foreground": "Pink",
                                              "Background": "BrightRed"
                                            },
                                            "Disabled": {
                                              "Foreground": "DarkGray",
                                              "Background": "White"
                                            }
                                          }
                                        }
                                       ]
                                     }
                                   }
                                 ]
                            }
                            """;

            // Capture hardCoded hard-coded scheme colors
            ImmutableSortedDictionary<string, Scheme> hardCodedSchemes = SchemeManager.GetHardCodedSchemes ()!;

            Color hardCodedTopLevelNormalFg = hardCodedSchemes ["TopLevel"].Normal.Foreground;
            Assert.Equal (new Color (StandardColor.CadetBlue).ToString (), hardCodedTopLevelNormalFg.ToString ());

            Assert.Equal (hardCodedSchemes ["Menu"].Normal.Style, SchemeManager.GetSchemesForCurrentTheme () ["Menu"]!.Normal.Style);

            // Capture current scheme colors
            Dictionary<string, Scheme> currentSchemes = SchemeManager.GetSchemes ()!;

            Color currentTopLevelNormalFg = currentSchemes ["TopLevel"].Normal.Foreground;

            Assert.Equal (new Color (StandardColor.CadetBlue).ToString (), currentTopLevelNormalFg.ToString ());

            // Load the test theme
            Load (ConfigLocations.Runtime);
            Assert.Equal ("Default", ThemeManager.Theme);
            // BUGBUG: We did not Apply after loading, so schemes should NOT have been updated
            Assert.Equal (TextStyle.Reverse, SchemeManager.GetSchemesForCurrentTheme () ["Menu"]!.Normal.Style);

            currentSchemes = SchemeManager.GetSchemesForCurrentTheme ()!;
            currentTopLevelNormalFg = currentSchemes ["TopLevel"].Normal.Foreground;
            // BUGBUG: We did not Apply after loading, so schemes should NOT have been updated
            //Assert.Equal (hardCodedTopLevelNormalFg.ToString (), currentTopLevelNormalFg.ToString ());

            // Now reset everything and reload
            ResetToHardCodedDefaults ();

            // Verify we're back to default
            Assert.Equal ("Default", ThemeManager.Theme);

            currentSchemes = SchemeManager.GetSchemes ()!;
            currentTopLevelNormalFg = currentSchemes ["TopLevel"].Normal.Foreground;
            Assert.Equal (hardCodedTopLevelNormalFg.ToString (), currentTopLevelNormalFg.ToString ());

        }
        finally
        {
            Disable (true);
        }
    }


    [Fact]
    public void Load_From_Json_Does_Not_Corrupt_HardCodedSchemes ()
    {
        try
        {
            Enable (ConfigLocations.HardCoded);

            // Create a test theme
            string json = """
                            {
                                 "Theme": "TestTheme",
                                 "Themes": [
                                   {
                                     "TestTheme": {
                                       "Schemes": [
                                                   {
                                          "TopLevel": {
                                            "Normal": {
                                              "Foreground": "AntiqueWhite",
                                              "Background": "DimGray"
                                            },
                                            "Focus": {
                                              "Foreground": "White",
                                              "Background": "DarkGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Wheat",
                                              "Background": "DarkGray",
                                              "Style": "Underline"
                                            },
                                            "HotFocus": {
                                              "Foreground": "LightYellow",
                                              "Background": "DimGray",
                                              "Style": "Underline"
                                            },
                                            "Disabled": {
                                              "Foreground": "Black",
                                              "Background": "DimGray"
                                            }
                                          }
                                        },
                                        {
                                          "Base": {
                                            "Normal": {
                                              "Foreground": "White",
                                              "Background": "Blue"
                                            },
                                            "Focus": {
                                              "Foreground": "DarkBlue",
                                              "Background": "LightGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "BrightCyan",
                                              "Background": "Blue"
                                            },
                                            "HotFocus": {
                                              "Foreground": "BrightBlue",
                                              "Background": "LightGray"
                                            },
                                            "Disabled": {
                                              "Foreground": "DarkGray",
                                              "Background": "Blue"
                                            }
                                          }
                                        },
                                        {
                                          "Dialog": {
                                            "Normal": {
                                              "Foreground": "Black",
                                              "Background": "LightGray"
                                            },
                                            "Focus": {
                                              "Foreground": "DarkGray",
                                              "Background": "LightGray"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Blue",
                                              "Background": "LightGray"
                                            },
                                            "HotFocus": {
                                              "Foreground": "BrightBlue",
                                              "Background": "LightGray"
                                            },
                                            "Disabled": {
                                              "Foreground": "Gray",
                                              "Background": "DarkGray"
                                            }
                                          }
                                        },
                                        {
                                          "Menu": {
                                            "Normal": {
                                              "Foreground": "White",
                                              "Background": "DarkBlue",
                                              "Style": "Reverse" // Not default Bold
                                            },
                                            "Focus": {
                                            "Foreground": "White",
                                            "Background": "DarkBlue",
                                              "Style": "Bold,Reverse"
                                            },
                                            "HotNormal": {
                                              "Foreground": "BrightYellow",
                                              "Background": "DarkBlue",
                                              "Style": "Bold,Underline"
                                            },
                                            "HotFocus": {
                                              "Foreground": "Blue",
                                              "Background": "White",
                                              "Style": "Bold,Underline"
                                            },
                                            "Disabled": {
                                              "Foreground": "Gray",
                                              "Background": "DarkGray",
                                              "Style": "Faint"
                                            }
                                          }
                                        },
                                        {
                                          "Error": {
                                            "Normal": {
                                              "Foreground": "Red",
                                              "Background": "Pink"
                                            },
                                            "Focus": {
                                              "Foreground": "White",
                                              "Background": "BrightRed"
                                            },
                                            "HotNormal": {
                                              "Foreground": "Black",
                                              "Background": "Pink"
                                            },
                                            "HotFocus": {
                                              "Foreground": "Pink",
                                              "Background": "BrightRed"
                                            },
                                            "Disabled": {
                                              "Foreground": "DarkGray",
                                              "Background": "White"
                                            }
                                          }
                                        }
                                       ]
                                     }
                                   }
                                 ]
                            }
                            """;

            // Capture dynamically created hardCoded hard-coded scheme colors
            ImmutableSortedDictionary<string, Scheme> hardCodedSchemes = SchemeManager.GetHardCodedSchemes ()!;

            Color hardCodedTopLevelNormalFg = hardCodedSchemes ["TopLevel"].Normal.Foreground;
            Assert.Equal (new Color (StandardColor.CadetBlue).ToString (), hardCodedTopLevelNormalFg.ToString ());

            // Capture current scheme colors
            Dictionary<string, Scheme> currentSchemes = SchemeManager.GetSchemes ()!;
            Color currentTopLevelNormalFg = currentSchemes ["TopLevel"].Normal.Foreground;
            Assert.Equal (new Color (StandardColor.CadetBlue).ToString (), currentTopLevelNormalFg.ToString ());

            // Load the test theme
            ConfigurationManager.SourcesManager?.Load (Settings, json, "UpdateFromJson", ConfigLocations.Runtime);

            Assert.Equal ("TestTheme", ThemeManager.Theme);
            Assert.Equal (TextStyle.Reverse, SchemeManager.GetSchemesForCurrentTheme () ["Menu"]!.Normal.Style);
            Dictionary<string, Scheme>? hardCodedSchemesViaScope = GetHardCodedConfigPropertiesByScope ("ThemeScope")!.ToFrozenDictionary () ["Schemes"].PropertyValue as Dictionary<string, Scheme>;
            Assert.Equal (hardCodedTopLevelNormalFg.ToString (), hardCodedSchemesViaScope! ["TopLevel"].Normal.Foreground.ToString ());

        }
        finally
        {
            Disable (true);
        }
    }

    [Fact (Skip = "WIP")]
    public void Apply_UpdatesSchemes ()
    {
        Enable (ConfigLocations.HardCoded);

        Assert.False (SchemeManager.GetSchemes ()!.ContainsKey ("test"));
        Assert.Equal (5, SchemeManager.GetSchemes ().Count); // base, toplevel, menu, error, dialog

        var theme = new ThemeScope ();
        Assert.NotEmpty (theme);

        ThemeManager.Themes!.TryAdd ("testTheme", theme);

        var scheme = new Scheme { Normal = new (Color.Red, Color.Green) };

        theme ["Schemes"].PropertyValue = new Dictionary<string, Scheme> (StringComparer.InvariantCultureIgnoreCase) { { "test", scheme } };

        Assert.Equal (
                      new (Color.Red),
                      ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!) ["test"].Normal.Foreground
                     );

        Assert.Equal (
                      new (Color.Green),
                      ((Dictionary<string, Scheme>)theme ["Schemes"].PropertyValue!) ["test"].Normal.Background
                     );

        // Act
        ThemeManager.Theme = "testTheme";
        ThemeManager.Themes! [ThemeManager.Theme]!.Apply ();
        Assert.Equal (5, SchemeManager.GetSchemes ().Count); // base, toplevel, menu, error, dialog

        // Assert
        Scheme updatedScheme = SchemeManager.GetSchemes () ["test"]!;
        Assert.Equal (new (Color.Red), updatedScheme.Normal.Foreground);
        Assert.Equal (new (Color.Green), updatedScheme.Normal.Background);

        // remove test Scheme from Colors to avoid failures on others unit tests with Scheme
        SchemeManager.GetSchemes ().Remove ("test");
        Assert.Equal (5, SchemeManager.GetSchemes ().Count);

        Disable (true);
    }
}
