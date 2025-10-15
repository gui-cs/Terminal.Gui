using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Moq;

namespace Terminal.Gui.ApplicationTests;
public class MainLoopTTests
{
    //[Fact]
    //public void MainLoopT_NotInitialized_Throws()
    //{
    //    var m = new MainLoop<int> ();

    //    Assert.Throws<NotInitializedException> (() => m.TimedEvents);
    //    Assert.Throws<NotInitializedException> (() => m.InputBuffer);
    //    Assert.Throws<NotInitializedException> (() => m.InputProcessor);
    //    Assert.Throws<NotInitializedException> (() => m.Out);
    //    Assert.Throws<NotInitializedException> (() => m.AnsiRequestScheduler);
    //    Assert.Throws<NotInitializedException> (() => m.WindowSizeMonitor);

    //    var componentFactory = new Mock<IComponentFactory<int>> ();

    //    componentFactory.Setup (
    //                            c => c.CreateWindowSizeMonitor (
    //                                                            It.IsAny<IConsoleOutput> (),
    //                                                            It.IsAny<IOutputBuffer> ()))
    //                    .Returns (Mock.Of <IWindowSizeMonitor>());

    //    m.Initialize (new TimedEvents (),
    //                  new ConcurrentQueue<int> (),
    //                  Mock.Of <IInputProcessor>(),
    //                  Mock.Of<IConsoleOutput>(),
    //                  componentFactory.Object
    //                 );

    //    Assert.NotNull (m.TimedEvents);
    //    Assert.NotNull (m.InputBuffer);
    //    Assert.NotNull (m.InputProcessor);
    //    Assert.NotNull (m.Out);
    //    Assert.NotNull (m.AnsiRequestScheduler);
    //    Assert.NotNull (m.WindowSizeMonitor);
    //}
}
