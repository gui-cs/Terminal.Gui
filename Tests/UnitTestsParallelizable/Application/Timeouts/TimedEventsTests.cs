#nullable enable
namespace ApplicationTests.TimedEventTests;

/// <summary>
///     Tests for TimedEvents class, focusing on high-resolution timing with Stopwatch.
/// </summary>
[Collection("Application Timer Tests")]
public class TimedEventsTests
{
    [Fact]
    public void HighFrequency_Concurrent_Invocations_No_Lost_Timeouts ()
    {
        var timedEvents = new TimedEvents ();
        var counter = 0;
        var expected = 1000;
        var completed = new ManualResetEventSlim (false);

        // Add many timeouts with TimeSpan.Zero concurrently
        Parallel.For (
                      0,
                      expected,
                      i =>
                      {
                          timedEvents.Add (
                                           TimeSpan.Zero,
                                           () =>
                                           {
                                               int current = Interlocked.Increment (ref counter);

                                               if (current == expected)
                                               {
                                                   completed.Set ();
                                               }

                                               return false; // One-shot
                                           });
                      });

        // Run timers multiple times to ensure all are processed
        for (var i = 0; i < 10; i++)
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
        var timedEvents = new TimedEvents ();

        // Add multiple timeouts with TimeSpan.Zero rapidly
        List<long> timestamps = new ();

        // Single event handler to capture all timestamps
        EventHandler<TimeoutEventArgs>? handler = null;
        handler = (s, e) => { timestamps.Add (e.Ticks); };

        timedEvents.Added += handler;

        for (var i = 0; i < 100; i++)
        {
            timedEvents.Add (TimeSpan.Zero, () => false);
        }

        timedEvents.Added -= handler;

        // Verify that we got timestamps
        Assert.True (timestamps.Count > 0, $"Should have captured timestamps. Got {timestamps.Count}");

        // Verify that we got unique timestamps (or very close)
        // With Stopwatch, we should have much better resolution than DateTime.UtcNow
        int uniqueTimestamps = timestamps.Distinct ().Count ();

        // We should have mostly unique timestamps
        // Allow some duplicates due to extreme speed, but should be > 50% unique
        Assert.True (
                     uniqueTimestamps > timestamps.Count / 2,
                     $"Expected more unique timestamps. Got {uniqueTimestamps} unique out of {timestamps.Count} total");
    }

    [Fact]
    public void TimeSpan_Zero_Executes_Immediately ()
    {
        var timedEvents = new TimedEvents ();
        var executed = false;

        timedEvents.Add (
                         TimeSpan.Zero,
                         () =>
                         {
                             executed = true;

                             return false;
                         });

        Assert.True (timedEvents.Timeouts.Keys [0] > 0);

        // Should execute on first RunTimers call
        timedEvents.RunTimers ();

        Assert.Empty (timedEvents.Timeouts);
        Assert.True (executed);
    }

    [Fact]
    public void Multiple_TimeSpan_Zero_Timeouts_All_Execute ()
    {
        var timedEvents = new TimedEvents ();
        var executeCount = 0;
        var expected = 100;

        for (var i = 0; i < expected; i++)
        {
            timedEvents.Add (
                             TimeSpan.Zero,
                             () =>
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
    public void StopAll_Stops_All_Timeouts ()
    {
        var timedEvents = new TimedEvents ();
        var executeCount = 0;
        var expected = 100;

        for (var i = 0; i < expected; i++)
        {
            timedEvents.Add (
                             TimeSpan.Zero,
                             () =>
                             {
                                 Interlocked.Increment (ref executeCount);

                                 return false;
                             });
        }

        Assert.Equal (expected, timedEvents.Timeouts.Count);

        timedEvents.StopAll ();

        Assert.Empty (timedEvents.Timeouts);

        // Run timers once
        timedEvents.RunTimers ();

        Assert.Equal (0, executeCount);
    }
}
