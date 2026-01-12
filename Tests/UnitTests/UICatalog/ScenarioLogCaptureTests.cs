#nullable enable

// Claude - Opus 4.5
using Microsoft.Extensions.Logging;
using UICatalog;
using Xunit.Abstractions;

namespace UnitTests.UICatalog;

public class ScenarioLogCaptureTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Fact]
    public void Constructor_InitializesWithEmptyState ()
    {
        // Arrange & Act
        ScenarioLogCapture capture = new ();

        // Assert
        Assert.False (capture.HasErrors);
        Assert.Equal (0, capture.ScenarioStartPosition);
        Assert.Equal (string.Empty, capture.GetScenarioLogs ());
        Assert.Equal (string.Empty, capture.GetAllLogs ());
    }

    [Fact]
    public void CreateLogger_CapturesMessage ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");

        // Act
        logger.LogInformation ("Test message");

        // Assert
        string logs = capture.GetAllLogs ();
        Assert.Contains ("[Information] Test message", logs);
    }

    [Fact]
    public void CreateLogger_MultipleMessages_CapturesAll ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");

        // Act
        logger.LogDebug ("Debug message");
        logger.LogInformation ("Info message");
        logger.LogWarning ("Warning message");

        // Assert
        string logs = capture.GetAllLogs ();
        Assert.Contains ("[Debug] Debug message", logs);
        Assert.Contains ("[Information] Info message", logs);
        Assert.Contains ("[Warning] Warning message", logs);
    }

    [Fact]
    public void HasErrors_FalseForNonErrorLevels ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");

        // Act
        logger.LogTrace ("Trace");
        logger.LogDebug ("Debug");
        logger.LogInformation ("Info");
        logger.LogWarning ("Warning");

        // Assert
        Assert.False (capture.HasErrors);
    }

    [Fact]
    public void HasErrors_TrueForErrorLevel ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");

        // Act
        logger.LogError ("Error message");

        // Assert
        Assert.True (capture.HasErrors);
    }

    [Fact]
    public void HasErrors_TrueForCriticalLevel ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");

        // Act
        logger.LogCritical ("Critical message");

        // Assert
        Assert.True (capture.HasErrors);
    }

    [Fact]
    public void MarkScenarioStart_SetsPositionAndResetsErrors ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");
        logger.LogError ("Pre-scenario error");
        Assert.True (capture.HasErrors);

        // Act
        capture.MarkScenarioStart ();

        // Assert
        Assert.False (capture.HasErrors);
        Assert.True (capture.ScenarioStartPosition > 0);
    }

    [Fact]
    public void GetScenarioLogs_ReturnsOnlyLogsSinceMark ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");
        logger.LogInformation ("Before mark");

        // Act
        capture.MarkScenarioStart ();
        logger.LogInformation ("After mark");

        // Assert
        string scenarioLogs = capture.GetScenarioLogs ();
        string allLogs = capture.GetAllLogs ();

        Assert.DoesNotContain ("Before mark", scenarioLogs);
        Assert.Contains ("After mark", scenarioLogs);
        Assert.Contains ("Before mark", allLogs);
        Assert.Contains ("After mark", allLogs);
    }

    [Fact]
    public void GetScenarioLogs_ReturnsEmptyWhenNoLogsSinceMark ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");
        logger.LogInformation ("Before mark");
        capture.MarkScenarioStart ();

        // Act
        string scenarioLogs = capture.GetScenarioLogs ();

        // Assert
        Assert.Equal (string.Empty, scenarioLogs);
    }

    [Fact]
    public void Clear_ResetsAllState ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");
        logger.LogError ("Error message");
        capture.MarkScenarioStart ();
        logger.LogInformation ("After mark");

        // Act
        capture.Clear ();

        // Assert
        Assert.False (capture.HasErrors);
        Assert.Equal (0, capture.ScenarioStartPosition);
        Assert.Equal (string.Empty, capture.GetScenarioLogs ());
        Assert.Equal (string.Empty, capture.GetAllLogs ());
    }

    [Fact]
    public void CreateLogger_ReturnsWorkingLogger ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("TestCategory");

        // Act
        logger.LogInformation ("Info from ILogger");
        logger.LogError ("Error from ILogger");

        // Assert
        string logs = capture.GetAllLogs ();
        Assert.Contains ("Info from ILogger", logs);
        Assert.Contains ("Error from ILogger", logs);
        Assert.True (capture.HasErrors);
    }

    [Fact]
    public void CreateLogger_IsEnabledReturnsTrue ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("TestCategory");

        // Assert
        Assert.True (logger.IsEnabled (LogLevel.Trace));
        Assert.True (logger.IsEnabled (LogLevel.Debug));
        Assert.True (logger.IsEnabled (LogLevel.Information));
        Assert.True (logger.IsEnabled (LogLevel.Warning));
        Assert.True (logger.IsEnabled (LogLevel.Error));
        Assert.True (logger.IsEnabled (LogLevel.Critical));
    }

    [Fact]
    public void CreateLogger_BeginScopeReturnsNull ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("TestCategory");

        // Act
        IDisposable? scope = logger.BeginScope ("test scope");

        // Assert
        Assert.Null (scope);
    }

    [Fact]
    public void MultipleScenarios_EachGetsSeparateLogs ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");

        // Scenario 1
        capture.MarkScenarioStart ();
        logger.LogInformation ("Scenario 1 log");
        string scenario1Logs = capture.GetScenarioLogs ();

        // Scenario 2
        capture.MarkScenarioStart ();
        logger.LogInformation ("Scenario 2 log");
        string scenario2Logs = capture.GetScenarioLogs ();

        // Assert
        Assert.Contains ("Scenario 1 log", scenario1Logs);
        Assert.DoesNotContain ("Scenario 2 log", scenario1Logs);

        Assert.Contains ("Scenario 2 log", scenario2Logs);
        Assert.DoesNotContain ("Scenario 1 log", scenario2Logs);

        // All logs should contain both
        string allLogs = capture.GetAllLogs ();
        Assert.Contains ("Scenario 1 log", allLogs);
        Assert.Contains ("Scenario 2 log", allLogs);
    }

    [Fact]
    public void HasErrors_ResetsPerScenario ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");

        // Scenario 1 - has errors
        capture.MarkScenarioStart ();
        logger.LogError ("Error in scenario 1");
        Assert.True (capture.HasErrors);

        // Scenario 2 - no errors
        capture.MarkScenarioStart ();
        logger.LogInformation ("Info in scenario 2");

        // Assert - HasErrors should be false for scenario 2
        Assert.False (capture.HasErrors);
    }

    [Fact]
    public void Dispose_DoesNotThrow ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        ILogger logger = capture.CreateLogger ("Test");
        logger.LogInformation ("Some log");

        // Act & Assert - should not throw
        Exception? ex = Record.Exception (() => capture.Dispose ());
        Assert.Null (ex);
    }

    [Fact]
    public void ThreadSafety_ConcurrentLogging ()
    {
        // Arrange
        ScenarioLogCapture capture = new ();
        var threadCount = 10;
        var logsPerThread = 100;
        List<Task> tasks = [];

        // Act - log from multiple threads concurrently
        for (var t = 0; t < threadCount; t++)
        {
            int threadId = t;

            tasks.Add (
                       Task.Run (() =>
                                 {
                                     ILogger logger = capture.CreateLogger ($"Thread{threadId}");

                                     for (var i = 0; i < logsPerThread; i++)
                                     {
                                         logger.LogInformation ($"Thread {threadId} Log {i}");
                                     }
                                 }));
        }

        Task.WaitAll ([.. tasks]);

        // Assert - all logs should be captured (check by counting lines)
        string allLogs = capture.GetAllLogs ();
        int lineCount = allLogs.Split ('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.Equal (threadCount * logsPerThread, lineCount);
    }
}
