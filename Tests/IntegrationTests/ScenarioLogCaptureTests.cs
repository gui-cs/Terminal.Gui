using Microsoft.Extensions.Logging;
using UICatalog;

namespace IntegrationTests;

public class ScenarioLogCaptureTests
{
    [Fact]
    public void LogBuffer_IsTrimmed_WhenItGrowsTooLarge ()
    {
        var capture = new ScenarioLogCapture ();
        ILogger logger = capture.CreateLogger ("test");
        string message = new ('x', 4096);

        for (var i = 0; i < 120; i++)
        {
            logger.LogInformation ("{Message}", message);
        }

        Assert.True (capture.GetAllLogs ().Length <= 256_000);
    }

    [Fact]
    public void GetScenarioLogs_RespectsScenarioStart_AfterTrim ()
    {
        var capture = new ScenarioLogCapture ();
        ILogger logger = capture.CreateLogger ("test");
        string message = new ('y', 4096);

        for (var i = 0; i < 120; i++)
        {
            logger.LogInformation ("{Message}", message);
        }

        capture.MarkScenarioStart ();
        logger.LogInformation ("after-start");

        string logs = capture.GetScenarioLogs ();
        Assert.Contains ("after-start", logs);
    }
}
