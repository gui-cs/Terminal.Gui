using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class MainLoopTTests
{
    [Fact]
    public void MainLoopT_NotInitialized_Throws()
    {
        var m = new MainLoop<int> ();

        Assert.Throws<NotInitializedException> (() => m.TimedEvents);
        Assert.Throws<NotInitializedException> (() => m.InputBuffer);
        Assert.Throws<NotInitializedException> (() => m.InputProcessor);
        Assert.Throws<NotInitializedException> (() => m.Out);
        Assert.Throws<NotInitializedException> (() => m.AnsiRequestScheduler);
        Assert.Throws<NotInitializedException> (() => m.WindowSizeMonitor);

        m.Initialize (new TimedEvents (),
                      new ConcurrentQueue<int> (),
                      Mock.Of <IInputProcessor>(),
                      Mock.Of<IConsoleOutput>());

        Assert.NotNull (m.TimedEvents);
        Assert.NotNull (m.InputBuffer);
        Assert.NotNull (m.InputProcessor);
        Assert.NotNull (m.Out);
        Assert.NotNull (m.AnsiRequestScheduler);
        Assert.NotNull (m.WindowSizeMonitor);
    }
}
