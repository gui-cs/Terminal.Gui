using UICatalog;
using Xunit.Abstractions;

namespace IntegrationTests.UICatalog;

/// <summary>
///     Integration tests for ForceDriver persistence when opening scenarios in UICatalog.
/// </summary>
public class ForceDriverTests
{
    private readonly ITestOutputHelper _output;

    public ForceDriverTests (ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    ///     Tests that ForceDriver persists when opening a scenario after Init/Shutdown cycles.
    ///     This verifies the fix for issue #4391.
    /// </summary>
    [Fact]
    public void ForceDriver_Persists_Across_Init_Shutdown_Cycles ()
    {
        // Arrange
        const string expectedDriver = "fake";
        
        ConfigurationManager.Disable (true);
        Application.ResetState (true);

        // Set ForceDriver in RuntimeConfig (simulating what UICatalog does with --driver option)
        ConfigurationManager.RuntimeConfig = $$"""
            {
                "Application.ForceDriver": "{{expectedDriver}}"
            }
            """;

        // Enable ConfigurationManager with all locations (as UICatalog does)
        ConfigurationManager.Enable (ConfigLocations.All);

        var firstDriverName = string.Empty;
        var secondDriverName = string.Empty;

        try
        {
            // Act - Cycle 1: Init and check driver
            _output.WriteLine ("Cycle 1: First Init");
            Application.Init ();
            firstDriverName = Application.Driver?.GetName () ?? string.Empty;
            _output.WriteLine ($"Cycle 1 driver: {firstDriverName}");
            Application.Shutdown ();

            // Act - Cycle 2: Reload RuntimeConfig and Init again (simulating scenario opening)
            _output.WriteLine ("Cycle 2: Reload RuntimeConfig and Init again");
            
            // This simulates what the fix does before each scenario
            ConfigurationManager.Load (ConfigLocations.Runtime);
            ConfigurationManager.Apply ();

            // Scenario calls Application.Init() without parameters
            Application.Init ();
            secondDriverName = Application.Driver?.GetName () ?? string.Empty;
            _output.WriteLine ($"Cycle 2 driver: {secondDriverName}");
            Application.Shutdown ();

            // Assert
            Assert.Equal (expectedDriver, firstDriverName);
            Assert.Equal (expectedDriver, secondDriverName);
            _output.WriteLine ($"SUCCESS: Driver '{expectedDriver}' persisted across Init/Shutdown cycles");
        }
        finally
        {
            ConfigurationManager.Disable (true);
            Application.ResetState (true);
        }
    }

    /// <summary>
    ///     Tests that ForceDriver is used when a scenario calls Application.Init() without parameters.
    ///     This simulates the actual UICatalog scenario execution flow.
    /// </summary>
    [Fact]
    public void ForceDriver_Used_By_Scenario_Init ()
    {
        // Arrange
        const string expectedDriver = "fake";
        Scenario? scenario = null;

        ConfigurationManager.Disable (true);
        Application.ResetState (true);

        // Set ForceDriver in RuntimeConfig
        ConfigurationManager.RuntimeConfig = $$"""
            {
                "Application.ForceDriver": "{{expectedDriver}}"
            }
            """;

        // Enable ConfigurationManager
        ConfigurationManager.Enable (ConfigLocations.All);

        try
        {
            // Get the first available scenario
            var scenarios = Scenario.GetScenarios ();
            Assert.NotEmpty (scenarios);
            
            scenario = scenarios[0];
            var scenarioName = scenario.GetName ();
            _output.WriteLine ($"Testing with scenario: {scenarioName}");

            // Reload RuntimeConfig before scenario (as the fix does)
            ConfigurationManager.Load (ConfigLocations.Runtime);
            ConfigurationManager.Apply ();

            // Scenario calls Application.Init() - it should use ForceDriver
            Application.Init ();
            var driverName = Application.Driver?.GetName () ?? string.Empty;
            _output.WriteLine ($"Scenario driver: {driverName}");

            // Assert
            Assert.Equal (expectedDriver, driverName);
            _output.WriteLine ($"SUCCESS: Scenario uses ForceDriver '{expectedDriver}'");

            Application.Shutdown ();
        }
        finally
        {
            scenario?.Dispose ();
            ConfigurationManager.Disable (true);
            Application.ResetState (true);
        }
    }
}
