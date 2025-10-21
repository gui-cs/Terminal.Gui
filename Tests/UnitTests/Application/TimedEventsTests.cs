using System.Diagnostics;

namespace UnitTests.ApplicationTests;

/// <summary>
/// Tests for TimedEvents class, focusing on high-resolution timing with Stopwatch.
/// </summary>
public class TimedEventsTests
{
    [Fact]
    public void HighFrequency_Concurrent_Invocations_No_Lost_Timeouts ()
    {
        var timedEvents = new Terminal.Gui.App.TimedEvents ();
        var counter = 0;
        var expected = 1000;
        var completed = new ManualResetEventSlim (false);

        // Add many timeouts with TimeSpan.Zero concurrently
        Parallel.For (0, expected, i =>
        {
            timedEvents.Add (TimeSpan.Zero, () =>
            {
                var current = Interlocked.Increment (ref counter);
                if (current == expected)
                {
                    completed.Set ();
                }
                return false; // One-shot
            });
        });

        // Run timers multiple times to ensure all are processed
        for (int i = 0; i < 10; i++)
        {
            timedEvents.RunTimers ();
            if (completed.IsSet)
            {
                break;
            }
            Thread.Sleep (10);
        }

        Assert.Equal (expected, counter);
    }

    [Fact]
    public void GetTimestampTicks_Provides_High_Resolution ()
    {
        var timedEvents = new Terminal.Gui.App.TimedEvents ();
        
        // Add multiple timeouts with TimeSpan.Zero rapidly
        var timestamps = new List<long> ();
        
        // Single event handler to capture all timestamps
        EventHandler<Terminal.Gui.App.TimeoutEventArgs>? handler = null;
        handler = (s, e) =>
        {
            timestamps.Add (e.Ticks);
        };
        
        timedEvents.Added += handler;
        
        for (int i = 0; i < 100; i++)
        {
            timedEvents.Add (TimeSpan.Zero, () => false);
        }
        
        timedEvents.Added -= handler;

        // Verify that we got timestamps
        Assert.True (timestamps.Count > 0, $"Should have captured timestamps. Got {timestamps.Count}");
        
        // Verify that we got unique timestamps (or very close)
        // With Stopwatch, we should have much better resolution than DateTime.UtcNow
        var uniqueTimestamps = timestamps.Distinct ().Count ();
        
        // We should have mostly unique timestamps
        // Allow some duplicates due to extreme speed, but should be > 50% unique
        Assert.True (uniqueTimestamps > timestamps.Count / 2, 
            $"Expected more unique timestamps. Got {uniqueTimestamps} unique out of {timestamps.Count} total");
    }

    [Fact]
    public void TimeSpan_Zero_Executes_Immediately ()
    {
        var timedEvents = new Terminal.Gui.App.TimedEvents ();
        var executed = false;

        timedEvents.Add (TimeSpan.Zero, () =>
        {
            executed = true;
            return false;
        });

        // Should execute on first RunTimers call
        timedEvents.RunTimers ();

        Assert.True (executed);
    }

    [Fact]
    public void Multiple_TimeSpan_Zero_Timeouts_All_Execute ()
    {
        var timedEvents = new Terminal.Gui.App.TimedEvents ();
        var executeCount = 0;
        var expected = 100;

        for (int i = 0; i < expected; i++)
        {
            timedEvents.Add (TimeSpan.Zero, () =>
            {
                Interlocked.Increment (ref executeCount);
                return false;
            });
        }

        // Run timers once
        timedEvents.RunTimers ();

        Assert.Equal (expected, executeCount);
    }

    [Fact]
    public void Stopwatch_Based_Timing_More_Precise_Than_DateTime ()
    {
        // Measure resolution by sampling multiple times rapidly
        var datetimeSamples = new List<long> ();
        var stopwatchSamples = new List<long> ();

        for (int i = 0; i < 1000; i++)
        {
            datetimeSamples.Add (DateTime.UtcNow.Ticks);
            stopwatchSamples.Add (Stopwatch.GetTimestamp () * TimeSpan.TicksPerSecond / Stopwatch.Frequency);
        }

        var datetimeUnique = datetimeSamples.Distinct ().Count ();
        var stopwatchUnique = stopwatchSamples.Distinct ().Count ();

        // Stopwatch should provide more unique values (better resolution)
        Assert.True (stopwatchUnique >= datetimeUnique,
            $"Stopwatch should have equal or better resolution. DateTime: {datetimeUnique}, Stopwatch: {stopwatchUnique}");
    }
}
