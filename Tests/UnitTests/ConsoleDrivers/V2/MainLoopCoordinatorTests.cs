using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.ConsoleDrivers.V2;
public class MainLoopCoordinatorTests
{
    [Fact]
    public void TestMainLoopCoordinator_InputCrashes_ExceptionSurfacesMainThread ()
    {

        var mockLogger = new Mock<ILogger> ();

        var beforeLogger = Logging.Logger;
        Logging.Logger = mockLogger.Object;

        var c = new MainLoopCoordinator<char> (new TimedEvents (),
                                               // Runs on a separate thread (input thread)
                                               () => throw new Exception ("Crash on boot"),

                                               // Rest runs on main thread
                                               new ConcurrentQueue<char> (),
                                               Mock.Of <IInputProcessor>(),
                                               ()=>Mock.Of<IConsoleOutput>(),
                                               Mock.Of<IMainLoop<char>>());

        // StartAsync boots the main loop and the input thread. But if the input class bombs
        // on startup it is important that the exception surface at the call site and not lost
        var ex = Assert.ThrowsAsync<AggregateException>(c.StartAsync).Result;
        Assert.Equal ("Crash on boot", ex.InnerExceptions [0].Message);


        // Restore the original null logger to be polite to other tests
        Logging.Logger = beforeLogger;


        // Logs should explicitly call out that input loop crashed.
        mockLogger.Verify (
                           l => l.Log (LogLevel.Critical,
                                       It.IsAny<EventId> (),
                                       It.Is<It.IsAnyType> ((v, t) => v.ToString () == "Input loop crashed"),
                                       It.IsAny<Exception> (),
                                       It.IsAny<Func<It.IsAnyType, Exception, string>> ())
                         , Times.Once);
    }
    /*
    [Fact]
    public void TestMainLoopCoordinator_InputExitsImmediately_ExceptionRaisedInMainThread ()
    {

        // Runs on a separate thread (input thread)
        // But because it's just a mock it immediately exists
        var mockInputFactoryMethod = () => Mock.Of<IConsoleInput<char>> ();


        var mockOutput = Mock.Of<IConsoleOutput> ();
        var mockInputProcessor = Mock.Of<IInputProcessor> ();
        var inputQueue = new ConcurrentQueue<char> ();
        var timedEvents = new TimedEvents ();

        var mainLoop = new MainLoop<char> ();
        mainLoop.Initialize (timedEvents,
                      inputQueue,
                      mockInputProcessor,
                      mockOutput
                      );

        var c = new MainLoopCoordinator<char> (timedEvents,
                                               mockInputFactoryMethod,
                                               inputQueue,
                                               mockInputProcessor,
                                               ()=>mockOutput,
                                               mainLoop
                                               );

        // TODO: This test has race condition
        //
        // * When the input loop exits it can happen
        // * - During boot
        // * - After boot
        // *
        // * If it happens in boot you get input exited
        // * If it happens after you get "Input loop exited early (stop not called)"
        //

        // Because the console input class does not block - i.e. breaks contract
        // We need to let the user know input has silently exited and all has gone bad.
        var ex = Assert.ThrowsAsync<Exception> (c.StartAsync).Result;
        Assert.Equal ("Input loop exited during startup instead of entering read loop properly (i.e. and blocking)", ex.Message);
    }*/
}
