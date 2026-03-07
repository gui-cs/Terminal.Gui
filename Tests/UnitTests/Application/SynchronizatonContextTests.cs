// Alias Console to MockConsole so we don't accidentally use Console

using UnitTests;

namespace UnitTests.ApplicationTests;

public class SyncrhonizationContextTests
{
    [Fact]
    public void SynchronizationContext_CreateCopy ()
    {
        Application.Init (DriverRegistry.Names.ANSI);
        SynchronizationContext context = SynchronizationContext.Current;
        Assert.NotNull (context);

        SynchronizationContext contextCopy = context.CreateCopy ();
        Assert.NotNull (contextCopy);

        Assert.NotEqual (context, contextCopy);
        Application.Shutdown ();
    }

    private readonly object _lockPost = new ();

    [Theory]
    [InlineData (DriverRegistry.Names.ANSI)]
    [InlineData (DriverRegistry.Names.WINDOWS)]
    [InlineData (DriverRegistry.Names.DOTNET)]
    [InlineData (DriverRegistry.Names.UNIX)]
    public void SynchronizationContext_Post (string driverName = null)
    {
        lock (_lockPost)
        {
            Application.Init (driverName);

            SynchronizationContext context = SynchronizationContext.Current;

            var success = false;

            Task.Run (() =>
                      {
                          while (Application.TopRunnable is { IsRunning: false })
                          {
                              Thread.Sleep (500);
                          }

                          // non blocking
                          context.Post (delegate
                                        {
                                            success = true;

                                            // then tell the application to quit
                                            Application.Invoke (() => Application.RequestStop ());
                                        },
                                        null);

                          if (Application.TopRunnable is { IsRunning: true })
                          {
                              Assert.False (success);
                          }
                      },
                      TestContext.Current.CancellationToken);

            // blocks here until the RequestStop is processed at the end of the test
            Application.Run<Runnable> ();
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

        Task.Run (() =>
                  {
                      Thread.Sleep (500);

                      // blocking
                      context.Send (delegate
                                    {
                                        success = true;

                                        // then tell the application to quit
                                        Application.Invoke (() => Application.RequestStop ());
                                    },
                                    null);
                      Assert.True (success);
                  },
                  TestContext.Current.CancellationToken);

        // blocks here until the RequestStop is processed at the end of the test
        Application.Run<Runnable> ();
        Assert.True (success);
        Application.Shutdown ();
    }
}
