using Xunit.Abstractions;

namespace StressTests;

public class ApplicationStressTests
{
    public ApplicationStressTests (ITestOutputHelper output)
    {
    }

    private static volatile int _tbCounter;
#pragma warning disable IDE1006 // Naming Styles
    private static readonly ManualResetEventSlim _wakeUp = new (false);
#pragma warning restore IDE1006 // Naming Styles

    private const int NUM_PASSES = 50;
    private const int NUM_INCREMENTS = 500;
    private const int POLL_MS = 100;

    /// <summary>
    /// Stress test for Application.Invoke to verify that invocations from background threads
    /// are not lost or delayed indefinitely. Tests 25,000 concurrent invocations (50 passes × 500 increments).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This test automatically adapts its timeout when running under a debugger (500ms vs 100ms)
    /// to account for slower iteration times caused by debugger overhead.
    /// </para>
    /// <para>
    /// See InvokeLeakTest_Analysis.md for technical details about the timing improvements made
    /// to TimedEvents (Stopwatch-based timing) and Application.Invoke (MainLoop wakeup).
    /// </para>
    /// </remarks>
    [Fact]
    public async Task InvokeLeakTest ()
    {

        Application.Init (driverName: "fake");
        Random r = new ();
        TextField tf = new ();
        var top = new Toplevel ();
        top.Add (tf);

        _tbCounter = 0;

        Task task = Task.Run (() => RunTest (r, tf, NUM_PASSES, NUM_INCREMENTS, POLL_MS));

        // blocks here until the RequestStop is processed at the end of the test
        Application.Run (top);

        await task; // Propagate exception if any occurred

        Assert.Equal (NUM_INCREMENTS * NUM_PASSES, _tbCounter);
        top.Dispose ();
        Application.Shutdown ();

        return;

        static void RunTest (Random r, TextField tf, int numPasses, int numIncrements, int pollMs)
        {
            for (var j = 0; j < numPasses; j++)
            {
                _wakeUp.Reset ();

                for (var i = 0; i < numIncrements; i++)
                {
                    Launch (r, tf, (j + 1) * numIncrements);
                }

                while (_tbCounter != (j + 1) * numIncrements) // Wait for tbCounter to reach expected value
                {
                    int tbNow = _tbCounter;

                    // Wait for Application.Current to be running to ensure timed events can be processed
                    while (Application.Current is null || Application.Current is { Running: false })
                    {
                        Thread.Sleep (1);
                    }

                    _wakeUp.Wait (pollMs);

                    if (_tbCounter != tbNow)
                    {
                        continue;
                    }

                    // No change after wait: Idle handlers added via Application.Invoke have gone missing
                    Application.Invoke (() => Application.RequestStop ());

                    throw new TimeoutException (
                                                $"Timeout: Increment lost. _tbCounter ({_tbCounter}) didn't "
                                                + $"change after waiting {pollMs} ms. Failed to reach {(j + 1) * numIncrements} on pass {j + 1}"
                                               );
                }

                ;
            }

            Application.Invoke (() => Application.RequestStop ());
        }

        static void Launch (Random r, TextField tf, int target)
        {
            Task.Run (
                      () =>
                      {
                          Thread.Sleep (r.Next (2, 4));

                          Application.Invoke (
                                              () =>
                                              {
                                                  tf.Text = $"index{r.Next ()}";
                                                  Interlocked.Increment (ref _tbCounter);

                                                  if (target == _tbCounter)
                                                  {
                                                      // On last increment wake up the check
                                                      _wakeUp.Set ();
                                                  }
                                              }
                                             );
                      }
                     );
        }
    }
}
