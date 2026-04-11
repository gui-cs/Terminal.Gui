namespace DriverTests.AnsiHandling;

// Copilot

[Collection ("Driver Tests")]
public class AnsiStartupGateTests
{
    [Fact]
    public void RegisterQuery_And_MarkComplete_MakesGateReady ()
    {
        DateTime nowUtc = DateTime.UtcNow;
        AnsiStartupGate gate = new (() => nowUtc);

        gate.RegisterQuery ("q1", TimeSpan.FromMilliseconds (500));

        Assert.False (gate.IsReady);
        Assert.Equal (["q1"], gate.PendingQueryNames);

        gate.MarkComplete ("q1");

        Assert.True (gate.IsReady);
        Assert.Empty (gate.PendingQueryNames);
    }

    [Fact]
    public void RegisterQuery_TimesOut_And_MakesGateReady ()
    {
        DateTime nowUtc = DateTime.UtcNow;
        AnsiStartupGate gate = new (() => nowUtc);

        gate.RegisterQuery ("q1", TimeSpan.FromMilliseconds (100));

        Assert.False (gate.IsReady);
        Assert.Equal (["q1"], gate.PendingQueryNames);

        nowUtc = nowUtc.AddMilliseconds (101);

        Assert.True (gate.IsReady);
        Assert.Empty (gate.PendingQueryNames);
    }

    [Fact]
    public void RegisterMultipleQueries_TracksOnlyPendingNames ()
    {
        DateTime nowUtc = DateTime.UtcNow;
        AnsiStartupGate gate = new (() => nowUtc);

        gate.RegisterQuery ("q1", TimeSpan.FromSeconds (1));
        gate.RegisterQuery ("q2", TimeSpan.FromSeconds (1));

        gate.MarkComplete ("q1");

        Assert.False (gate.IsReady);
        Assert.Equal (["q2"], gate.PendingQueryNames);
    }
}
