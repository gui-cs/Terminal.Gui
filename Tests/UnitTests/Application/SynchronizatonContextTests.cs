// Alias Console to MockConsole so we don't accidentally use Console

using UnitTests;

namespace Terminal.Gui.ApplicationTests;

public class SyncrhonizationContextTests
{
    [Fact]
    public void SynchronizationContext_CreateCopy ()
    {
        ConsoleDriver.RunningUnitTests = true;
        Application.Init ();
        SynchronizationContext context = SynchronizationContext.Current;
        Assert.NotNull (context);

        SynchronizationContext contextCopy = context.CreateCopy ();
        Assert.NotNull (contextCopy);

        Assert.NotEqual (context, contextCopy);
        Application.Shutdown ();
    }

    private object _lockPost = new ();

    [Theory]
    [InlineData (typeof (FakeDriver))]
    [InlineData (typeof (NetDriver))]
    [InlineData (typeof (WindowsDriver))]
    [InlineData (typeof (CursesDriver))]
    [InlineData (typeof (ConsoleDriverFacade<WindowsConsole.InputRecord>), "v2win")]
    [InlineData (typeof (ConsoleDriverFacade<ConsoleKeyInfo>), "v2net")]
    public void SynchronizationContext_Post (Type driverType, string driverName = null)
    {
        lock (_lockPost)
        {
            ConsoleDriver.RunningUnitTests = true;

            if (driverType.Name.Contains ("ConsoleDriverFacade"))
            {
                Application.Init (driverName: driverName);
            }
            else
            {
                Application.Init (driverName: driverType.Name);
            }

            SynchronizationContext context = SynchronizationContext.Current;

            var success = false;

            Task.Run (() =>
                      {
                          while (Application.Top is null || Application.Top is { Running: false })
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

                          if (Application.Top is { Running: true })
                          {
                              Assert.False (success);
                          }
                      }
                     );

            // blocks here until the RequestStop is processed at the end of the test
            Application.Run ().Dispose ();
            Assert.True (success);

            if (ApplicationImpl.Instance is ApplicationV2)
            {
                ApplicationImpl.Instance.Shutdown ();
            }
            else
            {
                Application.Shutdown ();
            }
        }
    }

    [Fact]
    [AutoInitShutdown]
    public void SynchronizationContext_Send ()
    {
        ConsoleDriver.RunningUnitTests = true;
        Application.Init ();
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
