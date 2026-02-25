#nullable enable

// Claude - Opus 4.5
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UICatalog;
using Xunit.Abstractions;
using Timeout = System.Threading.Timeout;

namespace UnitTests.UICatalog;

public class RunnerTests (ITestOutputHelper output) : TestsAllViews
{
    [Fact]
    public void Constructor_DoesNotSetRuntimeConfig ()
    {
        // Arrange - clear any previous config
        ConfigurationManager.RuntimeConfig = null;

        // Act
        Runner _ = new ();

        // Assert - RuntimeConfig should be null or empty when no args provided
        // This verifies the early return path in the constructor
        Assert.True (string.IsNullOrEmpty (ConfigurationManager.RuntimeConfig));
    }

    [Fact]
    public void SetRuntimeConfig_WithForceDriver_SetsRuntimeConfig ()
    {
        // Arrange
        ConfigurationManager.RuntimeConfig = null;
        var driverName = "TestDriver";

        // Act
        Runner runner = new ();
        runner.SetRuntimeConfig (driverName);

        // Assert
        Assert.NotNull (ConfigurationManager.RuntimeConfig);
        Assert.Contains ("Application.ForceDriver", ConfigurationManager.RuntimeConfig);
        Assert.Contains (driverName, ConfigurationManager.RuntimeConfig);

        // Cleanup
        ConfigurationManager.RuntimeConfig = null;
    }

    [Fact]
    public void SetRuntimeConfig_WithForce16Colors_SetsRuntimeConfig ()
    {
        // Arrange
        ConfigurationManager.RuntimeConfig = null;

        // Act
        Runner runner = new ();
        runner.SetRuntimeConfig (force16Colors: true);

        // Assert
        Assert.NotNull (ConfigurationManager.RuntimeConfig);
        Assert.Contains ("Driver.Force16Colors", ConfigurationManager.RuntimeConfig);
        Assert.Contains ("true", ConfigurationManager.RuntimeConfig);

        // Cleanup
        ConfigurationManager.RuntimeConfig = null;
    }

    [Fact]
    public void SetRuntimeConfig_WithBothOptions_SetsRuntimeConfig ()
    {
        // Arrange
        ConfigurationManager.RuntimeConfig = null;

        // Act
        Runner runner = new ();
        runner.SetRuntimeConfig ("TestDriver", false);

        // Assert
        Assert.NotNull (ConfigurationManager.RuntimeConfig);
        Assert.Contains ("Application.ForceDriver", ConfigurationManager.RuntimeConfig);
        Assert.Contains ("Driver.Force16Colors", ConfigurationManager.RuntimeConfig);

        // Cleanup
        ConfigurationManager.RuntimeConfig = null;
    }

    [Fact]
    public void SaveResultsToFile_WritesValidJson ()
    {
        // Arrange
        List<BenchmarkResults> results =
        [
            new () { Scenario = "TestScenario1", Duration = TimeSpan.FromSeconds (1), IterationCount = 100 },
            new () { Scenario = "TestScenario2", Duration = TimeSpan.FromSeconds (2), IterationCount = 200 }
        ];

        string tempFile = Path.GetTempFileName ();

        try
        {
            // Act
            Runner.SaveResultsToFile (results, tempFile);

            // Assert
            Assert.True (File.Exists (tempFile));
            string content = File.ReadAllText (tempFile);
            Assert.NotEmpty (content);

            // Verify it's valid JSON
            List<BenchmarkResults>? deserialized = JsonSerializer.Deserialize<List<BenchmarkResults>> (content);
            Assert.NotNull (deserialized);
            Assert.Equal (2, deserialized.Count);
            Assert.Equal ("TestScenario1", deserialized [0].Scenario);
            Assert.Equal ("TestScenario2", deserialized [1].Scenario);
        }
        finally
        {
            // Cleanup
            if (File.Exists (tempFile))
            {
                File.Delete (tempFile);
            }
        }
    }

    [Fact]
    public void BenchmarkAllScenarios_EmptyList_ReturnsEmptyResults ()
    {
        // Arrange
        ConfigurationManager.RuntimeConfig = null;
        Runner runner = new ();
        List<Scenario> emptyScenarios = [];

        // Act
        List<BenchmarkResults> results = runner.BenchmarkAllScenarios (emptyScenarios);

        // Assert
        Assert.Empty (results);
    }

    [Theory]
    [InlineData ("Generic")]
    public void RunScenario_WithValidScenario_MarksLogCaptureStart (string scenarioName)
    {
        // Skip on macOS due to timing issues
        if (RuntimeInformation.IsOSPlatform (OSPlatform.OSX))
        {
            output.WriteLine ("Skipping on macOS due to timing issues.");

            return;
        }

        // Arrange
        ApplicationImpl.ResetModelUsageTracking ();
        ConfigurationManager.Disable (true);

        // Clear log capture and add a pre-scenario log
        global::UICatalog.UICatalog.LogCapture.Clear ();
        ILogger logger = global::UICatalog.UICatalog.LogCapture.CreateLogger ("Test");
        logger.LogInformation ("Pre-scenario log");
        int preScenarioPosition = global::UICatalog.UICatalog.LogCapture.ScenarioStartPosition;

        ConfigurationManager.RuntimeConfig = null;
        Runner runner = new ();
        runner.SetRuntimeConfig (DriverRegistry.Names.ANSI);

        // We need to run the scenario in a way that it exits quickly
        // The Generic scenario is a simple one that should work

        // Use a timer to force quit after a short time
        Timer? quitTimer = null;
        IApplication? currentApp;

        Application.InstanceInitialized += OnInstanceInitialized;

        try
        {
            // Act
            runner.RunScenario (scenarioName, false);

            // Assert - the scenario start position should have been updated
            Assert.True (global::UICatalog.UICatalog.LogCapture.ScenarioStartPosition > preScenarioPosition,
                         "MarkScenarioStart should have been called, updating the position");

            // Scenario logs should not contain the pre-scenario log
            string scenarioLogs = global::UICatalog.UICatalog.LogCapture.GetScenarioLogs ();
            Assert.DoesNotContain ("Pre-scenario log", scenarioLogs);
        }
        finally
        {
            quitTimer?.Dispose ();
            Application.InstanceInitialized -= OnInstanceInitialized;
            ConfigurationManager.Disable (true);
            ConfigurationManager.RuntimeConfig = null;

            // Reset model tracking so other tests can use either model
            ApplicationImpl.ResetModelUsageTracking ();
        }

        return;

        void OnInstanceInitialized (object? sender, EventArgs<IApplication> e)
        {
            currentApp = e.Value;

            quitTimer = new Timer (_ =>
                                   {
                                       try
                                       {
                                           currentApp?.RequestStop ();
                                       }
                                       catch
                                       { /* ignore */
                                       }
                                   },
                                   null,
                                   500,
                                   Timeout.Infinite);
        }
    }

    [Fact]
    public void DisplayResultsUI_EmptyResults_ReturnsImmediately ()
    {
        // Arrange
        List<BenchmarkResults> emptyResults = [];

        // Act & Assert - should not throw and should return immediately
        Exception? ex = Record.Exception (() => Runner.DisplayResultsUI (emptyResults));
        Assert.Null (ex);
    }
}
