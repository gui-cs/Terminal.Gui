// Alias Console to MockConsole so we don't accidentally use Console

using UnitTests;

namespace UnitTests.ApplicationTests;

public class SyncrhonizationContextTests
{
    [Fact]
    public void SynchronizationContext_CreateCopy ()
    {
        Application.Init ("fake");
        SynchronizationContext context = SynchronizationContext.Current;
        Assert.NotNull (context);

        SynchronizationContext contextCopy = context.CreateCopy ();
        Assert.NotNull (contextCopy);

        Assert.NotEqual (context, contextCopy);
        Application.Shutdown ();
    }

    private readonly object _lockPost = new ();

    [Theory]
    [InlineData ("fake")]
    [InlineData ("windows")]
    [InlineData ("dotnet")]
   // [InlineData ("unix")]
    public void SynchronizationContext_Post (string driverName = null)
    {
        lock (_lockPost)
        {
            Application.Init (driverName);

            SynchronizationContext context = SynchronizationContext.Current;

            var success = false;

            Task.Run (() =>
                      {
                          while (Application.Current is null || Application.Current is { IsRunning: false })
                          {
                              Thread.Sleep (500);
                          }

                          // non blocking
                          context.Post (
                                        delegate
                                        {
                                            success = true;

                                            // then tell the application to quit
                                            Application.Invoke (() => Application.RequestStop ());
                                        },
                                        null
                                       );

                          if (Application.Current is { IsRunning: true })
                          {
                              Assert.False (success);
                          }
                      }
                     );

            // blocks here until the RequestStop is processed at the end of the test
            Application.Run ().Dispose ();
            Assert.True (success);

            Application.Shutdown ();
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void SynchronizationContext_Send ()
    {
        SynchronizationContext context = SynchronizationContext.Current;

        var success = false;

        Task.Run (
                  () =>
                  {
                      Thread.Sleep (500);

                      // blocking
                      context.Send (
                                    delegate
                                    {
                                        success = true;

                                        // then tell the application to quit
                                        Application.Invoke (() => Application.RequestStop ());
                                    },
                                    null
                                   );
                      Assert.True (success);
                  }
                 );

        // blocks here until the RequestStop is processed at the end of the test
        Application.Run ().Dispose ();
        Assert.True (success);
        Application.Shutdown ();
    }
}
