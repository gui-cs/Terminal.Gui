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
    ///     Tests that ForceDriver persists when running UICatalogTop and then opening a scenario.
    ///     
    ///     This test verifies the fix for issue #4391 works correctly by calling UICatalog's actual methods.
    ///     
    ///     THE BUG: Without the fix, ForceDriver was set directly on Application, but
    ///     ConfigurationManager would override it from config files when scenarios ran.
    ///     
    ///     THE FIX has two parts (both in UICatalog.cs):
    ///     1. SetupForceDriverConfig() - Sets ForceDriver in ConfigurationManager.RuntimeConfig
    ///     2. ReloadForceDriverConfig() - Reloads RuntimeConfig before each scenario
    ///     
    ///     This test calls both UICatalog methods to verify the fix works.
    ///     If you remove the calls to these methods or modify UICatalog.cs to remove the fix,
    ///     this test will fail.
    /// </summary>
    [Fact]
    public void ForceDriver_Persists_From_UICatalogTop_To_Scenario ()
    {
        // Arrange
        const string expectedDriver = "fake";
        
        ConfigurationManager.Disable (true);
        Application.ResetState (true);

        // Initialize UICatalog.Options (required by UICatalogTop)
        global::UICatalog.UICatalog.Options = new UICatalogCommandLineOptions
        {
            Driver = expectedDriver,
            DontEnableConfigurationManagement = false,
            Scenario = "none",
            BenchmarkTimeout = 2500,
            Benchmark = false,
            ResultsFile = string.Empty,
            DebugLogLevel = "Warning"
        };

        // Initialize cached scenarios (required by UICatalogTop)
        UICatalogTop.CachedScenarios = Scenario.GetScenarios ();

        // Call UICatalog's setup method (this is part 1 of the fix in UICatalog.cs)
        // This sets ForceDriver in RuntimeConfig
        global::UICatalog.UICatalog.SetupForceDriverConfig (expectedDriver);

        // Enable ConfigurationManager with all locations (as UICatalog does)
        ConfigurationManager.Enable (ConfigLocations.All);

        var topLevelDriverName = string.Empty;
        var scenarioDriverName = string.Empty;
        var iterationCount = 0;
        EventHandler<IterationEventArgs>? iterationHandler = null;

        try
        {
            // Phase 1: Run UICatalogTop (simulating main UI)
            _output.WriteLine ("=== Phase 1: Running UICatalogTop ===");
            Application.Init ();
            topLevelDriverName = Application.Driver?.GetName () ?? string.Empty;
            _output.WriteLine ($"UICatalogTop driver: {topLevelDriverName}");

            var top = new UICatalogTop ();
            
            // Set up to automatically select a scenario and stop
            iterationHandler = (sender, e) =>
            {
                iterationCount++;
                
                // On first iteration, select a scenario and request stop
                if (iterationCount == 1)
                {
                    // Select the first scenario
                    if (UICatalogTop.CachedScenarios is { Count: > 0 })
                    {
                        UICatalogTop.CachedSelectedScenario = 
                            (Scenario)Activator.CreateInstance (UICatalogTop.CachedScenarios[0].GetType ())!;
                        Application.RequestStop ();
                    }
                }
            };
            
            Application.Iteration += iterationHandler;
            Application.Run (top);
            Application.Iteration -= iterationHandler;

            top.Dispose ();
            Application.Shutdown ();
            
            _output.WriteLine ($"Selected scenario: {UICatalogTop.CachedSelectedScenario?.GetName ()}");
            _output.WriteLine ($"UICatalogTop completed after {iterationCount} iterations");

            // Phase 2: Run the selected scenario (simulating what UICatalog.cs does)
            if (UICatalogTop.CachedSelectedScenario is { } scenario)
            {
                _output.WriteLine ($"\n=== Phase 2: Running scenario '{scenario.GetName ()}' ===");
                
                // Call UICatalog's reload method (this is part 2 of the fix in UICatalog.cs)
                // This ensures ForceDriver persists across Init/Shutdown cycles
                global::UICatalog.UICatalog.ReloadForceDriverConfig ();
                _output.WriteLine ("Reloaded ForceDriver config via UICatalog.ReloadForceDriverConfig()");

                // Track the driver used inside the scenario
                string? driverInsideScenario = null;
                EventHandler<EventArgs<bool>>? initHandler = null;
                EventHandler<IterationEventArgs>? scenarioIterationHandler = null;
                
                initHandler = (s, e) =>
                {
                    if (e.Value)
                    {
                        driverInsideScenario = Application.Driver?.GetName ();
                        
                        // Request stop immediately so the scenario doesn't actually run
                        scenarioIterationHandler = (_, _) =>
                        {
                            Application.RequestStop ();
                            // Remove immediately to avoid assertions in Shutdown
                            if (scenarioIterationHandler != null)
                            {
                                Application.Iteration -= scenarioIterationHandler;
                                scenarioIterationHandler = null;
                            }
                        };
                        Application.Iteration += scenarioIterationHandler;
                    }
                };
                
                Application.InitializedChanged += initHandler;

                // Run the scenario's Main() method (this is what UICatalog does)
                scenario.Main ();
                scenarioDriverName = driverInsideScenario ?? string.Empty;
                
                Application.InitializedChanged -= initHandler;
                scenario.Dispose ();
                
                _output.WriteLine ($"Scenario driver: {scenarioDriverName}");
                _output.WriteLine ("Scenario completed and disposed");
            }
            else
            {
                _output.WriteLine ("ERROR: No scenario was selected");
                Assert.Fail ("No scenario was selected");
            }

            // Assert
            _output.WriteLine ($"\n=== Results ===");
            _output.WriteLine ($"UICatalogTop driver: {topLevelDriverName}");
            _output.WriteLine ($"Scenario driver: {scenarioDriverName}");
            
            Assert.Equal (expectedDriver, topLevelDriverName);
            Assert.Equal (expectedDriver, scenarioDriverName);
            _output.WriteLine ($"SUCCESS: Driver '{expectedDriver}' persisted from UICatalogTop to scenario");
        }
        finally
        {
            if (iterationHandler != null)
            {
                Application.Iteration -= iterationHandler;
            }
            ConfigurationManager.Disable (true);
            Application.ResetState (true);
        }
    }

    /// <summary>
    ///     Tests that ForceDriver persists when running multiple scenarios in sequence.
    ///     
    ///     This verifies the fix works correctly by calling UICatalog's ReloadForceDriverConfig() method.
    ///     
    ///     THE FIX: ReloadForceDriverConfig() in UICatalog.cs reloads RuntimeConfig before each scenario.
    ///     Without calling this method, the driver would revert to platform default.
    /// </summary>
    [Fact]
    public void ForceDriver_Persists_Across_Multiple_Scenarios ()
    {
        // Arrange
        const string expectedDriver = "fake";
        
        ConfigurationManager.Disable (true);
        Application.ResetState (true);

        // Initialize UICatalog.Options
        global::UICatalog.UICatalog.Options = new UICatalogCommandLineOptions
        {
            Driver = expectedDriver,
            DontEnableConfigurationManagement = false,
            Scenario = "none",
            BenchmarkTimeout = 2500,
            Benchmark = false,
            ResultsFile = string.Empty,
            DebugLogLevel = "Warning"
        };

        // Call UICatalog's setup method (this is part 1 of the fix in UICatalog.cs)
        global::UICatalog.UICatalog.SetupForceDriverConfig (expectedDriver);

        // Enable ConfigurationManager
        ConfigurationManager.Enable (ConfigLocations.All);

        string? driver1 = null;
        string? driver2 = null;
        EventHandler<EventArgs<bool>>? initHandler1 = null;
        EventHandler<EventArgs<bool>>? initHandler2 = null;
        EventHandler<IterationEventArgs>? iterHandler1 = null;
        EventHandler<IterationEventArgs>? iterHandler2 = null;

        try
        {
            // Get two different scenarios to test
            var scenarios = Scenario.GetScenarios ();
            Assert.True (scenarios.Count >= 2, "Need at least 2 scenarios for this test");
            
            var scenario1 = scenarios[0];
            var scenario2 = scenarios[1];
            
            _output.WriteLine ($"Testing with scenarios: {scenario1.GetName ()} and {scenario2.GetName ()}");

            // Run scenario 1
            initHandler1 = (s, e) =>
            {
                if (e.Value)
                {
                    driver1 = Application.Driver?.GetName ();
                    iterHandler1 = (_, _) =>
                    {
                        Application.RequestStop ();
                        // Remove immediately to avoid assertions in Shutdown
                        if (iterHandler1 != null)
                        {
                            Application.Iteration -= iterHandler1;
                            iterHandler1 = null;
                        }
                    };
                    Application.Iteration += iterHandler1;
                }
            };
            
            Application.InitializedChanged += initHandler1;
            scenario1.Main ();
            Application.InitializedChanged -= initHandler1;
            scenario1.Dispose ();
            _output.WriteLine ($"Scenario 1 completed with driver: {driver1}");

            // Call UICatalog's reload method (this is part 2 of the fix in UICatalog.cs)
            // This ensures ForceDriver persists across Init/Shutdown cycles
            global::UICatalog.UICatalog.ReloadForceDriverConfig ();
            _output.WriteLine ("Reloaded ForceDriver config via UICatalog.ReloadForceDriverConfig()");

            // Run scenario 2
            // Run scenario 2
            initHandler2 = (s, e) =>
            {
                if (e.Value)
                {
                    driver2 = Application.Driver?.GetName ();
                    iterHandler2 = (_, _) =>
                    {
                        Application.RequestStop ();
                        // Remove immediately to avoid assertions in Shutdown
                        if (iterHandler2 != null)
                        {
                            Application.Iteration -= iterHandler2;
                            iterHandler2 = null;
                        }
                    };
                    Application.Iteration += iterHandler2;
                }
            };
            
            Application.InitializedChanged += initHandler2;
            scenario2.Main ();
            Application.InitializedChanged -= initHandler2;
            scenario2.Dispose ();
            _output.WriteLine ($"Scenario 2 completed with driver: {driver2}");

            // Assert
            Assert.Equal (expectedDriver, driver1);
            Assert.Equal (expectedDriver, driver2);
            _output.WriteLine ($"SUCCESS: Driver '{expectedDriver}' persisted across both scenarios");
        }
        finally
        {
            // Cleanup any remaining handlers
            if (initHandler1 != null)
            {
                Application.InitializedChanged -= initHandler1;
            }
            if (iterHandler2 != null)
            {
                Application.InitializedChanged -= initHandler2;
            }
            if (iterHandler1 != null)
            {
                Application.Iteration -= iterHandler1;
            }
            if (iterHandler2 != null)
            {
                Application.Iteration -= iterHandler2;
            }
            
            ConfigurationManager.Disable (true);
            Application.ResetState (true);
        }
    }
}
